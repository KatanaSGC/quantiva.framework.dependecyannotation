using Microsoft.Extensions.DependencyInjection;
using quantiva.framework.dependecyannotation;

namespace DependencyAnnotation.Tests;

public interface IA { }
public interface IB { }

[SingletonService] public class Multi : IA, IB, IDisposable { public void Dispose() { } }
[ScopeService] public class ScopedSvc : IA { }
[TransientService] public class TransientSvc { }
[SingletonService] public abstract class AbstractSvc : IB { }
[SingletonService] public class GenericSvc<T> { }
public class NotAnnotated { }
[ScopeService, SingletonService] public class TwoAttrs { }

public class DependencyAnnotationTests
{
    private static ServiceProvider Build(out IServiceCollection services)
    {
        services = new ServiceCollection().AddDependencyAnnotation(typeof(IA).Assembly);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    [Fact]
    public void Interfaces_ShareTheSameInstance()
    {
        using var sp = Build(out _);
        Assert.Same(sp.GetRequiredService<IB>(), sp.GetRequiredService<Multi>());
        using var scope = sp.CreateScope();
        Assert.Same(sp.GetRequiredService<Multi>(), scope.ServiceProvider.GetServices<IA>().OfType<Multi>().Single());
    }

    [Fact]
    public void Lifetimes_AreRespected()
    {
        using var sp = Build(out var services);
        Assert.Equal(ServiceLifetime.Scoped, services.Single(d => d.ServiceType == typeof(ScopedSvc)).Lifetime);
        Assert.NotSame(sp.GetRequiredService<TransientSvc>(), sp.GetRequiredService<TransientSvc>());
        using var s1 = sp.CreateScope();
        using var s2 = sp.CreateScope();
        Assert.Same(s1.ServiceProvider.GetRequiredService<ScopedSvc>(), s1.ServiceProvider.GetRequiredService<ScopedSvc>());
        Assert.NotSame(s1.ServiceProvider.GetRequiredService<ScopedSvc>(), s2.ServiceProvider.GetRequiredService<ScopedSvc>());
    }

    [Fact]
    public void FrameworkInterfaces_AbstractGenericAndPlainTypes_AreIgnored()
    {
        Build(out var services);
        Assert.DoesNotContain(services, d => d.ServiceType == typeof(IDisposable));
        Assert.DoesNotContain(services, d => d.ServiceType == typeof(AbstractSvc));
        Assert.DoesNotContain(services, d => d.ServiceType.IsGenericTypeDefinition);
        Assert.DoesNotContain(services, d => d.ServiceType == typeof(NotAnnotated));
    }

    [Fact]
    public void MultipleAttributes_RegisterOnce()
    {
        Build(out var services);
        Assert.Single(services, d => d.ServiceType == typeof(TwoAttrs));
    }

    [Fact]
    public void CallingTwice_DoesNotDuplicate()
    {
        var services = new ServiceCollection().AddDependencyAnnotation(typeof(IA).Assembly);
        var count = services.Count;
        services.AddDependencyAnnotation(typeof(IA).Assembly);
        Assert.Equal(count, services.Count);
    }

    [Fact]
    public void ParameterlessOverload_ScansLoadedAssemblies()
    {
        var services = new ServiceCollection().AddDependencyAnnotation();
        Assert.Contains(services, d => d.ServiceType == typeof(Multi));
    }
}
