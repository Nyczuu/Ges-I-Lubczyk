# AGENTS.md

Map for AI coding agents working in the Ges-I-Lubczyk repository.

This file is intentionally short. It routes; it does not restate rules. Behavioral rules live in
[`Docs/ai-harness/00-system-instructions.md`](Docs/ai-harness/00-system-instructions.md), factual
reference in [`Docs/knowledge-base/`](Docs/knowledge-base/00-index.md). Read the linked doc for the
task at hand — do not copy their content into this file.

## What this repo is

A **hard fork of nopCommerce 5.00** (.NET 10, SDK `10.0.100` pinned in `global.json`) for a premium
jarred/canned gastronomy B2C store, with planned expansion into confectionery. Hard fork means we do
not track upstream nopCommerce — it does **not** mean core is a free-for-all: see rule 3 of
`00-system-instructions.md`.

| Area | Path |
|---|---|
| Host / storefront / `Areas/Admin` | `src/Presentation/Nop.Web` |
| Shared MVC framework | `src/Presentation/Nop.Web.Framework` |
| Libraries | `src/Libraries/{Nop.Core,Nop.Data,Nop.Services}` |
| Extensions (default home for new functionality) | `src/Plugins/Nop.Plugin.{Group}.{Name}` |
| Tests | `src/Tests/Nop.Tests/{Nop.Core,Nop.Data,Nop.Services,Nop.Web}.Tests` |
| Solution | `src/NopCommerce.sln` |

**Database:** PostgreSQL (native `DataProvider`, not a shim). **Deployment:** Docker to AWS ECS.

**Test stack:** NUnit 4.5.1 + Moq 4.20.72 + **AwesomeAssertions 9.4.0** (`using AwesomeAssertions;`),
base classes `BaseNopTest` / `ServiceTest`. Do not introduce xUnit, MSTest, TUnit, NSubstitute, or
**`FluentAssertions`** — the latter is a paid licence from v8, which is exactly why this repo uses the
`AwesomeAssertions` fork. Details: [`Docs/knowledge-base/13-testing.md`](Docs/knowledge-base/13-testing.md).

## Non-negotiable rules

They are **not** duplicated here. Read
[`Docs/ai-harness/00-system-instructions.md`](Docs/ai-harness/00-system-instructions.md) before any
change — 10 rules covering: no EF Core patterns, no ad-hoc DI registration, plugin-first with core
changes requiring human confirmation, narrowest extension point, PostgreSQL, the Docker publish step,
naming conventions, explicit extensibility choice, verifying doc snippets against source, and no
defensive code the architecture already makes unnecessary.

Red flags that mean *stop and ask a human* are listed at the end of
[`Docs/ai-harness/02-extensibility-and-plugins.md`](Docs/ai-harness/02-extensibility-and-plugins.md).

## Where to look first

| Need | Start here |
|---|---|
| Behavioral rules (always first) | [`ai-harness/00-system-instructions.md`](Docs/ai-harness/00-system-instructions.md) |
| Layers, DI, data, settings, localization, caching, validation, events — cheat sheet | [`ai-harness/01-architecture-and-standards.md`](Docs/ai-harness/01-architecture-and-standards.md) |
| Where does new functionality go? | [`ai-harness/02-extensibility-and-plugins.md`](Docs/ai-harness/02-extensibility-and-plugins.md) |
| Data access / migrations | [`knowledge-base/03-data-access-linq2db-fluentmigrator.md`](Docs/knowledge-base/03-data-access-linq2db-fluentmigrator.md) + [`ai-harness/03-database-postgres.md`](Docs/ai-harness/03-database-postgres.md) |
| Adding data to a core entity | [`knowledge-base/04-extending-core-entities.md`](Docs/knowledge-base/04-extending-core-entities.md) |
| Plugin lifecycle / plugin types | [`knowledge-base/05-plugin-system.md`](Docs/knowledge-base/05-plugin-system.md), [`06-plugin-types-reference.md`](Docs/knowledge-base/06-plugin-types-reference.md) |
| Events, scheduled tasks | [`knowledge-base/07-events-and-scheduled-tasks.md`](Docs/knowledge-base/07-events-and-scheduled-tasks.md) |
| Settings, permissions, validation | [`knowledge-base/08-settings-permissions-validation.md`](Docs/knowledge-base/08-settings-permissions-validation.md) |
| Themes, widget zones | [`knowledge-base/09-theming-and-design.md`](Docs/knowledge-base/09-theming-and-design.md) |
| appsettings, Redis for multi-instance | [`knowledge-base/10-configuration-appsettings.md`](Docs/knowledge-base/10-configuration-appsettings.md) |
| Docker / ECS | [`ai-harness/04-deployment-aws-ecs.md`](Docs/ai-harness/04-deployment-aws-ecs.md), [`knowledge-base/11-deployment-docker-iis-azure.md`](Docs/knowledge-base/11-deployment-docker-iis-azure.md) |
| Code style | [`knowledge-base/12-coding-standards.md`](Docs/knowledge-base/12-coding-standards.md) + `.editorconfig` |
| Tests | [`knowledge-base/13-testing.md`](Docs/knowledge-base/13-testing.md) |
| Gastronomy domain mapped onto nopCommerce entities | [`ai-harness/05-domain-gastronomy-guidelines.md`](Docs/ai-harness/05-domain-gastronomy-guidelines.md) |
| Cross-cutting refinement checklist | [`Standards/technical-considerations-checklist.md`](Docs/Standards/technical-considerations-checklist.md) |
| Specs and process | [`Docs/index.md`](Docs/index.md), `Docs/Specs/` |
| Claude Code specific harness loop | [`CLAUDE.md`](CLAUDE.md) |

