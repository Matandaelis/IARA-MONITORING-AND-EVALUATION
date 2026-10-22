# LogAlto Parity — Phase 1 (Foundation) Implementation Plan

**Spec:** `docs/LOGALTO_PARITY_IMPLEMENTATION_GUIDE.md` (sections 1, 2.4, 3.1, 5.2 Phase 1, 5.6).
**Scope:** Roadmap Phase 1 only — proof of the new UI seam, the logframe v2 schema, the audit/typed-answer/response-sync columns, the form→indicator ledger schema, the read-only tree API, the backfill, and a read-only TreeTable screen behind a feature flag.
**Branch:** current feature branch (never `main`/`master`).

## Non-goals (explicitly out of scope for this plan)

1. **Multi-tenancy is deferred.** No `TenantId`, no tenant filters, no tenant context. The client is evaluating Lago billing; the multi-tenancy/isolation decision is not settled and MUST NOT be pre-empted in code.
2. No form→indicator **engine**, workflow engine, reporting, offline sync, file storage, dashboards, or billing code. Phase 1 ships schema for the ledger only.
3. No big-bang Angular standalone migration; no PrimeNG conversion of existing screens.
4. No destructive schema changes: no dropped tables/columns, no data loss.

## Global Constraints

- **Stack:** ASP.NET Core `net10.0`, EF Core 10, SQL Server Express local (`.\SQLEXPRESS`, database `MONIC`), Angular 19.2.17 (NgModule), PrimeNG v19.
- **Backend build:** `dotnet build WEB/WEB.csproj --nologo -v q` must succeed with 0 errors.
- **Backend tests:** `dotnet test WEB.Tests/WEB.Tests.csproj --nologo -v q` must stay green (baseline: 66 passed). Tests use EF InMemory — no database required.
- **Migrations:** run from repo root:
  - `dotnet ef migrations add <Name> --project WEB`
  - `dotnet ef database update --project WEB`
  Each schema task adds its own migration and applies it to the local `MONIC` database. Never hand-edit a migration from an earlier task or `ApplicationDbContextModelSnapshot.cs` except through `dotnet ef`.
- **No new NuGet packages.** No new npm packages except `primeng@19`, `@primeng/themes@19`, `primeicons`, and `sass` (devDependency).
- **Cascade deletes:** `ApplicationDbContext.OnModelCreating` force-sets **every** foreign key to `DeleteBehavior.Restrict` in a loop after the `Configure*` calls. Do NOT rely on `Cascade`; services delete child rows explicitly, in order.
- **Change tracking:** `ApplicationDbContext` has `ChangeTracker.AutoDetectChangesEnabled = false`. In services/tests, attach or update entities explicitly (`Add`, `Update`, `Remove`) or call `ChangeTracker.DetectChanges()`.
- **Conventions:** follow the surrounding code style; `Monic.Web.Models` namespace for entities, `Monic.Web.Code` for services, `Monic.Web.Controllers` for controllers; existing classes are mostly non-nullable-oblivious — match the file you edit.
- **Commits:** conventional commits (`feat(scope): …`, `chore(scope): …`), one logical change per commit. Never commit unrelated files (`git add` specific paths only), never revert files another task owns, never touch the checkpoint commit.
- **Angular verification:** the repo has **no working Angular test harness** (no karma/jasmine devDependencies). Do not add one in Phase 1. Frontend verification is `npm run build:dev` and `npm run build` (production) succeeding.
- **Bundle budget:** production initial budget is 2.5 MB warning / 4 MB error. Do not raise the budget without listing measured before/after sizes in the report. Import PrimeNG components individually; never `import * from 'primeng'`.
- **Feature flag:** `environment.logFrameV2` (build-time). Default `false` in `environment.prod.ts`, `true` in `environment.ts`.
- **Exact names are binding:** entity/property/index/DTO/test names in a task are exact; do not rename or normalize them.

## Interfaces between tasks

