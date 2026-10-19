<!--
SYNC IMPACT REPORT
==================
Version Change: 1.0.0 -> 1.1.0 (MINOR: Expanded core principles and added M&E benchmark standards & development quality gates based on comparative gap analysis with DHIS2, ActivityInfo, and DevResults)
Modified Principles:
- "II. M&E Calculation & Domain Soundness" -> "II. M&E Calculation Determinism & Dimensional Soundness" (expanded with disaggregation consistency & target variance rules)
- "III. Security Boundary & Role-Based Access Control" -> "III. Immutable Audit Trail & Multi-Stage Governance" (elevated cell-level change history & 3-stage review lifecycle)
- "IV. Single-Container Cloud & DevOps Resilience" -> "IV. Security Boundary, Tenant Isolation & Role-Based Access Control" (added partner organization isolation & OpenIddict/PKCE)
- "V. Test & Verification Gate" -> "V. Single-Container DevOps Resilience & Offline-First Field Readiness" (added offline sync & PWA data collection resilience)
- Added Principle: "VI. Test-First Verification Gate & Continuous Assurance" (formalized AAA testing, deterministic rule engine coverage, and API contract freeze)
Added Sections:
- "## Benchmark Standards & M&E Gap Governance" (Formalized standards for dimensional disaggregations, target variance analysis, humanitarian PII protection, and donor data interoperability)
- "## Development Workflow & Quality Gates" (Enforces Spec Kit specification-driven lifecycle, contract-first OpenAPI design, and deterministic verification gates)
Removed Sections: None
Follow-up Deferred Items: None (all template placeholders resolved with concrete text)
-->

# MONIC Platform Constitution

## Core Principles

### I. Zero Data Loss & Database Migration Integrity
- Schema changes MUST strictly follow the Entity Framework Core migration lifecycle: Entity (`WEB/Models/`) ➔ DTO (`WEB/Models/DTOs/`) ➔ Migration (`dotnet ef migrations add`).
- Every migration MUST be inspected for both `Up()` and `Down()` operations to guarantee full reversibility without data corruption.
- Nullable unique constraints MUST be configured using `CreateNullableUniqueIndex` in `ApplicationDBContext.cs`.
- Deleting or altering columns, tables, or relations carrying beneficiary data without an explicit data retention and rollback migration is STRICTLY PROHIBITED.

### II. M&E Calculation Determinism & Dimensional Soundness
- Indicator calculation formulas MUST adhere to Reverse Polish Notation (RPN) token stacks (`Indicator`, `Number`, `Operator`, `Parenthesis`) and execute deterministically through `Calculation.cs`.
- Temporal rollups (`Month` ➔ `Quarter` ➔ `Year`) and aggregation rules (`Sum`, `Average`, `MostRecent`) MUST preserve mathematical determinism and prevent null-propagation, NaN, or divide-by-zero failures.
- Results-Based Management (RBM) and Theory of Change (ToC) causal pathways MUST enforce strict directed acyclic graph (DAG) topology (`Input` ➔ `Activity` ➔ `Output` ➔ `Outcome` ➔ `Impact`), rejecting circular dependencies and reverse causality.
- Multi-dimensional disaggregation (e.g., Sex, Age Category, Urban/Rural, Disability) MUST maintain mathematical parity: the sum of disaggregated values MUST equal the parent indicator aggregate total.

### III. Immutable Audit Trail & Multi-Stage Governance
- The 3-stage data approval lifecycle (**`Submit` ➔ `Verify` ➔ `Approve` / `Reject`**) is non-negotiable; unverified data MUST NEVER leak into official dashboard aggregations or donor-facing reports.
- Data modifications MUST generate an immutable audit trail capturing user identity, timestamp, previous value, modified value, and verification rationale.
- Rejection of reported indicators or survey submissions MUST mandate an actionable justification recorded directly in `DataReview`.

