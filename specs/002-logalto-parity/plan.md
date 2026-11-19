# Implementation Plan: Enterprise M&E Domain Integrity and Suite Delivery

**Date:** 2026-11-19  
**Status:** Proposed  
**Specification:** [spec.md](spec.md)  
**Related tasks:** [tasks.md](tasks.md)  
**Source implementation guide:** [LOGALTO_PARITY_IMPLEMENTATION_GUIDE.md](../../docs/LOGALTO_PARITY_IMPLEMENTATION_GUIDE.md)

## Executive direction

Build the enterprise suite by first correcting the domain semantics that can make results unauditable or wrong. Prioritize authority, provenance, periodization, authorization, idempotency, and reproducibility before expanding dashboards, charts, AI review, or integrations.

The delivery must extend the existing ASP.NET Core 10 / EF Core 10 / SQL Server / Angular 19 application. Keep `Datum`, `Indicator`, `Response`, `Answer`, `Calculation`, and legacy `LogFrameRow` compatible while introducing additive models behind feature flags. Do not silently guess reporting context or claim a capability is complete until its API, UI, permissions, audit trail, tests, monitoring, and rollback path exist.

## Domain rules to establish first

1. **Every measurement has provenance.** Capture source system, source record, collector/actor, collection time, ingestion time, reporting period, transformation/mapping version, and review state.
2. **Context is explicit.** An unresolved project, entity, indicator, or period is rejected or staged for correction; never default to the first/latest record.
3. **Collection, verification, approval, and publication are distinct.** A received submission is not approved data. A submitted survey is not necessarily a verified indicator value.
4. **Facts are immutable; projections are rebuildable.** Preserve input answers and accepted contributions, and derive `Datum`/reporting views deterministically from the ledger.
5. **Reporting periods close against snapshots.** Approved reports must retain a reproducible snapshot and must not change when live source data changes.
6. **Disaggregation reconciles.** Totals, categories, baselines, targets, and numerator/denominator values must follow explicit reconciliation rules.
7. **Authorization is policy-based and deny-by-default.** Project, organisation, form, indicator, report, attachment, and confidential-field access must use shared policy services.
8. **Audits are append-only and attributable.** Record actor, source integration/device, action, before/after state, reason, time, and correlation ID.
9. **External and offline writes are idempotent.** Replays must not duplicate records, contributions, notifications, or approvals.
10. **Automated findings support accountable human decisions.** Review rules and evidence are versioned; any override has a reason and named approver.

## Delivery strategy and gates

### Phase 0 — Domain audit, baseline, and safety fixes

**Objective:** establish verified current behavior and immediately eliminate unsafe data handling.

**Work:**

- Inventory endpoints and services that create or change `Datum`, `Response`, `Answer`, `DataReview`, budget, review, and logframe data.
- Trace API callers and imports to their data-write paths; identify duplicate or bypass pipelines.
- Map current authority states and enums to the target lifecycle; document legacy compatibility behavior.
- Remove or disable silent entity/date fallbacks in `ExternalPushController` and offline ingestion. Return actionable validation errors or stage the record for manual resolution.
- Ensure public API keys are not accepted from query strings; define a deprecation window and migration to signed/scoped integrations.
- Add correlation IDs, source IDs, actor/device IDs, and explicit rejection reasons to ingestion logs.
- Add tests reproducing wrong entity/date assignment, duplicate sync, cross-project access, and post-approval mutation.
- Publish a verified feature matrix: implemented, partial, missing, and not yet independently verified.

**Exit gate:** no ingestion path silently guesses the measurement entity or period; high-risk authorization and validation behaviors have regression tests; domain write paths are documented.

### Phase 1 — Canonical provenance and ingestion boundary

**Objective:** make every source enter through a consistent validated and auditable pipeline.

**Deliverables:**

