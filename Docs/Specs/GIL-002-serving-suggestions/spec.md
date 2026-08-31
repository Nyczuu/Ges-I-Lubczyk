---
id: GIL-002
kind: Task
title: Serving suggestion on products (title, description, image, steps)
status: In Progress
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

## Technical design (ddd-modeler)

> This design was produced in two passes. The first pass ran before GIL-001 (`Nop.Plugin.Misc.Ingredients`)
> was merged into `develop`, so several claims about "no plugin precedent exists yet" were provisional. After
> GIL-001 was merged locally into `develop` mid-review, `ddd-modeler` was re-invoked to reconcile the design
> against the real, now-merged GIL-001 code. Both passes are kept below — the second corrects specific claims
> in the first; the first pass's uncorrected sections still stand.

### Pass 1 — initial design

## Corrections to the spec's technical assumptions

- **§10's "cascade-delete" wording is ambiguous and, read as DB-level FK cascade, would not work.** `Product` implements `ISoftDeletedEntity` (`src/Libraries/Nop.Core/Domain/Catalog/Product.cs:13`), and `ProductController.Delete` (`src/Presentation/Nop.Web/Areas/Admin/Controllers/ProductController.cs:1465`) calls `ProductService.DeleteProductAsync` → `_productRepository.DeleteAsync(product)`, which for `ISoftDeletedEntity` sets `Deleted = true` and issues an `UPDATE`, never a `DELETE` (`src/Libraries/Nop.Data/EntityRepository.cs:439-441`). A `Product` row is **never actually removed**, so a DB `ON DELETE CASCADE` FK from `ServingSuggestion.ProductId → Product.Id` would never fire. The cleanup this spec wants must be an **application-level event consumer**, not schema cascade. **(Superseded by Pass 2, point 3 — see below: a single consumer suffices, not the dual-consumer pattern originally proposed here.)**

- **§5 cites `Category.PictureId` as the shape precedent, but that's the wrong precedent for a *required* picture, and the accompanying claim "this codebase does not declare DB-level FK constraints on these int columns anyway" is false as a general statement.** `CategoryBuilder.cs` indeed leaves `PictureId` unmapped (no FK) — but that's specifically because `Category.PictureId` uses `0` as a "no picture" sentinel, and a real FK would reject `0`. `ProductPictureBuilder.cs:22` — a column that, like ours, is *always* required — **does** declare `.WithColumn(nameof(ProductPicture.PictureId)).AsInt32().ForeignKey<Picture>()`. Since `ServingSuggestion.PictureId` is required and never a sentinel, `ProductPicture.PictureId` is the correct precedent, not `Category.PictureId`.

- **§5's "no explicit column-length override" framing for `Description`/`Step.Text`, taken literally, would make them nullable, contradicting "not nullable."** The FluentMigrator auto-map convention for any `string` property left out of a builder's `MapEntity` is `AsString(int.MaxValue).Nullable()` (`src/Libraries/Nop.Data/Extensions/FluentMigratorExtensions.cs:32`) — that's exactly why `Product.ShortDescription`/`FullDescription` (also unmapped) are nullable in the DB. To get unbounded **and** `NotNullable`, the builder must explicitly declare `.AsString(int.MaxValue).NotNullable()` — the pattern `Nop.Plugin.Misc.Polls`'s `PollBuilder`/`PollAnswerBuilder` already use for their unbounded, required `Name` columns.

- **The claim that GIL-001 "already shipped" and could be mirrored was false at the time of Pass 1.** At that point `Docs/Specs/GIL-001-product-ingredients/spec.md` was status `Ready` with no implementation on disk. **Superseded by Pass 2, point 1 — GIL-001 has since been merged, and its `Ingredient` entity is the actual first `ILocalizedEntity` plugin instance; GIL-002 follows it, not the reverse.**

## Resolution of §4's flagged open question

**Confirmed: a plugin can register a genuine new `<nop-card>` section through `AdminWidgetZones.ProductDetailsBlock`.** Traced the full render chain:

1. `_CreateOrUpdate.cshtml:114` calls `@await Component.InvokeAsync(typeof(AdminWidgetViewComponent), new { widgetZone = AdminWidgetZones.ProductDetailsBlock, additionalData = Model })` **directly inside** `<nop-cards id="product-cards">` (line 86), as a **sibling** of the built-in `<nop-card>` elements — not nested inside one.
2. `AdminWidgetViewComponent.InvokeAsync` (`src/Presentation/Nop.Web/Areas/Admin/Components/AdminWidgetViewComponent.cs:38`) calls `IWidgetModelFactory.PrepareRenderWidgetModelAsync` and renders `Areas/Admin/Views/Shared/Components/AdminWidget/Default.cshtml`, which just loops active widgets: `@await Component.InvokeAsync(widget.WidgetViewComponent, widget.WidgetViewComponentArguments)`.
3. `NopCardTagHelper` (`src/Presentation/Nop.Web.Framework/TagHelpers/Admin/NopCardTagHelper.cs:15`) is matched by **element name across any compiled Razor view**, not scoped to `_CreateOrUpdate.cshtml`. It fully replaces `<nop-card asp-name="..." ...>...</nop-card>` with a real `<div class="card card-secondary card-outline">` including header, icon, collapse toggle, and the `AdminWidgetZones.CardBefore/CardAfter` per-card sub-zones (lines 66-137) — indistinguishable from a built-in card.
4. Every plugin admin view (confirmed via `Nop.Plugin.Misc.Polls/Admin/Views/_ViewImports.cshtml:3`) does `@addTagHelper *, Nop.Web.Framework`, which registers this same tag helper for the plugin's own views. So if our plugin's widget-zone view component's view emits `<nop-card asp-name="serving-suggestion" asp-title="..." asp-hide-block-attribute-name="..." asp-hide="..." asp-advanced="false">...</nop-card>`, it renders as a genuine, fully-functional, collapsible card in the product-edit page — same JS (`admin.common.js:260-268`, generic `data-card-widget="collapse"` + `data-hideAttribute` → `admin/preferences/savepreference`) drives it, with no special-casing.

