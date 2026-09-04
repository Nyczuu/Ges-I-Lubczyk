---
id: GIL-006
kind: Task
title: Default shelf-life days on product, prefilling best-before date in Production batches
status: In Progress
---

# Task — Default shelf-life days on product, prefilling best-before date in Production batches

A single implementable unit of work (feature). Mirrors the Task checklist in `.claude/agents/spec-intake.md`.

## 1. Business goal & outcome

Today, logging a new production batch (`Nop.Plugin.Misc.ProductionLabels`'s "Add new" popup,
`ProductionBatchCreatePopup.cshtml`) requires the admin to type both `ProductionDateUtc` and
`BestBeforeDateUtc` by hand every time, for every batch. For most products the gap between the two is
a fixed number of days that rarely changes (the product's shelf life) — retyping it, and getting the
arithmetic right, on every single batch entry is repetitive and error-prone.

Success: an admin sets a default shelf-life (in days from production) once, per product. From then on,
whenever staff open the "Add new production batch" popup for that product and pick/change the
production date, the best-before date field is automatically prefilled as
`production date + default shelf-life days` — but remains a normal, freely editable input, so an
atypical batch (different-than-usual shelf life) can still be entered correctly by overwriting the
prefilled value. A product with no default configured behaves exactly as today (both dates entered
manually, no prefill).

## 2. Root cause / current behavior *(bug fixes only)*

N/A — new feature, not a bug fix.

## 3. Placement — plugin or core?

Extends the existing `Nop.Plugin.Misc.ProductionLabels` plugin — no new plugin, no core touch. The
value is only ever consumed by this plugin's own batch-creation flow (it has no other use anywhere in
the shop), so it lives alongside the plugin's existing per-product admin inputs (Storage conditions /
Country of origin, GIL-005 §5-§6), on the same product-edit tab
(`Admin/Views/Components/ProductionLabels.cshtml`), saved through the same
`ProductionLabelsAdmin/SaveProductInfo` action and the same `ProductionLabelsAdminModelFactory` that
already prepares that tab's model.

**Flag if wrong:** this spec assumes the field should **not** live on generic core `Product` admin
tabs (e.g. next to `Weight`/shipping fields) precisely because nothing outside this plugin reads it —
per `00-system-instructions.md`'s framing, this is convenience data for one plugin's own workflow, not
a shop-wide product attribute. If a future need (e.g. a storefront "best before" estimate, or another
plugin) wants to read the same value, that is scope growth for a later ticket, not assumed here.

## 4. Extension point

N/A — no new plugin type/interface. Existing `IMiscPlugin`/`IWidgetPlugin` registration
(`ProductionLabelsPlugin.cs`) and existing `AdminWidgetZones.ProductDetailsBlock` widget are unchanged;
this only adds a field to an already-existing admin surface and a small read used by the existing
batch-creation popup(s).

## 5. Data model & migration

