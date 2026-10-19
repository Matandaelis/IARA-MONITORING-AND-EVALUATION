# Phase 0: Research & Technical Decisions

**Feature**: Multi-Dimensional Indicator Disaggregations & Target Variance Tracking  
**Feature Directory**: `specs/001-indicator-disaggregations-targets`  
**Date**: 2026-10-19  

## 1. Schema Architecture for Disaggregations

### Decision
Implement an additive, normalized relational model with three new entities:
- `DisaggregationGroup`: The category header (e.g., "Sex", "Age Groups", "Location Type").
- `DisaggregationOption`: The discrete buckets (e.g., "Female", "Male", "Other").
- `DisaggregationDatum`: The numerical record binding a `Datum` (parent period observation) to a specific `DisaggregationOption`.
- Add an optional foreign key `DisaggregationGroupId` to `Indicator` (nullable `Guid?`).

### Rationale
- **Zero Breaking Changes**: Existing scalar indicators have `DisaggregationGroupId = null`. All existing EF Core queries on `Datum` continue to return `Datum.Value` as the aggregate without modification.
- **Normalized & Scalable**: Matches international M&E standards (DHIS2 category options and DevResults disaggregation sets) without denormalizing or creating brittle JSON-string columns.
- **Aggregation Agility**: Allows simple SQL queries (`SUM(DisaggregationDatum.Value)`) while caching the verified sum in the parent `Datum.Value`.

### Alternatives Considered
- *Store disaggregations as a JSON column in `Datum`*: Rejected because it prevents relational integrity, indexing, foreign keys, and makes SQL reporting/ETL significantly more complex.
- *Create child indicators for each option*: Rejected because it multiplies the indicator catalog exponentially (e.g., 50 indicators x 4 age brackets x 2 sexes = 400 indicators to manage).

---

## 2. Target Milestone & Baseline Architecture

### Decision
Create a dedicated `IndicatorTarget` entity:
- `IndicatorTargetId`: Guid (Primary Key)
- `IndicatorId`: Guid (Foreign Key to `Indicator`)
- `IndicatorDateId`: Guid (Foreign Key to `IndicatorDate` period)
- `EntityId`: Guid? (Optional Foreign Key to `Entity` for location-specific targets)
- `TargetValue`: decimal
- `BaselineValue`: decimal?
- `Notes`: string?

### Rationale
- Decouples target milestones from routine field data collection (`Datum`).
- Allows project managers to plan multi-year/quarterly targets in advance.
- Enables direct comparisons in SQL queries: `LEFT JOIN IndicatorTargets ON ...`.

### Alternatives Considered
- *Add `TargetValue` column directly to `Datum`*: Rejected because a target must exist *before* actual data is submitted, and a period may have a target even if no data has been submitted yet.

---

## 3. Variance & Achievement Calculation Engine

### Decision
Encapsulate variance logic inside a dedicated C# helper service (`VarianceEngine.cs` in `Monic.Web.Code`):
```csharp
public record VarianceResult(
    decimal? PercentageAchievement,
    string StatusBadge, // "Underperforming", "OnTrack", "Exceeding", "NotApplicable"
    decimal? AbsoluteVariance
);
```

### Formulae:
1. **Standard (Higher is Better)**:
   $$\text{PercentageAchievement} = \left(\frac{\text{Actual}}{\text{Target}}\right) \times 100$$
2. **Inverted (Lower is Better - e.g. Disease Incidence, Dropouts)**:
   $$\text{PercentageAchievement} = 100 + \left(\frac{\text{Target} - \text{Actual}}{\text{Target}}\right) \times 100$$
3. **Zero Target Boundary**:
   If $\text{Target} == 0$:
   - If $\text{Actual} == 0 \implies 100\%$ achievement ("On Track")
   - If $\text{Actual} > 0 \implies 0\%$ achievement ("Underperforming")

### Status Thresholds (aligned with constitution):
- `< 75%`: **Underperforming** (Warning Alert)
- `75% - 110%`: **On Track** (Normal / Healthy)
- `> 110%`: **Exceeding** (Overperforming)

---

## 4. Mathematical Parity & Validation Gate

### Decision
Enforce parity at two tiers:
1. **Frontend / API tier**: When submitting a `Datum` with `DisaggregationData`:
   - If user leaves `Datum.Value` empty, auto-sum `DisaggregationData.Sum(d => d.Value)`.
   - If user explicitly supplies `Datum.Value`, verify `Math.Abs(Datum.Value - DisaggregationData.Sum(d => d.Value)) < 0.0001m`. If unequal, return HTTP 422 Unprocessable Entity with descriptive error message.
2. **Deterministic Rule Engine**:
   - Add unit test validation in `WEB.Tests` verifying parity enforcement and boundary handling.

---

## 5. Backward Compatibility Guarantee

### Verification Checklist:
- [x] All new database foreign keys are nullable or target new tables.
- [x] Existing API endpoints (`/api/data`, `/api/indicators`) retain identical response structures; new disaggregation fields are additive.
- [x] Existing calculation engine (`Calculation.cs`) continues to resolve scalar indicator tokens without change.
- [x] Export routines (Excel, CSV) include disaggregations only when present.
