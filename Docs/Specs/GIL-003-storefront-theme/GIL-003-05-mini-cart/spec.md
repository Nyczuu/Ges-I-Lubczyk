---
id: GIL-003-05
kind: Task
title: Mini-cart restyle
status: Ready
parent: GIL-003
---

# Task — Mini-cart restyle

## 1. Business goal & outcome

The mockup's slide-over cart drawer (header cart button, item list, free-shipping progress bar,
subtotal, checkout CTA) needs to render using this build's real mini-cart mechanism, not the mockup's
client-only cart-state JavaScript.

Outcome: the existing flyout mini-cart matches the mockup's visual design end to end.

Success: adding a product to cart with the `GesILubczyk` theme active opens a slide-over panel styled
per the mockup, showing real cart contents, real subtotal, and (if the free-shipping threshold exists as
a real store setting) real progress toward it.

## 2. Root cause / current behavior

N/A — not a bug fix. Current behavior (verified, not assumed): this build already has a real flyout
mini-cart — `Views/Shared/Components/FlyoutShoppingCart/Default.cshtml` (`div#flyout-cart`), gated by
`ShoppingCartSettings.MiniShoppingCartEnabled`. The trigger and cart-quantity badge live in
`HeaderLinks/Default.cshtml`, wired by `wwwroot/js/public.ajaxcart.js`'s `AjaxCart.init`. This is the
real mechanism the mockup's `#cartDrawer`/`#cartBtn`/`#cartCountBadge` markup should map onto — it is not
being built from scratch.

## 3. Placement — plugin or core?

Theme (CSS/markup restyle of the existing flyout view + its trigger), no plugin — plus the same narrow,
developer-approved `Nop.Web.Framework` exception as `GIL-003-01` (§7): one small additive locale
migration for the free-shipping bar's brand-new, theme-owned copy. No other core change.

## 4. Extension point

- `Themes/GesILubczyk/Views/Shared/Components/FlyoutShoppingCart/Default.cshtml` — theme override of the
  core partial, restyled to the mockup's drawer layout (header, free-shipping bar, item list, subtotal,
  checkout button). Confirmed overridable the same way `Footer/Default.cshtml` is (GIL-003-01 §4).
  `_Root.cshtml`/`_Header.cshtml` are not touched here beyond what GIL-003-01 already does for the cart
  button itself.
- `HeaderLinks/Default.cshtml`'s existing hover/focus toggle and `.cart-qty` badge are restyled via CSS,
  not rewritten — this Task does not touch `public.ajaxcart.js`'s cart logic.

## 5. Data model & migration

N/A — no entity/schema change.

## 6. Admin & storefront surface

- `Content/css/mini-cart.css` (new theme file, per [GIL-003 §4](../spec.md)) — restyles the flyout
  drawer and its header cart button/badge to match the mockup.
- **Resolved (developer): the free-shipping progress bar is in scope**, wired to the real
  `ShippingSettings.FreeShippingOverXEnabled`/`FreeShippingOverXValue` settings and
  `OrderTotalCalculationService.IsFreeShippingAsync`'s underlying computation (both confirmed present in
  this build) — never a client-invented value the way the mockup's static JS displayed one. When
  `FreeShippingOverXEnabled` is `false`, the bar does not render (no fabricated threshold shown).
- No new admin page — `MiniShoppingCartEnabled` is an existing setting, unchanged.

## 7. Settings, permissions, localization

No new settings, no new permissions. Two distinct situations:

- **Empty-cart message** — the flyout's existing `ShoppingCart.Mini.NoItems` resource already covers
  this (verified: `FlyoutShoppingCart/Default.cshtml` renders it, a long-standing core key, not new). If
  the developer wants the mockup's "Twoja spiżarnia jest pusta" brand voice instead of the default
  wording, that is a **value** edit via Admin → Configuration → Languages → Resources, no new key, no
  migration.
- **Free-shipping bar copy — genuinely new, theme-owned strings.** No existing resource covers the
  mockup's dynamic "Do darmowej dostawy brakuje: {amount} zł" / "Przysługuje Ci darmowa wysyłka..." text
  (`Products.FreeShipping` is a different string, used on the product details page, not the cart). This
  copy has no plugin `InstallAsync` to own it — it lives in a theme view — so it follows the same
  mechanism `GIL-003-01` uses for its announcement-bar text: this Task ships its own small, additive
  `[NopUpdateMigration(..., UpdateMigrationType.Localization)]`-style migration in
  `Nop.Web.Framework/Migrations/`, scoped to just these keys, Polish only, per
  [GIL-003 §4](../spec.md)'s theme-owned-key rule. `ddd-modeler` picks exact key names and the Polish
  wording (mirroring the mockup's two states — below threshold, threshold reached).

## 8. Events & scheduled tasks

N/A.

## 9. Caching

N/A — reason: this Task restyles already-rendered cart output; it does not change cart caching behavior.

## 10. Failure scenarios

Empty-cart and AJAX-failure behavior in `public.ajaxcart.js` is existing, unchanged by this Task. New
for this Task: `FreeShippingOverXEnabled = false` must hide the progress bar entirely rather than show a
zero/broken bar — the one real conditional this restyle introduces (§6).

## 11. Test scenarios

Primarily manual/visual (presentation restyle of an already-functional cart), but the free-shipping bar
adds one real behavior worth a test at whatever level `ddd-modeler` places the computation (view
component/model level, not a new service method): the bar's visibility and displayed remaining amount
across `FreeShippingOverXEnabled` on/off and cart subtotal above/below `FreeShippingOverXValue`. Manual
verification for the rest: add a product to cart, confirm the flyout opens styled per the mockup with
correct item data, quantity controls, and subtotal.

## 12. Documentation impact

N/A — reason: no new business rule; the mini-cart mechanism is pre-existing and this Task is purely
visual.

## 13. Deployment & rollout

No `Dockerfile`/`appsettings` change. Per [GIL-003 §7](../spec.md), folded into the same single
theme-switch rollout as the other sibling Tasks.