| Producer | Interface | Consumer |
|---|---|---|
| Task 2 | `LogFrameNode`, `LogFrameLevel`, `LogFrameNodeIndicator`, `IsLogFrameV2Enabled` (config key `Features:LogFrameV2`) | Tasks 5, 6, 7 |
| Task 3 | `AnswerValueProjector.Apply(Answer)`, `AuditLog` entity | Task 6 (audit rows in backfill) |
| Task 4 | `IndicatorContribution`, `FormIndicatorMapping`, `QuestionIndicatorLink` | Phase 2 (engine) — nothing in Phase 1 |
| Task 5 | `LogFrameTreeService.LoadTreeAsync(Guid logFrameId, CancellationToken)` and `/api/v1/logframes/{id}/tree` | Task 7 |
| Task 6 | `LogFrameBackfillService.BackfillAsync(...)` / `ReconcileAsync(...)` | operators only; Task 7 does not call it |

## Task 1: Frontend toolkit — PrimeNG v19, SCSS layers, curated module

**Deliverable:** PrimeNG v19 installed and wired into the existing NgModule app without converting screens to standalone, with Bootstrap moved into CSS cascade layers so it cannot override PrimeNG.

**Files**

- `WEB/ClientApp/package.json` (+ `package-lock.json`) — dependencies.
- `WEB/ClientApp/src/app/prime-ng.module.ts` — new.
- `WEB/ClientApp/src/app/shared.module.ts` — import/export `PrimeNgModule`.
- `WEB/ClientApp/src/app/app.module.ts` — `providePrimeNG` provider only (no template changes).
- `WEB/ClientApp/src/scss/prime.scss` — new layered entry file.
- `WEB/ClientApp/angular.json` — replace `bootstrap.min.css` with `src/scss/prime.scss`.

**Requirements**

1. Install exactly: `primeng@19`, `@primeng/themes@19` (matching `@prime-ng` 19 minor — use the latest 19.x), `primeicons`, and `sass` as a devDependency. Run from `WEB/ClientApp`.
2. `prime-ng.module.ts` exports only these modules:
   `AccordionModule, ButtonModule, CardModule, CheckboxModule, ChipModule, ConfirmDialogModule, DatePickerModule, DialogModule, DrawerModule, FileUploadModule, InputNumberModule, InputTextModule, MessageModule, MultiSelectModule, ProgressBarModule, RadioButtonModule, SelectModule, SkeletonModule, SplitterModule, TableModule, TagModule, TextareaModule, ToastModule, ToolbarModule, TooltipModule, TreeTableModule` (import paths are lowercase, e.g. `primeng/treetable`, `primeng/datepicker`, `primeng/select`).
3. `app.module.ts` adds a `providePrimeNG` provider with an `Aura`-based preset named `MonicPreset` (primary palette 500 = `#2563eb`), `darkModeSelector: '.app-dark'`, and `cssLayer: { name: 'primeng', order: 'bootstrap-base, primeng, bootstrap-utilities' }`. Do not restructure `AppModule` otherwise.
4. `src/scss/prime.scss` declares `@layer bootstrap-base, primeng, bootstrap-utilities;` and imports Bootstrap SCSS partials into the two Bootstrap layers: base layer gets `reboot, type, images, containers, grid, tables, forms, buttons, transitions, dropdown, nav, navbar, card, accordion, close, modal, tooltip, popover`; utilities layer gets `utilities/api`. Variables/mixins/functions are imported outside any layer. Include the `primeicons` CSS import so icon fonts load.
5. `angular.json` styles array: remove `node_modules/bootstrap/dist/css/bootstrap.min.css`; add `node_modules/primeicons/primeicons.css` (or the equivalent file that exists in the installed version) and `src/scss/prime.scss`; keep `theme.css` and `styles.css` **last** (unlayered app CSS is allowed to win over PrimeNG). Do not change scripts, budgets, or any other build option.

**Tests / verification**

- `cd WEB/ClientApp; npm install` completes; `package-lock.json` updated and committed.
- `npm run build:dev` succeeds.
- `npm run build` succeeds. Record the initial-bundle size from the build output in the report. If the budget error trips, stop and report measured numbers instead of editing budgets.
- `git grep -n "bootstrap.min.css" WEB/ClientApp/angular.json` returns nothing.

