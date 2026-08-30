# Settings, Permissions, Validation

Source: adapted from `developer/tutorials/settings.html`, `developer/tutorials/permissions.html`
(4.80+ variant — this repo is 5.00), `developer/tutorials/data-validation.html`.

## Settings — `ISettings` classes, not raw key/value calls

Two ways exist; **prefer the strongly-typed class** for anything beyond a one-off flag:

```csharp
public partial class GastronomyComplianceSettings : ISettings
{
    public int DefaultShelfLifeDaysCanned { get; set; }
    public int DefaultShelfLifeDaysRefrigerated { get; set; }
    public bool RequireBatchNumberOnOrder { get; set; }
}
```

Inject `GastronomyComplianceSettings` directly via constructor wherever it's needed — nopCommerce's
settings infrastructure resolves and populates it from the `Setting` table automatically per store.
Save/update via `ISettingService.SaveSettingAsync(settingsInstance)`. Only fall back to the raw
`ISettingService.GetSettingByKeyAsync`/`SetSettingAsync(key, value)` pair for genuinely dynamic,
non-typed keys.

## Permissions (nopCommerce 4.80+ — the version relevant to this repo)

```csharp
public partial class GastronomyCompliancePermissionConfigManager : IPermissionConfigManager
{
    public const string MANAGE_BATCH_DATA = "GastronomyCompliance.ManageBatchData";

    public IList<PermissionConfig> AllConfigs => new List<PermissionConfig>
    {
        new("Manage batch & expiry data", MANAGE_BATCH_DATA, nameof(StandardPermission.Catalog),
            NopCustomerDefaults.AdministratorsRoleName)
    };
}
```

Records are installed into the DB automatically — no manual `InstallPermissionsAsync` call needed
(that pattern is the pre-4.80 `IPermissionProvider` approach; do not generate it for this codebase).
Remove the permission explicitly in `UninstallAsync`:

```csharp
var permission = (await _permissionService.GetAllPermissionRecordsAsync())
    .FirstOrDefault(x => x.SystemName == GastronomyCompliancePermissionConfigManager.MANAGE_BATCH_DATA);
await _permissionService.DeletePermissionRecordAsync(permission);
```

Check permissions either imperatively —

```csharp
if (await _permissionService.AuthorizeAsync(GastronomyCompliancePermissionConfigManager.MANAGE_BATCH_DATA))
    // ...
```

— or declaratively on a controller action:

```csharp
[CheckPermission(GastronomyCompliancePermissionConfigManager.MANAGE_BATCH_DATA)]
public virtual async Task<IActionResult> Configure() { ... }
```

Built-in permissions live on `StandardPermission` (`Nop.Services.Security`) — reuse those
(`StandardPermission.Configuration.MANAGE_SETTINGS`, etc.) instead of inventing a duplicate for
functionality that already has a first-party permission.

## Validation — FluentValidation, not DataAnnotations

```csharp
public partial class BatchInfoValidator : BaseNopValidator<BatchInfoModel>
{
    public BatchInfoValidator(ILocalizationService localizationService)
    {
        RuleFor(m => m.BatchNumber)
            .NotEmpty()
            .WithMessageAwait(localizationService.GetResourceAsync("Admin.Catalog.Products.Fields.BatchNumber.Required"));
        RuleFor(m => m.ExpirationDate).GreaterThan(DateTime.UtcNow).When(m => m.ExpirationDate.HasValue);
    }
}
```

ASP.NET Core resolves and runs the matching validator automatically on POST — never hand-roll
`ModelState.AddModelError` checks for rules a validator could express, and never reach for
`[Required]`/`[StringLength]` DataAnnotations attributes on a nopCommerce view model.
