# Technical Considerations Checklist

Cross-cutting technical aspects a Task should address before it clears refinement. Used by
`refinement-verifier` (`.claude/agents/refinement-verifier.md`) to check completeness **against real
code**, not just against the words in the spec.

This list only covers aspects **not already owned by another document**. For those, it points at the
owner instead of restating the rules:

| Aspect | Owned by |
|---|---|
| Layering, DI, where new code goes | [`ai-harness/01`](../ai-harness/01-architecture-and-standards.md), [`02`](../ai-harness/02-extensibility-and-plugins.md) |
| Entities, repositories, migrations | [`knowledge-base/03`](../knowledge-base/03-data-access-linq2db-fluentmigrator.md) |
| Adding data to a core entity | [`knowledge-base/04`](../knowledge-base/04-extending-core-entities.md) |
| Plugin lifecycle, plugin types | [`knowledge-base/05`](../knowledge-base/05-plugin-system.md), [`06`](../knowledge-base/06-plugin-types-reference.md) |
| Events, scheduled tasks | [`knowledge-base/07`](../knowledge-base/07-events-and-scheduled-tasks.md) |
| Settings, permissions, validation | [`knowledge-base/08`](../knowledge-base/08-settings-permissions-validation.md) |
| Themes, widget zones | [`knowledge-base/09`](../knowledge-base/09-theming-and-design.md) |
| PostgreSQL specifics (`citext`, provider syntax) | [`ai-harness/03`](../ai-harness/03-database-postgres.md) |
| Docker / ECS | [`ai-harness/04`](../ai-harness/04-deployment-aws-ecs.md), [`knowledge-base/11`](../knowledge-base/11-deployment-docker-iis-azure.md) |
| Tests | [`knowledge-base/13`](../knowledge-base/13-testing.md) |

## Aspects to check here

1. **Concurrency & idempotency.** Can two requests touching the same entity race? Is a retried operation
   safe to replay? For an `IScheduleTask`: ECS can run more than one instance of the app, so a task that
   assumes it is the only runner is a bug, not a risk.
2. **Authorization.** Who may call this? Is the permission enforced at the right layer — an
   `[AuthorizeAdmin]` attribute plus an explicit `IPermissionService` check, not an assumption that the
   caller is already trusted? Does the change expose data this customer/role could not previously reach?
   ACL and store mapping (`IAclService`) where applicable.
3. **Consistency across an operation.** If the change writes more than one entity, or writes and then
   publishes an event, what happens if a later step fails after an earlier one committed? Note that
   `IEventPublisher` is **synchronous and in-process** — a consumer that throws propagates back into the
   publisher's call, which is a consistency property, not just an error-handling detail.
4. **Caching & invalidation.** Is anything cached (`IStaticCacheManager`, `IShortTermCacheManager`, a
   built product/category cache) that this change makes stale? Is invalidation explicit, or is it left to
   a TTL — and is that TTL actually acceptable here? Multi-instance: without `DistributedCacheConfig`
   pointing at Redis, in-process caches diverge across ECS tasks.
5. **Installation & upgrade path.** Does this run correctly on an **existing** installation, not only a
   fresh one? Forward-only migrations, new settings needing a default for stores that never saw them,
   new locale resources for stores installed before this change, new permissions not granted to existing
   roles. `UninstallAsync` must remove everything `InstallAsync` added.
6. **Dependency failure / degraded mode.** If an external dependency (payment provider, carrier, SMTP,
   Redis) is down or slow, what does the customer-facing behavior become?
7. **Multi-store / multi-language variation.** nopCommerce supports several stores and languages in one
   installation. Does this behave the same across all of them, or does it need to differ — and if it
   differs, is that implemented or assumed? Every user-visible string must be a locale resource.
8. **Cost or load on a downstream dependency.** Could this be invoked in a way that hammers an external
   API or the database (a per-request external lookup, an N+1 in a listing page, an unbounded query
   without `IPagedList`)?
9. **Configuration/environment differences.** Does behavior differ between the local
   `postgresql-docker-compose.yml` environment and ECS? Is that difference covered by a test, or just
   assumed equivalent?
10. **Existing mechanism vs new code.** Does this add a table, column, or branch where nopCommerce already
    ships the mechanism — `ProductTag`, `SpecificationAttribute`, `ProductAttribute`, `GenericAttribute`,
    an existing widget zone? Catching this at refinement is the point; `reviewer` catches the same pattern
    at PR review, but by then it is already implemented.

For each item the spec should state either how it is addressed, or an explicit `N/A` with a one-line
reason — not silence. Silence is what `refinement-verifier` flags as a gap.
