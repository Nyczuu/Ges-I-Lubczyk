# Theming & Design

Source: adapted from `developer/design/*` pages (overview, new-theme, understanding-layout,
customizing-theme, widgets, tips-and-tricks).

## Theme anatomy

- All themes live under `src/Presentation/Nop.Web/Themes/`. A theme = a folder containing at minimum
  `theme.json`, a `Content/` (CSS/images), and optional view/layout overrides.
- New theme = copy an existing theme folder, rename it, edit `theme.json`'s name field, then restyle
  `Content/css`/images. Apply it in Admin → Configuration → Appearance.

## Layout hierarchy (Razor `_Layout` chain)

```
_Root.Head.cshtml   (charset, <head>, global CSS/JS links — edit here to add site-wide assets)
  └─ _Root.cshtml   (page chrome: header, footer, notifications)
       ├─ _ColumnsOne.cshtml   (single-column body — most pages)
       └─ _ColumnsTwo.cshtml   (two-column body — category/manufacturer listing pages, etc.)
```

Files under `Presentation/Nop.Web/Views/Shared/`. Do not fork the whole layout for a small tweak —
override the specific partial/view component instead (theme view overrides take precedence over the
base theme's own views of the same relative path).

## Widget zones — the sanctioned way to inject storefront markup

Never hand-edit `_Root.cshtml`/`_ColumnsOne.cshtml` to splice in third-party or plugin markup.
Register an `IWidgetPlugin` targeting the relevant `PublicWidgetZones` constant instead (see
[06-plugin-types-reference.md](06-plugin-types-reference.md)) — this is how the built-in Google
Analytics, Swiper, and Facebook Pixel integrations work, and it keeps the theme swappable without
losing the integration.

Common customization points that **are** meant to be edited directly in a theme (not via widget):
top menu (`Views/Shared/Components/TopMenu/Default.cshtml`) and footer
(`Views/Shared/Components/Footer/Default.cshtml`) markup/link lists.

## Resource files (CSS/JS) from a plugin or theme

```cshtml
@NopHtml.AddCssFileParts("~/Plugins/{PluginOutputDir}/Content/styles.css", excludeFromBundle: false)
@NopHtml.AddScriptParts(ResourceLocation.Footer, "~/Plugins/{PluginOutputDir}/Scripts/app.js", excludeFromBundle: true)
```
`ResourceLocation.Head` vs. `ResourceLocation.Footer` controls placement; render the collected tags
with `@NopHtml.GenerateScripts(ResourceLocation.Footer)` in the layout. For an inline external
`<script src>` that must not be touched by the tag helper, opt out per-element with `!`.

## Practical guidance for a gastronomy storefront theme

- Freshness/expiry badges, allergen icons, dietary tags on the PDP/PLP → a widget targeting
  `PublicWidgetZones.ProductDetailsTop`/`ProductBoxAddinfoMiddle`-style zones, not a layout fork.
  (Confirm exact zone constant names against the checked-out `PublicWidgetZones` class before use —
  they vary slightly by nopCommerce minor version.)
  - **Note**: the theming docs on file are dated 2020–2022 and describe the pre-5.00 default theme
    structure; before generating theme code, diff the actual
    `src/Presentation/Nop.Web/Themes/` folder in this checkout rather than trusting layout file names
    verbatim from this note.