**Out of scope:** converting any existing screen, adding PrimeNG components to existing templates, dark mode work, theming beyond the primary palette.
## Task 2: Logframe v2 schema

**Deliverable:** `LogFrameNode` / `LogFrameLevel` / `LogFrameNodeIndicator` entities, EF configuration, migration `AddLogFrameV2`, applied to the local `MONIC` database.

**Files**

- `WEB/Models/LogFrameV2.cs` — new entities.
- `WEB/Models/ApplicationDBContext.LogFrameV2.cs` — new partial (DbSets + `ConfigureLogFrameV2Model`).
- `WEB/Models/ApplicationDBContext.cs` — add one call to `ConfigureLogFrameV2Model(modelBuilder);` immediately after `ConfigureDisaggregationModel(modelBuilder);`.
- `WEB/Migrations/*` — generated only via `dotnet ef migrations add AddLogFrameV2 --project WEB`.
- `WEB.Tests/LogFrameV2ModelTests.cs` — new.

**Requirements**

Entities exactly as follows.

`LogFrameNode`: PK `LogFrameNodeId` (`Guid`, generated); `LogFrameId` required; `ParentNodeId` nullable self-FK; `LogFrameLevelId` required; `Code` required, `MaxLength(50)`, default `""`; `Title` required, `MaxLength(500)`, default `""`; `Description`, `MeansOfVerification`, `RisksAndAssumptions` nullable strings; `SortOrder` int required; `Depth` int required; `IsActive` bool required default true; `CustomFieldsJson` string default `"{}"`; `LegacyLogFrameRowId` nullable Guid; `InheritedFromNodeId` nullable Guid; `CreatedAtUtc` required; `CreatedById` nullable; `UpdatedAtUtc` required; `UpdatedById` nullable; `[Timestamp] public byte[] RowVersion`; navigations `ParentNode`, `ChildNodes`, `LogFrame`, `Level`, `Indicators`.

`LogFrameLevel`: PK `LogFrameLevelId`; `LogFrameId` nullable; `OrganisationId` nullable; `Name` required `MaxLength(100)`; `PluralName` required `MaxLength(100)`; `Rank` int required; `IsIndicatorLevel` bool required; `Color` `MaxLength(7)`; `RequiredFieldsJson` default `"[]"`; `IsActive` bool default true; navigation `Nodes`.

`LogFrameNodeIndicator`: composite PK `(LogFrameNodeId, IndicatorId)`; `Weight` `decimal(9,4)` default 1; `IsPrimary` bool; `SortOrder` int; navigations `Node`, `Indicator`.

EF configuration (in `ConfigureLogFrameV2Model`):

- `LogFrameNode`: `HasOne(ParentNode).WithMany(ChildNodes).HasForeignKey(ParentNodeId)` (delete behavior untouched — the global loop sets Restrict); `HasOne(Level).WithMany(Nodes).HasForeignKey(LogFrameLevelId)`; index `IX_LogFrameNodes_Tree` on `(LogFrameId, ParentNodeId, SortOrder)`; unique index `UX_LogFrameNodes_Code` on `(LogFrameId, Code)` with filter `[Code] <> ''`; index `IX_LogFrameNodes_LegacyRow` on `LegacyLogFrameRowId`; `Property(RowVersion).IsRowVersion()`.
- `LogFrameLevel`: unique index `UX_LogFrameLevels_LogFrame_Rank` on `(LogFrameId, Rank)` filter `[LogFrameId] IS NOT NULL`; unique index `UX_LogFrameLevels_Org_Rank` on `(OrganisationId, Rank)` filter `[OrganisationId] IS NOT NULL AND [LogFrameId] IS NULL`.
- `LogFrameNodeIndicator`: `HasKey(x => new { x.LogFrameNodeId, x.IndicatorId })`; `Property(Weight).HasColumnType("decimal(9,4)")`; `HasOne(Node).WithMany(Indicators)`; `HasOne<Indicator>().WithMany()`; index `IX_LogFrameNodeIndicators_Indicator` on `IndicatorId`.

Also add `Features:LogFrameV2` (`false`) to `WEB/appsettings.json` and `WEB/appsettings.Development.json` — it is the server-side switch for this plan and must exist before Task 5 reads it.

