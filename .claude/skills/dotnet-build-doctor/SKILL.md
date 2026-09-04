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

## A controller fix looks unapplied at runtime after a rebuild

Rebuilding while the app (`Nop.Web.exe`/`dotnet run`) is still running locks its own plugin `.dll`
under `Presentation/Nop.Web/Plugins/{Group}.{Name}/`. MSBuild retries the copy ~10 times, then fails
with `MSB3027: Could not copy ... Exceeded retry count of 10 ... The file is locked by: "Nop.Web (<pid>)"`
— this shows up only as extra error lines in the build's tail, not as a solution-wide build failure if
other projects still compiled, so it is easy to miss. The freshly compiled `obj/Debug/.../*.dll` is
correct; the one actually deployed under `Presentation/Nop.Web/Plugins/...` is still the old one.
Restarting the already-running process does **not** help — that process still holds its old copy open
and keeps serving it. Symptom: a fix confirmed present in source (e.g. an explicit view path) still
reproduces the exact pre-fix error, but only for actions not yet exercised since the last build that
copied successfully — which can make the fix look like it "didn't work" for that specific action while
appearing to work for another one built earlier. Fix: fully stop the running app, rebuild, confirm the
tail has no `MSB3026`/`MSB3027`, then start it again.

## Duplicate or conflicting assembly errors when a plugin loads

The `NopTarget` post-build target invoking `Build/ClearPluginAssemblies.proj` is missing from the plugin
`.csproj`, so framework DLLs were copied into the plugin output alongside the app's own copies. Copy the
target from a sibling plugin. Related: `CopyLocalLockFileAssemblies` set to `true` when the plugin has no
NuGet dependencies that need it.

## Building a single plugin `.csproj` directly fails with cascading "namespace does not exist" errors

Symptom: `dotnet build src/Plugins/Nop.Plugin.{Group}.{Name}/Nop.Plugin.{Group}.{Name}.csproj` (invoked
directly, not through `src/NopCommerce.sln`) fails with a pile of `CS0234`/`CS0246` errors against types
that obviously exist — `ILocalizationService`, `IRepository<>`, `Nop.Web.Areas.Admin...` — even right
after a full `dotnet restore src/NopCommerce.sln`. Looks exactly like a broken reference in the code just
written; it is not.

Cause: every plugin's `.csproj` declares its `ProjectReference` to `Nop.Web.csproj` as
`$(SolutionDir)\Presentation\Nop.Web\Nop.Web.csproj`. `$(SolutionDir)` is only populated by MSBuild when
the build entry point is the `.sln` itself. Build the plugin `.csproj` on its own and `$(SolutionDir)` is
empty, so that direct reference silently fails to resolve — the plugin's *transitive* dependencies
(`Nop.Core`, `Nop.Data`, `Nop.Services`, `Nop.Web.Framework`) still build fine because restore already
flattened those into the plugin's `project.assets.json`, which is what makes the failure look selective
and confusing rather than a flat "can't find the project" error.

Fix, in order of preference:

