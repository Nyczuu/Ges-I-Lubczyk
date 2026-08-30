---
name: security-permissions-check
description: >-
  Load this when a change touches authorization, admin actions, customer-scoped or store-scoped data,
  external input, encryption, or logging of anything personal. Use it BEFORE writing the code: a
  missing permission check or store-scoping filter produces working, testable code whose only symptom
  is that the wrong person can see or do something.
---

# Security & Permissions Check

Full doc: [`Docs/knowledge-base/08-settings-permissions-validation.md`](../../../Docs/knowledge-base/08-settings-permissions-validation.md).
This is the checklist form. Controller mechanics live in `admin-ui-standards-check`.

## Permissions

- [ ] Declared via `IPermissionConfigManager.AllConfigs` (the 4.80+ mechanism). Records install
      automatically — **do not generate an `IPermissionProvider` / `InstallPermissionsAsync`**, that is
      the pre-4.80 pattern and does not apply to this codebase.
- [ ] Reuse a `StandardPermission` (`StandardPermission.Configuration.MANAGE_SETTINGS`, catalog
      permissions, …) rather than inventing a near-duplicate for functionality that already has one.
- [ ] Permission system name is a `const` on the config manager, referenced everywhere — no literals.
- [ ] The permission is **removed in `UninstallAsync`** via `DeletePermissionRecordAsync`. Installation
      is automatic; removal is not.
- [ ] Every admin action is guarded, declaratively with `[CheckPermission(...)]` or imperatively with
      `IPermissionService.AuthorizeAsync(...)`. An unguarded admin action is a finding.
- [ ] The guard is on the action that *does* the thing, not only on the page that links to it.

## Data scoping

- [ ] Customer-scoped data is filtered by the current customer from `IWorkContext` — never by an id
      taken from the request without an ownership check. "The UI only ever sends their own id" is not a
      check.
- [ ] Store-scoped data respects store mapping; catalogue visibility respects `IAclService`. A query
      that ignores store mapping leaks one store's data into another in a multi-store installation.
- [ ] The change does not widen what an existing role can reach as a side effect.

## Input and injection

- [ ] No raw SQL, and no string interpolation into anything SQL-adjacent. Linq2DB parameterises;
      dropping to raw SQL discards that.
- [ ] External input is validated by a `BaseNopValidator<TModel>`, including bounded lengths — Postgres
      will not enforce column width (see `data-access-standards-check`).
- [ ] File uploads: extension and content type checked, path never built from user input.
- [ ] `[AutoValidateAntiforgeryToken]` on state-changing admin controllers; no POST action exempt
      without a stated reason.

## Secrets and sensitive data

- [ ] No credential, API key, or connection string in source, in `appsettings.json` committed to the
      repo, or in a locale resource. Configuration comes from environment variables at deploy time.
- [ ] Passwords and tokens go through `IEncryptionService` / the platform's own handling — never a
      hand-rolled hash or a reversible scheme invented for the occasion.
- [ ] Nothing personal or secret is written to logs: no full card data, no password, no token, no full
      address dump in an exception message. Log identifiers, not payloads.
- [ ] Exception messages surfaced to a customer say what they need, not what the system is; the detail
      goes to the log.

## Before calling this done

- [ ] Every new admin action has an explicit permission check.
- [ ] Permission removed on uninstall.
- [ ] Every query touching customer- or store-scoped data is scoped in the query, not by assumption.
- [ ] No secrets in the diff; no sensitive values in log statements.
