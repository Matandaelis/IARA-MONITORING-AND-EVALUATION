# Data Model: LogAlto-Depth Parity

**Specification:** [spec.md](spec.md)

## Principles

1. Extend existing classes as partial classes.
2. Preserve persisted enum values.
3. Use UUID primary keys for distributed/offline-safe entities.
4. Use SQL row versions for optimistic concurrency.
5. Use unique indexes for reporting and offline idempotency.
6. Keep JSON bounded, schema-validated, and not used for authoritative aggregate facts.

## Core entities

### Logframes

- `LogFrameNode`: arbitrary-depth hierarchy node.
- `LogFrameLevel`: organisation/logframe vocabulary and rank.
- `LogFrameNodeIndicator`: weighted indicator linkage.
- `LogFrameNode` links to legacy `LogFrameRow` through `LegacyLogFrameRowId` for backfill.

Unique and concurrency rules:

- unique code by logframe when code is not blank;
- unique level rank within a logframe or organisation template;
- restrict deletes on parent/level relationships;
- preserve sort gaps such as 1000/2000/3000;
- use row versions for node updates.

### Indicators

Extend `Indicator` with:

- kind;
- numerator/denominator;
- definition;
- collection method;
- risks/assumptions;
- library ownership and sharing state.

New entities:

- `IndicatorTag`;
- `IndicatorTagLink`;
- `FormIndicatorMapping`;
- `QuestionIndicatorLink`;
- `IndicatorContribution`;
- `IndicatorTargetDisaggregation`;
- `DatumRevision`.

Rules:

- `Datum` remains the reporting projection;
- contributions are immutable after computation;
- only submitted responses contribute unless explicitly configured otherwise;
- recalculation replaces the projection for the affected entity/period/indicator scope;
- disaggregation totals and option rows must reconcile or be quarantined;
- target/baseline option values use `IndicatorTargetDisaggregation`.

### Data approvals

- `DataSubmission`;
- `DataSubmissionItem`;
- `ApprovalWorkflow`;
- `ApprovalWorkflowStep`;
- `ApprovalWorkflowInstance`;
- `ApprovalAction`.

Recommended state machine:

```text
Draft -> InProgress -> Approved
                    -> Rejected
                    -> Returned -> InProgress
                    -> Cancelled
```

No workflow action may be applied twice. No approver may act outside the active step. Previous-user separation is configurable.

### Forms

Extend `Question` and `Answer` with:

- number, date, yes/no, geopoint, location, project, activity, user, and signature types;
- validation limits and regex;
- confidentiality;
- numeric/date/boolean typed projections;
- future/past date rules.

New entities:

- `FormShare`;
- `FormLink`;
- `RecordLink`;
- `LocalizedText`.

Append new `QuestionType` values only after existing values. Never reorder persisted enum members.

### Portfolio and finance

Extend `Project` with code, description, organisation unit, manager, dates, status, and currency.

New entities:

- `Activity`;
- `ProjectFavorite`;
- `CustomFieldDefinition`;
- `CustomFieldValue`;
- `BudgetLine`;
- `Expenditure`.

Custom values belong to exactly one definition/entity pair. Parent activities use restricted-delete relationships.

### Collaboration and files

- `Attachment`;
- `Comment`;
- `CommentReaction`;
- `Notification`.

Attachments may reference legacy `Document` rows or external/blob storage. They must not duplicate file bytes in multiple storage systems.

### Reports and dashboards

- `ReportDefinition`;
- `ReportInstance`;
- `ReportSectionInstance`;
- `Dashboard`;
- `DashboardWidget`.

Report instances use a stable `ScopeKey` and unique scope/period key. Section content is a snapshot rather than a live render.

### Offline synchronization

- `ClientDevice`;
- `SyncOperation`;
- `ChangeLog`.

Rules:

- device identity belongs to a user;
- revoked devices cannot sync;
- `ClientOperationId` is unique per device;
- response client IDs are unique per questionnaire while active;
- sync operations retain accepted/conflict/rejected status;
- change logs are append-only and ordered by SQL `IDENTITY bigint` version.

### Audit and privacy

- `AuditLog`;
- `DatumRevision`;
- retention metadata owned by policy entities;
- consent fields owned by form response models.

Audit logs contain action, before/after JSON, user, IP, user-agent, entity, and time. Audit data is append-only.

## Required indexes

- logframe tree: logframe, parent, sort order;
- logframe node code: unique active code per logframe;
- form/indicator mapping: questionnaire, indicator, optional section;
- contribution: response, indicator, entity, date, disaggregation;
- indicator target: indicator, entity, date, disaggregation option;
- datum revisions: series and changed time;
- datum revisions: source and source ID;
- custom field definitions: entity type and field key;
- custom field values: entity/field pair;
- dashboard widgets: dashboard and sort order;
- response: questionnaire plus client-generated ID, filtered to active records;
- sync operation: device and client operation ID;
- change log: entity type/version;
- notification: recipient/read/created time.

## EF guidance

- Configure entities in partial `ApplicationDbContext` methods.
- Keep existing `OnModelCreating` calls intact.
- Use restricted delete for parent/level relationships that are part of an audit trail.
- Use cascades only for owned child collections such as report sections or dashboard widgets.
- Use SQL filtered unique indexes for nullable client identifiers.
- Add no tenant discriminator until the tenancy decision is accepted.

## Migration sequence

1. Add additive tables and nullable columns.
2. Add indexes.
3. Backfill legacy logframe rows and indicator metadata.
4. Add form mappings and contributions after pilot mapping configuration.
5. Add workflow tables and migrate data-review records by adapter.
6. Add reporting, dashboard, and sync entities.
7. Add privacy and retention policies.
8. Test forward migration and rollback from a production-sized fixture.
