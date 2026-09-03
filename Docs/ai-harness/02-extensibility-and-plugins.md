# Extensibility & Plugins — Step-by-Step Rules

Full reference: [knowledge-base/04](../knowledge-base/04-extending-core-entities.md),
[05](../knowledge-base/05-plugin-system.md), [06](../knowledge-base/06-plugin-types-reference.md),
[09](../knowledge-base/09-theming-and-design.md).

## Decision tree: where does new functionality go?

```
Is it a new business capability (payment, shipping, tax, widget, admin tool, background job)?
  → New plugin: src/Plugins/Nop.Plugin.{Group}.{Name}

Is it new data on an EXISTING core entity (Product, Order, Category, Customer, ...)?
  → See "Extending a core entity" below — still lives in a plugin's migration, not core, wherever possible.

Is it visual/layout only (branding, colors, header/footer links)?
  → Theme edit under src/Presentation/Nop.Web/Themes/{YourTheme}

Is it markup injected INTO an existing page from outside (badge, banner, tracking snippet)?
  → IWidgetPlugin targeting a PublicWidgetZones constant — never a layout fork.

Does it require changing behavior of the engine itself (DI container, migration runner, routing pipeline)?
  → STOP. Confirm with a human first. This is core-modification territory.
```

## Extending an existing plugin vs. adding a new plugin that depends on it

Applies once a plugin already exists (e.g. `Nop.Plugin.Misc.Ingredients`) and a new capability
touches the same data.

```
Does the new data belong to the SAME entity, always needed together with what's already there,
with no independent install/uninstall lifecycle of its own (e.g. calories per ingredient)?
  → Extend the existing plugin: new column via a schema migration on its own entity.

Does the new capability have an independent lifecycle - could be installed, uninstalled, or rolled
back on its own without breaking the plugin it reads from (e.g. a "calorie total" widget on the
product page, built on top of the ingredient list)?
  → New plugin. Reference the owning plugin's PUBLIC service interface only (e.g.
    `IIngredientService`) via ProjectReference - never its internals, never its repository/DbContext
    directly.
```

Rule of thumb: uninstalling the new (dependent) plugin must leave the plugin it depends on fully
functional. Uninstalling the depended-on plugin while the dependent one stays installed must degrade
gracefully (empty/null reads), not throw.

## Writing a new plugin — required steps in order

1. Create `src/Plugins/Nop.Plugin.{Group}.{Name}` class library, target `net10.0` to match the
   solution. Reference `Nop.Web.Framework` (pulls in `Nop.Core`/`Nop.Data`/`Nop.Services`
   transitively). Set `<OutDir>$(SolutionDir)\Presentation\Nop.Web\Plugins\{Group}.{Name}</OutDir>`.
2. Add `plugin.json` (`Copy to Output Directory = Copy if newer`) with a globally-unique `SystemName`
   and `SupportedVersions` including `"5.00"`.
3. Add the domain/data layer if the plugin owns new data — domain class(es) extending `BaseEntity`
   (no navigation properties), a `NopEntityBuilder<T>` per entity, a `[NopMigration(...)]` (**not**
   `[NopSchemaMigration(...)]` — that attribute forces the migration to run before the DI container is
   available; every real plugin migration in this repo uses plain `NopMigration`) calling
   `this.CreateTableIfNotExists<T>()` (verified `MigrationBase` extension — **not**
   `Create.TableFor<T>()`, which appears in some older nopCommerce doc pages but does not exist in
   this codebase). See [knowledge-base/03](../knowledge-base/03-data-access-linq2db-fluentmigrator.md)
   for the full pattern.
4. Add the service layer — an interface + implementation injecting `IRepository<T>`, registered via
   an `Infrastructure/NopStartup.cs : INopStartup`.
5. Add the one required `IPlugin` class (via `BasePlugin`), choosing the **narrowest** applicable
   interface (`IPaymentMethod`, `IShippingRateComputationMethod`, `IWidgetPlugin`, `ITaxProvider`,
   `IDiscountRequirementRule`, `IExternalAuthenticationMethod`, `IMultiFactorAuthenticationMethod`,
   `IMiscPlugin` only as a last resort). Implement `InstallAsync`/`UninstallAsync` calling `base.X()`;
   seed settings + locale resources on install, remove them on uninstall.