**Tests** (`LogFrameV2ModelTests`, EF InMemory is enough for model metadata)

1. `LogFrameNode_HasCompositeIndexes_Configured` — assert the three index names exist on the entity type with expected properties.
2. `LogFrameNode_CodeIndex_IsUniqueAndFiltered` — assert `IsUnique` and filter `[Code] <> ''`.
3. `LogFrameLevel_RankIndexes_AreFiltered` — both unique filtered indexes present.
4. `LogFrameNodeIndicator_HasCompositeKey` — key properties are `LogFrameNodeId`, `IndicatorId` in order.
5. `LogFrameNode_SelfReference_UsesParentNodeId` — the self-referencing FK exists and points at `ParentNodeId`.
6. `Configuration_LoadsFromDefaultConfig` — `new ConfigurationBuilder().AddJsonFile("appsettings.json")` yields `Features:LogFrameV2 == false`. (Locate the file relative to the test assembly; skip if the file is not copied — then assert on `appsettings.Development.json` copied by the test project. If neither is available at runtime, delete this test and say so in the report.)

**Verification**

- `dotnet build WEB/WEB.csproj --nologo -v q` → 0 errors.
- `dotnet test WEB.Tests/WEB.Tests.csproj --nologo -v q` → all green.
- `dotnet ef database update --project WEB` → applies cleanly to `MONIC`.

**Out of scope:** backfill (Task 6), API (Task 5), UI, `LogFrameRow` changes.

## Task 3: Audit log, typed answer columns, response sync columns

**Deliverable:** `AuditLog` table; `Answer.ValueNumber/ValueDate/ValueBoolean` populated on every answer save and backfilled; `Response.ClientGeneratedId/ClientDeviceId/IsDeleted/LastSyncedAtUtc/RowVersion` for Phase 4 sync, with migration `AddAuditTypedAnswersAndResponseSync` applied locally.

**Files**

- `WEB/Models/AuditLog.cs` — new.
- `WEB/Models/Answer.cs` — make `public partial class Answer` and add the three typed columns.
- `WEB/Models/Response.cs` — add the five sync columns (class is already partial; do not redeclare `Submitted`).
- `WEB/Code/AnswerValueProjector.cs` — new static projector.
- `WEB/Models/DTOs/AnswerDTO.cs` — call the projector inside `Hydrate`.
- `WEB/Models/ApplicationDBContext.AuditTypedAnswers.cs` — new partial (DbSet + `ConfigureAuditTypedAnswersModel(ModelBuilder)`); call added to `ApplicationDBContext.cs` after `ConfigureLogFrameV2Model`.
- `WEB/Migrations/*` — generated via `dotnet ef migrations add AddAuditTypedAnswersAndResponseSync --project WEB`; backfill SQL added to the generated migration's `Up()` only.
- `WEB.Tests/AnswerValueProjectorTests.cs` — new.

**Requirements**

1. `AuditLog`: PK `AuditLogId`; `EntityType` required `MaxLength(50)`; `EntityId` Guid; `Action` required `MaxLength(50)`; `UserId` nullable Guid; `BeforeJson`, `AfterJson` strings; `IpAddress` `MaxLength(64)`; `UserAgent` `MaxLength(300)`; `CreatedAtUtc` required. Index `IX_AuditLogs_Entity_CreatedAt` on `(EntityType, EntityId, CreatedAtUtc)`.
2. `AnswerValueProjector.Apply(Answer answer)` is a pure static void method. Column types on the new properties: `ValueNumber` `[Column(TypeName = "decimal(20,8)")] decimal?`; `ValueDate` `[Column(TypeName = "date")] DateTime?`; `ValueBoolean` `bool?`.
   - `ValueNumber` set from `decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture)`, else null.
   - `ValueBoolean` = `bool.TryParse` result; also `"yes"`/`"no"` (case-insensitive) → true/false, else null.
   - `ValueDate` = `DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal)` result, else null.
   - A value may populate more than one column only when parsing genuinely succeeds for both (e.g. `"1"` sets `ValueNumber` and not `ValueDate`); `"2024-03-01"` sets `ValueDate` and not `ValueNumber`. Null/whitespace clears all three.
   - It never throws and never mutates `Value`.
