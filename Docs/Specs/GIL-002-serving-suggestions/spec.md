---
id: GIL-002
kind: Task
title: Serving suggestion on products (title, description, image, steps)
status: Ready
---

# Task — Serving suggestion on products (title, description, image, steps)

> **Ready.** Confirmed by `spec-intake` (round 3) — all gaps from rounds 1-2 resolved. §4 leaves one
> mechanism-level fact (whether a plugin can add a genuine new `<nop-card>` section through
> `admin_product_details_block`, vs. only piggyback into an existing one) explicitly flagged for
> `ddd-modeler` to verify first. Next step: `plan-and-implement`.

## 1. Business goal & outcome

Every product in the catalogue (premium jarred/canned meals) can carry one **serving suggestion** —
guidance on how to serve/present the dish — shown to the customer on the product page.

Content agreed with the developer for v1: a **title**, a **description**, one **image**, and an ordered
list of **instruction steps** (a short "how to serve it" mini-recipe). Exactly **one** serving suggestion
per product, owned by that product — not a shared/reusable catalogue entry the way `Ingredient` (GIL-001)
is shared across products.

**Outcome:** an admin opens a product, writes its serving suggestion (title, description, image, ordered
steps), and the storefront renders it on that product's page.

**Resolved (round 2):**
- **Image is required** on the entity — the admin form must always be able to pick/replace a picture.
  Before go-live, products without a real photo yet get a placeholder image; that is an admin content
  task (§13), not a schema exception.
- **Steps are optional** — a serving suggestion may be title/description/image only, with zero steps.
- **Length limits** follow existing `Product` precedent: title bounded like `Product.Name`
  (`AsString(400)`, `ProductBuilder.cs:20`); description and step text unbounded, like
  `Product.ShortDescription`/`FullDescription`, which have no explicit column-length override in the
  same builder (`ProductBuilder.cs:20-27`).

## 2. Root cause / current behavior

N/A — new feature.

## 3. Placement — plugin or core?

**New plugin**, following the same shape as GIL-001. No core change: the data is entirely new and
storefront rendering goes through a widget zone.

Verified: `IMiscPlugin` (`src/Libraries/Nop.Services/Common/IMiscPlugin.cs:9`) and `IWidgetPlugin`
(`src/Libraries/Nop.Services/Cms/IWidgetPlugin.cs:8`) both derive from `IPlugin`. `PublicWidgetZones`
(`src/Presentation/Nop.Web.Framework/Infrastructure/PublicWidgetZones.cs:159-173`) exposes fifteen
`productdetails_*` zones already rendered in both product templates — no view fork needed.

**Proposed:** own plugin `Nop.Plugin.Misc.ServingSuggestions` (`SystemName` `Misc.ServingSuggestions`).
Distinct from `Nop.Plugin.Misc.Ingredients` (GIL-001) — different content, different lifecycle, and
GIL-001 already claims `productdetails_before_collateral` (see §6), so the two plugins do not collide on
the same zone.

## 4. Extension point

`IMiscPlugin` + `IWidgetPlugin`, same as GIL-001.

**Resolved (round 2):** no separate admin menu entry, no `AdminMenuCreatedEvent` consumer — a tab/section
on the product-edit page only. Unlike GIL-001 (a shared catalogue plus a product-edit tab), this entity
is one-per-product and non-shared, so no catalogue-style listing page is needed.

**Candidate mechanism, unverified — for `ddd-modeler` to confirm before relying on it:** the admin
widget zone `AdminWidgetZones.ProductDetailsBlock` (`"admin_product_details_block"`,
`src/Presentation/Nop.Web.Framework/Infrastructure/AdminWidgetZones.cs:197`), rendered inside
`<nop-cards id="product-cards">` on the product-edit page
(`src/Presentation/Nop.Web/Areas/Admin/Views/Product/_CreateOrUpdate.cshtml:114`) via the same
`IWidgetPlugin` pipeline as the storefront zones. The only in-repo precedent for this zone,
`Nop.Plugin.Tax.Avalara`'s `EntityUseCodeViewComponent`
(`src/Plugins/Nop.Plugin.Tax.Avalara/Components/EntityUseCodeViewComponent.cs:95`), injects one field
into an *existing* card via jQuery DOM manipulation
(`src/Plugins/Nop.Plugin.Tax.Avalara/Views/EntityUseCode/EntityUseCode.cshtml:10`) rather than adding a
new `<nop-card>` section of its own. Whether a plugin can register a genuine new collapsible card
section through this zone, or must instead piggyback on an existing one, is not demonstrated anywhere in
this codebase today — `ddd-modeler` verifies this before committing to it as the mechanism.

