# MONIC Gap Analysis — vs. M&E System Specification

> Source specification: `deepseek_markdown_20261021_81d64e.md` (Monitoring and Evaluation System — Functional and Technical Details).
> Analysis date: 2026-10-21. Codebase: MONIC v2 (`ASP.NET Core 10 + SQL Server + Angular 19`).

## 1. Feature Coverage Matrix

| # | Specification area | Status in MONIC | Evidence |
|---|--------------------|-----------------|----------|
| 1 | Project portfolio (list/grid, filters) | ⚠️ Partial | `admin/projects/project.list.component` — admin CRUD list, no favourites/grid-card view |
| 2 | Project fields configuration (dynamic fields) | ✅ Covered | `Fields` / `ItemFields` / `ItemOptions` entities |
| 3 | Project dashboard (per-project overview) | ❌ Missing | Only Home widget dashboard + per-project Gantt |
| 4 | Activities & sub-activities, Gantt / workplan | ✅ Covered | `Task`, `Milestone`, custom Gantt (`gantt/gantt.component`) |
| 5 | Implementation rate (indicator/milestones/sub-tasks) | ⚠️ Partial | Tasks have `PercentCompleted`; no composite rate engine |
| 6 | Budget & expenditure tracking by expense category | ❌ Missing | No entity/controller/DTO anywhere |
| 7 | Logframe design (goals/outcomes/outputs + drag-drop) | ✅ Covered | v1 editor (`logframe/logframe.component` + CDK drag) and v2 tree (`logframe-v2`) |
| 8 | Customizable vocabulary / analytical levels | ✅ Covered (v2) | `LogFrameLevel` custom levels; v1 fixed Goals/Outcomes/Outputs |
| 9 | Global / org-unit logframes, corporate indicators | ⚠️ Partial | LogFrames not bound to org units; no "mandatory corporate indicator" propagation |
| 10 | Indicator library (search by category/tags) | ⚠️ Partial | Categories exist; **no tags** on indicators |
| 11 | Indicator types (quant/qual/num-denom/percentage) | ✅ Covered | `IndicatorType` enum + numerator/denominator fields |
| 12 | Indicator data entry + past entries | ✅ Covered | `/dataentry` grid, `Datum` history |
| 13 | Disaggregation of values/baseline/targets | ⚠️ Partial | `DisaggregationGroup/Option/Datum` schema + service; indicator targets table exists; **no admin UI** for groups; no baseline/target disaggregation |
| 14 | Calculated indicators from collected data | ⚠️ Partial | RPN `Calculation.cs` + SQL procs; `FormIndicatorLedger` is schema-only (Phase 1) — no engine writing `IndicatorContributions` |
| 15 | Form designer (drag-drop, skip logic, conditional fields, min/max) | ✅ Covered | `questionnaire.edit.component`, `QuestionLogic`, `SkipLogicOption`, `QuestionOptionGroup` |
| 16 | Form translation into any language | ❌ Missing | No translation tables on Question/Section/Option |
| 17 | Link forms together (record relations) | ❌ Missing | No `ResponseLink`/record-relation entity |
| 18 | Import data from Excel (.xlsx) | ⚠️ Partial | CSV only (`ImportCSV.cs`); export uses EPPlus but import cannot read .xlsx |
| 19 | Record page (fields + linked records + comments + approve) | ⚠️ Partial | `response.edit.component` shows fields; **no linked-records panel, no comments** |
| 20 | Sharing access to a form (users/projects/org-units/profiles + rights + confidential) | ❌ Missing | Only entity/indicator permissions; no per-form sharing table |
| 21 | Public survey link (non-users) | ✅ Covered | `/public/questionnaire/:publicCode`, `PublicCode` on Response |
| 22 | Mobile app / offline data collection + sync | ❌ Missing | No sync API (partial groundwork: `ClientGeneratedId` on Response) |
| 23 | Files upload + link-to-file, attached to projects/activities/records | ✅ Covered | `Document` + `DocumentsStorageService`, attached to `Item` |
| 24 | File tags + search by tags | ❌ Missing | No `Tag` entity; documents searchable by name only |
| 25 | Comments and notes (activities, records, indicators) + reactions + mention notifications | ❌ Missing | Only free-text `comments` inside M&E Review scorecard |
| 26 | Analytics: tables, pivot, charts | ⚠️ Partial | Chart builder (ECharts) + questionnaire analysis; no pivot-table builder |
| 27 | Analytics: maps (distribution + location) | ⚠️ Partial | Map exists as dashboard widget only, not a chart type in the builder |
| 28 | Export to Excel / JPG / PNG | ⚠️ Partial | Excel export (`ExcelReport.cs`), chart image export possible via ECharts; no map export |
| 29 | Dashboards: global/project/user, widget add/remove/resize/place | ⚠️ Partial | User dashboard with resize; no global/project dashboards, no drag repositioning |
| 30 | Power BI integration | ❌ Missing | No references |
| 31 | User profile page | ✅ Covered | `account.component` (+ password change, zxcvbn) |
| 32 | Profiles (custom, editable access rights matrix) | ⚠️ Partial | Fixed `Roles` enum; `ProfileModel` not persisted; no per-profile rights editor |
| 33 | Access rights per project / org unit | ⚠️ Partial | `EntityPermission` (org units) + `IndicatorPermission`; **no per-project permission** |
| 34 | Access to forms (per-form sharing) | ❌ Missing | Same as #20 |
| 35 | Planning & reporting documents (quarterly/annual, configurable sections, approval workflow) | ❌ Missing | Only data-level review workflow (`DataReview`: Submit→Verify→Approve) |
| 36 | Help center | N/A | Out of app scope |
| 37 | Hosting/backup/monitoring (Docker, health check) | ✅ Covered | `Dockerfile`, `docker-compose.yml`, `/health`, docs |
| 38 | Security (OAuth2, TLS, roles, server-side validation) | ✅ Covered | OpenIddict (password + refresh + PKCE), `ApiExceptionAttribute`, server-side validation |
| 39 | Audit trail | ✅ Covered | `AuditLog` entity (append-only JSON diffs) |
| 40 | Multi-language system UI (i18n) | ❌ Missing | Only `LOCALE_ID: 'en-ZA'`; no `$localize`/translate |
| 41 | REST API for 3rd-party integration | ✅ Covered | OpenIddict-secured REST API |
| 42 | GDPR (PII, batch delete, consent) | ⚠️ Partial | No batch-delete tooling or consent-form support |