3. `AnswerDTO.Hydrate` calls `AnswerValueProjector.Apply(answer)` after assigning `Value`, so `AnswersController.Save` and `SurveysController.SaveAnswer` both populate typed columns without controller changes.
4. Migration adds `IX_Answers_Question_Number` on `Answers(QuestionId, ValueNumber)` with filter `WHERE ValueNumber IS NOT NULL`, plus a one-time backfill in `Up()` using SQL Server `TRY_CAST`/`TRY_CONVERT` equivalent to the projector (numeric → `ValueNumber`, `'yes'/'no'`/`'true'/'false'` → `ValueBoolean`, dates → `ValueDate`). Filtered index name is exact.
5. `Response` columns: `ClientGeneratedId` `MaxLength(100)`; `ClientDeviceId` nullable Guid; `IsDeleted` bool required default false; `LastSyncedAtUtc` nullable; `[Timestamp] RowVersion`. Add unique filtered index `UX_Responses_ClientGeneratedId` on `(QuestionnaireId, ClientGeneratedId)` filter `[ClientGeneratedId] IS NOT NULL AND [IsDeleted] = 0`.

**Tests** (`AnswerValueProjectorTests`)

1. `Apply_Numeric_SetsValueNumberOnly`
2. `Apply_Boolean_And_YesNo_SetValueBooleanOnly` (theory: `true`, `False`, `yes`, `NO`)
3. `Apply_IsoDate_SetsValueDateOnly`
4. `Apply_ArbitraryText_ClearsAllTypedColumns`
5. `Apply_NullOrWhitespace_ClearsAllTypedColumns`
6. `Apply_ClearsStaleValues_WhenNewValueDoesNotParse` (pre-set a typed column, apply text, assert it is null)

**Verification:** build + `dotnet test` green; `dotnet ef database update --project WEB` applies cleanly; inspect the generated migration to confirm the backfill SQL and filtered index are present.

**Out of scope:** wiring client-generated ids through the sync API, Hangfire, audit-writing middleware/services (the entity only).

## Task 4: Form→indicator ledger schema

**Deliverable:** `IndicatorContribution`, `FormIndicatorMapping`, `QuestionIndicatorLink` + enums, migration `AddFormIndicatorLedger` applied locally. **Schema only — no engine.**

**Files**

- `WEB/Models/FormIndicatorLedger.cs` — new entities + enums.
- `WEB/Models/ApplicationDBContext.FormIndicatorLedger.cs` — new partial, call added after `ConfigureAuditTypedAnswersModel`.
- `WEB/Migrations/*` — `dotnet ef migrations add AddFormIndicatorLedger --project WEB`.
- `WEB.Tests/FormIndicatorLedgerModelTests.cs` — new.

**Requirements**

`IndicatorContribution`: PK `IndicatorContributionId`; required `FormIndicatorMappingId`, `ResponseId`, `IndicatorId`, `EntityId`, `DateId`; nullable `DisaggregationOptionId`; `Value` `decimal(20,8)` required; nullable `Numerator`, `Denominator` `decimal(20,8)`; `ComputedAtUtc` required; `[Timestamp] RowVersion`.

`FormIndicatorMapping`: PK `FormIndicatorMappingId`; required `QuestionnaireId`, `IndicatorId`; nullable `SectionId`; `Aggregation` enum (`MappingAggregation { Sum, Count, CountDistinct, Average, Min, Max, MostRecent }`) default `Sum`; `ValueExpressionJson` required default `{"op":"sum","args":[]}`; `DateBindingJson` default `{"source":"response"}`; `EntityBindingJson` default `{"source":"response"}`; nullable `FilterJson`; nullable `DisaggregationGroupId`; `CountOnlySubmitted` bool default true; `IsActive` bool default true; `UpdatedAtUtc` required.

