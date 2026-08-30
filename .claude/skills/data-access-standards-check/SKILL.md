---
name: data-access-standards-check
description: >-
  Load this when writing or reviewing any data-access code in this repo — a new plugin-owned entity,
  a service that queries or writes through IRepository, a listing/report query, or anything touching
  caching of persisted data. Use it BEFORE writing the code: this stack is Linq2DB, not EF Core, and
  the patterns a model reaches for by default (DbContext, navigation properties, LINQ that assumes
  lazy loading) compile into something this codebase does not support.
---

# Data Access Standards Check

Full docs: [`Docs/knowledge-base/03-data-access-linq2db-fluentmigrator.md`](../../../Docs/knowledge-base/03-data-access-linq2db-fluentmigrator.md)
and [`Docs/ai-harness/03-database-postgres.md`](../../../Docs/ai-harness/03-database-postgres.md).
This is the checklist form. For schema changes see `migration-standards-check`; for adding a field to a
core entity see `entity-extension-check`.

## The stack (non-negotiable)

- **Linq2DB** for queries, **FluentMigrator** for schema. There is no EF Core anywhere in this codebase.
  No `DbContext`, no `DbSet<T>`, no `OnModelCreating`, no `AddDbContext`, no `Add-Migration`.
- **`IRepository<TEntity>`** is the entry point — inject it, never construct a data connection.
  `IRepository<T>` needs **no DI registration**; a generic factory already resolves it.
- **`BaseEntity`** for every persisted POCO. **No navigation properties**, ever — Linq2DB does not
  support them and their absence is deliberate. Resolve related data through an explicit
  service/repository call.

## Entities

- [ ] Domain class is a plain POCO deriving from `BaseEntity`, in the plugin's `Domain` namespace.
- [ ] No `virtual ICollection<X>` / navigation properties.
- [ ] A `NopEntityBuilder<TEntity>` exists for anything beyond default int/string columns — foreign
      keys, explicit lengths, decimals, nullability.
- [ ] Foreign keys declared as `.ForeignKey<TPrincipal>(onDelete: Rule...)` in the builder, not as a
      navigation property or a hand-written constraint.

## Queries

- [ ] Reads go through `IRepository<T>.GetByIdAsync` / `.Table` / `GetAllAsync`, composed as `IQueryable`.
- [ ] Paged results use `IPagedList` — no unbounded `ToListAsync()` on a table that grows.
- [ ] No N+1: a per-row lookup inside a loop over query results is the failure mode to check for.
- [ ] No raw SQL. If it seems unavoidable, that is a design question for the spec, not a local decision.

## Writes and the events they raise

`InsertAsync`/`UpdateAsync`/`DeleteAsync` take `bool publishEvent = true`. A plain `InsertAsync(entity)`
therefore **already raises `EntityInsertedEvent<T>`** for every subscribed `IConsumer<T>`.

- [ ] Do not hand-publish a "something changed" event alongside a repository write — the repository
      already did it. See `event-consumer-standards-check`.
- [ ] `publishEvent: false` is passed only for a bulk/internal write that deliberately must stay silent,
      and the reason is stated in a comment.

## PostgreSQL specifics

This project targets PostgreSQL, where the provider remaps every CLR `string` to **`citext`**:

- [ ] No `ILIKE` or `LOWER()` added "to make comparison case-insensitive" — `citext` is already
      case-insensitive, and these defeat indexes.
- [ ] No provider-specific SQL: no `WITH (NOLOCK)`, `GETDATE()`, `ISNULL(`, `TOP n`, `GROUP_CONCAT`,
      backtick identifiers, `sp_` procedures, or `IDENTITY` syntax.
- [ ] **`AsString(n)` does not enforce length at the database level on Postgres.** Every bounded string
      column is paired with a `RuleFor(m => m.X).MaximumLength(n)` in the matching
      `BaseNopValidator<TModel>` — otherwise Postgres silently accepts an over-length value that a
      SQL-Server-backed deployment of the same code would have rejected.

## Caching

- [ ] Cache keys are `CacheKey` constants on the plugin's `{Name}Defaults` class, never inline strings.
- [ ] Anything this write makes stale is invalidated explicitly — by key, or by prefix for a set.
- [ ] `IShortTermCacheManager` for per-request reuse; `IStaticCacheManager` otherwise.
- [ ] Multi-instance: in-process caches diverge across ECS tasks unless `DistributedCacheConfig` points
      at Redis. If this change introduces cache state that must be coherent across instances, say so in
      the spec — see [`Docs/knowledge-base/10-configuration-appsettings.md`](../../../Docs/knowledge-base/10-configuration-appsettings.md).

## Before calling data-access work done

- [ ] No EF Core type, attribute, or method anywhere in the diff.
- [ ] No navigation properties on any entity.
- [ ] Every new bounded string column has a matching validator length rule.
- [ ] Cache invalidation is explicit, or its absence is a deliberate, stated TTL decision.
- [ ] Service methods that will be overridden by other plugins are `virtual` (the codebase convention).