At Pass 1 time, no plugin in this repo demonstrated this end-to-end. **Superseded by Pass 2, point 2 — GIL-001's `ProductIngredients.cshtml` already does exactly this, live, in `develop`.**

## Placement

New plugin `Nop.Plugin.Misc.ServingSuggestions`, `SystemName` = `Misc.ServingSuggestions`. Implements `IMiscPlugin` (`src/Libraries/Nop.Services/Common/IMiscPlugin.cs:9`) + `IWidgetPlugin` (`src/Libraries/Nop.Services/Cms/IWidgetPlugin.cs:8`) on one `BasePlugin` subclass, mirroring `PollsPlugin`/`AvalaraTaxPlugin`. No core change. `GetWidgetZonesAsync()` returns both `AdminWidgetZones.ProductDetailsBlock` and `PublicWidgetZones.ProductDetailsBottom` (both admin and public zones legitimately coexist in one list — confirmed by `AvalaraTaxProvider.GetWidgetZonesAsync` mixing `AdminWidgetZones.*` and other zones together, and now also by `IngredientsPlugin.GetWidgetZonesAsync`). `GetWidgetViewComponent(widgetZone)` switches between an admin card view component and a storefront view component.

## Domain model

```csharp
// src/Plugins/Nop.Plugin.Misc.ServingSuggestions/Domain/ServingSuggestion.cs
public partial class ServingSuggestion : BaseEntity, ILocalizedEntity
{
    public int ProductId { get; set; }
    public string Title { get; set; }          // localized
    public string Description { get; set; }     // localized
    public int PictureId { get; set; }           // required — no sentinel value
}

// src/Plugins/Nop.Plugin.Misc.ServingSuggestions/Domain/ServingSuggestionStep.cs
public partial class ServingSuggestionStep : BaseEntity, ILocalizedEntity
{
    public int ServingSuggestionId { get; set; }
    public string Text { get; set; }             // localized
    public int DisplayOrder { get; set; }
}
```

No navigation properties (rule 1). Invariant "exactly one `ServingSuggestion` per product" is **not** a DB unique constraint — confirmed no builder in `src/Libraries/Nop.Data/Mapping/Builders/**` calls `.Unique()` anywhere; the codebase's convention (also spec's own posture on FK/lock enforcement) is service-layer enforcement: `ServingSuggestionService` exposes get-or-create semantics keyed by `ProductId`, never a bare "insert new" the admin UI could call twice.

**`NopEntityBuilder<T>` mapping** (see Pass 2, point 1 for the corrected `ForeignKey<T>()` wiring on both builders):

```csharp
// ServingSuggestionBuilder
table
    .WithColumn(nameof(ServingSuggestion.Title)).AsString(400).NotNullable()          // mirrors Product.Name, ProductBuilder.cs:20
    .WithColumn(nameof(ServingSuggestion.Description)).AsString(int.MaxValue).NotNullable()
    .WithColumn(nameof(ServingSuggestion.PictureId)).AsInt32().ForeignKey<Picture>()   // mirrors ProductPicture.PictureId, not Category.PictureId
    .WithColumn(nameof(ServingSuggestion.ProductId)).AsInt32().ForeignKey<Product>();  // Rule.Cascade default; decorative only — see Pass 2 point 3

// ServingSuggestionStepBuilder
table
    .WithColumn(nameof(ServingSuggestionStep.Text)).AsString(int.MaxValue).NotNullable()
    .WithColumn(nameof(ServingSuggestionStep.ServingSuggestionId)).AsInt32().ForeignKey<ServingSuggestion>(); // real DB cascade — ServingSuggestion itself is hard-deleted
```

`Id` PK auto-added by `RetrieveTableExpressions` (`FluentMigratorExtensions.cs:248-271`). `.ForeignKey<T>()` auto-indexes the FK column (`FluentMigratorExtensions.cs:114`) and defaults `OnDelete(Rule.Cascade)`.

