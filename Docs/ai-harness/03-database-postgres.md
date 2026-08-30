# PostgreSQL Guidelines

Verified against `src/Libraries/Nop.Data/DataProviders/PostgreSqlDataProvider.cs` and
`DataProviderType.cs`. Context: [knowledge-base/03](../knowledge-base/03-data-access-linq2db-fluentmigrator.md),
[knowledge-base/10](../knowledge-base/10-configuration-appsettings.md).

## PostgreSQL is native, not a shim

`DataProviderType.PostgreSQL` (`[EnumMember(Value = "postgresql")]`) is one of three first-class,
directly-implemented `INopDataProvider`s alongside SQL Server and MySQL — there is no compatibility
layer or translation step to reason about. Set in `appsettings.json`:

```json
"DataConfig": { "DataProvider": "PostgreSQL", "ConnectionString": "Host=db;Database=nopcommerce;Username=nop;Password=***" }
```
or via env vars in a container: `DataConfig__DataProvider=PostgreSQL`, `DataConfig__ConnectionString=...`.

## The one PostgreSQL-specific gotcha: string columns map to `citext`

`[verified: src/Libraries/Nop.Data/DataProviders/PostgreSqlDataProvider.cs]` —
`PostgreSqlDataProvider.CreateDataConnection()` explicitly remaps the CLR `string` type:

```csharp
dataContext.MappingSchema.SetDataType(typeof(string), new SqlDataType(new DbDataType(typeof(string), "citext")));
```

This means every `string` property on every entity is backed by PostgreSQL's **`citext`**
(case-insensitive text) type, not plain `text`/`varchar`. Practical consequences:

- String equality/`LIKE` comparisons against nopCommerce tables are **case-insensitive by default on
  Postgres**, matching SQL Server's default collation behavior — this is intentional parity, not a
  bug. Don't "fix" it by adding `ILIKE`/`LOWER()` calls; they're redundant and can defeat indexes.
  Don't assume MySQL/SQL-Server-observed case-sensitivity carries over either way when writing
  cross-provider-safe queries — trust `citext`'s semantics on this provider.
- The `citext` extension must be enabled on the target Postgres database
  (`CREATE EXTENSION IF NOT EXISTS citext;`) — nopCommerce's own installer/migration runner handles
  this on a fresh install; if provisioning a database out-of-band (e.g. a pre-created RDS/Aurora
  instance for ECS), confirm the extension is enabled before pointing the app at it, or every schema
  migration touching a string column will fail.
- Never write a migration or query that special-cases `citext` conversion manually — this is handled
  transparently by the data provider; write ordinary `AsString(...)` column definitions in
  `NopEntityBuilder<T>` regardless of target database.

## What NOT to do on this provider

- **No T-SQL-specific hints.** `WithNoLock` in `appsettings.json`'s `DataConfig` is SQL-Server-only;
  always leave it `false` for PostgreSQL (it's a no-op there, but setting it `true` signals a
  misunderstanding of the provider to future readers).
- **No MySQL-only functions** (`GROUP_CONCAT`, backtick-quoted identifiers) or SQL-Server-only
  functions (`ISNULL`, `TOP n`, `GETDATE()`) in any raw SQL that might appear in a migration or
  reporting query — Linq2DB/FluentMigrator abstractions are cross-provider by design; dropping to
  raw, provider-specific SQL defeats that and breaks portability the moment someone runs this project
  against a different provider (e.g. local MySQL dev vs. Postgres in ECS).
- **No `sp_` stored procedures, no `IDENTITY` column syntax** — auto-increment/identity handling is
  abstracted by `INopDataProvider`; `NopEntityBuilder<T>` + `PrimaryKey()` is sufficient, the provider
  picks the right underlying mechanism (`SERIAL`/`IDENTITY GENERATED` for Postgres) automatically.
- **No Entity Framework Core Npgsql provider** — irrelevant; this stack has no EF Core anywhere.

## Type mapping quick reference (C# domain property → Postgres column)

| `NopEntityBuilder` call | Postgres column type |
|---|---|
| `.AsInt32()` | `integer` |
| `.AsInt64()` | `bigint` |
| `.AsBoolean()` | `boolean` |
| `.AsString(n)` | `citext` (length `n` not enforced at the DB level the way `varchar(n)` would be — enforce max length in the FluentValidation validator, not just the column) |
| `.AsDecimal(precision, scale)` | `numeric(precision, scale)` |
| `.AsDateTime2()` | `timestamp` |
| `.AsDouble()` | `double precision` |

Since `AsString(n)` doesn't hard-cap length at the Postgres level the way SQL Server's `nvarchar(n)`
does, always pair a bounded string column with a matching `RuleFor(m => m.X).Length(0, n)` (or
`MaximumLength(n)`) in the corresponding `BaseNopValidator<TModel>` — otherwise Postgres silently
accepts an over-length value that a SQL Server-backed deployment of the same code would reject.

## Local development against Postgres

Repo root already ships `postgresql-docker-compose.yml` pairing `nopcommerce_web` with
`postgres:latest`. Prefer this over `docker-compose.yml` (SQL Server-oriented) for any local
development or CI job that should mirror the AWS ECS target's database engine.
