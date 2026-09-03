---
id: GIL-004
kind: Task
title: Nutritional values (kcal + macros) on Ingredient, per 100g
status: Ready
---

# Task — Nutritional values (kcal + macros) on Ingredient, per 100g

> **Ready.** Confirmed by `spec-intake` (round 3) — all gaps from rounds 1-2 resolved. Next step:
> `plan-and-implement`.

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

All four **nullable** decimal — an ingredient can be created before its nutritional data is known, and
GIL-001's existing ingredients in any already-seeded/test data have none today. Exact decimal
precision/scale is for `ddd-modeler`.

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

Forward-only, additive, nullable columns: safe on a store that already has ingredient rows (existing rows
simply read back `null` for all four), and safe under a rolling deploy (old app instances never reference
the new columns).

`plugin.json`'s `Version` must be bumped in the same change (`migration-standards-check` requirement) so
the migration runner re-applies on upgrade.

## 6. Admin & storefront surface

**Admin only** in this task — no storefront rendering. Four new numeric fields added to the existing
Ingredient Create (`Admin/Views/Create.cshtml`) and Edit (`Admin/Views/Edit.cshtml`) forms, alongside
`Name`/`Description`/`Allergen`. Not localized (the values are language-independent numbers, unlike
`Name`/`Description`), so no change to `IngredientLocalizedModel` or the `Locales` editor.

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

- **Invalid input** (negative value, non-numeric). Resolved: standard admin-form validation —
  `IngredientValidator` gets a `GreaterThanOrEqualTo(0)` rule per field when a value is supplied; `null`
  (not yet known) remains valid, since the fields are optional.
- **Existing ingredients with no nutritional data.** Not a failure case — nullable columns read back as
  `null`/empty on the edit form, same as any admin backfill scenario.
- **Concurrent edit of the same ingredient.** Already covered by GIL-001's existing concurrency handling
  for `Ingredient` edits (no separate optimistic-lock concern introduced by four more plain columns).

## 11. Test scenarios

- Creating an ingredient with all four values set persists and reads them back correctly.
- Creating an ingredient with all four values left empty succeeds (fields optional) and reads back as
  `null`.
- Editing an existing ingredient to add previously-missing nutritional values persists them.
- Negative value for any of the four fields is rejected by admin-form validation.
- A migration run against a database that already has `Ingredient` rows (from GIL-001) leaves existing
  rows intact with the four new columns `null`.

## 12. Documentation impact

`Docs/BusinessLogic/product-ingredients.md` (exists, already documents the Allergen classification field
in the same shape — real column, not free text) is updated in the same commit to add the four nutritional
fields. No new glossary term.

## 13. Deployment & rollout

No Docker/`appsettings`/ECS change. Purely additive nullable columns plus a `plugin.json` version bump —
safe on an existing installation, immediate rollout, no data backfill required (existing ingredients
simply have no nutritional data until an admin fills it in).
