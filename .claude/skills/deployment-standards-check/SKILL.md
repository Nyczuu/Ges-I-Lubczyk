---
name: deployment-standards-check
description: >-
  Load this when a change touches the Dockerfile, appsettings, environment configuration, anything
  written to the container filesystem, or an ECS task/service definition. Use it BEFORE writing the
  change: the two failure modes here — plugins silently missing from the image, and state written to
  ephemeral container storage — both look completely healthy locally and only appear in production.
---

# Deployment Standards Check

Full docs: [`Docs/ai-harness/04-deployment-aws-ecs.md`](../../../Docs/ai-harness/04-deployment-aws-ecs.md)
and [`Docs/knowledge-base/11-deployment-docker-iis-azure.md`](../../../Docs/knowledge-base/11-deployment-docker-iis-azure.md).
This is the checklist form.

## The image

- [ ] The `Dockerfile` **builds the whole `NopCommerce.sln`** before publishing `Nop.Web.csproj`.
      Changing the publish step to target `Nop.Web.csproj` alone silently drops every plugin from the
      image — the app boots fine and the features are simply gone. Rule 6; do not regress it.
- [ ] The Alpine `icu-libs`/`icu-data-full` and `libgdiplus`/`tiff` layers stay. They back
      locale-correct currency formatting and thumbnail generation — they are dependencies, not hardening.
- [ ] Images are tagged by commit SHA, not only `latest`, so a rollback is a task-definition revision
      change rather than a re-pull of a mutable tag.

## Configuration

- [ ] No connection string, credential, or secret baked into the image or into a committed
      `appsettings.*.json`. Everything comes from env vars in the ECS task definition — non-secret in
      `environment`, secret in `secrets` (Secrets Manager / SSM).
- [ ] Env var names follow the `Section__Key` override form (`DataConfig__DataProvider`,
      `HostingConfig__UseProxy`).
- [ ] `DataConfig__DataProvider` is `PostgreSQL`; `WithNoLock` stays `false`.

## Behind the load balancer

- [ ] `HostingConfig.UseProxy = true` with matching forwarded-header names. Without it nopCommerce sees
      every request as plain HTTP from the ALB's private IP, breaking HTTPS URL generation and any
      IP-based logic.
- [ ] TLS terminates at the ALB; the container serves plain HTTP.
- [ ] Health check tolerant of a slow first boot — startup performs database work.

## Running more than one task

- [ ] **`DistributedCacheConfig.Enabled = true` pointed at Redis before task count goes above one.**
      With the in-process cache, tasks diverge on cached catalogue and settings data. Correctness, not
      performance — see `caching-standards-check`.
- [ ] Every custom `IScheduleTask` is idempotent: the scheduler runs inside *every* task instance, so a
      task fires redundantly once you scale out. Built-in tasks are already safe; a project-specific one
      is not until you check it (see `event-consumer-standards-check`).
- [ ] Data Protection keys are persisted and shared across tasks. Without that, a task restart
      invalidates every auth cookie and antiforgery token cluster-wide.

## Anything written to disk

Container storage is ephemeral and not shared. Uploaded product images, exports, and bundles under
`wwwroot/` and `App_Data/` do not survive a restart and do not exist on the sibling task.

- [ ] New code does not write persistent state to the container filesystem. If it must, it goes through
      external storage (an S3 media provider plugin following the shape of
      `src/Plugins/Nop.Plugin.Misc.AzureBlob`) or an EFS-mounted path already in the task definition.

## Migrations during a rolling deploy

FluentMigrator migrations run **at application startup**, so a rolling deployment briefly has the new
version's migrations applied while old-version tasks are still serving traffic.

- [ ] Schema changes are additive and backward compatible across a single deploy — expand now, contract
      in a later release. A migration that drops or renames a column the still-running old version reads
      takes the site down mid-deploy.
- [ ] No separate migration step is added to the pipeline; startup already handles it.

## Before calling deployment work done

- [ ] Full-solution build preserved in the Dockerfile; plugins present in the built image.
- [ ] No secret in the repo or the image.
- [ ] Multi-task prerequisites (Redis, shared Data Protection keys, idempotent scheduled tasks) hold, or
      the service is explicitly single-task.
- [ ] Schema change is deploy-safe under expand/contract.
