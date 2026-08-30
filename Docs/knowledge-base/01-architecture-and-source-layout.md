# Architecture & Source Layout

Source: adapted from `developer/tutorials/architecture-of-nopCommerce.html` and
`developer/tutorials/source-code-organization.html`, verified against `src/`.

## Onion architecture — dependency direction

nopCommerce follows onion/clean architecture: dependencies point **inward only**. A project may
reference projects closer to the center; the center never references outward.

```
                 ┌─────────────────────────────┐
                 │   Nop.Web (Presentation)     │  ASP.NET Core app, admin area, Startup project
                 ├─────────────────────────────┤
                 │   Nop.Web.Framework          │  Shared MVC infra (both Web + Admin)
                 ├─────────────────────────────┤
                 │   Nop.Services               │  Business logic, validation, facades
                 ├─────────────────────────────┤
                 │   Nop.Data                   │  IRepository<T>, Linq2DB, FluentMigrator
                 ├─────────────────────────────┤
                 │   Nop.Core                   │  Domain entities, caching, events, no deps
                 └─────────────────────────────┘
```

- **`Nop.Core`** (`src/Libraries/Nop.Core`) — innermost. Domain entities (`Nop.Core.Domain.*`),
  `BaseEntity`, caching abstractions, the event bus, `IEngine`/`EngineContext`. Zero project
  references.
- **`Nop.Data`** (`src/Libraries/Nop.Data`) — depends only on `Nop.Core`. All read/write access:
  `IRepository<TEntity>`, Linq2DB data providers, FluentMigrator migrations and entity builders.
- **`Nop.Services`** (`src/Libraries/Nop.Services`) — depends on `Nop.Core` + `Nop.Data`. Business
  logic, calculations, `ISettings` consumers, plugin infrastructure (`BasePlugin`, `IPlugin`).
- **`Nop.Web.Framework`** (`src/Presentation/Nop.Web.Framework`) — shared MVC building blocks
  (`BaseNopModel`, `NopViewComponent`, TagHelpers, `INopUrlHelper`) used by both the storefront and
  the admin area.
- **`Nop.Web`** (`src/Presentation/Nop.Web`) — the startup project. Storefront controllers/views +
  the **Admin area** (`Areas/Admin`) live here, not in a separate assembly.
- **`\Plugins`** — solution folder at repo root (`src/Plugins/Nop.Plugin.{Group}.{Name}`), physically
  built to `Presentation/Nop.Web/Plugins/{Group}.{Name}` via each `.csproj`'s
  `<OutDir>$(SolutionDir)\Presentation\Nop.Web\Plugins\...</OutDir>`.
- **`\Tests`** — `Nop.Tests` (shared test helpers, `BaseNopTest`), plus one test project per library
  layer (`Nop.Core.Tests`, `Nop.Data.Tests`, `Nop.Services.Tests`, `Nop.Web.Tests`). NUnit.

## Practical implication for an AI assistant

1. **Never** add a reference from `Nop.Core` or `Nop.Data` outward. A domain entity in `Nop.Core`
   must not know about `Nop.Services` or MVC types.
2. Business rules go in `Nop.Services`, never in controllers. Controllers orchestrate services and
   map view models; they are not where validation or calculation logic lives.
3. A new bounded feature for this project (e.g. gastronomy batch/expiry tracking) should default to
   living in a **plugin** (`src/Plugins/Nop.Plugin.Misc.GastronomyCompliance` or similar), which
   itself may contain its own mini onion (Domain → Data → Services → Components/Controllers) — see
   [05-plugin-system.md](05-plugin-system.md).
4. Solution file is `src/NopCommerce.sln`; open/build from `src/`, not repo root.
