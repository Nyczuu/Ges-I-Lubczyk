# Plugin System

Source: adapted from `developer/plugins/index.html`, `developer/tutorials/description-of-plugin-system.html`,
`developer/plugins/how-to-write-plugin-4.90.html`, `developer/plugins/plugin.json.html`.
Verified against `src/Plugins/*` (25+ real plugins in this repo).

## Project skeleton

- Location: `src/Plugins/Nop.Plugin.{Group}.{Name}` (e.g. `Nop.Plugin.Payments.PayPalCommerce`).
  `{Group}` is a category (`Payments`, `Shipping`, `Tax`, `Misc`, `Widgets`, `DiscountRules`,
  `ExternalAuth`, `MultiFactorAuth`); `{Name}` is the specific plugin. Not a hard requirement, but
  breaking it makes a plugin harder to place correctly in the admin "Local Plugins" grouping.
- `.csproj` build output is redirected out of the normal `bin` folder into
  `$(SolutionDir)\Presentation\Nop.Web\Plugins\{Group}.{Name}` so the plugin's static assets
  (Content/, Scripts/, Views/) ship alongside the DLL without a copy step.
- **`plugin.json`** (required, must have `Copy to Output Directory = Copy if newer`):
  ```json
  {
    "Group": "Misc",
    "FriendlyName": "Gastronomy Compliance",
    "SystemName": "Misc.GastronomyCompliance",
    "Version": "5.00.1",
    "SupportedVersions": [ "5.00" ],
    "Author": "Your Company",
    "DisplayOrder": 1,
    "FileName": "Nop.Plugin.Misc.GastronomyCompliance.dll",
    "Description": "Batch/expiry tracking and dietary tagging for jarred/canned meal products."
  }
  ```
  `SystemName` must be globally unique — never register two plugins with the same value.
  `SupportedVersions` must include the running nopCommerce version or the plugin won't load.
  Bump `Version` whenever a new migration is added (drives `UpdateAsync`).

## The one required class: IPlugin

Every plugin has exactly one class implementing `IPlugin` (`Nop.Services.Plugins`), almost always via
`BasePlugin`:

```csharp
public class GastronomyCompliancePlugin : BasePlugin, IMiscPlugin
{
    public override async Task InstallAsync()
    {
        await _settingService.SaveSettingAsync(new GastronomyComplianceSettings { DefaultShelfLifeDays = 730 });
        await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
        {
            ["Plugins.Misc.GastronomyCompliance.Fields.BatchNumber"] = "Batch number"
        });
        await base.InstallAsync(); // always call base — never hide it
    }

    public override async Task UninstallAsync()
    {
        await _settingService.DeleteSettingAsync<GastronomyComplianceSettings>();
        await base.UninstallAsync();
    }

    public override async Task UpdateAsync(string currentVersion, string targetVersion) { /* migrate settings/data */ }
}
```

`PreparePluginToUninstallAsync()` runs *before* uninstall — use it to block removal when another
installed plugin still depends on this one.

## Handling requests (Controllers / Models / Views)

- Controller: `{Group}{Name}Controller`, derived from `BasePluginController` (or plain controller
  with `[Area(AreaNames.ADMIN)]`, `[AuthorizeAdmin]`, `[AutoValidateAntiforgeryToken]`).
- View: `Views/Configure.cshtml`, `Build Action = Content`, `Copy to Output Directory = Copy always`,
  uses the `_ConfigurePlugin` layout. `_ViewImports.cshtml` copied from any existing plugin.
- Register the configuration route via a `RouteProvider : IRouteProvider` (auto-discovered by
  `ITypeFinder`, no manual registration list) and expose its name as a constant on a `{Name}Defaults`
  class — never hardcode route name strings at call sites:
  ```csharp
  public class GastronomyComplianceDefaults
  {
      public static class Route
      {
          public static string Configuration => "Plugin.Misc.GastronomyCompliance.Configure";
      }
  }
  ```
- `BasePlugin.GetConfigurationPageUrl()` returns this route's URL via `INopUrlHelper` — implement it
  so the "Configure" button in Local Plugins works.

## Lifecycle summary

| Method | When | Must call `base.X()`? |
|---|---|---|
| `InstallAsync` | Plugin installed from Local Plugins | Yes, at the end |
| `UninstallAsync` | Plugin uninstalled | Yes, at the end |
| `UpdateAsync(current, target)` | `plugin.json` `Version` increased since last load | No base call required, but do run pending data migrations here if not handled purely by the migration runner |
| `PreparePluginToUninstallAsync` | Just before `UninstallAsync`, validation hook | No |

Installed plugin state lives in `\App_Data\plugins.json` — never hand-edit this file; it's
managed by the plugin manager.

## Visual Studio template

nopCommerce publishes an official VS project template that scaffolds the folders
(Controllers/Views/Models/Infrastructure/PluginNopStartup.cs/_ViewImports.cshtml/plugin.json) —
prefer generating new plugins from that template's shape (mirrored from any existing
`src/Plugins/Nop.Plugin.Misc.*` project in this repo) over hand-rolling structure from scratch.
