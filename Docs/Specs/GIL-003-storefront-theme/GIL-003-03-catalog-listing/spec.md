---
id: GIL-003-03
kind: Task
title: Catalog & product listing restyle
status: In Progress
parent: GIL-003
---

# Task — Catalog & product listing restyle

## 1. Business goal & outcome

The mockup's product grid (card with image, title, short description, price, add-to-cart button) and
category filter pills need to render on the real catalog, using this build's actual category navigation
and product-listing views rather than the mockup's client-side fake filter over a hardcoded JS array.

**Resolved scope (developer):** the card is a minimal restyle of real, already-available data —
`ProductOverviewModel` has no field for the mockup's badge overlay or "Słój: &lt;weight&gt;" line, and
wiring either in would mean changing `ProductOverviewModel`/`ProductModelFactory` (a `Nop.Web` change
beyond this Task's CSS-only scope). Both are **dropped** from the real card rather than backed by new
model fields. The mockup's separate "Skład i notatki z kuchni Joanny →" quick-view link is also dropped —
the card's existing image/title links already navigate to the product details page, matching the Epic's
"no quick-view modal" default (Epic §2). The add-to-cart button keeps its existing shared resource text
(no code change to `ShoppingCart.AddToCart`-style copy); if the developer later wants brand voice on it,
that is an Admin resource-value edit, not part of this Task.

Outcome: category listing pages show product cards matching the mockup's visual design, and category
navigation (the mockup's "Wszystkie / Dania z Gęsiną / Rosoły / ..." pills) is real site navigation
between real category pages.

Success: browsing a category page with the `GesILubczyk` theme active shows product cards styled per the
mockup, backed by real product data (see [GIL-003 §1](../spec.md) — nopCommerce's own sample catalog is
an acceptable stand-in), with working navigation between categories.

## 2. Root cause / current behavior

N/A — not a bug fix.

## 3. Placement — plugin or core?

Theme only (CSS restyle of existing views), no plugin, no core change.

## 4. Extension point

Verified against the checkout:

- Product card: `src/Presentation/Nop.Web/Views/Shared/_ProductBox.cshtml` (model
  `ProductOverviewModel`), used from the category grid/lines template. This Task restyles this partial's
  existing theme-overridable path (`Themes/GesILubczyk/Views/Shared/_ProductBox.cshtml`), not a new view.
- Category navigation: `Views/Shared/Components/MainMenu/Default.cshtml`. **This fork does not walk the
  raw category tree for its nav** — it uses a custom, admin-configurable `Menu`/`MenuItem` domain
  (`Nop.Core.Domain.Menus`, driven by `MenuModelFactory`) whose items can point at categories,
  manufacturers, vendors, products, or topics. The mockup's category-pill row therefore maps to **real
  menu items pointing at real categories**, styled as pills — not a reproduction of the mockup's
  client-side `data-cat` JS filter over one page.

## 5. Data model & migration

N/A — reason: no entity/schema change. Categories and the `Menu`/`MenuItem` rows the pills point at are
store content (Admin → Catalog → Categories, Admin → Content Management → Menus), not code.

## 6. Admin & storefront surface

- `Content/css/catalog.css` (new theme file, per [GIL-003 §4](../spec.md)) — restyles `_ProductBox` to
  the mockup's card (image, title, truncated description, price, add-to-cart button — no badge overlay,
  no weight line, no quick-view link, per the resolved scope in §1) and restyles the category pill row
  rendered from `MainMenu`.
- No new admin page. Populating real categories/menu items to match the mockup's five pills ("Wszystkie
  Dania", "Dania z Gęsiną i Mięsem", ...) is store-owner content work, out of scope per
  [GIL-003 §2](../spec.md) (same "data entry is not a dev task" boundary as the Epic).

## 7. Settings, permissions, localization

No new settings or permissions. No new locale resource keys are anticipated — category names and menu
labels are store content (already localizable via existing `Category`/`MenuItem` localization), not view
literals.

## 8. Events & scheduled tasks

N/A.

## 9. Caching

N/A — reason: this Task changes only the CSS/markup styling of already-cached `MainMenu` and category
listing output; it does not change what is cached or add a new cache key.

## 10. Failure scenarios

N/A — no external dependency; an empty category (no products) or an empty menu already has existing,
unchanged fallback behavior in the core views this Task restyles.

## 11. Test scenarios

N/A — reason: presentation-only CSS/markup restyle of existing views, no service-layer behavior. Manual
verification: browse a category with products, confirm card layout matches the mockup; click between
category pills and confirm real navigation (not a client-side filter) between category pages.

## 12. Documentation impact

N/A — reason: no new business rule; category/menu mechanisms are pre-existing and this Task is purely
visual.

## 13. Deployment & rollout

No `Dockerfile`/`appsettings` change. Per [GIL-003 §7](../spec.md), folded into the same single
theme-switch rollout as the other sibling Tasks.

## Technical design (ddd-modeler)

### Corrections to the spec's technical assumptions

- **Spec §4 claims the mockup's category-pill row "maps to real menu items pointing at real categories,
  styled as pills" via the fork's `MainMenu`/`Menu`/`MenuItem` domain.** Not achievable within this
  Task's own "theme only, no core change" placement:
  - `Nop.Core.Domain.Menus.MenuType` has exactly two values, `Main = 5` and `Footer = 10` — no
    page-scoped "category filter" menu type. Adding one is a `Nop.Core` enum change.
  - `MainMenuViewComponent.InvokeAsync()` takes **no parameters** and always renders
    `PrepareMenuModelsAsync(MenuType.Main).FirstOrDefault()`, invoked exactly once per page, in the
    header (already spoken for by GIL-003-01). There is no way to render a *second*, distinct
    Main-type menu at a different page location without a `.cs` change to this component.
  - So `MainMenu` is the header nav, not something reusable for a second, page-local pill row without a
    core touch this Task rules out. **Design uses `CategoryNavigationViewComponent` instead — see below.**

- **Spec §1 claims "`ProductOverviewModel` has no field for the mockup's badge overlay."** Partially
  inaccurate: `ProductOverviewModel.MarkAsNew` already exists and is populated from real per-product
  admin data (`Product.MarkAsNew` + start/end dates, `ProductModelFactory.cs:1411-1413`) — simply never
  rendered by `_ProductBox.cshtml` today. It cannot reproduce the mockup's per-dish *descriptive* badge
  text ("Specjalność Joanny" etc. — correctly identified as unbacked), but could back a generic "Nowość"
  ribbon from real data. The already-approved "no badge" scope decision (Gate 1) covers this too — noted
  for completeness, not acted on.

### Design

**Product card — `_ProductBox.cshtml`: CSS-only, no view override (spec's plan holds here).**
`_ProductBox.cshtml` markup: `article.product-item` → `.picture` → `.details` → `h2.product-title` →
optional sku/rating → `.description` (`@Html.Raw(Model.ShortDescription)`) → `.add-info` → `.prices` +
`.buttons`. None of the mockup's dropped elements (badge, weight line, quick-view link) exist in this
markup, so there is nothing to hide — zero markup change required for the resolved scope.

`Content/css/catalog.css` restyles this existing markup only:
- `.product-item` → mockup's rounded card, using `--gil-*` tokens from `tokens.css`.
- `.picture img` → fixed height, `object-fit: cover`; `.description` → `-webkit-line-clamp: 2` (mockup's
  truncation).
- `.add-info { display:flex; justify-content:space-between; align-items:center }` puts `.prices` left /
  `.buttons` right with zero markup change.
- `.buttons .product-box-add-to-cart-button` restyled as the pill button; text stays
  `T("ShoppingCart.AddToCart")`, unchanged.
- `.item-grid` (in `_ProductsInGridOrLines.cshtml`) gets the mockup's CSS grid.
- **Resolved (developer): Compare/Wishlist buttons are left as-is** (whatever `CatalogSettings`/wishlist
  settings currently render) — no CSS hiding, consistent with using real settings rather than
  reproducing the mockup's single-button assumption.
- **Resolved (developer): no "Nowość" ribbon added** — the already-approved "no badge" scope decision
  (Gate 1) covers `MarkAsNew` too; not wired in.

**Category pills — reuse `CategoryNavigationViewComponent`, not `MainMenu`.**

The real, already-existing, already-cached mechanism is `CategoryNavigationViewComponent` +
`ICatalogModelFactory.PrepareCategoryNavigationModelAsync` (cached under
`NopModelCacheDefaults.CategoryAllModelKey` — no new cache key, confirms spec §9). It already walks the
real category tree and is already invoked in the category page's sidebar via `_ColumnsTwo.cshtml:43`.

Two theme-level Razor overrides (no `.cs` file touched anywhere):

1. `Themes/GesILubczyk/Views/Shared/Components/CategoryNavigation/Default.cshtml` — override of the core
   view. Flattens rendering to root-level categories only (mockup is a flat, single-level 5-pill row —
   no nested dropdowns), keeps the existing active/inactive detection so the current category's pill
   gets the "active" look — real `<a href>` navigation, not the mockup's `data-cat` JS filter. This also
   changes the *existing* sidebar rendering (an accepted side effect).
2. `Themes/GesILubczyk/Views/Catalog/CategoryTemplate.ProductsInGridOrLines.cshtml` — override of the
   category page's own content view (the only stock category template). Full copy of the core file plus
   one addition: a second `@await Component.InvokeAsync(typeof(CategoryNavigationViewComponent), new {
   currentCategoryId = Model.Id, currentProductId = 0 })` wrapped in a `<div class="category-pills-row">`,
   placed after the page title, rendering as a horizontal band above the grid, matching the mockup.
   **Resolved (developer): both this header-band duplication and the sidebar restyle are in scope** (not
   sidebar-only).

Why a second invocation rather than pure CSS repositioning of the sidebar: `<aside class="side-2">` also
carries Manufacturer Navigation, Vendor Navigation, Recently Viewed Products and Popular Product Tags.
CSS-reordering the whole `<aside>` above the grid would drag those unrelated blocks along. A second,
targeted invocation avoids touching the shared `_ColumnsTwo.cshtml` layout at all, per
`theming-standards-check`'s "never fork a shared layout for a localized change" rule. **Resolved
(developer): the sidebar's other blocks (Manufacturer/Vendor Navigation, Recently Viewed, Popular
Product Tags) stay visible, unchanged** — not hidden.

Verified mechanism for overriding a component's own view from a theme:
`ThemeableViewLocationExpander.ExpandViewLocations` prepends `/Themes/{theme}/Views/Shared/{0}.cshtml`;
`NopViewComponent.ViewAsync()` resolves via the qualified name `Components/{ComponentName}/{ViewName}` —
landing exactly on `/Themes/{theme}/Views/Shared/Components/CategoryNavigation/Default.cshtml`. No theme
in this repo currently overrides a component view (standard-ASP.NET-Core-verified reasoning, not an
in-repo precedent — worth a quick manual smoke check once implemented).

**Resolved (developer): the mockup's static "Receptura Joanny" caption is dropped** — avoids a brand-new
locale key plus the Epic's `Nop.Web.Framework` migration for one decorative label.

### Placement

Theme only, `Themes/GesILubczyk` — no plugin, no `.cs`/core file. This is CSS plus two small theme-level
Razor view overrides (reusing an existing view component's own view path), not "CSS restyle" alone —
still squarely in the theming skill's sanctioned "override that partial/view component" bucket, not a
core change.

### Domain model / Extension decision

N/A — no new entity or schema; no core entity gains a field; this reuses existing entities/fields
end-to-end.

### Simplicity check

Smallest version: CSS-only restyle of `_ProductBox.cshtml` (achieved). For pills, the smallest version
that still satisfies "real navigation between category pages" is restyling only the existing sidebar
invocation (one file) — pills would sit in the left column, not a header band. Gate 1 approved the larger
version (two files) to match the mockup's actual layout.

### Blast radius

- `_ProductBox.cshtml` CSS applies everywhere the partial renders: `RelatedProducts`,
  `RecentlyViewedProductsBlock`, `ProductsAlsoPurchased`, `HomepageProducts`, `HomepageBestSellers`,
  `CrossSellProducts` components — consistent styling, desirable, worth browsing beyond the category page
  once implemented.
- The `CategoryNavigation/Default.cshtml` override applies everywhere `_ColumnsTwo.cshtml` is used:
  Category, Manufacturer(All), Vendor(All/Reviews), ProductTags(All)/ProductsByTag,
  Search(ByFilterLevelValues), CompareProducts, CustomerProductReviews, `RecentlyViewedProducts.cshtml`.
  All these sidebars go from a nested tree to flat pills — broader than "category listing pages," but
  harmless and consistent with the Task's actual title. `ProductTemplate.Simple/Grouped.cshtml` (product
  details, GIL-003-04) use `_ColumnsOne` — confirmed unaffected, no cross-task collision.
- The `CategoryTemplate.ProductsInGridOrLines.cshtml` override is scoped to the category page only — no
  spillover onto Manufacturer/Vendor/Search pages from that file.
- `Head.cshtml` registration: already covered by GIL-003-01's pre-registered six-file list — no shared
  touch-point for this Task.

### Installed-store impact

No schema, settings, permissions, or locale-resource changes. Inert until `GesILubczyk` becomes the
active theme.

**Blocking dependency, verified:** `src/Presentation/Nop.Web/Themes/GesILubczyk/` does not exist in this
checkout yet — only `DefaultClean` is present. This Task cannot be implemented until GIL-003-01 ships the
scaffold, per Epic §5's sequencing.

**Approved by:** Mateusz Nycz (developer)
**Date:** 2026-09-03
**Revision notes:** Resolved during Gate 1 — (1) pills render both as a header band above the grid AND
in the restyled sidebar, not sidebar-only; (2) sidebar's other blocks (Manufacturer/Vendor Nav, Recently
Viewed, Popular Product Tags) stay visible, unchanged; (3) Compare/Wishlist buttons left as-is, no CSS
hiding; (4) "Receptura Joanny" caption dropped, no new locale key; (5) no "Nowość"/`MarkAsNew` ribbon —
covered by the existing "no badge" scope decision.

