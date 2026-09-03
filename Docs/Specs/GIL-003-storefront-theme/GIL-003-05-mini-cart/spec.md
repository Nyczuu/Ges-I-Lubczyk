---
id: GIL-003-05
kind: Task
title: Mini-cart restyle
status: In Progress
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

## Technical design (ddd-modeler)

### Corrections to the spec's technical assumptions

- **"Wired to `IsFreeShippingAsync`'s underlying computation" is compatible with "no other core change"
  only if you accept a less-accurate bar.** `MiniShoppingCartModel` has no free-shipping fields at all,
  and `ShoppingCartModelFactory.PrepareMiniShoppingCartModelAsync` never touches `ShippingSettings`.
  Reusing the real `IsFreeShippingAsync` (which also covers customer-role and per-item free-shipping, not
  just the X-value threshold) requires a small additive extension to this `Nop.Web` model+factory. §11 of
  this Task's own spec already anticipated this; §3 didn't reconcile it with "no other core change." —
  **resolved at Gate 1: the accurate, additive version is approved.**
- **The Epic's designated locale-seeding mechanism silently targets English, not "whatever language this
  store runs."** `MigrationExtensions.AddOrUpdateLocaleResource` resolves its target language via
  `NopCommonDefaults.DefaultLanguageCulture => "en-US"` — hardcoded, not configurable. nopCommerce's
  installer always creates an English `Language` row in addition to whatever culture was selected during
  install. **Confirmed live at Gate 1: this store's database does have an English row alongside Polish**
  — literally reusing the FluentMigrator static helper would silently write this Task's Polish copy into
  the English resource row. This affects GIL-003-01 identically (same shared mechanism), not something
  specific to this Task — both migrations must bypass the helper (see Design).

### Deviations from the spec's stated approach

- **Free-shipping bar computed at the Factory/Model level, not the view** — §11 left the location open;
  placed in `ShoppingCartModelFactory.PrepareMiniShoppingCartModelAsync` because `IOrderTotalCalculationService`
  and `ShippingSettings` are already injected into that factory. Zero new DI wiring needed. **Approved at
  Gate 1** (the accurate option, over the cheaper view-only alternative).
- **Locale migration does not literally reuse `this.AddOrUpdateLocaleResource(...)`** — resolves
  `ILanguageService`/`ILocalizationService` directly and calls
  `ILocalizationService.AddOrUpdateLocaleResourceAsync(dict, languageId)`, explicitly targeting the
  language whose `UniqueSeoCode == "pl"`.
- **A small, theme-scoped `<script>` block is added inside the restyled `FlyoutShoppingCart/Default.cshtml`**
  for the close button — see Design for why this is mechanically necessary, not a scope choice.

### Placement

Theme (`Themes/GesILubczyk/Views/Shared/Components/FlyoutShoppingCart/Default.cshtml`, CSS in
`Content/css/mini-cart.css`), no plugin.

**Two `Nop.Web`/`Nop.Web.Framework` touches, both confirmed at Gate 1:**
1. `src/Presentation/Nop.Web/Models/ShoppingCart/MiniShoppingCartModel.cs` — four additive properties.
2. `src/Presentation/Nop.Web/Factories/ShoppingCartModelFactory.cs` — one additive block (~15 lines)
   inside the existing `if (cart.Any())` branch, using dependencies already injected into the class. No
   new interface, no new service, no new DI registration.
3. **Resolved (developer): the migration lands in `Nop.Web.Framework/Migrations/UpgradeTo500/`**, not a
   new `Migrations/GesILubczyk/` folder — matching GIL-003-01's migration location, and this fork's
   existing precedent of appending non-version-bump files to that folder
   (`RemindersMigration.cs`/`AppSettingsMigration.cs`).

### Domain model / Extension decision

N/A: no new persisted entity; `ShippingSettings`/`FreeShippingOverX*` are pre-existing, unchanged. For
the two genuinely new pieces of state: free-shipping bar data on `MiniShoppingCartModel` is plain
additive view-model properties (not `GenericAttribute`, not schema — nothing to persist, computed
per-request); the new locale keys use the Epic's sanctioned migration mechanism, corrected to target
Polish explicitly. Rejected: `GenericAttribute` (nothing entity-specific to store), a new
`IShippingRateComputationMethod` (this isn't a shipping method, just a merchandising readout of an
existing setting), a new settings class (settings already exist).

### Design