6. If the plugin needs an admin configuration page: controller (`{Group}{Name}Controller`,
   `[Area(AreaNames.ADMIN)]`, `[AuthorizeAdmin]`, `[AutoValidateAntiforgeryToken]`) +
   `Views/Configure.cshtml` (`_ConfigurePlugin` layout) + a `RouteProvider : IRouteProvider` +
   `{Name}Defaults.Route.Configuration` constant. Wire `BasePlugin.GetConfigurationPageUrl()` via
   `INopUrlHelper`. **Every `return View(...)` in that controller needs the explicit
   `~/Plugins/{Group}.{Name}/...` virtual path** — this codebase has no view-location expander for
   plugin views, so the bare `View(model)` convention compiles but never resolves at runtime; see the
   `admin-ui-standards-check` checklist.
7. If the plugin needs an admin menu entry: subscribe to `AdminMenuCreatedEvent` via
   `IConsumer<AdminMenuCreatedEvent>` and call `eventMessage.RootMenuItem.InsertBefore(...)` guarded
   by a permission check. **Never** edit a sitemap/config file — that's the pre-4.80 mechanism and
   does not apply to this 5.00 codebase.
8. If the plugin needs a custom permission: `IPermissionConfigManager.AllConfigs` (4.80+ mechanism —
   records install automatically), remove the permission record explicitly in `UninstallAsync`.

## Extending a core domain entity — the two sanctioned mechanisms

Full decision matrix: [knowledge-base/04](../knowledge-base/04-extending-core-entities.md). Summary:

| Need | Mechanism | Touches core? |
|---|---|---|
| SQL-filterable/sortable/joinable structured field (e.g. an expiration date used in a "expiring soon" admin filter) | **Schema migration**: add the property to the core domain class in `Nop.Core.Domain.*`, then a `[NopSchemaMigration]` calling the idempotent `this.AddOrAlterColumnFor<TEntity>(x => x.Prop).AsString(255).Nullable()` extension | Yes — one additive, nullable property on the domain class; everything else (migration, view model, validator, view, controller mapping) stays outside core |
| Free-form/rarely-queried metadata (notes, flags, JSON blob) | **`GenericAttribute`** via `IGenericAttributeService.SaveAttributeAsync`/`GetAttributeAsync<T>` | No — zero schema change, zero core touch |

For either mechanism, still add the standard MVC round-trip pieces in the admin area: property on the
view model with `[NopResourceDisplayName("...")]`, a FluentValidation rule, a `_CreateOrUpdate.*`
partial view section, and explicit `Model → Entity`/`Entity → Model` mapping in the controller
wherever AutoMapper's default 1:1 convention doesn't already cover it.

## Themes

New theme = copy an existing theme folder under `src/Presentation/Nop.Web/Themes/`, rename it, update
`theme.json`, restyle `Content/`. Layout chain: `_Root.Head.cshtml` → `_Root.cshtml` →
`_ColumnsOne.cshtml`/`_ColumnsTwo.cshtml`. Override a specific partial/view component rather than
forking a whole layout file. Inject third-party or plugin markup via `IWidgetPlugin` + widget zones,
never by hand-editing shared layout files. Full detail:
[knowledge-base/09](../knowledge-base/09-theming-and-design.md).

## Red flags — stop and ask a human

- Editing anything under `src/Libraries/Nop.Data/Migrations/UpgradeToXXX/` (those are nopCommerce's
  own version-upgrade migrations, not a place for project-specific schema changes).
- Adding a project reference **from** `Nop.Core`/`Nop.Data` **to** anything.
- Introducing a second ORM, a raw ADO.NET `SqlCommand`, or Entity Framework anywhere.
- Any change to `NopEngine`, `EngineContext`, or the plugin loading pipeline itself.
