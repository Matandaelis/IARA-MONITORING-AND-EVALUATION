# Tasks: Multi-Dimensional Indicator Disaggregations & Target Variance Tracking

**Feature Branch**: `001-indicator-disaggregations-and`  
**Feature Directory**: `specs/001-indicator-disaggregations-targets`  
**Date**: 2026-10-19  
**Status**: Ready for Implementation  

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Shared enums and Data Transfer Objects (DTOs) for disaggregations and milestone targets.

- [X] T001 Add `IndicatorDirection` enum (`HigherIsBetter = 0`, `LowerIsBetter = 1`) in `WEB/Models/Enums.cs`
- [X] T002 [P] Create DTOs `DisaggregationGroupDTO`, `DisaggregationOptionDTO`, `DisaggregationDatumDTO` in `WEB/Models/DTOs/DisaggregationDTOs.cs`
- [X] T003 [P] Create DTOs `IndicatorTargetDTO` and `IndicatorVarianceDTO` in `WEB/Models/DTOs/IndicatorTargetDTO.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core entity models, DbContext mappings, and calculation engines that MUST be completed before any user story endpoint can be implemented.

- [X] T004 [P] Create `DisaggregationGroup` entity with Name, Description, IsActive, CreatedAt, and Options navigation in `WEB/Models/DisaggregationGroup.cs`
- [X] T005 [P] Create `DisaggregationOption` entity with DisaggregationGroupId, Name, Code, SortOrder in `WEB/Models/DisaggregationOption.cs`
- [X] T006 [P] Create `DisaggregationDatum` entity with DatumId, DisaggregationOptionId, Value in `WEB/Models/DisaggregationDatum.cs`
- [X] T007 [P] Create `IndicatorTarget` entity with IndicatorId, IndicatorDateId, EntityId, TargetValue, BaselineValue, Notes, UpdatedAt in `WEB/Models/IndicatorTarget.cs`
- [X] T008 Update `Indicator.cs` with nullable `DisaggregationGroupId`, `Direction`, and `IndicatorTargets` navigation in `WEB/Models/Indicator.cs`
- [X] T009 Update `Datum.cs` with `DisaggregationData` navigation collection in `WEB/Models/Datum.cs`
- [X] T010 Register new DbSets (`DisaggregationGroups`, `DisaggregationOptions`, `DisaggregationData`, `IndicatorTargets`) and configure indexes and relationships in `WEB/Models/ApplicationDBContext.cs`
- [X] T011 Implement `VarianceEngine` calculation service with standard, inverted, and zero-target safeguards in `WEB/Code/VarianceEngine.cs`

**Checkpoint**: Foundational entities and variance engine ready — User stories can now be implemented.

---

## Phase 3: User Story 1 - Target Setting & Automated Milestone Progress Tracking (Priority: P1) 🎯 MVP

**Goal**: Allow project managers to define scheduled indicator targets and compute automated variance scoring (% achievement and performance badges) upon data entry.

**Independent Test**: Create an indicator target, enter actual data, and verify that the variance endpoint returns correct % achievement and performance status badge.

- [X] T012 [P] [US1] Create unit tests covering standard variance, inverted variance, zero-target edge case, and threshold status badge classification in `WEB.Tests/VarianceEngineTests.cs`
- [X] T013 [US1] Implement `IndicatorTargetsController` with endpoints `GET /api/indicators/{indicatorId}/targets`, `POST /api/indicators/{indicatorId}/targets`, and `GET /api/indicators/{indicatorId}/variance` in `WEB/Controllers/IndicatorTargetsController.cs`

**Checkpoint**: User Story 1 is fully functional and testable as the core MVP.

---

## Phase 4: User Story 2 - Multi-Dimensional Disaggregation Category Setup (Priority: P1)

**Goal**: Allow M&E specialists to configure reusable disaggregation groups (e.g. Sex, Age Group) and attach them to indicators.

**Independent Test**: Create a disaggregation group with options, attach to an indicator, and verify metadata retrieval.

- [X] T014 [P] [US2] Implement `DisaggregationGroupsController` with CRUD endpoints for groups and child options in `WEB/Controllers/DisaggregationGroupsController.cs`
- [X] T015 [US2] Update `IndicatorsController` to bind and return `DisaggregationGroupId` and `Direction` in `WEB/Controllers/IndicatorsController.cs`

**Checkpoint**: User Stories 1 and 2 operate cleanly together.

---

## Phase 5: User Story 3 - Disaggregated Data Entry & Mathematical Parity Validation (Priority: P2)

**Goal**: Field data entry captures breakdown values with automated auto-summing and strict parity enforcement (`Sum(Disaggregates) == Value`).

**Independent Test**: Submit a datum with disaggregated values, verify sum consistency, and verify 422 error on parity mismatch.

- [X] T016 [P] [US3] Create unit tests verifying disaggregation parity validation, auto-summing, and tolerance rules in `WEB.Tests/DisaggregationParityTests.cs`
- [X] T017 [US3] Extend `DataController.cs` to accept `disaggregationData`, enforce mathematical parity, auto-sum if parent value is omitted, and save records in `WEB/Controllers/DataController.cs`

**Checkpoint**: Disaggregated data collection is live with data integrity guaranteed.

---

## Phase 6: User Story 4 - Disaggregation & Milestone Variance Analytics Dashboard (Priority: P3)

**Goal**: Provide tabular export and analytics views showing targets, actuals, variance, and demographic breakdowns.

- [X] T018 [P] [US4] Extend Excel and CSV export methods in `WEB/Controllers/DownloadsController.cs` to include target values and disaggregated option columns when present
- [X] T019 [US4] Add target and variance summary metrics to indicator dashboard details in `WEB/Controllers/IndicatorsController.cs`

---

## Phase 7: Polish & Verification

**Purpose**: End-to-end test execution and backward compatibility verification.

- [X] T020 Run test suite `dotnet test WEB.Tests/WEB.Tests.csproj` and ensure 100% pass rate
- [X] T021 Validate zero regressions on existing scalar indicators and public survey responses per `quickstart.md`

---

## Dependencies & Execution Order

- **Phase 1 (Setup)**: Can start immediately.
- **Phase 2 (Foundational)**: Depends on Phase 1; blocks all User Stories.
- **Phase 3 (US1 - MVP)**: Can start after Phase 2; provides standalone target milestone and variance functionality.
- **Phase 4 (US2)**: Can run after Phase 2; independent configuration of disaggregation groups.
- **Phase 5 (US3)**: Depends on US2 (disaggregation groups exist); completes field data entry parity.
- **Phase 6 (US4)**: Depends on US1, US2, and US3; integrates reporting and exports.
- **Phase 7 (Polish)**: Final verification across all stories.
