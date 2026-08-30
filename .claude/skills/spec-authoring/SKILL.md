---
name: spec-authoring
description: >-
  Load this when the user hands you a raw ticket, idea, or bug report and wants an implementation-ready
  spec before any design or code. Handles Tasks, Epics, Spikes, and Initiatives. Drives the full
  check-then-ask loop with the spec-intake agent end to end — batching every gap question back to the
  user at once and re-invoking intake until it reports Ready — instead of relaying gaps one round at a
  time. Not for designing the technical solution, and not for writing code.
---

# Spec Authoring

Produces a finalized spec at `Docs/Specs/<ID>-<slug>/spec.md`. **The file is the ticket** — there is no
external tracker, so nothing about the request survives that is not written here.

## Procedure

1. **Establish the kind and the ID.** Task, Epic, Spike, or Initiative — ask if it is not obvious from
   the request; the four have different checklists and `spec-intake` needs to be told which. The `<ID>`
   is the developer's to assign (`GIL-<n>`); ask rather than inventing one. Slug is kebab-case.

   An Epic's children live in nested directories under it — see
   [`Docs/Specs/README.md`](../../../Docs/Specs/README.md).

2. **Write the first draft** from the matching `Docs/Specs/TEMPLATE-<kind>.md` plus everything the user
   said. Frontmatter `status: Draft`. Do not invent answers to fill a section — an unanswered section
   becomes a question in step 4, not a plausible guess that ships as if it were a decision.

   Write `N/A — <reason>` where a section genuinely does not apply.

3. **Invoke `spec-intake`**, handing it the spec file path and the ticket kind. It reports either
   "implementation-ready" or exactly what is missing.

4. **Batch every gap back to the user in one message.** Not one at a time — a five-round relay of single
   questions is the failure mode this skill exists to prevent. Group related gaps, state why each
   matters, and where a reasonable default exists, propose it so the user can confirm rather than
   compose an answer.

5. **Fold the answers into the spec and re-invoke `spec-intake`.** Repeat 4–5 until intake reports Ready.
   Do not stop early because the spec looks fine to you — a clean read is not the same as intake's
   checklist passing.

6. **Set `status: Ready`** and commit the spec file. Tell the user the spec is ready and that
   `plan-and-implement` is the next step.

## What makes a spec Ready here

`spec-intake` owns the full checklists. The recurring gaps in this codebase, which are worth pre-empting
while drafting:

- **Placement.** Plugin or core? Which plugin? If core, the justification, because rule 3 of
  `00-system-instructions.md` requires human confirmation and the spec is where that starts.
- **Extension mechanism.** For new data on a core entity: existing mechanism (`ProductTag`,
  `SpecificationAttribute`, `ProductAttribute`), `GenericAttribute`, or schema migration — with the
  reason. See `entity-extension-check`.
- **Existing installations.** What happens to a store that already runs the previous version: new
  settings, new locale keys, new permissions, migrations on populated tables.
- **Localization.** Which user-facing strings appear, and that they are resources.
- **Permissions.** Who may do this, and which permission record expresses it.
- **The regression case,** for a bug fix — the behaviour a test must fail on before the fix.
- **Cross-cutting aspects** from
  [`Docs/Standards/technical-considerations-checklist.md`](../../../Docs/Standards/technical-considerations-checklist.md):
  concurrency, caching, multi-store, ECS multi-instance.

## Guardrails

- **Never answer a gap on the user's behalf.** If you can verify it from the code, verify it and say so
  with the file path. If it is a product or business decision, it is theirs.
- Never design the technical solution here — that is `ddd-modeler`, invoked later by
  `plan-and-implement`. A spec states what and why; the design states how.
- Never mark `Ready` without `spec-intake` having said so on its last run.
- Never delete a template section to make the spec look complete — `N/A` with a reason is the answer.
