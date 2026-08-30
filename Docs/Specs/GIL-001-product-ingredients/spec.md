---
id: GIL-001
kind: Task
title: Ingredient list on products, with composite ingredients
status: Draft
---

# Task — Ingredient list on products, with composite ingredients

> **Intake round 1: Not Ready.** Corrections below are folded in; the open questions at the bottom are
> the batch awaiting developer answers. Re-run `spec-intake` after they are answered.

## 1. Business goal & outcome

Every product in the catalogue (premium jarred/canned meals) can carry a list of its ingredients, shown
to the customer on the product page.

An ingredient is either **simple** (salt, water, onion) or **composite** — itself made of ingredients.
Onion soup lists "beef broth" as one ingredient, and beef broth is itself bones, water, carrot, celery,
salt. The nesting is the point: the customer must see what is actually in the jar, and the shop must not
re-type the broth's composition into every recipe that uses it.

**Outcome:** an admin defines an ingredient once, composes it from other ingredients, attaches it to any
number of products, and the storefront renders the full composition.

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

Plugin name and `SystemName` are **not yet fixed** — see Q0.

## 4. Extension point

`IMiscPlugin` + `IWidgetPlugin`. The admin menu entry goes through `IAdminMenuPlugin` /
`BaseAdminMenuCreatedEventConsumer` (`src/Presentation/Nop.Web.Framework/Menu/IAdminMenuPlugin.cs`,
`src/Presentation/Nop.Web.Framework/Events/BaseAdminMenuCreatedEventConsumer.cs`).

## 5. Data model & migration

New plugin-owned entities. The composition is relational and must be queryable, which rules out
`GenericAttribute` regardless of the other arguments.

Existing mechanisms considered and rejected (checklist item S5). Everything below is verified against
`src/`:

| Mechanism | Evidence | Why it does not fit |
|---|---|---|
| `ProductTag` | — | Flat labels. No composition, no per-ingredient fields. |
| `SpecificationAttribute` / `Option` | `SpecificationAttribute.cs:13-23`, `SpecificationAttributeOption.cs:13`, `ProductSpecificationAttribute.cs:13,23,28`, `SpecificationAttributeGroup.cs:8` | Exactly one grouping level, no attribute-to-attribute relation. "Beef broth is made of X, Y, Z" is inexpressible. |
| `ProductAttribute` / `ProductAttributeCombination` | — | Models customer-selectable variants affecting price and stock. An ingredient is not a purchase choice. |
| `GenericAttribute` | — | Schema-free blob. Cannot answer "which products contain celery" — the whole point for allergens. |
| **Grouped products** (`Product.ParentGroupedProductId`) | `src/Libraries/Nop.Core/Domain/Catalog/Product.cs:23` | A real self-reference on `Product`, but **one level only** and it means "variant of", not "made of". |
| **Bundled products** (`AttributeValueType.AssociatedToProduct`) | `ProductAttributeValue.cs:24`, `AttributeValueType.cs:14,16` | nopCommerce's actual bundling mechanism — product → product via an attribute value. Not recursive, and it makes every ingredient a sellable product. |
| **`FilterLevelValue`** | `src/Libraries/Nop.Core/Domain/FilterLevels/FilterLevelValue.cs:9`, `FilterLevelValueProductMapping.cs:11,16`, `StandardPermission.cs:67-69` | Closest shipped mechanism: hierarchical, `ILocalizedEntity`, product-mapped, with its own admin controller, permissions and storefront search. But the hierarchy is **fixed at three levels** and the levels are a classification taxonomy, not a composition. Rules out only if arbitrary depth is a hard requirement — see Q8. |
| Product-as-ingredient (self-referencing mapping on `Product`) | — | Would put "beef bones" in the product table. See Q1. |

Precedent for a self-referencing tree in this codebase: `Category.ParentCategoryId`
(`src/Libraries/Nop.Core/Domain/Catalog/Category.cs:48`).

**Conflict with the harness's own domain guidance — must be resolved, not ignored.**
`Docs/ai-harness/05-domain-gastronomy-guidelines.md:17` prescribes, for exactly "Ingredient list /
ingredients-of-concern", either `GenericAttribute` or `SpecificationAttribute`, and says to prefer the
latter "before inventing a new column". This spec rejects both. If the rejection stands, that line is
wrong and must be corrected in the same commit — otherwise the harness keeps prescribing the mechanism
we deliberately rejected. See Q9.

Exact entity shape, nullability, and defaults are for `ddd-modeler`, but it cannot start until Q1 and Q3
are answered — they determine whether there is a new entity at all and whether the composition relation
carries a quantity column.

## 6. Admin & storefront surface

Blocked on Q4 (nesting presentation) and Q5 (admin placement) — no pages, view models, or validators can
be specified until those are decided.

## 7. Settings, permissions, localization

**Permissions.** This codebase splits catalog permissions into View / CreateEditDelete pairs
(`src/Libraries/Nop.Services/Security/StandardPermission.cs:54-73`). Which shape and which roles: Q6.

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

Ingredient names are customer-facing and must be localized. Uninstall must decide what happens to the
`LocalizedProperty` rows — they are data, not locale resources: Q7.

**Settings.** Names and defaults undecided, contingent on Q4.

## 8. Events & scheduled tasks

Built-in `EntityInserted/Updated/DeletedEvent<T>` cover change notification. No custom event, no
scheduled task.

