# Tasks: LogAlto-Depth Parity Implementation

**Specification:** [spec.md](spec.md)  
**Plan:** [plan.md](plan.md)

## Workstream 1 — Migration and model foundation

- [ ] Create `LogAltoParity` EF partial context.
- [ ] Add logframe node and level entities.
- [ ] Add indicator mapping and contribution entities.
- [ ] Add disaggregated target/revision entities.
- [ ] Add data-submission and approval entities.
- [ ] Add form-share/localization/link entities.
- [ ] Add attachment/comment/notification entities.
- [ ] Add portfolio/activity/budget entities.
- [ ] Add report/dashboard entities.
- [ ] Add sync/change/audit entities.
- [ ] Add EF indexes and filtered unique constraints.
- [ ] Add forward and rollback migrations.
- [ ] Add SQL Server integration fixture coverage.

## Workstream 2 — Logframe v2

- [ ] Implement arbitrary-depth node tree service.
- [ ] Implement custom vocabulary and rank validation.
- [ ] Implement node reorder and move endpoints.
- [ ] Implement node indicator weight and primary indicator APIs.
- [ ] Implement legacy `LogFrameRow` backfill preview and commit.
- [ ] Implement legacy/v2 dual-read feature flag.
- [ ] Implement Angular logframe-v2 node CRUD.
- [ ] Add drag/drop and keyboard reordering.
- [ ] Add reconciliation and divergence report.
- [ ] Add corporate/global/organization indicator inheritance.

## Workstream 3 — Indicator ledger

- [ ] Implement form-to-indicator mapping model.
- [ ] Implement safe expression-tree validator.
- [ ] Implement question-role binding.
- [ ] Implement mapping preview for sample responses.
- [ ] Implement immutable contribution creation.
- [ ] Implement contribution deduplication and refresh.
- [ ] Project ledger rows into `Datum` transactionally.
- [ ] Implement full and scoped datum rebuild.
- [ ] Implement datum revisions.
- [ ] Implement disaggregated baselines and targets.
- [ ] Implement indicator tags and library search.
- [ ] Add ledger and formula test matrices.

## Workstream 4 — Data and reporting workflows

- [ ] Implement `DataSubmission` state machine.
- [ ] Implement configurable approval workflow entities.
- [ ] Implement active-step/action enforcement.
- [ ] Implement submit/verify/approve/reject/return endpoints.
- [ ] Implement separation-of-duty validation.
- [ ] Implement SLA and delegation behavior.
- [ ] Integrate existing `DataReview` migration adapter.
- [ ] Implement report definitions and instances.
- [ ] Implement report section snapshots.
- [ ] Implement report approval and export workflows.
- [ ] Implement notifications for every decision.

## Workstream 5 — Surveys and forms

- [ ] Add rich question types and validation fields.
- [ ] Append enum values without renumbering existing values.
- [ ] Implement typed answer projections.
- [ ] Implement public/private/confidential form policies.
- [ ] Implement form sharing principals and access levels.
- [ ] Implement form/record links.
- [ ] Implement localisation tables and Angular locale selection.
- [ ] Implement Excel/CSV import preview and commit.
- [ ] Add Kobo/ODK mapping profiles.
- [ ] Add signed webhook registration and replay protection.
- [ ] Implement response review and revision state.

## Workstream 6 — Attachments and collaboration

- [ ] Implement polymorphic attachments.
- [ ] Implement presigned upload and storage verification.
- [ ] Implement Azure Blob and S3/MinIO providers.
- [ ] Implement attachment tags and confidentiality.
- [ ] Implement threaded comments.
- [ ] Implement reactions and mention extraction.
- [ ] Implement persisted notifications.
- [ ] Implement read/unread deep-link behavior.
- [ ] Implement notification preferences.

## Workstream 7 — Offline and external sync

- [ ] Implement device registration and revocation.
- [ ] Implement client operation idempotency.
- [ ] Implement change log and delta API.
- [ ] Implement conflict detection and staging.
- [ ] Implement conflict resolution UI.
- [ ] Implement signed external API credentials.
- [ ] Remove silent first-record fallback behavior.
- [ ] Reject unknown entity/date context instead of guessing.
- [ ] Implement queue, retry, and partial sync responses.
- [ ] Implement Angular IndexedDB and service worker staging.

## Workstream 8 — Portfolio and analytics

- [ ] Implement activity/subactivity hierarchy.
- [ ] Implement Gantt data and UI.
- [ ] Implement implementation-rate calculation.
- [ ] Implement project favourites.
- [ ] Implement configurable custom fields.
- [ ] Implement budgets, expenditures, and variance.
- [ ] Implement dashboard and widget models.
- [ ] Implement global/project/user dashboard APIs.
- [ ] Implement pivot table and map widgets.
- [ ] Implement virtualized logframe and data grids.
- [ ] Implement Power BI integration contract.
- [ ] Implement Excel, PDF, CSV, PNG exports.

## Workstream 9 — Enterprise operations

- [ ] Add Redis/hybrid cache adapters.
- [ ] Add import/calculation/export background worker jobs.
- [ ] Add worker retry/dead-letter handling.
- [ ] Add blob and data-protection key storage providers.
- [ ] Add PII inventory and consent records.
- [ ] Add retention and anonymisation jobs.
- [ ] Add GDPR export/delete tooling.
- [ ] Add OpenTelemetry and structured logs.
- [ ] Add backup/restore automation.
- [ ] Add disaster recovery documentation.
- [ ] Add SQL Server and PostgreSQL migration assessment.
- [ ] Add tenant readiness assessment and policy isolation package.

## Workstream 10 — Verification and release

- [ ] Unit tests for formula mappings and expression trees.
- [ ] Unit tests for logframe hierarchy and cycles.
- [ ] Unit tests for workflow transitions.
- [ ] SQL Server integration tests for ledgers and rebuild.
- [ ] API tests for authorization and confidentiality.
- [ ] Angular tests for routing, form rendering, and sync state.
- [ ] End-to-end test: plan, collect, verify, approve, calculate, report.
- [ ] Migration parity fixture for legacy and v2 logframes.
- [ ] Migration rollback rehearsal.
- [ ] Performance fixture for 1,000 projects and 100,000 values.
- [ ] Load test for imports, sync, report, and dashboards.
- [ ] Security review and credential rotation test.
- [ ] GDPR deletion/anonymisation acceptance test.
- [ ] Production feature-flag rollout and rollback plan.
