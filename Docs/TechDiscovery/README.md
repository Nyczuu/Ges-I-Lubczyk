# Tech Discovery

Written analysis produced by a Spike, before any production code. One directory per discovery:
`Docs/TechDiscovery/<slug>/`.

Produced with the `tech-discovery` skill and checked by the `tech-discovery-intake` agent, which runs a
check-then-ask loop until the pack is discovery-ready.

## Always write

### `index.md`

Entry point. States the goal, the reading order for the rest of the pack, and — the part that matters —
**what must be true before production code is written**: the decisions this pack settled, and the
assumptions it verified against real code in `src/` versus took on trust.

### `open-questions.md`

Everything still undecided, split into **blocking** (production code cannot start until this is answered)
and **not blocking**. Also a *Closed in this discovery* section — decisions made here, so a later reader
does not silently reopen them.

An open question is written as the decision to be made, phrased so an answer resolves it. "Investigate
caching" is not an open question; "does the expiring-soon report need its own cache key, or is the
existing product cache invalidation sufficient?" is.

### `implementation-plan.md`

Phases, work packages (planning IDs, not spec IDs), definition of done, and what is deliberately left
unchanged.

## Write when the discovery calls for it

Omit any of these that does not apply, with a one-line reason on `index.md` — an omission with a stated
reason is information; a silently missing file is not.

- `current-state.md` — how the relevant part of the system works today.
- `gaps-and-approach.md` — what is missing and the proposed approach.
- `scope.md` — boundaries, when the Spike's own scope needed refining.
- `flows-and-data-exchange.md` — sequence of calls/events across components.
- `technical-specification.md` — concrete design detail where the decision is already firm.

## Rules

- **Verify against `src/`, do not paraphrase documentation.** Rule 9 of
  [`../ai-harness/00-system-instructions.md`](../ai-harness/00-system-instructions.md) applies with full
  force here: this pack becomes the basis for implementation, so an unverified claim in it propagates
  into code. Mark each load-bearing claim as verified (with the path) or as assumed.
- **No production code.** A Spike produces the pack; the Tasks that come out of it produce code.
- This pack is not documentation of what we built — that is
  [`../BusinessLogic/`](../BusinessLogic/README.md), written later, with the code.
