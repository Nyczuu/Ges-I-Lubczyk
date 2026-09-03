---
id: GIL-003-04
kind: Task
title: Product details restyle, including Ingredients and ServingSuggestions plugin output
status: Ready
parent: GIL-003
---

# Task — Product details restyle, including Ingredients and ServingSuggestions plugin output

## 1. Business goal & outcome

The mockup's per-product "Pełna Etykieta" (ingredients) and "Komentarz Joanny Nycz" (chef's serving tip)
panels already exist as data and public rendering in this repo, shipped by
[GIL-001](../../GIL-001-product-ingredients/spec.md) (`Nop.Plugin.Misc.Ingredients`) and
[GIL-002](../../GIL-002-serving-suggestions/spec.md) (`Nop.Plugin.Misc.ServingSuggestions`) — both render
into real product-details widget zones today, but with plain unstyled markup and generic English labels.

Outcome: the product details page, including both plugins' output, visually matches the mockup's panels.

Success: browsing a product with the `GesILubczyk` theme active shows the ingredients list and serving
suggestion styled per the mockup, with Polish panel titles matching the mockup's tone ("Pełna Etykieta",
"Komentarz Joanny Nycz").

## 2. Root cause / current behavior

N/A — not a bug fix. Current behavior (verified, not assumed): both plugins already render publicly, with
no CSS targeting their output classes in either theme.

## 3. Placement — plugin or core?

Two existing plugins, styled and relabeled only — no new plugin, no core change:

- `Nop.Plugin.Misc.Ingredients` — `IngredientsViewComponent` renders `.product-ingredients` into
  `PublicWidgetZones.ProductDetailsBeforeCollateral` (`productdetails_before_collateral`).
- `Nop.Plugin.Misc.ServingSuggestions` — `ServingSuggestionViewComponent` renders
  `.product-serving-suggestion` into `PublicWidgetZones.ProductDetailsBottom` (`productdetails_bottom`).

Per [GIL-003 §4](../spec.md)'s "content-plugin ownership stays put" constraint, this Task does not create
a competing view component or move either plugin's data model — it styles their existing output and adds
new locale resources for their block titles within each plugin's own existing locale-resource set.

## 4. Extension point

N/A beyond the two already-existing `IWidgetPlugin` registrations above — this Task adds no new
extension point.

## 5. Data model & migration

N/A — reason: no new/changed entities. `Ingredient`, `IngredientComposition`,
`ProductIngredientMapping`, `ServingSuggestion`, and `ServingSuggestionStep` already exist from GIL-001
and GIL-002.

## 6. Admin & storefront surface

- `Content/css/product-details.css` (new theme file, per [GIL-003 §4](../spec.md)) — styles
  `.product-ingredients` as the mockup's "Pełna Etykieta" panel.
- **Resolved (developer):** `ServingSuggestion.cshtml` has no static heading today — it renders only the
  per-product, admin-entered `Model.Title` (`ServingSuggestionViewComponent.cs:59`). This Task adds a
  fixed "Komentarz Joanny Nycz" heading above `Model.Title` in that view — a small, in-scope markup
  change to this plugin's own view (not a competing component, per [GIL-003 §4](../spec.md)'s
  plugin-ownership constraint) plus one new locale resource key on `Nop.Plugin.Misc.ServingSuggestions`.
- No new admin page — both plugins' existing admin CRUD (product-edit tabs) is unchanged.
- **Resolved (developer): "Masa słoju" (portion weight) and "Przygotowanie" (prep time) are out of scope**
  for this Task. Neither maps to an existing field on `Ingredient`/`ServingSuggestion`, and no new field
  is added to either entity here — this Task styles and relabels the two plugins' existing output only.

## 7. Settings, permissions, localization

No new settings, no new permissions (both plugins' existing permission records are unchanged).

Two distinct locale-resource situations, not one:

- **`Ingredients`'s existing panel-title resource** (currently generic English) — a **value** change on
  an already-existing key. On a fresh install, the updated `InstallAsync` dictionary literal ships it
  automatically. On an environment where this plugin is already installed (this repo's own `develop`,
  post-GIL-001), `InstallAsync` will not re-run — the developer updates the resource's value manually via
  Admin → Configuration → Languages → Resources as a one-time step, per
  [GIL-003 §4](../spec.md)'s "content changes are manual admin steps" pattern. No migration needed for
  this key.
- **`ServingSuggestions`'s brand-new "Komentarz Joanny Nycz" heading resource** (§6) — a key that has
  never existed in any environment. `InstallAsync`'s dictionary is updated so a fresh install/reinstall
  picks it up automatically; on this repo's own already-installed environment, the developer adds the
  resource row manually via Admin as the same kind of one-time step — this Task does not add a plugin
  upgrade-migration mechanism neither plugin has today.

Confirm `UninstallAsync` on both plugins already removes what it adds (pre-existing behavior from
GIL-001/GIL-002) — the one new resource key from this Task falls under that existing removal logic, not
a new one.

## 8. Events & scheduled tasks

N/A — no new events, no scheduled tasks.

## 9. Caching

N/A — reason: this Task changes only the CSS/label styling of already-rendered widget output; it does
not change either plugin's existing caching behavior (including `Nop.Plugin.Misc.Ingredients`'s
`IngredientClosure` materialized-closure cache).

## 10. Failure scenarios

N/A — no new failure mode introduced; a product with no ingredients or no serving suggestion already has
existing, unchanged behavior in both plugins' view components (they render nothing/empty state today).

## 11. Test scenarios

N/A — reason: presentation-only CSS/label change to already-tested plugin rendering (GIL-001/GIL-002
cover the underlying data and rendering logic). Manual verification: browse a product that has both an
ingredient list and a serving suggestion, confirm both panels render per the mockup's visual design with
the new Polish titles.

## 12. Documentation impact

N/A — reason: this Task does not change the business logic documented in
[`Docs/BusinessLogic/product-ingredients.md`](../../../BusinessLogic/product-ingredients.md) or
[`Docs/BusinessLogic/product-serving-suggestions.md`](../../../BusinessLogic/product-serving-suggestions.md);
it is presentation and label text only.

## 13. Deployment & rollout

No `Dockerfile`/`appsettings` change. Per [GIL-003 §7](../spec.md), folded into the same single
theme-switch rollout as the other sibling Tasks.
