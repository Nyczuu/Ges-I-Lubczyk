---
name: dotnet-build-doctor
description: >-
  Load this when a build, test run, or app start fails in this repo, or when a plugin you just wrote
  does not appear in Local Plugins. It maps the known nopCommerce-specific failure signatures to their
  actual cause, so a mechanical environment or wiring problem is not misdiagnosed as a bug in the code
  being written. Confirm the error text matches a signature below before applying its fix.
---

# .NET Build Doctor

Diagnostic map for failures that are **not** bugs in the diff. If the symptom is not listed here, treat
it as a real problem in the code, not as an environment quirk.

## Plugin builds fine but does not appear in Local Plugins

Most common failure, and it produces no error at all.

1. `<OutputPath>` / `<OutDir>` missing or wrong — the assembly went to the project's own `bin/` instead
   of `$(SolutionDir)\Presentation\Nop.Web\Plugins\{Group}.{Name}`. Compare against a sibling plugin.
2. `plugin.json` missing from the output — it needs `<Content>` with
   `CopyToOutputDirectory = PreserveNewest`.
3. `SupportedVersions` does not include `"5.00"` — the loader skips the plugin silently.
4. `SystemName` collides with another installed plugin.
5. Stale output from a previous build under `Presentation/Nop.Web/Plugins/` — delete that folder's
   contents and rebuild before investigating anything else.

## Views or static assets missing at runtime, no build error

The `.cshtml` / `Content` file is not listed as `<Content>` with `PreserveNewest` in the `.csproj`.
Every view in this repo's plugins is listed explicitly; a new one that is not listed does not ship.

## Duplicate or conflicting assembly errors when a plugin loads

The `NopTarget` post-build target invoking `Build/ClearPluginAssemblies.proj` is missing from the plugin
`.csproj`, so framework DLLs were copied into the plugin output alongside the app's own copies. Copy the
target from a sibling plugin. Related: `CopyLocalLockFileAssemblies` set to `true` when the plugin has no
NuGet dependencies that need it.

## SDK version mismatch

`global.json` pins `10.0.100` with `rollForward: latestFeature`. A machine without a 10.0.x SDK fails at
restore. Check `dotnet --list-sdks` before assuming the project file is wrong.

## Scheduled task never fires, nothing in the log

`ScheduleTask.Type` must be exactly `Namespace.ClassName, AssemblyName`. A mismatch fails silently at
runtime. Also: a newly inserted task row needs an application restart to be picked up.

## Migration fails on a string column against PostgreSQL

The `citext` extension is not enabled on the target database. nopCommerce's own installer handles it on a
fresh install, but a database provisioned out of band (a pre-created RDS/Aurora instance) may not have it
— every migration touching a string column then fails. Fix the database
(`CREATE EXTENSION IF NOT EXISTS citext;`), not the migration.

## Long path failures on Windows

The plugin output path under `Presentation/Nop.Web/Plugins/` plus a deep worktree path can exceed
`MAX_PATH`. Verify Windows long-path support is actually enabled before claiming it as the cause — that
exact claim has been made confidently and wrongly before, on a machine where the setting was already on
and the failure still reproduced. If it is already enabled, the cause is something else.

## Integration/data tests failing on a clean checkout

Data-touching tests run migrations against SQLite via `SqLiteNopDataProvider`. A migration that uses
provider-specific SQL passes against PostgreSQL and fails here — that is the test suite catching a real
portability bug, not a broken test harness.

## Rule for using this skill

Report what the error actually says, quoting the shortest decisive line. Do not restate a diagnosis you
have not confirmed against the failing output — an environment claim is exactly the kind neither the
compiler nor the tests will contradict.
