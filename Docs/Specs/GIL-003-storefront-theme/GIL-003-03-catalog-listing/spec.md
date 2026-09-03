---
id: GIL-003-03
kind: Task
title: Catalog & product listing restyle
status: Ready
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
