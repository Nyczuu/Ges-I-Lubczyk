---
name: plan-and-implement
description: >-
  Load this once a spec in Docs/Specs has reached Status Ready, to take it from an agreed domain
  design through a developer-approved implementation plan to shipped, reviewed code. Single entry
  point for both a standalone Task and an Epic — an Epic is handled as N>=1 units through the
  identical procedure via task-decomposer, not a separate pipeline. Two hard developer-approval
  stops before any code is written, with no bypass for small changes. Does not create PRs.
---

# Plan and Implement

Orchestrates a Task (or every sibling Task under an Epic) from a Ready spec through an approved domain
design, an approved file-level implementation plan, shipped code, and a post-implementation gate.

An Epic is not a different pipeline — it is `task-decomposer` reducing to N>=1 units that each go
through the identical procedure below.

## Precondition

Every spec this run covers has `status: Ready` in its frontmatter (set by `spec-authoring` once
`spec-intake` reported complete). If any does not, stop and say so rather than starting against an
unrefined spec.

## Procedure

1. **Determine scope.** A standalone Task → one unit, skip decomposition. An Epic → confirm every child
   Task under `Docs/Specs/<EPIC>/` is in scope for this run; if any is not, say so explicitly rather
   than proceeding on a partial set.

2. **Invoke `ddd-modeler`, once per Task.** Hand it the **path** to that Task's `spec.md` — the spec file
   is the complete source of truth in this repo, so the agent reads it directly with its own `Read`
   tool. Batch the calls in one message when there are siblings.

   `ddd-modeler` verifies every technical claim in the spec against real code rather than trusting its
   prose, and returns the nopCommerce-shaped design: entity, entity builder, migration, service,
   events, cache keys, permissions, locale resources, and — explicitly — the entity-extension decision
   (schema migration vs `GenericAttribute` vs an existing mechanism).

3. **Gate 1 — developer approval of the domain design(s).** Show every design verbatim, batched into one
   message. **Hard stop:** nothing past this point runs until the developer explicitly approves. If they
   request a change, re-invoke `ddd-modeler` with the correction folded in, re-show, repeat — do not
   guess at the fix yourself.

   Persist: append a `## Technical design (ddd-modeler)` section to the Task's `spec.md` with the
   approved design verbatim, plus:
   ```
   **Approved by:** <name/role, never blank>
   **Date:** <date>
   **Revision notes:** <if any, else "none">
   ```
   Set `status: In Progress` in the frontmatter. **Commit this** before continuing — step 7 creates
   worktrees from a commit, so an uncommitted append is invisible inside them and the design path handed
   to `unit-implementer` would resolve to stale content.

4. **[Epic only] Invoke `task-decomposer`** over the approved designs, handing it **file paths, not
   pasted content**. Show the resulting phase/unit plan verbatim and get explicit confirmation before
   spending further tokens. If the developer overrides a grouping, record the override and its reason in
   the final report rather than applying it silently.

   Skip entirely for a standalone Task — there is one unit and nothing to decompose.

5. **Invoke `implementation-planner`, once per unit.** Hand it file paths only. It produces the
   file-by-file plan — exact files to create or change, exact signatures — by mirroring an existing
   analogous file in this repo, without making further domain decisions.

6. **Gate 2 — developer approval of the implementation plan(s).** Show every plan verbatim, batched.
   Hard stop, same revise loop as Gate 1.

   Persist: append `## Implementation plan (implementation-planner)` plus an approval record in the same
   shape, and **commit** before step 7 — same reason as Gate 1.

7. **Dispatch `unit-implementer`, one call per unit,** only after that unit's Gate 2 approval. Each call
   gets that unit's brief only: spec path, where the approved design and plan live in it, the
   standards-check skills that apply, and the acceptance criteria. Never include another unit's brief or
   this session's transcript.

   **Each unit runs in its own isolated workspace, on its own branch off the shared integration branch**
   — use `superpowers:using-git-worktrees` (native tool first, `git worktree` fallback). Hard
   requirement even for a single-unit run: two units writing the same working directory in parallel
   corrupt each other's uncommitted files, and a uniform mechanism avoids a special case to remember.

   Parallel within a phase, sequential across phases, per the `task-decomposer` graph.

   `unit-implementer` runs its own TDD loop internally. Do **not** insert a pre-implementation
   test-planning step — that breaks the red-green-refactor sequence for no benefit.

