---
name: plugin-standards-check
description: >-
  Load this when creating a plugin or changing anything about an existing one — plugin.json, the
  BasePlugin class, install/uninstall, settings, DI registration, or the configuration route. Use it
  BEFORE writing the code: SystemName is a plugin's permanent identity and install/uninstall asymmetry
  leaves orphaned rows in every store that ever had the plugin, and neither failure is visible at
  compile time or in tests.
---

# Plugin Standards Check

Full docs: [`Docs/knowledge-base/05-plugin-system.md`](../../../Docs/knowledge-base/05-plugin-system.md),
[`06-plugin-types-reference.md`](../../../Docs/knowledge-base/06-plugin-types-reference.md), and the
8-step procedure in [`Docs/ai-harness/02-extensibility-and-plugins.md`](../../../Docs/ai-harness/02-extensibility-and-plugins.md).
This is the checklist form. To generate a new skeleton, use `plugin-scaffold`.

## Project

- [ ] Path `src/Plugins/Nop.Plugin.{Group}.{Name}`; `{Group}` is one of `Payments`, `Shipping`, `Tax`,
      `Misc`, `Widgets`, `DiscountRules`, `ExternalAuth`, `MultiFactorAuth`.
- [ ] Targets `net10.0`. References `Nop.Web.Framework.csproj` (pulls Core/Data/Services transitively),
      or `Nop.Web.csproj` when the plugin needs admin-area infrastructure — both patterns exist in
      `src/Plugins/`, roughly half each. Mirror whichever sibling plugin is closest to yours.
- [ ] Both `<OutputPath>` **and** `<OutDir>` set to
      `$(SolutionDir)\Presentation\Nop.Web\Plugins\{Group}.{Name}` — copy the pair from an existing
      plugin. Without it the DLL and static assets never reach the running app, and the plugin simply
      does not appear in Local Plugins.
- [ ] `CopyLocalLockFileAssemblies` is `false` unless the plugin has NuGet dependencies whose DLLs must
      ship with it — then `true`, deliberately.
- [ ] The `NopTarget` / `ClearPluginAssemblies` post-build target is present, copied from a sibling
      plugin. It strips framework assemblies that must not be duplicated in the plugin output.
- [ ] Every view, `logo.png`, and `plugin.json` listed as `<Content>` with
      `<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>`. A view that is not listed is
      missing at runtime with no build error.

## plugin.json

- [ ] Present, with `Copy to Output Directory = Copy if newer`.
- [ ] `SystemName` **globally unique and permanent**. Changing it later orphans every setting,
      permission, and installed-state row tied to the old name — the store silently loses the plugin's
      configuration. Treat it like a database primary key.
- [ ] `SupportedVersions` includes `"5.00"`, or the plugin will not load.
- [ ] `FileName` matches the built assembly.
- [ ] `Version` bumped whenever a migration is added (drives `UpdateAsync` and the migration runner).

## The IPlugin class

- [ ] Exactly one class, deriving from `BasePlugin`, implementing the **narrowest** applicable
      interface: `IPaymentMethod`, `IShippingRateComputationMethod`, `IWidgetPlugin`, `ITaxProvider`,
      `IDiscountRequirementRule`, `IExternalAuthenticationMethod`, `IMultiFactorAuthenticationMethod`.
      `IMiscPlugin` is the last resort and needs a stated reason (rule 4).
- [ ] `InstallAsync` and `UninstallAsync` both call `base.X()` at the end.
- [ ] `GetConfigurationPageUrl()` implemented via `INopUrlHelper` and the route constant, so the
      Configure button in Local Plugins works.
- [ ] `PreparePluginToUninstallAsync()` used if another plugin can depend on this one.

## Install/uninstall symmetry — check each pair

Everything `InstallAsync` creates, `UninstallAsync` removes. This is the single most commonly missed
item, because nothing fails when it is wrong:

| Installed | Removed in `UninstallAsync` |
|---|---|
| `SaveSettingAsync(new XSettings { ... })` | `DeleteSettingAsync<XSettings>()` |
| `AddOrUpdateLocaleResourceAsync(...)` | `DeleteLocaleResourcesAsync(...)` for the same keys/prefix |
| permission record (`IPermissionConfigManager`) | `DeletePermissionRecordAsync(...)` — installed automatically, **not** removed automatically |
| `InsertTaskAsync(new ScheduleTask { ... })` | delete that task row |

- [ ] Tables created by migrations: decide deliberately whether uninstall drops them. Dropping loses
      customer data; keeping leaves orphans. State the choice — do not leave it accidental.

## Dependency injection

- [ ] Registration goes through an `INopStartup` implementation in `Infrastructure/`, discovered by
      `ITypeFinder`. **Never** a bare `services.AddScoped<T>()` in `Program.cs`/`Startup` (rule 2).
- [ ] `Order` set deliberately — higher runs later and can override an earlier registration.
- [ ] `IRepository<T>` is not registered manually; the generic factory already resolves it.
- [ ] Nothing else is hand-wired into a master list either: `IRouteProvider`, `IConsumer<T>`,
      `IEntityBuilder`, `IOrderedMapperProfile`, `IStartupTask` are all auto-discovered. Editing a
      registration list means you are fighting the framework.

## Conventions

- [ ] `{Name}Defaults` constants class — route names, cache keys, system names. No inline magic strings.
- [ ] `{Group}{Name}Controller` naming.
- [ ] `{Name}Settings : ISettings` injected directly where needed, saved via `ISettingService`.

## Before calling plugin work done

- [ ] `SystemName` unchanged (for an existing plugin) or unique (for a new one).
- [ ] Every install action has its uninstall counterpart, checked pair by pair.
- [ ] `<OutDir>` correct and `plugin.json` copied to output.
- [ ] No ad-hoc DI registration anywhere in the diff.
- [ ] Narrowest plugin interface used, or a reason given for `IMiscPlugin`.
