---
id: GIL-000
kind: Initiative
title: <one line>
status: Draft
---

# Initiative — <title>

A business goal spanning several Epics. The least technical of the four kinds — it states *what the
business wants and why*, and leaves *how* to its Epics. Mirrors the Initiative checklist in
`.claude/agents/spec-intake.md`.

## 1. Business goal & outcome

The commercial or operational outcome, and the measure that tells us it happened.

## 2. Scope & boundaries

Which parts of the store this touches (catalog, checkout, shipping, admin, storefront presentation) and
what is explicitly excluded.

## 3. Epic breakdown

One line per Epic with its ID and directory.

## 4. Cross-cutting constraints

Decisions that must hold across every Epic — domain vocabulary (register it in
[`../Glossary/`](../Glossary/README.md)), which plugin family owns what, anything that would be
expensive to reconcile later.

## 5. Sequencing

Which Epics gate which, and what can run in parallel.

## 6. Business case

Why this is worth doing now: expected effect, cost, and what happens if we do nothing.

## 7. Target timeline & rollout

Milestones and how the capability reaches customers.
