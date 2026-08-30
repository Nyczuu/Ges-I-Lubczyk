# System Instructions — nopCommerce AI Development Harness

You are an AI coding assistant working inside a **nopCommerce 5.00** solution (verified: `global.json`
pins SDK `10.0.100`, target framework `net10.0`; `postgresql-docker-compose.yml` at repo root confirms
native PostgreSQL support). This is **not** a generic ASP.NET Core MVC codebase, and it is **not** a
legacy nopCommerce 4.x codebase either — do not pattern-match onto either. Read this file first, then
consult the other files in this folder and in [`../knowledge-base/`](../knowledge-base/00-index.md)
before writing code.

## ⚠️ Frame everything you do with this

> ### **We are building a SHOP.**
>
> **nopCommerce _is_ a shop, and its vocabulary _is_ our vocabulary. Food is only what we happen to
> sell.**

This is the project owner's own framing, given as a correction when an earlier session drifted from it.

There is **no separate domain layered on top of nopCommerce.** A jar of soup is a `Product`. A dietary
claim is a `ProductTag`. A shipping surcharge is an `IShippingRateComputationMethod`. Reach for the
shop's own concepts first, every time, and only invent something when a real requirement provably does
not fit one of them.

What this rules out, because each has already happened here:

- Inventing domain vocabulary nobody asked for. A glossary draft once shipped nine speculative food
  terms — cold chain, drained weight, shelf life — none of which any spec had put in play. They were cut.
- Treating a feature request as an invitation to model an industry. "Add an ingredient list" is a shop
  feature, not a food-compliance platform; EU labelling, allergen severity, and quantity percentages are
  separate decisions the owner makes, not scope you assume.
- Splitting docs into "platform" and "domain" halves. There is one vocabulary. See
  [`../Glossary/README.md`](../Glossary/README.md).

Scope grows when the owner grows it. Ask, or write it down as an open question — never widen quietly.

## Read order

1. This file (behavioral rules).
2. [01-architecture-and-standards.md](01-architecture-and-standards.md) — core patterns cheat sheet.
3. [02-extensibility-and-plugins.md](02-extensibility-and-plugins.md) — how to add features without
   touching the core.
4. [03-database-postgres.md](03-database-postgres.md) — when touching data access.
5. [04-deployment-aws-ecs.md](04-deployment-aws-ecs.md) — when touching Docker/infra/CI.
6. [05-domain-gastronomy-guidelines.md](05-domain-gastronomy-guidelines.md) — when the task is
   business/domain-specific (product, order, shipping features).
7. [`../knowledge-base/`](../knowledge-base/00-index.md) — deep reference for any topic above; each
   file there is scoped to one subsystem and cites verified file paths in this repo.

## Non-negotiable rules

1. **Never hallucinate generic ASP.NET Core MVC or EF Core patterns.** This codebase has no
   `DbContext`, no `OnModelCreating`, no `Add-Migration`/`Update-Database`, no `[Required]`
   DataAnnotations on view models, and no navigation properties on domain entities. Data access is
   **Linq2DB + FluentMigrator** exclusively (see
   [knowledge-base/03](../knowledge-base/03-data-access-linq2db-fluentmigrator.md)). If you're about
   to write `services.AddDbContext` or `modelBuilder.Entity<T>()`, stop — that's the wrong framework.
2. **Never register a service with a bare `services.AddScoped<T>()` call in `Program.cs`/`Startup`.**
   Every registration goes through an `INopStartup` implementation (see
   [knowledge-base/02](../knowledge-base/02-dependency-injection-and-typefinder.md)). This applies to
   core-level registrations, not just plugins.
3. **Core engine modification is minimized, not forbidden.** The project's explicit strategy is
   plugins + custom themes first. The one sanctioned exception is adding an additive, nullable
   property directly to a core domain class as part of the documented entity-extension pattern (see
   [knowledge-base/04](../knowledge-base/04-extending-core-entities.md)) — treat every other change
   to `src/Libraries/Nop.Core`, `Nop.Data`, `Nop.Services`, or `src/Presentation/Nop.Web(.Framework)`
   as something to flag and get explicit confirmation for before doing, since it makes upgrading
   nopCommerce itself materially harder. Default location for new functionality is
   `src/Plugins/Nop.Plugin.{Group}.{Name}`.
4. **Use the specific extension point, not the general one.** A payment integration is
   `IPaymentMethod`, not a controller hack. Admin menu items are `AdminMenuCreatedEvent`, not
   sitemap.config editing (that's pre-4.80). Widget rendering is `IWidgetPlugin` + a widget zone, not
   a layout fork. See [knowledge-base/05](../knowledge-base/05-plugin-system.md) and
   [06](../knowledge-base/06-plugin-types-reference.md).
5. **PostgreSQL is the target database for this project**, and it is nopCommerce's native
   `DataProvider: PostgreSQL`, not a compatibility shim — never suggest MySQL/SQL-Server-only syntax,
   T-SQL-specific hints (`WITH (NOLOCK)`, `sp_` procs), or an EF Core Npgsql provider.
6. **Deployment target is AWS ECS via Docker.** The repo's root `Dockerfile` already solves the
   "plugins not built" problem correctly (builds the whole `.sln` before publish) — do not regress
   that by changing the publish step to target only `Nop.Web.csproj`.
7. **Follow existing naming and folder conventions exactly** — `Nop.Plugin.{Group}.{Name}`,
   `{Name}Defaults` constants class, `{Group}{Name}Controller`, `plugin.json` fields, `.editorconfig`
   style (Allman braces, `var` everywhere, `_camelCase` private fields). See
   [knowledge-base/12](../knowledge-base/12-coding-standards.md).
8. **State your extensibility choice explicitly** when adding a field to a core entity: schema
   migration (queryable) vs. `GenericAttribute` (schema-free) — see
   [knowledge-base/04](../knowledge-base/04-extending-core-entities.md) for the decision rule, and
   apply the gastronomy-specific version of that call in
   [05-domain-gastronomy-guidelines.md](05-domain-gastronomy-guidelines.md).
9. **Verify against the checked-out source before trusting a doc snippet.** The knowledge base is
   adapted from nopCommerce's official docs (some pages dated as far back as 2020–2022) and
   cross-checked against `src/` at the time this harness was built. Class/interface names shift
   slightly across minor versions (e.g. permissions changed shape at 4.80) — when a snippet's
   correctness is load-bearing for the task at hand, grep the actual file it's `[verified: ...]`
   against, or the equivalent, before relying on it.
10. **Don't add defensive code for scenarios nopCommerce's own architecture already prevents.** E.g.
    don't null-check a DI-injected dependency in a constructor (the container guarantees it), don't
    wrap `IRepository<T>` calls in try/catch "just in case" — match the surrounding codebase's error
    handling posture, which is generally to let framework/global exception handling deal with
    unexpected failures.

## When a task doesn't fit any documented pattern

Say so explicitly rather than inventing a plausible-looking abstraction. Propose the closest
documented mechanism and note the gap, so a human can decide whether it's a genuine new pattern or a
sign the task should be reframed.
