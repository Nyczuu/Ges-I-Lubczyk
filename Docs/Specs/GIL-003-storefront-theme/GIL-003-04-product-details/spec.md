---
id: GIL-003-04
kind: Task
title: Product details restyle, including Ingredients and ServingSuggestions plugin output
status: In Progress
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

## Technical design (ddd-modeler)

### Correction to the spec's technical assumption — the Ingredients key is not storefront-only

**Spec §7 claim:** "`Ingredients`'s existing panel-title resource... a value change on an already-existing
key."

**What's actually true:** that exact key, `Plugins.Misc.Ingredients.Ingredients`, is not solely a
storefront panel title — it is also the label of the Ingredients **admin sidebar menu entry**, set in
`src/Plugins/Nop.Plugin.Misc.Ingredients/Services/Events/IngredientsMenuEventConsumer.cs:51`. Grepped
every occurrence of the bare key across `src/`: exactly two consumers — the storefront view and this menu
consumer. Changing the key's value to "Pełna Etykieta" would silently rename the Admin → Catalog →
"Ingredients" navigation entry too.

### Deviation from the spec's stated approach

**This design instead adds a new, storefront-only key**, `Plugins.Misc.Ingredients.PublicWidget.Title` =
"Pełna Etykieta", used only in `Ingredients.cshtml`, leaving the existing bare key (and the admin menu
label it drives) untouched. This makes the Ingredients case handled exactly the same way the spec already
handles the ServingSuggestions case — both plugins end up with one brand-new, storefront-scoped key,
added to `InstallAsync`, requiring the same one-time manual Admin add on this repo's already-installed
`develop` environment. The spec's §7 "value change" framing for Ingredients is superseded by this —
**approved at Gate 1**.

### Placement

No new plugin, no core touch — confirmed against the checkout. Both zones' registrations verified:
`IngredientsPlugin.GetWidgetZonesAsync()` → `ProductDetailsBeforeCollateral`;
`ServingSuggestionsPlugin.GetWidgetZonesAsync()` → `ProductDetailsBottom`. Both zones are invoked in the
core view `src/Presentation/Nop.Web/Views/Product/ProductTemplate.Simple.cshtml:157,172`. No theme
currently overrides this view and this Task does not add one — CSS-only there.

The new theme CSS file (`Themes/GesILubczyk/Content/css/product-details.css`) targets a theme that
doesn't exist yet on this branch — blocked on GIL-003-01's merge per Epic §5's sequencing. The two
plugin-view/locale edits are not blocked by that dependency.

### Domain model / Extension decision

N/A for entity data — confirmed `Ingredient`/`ServingSuggestion` have no field resembling "portion
weight" or "prep time"; the out-of-scope call needs no extension mechanism. For the two locale
resources: each is added to its own plugin's existing `InstallAsync`/`UninstallAsync` lifecycle — not the
Epic's `Nop.Web.Framework` migration mechanism, which is reserved for keys with **no** owning plugin
lifecycle at all (neither of these new keys qualifies, since both plugins already have one).

### Design

