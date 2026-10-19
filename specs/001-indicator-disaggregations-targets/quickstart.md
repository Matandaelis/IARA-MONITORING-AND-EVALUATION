# Quickstart Validation Guide: Disaggregations & Target Variance Tracking

**Feature**: Multi-Dimensional Indicator Disaggregations & Target Variance Tracking  
**Feature Directory**: `specs/001-indicator-disaggregations-targets`  
**Date**: 2026-10-19  

## 1. Prerequisites
- .NET 10 SDK installed
- Local SQL Server or LocalDB instance configured (or in-memory test database)
- Completed EF Core migration: `AddDisaggregationsAndTargetTracking`

---

## 2. Test Execution & Verification

### Scenario A: Unit Test Verification of Parity & Variance
Run the automated test suite targeting the variance engine and disaggregation parity:
```bash
dotnet test WEB.Tests/WEB.Tests.csproj --filter "FullyQualifiedName~DisaggregationOrVariance"
```
**Expected Outcome**:
- `VarianceEngineTests`: Tests cover standard variance, inverted variance (lower is better), zero target edge case, and status badge thresholds.
- `DisaggregationParityTests`: Tests verify mathematical parity enforcement, auto-summing of child buckets, and rejection of conflicting totals (422 Unprocessable Entity).

### Scenario B: API Integration Verification

#### 1. Create a Disaggregation Group:
```bash
curl -X POST http://localhost:8080/api/disaggregationgroups \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Gender",
    "options": [
      { "name": "Female", "code": "F", "sortOrder": 1 },
      { "name": "Male", "code": "M", "sortOrder": 2 }
    ]
  }'
```
**Expected Outcome**: `201 Created` with new group ID.

#### 2. Set an Indicator Target Milestone:
```bash
curl -X POST http://localhost:8080/api/indicators/{INDICATOR_ID}/targets \
  -H "Content-Type: application/json" \
  -d '{
    "indicatorDateId": "{PERIOD_ID}",
    "targetValue": 500,
    "baselineValue": 100
  }'
```
**Expected Outcome**: `200 OK` with saved target.

#### 3. Submit Disaggregated Data Entry:
```bash
curl -X POST http://localhost:8080/api/data \
  -H "Content-Type: application/json" \
  -d '{
    "indicatorId": "{INDICATOR_ID}",
    "indicatorDateId": "{PERIOD_ID}",
    "entityId": "{ENTITY_ID}",
    "value": 450,
    "disaggregationData": [
      { "disaggregationOptionId": "{FEMALE_OPTION_ID}", "value": 250 },
      { "disaggregationOptionId": "{MALE_OPTION_ID}", "value": 200 }
    ]
  }'
```
**Expected Outcome**: `200 OK` (250 + 200 = 450, parity holds).

#### 4. Query Variance Progress:
```bash
curl http://localhost:8080/api/indicators/{INDICATOR_ID}/variance
```
**Expected Outcome**:
- `percentageAchievement`: `90.0`
- `statusBadge`: `"OnTrack"` (between 75% and 110%)
- Disaggregated breakdown listed cleanly.

---

## 3. UI Verification
1. Navigate to **Indicators** ➔ Edit Indicator ➔ Select Disaggregation Group: "Gender".
2. Open **Data Entry** for that indicator: Observe expandable sub-rows for Female and Male.
3. Observe live auto-summing into the Total field.
4. View **Indicator Dashboard**: Observe target progress bar with variance badge (`90% On Track`).
