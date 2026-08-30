---
id: GIL-001
kind: Task
title: Ingredient list on products, with composite ingredients
status: In Progress
---

# Task — Ingredient list on products, with composite ingredients

> **Ready.** Confirmed by `spec-intake` (round 3) — all open questions from round 1 resolved and folded
> into the sections below; two narrow round-2 gaps (admin-menu extension point, external-dependency N/A)
> corrected. Next step: `plan-and-implement`.

## 1. Business goal & outcome

Every product in the catalogue (premium jarred/canned meals) can carry a list of its ingredients, shown
to the customer on the product page.

An ingredient is either **simple** (salt, water, onion) or **composite** — itself made of ingredients.
Onion soup lists "beef broth" as one ingredient, and beef broth is itself bones, water, carrot, celery,
salt. The nesting is the point: the customer must see what is actually in the jar, and the shop must not
re-type the broth's composition into every recipe that uses it.

**Outcome:** an admin defines an ingredient once, composes it from other ingredients, attaches it to any
number of products, and the storefront renders the full composition.

**Out of scope for v1 (QS), resolved:** allergen severity, quantities/percentages, descending-by-weight
ordering, full EU-labelling compliance, ingredient import/export, and storefront filtering by ingredient
or diet (e.g. "show me vegan products"). These are explicitly anticipated future work, not rejected —
the data model must not block them: an ingredient's allergen classification (§5) is stored as a real,
queryable enum rather than free text specifically so a future "vegan/allergen filter" feature can be
built as an additive query against the existing schema, not a rework of it.

## 2. Root cause / current behavior

N/A — new feature.

## 3. Placement — plugin or core?

**New plugin.** No core change expected: the data is entirely new and the storefront rendering goes
through a widget zone.

Verified: `IMiscPlugin` (`src/Libraries/Nop.Services/Common/IMiscPlugin.cs:9`) and `IWidgetPlugin`
(`src/Libraries/Nop.Services/Cms/IWidgetPlugin.cs:8`) both derive from `IPlugin`, so one plugin class can
implement both. Fifteen `productdetails_*` widget zones exist
(`src/Presentation/Nop.Web.Framework/Infrastructure/PublicWidgetZones.cs:159-173`) and are rendered in
both product templates — **no view fork is needed**.

**Resolved (Q0):** own plugin, `Nop.Plugin.Misc.Ingredients` (`SystemName` `Misc.Ingredients`) — not
folded into the proposed (and not yet built) `Nop.Plugin.Misc.GastronomyCompliance`. Composition and
compliance data have different lifecycles and different ownership.

## 4. Extension point

`IMiscPlugin` + `IWidgetPlugin`. The admin menu entry goes through an `AdminMenuCreatedEvent`/
`ThirdPartyPluginsMenuItemCreatedEvent` consumer (`BaseAdminMenuCreatedEventConsumer`,
`src/Presentation/Nop.Web.Framework/Events/BaseAdminMenuCreatedEventConsumer.cs`) — **not**
`IAdminMenuPlugin`, which is `[Obsolete]` (`IAdminMenuPlugin.cs:8`) and explicitly superseded by these
events. Every plugin under `src/Plugins/` that extends the admin menu already does it this way (e.g.
`Nop.Plugin.Misc.Polls`, `Nop.Plugin.Misc.News`, `Nop.Plugin.Misc.Forums`); none implements the obsolete
interface.

## 5. Data model & migration

New plugin-owned entities. The composition is relational and must be queryable, which rules out
`GenericAttribute` regardless of the other arguments.

Existing mechanisms considered and rejected (checklist item S5). Everything below is verified against
`src/`. The exhaustive list of what exists — including the relations this spec originally missed — now
lives in [`Docs/knowledge-base/14-product-relations-map.md`](../../knowledge-base/14-product-relations-map.md);
the table here records only the rejection reasons specific to this feature.

