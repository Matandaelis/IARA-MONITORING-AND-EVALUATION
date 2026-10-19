# Implementation Plan: Multi-Dimensional Indicator Disaggregations & Target Variance Tracking

**Branch**: `001-indicator-disaggregations-and` | **Date**: 2026-10-19 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/001-indicator-disaggregations-targets/spec.md`

## Summary

This feature implements two core enterprise M&E capabilities inspired by international benchmarks (DHIS2 and DevResults):
1. **Multi-Dimensional Indicator Disaggregations**: Allows indicators to optionally associate with reusable disaggregation groups (e.g., Sex, Age Group, Geography) and capture sub-population breakdowns with automated mathematical parity validation.
2. **Target Milestones & Variance Tracking**: Allows project managers to establish scheduled targets (monthly, quarterly, annual) and automatically compute percentage achievements with performance badges (On Track, Underperforming, Exceeding) upon actual data submission.

**Strict Backward Compatibility**: This implementation is completely additive. Existing scalar indicators without disaggregations or targets continue to operate with 100% backward compatibility and zero data changes.

---

## Technical Context

**Language/Version**: C# / .NET 10.0 (Backend), TypeScript 5.6+ / Angular 19+ (Frontend)  
**Primary Dependencies**: Entity Framework Core 10, OpenIddict 5.8+, xUnit, ECharts / D3  
**Storage**: Microsoft SQL Server 2022/2025 (`ApplicationDbContext`)  
**Testing**: xUnit (`WEB.Tests`) with AAA unit test patterns  
**Target Platform**: Linux / Windows Docker Container (`monic-app`)  
**Project Type**: Full-stack Web Application (ASP.NET Core Web API + Angular SPA)  
**Performance Goals**: < 50ms variance scoring calculation, instantaneous UI aggregation  
**Constraints**: Strict Zero Data Loss (Constitution Principle I), Mathematical Parity (Constitution Principle II), Non-breaking API contracts  
**Scale/Scope**: 64 existing tables, non-destructive addition of 4 new entity models  

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] **Principle I: Zero Data Loss & Migration Integrity**: All database modifications use additive nullable foreign keys and new tables (`DisaggregationGroup`, `DisaggregationOption`, `DisaggregationDatum`, `IndicatorTarget`). No existing columns or tables are deleted or renamed. Migration will be fully reversible (`Up()` and `Down()`).
- [x] **Principle II: M&E Calculation Determinism & Dimensional Parity**: Strict mathematical parity enforced (`Sum(Disaggregates) == Parent.Value`). Variance calculations handle boundary conditions (zero target, inverted directionality) deterministically without divide-by-zero errors.
- [x] **Principle III: Immutable Audit Trail & Multi-Stage Governance**: Disaggregated data inherits parent `Datum` 3-stage review lifecycle (`Submit` ➔ `Verify` ➔ `Approve`).
- [x] **Principle IV: Security Boundary & RBAC**: Target configuration restricted to `Administrator` and `Manager` roles; field workers enter disaggregated numbers.
- [x] **Principle V: Single-Container DevOps Resilience**: No extra services or background daemons required; runs cleanly inside the standard `monic-app` container.
- [x] **Principle VI: Test-First Verification Gate**: Comprehensive unit tests added to `WEB.Tests` validating variance calculations and parity logic before release.

---

## Project Structure

### Documentation (this feature)

```text
specs/001-indicator-disaggregations-targets/
├── spec.md              # Feature specification
├── plan.md              # This implementation plan
├── research.md          # Phase 0 research & technical decisions
├── data-model.md        # Phase 1 data model specification
├── contracts/           # Phase 1 API contract schemas
│   └── api-contracts.md # REST endpoint contracts
├── quickstart.md        # Phase 1 verification and testing guide
├── checklists/
│   └── requirements.md  # Specification quality checklist
└── tasks.md             # Phase 2 output (/speckit-tasks command)
```

### Source Code Layout

```text
WEB/
├── Models/
│   ├── DisaggregationGroup.cs       # Category header entity
│   ├── DisaggregationOption.cs      # Category bucket option entity
│   ├── DisaggregationDatum.cs       # Recorded child breakdown observation
│   ├── IndicatorTarget.cs           # Target milestone entity
│   ├── Indicator.cs                 # Updated with DisaggregationGroupId & Direction
│   ├── Datum.cs                     # Updated with DisaggregationData collection
│   ├── Enums.cs                     # Added IndicatorDirection enum
│   └── ApplicationDBContext.cs      # DbSets and EF Core configurations
├── Models/DTOs/
│   ├── DisaggregationGroupDTO.cs    # DTO for group configuration
│   ├── IndicatorTargetDTO.cs        # DTO for milestone targets
│   └── IndicatorVarianceDTO.cs      # DTO for variance performance scoring
├── Code/
│   └── VarianceEngine.cs            # Variance computation and threshold categorization
├── Controllers/
│   ├── DisaggregationGroupsController.cs # CRUD for disaggregation groups & options
│   ├── IndicatorTargetsController.cs     # Endpoints for targets & variance
│   └── DataController.cs                 # Extended for disaggregated data entry parity
WEB.Tests/
├── VarianceEngineTests.cs           # Unit tests for variance and threshold bands
└── DisaggregationParityTests.cs     # Unit tests for parity validation and auto-summing
```

---

## Complexity Tracking

> *No constitutional violations. Design is strictly additive, normalized, and follows established MONIC conventions.*
