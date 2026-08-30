# Docs/superpowers/specs — harness design docs

Point-in-time design and decision records for this repo's AI harness (`.claude/agents/`,
`.claude/skills/`, `.claude/hooks/`, `Docs/ai-harness/`, `Docs/Specs/TEMPLATE-*.md`) — why an agent or
skill exists, what it replaced, what was rejected.

New file: `YYYY-MM-DD-<topic>-design.md`, following [`TEMPLATE.md`](TEMPLATE.md). Every file states a
**Date** and a **Status** from the template's fixed vocabulary (`PROPOSED` /
`APPROVED, NOT YET IMPLEMENTED` / `SHIPPED AS DESIGNED` / `SUPERSEDED` / `LIVING`), so a reader can tell
at a glance whether a doc describes something real, still-pending, or dead — without cross-checking the
actual `.claude/` files first.

A doc here is written once and then either left alone, marked `SHIPPED AS DESIGNED` with a pointer to
the real files, or marked `SUPERSEDED` with a pointer to whatever replaced it. **Never silently
rewritten to look current** — that destroys the record of what was actually decided when.

`LIVING` is the one exception: a continuously-updated current-state map rather than a point-in-time
record. Use it sparingly; at most one or two files should ever carry it.

## What triggers a doc here

- The harness self-improvement loop in [`../../../CLAUDE.md`](../../../CLAUDE.md) produced a change worth
  recording — a gap found, or duplication removed.
- A new agent or skill was designed, or an existing one's interface changed in a way something else
  depends on.
- A harness decision was made that a future reader would otherwise have to reverse-engineer.

Small wording fixes and extensions of an already-approved mechanism do not need a doc — see *Iterating on
a shipped harness change* in `CLAUDE.md`.
