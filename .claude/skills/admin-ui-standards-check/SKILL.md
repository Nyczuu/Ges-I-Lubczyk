---
name: admin-ui-standards-check
description: >-
  Load this when writing or reviewing a controller, view model, validator, view, admin menu entry, or
  route — in Areas/Admin or in a plugin's admin surface. Use it BEFORE writing the code: the pieces
  that get skipped (permission attribute, antiforgery, locale-backed labels, the route provider) are
  exactly the ones nothing fails without, so they are never caught later by the compiler or a test.
---

# Admin UI Standards Check

Full doc: [`Docs/knowledge-base/08-settings-permissions-validation.md`](../../../Docs/knowledge-base/08-settings-permissions-validation.md),
plus steps 6–8 of [`Docs/ai-harness/02-extensibility-and-plugins.md`](../../../Docs/ai-harness/02-extensibility-and-plugins.md).
This is the checklist form. Localization detail lives in `localization-standards-check`; permission and
ACL depth in `security-permissions-check`.

## Controller

- [ ] Named `{Group}{Name}Controller`, deriving from `BasePluginController` (or a plain controller with
      `[Area(AreaNames.ADMIN)]`).
- [ ] `[AuthorizeAdmin]` and `[AutoValidateAntiforgeryToken]` present on an admin controller.
- [ ] Every action guarded by a permission — declaratively with
      `[CheckPermission(XPermissionConfigManager.MANAGE_...)]`, or imperatively via
      `IPermissionService.AuthorizeAsync(...)`. An action with no permission check is a finding, not a
      style preference.
- [ ] **Thin.** The controller resolves input, calls a service, and returns a result. Query composition,
      business rules, and model assembly do not live here.
- [ ] Model building delegated to a factory (`I{Name}ModelFactory`) rather than inlined, matching the
      shape of `Presentation/Nop.Web/Areas/Admin/Factories`.
- [ ] **An action that uploads a file (a picture, an import) alongside other model validation in the same
      POST validates everything else first, and only performs the upload once the rest of `ModelState`
      is already known-valid.** Uploading unconditionally and validating after leaves an orphaned row
      (e.g. a `Picture` with nothing pointing at it) every time an unrelated field fails — and the file
      input can't be re-populated on the re-rendered form, so a corrected resubmission orphans another
      copy. `ProductController.ProductPictureAdd`'s separate-AJAX-call shape sidesteps this by construction
      (no other required field in that request); a single combined action does not get that for free.
- [ ] **Every `return View(...)`/`PartialView(...)` in a plugin admin controller uses the explicit
      virtual path** — `View("~/Plugins/{Group}.{Name}/Admin/Views/{ActionName}.cshtml", model)` — never
      the bare `View(model)`/`PartialView(model)` convention shortcut. This codebase has no
      `IViewLocationExpander` for plugin views (only themes get one, and it explicitly skips the Admin
      area — see `ThemeableViewLocationExpander`), so the implicit lookup only ever searches
      `Areas/Admin/Views/{Controller}/{Action}.cshtml` and never finds a plugin's own `Views` folder. It
      compiles clean and every unit test mocking the model factory passes — only a real browser request
      surfaces `InvalidOperationException: The view '...' was not found`. This slipped past two plugins'
      (GIL-001, GIL-002) full post-implementation gate for exactly that reason; verify by actually loading
      the page, not just by building. **The identical failure exists one layer down**: a plugin's own
      `.cshtml` calling one of its own partials via `@await Html.PartialAsync("_PartialName", Model)` has
      the same bare-name problem and needs the same explicit `~/Plugins/...` treatment — see Views below.
      A view or view component that references a genuinely core/shared partial or layout (nopCommerce's
      own `"Table"` grid partial, `"_AdminLayout"`, `"_AdminPopupLayout"`, `"_ColumnsTwo"`, ...) is
      correct as-is; only plugin-owned files need the explicit path.

## Routes

- [ ] Configuration route registered via a `RouteProvider : IRouteProvider` — auto-discovered, no manual
      registration list.
- [ ] Route name exposed as a constant on `{Name}Defaults.Route`, never a string literal at the call site.
- [ ] `BasePlugin.GetConfigurationPageUrl()` resolves it through `INopUrlHelper`.

## Models and validation

- [ ] View model derives from `BaseNopModel`; it is a **view model**, not a domain entity passed through.
- [ ] Every user-facing label uses `[NopResourceDisplayName("Plugins.{Group}.{Name}.Fields.X")]`.
- [ ] Validation is **FluentValidation** in a `BaseNopValidator<TModel>` — resolved and run automatically
      on POST. **Never** DataAnnotations (`[Required]`, `[StringLength]`) on a nopCommerce view model,
      and never a hand-rolled `ModelState.AddModelError` for a rule a validator can express against the
      bound model. Exception: re-checking that same rule against a value only known *after* the action
      processes an upload in the same request (e.g. a final `PictureId` once a new file has been saved) —
      the validator ran against the stale pre-upload value, so a manual check against the resolved value
      is the correct place for it, not a bypass of FluentValidation.
