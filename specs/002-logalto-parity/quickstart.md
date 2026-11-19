# Quickstart: Developing LogAlto-Depth Parity Features

**Specification:** [spec.md](spec.md)

## Prerequisites

- .NET 10 SDK;
- SQL Server 2022 or integration fixture;
- Node.js 20+
- Angular CLI compatible with Angular 19;
- Docker and Docker Compose;
- access to local/test OpenIddict configuration;
- optional Azure Blob or S3/MinIO-compatible test storage.

## Backend workflow

```bash
cd WEB
dotnet restore
 dotnet ef migrations add AddLogAltoParityBaseline
 dotnet ef database update
 dotnet run
```

Use the repository's existing startup database initialization carefully. Do not enable startup backfills before testing against a copy of production data.

## Frontend workflow

```bash
cd WEB/ClientApp
npm ci
npm run build
npm start
```

For a fast Angular-only pilot:

```bash
cd WEB/ClientApp
npm start
```

The ASP.NET SPA proxy remains the supported integration path. Do not create a separate Angular host for production parity features.

## Docker workflow

```bash
cp .env.example .env
docker compose up -d --build
```

Verify:

```text
http://localhost:8080/health
http://localhost:8080/openapi/v1.json
```

## Test workflow

```bash
dotnet test WEB.Tests/WEB.Tests.csproj
```

Add focused test runs for migrations and integration fixtures:

```bash
dotnet test WEB.Tests/WEB.Tests.csproj --filter "FullyQualifiedName~LogFrame"
dotnet test WEB.Tests/WEB.Tests.csproj --filter "FullyQualifiedName~IndicatorContribution"
dotnet test WEB.Tests/WEB.Tests.csproj --filter "FullyQualifiedName~OfflineSync"
```

## Pilot implementation sequence

1. Add and migrate a single new entity set.
2. Add a unit test and SQL integration test.
3. Add API endpoints and authorization policies.
4. Add Angular route/component behind a feature flag.
5. Backfill legacy data in preview mode.
6. Compare legacy and v2 results.
7. Enable writes for a test project.
8. Add monitoring and rollback.
9. Expand rollout.

## Example development flags

Add configuration names only when implemented:

```text
Features:LogFrameV2Write=true
Features:IndicatorContributions=true
Features:DataSubmissionWorkflow=true
Features:OfflineSync=true
Features:ReportInstances=true
Features:DashboardV2=true
```

## Required environment settings

Use existing settings for:

- database connection;
- root URL;
- OpenIddict certificates/data protection;
- SMTP;
- Azure infrastructure;
- external API credentials;
- storage providers;
- cache provider.

Do not place API secrets or signing keys in source files.

## Validation checklist

Before exposing a parity feature:

- migrate a copy of representative data;
- verify legacy and v2 reads;
- verify authorization with a non-admin account;
- verify confidential fields are omitted;
- verify replay/idempotency;
- verify stale row-version conflicts;
- verify generated audit records;
- verify report snapshot stability;
- verify health checks and worker retry;
- rehearse rollback and data restore.