1. Build via the solution, per [`AGENTS.md`](../../../AGENTS.md#how-to-verify) — sidesteps the problem
   entirely and is the documented way to verify a change.
2. If a fast, scoped rebuild of one plugin is genuinely needed, pass `$(SolutionDir)` explicitly as an
   **absolute** path with a trailing separator:
   `dotnet build src/Plugins/Nop.Plugin.{Group}.{Name}/Nop.Plugin.{Group}.{Name}.csproj -p:SolutionDir=<absolute-path-to-src>\`
   A relative value (e.g. `src\`) does not work — MSBuild evaluates it relative to the project file's own
   directory, not the shell's working directory, so it silently reproduces the same failure.

## A different plugin's tests suddenly fail with "no such table: X", and nothing you touched looks related

Symptom: tests for a plugin you did not modify start failing with e.g.
`Microsoft.Data.Sqlite.SqliteException : SQLite Error 1: 'no such table: ProductionBatch'`, right after
a change that only added a migration to a *different* plugin.

Cause: `NopMigrationAttribute.Version` is `Ticks` of the literal `"yyyy-MM-dd HH:mm:ss"` string passed to
it, with no per-plugin salt, and `MigrationVersionInfo` — the table FluentMigrator uses to track which
versions have run — is one table shared across the entire solution with a unique index on `Version`. If
your new migration's date/time string is byte-identical to some other plugin's existing migration, both
compute the same `Version`. `BaseDataProvider.InitializeDatabase()`'s "mark update migrations as
applied" fresh-install pass runs first and marks every `Update`-type migration it discovers as applied
**without running its `Up()`** — so if your migration is the one that gets marked first, the other
plugin's colliding migration is later found "already applied" and is silently skipped, whether it was
`Update` or `Installation` type, whether or not it ever actually ran. No error, no log line — the only
symptom is that plugin's table never getting created, discovered only when its own tests query it.

Confirm the diagnosis cheaply: `grep -rn 'NopMigration("<the suspect date>' src` — if it matches more
than one migration, that is the bug. Fix by changing one migration's seconds field to something unique
(see `migration-standards-check` for the write-time check and existing precedent for disambiguating
same-day migrations). Do not "fix" this by touching the failing plugin's own migration or test setup —
it never ran because of an unrelated migration's collision, not because of anything wrong in it.

## Thousands of errors, all NU1301 / 401 against a feed that is not nuget.org

```
error NU1301: Unable to load the service index for source https://<something>.codeartifact...
error NU1301:   Response status code does not indicate success: 401 (Unauthorized).
```

Restore is querying a private feed inherited from the machine-level
`%APPDATA%\NuGet\NuGet.Config`, whose credentials have expired. Every package fails, so the error count
is enormous and looks catastrophic — it is one problem, not thousands, and none of them are in the code.

The repo-level `nuget.config` at the root exists to prevent exactly this: its `<clear />` drops inherited
sources before adding nuget.org. If you see this signature, check that file is present and that the build
is running from the repo root. Confirm the diagnosis cheaply before acting:

```bash
dotnet restore src/Libraries/Nop.Core/Nop.Core.csproj --source https://api.nuget.org/v3/index.json
```

If that succeeds, the private feed is the whole problem. Never "fix" this by adding credentials for a
feed this project does not need packages from.

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

## Giving a plugin test coverage for the first time — schema never created, or DI can't resolve it

`Nop.Tests` does not reference any plugin project by default, and `BaseNopTest`'s test-only DI container
does not auto-discover `INopStartup` implementations the way the real app does — both have to be wired
explicitly, and the wiring is easy to get subtly wrong in a way that produces no build error and only
shows up as unrelated tests breaking. This happened for real once already (GIL-001, the first plugin in
this repo to get test coverage at all — check whether a plugin has already gone through this before
repeating the investigation):

1. **`ProjectReference` from `Nop.Tests.csproj` to the plugin's `.csproj`** — without it, the plugin's
   types aren't visible to the test project at all.
2. **A `PluginDescriptor` entry plus an explicit
   `migrationManager.ApplyUpMigrations(pluginAssembly, MigrationProcessType.Installation)` call in
   `ServiceTest.InitPlugins()`** — marking a plugin descriptor "installed" only affects `IPluginManager`
   resolution, it does not create the plugin's schema. Skip this and every test touching the plugin's
   entities fails with a "no such table" SQLite error that looks like a broken migration when the
   migration is actually fine, just never run.
3. **Hand-register the plugin's services in `BaseNopTest`'s `ConfigureServices`**, mirroring every other
   service already registered there — `INopStartup.ConfigureServices` is never invoked by this test
   harness, so a plugin's own DI registrations are simply skipped, producing a DI-resolution failure at
   the first `GetService<T>()` call for anything the plugin owns.
4. **If (and only if) the plugin's migration derives from `Migration` rather than `ForwardOnlyMigration`**
   (needed for a real `Down()`, e.g. so uninstall actually drops the plugin's tables) — `BaseNopTest`'s
   migration-assembly scan only calls `FindClassesOfType<ForwardOnlyMigration>()`, so that migration is
   invisible to it. **Union in that one plugin's assembly by direct type reference
   (`.Union([typeof(TheirSchemaMigration).Assembly])`); do not widen the `FindClassesOfType<T>` type
   argument itself to `MigrationBase` "to be safe."** That widening is not scoped to the new plugin — it
   also newly exposes `Nop.Web.Framework`'s real 4.40→5.00 upgrade-path migrations (which have plenty of
   `MigrationBase`-derived, non-`ForwardOnlyMigration` classes: `SettingMigration`, `AclMigration`,
   `LocalizationMigration`, etc.) to `ApplyUpMigrations`, and those run for real against the freshly
   installed SQLite test database — silently corrupting seeded defaults for tests that have nothing to do
   with the plugin being added. Confirmed reproduction: this exact widening broke
   `TaxServiceTests.CanGetProductPrice` and `ProductAttributeParserTests.CanRenderAttributesWithoutPrices`
   with no error at the point of the change, only a wrong assertion value several files away.

Step 4's fix is a **named, single-plugin literal, not a general rule** — the next plugin that also needs
a `Migration`-based schema migration under test will hit the identical silent no-op unless someone
remembers to extend that same union. There is no automatic way to make this generic without reintroducing
the exact regression above; check `src/Tests/Nop.Tests/BaseNopTest.cs`'s migration-scan comment for the
current list before assuming it already covers a new plugin.

## Integration/data tests failing on a clean checkout

Data-touching tests run migrations against SQLite via `SqLiteNopDataProvider`. A migration that uses
provider-specific SQL passes against PostgreSQL and fails here — that is the test suite catching a real
portability bug, not a broken test harness.

## Rule for using this skill

Report what the error actually says, quoting the shortest decisive line. Do not restate a diagnosis you
have not confirmed against the failing output — an environment claim is exactly the kind neither the
compiler nor the tests will contradict.