| Mechanism | Evidence | Why it does not fit |
|---|---|---|
| `ProductTag` | — | Flat labels. No composition, no per-ingredient fields. |
| `SpecificationAttribute` / `Option` | `SpecificationAttribute.cs:13-23`, `SpecificationAttributeOption.cs:13`, `ProductSpecificationAttribute.cs:13,23,28`, `SpecificationAttributeGroup.cs:8` | Exactly one grouping level, no attribute-to-attribute relation. "Beef broth is made of X, Y, Z" is inexpressible. |
| `ProductAttribute` / `ProductAttributeCombination` | — | Models customer-selectable variants affecting price and stock. An ingredient is not a purchase choice. |
| `GenericAttribute` | — | Schema-free blob. Cannot answer "which products contain celery" — the whole point for allergens. |
| **Grouped products** (`Product.ParentGroupedProductId`) | `src/Libraries/Nop.Core/Domain/Catalog/Product.cs:23` | A real self-reference on `Product`, but **one level only** and it means "variant of", not "made of". |
| **Bundled products** (`AttributeValueType.AssociatedToProduct`) | `ProductAttributeValue.cs:24`, `AttributeValueType.cs:14,16` | nopCommerce's actual bundling mechanism — product → product via an attribute value. Not recursive, and it makes every ingredient a sellable product. |
| **`FilterLevelValue`** | `src/Libraries/Nop.Core/Domain/FilterLevels/FilterLevelValue.cs:9`, `FilterLevelValueProductMapping.cs:11,16`, `StandardPermission.cs:67-69` | Closest shipped mechanism: hierarchical, `ILocalizedEntity`, product-mapped, with its own admin controller, permissions and storefront search. **Rejected (Q8):** not actually a tree — `FilterLevel1/2/3Value` are three string columns on one row, not parent-child links, so composition is inexpressible regardless of depth. |
| Product-as-ingredient (self-referencing mapping on `Product`) | — | **Rejected (Q1):** would put "beef bones" in the product table, inheriting `Published`/`VisibleIndividually`/price/stock it has no use for. Own entity confirmed — reinforced by ingredient-specific fields (allergen classification) a `Product` row has no place for. |

Precedent for a self-referencing tree in this codebase: `Category.ParentCategoryId`
(`src/Libraries/Nop.Core/Domain/Catalog/Category.cs:48`).

**Conflict with the harness's own domain guidance — resolved.**
`Docs/ai-harness/05-domain-gastronomy-guidelines.md:17` prescribed, for "Ingredient list /
ingredients-of-concern", either `GenericAttribute` or `SpecificationAttribute`. This spec rejects both,
and the rejection stands (Q9): the guideline's ingredient-list row and its "⚠️ Contested" callout are
corrected in the same commit as this spec's implementation, so the harness stops prescribing a mechanism
this project rejected.

**Resolved for `ddd-modeler`:** own entity (`Ingredient`); no quantity column (Q3) — the composition
relation carries only a `DisplayOrder` (int), no percentage or weight. `Ingredient` carries, beyond the
localized `Name` (Q7b): a localized `Description`, and an allergen classification — an **enum of the 14
EU-regulated allergens**, not a bool, since labelling needs to name *which* allergen, not just flag that
one exists. No `IngredientCategory` table (dairy/meat/plant/etc.) in v1 — deferred until a concrete
filtering feature needs it; adding it later is an additive migration, not a rework of what ships here.
Exact column types, nullability, and defaults remain for `ddd-modeler`.

**Future direction, not built now (Q13):** a single store exists today, and the ingredient catalogue is
not store-mapped. When additional stores are introduced, ingredient catalogues are expected to become
store-specific — planned as a future migration adding `IStoreMappingSupported` to `Ingredient`, not part
of this spec.

## 6. Admin & storefront surface

**Resolved:**

- **Storefront (Q4):** nesting shown fully expanded inline, EU-label style — e.g. "beef broth (bones,
  water, carrot, celery, salt)". Rendered into widget zone `productdetails_before_collateral`
  (`PublicWidgetZones.cs:163`).
- **Admin (Q5):** both a dedicated Catalog page (manage the shared ingredient catalogue) and a tab on the
  product-edit page (attach/detach/reorder ingredients for that product).

Page, view model, and validator specifics are for `ddd-modeler`/`implementation-planner`.

**Resolved (Q14 — storefront visibility):** the ingredient list is not independently ACL'd. It inherits
the product's own `IAclSupported` restrictions (`Product.cs:13`) — if a guest or customer role can see
the product, they see its ingredients; no separate visibility rule.

**Resolved (Q15 — read scope):** rendered only on the product detail page in v1, not on listing or
quick-view. Because the composition closure is materialized at write time (§9), a page render reads it
with a single query against already-current data — no per-level or recursive read-time traversal.

## 7. Settings, permissions, localization

**Permissions.** This codebase splits catalog permissions into View / CreateEditDelete pairs
(`src/Libraries/Nop.Services/Security/StandardPermission.cs:54-73`). **Resolved (Q6):** a
View/CreateEditDelete pair, following that convention. **Administrators only** — Vendors are never
given this permission; vendor-scoped ingredient ownership is not a concept this feature has.

**Localization.** `ILocalizedEntity` is an empty marker
(`src/Libraries/Nop.Core/Domain/Localization/ILocalizedEntity.cs:6`); values live as `LocalizedProperty`
rows (`LocalizedProperty.cs:11-31`), read via `ILocalizationService.GetLocalizedAsync<TEntity,TPropType>`
(`ILocalizationService.cs:128-130`); the admin side is `ILocalizedModel<T>` / `ILocalizedModelFactory`.

Two facts that matter and have no precedent here:

- `LocaleKeyGroup` is the **unqualified** type name (`entity.GetType().Name`,
  `src/Libraries/Nop.Services/Localization/LocalizationService.cs:506`), so a plugin entity called
  `Ingredient` occupies a global key space shared with core types.
