using Microsoft.Extensions.DependencyInjection;

namespace quantiva.framework.dependecyannotation;

/// <summary>
/// Marks a class to be registered automatically with the <see cref="ServiceLifetime.Transient"/> lifetime
/// when <see cref="DependecyAnnotationConfig.AddDependencyAnnotation(IServiceCollection)"/> is called.
/// </summary>
/// <remarks>
/// Lifetime: a new instance every time it is requested.
/// The concrete type is registered with this lifetime and every non-framework interface it implements
/// is forwarded to the same registration (same instance). The attribute is inherited by derived classes and can only be applied to classes.
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public class TransientService : Attribute
{
    
}