**Important consequence of `ServingSuggestion.PictureId → Picture` cascade:** `Picture` is **not** `ISoftDeletedEntity` (`src/Libraries/Nop.Core/Domain/Media/Picture.cs:6`) — `IPictureService.DeletePictureAsync` does a genuine `DELETE` (`PictureService.cs:707`). If the service ever deletes the `Picture` *before* the `ServingSuggestion` row, the DB cascade would silently delete the `ServingSuggestion`/`ServingSuggestionStep` rows as a side effect, ahead of the explicit application code. The service must always delete `Picture` **last** (matching `CategoryController.cs:294-299`'s "delete old picture after everything else" ordering).

## Extension decision

Schema migration — new plugin-owned entities, not `GenericAttribute`. Reasoning matches spec §5 exactly and is verified: `GenericAttribute` cannot express an ordered child collection (`ServingSuggestionStep`) or a real `PictureId` FK. `ProductTag`/`SpecificationAttribute` rejected for the same reasons the spec gives — confirmed by reading `ProductTag.cs`/`SpecificationAttribute.cs` shapes, neither has ordered sub-content.

## Design

**Migration** (`Data/Migrations/SchemaMigration.cs`, mirroring `Nop.Plugin.Misc.Polls/Data/Migrations/SchemaMigration.cs`):

```csharp
[NopMigration("2026-08-31 00:00:00", "Misc.ServingSuggestions schema", MigrationProcessType.Installation)]
public class SchemaMigration : Migration
{
    public override void Up()
    {
        this.CreateTableIfNotExists<ServingSuggestion>();
        this.CreateTableIfNotExists<ServingSuggestionStep>();
    }
    public override void Down()
    {
        this.DeleteTableIfExists<ServingSuggestionStep>();
        this.DeleteTableIfExists<ServingSuggestion>();
    }
}
```

Safe on a populated store: purely additive new tables, no existing schema touched. Safe under rolling deploy: no existing table altered.

**Service** — `IServingSuggestionService` / `ServingSuggestionService` (own repositories `IRepository<ServingSuggestion>`, `IRepository<ServingSuggestionStep>`, `IRepository<LocalizedProperty>`, `IPictureService`, `INopDataProvider`):
- `GetServingSuggestionByProductIdAsync(int productId)`
- `GetServingSuggestionStepsAsync(int servingSuggestionId)`
- `InsertServingSuggestionAsync` / `UpdateServingSuggestionAsync` — wraps entity insert/update + step writes + `LocalizedProperty` writes in `_dataProvider.CreateTransactionScope()` + `transaction.Complete()` (verified pattern: `src/Libraries/Nop.Services/Helpers/SyncCodeHelper.cs:103-105`, and `EntityRepository.cs:467-478`), satisfying §10's transactional requirement.
- `DeleteServingSuggestionAsync(ServingSuggestion)` — single method used both by the admin "delete" action and the product-deletion event consumer: deletes `LocalizedProperty` rows for both `LocaleKeyGroup`s (queried via `IRepository<LocalizedProperty>.GetAllAsync(q => q.Where(lp => lp.EntityId == id && lp.LocaleKeyGroup == "ServingSuggestion"/"ServingSuggestionStep"))` — no bulk-delete-by-entity helper exists on `ILocalizedEntityService`, confirmed by reading its full interface; this direct-repository pattern matches how `ProductService.cs:924-1032` itself queries `LocalizedProperty` by `LocaleKeyGroup == nameof(Product)`), then the `ServingSuggestion` row (DB cascades `ServingSuggestionStep` rows automatically), then `IPictureService.DeletePictureAsync` **last**.
- No optimistic concurrency token — matches spec's explicit last-write-wins decision and the codebase's general posture (no `RowVersion`/`ConcurrencyStamp` field anywhere in these builders).

**Events consumed** — see Pass 2, point 3 for the corrected (single-consumer) design; the dual-consumer version originally proposed here is superseded.

**Events published** — none beyond built-in `EntityInserted/Updated/DeletedEvent<ServingSuggestion>`/`<ServingSuggestionStep>` (fired automatically by `EntityRepository<T>` on every insert/update/delete — no custom event needed, matching spec §8).

**Caching** — see Pass 2, point 5 for a required addition (`CacheEventConsumer<ServingSuggestion>`); the "no caching needed" conclusion for a bespoke derived/render cache still stands unchanged.

**Permissions** — `IPermissionConfigManager` implementation (auto-discovered via `ITypeFinder.FindClassesOfType<IPermissionConfigManager>()` + `Activator.CreateInstance`, confirmed at `PermissionService.cs:403-409` — **no DI registration needed**, confirmed by both `Nop.Plugin.Misc.Polls`'s `PollPermissionConfigManager` and GIL-001's `IngredientsPermissionConfigManager` not appearing in their plugins' `NopStartup.cs`):

```csharp
public class ServingSuggestionsPermissionConfigManager : IPermissionConfigManager
{
    public IList<PermissionConfig> AllConfigs => new List<PermissionConfig>
    {
        new("Admin area. Serving suggestions. View", ServingSuggestionsDefaults.Permissions.VIEW,
            nameof(StandardPermission.Catalog), NopCustomerDefaults.AdministratorsRoleName),
        new("Admin area. Serving suggestions. Create, edit, delete", ServingSuggestionsDefaults.Permissions.CREATE_EDIT_DELETE,
            nameof(StandardPermission.Catalog), NopCustomerDefaults.AdministratorsRoleName),
    };
}
```

`SystemName`s (`"ServingSuggestions.View"` / `"ServingSuggestions.CreateEditDelete"`) are plugin-owned constants in `ServingSuggestionsDefaults.Permissions`, **not** added into core `StandardPermission.cs` — the "Catalog View/CreateEditDelete convention" the spec asks for is the *naming shape*, borrowed via `Category = nameof(StandardPermission.Catalog)` purely for admin-UI grouping. Adding literal constants to `StandardPermission.cs` would be a core change requiring the rule-3 confirmation this design avoids.

**Admin surface** — a genuine new `<nop-card>` rendered via `AdminWidgetZones.ProductDetailsBlock` (confirmed live in Pass 2, point 2), backed by its own admin controller `ServingSuggestionController` (own AJAX actions scoped by `productId`, `[CheckPermission(...)]`-guarded) — **not** fields folded into the main `ProductModel`/`product-form` POST, because `Edit.cshtml:12`'s `<form>` binds strictly to `ProductModel`, which has no room for plugin fields without a core change. This mirrors the established pattern of `ProductController.ProductPictureAdd/Update/Delete` (`ProductController.cs:2118,2199,2233`) and GIL-001's `ProductIngredientsAdminViewComponent`/`IngredientsAdminController`/`ProductIngredients.cshtml` (DataTables grid + inline edit + AJAX add, hide-block `GenericAttribute` for collapse-state) for the ordered steps sub-list, and `ProductController.ProductPictureAdd`'s `IPictureService.InsertPictureAsync(IFormFile)` for the single required picture. No separate admin menu entry (spec §4, confirmed no `AdminMenuCreatedEvent` consumer needed).

**Storefront surface** — a `ServingSuggestionViewComponent` registered for `PublicWidgetZones.ProductDetailsBottom` (`"productdetails_bottom"`, confirmed `PublicWidgetZones.cs:166`), reading via `ServingSuggestionService.GetServingSuggestionByProductIdAsync` + `ILocalizationService.GetLocalizedAsync`. Renders nothing if no serving suggestion exists (no error, no empty markup — spec test scenario). Inherits product's own ACL/store-mapping/Published gating — no independent check needed since it only renders inside the already-gated product-details template.

**Localization** — `Title`/`Description`/`ServingSuggestionStep.Text` via `ILocalizedEntityService.SaveLocalizedValueAsync<T,TPropType>`, `LocaleKeyGroup` = unqualified type name (`"ServingSuggestion"`/`"ServingSuggestionStep"`, confirmed convention at `LocalizationService.cs:506`; confirmed no name collision anywhere in `src/Libraries`). On plugin uninstall and on product deletion, `LocalizedProperty` rows are deleted explicitly (no automatic mechanism exists — see Service above; uninstall ordering corrected in Pass 2, point 6).

## Simplicity check

The smallest version that works is what's described: two plugin-owned tables (no soft-delete, no store-mapping, no ACL, no settings, no scheduled task, no optimistic lock), one service, one event consumer, one admin card, one storefront widget. This design matches that size. The one piece that looks larger than "an additive nullable property" is the two-entity schema + its own admin controller — that's the named, justified constraint from §5 (an ordered child collection and a required picture FK cannot live in `GenericAttribute`), not scope creep.

## Blast radius

- **`AdminWidgetZones.ProductDetailsBlock`** — at Pass 1 time, believed rendered only by `Nop.Plugin.Tax.Avalara`'s `EntityUseCodeViewComponent` (field injection). **Updated (Pass 2):** GIL-001's `IngredientsPlugin` also renders a genuine `<nop-card>` into this same zone — so this design's new card will be the *third* widget sharing the zone, alongside Avalara's field-injection and GIL-001's ingredients card. Additive — `Default.cshtml`'s `foreach` already supports multiple widgets rendering into the same zone; none interact.
- **`PublicWidgetZones.ProductDetailsBottom`** — not currently claimed by any plugin in this repo (confirmed no other plugin returns it from `GetWidgetZonesAsync`, including now-merged GIL-001, which claims `productdetails_before_collateral` per spec §3) — no collision.
- **`LocaleKeyGroup` string space** — `"ServingSuggestion"`/`"ServingSuggestionStep"` are new, unused strings; no collision with core or GIL-001's `"Ingredient"`.
- **`EntityDeletedEvent<Product>`** — already consumed elsewhere in the repo by `Nop.Plugin.Misc.Zettle`'s `EventConsumer`. GIL-001 does **not** consume it (see Pass 2, point 3 — a real gap in GIL-001, out of scope for this task to fix). `IEventPublisher` invokes all registered consumers for a given event type independently and synchronously; our consumer doesn't touch product data itself. A throw in *any* consumer (including ours) will propagate up through `ProductService.DeleteProductAsync`'s synchronous call chain and abort the whole delete for the admin user, same as it already does for Zettle's consumer today.

## Installed-store impact

New tables only — no existing table altered, no existing settings/permissions changed. Rollout is immediate: existing products render nothing extra until an admin adds a serving suggestion (spec §13). New permissions (`ServingSuggestions.View`/`CreateEditDelete`) are synced automatically via `PermissionService`'s `IPermissionConfigManager` scan (no explicit call needed in `InstallAsync`, confirmed neither `PollsPlugin.InstallAsync` nor GIL-001's `IngredientsPlugin.InstallAsync` calls it either) and granted to Administrators only — no vendor/other-role impact. Rolling deploy: the migration only creates tables, so old and new app instances can run concurrently against the same DB without either breaking (old instances simply never query the new tables).

### Pass 2 — reconciliation against merged GIL-001

Between Pass 1 and developer approval, GIL-001 (`Nop.Plugin.Misc.Ingredients`) was merged locally into `develop`. `ddd-modeler` was re-invoked to re-verify Pass 1's claims against the real, now-available GIL-001 code rather than trust its own earlier inferences. The corrections below are authoritative over the equivalent Pass 1 sections; Pass 1 text not mentioned here still stands.

**1. `ILocalizedEntity` precedent + builder conventions — correction (framing), column shape confirmed.**
`Domain/Ingredient.cs:9` does declare `ILocalizedEntity`. Pass 1's claim "no plugin declares `ILocalizedEntity`, this design is first" is false — **GIL-001 is the first instance, GIL-002 follows it**, not the reverse.
Column mapping corroborates the planned shape: `IngredientBuilder.cs:18` — `Name` → `.AsString(400).NotNullable()`, exactly mirroring `Product.Name`. `Ingredient.Description` is absent from `IngredientBuilder.MapEntity` entirely (unbounded, nullable by default) — direct corroboration that `Product`-style unmapped-string convention is real, not inferred. Note: the spec's own §5 wants `ServingSuggestion.Description` **not nullable**, diverging from both `Product` and `Ingredient`'s nullable-by-default columns — not a blocking issue, just declare it explicitly as `.AsString(int.MaxValue).NotNullable()`.
`IngredientCompositionBuilder.cs` is empty because both its FK columns target the *same* table (`Ingredient`) and `ForeignKey<TPrimary>` has no constraint-name parameter to disambiguate two FKs to one table — **does not apply to GIL-002**, since `ServingSuggestion.ProductId → Product` and `ServingSuggestionStep.ServingSuggestionId → ServingSuggestion` target different tables. Both builders should declare real `ForeignKey<T>()` calls per `ProductIngredientMappingBuilder.cs:22-23`'s pattern — default `Rule.Cascade` is fine for both (no override needed, unlike `ProductIngredientMappingBuilder`'s deliberate `Rule.None` on `IngredientId`, which exists only because ingredient deletion is app-guarded — nothing analogous applies here).