- **No plugin under `src/Plugins/` currently declares `ILocalizedEntity` on its own entity.** This would
  be the first, so there is no in-repo pattern to mirror.

Ingredient names (and descriptions) are customer-facing and must be localized. **Resolved (Q7):** on
uninstall, `LocalizedProperty` rows for `LocaleKeyGroup = "Ingredient"` are deleted along with the
entity rows — no orphaned rows left behind.

**Settings.** None required in v1 — presentation (Q4) and admin placement (Q5) are fixed, not
store-configurable.

## 8. Events & scheduled tasks

Built-in `EntityInserted/Updated/DeletedEvent<T>` cover change notification. No custom event, no
scheduled task.

Note for design: `IEventPublisher` is synchronous and in-process, so a consumer that throws propagates
into the publisher's call — relevant to the write path in Q11.

## 9. Caching

**Resolved (Q10):** the transitive composition closure (every ingredient a given ingredient or product
contains, at any depth) is maintained **transactionally in the database at write time**, not computed
and cached at read time. Editing beef broth updates the closure rows for everything that contains it, in
the same transaction as the edit (see §10, Q11). There is no derived, cacheable read-time artifact whose
staleness could show a wrong ingredient list, and no dependency on cross-instance cache coherence for
correctness. This also means a future filtering query (e.g. "products containing only plant-origin
ingredients") reads the same always-current closure directly, with no separate correctness concern of
its own.

Multi-instance: `DistributedCacheConfig.Enabled` defaults to `false`
(`src/Libraries/Nop.Core/Configuration/DistributedCacheConfig.cs:20`); there is no deployment yet — only
localhost. Redis/`RedisSynchronizedMemory` is **not a prerequisite** for this feature. An optional
in-process render cache may still be layered on later purely for performance; any staleness there would
be a cosmetic display lag, not an incorrect answer, since it is never the source of truth for a filter
query.

## 10. Failure scenarios

**External dependencies:** N/A — no external dependency (payment provider, carrier, SMTP, Redis) is
involved. The only dependency is the database, assumed available like any other nopCommerce write.

- **Cycles.** Broth contains stock, stock contains broth. **Resolved (Q11):** cycle-checked and written
  inside a single database transaction, not a best-effort read-then-write — two admins each adding one
  edge cannot commit a cycle the other's transaction didn't see.
- **Partial write.** Entity row, composition rows, and `LocalizedProperty` rows are three writes.
  **Resolved (Q11):** the whole operation is transactional; a half-created ingredient is not acceptable.
- **Deleting an ingredient still in use. Resolved (Q12):** blocked, with a message listing what still
  uses it. No cascade.
- **Maximum nesting depth. Resolved (Q12):** a hard write-time limit of **3**. Real recipes are expected
  to need 2 levels; 3 is the developer-set absolute ceiling. Enforced at write time (reject an edge that
  would exceed it) and re-checked at render time as a defensive cut-off against bad data.

## 11. Test scenarios

- Simple ingredient attached to a product renders.
- Composite ingredient renders its children; nesting at the maximum depth (3 levels) renders correctly.
- An edge that would exceed the maximum depth (3 levels) is rejected at write time.
- Cycle creation is rejected, including the concurrent case (two transactions each adding one edge of the
  same would-be cycle).
- Editing a nested ingredient's composition updates the closure for every product that contains it, at
  any depth, in the same transaction.
- An ingredient still in use cannot be deleted; the error names what still uses it.
- Localized name falls back correctly when a translation is missing.

## 12. Documentation impact

New `Docs/BusinessLogic/product-ingredients.md` and glossary entries, same commit as the code. Plus the
correction to `Docs/ai-harness/05-domain-gastronomy-guidelines.md:17` (Q9, resolved — rejection stands).

## 13. Deployment & rollout

No image, `appsettings`, or ECS change expected — resolved (Q10): no Redis prerequisite, and there is no
deployment yet, only localhost. Existing products are unaffected until ingredients are attached, so
rollout is immediate.

## Technical design (ddd-modeler)

### Corrections to the spec's technical assumptions

None of the spec's technical claims were contradicted by the source. Everything cited in section 3–8
(interfaces, event consumer base class, `FilterLevelValue` shape, permission split, `LocaleKeyGroup`
derivation, absence of `ILocalizedEntity` on any plugin entity, absence of `IAdminMenuPlugin`
implementations) checked out against `src/` exactly as stated. Specific re-verifications performed for
this design (beyond what spec-intake already did):

- Re-grepped `ILocalizedEntity` across `src/Plugins` (member-level, not filename) — zero hits, confirming
  Ingredient is genuinely first. Re-grepped `IAdminMenuPlugin` across `src/Plugins` — zero hits.
- Re-grepped `class Ingredient` (and `Ingredient*`) across all of `src/` — no existing type occupies that
  name, so `LocaleKeyGroup = "Ingredient"` is not contested by anything already in the shared key space.

### Placement

New plugin `Nop.Plugin.Misc.Ingredients`, `SystemName` `Misc.Ingredients`, implementing `IMiscPlugin` +
`IWidgetPlugin` on one `IngredientsPlugin : BasePlugin` class — matches the exact shape of
`Nop.Plugin.Misc.News` (`NewsPlugin : BasePlugin, IMiscPlugin, IWidgetPlugin`,
`src/Plugins/Nop.Plugin.Misc.News/NewsPlugin.cs:16`), `Nop.Plugin.Misc.RFQ` (`RfqPlugin.cs:20`) — both are
"Misc" plugins that own their own data and inject storefront/admin widget markup, the closest structural
precedent. No core touch anywhere in this design.

### Domain model

Four new plugin-owned tables, all under `Nop.Plugin.Misc.Ingredients.Domain`:

**`Ingredient : BaseEntity, ILocalizedEntity`**
- `Name` (string, 400, not nullable) — localizable default value; mirrors `Category.Name`
  (`CategoryBuilder.cs:20`, `AsString(400).NotNullable()`).
- `Description` (string, nullable) — localizable default value; left to the builder's default column
  type, mirroring `Category.Description`, which `CategoryBuilder.MapEntity` also leaves unconfigured.
- `AllergenId` (int, not nullable, default 0) — backing field for a computed
  `AllergenType Allergen { get => (AllergenType)AllergenId; set => AllergenId = (int)value; }` property,
  exactly the pattern `Product.ProductTypeId`/`Product.ProductType` uses (`Product.cs:18,538-542`). Only
  `AllergenId` is mapped by the entity builder; `Allergen` is not a column.
- `CreatedOnUtc`, `UpdatedOnUtc` (DateTime) — mirrors `FilterLevelValue` (`FilterLevelValue.cs:29,34`), the
  closest shipped precedent for a hierarchical, localized, admin-managed master-data entity.
- No `IAclSupported`, no `IStoreMappingSupported` (both explicitly deferred — spec §5 Q13, §6 Q14).

`AllergenType` enum (`Nop.Plugin.Misc.Ingredients.Domain`), the 14 EU Regulation 1169/2011 Annex II
allergens plus `None = 0`:
```
None, CerealsContainingGluten, Crustaceans, Eggs, Fish, Peanuts, Soybeans, Milk,
Nuts, Celery, Mustard, SesameSeeds, SulphurDioxideAndSulphites, Lupin, Molluscs
```
This regulatory list is public knowledge, not something verifiable from `src/` — flagged for a final
compliance check before shipping, not a code-verification gap.

**Resolved (open question 3 — single-value allergen classification):** one `AllergenId` per row is
sufficient. A real ingredient that carries more than one EU allergen (e.g. soy sauce: soybeans + wheat)
is modeled as a 2-level composite (soy sauce → soybean extract, wheat) instead of multiple allergen tags
on one row — confirmed acceptable admin data-entry UX, same composition mechanism, not a second one.

**`IngredientComposition : BaseEntity`** (the direct edges — source of truth for the DAG)
- `ParentIngredientId` (int, not nullable) — the composite ingredient.
- `ChildIngredientId` (int, not nullable) — the component.
- `DisplayOrder` (int, not nullable, default 0).
- **No FK constraint on either column.** Verified precedent: `RelatedProduct.ProductId1`/`ProductId2`
  (`RelatedProduct.cs:11,16`) — two columns pointing at the *same* target table — has an **empty**
  `MapEntity` (`RelatedProductBuilder.cs:17-19`), and `Category.ParentCategoryId` (single self-reference)
  is likewise left unconstrained in `CategoryBuilder`. Confirmed the reason: `NopEntityBuilder`'s
  `.ForeignKey<TPrimary>(...)` extension (`FluentMigratorExtensions.cs:106-115`) exposes no
  constraint-name parameter — declaring it twice against the same target table from the same source table
  would collide on FluentMigrator's auto-generated constraint name.

**`IngredientClosure : BaseEntity`** (the transitive closure §9 explicitly requires, maintained at write
time — not admin-editable, no `ILocalizedEntity`)
- `AncestorIngredientId`, `DescendantIngredientId` (int, not nullable, no FK — same reasoning as above).
- `Depth` (int, not nullable) — the **longest** known number of `IngredientComposition` edges between the
  pair (reflexive rows `(X,X,0)` for every ingredient). Longest, not shortest: the constraint being
  enforced is "no realizable expansion path exceeds 3 edges," and a diamond-shaped reuse (e.g. "salt"
  reachable from "onion soup" both directly and via "beef broth") must be judged by its longest path, or a
  shorter alternate path would mask a real over-depth chain.

**Resolved (open question 2 — depth-counting convention):** the maximum depth of 3 counts only
ingredient-to-ingredient `IngredientComposition` edges, never the product-to-ingredient attachment hop.
Confirmed by example: a product attaches a composite ingredient (e.g. beef broth), which itself has other
ingredients (bones, water, carrot, celery, salt) — that is the real shape the 3-level ceiling has to
cover, and the product attachment is a separate, uncounted relation.

**`ProductIngredientMapping : BaseEntity`**
- `ProductId` (int, not nullable, `.ForeignKey<Product>()` — default `Rule.Cascade`, matching
  `FilterLevelValueProductMapping.ProductId` exactly, `FilterLevelValueProductMappingBuilder.cs:23`).
- `IngredientId` (int, not nullable, `.ForeignKey<Ingredient>(onDelete: Rule.None)`) — explicit `Rule.None`
  rather than the default `Cascade`, because deletion-while-in-use is blocked at the service layer (§10,
  "no cascade") and the DB constraint should not silently contradict that if some future path ever
  bypasses the service. `Rule.None` is a real, precedented option (`Order.CustomerId`,
  `CustomWishlist.CustomerId`, `DiscountRequirement.ParentId` all use it, `OrderBuilder.cs:26`,
  `CustomWishlistBuilder.cs:22`).
- `DisplayOrder` (int, not nullable, default 0).

Table names: unqualified type name (`Ingredient`, `IngredientComposition`, `IngredientClosure`,
`ProductIngredientMapping`), matching every other entity in this codebase
(`NameCompatibilityManager.GetTableName`).

**Invariants:**
- No self-loop (`ParentIngredientId != ChildIngredientId`).
- No cycle: adding edge `(P,C)` is rejected if `C` is already an ancestor of `P` in `IngredientClosure`.
- No path between any ancestor of `P` (inclusive) and any descendant of `C` (inclusive) may exceed 3
  edges.
- Uniqueness of `(ParentIngredientId, ChildIngredientId)` and `(ProductId, IngredientId)` enforced at the
  service layer (check-then-insert), not via a DB unique index — this matches the existing precedent
  everywhere else in the codebase (`ProductController`'s `RelatedProductAddPopup`/
  `FilterLevelValuesAddPopup` do the identical "does this mapping already exist" check before insert; no
  migration in this repo declares a composite unique index for a mapping table).

### Extension decision

Own entity, confirmed against `src/`, not `GenericAttribute`/`SpecificationAttribute`/`FilterLevelValue`/
grouped or bundled products — this exact rejection chain is already in spec §5 with file:line evidence
and was re-verified independently:
- `FilterLevelValue.cs:9-24` — confirmed 3 string columns (`FilterLevel1Value/2Value/3Value`), no
  parent-child FK, so "not a tree" is accurate.
- `RelatedProduct.cs`/`ProductAttributeValue.AssociatedProductId` — confirmed one level only, no
  recursion.

This is a schema migration, not `GenericAttribute`, because the composition must be joinable (cycle/depth
checks, the future "products containing celery" filter) — `GenericAttribute` is a flat key/value blob per
entity instance and cannot answer either question, which is exactly the stated decision rule in
`Docs/knowledge-base/04-extending-core-entities.md`.

### Design

**Migration.** One migration class, `Migration` base (not `ForwardOnlyMigration`), with a real `Down()` —
verified precedent: `Nop.Plugin.Misc.RFQ`'s `SchemaMigration.cs`
(`[NopMigration("2024-07-03 10:30:08", "Nop.Plugin.Misc.RFQ schema", MigrationProcessType.Installation)]
public class SchemaMigration : Migration`) creates 4 tables in `Up()` and calls
`this.DeleteTableIfExists<T>()` for each, in reverse order, in `Down()`. This matters mechanically:
`PluginService.UninstallPluginsAsync` (`PluginService.cs:581`) calls
`_migrationManager.Value.ApplyDownMigrations(assembly)` **immediately after** `plugin.UninstallAsync()` —
so our own tables are dropped automatically by the framework on uninstall; `UninstallAsync()` itself must
not try to duplicate this.
```csharp
[NopMigration("2026-08-30 00:00:00", "Nop.Plugin.Misc.Ingredients schema", MigrationProcessType.Installation)]
public class SchemaMigration : Migration
{
    public override void Up()
    {
        this.CreateTableIfNotExists<Ingredient>();
        this.CreateTableIfNotExists<IngredientComposition>();
        this.CreateTableIfNotExists<IngredientClosure>();
        this.CreateTableIfNotExists<ProductIngredientMapping>();
    }
    public override void Down()
    {
        this.DeleteTableIfExists<ProductIngredientMapping>();
        this.DeleteTableIfExists<IngredientClosure>();
        this.DeleteTableIfExists<IngredientComposition>();
        this.DeleteTableIfExists<Ingredient>();
    }
}
```
This automatic table-drop is *not* special to this feature — it's how every plugin with its own schema
behaves here (RFQ loses its request/quote history on uninstall too). What genuinely is special here (per
spec §7, Q7): `LocalizedProperty` is a **shared core table** our migration must never touch structurally,
so rows with `LocaleKeyGroup = "Ingredient"` survive the table drop as orphans unless `UninstallAsync()`
explicitly purges them — this is the one piece of cleanup our plugin must do that no existing plugin
needed to.

**Transactions (§10, cycle/depth check).** `INopDataProvider.CreateTransactionScope()`
(`BaseDataProvider.cs:503-534`) returns a real ambient `System.Transactions.TransactionScope` with
`IsolationLevel.Serializable` and `TransactionScopeAsyncFlowOption.Enabled` — already used for bulk
operations (`EntityRepository.cs:361,467`, `SyncCodeHelper.cs:103,119`). This is the mechanism for §10's
"cycle-checked and written inside a single database transaction": inject `INopDataProvider`, wrap the
whole add/remove-composition-edge operation in
`using var transaction = _dataProvider.CreateTransactionScope(); ... transaction.Complete();`.
`Serializable` isolation on PostgreSQL is true serializable-snapshot isolation, so two concurrent
transactions that would jointly commit a cycle get a serialization-failure abort on one of them (Npgsql
`PostgresException`, SQLSTATE `40001`) rather than silently both succeeding.

**Resolved (open question 4 — concurrent-conflict UX):** the losing transaction surfaces to the admin as
a generic "someone else changed this at the same time, please try again" error. No automatic retry.

**Closure maintenance algorithm** (concretizing §9, which states the mechanism exists but not its shape):
1. Inside the transaction, validate the candidate edge `(P,C)` against the **current**
   `IngredientClosure`: reject if `P==C`; reject if a row `(C,P,_)` exists (cycle); reject if
   `max over ancestors A of P (incl. P) and descendants D of C (incl. C) of depth(A,P)+1+depth(C,D) > 3`.
2. Apply the edge to `IngredientComposition`.
3. Recompute `IngredientClosure` from scratch: delete all rows, reseed reflexive `(X,X,0)` for every
   ingredient, then run a fixed-point join against `IngredientComposition` bounded to 3 rounds (bounded by
   the depth cap itself), taking `Depth = max(existing, candidate)` per pair.
4. `transaction.Complete()`.

Full recompute rather than incremental delta maintenance is the deliberate simplification here — see
Simplicity check.

**Services** (`Nop.Plugin.Misc.Ingredients.Services`):
- `IIngredientService` / `IngredientService` — `GetIngredientByIdAsync` (mirrors
  `CategoryService.GetCategoryByIdAsync`'s `_repository.GetByIdAsync(id, cache => default)`, which
  resolves to the standard `NopEntityCacheDefaults<Ingredient>.ByIdCacheKey`), `GetAllIngredientsAsync`
  (paged, uncached, mirrors `FilterLevelValueService.GetAllFilterLevelValuesAsync`),
  `InsertIngredientAsync`/`UpdateIngredientAsync` (transactional, validate+localize),
  `DeleteIngredientAsync` (checks `IngredientComposition.ChildIngredientId == id` and
  `ProductIngredientMapping.IngredientId == id`, throws a message naming the composite ingredients and
  products found, else deletes the row plus its own outgoing `IngredientComposition` rows and recomputes
  the closure — no cascade).
- `IIngredientCompositionService` — add/remove child edge (the transactional cycle/depth-checked write
  above), reorder (plain `DisplayOrder` update, no closure recompute needed), get children of an
  ingredient.
- `IProductIngredientService` (or folded into the above) — attach/detach/reorder ingredients on a product
  (`ProductIngredientMapping` CRUD, no closure impact), and the storefront read: get a product's
  directly-attached ingredients + all `IngredientComposition` rows reachable from them via
  `IngredientClosure` in one extra query, then build the nested "beef broth (bones, water, ...)" string in
  memory, bounded to 3 levels as the defensive render-time cutoff §10 asks for.

**Resolved (open question 1 — "single query" render, §6/§9):** 2 focused database round trips per render
(directly-attached ingredients, then all reachable `IngredientComposition` edges via one
`IngredientClosure`-scoped subquery) is an acceptable reading of §6's "a single query" — not literally one
SQL statement.

- `IngredientCacheEventConsumer : CacheEventConsumer<Ingredient>` (empty body) — wires the standard
  `EntityInserted/Updated/DeletedEvent<Ingredient>` → `ByIdCacheKey` invalidation, matching every other
  entity's cache consumer. This is the only cache anywhere in this design; the composition/closure data
  itself is never cached, per §9's resolved decision.

**Events.** Built-in `EntityInserted/Updated/DeletedEvent<Ingredient>` only — no custom event, matching
§8. No scheduled task.

**Permissions.** Plugin-owned constants (cannot literally extend `StandardPermission.Catalog` — it's a
`partial class` in `Nop.Services.Security`, a different assembly; verified precedent:
`Nop.Plugin.Misc.RFQ`'s and `Nop.Plugin.Misc.News`'s permission managers define their own constant strings
and pass `nameof(StandardPermission.Catalog)` only as the category label string for admin-grid grouping):
```csharp
public class IngredientsPermissionConfigManager : IPermissionConfigManager
{
    public const string INGREDIENTS_VIEW = "Ingredients.IngredientsView";
    public const string INGREDIENTS_CREATE_EDIT_DELETE = "Ingredients.IngredientsCreateEditDelete";
    public IList<PermissionConfig> AllConfigs => new List<PermissionConfig>
    {
        new("Admin area. Ingredients. View", INGREDIENTS_VIEW, nameof(StandardPermission.Catalog), NopCustomerDefaults.AdministratorsRoleName),
        new("Admin area. Ingredients. Create, edit, delete", INGREDIENTS_CREATE_EDIT_DELETE, nameof(StandardPermission.Catalog), NopCustomerDefaults.AdministratorsRoleName)
    };
}
```
Only `AdministratorsRoleName` passed (no `VendorsRoleName`) — satisfies §7's "Administrators only."

**Admin menu.** `IngredientsMenuEventConsumer : BaseAdminMenuCreatedEventConsumer`, **not**
`IAdminMenuPlugin` — override `PluginSystemName`, `CheckAccessAsync` (→
`AuthorizeAsync(INGREDIENTS_VIEW)`), `GetAdminMenuItemAsync` returning a leaf item, `InsertType = After`,
`AfterMenuSystemName = "Filter level values"` — places entry as sibling of "Product tags"/"Filter level
values" inside the existing "Catalog" node, not under "Local plugins."

**Admin controller/views.** `IngredientsAdminController : BasePluginController`, decorated exactly like
`NewsAdminController` (`[Area(AreaNames.ADMIN)] [AutoValidateAntiforgeryToken] [ValidateIpAddress]
[AuthorizeAdmin] [SaveSelectedTab]`), gated per-action with `[CheckPermission(...)]`:
- Dedicated Catalog page: `List`/`Create`/`Edit`/`Delete` for the ingredient catalogue, plus a nested
  DataTables grid on the Ingredient edit page for its own children (attach/detach/reorder composition),
  reusing the exact `AddPopup` + inline-edit `DisplayOrder` + `RenderButtonRemove` pattern already shipped
  for `_CreateOrUpdate.RelatedProducts.cshtml`/`ProductController.RelatedProductUpdate`.
- Product-edit tab: rendered via `IWidgetPlugin` targeting `AdminWidgetZones.ProductDetailsBlock`,
  rendered inside `<nop-cards>` in `_CreateOrUpdate.cshtml:114` — must render an actual
  `<nop-card asp-name="product-ingredients" ...>` fragment to appear as a genuine additional section, and
  must itself check `AuthorizeAsync(INGREDIENTS_VIEW)` before rendering — also what naturally keeps it
  invisible to Vendors without extra code.

**Storefront.** `IngredientsViewComponent : NopViewComponent`, `GetWidgetViewComponent` returns it for
`PublicWidgetZones.ProductDetailsBeforeCollateral`, rendered by both `ProductTemplate.Simple.cshtml:157`
and `ProductTemplate.Grouped.cshtml:110` with `additionalData = Model`. No separate ACL check — inherits
whatever visibility the product page itself already enforced.

**Localization.** `ILocalizedEntity` on `Ingredient`, `LocaleKeyGroup = "Ingredient"` — genuinely
first-of-its-kind in this repo. Names/descriptions localized the same way `Category.Name` is. Allergen
display text uses `ILocalizationService.GetLocalizedEnumAsync<AllergenType>`, resource key
`Enums.Nop.Plugin.Misc.Ingredients.Domain.AllergenType.Celery` etc. `InstallAsync` seeds these plus
`Plugins.Misc.Ingredients.*` UI strings; `UninstallAsync` removes them and **explicitly** bulk-deletes
`LocalizedProperty` rows via `IRepository<LocalizedProperty>.DeleteAsync(lp => lp.LocaleKeyGroup ==
"Ingredient")` — the one cleanup step not covered by the automatic table-drop.

**Widget registration.** `InstallAsync` must add `Misc.Ingredients` to `WidgetSettings.ActiveWidgetSystemNames`
and save it — without this the storefront widget pipeline never invokes the plugin. `HideInWidgetList =>
true` (matches News/Polls/Forums/RFQ).

### Simplicity check

The smallest version that satisfies the feature would skip `IngredientClosure` entirely: since the depth
cap is only 3, cycle/depth validation and rendering could both be done with a bounded (≤3-hop) read-time
traversal directly over `IngredientComposition`, no extra table, no recompute-on-every-write burden. Not
done, because §9 is already resolved against it, for two named reasons: (1) product-page render must be a
small fixed number of non-recursive queries, not per-level traversal; (2) the anticipated future "products
containing celery" filter needs to read the same always-current structure. Given that constraint, the
smaller implementation of the closure itself is proposed: full recompute-from-scratch on every composition
write (not incremental delta maintenance of the closure, a substantially harder graph problem for a DAG
under deletion) — appropriate given expected scale (tens to low hundreds of rows for one store).

### Blast radius

- `AdminWidgetZones.ProductDetailsBlock` is shared with `Nop.Plugin.Tax.Avalara`'s
  `EntityUseCodeViewComponent`. Multiple widgets targeting the same zone render in sequence — no
  conflict.
- `WidgetSettings.ActiveWidgetSystemNames` is shared core state; install/uninstall only add/remove our own
  `SystemName`.
- `"Catalog"` permission label reused only as UI grouping, shared with `FilterLevelValue`/`ProductTags` —
  no functional coupling.
- `LocalizedProperty` shared by every localized entity; our uninstall-time bulk delete is scoped to
  `LocaleKeyGroup == "Ingredient"` only, verified uncontested.
- Nothing touches `Product`, `Category`, or any other core entity's schema.

### Installed-store impact

No existing store data affected until an admin attaches an ingredient to a product. First deploy: one
migration creates 4 empty tables, one new permission pair (Administrators only — no existing role's
effective permissions shrink), a handful of new locale resources, one entry appended to
`WidgetSettings.ActiveWidgetSystemNames`. All additive, safe under rolling deploy. If uninstalled:
`ApplyDownMigrations` drops all 4 owned tables (data loss, matching every other schema-owning plugin's
uninstall behavior) and `UninstallAsync` explicitly purges orphaned `LocalizedProperty` rows.

### Reference files (for implementation-planner)

- `src/Presentation/Nop.Web.Framework/Events/BaseAdminMenuCreatedEventConsumer.cs`, `AdminMenuItem.cs`,
  `AdminMenu.cs:90-146`
- `src/Presentation/Nop.Web.Framework/Infrastructure/AdminWidgetZones.cs:197`,
  `PublicWidgetZones.cs:159-173`
- `src/Presentation/Nop.Web/Areas/Admin/Views/Product/_CreateOrUpdate.cshtml:86-115`,
  `_CreateOrUpdate.RelatedProducts.cshtml`, `_CreateOrUpdate.FilterLevelValuesProducts.cshtml`
- `src/Presentation/Nop.Web/Areas/Admin/Controllers/ProductController.cs:1696-1711`
  (RelatedProductUpdate), `1895-1980` (FilterLevelValue tab actions)
- `src/Plugins/Nop.Plugin.Misc.RFQ/Data/Migrations/SchemaMigration.cs`, `RfqPlugin.cs`,
  `Services/RfqPermissionConfigManager.cs`
- `src/Plugins/Nop.Plugin.Misc.News/NewsPlugin.cs`, `Services/NewsPermissionConfigManager.cs`,
  `Admin/Controllers/NewsAdminController.cs`
- `src/Plugins/Nop.Plugin.Tax.Avalara/Components/EntityUseCodeViewComponent.cs`,
  `AvalaraTaxProvider.cs`
- `src/Libraries/Nop.Data/Extensions/FluentMigratorExtensions.cs:106-135,308-316`
- `src/Libraries/Nop.Data/DataProviders/BaseDataProvider.cs:503-534`,
  `EntityRepository.cs:122-145,361,467`
- `src/Libraries/Nop.Services/Plugins/PluginService.cs:571-593`
- `src/Libraries/Nop.Services/Caching/CacheEventConsumer.cs`,
  `Catalog/Caching/CategoryCacheEventConsumer.cs`
- `src/Libraries/Nop.Services/Localization/LocalizationService.cs:491-621`
- `src/Libraries/Nop.Services/Security/StandardPermission.cs:52-79`,
  `DefaultPermissionConfigManager.cs:82-84`
- `src/Libraries/Nop.Core/Domain/Catalog/Category.cs`, `Product.cs`, `RelatedProduct.cs`,
  `FilterLevels/FilterLevelValue.cs`, `FilterLevelValueProductMapping.cs`

**Approved by:** Mateusz Nycz
**Date:** 2026-08-30
**Revision notes:** Developer confirmed the design as-is and resolved ddd-modeler's four open
questions in the same round (no re-invocation needed — none required a design change): (1) 2-query
render accepted as satisfying "single query" intent; (2) max-depth-3 confirmed to count only
ingredient-to-ingredient composition edges, not the product attachment hop; (3) single-value allergen
classification per row accepted, multi-allergen ingredients modeled as a 2-level composite; (4)
concurrent write conflicts surface as a generic retry error, no automatic retry.
