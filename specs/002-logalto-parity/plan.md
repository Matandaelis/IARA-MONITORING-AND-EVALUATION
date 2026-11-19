# Implementation Plan: LogAlto-Depth M&E Parity

**Date:** 2026-11-19  
**Branch:** master  
**Specification:** [spec.md](spec.md)

## Summary

Deliver parity in controlled phases without rewriting the existing platform. New capabilities use additive EF Core entities, dual-read/migration feature flags, reusable policy services, and background processors. Existing calculation, logframe, questionnaire, and value classes remain the source primitives wherever possible.

## Technical context

- Backend: ASP.NET Core 10 / C# 13
- ORM: EF Core 10
- Database: SQL Server 2022+
- Frontend: Angular 19, NgModule-first
- UI: PrimeNG 19, Bootstrap 5.3
- Tests: xUnit, EF in-memory, SQL Server integration fixtures
- Files: Azure Blob, S3/MinIO-compatible storage
- Sync: SQL change log plus API operations
- Cache: EF formula cache, in-memory cache, then Redis/hybrid cache
- Jobs: hosted services first; dedicated queues later

## Implementation phases

### Phase A — Schema and migration foundation

Add entities and migrations for:

- logframe v2 levels/nodes/links;
- indicator tags and disaggregated targets;
- data submissions/items and audit revisions;
- form mappings/contributions;
- sharing, localisation, attachments, comments, notifications;
- activities, budgets, expenditures, favourites;
- dashboards, reports, and workflow;
- devices, sync operations, change logs, and audit logs.

Deliverables:

- EF model configuration;
- migration scripts;
- backfill jobs;
- dual-read feature flags;
- seed data and sample projects;
- migration rollback procedure.

### Phase B — Logframe and indicator core

Implement:

- custom levels and arbitrary-depth hierarchy;
- drag/drop reorder API;
- corporate indicator inheritance;
- disaggregated targets and baselines;
- indicator tag library;
- form-to-indicator mappings;
- immutable contributions;
- deterministic datum projection;
- ledger replay and reconciliation;
- revision history.

Acceptance: legacy and v2 logframes agree across reporting periods and roll-ups.

### Phase C — Survey and field operation

Implement:

- rich question types;
- validation and confidential fields;
- translations;
- form/record links;
- form sharing;
- Excel/CSV imports;
- typed answer values;
- public and expiry-controlled form access;
- Kobo/ODK adapters;
- signed webhook reception;
- offline device, sync operation, and change log APIs.

Acceptance: a submission can be replayed, imported, synced, and deduplicated without changing final values.

### Phase D — Governance and reporting

Implement:

- configurable workflows;
- submit/verify/approve/reject/return behavior;
- project, entity, indicator, form, and report authorization;
- report definitions and instances;
- report section snapshots;
- PDF/Excel exports;
- comments and notifications;
- high/critical feedback auto-actions.

Acceptance: every approved aggregate has an approval trail and reproducible report snapshot.

### Phase E — Analytics and enterprise data products

Implement:

- dashboard entities and widget APIs;
- portfolio/global/project dashboards;
- pivots and maps;
- virtualized data grids;
- Power BI integration;
- reporting snapshot tables;
- data coverage and timeliness metrics;
- export/download job tracking.

Acceptance: enterprise dashboards remain usable for large matrices and reporting-period closes.

### Phase F — Infrastructure, enterprise readiness, and operations

Implement:

- Redis/hybrid cache;
- background queue and worker dashboards;
- S3/MinIO and Azure Blob support;
- integration credentials and scopes;
- PII retention/anonymisation;
- backup and restore runbooks;
- observability and release metrics;
- deployment and rollback automation;
- multi-tenant assessment.

Acceptance: operators can recover, scale, observe, audit, and rotate credentials without application restarts.

## Suggested delivery order

1. `LogFrameNode`, levels, backfill, and parity tests.
2. `IndicatorContribution`, mapping, replay, and disaggregated targets.
3. `DataSubmission` and configurable approval workflow.
4. Forms, sharing, localised text, attachments, comments, and notifications.
5. Offline sync and external field-tool ingestion.
6. Report documents and dashboard widgets.
7. Enterprise caching, jobs, integrations, privacy tooling, and tenancy readiness.

## Architecture boundaries

### Existing areas to extend

- `WEB/Models/Indicator.cs`
- `WEB/Models/Question.cs`
- `WEB/Models/Answer.cs`
- `WEB/Models/Response.cs`
- `WEB/Models/Project.cs`
- `WEB/Models/ApplicationDbContext*.cs`
- `WEB/Code/Calculation.cs`
- `WEB/Controllers/*`
- `WEB/Services/*`
- `WEB/ClientApp/src/app/*`
- `WEB.Tests/*`

### New service areas

- `LogFrameV2Service`
- `FormIndicatorContributionEngine`
- `DataSubmissionService`
- `ApprovalWorkflowService`
- `ReportSnapshotService`
- `AttachmentStorageService`
- `CommentNotificationService`
- `OfflineSyncService`
- `AuditProjectionService`
- `IntegrationCredentialService`

## Migration strategy

1. Migrations add only nullable/additive fields and tables.
2. Backfill legacy logframes and relationships in idempotent jobs.
3. Read v2 values behind a feature flag; compare against legacy values.
4. Enable v2 writes for pilot organisations/projects.
5. Pause v2 writes while rollbacks are verified.
6. Migrate remaining customers and remove legacy UI only after reconciliation.

## Test strategy

- unit: value expressions, topological formulas, hierarchy, state transitions;
- model: constraints, cascade behavior, unique indexes, row versions;
- integration: SQL Server stored procedures and EF transactions;
- API: authorization, consent/confidentiality, validation, idempotency;
- frontend: routing, form rendering, drag/drop, offline queue;
- end-to-end: plan → collect → approve → calculate → report;
- migration: legacy/v2 parity, replay, rollback;
- performance: large logframes, imports, sync, report generation, dashboards.

## Rollout controls

Recommended flags:

- `Features:LogFrameV2Write`
- `Features:IndicatorContributions`
- `Features:OfflineSync`
- `Features:DataSubmissionWorkflow`
- `Features:ReportInstances`
- `Features:DashboardV2`
- `Features:ExternalPushV2`

## Definition of done

A capability is not complete until it includes:

- data model;
- migration/backfill;
- API;
- authorization;
- UI;
- audit/provenance;
- tests;
- observability;
- documentation;
- rollback behavior;
- acceptance sign-off.
