---
name: refinement-check
description: >-
  Load this when given a spec in Docs/Specs to check before or after it clears refinement, or after its
  implementation has already merged — whether the cross-cutting technical considerations are complete,
  whether the business logic it touches is documented and the documentation still matches reality,
  whether the stated scope covers corner cases visible in the code, and (post-implementation) whether
  the spec still matches what actually shipped. Not for designing a solution.
---

# Refinement Check

Runs the check-then-ask loop with the `refinement-verifier` agent against a spec file and the real
codebase. Same shape as `spec-authoring`, different question: `spec-intake` judges whether the spec says
enough; this judges whether what it says is **complete against the code and still true**.

## Procedure

1. **Read the spec** at `Docs/Specs/<ID>-<slug>/spec.md` and note its `status`.

2. **Invoke `refinement-verifier`** with the spec path. It checks three things nothing else covers:
   - every aspect in
     [`Docs/Standards/technical-considerations-checklist.md`](../../../Docs/Standards/technical-considerations-checklist.md)
     is addressed or explicitly `N/A` with a reason — silence is a gap;
   - the business logic this touches is documented under `Docs/BusinessLogic/`, and that documentation
     still matches the code;
   - the stated scope covers corner cases that are visible in the code but absent from the spec.

3. **Batch every finding back to the user in one message**, with the file and line that prompted it.
   Findings about the code are verifiable; findings about intent are questions.

4. **Fold answers into the spec, re-invoke, repeat** until the verifier reports no gaps.

5. **Commit the updated spec.**

## Post-implementation mode

When run after the work has merged, the question becomes: **does the spec still describe what actually
shipped?** Implementations drift from their spec during review, and a spec left describing the original
plan is worse than none — it is a confident record of something that is not true.

- [ ] Behaviour described in the spec matches the merged code.
- [ ] Scope that was cut is marked as cut, not left reading as delivered.
- [ ] Anything added during implementation is reflected.
- [ ] The `Docs/BusinessLogic/` entry exists and matches — remembering that it should have shipped in the
      same commit as the code, per `AGENTS.md`.
- [ ] `status: Shipped`.

## Guardrails

- **Verify against code, not against the spec's own prose.** A spec claiming a mechanism works a certain
  way is a hypothesis; the file that implements it is the evidence. Cite `file:line`.
- Do not design the solution — that is `ddd-modeler`.
- Do not re-litigate spec completeness in the `spec-intake` sense; that loop already ran.
- Never mark a gap resolved because it now has words next to it. It is resolved when the answer is
  specific enough that an implementer would not have to guess.
