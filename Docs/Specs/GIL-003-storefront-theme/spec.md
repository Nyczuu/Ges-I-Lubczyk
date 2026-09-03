---
id: GIL-003
kind: Epic
title: Storefront theme reskin to Gęś i Lubczyk brand
status: In Progress
---

# Epic — Storefront theme reskin to Gęś i Lubczyk brand

A group of related Tasks delivering one coherent capability. Each child Task gets its own directory
under this one, with its own `spec.md` from [`TEMPLATE-task.md`](../TEMPLATE-task.md). Mirrors the Epic
checklist in `.claude/agents/spec-intake.md`.

## 1. Business goal & outcome

The storefront currently runs the stock `DefaultClean` theme with no brand identity. The developer
produced an approved visual mockup, preserved verbatim at
[`mockup-reference.html`](mockup-reference.html) (static HTML/Tailwind, single file), expressing the
intended brand: a herbal/artisanal look (sage-green, clay, gold palette; Fraunces + Plus Jakarta Sans
typography) themed around the "Gęś i Lubczyk" jarred-food brand. Each child Task's `ddd-modeler` step
consults this file directly for exact markup, spacing and copy — not just the extracted colors and
font names in Section 4.

Outcome: a new nopCommerce theme, installed and active, that gives every real customer-facing storefront
page (home, catalog, product details, cart) the mockup's look — built on nopCommerce's actual catalog,
cart and product mechanisms, not the mockup's static fake product array and client-only cart state.

Success is verified by: browsing the live site with the new theme active and finding the home page,
a category listing, a product details page, and the mini-cart visually matching the mockup's palette,
typography and layout language. Acceptance uses whatever product/category data exists in the store at
the time (nopCommerce's own sample/demo catalog is sufficient) — populating real "Gęś i Lubczyk" products
is explicitly out of scope (Section 2) and is not a precondition for closing this Epic.

## 2. Scope & boundaries

**In scope**

- One new theme (`Content/`, `Views/Shared` overrides), copied from `DefaultClean` per
  `theming-standards-check`'s "New theme" checklist, carrying the mockup's palette and typography as
  reusable tokens.
- Header (top announcement bar, logo, real `MainMenu` navigation, cart button) and footer restyle,
  keeping this build's real components (`MainMenuViewComponent`; footer's `FooterMenuViewComponent`,
  `SocialButtonsViewComponent`, `NewsLetterBoxViewComponent`, `StoreThemeSelectorViewComponent`, and its
  "Powered by nopCommerce"/tax-disclaimer notices) re-styled to the mockup's visual layout — the
  mockup's specific nav/footer link labels are illustrative, not real store content to reproduce
  verbatim. Footer is a sanctioned direct-edit per the theming checklist.
- Homepage hero and brand-story content (mockup's "Dlaczego weki" / "Czysty skład" sections) delivered
  through a real nopCommerce content mechanism (Topic, widget zone, or similar — the exact mechanism is
  a `ddd-modeler` decision for that child Task, not fixed here).
- Catalog / category listing restyle: product box card matching the mockup, using real category
  navigation instead of the mockup's client-side category filter buttons.
- Product details page restyle, including styling the two already-existing content plugins so their
  output matches the mockup's per-product panels:
  - `Nop.Plugin.Misc.Ingredients` → its `IngredientsViewComponent` output (`.product-ingredients`,
    rendered into `PublicWidgetZones.ProductDetailsBeforeCollateral`) styled as the mockup's
    "Pełna Etykieta" panel.
  - `Nop.Plugin.Misc.ServingSuggestions` → its `ServingSuggestionViewComponent` output
    (`.product-serving-suggestion`, rendered into `PublicWidgetZones.ProductDetailsBottom`) styled as
    the mockup's "Komentarz Joanny Nycz" panel.
  - New Polish locale resources for both plugins' block titles/labels (currently generic English
    strings) — added to those plugins' existing locale resources, not a new duplicate view component.
- Mini-cart restyle to match the mockup's slide-over drawer, built on this build's real flyout mini-cart
  (`FlyoutShoppingCart`, confirmed present and gated by `ShoppingCartSettings.MiniShoppingCartEnabled`),
  including a real free-shipping progress indicator wired to `ShippingSettings.FreeShippingOverXEnabled`/
  `FreeShippingOverXValue` — hidden when that setting is off, never a client-invented value.
- Fraunces and Plus Jakarta Sans web fonts, self-hosted and registered through `NopHtml`, replacing the
  mockup's Google Fonts CDN `<link>`.

**Out of scope**

- Product catalog data entry — the mockup's six example dishes (names, prices, descriptions,
  ingredients) are placeholder content for the visual design. Populating real products is store-owner
  content work done in Admin, not part of this Epic, unless a later decision says otherwise.