**2. Admin `<nop-card>` mechanism — confirmed live, caveat now obsolete.**
Real, working, end-to-end precedent traced: `IngredientsPlugin.cs:71,89` routes `AdminWidgetZones.ProductDetailsBlock` to `ProductIngredientsAdminViewComponent`; the component (`:46-63`) casts `additionalData` to `BaseNopEntityModel` for the product id, gates on `_permissionService.AuthorizeAsync(...VIEW)` (silent `Content(string.Empty)` if denied), renders `ProductIngredients.cshtml`, which emits a genuine new `<nop-card asp-name="product-ingredients" asp-icon="..." asp-title="..." asp-hide-block-attribute-name="@hideIngredientsBlockAttributeName" asp-hide="@hideIngredientsBlock">` — not DOM injection. Collapse-state persistence is a per-admin `GenericAttribute` (`"ProductPage.HideIngredientsBlock"`), handled by the tag helper itself, no controller action needed. Controller shape (`IngredientsAdminController.cs:18-22`): `[Area(AreaNames.ADMIN)] [AutoValidateAntiforgeryToken] [ValidateIpAddress] [AuthorizeAdmin] [SaveSelectedTab]` at class level, `[CheckPermission(...VIEW)]` on reads, `[CheckPermission(...CREATE_EDIT_DELETE)]` on writes.
**Design update:** mirror this exact shape for `ServingSuggestionAdminViewComponent`/`ServingSuggestionController` — no independent verification exercise or fallback plan needed.