## 5. Data model & migration

New plugin-owned entities — schema migration, not `GenericAttribute`: the content is multi-field
(title, description, image, ordered steps), and `GenericAttribute` is a schema-free blob that cannot
express an ordered child collection (same reasoning GIL-001 used to reject it for ingredients).

Existing mechanisms considered and rejected:

| Mechanism | Why it does not fit |
|---|---|
| `ProductTag` | Flat label, no title/description/image/steps. |
| `SpecificationAttribute` | One grouping level, no ordered sub-content. |
| `GenericAttribute` | Schema-free; cannot hold an ordered step collection or a real `PictureId` FK cleanly. |
| `Ingredient` (GIL-001) | Different concept — composition of what's in the product, not how to serve it. |

**Resolved entities:**

- `ServingSuggestion` — `ProductId` (FK to `Product`, one row per product), `Title` (`AsString(400)`,
  not nullable), `Description` (unbounded, not nullable), `PictureId` (**non-nullable** `int`, same
  column shape as `Category.PictureId`, `src/Libraries/Nop.Core/Domain/Catalog/Category.cs:53` — but a
  different semantic: `Category.PictureId` uses `0` for "no picture", whereas here the field is
  required, so the value must always reference a real `Picture` row; enforced at the admin-validation
  layer, since this codebase does not declare DB-level FK constraints on these `int` columns anyway).
- `ServingSuggestionStep` — `ServingSuggestionId` (FK), `Text`, `DisplayOrder` (`int`) — ordering
  precedent: `ProductPicture.DisplayOrder` (`src/Libraries/Nop.Core/Domain/Catalog/ProductPicture.cs:21`).

`Title`, `Description`, and `ServingSuggestionStep.Text` are customer-facing and expected to be
`ILocalizedEntity`, mirroring `Product.Name`/`Product.ShortDescription`. Note from GIL-001 (§7): no
plugin under `src/Plugins/` currently declares `ILocalizedEntity` on its own entity — whichever of
GIL-001 or this spec ships first is the actual first instance in this repo, the other follows its
resolved pattern rather than re-deciding it.

Exact column types/nullability/defaults are for `ddd-modeler`.

## 6. Admin & storefront surface

**Resolved (round 2):**
- **Storefront:** rendered into widget zone `productdetails_bottom`
  (`PublicWidgetZones.cs:166`) — distinct from GIL-001's `productdetails_before_collateral` so the two
  features don't compete for the same slot.
- **Admin:** a tab on the product-edit page (create/edit/delete the one serving suggestion for that
  product, reorder its steps). No separate catalogue-style admin page (§4).

Visibility: inherits the product's own `IAclSupported`/`Published` restrictions, same as GIL-001 §6
Q14 — no independent visibility rule.

Store-mapping: inherits the product's own store mapping — no independent `IStoreMappingSupported` on
`ServingSuggestion`. Unlike GIL-001's shared, independently-listed `Ingredient` (where store-scoping the
catalogue itself is a deferred future concern, §5), `ServingSuggestion` is a one-row-per-product child of
`Product`, so it has no existence — and so no visibility question — independent of the product it
belongs to.

Read scope: product detail page only in v1, not listing or quick-view — same as GIL-001 §6 Q15.

## 7. Settings, permissions, localization

**Permissions.** Following the Catalog View/CreateEditDelete convention
(`src/Libraries/Nop.Services/Security/StandardPermission.cs:54-55` shows the `Products` pair as
precedent). **Resolved (round 2):** a single View/CreateEditDelete pair, Administrators only — same posture as
GIL-001 §7 Q6 (no vendor-scoped ownership concept for this feature either).

