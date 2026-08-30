---
name: upgrade-safety-detector
description: >-
  Load this before merging a change that touches a plugin's SystemName, settings, locale resource keys,
  permission records, database schema, or a public service signature other plugins call. It classifies
  each change as safe or breaking for stores that already have the previous version installed — a
  dimension neither the compiler nor the test suite covers, since both only ever see a fresh install.
---

# Upgrade Safety Detector

A load-before-merge diff check, not a write-time gate. The question it answers: **what happens to a store
that already has the previous version installed and running?**

Everything in this repo's test suite runs against a fresh database. That means every failure mode below
is invisible to a green build.

## Method

Diff the change against the previous version of the same files and classify each item.

## Breaking — needs a migration path, not just a code change

| Change | What breaks |
|---|---|
| `SystemName` changed or removed | The store loses every setting, permission, and installed-state row tied to the old name. The plugin appears as a new, unconfigured plugin; the old rows are orphans. Effectively irreversible for the customer. |
| Locale resource key removed or renamed | Any view still referencing it renders an empty label or the raw key. Renaming is a remove + add: both halves needed, in the update path. |
| Setting property removed or renamed | The stored value is orphaned and the new property reads its type default — silently changing behaviour on an existing store. |
| Permission system name changed | Existing role grants point at the old name; the action becomes inaccessible to roles that had it. |
| Column dropped, renamed, or narrowed | Data loss, and — because migrations run at app startup during a rolling deploy — potential mid-deploy failure for the old version still serving traffic. |
| Non-nullable column added without default or backfill | Migration fails on a populated table. |
| Public service method signature changed | Any other plugin compiled against it breaks. In a fork this may be nothing, but check before assuming. |
| Event class shape changed | Consumers in other plugins read properties that no longer exist. |

## Safe

- New setting property with a sensible default.
- New locale resource key, **provided it is added through the update path too** — an existing store never
  re-runs `InstallAsync`, so a key added only there reaches new installs only.
- New nullable column, new table.
- New permission record (installed automatically), provided the roles that need it are considered.
- New method on a service; new event type.

## The update path

- [ ] `plugin.json` `Version` bumped — the migration runner and `UpdateAsync` both key off it. Without
      the bump nothing new runs on an existing store.
- [ ] Anything an existing store needs (new locale keys, a settings migration, a data backfill) happens
      in `UpdateAsync(currentVersion, targetVersion)` or in a versioned migration — not only in
      `InstallAsync`.
- [ ] `UninstallAsync` still removes everything the new version installs.

## Rolling deploy

- [ ] Schema change is expand/contract-safe: additive now, destructive change deferred to a later
      release, because both versions run simultaneously during the roll. See
      `deployment-standards-check`.

## Output

For each classified item state: **change → classification → what an existing store experiences → what the
change needs to become safe.** Report; do not silently "fix" a breaking change by reverting it — whether
to accept the break is the developer's call.