`QuestionIndicatorLink`: PK `QuestionIndicatorLinkId`; required `FormIndicatorMappingId`, `QuestionId`; `Role` enum `QuestionIndicatorRole { Value, Numerator, Denominator, Disaggregation, Filter, Date, Entity }`; nullable `DisaggregationOptionId`; nullable `ValueMapJson`; `SortOrder` int.

EF configuration:

- `IndicatorContribution`: decimal column types as specified; index `IX_Contributions_Aggregate` on `(IndicatorId, EntityId, DateId)`; unique filtered index `UX_Contributions_Response_Option` on `(FormIndicatorMappingId, ResponseId, DisaggregationOptionId)` filter `[DisaggregationOptionId] IS NOT NULL`; unique filtered index `UX_Contributions_Response_Total` on `(FormIndicatorMappingId, ResponseId)` filter `[DisaggregationOptionId] IS NULL`; `HasOne<Response>().WithMany().HasForeignKey(ResponseId)`; `HasOne<FormIndicatorMapping>().WithMany().HasForeignKey(FormIndicatorMappingId)`.
- `FormIndicatorMapping`: unique filtered index `UX_FormIndicatorMappings_Form_Indicator` on `(QuestionnaireId, IndicatorId)` filter `[SectionId] IS NULL`; unique filtered index `UX_FormIndicatorMappings_Form_Section_Indicator` on `(QuestionnaireId, IndicatorId, SectionId)` filter `[SectionId] IS NOT NULL`; `HasOne<Questionnaire>().WithMany().HasForeignKey(QuestionnaireId)`; `HasOne<Indicator>().WithMany().HasForeignKey(IndicatorId)`.
- `QuestionIndicatorLink`: navigation `Mapping` ← `FormIndicatorMapping.QuestionLinks`; `HasOne<Question>().WithMany().HasForeignKey(QuestionId)`; index `IX_QuestionIndicatorLinks_Mapping` on `(FormIndicatorMappingId, Role)`.

**Tests** (`FormIndicatorLedgerModelTests`)

1. `Contribution_UniqueIndexes_AreFilteredAndUnique` — both index names, `IsUnique`, exact filters.
2. `Contribution_AggregateIndex_Exists` — `(IndicatorId, EntityId, DateId)`.
3. `Mapping_FormIndicatorIndex_FiltersNullSection` / `Mapping_FormSectionIndicatorIndex_FiltersNonNullSection`.
4. `QuestionIndicatorLink_MappingNavigation_IsConfigured` — `FormIndicatorMapping.QuestionLinks` has an inverse FK to `QuestionIndicatorLink.FormIndicatorMappingId`.
5. `Enums_HaveExpectedMembers` — exact member sets and order for `MappingAggregation` and `QuestionIndicatorRole` (guards future renumbering).

**Verification:** build + tests green; `dotnet ef database update --project WEB` applies cleanly; migration is additive only (no `DropColumn`/`DropTable`).

**Out of scope:** expression evaluator, engine, controller endpoints, admin UI.

## Task 5: Read-only logframe tree service and API

**Deliverable:** `LogFrameTreeService` + `GET /api/v1/logframes/{logFrameId}/tree`, returning a flat ordered node list, with ETag, plus tests.

**Files**

- `WEB/Code/LogFrameTreeService.cs` — new service + DTO records.
- `WEB/Controllers/LogFramesV2Controller.cs` — new controller.
- `WEB/Program.cs` — register `LogFrameTreeService` (scoped) if the project requires explicit registration; check whether the repo registers services explicitly and follow that convention.
- `WEB.Tests/LogFrameTreeServiceTests.cs` — new.

**Requirements**

1. DTO shapes (records are fine):
   - `LogFrameNodeDto(Guid Id, Guid? ParentId, string Code, string Title, string Level, int Depth, int SortOrder, string RowVersion, int IndicatorCount)`
   - `LogFrameTreeDto(Guid LogFrameId, string Name, IReadOnlyList<LogFrameNodeDto> Nodes)`
2. `LogFrameTreeService.LoadTreeAsync(Guid logFrameId, CancellationToken ct)`:
   - returns `null` when the logframe does not exist;
   - selects only `IsActive` nodes for that logframe, ordered by `Depth`, then `SortOrder`;
   - `Level` = the node's `LogFrameLevel.Name`;
   - `IndicatorCount` = count of `LogFrameNodeIndicator` rows (single query projection — no N+1);
   - `RowVersion` = `Convert.ToBase64String(node.RowVersion)`, `""` when null;
   - uses `AsNoTracking()`.