**Theme view** — overrides the core `FlyoutShoppingCart/Default.cshtml`, keeping `<div id="flyout-cart">`
(external contract: `public.ajaxcart.js:159` does `$(AjaxCart.flyoutcartselector).replaceWith(...)` — the
markup is wholesale-replaced on every add-to-cart, so any interaction JS must be re-executable each time,
i.e. embedded inline `<script>`, not `asp-location="Footer"`). Internal structure adds a backdrop element
and drawer panel nested inside `#flyout-cart`, both CSS-driven off the existing `#flyout-cart.active`
class already toggled by `HeaderLinks/Default.cshtml`'s inline script — no change to that trigger or to
`public.ajaxcart.js`.

**The close (X) button needs a few lines of inline JS, not pure CSS** — the existing open/close mechanism
is hover/focus-driven (`mouseenter`/`mouseleave`/`focus`/`focusout` on `.header-upper`), not a
click-toggle. A close button that is a descendant of `#flyout-cart` would, on click, just move focus
*inside* the tracked region — the existing `focusout` check would still find it and keep the drawer open.
A backdrop click needs no JS (clicking outside blurs the focused element, and the existing `focusout`
handler already closes on that); the explicit close button does need one small inline script
(`element.addEventListener('click', () => flyoutCart.classList.remove('active'))`), scoped inside this
theme's own view, not touching `public.ajaxcart.js` or `HeaderLinks/Default.cshtml`. **Accepted as
mechanically necessary at Gate 1.**

**Free-shipping bar — `ShoppingCartModelFactory.PrepareMiniShoppingCartModelAsync`, inside
`if (cart.Any())`, before `return model;`:**

```csharp
if (_shippingSettings.FreeShippingOverXEnabled)
{
    var (_, _, _, freeShippingSubTotalBase, _) = await _orderTotalCalculationService
        .GetShoppingCartSubTotalAsync(cart, _shippingSettings.FreeShippingOverXIncludingTax);

    model.DisplayFreeShippingBar = true;
    model.FreeShippingReached = await _orderTotalCalculationService.IsFreeShippingAsync(cart, freeShippingSubTotalBase);

    if (!model.FreeShippingReached)
    {
        var remainingBase = _shippingSettings.FreeShippingOverXValue - freeShippingSubTotalBase;
        var remaining = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(remainingBase, currentCurrency);
        model.AmountToFreeShipping = await _priceFormatter.FormatPriceAsync(remaining);
        model.FreeShippingProgressPercentage = _shippingSettings.FreeShippingOverXValue <= 0
            ? 0
            : (int)Math.Min(100, Math.Round(freeShippingSubTotalBase / _shippingSettings.FreeShippingOverXValue * 100));
    }
}
```

Deliberately scoped *inside* `if (cart.Any())`: `IsFreeShippingAsync`'s "all cart items are free-shipping"
check is vacuously `true` for an empty list, which would incorrectly report "free shipping reached" for
an empty cart — a real bug to avoid. `freeShippingSubTotalBase`/`FreeShippingOverXValue` are both
primary-store-currency amounts (matching what checkout actually enforces); only the displayed remaining
amount is converted to the customer's working currency.

`MiniShoppingCartModel` additions:
```csharp
public bool DisplayFreeShippingBar { get; set; }
public bool FreeShippingReached { get; set; }
public string AmountToFreeShipping { get; set; }
public int FreeShippingProgressPercentage { get; set; }
```

View: `@if (Model.DisplayFreeShippingBar)` renders the bar; `Model.FreeShippingReached` swaps between the
"reached" message (bar fill 100%, `--gil-color-sage`/`--gil-color-leaf`) and the "remaining" message with
`Model.FreeShippingProgressPercentage`-driven width and `--gil-color-gold` fill — using GIL-003-01's
fixed `--gil-` tokens, never a hardcoded hex.

**Localization** — new file `src/Presentation/Nop.Web.Framework/Migrations/UpgradeTo500/MiniCartLocalizationMigration.cs`
(folder per Gate 1 resolution above):

```csharp
[NopUpdateMigration("2026-09-03 14:00:00", "5.00", UpdateMigrationType.Localization)]
public class MiniCartLocalizationMigration : MigrationBase
{
    public override void Up()
    {
        if (!DataSettingsManager.IsDatabaseInstalled())
            return;

        var languageService = EngineContext.Current.Resolve<ILanguageService>();
        var localizationService = EngineContext.Current.Resolve<ILocalizationService>();

        var polishLanguageId = languageService.GetAllLanguagesAsync(true).Result
            .FirstOrDefault(l => l.UniqueSeoCode == "pl")?.Id;

        localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
        {
            ["ShoppingCart.Mini.FreeShipping.AmountToGo"] = "Do darmowej dostawy brakuje: {0}",
            ["ShoppingCart.Mini.FreeShipping.Reached"] = "Przysługuje Ci darmowa dostawa!"
        }, polishLanguageId).Wait();
    }
}
```

