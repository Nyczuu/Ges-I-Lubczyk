---
id: GIL-003-02
kind: Task
title: Homepage brand-story content restyle
status: Shipped
parent: GIL-003
---

# Task — Homepage brand-story content restyle

## 1. Business goal & outcome

The homepage's hero and brand-story sections ("Dlaczego weki?", "Czysty skład") in
[`mockup-reference.html`](../mockup-reference.html) need to render on the real storefront homepage,
using this build's actual homepage content mechanism rather than static markup baked into a layout.

Outcome: `Home/Index` shows a hero section and the manifesto/philosophy content styled to match the
mockup, sourced from the same place the store owner already edits homepage copy today.

Success: browsing `/` with the `GesILubczyk` theme active shows the hero and brand-story content in the
mockup's visual style, editable by the store owner the same way the existing homepage text is.

## 2. Root cause / current behavior

N/A — not a bug fix.

## 3. Placement — plugin or core?

Theme only (CSS + confirming/extending existing homepage content), no plugin, no core change.

## 4. Extension point

Verified against the checkout, not assumed from nopCommerce documentation — this fork's homepage
(`src/Presentation/Nop.Web/Views/Home/Index.cshtml`) already renders exactly the kind of free-form rich
content the mockup needs, through the existing `Topic` mechanism:

- `Home/Index.cshtml:31` renders `TopicBlockViewComponent` with `systemName: "HomepageText"` — a `Topic`
  entity edited in Admin → Content Management → Topics, whose body is arbitrary HTML. This is the
  existing, store-owner-editable place for hero/manifesto copy — **no new plugin or widget is needed for
  this content**, per `entity-extension-check`'s "does an existing mechanism already cover this" question.
- Separate real widget zones also exist on the homepage for future plugin-injected content:
  `HomepageTop` (`home_page_top`), `HomepageBeforeCategories`, `HomepageBeforeProducts`,
  `HomepageBeforeBestSellers`, `HomepageBottom` (`PublicWidgetZones.cs`, `Home/Index.cshtml:30-39`). This
  Task does not use them — the `HomepageText` topic is the right mechanism because the content is
  store-authored prose, not a plugin feature.

## 5. Data model & migration

N/A — reason: `Topic`/`HomepageText` already exists; no schema change. Restructuring its content to carry
the mockup's hero/manifesto/three-pillar markup is a manual Admin → Content Management → Topics content
edit, performed as a coordinated rollout step alongside the single theme-switch in
[GIL-003 §7](../spec.md) — **not a code migration overwriting `Topic.Body`**, per
[GIL-003 §4](../spec.md)'s "store-owner content changes are manual admin steps" constraint. This
sidesteps the risk of a migration clobbering store-owner-customized content, and needs no per-language
variant since this store runs Polish only (Epic §4).

**Resolved: the `Topic` has two independently-rendered fields, not one.** `TopicBlock/Default.cshtml`
renders `Title` inside its own wrapper, before `Body` — `<div class="topic-block-title"><h2>@Model.Title
</h2></div>` (`src/Presentation/Nop.Web/Views/Shared/Components/TopicBlock/Default.cshtml:72-74`; the
class is on the wrapping `<div>`, the `<h2>` itself carries no class) — it does not sit "inside" the body
HTML. The seeded default is the English "Welcome to our store" (`InstallRequiredData.cs`), so the manual
Admin content edit must set **both** fields, not just `Body`: `Title` becomes the mockup's hero headline
text ("Prawdziwy obiad to lekarstwo..."), `Body` carries everything else (subhead, CTA buttons, stat
pills, image panel, the "Dlaczego weki" essay, and the three-pillar grid). `home.css` targets
`.topic-block-title` (the div) or `.topic-block-title h2` (the heading itself) to match the mockup's
`<h1>` treatment rather than hiding it — this is a styling target for GIL-003-02, not dead markup to
suppress.

## 6. Admin & storefront surface

