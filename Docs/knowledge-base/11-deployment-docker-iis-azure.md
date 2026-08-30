# Deployment Reference (Docker / IIS / Azure — general patterns)

Source: adapted from `developer/tutorials/docker.html`, `developer/tutorials/azure-deploy.html`,
`developer/tutorials/azure-publish.html`. Cloud-specific detail for **this** project's target
(AWS ECS) is in [04-deployment-aws-ecs.md](../ai-harness/04-deployment-aws-ecs.md) — this file covers
the underlying, provider-agnostic mechanics that the docs actually describe.

## The repo's real Dockerfile (verified, root `Dockerfile`)

Multi-stage build, already solves the classic "plugins aren't built" problem by building the **whole
solution** before publishing just the web project:

```dockerfile
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY ./src ./
RUN dotnet build NopCommerce.sln --no-incremental -c Release
WORKDIR /src/Presentation/Nop.Web
RUN dotnet publish Nop.Web.csproj -c Release -o /app/published
# ... chmod required writable dirs: App_Data, App_Data/DataProtectionKeys, bin, logs, Plugins,
#     wwwroot/bundles, wwwroot/db_backups, wwwroot/files/exportimport, wwwroot/icons,
#     wwwroot/images(+thumbs+uploaded), wwwroot/sitemaps

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
RUN apk add --no-cache icu-libs icu-data-full
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
# tiff + libgdiplus for image processing (thumbnails), tzdata, gcompat
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
ENTRYPOINT ["dotnet", "Nop.Web.dll"]
```

Key lessons this bakes in:
1. **Build the full `.sln`, not just `Nop.Web.csproj`** — a `dotnet publish` targeting only `Nop.Web`
   would silently skip every plugin project reference. This is the exact problem the old Azure
   Kudu-script doc works around manually with per-plugin `dotnet build` calls; the Dockerfile
   sidesteps it entirely by building the solution first.
2. **Alpine + globalization**: `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false` + `icu-libs`/`icu-data-full`
   are required for correct currency/locale formatting on Alpine — omitting them causes runtime
   culture-related exceptions, not a build failure, so this is easy to miss until multi-currency or
   non-`en-US` locale features are exercised.
3. **`libgdiplus`/`tiff`** are required for the built-in image-resizing pipeline (product thumbnails)
   to work at all on Linux — a missing dependency here manifests as broken thumbnail generation, not
   a startup crash.
4. Writable-directory `chmod` list is exhaustive and load-bearing — a container running read-only
   root filesystem (a common ECS/Fargate hardening choice) must mount volumes at (at least) `App_Data`,
   `wwwroot/images`, `wwwroot/bundles`, `logs`, and `Plugins` or installation/upload features fail.

## Local docker-compose (root, SQL-Server-oriented) vs. Postgres variant

Root `docker-compose.yml` pairs `nopcommerce_web` with a plain `postgres:latest` service (there's also
`mysql-docker-compose.yml` for MySQL). See
[03-database-postgres.md](../ai-harness/03-database-postgres.md) for the full Postgres story; the
takeaway from the deployment docs specifically is that switching database engine is a
**`DataConfig.DataProvider` value + connection-string change**, never a code change.

## IIS hosting (if ever needed outside containers)

ASP.NET Core apps on IIS run behind the `AspNetCoreModuleV2` reverse-proxy module, forwarding to a
Kestrel process (`dotnet.exe Nop.Web.dll`) — IIS itself never executes managed code directly. Grant
`IIS_IUSRS` (or the configured app pool identity) Read & Execute on `wwwroot`. Enable
`stdoutLogEnabled="true"` in `web.config` when diagnosing a "the process failed to start" error — it's
the only way to see the actual .NET startup exception through IIS.

## Legacy Azure VM / Kudu notes (context only — not applicable to this project's AWS target)

The documented Azure VM path (manual IIS + WebDeploy + hand-edited `deploy.cmd` inserting
`dotnet build` calls per plugin project between Kudu's default restore/publish steps) predates the
container-first Dockerfile in this repo and should **not** be reproduced for new work — it's included
here only so an AI assistant recognizes it as legacy/irrelevant if it surfaces in older
StackOverflow/forum answers, rather than mistakenly porting it into an ECS pipeline.
