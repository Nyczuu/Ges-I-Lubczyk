# Data Access: Linq2DB + FluentMigrator

Source: adapted from `developer/tutorials/migrations.html`, `developer/tutorials/db-schema.html`,
`developer/plugins/plugin-with-data-access.html`. Verified against `src/Libraries/Nop.Data`.

## Why not Entity Framework Core

Since **v4.30**, nopCommerce dropped EF Core entirely in favor of **Linq2DB** (ORM/data-access) +
**FluentMigrator** (schema evolution). Reasons documented by the nopCommerce team: full control over
generated SQL and query execution timing, and much simpler multi-database support (SQL Server, MySQL,
PostgreSQL all implement `INopDataProvider`). **There are no navigation properties** anywhere in the
domain model — Linq2DB does not support them, and this is intentional. Never add
`public virtual ICollection<X> Xs { get; set; }`-style navigation properties to a domain entity;
resolve related data explicitly through the appropriate service/repository call.

## Core building blocks

| Type | Role |
|---|---|
| `BaseEntity` (`Nop.Core`) | Base class for every persisted POCO; has `Id` only |
| `IRepository<TEntity>` (`Nop.Data`) | Facade over the data provider — inject this, never construct it |
| `INopDataProvider` | One implementation per DB engine (SqlServer/MySql/PostgreSQL) |
| `NopEntityBuilder<TEntity>` (abstract) | Declares column/FK mapping for `TEntity` via FluentMigrator's `CreateTableExpressionBuilder` |
| `IEntityBuilder` | Interface `NopEntityBuilder<T>` implements; auto-discovered by `ITypeFinder` |
| `MigrationBase` / `ForwardOnlyMigration` (FluentMigrator) | Base class for a versioned schema change |
| `[NopMigration]`, `[NopSchemaMigration]`, `[NopUpdateMigration]` | Attributes carrying a sortable timestamp instead of FluentMigrator's raw long version number |
| `INameCompatibility` / `BaseNameCompatibility` | Maps PascalCase C# names onto legacy `snake_case`/mixed-case columns for backward compatibility |
| `CreateTableIfNotExists<TEntity>()` | `MigrationBase` extension (`Nop.Data.Extensions.FluentMigratorExtensions`) — idempotent table creation from the matching `NopEntityBuilder<T>` |
| `AddOrAlterColumnFor<TEntity>(x => x.Prop)` | `MigrationBase` extension — idempotent add-or-alter of a single column by property selector; see [04-extending-core-entities.md](04-extending-core-entities.md) |
| `AddOrAlterForeignKeyColumnFor<TEntity, TPrimary>(x => x.Prop)` | Same, for a column that is also a foreign key |

Real mapping builder in this repo, `[verified: src/Libraries/Nop.Data/Mapping/Builders/Catalog/ProductAttributeMappingBuilder.cs]`:

```csharp
public partial class ProductAttributeMappingBuilder : NopEntityBuilder<ProductAttributeMapping>
{
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(ProductAttributeMapping.ProductAttributeId)).AsInt32().ForeignKey<ProductAttribute>()
            .WithColumn(nameof(ProductAttributeMapping.ProductId)).AsInt32().ForeignKey<Product>();
    }
}
```

## Creating a brand-new plugin-owned entity (full walkthrough)

1. **Domain class** — plain POCO, no navigation properties:

```csharp
namespace Nop.Plugin.Misc.MyPlugin.Domain;

public class MyRecord : BaseEntity
{
    public int ProductId { get; set; }
    public string Note { get; set; }
}
```

2. **Entity builder** (optional but recommended for anything beyond default string/int columns):

```csharp
public class MyRecordBuilder : NopEntityBuilder<MyRecord>
{
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table.WithColumn(nameof(MyRecord.Id)).AsInt32().PrimaryKey()
            .WithColumn(nameof(MyRecord.ProductId)).AsInt32().ForeignKey<Product>(onDelete: Rule.Cascade)
            .WithColumn(nameof(MyRecord.Note)).AsString(400);
    }
}
```