- `Content/css/home.css` (new theme file, per [GIL-003 §4](../spec.md)) — styles the `HomepageText`
  topic's rendered HTML to match the mockup's hero layout (headline, CTA buttons, stat pills, image
  panel) and the "Dlaczego weki" section (portrait/quote card, essay copy, "three pillars" grid).
- The `HomepageText` topic's **body HTML** itself needs restructuring to carry the mockup's sections
  (hero, quote block, three-pillar grid) as real markup for `home.css` to target — this is a content
  change in Admin → Content Management → Topics, not a code change, and is called out here so it isn't
  silently assumed to already exist.
- No new admin page, no new widget zone usage.

## 7. Settings, permissions, localization

No new settings, no new permissions, no new locale resource keys. **Resolved:** every visible string in
the restyled hero/brand-story sections lives inside the `HomepageText` Topic body (data, edited in
Admin), not in Razor markup or CSS — the Epic's "no hardcoded strings" rule is satisfied structurally,
not by an added resource. `ddd-modeler` and `unit-implementer` must not introduce a section
eyebrow/kicker or other label as markup text; if the design needs one, it goes into the Topic body too.
No per-language `Topic` body variant is needed (Polish only, per GIL-003 §4).

## 8. Events & scheduled tasks

N/A.

## 9. Caching

N/A — reason: `Topic` content already goes through this codebase's existing topic caching; this Task
does not change what is cached, only how it is styled.

## 10. Failure scenarios

N/A — no external dependency; an empty/unpopulated `HomepageText` topic degrades to whatever
`TopicBlockViewComponent` already does today (existing behavior, unchanged by this Task).

## 11. Test scenarios

N/A — reason: presentation-only CSS/content change, no service-layer behavior. Manual verification:
browse `/` with the new theme active and confirm the hero and brand-story sections render per the
mockup's layout and tokens.

## 12. Documentation impact

N/A — reason: no new business rule; the `Topic`/`HomepageText` mechanism is pre-existing and undocumented
change to it is purely visual.

## 13. Deployment & rollout

No `Dockerfile`/`appsettings` change. Per [GIL-003 §7](../spec.md), folded into the same single
theme-switch rollout as the other sibling Tasks.

## Technical design (ddd-modeler)

### Corrections to the spec's technical assumptions

- **"`Title` becomes the mockup's hero headline text" (spec §5) implies the headline's visual treatment
  carries over — it cannot.** `Topic.Title` is rendered as `<h2>@Model.Title</h2>` with **no
  `Html.Raw`** (`src/Presentation/Nop.Web/Views/Shared/Components/TopicBlock/Default.cshtml:72-74`), so
  any markup typed into it is HTML-encoded and shows as literal text. The Admin editor confirms this is
  deliberate, not an oversight: `Title` uses a plain `nop-editor` (default single-line text-input
  template), while `Body` explicitly opts into `asp-template="RichEditor"` two lines below it
  (`src/Presentation/Nop.Web/Areas/Admin/Views/Topic/_CreateOrUpdate.Info.cshtml:34` vs. `:43`). The
  mockup's headline is two lines forced by a literal `<br>` with the second line in italic clay via a
  `<span class="italic ... text-herbal-clay">` (`mockup-reference.html:114-117`). None of that — the
  line break or the two-tone italic split — survives in `Title`; it renders flat, single-line,
  single-color.

### Deviation from the spec's stated approach — resolved (developer)

**Spec's approach:** "`Title` becomes the mockup's hero headline text ... `Body` carries everything
else" (§5).

**This design instead:** leave `Topic.Title` **empty** and move the entire headline — including its
`<br>` line break and the italic-clay accent span — into the start of `Body` as raw HTML (an
`<h1 class="gil-hero-title">`). `TopicBlock/Default.cshtml:70` already skips rendering
`.topic-block-title` entirely when `Title` is empty (`@if (!string.IsNullOrEmpty(Model.Title))`), so this
degrades cleanly with no dead markup. **Approved at Gate 1** — this reverses the spec's "Resolved" §5
call because that call didn't account for `Title`'s plain-text, single-line constraint (verified above).

