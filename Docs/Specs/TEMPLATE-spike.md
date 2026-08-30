---
id: GIL-000
kind: Spike
title: <one line>
status: Draft
parent: <parent Epic ID, or omit>
---

# Spike — <title>

A time-boxed investigation that produces **written analysis, never production code**. Mirrors the Spike
checklist in `.claude/agents/spec-intake.md`.

## 1. Question to resolve

The specific decision this Spike exists to unblock, phrased so the answer is falsifiable. "Investigate
batch tracking" is not a question; "should batch/expiry live on `Product` as columns or as a
plugin-owned `ProductBatch` entity, given the expiring-soon admin report needs to filter and sort on
it?" is.

## 2. Time-box

How long, and what happens when it expires — the fallback decision if the investigation is inconclusive.

## 3. Investigation approach

What will actually be examined: which parts of `src/`, which nopCommerce mechanisms, which external
sources. Note explicitly if a claim will be verified against the checked-out source rather than taken
from documentation (rule 9 of `00-system-instructions.md`).

## 4. Deliverables

A Tech Discovery pack under [`../TechDiscovery/<slug>/`](../TechDiscovery/README.md), produced with the
`tech-discovery` skill. It must state what was decided, what remains open, and what was verified against
real code versus assumed. The Spike is done when the pack passes `tech-discovery-intake`.

Child Tasks that come out of this Spike are created afterwards, as separate specs — not appended here.

## 5. Out of scope

What this Spike deliberately does not answer, so the next reader does not mistake silence for a finding.
