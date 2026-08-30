---
name: migration-standards-check
description: >-
  Load this when writing or reviewing any schema change — a new table, a new or altered column, an
  index, or a plugin's installation migration. Use it BEFORE writing the migration: a migration that
  has already run somewhere cannot be edited, so the cost of getting the attribute, the timestamp, or
  the idempotency wrong is a second corrective migration rather than a fix.
---

# Migration Standards Check

Full doc: [`Docs/knowledge-base/03-data-access-linq2db-fluentmigrator.md`](../../../Docs/knowledge-base/03-data-access-linq2db-fluentmigrator.md).
This is the checklist form. Provider-specific SQL rules live in
[`Docs/ai-harness/03-database-postgres.md`](../../../Docs/ai-harness/03-database-postgres.md).

## Shape

```csharp
[NopSchemaMigration("2026-08-30 00:00:00", "Misc.MyPlugin base schema", MigrationProcessType.Installation)]
public class SchemaMigration : ForwardOnlyMigration
{
    public override void Up() => this.CreateTableIfNotExists<MyRecord>();
}
```

- [ ] Attribute is `[NopSchemaMigration]` / `[NopMigration]` / `[NopUpdateMigration]` — never
      FluentMigrator's raw `[Migration(longVersion)]`.
- [ ] The version is a **sortable timestamp string** `"yyyy-MM-dd HH:mm:ss"` set to authoring time, not
      an incrementing counter. Two migrations must never share one.
- [ ] `MigrationProcessType` is set deliberately — `Installation` for a plugin's own schema.
- [ ] Base class is `ForwardOnlyMigration` unless a genuine `Down()` is both possible and wanted.

## Use the verified extensions, not the doc-page names

- [ ] `this.CreateTableIfNotExists<TEntity>()` — reads the matching `NopEntityBuilder<T>` and checks
      `Schema.Table(...).Exists()` first, so it is safe to re-run.
- [ ] `this.AddOrAlterColumnFor<TEntity>(x => x.Prop)` for a single column;
      `AddOrAlterForeignKeyColumnFor<TEntity, TPrincipal>(x => x.Prop)` when it is also an FK.
- [ ] **`Create.TableFor<T>()` and `IMigrationManager.BuildTable<T>()` do not exist in this codebase.**
      They appear in older nopCommerce documentation pages. If a snippet disagrees with
      `Nop.Data.Extensions.FluentMigratorExtensions`, the source wins.

## Idempotency and re-runs

- [ ] `Up()` is safe to execute twice — that is what the `IfNotExists` / `AddOrAlter` extensions buy.
- [ ] The migration applies cleanly to an **existing installation**, not only to a fresh install. A new
      non-nullable column on a populated table needs a default or a backfill; state which.
- [ ] Nothing in the migration deletes or rewrites customer data without that being the explicit,
      spec-level intent.
- [ ] **Deploy-safe under expand/contract.** Migrations run at application startup, so during a rolling
      ECS deployment the new version's schema is live while old-version tasks are still serving. A
      migration that drops or renames a column the old version reads takes the site down mid-deploy —
      expand now, contract in a later release. See `deployment-standards-check`.

## Hard boundaries

- [ ] **Never edit a migration that has already shipped.** Its version is recorded in the target
      database; changing the body means installed stores keep the old schema while the code expects the
      new one. Write a new migration instead.
- [ ] **Never add or modify anything under `src/Libraries/Nop.Data/Migrations/UpgradeTo*/`.** Those are
      nopCommerce's own version-upgrade migrations, not a place for project schema changes — this is a
      documented red flag in [`Docs/ai-harness/02-extensibility-and-plugins.md`](../../../Docs/ai-harness/02-extensibility-and-plugins.md).
- [ ] No hand-written provider-specific DDL. `NopEntityBuilder<T>` plus `PrimaryKey()` covers
      auto-increment; the provider picks `SERIAL`/`IDENTITY GENERATED` itself.

## Plugin migrations

- [ ] `plugin.json`'s `Version` is bumped in the same change as a new migration — the runner re-applies
      migrations on a version increase, and without the bump the migration never runs on an update.
- [ ] Whatever the migration creates, `UninstallAsync` accounts for (see `plugin-standards-check`).

## Tests

- [ ] There is a test that exercises the new schema through the service that uses it (the SQLite test
      provider runs migrations) — see [`Docs/knowledge-base/13-testing.md`](../../../Docs/knowledge-base/13-testing.md).

## Before calling a migration done

- [ ] Unique timestamp, correct attribute, correct `MigrationProcessType`.
- [ ] `Up()` idempotent and correct against a populated existing database.
- [ ] `plugin.json` `Version` bumped.
- [ ] No shipped migration edited, nothing written under `UpgradeTo*/`.