## Implementation plan (implementation-planner)

### Files

- **`src/Presentation/Nop.Web/Themes/GesILubczyk/Content/css/catalog.css`** — new (mirrors
  `Themes/DefaultClean/Content/css/styles.css`). Already referenced by GIL-003-01's pre-registered
  `Head.cshtml` list — no wiring change needed, only content. Selectors: `.product-item` (card shape),
  `.picture img` (fixed height, `object-fit: cover`), `.description` (`-webkit-line-clamp: 2`),
  `.add-info` (`display:flex; justify-content:space-between; align-items:center`),
  `.buttons .product-box-add-to-cart-button` (pill restyle), `.item-grid` (CSS grid),
  `.block-category-navigation .listbox .list`/`.list li`/`.list li.active`/`.list li.inactive` (pill
  look, targeting the existing `active`/`inactive`/`last` classes `CategoryNavigation/Default.cshtml`
  already emits — applies to both sidebar and header-band instances), `.category-pills-row
  .block-category-navigation` (horizontal-band layout scoped to the header instance only).
- **`src/Presentation/Nop.Web/Themes/GesILubczyk/Views/Shared/Components/CategoryNavigation/Default.cshtml`**
  — new override (mirrors core `Views/Shared/Components/CategoryNavigation/Default.cshtml`). Full copy,
  `@model CategoryNavigationModel`, keeping the `BreadCrumbContainsCurrentCategoryId` helper and
  `active`/`inactive`/`last` `liClass` computation unchanged. Change: inside `CategoryLine(...)`, delete
  the recursive `<ul class="sublist">` block for `SubCategories` — each `<li>` becomes just the `<a href>`
  + optional product count, no nesting. **Required addition, not in the design's own listing:** add
  `@inject INopUrlHelper NopUrl` at the top — a theme-overridden view's `_ViewImports.cshtml` ancestor
  chain resolves from its own physical path and never merges with `Nop.Web/Views/_ViewImports.cshtml`;
  GIL-003-01's theme `_ViewImports.cshtml` (copied from `DefaultClean`) injects `NopHtml` only, not
  `NopUrl`, and this view calls `NopUrl.RouteGenericUrlAsync<Category>(...)`.