New resource keys, Polish only, matching the existing `ShoppingCart.Mini.*` naming already used by this
same view's other resources (`.NoItems`, `.ItemsText`, `.UnitPrice`, etc.). The "reached" copy is kept
simpler than the mockup's "...w boksie chłodniczym" (cold-chain packaging claim) since that packaging
fact belongs to GIL-003-01's announcement-bar copy, not this Task — not duplicating/assuming a claim
outside this Task's scope.

**Empty-cart message**: unchanged — reuses the existing `ShoppingCart.Mini.NoItems` resource; a brand
voice change is a value edit, no new key, no migration.

**Caching:** none — `ShippingSettings` is already cached via the standard `ISettingService` mechanism; no
new cache key.

### Simplicity check

Smallest version without any core touch: compute the bar entirely in the theme view from
`Model.SubTotalValue` + `@inject ShippingSettings`. Strictly less correct — can't see customer-role or
per-item free shipping, duplicates rather than reuses `IsFreeShippingAsync`'s logic. Gate 1 approved the
slightly larger, additive Factory/Model design because the spec's own §6 named constraint ("wired to
`IsFreeShippingAsync`'s underlying computation, never a fabricated value") can't be fully honored by the
zero-core-touch alternative.

### Blast radius

- `MiniShoppingCartModel`/`PrepareMiniShoppingCartModelAsync` are also used by the stock `DefaultClean`
  theme's own `FlyoutShoppingCart/Default.cshtml` — purely additive properties, so `DefaultClean`'s
  unrestyled view is unaffected.
- Existing test `ShoppingCartModelFactoryTests.CanPrepareMiniShoppingCartModel` asserts nothing about
  free shipping — unaffected. A new test should exercise `DisplayFreeShippingBar`/`FreeShippingReached`/
  `AmountToFreeShipping`/`FreeShippingProgressPercentage` across `FreeShippingOverXEnabled` on/off and
  subtotal above/below `FreeShippingOverXValue`, per spec §11.
- The shared `AddOrUpdateLocaleResource` FluentMigrator static helper itself is untouched — this design
  bypasses it rather than changes its signature, so no other `UpgradeToXXX/LocalizationMigration.cs`
  caller is affected. The English-targeting correction only changes this Task's own (and GIL-003-01's
  own) migration file.
- `HeaderLinks/Default.cshtml` and `public.ajaxcart.js` are unmodified.

### Installed-store impact

- New `MiniShoppingCartModel` properties default to `false`/`0`/`null` — no behavior change until the
  theme is active and its view override reads them.
- The locale migration is additive-only, targets only the Polish language row — no impact on any other
  language, no schema change.
- Rolling deploy: safe — no table lock, no long-running operation, resource seeding only.
- **Coordination requirement:** the `dateTime` argument in `[NopUpdateMigration(...)]` must be unique
  across every sibling GIL-003 migration (GIL-003-01's included) — whoever implements this Task picks a
  timestamp distinct from GIL-003-01's `2026-09-03 00:00:00`.

### Frozen contract with GIL-003-01 (task-decomposer)

This Task's `mini-cart.css` owns the header cart trigger end-to-end
(`#topcartlink`/`.ico-cart`/`.cart-qty` in `HeaderLinks/Default.cshtml`), including its header
appearance, not just the flyout drawer contents. GIL-003-01's `header-footer.css` does not style this
element — frozen to avoid competing CSS on the same element surfacing only at `epic-integration-auditor`
time.

**Approved by:** Mateusz Nycz (developer)
**Date:** 2026-09-03
**Revision notes:** Resolved during Gate 1 — (1) confirmed this store's database has an English
`Language` row alongside Polish, making the AddOrUpdateLocaleResource English-targeting bug live and
real — both this Task's and GIL-003-01's migrations must bypass the helper and target Polish explicitly;
(2) the accurate free-shipping design (additive `MiniShoppingCartModel`/`ShoppingCartModelFactory`
touch) is approved over the cheaper view-only alternative; (3) migration lands in
`Migrations/UpgradeTo500/`, matching GIL-003-01, not a new `Migrations/GesILubczyk/` folder; (4) the
inline close-button `<script>` is accepted as mechanically necessary, not a scope expansion.
