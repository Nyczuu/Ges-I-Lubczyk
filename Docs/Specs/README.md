# Specs

The spec file **is** the ticket. There is no external tracker — `Docs/Specs/` is the source of truth.

## Layout

```
Docs/Specs/
├── TEMPLATE-task.md · TEMPLATE-epic.md · TEMPLATE-spike.md · TEMPLATE-initiative.md
├── GIL-001-batch-tracking/
│   └── spec.md                     standalone Task
└── GIL-010-gastronomy-compliance/  Epic
    ├── spec.md                     the Epic itself
    ├── GIL-011-product-batch-entity/spec.md
    └── GIL-012-expiring-soon-report/spec.md
```

- **ID:** `GIL-<n>`, assigned by the developer. Sequential, never reused.
- **Slug:** short kebab-case summary of the ticket.
- **Epic:** a directory whose own `spec.md` is the Epic; child Tasks are nested directories.

## Frontmatter — required on every spec

```yaml
---
id: GIL-001
kind: Task            # Task | Epic | Spike | Initiative
title: Track product batch and best-before date
status: Draft         # Draft | Ready | In Progress | Shipped
parent: GIL-010       # omit for a standalone ticket
---
```

`status` is what the pipeline reads:

| Status | Meaning |
|---|---|
| `Draft` | being written; `spec-intake` has not passed it |
| `Ready` | `spec-intake` reported complete — `plan-and-implement` may pick it up |
| `In Progress` | design or implementation started |
| `Shipped` | merged; spec kept as the historical record of what was asked for |

## How a spec gets written

Invoke `spec-authoring`. It drives the check-then-ask loop with the `spec-intake` agent: intake reports
what is missing, the skill batches every gap question to you at once, you answer, it rewrites the spec
and re-runs intake until intake reports Ready. Do not hand-relay gaps one at a time.

Write `N/A — <reason>` for a section that genuinely does not apply, rather than deleting it. Silence is
what `spec-intake` and `refinement-verifier` flag as a gap; an explicit `N/A` is an answer.

## After it ships

A `Shipped` spec is a historical record, not living documentation. If the mechanism it describes needs
to stay documented, that belongs in [`../BusinessLogic/`](../BusinessLogic/README.md) — written in the
same commit as the code, per the process constraint in `AGENTS.md`.
