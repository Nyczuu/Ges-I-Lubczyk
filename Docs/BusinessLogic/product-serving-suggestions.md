# Product serving suggestions

Every product can carry **one** serving suggestion — guidance on how to serve/present the dish, shown on
its product page. Shipped by [GIL-002](../Specs/GIL-002-serving-suggestions/spec.md) as
`Nop.Plugin.Misc.ServingSuggestions` — a self-contained plugin, no core change. See the
[Glossary](../Glossary/shop.md#what-we-sell) for the term "Serving suggestion".

## What a serving suggestion is

A serving suggestion (`ServingSuggestion`) is title/description/image/ordered-steps content owned by
exactly **one** product — not a shared, reusable catalogue entry the way `Ingredient` (GIL-001) is shared
across products. It has:

- **Title** — required, bounded like `Product.Name` (`AsString(400)`).
- **Description** — required, unbounded.
- **Picture** — required. The admin form must always be able to pick/replace a picture; there is no "no
  picture" sentinel the way `Category.PictureId` uses `0`.
- **Steps** (`ServingSuggestionStep`) — optional, zero or more ordered instruction lines
  (`DisplayOrder`), an ordered mini-recipe ("how to serve it").

Unlike `Ingredient`, a serving suggestion has no existence independent of the product it belongs to: it
inherits the product's own `IAclSupported`/`Published`/store-mapping restrictions, with no independent
visibility rule of its own.

## Exactly one per product

The "exactly one `ServingSuggestion` per product" invariant is **not** a database unique constraint — no
builder in this codebase declares `.Unique()` on this kind of column. It is enforced at the service/admin
layer instead: the admin surface is a single "get-or-create" popup
(`ServingSuggestionController.ServingSuggestionEditPopup`) keyed by `ProductId`, never a bare "insert new"
action the admin UI could call twice.

## Required picture and its lifecycle

The picture is uploaded via a bespoke `IFormCollection`-based upload (mirroring
`ProductController.ProductPictureAdd`'s `form.Files` + `IPictureService.InsertPictureAsync(IFormFile)`
pattern), not the shared `[UIHint("Picture")]` + `PictureController.AsyncUpload` route.

**Replacing an existing picture** deletes the previous `Picture` row, mirroring
`CategoryController.cs:294-299`'s ordering: the entity write (new `PictureId`) happens first, and the old
picture is deleted only after that write succeeds — never the other way around. Since the required image
makes editing a serving suggestion's picture the common case rather than an edge case, skipping this
cleanup would accumulate unused `Picture` rows on every re-photograph.

**`ServingSuggestion.PictureId → Picture` has a real database foreign key** (`ForeignKey<Picture>()`,
default `Rule.Cascade`), unlike `Category.PictureId` (unmapped, because `Category` uses `0` as a "no
picture" sentinel a real FK would reject). This makes the deletion **ordering inside
`ServingSuggestionService.DeleteServingSuggestionAsync` load-bearing**: `Picture` is not
`ISoftDeletedEntity`, so `IPictureService.DeletePictureAsync` issues a genuine `DELETE`. If the service
ever deleted the `Picture` row *before* the `ServingSuggestion` row, the FK cascade would silently take the
`ServingSuggestion`/`ServingSuggestionStep` rows with it as a side effect, ahead of the method's own
explicit cleanup. The service therefore always deletes, in order: `LocalizedProperty` rows (both
`LocaleKeyGroup`s), the `ServingSuggestion` row (which cascades its `ServingSuggestionStep` rows via a
real FK), and only then the `Picture` row, last.

## Product deletion

`Product` implements `ISoftDeletedEntity`; `ProductService.DeleteProductAsync` therefore never issues a
physical `DELETE` for a product, only an `UPDATE` setting `Deleted = true`. A DB-level cascade FK from
`ServingSuggestion.ProductId → Product` would never fire on this path. Cleanup is instead an
application-level consumer, `ServingSuggestionProductDeletedEventConsumer : IConsumer<EntityDeletedEvent<Product>>`
— `EntityRepository<T>.DeleteAsync` always publishes `EntityDeletedEvent<T>` even for a soft-deleted
entity, so this event is a reliable hook regardless of the physical-delete question. The consumer looks up
the product's serving suggestion (if any) and runs the same `DeleteServingSuggestionAsync` the admin
"delete" action uses, so the ordering guarantee above applies here too. The same cleanup runs on plugin
uninstall (see below).

GIL-001's `Ingredient` has no equivalent consumer — `ProductIngredientMapping` rows are left orphaned on
product soft-delete. That is a pre-existing, out-of-scope gap in GIL-001, not something this plugin needed
to match.

## Storefront rendering

Rendered only on the product detail page (not listing or quick-view), via `ServingSuggestionViewComponent`
in widget zone `productdetails_bottom` — distinct from GIL-001's `productdetails_before_collateral`, so
the two plugins never compete for the same slot. It inherits whatever visibility the product page itself
already enforces. A product with no serving suggestion renders nothing extra (no error, no empty markup).

Steps render in `DisplayOrder` order.

## Localization

`Title`, `Description`, and `ServingSuggestionStep.Text` are `ILocalizedEntity`, following the same
`LocalizedProperty` mechanism GIL-001's `Ingredient` established as the first plugin-owned entity to use
it in this codebase (`LocaleKeyGroup` = unqualified type name, `"ServingSuggestion"`/
`"ServingSuggestionStep"`).

Per-language editing is full: the admin popups for both the serving suggestion itself and each step use
`Html.LocalizedEditorAsync`, matching `Product.Name`/`Category.Name`'s own editing pattern.

On product deletion and on plugin uninstall, `LocalizedProperty` rows are deleted explicitly — no
automatic mechanism purges them, and a shared core table like `LocalizedProperty` is never touched by the
plugin's own `SchemaMigration.Down()`.

## Caching

`ServingSuggestionService`'s by-id reads (`GetServingSuggestionByIdAsync`,
`GetServingSuggestionStepByIdAsync`) use the cached `GetByIdAsync(id, cache => default)` pattern, which
makes `CacheEventConsumer<T>` boilerplate mandatory: `ServingSuggestionCacheEventConsumer` and
`ServingSuggestionStepCacheEventConsumer`. There is no bespoke derived/render cache the way GIL-001 has
for its composition closure — a serving suggestion is a flat title/description/image/ordered-steps read
for one product, no traversal.

**A nuance surfaced while implementing this plugin, not introduced by it:** `Nop.Services.Media` itself
declares no `CacheEventConsumer<Picture>`, but `Nop.Plugin.Misc.AzureBlob` does
(`PictureCacheEventConsumer`) — its base-class by-id/by-ids/all invalidation runs on every
insert/update/delete regardless of whether Azure Blob storage is actually configured, since this
codebase's event consumers are discovered by type (`ITypeFinder`), not gated on plugin installation state.
So in an actual deployment built from this solution — the Dockerfile builds the whole `.sln`, and every
plugin's assembly ships in the image — `Picture`'s by-id cache is very likely already invalidated as a
side effect of that unrelated plugin being present, whether or not a store actually uses Azure Blob
storage. The gap is real only inside this repo's **isolated test project**, which references a narrow set
of plugin assemblies and does not include `Nop.Plugin.Misc.AzureBlob`: `IPictureService.GetPictureByIdAsync(id)`
uses the same cached `GetByIdAsync(id, cache => default)` pattern, so within that test project once a
picture id has been looked up once in a process, deleting that picture via
`IPictureService.DeletePictureAsync` does not invalidate the cached entry there — a later
`GetPictureByIdAsync` call for the same id in the same test run can return a stale, already-deleted object.
This plugin's own tests verify picture deletion through `IRepository<Picture>` directly rather than
through `IPictureService`'s cached read, to avoid being masked or confused by that test-project-only gap.

## Uninstall cleanup ordering

`ServingSuggestionsPlugin.UninstallAsync` must purge `Picture` rows for every `ServingSuggestion` **before**
removing the widget system name, permissions, and locale resources — because `PluginService.UninstallPluginsAsync`
runs `SchemaMigration.Down()` immediately after `UninstallAsync()` returns, dropping the `ServingSuggestion`
table (and every `PictureId` value it held) before any later step could still discover which pictures need
deleting. `Ingredient` has no `Picture` field, so this is the first plugin-uninstall precedent in this
codebase that needs to purge a shared core table's rows keyed off values that are about to disappear with
the plugin's own schema.

## Future direction, not built now

- **Settings / configurability.** Presentation and admin placement are fixed for v1, matching GIL-001.
- **Store mapping / ACL independent of the product.** Not needed — a serving suggestion has no existence
  independent of its product.