**3. Event consumer for Product deletion — correction: drop the dual-consumer design.**
GIL-001 has **zero** product-deletion consumers (grepped the whole plugin tree for `IConsumer<EntityDeletedEvent<Product>>`/`IConsumer<EntityUpdatedEvent<Product>>`/`ISoftDeletedEntity` — no matches). `ProductIngredientMapping` rows are simply left orphaned on product soft-delete — a real, silent gap in GIL-001, out of scope to fix here, but instructive: `ProductIngredientMappingBuilder.cs:22` maps `ProductId` as `ForeignKey<Product>()` with default `Rule.Cascade`, but since `Product` is `ISoftDeletedEntity` and `EntityRepository.DeleteAsync` only ever issues an `UPDATE` for soft-deleted entities (never physical `DELETE`), that Cascade rule structurally never fires — it's decorative. Crucially, `EntityRepository.DeleteAsync` **does** always call `_eventPublisher.EntityDeletedAsync(entity)` even for soft-deleted entities (`EntityRepository.cs:450-451,481-486`) — so `EntityDeletedEvent<Product>` reliably fires on every real product deletion, and a second consumer watching `EntityUpdatedEvent<Product>` for `.Deleted` flipping true (the pattern originally borrowed from `Zettle`) is solving a case with no code path in this repo.
**Corrected design:** a single consumer is sufficient and correct:
```csharp
public class ServingSuggestionProductDeletedEventConsumer : IConsumer<EntityDeletedEvent<Product>>
{
    public async Task HandleEventAsync(EntityDeletedEvent<Product> eventMessage)
    {
        // transactional: delete ServingSuggestionStep rows, the ServingSuggestion row,
        // its LocalizedProperty rows, and its Picture via IPictureService
    }
}
```
Drop the `EntityUpdatedEvent<Product>` consumer entirely. State explicitly that `ForeignKey<Product>()` Cascade on `ServingSuggestion.ProductId` is defense-in-depth decoration only, not the actual cleanup mechanism — the consumer is load-bearing because the DB-level cascade will not fire on the normal soft-delete path.

**4. `IngredientsPermissionConfigManager` shape — confirmed, no change.**
`Services/IngredientsPermissionConfigManager.cs:9-21` matches Pass 1's proposal exactly: plugin-owned `const string` names, `Category = nameof(StandardPermission.Catalog)`, Administrators-only, auto-discovered (not registered in `NopStartup.cs`). `ServingSuggestionsPermissionConfigManager` follows this shape verbatim, no change.

**5. Caching — "no caching needed" confirmed, one required addition surfaces.**
`IngredientCacheEventConsumer.cs:9` is `CacheEventConsumer<Ingredient>` — the framework's generic by-id/by-ids/all-prefix cache invalidator, required boilerplate that pairs with any cached `GetByIdAsync(id, cache => default)` call (`IngredientService.cs:73` uses this pattern, and `EntityRepository.GetByIdAsync` treats any non-null `getCacheKey` delegate, even one returning `default`, as "use the static cache manager"). This confirms the rejection of a bespoke closure-style cache (`ServingSuggestion` is flat, no traversal — unchanged) but surfaces a small necessary addition: **if** the service uses the same cached `GetByIdAsync` pattern (likely, to mirror `IngredientService`), then bare `CacheEventConsumer<ServingSuggestion>` (and possibly `<ServingSuggestionStep>`) is mandatory standard-invalidation boilerplate, not optional future polish — orthogonal to and not contradicting "no derived render cache needed."

**6. Uninstall cleanup ordering — confirmed pattern, one gap has no GIL-001 precedent (Picture).**
`IngredientsPlugin.UninstallAsync` (`:163-195`) removes the widget system name, deletes permission records by system name, deletes locale resources by prefix, and purges `LocalizedProperty` rows via `_localizedPropertyRepository.DeleteAsync(property => property.LocaleKeyGroup == nameof(Ingredient))` — the one step its own code comment calls out as necessary because nothing automatic covers it. It does **not** manually delete its own entity rows (`Ingredient`/`IngredientComposition`/etc.), because `PluginService.UninstallPluginsAsync` (`PluginService.cs:577,581`) calls `UninstallAsync()` **then** runs `SchemaMigration.Down()`, which drops the plugin's own tables automatically — only shared core tables need explicit purging. This confirms the general shape of the design's uninstall cleanup — no change there.
The one piece with **no GIL-001 precedent** (Ingredient has no `Picture` field): spec §11 requires "Uninstalling the plugin removes ... `Picture` rows for every product." `Picture` is a shared core table, untouched by `SchemaMigration.Down()`. Since `Down()` runs *after* `UninstallAsync()` and will drop the `ServingSuggestion` table (taking every `PictureId` value with it), the design must enumerate all `ServingSuggestion` rows and call `IPictureService.DeletePictureAsync(pictureId)` for each **inside `UninstallAsync()` itself**, before it returns control to `PluginService` for the `Down()` migration — this cannot be deferred, since the source table and its `PictureId` column will no longer exist by then.