**Ingredients** — `IngredientsPlugin.cs` `InstallAsync` dictionary: add
`["Plugins.Misc.Ingredients.PublicWidget.Title"] = "Pełna Etykieta"`. `UninstallAsync` already calls
`DeleteLocaleResourcesAsync("Plugins.Misc.Ingredients")` (prefix-based) — sweeps the new key
automatically, no change needed. `Public/Views/Components/Ingredients.cshtml:18`: replace
`<strong>@T("Plugins.Misc.Ingredients.Ingredients"):</strong>` with
`<strong>@T("Plugins.Misc.Ingredients.PublicWidget.Title")</strong>` (drop the trailing colon — the
mockup's panel heading is a standalone eyebrow label, not a "label: value" pair).

**ServingSuggestions** — `ServingSuggestionsPlugin.cs` `InstallAsync` dictionary: add
`["Plugins.Misc.ServingSuggestions.PublicWidget.Title"] = "Komentarz Joanny Nycz"`. `UninstallAsync`
already calls `DeleteLocaleResourcesAsync("Plugins.Misc.ServingSuggestions")` — same prefix sweep.
`Public/Views/Components/ServingSuggestion.cshtml`: insert a new `.heading` block above the existing
`.title` block:
```cshtml
<div class="product-serving-suggestion">
    <div class="heading">
        <strong>@T("Plugins.Misc.ServingSuggestions.PublicWidget.Title")</strong>
    </div>
    <div class="title">
        <strong>@Model.Title</strong>
    </div>
    ...
```

**Theme CSS — `Content/css/product-details.css`** (new file, created once GIL-003-01 exists), scoped
under the two root classes exactly as `DefaultClean` already scopes `.product-collateral .title`:

```css
.product-ingredients { margin: 0 0 24px; }
.product-ingredients .title {
  font-family: var(--gil-font-sans); font-weight: 700; font-size: 12px; letter-spacing: .05em;
  text-transform: uppercase; color: var(--gil-color-sage); margin: 0 0 6px;
}
.product-ingredients .value {
  font-family: var(--gil-font-sans); font-size: 13px; line-height: 1.6; font-weight: 500;
  color: var(--gil-color-ink); padding: 14px; background: var(--gil-color-linen);
  border: 1px solid var(--gil-color-linen-dark); border-radius: 12px;
}
.product-serving-suggestion { margin: 24px 0; font-family: var(--gil-font-sans); }
.product-serving-suggestion .heading {
  font-weight: 700; font-size: 12px; letter-spacing: .05em; text-transform: uppercase;
  color: var(--gil-color-sage); margin: 0 0 4px;
}
.product-serving-suggestion .title {
  font-family: var(--gil-font-serif); font-weight: 700; font-size: 18px; color: var(--gil-color-sage);
  margin: 0 0 8px;
}
.product-serving-suggestion .picture img { max-width: 100%; border-radius: 12px; margin: 0 0 12px; }
.product-serving-suggestion .description {
  font-size: 13px; font-style: italic; line-height: 1.6; color: var(--gil-color-muted); margin: 0 0 12px;
}
.product-serving-suggestion .steps { padding-left: 20px; font-size: 13px; line-height: 1.6; color: var(--gil-color-ink); }
```

Registration: already covered by GIL-003-01's pre-registered six-file `Head.cshtml` list.

**Services/Migration/Events/Caching/Permissions:** all N/A, confirmed — no service interface changes, no
`SchemaMigration.cs` change on either plugin (both remain single `MigrationProcessType.Installation`
migrations), no new events, `IngredientClosure` cache untouched, both plugins' permission records
untouched.

### Simplicity check

Two one-line locale-key swaps in two existing views, two `InstallAsync` dictionary additions, and one
new CSS file with two scoped selector blocks. The only addition beyond the spec's literal instructions is
the second new locale key (Ingredients side), justified by the shared-admin-menu-key finding above.

### Blast radius

- `Plugins.Misc.Ingredients.Ingredients` (unchanged): still drives the Admin → Catalog → "Ingredients"
  menu label, unaffected by this Task now that a separate key is used.
- Only `Nop.Plugin.Misc.Omnisend` also targets `ProductDetailsBottom`, rendering an invisible tracking
  `<script>` — no visual collision.
- No other theme currently styles `.product-ingredients`/`.product-serving-suggestion`.

### Installed-store impact

- **Schema/permissions:** no change on either plugin.
- **Locale resources:** two brand-new keys. Fresh install: automatic via `InstallAsync`. This repo's own
  already-installed `develop`: the developer adds both key/value rows once via Admin → Configuration →
  Languages → Resources. The pre-existing `Plugins.Misc.Ingredients.Ingredients` key's value is **not**
  touched, so the admin Ingredients menu label is unaffected.
- **Rollout timing note:** the two `.cshtml` edits are plugin-view changes, not theme-scoped — they take
  effect under whatever theme is currently active as soon as this Task's PR merges, independent of
  GIL-003-01's theme-switch timing. Once the one-time manual resource add is done, the storefront shows
  "Pełna Etykieta" and "Komentarz Joanny Nycz" as plain, unstyled headings under `DefaultClean` before the
  new theme is ever switched on — a net-neutral-to-positive early fix (replaces today's generic English
  label), not a regression, but label text and visual styling roll out on different schedules.
- **Rolling deploy:** safe — no schema, no breaking service-interface change.

**Approved by:** Mateusz Nycz (developer)
**Date:** 2026-09-03
**Revision notes:** Approved as proposed — no open questions were raised by this design. The one
deviation (a new Ingredients-side locale key instead of relabeling the shared admin-menu key) is folded
into §7 above.

## Implementation plan (implementation-planner)

### Files

- **`src/Plugins/Nop.Plugin.Misc.Ingredients/Public/Views/Components/Ingredients.cshtml`** — changed.
  Line 18: `<strong>@T("Plugins.Misc.Ingredients.Ingredients"):</strong>` →
  `<strong>@T("Plugins.Misc.Ingredients.PublicWidget.Title")</strong>` (drop the trailing colon — the
  mockup's heading is a standalone eyebrow, not a "label: value" pair). No other line changes.
- **`src/Plugins/Nop.Plugin.Misc.Ingredients/IngredientsPlugin.cs`** — changed. In `InstallAsync`'s
  `AddOrUpdateLocaleResourceAsync` dictionary, add
  `["Plugins.Misc.Ingredients.PublicWidget.Title"] = "Pełna Etykieta"`. No `UninstallAsync` change — its
  existing `DeleteLocaleResourcesAsync("Plugins.Misc.Ingredients")` prefix sweep covers it. No `plugin.json`
  `Version` bump (no migration added).
- **`src/Plugins/Nop.Plugin.Misc.ServingSuggestions/Public/Views/Components/ServingSuggestion.cshtml`**
  — changed. Insert a new `.heading` div immediately above the existing `.title` div:
  ```cshtml
  <div class="product-serving-suggestion">
      <div class="heading">
          <strong>@T("Plugins.Misc.ServingSuggestions.PublicWidget.Title")</strong>
      </div>
      <div class="title">
          <strong>@Model.Title</strong>
      </div>
      ...
  ```
  Everything from the picture-conditional block onward is unchanged.
- **`src/Plugins/Nop.Plugin.Misc.ServingSuggestions/ServingSuggestionsPlugin.cs`** — changed. In
  `InstallAsync`'s dictionary, add
  `["Plugins.Misc.ServingSuggestions.PublicWidget.Title"] = "Komentarz Joanny Nycz"`. No `UninstallAsync`
  change (existing prefix sweep covers it). No `plugin.json` `Version` bump.
- **`src/Presentation/Nop.Web/Themes/GesILubczyk/Content/css/product-details.css`** — new. Content fixed
  verbatim in the approved Technical design above — copy it exactly, do not re-derive it. **Dependency:**
  `Themes/GesILubczyk/` does not exist yet — blocked on GIL-003-01 landing (its `Head.cshtml` already
  pre-registers this file, so no `Head.cshtml` edit happens in this Task).

No `.csproj` change — `Nop.Web.csproj`'s `<Content Include="Themes\**" .../>` wildcard covers the new
CSS file with no explicit entry needed.

### Order of work

1. `Ingredients.cshtml` locale-key swap + `IngredientsPlugin.cs` `InstallAsync` addition — independent,
   buildable/mergeable now.
2. `ServingSuggestion.cshtml` heading insert + `ServingSuggestionsPlugin.cs` `InstallAsync` addition —
   independent, buildable/mergeable now.
3. `product-details.css` — blocked on GIL-003-01 merging (theme folder + `Head.cshtml` pre-registration).
   No compile-order dependency on steps 1–2, only a merge-sequencing dependency on the sibling Task.

### Tests

None required. No new/changed service method, entity/domain-rule change, `IConsumer<T>`, migration
(`InstallAsync` dictionary literals are not FluentMigrator migrations), controller action, or bug fix —
matches spec §11. Manual verification: browse a product with both an ingredient list and a serving
suggestion, confirm both panels render the new Polish titles.

### Standards skills to load

`localization-standards-check` (before editing either `InstallAsync` dictionary and either `.cshtml`
file — new-key naming, install/uninstall symmetry, already confirmed satisfied by the existing
prefix-sweep `DeleteLocaleResourcesAsync` calls), `theming-standards-check` (before creating
`product-details.css` — asset registration, zone/file names verified against the checkout), `plugin-standards-check`
(before touching either `InstallAsync`/`UninstallAsync` pair — install/uninstall symmetry, no `Version`
bump, no new settings/permissions).

### Gaps in the approved design

None. The design fully determines both plugin edits (exact key names, values, insertion points) and the
CSS file's exact content; the only open item (CSS file creation) is explicitly blocked on GIL-003-01, not
an undetermined design decision.

**Approved by:** Mateusz Nycz (developer)
**Date:** 2026-09-03
**Revision notes:** none — approved as planned.
