---
id: GIL-004
kind: Task
title: Nutritional values (kcal + macros) on Ingredient, per 100g
status: Shipped
---

# Task — Nutritional values (kcal + macros) on Ingredient, per 100g

> **Shipped.** Implemented and merged into `develop`. Post-implementation gate (reviewer, test-engineer,
> integration-auditor, migration/plugin/admin-ui/localization-standards-check, upgrade-safety-detector)
> all passed; one test-coverage gap (missing update-persistence test, spec §11) found by `test-engineer`
> and closed before merge. See `Docs/BusinessLogic/product-ingredients.md` for the mechanism as documented
> going forward — this spec is kept as the historical record of what was asked for.

## 1. Business goal & outcome

Every `Ingredient` (`Nop.Plugin.Misc.Ingredients`, GIL-001) can carry its own nutritional value: energy
(kcal) and the three macronutrients (protein, fat, carbohydrate), each expressed **per 100g** — the
standard basis on Polish food labels.

This is deliberately the smaller of two related pieces of work. The larger goal — a calorie/nutrition
table computed for a whole product/recipe from the ingredients it's composed of — is **out of scope
here**. `IngredientComposition` and `ProductIngredientMapping` (`Domain/IngredientComposition.cs`,
`Domain/ProductIngredientMapping.cs`) carry no quantity/weight field today, so there is nothing yet to
multiply a per-100g value by; that aggregation is a separate future task once quantity tracking exists.

**Outcome:** an admin editing an ingredient can enter kcal/protein/fat/carbohydrate per 100g; the values
are stored and shown back on the ingredient edit form. No storefront or recipe-level surface in this
task.

**Developer-confirmed (round 1):**
- Scope is kcal + protein/fat/carbohydrate (not kcal alone).
- Basis is per 100g (not per portion/serving — no portion concept exists in this model).

**Developer-confirmed (Gate 1 revision):** all four values are **required**, not optional. An admin
cannot save an ingredient (create or edit) without providing all four — an ingredient genuinely at zero
(e.g. water, salt) is entered as `0`, not left blank. Superseded from the original Draft, which proposed
nullable/optional fields; rejected because `0` is a real, meaningful value for some ingredients (water,
salt), so it cannot double as an "unknown" sentinel without corrupting the future recipe-aggregation goal
this data exists for — the developer chose to instead force the data to always exist, rather than
distinguish "unknown" from "genuinely zero."

## 2. Root cause / current behavior

N/A — new feature, not a bug fix.

## 3. Placement — plugin or core?

Existing plugin `Nop.Plugin.Misc.Ingredients` (GIL-001) — this is new data on that plugin's own
`Ingredient` entity, not a core change. No new plugin.

## 4. Extension point

N/A — no new plugin type/interface involved. `IngredientsPlugin` already implements `IMiscPlugin` +
`IWidgetPlugin` (GIL-001); this task adds fields to the existing `Ingredient` entity and its existing
admin Create/Edit surface, nothing new to register.

## 5. Data model & migration

**Extension mechanism: schema migration on the plugin's own `Ingredient` table**, not `GenericAttribute`.
`Ingredient` is a plugin-owned entity already backed by a real table (`IngredientBuilder.cs`), not a core
entity being extended, so the Option A/B core-entity decision tree
([`knowledge-base/04`](../../knowledge-base/04-extending-core-entities.md)) doesn't directly apply — but
the same reasoning holds: these four values are meant to be read back and, in the future, summed/derived
per recipe, which is exactly the "structured, queryable fact" case, not a free-form note.

No existing mechanism (`SpecificationAttribute`, `ProductTag`, `GenericAttribute`) fits: these are
per-ingredient numeric facts read by future arithmetic, not descriptive tags.

**Proposed new columns on `Ingredient`:**
- `CaloriesPer100g` (kcal)
- `ProteinPer100g` (g)
- `FatPer100g` (g)
- `CarbohydratePer100g` (g)

All four **required (not nullable)** decimal, per Gate 1 revision — the admin form must always collect a
value; an ingredient genuinely at zero (water, salt) is entered as `0`. Exact decimal precision/scale is
for `ddd-modeler`.

**Existing-installation consequence (required, not nullable):** GIL-001's existing `Ingredient` rows
(any already-seeded/test data) have no value for these four columns today. A `NOT NULL` column added to a
populated table needs a default or a backfill (`migration-standards-check`) — the migration backfills
existing rows to `0` for all four columns before/while making them non-nullable, so the column constraint
is satisfiable immediately. This is a one-time data-migration default, not a claim that those ingredients
are actually zero-calorie; an admin corrects them to real values afterward like any other backfilled
field. For `ddd-modeler` to confirm the exact FluentMigrator sequencing (populate-then-constrain vs a
single statement the provider supports).