3. `LogFramesV2Controller : BaseApiController` with the standard constructor `(IDbContextFactory<ApplicationDbContext> dbFactory, UserManager<User> userManager, AppSettings appSettings)`. Route: `[ApiController, Route("api/v1/logframes")]`. Action `[HttpGet("{logFrameId:guid}/tree")]`, returns `NotFound()` for a missing logframe, else `Ok(dto)` and sets the response `ETag` header to `"<logFrameId:N>"` (quoted).
4. Authorization: inherit whatever `BaseApiController` already enforces; do not add new permission logic in Phase 1.

**Tests** (`LogFrameTreeServiceTests`, EF InMemory)

1. `LoadTree_ReturnsNull_ForUnknownLogFrame`
2. `LoadTree_OrdersByDepthThenSortOrder`
3. `LoadTree_ProjectsParentAndLevelName`
4. `LoadTree_CountsIndicatorsPerNode` (two link rows on one node)
5. `LoadTree_ExcludesInactiveNodes`
6. `LoadTree_IncludesRootNodesWithoutParent`

Seeding note: `ApplicationDbContext.AutoDetectChangesEnabled` is false — set `db.ChangeTracker.AutoDetectChangesEnabled = true` in tests or attach entities explicitly before `SaveChangesAsync`.

**Verification:** build + tests green. Report the exact command used to start nothing server-side — verification is by tests and build only.

**Out of scope:** writes, reorder endpoint, node CRUD, permission filtering, caching, Angular screen.

## Task 6: Logframe v2 backfill and reconciliation

**Deliverable:** `LogFrameBackfillService` that converts existing flat `LogFrameRow` data into `LogFrameNode` rows idempotently, plus a reconciliation report and a unit-tested guarantee that a second run is a no-op.

**Files**

- `WEB/Code/LogFrameBackfillService.cs` — new service + result records.
- `WEB.Tests/LogFrameBackfillServiceTests.cs` — new.

**Requirements**

1. Level vocabulary, created once per logframe if missing, keyed by `(LogFrameId, Rank)`: rank 1 `Goals`, rank 2 `Outcomes`, rank 3 `Outputs`, rank 4 `Activities`; `PluralName` equals `Name`; `IsIndicatorLevel = true`; `IsActive = true`.
2. Row → type mapping: `LogFrameRowType.Goals`→rank 1, `Outcomes`→2, `Outputs`→3, `Activities`→4.
3. Node creation rules per `LogFrameRow` ordered by `RowNumber`:
   - `Code = ""` (never synthesize from text — keeps the filtered unique code index trivially safe);
   - `Title = row.Description` trimmed to 500 chars (empty is allowed);
   - `Description = row.Description`, `MeansOfVerification = row.MeansOfVerification`, `RisksAndAssumptions = row.RisksAndAssumptions`;
   - `SortOrder = row.RowNumber * 1000`, `Depth = parentDepth + 1`;
   - parent = the most recent previously processed node whose rank is lower-numbered (i.e. closer to Goals) than the current row's rank; when none exists the node is a root (`ParentNodeId = null`, `Depth = 0`);
   - `LegacyLogFrameRowId = row.LogFrameRowId`, `CustomFieldsJson = "{}"`.
4. Idempotency: a row whose `LogFrameRowId` already appears in `LogFrameNodes.LegacyLogFrameRowId` (for that logframe) is skipped; a logframe with no v2 nodes yet is processed in full. Running twice produces zero new rows.
5. `ReconcileAsync(Guid logFrameId, CancellationToken)` returns a report record: `Rows`, `Nodes`, `MissingNodes` (rows with no node), `OrphanNodes` (nodes whose `LegacyLogFrameRowId` no longer exists), `DuplicateCodes` (always 0 today but computed), `MaxDepth`.
6. Both methods are transactional per logframe (`SaveChangesAsync` once at the end) and log nothing user-facing; no HTTP endpoint in this task.