**Localization.** Same mechanism as GIL-001 §7: `ILocalizedEntity` marker
(`src/Libraries/Nop.Core/Domain/Localization/ILocalizedEntity.cs:6`), values as `LocalizedProperty` rows,
read via `ILocalizationService.GetLocalizedAsync<TEntity,TPropType>`. On uninstall, `LocalizedProperty`
rows for `LocaleKeyGroup = "ServingSuggestion"` / `"ServingSuggestionStep"` are deleted along with the
entity rows.

**Settings.** None proposed for v1 — presentation and admin placement are fixed, not store-configurable,
same as GIL-001.

## 8. Events & scheduled tasks

Built-in `EntityInserted/Updated/DeletedEvent<T>` cover change notification. No custom event, no
scheduled task.

## 9. Caching

No derived/transitive read-time artifact (unlike GIL-001's composition closure) — a serving suggestion
is a flat title/description/image/ordered-steps read for one product, no traversal. An optional
in-process render cache may be layered on later purely for performance; not required for correctness.
Multi-instance: `DistributedCacheConfig.Enabled` defaults to `false`
(`src/Libraries/Nop.Core/Configuration/DistributedCacheConfig.cs:20`) — no deployment yet, only
localhost, so this is not a prerequisite.

## 10. Failure scenarios

**External dependencies:** N/A — no external dependency involved; only the database, as with any other
nopCommerce write.

- **Product deleted while it has a serving suggestion.** Resolved: cascade-delete the
  `ServingSuggestion`/`ServingSuggestionStep`/`LocalizedProperty` rows with it, **and** its `Picture` via
  `IPictureService` — unlike GIL-001's shared `Ingredient`, this content has no other owner and nothing
  else references it, so there is no "still in use elsewhere" case to block on. Same cleanup on plugin
  uninstall.
- **Partial write.** Entity row, step rows, and `LocalizedProperty` rows are multiple writes — resolved:
  transactional, same posture as GIL-001 §10.
- **Concurrent edit.** Two admins editing the same product's serving suggestion at once. Resolved:
  last-write-wins, no optimistic concurrency check added — consistent with the rest of the codebase's
  admin-edit posture, and the developer notes only one admin account exists in practice today.
- **Replacing an existing serving suggestion's image** (not product deletion — an ordinary edit).
  Resolved: delete the previous `Picture` row via `IPictureService`, same pattern as
  `CategoryController.cs:294-299` (`if (prevPictureId > 0 && prevPictureId != category.PictureId) ...
  DeletePictureAsync`). Required-image (§1) makes this the common case, not an edge case — leaving the
  old picture orphaned would accumulate unused rows on every re-photograph.

## 11. Test scenarios

- A product with a serving suggestion (title, description, image, steps) renders it on the product page
  in step order.
- A product with no serving suggestion renders nothing extra (no error, no empty section).
- Deleting the product removes its serving suggestion and steps.
- Localized title/description/step text falls back correctly when a translation is missing.
- Creating a serving suggestion without an image is rejected by admin-form validation (image required,
  §1).
- Creating a serving suggestion with zero steps succeeds (steps optional, §1).
- Deleting a product removes its serving suggestion's `Picture` row (`IPictureService`), not just the
  entity rows.
- Uninstalling the plugin removes `ServingSuggestion`/`ServingSuggestionStep`/`LocalizedProperty`/
  `Picture` rows for every product, not just the plugin's own settings/locale resources.
- Replacing an existing serving suggestion's image deletes the previous `Picture` row and keeps only the
  new one.

## 12. Documentation impact

New `Docs/BusinessLogic/product-serving-suggestions.md`, same commit as the code. **Resolved (round
2):** yes, add a **Serving suggestion** entry to `Docs/Glossary/shop.md` ("What we sell") — the term
collides with the food-labelling sense of "sposób podania"/serving suggestion on packaging, distinct
from this feature's structured entity (title/description/image/ordered steps).

## 13. Deployment & rollout

No image, `appsettings`, or ECS change expected. No Redis prerequisite (§9). Existing products are
unaffected until a serving suggestion is added to them, so rollout is immediate. Because the image is
required, any product an admin starts a serving suggestion for needs a real photo (or an interim
placeholder) before that suggestion can be saved — a content task, not a migration/deployment concern.