**Approved by:** Mateusz Nycz
**Date:** 2026-08-31
**Revision notes:** Two ddd-modeler passes. Pass 1 approved conditionally pending reconciliation against GIL-001, which was merged into `develop` mid-review (developer request: merge GIL-001 implementation branch locally before continuing GIL-002). Pass 2 re-verified Pass 1's claims against the real GIL-001 code and corrected: the ILocalizedEntity-precedent framing (GIL-001 is first, not GIL-002), confirmed the admin `<nop-card>` mechanism as live (no longer unverified), dropped the dual-event-consumer design in favor of a single `EntityDeletedEvent<Product>` consumer, added required `CacheEventConsumer<ServingSuggestion>` boilerplate, and added an explicit Picture-cleanup ordering requirement in `UninstallAsync`. Final design (Pass 1 + Pass 2 corrections) approved as a whole.

## Implementation plan (implementation-planner)

File-by-file plan for a new plugin `Nop.Plugin.Misc.ServingSuggestions`, mirroring `src/Plugins/Nop.Plugin.Misc.Ingredients` (GIL-001) as the closest analogous sibling plugin wherever a precedent exists there, and other in-repo precedents (`ProductController.ProductPictureAdd/Update/Delete`, `CategoryController.cs:294-299`, `SpecificationAttributeController.OptionCreatePopup/OptionEditPopup`) where it does not.

### Root

**`ServingSuggestionsPlugin.cs`** — new, mirrors `IngredientsPlugin.cs`
```csharp
public class ServingSuggestionsPlugin : BasePlugin, IMiscPlugin, IWidgetPlugin
{
    protected readonly ILocalizationService _localizationService;
    protected readonly IPermissionService _permissionService;
    protected readonly IPictureService _pictureService;
    protected readonly IRepository<LocalizedProperty> _localizedPropertyRepository;
    protected readonly IRepository<ServingSuggestion> _servingSuggestionRepository;
    protected readonly ISettingService _settingService;
    protected readonly WidgetSettings _widgetSettings;

    public Type GetWidgetViewComponent(string widgetZone); // ProductDetailsBlock -> ServingSuggestionAdminViewComponent, else ServingSuggestionViewComponent
    public Task<IList<string>> GetWidgetZonesAsync(); // { PublicWidgetZones.ProductDetailsBottom, AdminWidgetZones.ProductDetailsBlock }
    public override async Task InstallAsync();
    public override async Task UninstallAsync();
    public bool HideInWidgetList => true;
}
```
No `GetConfigurationPageUrl()` override — no admin menu entry, no catalog list page (spec §4). Deliberate deviation from Ingredients — drop `RouteProvider.cs` entirely.

`UninstallAsync` ordering (load-bearing — `Down()` drops the table after this runs, taking `PictureId` with it):
1. Enumerate all `ServingSuggestion` rows, `IPictureService.DeletePictureAsync` each — must happen here.
2. Widget system-name removal.
3. Permission record removal.
4. `DeleteLocaleResourcesAsync("Plugins.Misc.ServingSuggestions")`.
5. Delete `LocalizedProperty` rows for both `LocaleKeyGroup`s.
6. `base.UninstallAsync()`.

**`ServingSuggestionsDefaults.cs`** — new
```csharp
public class ServingSuggestionsDefaults
{
    public static string SystemName => "Misc.ServingSuggestions";
}
```

**`Nop.Plugin.Misc.ServingSuggestions.csproj`** — new, mirrors Ingredients' shape. No `Npgsql` reference (that was for Ingredients' concurrent-write serialization detection, doesn't apply here — no optimistic concurrency).

**`plugin.json`** — new
```json
{
  "Group": "Misc",
  "FriendlyName": "Serving suggestions",
  "SystemName": "Misc.ServingSuggestions",
  "Version": "5.00.1",
  "SupportedVersions": [ "5.00" ],
  "Author": "Ges I Lubczyk",
  "DisplayOrder": 1,
  "FileName": "Nop.Plugin.Misc.ServingSuggestions.dll",
  "Description": "Serving suggestion (title, description, image, ordered steps) on products."
}
```

**`logo.png`** — new, any valid PNG.

### Domain

**`Domain/ServingSuggestion.cs`**
```csharp
public partial class ServingSuggestion : BaseEntity, ILocalizedEntity
{
    public int ProductId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int PictureId { get; set; }
}
```

**`Domain/ServingSuggestionStep.cs`**
```csharp
public partial class ServingSuggestionStep : BaseEntity, ILocalizedEntity
{
    public int ServingSuggestionId { get; set; }
    public string Text { get; set; }
    public int DisplayOrder { get; set; }
}
```

### Data

**`Data/Mapping/Builders/ServingSuggestionBuilder.cs`**
```csharp
public class ServingSuggestionBuilder : NopEntityBuilder<ServingSuggestion>
{
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(ServingSuggestion.Title)).AsString(400).NotNullable()
            .WithColumn(nameof(ServingSuggestion.Description)).AsString(int.MaxValue).NotNullable()
            .WithColumn(nameof(ServingSuggestion.PictureId)).AsInt32().ForeignKey<Picture>()
            .WithColumn(nameof(ServingSuggestion.ProductId)).AsInt32().ForeignKey<Product>();
    }
}
```

**`Data/Mapping/Builders/ServingSuggestionStepBuilder.cs`**
```csharp
public class ServingSuggestionStepBuilder : NopEntityBuilder<ServingSuggestionStep>
{
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(ServingSuggestionStep.Text)).AsString(int.MaxValue).NotNullable()
            .WithColumn(nameof(ServingSuggestionStep.ServingSuggestionId)).AsInt32().ForeignKey<ServingSuggestion>();
    }
}
```
Both `ForeignKey<T>()` left at default `Rule.Cascade`.

