# Research: LogAlto-Depth Parity Decisions

**Specification:** [spec.md](spec.md)

## Existing primitives to preserve

- `Datum` is the authoritative time-series fact and projection surface.
- `Calculation` owns formula interpretation and topological execution.
- `Response` and `Answer` own questionnaire submissions and answer values.
- `LogFrameRow` remains the legacy hierarchy implementation until v2 reaches parity.
- `DataReview` remains the legacy append-only review mechanism during migration.
- `Indicator` remains the central indicator configuration entity.
- `Project`, `Entity`, and `Date` remain organisation/project/period primitives.

## Decision: Add a logframe v2 rather than modify v1 in place

Rationale:

- arbitrary levels and weights require an additive model;
- legacy `LogFrameRow` is already used in controllers and UI;
- dual-read and backfill reduce rollout risk;
- preserving the legacy ID in the v2 node provides audit and rollback traceability.

## Decision: Add an indicator ledger

Rationale:

- form submissions are sources of indicator values;
- recalculation must not depend on mutable totals;
- audit and replay require an immutable source record;
- disaggregation and denominator values need provenance.

`Datum` remains the projection for compatibility, while `IndicatorContribution` is the audit ledger.

## Decision: Use JSON only for configuration and snapshots

JSON is appropriate for:

- expression trees;
- custom-field values;
- chart and dashboard layout;
- report section content;
- sync conflict payload;
- before/after audit snapshots.

JSON is not appropriate as the sole source for:

- indicator values;
- approvals;
- workflow state;
- reporting facts;
- data ownership;
- user permissions.

## Decision: Store approval workflow definitions relationally

Configurable workflow definitions require relational keys for:

- versioning;
- step sequencing;
- permission checks;
- SLA calculations;
- delegation;
- audit;
- report/submission association.

A workflow engine library is optional. The relational aggregate and state machine remain the source of truth.

## Decision: Use SQL change log for delta synchronization

A monotonic SQL `IDENTITY bigint` change version is practical for the initial implementation. It supports:

- delta retrieval;
- ordering;
- retained operation history;
- conflict detection;
- reconnecting clients.

A message broker is unnecessary until change throughput outgrows SQL change-log readers.

## Decision: Offline conflicts are staged

Silent last-write-wins behavior is not suitable for monitoring data. Every sync operation must return an explicit:

- accepted;
- duplicate;
- conflict;
- rejected;
- staged for review;
- partial success;

## Decision: Use external credentials, not global API keys

A global API key cannot represent source, scope, expiry, project, or caller identity. Add integration registration entities and signed request verification before treating external systems as first-class sources.

## Open decisions

- cache provider and Redis hosting model;
- S3/MinIO versus Azure Blob as the default enterprise provider;
- Hangfire or a dedicated queue for background workers;
- Elasticsearch or database-backed search for attachments/comments;
- whether to expose Power BI as export, embed, or connector;
- whether tenant isolation is database, schema, or application-policy based;
- whether report snapshots should use append-only blob storage;
- whether i18n should use Angular native i18n or a runtime translation provider;
- how to migrate existing SQL Server formula procedures into an isolated calculation service.