- Add an ingestion/submission aggregate with source kind, integration/device, external ID, project/entity/period, received time, status, idempotency key, validation result, and mapping version.
- Define stable lifecycle semantics, for example `Received → Staged → Validated → Submitted → Verified → Approved → Published`, plus `Rejected`, `Returned`, `Cancelled`, and `Duplicate` outcomes. Document allowed transitions and actors.
- Build a shared ingestion service used by manual entry, CSV/XLSX, Kobo/ODK, external pushes, and offline sync.
- Implement preview/validate/commit semantics, per-row error reporting, bounded transactions, retry safety, and rollback of uncommitted imports.
- Enforce source-specific mapping profiles and strict resolution of questionnaire, question, project, entity, and reporting period.
- Persist an append-only provenance/audit record for accepted, changed, rejected, and reviewed data.
- Add source metadata and versioned mappings to import jobs and responses.
- Add bounded paging and an explicit correction queue for unresolved submissions.

**Exit gate:** same source payload produces the same validated outcome on replay; every accepted value can be traced to source and mapping version; unresolved context never reaches `Datum`.

### Phase 2 — Canonical indicator ledger and measurement semantics

**Objective:** unify form-derived, imported, and manually entered indicator results without breaking `Datum` consumers.

**Deliverables:**

- Define and migrate `IndicatorContribution` as an immutable record with indicator, entity, period, disaggregation, value/numerator/denominator, source submission, mapping version, and computation time.
- Add safe, typed expression trees and validation; prohibit arbitrary code or SQL execution.
- Implement form/question-to-indicator mappings with preview against sample records.
- Use one deterministic contribution processor for submitted responses and batch imports.
- Make projection into `Datum` transactionally replaceable for a defined series/period scope; record `DatumRevision` and reconciliation metadata.
- Define how manual adjustments work: adjustment record, reason, actor, approval, and relationship to source values. Do not overwrite contributions invisibly.
- Add explicit rules for count, sum, average, distinct count, numerator/denominator, missing values, zero denominators, late data, corrections, and duplicates.
- Implement disaggregated baseline/target APIs and total reconciliation policy; quarantine inconsistent totals rather than silently balancing them.
- Preserve current formula dependency topological execution and cycle detection in `Calculation.cs`; add integration tests for SQL stored procedures.
- Provide ledger replay, scoped rebuild, and reconciliation reports.

**Exit gate:** ledger replay reproduces `Datum` for tested scopes; duplicate and corrected submissions are deterministic; totals and disaggregations reconcile or are visibly held for review.

### Phase 3 — Approval workflows and period close

**Objective:** make the result lifecycle and separation of duties consistent across indicator data, surveys, and reports.

**Deliverables:**

- Introduce `DataSubmission` as approval aggregate for indicator value batches rather than attaching workflow to `Datum` composite keys.
- Implement configurable workflow definition, versioned steps, instance, actions, delegation, SLA, due dates, and notifications.
- Enforce active-step authorization, separation of duties, row-version concurrency, idempotent actions, and append-only action history.
- Adapt existing `DataReview` behavior through a compatibility adapter and migration report; preserve legacy history.
- Define whether survey response approval is separate from contribution approval, and enforce the decision consistently.
- Implement reporting-period close: freeze approved scope, record close actor/time, ledger watermark and snapshot references, and a controlled reopen/correction process.
- Ensure late or corrected data creates a new review/revision rather than mutating an already published report silently.
- Define status translation for existing `Response.Submitted`, computed flags, and legacy review states.

**Exit gate:** unauthorized or repeated transitions fail; approved periods and reports remain stable; reopening produces a traceable new version.

### Phase 4 — Central authorization and privacy

**Objective:** prevent access control from varying across controllers and exports.

**Deliverables:**

- Implement shared authorization services/policies for organisation, project, entity, indicator, questionnaire, response, report, attachment, and export scope.
- Enforce checks at query and serialization boundaries, including bulk exports, notifications, files, dashboards, and background jobs.
- Add explicit form sharing and confidential field policies; filter protected answers before DTO creation.
- Remove inconsistent controller-level role-only decisions in favor of policy checks while retaining compatibility tests.
- Add signed, scoped, rotatable integration credentials with identity, tenant/project scope, expiry, revocation, and audit history.
- Secure Kobo/ODK webhooks with HMAC signature and timestamp/replay checks; do not accept secrets in query strings.
- Add PII inventory, consent capture where required, retention policy, export, anonymization, and deletion lineage.
- Ensure audit logs never include secrets or raw sensitive values unnecessarily.

