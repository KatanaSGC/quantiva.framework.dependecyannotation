# QuantiveCorp.Framework.DependencyAnnotation

This package simplifies the implementation of dependency injection. It only requires adding one annotation to classes, avoiding the need to load Program or Startup at the dependency configuration level.

## Features

- **ScopeAnnotation**: Mark classes for scoped lifetime.
- **SingletonService**: Mark classes for singleton lifetime.
- **TransientService**: Mark classes for transient lifetime.

## Usage

```csharp
[SingletonService]
public class MyService
{
    // ...
}
```

## License

MIT
