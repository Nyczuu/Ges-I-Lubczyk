# appsettings.json Reference

Source: adapted from `developer/tutorials/appsettings-json-file.html`. **All settings in this file
can be overridden by environment variables** — this is the primary mechanism for container/ECS
configuration; see [11-deployment-docker-iis-azure.md](11-deployment-docker-iis-azure.md).

## DataConfig — the database connection

```json
"DataConfig": {
  "ConnectionString": "",
  "DataProvider": "PostgreSQL",
  "SQLCommandTimeout": null,
  "WithNoLock": false
}
```

- `DataProvider` ∈ `SqlServer | MySql | PostgreSQL` — set at install time, stored back into this file
  (or overridden by `DataConfig__DataProvider` / connection-string env vars in a container).
- `WithNoLock` is SQL-Server-only — always leave `false` on PostgreSQL; it's a no-op there but keep
  it explicit to avoid confusing future maintainers coming from a SQL Server background.
- `SQLCommandTimeout` — unset uses the provider default; `0` = infinite. Consider setting explicitly
  for long-running import/export jobs.

## CacheConfig

```json
"CacheConfig": { "DefaultCacheTime": 60, "LinqDisableQueryCache": false }
```

## DistributedCacheConfig — for horizontal scaling (multiple ECS tasks)

```json
"DistributedCacheConfig": {
  "DistributedCacheType": "Redis",
  "Enabled": true,
  "ConnectionString": "127.0.0.1:6379,ssl=False",
  "InstanceName": "nopCommerce"
}
```

`DistributedCacheType` ∈ `Memory | SqlServer | Redis | RedisSynchronizedMemory`. **Running more than
one ECS task behind a load balancer without enabling Redis here will cause cache incoherence between
tasks** — this is a hard requirement for any multi-task ECS service definition, not an optional
performance tweak. `RedisSynchronizedMemory` (4.70+) keeps the cache itself in-process memory and uses
Redis purely as a change-notification bus — lower latency than pure Redis, still safe across tasks.

## HostingConfig — required behind an ALB/ECS load balancer

```json
"HostingConfig": {
  "UseProxy": true,
  "ForwardedProtoHeaderName": "X-Forwarded-Proto",
  "ForwardedForHeaderName": "X-Forwarded-For",
  "KnownNetworks": "10.0.0.0/8"
}
```

Without `UseProxy: true` (and correct forwarded-header names) behind an AWS ALB, nopCommerce will see
every request as `http` from the ALB's private IP — breaking HTTPS redirects, `IWebHelper` URL
generation, and IP-based logic.

## CommonConfig — relevant flags

- `UseAutofac` — Autofac vs. the default .NET container; leave as shipped unless there's a specific
  reason to change it.
- `PermitLimit` / `QueueCount` / `RejectionStatusCode` — built-in rate limiting (`FixedWindowRateLimiter`);
  `0` disables the corresponding feature.
- `DisplayFullErrorStack` — always `false` in production; ignored (always verbose) in Development.

## InstallationConfig — useful for scripted/unattended install (CI, container first-boot)

```json
"InstallationConfig": {
  "DisableSampleData": true,
  "DisabledPlugins": ""
}
```

## Overriding via environment variables (double-underscore convention)

ASP.NET Core's configuration binder maps `Section__Key` env vars onto nested JSON, e.g.:

```bash
DataConfig__DataProvider=PostgreSQL
DataConfig__ConnectionString="Host=db;Database=nopcommerce;Username=nop;Password=***"
HostingConfig__UseProxy=true
DistributedCacheConfig__Enabled=true
DistributedCacheConfig__ConnectionString=redis.internal:6379
```

This is the recommended path for AWS ECS task definitions — inject secrets (DB password, Redis auth)
via ECS `secrets` (Secrets Manager/SSM) into these env var names rather than baking them into
`appsettings.json` or `appsettings.Production.json` in the image.