- The mockup's quick-view interaction (clicking "Skład i notatki z kuchni Joanny →" on a listing card
  opens ingredients/serving-suggestion in a popup without navigating away). Default assumption: this
  Epic keeps nopCommerce's normal pattern of navigating to the full product details page. A JS
  quick-view/modal pattern is a separate, explicitly-scoped future task if wanted.
- Checkout, account pages, admin UI, and any storefront page the mockup does not depict — these keep
  the current theme's look unless a child Task states otherwise.
- Reimplementing cart logic — the real cart/basket endpoints are restyled, not replaced with the
  mockup's client-only cart-state JavaScript.

## 3. Task breakdown

Proposed — epic-scoped IDs per [`Docs/Specs/README.md`](../README.md) (never a new top-level `GIL-<n>`,
which could collide with a standalone ticket assigned concurrently by another session — as happened
during this Epic's own drafting, when `GIL-004` was independently claimed by an unrelated standalone
Task before this list was corrected):

- `GIL-003-01` — Theme scaffold, design tokens, header & footer restyle
- `GIL-003-02` — Homepage brand-story content (hero + "Dlaczego weki" + "Czysty skład" sections)
- `GIL-003-03` — Catalog & product listing restyle (product box, category navigation)
- `GIL-003-04` — Product details restyle, including styling the Ingredients and ServingSuggestions
  plugin output and their new locale resources
- `GIL-003-05` — Mini-cart restyle

## 4. Cross-cutting constraints

- **Theme identity:** `SystemName: GesILubczyk`, `FriendlyName: Gęś i Lubczyk`. One theme, copied from
  `DefaultClean`, not a fork of shared layouts.
- **CSS approach:** hand-authored CSS custom properties and selectors, split across one file per child
  Task instead of one shared stylesheet, so `GIL-003-02`–`GIL-003-05` can land in parallel without
  overlapping edits to the same file:
  - `Content/css/tokens.css` — `GIL-003-01`, the design tokens below plus `@font-face` rules. Every
    other file only consumes these custom properties, never redefines a color or font.
  - `Content/css/header-footer.css` — `GIL-003-01`.
  - `Content/css/home.css` — `GIL-003-02`.
  - `Content/css/catalog.css` — `GIL-003-03`.
  - `Content/css/product-details.css` — `GIL-003-04`.
  - `Content/css/mini-cart.css` — `GIL-003-05`.

  All registered through `NopHtml.AppendCssFileParts` in the theme's `Head.cshtml`, `tokens.css` first.
  No Tailwind CDN script and no new client-side CSS build pipeline are introduced by this Epic — the
  mockup's Tailwind CDN usage is a prototyping tool, not a decision that carries into the theme.
- **Design tokens** (fixed for every child Task, sourced from the mockup's `tailwind.config`, exposed as
  CSS custom properties with the `--gil-` prefix defined once in `tokens.css`):
  - Colors — `--gil-color-sage: #455E4F`, `--gil-color-sage-light: #597564`,
    `--gil-color-sage-soft: #E8EEE9`, `--gil-color-leaf: #8BA593`, `--gil-color-cream: #FCFAF6`,
    `--gil-color-linen: #F6F2EB`, `--gil-color-linen-dark: #E9E2D5`, `--gil-color-clay: #C57056`,
    `--gil-color-gold: #CD9752`, `--gil-color-ink: #2A332E`, `--gil-color-muted: #68776F`.
  - Fonts — `--gil-font-serif: 'Fraunces', serif` (headings), `--gil-font-sans: 'Plus Jakarta Sans',
    sans-serif` (body).
  - Every child Task references these variables (e.g. `color: var(--gil-color-sage)`); none hardcodes
    a hex value or font-family string outside `tokens.css`.
- **Font hosting:** self-hosted `.woff2` files under the theme's `Content/fonts/`, loaded via
  `@font-face` in the theme's own stylesheet — no external Google Fonts request at runtime.
- **Content-plugin ownership stays put:** GIL-003-04 styles and relabels the existing `Ingredients` and
  `ServingSuggestions` plugins' output in their existing widget zones; it does not create competing
  view components or move their data model.
- **No hardcoded strings:** every user-facing string introduced by this Epic goes through locale
  resources, per `localization-standards-check` — including the two plugins' new block-title resources.
