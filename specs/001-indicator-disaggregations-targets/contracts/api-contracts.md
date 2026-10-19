# API Contracts: Disaggregations & Target Variance Tracking

**Feature**: Multi-Dimensional Indicator Disaggregations & Target Variance Tracking  
**Feature Directory**: `specs/001-indicator-disaggregations-targets`  
**Date**: 2026-10-19  

## 1. Disaggregation Groups API

### 1.1 List Disaggregation Groups
* **GET** `/api/disaggregationgroups`
* **Response** `200 OK`:
```json
[
  {
    "disaggregationGroupId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Sex",
    "description": "Standard gender disaggregation",
    "isActive": true,
    "options": [
      {
        "disaggregationOptionId": "7b1fa2e1-4567-4890-a1b2-3c4d5e6f7a8b",
        "name": "Female",
        "code": "F",
        "sortOrder": 1
      },
      {
        "disaggregationOptionId": "8c2fb3f2-5678-4901-b2c3-4d5e6f7a8b9c",
        "name": "Male",
        "code": "M",
        "sortOrder": 2
      }
    ]
  }
]
```

### 1.2 Create Disaggregation Group
* **POST** `/api/disaggregationgroups`
* **Request Body**:
```json
{
  "name": "Age Category",
  "description": "USAID standard age brackets",
  "options": [
    { "name": "< 5 Years", "code": "U5", "sortOrder": 1 },
    { "name": "5 - 17 Years", "code": "5_17", "sortOrder": 2 },
    { "name": "18+ Years", "code": "18P", "sortOrder": 3 }
  ]
}
```
* **Response** `201 Created`

---

## 2. Indicator Targets API

### 2.1 Get Indicator Targets
* **GET** `/api/indicators/{indicatorId}/targets`
* **Response** `200 OK`:
```json
[
  {
    "indicatorTargetId": "4a85f64-5717-4562-b3fc-2c963f66afa6",
    "indicatorId": "1a2b3c4d-5717-4562-b3fc-2c963f66afa6",
    "indicatorDateId": "2b3c4d5e-5717-4562-b3fc-2c963f66afa6",
    "periodName": "2026 Q1",
    "entityId": null,
    "targetValue": 1000.0,
    "baselineValue": 200.0,
    "notes": "Target for Q1 rollout"
  }
]
```

### 2.2 Create or Upsert Indicator Target
* **POST** `/api/indicators/{indicatorId}/targets`
* **Request Body**:
```json
{
  "indicatorDateId": "2b3c4d5e-5717-4562-b3fc-2c963f66afa6",
  "entityId": null,
  "targetValue": 1200.0,
  "baselineValue": 200.0,
  "notes": "Revised target based on donor funding"
}
```
* **Response** `200 OK`

---

## 3. Indicator Variance & Progress Analytics API

### 3.1 Get Indicator Milestone Variance
* **GET** `/api/indicators/{indicatorId}/variance?entityId={optionalEntityId}`
* **Response** `200 OK`:
```json
{
  "indicatorId": "1a2b3c4d-5717-4562-b3fc-2c963f66afa6",
  "indicatorName": "Number of Farmers Trained",
  "direction": "HigherIsBetter",
  "periods": [
    {
      "indicatorDateId": "2b3c4d5e-5717-4562-b3fc-2c963f66afa6",
      "periodName": "2026 Q1",
      "targetValue": 1000.0,
      "actualValue": 850.0,
      "percentageAchievement": 85.0,
      "statusBadge": "OnTrack",
      "disaggregations": [
        { "optionName": "Female", "code": "F", "value": 500.0 },
        { "optionName": "Male", "code": "M", "value": 350.0 }
      ]
    }
  ]
}
```

---

## 4. Enhanced Data Entry API (Non-Breaking Additive Extension)

### 4.1 Save Datum with Disaggregations
* **POST** `/api/data`
* **Request Body**:
```json
{
  "indicatorId": "1a2b3c4d-5717-4562-b3fc-2c963f66afa6",
  "indicatorDateId": "2b3c4d5e-5717-4562-b3fc-2c963f66afa6",
  "entityId": "5c6d7e8f-5717-4562-b3fc-2c963f66afa6",
  "value": 100.0,
  "disaggregationData": [
    { "disaggregationOptionId": "7b1fa2e1-4567-4890-a1b2-3c4d5e6f7a8b", "value": 60.0 },
    { "disaggregationOptionId": "8c2fb3f2-5678-4901-b2c3-4d5e6f7a8b9c", "value": 40.0 }
  ]
}
```
* **Validation Rules**:
  - If `disaggregationData` is provided, `Sum(disaggregationData.value)` must equal `value` (or `value` will be auto-populated if omitted).
  - If sum does not equal `value`, returns `422 Unprocessable Entity`:
    ```json
    {
      "error": "DisaggregationParityError",
      "message": "The sum of disaggregated values (100.0) does not match the aggregate total (120.0)."
    }
    ```
