---
name: tech-discovery
description: >-
  Load this when a Spike needs its written analysis turned into a Tech Discovery pack before any
  production code — scaffolds Docs/TechDiscovery/<slug>/, verifies every load-bearing assumption against
  this repo rather than against documentation, writes explicit open questions for everything still
  undecided, and drives the check-then-ask loop with tech-discovery-intake until it reports Ready. Never
  use it to write production code or to judge spec prose completeness.
---

# Tech Discovery

Turns a Spike plus the developer's own findings into a pack at `Docs/TechDiscovery/<slug>/` that becomes
the basis for the Tasks that follow. Structure and file catalog:
[`Docs/TechDiscovery/README.md`](../../../Docs/TechDiscovery/README.md).

## Procedure

1. **Read the Spike spec** (`Docs/Specs/<ID>-<slug>/spec.md`) — specifically its question to resolve,
   time-box, and out-of-scope section. The pack answers that question; it does not quietly widen it.

2. **Scaffold the pack.** Always: `index.md`, `open-questions.md`, `implementation-plan.md`. Add the
   optional files the discovery actually calls for; for each one omitted, put a one-line reason on
   `index.md`. A stated omission is information; a silently missing file is not.

3. **Verify every load-bearing claim against `src/`.** This is the step that distinguishes a discovery
   pack from a summary of what the model already believes about nopCommerce. For each claim the plan
   depends on:
   - locate the actual file and read it;
   - mark the claim **verified** with the path, or **assumed** with what would confirm it;
   - state explicitly when the knowledge base and the source disagree — the source wins, and the
     knowledge-base file needs a correction as a separate change.

   Rule 9 of [`Docs/ai-harness/00-system-instructions.md`](../../../Docs/ai-harness/00-system-instructions.md)
   applies with full force here: an unverified claim in this pack propagates straight into implementation.

4. **Write open questions as decisions, not topics.** "Investigate caching" is not an open question;
   "does the expiring-soon report need its own cache key, or does existing product-cache invalidation
   already cover it?" is. Split **blocking** from **not blocking**, and record what was **closed in this
   discovery** so a later reader does not silently reopen it.

5. **Invoke `tech-discovery-intake`.** It reports whether the pack is discovery-ready and, if not,
   exactly what is missing, internally inconsistent, or contradicted by this repo's code.

6. **Batch every gap back to the user in one message**, fold in the answers, re-invoke. Repeat until
   intake reports Ready.

7. **Commit the pack** and set the Spike spec's `status: Shipped`. The Tasks that come out of the
   discovery are written afterwards as their own specs — not appended to the Spike.

## What a nopCommerce discovery usually has to settle

- Plugin or core, and which extension point (rule 4: the narrowest one that fits).
- Entity extension mechanism, with the reason — see `entity-extension-check`.
- Schema shape and whether the migration is safe on populated, already-installed stores.
- Effect on existing installations: settings, locale keys, permissions, `SystemName`.
- Multi-store and multi-language behaviour.
- ECS multi-instance implications: cache coherence, scheduled-task idempotency, anything written to disk.
- What existing nopCommerce mechanism was considered and rejected, and why.

## Guardrails

- **No production code.** The pack produces the plan; the Tasks produce the code.
- Never present an assumption as a finding. Every claim is labelled verified or assumed.
- Never let the pack answer a question the Spike did not ask without saying that the scope grew.
- This is not documentation of what we built — that is `Docs/BusinessLogic/`, written later, with the code.
