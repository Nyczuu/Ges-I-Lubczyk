# Dependency Injection & ITypeFinder

Source: adapted from `developer/tutorials/inversion-of-control.html` and
`developer/tutorials/type-finder.html`, verified against `src/Libraries/Nop.Core/Infrastructure`.

## INopStartup — never register services ad hoc

nopCommerce uses ASP.NET Core's built-in `IServiceProvider`, but service registration is not done in
`Program.cs` directly. Instead, every module (core or plugin) implements `INopStartup`
(`Nop.Core.Infrastructure` namespace, `[verified: src/Libraries/Nop.Core/Infrastructure/INopStartup.cs]`):

```csharp
public interface INopStartup
{
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);
    void Configure(IApplicationBuilder application);
    int Order { get; }
}
```

Implementation pattern used throughout the codebase (e.g. `NopStartup` classes in `src/Libraries/*`
and every plugin's `Infrastructure/NopStartup.cs` or `PluginNopStartup.cs`):

```csharp
public class NopStartup : INopStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IMyPluginService, MyPluginService>();
    }

    public void Configure(IApplicationBuilder application) { }

    public int Order => 3000; // higher = registered later = can override earlier registrations
}
```

- `NopEngine` (`src/Libraries/Nop.Core/Infrastructure/NopEngine.cs`) discovers every `INopStartup`
  implementation via `ITypeFinder`, sorts by `Order` ascending, and runs `ConfigureServices` /
  `Configure` in that sequence.
- To **override** a core service registration, register your replacement with an `Order` greater than
  the core class that first registered it — the later registration wins for a `TryAdd`-style pattern,
  or simply re-registers the descriptor. Prefer plugin-scoped overrides sparingly; this is the
  supported mechanism for engine-level customization when a plugin genuinely must swap a core
  implementation.
- **Never** call `services.AddScoped<T>()` directly in `Nop.Web`'s `Program.cs`/`Startup` for
  feature code — that bypasses the module system and breaks plugin parity. Always add a
  `NopStartup : INopStartup` class instead, even for engine-level (non-plugin) code.

## ITypeFinder — the reflection backbone

`Nop.Core.Infrastructure.ITypeFinder` (default impl `WebAppTypeFinder` → `AppDomainTypeFinder`) is
what makes the whole "drop a DLL in and it just works" plugin model possible:

```csharp
public interface ITypeFinder
{
    IEnumerable<Type> FindClassesOfType(bool onlyConcreteClasses = true);
    IEnumerable<Type> FindClassesOfType(Type assignTypeFrom, bool onlyConcreteClasses = true);
    IList<Assembly> GetAssemblies();
}
```

It scans every assembly in `\Bin` (not just the current AppDomain) to auto-discover implementations
of, among others:

| Interface | Purpose |
|---|---|
| `IStartupTask` | One-time init tasks run at app start |
| `INopStartup` | DI + middleware registration (see above) |
| `IOrderedMapperProfile` | AutoMapper profile registration |
| `IEntityBuilder`, `INameCompatibility` | Linq2DB entity mapping / legacy column naming |
| `IRouteProvider` | MVC route registration |
| `IConsumer<T>` | Event bus subscribers |
| `IExternalAuthenticationRegistrar` | External auth method registration |

**Consequence for AI-generated code**: you almost never need to manually "wire up" a mapping builder,
event consumer, or route provider into some master list — implement the interface, and
`ITypeFinder` + the relevant subsystem finds it automatically at startup. If you find yourself writing
code that manually enumerates or registers these, stop — that is very likely the wrong pattern for
this codebase.
