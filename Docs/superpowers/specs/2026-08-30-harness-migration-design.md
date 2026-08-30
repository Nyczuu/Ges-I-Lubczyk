# AI harness migration from Product Catalog — design

**Date:** 2026-08-30
**Status:** SHIPPED AS DESIGNED — see `AGENTS.md`, `CLAUDE.md`, `.claude/agents/`, `.claude/skills/`,
`.claude/hooks/`, `.github/scripts/lint-ai-harness.py` for current behavior; this doc will not be kept in
sync with them.

## Problem

This repo already had the **content** layer of an AI harness: `Docs/ai-harness/` (six files of behavioral
rules) and `Docs/knowledge-base/` (fourteen files of factual reference verified against `src/`), added in
commit `7d0eab2261`.

What it did not have was the **machinery** layer. Rules in a document only apply when the document is in
context, and Claude Code auto-loads `CLAUDE.md` — which did not exist — not `Docs/ai-harness/`. So the
ten non-negotiable rules were reachable only if someone happened to point at them.

A sibling repository (Northmill's Product Catalog) had the machinery: an `AGENTS.md`/`CLAUDE.md` split,
skills as write-time gates, an eleven-agent pipeline from spec intake to integration audit, a PreToolUse
hook, and a CI linter protecting the harness's own files. Its *content* was useless here — EF Core,
DynamoDB, Minimal APIs, SDK NuGet packages, Outbox, AWS Lambda, TUnit — but its *shape* was proven
against real tickets.

## Decisions

### Migrate the machinery, keep the content

The port takes structure, protocols, and disciplines from Product Catalog and drops its subject matter
entirely. `Docs/ai-harness/` and `Docs/knowledge-base/` keep their existing shape — the
behavioral/factual split is better than Product Catalog's `Standards/` + `Architecture/` layout — and
nothing was restructured to match the source repo.

Skills are the **checklist form** of a document, opening with a `Full doc:` pointer. A skill that grows
into a second copy of its knowledge-base page is the failure mode the self-improvement loop in
`CLAUDE.md` exists to catch.

### Three-file responsibility split at the entry point

Product Catalog has two entry files; this repo has three, because `00-system-instructions.md` already
existed and is good.

| File | Owns | Does not contain |
|---|---|---|
| `Docs/ai-harness/00-system-instructions.md` | the ten non-negotiable rules | repo map, verification commands, process |
| `AGENTS.md` | repo map, reading paths, verification, process constraints | the ten rules — only a pointer |
| `CLAUDE.md` | self-improvement loop, harness file conventions, and the unconditional redirect to `00-system-instructions.md` | anything about nopCommerce architecture |

### The process layer is files, not a tracker

`Docs/Specs/<ID>-<slug>/spec.md` **is** the ticket, with `status` in frontmatter driving the pipeline
(`Draft` → `Ready` → `In Progress` → `Shipped`). This removes Product Catalog's whole Jira-fetch step:
`ddd-modeler` reads the spec file directly instead of being handed field text it cannot re-read.

Both hard developer-approval gates survive unchanged — domain design, then implementation plan, with no
bypass for small changes.

### Enforcement is mechanical, not advisory

Four layers, because prose alone does not hold against a strong prior for "typical .NET":

1. Rules in `00-system-instructions.md`.
2. `block-forbidden-stack.mjs` — a PreToolUse deny on EF Core types, `Create.TableFor<T>`,
   `FluentAssertions`, xUnit/TUnit/NSubstitute, provider-specific SQL, bare `AddScoped` in `Program.cs`,
   and DataAnnotations on view models. Core paths **warn** rather than deny: rule 3 makes that a human
   decision the hook cannot adjudicate.
3. Skills loaded before writing, enforced by `unit-implementer`'s own instructions.
4. CI: `05.lint-ai-harness.yml` for harness integrity, existing `dotnet.yml` for the code.

The selection criterion for a hook rule was: **does the wrong version compile and pass tests?** Every
denied pattern does. Anything the compiler already catches was left out.

### Agent roster: same eleven, retargeted

`dynamodb-standards-check`, `sdk-design-standards-check`, `sdk-api-documentation-enforcer`, and
`backward-compatibility-detector` had no meaning here. Their useful half became `upgrade-safety-detector`,
which asks the question this repo actually has: what happens to a store that already runs the previous
version — a dimension no test in a fresh-install suite can see.

The disciplines carried over verbatim, because each was earned: `ddd-modeler` verifying every spec claim
against code and separately challenging whether a correct design is *necessary*; `reviewer` citing the
rule behind every blocking finding and reporting what it could not check; the standing instruction to
verify a subagent's work rather than its summary.

## Drift found while migrating

`Docs/knowledge-base/13-testing.md` and `00-index.md` both named **FluentAssertions** as the assertion
library. The actual package reference is **AwesomeAssertions 9.4.0** — the free fork created after
FluentAssertions v8 moved to a paid licence. The API is identical, so an agent following the doc would
have added a commercial package that compiles and passes every test.

This is precisely the failure class the "verify a subagent's work, not its summary" constraint describes:
a claim about an *external package*, which neither the compiler nor the test suite checks. It is now
cited as the local example in `AGENTS.md` rather than the borrowed Product Catalog anecdotes, and it is a
`block-forbidden-stack.mjs` deny rule.

Two further corrections, from reading real plugin `.csproj` files rather than the doc: plugins here
reference `Nop.Web.csproj` about as often as `Nop.Web.Framework.csproj`, and they need `OutputPath` plus
`OutDir`, the `ClearPluginAssemblies` post-build target, and an explicit `<Content>` entry per view. Also:
migrations run at application startup, so a rolling ECS deployment requires expand/contract discipline —
recorded in `migration-standards-check` and `deployment-standards-check`.

## First gap the harness surfaced on itself

The first real run of the harness's own verification command failed: `dotnet build src/NopCommerce.sln`
produced 4836 errors, every one `NU1301` / `401` against a CodeArtifact feed belonging to an unrelated
employer project. This repo had no `nuget.config`, so restore inherited the machine-level one.

Two things came out of it, and both are the loop working as intended. A root `nuget.config` with
`<clear />` now isolates the repo (build: 0 errors, 2:58; tests: 1098 passed, 0 failed, 10 skipped). And
`dotnet-build-doctor` gained the signature — it had been written to cover nopCommerce-specific failures
and missed the most basic environmental one, which is exactly the kind of gap that only shows up when
something is actually run rather than reviewed.

## Rejected alternatives

- **Restructuring `Docs/` to mirror Product Catalog's `Standards/`+`Architecture/` layout.** Would have
  meant refactoring twenty working files and risking the `[verified: <path>]` citations, to gain
  consistency with a repo that shares no code.
- **Porting Product Catalog's standards documents and rewriting them for nopCommerce.** The existing
  knowledge base is better grounded — it was written against this checkout, with citations.
- **Denying all writes to core.** The user chose a hard fork, but `00-system-instructions.md`'s
  plugin-first rule was kept: hard fork means we do not track upstream, not that core is unguarded.
  A hook cannot distinguish an approved additive nullable property from an unapproved refactor, so it
  warns.

## Out of scope

Validating the pipeline end to end on a real ticket. That requires the developer at two hard approval
gates, so it cannot be completed by an agent alone. The recommended first candidate is
`Nop.Plugin.Misc.GastronomyCompliance` from
[`Docs/ai-harness/05-domain-gastronomy-guidelines.md`](../../ai-harness/05-domain-gastronomy-guidelines.md):
a batch/expiry entity, a migration, a service, an admin page with a permission and a menu entry, locale
resources, and a scheduled task — which exercises eight of the write-time skills at once.