- **Locale-resource seeding for brand-new, theme-owned keys (developer-approved exception to "no core
  touch"):** a **theme** has no `InstallAsync`/`UninstallAsync` lifecycle to seed a locale resource key
  that never existed before (the announcement bar, any new footer copy). The only mechanism this
  codebase already has for exactly that situation is the core FluentMigrator pattern nopCommerce itself
  uses for version-to-version resource additions (`[NopUpdateMigration(..., UpdateMigrationType.Localization)]`, precedent:
  `Nop.Web.Framework/Migrations/UpgradeTo500/LocalizationMigration.cs`). A child Task that introduces a
  **brand-new, theme-owned** resource key (one with no plugin `InstallAsync` to seed it) ships its own
  small migration in `Nop.Web.Framework/Migrations/` for just its own keys (never one shared migration
  file, for the same parallel-merge reason as the CSS split above). This is a deliberate, explicitly
  developer-approved exception to this Epic otherwise being theme/plugin-only — rule 3 of
  `00-system-instructions.md` is satisfied by that approval, not silently bypassed.

  This migration requirement does **not** apply to a **plugin-owned** key, new or existing:
  `Ingredients`/`ServingSuggestions` already have their own `InstallAsync`/`UninstallAsync` lifecycle
  (per "Content-plugin ownership stays put" above), so a brand-new key on either plugin is added to that
  plugin's own `InstallAsync` dictionary — automatic on a fresh install — plus, on an environment where
  the plugin is already installed (this repo's own `develop`), a one-time manual Admin add of that same
  resource, exactly like a value change on an already-existing key (e.g. rewording the shared "Add to
  cart" button text, or relabeling an already-installed `Ingredients` resource). Both cases are a content
  edit via Admin → Configuration → Languages → Resources on an existing install, never a
  `Nop.Web.Framework` migration — the migration mechanism above exists solely for keys with **no** owning
  plugin lifecycle at all.
- **Languages:** this store has both Polish and English configured, and **English is a live,
  Published, customer-selectable working language** (confirmed by the developer after GIL-003-01's
  post-implementation review found the risk: seeding a theme-owned key for Polish only means an
  English-working-language customer sees the raw resource key text plus a per-page-view warning log,
  per `LocalizationService.GetResourceAsync`'s documented missing-key fallback). Any new theme-owned
  locale key introduced by this Epic (via the `[NopUpdateMigration(...)]` mechanism above) must seed
  initial text for **every currently-Published language** on this store, not Polish only — looping over
  `languageService.GetAllLanguagesAsync(true)` (or the subset that is `Published`), not a single
  hardcoded `UniqueSeoCode == "pl"` lookup. Each language gets its own reasonable initial copy (a Polish
  string for `pl`, an English one for `en`), both store-owner-editable afterward via Admin, same as a
  Polish-only key would have been. This supersedes this Epic's earlier "Polish only" framing.
- **Store-owner content changes are manual admin steps, not code:** the `HomepageText` Topic's body
  (GIL-003-02) and any category/menu content the header/footer/catalog restyle needs (GIL-003-01,
  GIL-003-03) are edited directly in Admin as part of rollout, coordinated with the single theme-switch
  in Section 7 — never a data migration overwriting store content.
- **No new client-side cart/product state:** "add to cart", quantities, and totals go through the real
  nopCommerce cart endpoints; no reimplementation of the mockup's in-memory JS cart or product array.

## 5. Sequencing & dependencies

1. `GIL-003-01` (theme scaffold + tokens + header/footer) lands first — every other Task depends on the
   theme existing and the palette/font tokens being defined once, in one place.
2. `GIL-003-02`, `GIL-003-03`, `GIL-003-04`, `GIL-003-05` each depend only on `GIL-003-01` and are
   independent of each other — they touch disjoint pages/components, and thanks to the per-Task CSS
   file split in Section 4 they also touch disjoint files, so they can proceed and merge in parallel
   once `GIL-003-01` merges.

## 6. Data & migration strategy across the Epic

No new entities or schema changes. The two content plugins' schemas already shipped in GIL-001 and
GIL-002. Any locale-resource key on either plugin — value change on an existing key, or a brand-new one —
is seeded through that plugin's own `InstallAsync` (automatic on a fresh install) plus, on an already-
installed environment, a one-time manual Admin edit/add. Neither case is a `Nop.Web.Framework` migration.

One narrow, developer-approved exception (Section 4): a child Task that introduces a **theme-owned**
locale resource key — one with no plugin `InstallAsync` to seed it, because a theme has no install
lifecycle at all — ships its own small, additive `[NopUpdateMigration(..., UpdateMigrationType.Localization)]`-style migration in
`Nop.Web.Framework/Migrations/`, scoped to just that Task's new keys, seeded for every currently-Published
language on this store (Section 4) — not Polish only. This is the one place this Epic touches
`Nop.Web.Framework` — confirmed with the developer (rule 3 of
`00-system-instructions.md`), not decided unilaterally.

## 7. Deployment & rollout strategy

Default assumption: ship as one theme switch in Admin → Configuration → Themes once all child Tasks
have merged, so customers never see a half-styled site. No Dockerfile, appsettings, or ECS task
definition changes — theme files ship in the same image as today, per
[`ai-harness/04-deployment-aws-ecs.md`](../../ai-harness/04-deployment-aws-ecs.md).
