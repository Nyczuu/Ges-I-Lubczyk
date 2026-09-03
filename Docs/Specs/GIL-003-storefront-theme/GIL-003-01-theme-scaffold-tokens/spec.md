---
id: GIL-003-01
kind: Task
title: Theme scaffold, design tokens, header & footer restyle
status: Shipped
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
`Nop.Web.Framework/Migrations/`, scoped to just its own new keys, seeded for every language this store
has Published (Polish and English — corrected post-implementation, see the Technical design below; not
Polish only as first drafted). This is the one place this Task touches a layer rule 3 gates — confirmed
with the developer, not decided here.

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
  announcement-bar and new footer-copy resource keys, for every Published language on this store.

## 7. Settings, permissions, localization

No new `ISettings` properties, no new permission records.

New locale resource keys are needed for the announcement-bar text and any new footer copy the mockup
introduces (e.g. address line, legal-link labels) — no hardcoded literal per `localization-standards-check`.
**Resolved (developer, per [GIL-003 §4](../spec.md)):** seeded via this Task's own additive
`Nop.Web.Framework/Migrations/` localization migration (§3), for every Published language this store has
(Polish and English — corrected post-implementation after review found English is live and
customer-selectable, not "Polish only" as first assumed).
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

## Technical design (ddd-modeler)

### Corrections to the spec's technical assumptions

- **Spec (§7): "new locale resource keys are needed for... e.g. address line"** — Actually `Store`
  already carries `CompanyAddress`/`CompanyPhoneNumber` (`src/Libraries/Nop.Core/Domain/Stores/Store.cs:74,79`),
  admin-editable today at Admin → Configuration → Stores
  (`src/Presentation/Nop.Web/Areas/Admin/Views/Store/_CreateOrUpdate.Info.cshtml:96-109`). These fields
  are not currently rendered anywhere in the storefront, but they are the correct source for the
  mockup's footer address/phone line — not a new locale resource, and not covered by this Task's
  migration.

