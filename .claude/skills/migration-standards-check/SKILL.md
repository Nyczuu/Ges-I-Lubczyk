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
      an incrementing counter. Two migrations must never share one — and "never" is **solution-wide**,
      not just within your own plugin: `MigrationVersionInfo` is a single global table with a unique
      index on `Version`, and `NopMigrationAttribute.Version` is nothing but `Ticks` of the literal
      timestamp string, with no per-plugin or per-assembly salt. Two unrelated plugins' migrations dated
      the identical `"yyyy-MM-dd HH:mm:ss"` collide. Before landing a migration, grep the whole repo for
      its exact date:
      `grep -rn 'NopMigration("<your-date>' src` (or the equivalent Grep-tool call) and disambiguate with
      the seconds field if anything else already uses it — see `Payments.PayPalCommerce`'s
      `SchemaMigration`/`AdvancedCardsMigration` pair (`00:00:00` / `00:00:01` same day) or
      `Tax.Avalara`'s `00:00:02` migrations for the precedent.
      **The failure mode is silent and looks unrelated:** whichever migration's version gets recorded
      first — including via `BaseDataProvider.InitializeDatabase()`'s "mark update migrations as
      applied" fresh-install pass, which runs before any explicit `Installation`-type call and marks
      every discovered `Update`-type migration as applied *without running it* — makes
      `HasAppliedMigration` return true for that version number for every other migration that happens
      to share it, so the second migration is skipped as "already applied." No exception, no log line;
      the only symptom is the second migration's own table never getting created, surfacing later as a
      "no such table" failure in a completely different plugin's tests. Confirmed reproduction: a new
      `Nop.Plugin.Misc.Ingredients` migration dated identically to `Nop.Plugin.Misc.ProductionLabels`'s
      `SchemaMigration` silently skipped the latter, failing 12+ unrelated `ProductionBatchServiceTests`
      with `SQLite Error 1: 'no such table: ProductionBatch'`.
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
