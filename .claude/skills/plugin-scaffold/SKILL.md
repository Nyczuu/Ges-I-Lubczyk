---
name: plugin-scaffold
description: >-
  Use this when creating a new plugin in this repo, to generate the skeleton by mirroring an existing
  plugin rather than writing project structure from memory. It produces the csproj, plugin.json, plugin
  class, defaults, settings, DI startup, route provider, and folder layout in the shapes this codebase
  actually uses — including the build wiring that has no compile-time symptom when it is wrong.
---

# Plugin Scaffold

Generates a new `src/Plugins/Nop.Plugin.{Group}.{Name}` skeleton.

**Mirror, do not invent.** Before generating, read the closest existing plugin and copy its shape:

| Need | Mirror |
|---|---|
| Full plugin with data, migrations, admin UI, views | `src/Plugins/Nop.Plugin.Misc.RFQ` |
| Widget | `src/Plugins/Nop.Plugin.Widgets.GoogleAnalytics` |
| Payment | `src/Plugins/Nop.Plugin.Payments.CheckMoneyOrder` |
| Shipping | `src/Plugins/Nop.Plugin.Shipping.FixedByWeightByTotal` |
| External media/storage | `src/Plugins/Nop.Plugin.Misc.AzureBlob` |

Full rules: `plugin-standards-check`. Procedure this follows: the 8 steps in
[`Docs/ai-harness/02-extensibility-and-plugins.md`](../../../Docs/ai-harness/02-extensibility-and-plugins.md).

## Before generating — decide and state

1. **`{Group}`** — `Payments`, `Shipping`, `Tax`, `Misc`, `Widgets`, `DiscountRules`, `ExternalAuth`,
   `MultiFactorAuth`.
2. **Plugin interface** — the *narrowest* one that fits. `IMiscPlugin` is the last resort and needs a
   reason.
3. **`SystemName`** — `{Group}.{Name}`. Permanent; changing it later loses the plugin's stored
   configuration on every store.
4. **Does it own data?** If yes, the skeleton includes `Domains/`, `Data/Migrations/`, and a service over
   `IRepository<T>`. If no, leave those out rather than scaffolding empty folders.

## Files to generate

```
src/Plugins/Nop.Plugin.{Group}.{Name}/
├── Nop.Plugin.{Group}.{Name}.csproj
├── plugin.json
├── logo.png                              (placeholder; replace before release)
├── {Name}Plugin.cs                       BasePlugin + chosen interface
├── {Name}Defaults.cs                     route names, cache keys, system name constants
├── {Name}Settings.cs                     ISettings
├── Infrastructure/
│   ├── NopStartup.cs                     INopStartup — the only place services are registered
│   └── RouteProvider.cs                  IRouteProvider for the configuration route
├── Controllers/{Group}{Name}Controller.cs
├── Models/                               view models deriving from BaseNopModel
├── Validators/                           BaseNopValidator<TModel>
├── Views/
│   ├── Configure.cshtml                  _ConfigurePlugin layout
│   └── _ViewImports.cshtml               copied from an existing plugin
├── Domains/                              (only if the plugin owns data)
├── Data/Migrations/                      (only if the plugin owns data)
└── Services/
```

## csproj wiring — the part with no compile-time symptom

- `<TargetFramework>net10.0</TargetFramework>`
- `<OutputPath>` **and** `<OutDir>` both `$(SolutionDir)\Presentation\Nop.Web\Plugins\{Group}.{Name}`
- `<CopyLocalLockFileAssemblies>false</CopyLocalLockFileAssemblies>` unless NuGet DLLs must ship
- `ProjectReference` to `Nop.Web.Framework.csproj`, or `Nop.Web.csproj` if the plugin needs admin-area
  infrastructure — match the mirrored plugin
- the `NopTarget` post-build target invoking `Build/ClearPluginAssemblies.proj`
- **every** view, `logo.png`, and `plugin.json` listed as `<Content>` with
  `<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>`

## plugin.json

```json
{
  "Group": "{Group}",
  "FriendlyName": "{Friendly name}",
  "SystemName": "{Group}.{Name}",
  "Version": "5.00.1",
  "SupportedVersions": [ "5.00" ],
  "Author": "Ges I Lubczyk",
  "DisplayOrder": 1,
  "FileName": "Nop.Plugin.{Group}.{Name}.dll",
  "Description": "<one line>"
}
```

## Install/uninstall — generate both halves together

Scaffold `InstallAsync` and `UninstallAsync` as a matched pair from the start: settings saved/deleted,
locale resources added/removed, permission record declared/removed, scheduled task inserted/deleted. Both
end with `await base.X()`. Generating install now and uninstall "later" is how the asymmetry ships.

## After generating

- [ ] Add the project to `src/NopCommerce.sln`.
- [ ] `dotnet build src/NopCommerce.sln --configuration Release` and confirm the output landed in
      `Presentation/Nop.Web/Plugins/{Group}.{Name}` with `plugin.json` beside the DLL.
- [ ] Install it in the running app, then uninstall it, and confirm nothing is left behind.