**Migration:** GIL-001's `SchemaMigration.cs` (`MigrationProcessType.Installation`) is already merged
into `develop` — per `migration-standards-check`, a shipped migration is never edited. This task adds a
**new** `[NopMigration(...)]`/`ForwardOnlyMigration`, tagged **`MigrationProcessType.Update`** (not
`Installation`) using `this.AddOrAlterColumnFor<Ingredient>(x => x.CaloriesPer100g)...` (and the other
three), one per column or grouped in one `Up()` — for `ddd-modeler` to decide. `[NopSchemaMigration]` is
**not** the right attribute here — verified (spec-intake, round 2) that it's reserved for the core
base-schema migration that runs before the IoC container is ready (`NopSchemaMigrationAttribute.cs:3-10`);
every plugin migration in this codebase, GIL-001's own included, uses `[NopMigration(...)]`.

**Verified (spec-intake, round 1):** `MigrationProcessType.Installation` migrations only run on a fresh
plugin install (`PluginService.cs:509`); an already-installed store only picks up a new migration on a
`plugin.json` version bump via `PluginService.UpdatePluginsAsync`, which runs migrations tagged
`MigrationProcessType.Update` (`PluginService.cs:690`). Leaving the new migration as `Installation` (or
copying GIL-001's shipped tag) would mean it never runs where GIL-001 is already installed — the codebase's
own precedent for this exact "add a column to an already-shipped plugin" case is
`Nop.Plugin.Misc.Zettle`'s `InventoryBalanceMigration.cs` (`MigrationProcessType.Update`).

Forward-only: safe under a rolling deploy (old app instances never reference the new columns). Not purely
additive in the "existing rows read back null" sense anymore, since the columns are required — see the
backfill-to-`0` note above.

`plugin.json`'s `Version` must be bumped in the same change (`migration-standards-check` requirement) so
the migration runner re-applies on upgrade.

## 6. Admin & storefront surface

**Admin only** in this task — no storefront rendering. Four new **required** numeric fields added to the
existing Ingredient Create/Edit forms, alongside `Name`/`Description`/`Allergen` — marked required the
same way `Name` already is (asterisk/required indicator), since neither a new ingredient nor an edit to
an existing one can be saved without all four. Not localized (the values are language-independent
numbers, unlike `Name`/`Description`), so no change to `IngredientLocalizedModel` or the `Locales`
editor.

Ingredient list page (`List.cshtml`/`IngredientListModel`): N/A — developer confirmed no new grid column,
kcal/macros stay on the edit form only for v1.

Storefront: N/A — no product-facing display of these values in this task (that's the future recipe-table
task).

## 7. Settings, permissions, localization

**Settings.** None — no new `ISettings` needed.

**Permissions.** None new — reuses the existing `IngredientsPermissionConfigManager` View/CreateEditDelete
pair (`INGREDIENTS_VIEW` / `INGREDIENTS_CREATE_EDIT_DELETE`) already guarding the Ingredient Create/Edit
actions in `IngredientsAdminController`. These four fields are edited through the same actions, so no new
permission record is needed.

**Localization.** New locale resource keys for the four field labels/hints:
- `Plugins.Misc.Ingredients.Fields.CaloriesPer100g` (+ `.Hint`)
- `Plugins.Misc.Ingredients.Fields.ProteinPer100g` (+ `.Hint`)
- `Plugins.Misc.Ingredients.Fields.FatPer100g` (+ `.Hint`)
- `Plugins.Misc.Ingredients.Fields.CarbohydratePer100g` (+ `.Hint`)

Added to `IngredientsPlugin.InstallAsync`'s existing dictionary for a **fresh** install — but that alone
does not reach a store where GIL-001 is already installed: `InstallAsync` runs once, at first install
only, and `IngredientsPlugin` does not override `UpdateAsync` (inherits `BasePlugin.UpdateAsync`'s no-op
default), which is what actually runs on an already-installed plugin's version bump
(`PluginService.cs:686-693`). **Verified (spec-intake, round 1):** the codebase's own precedent for
seeding new locale resources into an already-installed plugin is a dedicated `MigrationProcessType.Update`
migration that calls `this.AddOrUpdateLocaleResource(...)` itself (e.g.
`Nop.Plugin.Widgets.GoogleAnalytics`'s `UpgradeTo470/LocalizationMigration.cs:22`) — **not**
`AddOrUpdateLocaleResourceAsync` (no such migration-extension method exists; corrected, spec-intake round
2). This synchronous extension resolves `ISyncCodeHelper`/`IStaticCacheManager` via `EngineContext.Current`
(`MigrationExtensions.cs:213-216`), i.e. it needs the IoC container available — consistent with §5's
correction to `[NopMigration(...)]` rather than `[NopSchemaMigration]`, which runs before DI is ready. So
these four keys are seeded from the same `[NopMigration(...)]`/`MigrationProcessType.Update` migration as
the schema change (§5), via `AddOrUpdateLocaleResource`, not solely from `InstallAsync`. Exact migration
structure (one migration doing both, or two) is for `ddd-modeler`.

`UninstallAsync` already does a prefix delete (`DeleteLocaleResourcesAsync("Plugins.Misc.Ingredients")`),
so these new keys are covered automatically regardless of how they were seeded — no change needed there.

## 8. Events & scheduled tasks

None. Built-in `EntityUpdatedEvent<Ingredient>` (fired automatically by `EntityRepository<T>` on every
update) already covers change notification for these new fields — no custom event, no scheduled task.

## 9. Caching

`IngredientCacheEventConsumer` (`CacheEventConsumer<Ingredient>`) already exists and already invalidates
on any `Ingredient` update — these new columns are plain properties on the same entity, so no change
needed here. No new cache key. Multi-instance: same posture as GIL-001/GIL-002 — no Redis/distributed
cache configured yet (localhost-only today), not a prerequisite for this task.

## 10. Failure scenarios

**External dependencies:** N/A — no external dependency; only the database, same as any other admin edit
in this plugin.

- **Invalid input** (missing value, negative value, non-numeric). Resolved: standard admin-form
  validation — `IngredientValidator` gets a `NotNull`/required rule plus a `GreaterThanOrEqualTo(0)` rule
  per field; leaving any of the four blank is rejected the same way an empty `Name` is today.
- **Existing ingredients with no nutritional data (pre-GIL-004 rows).** Resolved: the migration backfills
  them to `0` for all four columns (§5) so the `NOT NULL` constraint is satisfiable immediately; an admin
  corrects the real values afterward. Not a UI-facing failure case — those rows simply show `0` on the
  edit form until corrected.
- **Concurrent edit of the same ingredient.** Already covered by GIL-001's existing concurrency handling
  for `Ingredient` edits (no separate optimistic-lock concern introduced by four more plain columns).

## 11. Test scenarios

- Creating an ingredient with all four values set (including a genuine `0`, e.g. water) persists and
  reads them back correctly.
- Creating an ingredient with any of the four values left blank is rejected by admin-form validation
  (fields required).
- Editing an existing ingredient to change its nutritional values persists them.
- Negative value for any of the four fields is rejected by admin-form validation.
- A migration run against a database that already has `Ingredient` rows (from GIL-001) backfills existing
  rows to `0` for all four new columns, satisfying the `NOT NULL` constraint without breaking existing
  reads/writes of those rows.

## 12. Documentation impact

`Docs/BusinessLogic/product-ingredients.md` (exists, already documents the Allergen classification field
in the same shape — real column, not free text) is updated in the same commit to add the four nutritional
fields. No new glossary term.

## 13. Deployment & rollout

No Docker/`appsettings`/ECS change. Additive required columns plus a `plugin.json` version bump — safe on
an existing installation because the migration backfills existing rows to `0` (§5) before/while applying
the `NOT NULL` constraint. Immediate rollout; the `0` backfill is a one-time technical default, not a
business claim that existing ingredients are zero-calorie — an admin corrects them to real values as a
follow-up content task, not a blocker to shipping this.

## Technical design (ddd-modeler)

> Two passes. Pass 1 designed against the original (nullable-fields) spec. At Gate 1 the developer
> overrode that decision — all four fields are required, not nullable (see the "Developer-confirmed
> (Gate 1 revision)" note in §1), and the spec was corrected before Pass 2 re-verified the design against
> the revised spec. Pass 2 corrections are authoritative over the equivalent Pass 1 sections; Pass 1 text
> not mentioned in Pass 2 still stands.

### Pass 1 — initial design (nullable fields)

**Corrections to the spec's technical assumptions**

- Migration base class: the spec said `[NopMigration(...)]`/`ForwardOnlyMigration`. `ForwardOnlyMigration`
  is real but appears only in core's own `Nop.Data.Migrations.{Installation,UpgradeToXXX}.*` classes
  (`src/Libraries/Nop.Data/Migrations/UpgradeTo500/SchemaMigration.cs:11`) — no plugin migration in this
  codebase uses it. Every comparable plugin precedent (additive column, `MigrationProcessType.Update`,
  plugin-owned entity) uses `MigrationBase` with an explicit no-op `Down()`:
  `Nop.Plugin.Pickup.PickupInStore/Data/Migrations/LonLatUpdateMigration.cs:9`,
  `Nop.Plugin.Misc.Zettle/Data/InventoryBalanceMigration.cs:10`,
  `Nop.Plugin.Widgets.FacebookPixel/Data/ConversionsApiMigration.cs:11` (closest precedent overall — does
  schema-alter and `AddOrUpdateLocaleResource` in the same `Up()`, exactly this task's shape). Likely
  leaked from `Docs/knowledge-base/04-extending-core-entities.md:27`'s Option A example, which is for a
  core entity's bootstrap-phase migration, not a plugin `Update` migration. Design uses `MigrationBase`.
- File citation: the spec named `Create.cshtml`/`Edit.cshtml`. Both only render a form shell and delegate
  to a shared partial (`Create.cshtml:28`, `Edit.cshtml:33` both call `Html.PartialAsync(".../
  _CreateOrUpdate.cshtml", Model)`), which renders `_CreateOrUpdate.Info.cshtml` — `Name`/`Description`/
  `AllergenId` live at `_CreateOrUpdate.Info.cshtml:30-56`; that's the real file to edit.

**Placement.** Existing plugin `Nop.Plugin.Misc.Ingredients`, no new plugin, no new extension point —
`IngredientsPlugin` already implements `IMiscPlugin`/`IWidgetPlugin`.

**Extension decision.** Schema migration on the plugin's own `Ingredient` table, not `GenericAttribute` —
`Ingredient` already has a real backing table, and these four values are meant to be summed per recipe
once quantity tracking exists (spec §1): a structured, queryable fact, not a free-form note.
`SpecificationAttribute`/`ProductTag` rejected — both attach to `Product`, not `Ingredient`, and neither is
numeric-native.

**Simplicity check.** One migration file, four domain properties, four model properties + validator rules
+ view fields, locale keys in two places. No new service methods, controller actions, factory methods,
builder change, `GenericAttribute` fallback, or admin grid column.

**Blast radius.** `ProductIngredientsAdminViewComponent`, `IngredientsViewComponent` (storefront),
`_CreateOrUpdate.Composition.cshtml`/`IngredientCompositionService`, `Public/Models/IngredientsModel.cs`:
none read or map these fields. `IngredientListModel`/`List.cshtml`: reuses `IngredientModel` for grid rows,
but the Kendo grid only renders explicitly declared `ColumnProperty` entries — no accidental UI change.
Nothing else keys off `Ingredient`'s property set structurally.

### Pass 2 — Gate 1 revision (required, not nullable)

**Corrections to the spec's technical assumptions**

- §10/§11's proposed `NotNull`/required validator rule is wrong and would reintroduce the exact bug the
  revision exists to prevent. FluentValidation's `NotEmpty()` on a value type checks equality against
  `default(T)` — for `decimal`, `default` is `0m`. A `NotEmpty()`-style rule on `CaloriesPer100g` etc.
  would reject a genuine `0` entry for water/salt, which §1 explicitly requires to accept. `NotNull()` is
  harmless but dead code on a non-nullable value type. **Correct design:** no not-null/not-empty rule in
  `IngredientValidator` for these four fields — "required" is enforced structurally: a plain (non-nullable)
  `decimal` model property makes an empty form submission fail ASP.NET Core model binding before
  FluentValidation runs, `ModelState.IsValid` becomes `false`. Same mechanism that already makes
  `ProductModel.Price` (`Areas/Admin/Models/Catalog/ProductModel.cs:332`) behave as required with no
  explicit validator rule (confirmed empty grep on `Price` in `ProductValidator.cs`). The localized message
  comes from an existing, already-active mechanism: `BasePluginController`
  (`Nop.Web.Framework/Controllers/BasePluginController.cs:8`, inherited by `IngredientsAdminController`)
  carries `[NotNullValidationMessage]`; nopCommerce's model-binding setup
  (`ServiceCollectionExtensions.cs:339-341`) replaces .NET's raw "The value '' is invalid" with a locale
  key, and `NotNullValidationMessageAttribute`
  (`Nop.Web.Framework/Mvc/Filters/NotNullValidationMessageAttribute.cs:58-116`) rewrites it to a localized
  "`{field display name}` is required" using each property's `[NopResourceDisplayName]`.
- §7's locale-resource list was incomplete for the required-fields shape: once `GreaterThanOrEqualTo(0)`
  rules exist, each needs its own message resource per this codebase's convention
  (`Admin.Vendors.Fields.PriceFrom.GreaterThanOrEqualZero` — `VendorValidator.cs:35-37`, same pattern in
  `ManufacturerValidator.cs`/`CategoryValidator.cs`/`CatalogSettingsValidator.cs`) — no shared/generic
  message key exists. Four more keys needed: `Plugins.Misc.Ingredients.Fields.{Field}.GreaterThanOrEqualZero`.

**Confirmed: backfill / NOT NULL migration sequencing — single statement, not two-step.**
`AddOrAlterColumnFor<TEntity>` (`Nop.Data/Extensions/FluentMigratorExtensions.cs:157-166`) returns an
`IAlterTableColumnAsTypeSyntax` for either `AddColumn` or `AlterColumn` — the migration chains
type/nullability/default explicitly. `.AsX().NotNullable().WithDefaultValue(v)` is core's own shipped
precedent for adding a required column to a table guaranteed to already have rows:

```csharp
// src/Libraries/Nop.Data/Migrations/UpgradeTo490/SchemaMigration.cs:18-21
this.AddOrAlterColumnFor<Product>(t => t.AgeVerification)
    .AsBoolean()
    .NotNullable()
    .WithDefaultValue(false);
```

Applied here: `this.AddOrAlterColumnFor<Ingredient>(x => x.CaloriesPer100g).AsDecimal(18, 4).NotNullable().WithDefaultValue(0);`
compiles to one `ALTER TABLE "Ingredient" ADD COLUMN "CaloriesPer100g" numeric(18,4) NOT NULL DEFAULT 0`
on Postgres — the constant `DEFAULT` applies to all existing rows as part of that same DDL statement
(metadata-only since PG11, no separate `UPDATE`, no table rewrite), so the `NOT NULL` constraint is
satisfiable immediately, no race window, no second pass needed. `AddOrAlterColumnFor` returns the same
fluent-chain type regardless of `AddColumn`/`AlterColumn` branch, so one line correctly handles both an
already-installed store (column doesn't exist, `AddColumn`) and a fresh install where the column already
exists via the domain-class auto-map path (`AlterColumn`, a harmless no-op re-apply of the same
`NOT NULL`/`DEFAULT`).

**Domain model (supersedes Pass 1).** `Domain/Ingredient.cs` — four plain (non-nullable) `decimal`
properties:

```csharp
public decimal CaloriesPer100g { get; set; }
public decimal ProteinPer100g { get; set; }
public decimal FatPer100g { get; set; }
public decimal CarbohydratePer100g { get; set; }
```

`IngredientBuilder.cs` needs no change: `FluentMigratorExtensions.RetrieveTableExpressions`'s auto-map
path (`FluentMigratorExtensions.cs:273-297`) maps a plain `decimal` to `AsDecimal(18, 4)` and — not being
`Nullable<decimal>` — leaves it not-nullable (`FluentMigratorExtensions.cs:290-296`), identical to what the
new migration sets explicitly. A fresh install (GIL-001's `Installation`-tagged `SchemaMigration` against
the current `Ingredient.cs`, then every `Update`-tagged migration replayed per `PluginService.cs:196-203`)
and an upgrading store converge on the same schema via two different code paths that agree by construction.

**Design (updated parts, supersede Pass 1 where they overlap).**

Migration — one new file, `Data/Migrations/NutritionalValuesMigration.cs` (exact filename/date-stamp left
to `implementation-planner`), mirroring `Nop.Plugin.Misc.Zettle/Data/InventoryBalanceMigration.cs`'s shape:

```csharp
[NopMigration("2026-09-03 00:00:00", "Nop.Plugin.Misc.Ingredients nutritional values", MigrationProcessType.Update)]
public class NutritionalValuesMigration : MigrationBase
{
    public override void Up()
    {
        if (!DataSettingsManager.IsDatabaseInstalled())
            return;

        this.AddOrAlterColumnFor<Ingredient>(x => x.CaloriesPer100g).AsDecimal(18, 4).NotNullable().WithDefaultValue(0);
        this.AddOrAlterColumnFor<Ingredient>(x => x.ProteinPer100g).AsDecimal(18, 4).NotNullable().WithDefaultValue(0);
        this.AddOrAlterColumnFor<Ingredient>(x => x.FatPer100g).AsDecimal(18, 4).NotNullable().WithDefaultValue(0);
        this.AddOrAlterColumnFor<Ingredient>(x => x.CarbohydratePer100g).AsDecimal(18, 4).NotNullable().WithDefaultValue(0);

        // + AddOrUpdateLocaleResource for the 4 Fields.X / .Hint keys and the 4 new .GreaterThanOrEqualZero keys
    }

    public override void Down()
    {
        //nothing - forward-only
    }
}
```

`[NopMigration(...)]` (not `[NopSchemaMigration]`), `MigrationProcessType.Update`, and dual-path locale
seeding (`IngredientsPlugin.InstallAsync`'s dictionary for fresh installs + this migration's own
`AddOrUpdateLocaleResource` for upgrading stores — `InstallAsync` only runs once at first install, and
`IngredientsPlugin` does not override `UpdateAsync`) are unaffected by this revision, carried forward from
Pass 1. `plugin.json`'s `Version` bumped in the same change (5.00.1 → 5.00.2) — required so
`PluginService.UpdatePluginsAsync` actually re-applies migrations on the version mismatch
(`PluginService.cs:686-693`).

`IngredientModel` (`Admin/Models/IngredientModel.cs`) — plain `decimal`, `[UIHint("Decimal")]` (not
`DecimalNullable`), matching precedent `Nop.Plugin.Misc.RFQ/Models/Customer/RequestQuoteItemModel.cs:15-16`:

```csharp
[NopResourceDisplayName("Plugins.Misc.Ingredients.Fields.CaloriesPer100g")]
[UIHint("Decimal")]
public decimal CaloriesPer100g { get; set; }
// + ProteinPer100g, FatPer100g, CarbohydratePer100g, same shape
```

Free byproduct of the required-fields revision: `IngredientValidator`'s existing
`SetDatabaseValidationRules<Ingredient>()` call filters for model properties whose `PropertyType ==
typeof(decimal)` exactly (`BaseNopValidator.cs:74-76`) — `decimal?` was excluded, plain `decimal` is
included, so an upper-bound rule sourced from the column's own `AsDecimal(18,4)` metadata is now added
automatically, zero extra code.

`IngredientValidator` (`Admin/Validators/IngredientValidator.cs`) — range check only, no required/not-null
rule:

```csharp
RuleFor(model => model.CaloriesPer100g)
    .GreaterThanOrEqualTo(0)
    .WithMessageAwait(localizationService.GetResourceAsync("Plugins.Misc.Ingredients.Fields.CaloriesPer100g.GreaterThanOrEqualZero"));
// + same for ProteinPer100g, FatPer100g, CarbohydratePer100g

SetDatabaseValidationRules<Ingredient>(); // unchanged call; now also covers the 4 new fields' upper bound
```

Mapping — `Infrastructure/MapperConfiguration.cs` needs no change: `CreateMap<Ingredient,
IngredientModel>()`/reverse are convention-based; the four new identically-named properties map
automatically.

View — `Admin/Views/_CreateOrUpdate.Info.cshtml`, required-marker convention matches `Name`'s own
rendering (`asp-required="true"` on `nop-editor` — visual asterisk only via `NopEditorTagHelper.cs:69-73`,
not what enforces validation):

```cshtml
<div class="form-group row">
    <div class="col-md-3">
        <nop-label asp-for="CaloriesPer100g" />
    </div>
    <div class="col-md-9">
        <nop-editor asp-for="CaloriesPer100g" asp-required="true" />
        <span asp-validation-for="CaloriesPer100g"></span>
    </div>
</div>
```
(×4, placed after the existing `AllergenId` block, `_CreateOrUpdate.Info.cshtml:48-56`.)
`[UIHint("Decimal")]` resolves to `Areas/Admin/Views/Shared/EditorTemplates/Decimal.cshtml` — a plain
`<input type="number">` bound directly to the `decimal`, unlike `DecimalNullable.cshtml`.

**Simplicity check (Pass 2).** Same smallest-version shape as Pass 1 — four columns, four model
properties, one migration, one validator range-rule per field, four view blocks — with the type flipped
from `decimal?` to `decimal` and the validator rule reduced (not expanded) to a pure range check. No new
abstractions introduced by the revision.

**Blast radius (Pass 2 additions).** `SetDatabaseValidationRules<Ingredient>()`/`SetDecimalMaxValue` is
shared framework code used by every validator that calls it — unaffected, only adds rules for properties it
finds by reflection. `NotNullValidationMessageAttribute` on `BasePluginController` is shared by every
plugin controller that extends it — this design relies on existing behavior, doesn't change it.
`IngredientBuilder`'s auto-map interaction is scoped to `Ingredient`'s own table.

**Installed-store impact.** An already-installed store picks up the four new `NOT NULL DEFAULT 0` columns
via the `Update`-tagged migration on the `plugin.json` version bump; existing rows read back `0` for all
four fields immediately (a technical backfill default, not a business claim, per §13) with no manual
data-fix step and no window where the constraint is unsatisfied. Rolling deploy: an old app instance never
references the new columns/properties, unaffected while new instances roll out. Locale keys and
permissions: dual-path seeding as in Pass 1, unchanged.

**Approved by:** Mateusz Nycz
**Date:** 2026-09-03
**Revision notes:** Two ddd-modeler passes. Pass 1 designed against the original nullable-fields spec. At
Gate 1 the developer overrode that decision to required-not-nullable (0 for a genuinely zero-calorie
ingredient, no null/unknown state) — spec corrected first (§1/§5/§10/§11/§13), then Pass 2 re-verified the
design end to end: dropped the unsafe `NotEmpty`-style required validator rule (would reject a legitimate
`0`) in favor of the framework's existing non-nullable-model-binding + `NotNullValidationMessage`
mechanism, confirmed the single-statement `NOT NULL DEFAULT 0` migration sequencing against core's own
`Product.AgeVerification` precedent, and added the four missing `.GreaterThanOrEqualZero` locale keys.
Final design (Pass 1 + Pass 2 corrections) approved as a whole.

## Implementation plan (implementation-planner)

File-by-file plan, mirroring named precedents throughout — no further domain decisions, the approved
two-pass design above is fully determined for every file below.

### New

**`Data/Migrations/NutritionalValuesMigration.cs`** — mirrors `Nop.Plugin.Misc.Zettle/Data/InventoryBalanceMigration.cs`
(shape/attribute/`MigrationProcessType.Update`), `Nop.Plugin.Widgets.FacebookPixel/Data/ConversionsApiMigration.cs`
(schema-alter + `AddOrUpdateLocaleResource` in one `Up()`), and `Nop.Data/Migrations/UpgradeTo490/SchemaMigration.cs:18-21`
(`.AsX().NotNullable().WithDefaultValue(v)` chain shape):

```csharp
[NopMigration("2026-09-03 00:00:00", "Nop.Plugin.Misc.Ingredients nutritional values", MigrationProcessType.Update)]
public class NutritionalValuesMigration : MigrationBase
{
    public override void Up()
    {
        if (!DataSettingsManager.IsDatabaseInstalled())
            return;

        this.AddOrAlterColumnFor<Ingredient>(x => x.CaloriesPer100g).AsDecimal(18, 4).NotNullable().WithDefaultValue(0);
        this.AddOrAlterColumnFor<Ingredient>(x => x.ProteinPer100g).AsDecimal(18, 4).NotNullable().WithDefaultValue(0);
        this.AddOrAlterColumnFor<Ingredient>(x => x.FatPer100g).AsDecimal(18, 4).NotNullable().WithDefaultValue(0);
        this.AddOrAlterColumnFor<Ingredient>(x => x.CarbohydratePer100g).AsDecimal(18, 4).NotNullable().WithDefaultValue(0);

        this.AddOrUpdateLocaleResource(new Dictionary<string, string>
        {
            ["Plugins.Misc.Ingredients.Fields.CaloriesPer100g"] = "Calories per 100g (kcal)",
            ["Plugins.Misc.Ingredients.Fields.CaloriesPer100g.Hint"] = "The energy value of this ingredient, in kilocalories per 100g.",
            ["Plugins.Misc.Ingredients.Fields.CaloriesPer100g.GreaterThanOrEqualZero"] = "Calories per 100g must be zero or greater.",
            ["Plugins.Misc.Ingredients.Fields.ProteinPer100g"] = "Protein per 100g (g)",
            ["Plugins.Misc.Ingredients.Fields.ProteinPer100g.Hint"] = "The protein content of this ingredient, in grams per 100g.",
            ["Plugins.Misc.Ingredients.Fields.ProteinPer100g.GreaterThanOrEqualZero"] = "Protein per 100g must be zero or greater.",
            ["Plugins.Misc.Ingredients.Fields.FatPer100g"] = "Fat per 100g (g)",
            ["Plugins.Misc.Ingredients.Fields.FatPer100g.Hint"] = "The fat content of this ingredient, in grams per 100g.",
            ["Plugins.Misc.Ingredients.Fields.FatPer100g.GreaterThanOrEqualZero"] = "Fat per 100g must be zero or greater.",
            ["Plugins.Misc.Ingredients.Fields.CarbohydratePer100g"] = "Carbohydrate per 100g (g)",
            ["Plugins.Misc.Ingredients.Fields.CarbohydratePer100g.Hint"] = "The carbohydrate content of this ingredient, in grams per 100g.",
            ["Plugins.Misc.Ingredients.Fields.CarbohydratePer100g.GreaterThanOrEqualZero"] = "Carbohydrate per 100g must be zero or greater."
        });
    }

    public override void Down()
    {
        //nothing - forward-only
    }
}
```
English copy above is implementer-authored (design fixed only the key names) — free to adjust wording
without a re-approval cycle, not a domain decision.

### Changed

- **`Domain/Ingredient.cs`** — four plain, non-nullable `decimal` properties (`CaloriesPer100g`,
  `ProteinPer100g`, `FatPer100g`, `CarbohydratePer100g`), placed after `Allergen`/`AllergenId`, before
  `CreatedOnUtc`.
- **`plugin.json`** — `"Version"`: `"5.00.1"` → `"5.00.2"`.
- **`IngredientsPlugin.cs`** — same 12 locale keys added to `InstallAsync`'s
  `AddOrUpdateLocaleResourceAsync` dictionary (dual-path seeding: this covers a fresh install, the
  migration above covers a store upgrading from GIL-001). `UninstallAsync` unchanged — its existing
  prefix delete already covers these keys.
- **`Admin/Models/IngredientModel.cs`** — add `using System.ComponentModel.DataAnnotations;`; four
  properties on `IngredientModel` (not `IngredientLocalizedModel`), after `AllergenId`:
  ```csharp
  [NopResourceDisplayName("Plugins.Misc.Ingredients.Fields.CaloriesPer100g")]
  [UIHint("Decimal")]
  public decimal CaloriesPer100g { get; set; }
  // + ProteinPer100g, FatPer100g, CarbohydratePer100g, same shape
  ```
  Mirrors `Nop.Plugin.Misc.RFQ/Models/Customer/RequestQuoteItemModel.cs`'s `UnitPrice` (plain `decimal` +
  `[UIHint("Decimal")]`, not `DecimalNullable`).
- **`Admin/Validators/IngredientValidator.cs`** — four range rules before the existing
  `SetDatabaseValidationRules<Ingredient>()` call, mirroring `VendorValidator.cs:34-37`'s `PriceFrom`
  rule:
  ```csharp
  RuleFor(model => model.CaloriesPer100g)
      .GreaterThanOrEqualTo(0)
      .WithMessageAwait(localizationService.GetResourceAsync("Plugins.Misc.Ingredients.Fields.CaloriesPer100g.GreaterThanOrEqualZero"));
  // + ProteinPer100g, FatPer100g, CarbohydratePer100g, same shape
  ```
  No `NotEmpty`/`NotNull` rule — confirmed dead/harmful for this value type in the approved design.
- **`Admin/Views/_CreateOrUpdate.Info.cshtml`** — four `form-group row` blocks after the existing
  `AllergenId` block:
  ```cshtml
  <div class="form-group row">
      <div class="col-md-3">
          <nop-label asp-for="CaloriesPer100g" />
      </div>
      <div class="col-md-9">
          <nop-editor asp-for="CaloriesPer100g" asp-required="true" />
          <span asp-validation-for="CaloriesPer100g"></span>
      </div>
  </div>
  ```
  (×4). Already a `<Content Include>` item in the `.csproj` — content edit, not a new file.
- **`Docs/BusinessLogic/product-ingredients.md`** — new "Nutritional values" section between the existing
  "Allergen classification" and "Deletion is blocked while in use" sections: the four required per-100g
  fields, real schema column (not `GenericAttribute`), pre-GIL-004 rows backfill to `0` via the migration
  (technical default, not a business claim), admin-only in this task (no storefront/recipe-aggregation
  surface yet).

### Confirmed — no change needed

`Data/Mapping/Builders/IngredientBuilder.cs` (auto-map path already yields `AsDecimal(18,4) NOT NULL` for
an unconfigured plain `decimal`), `Infrastructure/MapperConfiguration.cs` (convention-based mapping,
already round-trips identically-named properties), `Admin/Controllers/IngredientsAdminController.cs`
(`ToEntity`/`ToModel`, no manual field assignment), `Admin/Factories/IngredientAdminModelFactory.cs`
(same), `IngredientLocalizedModel` (fields not localized, spec §6), `.csproj` (no new content files, `.cs`
compiles via SDK-style default globbing).

### Order of work

1. `Domain/Ingredient.cs` → 2. `Data/Migrations/NutritionalValuesMigration.cs` → 3. `plugin.json` →
4. `IngredientsPlugin.cs` → 5. `IngredientModel.cs` → 6. `IngredientValidator.cs` →
7. `_CreateOrUpdate.Info.cshtml` → 8. `Docs/BusinessLogic/product-ingredients.md` → 9. tests alongside
steps 1/5/6, not after.

### Tests

- `Nop.Tests.Nop.Services.Tests/Ingredients/IngredientServiceTests.cs` — new round-trip test inserting an
  `Ingredient` with all four fields set (including a genuine `0` case, e.g. water), reload, assert exact
  values; mirrors the file's own `InsertIngredientAsync_SeedsAReflexiveClosureRow` shape.
- New `Nop.Tests.Nop.Services.Tests/Ingredients/IngredientValidatorTests.cs`, mirroring
  `ServingSuggestionValidatorTests.cs` (`ServiceTest` base, AwesomeAssertions, one `[Test]` per scenario):
  `Validate_Fails_When{Field}IsNegative` ×4, `Validate_Succeeds_WhenAllNutritionalValuesAreZero`.
- **Not separately tested, by design/precedent:** blank-field-rejected (framework model-binding +
  `NotNullValidationMessageAttribute` mechanism — same as `ProductModel.Price`, untested anywhere in this
  repo either) and the migration body itself (`MigrationProcessType.Update` migrations never execute in
  `ServiceTest.InitPlugins()`, confirmed no precedent in this codebase tests a migration body directly —
  schema correctness is exercised through the service round-trip test instead).

### Standards skills to load during implementation

`data-access-standards-check`, `migration-standards-check`, `localization-standards-check`,
`admin-ui-standards-check`, `testing-standards-check`.

**Approved by:** Mateusz Nycz
**Date:** 2026-09-03
**Revision notes:** none — approved as proposed.
