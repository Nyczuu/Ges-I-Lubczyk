---
id: GIL-003-01
kind: Task
title: Theme scaffold, design tokens, header & footer restyle
status: Ready
parent: GIL-003
---

# Task — Theme scaffold, design tokens, header & footer restyle

## 1. Business goal & outcome

The storefront runs the stock `DefaultClean` theme with no brand identity. This Task creates the new
`GesILubczyk` theme (the container every sibling Task under [GIL-003](../spec.md) builds into) and gives
it the mockup's palette, typography, header, and footer.

Outcome: the new theme exists, is selectable in Admin → Configuration → Themes, and once active gives
every storefront page the mockup's sage/clay/gold palette and Fraunces/Plus Jakarta Sans typography via
shared tokens; the header (logo, nav, cart button, and a new slim announcement bar) and footer visually
match [`mockup-reference.html`](../mockup-reference.html).

Success: activating the theme and browsing any storefront page shows the new fonts/colors on header and
footer; the announcement bar text is visible site-wide; `print.css` and RTL support (both already shipped
by `DefaultClean`) still resolve correctly from the copy.

## 2. Root cause / current behavior

N/A — not a bug fix.

## 3. Placement — plugin or core?

Theme only, plus one narrow, developer-approved exception: new folder
`src/Presentation/Nop.Web/Themes/GesILubczyk`, copied from `DefaultClean` per
`theming-standards-check`'s "New theme" checklist. No plugin, no `Nop.Core`/`Nop.Data`/`Nop.Services`
change.

**`Nop.Web.Framework` touch, explicitly approved (rule 3):** this Task introduces brand-new locale
resource keys (announcement bar, new footer copy) that never existed before, and a theme has no
`InstallAsync`-style lifecycle to seed them. Per [GIL-003 §4](../spec.md)'s cross-cutting decision, this
Task ships one small, additive `[NopUpdateMigration(..., UpdateMigrationType.Localization)]`-style migration under
`Nop.Web.Framework/Migrations/`, scoped to just its own new keys, Polish only. This is the one place
this Task touches a layer rule 3 gates — confirmed with the developer, not decided here.

## 4. Extension point

Not a plugin interface — the mechanism is nopCommerce's theme-folder convention plus
`ThemeableViewLocationExpander`, which lets a theme override a core view by placing a same-named file
under `Themes/{theme}/Views/...` (confirmed: this is exactly how `DefaultClean` already overrides
`Views/Shared/Head.cshtml` today).

Two narrow overrides, verified against the current checkout rather than assumed from nopCommerce
documentation:

- `Views/Shared/_Header.cshtml` (core file: `src/Presentation/Nop.Web/Views/Shared/_Header.cshtml`,
  containing the logo, `HeaderLinksViewComponent`, and `FlyoutShoppingCartViewComponent`) — overridden to
  add the mockup's announcement bar. **No existing announcement-bar mechanism exists in this fork**
  (grepped for "Announcement"/"NoticeBar"/"TopBar" in `Nop.Web`, no match) — this is new markup in one
  specific block, not a whole-layout fork, per the theming checklist's decision table.
- `Views/Shared/Components/Footer/Default.cshtml` (core: same path under `Nop.Web`) — the theming
  checklist's sanctioned direct-edit case, and confirmed technically supported the same way as the
  `Head.cshtml` precedent. Its real sub-components (`FooterMenuViewComponent`, `SocialButtonsViewComponent`,
  `NewsLetterBoxViewComponent`, `StoreThemeSelectorViewComponent`, the "Powered by nopCommerce" and
  tax/shipping disclaimer notices) are kept and re-columned to the mockup's layout — not replaced by the
  mockup's illustrative link labels, which are not real store content.

The real top navigation is a separate component, `Views/Shared/Components/MainMenu/Default.cshtml`,
invoked from `_Root.cshtml` outside `_Header.cshtml` — this fork drives it from an admin-configurable
`Menu`/`MenuItem` CMS domain (`Nop.Core.Domain.Menus`), not a raw category-tree walk. This Task restyles
`MainMenu`'s rendered output via `header-footer.css` to match the mockup's nav visually; it does not
override `MainMenu/Default.cshtml` unless `ddd-modeler` finds the existing markup structurally
insufficient for the mockup's look. `_Root.cshtml` itself is **not** forked.

## 5. Data model & migration

No entities, no schema change. The only "migration" this Task ships is the additive
`Nop.Web.Framework/Migrations/` localization migration from §3/§7 (new resource keys, no table/column
changes) — not a data-model change.

## 6. Admin & storefront surface

- `theme.json`: `SystemName: GesILubczyk`, `FriendlyName: Gęś i Lubczyk`. Selectable in Admin →
  Configuration → Themes (existing mechanism, no new admin page).
- `Content/css/tokens.css` — the `--gil-*` custom properties fixed in [GIL-003 §4](../spec.md), plus
  `@font-face` rules for self-hosted Fraunces/Plus Jakarta Sans `.woff2` files under `Content/fonts/`.
- `Content/css/header-footer.css` — header and footer styling built on the tokens above.
- Both registered via `NopHtml.AppendCssFileParts` in the theme's own `Head.cshtml` (copied from
  `DefaultClean`'s, `tokens.css` appended first), not a raw `<link>`.
- `_Header.cshtml` override adds the announcement-bar markup; `Footer/Default.cshtml` override
  re-columns the real footer sub-components (menu, social, newsletter, theme selector, disclaimers)
  into the mockup's visual layout.
- `MainMenu/Default.cshtml` output restyled via CSS only (see §4) unless design finds otherwise.
- One additive `Nop.Web.Framework/Migrations/` localization migration (see §3) seeding the
  announcement-bar and new footer-copy resource keys, Polish only.

## 7. Settings, permissions, localization

No new `ISettings` properties, no new permission records.

New locale resource keys are needed for the announcement-bar text and any new footer copy the mockup
introduces (e.g. address line, legal-link labels) — no hardcoded literal per `localization-standards-check`.
**Resolved (developer, per [GIL-003 §4](../spec.md)):** seeded via this Task's own additive
`Nop.Web.Framework/Migrations/` localization migration (§3), Polish only — this store runs one language.
`ddd-modeler` picks the exact migration version/file name by mirroring the existing
`UpgradeTo500/LocalizationMigration.cs` precedent; the mechanism itself is fixed here, not open.

## 8. Events & scheduled tasks

N/A — no events, no scheduled tasks.

## 9. Caching

N/A — static theme assets (CSS, fonts, views); no new cached data, no coherence concern across ECS
instances.

## 10. Failure scenarios

N/A — no external dependency in the new theme (self-hosting the fonts specifically removes the Google
Fonts CDN network-failure mode the mockup's own `<link>` approach would have introduced).

## 11. Test scenarios

N/A — reason: `testing-standards-check` governs C#/service-layer automated tests; this Task is Razor
markup and CSS with no service-layer behavior to unit test. Verification is manual/visual per
`theming-standards-check`: activate the theme and browse the home, a category, a product, and the cart
page, confirming header/footer/tokens render correctly and `styles.rtl.css`/`print.css` still resolve.

## 12. Documentation impact

N/A — reason: no `BusinessLogic`/`Glossary` concept introduced; purely a visual theme with no business
rule to document.

## 13. Deployment & rollout

No `Dockerfile`/`appsettings` change — the new theme's static files ship in the same image as today.
Per [GIL-003 §7](../spec.md), the theme is not switched to active in Admin until all sibling Tasks
(GIL-003-02 through GIL-003-05) have merged, so customers never see a half-styled site.