## 2. Prioritized Gap Backlog

| Priority | Gap | Rationale | Effort |
|----------|-----|-----------|--------|
| **P1 (this build)** | Comments & notes + @mention notifications | Collaboration is foundational; spec ties comments to records, indicators, activities, reviews | M |
| **P1 (this build)** | In-app notifications (bell, unread count, mark read) | Required by mentions and review workflow alerts | M |
| **P1 (this build)** | File/document tags + tag-filtered search | Small, self-contained, high usability win | S |
| **P1 (this build)** | Budget & expenditure tracking per project by category | Core RBM feature explicitly named in spec ("Activities and Expenditures Monitoring") | M |
| P2 | Project dashboard page | Aggregates existing data; no new domain tables | M |
| P2 | Disaggregation admin UI | Schema exists; UI gap only | S |
| P2 | Excel (.xlsx) import of records | EPPlus already in stack for export | M |
| P2 | Maps as chart type in Analytics | ECharts + Google Maps already available | M |
| P3 | Per-form sharing (rights + confidential fields) | Needs new permission model + UI | L |
| P3 | Planning & reporting documents + approval workflow | Large: document types, sections, workflow states, UI | L |
| P3 | Multi-language UI (i18n) + form translation | Large; touches every template + translation tables | L |
| P3 | Linked forms / record relations | New relation entity + record-page panel | M |
| P3 | Indicator library tags | After document tags prove the tag pattern | S |
| P4 | Pivot-table analytics | Specialized UI | L |
| P4 | Power BI integration | External dependency, embedding + licensing | M |
| P4 | Offline mobile app + sync API | Very large; groundwork (`ClientGeneratedId`) exists | XL |

## 3. Decisions taken for P1 build

1. **Comments** are polymorphic (`SubjectType` + `SubjectId`) so the same panel serves records (Response), indicator data (Datum), tasks (activity) and logframe rows without schema churn.
2. **Reactions** are a simple string badge (`like` / `dislike`, extensible), one row per user per comment.
3. **Notifications** are server-persisted per recipient with a client `Link` route; the bell polls unread count. Mention parsing is explicit (mention picker → `mentionUserIds`), avoiding fragile text parsing.
4. **Tags** are a global dictionary (`Tag`) with a `DocumentTag` join; document tags sync through `DocumentDTO.Tags` on save, so the existing document modal flow stays intact.
5. **Budget** = `BudgetCategory` (admin-managed dictionary) + `BudgetLine` (project, category, period, planned, actual). Summary endpoint aggregates planned/actual/variance/utilization per category. Currency code stored per line.
6. **Authorization**: comments & notifications are open to any authenticated user (user-facing); tags & budget follow existing role conventions (`Manager`+`Administrator` for writes).
7. **Data-review integration**: when a review is Verified/Approved/Rejected, the submitter receives a notification (spec: comments/notifications relate to the approval workflow).
