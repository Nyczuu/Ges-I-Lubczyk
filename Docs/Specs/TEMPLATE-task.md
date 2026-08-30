---
id: GIL-000
kind: Task
title: <one line>
status: Draft
parent: <parent Epic ID, or omit for standalone>
---

# Task — <title>

A single implementable unit of work (feature or bug fix). For an Epic, Spike, or Initiative use the
matching template instead. Mirrors the Task checklist in `.claude/agents/spec-intake.md` — see there for
the reasoning behind each section.

Write `N/A — <reason>` for a section that genuinely does not apply, rather than deleting it. Skim the
referenced doc before writing a section from memory; a stale assumption costs a wasted round-trip later.

## 1. Business goal & outcome

Bug fix: the observed wrong behavior, and what "fixed" looks like. Feature: the problem or opportunity
and the success criterion.

## 2. Root cause / current behavior *(bug fixes only)*

A verified fact about the code (`file:line`), not an assumption — say so explicitly if unconfirmed.

## 3. Placement — plugin or core?

See [`ai-harness/02-extensibility-and-plugins.md`](../ai-harness/02-extensibility-and-plugins.md)
(decision tree). Which plugin owns this, or which existing one is extended. If the change requires
touching `Nop.Core`/`Nop.Data`/`Nop.Services`/`Nop.Web(.Framework)`, say so explicitly and justify it —
rule 3 of `00-system-instructions.md` requires human confirmation, and the spec is where that
conversation starts, not the PR.

## 4. Extension point

See [`knowledge-base/06-plugin-types-reference.md`](../knowledge-base/06-plugin-types-reference.md). The
narrowest applicable interface (`IPaymentMethod`, `IShippingRateComputationMethod`, `IWidgetPlugin`,
`ITaxProvider`, …). `IMiscPlugin` needs a reason.

## 5. Data model & migration

See [`knowledge-base/03`](../knowledge-base/03-data-access-linq2db-fluentmigrator.md) and
[`04`](../knowledge-base/04-extending-core-entities.md). New/changed entities and columns, nullability,
defaults. **For a new field on a core entity, state the extensibility choice explicitly:** schema
migration (queryable/sortable/joinable) vs `GenericAttribute` (schema-free) — with the reason. Also:
does an existing mechanism (`ProductTag`, `SpecificationAttribute`, `ProductAttribute`) already cover
this? Is the migration forward-only, and what happens to data on existing installations?

## 6. Admin & storefront surface

See [`knowledge-base/08`](../knowledge-base/08-settings-permissions-validation.md) and
[`09`](../knowledge-base/09-theming-and-design.md). New admin pages/menu entries, storefront changes,
widget zones touched, view models and their validators.

## 7. Settings, permissions, localization

New `ISettings` properties. New permission records (`IPermissionConfigManager`) and who gets them. New
locale resource keys — and confirmation that `UninstallAsync` removes everything `InstallAsync` added.

## 8. Events & scheduled tasks

See [`knowledge-base/07`](../knowledge-base/07-events-and-scheduled-tasks.md). Events published or
consumed; whether a built-in `EntityInserted/Updated/DeletedEvent<T>` already covers it. For an
`IScheduleTask`: is it idempotent and safe to run concurrently (ECS may run more than one instance)?

## 9. Caching

Cache keys added or invalidated. Anything this change makes stale. Multi-instance implication — see
[`knowledge-base/10`](../knowledge-base/10-configuration-appsettings.md).

## 10. Failure scenarios

What happens when an external dependency is down/slow/erroring, when input is invalid, when the
operation is retried. Match the codebase's error-handling posture — do not add defensive code the
architecture already makes unnecessary (rule 10).

## 11. Test scenarios

See [`knowledge-base/13-testing.md`](../knowledge-base/13-testing.md). Scenarios to cover, including the
**regression case for a bug fix** — a test that demonstrably fails against the old code path.

## 12. Documentation impact

Which `Docs/BusinessLogic/*` or `Docs/Glossary/*` file this change adds or updates, in the same commit.

## 13. Deployment & rollout

See [`ai-harness/04-deployment-aws-ecs.md`](../ai-harness/04-deployment-aws-ecs.md). Anything affecting
the Docker image, `appsettings`/env vars, or ECS task configuration. Immediate or staged rollout.
