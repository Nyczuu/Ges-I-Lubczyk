# Deployment — Docker on AWS ECS

Builds on the repo's real, verified `Dockerfile` (see
[knowledge-base/11](../knowledge-base/11-deployment-docker-iis-azure.md) for the walkthrough of that
file). This file covers the AWS-ECS-specific layer on top: task definition shape, storage, secrets,
scaling. nopCommerce's own docs don't cover AWS/ECS — this section is derived from general AWS/.NET
containerization practice applied to the constraints already established in the repo's Dockerfile and
`appsettings.json`.

## Container image — don't regress what's already correct

The existing `Dockerfile` builds `NopCommerce.sln` (all plugin projects included) before publishing
only `Nop.Web.csproj` — this is correct and required; a change that publishes `Nop.Web.csproj` without
first building the full solution will silently drop every plugin from the image. Keep the
Alpine + `icu-libs`/`icu-data-full` + `libgdiplus`/`tiff` layers — they're required for
locale-correct currency formatting and product thumbnail generation respectively, not optional
hardening.

Tag and push to ECR as part of CI:
```bash
aws ecr get-login-password --region <region> | docker login --username AWS --password-stdin <account>.dkr.ecr.<region>.amazonaws.com
docker build -t nopcommerce .
docker tag nopcommerce:latest <account>.dkr.ecr.<region>.amazonaws.com/nopcommerce:<git-sha>
docker push <account>.dkr.ecr.<region>.amazonaws.com/nopcommerce:<git-sha>
```
Tag by commit SHA, not just `latest` — ECS task definitions should reference an immutable tag so a
rollback is a task-definition revision change, not a re-pull of a mutable tag.

## Configuration via environment variables, not baked-in appsettings

Never bake `DataConfig.ConnectionString` or any credential into the image or into
`appsettings.Production.json`. Every setting in `appsettings.json` is overridable via
`Section__Key`-style env vars (see
[knowledge-base/10](../knowledge-base/10-configuration-appsettings.md)) — inject these through the ECS
task definition's `environment` (non-secret) and `secrets` (Secrets Manager/SSM Parameter Store) lists:

```jsonc
// ECS task definition — containerDefinitions[0] (excerpt)
{
  "environment": [
    { "name": "ASPNETCORE_ENVIRONMENT", "value": "Production" },
    { "name": "DataConfig__DataProvider", "value": "PostgreSQL" },
    { "name": "HostingConfig__UseProxy", "value": "true" },
    { "name": "HostingConfig__ForwardedProtoHeaderName", "value": "X-Forwarded-Proto" },
    { "name": "DistributedCacheConfig__Enabled", "value": "true" },
    { "name": "DistributedCacheConfig__DistributedCacheType", "value": "Redis" }
  ],
  "secrets": [
    { "name": "DataConfig__ConnectionString", "valueFrom": "arn:aws:secretsmanager:<region>:<account>:secret:nop/db-connstring" },
    { "name": "DistributedCacheConfig__ConnectionString", "valueFrom": "arn:aws:secretsmanager:<region>:<account>:secret:nop/redis-connstring" }
  ]
}
```

**Never** run more than one ECS task without `DistributedCacheConfig.Enabled = true` pointed at
ElastiCache Redis — with the default in-memory cache, multiple tasks behind the same ALB target group
will diverge on cached catalog/settings data. This is a correctness requirement for horizontal scaling,
not a performance nice-to-have (see [knowledge-base/10](../knowledge-base/10-configuration-appsettings.md)).

## Persistent storage for uploaded media

ECS tasks (Fargate especially) have ephemeral, non-shared container storage — a task restart or a
scale-out to a second task loses/desyncs anything written to the container filesystem. nopCommerce
writes uploaded product images, exported files, and (optionally) Data Protection keys under `wwwroot/`
and `App_Data/`. Two supported approaches, in order of preference for this project:

