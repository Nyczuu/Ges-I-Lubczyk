# Docs

Entry point for this repository's documentation. Three kinds of content, deliberately separated:

| Folder | Holds | Changes when |
|---|---|---|
| [`ai-harness/`](ai-harness/00-system-instructions.md) | **Behavioral rules** for AI assistants — what to do, what never to do, which extension point to reach for | our conventions change |
| [`knowledge-base/`](knowledge-base/00-index.md) | **Factual reference** on how nopCommerce 5.00 works, verified against `src/` | the framework or our fork changes |
| everything below | **Our project** — specs, domain behaviour, glossary, process | we ship work |

Do not restate content across the three. A spec that re-explains how FluentMigrator works, or a skill
that copies a knowledge-base page, is a maintenance bug — reference instead.

## Process

Work flows through files, not a ticket tracker:

```
Docs/Specs/<ID>-<slug>/spec.md          spec-authoring + spec-intake  (until Status: Ready)
        ↓
   domain design                        ddd-modeler        [developer approval gate 1]
        ↓
   implementation plan                  implementation-planner  [developer approval gate 2]
        ↓
   code + tests                         unit-implementer, test-engineer
        ↓
   review                               reviewer, integration-auditor
```

`plan-and-implement` drives everything from the design stage onward. Both approval gates are hard —
there is no bypass for small changes.

| Kind of ticket | Template |
|---|---|
| Task — one implementable unit | [`Specs/TEMPLATE-task.md`](Specs/TEMPLATE-task.md) |
| Epic — several related Tasks | [`Specs/TEMPLATE-epic.md`](Specs/TEMPLATE-epic.md) |
| Spike — time-boxed investigation, no production code | [`Specs/TEMPLATE-spike.md`](Specs/TEMPLATE-spike.md) |
| Initiative — a business goal spanning Epics | [`Specs/TEMPLATE-initiative.md`](Specs/TEMPLATE-initiative.md) |

A Spike's written analysis goes to [`TechDiscovery/`](TechDiscovery/README.md), not into the spec.

## Project documentation

- [`BusinessLogic/`](BusinessLogic/README.md) — what a mechanism we built actually does, including
  corner cases. Written **with the code it describes**, in the same commit.
- [`Glossary/`](Glossary/README.md) — the canonical term for a domain concept. Check here before
  naming anything new.
- [`Standards/technical-considerations-checklist.md`](Standards/technical-considerations-checklist.md) —
  cross-cutting refinement checklist for aspects no other doc owns.
- [`superpowers/specs/`](superpowers/specs/README.md) — decision records about the AI harness itself.

## Where the rest lives

Repo map, task reading paths, and verification commands: [`../AGENTS.md`](../AGENTS.md).
Claude Code specific instructions: [`../CLAUDE.md`](../CLAUDE.md).
