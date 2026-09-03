# Architecture & Standards Cheat Sheet

Condensed operational reference. Full detail: [`../knowledge-base/`](../knowledge-base/00-index.md)
files 01, 02, 03, 12.

## Layer map (dependencies point inward only)

```
Nop.Web (startup, storefront + Admin area) → Nop.Web.Framework → Nop.Services → Nop.Data → Nop.Core
src/Plugins/Nop.Plugin.{Group}.{Name}  (references Nop.Web.Framework; never referenced back by core)
```

- `Nop.Core` — domain entities (`BaseEntity` subclasses, no navigation properties), caching, events,
  `IEngine`/`EngineContext`. No project dependencies.
- `Nop.Data` — `IRepository<T>`, Linq2DB providers, FluentMigrator migrations/entity builders.
- `Nop.Services` — business logic, `ISettings` consumers, plugin base classes.
- `Nop.Web.Framework` — shared MVC (`BaseNopModel`, `NopViewComponent`, TagHelpers, `INopUrlHelper`).
- `Nop.Web` — storefront controllers/views + `Areas/Admin`.

## Dependency injection — `INopStartup`, never ad-hoc registration

```csharp
public class NopStartup : INopStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => services.AddScoped<IMyService, MyService>();
    public void Configure(IApplicationBuilder application) { }
    public int Order => 3000; // ascending order at startup; higher = later = can override
}
```
Auto-discovered by `ITypeFinder` (`Nop.Core.Infrastructure`) — no manual registration list anywhere.
`ITypeFinder` also auto-discovers: `IStartupTask`, `IOrderedMapperProfile` (AutoMapper),
`IEntityBuilder`/`INameCompatibility` (Linq2DB mapping), `IRouteProvider`, `IConsumer<T>` (events),
`IExternalAuthenticationRegistrar`. If you're manually wiring one of these into a master list, you're
fighting the framework.

## Data access — Linq2DB + FluentMigrator (no EF Core, ever)

```csharp
public class MyRecord : BaseEntity { public int ProductId { get; set; } public string Note { get; set; } }

public class MyRecordBuilder : NopEntityBuilder<MyRecord>
{
    public override void MapEntity(CreateTableExpressionBuilder table) =>
        table.WithColumn(nameof(MyRecord.Id)).AsInt32().PrimaryKey()
             .WithColumn(nameof(MyRecord.ProductId)).AsInt32().ForeignKey<Product>(onDelete: Rule.Cascade)
             .WithColumn(nameof(MyRecord.Note)).AsString(400);
}

[NopMigration("2026-08-30 00:00:00", "MyPlugin base schema", MigrationProcessType.Installation)]
public class SchemaMigration : ForwardOnlyMigration
{
    public override void Up() => this.CreateTableIfNotExists<MyRecord>();
}
```
`CreateTableIfNotExists<T>()`/`AddOrAlterColumnFor<T>(x => x.Prop)` are verified `MigrationBase`
extensions in `Nop.Data.Extensions.FluentMigratorExtensions` — use these, not the `Create.TableFor<T>()`
name that appears in some older nopCommerce doc pages (that API doesn't exist in this codebase).
Inject `IRepository<MyRecord>` — needs no manual DI registration (generic factory resolves it).
Full walkthrough + attribute reference:
[knowledge-base/03](../knowledge-base/03-data-access-linq2db-fluentmigrator.md).

## Settings

```csharp
public partial class MySettings : ISettings { public bool SomeFlag { get; set; } }
```
Inject the settings class directly; save via `ISettingService.SaveSettingAsync(instance)`. Reserve
`GetSettingByKeyAsync`/`SetSettingAsync(key, value)` for genuinely dynamic keys only.

## Localization

`ILocalizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string,string> {...})` in
`InstallAsync`; read via `ILocalizationService.GetResourceAsync("key")` or the
`[NopResourceDisplayName("key")]` attribute on view model properties.

## Caching

`IStaticCacheManager` (in-process) is the default; for multi-instance ECS deployments, Redis is
configured via `DistributedCacheConfig` in `appsettings.json` (`Enabled: true`,
`DistributedCacheType: Redis` or `RedisSynchronizedMemory`) — required once more than one app
instance runs behind a load balancer, or cached data goes incoherent across instances. See
[knowledge-base/10](../knowledge-base/10-configuration-appsettings.md).

## Coding standards (enforced by root `.editorconfig`)

Allman braces · `var` everywhere · no `this.` qualifier · C# keyword aliases (`int` not `Int32`) ·
expression-bodied simple properties/lambdas, block-bodied methods/constructors · pattern matching
over cast-then-check · `?.`/`??` over null checks · `_camelCase` private fields · `PascalCase`
everything else · `I`-prefixed interfaces · `SCREAMING_SNAKE_CASE` constants. Full table:
[knowledge-base/12](../knowledge-base/12-coding-standards.md).

## Validation

FluentValidation only (`BaseNopValidator<TModel>`, `RuleFor(...)`) — never DataAnnotations. See
[knowledge-base/08](../knowledge-base/08-settings-permissions-validation.md).

## Events

`IEventPublisher.PublishAsync(new SomeEvent(...))` to publish; `IConsumer<SomeEvent>` to subscribe
(auto-discovered). Every `BaseEntity` already gets free `EntityInsertedEvent<T>`/`EntityUpdatedEvent<T>`/
`EntityDeletedEvent<T>` — use these instead of hand-rolling change notifications. See
[knowledge-base/07](../knowledge-base/07-events-and-scheduled-tasks.md).