- [ ] Error messages come from locale resources via `.WithMessageAwait(localizationService.GetResourceAsync(...))`,
      not hardcoded English.
- [ ] Bounded string fields have an explicit `MaximumLength` rule — Postgres will not enforce the column
      width for you (see `data-access-standards-check`).

## Views

- [ ] `Views/Configure.cshtml` with `Build Action = Content`, `Copy to Output Directory = Copy always`,
      `_ConfigurePlugin` layout, and a `_ViewImports.cshtml` copied from an existing plugin.
- [ ] No business logic in the view. No hardcoded UI strings — locale resources only.
- [ ] Extending an existing page: override the specific partial or view component, or inject through a
      widget zone. **Never fork a shared layout file** (see `theming-standards-check`).
- [ ] Every `@await Html.PartialAsync(...)` (or `Html.RenderPartialAsync`/`Html.Partial`) that renders
      another view file **owned by this plugin** passes its explicit `~/Plugins/{Group}.{Name}/...cshtml`
      path, not the bare partial name — same cause and same silent-until-runtime failure as the Controller
      bullet above. This includes a `_CreateOrUpdate`-style partial calling further nested partials of its
      own (e.g. `_CreateOrUpdate.Info`, `_CreateOrUpdate.Composition`) — each nesting level needs the fix
      independently, a fixed outer view does not make an unfixed inner one work.
- [ ] **A tab/card that lets the admin add a row referencing a not-yet-saved parent's id (a product-edit
      tab adding rows keyed by `ProductId`, a category-edit tab keyed by `CategoryId`, ...) guards on
      `Model.ProductId > 0` (or the equivalent id) and shows a save-first message instead of the grid and
      "Add" button when it is `0`.** Mirror nopCommerce's own
      `Areas/Admin/Views/Product/_CreateOrUpdate.RelatedProducts.cshtml`: `@if (Model.Id > 0) { grid +
      add button } else { @T("...SaveBeforeEdit") }`. Skipping this renders the "Add" button on a brand
      new, unsaved product; clicking it inserts a child row with `ProductId = 0`, which fails at the
      database as a raw, unhandled foreign-key violation (`23503`) instead of a clean message — this
      slipped past both GIL-001's ingredients tab and GIL-002's serving-suggestion tab.
- [ ] **A multi-select "Add" popup (`RenderCheckBox` + `IsMasterCheckBox` on a paginated/searchable
      DataTables grid — the `IngredientCompositionAddPopup`/`ProductIngredientAddPopup` pattern) persists
      checked rows across page and search reloads client-side, not just within the currently visible
      page.** DataTables replaces the grid's `<tbody>` on every page turn *and* every search, so a plain
      checkbox-driven form submit only ever reflects whatever page happens to be showing at Save time —
      anything checked on a page the admin already navigated away from is silently dropped. nopCommerce's
      own `selectedIds`/`clearMasterCheckbox` globals (`admin.table.js`) look like they would help but
      don't: that mechanism exists only to drive the master-checkbox tri-state and is deliberately reset
      on every single reload, including a plain page turn. The fix is a small script local to the popup's
      own view, not a core change: a JS `Set` that survives reloads (closure state, not DOM) updated on
      checkbox `change`; re-applying `.checked` to matching rows on the grid's `draw.dt` event so a
      revisited page still shows what was picked; and on form `submit`, stripping `name` from the
      currently-visible checkboxes and injecting one hidden input per id in the `Set` instead, so the
      full cross-page selection posts regardless of which page is on screen. Reference implementation:
      `ProductIngredientAddPopup.cshtml`/`IngredientCompositionAddPopup.cshtml` in
      `Nop.Plugin.Misc.Ingredients`.

## Admin menu

- [ ] Menu entry added by subscribing to `AdminMenuCreatedEvent` via `IConsumer<AdminMenuCreatedEvent>`
      and calling `eventMessage.RootMenuItem.InsertBefore(...)`, guarded by a permission check.
- [ ] **Never** a sitemap/config file edit — that is the pre-4.80 mechanism and does not apply here.

## Before calling admin work done

- [ ] Every action has an explicit permission check.
- [ ] Antiforgery and `[AuthorizeAdmin]` in place on the admin controller.
- [ ] Zero hardcoded user-facing strings in controller, model, validator, or view.
- [ ] Validator exists for every POSTed model, with length rules on bounded strings.
- [ ] Route registered through `IRouteProvider` with the name in `{Name}Defaults`.
- [ ] Every `return View(...)` in a plugin admin controller, and every `Html.PartialAsync(...)` in a
      plugin's own views, carries an explicit `~/Plugins/...` path to a plugin-owned file — confirmed by
      actually loading the page (including opening every popup/nested partial, not just the top-level
      page), not just by a successful build.