### Placement

Theme only, confirmed: no plugin, no `Nop.Core`/`Nop.Data`/`Nop.Services` touch. Concretely: new file
`src/Presentation/Nop.Web/Themes/GesILubczyk/Content/css/home.css`, plus its `AppendCssFileParts` line —
already pre-registered by GIL-003-01's `Head.cshtml` design, so this Task doesn't need to add it.

**Dependency gate:** the `GesILubczyk` theme does not exist yet on this branch (only `DefaultClean` is
present under `Themes/`) — this design targets files GIL-003-01 must create first, per Epic §5's
sequencing.

### Domain model

N/A — no new persisted data. Content lives in the existing `Topic` entity
(`src/Libraries/Nop.Core/Domain/Topics/Topic.cs`), row `SystemName = "HomepageText"`, already seeded by
`src/Libraries/Nop.Services/Installation/InstallRequiredData.cs:2408-2418`. No schema change, no
migration.

### Extension decision

**Existing mechanism (`Topic` + `TopicBlockViewComponent`), confirmed sufficient:**
- `src/Presentation/Nop.Web/Views/Home/Index.cshtml:31` already invokes `TopicBlockViewComponent` with
  `systemName: "HomepageText"`.
- `TopicBlockViewComponent.InvokeAsync` resolves via `ITopicModelFactory.PrepareTopicModelBySystemNameAsync`,
  which loads the topic filtered by `Published`, ACL, store-mapping and availability window
  (`src/Libraries/Nop.Services/Topics/TopicService.cs:94-109`) and is cached under
  `NopTopicDefaults.TopicBySystemNameCacheKey`.
- `Body` is rendered with `@Html.Raw(Model.Body)` — arbitrary HTML, exactly what the mockup's content
  needs.

**Alternatives rejected:** `GenericAttribute` (wrong shape — rich free-form HTML on a page, not a scalar
value on a core entity); a new widget zone/`IWidgetPlugin` (would duplicate a mechanism nopCommerce
already gives content editors for exactly this, and the Epic explicitly rejects it); a schema migration
(no new field needed — `Topic.Title`/`Body` already exist).

### Design

**CSS registration**: `home.css` is already covered by GIL-003-01's pre-registered six-file list in
`Head.cshtml` — no Razor view is created or overridden. `Home/Index.cshtml` is a core file, unmodified by
any theme today, and already calls the right component at the right place.

**Correction to an Epic-level claim, relevant here:** Epic §5 asserts sibling Tasks GIL-003-02..05 "touch
disjoint files" thanks to the per-Task CSS split — but every sibling Task's CSS file is registered in the
same `Head.cshtml`. With GIL-003-01 now pre-registering all six lines up front (per its own design), this
is moot in practice — no sibling Task needs to touch `Head.cshtml` at all.

**Selector scoping (the actual risk this design has to close):** `.topic-block`/`.topic-block-title`/
`.topic-block-body` are **not** homepage-exclusive markup — the same partial renders on
`Customer/Login.cshtml`, `Common/ContactUs.cshtml`, `Common/PageNotFound.cshtml`, `Vendor/ApplyVendor.cshtml`,
and any standalone `Topic/TopicDetails.cshtml` page. `DefaultClean/Content/css/styles.css:3609` already
scopes its one rule as `.home-page .topic-block { margin: 0 0 50px; }` — every `home.css` selector must
follow the same `.home-page`-prefixed pattern, and must never use a bare-tag selector like
`.home-page h2` (`HomepageCategoriesViewComponent`'s per-category `<h2 class="title">` lives on the same
page and would be caught by anything less specific).

**Markup contract** (what the coordinated manual Admin edit, Epic §7, must produce in `Body`):