**Tests** (`LogFrameBackfillServiceTests`, EF InMemory)

1. `Backfill_CreatesFourLevels_WithExpectedRanks`
2. `Backfill_NestsRows_UnderNearestHigherRank` — Goals, Activities, Outputs, Activities produces goal→output→activity nesting correctly ordered by `RowNumber`.
3. `Backfill_SetsDepthAndSortOrder` — `SortOrder == RowNumber * 1000`, depth chain 0/1/2.
4. `Backfill_SetsLegacyLogFrameRowId_ForEveryRow`
5. `Backfill_IsIdempotent_OnSecondRun` — node count unchanged, no duplicate levels.
6. `Backfill_LeavesEmptyLogFrame_Untouched`
7. `Backfill_TruncatesTitleTo500Chars`
8. `Reconcile_ReportsMissingAndOrphanNodes`

**Verification:** build + `dotnet test` green.

**Out of scope:** invoking the backfill against production, API endpoints, admin UI, dual-read switching, `LogFrameRow` deletion.

## Task 7: Read-only logframe TreeTable screen behind a feature flag

**Deliverable:** an Angular feature route `/logframe-v2/:logFrameId` rendering the tree from Task 5 with PrimeNG `p-treetable`, visible only when the flag is on; existing screens untouched.

**Files**

- `WEB/ClientApp/src/environments/environment.ts` — `logFrameV2: true`.
- `WEB/ClientApp/src/environments/environment.prod.ts` — `logFrameV2: false`.
- `WEB/ClientApp/src/app/logframe-v2/logframe-v2.module.ts` — new NgModule (`standalone: false` components, matching repo conventions).
- `WEB/ClientApp/src/app/logframe-v2/logframe-v2.routes.ts` — lazy `loadChildren` route guarded by the flag (a `canMatch` guard returning `environment.logFrameV2`).
- `WEB/ClientApp/src/app/app.routes.ts` — register the lazy route.
- `WEB/ClientApp/src/app/logframe-v2/logframe-tree.service.ts` — HTTP + signals store.
- `WEB/ClientApp/src/app/logframe-v2/logframe-tree.component.ts` / `.html` — new.
- `WEB/ClientApp/src/app/logframe-v2/logframe-tree.component.spec.ts` — **optional**, only if it can run; do not add a test harness.
- Navigation: add a sidebar/nav entry rendered only when `environment.logFrameV2` is true. Find the existing nav definition (grep for a nav/menu component) and gate it with `*ngIf`.

**Requirements**

1. Service `LogFrameTreeService` (Angular): signal store with `tree`, `loading`, `error`, `nodes`, `roots` computed to PrimeNG `TreeNode<LogFrameNodeDto>[]` by a pure `buildTree` function; `load(logFrameId)` calls `GET /api/v1/logframes/{id}/tree` through the existing HTTP setup (base URL comes from the existing environment/interceptor — do not hardcode hosts). Types mirror the API DTO; `status`/`achievement` are optional and unused in this pass.
2. Component: `p-treetable` with `dataKey="id"`, `[scrollable]="true"`, `scrollHeight="flex"`, expand/collapse, columns Code / Level / Indicators, empty message, and a loading skeleton or overlay. Read-only: no edit, no drag-drop, no reorder.
3. Error state renders the `error` signal text; unknown logframe (404) shows a not-found message.
4. Lazy route keeps the new screen out of the initial bundle. Production build must not exceed the JS budgets; if it does, report measured sizes instead of changing budgets.
5. Do not modify existing logframe components, routes other than adding the new one, or shared styles beyond what the new component needs (component-scoped styles only).

**Verification**

- `cd WEB/ClientApp; npm run build:dev` succeeds.
- `npm run build` succeeds; record initial-bundle size.
- `git grep -n "logFrameV2" WEB/ClientApp/src/environments` shows both files.
- No changes to files under `WEB/ClientApp/src/app/logframe/` (the existing screen).

**Out of scope:** editing, drag-drop reorder, evaluation matrix, charts, filters, i18n, mobile layout work, converting the existing logframe screen.
