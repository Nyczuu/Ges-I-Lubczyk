# CLAUDE.md

Instructions for Claude Code when working in this repository.

## Read this first, unconditionally

**Before any change, read
[`Docs/ai-harness/00-system-instructions.md`](Docs/ai-harness/00-system-instructions.md).** Claude Code
auto-loads this file, not that one — so without this pointer the ten non-negotiable rules never reach
your context, and the failure mode is silent: plausible, well-formed ASP.NET Core code that is wrong
for this codebase.

Then see [AGENTS.md](AGENTS.md) for the repo map (where to look, task reading paths, process
constraints, verification). Everything below is specific to Claude Code itself.

## Harness self-improvement loop

This repo's AI-assisted pipeline (`.claude/agents/*`, `.claude/skills/*`, `Docs/ai-harness/*`,
`Docs/knowledge-base/*`, `Docs/Specs/TEMPLATE-*.md`) is built incrementally and validated against real
work rather than speculatively. It is expected to keep growing, so this rule is about the harness as a
whole, not about any fixed set of stages.

**Trigger:** any time the user finalizes or approves an output produced by *any* stage of this harness
— whichever agent, skill, template, or manual step was actually responsible for it — proactively ask,
before moving on to the next task, whether they want to review the current setup for gaps *or* for
duplication/bloat this task just surfaced. Don't wait to be asked, and don't skip it because the
artifact looks fine to you — the whole point is that a clean pass doesn't prove the responsible stage
asked everything worth asking, or that the harness's own files are still lean.

This has happened for real more than once in the sibling Product Catalog harness this one is adapted
from: a design review and a compliance review both cleanly approved a fix that was still needlessly
complex (POR-1873); a spec-completeness check reported "Ready" on a Spike spec that was missing five
concrete things the user's own rewrite added (POR-1903); and a set of checklists/templates that each
grew correct-but-repeated content one edit at a time went unnoticed until someone was asked to
specifically look for it (2026-07-30 consolidation pass). In all three cases nobody thought to ask
"does the setup need patching?" until asked to.

Don't fire this after every small back-and-forth answer or minor clarification — only after a real
artifact from some stage of the harness is finalized or approved.

If the user says yes, check both dimensions:

- **Gaps:** diff what the finalized artifact actually needed against what the stage responsible for
  producing/checking it currently asks for or produces, and propose concrete edits closing that
  specific gap.
- **Duplication/bloat:** check whether the content just added or touched restates something already
  defined elsewhere in the harness — most likely a `Docs/knowledge-base/` or `Docs/ai-harness/` file —
  instead of referencing it. Skills in this repo are deliberately *checklist form* over a `Full doc:`
  pointer; a skill that has grown into a second copy of its source document is the bug this check
  exists to catch. If so, propose extracting a single definition and referencing it everywhere.

Either way, keep the fix scoped to what this task actually surfaced — not a general audit of the whole
pipeline.

## Harness design docs

Point-in-time design/decision records for this harness (why an agent/skill exists, what it replaced)
live under `Docs/superpowers/specs/`, one file per decision, following
`Docs/superpowers/specs/TEMPLATE.md` — see that folder's `README.md`. Every file states a Status
(`PROPOSED`, `APPROVED, NOT YET IMPLEMENTED`, `SHIPPED AS DESIGNED`, `SUPERSEDED`, or `LIVING` for a
continuously-updated roadmap doc) so it is never ambiguous whether a doc describes something real,
still-pending, or dead.

## Iterating on a shipped harness change

The full brainstorming → design doc → implementation plan flow is required for a harness change's
*initial* design — that is what produces the design doc and plan the section above describes. It is not
required again for every later adjustment to something already shipped that way.

**Skip straight to implementation** (edit, verify with grep/read, commit) when a follow-up extends an
already-approved mechanism to more cases, fixes wording, or closes a small gap the shipped version
missed — discuss it in conversation first, then just make it.

**Go back to the full flow** only when the follow-up introduces a genuinely new mechanism, restructures
an interface another agent/skill depends on, or real disagreement about the *approach* (not just its
scope) surfaces during discussion. When unsure which case applies, say so and let the user decide.

## Harness file conventions

**Everything written into this repository is in English** — docs, specs, glossary entries, code comments,
commit messages. Conversation happens in whatever language the developer is using; the artefacts do not
follow it. In particular, do not quote the developer verbatim in a document when they wrote in another
language — translate the point and attribute it instead.

When creating or editing any `.claude/agents/*.md` or `.claude/skills/*/SKILL.md`, write the frontmatter
`description` as a **folded block scalar** (`description: >-` followed by indented lines wrapped at ~96
columns) — never a plain single-line scalar. A plain scalar silently breaks the entire frontmatter as
soon as the text contains `: ` (YAML parses it as a new mapping), which makes Claude Code drop the
skill's description or the whole agent **without any error**. If a skill or agent you just wrote does
not appear in the available list after a session restart, this is the first thing to check.

CI enforces this via `.github/scripts/lint-ai-harness.py` (`05.lint-ai-harness.yml`), which also checks
name-to-filename match, tool/model validity, body-vs-tools drift, and that every referenced
doc/agent/skill path actually exists.

## Skills are gates, not references

Load the matching `*-check` skill **before** writing the code it covers, not after. A skill is the cheap
write-time checklist; the `Docs/` file it points at is the source of truth when the two disagree.
`unit-implementer` is required to do this; the main session should do the same when implementing
directly.

## Code navigation

Shared rule: prefer LSP over Grep for symbol navigation — see
[AGENTS.md — Code navigation](AGENTS.md#code-navigation).

**Claude Code note:** this setup may not expose LSP-backed tools; check your actual tool list rather
than assuming. Use Grep when LSP is unavailable, with the exclusions listed in AGENTS.md.
