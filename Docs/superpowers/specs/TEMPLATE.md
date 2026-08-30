<!--
Template for design/decision docs under Docs/superpowers/specs/. See README.md in this folder for when
to use it. Sections marked "optional" exist because not every doc needs them — omit an optional section
entirely rather than keeping it as an empty placeholder. Delete this comment block in the real file.
-->

# <Title> — design

**Date:** YYYY-MM-DD
**Status:** <one fixed value below> — <one-line detail>

Fixed `Status` values — pick exactly one, keep the exact keyword so it stays greppable:

- `PROPOSED` — design written, not yet approved.
- `APPROVED, NOT YET IMPLEMENTED` — approved, implementation has not happened yet. Detail: what is still
  needed before it can ship, if known.
- `SHIPPED AS DESIGNED` — implemented. Detail must point at the real files: "see `<paths>` for current
  behavior; this doc will not be kept in sync with them."
- `SUPERSEDED` — replaced by a newer doc. Detail must point at the replacement and say plainly "do not
  implement/use this as written."
- `LIVING` — an ongoing, continuously-updated current-state map rather than a point-in-time decision.
  Rare. Detail: how and when it gets updated.

## Problem

Required. Why this doc exists: the gap, the incident, or the question that triggered it. Concrete — "the
harness let X through" beats "the harness could be improved."

## Decision(s)

Required. The actual design or choice made. For a doc about an agent, skill, or pipeline, use whichever
of these are relevant — none are mandatory:

- **Pipeline / flow** — step list or diagram, for multi-stage orchestration.
- **Which stage owns what** — especially where responsibility moved between two existing stages.
- **Failure handling** — what happens when a stage fails, times out, or contradicts another stage.
- **Output format** — the exact shape of the report or artifact this produces.

## Rejected alternatives

Optional — omit if nothing meaningfully competed. What else was considered and why it lost.

## Out of scope

Optional. What this deliberately does not cover, so the next reader does not mistake silence for a
decision.
