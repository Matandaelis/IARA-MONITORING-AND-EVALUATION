# Containerization Progress

## Environment Detection
- [x] .NET version detection (version: net10.0, from WEB/WEB.csproj)
- [x] Linux distribution selection (distribution: alpine for app stages; SQL Server has no Alpine image - stays on mcr.microsoft.com/mssql/server:2022-latest)

## Configuration Changes
- [x] Application configuration verification for environment variable support (Program.cs reads PORT + all Settings via env; entrypoint respects ConnectionStrings__DefaultConnection)
- [x] NuGet package source configuration (none needed - public nuget.org)

## Containerization
- [x] Dockerfile creation (3 stages: node:20-alpine SPA, sdk:10.0-alpine publish, aspnet:10.0-alpine runtime)
- [x] .dockerignore file creation (**/node_modules, **/bin, **/obj, WEB.Tests, env files; lockfile + patches/ preserved)
- [x] Build stage created with SDK image
- [x] csproj file(s) copied for package restore
- [x] NuGet.config copied if applicable (not present - n/a)
- [x] Runtime stage created with runtime image
- [x] Non-root user configuration (USER $APP_UID; /app/data + /app/logs chowned)
- [x] Dependency handling (busybox wget for healthcheck; no extra system packages needed)
- [x] Health check configuration (wget /health, start-period 300s for first-boot migrations)
- [x] Special requirements implementation (SQL Server moved OUT of app container to match compose + docs topology; entrypoint.sh no longer overrides the connection string; fresh SPA overlay replaces stale committed wwwroot)

## Verification
- [x] Review containerization settings and make sure that all requirements are met
- [x] package-lock.json in sync (npm ci --dry-run passed)
- [x] dotnet publish Release /p:PublishRunWebpack=false succeeded (mirrors build stage)
- [ ] Docker build success - NOT RUN: Docker is not installed on this machine.
      Verify on a Docker host with: `docker build -t monic:latest .` then `docker compose up -d --build`
