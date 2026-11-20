# QuantiveCorp.Framework.DependencyAnnotation

Attribute-based dependency injection registration for .NET. Add one attribute to a class and it is registered in the `IServiceCollection` automatically, with no manual `services.AddXxx(...)` lines in `Program.cs` or `Startup`.

- **Package id:** `QuantiveCorp.Framework.DependencyAnnotation`
- **Namespace:** `quantiva.framework.dependecyannotation` (note the spelling `dependecy`, kept as is)
- **Targets:** `net8.0`, `net9.0`, `net10.0`
- **Dependency:** `Microsoft.Extensions.DependencyInjection.Abstractions` (matching version per target)
- **License:** MIT

## Installation

```bash
dotnet add package QuantiveCorp.Framework.DependencyAnnotation
```

## Quick start

1. Annotate the implementation class:

```csharp
using quantiva.framework.dependecyannotation;

public interface IGreetingService { string Greet(string name); }

[SingletonService]
public class GreetingService : IGreetingService
{
    public string Greet(string name) => $"Hello, {name}!";
}
```

2. Call `AddDependencyAnnotation()` once at startup:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDependencyAnnotation();
var app = builder.Build();

app.MapGet("/greet/{name}", (string name, IGreetingService s) => s.Greet(name));
app.Run();
```

## API reference

| Type | Kind | Lifetime | Equivalent call |
|------|------|----------|-----------------|
| `ScopeService` | attribute | Scoped (one per scope / HTTP request) | `services.AddScoped(...)` |
| `SingletonService` | attribute | Singleton (one for the app lifetime) | `services.AddSingleton(...)` |
| `TransientService` | attribute | Transient (new instance per resolution) | `services.AddTransient(...)` |
| `AddDependencyAnnotation(this IServiceCollection)` | extension method | - | Scans loaded assemblies; returns the same `IServiceCollection` |
| `AddDependencyAnnotation(this IServiceCollection, params Assembly[])` | extension method | - | Scans only the given assemblies |

All attributes are declared with `[AttributeUsage(AttributeTargets.Class)]` (classes only, `Inherited` defaults to `true`, a single use per class by default).

> The scoped attribute class is named `ScopeService` (it lives in the file `ScopeAnnotation.cs`). Use `[ScopeService]`, not `[ScopeAnnotation]`.

## How it works

`AddDependencyAnnotation()`:

1. Gets the assemblies to scan (loaded, non-dynamic, non-framework ones by default).
2. For each concrete class, checks the attributes in order scoped, singleton, transient (inherited lookup).
3. For every selected type:
   - Registers the concrete type with the lifetime, then forwards each non-framework interface to it.
4. Returns the same `IServiceCollection`.

### Registration rules and examples

| Class | Registrations produced |
|-------|------------------------|
| `[SingletonService] class A : IA` | `A` and `IA` (forwarded to `A`), singleton |
| `[ScopeService] class B : IB, IC` | `B`, `IB`, `IC` - all resolve to the same instance per scope |
| `[TransientService] class C` | `C` (transient) |
| `[ScopeService] class D : IDisposable` | `D` only; `System*`/`Microsoft*` interfaces are never registered |
| abstract or open generic class | ignored |

## Behaviors and caveats

- **The concrete type is always registered**, and each interface is forwarded to it, so all interfaces of one class share the same instance (per lifetime rules).
- **Framework interfaces are skipped** (`IDisposable`, `IEquatable<T>`, ... any interface in a `System*`/`Microsoft*` namespace).
- **Assemblies scanned:** `AddDependencyAnnotation()` scans loaded non-dynamic assemblies excluding `System*`, `Microsoft*`, `netstandard`, `mscorlib`. Use `AddDependencyAnnotation(params Assembly[])` to scan specific assemblies (including ones not loaded yet).
- **Idempotent:** a class already registered as a service is skipped; calling the method twice does not duplicate registrations.
- **Resilient:** types that fail to load (`ReflectionTypeLoadException`) are ignored.
- **Multiple implementations of one interface** are all registered. `GetService<I>()` returns the last one; `GetServices<I>()` returns all.
- **Inherited attribute:** a concrete subclass of an annotated class is also registered with the base lifetime.
- **Several lifetime attributes** on one class: the first in the order scoped, singleton, transient wins.
- **Reflection:** runtime scan at startup; not compatible with trimming/NativeAOT.
- **Captive dependencies:** usual DI rules apply (a singleton must not depend on scoped services).

## Lifetime guide

| Use | When |
|-----|------|
| `[SingletonService]` | Stateless or thread-safe shared services, caches, configuration wrappers |
| `[ScopeService]` | Per-request state, units of work, `DbContext`-like services |
| `[TransientService]` | Lightweight, stateless, or short-lived objects |

## Sample project

[`TestWebApi`](../TestWebApi) is a minimal ASP.NET Core app that uses the package: `GreetingService` is annotated with `[SingletonService]`, `Program.cs` calls `AddDependencyAnnotation()`, and `GET /greet/{name}` resolves `IGreetingService`. Its `nuget.config` adds `../DependencyAnnotation/bin/Release` as a local package source, so run `dotnet pack -c Release` in `DependencyAnnotation` before restoring it.

## Building and packing

```bash
dotnet build
dotnet test DependencyAnnotation.Tests
dotnet pack DependencyAnnotation -c Release
```

## License

MIT