### IV. Security Boundary, Tenant Isolation & Role-Based Access Control
- All API endpoints MUST inherit from `BaseApiController` and apply explicit authorization policies matching system roles (`Administrator`, `Manager`, `Oversight`, `DataEntry`).
- Implementing partner organizations MUST be isolated via entity-level permissions: partners MUST NOT access unapproved or restricted data belonging to peer organizations.
- Authentication MUST enforce OAuth 2.0 with PKCE and OpenIddict token validation.
- Survey responses containing Personally Identifiable Information (PII) MUST be protected with field-level access constraints and anonymized in public exports.

### V. Single-Container DevOps Resilience & Offline-First Field Readiness
- The unified container deployment (SQL Server 2022/2025, .NET 10 Kestrel runtime, Angular SPA in `wwwroot`) MUST boot deterministically within defined container health checks (`/health`).
- All persistent runtime state, Data Protection keys, and OpenIddict signing certificates MUST reside strictly under persistent mount volume `/app/data`.
- Field survey modules MUST support offline-first operation, client-side caching, and structured synchronization with conflict-resolution semantics when network connectivity is restored.

### VI. Test-First Verification Gate & Continuous Assurance
- Every new feature, calculation operator, or validation rule MUST be accompanied by automated unit or integration tests before merge.
- The `DeterministicRuleEngine` suite MUST maintain 100% pass rate on ToC acyclicity, survey skip logic, sampling plan power calculations (Cochran/Yamane formulas), and LogFrame structure tests.
- Breaking API changes MUST NOT be released without backward-compatible versioning for public survey links (`PublicCode`) and external integrations.

## Benchmark Standards & M&E Gap Governance

### Dimensional Disaggregation Matrix Standard
- Inspired by international standards (DHIS2 Category Combos and DevResults Disaggregation Matrices), MONIC establishes formal dimension categories for standard humanitarian and development reporting.
- Any disaggregated indicator MUST define valid dimension categories with mutually exclusive and collectively exhaustive buckets.

### Target Baseline & Variance Monitoring Standard
- Indicator configurations MUST support baseline values, target schedules (quarterly/annual), and cumulative vs. non-cumulative tracking.
- Automated variance scoring MUST classify indicator performance against milestones (Underperforming: < 75%, On Track: 75% - 110%, Exceeding: > 110%) with automated alerts for off-track indicators.

### Humanitarian Data Ethics & "Do No Harm" Compliance
- Field collection questionnaires MUST include explicit informed consent gates prior to question routing.
- Vulnerability data, child safeguarding metrics, and sensitive geographic identifiers MUST be masked or encrypted at rest in accordance with international humanitarian data standards.

### Interoperability & Data Exchange Standard
- MONIC APIs MUST support standardized tabular exports (Excel, CSV) and standard REST payload contracts for external reporting (e.g., IATI, UN OCHA, and donor portals).

## Development Workflow & Quality Gates

### Spec Kit Lifecycle Compliance
- Every functional modification or enhancement MUST adhere to the specification-driven development workflow:
  1. Feature Specification via `/speckit-specify`
  2. Plan & Architecture Design via `/speckit-plan`
  3. Actionable Task Breakdown via `/speckit-tasks`
  4. Test-Gated Implementation via `/speckit-implement`
  5. Post-implementation convergence audit via `/speckit-converge`

### Code Quality & Contract Review
- C# backend code MUST comply with `.NET 10` best practices, nullable reference types, and explicit DTO validation.
- Angular frontend code MUST adhere to Angular 19+ best practices, Signals reactivity, and strict typing.
- All database modifications MUST be scripted and validated against zero-data-loss guardrails prior to production deployment.

## Governance
- This constitution represents the foundational architectural law of the MONIC platform and supersedes any unformalized development practices.
- Amendments to this constitution require:
  1. A formal amendment proposal detailing rationale, industry benchmark alignment, and impact assessment.
  2. Lead Coordinator review and verification against core M&E domain integrity.
  3. A semantic version increment (MAJOR for breaking changes or principle removal, MINOR for new principles or expanded standards, PATCH for textual clarifications).
- All pull requests, code reviews, and automated builds MUST verify compliance with these constitutional principles before deployment.

**Version**: 1.1.0 | **Ratified**: 2026-10-14 | **Last Amended**: 2026-10-19
