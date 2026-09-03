# Extending Core Domain Entities (without touching core semantics)

Source: adapted from `developer/tutorials/update-existing-entity.html` and the `GenericAttribute`
pattern documented in `developer/tutorials/db-schema.html`. This is the single most important file
for the "extensibility strategy" constraint of this project — read it before adding any property to
`Product`, `Order`, `Category`, `Customer`, etc.

There are **two officially sanctioned ways** to attach new data to a core entity. Picking the wrong
one is the most common nopCommerce extensibility mistake.

## Option A — Schema migration + property on the core class (structural, queryable, indexable)

Use this when the new field must be **queried, filtered, sorted, or joined on** at the SQL level
(e.g. `Product.ExpirationDate` for a "show expiring soon" admin filter). This is the nopCommerce
team's own documented technique, and it **does** touch a core file — this is expected and sanctioned,
not an anti-pattern, provided it's done as an additive, backward-compatible migration:

1. Add the property directly to the core domain class:
   `src/Libraries/Nop.Core/Domain/Catalog/Category.cs`:
   ```csharp
   public string SomeNewProperty { get; set; }
   ```
2. Add a migration that adds the column idempotently, using the current (verified) helper rather than
   hand-writing the exists-check:
   ```csharp
   [NopMigration("2026-08-30 00:00:00", "Category. Add some new property")]
   public class AddSomeNewProperty : ForwardOnlyMigration
   {
       public override void Up() =>
           this.AddOrAlterColumnFor<Category>(c => c.SomeNewProperty).AsString(255).Nullable();
   }
   ```
   `AddOrAlterColumnFor<TEntity>(this MigrationBase migration, Expression<Func<TEntity, object>> selector)`
   `[verified: src/Libraries/Nop.Data/Extensions/FluentMigratorExtensions.cs]` resolves the column name
   from the property selector via `INameCompatibility`, then internally branches to `AddColumn(...)` or
   `AlterColumn(...)` depending on whether the column already exists — the manual
   `Schema.Table(tableName).Column(...).Exists() ? ... : ...` guard shown in older nopCommerce doc
   pages is exactly what this extension does internally; call the extension, don't re-implement the
   guard by hand. A foreign-key-column variant exists too:
   `AddOrAlterForeignKeyColumnFor<TEntity, TPrimary>(x => x.Prop, onDelete: Rule.Cascade)`.
3. Add the corresponding property to the **admin view model** (`Areas/Admin/Models/...`) with a
   `[NopResourceDisplayName]` for localization, a FluentValidation rule in the matching validator, a
   view partial, and map it in the controller's `Create`/`Edit` actions (AutoMapper handles the
   simple 1:1 case automatically via `.ToModel()`/`.ToEntity()`; only add manual mapping when the
   shapes diverge).

**Where to actually put this in *this* project**: prefer placing the migration class inside a
dedicated plugin (e.g. `Nop.Plugin.Misc.GastronomyCatalogExtensions`) rather than editing
`Nop.Data.Migrations` in `Nop.Core`/`Nop.Data` directly, so the change ships and upgrades independently
of the core solution. The domain-class property edit is the one unavoidable core-file touch; keep it
to additive nullable properties only, and track every such touch explicitly (see
[00-system-instructions.md](../ai-harness/00-system-instructions.md) for the project's core-modification
policy).

## Option B — `GenericAttribute` (schema-free, arbitrary key/value, per-entity)

Use this when the new data is **descriptive metadata** that does not need SQL-level filtering — a
free-form note, a flag, JSON blob, or a value only ever read back for the one entity instance it's
attached to. This requires **zero migrations and zero core file changes**.

`Nop.Services.Common.IGenericAttributeService` (`[verified: src/Libraries/Nop.Services/Common/GenericAttributeService.cs]`)
backs the `GenericAttribute` table, which stores `(EntityId, KeyGroup, Key, Value)` rows against
**any** entity type — this is the exact mechanism nopCommerce itself uses for custom Customer and
Vendor attributes.

```csharp
// write
await _genericAttributeService.SaveAttributeAsync(product, "BatchNumber", "LOT-2026-0830");

// read
var batchNumber = await _genericAttributeService.GetAttributeAsync<string>(product, "BatchNumber");
```

Trade-offs vs. Option A:

| | Schema migration (A) | GenericAttribute (B) |
|---|---|---|
| SQL filtering / sorting / joins | Yes | No (value stored as string in a shared table) |
| Core file touched | Yes (domain class) | No |
| Admin grid column, DB index | Straightforward | Awkward / not recommended |
| Good for | Batch number, expiration date, structured facts you'll query | Free-text notes, feature flags, JSON blobs, rarely-queried metadata |

## Decision rule for this project

Given the gastronomy domain (see
[05-domain-gastronomy-guidelines.md](../ai-harness/05-domain-gastronomy-guidelines.md)):

- **Batch number, expiration date, allergen/dietary tags meant to be filterable in admin grids or
  storefront facets** → Option A (real column, plugin-owned migration, indexed if used in `WHERE`).
- **Anything closer to "notes" or one-off flags with no reporting requirement** → Option B
  (`GenericAttribute`, zero schema risk).

Never invent a third mechanism (custom side-table joined manually in application code, raw SQL,
reflection-based property bags) — both paths above are what the engine, the admin grid
infrastructure, and the import/export tooling already understand.