Note for design: `IEventPublisher` is synchronous and in-process, so a consumer that throws propagates
into the publisher's call — relevant to the write path in Q11.

## 9. Caching

The rendered ingredient tree is read on every product-page view and changes rarely — a strong cache
candidate. The key must include product, **language**, and store.

Invalidation must be **transitive**: editing beef broth invalidates every product containing it, at any
depth. This is the sharpest technical risk in the task.

Multi-instance: `DistributedCacheConfig.Enabled` defaults to `false`
(`src/Libraries/Nop.Core/Configuration/DistributedCacheConfig.cs:20`). Without Redis, an invalidation on
one ECS task leaves the others serving a stale tree until TTL. See Q10.

## 10. Failure scenarios

- **Cycles.** Broth contains stock, stock contains broth. Must be prevented at write time, and rendering
  must not recurse infinitely even against bad data. Concurrency: two admins each adding one edge can
  commit a cycle neither transaction saw — Q11.
- **Partial write.** Entity row, composition rows, and `LocalizedProperty` rows are three writes — Q11.
- Deleting an ingredient still in use — Q12.
- Maximum depth — Q12.

## 11. Test scenarios

- Simple ingredient attached to a product renders.
- Composite ingredient renders its children; nesting deeper than two levels renders correctly.
- Cycle creation is rejected.
- Editing a nested ingredient invalidates the containing products' cached output.
- An ingredient in use cannot be deleted (or is cascaded, per Q12).
- Localized name falls back correctly when a translation is missing.

## 12. Documentation impact

New `Docs/BusinessLogic/product-ingredients.md` and glossary entries, same commit as the code. Plus the
correction to `Docs/ai-harness/05-domain-gastronomy-guidelines.md:17` if Q9 confirms the rejection.

## 13. Deployment & rollout

No image, `appsettings`, or ECS change expected — unless Q10 makes Redis a prerequisite. Existing
products are unaffected until ingredients are attached, so rollout is immediate.

---

## Open questions — the batch awaiting answers

**Scope**

- **Q0 — Plugin name and `SystemName`.** Own plugin (`Nop.Plugin.Misc.Ingredients`), or folded into the
  `Nop.Plugin.Misc.GastronomyCompliance` already proposed in the domain guidelines? `SystemName` is what
  settings keys, permission names, locale prefixes and uninstall all hang off, so it must be frozen
  before any of them are minted.
- **QS — What is explicitly OUT of scope?** Each needs an in/out call, not a question mark: allergen
  severity data, quantities/percentages, descending-by-weight ordering, EU-labelling compliance,
  ingredient import/export, storefront filtering by ingredient.

**Data model**

- **Q1 — Own entity, or Product-as-ingredient?** Own entity keeps the catalogue clean; Product-as-
  ingredient reuses pictures, localization and admin UI for free but puts "beef bones" in the product
  table. Recommendation: own entity.
- **Q3 — Quantities?** "2 g salt" / "60% beef broth", or an ordered list of names only? EU labelling
  wants descending order by weight and a percentage for emphasised ingredients — a far bigger feature.
- **Q8 — Does `FilterLevelValue`'s fixed three-level hierarchy cover the real cases?** Most recipes may
  not nest deeper. If three levels suffice, a shipped mechanism with admin UI, permissions and storefront
  search already exists.
- **Q7 — Ingredient fields besides the name:** picture, description, origin/supplier, "may contain traces
  of"?

**Behaviour**

- **Q4 — How does the storefront show nesting?** Fully expanded inline (EU style: "beef broth (bones,
  water, carrot, celery, salt)"), collapsible tree, or flattened with duplicates merged? And which widget
  zone from `PublicWidgetZones.cs:159-173`?
- **Q5 — Admin placement:** dedicated Catalog page plus a product-edit tab, or product page only?
- **Q12 — Deleting an ingredient in use:** block with a message, or cascade? And what is the maximum
  nesting depth — a hard write-time limit, or only a render-time cut-off?

**Cross-cutting**

- **Q6 — Permissions:** one permission, or a View / CreateEditDelete pair per the `Catalog.*` convention?
  Which roles get it at install on an existing store — Administrators only, or Vendors too, and does a
  Vendor manage ingredients for their own products?
- **Q10 — Is Redis enabled for this deployment?** If not, is a stale ingredient tree on other instances
  until TTL acceptable, and what TTL — or does this feature make Redis a prerequisite?
- **Q11 — Write-path guarantees:** is cycle prevention allowed to be a best-effort read-then-write check
  or does it need a transaction/lock? And if composition or localized rows fail after the ingredient row
  committed, is the whole operation transactional or is a half-created ingredient acceptable?
- **Q13 — Multi-store:** is the ingredient catalogue shared across all stores, or store-mapped
  (`IStoreMappingSupported`)?
- **Q14 — Storefront visibility:** is the ingredient list public to guests, or does it respect the
  product's ACL / customer-role restrictions?
- **Q15 — Read cost:** is the tree rendered only on the product detail page, or also on listings and
  quick-view? Acceptable cold-cache cost — one recursive query per product, or per-level loading?

**Harness**

- **Q9 — Confirm the override of `05-domain-gastronomy-guidelines.md:17`,** which currently prescribes
  `GenericAttribute` / `SpecificationAttribute` for ingredient lists. If the rejection stands, that line
  is corrected in the same commit.
