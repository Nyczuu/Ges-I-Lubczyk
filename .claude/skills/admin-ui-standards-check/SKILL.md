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