## Task reading paths

- **New plugin** → ai-harness 02 (8-step procedure) + knowledge-base 05, 06 + skill `plugin-standards-check`
- **New/changed service** → ai-harness 01 + knowledge-base 03 + skill `data-access-standards-check`
- **Schema change** → knowledge-base 03 + skill `migration-standards-check`
- **New field on a core entity** → knowledge-base 04 + ai-harness 05 + skill `entity-extension-check`
- **Admin page / controller** → knowledge-base 08 + ai-harness 02 (steps 6-8) + skill `admin-ui-standards-check`
- **Event consumer / scheduled task** → knowledge-base 07 + skill `event-consumer-standards-check`
- **Theme / widget** → knowledge-base 09 + skill `theming-standards-check`
- **Docker / ECS / config** → ai-harness 04 + knowledge-base 10, 11

## Code navigation

Prefer **LSP / language-server tools** (`go_to_definition`, `find_references`, `find_implementations`)
over Grep when locating a symbol's definition or its real usages. Semantic lookup follows interfaces
and base types a literal-name search misses. Use Grep for comments, config values, string literals and
docs, or when no LSP-backed tool is available — check what you actually have rather than assuming.

When text-searching, exclude the noise that matches nearly every domain term:
`src/Presentation/Nop.Web/App_Data/Localization/**` (`defaultResources.nopres.xml` contains every UI
string in the product), `src/Presentation/Nop.Web/wwwroot/lib/**`, `**/*.min.js`, `**/*.min.css`, and
`src/Presentation/Nop.Web/Plugins/**` (plugin build output, a duplicate of compiled plugin assemblies).

## Process constraints

1. **One PR, one concern — split, and stack when it is all one task.** Order that works: harness/docs
   changes (`.claude/*`, `Docs/*`) first, since they are orthogonal and mergeable alone; then one PR per
   plugin/layer in dependency order; **behaviour docs (`Docs/BusinessLogic/*`, `Docs/Glossary/*`)
   travel with the code they describe, never ahead of it.** Every PR in the stack must be green alone.
2. **Verify a subagent's work, not its summary.** A subagent's final report is a claim, not evidence.
   Run the build and tests yourself; read the diff it actually wrote. Pay particular attention to
   claims about the *environment* or an *external package* — neither the compiler nor the test suite
   checks those. This repo already shipped one: the knowledge base named `FluentAssertions` as the
   assertion library while the actual package reference was `AwesomeAssertions`, and every build and
   test stayed green throughout.
3. **Doc updates in the same change.** If a change touches user-facing behavior, a plugin's public
   contract, the schema, or anything already described under `Docs/`, update the affected doc in the
   same commit — not as follow-up work.
4. **`Docs/knowledge-base/` is reference, not gospel.** Rule 9 of `00-system-instructions.md` (verify a
   snippet against `src/` when its correctness is load-bearing) applies to harness agents too.

## How to verify

```bash
dotnet build src/NopCommerce.sln --configuration Release
```

```bash
dotnet test src --configuration Release
```

Local environment mirroring the ECS target: `postgresql-docker-compose.yml` at repo root (prefer it
over the SQL-Server-oriented `docker-compose.yml`).

When this map grows past ~120 lines or starts restating `Docs/`, move detail into `Docs/` and keep this
file as pointers.