```html
<!-- Topic.Title: left EMPTY -->

<!-- Topic.Body: -->
<h1 class="gil-hero-title">Prawdziwy obiad to lekarstwo. <br>
  <span class="gil-hero-title-accent">Nie szybka kaloria.</span></h1>

<div class="gil-hero"> ... badge, lead paragraph, CTA buttons, stat pills, hero image figure ... </div>

<section id="gil-manifesto" class="gil-manifesto">
  <div class="gil-manifesto-heading"> ... eyebrow + h2 ... </div>
  <div class="gil-manifesto-grid"> ... quote card, essay copy, callout box ... </div>
  <div class="gil-pillars"> ... three pillar cards ... </div>
</section>

<!-- "Czysty Skład" trust-badge section, in scope per Gate 1 (see Open question resolution below) -->
<section class="gil-trust-badges"> ... four icon+label pairs ... </section>
```

`home.css` selectors are `.home-page .topic-block-body .gil-hero-title { font-family: var(--gil-font-serif); color: var(--gil-color-sage); }`, `.home-page .topic-block-body .gil-hero-title-accent { font-style: italic; color: var(--gil-color-clay); }`, and so on for `.gil-hero`, `.gil-manifesto`,
`.gil-quote-card`, `.gil-pillars .gil-pillar`, `.gil-trust-badges`, all consuming the fixed `--gil-*`
tokens from `tokens.css` — never a hardcoded hex or font-family.

If the store owner has not yet performed the content edit, the page degrades to plain
`.topic-block-title`/`.topic-block-body` content with no styling beyond whatever base rule already exists
in `styles.css`, matching spec §10's stated failure behavior.

### Caching

Unchanged. A `Topic` edit in Admin goes through the standard `IRepository<Topic>.UpdateAsync` →
`EntityUpdatedEvent<Topic>` path, which already invalidates `NopTopicDefaults.TopicBySystemNameCacheKey`
— no new cache key, no coherence concern across ECS instances.

### Localization

None — every visible string is topic-body data, not markup/CSS text.

### Simplicity check

Smallest version that works: one new CSS file (already registration-covered by GIL-003-01) and a manual
Admin content edit following a documented class-name contract. No new view, no view-component override,
no widget zone, no plugin, no migration, no locale keys. The `gil-*` markup contract is not scope growth
— it is the missing specification the spec itself flagged as needed, without which `home.css` has
nothing to target.

### Blast radius

- `.topic-block*` classes are shared by `Login`, `ContactUs`, `PageNotFound`, `ApplyVendor`, and every
  standalone `Topic/TopicDetails` page — addressed by scoping every `home.css` selector under
  `.home-page`.
- `<h2>` is also used by `HomepageCategoriesViewComponent`'s per-category titles on the same homepage —
  addressed by never using a bare-tag selector, only the `.topic-block-title`/`.gil-*` class chain.
- No other code references the `"HomepageText"` system name besides the seed data and
  `Home/Index.cshtml:31` — no other consumer to break.

### Installed-store impact

- No schema, settings, permissions, or locale-resource change — nothing to seed on install/upgrade.
- Until the coordinated manual Admin content edit happens, the existing seeded English placeholder
  ("Welcome to our store…") keeps rendering, now wrapped in the new (but class-agnostic) `home.css`
  rules — visually plain but not broken.
- Rolling deploy: safe. `home.css` is a static asset; no behavior change until both the new theme is
  activated and the content edit lands, in either order.
- Hero image: the mockup hot-links a placeholder Unsplash URL — whether the rollout content edit reuses
  that URL or a self-hosted asset is a content decision for that manual step, not a code change.