1. **Azure Blob-style external storage adapted to S3**: the repo already contains a first-party
   pattern for this exact problem — `src/Plugins/Nop.Plugin.Misc.AzureBlob`
   (`[verified: src/Plugins/Nop.Plugin.Misc.AzureBlob/]`) implements external picture storage +
   thumbnail generation as a plugin (`AzureThumbService`, `PictureCacheEventConsumer`). For AWS,
   either use the community/commercial nopCommerce S3 media provider plugin if one is adopted, or
   write an equivalent `Nop.Plugin.Misc.S3Storage` plugin following that exact shape (implement the
   same media-provider seam the AzureBlob plugin hooks into, backed by the AWS S3 SDK instead of
   `Azure.Storage.Blobs`). This is the architecturally correct fix — it removes the shared-filesystem
   requirement entirely.
2. **EFS-backed volume mount** (faster to stand up, keep as an interim/simpler option): mount an EFS
   access point at `wwwroot/images`, `wwwroot/files/exportimport`, `wwwroot/bundles`,
   `wwwroot/db_backups`, and `App_Data/DataProtectionKeys` via the ECS task definition's
   `volumes`/`mountPoints` (works for both EC2-backed and Fargate tasks). This satisfies the
   `Dockerfile`'s own `chmod` list of writable directories without requiring a plugin rewrite, but
   adds an EFS dependency and NFS-latency characteristics to every image read/write.

Either way, **Data Protection keys must be persisted and shared** across tasks (EFS, or the
`AzureBlobConfig`/equivalent `StoreDataProtectionKeys` mechanism pointed at S3) — without this, a
task restart invalidates every user's auth cookie and anti-forgery token, forcing re-logins
cluster-wide.

## Networking — behind an ALB

Set `HostingConfig.UseProxy = true` with `ForwardedProtoHeaderName`/`ForwardedForHeaderName` matching
the ALB's headers (defaults `X-Forwarded-Proto`/`X-Forwarded-For` work out of the box) — without this,
nopCommerce sees every request as plain HTTP from the ALB's private IP, breaking HTTPS-relative URL
generation and any IP-based logic. Terminate TLS at the ALB; the container listens on plain HTTP
(`ASPNETCORE_URLS=http://+:80`, already set in the Dockerfile) behind it.

ALB target group health check: point at `/` (the storefront home page returns 200 once the app and DB
connection are healthy) with a generous initial `healthy_threshold`/`unhealthy_threshold` window —
first boot performs database connectivity checks and can take longer than a typical ASP.NET Core app.

## Task definition sizing & scaling

- Start with a single task, `cpu: 1024` / `memory: 2048` (Fargate) as a baseline; product-image
  processing (thumbnail generation via `libgdiplus`) is the most memory-spiky workload — profile
  before scaling down.
- Scale the **ECS service** (task count) on ALB `RequestCountPerTarget` or CPU, only after
  `DistributedCacheConfig` (Redis) is confirmed enabled — see above.
- Scheduled tasks (`IScheduleTask`, e.g. queued-email sending) run **inside every task instance** on
  its own timer; at task-count > 1 this means the same scheduled task fires redundantly across tasks
  unless the task's own logic is idempotent/leader-elected. Audit any custom `IScheduleTask` for
  this before scaling out — nopCommerce's own built-in tasks are already safe to run redundantly
  (e.g. "send next queued email" naturally de-duplicates via the queue's own state), but a
  project-specific one (see [05-domain-gastronomy-guidelines.md](05-domain-gastronomy-guidelines.md)
  for likely candidates like an expiring-batch alert) may not be.

## CI/CD sketch

1. Build/test on push (`dotnet build src/NopCommerce.sln`, `dotnet test`).
2. Build + push the Docker image to ECR, tagged by commit SHA.
3. Register a new ECS task definition revision referencing the new image tag.
4. Update the ECS service to the new task definition revision (rolling deployment;
   `minimumHealthyPercent`/`maximumPercent` sized so at least one task stays up during the roll).
5. Database migrations (FluentMigrator) run automatically at application startup
   (`InitializeDatabase()` → `migrationManager.ApplyUpMigrations(...)`) — no separate migration step
   is required in the pipeline, but be aware a rolling deployment briefly runs the **new** app version
   against a DB the **old** version is still serving from; keep schema migrations additive/backward
   compatible across a single deploy (standard expand/contract migration discipline), same
   requirement as any zero-downtime relational-DB deployment.
