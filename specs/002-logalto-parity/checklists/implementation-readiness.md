# Implementation Checklist: LogAlto-Depth Parity

**Specification:** [spec.md](spec.md)

## Architectural completeness

- [ ] Existing entities remain compatible.
- [ ] New entities are additive.
- [ ] EF migrations are forward and reversible.
- [ ] Feature flags govern new behavior.
- [ ] Legacy and v2 paths have parity tests.
- [ ] JSON is schema-validated.
- [ ] No authoritative fact is JSON-only.
- [ ] Client/approval states are explicit.

## Security and authorization

- [ ] Project, entity, indicator, form, report, and attachment scopes are enforced.
- [ ] Confidential fields are excluded before serialization.
- [ ] Public form tokens expire and cannot expose unrelated records.
- [ ] External keys are scoped and rotatable.
- [ ] Webhooks are signed and replay-protected.
- [ ] API users have separation of duty.
- [ ] Audit records include actor and source context.

## Data integrity

- [ ] Existing enum values are not renumbered.
- [ ] Ledger replay is deterministic.
- [ ] Contributions do not duplicate on replay.
- [ ] Form responses are deduplicated by client ID.
- [ ] Disaggregation totals reconcile or remain unapproved.
- [ ] Stale row versions reject updates.
- [ ] Missing entity or period context is rejected.
- [ ] Cycles are rejected at configuration and execution time.

## Survey and offline completeness

- [ ] Rich question types work in Angular and API.
- [ ] Skip logic cannot create cycles or hide required responses incorrectly.
- [ ] Offline submissions can be staged and retried.
- [ ] Reconnected devices receive server conflicts.
- [ ] Media uploads are resumable and scoped.
- [ ] Questionnaire schemas are versioned.

## Reporting completeness

- [ ] Report instances have stable scope keys.
- [ ] Approved snapshots do not silently mutate.
- [ ] PDF/Excel exports are reproducible.
- [ ] Export jobs have expiry and audit trails.
- [ ] Dashboards enforce project and organisation policies.

## Operations

- [ ] Health checks cover app and database dependencies.
- [ ] Backups include database, blobs, and data-protection keys.
- [ ] Restore runbooks are tested.
- [ ] Worker retries are bounded.
- [ ] Failed jobs are observable and replayable.
- [ ] Cache expiry never corrupts formula execution.
- [ ] Docker and Coolify environment variables are complete.

## Rollout gates

- [ ] Unit and integration tests pass.
- [ ] Pilot project sign-off is complete.
- [ ] Legacy/v2 reconciliation passes.
- [ ] Security review is complete.
- [ ] Rollback is rehearsed.
- [ ] Backfills are resumable and idempotent.
- [ ] Monitoring and alerts are active.