- **`src/Presentation/Nop.Web/Themes/GesILubczyk/Views/Catalog/CategoryTemplate.ProductsInGridOrLines.cshtml`**
  — new override (mirrors core same path). Full copy (`@model CategoryModel`, same `@using`s/`@inject`s,
  same breadcrumb/filter sections, same subcategory/featured-products grids, same `_CatalogProducts`
  call). Addition: between the `page-title` div and `page-body` div, insert
  `<div class="category-pills-row">@await Component.InvokeAsync(typeof(CategoryNavigationViewComponent),
  new { currentCategoryId = Model.Id, currentProductId = 0 })</div>`. **Same required addition:**
  `@inject INopUrlHelper NopUrl` at the top (the mirror calls `NopUrl.RouteGenericUrlAsync<Category>(...)`
  at three points).

No `.csproj` edit — `Nop.Web.csproj`'s `<Content Include="Themes\**" .../>` wildcard covers every new
file under `Themes/GesILubczyk/`.

### Order of work

1. Confirm GIL-003-01 prerequisites exist: `theme.json`, `Views/_ViewImports.cshtml`,
   `Views/Shared/Head.cshtml` (with `catalog.css` already registered).
2. Create the `CategoryNavigation/Default.cshtml` override (flattened, with the local `NopUrl` inject).
3. Create the `CategoryTemplate.ProductsInGridOrLines.cshtml` override (full copy + pills-row addition,
   with the local `NopUrl` inject).
