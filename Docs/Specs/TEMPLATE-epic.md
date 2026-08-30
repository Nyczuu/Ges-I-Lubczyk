---
id: GIL-000
kind: Epic
title: <one line>
status: Draft
---

# Epic — <title>

A group of related Tasks delivering one coherent capability. Each child Task gets its own directory
under this one, with its own `spec.md` from [`TEMPLATE-task.md`](TEMPLATE-task.md). Mirrors the Epic
checklist in `.claude/agents/spec-intake.md`.

Write `N/A — <reason>` rather than deleting a section.

## 1. Business goal & outcome

What capability this delivers and how we will know it worked.

## 2. Scope & boundaries

What is in, and explicitly what is out. Which plugin(s) this creates or extends — see
[`ai-harness/02-extensibility-and-plugins.md`](../ai-harness/02-extensibility-and-plugins.md).

## 3. Task breakdown

One line per child Task with its ID and directory. A Spike belongs here too if the Epic needs an
investigation before its Tasks can be specified.

## 4. Cross-cutting constraints

Decisions that must hold identically across every child Task, so two Tasks do not each invent their own
answer: entity ownership, schema shape, permission naming, locale key prefix, cache key prefix, plugin
`SystemName`. These are the contracts `task-decomposer` freezes between units — state them here or they
get decided inconsistently.

## 5. Sequencing & dependencies

Which Tasks must land in order and why (schema before the service that reads it, plugin skeleton before
the admin page). Which are genuinely independent and can proceed in parallel.

## 6. Data & migration strategy across the Epic

How the Epic's schema changes compose. Forward-only migrations must apply cleanly in sequence on an
existing installation, not just on a fresh install.

## 7. Deployment & rollout strategy

Whether the Epic ships incrementally or behind a single switch. Anything affecting the Docker image or
ECS task configuration — see [`ai-harness/04-deployment-aws-ecs.md`](../ai-harness/04-deployment-aws-ecs.md).
