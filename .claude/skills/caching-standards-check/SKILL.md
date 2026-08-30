---
name: caching-standards-check
description: >-
  Load this when a change caches something, invalidates something cached, or writes data that another
  cache already holds. Use it BEFORE writing the code: this app runs as more than one ECS task, so an
  in-process cache that looks correct on a developer machine goes incoherent in production, and stale
  reads surface as "sometimes wrong" rather than as a failure anyone can reproduce.
---

# Caching Standards Check

Full docs: the Caching section of [`Docs/ai-harness/01-architecture-and-standards.md`](../../../Docs/ai-harness/01-architecture-and-standards.md)
and [`Docs/knowledge-base/10-configuration-appsettings.md`](../../../Docs/knowledge-base/10-configuration-appsettings.md).
This is the checklist form.

## Which manager

| Manager | Use for |
|---|---|
| `IStaticCacheManager` | the default — data reused across requests |
| `IShortTermCacheManager` | data reused several times within one request |
| `PerRequestCacheManager` | per-request state, not a general cache |

- [ ] The choice is deliberate. Caching per-request data in the static cache is a correctness bug, not
      just waste.

## Keys

- [ ] Every key is a `CacheKey` constant on the plugin's `{Name}Defaults` class. No inline strings, no
      string concatenation at the call site.
- [ ] Keys carry every variable the cached value depends on — entity id, **store id**, **language id**,
      customer role set where relevant. A key that omits language returns Polish text to an English
      store, and nothing fails.
- [ ] Related keys share a prefix so a set can be invalidated at once.

## Invalidation

- [ ] Every write path that changes cached data invalidates it — by key, or by prefix for a set.
- [ ] Relying on TTL expiry instead of explicit invalidation is a decision, stated with the acceptable
      staleness window. Silence here is the gap `refinement-verifier` flags.
- [ ] `IRepository<T>`'s built-in entity events are the natural invalidation hook for entity-derived
      caches — see `event-consumer-standards-check`.

## Multi-instance (ECS) — the part that bites

`IStaticCacheManager` is in-process. With more than one ECS task behind the load balancer, each task
holds its own copy, and an invalidation on one task does not reach the others.

- [ ] If the cached value must be coherent across instances, the deployment needs
      `DistributedCacheConfig` with `Enabled: true` and `DistributedCacheType` of `Redis` or
      `RedisSynchronizedMemory`. `RedisSynchronizedMemory` keeps the cache in-process and uses Redis
      only as a change-notification bus — lower latency, still coherent.
- [ ] A change that introduces cross-instance-sensitive cache state says so in the spec, because it is
      an infrastructure requirement, not just code.
- [ ] Nothing in the code assumes the cache is warm, or that a value it just wrote is readable back from
      cache on the next request — it may land on another task.

## Before calling caching work done

- [ ] Keys are constants, and include store/language where the value varies by them.
- [ ] Every write invalidates what it makes stale, or accepts a stated TTL.
- [ ] Cross-instance coherence considered explicitly, not assumed.