4. Create `Content/css/catalog.css`.
5. Manual smoke check (no in-repo precedent for a theme overriding a component view — worth verifying):
   browse a category page with `GesILubczyk` active, confirm (a) sidebar renders flat pills, (b) a
   second horizontal pill row renders above the grid, (c) both link to real category URLs with correct
   active/inactive state, (d) product cards match the mockup, (e) other `_ProductBox` consumers
   (`RelatedProducts`, `HomepageProducts`, etc.) still render correctly with the new CSS.

### Tests

None. No service method, entity method, `IConsumer<T>`, migration, or controller action with logic is
introduced or changed — matches spec §11.

### Standards skills to load

`theming-standards-check` (both `.cshtml` overrides and `catalog.css` — "override that partial/view
component" placement, `NopHtml`-registration rule already satisfied by GIL-003-01),
`localization-standards-check` (confirms no new literal — both copies carry over only pre-existing
`@T(...)` calls; the design explicitly drops "Receptura Joanny" to avoid a new key).

### Gaps in the approved design

None. The one technical fact the approved design didn't anticipate — the missing `NopUrl` inject in the
theme's `_ViewImports.cshtml` — has a single, self-contained fix (a local `@inject` line in each new
file), folded into the file plan above rather than left as an open item.

**Approved by:** Mateusz Nycz (developer)
**Date:** 2026-09-03
**Revision notes:** none — approved as planned.