**`Data/Migrations/SchemaMigration.cs`**
```csharp
[NopMigration("2026-08-31 00:00:00", "Misc.ServingSuggestions schema", MigrationProcessType.Installation)]
public class SchemaMigration : Migration
{
    public override void Up()
    {
        this.CreateTableIfNotExists<ServingSuggestion>();
        this.CreateTableIfNotExists<ServingSuggestionStep>();
    }
    public override void Down()
    {
        this.DeleteTableIfExists<ServingSuggestionStep>();
        this.DeleteTableIfExists<ServingSuggestion>();
    }
}
```

### Services

**`Services/IServingSuggestionService.cs`** — resolved gap: per-row step CRUD (developer-confirmed, see Gate 2 resolution below)
```csharp
public record ServingSuggestionLocalizedValue(int LanguageId, string Title, string Description);
public record ServingSuggestionStepLocalizedValue(int LanguageId, string Text);

public interface IServingSuggestionService
{
    Task<ServingSuggestion> GetServingSuggestionByIdAsync(int servingSuggestionId);
    Task<ServingSuggestion> GetServingSuggestionByProductIdAsync(int productId);
    Task InsertServingSuggestionAsync(ServingSuggestion servingSuggestion, IList<ServingSuggestionLocalizedValue> localizedValues = null);
    Task UpdateServingSuggestionAsync(ServingSuggestion servingSuggestion, IList<ServingSuggestionLocalizedValue> localizedValues = null);
    Task DeleteServingSuggestionAsync(ServingSuggestion servingSuggestion);

    Task<IList<ServingSuggestionStep>> GetServingSuggestionStepsAsync(int servingSuggestionId);
    Task<ServingSuggestionStep> GetServingSuggestionStepByIdAsync(int servingSuggestionStepId);
    Task InsertServingSuggestionStepAsync(ServingSuggestionStep step, IList<ServingSuggestionStepLocalizedValue> localizedValues = null);
    Task UpdateServingSuggestionStepAsync(ServingSuggestionStep step, IList<ServingSuggestionStepLocalizedValue> localizedValues = null);
    Task DeleteServingSuggestionStepAsync(ServingSuggestionStep step);
}
```

