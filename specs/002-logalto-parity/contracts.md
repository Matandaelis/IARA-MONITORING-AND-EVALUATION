# API and Integration Contracts

**Specification:** [spec.md](spec.md)

## REST conventions

- use ASP.NET Core controller and OpenAPI conventions already in MONIC;
- return `ProblemDetails` for validation and authorization failures;
- use route IDs for existing resources;
- use idempotency IDs for imports and sync operations;
- return `Location` headers for created aggregates;
- use cursor or keyset pagination for ledgers, change logs, comments, and notifications.

## Logframe APIs

- `GET /api/logframes-v2/{id}`
- `GET /api/logframes-v2/{id}/tree`
- `POST /api/logframes-v2/{id}/nodes`
- `PUT /api/logframes-v2/nodes/{nodeId}`
- `POST /api/logframes-v2/nodes/{nodeId}/move`
- `POST /api/logframes-v2/nodes/{nodeId}/indicators`
- `DELETE /api/logframes-v2/nodes/{nodeId}/indicators/{indicatorId}`
- `POST /api/logframes-v2/{id}/backfill-preview`
- `POST /api/logframes-v2/{id}/backfill-commit`

The move contract shall include parent, sort position, and row version. A stale row version returns a conflict and no changes.

## Indicator and ledger APIs

- `GET /api/indicator-tags`
- `POST /api/indicator-tags`
- `POST /api/indicator-mappings`
- `POST /api/indicator-mappings/validate`
- `POST /api/responses/{responseId}/contributions/refresh`
- `POST /api/ledger/{indicatorId}/rebuild`
- `GET /api/indicators/{indicatorId}/revisions`
- `PUT /api/indicators/{indicatorId}/targets/disaggregated`

Contribution refresh must be idempotent. Rebuild APIs require project/indicator authorization and a reporting-period scope.

## Submission and workflow APIs

- `POST /api/data-submissions`
- `GET /api/data-submissions/{id}`
- `PUT /api/data-submissions/{id}`
- `POST /api/data-submissions/{id}/submit`
- `POST /api/data-submissions/{id}/actions`
- `GET /api/workflows`
- `POST /api/workflows`
- `POST /api/workflows/{id}/activate`

The action contract:

```json
{
  "decision": "Approve",
  "comment": "Validated against source evidence",
  "expectedRowVersion": "base64-row-version"
}
```

The server must enforce workflow step, role, required permission, separation of duties, and row version.

## Survey APIs

- `GET /api/forms/{publicCode}/public-definition`
- `POST /api/forms/{publicCode}/responses`
- `POST /api/forms/{questionnaireId}/responses/{responseId}/submit`
- `PUT /api/forms/responses/{responseId}/answers/{answerId}`
- `GET /api/forms/{questionnaireId}/translations`
- `PUT /api/forms/{questionnaireId}/translations`
- `POST /api/forms/{questionnaireId}/imports/preview`
- `POST /api/forms/{questionnaireId}/imports/{importJobId}/commit`

Answer writes must preserve typed projections and reject invalid confidential or disaggregation values.

## External push and offline sync

Existing external-push endpoints remain compatible but are hardened:

- `POST /api/external/records`
- `POST /api/external/kobo-webhook/{questionnaireId}`
- `POST /api/external/offline-sync`

Add:

- `POST /api/devices/register`
- `POST /api/devices/revoke`
- `POST /api/sync/operations`
- `GET /api/sync/changes?fromVersion={number}`
- `POST /api/integrations/{name}/rotate-secret`

Offline operation contract:

```json
{
  "deviceOperationId": "uuid-or-client-generated-id",
  "entityType": "Response",
  "entityId": "guid",
  "operation": "create",
  "rowVersion": "optional-base64-row-version",
  "payload": {}
}
```

Rules:

- reject unknown device IDs;
- reject revoked devices;
- enforce device/user scope;
- deduplicate by device and operation ID;
- return conflict payload rather than silent overwrite;
- never use latest available entity/date context as a silent fallback.

## Webhook security

- use `X-IARA-Signature` or an HMAC signature with timestamp;
- reject stale timestamps and replayed signatures;
- identify the integration registration and source tenant/project;
- use scoped API credentials rather than one global secret;
- support key rotation with overlap;
- log request ID, integration ID, source submission ID, and outcome.

## Portfolio, finance, and monitoring

- `GET /api/projects`
- `GET /api/projects/{id}`
- `GET /api/projects/{id}/activities`
- `POST /api/projects/{id}/activities`
- `GET /api/projects/{id}/budget`
- `POST /api/projects/{id}/budget-lines`
- `POST /api/budget-lines/{id}/expenditures`
- `GET /api/monitoring/projects/{projectId}/summary`
- `GET /api/monitoring/projects/{projectId}/matrix`
- `GET /api/monitoring/projects/{projectId}/coverage`

## Collaboration and attachments

- `GET /api/comments?entityType=&entityId=`
- `POST /api/comments`
- `POST /api/comments/{id}/reactions`
- `GET /api/notifications?unreadOnly=true`
- `POST /api/notifications/{id}/read`
- `POST /api/attachments/presign`
- `POST /api/attachments/complete`
- `DELETE /api/attachments/{id}`

Presigned uploads must bind project/entity/user/scopes and expire. Completion writes metadata only after storage verification.

## Reports and dashboards

- `GET /api/report-definitions`
- `POST /api/report-definitions`
- `POST /api/reports/instances`
- `POST /api/reports/instances/{id}/refresh`
- `POST /api/reports/instances/{id}/submit`
- `GET /api/reports/instances/{id}/snapshot`
- `GET /api/dashboards`
- `POST /api/dashboards`
- `PUT /api/dashboards/{id}/layout`
- `POST /api/dashboards/{id}/widgets`

Report creation includes a stable project/entity/period scope key. Refresh creates a snapshot version and never silently changes an approved report.

## Export contract

`POST /api/exports`:

```json
{
  "format": "xlsx|pdf|csv|png",
  "resourceType": "Monitoring|Report|Indicator|Dashboard",
  "resourceId": "guid",
  "scope": {},
  "snapshot": true
}
```

Exports are asynchronous jobs with:

- `queued`, `running`, `completed`, `failed`, `expired` states;
- signed download URLs;
- expiry;
- checksum;
- audit event;
- repeatable snapshot reference.

## OpenAPI requirements

- every endpoint has authorization metadata;
- request and response types are named and schema-documented;
- enums are explicit and append-only;
- no API key appears in the query-string contract;
- row-version and conflict behavior are represented;
- public form APIs cannot expose confidential fields;
- integration APIs expose replay/duplicate behavior.
