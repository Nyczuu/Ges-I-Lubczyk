# nopCommerce Developer Knowledge Base — Index

Adapted, condensed reference distilled from `docs.nopcommerce.com/en/developer/*` and cross-checked
against the actual source tree in this repository (`src/`, nopCommerce **5.00**, target framework
`net10.0`, solution `src/NopCommerce.sln`). Written for AI coding agents (Claude Code, Copilot) working
on this fork. Each file is self-contained; code snippets are verified against real files in `src/`
where noted as `[verified: <path>]`.

Consumed by the AI Harness in [`../ai-harness/`](../ai-harness/00-system-instructions.md) — that folder
holds the *behavioral* rules for the assistant; this folder holds the *factual* reference material.

## Files

| File | Covers |
|---|---|
| [01-architecture-and-source-layout.md](01-architecture-and-source-layout.md) | Onion architecture, project dependency graph, `\Libraries`, `\Presentation`, `\Plugins`, `\Tests` |
| [02-dependency-injection-and-typefinder.md](02-dependency-injection-and-typefinder.md) | `INopStartup`, `ITypeFinder`, `IEngine`/`EngineContext`, registration order |
| [03-data-access-linq2db-fluentmigrator.md](03-data-access-linq2db-fluentmigrator.md) | `IRepository<T>`, Linq2DB, FluentMigrator, `NopEntityBuilder<T>`, migration attributes |
| [04-extending-core-entities.md](04-extending-core-entities.md) | The two official ways to add data to core entities: schema migration vs. `GenericAttribute` |
| [05-plugin-system.md](05-plugin-system.md) | `IPlugin`/`BasePlugin`, `plugin.json`, project layout, install/uninstall lifecycle |
| [06-plugin-types-reference.md](06-plugin-types-reference.md) | `IPaymentMethod`, `IShippingRateComputationMethod`, `IWidgetPlugin`, `ITaxProvider`, etc. |
| [07-events-and-scheduled-tasks.md](07-events-and-scheduled-tasks.md) | `IEventPublisher`/`IConsumer<T>`, entity CRUD events, `IScheduleTask` |
| [08-settings-permissions-validation.md](08-settings-permissions-validation.md) | `ISettings`/`ISettingService`, ACL permissions (`IPermissionConfigManager`), FluentValidation |
| [09-theming-and-design.md](09-theming-and-design.md) | Theme folder structure, layouts, widget zones, resource bundling |
| [10-configuration-appsettings.md](10-configuration-appsettings.md) | `appsettings.json` sections, env var overrides, `DataConfig` |
| [11-deployment-docker-iis-azure.md](11-deployment-docker-iis-azure.md) | Native Dockerfile walkthrough, hosting options, plugin build gotchas |
| [12-coding-standards.md](12-coding-standards.md) | .editorconfig-enforced style rules, naming conventions |
| [13-testing.md](13-testing.md) | `Nop.Tests` structure, NUnit + FluentAssertions pattern |

## Version grounding (important)

This repository is **not** a plain "latest v4.x" checkout — verified facts as of this scan:

- `global.json` pins SDK **`10.0.100`**, `rollForward: latestFeature` → target framework `net10.0`.
- Recent commits reference migrating **schema from < 4.80 up to 5.00** — this is the nopCommerce
  **5.00** `develop` branch.
- `postgresql-docker-compose.yml` and `mysql-docker-compose.yml` exist at repo root alongside the
  default SQL Server-oriented `docker-compose.yml` — PostgreSQL is a **first-class, natively
  supported** `DataProvider`, not a plugin or bolt-on.
- Data access is **Linq2DB + FluentMigrator** (since 4.30) — there is no Entity Framework Core
  anywhere in this codebase. Never suggest `DbContext`, `OnModelCreating`, EF migrations, or
  `Add-Migration`.

Any suggestion that contradicts a fact in this index should be treated as suspect — re-verify against
`src/` before acting on it.