**`Services/ServingSuggestionService.cs`** — mirrors `IngredientService.cs`+`ProductIngredientService.cs`. Fields: `ILocalizedEntityService`, `INopDataProvider`, `IRepository<ServingSuggestion>`, `IRepository<ServingSuggestionStep>`, `IRepository<LocalizedProperty>`, `IPictureService`.
- Insert/Update (parent): transaction-wrapped entity write + `SaveLocalizedValueAsync` per locale for `Title`/`Description`.
- Insert/Update (step): plain entity write + `SaveLocalizedValueAsync` for `Text`, no transaction wrapper (mirrors `ProductIngredientService`'s single-row writes).
- Delete (parent), ordering load-bearing (Picture cascades from ServingSuggestion, so Picture must go last or it takes the row with it):
  1. Delete `LocalizedProperty` rows, `LocaleKeyGroup == nameof(ServingSuggestion)`.
  2. Delete `LocalizedProperty` rows, `LocaleKeyGroup == nameof(ServingSuggestionStep)`, for every step.
  3. Delete `ServingSuggestion` row (cascades steps).
  4. `DeletePictureAsync` — last.
  All in one transaction.
- Gets use `_repository.GetByIdAsync(id, cache => default)` — makes `CacheEventConsumer<T>` mandatory.

**`Services/ServingSuggestionsPermissionConfigManager.cs`**
```csharp
public class ServingSuggestionsPermissionConfigManager : IPermissionConfigManager
{
    public const string SERVING_SUGGESTIONS_VIEW = "ServingSuggestions.View";
    public const string SERVING_SUGGESTIONS_CREATE_EDIT_DELETE = "ServingSuggestions.CreateEditDelete";

    public IList<PermissionConfig> AllConfigs => new List<PermissionConfig>
    {
        new("Admin area. Serving suggestions. View", SERVING_SUGGESTIONS_VIEW, nameof(StandardPermission.Catalog), NopCustomerDefaults.AdministratorsRoleName),
        new("Admin area. Serving suggestions. Create, edit, delete", SERVING_SUGGESTIONS_CREATE_EDIT_DELETE, nameof(StandardPermission.Catalog), NopCustomerDefaults.AdministratorsRoleName)
    };
}
```
Not registered in `NopStartup.cs` — auto-discovered.

**`Services/Caching/ServingSuggestionCacheEventConsumer.cs`**
```csharp
public class ServingSuggestionCacheEventConsumer : CacheEventConsumer<ServingSuggestion>;
```
**`Services/Caching/ServingSuggestionStepCacheEventConsumer.cs`**
```csharp
public class ServingSuggestionStepCacheEventConsumer : CacheEventConsumer<ServingSuggestionStep>;
```

**`Services/Events/ServingSuggestionProductDeletedEventConsumer.cs`** — no Ingredients precedent (real gap in GIL-001)
```csharp
public class ServingSuggestionProductDeletedEventConsumer : IConsumer<EntityDeletedEvent<Product>>
{
    protected readonly IServingSuggestionService _servingSuggestionService;

    public ServingSuggestionProductDeletedEventConsumer(IServingSuggestionService servingSuggestionService)
        => _servingSuggestionService = servingSuggestionService;

    public async Task HandleEventAsync(EntityDeletedEvent<Product> eventMessage)
    {
        var servingSuggestion = await _servingSuggestionService.GetServingSuggestionByProductIdAsync(eventMessage.Entity.Id);
        if (servingSuggestion != null)
            await _servingSuggestionService.DeleteServingSuggestionAsync(servingSuggestion);
    }
}
```

### Admin

- **`Admin/Components/ServingSuggestionAdminViewComponent.cs`** — guards `widgetZone == AdminWidgetZones.ProductDetailsBlock`, `AuthorizeAsync(...VIEW)` else empty content, renders card view.
- **`Admin/Factories/ServingSuggestionAdminModelFactory.cs`** — `PrepareServingSuggestionModelAsync`, `PrepareServingSuggestionStepSearchModelAsync`, `PrepareServingSuggestionStepListModelAsync`, `PrepareServingSuggestionStepModelAsync`.
- **`Admin/Controllers/ServingSuggestionController.cs`** — `[Area(ADMIN)] [AutoValidateAntiforgeryToken] [ValidateIpAddress] [AuthorizeAdmin] [SaveSelectedTab]`, actions: `ServingSuggestionEditPopup` (GET/POST, get-or-create + picture upload via `IFormCollection`, mirrors `ProductController.ProductPictureAdd`, delete-old-picture-last per `CategoryController.cs:294-299`), `ServingSuggestionDelete`, `ServingSuggestionStepList`, `ServingSuggestionStepCreatePopup` (GET/POST), `ServingSuggestionStepEditPopup` (GET/POST, full Locales editor per developer confirmation), `ServingSuggestionStepUpdate` (inline `DisplayOrder` edit), `ServingSuggestionStepDelete`.
- **`Admin/Models/ServingSuggestionModel.cs`** (+`ServingSuggestionLocalizedModel`), **`ServingSuggestionStepModel.cs`** (+`ServingSuggestionStepLocalizedModel`, developer-confirmed full per-language editing), **`ServingSuggestionStepSearchModel.cs`**, **`ServingSuggestionStepListModel.cs`**.
- **`Admin/Validators/ServingSuggestionValidator.cs`** — `Title` not empty, `PictureId > 0` (spec: image required).
- **`Admin/Validators/ServingSuggestionStepValidator.cs`** — `Text` not empty.
- Views: `Admin/Views/Components/ServingSuggestion.cshtml` (`<nop-card>` + steps DataTables grid, mirrors `ProductIngredients.cshtml`), `ServingSuggestionEditPopup.cshtml` (`_AdminPopupLayout`, `Html.LocalizedEditorAsync` for Title/Description, file input for picture), `ServingSuggestionStepCreatePopup.cshtml`/`StepEditPopup.cshtml` (mirrors `SpecificationAttribute/OptionCreatePopup.cshtml`/`OptionEditPopup.cshtml`), `_ViewImports.cshtml`, `_ViewStart.cshtml`.

### Public (storefront)

- **`Public/Components/ServingSuggestionViewComponent.cs`** — `PrepareServingSuggestionModelAsync` (null if none; else localized Title/Description, steps ordered by `DisplayOrder`, picture URL), `InvokeAsync` (empty content if null model).
- **`Public/Models/PublicServingSuggestionModel.cs`** (+`PublicServingSuggestionStepModel`).
- Views: `Public/Views/Components/ServingSuggestion.cshtml`, `_ViewImports.cshtml`, `_ViewStart.cshtml`.

### Infrastructure

- **`Infrastructure/NopStartup.cs`** — registers `IServingSuggestionService`/`ServingSuggestionAdminModelFactory`, `Order => 3000`.
- **`Infrastructure/MapperConfiguration.cs`** — AutoMapper profiles, entity ↔ model, `Locales`/`PictureUrl`/`ServingSuggestionStepSearchModel` ignored on the entity→model map.
- No `RouteProvider.cs`.

### Wiring changes (not caught by the compiler)

- **`src/NopCommerce.sln`** — add project entry, 6 build-config lines, nested-project line (mirrors GIL-001's).
- **`src/Tests/Nop.Tests/Nop.Tests.csproj`** — add `ProjectReference`.
- **`src/Tests/Nop.Tests/Nop.Services.Tests/ServiceTest.cs`** — register plugin descriptor + `ApplyUpMigrations` call.

### Documentation

- **`Docs/BusinessLogic/product-serving-suggestions.md`** — new, same commit as code.
- **`Docs/Glossary/shop.md`** — add "Serving suggestion" entry (spec §12).

### Order of work
1. Domain entities → 2. Data builders + migration → 3. Service (incl. step CRUD) + permission manager → 4. Cache consumers → 5. Plugin/defaults/csproj/infrastructure → 6. `.sln`/test wiring → 7. Product-deleted event consumer → 8. Admin (models→validator→factory→controller→views) → 9. Public → 10. Docs.

### Tests
`ServingSuggestionServiceTests.cs` (transactional insert/update, localization fallback, delete cascade incl. Picture, picture-replace-deletes-old, validation: no image rejected / zero steps succeeds), `ServingSuggestionCacheEventConsumerTests.cs`, `ServingSuggestionViewComponentTests.cs` (renders with/without suggestion, step order), `ServingSuggestionProductDeletedEventConsumerTests.cs` (no Ingredients precedent), `ServingSuggestionsPluginTests.cs` (uninstall purges LocalizedProperty + Picture rows, no Ingredients precedent for the Picture half).

### Gate 2 resolution — three open gaps, developer-confirmed via multiple-choice

1. **Step CRUD shape:** per-row Insert/Update/Delete methods (not bundled into the parent's own insert/update) — matches every other "ordered child rows" admin UI in this codebase.
2. **Picture upload mechanism:** bespoke `IFormCollection`-based upload mirroring `ProductController.ProductPictureAdd`, not the simpler `[UIHint("Picture")]` + shared `PictureController.AsyncUpload` route.
3. **Per-language step editing:** full Locales editor for `ServingSuggestionStep.Text` in v1, consistent with the entity being `ILocalizedEntity`.

**Approved by:** Mateusz Nycz
**Date:** 2026-08-31
**Revision notes:** none beyond the three Gate 2 gap resolutions above, all confirmed as the plan's own recommended option.
