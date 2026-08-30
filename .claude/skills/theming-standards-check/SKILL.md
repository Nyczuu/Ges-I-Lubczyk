---
name: theming-standards-check
description: >-
  Load this when changing storefront presentation — a theme, a Razor view or partial, a view component,
  or markup injected into an existing page. Use it BEFORE writing the code: forking a shared layout to
  land a small tweak works immediately and silently breaks theme swapping and future upgrades, and the
  theming documentation on file predates 5.00, so its file names must be checked against the checkout.
---

# Theming Standards Check

Full doc: [`Docs/knowledge-base/09-theming-and-design.md`](../../../Docs/knowledge-base/09-theming-and-design.md).
This is the checklist form.

## Verify before you generate

The theming pages in the knowledge base are adapted from nopCommerce docs dated 2020–2022 and describe
the pre-5.00 default theme. Rule 9 of `00-system-instructions.md` applies with unusual force here:

- [ ] Diff the actual `src/Presentation/Nop.Web/Themes/` folder before writing theme code — do not trust
      layout file names from the doc verbatim.
- [ ] Confirm any `PublicWidgetZones` constant against the checked-out class. Zone names shift between
      minor versions, and a wrong constant renders nothing, with no error.

## Where a change belongs

```
Visual only (colours, spacing, branding)      → theme Content/ under Themes/{YourTheme}
Markup injected into an existing page         → IWidgetPlugin targeting a PublicWidgetZones constant
Changed structure of one specific block       → override that partial / view component in the theme
Site-wide asset (CSS/JS on every page)        → _Root.Head.cshtml in the theme
```

- [ ] **Never fork a whole layout** (`_Root.cshtml`, `_ColumnsOne.cshtml`, `_ColumnsTwo.cshtml`) for a
      localized change. Theme view overrides take precedence by relative path — override the narrowest
      file that contains the change.
- [ ] **Never hand-edit a shared layout to splice in plugin or third-party markup.** That is what widget
      zones exist for, and it is how the built-in analytics and slider integrations work. A layout edit
      loses the integration the moment the theme is swapped.
- [ ] Top menu and footer markup are the sanctioned exceptions — those are meant to be edited directly
      in a theme.

## New theme

- [ ] Copy an existing theme folder, rename it, update `theme.json`, restyle `Content/`.
- [ ] Do not start from an external Bootstrap template and reconstruct the view tree by hand.

## Assets

- [ ] CSS/JS registered through `NopHtml.AddCssFileParts` / `AddScriptParts` with an explicit
      `ResourceLocation`, not a raw `<link>`/`<script>` tag dropped into a view.
- [ ] Plugin assets referenced from the plugin's output directory under `~/Plugins/{Group}.{Name}/`.
- [ ] `excludeFromBundle` set deliberately for anything that must not be bundled.

## Localization and logic

- [ ] No user-facing literal in a view — see `localization-standards-check`.
- [ ] No business logic or data access in a view. If a view needs computed data, it comes from a model
      built by a factory or a view component.

## Before calling theming work done

- [ ] Real file and zone names verified against the checkout, not the doc.
- [ ] Narrowest override used; no shared layout forked or hand-spliced.
- [ ] Assets registered through `NopHtml`.
- [ ] Change survives a theme switch (or its theme-specific nature is intentional and stated).
