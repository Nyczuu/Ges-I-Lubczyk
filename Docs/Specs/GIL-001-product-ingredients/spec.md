---
id: GIL-001
kind: Task
title: Ingredient list on products, with composite ingredients
status: Ready
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