- **Spec (§7): "...and legal-link labels" need new locale resources** — Actually `FooterMenuViewComponent`
  already renders admin-configurable content via the same `Menu`/`MenuItem` CMS domain that drives
  `MainMenu`, filtered to `MenuType.Footer` (`src/Presentation/Nop.Web/Components/FooterMenuViewComponent.cs:29`,
  `src/Libraries/Nop.Core/Domain/Menus/MenuType.cs`). Footer link labels (including anything like the
  mockup's "Polityka czystego składu" / "Regulamin sklepu") are store-owner content edited in Admin
  through that existing menu, not a hardcoded string this Task seeds via migration.

- **Spec (§4): "Two narrow overrides" (`_Header.cshtml`, `Footer/Default.cshtml`) is incomplete for the
  mockup's logo lockup.** The real `Logo/Default.cshtml`
  (`src/Presentation/Nop.Web/Views/Shared/Components/Logo/Default.cshtml`) renders only
  `<a><img></a>` — no monogram badge, no store-name/tagline text block. CSS alone cannot manufacture the
  mockup's "circular G&L badge + two-line brand name/tagline" from that markup.

### Deviations from the spec's stated approach

None — the corrections above are corrections (spec's factual claims were wrong), not chosen departures
from an otherwise-correct approach.

### Placement

Theme only: `src/Presentation/Nop.Web/Themes/GesILubczyk/`, copied from `DefaultClean`. Confirmed theme
discovery is a pure folder-scan for `theme.json` (`src/Libraries/Nop.Services/Themes/ThemeProvider.cs`,
`NopThemeDefaults.ThemesPath`/`ThemeDescriptionFileName`) — no registry file, no other registration step.

Plus the one already developer-approved `Nop.Web.Framework` touch (rule 3): an additive
localization-only migration, since a theme has no `InstallAsync` to seed a brand-new resource key. No
plugin interface applies here — this is nopCommerce's theme-folder + `ThemeableViewLocationExpander`
convention.

### Domain model

N/A — no new entity, no schema change. The migration only inserts rows into the existing
`LocaleStringResource` table.

### Extension decision

| Data | Mechanism | Why |
|---|---|---|
| Footer address/phone | Existing `Store.CompanyAddress`/`CompanyPhoneNumber` | Already exists, already admin-editable, exactly the shape needed (per-store, not per-language) |
| Footer link labels (legal, catalog links) | Existing `Menu`/`MenuItem` (`MenuType.Footer`) via `FooterMenuViewComponent` | Already the store's real footer-content mechanism |
| Announcement-bar text | New `Nop.Web.Framework/Migrations/UpgradeTo500/` localization-only update migration, `[NopUpdateMigration(..., UpdateMigrationType.Localization)]` | The one mechanism a theme has for a resource key with **no** owning install lifecycle — per Epic §4's explicit, already-confirmed exception. `GenericAttribute` rejected: static per-store-language site copy, not keyed per-entity data. A new theme `ISettings` class rejected: Task §7 rules out new settings, and settings aren't the localizable-string mechanism. A raw string literal in the view rejected: violates the no-hardcoded-literal rule. |

### Design

**Copy step** (theming-standards-check "New theme"): copy `DefaultClean` verbatim, then modify only what's
listed below.

```
src/Presentation/Nop.Web/Themes/GesILubczyk/
├── theme.json                         (SystemName: GesILubczyk, FriendlyName: "Gęś i Lubczyk")
├── preview.jpg                        (new asset)
├── Views/
│   ├── _ViewImports.cshtml            (copied verbatim — Razor resolves @inherits/@using from the
│   │                                    physical file location; DefaultClean ships this exact file)
│   └── Shared/
│       ├── Head.cshtml                (copied + modified — see below)
│       ├── _Header.cshtml             (new override — announcement bar only)
│       └── Components/
│           └── Footer/Default.cshtml  (new override — re-column real sub-components)
└── Content/
    ├── css/
    │   ├── styles.css, styles.rtl.css, print.css   (copied verbatim, unmodified)
    │   ├── tokens.css                 (new — --gil-* custom properties + @font-face)
    │   └── header-footer.css          (new)
    ├── fonts/                         (new — self-hosted Fraunces + Plus Jakarta Sans .woff2)
    └── images/                        (copied verbatim; logo.png replaced with the branded lockup)
```

**`Head.cshtml`**: keep `DefaultClean`'s existing registrations (`styles(.rtl).css`, swiper, jquery-ui —
needed for `_Print.cshtml`'s print-CSS reference and `Html.ShouldUseRtlThemeAsync()` to keep resolving).
**Pre-register all six of the Epic's CSS files now** (developer-approved — the four not yet created by
sibling Tasks 404 harmlessly, invisible to real traffic since the theme isn't switched live until every
sibling merges, per Epic §7/Task §13):

```csharp
NopHtml.AppendCssFileParts($"~/Themes/{themeName}/Content/css/tokens.css");
NopHtml.AppendCssFileParts($"~/Themes/{themeName}/Content/css/header-footer.css");
NopHtml.AppendCssFileParts($"~/Themes/{themeName}/Content/css/home.css");
NopHtml.AppendCssFileParts($"~/Themes/{themeName}/Content/css/catalog.css");
NopHtml.AppendCssFileParts($"~/Themes/{themeName}/Content/css/product-details.css");
NopHtml.AppendCssFileParts($"~/Themes/{themeName}/Content/css/mini-cart.css");
```
Order matters: `styles.css` must load first so these override files' selectors win the cascade against
`DefaultClean`'s structural rules; `tokens.css` first among the six so every other file can consume its
custom properties.

**`_Header.cshtml`**: copy the core file verbatim (`src/Presentation/Nop.Web/Views/Shared/_Header.cshtml`),
keep every existing `Component.InvokeAsync` call (`TaxTypeSelectorViewComponent`,
`CurrencySelectorViewComponent`, `LanguageSelectorViewComponent`, `HeaderLinksViewComponent`,
`FlyoutShoppingCartViewComponent`, `LogoViewComponent`, `SearchBoxViewComponent`, and all four
`WidgetViewComponent` zone calls), and prepend one new block before `<header class="header">`:
```cshtml
<div class="gil-announcement-bar">@T("Header.AnnouncementBar.Text")</div>
```
Confirmed the mechanism is real: `_Root.cshtml` renders this file via `Html.RenderPartialAsync("_Header")`,
subject to `ThemeableViewLocationExpander`, which injects `/Themes/{theme}/Views/Shared/{0}.cshtml` ahead
of the core search path. No announcement-bar mechanism exists to reuse (grepped
`Announcement`/`NoticeBar`/`TopBar` under `Nop.Web`; only an unrelated CLDR JSON data file matched).

**`Footer/Default.cshtml`**: copy the core view, keep all real component calls (`FooterMenuViewComponent`,
`SocialButtonsViewComponent`, `NewsLetterBoxViewComponent`, `StoreThemeSelectorViewComponent`) and the
tax/shipping disclaimer + "Powered by nopCommerce" blocks, re-columned into the mockup's grid via new
wrapper markup/CSS classes. **Resolved scope (developer): footer content is real components + `Store`
fields only — no invented brand-story prose** (the mockup's "Rzemieślnicza wekownia założona przez
Joannę Nycz..." prose belongs to GIL-003-02's homepage scope, not here). For the address/phone line,
inject `IStoreContext` directly in the view and call `GetCurrentStoreAsync()` — mirrors the file's own
existing pattern (it already injects `IWorkContext` and calls `GetTaxDisplayTypeAsync()` inline) rather
than growing `FooterModel`/`CommonModelFactory` (an additional, unapproved core touch). Render the
address/phone block conditionally on non-empty values — an installed store with blank
`CompanyAddress`/`CompanyPhoneNumber` must not show an empty block.

**Header logo lockup — resolved (developer): zero-code image swap.** Replace `Content/images/logo.png`
with a custom-designed image containing the full lockup (monogram + name + tagline). This is
nopCommerce's own existing fallback path — `CommonModelFactory.PrepareLogoModelAsync` falls back to
exactly `Themes/{theme}/Content/images/logo.png` when no `LogoPictureId` is uploaded
(`src/Presentation/Nop.Web/Factories/CommonModelFactory.cs:296-307`). No view override, no new locale
keys.

**Fonts**: `tokens.css` defines `:root { --gil-color-*, --gil-font-serif: 'Fraunces', serif,
--gil-font-sans: 'Plus Jakarta Sans', sans-serif }` plus `@font-face` rules pointing at `../fonts/*.woff2`
(relative to `Content/css/`). Sourcing/optimizing the actual `.woff2` binaries for the weights the
mockup uses (Fraunces 300/500/600/700 + italic 400; Plus Jakarta Sans 300/400/500/600/700) is an
implementation-plan work item.

**Migration — corrected after post-implementation review (reviewer + developer).** Two problems were
found in the first-shipped version of this migration, both fixed in the design below:

1. **Blocking (reviewer):** the first version resolved `ILanguageService`/`ILocalizationService` (async
   service interfaces) and blocked on them with `.Result`/`.Wait()` — this codebase's own documented
   anti-pattern (`Docs/knowledge-base/12-coding-standards.md`: "don't wrap a synchronous method with
   `.Result`/`.Wait()` to avoid making a call chain async"). Every sibling migration in this exact folder
   does the equivalent work synchronously via `ISyncCodeHelper`, matching the pattern in
   `src/Presentation/Nop.Web.Framework/Extensions/MigrationExtensions.cs`'s `AddOrUpdateLocaleResource`.
2. **English is a live, Published, customer-selectable language on this store** (confirmed by the
   developer after this same review flagged the risk) — seeding `Header.AnnouncementBar.Text` for Polish
   only means an English-working-language customer sees the raw resource key text plus a per-page-view
   warning log (`LocalizationService.GetResourceAsync`'s documented missing-key fallback). Per the Epic's
   now-corrected §4 "Languages" constraint, the migration must seed every configured language it has
   copy for, not a single hardcoded `"pl"` lookup.

Corrected design, using `ISyncCodeHelper` throughout (no `.Result`/`.Wait()` on any service call — the
only `.Wait()` left is on `IStaticCacheManager.RemoveByPrefixAsync`, which has no synchronous counterpart,
matching the exact same exception the codebase's own `AddOrUpdateLocaleResource` extension already makes)
and a **pure, unit-testable** language-resolution step (closing `test-engineer`'s gap that the original
version's `polishLanguageId is null` branch had no injectable seam to test):

```csharp
namespace Nop.Web.Framework.Migrations.UpgradeTo500;

[NopUpdateMigration("2026-09-03 09:01:00", "5.00", UpdateMigrationType.Localization)]
public class GilThemeAnnouncementBarLocalizationMigration : MigrationBase
{
    private const string ResourceName = "Header.AnnouncementBar.Text";

    private static readonly IDictionary<string, string> ValuesByTwoLetterCode = new Dictionary<string, string>
    {
        ["pl"] = "<initial Polish placeholder, store-owner-editable afterward>",
        ["en"] = "<initial English placeholder, store-owner-editable afterward>",
    };

    public override void Up()
    {
        if (!DataSettingsManager.IsDatabaseInstalled())
            return;

        var syncCodeHelper = EngineContext.Current.Resolve<ISyncCodeHelper>();
        var staticCacheManager = EngineContext.Current.Resolve<IStaticCacheManager>();

        var languages = syncCodeHelper.GetAllLanguages(true);
        var resourcesToSeed = ResolveResourcesToSeed(languages, ValuesByTwoLetterCode);

        if (!resourcesToSeed.Any())
        {
            EngineContext.Current.Resolve<ILogger>().WarningAsync(
                $"{nameof(GilThemeAnnouncementBarLocalizationMigration)}: no matching language found for " +
                $"{ResourceName}, skipping resource seed.").Wait();
            return;
        }

        var existingByLanguageId = syncCodeHelper.GetAllEntities<LocaleStringResource>(query =>
                query.Where(r => r.ResourceName.ToLower() == ResourceName.ToLowerInvariant()
                    && resourcesToSeed.Keys.Contains(r.LanguageId)))
            .ToDictionary(r => r.LanguageId);

        var toInsert = new List<LocaleStringResource>();
        var toUpdate = new List<LocaleStringResource>();

        foreach (var (languageId, value) in resourcesToSeed)
        {
            if (existingByLanguageId.TryGetValue(languageId, out var existing))
            {
                if (existing.ResourceValue == value)
                    continue;

                existing.ResourceValue = value;
                toUpdate.Add(existing);
            }
            else
            {
                toInsert.Add(new LocaleStringResource
                {
                    LanguageId = languageId,
                    ResourceName = ResourceName.ToLowerInvariant(),
                    ResourceValue = value
                });
            }
        }

        if (toInsert.Any())
            syncCodeHelper.InsertEntities(toInsert);

        if (toUpdate.Any())
            syncCodeHelper.UpdateEntities(toUpdate);

        staticCacheManager.RemoveByPrefixAsync(NopEntityCacheDefaults<LocaleStringResource>.Prefix).Wait();
    }

    /// <summary>
    /// Pure, no EngineContext/DB access — unit-testable directly. Maps each language whose two-letter
    /// code has a configured value to that value; languages with no configured copy are skipped, not
    /// defaulted to another language's text.
    /// </summary>
    internal static IDictionary<int, string> ResolveResourcesToSeed(
        IList<Language> languages, IDictionary<string, string> valuesByTwoLetterCode)
    {
        return languages
            .Where(language => valuesByTwoLetterCode.ContainsKey(language.UniqueSeoCode))
            .ToDictionary(language => language.Id, language => valuesByTwoLetterCode[language.UniqueSeoCode]);
    }

    public override void Down()
    {
        // do nothing in a fresh installation
    }
}
```

Key naming follows the existing flat `Header.*`/`Footer.*` convention (`Header.SkipNavigation.Text`,
`Footer.FollowUs`), not a `Plugins.{Group}.{Name}.` prefix, since this is a site-wide Header/Footer area,
not a plugin bundle. **Resolved (developer): initial copy per language is the implementer's choice**
(placeholders, store-owner-editable afterward via Admin) — not the mockup's specific batch-number flavor
text, consistent with Epic §2 excluding real content population.

**New test required** (closes `test-engineer`'s gap): a plain NUnit test for
`GilThemeAnnouncementBarLocalizationMigration.ResolveResourcesToSeed` — no `EngineContext`, no DB, no
`ForwardOnlyMigration` base needed, since the method is `static` and pure. Cover: a language whose
`UniqueSeoCode` matches a configured key is included with the right value; a language with no matching
key is excluded (not defaulted); an empty language list returns an empty result.

**No events, no caching, no permissions** — confirmed nothing in this Task touches any of those.

### Simplicity check

Smallest version that satisfies Task §1's literal success criteria: copy `DefaultClean` → rename
`theme.json` → `tokens.css` + `header-footer.css` + font files → one `_Header.cshtml` override
(announcement bar div + CSS-only restyle of unchanged component calls) → one `Footer/Default.cshtml`
override (re-column via CSS/wrapper markup, same real components) → swap `Content/images/logo.png` for a
branded asset → one localization migration for the announcement-bar text. That is exactly the design
above.

### Blast radius

- `ThemeableViewLocationExpander` is shared machinery; adding a second theme doesn't change its behavior
  for `DefaultClean` (resolution is keyed by the currently active theme name at render time).
- `Store.CompanyAddress`/`CompanyPhoneNumber` are read-only from this Task's perspective — nothing else
  in `Nop.Web` currently reads them (confirmed Admin-view-only usage today), so this is the first
  storefront consumer.
- `FooterMenuViewComponent`/`MainMenuViewComponent` and the underlying `Menu`/`MenuItem` domain are
  consumed as-is, unmodified.
- The new migration file is additive and independently versioned; the implementer must pick a timestamp
  that doesn't collide with any existing file in `UpgradeTo500/`, nor with GIL-003-05's own new migration
  in the same folder.

### Installed-store impact

- **Schema**: none. **Settings**: none. **Permissions**: none.
- **Locale resources**: one new key (`Header.AnnouncementBar.Text`) added via the new migration,
  explicitly targeting the Polish language row — additive, no existing key touched.
- **Footer behavior change**: on go-live, a store with non-empty `Store.CompanyAddress`/
  `CompanyPhoneNumber` starts showing that data in the storefront footer for the first time (previously
  Admin-only). Conditional rendering avoids an empty block if those fields are blank.
- **Rolling deploy**: pure additive files (new theme folder, new migration) — safe without downtime. The
  new theme is not switched to active (`StoreInformationSettings.DefaultStoreTheme`) until every sibling
  Task has merged, so no customer sees a half-styled site or a 404'ing not-yet-shipped CSS file during
  the rollout window.

### Key files referenced

`Themes/DefaultClean/` (theme.json, Views/_ViewImports.cshtml, Views/Shared/Head.cshtml, Content/css/*),
`Nop.Web.Framework/Themes/ThemeableViewLocationExpander.cs`, `Nop.Web/Views/Shared/_Root.cshtml`,
`_Header.cshtml`, `Views/Shared/Components/Footer/Default.cshtml`,
`Views/Shared/Components/Logo/Default.cshtml`, `Nop.Web/Factories/CommonModelFactory.cs`
(`PrepareLogoModelAsync`), `Nop.Web/Components/{FooterMenuViewComponent,MainMenuViewComponent}.cs`,
`Nop.Core/Domain/Stores/Store.cs`, `Nop.Core/Domain/Menus/{Menu,MenuItem,MenuType}.cs`,
`Nop.Data/Migrations/{NopUpdateMigrationAttribute,MigrationManager,UpdateMigrationType}.cs`,
`Nop.Web.Framework/Migrations/UpgradeTo500/LocalizationMigration.cs`, `Nop.Web/Views/Shared/_Print.cshtml`.

### Frozen contract with GIL-003-05 (task-decomposer)

`header-footer.css` (this Task) owns logo, `MainMenu` nav, the announcement bar, and footer only. It
does **not** style the header cart trigger (`#topcartlink`/`.ico-cart`/`.cart-qty` in
`HeaderLinks/Default.cshtml`) — that element, including its header appearance (not just the flyout drawer
contents), is owned end-to-end by GIL-003-05's `mini-cart.css`. Both designs referenced "the cart button"
without this split; frozen here to avoid competing CSS on the same element surfacing only at
`epic-integration-auditor` time.

**Approved by:** Mateusz Nycz (developer)
**Date:** 2026-09-03
**Revision notes:** Resolved during Gate 1 — (1) pre-register all six Epic CSS files in `Head.cshtml`
now, per this design's own recommendation; (2) logo lockup via image-swap, not a live-text view override;
(3) migration lands in `Migrations/UpgradeTo500/`, not a new `Migrations/GesILubczyk/` folder; (4) the
migration must explicitly target the Polish `Language` row via `ILocalizationService`, not the
English-hardcoded `AddOrUpdateLocaleResource` FluentMigrator helper — confirmed necessary, this store's
database has an English `Language` row alongside Polish; (4a) Gate 2 addition: the migration must guard
`polishLanguageId is null` (log a warning, skip the seed) rather than pass `null` through — a `null`
`languageId` means "seed every language" per `AddOrUpdateLocaleResourceAsync`'s own semantics, which
would silently reintroduce the wrong-language failure mode this fix exists to prevent, just aimed at
every language instead of English; (5) footer scope is real components + `Store`
fields only, no invented prose; (6) initial announcement-bar copy is the implementer's free choice.

## Implementation plan (implementation-planner)

### Files

- **`theme.json`** — new (mirrors `Themes/DefaultClean/theme.json`): same four fields, `SystemName:
  "GesILubczyk"`, `FriendlyName: "Gęś i Lubczyk"`, `SupportRTL: true`,
  `PreviewImageUrl: "~/Themes/GesILubczyk/preview.jpg"`, `PreviewText: "The 'Gęś i Lubczyk' site theme"`.
- **`preview.jpg`** — new (mirrors `Themes/DefaultClean/preview.jpg`). Cosmetic only, not required by
  `ThemeProvider.InitializeAsync`'s folder-scan.
- **`Views/_ViewImports.cshtml`** — new, byte-for-byte copy of `Themes/DefaultClean/Views/_ViewImports.cshtml`.
- **`Views/Shared/Head.cshtml`** — new (mirrors `Themes/DefaultClean/Views/Shared/Head.cshtml`): keep the
  three existing registrations verbatim, append in this order:
  `tokens.css`, `header-footer.css`, `home.css`, `catalog.css`, `product-details.css`, `mini-cart.css`
  (all via `NopHtml.AppendCssFileParts($"~/Themes/{themeName}/Content/css/{file}")`).
- **`Views/Shared/_Header.cshtml`** — new override (mirrors core `Views/Shared/_Header.cshtml`): copy
  verbatim, prepend `<div class="gil-announcement-bar">@T("Header.AnnouncementBar.Text")</div>` before
  `<header class="header">`.
- **`Views/Shared/Components/Footer/Default.cshtml`** — new override (mirrors core same path): copy
  verbatim (all real component calls, disclaimer, "Powered by" block unchanged), add
  `@inject IStoreContext storeContext`, render a conditional address/phone block from
  `(await storeContext.GetCurrentStoreAsync()).CompanyAddress`/`CompanyPhoneNumber`.
- **`Content/css/{styles,styles.rtl,print}.css`** — new, byte-for-byte copies of `DefaultClean`'s.
- **`Content/css/tokens.css`** — new (no mirror): `:root` block with the eleven `--gil-color-*` and two
  `--gil-font-*` custom properties, plus `@font-face` rules for ten `.woff2` files under `Content/fonts/`.
- **`Content/css/header-footer.css`** — new (no mirror): logo, `MainMenu` output, announcement bar,
  footer only — consumes only `var(--gil-*)`. **Excludes** `#topcartlink`/`.ico-cart`/`.cart-qty` per the
  frozen contract with GIL-003-05 above.
- **`Content/fonts/*.woff2`** — ten new binary assets matching `tokens.css`'s `@font-face` paths.
- **`Content/images/**`** — copy `DefaultClean`'s directory verbatim except `logo.png`, replaced with the
  branded lockup.
- **`Nop.Web.Framework/Migrations/UpgradeTo500/GilThemeAnnouncementBarLocalizationMigration.cs`** — new,
  per the corrected Technical design above: `[NopUpdateMigration("2026-09-03 09:01:00", "5.00",
  UpdateMigrationType.Localization)]`, uses `ISyncCodeHelper` throughout (no `.Result`/`.Wait()` on any
  service call — the Blocking finding from post-implementation review), seeds `Header.AnnouncementBar.Text`
  for every language it has copy for (Polish and English) via the pure, unit-tested
  `ResolveResourcesToSeed` helper, logs-and-skips if no configured language matches.

No `.csproj`/solution-file change anywhere — `Nop.Web.csproj`'s `<Content Include="Themes\**" .../>`
wildcard and `Nop.Web.Framework.csproj`'s implicit `Compile` globbing already cover every new file. No
`plugin.json`/`InstallAsync` — this is a theme, not a plugin. Theme is **not** switched to active as part
of this Task.

### Order of work

1. Copy `Themes/DefaultClean/` → `Themes/GesILubczyk/` verbatim (theme.json, preview.jpg,
   `_ViewImports.cshtml`, `Head.cshtml`, the three CSS files, `Content/images/**`).
2. Edit `theme.json`.
3. Author `tokens.css`; source the ten `.woff2` files under `Content/fonts/`.
4. Edit `Head.cshtml` to append the six `AppendCssFileParts` calls.
5. Author `header-footer.css`.
6. Create `_Header.cshtml` override.
7. Create `Footer/Default.cshtml` override.
8. Replace `Content/images/logo.png`.
9. Add `GilThemeAnnouncementBarLocalizationMigration.cs` (must run against a dev/test DB before step 6's
   bar shows real copy rather than the raw key name).
10. Manual verification: activate theme in a non-production environment, browse home/category/product/
    cart, confirm header/footer/tokens render, `styles.rtl.css`/`print.css` resolve, the four
    not-yet-shipped sibling CSS files 404 harmlessly.

### Tests

**Updated post-implementation:** the Razor/CSS side and the migration's `Up()`/`Down()` orchestration
remain untested for the reasons originally stated (no service-layer behavior; this repo's test harness
structurally excludes `Nop.Web.Framework/Migrations/UpgradeTo500/*` from its migration scan). But the
corrected design extracts `GilThemeAnnouncementBarLocalizationMigration.ResolveResourcesToSeed` as a
`static`, pure method with no `EngineContext`/DB dependency — a plain NUnit test file (e.g.
`src/Tests/Nop.Tests/Nop.Web.Framework.Tests/Migrations/GilThemeAnnouncementBarLocalizationMigrationTests.cs`)
covering the three scenarios listed in the Technical design section is now required; this is not covered
by the original "no sibling migration has ever needed a test" reasoning, since no sibling had branching
logic to test.

### Standards skills to load

`theming-standards-check` (theme copy, `Head.cshtml`, `_Header.cshtml`, `Footer/Default.cshtml`),
`localization-standards-check` (migration file, `@T("Header.AnnouncementBar.Text")` call).

### Gaps in the approved design

None outstanding — the one gap this plan raised (automated-test coverage for the migration) is resolved
above by the harness-scope evidence; the null-language-id guard gap is resolved in the Technical design's
revision notes.

**Approved by:** Mateusz Nycz (developer)
**Date:** 2026-09-03
**Revision notes:** none — approved as planned, with the null-guard code change already folded into the
Technical design section above.

## Post-implementation review — round 1 correction (2026-09-03)

Unit-implementer shipped commit `f1e00ccf4d`; build succeeded, 1142/1142 existing tests passed. The
post-implementation gate found:

- `reviewer` — **Blocking**: the migration used `.Result`/`.Wait()` on async service calls
  (`ILanguageService`/`ILocalizationService`), violating this codebase's documented anti-pattern rule.
  Root-caused to the approved Technical design's own pseudocode, not implementer drift.
- `test-engineer` — non-blocking gap: the migration's null-guard branch had correctness-sensitive logic
  with no injectable seam to test.
- Combined `theming-standards-check`/`localization-standards-check`/`migration-standards-check`/
  `upgrade-safety-detector` — found a live risk: English is confirmed (by the developer) to be a live,
  Published, customer-selectable language on this store, so seeding `Header.AnnouncementBar.Text` for
  Polish only would show English-working-language customers the raw resource key text plus a
  per-page-view warning log.
- `integration-auditor` — no blocking findings; flagged the migration-timestamp-collision-with-GIL-003-05
  check as unverifiable from this single unit (to recheck at epic-integration time).

All three are fixed in the Technical design and Implementation plan sections above (rewritten to use
`ISyncCodeHelper`, seed every Published language via a pure `ResolveResourcesToSeed` method, with a new
required unit test). The Epic's own §4 "Languages" cross-cutting constraint was also corrected to match
(was: Polish-only). Re-dispatched to a fresh `unit-implementer` call for the same unit/worktree.