**Approved by:** Mateusz Nycz (developer)
**Date:** 2026-09-03
**Revision notes:** Resolved during Gate 1 — (1) `Topic.Title` left empty, full two-tone headline
(including line break and italic-clay accent) authored as raw HTML at the top of `Body` instead —
approved as this design proposed, reversing the spec's original §5 call; (2) the mockup's "Czysty Skład"
four-icon trust-badge section is **in scope** for this Task, rendered inside the same `Topic` block
(Epic §3 named it as this Task's scope but the original spec text omitted it) — it loses its mockup
position (after the product grid) since no other homepage render slot exists before the grid without a
widget zone, which this Task's mechanism explicitly avoids; accepted as a known, deliberate trade-off.

## Implementation plan (implementation-planner)

### Files

- **`src/Presentation/Nop.Web/Themes/GesILubczyk/Content/css/home.css`** — new (mirrors
  `Themes/DefaultClean/Content/css/styles.css`'s `/*** TOPICS ***/` block, lines 3569–3611, for selector
  shape/scoping convention; structurally mirrors GIL-003-01's `header-footer.css` for how a per-Task
  theme CSS file consumes `tokens.css`). Plain hand-authored CSS, no Razor, no `@import`.
  - Every selector prefixed `.home-page` (mirrors `styles.css:3609`) — never a bare `.topic-block*` or a
    bare-tag selector (`.home-page h2` is forbidden — `HomepageCategoriesViewComponent`'s per-category
    `<h2 class="title">` lives on the same page).
  - Target classes, nested under `.home-page .topic-block-body`: `.gil-hero-title`,
    `.gil-hero-title-accent`, `.gil-hero`, `.gil-manifesto`, `.gil-manifesto-heading`,
    `.gil-manifesto-grid`, `.gil-quote-card`, `.gil-pillars`/`.gil-pillars .gil-pillar`,
    `.gil-trust-badges`.
  - Every color/font value is `var(--gil-*)` — never a hardcoded hex/font-family.
  - Exact spacing/layout read directly from `mockup-reference.html`'s hero/manifesto/trust-badge markup.

No other file changes: `Head.cshtml` already pre-registers `home.css` (GIL-003-01, not touched here);
`Home/Index.cshtml` and `TopicBlock/Default.cshtml` (core) are unmodified; no `.csproj` entry needed —
`Nop.Web.csproj`'s `<Content Include="Themes\**" .../>` wildcard already covers a new `home.css`.

### Order of work

1. Confirm GIL-003-01 has landed: `Themes/GesILubczyk/theme.json` exists and its `Head.cshtml` already
   contains the `home.css` registration line. This Task adds no registration itself.
2. Author `Content/css/home.css` per the contract above.
3. Build `Nop.Web` — confirms the new static file copies via the `Themes\**` glob (no compile step is
   actually exercised, CSS-only).
4. Manual, out-of-code step (coordinated at rollout per Epic §7): edit the `HomepageText` Topic in
   Admin → Content Management → Topics — leave `Title` empty, set `Body` to the markup contract above.
5. Manual visual verification: browse `/` with `GesILubczyk` active, confirm hero/manifesto/trust-badge
   sections match the mockup.

### Tests

None. No new/changed service method, entity method, `IConsumer<T>`, migration, controller action, or bug
fix — this Task is a single static CSS file plus a manual (non-code) content edit, matching spec §11.

### Standards skills to load

`theming-standards-check` (placement, asset-registration checklist — already satisfied by GIL-003-01,
nothing to add; narrowest-override principle — no view override needed at all), `localization-standards-check`
(confirms no user-facing string is introduced in CSS content or any Razor — none is, per spec §7; catches
an accidental hardcoded label if tempted to add a section eyebrow via CSS `content:`, which the spec
forbids).

### Gaps in the approved design

None. The approved design fixes the file to create, its registration (done upstream by GIL-003-01), the
selector-scoping rule, the class-name contract, and the token-only color/font rule; the remaining detail
(exact spacing/markup translation from the mockup) is delegated by Epic §1 to direct consultation of that
file — the design's stated mechanism for that level of detail, not a gap.

**Approved by:** Mateusz Nycz (developer)
**Date:** 2026-09-03
**Revision notes:** none — approved as planned.
