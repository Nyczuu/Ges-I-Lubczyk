---
id: GIL-003-02
kind: Task
title: Homepage brand-story content restyle
status: Ready
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