3. **Schema migration** — creates the table; version derives from the timestamp string, so a
   near-guaranteed-unique value is "now" at authoring time, not an incrementing counter:

```csharp
[NopSchemaMigration("2026-08-30 00:00:00", "Misc.MyPlugin base schema", MigrationProcessType.Installation)]
public class SchemaMigration : ForwardOnlyMigration
{
    public override void Up() => this.CreateTableIfNotExists<MyRecord>();
}
```

`CreateTableIfNotExists<TEntity>()` `[verified: src/Libraries/Nop.Data/Extensions/FluentMigratorExtensions.cs]`
is a `MigrationBase` extension method — it checks `Schema.Table(tableName).Exists()` first (safe to
re-run), then reads the matching `NopEntityBuilder<T>` automatically via `RetrieveTableExpressions`.
You do not hand-write `Create.Table("MyRecord").WithColumn(...)` unless you skip the builder class
entirely. (An older nopCommerce doc page references `Create.TableFor<T>()` / `IMigrationManager.BuildTable<T>()`
— that API does **not** exist in this 5.00 codebase; `CreateTableIfNotExists<T>()` is the current,
verified equivalent. Always prefer whichever name is actually present in
`Nop.Data.Extensions.FluentMigratorExtensions` over a doc snippet if the two ever disagree again in a
future version.)

4. **Repository usage** — never write raw SQL or a custom `DbContext`-like class; inject
   `IRepository<MyRecord>` directly into a plugin service:

```csharp
public class MyRecordService : IMyRecordService
{
    private readonly IRepository<MyRecord> _repository;
    public MyRecordService(IRepository<MyRecord> repository) => _repository = repository;

    public virtual async Task InsertAsync(MyRecord record) => await _repository.InsertAsync(record);
    public virtual async Task<MyRecord> GetByIdAsync(int id) => await _repository.GetByIdAsync(id);
}
```

`[verified: src/Libraries/Nop.Data/IRepository.cs]` — real signatures are
`InsertAsync(TEntity entity, bool publishEvent = true)`, `UpdateAsync(TEntity entity, bool publishEvent = true)`,
`DeleteAsync(TEntity entity, bool publishEvent = true)`, and
`GetByIdAsync(int? id, Func<ICacheKeyService, CacheKey> getCacheKey = null, bool includeDeleted = true, bool useShortTermCache = false)`
— the extra optional parameters are why entity CRUD events (
[knowledge-base/07](07-events-and-scheduled-tasks.md)) fire automatically: `publishEvent` defaults to
`true`, so a plain `InsertAsync(record)` call already raises `EntityInsertedEvent<MyRecord>` for any
subscribed `IConsumer<T>` — pass `publishEvent: false` explicitly on the rare occasion a bulk/internal
write should stay silent.

`IRepository<T>` needs **no manual DI registration** — it is resolved through a generic factory
already wired into the container.

5. **Bump `plugin.json`'s `Version`** whenever you add a new migration — the migration runner only
   re-applies migrations tied to a version increase (see `SkipMigrationOnUpdateAttribute` for the
   opt-out).

## Database schema at a glance

Default install creates ~126 tables. Naming is a deliberate mix of `PascalCase` (current standard)
and legacy formats, reconciled via `INameCompatibility`/`BaseNameCompatibility` rather than by
renaming existing columns (would break third-party SQL scripts already in the wild). Notable
"extension by design" table: **`GenericAttribute`** — see
[04-extending-core-entities.md](04-extending-core-entities.md) for how it lets you attach arbitrary
key/value data to *any* entity without a schema change.

Two enums worth knowing when working with cart/discount domain: `ShoppingCartType` (`ShoppingCart = 1`,
`Wishlist = 2` — same table backs both) and `RequirementGroupInteractionType` (`And = 0`, `Or = 2`) on
`DiscountRequirement.InteractionTypeId`.