**Exit gate:** permission matrix tests prove deny-by-default and no cross-project leakage for APIs, search, attachments, reports, and exports; credential rotation/revocation is tested.

### Phase 5 — Offline and external field operations

**Objective:** deliver reliable field collection across intermittent connectivity.

**Deliverables:**

- Add device registration, user binding, revocation, app version, and last-seen telemetry.
- Implement IndexedDB local staging and service worker application shell; protect local sensitive data and define device logout/wipe behavior.
- Add client-generated operation IDs, schema versions, resumable batches, attachment queues, bounded retries, and partial success responses.
- Add delta change feed with durable cursor and retention/expiry policy.
- Implement conflict detection using row versions and explicit conflict staging/resolution; do not use silent last-write-wins.
- Validate that offline submissions retain client timestamps as supplied metadata while server receive time remains separately authoritative.
- Add Kobo/ODK mapping profiles, webhook signing, duplicate detection, and transparent ingest status.
- Provide operational screens for sync status, rejected rows, conflicts, and retry.

**Exit gate:** device replay, disconnect/reconnect, partial upload, duplicate delivery, revocation, and conflict tests pass without misattribution or data loss.

### Phase 6 — Logframe and M&E planning parity

**Objective:** converge planning semantics while maintaining legacy compatibility.

**Deliverables:**

- Add custom levels, arbitrary-depth `LogFrameNode`, indicators, weights, corporate templates, and required fields.
- Implement resumable/idempotent backfill from `LogFrameRow`, preserving legacy IDs and reporting ambiguous mappings.
- Compare legacy and v2 tree counts, indicator associations, weights, and roll-ups.
- Implement dual-read and pilot-only v2 writes behind flags.
- Add explicit project/programme/organisation relationships and crosswalks; do not infer scope from display names.
- Integrate theory of change, risks/assumptions, activities, milestones, and indicator targets with traceable links.
- Add lifecycle and archive behavior so retired logframe nodes remain available to historical reports.

**Exit gate:** reconciliation passes on representative data and pilot users sign off; rollback to legacy reads is proven before broad write cutover.

### Phase 7 — Budget, delivery, feedback, and corrective action

**Objective:** connect resources and implementation to measurable results without overstating causality.

**Deliverables:**

- Align projects, activities, milestones, budget categories, budget lines, expenditures, currency, and period.
- Define forecast, commitment, actual expenditure, exchange-rate, and variance semantics distinctly.
- Link budget lines and expenditures to project/activity/logframe result nodes where valid.
- Define implementation progress calculation as a configurable and explainable rule; avoid conflating milestone completion with outcome achievement.
- Add beneficiary feedback privacy, anonymity, safeguarding, severity, assignment, resolution, and retention policies.
- Ensure high-severity complaints can trigger corrective actions transactionally with the feedback record and duplicate-safe rules.

**Exit gate:** budget summaries are reproducible and currency/period-aware; progress scores expose their calculation; feedback identity and confidentiality tests pass.

### Phase 8 — Reporting, analytics, and collaboration

**Objective:** make monitoring and reporting usable at portfolio scale and reproducible for donors.

**Deliverables:**

- Add report definitions, period/scope instances, versioned section schemas, immutable snapshots, approvals, PDF/Excel/CSV exports, and export-job audit.
- Build global, organisation, project, and user dashboards from authorized, period-aware read models.
- Add coverage, timeliness, variance, disaggregation, budget, activity, feedback, and corrective action views.
- Implement pivot, map, chart, and KPI widgets only after query and authorization semantics are established.
- Add virtualized tables and data grids for large logframes and indicator matrices.
- Add collaboration comments, mentions, reactions, notifications, preferences, and attachment policies against explicit supported domain entities.
- Add localization for UI, question content, option labels, reports, and date/number formatting.
- Add Power BI or other BI access via documented, scoped, and revocable integration contract.

**Exit gate:** approved historical reports reproduce identically; dashboard and export queries enforce row/field permissions and meet agreed performance targets.

