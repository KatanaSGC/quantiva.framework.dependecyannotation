using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace quantiva.framework.dependecyannotation;

/// <summary>
/// Entry point of the library: scans assemblies and registers annotated classes in the DI container.
/// </summary>
public static class DependecyAnnotationConfig
{
    private static readonly (Type Attribute, ServiceLifetime Lifetime)[] Lifetimes =
    [
        (typeof(ScopeService), ServiceLifetime.Scoped),
        (typeof(SingletonService), ServiceLifetime.Singleton),
        (typeof(TransientService), ServiceLifetime.Transient)
    ];

    private static readonly string[] IgnoredAssemblyPrefixes = ["System", "Microsoft", "netstandard", "mscorlib", "WindowsBase"];

    /// <summary>
    /// Registers every class decorated with <see cref="ScopeService"/>, <see cref="SingletonService"/>
    /// or <see cref="TransientService"/> found in the assemblies loaded in the current <see cref="AppDomain"/>.
    /// Dynamic assemblies and framework assemblies (System*, Microsoft*, ...) are skipped.
    /// </summary>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
    /// <inheritdoc cref="AddDependencyAnnotation(IServiceCollection, Assembly[])" path="/remarks"/>
    public static IServiceCollection AddDependencyAnnotation(this IServiceCollection services)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !IsFrameworkAssembly(a));
        return services.AddDependencyAnnotation(assemblies.ToArray());
    }

    /// <summary>
    /// Registers annotated classes found only in the given assemblies.
    /// Use it to scan assemblies that are not loaded yet or to limit the scan.
    /// </summary>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <param name="assemblies">Assemblies to scan.</param>
    /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item>Only concrete classes are considered (abstract and open generic classes are ignored).</item>
    /// <item>The concrete type is always registered with its lifetime. Each implemented interface
    /// (except those in <c>System*</c>/<c>Microsoft*</c> namespaces, e.g. <see cref="IDisposable"/>)
    /// is forwarded to that registration, so all interfaces of a class resolve to the same instance.</item>
    /// <item>Idempotent: a class already registered as a service is skipped, so calling the method twice does not duplicate registrations.</item>
    /// <item>Types that cannot be loaded from an assembly (<see cref="ReflectionTypeLoadException"/>) are ignored instead of failing.</item>
    /// <item>A class with several lifetime attributes uses the first one in this order: scoped, singleton, transient.</item>
    /// </list>
    /// </remarks>
    public static IServiceCollection AddDependencyAnnotation(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        foreach (var assembly in assemblies.Distinct())
        foreach (var type in GetLoadableTypes(assembly))
        {
            if (!type.IsClass || type.IsAbstract || type.IsGenericTypeDefinition) continue;

            foreach (var (attribute, lifetime) in Lifetimes)
            {
                if (!type.IsDefined(attribute, true)) continue;
                Register(services, type, lifetime);
                break;
            }
        }

        return services;
    }

    private static void Register(IServiceCollection services, Type type, ServiceLifetime lifetime)
    {
        if (services.Any(d => d.ServiceType == type)) return;

        services.Add(new ServiceDescriptor(type, type, lifetime));
        foreach (var iface in type.GetInterfaces().Where(i => !IsFrameworkNamespace(i.Namespace)))
            services.Add(new ServiceDescriptor(iface, sp => sp.GetRequiredService(type), lifetime));
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.OfType<Type>();
        }
    }

    private static bool IsFrameworkAssembly(Assembly assembly)
    {
        var name = assembly.GetName().Name ?? string.Empty;
        return IgnoredAssemblyPrefixes.Any(p => name.StartsWith(p, StringComparison.Ordinal));
    }

    private static bool IsFrameworkNamespace(string? ns) =>
        ns is not null && (ns == "System" || ns.StartsWith("System.", StringComparison.Ordinal)
                           || ns == "Microsoft" || ns.StartsWith("Microsoft.", StringComparison.Ordinal));
}
