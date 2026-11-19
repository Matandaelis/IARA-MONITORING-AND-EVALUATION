# Specification: LogAlto-Depth Parity and Enterprise M&E Suite

**Status:** Proposed  
**Source:** [LOGALTO_PARITY_IMPLEMENTATION_GUIDE.md](../../docs/LOGALTO_PARITY_IMPLEMENTATION_GUIDE.md)  
**Target stack:** ASP.NET Core 10, EF Core 10, SQL Server 2022+, Angular 19, PrimeNG 19  
**Product scope:** MONIC/IARA

## 1. Purpose

Implement the operational, analytical, governance, reporting, and integration capabilities described by the LogAlto parity guide. The result must extend the existing MONIC/IARA domain rather than replacing `Datum`, `Indicator`, `Token`, `Response`, `LogFrameRow`, `Calculation`, or `DataReview`.

## 2. Non-negotiable design constraints

1. **Do not fork the monitoring data model.** `Datum` remains the time-series fact table. Form submissions produce contribution rows that project deterministically into `Datum` values.
2. **Do not fork the logframe.** Add the v2 `LogFrameNode` hierarchy alongside `LogFrameRow`. Backfill, dual-read, feature-flag writes, and migrate only after parity testing.
3. **Ledger before projection.** `IndicatorContribution` records immutable form-to-indicator contributions. `Datum` is a rebuildable projection of that ledger.
4. **Workflow by aggregate.** Approval attaches to `DataSubmission`, `ReportInstance`, and other aggregate entities, not to a composite data key.
5. **Relational facts, JSON configuration.** Store facts and audit records relationally. Store expression trees, layouts, filters, custom fields, and report section configuration as validated JSON.
6. **Single tenant until funded.** Do not add tenant IDs to every table before an enterprise tenancy requirement is accepted.
7. **Incremental Angular migration.** Preserve NgModules and existing routes. New screens may be standalone components when integrated through `SharedModule`.

## 3. Functional scope

### 3.1 Project portfolio

The platform shall support:

- project codes, organisation/unit ownership, lifecycle status, dates, manager, description, and currency;
- configurable project, activity, indicator, and logframe fields;
- project favourites and configurable portfolio grids;
- project and global dashboards;
- activities and sub-activities;
- planned and actual dates;
- task implementation rates derived from milestones, indicators, or sub-tasks;
- Gantt views;
- budget categories, budget lines, expenditures, and variance;
- project permissions and visible project lists.

### 3.2 Arbitrary logframe hierarchy

The platform shall support:

- configurable logframe levels and organisation-level vocabulary;
- goals, outcomes, outputs, impacts, or organization-specific names at any number of levels;
- arbitrary-depth parent/child nodes;
- drag/drop reordering without renumbering all records;
- node-level required fields;
- per-node indicator links, weights, and primary indicators;
- project, organisation-unit, and global/corporate logframes;
- inherited/copyable corporate indicators;
- backfill from legacy `LogFrameRow` records.

### 3.3 Indicator library and calculation

The platform shall support:

- quantitative, qualitative, numerator/denominator, and percentage indicators;
- indicator definitions, collection methods, risks, responsible users, and tags;
- baselines and targets at total and disaggregation levels;
- value, baseline, and target disaggregation;
- indicator formulas and linked source indicators;
- formula-to-form mappings;
- question roles for value, numerator, denominator, disaggregation, filter, date, and entity;
- sum, count, distinct-count, average, minimum, maximum, and most-recent mappings;
- immutable, replayable indicator contributions;
- append-only value revisions and source traceability.

### 3.4 Surveys and field data

The platform shall support:

- rich question types: text, multiline, number, date, yes/no, document, geopoint, location, project, activity, user, signature, and option lists;
- min/max values, date ranges, maximum text length, validation regex, required fields, and confidentiality;
- drag/drop survey construction;
- conditional and skip logic, including cycle detection;
- public and private forms;
- form translations and system localisation;
- form-to-form and record-to-record links;
- Excel/CSV import and validation preview;
- typed answer projections for analytical queries;
- public survey links with expiry and access policies;
- offline device registration, idempotent sync, delta change feed, and conflict staging;
- response review and submission state.

### 3.5 Files, comments, and collaboration