### Phase 9 — Enterprise operations and suite readiness

**Objective:** support maintainable production operation and expansion.

**Deliverables:**

- Introduce background workers for import, calculation, report generation, sync, notifications, and privacy jobs, with bounded retries and dead-letter/replay tooling.
- Add OpenTelemetry traces, structured logs, metrics, queue health, calculation latency, import/sync outcomes, and audit correlation.
- Evaluate Redis/hybrid caching based on measured needs; invalidate formula catalogs and permission-sensitive caches correctly.
- Support Azure Blob and S3/MinIO adapters through a common storage contract; test object integrity and access expiration.
- Automate backup/restore for SQL, blobs, data-protection keys, and configuration; rehearse recovery.
- Complete security review, threat model, dependency policy, secret rotation, vulnerability response, and operational runbooks.
- Assess tenant model, database portability, PostgreSQL, data warehouse, and integration marketplace only as funded design decisions.
- Establish Community, Enterprise, and Managed deployment feature boundaries without maintaining divergent application forks.

**Exit gate:** operators can deploy, observe, recover, rotate credentials, replay jobs, and export/delete data under documented procedures.

## Cross-cutting implementation requirements

Every phase must include:

- additive model and reversible/idempotent migration;
- API contract and OpenAPI updates;
- shared authorization and validation;
- Angular route, state, and error handling where user-facing;
- unit, SQL Server integration, API, and relevant UI tests;
- audit/provenance and observability;
- feature flag and operator documentation;
- data backfill preview, retry, reconciliation, and rollback;
- explicit acceptance criteria and product/domain owner sign-off.

## Release and rollout controls

Suggested flags, enabled independently per environment and pilot scope:

- `Features:IngestionV2`
- `Features:IndicatorContributions`
- `Features:DataSubmissionWorkflow`
- `Features:LogFrameV2Write`
- `Features:OfflineSync`
- `Features:ReportInstances`
- `Features:DashboardV2`
- `Features:ExternalPushV2`

Rollout sequence:

1. Schema-only release.
2. Backfill dry-run and data quality report.
3. Pilot read/compare mode.
4. Pilot write mode for selected projects.
5. Reconciliation and operational sign-off.
6. Progressive rollout with metrics and support readiness.
7. Legacy write retirement only after a defined observation period.

At every stage, preserve an emergency feature-flag rollback and a tested database restore path. Do not drop legacy columns/tables in the parity release.

## Verification plan

### Domain correctness

- source provenance preserved across entry methods;
- invalid or ambiguous project/entity/period context rejected;
- duplicate imports and sync operations idempotent;
- zero, missing, not-applicable, and suppressed values remain distinct;
- numerator/denominator and disaggregation reconciliation rules tested;
- formula cycles and stale downstream indicators rejected;
- approved snapshots immutable and reproducible;
- automated review output has rule version and evidence trace.

### Security and privacy

- authorization matrix across roles, projects, entities, forms, reports, attachments, and exports;
- confidential question values excluded for unauthorized users;
- webhook signature/replay and key revocation tests;
- consent, export, anonymization, and retention tests;
- no sensitive data in logs, cache keys, or error payloads.

### Operational scale

Test targets must be agreed before release and cover at minimum:

- large logframe trees and indicator matrices;
- 100,000+ indicator values;
- import batches with mixed valid and invalid rows;
- device sync with duplicates, conflicts, attachments, and retries;
- report snapshots and concurrent exports;
- concurrent approvals and stale row versions;
- worker failure, restart, replay, and dead-letter recovery;
- full backup restore including blob and data-protection keys.

## Definition of done

The enterprise M&E suite is ready for production rollout only when:

- domain states and data authority are documented and consistent;
- core write paths share provenance and validation behavior;
- legacy and new paths reconcile on production-like fixtures;
- approvals and reports are immutable and auditable;
- security and privacy acceptance tests pass;
- offline and integration paths are idempotent and recoverable;
- performance targets and operational SLOs are agreed and met;
- migration, rollback, restore, and support runbooks are rehearsed;
- product owners approve each module's scope and known limitations.