8. **After each unit returns, run the post-implementation gate on that unit's diff**, dispatched in
   parallel. Determine triggers by grepping the actual diff yourself:

   | Check | Trigger |
   |---|---|
   | `reviewer` (agent) | always |
   | `test-engineer` (agent) | always — coverage recheck against the real diff |
   | `integration-auditor` (agent) | always (fast N/A when nothing crosses a plugin/core boundary) |
   | `upgrade-safety-detector` (skill) | diff touches `plugin.json`, settings, locale keys, permissions, schema, or a public service signature |
   | `migration-standards-check` (skill) | diff adds or changes a migration |
   | `plugin-standards-check` (skill) | diff touches a plugin class, `plugin.json`, or plugin `.csproj` |
   | `admin-ui-standards-check` (skill) | diff adds or changes a controller, view model, validator, or route |
   | `localization-standards-check` (skill) | diff touches a view, view model, validator, or install/uninstall resources |
   | `security-permissions-check` (skill) | diff touches authorization, customer/store-scoped queries, external input, or logging |
   | `deployment-standards-check` (skill) | diff touches `Dockerfile`, `appsettings*`, or anything writing to disk |
   | `theming-standards-check` (skill) | diff touches a theme, view, or widget zone |

   Agents are dispatched by name; skills are dispatched as a general-purpose `Agent` call that loads and
   follows that skill by name. A check that errors out or does not apply is reported under **"Not
   checked"** — never silently dropped, never counted as a pass.

   If `reviewer` reports Blocking findings, send them back to a fresh `unit-implementer` call for the
   same unit and worktree, then re-review — up to 2 retries before stopping and flagging the unit for
   human attention rather than looping forever.

   **Once a unit passes, integrate it before its worktree is torn down.** `unit-implementer` never
   commits, so the changes exist only inside that worktree: commit them there (or via
   `git -C <worktree-path>`), merge into the shared integration branch from the main checkout, then
   remove the worktree. Skipping either half means a later phase and `epic-integration-auditor` build
   against code that silently does not include this unit.

9. **Relay each phase's results compactly** before starting the next (Epic runs): per unit — status,
   files changed, anything blocked. Do not dispatch a phase until every unit it depends on has actually
   been merged. A blocked unit halts any dependent phase; stop and tell the developer rather than
   routing around it.

10. **[Epic only] Invoke `epic-integration-auditor` once** against the integration branch's own checkout:
    combined build and test run, re-verification that the contracts `task-decomposer` froze between units
    still hold in the shipped code, deployment sequencing. Different question from per-unit
    `integration-auditor` — both run, neither replaces the other.

11. **Report a summary:** what is implemented and reviewed, what the gate found, what is still open, and
    an explicit reminder that **this skill does not create PRs** — that stays a separate, deliberate step.

## Guardrails

- Never skip Gate 1 or Gate 2, for any size of change. No bypass exists.
- Never dispatch an unconfirmed `task-decomposer` phase plan.
- Never skip the worktree-per-unit requirement, even for a single-unit run.
- Never leave an approved design or plan uncommitted before a worktree is created, and never leave a
  passed unit merely reviewed but unmerged before its worktree is removed. Both leave later steps reading
  stale content with nothing to signal it.
- Never auto-resolve a blocked unit by widening scope or guessing the missing piece — that is a re-run
  with the developer present.
- **Never restate a subagent's "all green" as your own result.** Run the build and tests yourself before
  reporting them; read the diff the subagent actually wrote. Its report is a claim, not evidence.
- If implementing a unit turns out to require a change to `Nop.Core`/`Nop.Data`/`Nop.Services`/
  `Nop.Web(.Framework)` beyond an additive nullable property, stop and get explicit human confirmation —
  rule 3 of `Docs/ai-harness/00-system-instructions.md` is not the implementing agent's call to make.