The platform shall support:

- attachments linked polymorphically to projects, activities, responses, indicators, reports, and records;
- legacy document links and external URLs;
- blob/S3/MinIO storage selection;
- SHA-256 integrity metadata;
- attachment tags and confidential attachments;
- comments on projects, activities, responses, indicators, reports, and logframe nodes;
- threaded comments and reactions;
- @mentions and persisted notifications;
- unread notification counts, deep links, and in-app realisation;
- notification delivery preferences.

### 3.6 Analytics and dashboards

The platform shall support:

- global, project, organisation, and personal dashboards;
- table, pivot, chart, map, KPI, indicator, and report widgets;
- aggregation by organisation, location, period, disaggregation, project, and indicator;
- configurable filters and target lines;
- PrimeNG/Angular, ECharts, D3, and existing chart infrastructure;
- Excel and image export;
- chart, dashboard, and report configuration as validated JSON;
- Power BI export or embedding through a supported integration contract;
- offline-safe and virtualised high-volume data grids.

### 3.7 Governance and access control

The platform shall support:

- profiles plus fixed and custom permission matrices;
- user, profile, organisation-unit, and project-based form shares;
- guest, collaborator, data collector, reviewer, and administrator form access levels;
- confidential-field access;
- project, indicator, entity, form, and report policies;
- configurable approval workflows and workflow steps;
- submit, verify, approve, reject, return, delegate, and cancel decisions;
- separation of duties;
- SLA and delegation metadata;
- append-only audit and change logs;
- PII/deletion/consent records and retention policies;
- GDPR export and anonymisation tools.

### 3.8 Reporting documents

The platform shall support:

- report definitions for planning and reporting;
- project, organisation-unit, and global scope;
- monthly, quarterly, and annual periods;
- indicator grid, activity, budget, narrative, attachments, and custom sections;
- report instances for stable period and scope keys;
- approval workflow attachment;
- report snapshots for reproducibility;
- Excel and PDF exports.

### 3.9 Infrastructure and integrations

The platform shall support:

- OpenIddict/OIDC authentication;
- REST/OpenAPI integration;
- external API credentials with scopes and expiry;
- signed Kobo/ODK webhooks and replay protection;
- Redis or hybrid cache for formula/catalog data;
- background workers for imports, calculations, reports, and sync;
- blobs for documents and data-protection keys;
- health and readiness endpoints;
- Docker/Coolify/Azure deployment;
- PostgreSQL migration assessment, though SQL Server remains the initial engine;
- backup, restore, retention, and disaster recovery procedures.

## 4. Acceptance criteria

1. Existing MONIC rows continue to work through legacy entities during migration.
2. Every new implementation can be enabled independently with feature flags.
3. Logframe writes may switch from legacy rows to v2 nodes only after a dual-read parity test passes.
4. A submitted response creates exactly one immutable contribution set for a mapping; replaying the submission produces no duplicate contribution.
5. Rebuilding `Datum` from the ledger produces the same result as projection in the tested reporting period.
6. Calculated indicators are executed topologically, and cycles and self-references are rejected.
7. Form answers preserve values and typed projections consistently, including disaggregation options.
8. Offline submissions are deduplicated by `ClientGeneratedId` and device context.
9. Confidential values are never returned to users without the required form, project, or field policy.
10. Approval instances cannot be acted on by a user lacking the required permission or by the previous approver when prohibited.
11. Reports can be recreated from a stored snapshot after source values are edited.
12. The Docker deployment passes health checks and restores persisted keys and database state.
13. xUnit tests cover calculation, ledger, import, sync, permissions, workflow, disaggregation, reporting, and GDPR behavior.

## 5. Explicit exclusions

- Copying proprietary LogAlto schemas or APIs.
- Replacing existing entities before a verified migration.
- Tenant implementation until tenancy is funded and specified.
- Client-side formula evaluation for authoritative values.
- Silently resolving missing entity or reporting-period context.
- AI-produced reviews as final results without human approval.

## 6. Traceability

The implementation shall maintain a mapping between:

- guide capability;
- requirement/story;
- entity/controller/service;
- frontend route/component;
- migration;
- API contract;
- test;
- rollout and rollback procedure.