**Extensibility choice:** `GenericAttribute` on `Product`, new key
`ProductionLabels.DefaultShelfLifeDays` (int, nullable/optional — unset means "no default configured,
behave as today"). Per `knowledge-base/04-extending-core-entities.md`'s decision rule: nothing ever
filters, sorts, or joins on this value (it is read back for exactly one product at a time, purely to
compute a prefill), so it fails the "does something need `WHERE`/`ORDER BY` on it" test the same way
GIL-005's Storage conditions/Country of origin fields did — no schema migration, no core touch.

**Unlike Storage conditions/Country of origin, this value is *not* per-language** — a number of days
has no translation, so a single `GenericAttribute` key per product is sufficient (no
`{languageId}`-suffixed key family, no `Locales` involvement, no `ILocalizedModel` plumbing for this
field).

**Existing mechanisms considered and rejected** (`ProductTag`/`SpecificationAttribute`/
`ProductAttribute`): none fit a single admin-only numeric default read back by one specific plugin's
workflow; `SpecificationAttribute` would additionally leak it into storefront comparison/filter UI,
which is out of scope (no storefront surface at all — see §6).

**Validation:** when provided, must be a positive integer (`> 0`). Left blank/cleared: valid, means "no
default" (prefill does not fire; today's manual-entry behavior). Rejected at the admin validator/service
layer with a normal validation error, not silently clamped — same posture GIL-005 already established
for its own numeric field (`Quantity > 0`).

No schema migration needed for the data itself (GenericAttribute is schema-free); no impact to existing
installations beyond a new, uniquely-prefixed, currently-empty key.

**Existing-installation impact — resolved (added round 3):** `Nop.Plugin.Misc.ProductionLabels` is
already shipped (GIL-005). `ProductionLabelsPlugin.InstallAsync` only runs once, at first install, so
its locale-resource dictionary does not retroactively reach a store where this plugin is already
installed — the new field's locale keys would render as raw/missing resource keys there without
something to deliver them. This repo has a very recent precedent for exactly this situation:
`Nop.Plugin.Misc.Ingredients`'s `NutritionalValuesMigration.cs`
(`MigrationProcessType.Update`, dated 2026-09-03), paired with a `plugin.json` version bump
(`5.00.1` → `5.00.2`). This ticket follows the same pattern: a new `[NopMigration(..., MigrationProcessType.Update)]`
that adds the new locale resource keys (label, `.Hint`, and any new validation message) via
`this.AddOrUpdateLocaleResource(...)` (the synchronous `IMigration` extension method the cited
precedent actually calls — not the async `ILocalizationService.AddOrUpdateLocaleResourceAsync`
instance method this same plugin's `InstallAsync`/`UninstallAsync` use elsewhere; the two are not
interchangeable inside a `MigrationBase.Up()`), plus a `plugin.json` version bump for
`Nop.Plugin.Misc.ProductionLabels` (exact target version left to `ddd-modeler`/`implementation-planner`,
mirroring how the Ingredients precedent incremented its own patch version).

## 6. Admin & storefront surface

No storefront surface — admin-only, exactly like the rest of this plugin's product-level inputs.

**Product-edit tab** (`ProductionLabels.cshtml`, `ProductionLabelsProductModel`): one new number input,
"Default shelf-life (days)", alongside the existing Storage conditions/Country of origin inputs, saved
through the same `SaveProductInfo` form/action. **Unlike those two fields, this one is not
per-language**, so it is a single flat property on `ProductionLabelsProductModel` (not nested under
`Locales`) and must be saved **unconditionally** by `SaveProductInfo` — that action currently branches
into two mutually exclusive paths (`model.Locales.Any()` vs. the flat-property fallback for a
single-language store, per GIL-005's round-2 gate fix); the new field's save must happen in **both**
branches (or be factored out of the branch entirely), since it is not part of what that branching
exists to handle.

**"Add new production batch" popup — both entry points:**

- **Product-edit tab flow** (`ProductionBatchCreatePopup`, reached with a known `productId`): the
  popup's model already carries enough context to resolve the product's configured
  `DefaultShelfLifeDays` server-side when the popup is first prepared
  (`PrepareProductionBatchModelAsync`). When a value is configured, `BestBeforeDateUtc` changing to
  reflect `ProductionDateUtc + DefaultShelfLifeDays` is client-side behavior: whenever the admin
  changes the Production date input, the Best-before date input is recalculated and updated
  automatically — but stays a normal, directly-editable field the admin can overwrite at any time,
  including after an auto-fill has happened. **Proposed default (flag if wrong):** once the admin has
  manually typed into the Best-before date field themselves, further Production-date changes stop
  overwriting it (the same "don't clobber a value the user just touched" convention common to prefill
  UX) — the alternative (always overwrite on every Production-date change, even after a manual edit)
  is simpler but would silently discard a deliberate override the moment the admin nudges the
  production date again.
- **Standalone "Production" section flow** (product picker, `productId` starts at `0` /
  unknown until the admin picks one): the same prefill must apply once a product is chosen from the
  dropdown, which means the configured `DefaultShelfLifeDays` for the *selected* product is not known
  until that selection happens client-side. This needs a small read endpoint (JSON, by `productId`) the
  popup's script calls when the product dropdown changes, in addition to reacting to Production-date
  changes as above.

If no default is configured for the product (or, in the standalone flow, before any product is picked),
both fields behave exactly as today — no automatic date is filled in beyond the existing "today" default
already set by `PrepareProductionBatchModelAsync`.

## 7. Settings, permissions, localization

No new `ISettings`. No new permission — saving the new field goes through the existing
`SaveProductInfo` action, already gated by `ProductionLabels.Create` (same as Storage
conditions/Country of origin); reading it back for the batch-popup prefill (including the new
by-`productId` read endpoint for the standalone flow) is gated by `ProductionLabels.Create` too —
matching `ProductionBatchCreatePopup`, the action whose flow it serves (verified: that action is
gated by `PRODUCTION_LABELS_CREATE`, not `PRODUCTION_LABELS_VIEW` — see the correction below).

New locale resource keys under `Plugins.Misc.ProductionLabels.*` for the field's label + `.Hint` and any
new validation message.

**Correction:** the new by-`productId` prefill-read endpoint (§6, standalone-flow) must be gated by
`ProductionLabels.Create`, not `ProductionLabels.View` — verified against
`ProductionLabelsAdminController.cs`: the popup action it serves
(`ProductionBatchCreatePopup(int productId)`) is itself gated by
`PRODUCTION_LABELS_CREATE`, not `PRODUCTION_LABELS_VIEW` (that permission gates `List`/
`GenerateLabelPopup` instead). The new endpoint matches the action whose flow it serves.

**Uninstall:** `ProductionLabelsPlugin.UninstallAsync` must additionally purge the new
`ProductionLabels.DefaultShelfLifeDays` key for every product that has one set. Unlike Storage
conditions/Country of origin, this is **not** per-language, so it does not need the per-language
enumeration those two keys require — a single, product-scoped
`IGenericAttributeService.DeleteAttributesAsync<Product>(...)` sweep on this one key (exact iteration
approach — e.g. whether to enumerate all products or use a bulk delete-by-key path — left to
`ddd-modeler`, matching how GIL-005 left its own exact iteration approach open at spec stage).

## 8. Events & scheduled tasks

N/A — no new events published or consumed, no `IScheduleTask`.

**Cross-cutting checklist items, addressed:**

- **Multi-store variation — resolved:** a single value per product, no store-mapping — same posture
  GIL-005 kept for the sibling `StorageConditions`/`CountryOfOrigin` `GenericAttribute` fields on the
  same entity (never resolved to per-store there either; this stays consistent with that precedent).
- **Concurrency & idempotency — resolved:** last-write-wins on a same-product concurrent save is
  acceptable — a plain `IGenericAttributeService.SaveAttributeAsync` check-then-act write, no
  transaction, matching this plugin's existing posture for its other admin-editable fields.
- **Consistency across an operation — N/A:** this ticket writes exactly one `GenericAttribute` value
  and publishes no event as part of that write; there is no multi-step operation where a later step
  could fail after an earlier one committed.
- **Cost/load on a downstream dependency — N/A:** the new by-`productId` read is a single
  `GenericAttribute` lookup fired once per product-dropdown selection in the standalone popup (a
  manual, low-frequency admin interaction) — not a loop, not N+1, nothing that could hammer the
  database.
- **Configuration/environment differences — N/A:** rides the same `GenericAttribute` mechanism already
  in production use by this plugin's `StorageConditions`/`CountryOfOrigin` fields, which behaves
  identically between the local `postgresql-docker-compose.yml` environment and ECS today.

## 9. Caching

Rides the same `GenericAttribute` caching GIL-005 §9 already established for Storage
conditions/Country of origin (`IGenericAttributeService.GetAttributesForEntityAsync`, backed by
`IShortTermCacheManager`/`PerRequestCacheManager`, invalidated via the framework's own
`GenericAttributeCacheEventConsumer`) — no new caching concern, no multi-instance/ECS coherence issue.

## 10. Failure scenarios

- No default configured for the product: no prefill; admin enters both dates manually, identical to
  today's behavior — this is the majority case for any product until an admin opts in.
- `DefaultShelfLifeDays` submitted as `0` or negative: rejected with a validation error (§5), not
  silently clamped or treated as "no default."
- Production date left blank when a default is configured: nothing to add days to — no prefill computed
  (Best-before date keeps whatever value it already had, e.g. the existing "today" default), not an
  error.
- JavaScript disabled / script error in the admin's browser: the popup still functions exactly as
  today — both dates are ordinary editable inputs the admin fills in by hand; the prefill is a
  convenience layer, not something either the model binder or the service layer depends on for correct
  behavior.
- Standalone-section popup: the by-`productId` read (for the product-picker flow) fails or is slow: the
  prefill for that field simply doesn't happen for that interaction — same as "no default configured" —
  and does not block filling in or submitting the rest of the form.

## 11. Test scenarios

- `ProductionLabelsAdminModelFactory.PrepareProductionLabelsProductModelAsync`: populates
  `DefaultShelfLifeDays` from the `GenericAttribute` when set; `null` when unset.
- `SaveProductInfo`: persists `DefaultShelfLifeDays` on **both** the multi-language (`Locales`
  populated) and single-language (flat-property fallback) save paths — a regression-shaped test given
  §6's note that the field must not get stranded inside either branch.
- Validation: `DefaultShelfLifeDays <= 0` rejected when provided; `null`/blank accepted as "no default."
- `PrepareProductionBatchModelAsync` (product-tab flow): when the product has a configured
  `DefaultShelfLifeDays`, the prepared model exposes it (for the client-side computation) — asserted
  against the `GenericAttribute` value; `null`/absent when unconfigured.
- New by-`productId` read endpoint (standalone flow): returns the configured value for a product that
  has one, and a "no default" response (e.g. `null`) for a product that doesn't; permission-gated the
  same as the rest of the popup's reads (§7).
- Uninstall purges the new `GenericAttribute` key for every product that had one set.

**Flag — not coverable by this repo's existing test stack:** the actual date-arithmetic prefill and
the "don't clobber a manual edit" behavior (§6) are client-side JavaScript in a Razor view; this
repo's test suite is NUnit/Moq/AwesomeAssertions against C#, with no JS-level test runner identified
in this codebase so far. These behaviors need a manual browser check as part of verifying this ticket,
not a unit test — same treatment GIL-005 already gave its own JS-adjacent admin popup flows (no
automated coverage claimed for them either).

## 12. Documentation impact

Update `Docs/BusinessLogic/product-production-labels.md` (already exists, written for GIL-005) to
document the new default-shelf-life field: what it's for, that it's optional/per-product, and that it
only drives a client-side prefill on batch creation — it never changes what gets stored on a
`ProductionBatch` row itself (the admin's actually-submitted `BestBeforeDateUtc`, whether prefilled or
overridden, remains the single source of truth once a batch is saved).

## 13. Deployment & rollout

None beyond the ordinary plugin deploy — no new dependency, no Docker/image change, no migration to
run. Immediate rollout once merged, consistent with GIL-005's own posture at this scope.

## Technical design (ddd-modeler)

### Corrections to the spec's technical assumptions

- **"Rejected at the admin validator/service layer with a normal validation error"** — verified against
  `ProductionLabelsAdminController.SaveProductInfo`
  (`src/Plugins/Nop.Plugin.Misc.ProductionLabels/Admin/Controllers/ProductionLabelsAdminController.cs:192-225`):
  unlike `ProductionBatchCreatePopup` (which does check `ModelState.IsValid`, mirroring `Quantity`'s
  double-enforcement), `SaveProductInfo` **never checks `ModelState.IsValid` at all** and has no
  FluentValidation validator for `ProductionLabelsProductModel` today. Adding a validator alone would
  populate `ModelState` but change nothing — the action would silently save the invalid value anyway.
  This ticket must add the `ModelState.IsValid` check to `SaveProductInfo` itself, not just a validator
  class.
- **"Follows the same pattern" (the `NutritionalValuesMigration` precedent)** — verified against
  `PluginService.cs`: `InstallPluginsAsync` runs `Installation`-type migrations, then separately calls
  `IPlugin.InstallAsync()`; it marks `Update`-type migrations as applied **without running their
  `Up()`** on a fresh install (`InsertPluginData`: `commitVersionOnly: true` for `Update`-type when the
  plugin is being installed for the first time). A brand-new install of this plugin (any store
  installing it *after* this ships) will **never execute** the new Update migration's `Up()` — it only
  gets stamped as already-applied. The locale keys must therefore also be added to
  `ProductionLabelsPlugin.InstallAsync()`'s dictionary, exactly as `IngredientsPlugin.InstallAsync()`
  duplicates its own `NutritionalValuesMigration` keys
  (`src/Plugins/Nop.Plugin.Misc.Ingredients/IngredientsPlugin.cs:127-129`). The spec's §5 wording only
  mentions the migration; the actual precedent it cites requires both.

No other deviations from the spec's stated approach — placement, extension choice (`GenericAttribute`),
migration type, and permission reuse all match the spec's stated approach once verified.

### Placement

No new plugin. Extends the already-shipped `Nop.Plugin.Misc.ProductionLabels` (`IMiscPlugin`/
`IWidgetPlugin`, unchanged). No core touch.

### Domain model

N/A — no new persisted entity, no schema migration. One new `GenericAttribute` key on `Product`:
`ProductionLabelsDefaults.DefaultShelfLifeDaysAttributeKey => "ProductionLabels.DefaultShelfLifeDays"`,
value type `int?`, **not** per-language (deliberately no prefix/`{languageId}` suffix, unlike the two
sibling keys `StorageConditionsAttributeKeyPrefix`/`CountryOfOriginAttributeKeyPrefix`).

### Extension decision

`GenericAttribute` on `Product` — matches the decision rule in
`Docs/knowledge-base/04-extending-core-entities.md`: nothing filters/sorts/joins on this value; it's
read back for exactly one product at a time. `IGenericAttributeService.SaveAttributeAsync<TPropType>`
clears the row on a blank/null value (giving "blank = no default" for free); the entity-id read overload
`GetAttributeAsync<TEntity, TPropType>(int entityId, string, ...)` avoids a full `Product` load for the
new by-id read endpoint. `int?` as `TPropType` is a proven pattern elsewhere in this codebase
(`WebWorkContext.cs:242`, `BrevoMessageService.cs:165`).

Rejected alternatives, per spec: `ProductTag`/`SpecificationAttribute`/`ProductAttribute` (the latter
would leak into storefront facets — no storefront surface exists here at all) and a schema migration
(nothing ever needs `WHERE`/`ORDER BY` on it).

### Design

**`ProductionLabelsDefaults.cs`** — add the new key constant (above).

**`ProductionLabelsAdminModelFactory.cs`** — one shared helper, used by the product tab, the
product-tab batch-popup flow, and the new read endpoint:
```csharp
public virtual async Task<int?> GetDefaultShelfLifeDaysAsync(int productId)
{
    return await _genericAttributeService.GetAttributeAsync<Product, int?>(productId,
        ProductionLabelsDefaults.DefaultShelfLifeDaysAttributeKey);
}
```
- `PrepareProductionLabelsProductModelAsync`: inside the existing `if (productId > 0)` block, add
  `model.DefaultShelfLifeDays = await GetDefaultShelfLifeDaysAsync(productId);`.
- `PrepareProductionBatchModelAsync`: add an `else` to the existing
  `if (productId == 0) ... PrepareAvailableProductsAsync(...)` — when `productId > 0`, set
  `model.DefaultShelfLifeDays = await GetDefaultShelfLifeDaysAsync(productId);`. A plain `if/else` on
  one condition (product known vs. unknown at popup-open), not the fragile `Locales.Any()`-style
  branching `SaveProductInfo` has.

**`ProductionLabelsProductModel.cs`** — new flat (non-`Locales`) property:
```csharp
[NopResourceDisplayName("Plugins.Misc.ProductionLabels.Fields.DefaultShelfLifeDays")]
public int? DefaultShelfLifeDays { get; set; }
```

**`ProductionBatchModel.cs`** — new property, not user-editable (rendered as a hidden field only, feeds
the popup's client-side script):
```csharp
public int? DefaultShelfLifeDays { get; set; }
```
Not present on the `ProductionBatch` entity, so it needs no `MapperConfiguration.cs` change — AutoMapper
already leaves unmatched *source* members unmapped without complaint in the model→entity direction
(the existing precedent: `ProductName`/`AvailableProducts` are already unmapped source-only members on
this same model).

**New validator `Admin/Validators/ProductionLabelsProductValidator.cs`** (naming mirrors
`ProductionBatchValidator`, i.e. drops "Model"):
```csharp
public class ProductionLabelsProductValidator : BaseNopValidator<ProductionLabelsProductModel>
{
    public ProductionLabelsProductValidator(ILocalizationService localizationService)
    {
        RuleFor(model => model.DefaultShelfLifeDays)
            .GreaterThan(0)
            .When(model => model.DefaultShelfLifeDays.HasValue)
            .WithMessageAwait(localizationService.GetResourceAsync("Plugins.Misc.ProductionLabels.Fields.DefaultShelfLifeDays.GreaterThanZero"));
    }
}
```
No manual DI registration needed — validators auto-register via
`services.AddValidatorsFromAssemblies(...)` over every `Nop*`-prefixed assembly part
(`src/Presentation/Nop.Web.Framework/Infrastructure/Extensions/ServiceCollectionExtensions.cs:348-353`),
the same mechanism that already wires up `ProductionBatchValidator` with zero registration code.

**`ProductionLabelsAdminController.SaveProductInfo`** — add the missing `ModelState.IsValid` check
(correction above) and save the new field **outside** the `Locales.Any()`/fallback branch (spec's
explicitly allowed alternative):
```csharp
if (!ModelState.IsValid)
{
    foreach (var error in ModelState.Values.SelectMany(state => state.Errors))
        _notificationService.ErrorNotification(error.ErrorMessage);

    return RedirectToAction("Edit", "Product", new { id = model.ProductId, area = AreaNames.ADMIN });
}

// ... existing if (model.Locales.Any()) { ... } else { ... } block, unchanged ...

// not per-language (spec §5/§6) — saved once, regardless of which branch above ran
await _genericAttributeService.SaveAttributeAsync(product,
    ProductionLabelsDefaults.DefaultShelfLifeDaysAttributeKey, model.DefaultShelfLifeDays);
```
`INotificationService.ErrorNotification` is TempData-backed, so it survives the subsequent redirect.
**Trade-off, stated explicitly**: because this tab has no mechanism to redisplay itself with the
just-typed values on a validation failure (it always redirects to `Product/Edit`), a rejected save
discards *all three* fields' just-typed input for that request, not just the invalid one — the admin
must re-enter and resubmit. This is a pre-existing architectural limit of this tab (not introduced by
this ticket) that a validator now actually enforces.

**New read endpoint**, delegating to the factory (thin controller, matching every other action here):
```csharp
[CheckPermission(ProductionLabelsPermissionConfigManager.PRODUCTION_LABELS_CREATE,
    CheckPermissionAttribute.CheckPermissionResultType.Json)]
public virtual async Task<IActionResult> GetDefaultShelfLifeDays(int productId)
{
    var defaultShelfLifeDays = await _productionLabelsAdminModelFactory.GetDefaultShelfLifeDaysAsync(productId);

    return Json(new { DefaultShelfLifeDays = defaultShelfLifeDays });
}
```
No `[HttpPost]` (matches the existing unattributed GET actions in this controller — `List()`,
`ProductionBatchCreatePopup(int)`). The explicit `CheckPermissionResultType.Json` is a verified,
non-obvious necessity: `CheckPermissionAttribute`'s default resolution maps **every** GET request to
`Html` (a redirect to `AccessDenied`) regardless of whether it's an AJAX call — without this override,
a permission failure on this JSON endpoint would hand the popup's `$.ajax` call an HTML redirect body
instead of JSON. No new route registration needed — the generic
`{area:exists}/{controller}/{action}/{id?}` route already covers it, the same way every other action on
this controller (bar `List`) resolves today.

**Views:**
- `Admin/Views/Components/ProductionLabels.cshtml` — one new `form-group row` (label/`nop-editor`/
  `asp-validation-for`) for `DefaultShelfLifeDays`, placed **outside** the
  `Html.LocalizedEditorAsync(...)` call (it is not per-language), inside the same
  `<form asp-action="SaveProductInfo">`.
- `Admin/Views/ProductionBatchCreatePopup.cshtml` — add
  `<input asp-for="DefaultShelfLifeDays" type="hidden" />` and a `<script>` block:
  - Both date inputs are native `<input type="date">` (`EditorTemplates/Date.cshtml`), format
    `yyyy-MM-dd` — no datepicker plugin involved, so the JS is plain string/Date arithmetic.
  - Track a `bestBeforeManuallyEdited` flag set only by a genuine `change` event on
    `#BestBeforeDateUtc`; programmatic `.val(...)` writes from the prefill logic do not trigger
    `change`, so the flag only flips on a real user edit — implementing the "don't clobber a manual
    edit" default directly and correctly.
  - On `#ProductionDateUtc` `change`: if `#DefaultShelfLifeDays` has a numeric value and
    `bestBeforeManuallyEdited` is false, recompute and set `#BestBeforeDateUtc`.
  - Only when `Model.AvailableProducts.Any()` (the standalone-flow branch): on `#ProductId` `change`,
    `GET GetDefaultShelfLifeDays?productId=...`, write the result into the hidden
    `#DefaultShelfLifeDays` field, **reset `bestBeforeManuallyEdited = false`** (developer-confirmed:
    switching products is a fresh context, prefill should re-arm), and re-run the prefill.
  - Failure handling: no `error` callback needed — `parseInt(undefined-or-empty, 10)` is `NaN`, which
    the prefill function already treats as "no default," matching spec §10's failure scenario.

**Migration** — `Data/Migrations/DefaultShelfLifeDaysMigration.cs`, mirroring
`NutritionalValuesMigration.cs`:
```csharp
[NopMigration("2026-09-04 12:00:00", "Misc.ProductionLabels default shelf-life days", MigrationProcessType.Update)]
public class DefaultShelfLifeDaysMigration : MigrationBase
{
    public override void Up()
    {
        if (!DataSettingsManager.IsDatabaseInstalled())
            return;

        this.AddOrUpdateLocaleResource(new Dictionary<string, string>
        {
            ["Plugins.Misc.ProductionLabels.Fields.DefaultShelfLifeDays"] = "Default shelf-life (days)",
            ["Plugins.Misc.ProductionLabels.Fields.DefaultShelfLifeDays.Hint"] = "The number of days from production to best-before, used to prefill new batches; leave blank for no default.",
            ["Plugins.Misc.ProductionLabels.Fields.DefaultShelfLifeDays.GreaterThanZero"] = "Default shelf-life (days) must be greater than zero."
        });
    }

    public override void Down()
    {
        //nothing - forward-only
    }
}
```
Timestamp only needs to sort after `SchemaMigration`'s `2026-09-04 00:00:00` and be unique; exact value
is `implementation-planner`'s call. `this.AddOrUpdateLocaleResource` is the synchronous `IMigration`
extension (`src/Presentation/Nop.Web.Framework/Extensions/MigrationExtensions.cs:213`), confirmed
distinct from `ILocalizationService.AddOrUpdateLocaleResourceAsync` — matches the spec's own
correction.

**`ProductionLabelsPlugin.cs`**:
- `InstallAsync()` — add the **same three keys** to its dictionary (the correction above — required for
  fresh installs, since `MigrationProcessType.Update` never runs `Up()` at install time).
- `UninstallAsync()` — one bulk sweep, **not** per-language (per spec §7, the simpler of the two allowed
  options):
  ```csharp
  await _genericAttributeService.DeleteAttributesAsync<Product>(ProductionLabelsDefaults.DefaultShelfLifeDaysAttributeKey);
  ```
  `DeleteAttributesAsync<TEntity>(string key)` is a single `DELETE ... WHERE Key = @key AND KeyGroup =
  @keyGroup` — no per-product iteration needed.

**`plugin.json`** — bump `Version` from `5.00.1` to `5.00.2` (same patch-bump convention
`Nop.Plugin.Misc.Ingredients` used for its own analogous change).

**Permissions** — no new permission. `SaveProductInfo` stays gated by `PRODUCTION_LABELS_CREATE`
(unchanged); the new read endpoint is gated by the same, matching `ProductionBatchCreatePopup`'s own
gate.

**Documentation** — `Docs/BusinessLogic/product-production-labels.md` needs a new section (after
"Storage conditions and country of origin are per-product, per-language admin input") documenting:
optional, per-product (not per-language), admin-only, drives only a client-side prefill, never itself
persisted onto a `ProductionBatch` row.

### Simplicity check

Smallest version that works: one `GenericAttribute` key, one factory helper reused three ways, one
validator, one controller `ModelState` check that should already have existed, one thin JSON read
action, client-side date math against native `<input type="date">` values (no datepicker library to
fight), one Update migration + `InstallAsync` duplication for the locale strings. The only things beyond
a bare minimum are the `ModelState.IsValid` addition to `SaveProductInfo` (necessary to make the spec's
own "reject, don't clamp" requirement real) and the `CheckPermissionResultType.Json` override
(necessary for the new endpoint to fail as JSON instead of an HTML redirect body). Nothing else was
added speculatively.

### Blast radius

`IGenericAttributeService`, `AddOrUpdateLocaleResource`/`AddOrUpdateLocaleResourceAsync`,
`MigrationProcessType.Update`, `CheckPermissionAttribute` — all shared, generic core mechanisms; this
change only adds new call-site usages, it does not alter their behavior for any other caller.
`SaveProductInfo`'s new `ModelState.IsValid` check is scoped to this one action; it does not touch
`ProductionBatchCreatePopup`'s existing, separate check. `ProductionLabelsDefaults.DefaultShelfLifeDaysAttributeKey`
is a new, uniquely-prefixed key — grepped against the existing sibling key usages, no collision and no
other reader of this new key exists anywhere else in the solution.

### Installed-store impact

**Schema**: none — `GenericAttribute` is schema-free. **Locale resources**: a store already running
`Misc.ProductionLabels` (GIL-005) will not see the new keys until `plugin.json`'s version bump is
deployed and `PluginService.UpdatePluginsAsync()` detects the version mismatch on next app start,
running `DefaultShelfLifeDaysMigration.Up()`. Until then the field is simply absent from the admin UI
(no visible gap, since it doesn't exist in already-deployed code either). **Rolling deploy (ECS)**:
safe — same pattern already shipped for `NutritionalValuesMigration` in this exact environment; nothing
structural changes, so a task briefly running old code alongside one that already migrated is
unaffected. **Existing products**: none has the new `GenericAttribute` row until an admin opts in.
**Uninstall**: the new sweep removes every product's `DefaultShelfLifeDays` row.

### Resolved during Gate 1

Switching the selected product in the standalone popup's dropdown **resets** the "manually edited
Best-before date" flag — developer-confirmed: a new product selection is a fresh context, so the
prefill re-arms until the admin manually edits Best-before again for that product.

**Approved by:** Mateusz Nycz (developer)
**Date:** 2026-09-04
**Revision notes:** none — approved as proposed, with one open UX question (reset-on-product-change)
resolved inline during Gate 1 rather than sent back for a second `ddd-modeler` pass.
