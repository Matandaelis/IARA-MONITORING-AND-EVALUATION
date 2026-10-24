# Universal M&E System Completion Design Specification

**Date**: 2026-10-24  
**Status**: Approved (Architectural Path)  
**Target Platform**: MONIC (Monitoring & Evaluation Platform)  
**Codebase**: `C:/Users/Administrator/Downloads/MONICv2/MONICv.001`  
**Domain Standard Reference**: [tools4dev — How to Create a Monitoring and Evaluation (M&E) System](https://tools4dev.org/resources/how-to-create-an-monitoring-and-evaluation-system/)  
**Technology Stack**: ASP.NET Core (.NET 10 / C# 13), Entity Framework Core 10, SQL Server, Angular 19.2

---

## 1. Executive Summary & Architectural Goals

The objective of this design is to elevate MONIC from an indicator tracking tool into a **universally reusable, full-lifecycle Monitoring & Evaluation (M&E) platform** compliant with the [tools4dev framework](https://tools4dev.org/resources/how-to-create-an-monitoring-and-evaluation-system/) and international donor standards (USAID ADS 201, European Commission, World Bank, FCDO, and UN agencies).

### Key Architectural Pillars
1. **Universal Sector Reusability**: Accommodation of any project type (Health, Education, Agriculture, WASH, Climate/Environment, Economic Livelihoods, Governance, Humanitarian, or Infrastructure).
2. **Upgraded Legacy 4x4 LogFrame**: Retention and enhancement of the familiar 4-tier matrix (Goals, Outcomes, Outputs, Activities) with structured linkages to Risks & Assumptions, workplan Tasks, Budgets, and live indicator progress chips.
3. **End-to-End Automation**: Automated runtime contribution ledger connecting survey submissions directly to indicator calculations, accompanied by event-driven Corrective Action Plan (CAP) triggering on data approval.
4. **Field Usability & Offline Capability**: Modern field question types (GPS, Range-Validated Numbers, Dates, Likert Matrix, Digital Signatures) and offline PWA storage with background synchronization.
5. **Donor Compliance & Document Generation**: Complete M&E Plan generation in editable Word (.docx) and PDF formats, along with official IATI XML v2.03 and USAID/EU report exporters.

---

## 2. Universal Reusability Architecture

To ensure the platform is not restricted to any single sector or project model, five domain-agnostic abstractions are embedded into the core:

### 2.1 Configurable Results Vocabulary (LogFrame Aliases)
Projects can configure custom display labels for the 4-tier results hierarchy without breaking underlying data models or rollup logic:

| Terminology Standard | Level 4 (Highest) | Level 3 | Level 2 | Level 1 (Operational) |
| :--- | :--- | :--- | :--- | :--- |
| **Standard / ToC** | Impact / Goal | Outcomes | Outputs | Activities |
| **European Commission (EU)** | Overall Objective | Specific Objective | Expected Results | Activities |
| **USAID / RBM** | Goal | Strategic Objective (SO) | Intermediate Results (IR) | Outputs / Activities |
| **World Bank / DFID** | High-Level Impact | Project Development Objective | Key Deliverables | Tasks & Inputs |
| **Custom** | *Configurable* | *Configurable* | *Configurable* | *Configurable* |

### 2.2 Universal Project Metadata Taxonomy
Enrich `Project.cs` with standard M&E project attributes:
* `Sector`: Multi-sector classification (`Health`, `Education`, `Agriculture`, `WASH`, `Environment`, `Economic Growth`, `Governance`, `Humanitarian`, `Infrastructure`).
* `Dates`: `StartDate`, `EndDate`, `CurrentPhase` (Inception, Implementation, Evaluation, Closed).
* `Funding & Governance`: `DonorName`, `GrantCode`, `TotalBudget`, `Currency`.
* `Beneficiary Reach`: `TargetDirectBeneficiaries`, `TargetIndirectBeneficiaries`.
* `Workflow Mode`: Toggle between `Streamlined` (Entry $\to$ Approve) and `Rigorous` (Entry $\to$ Verify $\to$ Approve).

### 2.3 Opt-In Multi-Sector Indicator Template Packs
Pre-seeded, standardized indicator repositories importable with one click:
* **UN SDGs**: Core indicators covering SDGs 1–17.
* **Global Health (WHO / Global Fund)**: Maternal health, immunization, facility stock-outs.
* **Education & Child Protection (UNICEF)**: Gross enrollment, completion, literacy benchmarks.
* **Agriculture & Food Security (FAO / Feed the Future)**: Yield per hectare, post-harvest losses, adoption rates.
* **WASH**: Clean water access, sanitation point functionality.
* **Climate & Environment**: Carbon sequestration, reforested hectares.
* **Organizational Custom Packs**: Reusable across multiple projects within an organization.

---

## 3. Detailed Technical Architecture by Layer

```
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                              MONIC Technical Architecture                              │
├────────────────────────────────────────────────────────────────────────────────────────┤
│ Layer 4: Document Generation, Offline PWA & Integrations                               │
│ • M&E Plan Builder (OpenXML/QuestPDF) • IATI XML / USAID QPR • Offline PWA Dexie Sync │
├────────────────────────────────────────────────────────────────────────────────────────┤
│ Layer 3: UI & Interactive User Workflows                                               │
│ • 4x4 LogFrame Matrix • Disaggregated Target Grid • PIRS Sheet • CFM Feedback Portal  │
├────────────────────────────────────────────────────────────────────────────────────────┤
│ Layer 2: Core Automation Engines & Business Logic                                      │
│ • Form-to-Indicator Engine • Event-Driven CAP Watcher • DQA Spot-Check Audit Service   │
├────────────────────────────────────────────────────────────────────────────────────────┤
│ Layer 1: Data Architecture & Unified EF Core Migrations                                │
│ • Disaggregated Targets • PIRS Metadata • LogFrame Junctions • Question Types • CFM   │
└────────────────────────────────────────────────────────────────────────────────────────┘
```

---

### 3.1 Layer 1: Data Architecture & Unified EF Core Migrations

A single, consolidated EF Core migration will introduce all required schema additions:

#### 1. Disaggregated Targets & Baselines
* **Entity**: `IndicatorTarget.cs`
* **Changes**:
  * Add `public Guid? DisaggregationOptionId { get; set; }`
  * Add navigation property `[ForeignKey("DisaggregationOptionId")] public virtual DisaggregationOption DisaggregationOption { get; set; }`
  * Update composite unique index: `(IndicatorId, DateId, EntityId, DisaggregationOptionId)` to accommodate both aggregate targets (`DisaggregationOptionId == null`) and option-specific targets (e.g. Male, Female).

#### 2. Unified PIRS (Performance Indicator Reference Sheet) Attributes
* **Entity**: `Indicator.cs`
* **Changes**:
  * `public string OperationalDefinition { get; set; }`
  * `public string PurposeRationale { get; set; }`
  * `public string DataQualityProtocol { get; set; }`
  * `public string ReportingSchedule { get; set; }`
  * `public string CalculationFormulaNarrative { get; set; }`
  * `public Guid? NumeratorIndicatorId { get; set; }`
  * `public Guid? DenominatorIndicatorId { get; set; }`
  * Navigation properties for `NumeratorIndicator` and `DenominatorIndicator`.

#### 3. LogFrame Row Standard 4x4 Relational Junctions
* **Entity**: `LogFrameRow.cs`
* **Changes**:
  * Retain existing string fields (`Description`, `Indicators`, `MeansOfVerification`, `RisksAndAssumptions`) for backward compatibility.
  * Add junction table `LogFrameRowRiskAssumption`: `(LogFrameRowId, RiskAssumptionId)`.
  * Add junction table `LogFrameRowTask`: `(LogFrameRowId, TaskId)` for Activity rows.
  * Add junction table `LogFrameRowBudgetLine`: `(LogFrameRowId, BudgetLineId)` for Activity rows.

#### 4. Expanded Survey Question Types
* **Enum**: `QuestionType` in `Enums.cs`
* **Values**:
  * `Text = 0`, `OptionList = 1`, `Multiline = 2`, `Note = 3`, `Document = 4`
  * `Number = 5` (Numeric entry with min/max validation)
  * `Date = 6` (Calendar date selection)
  * `GPS = 7` (Latitude, Longitude, Altitude, Accuracy)
  * `Likert = 8` (1–5 agreement matrix)
  * `Signature = 9` (Digital canvas signature)
* **Entity Additions** on `Question.cs`: `MinValue`, `MaxValue`, `ValidationRegex`.

#### 5. Accountability to Affected Populations (CFM / Feedback)
* **New Entity**: `BeneficiaryFeedback.cs`
* **Properties**:
  * `Guid Id`, `Guid ProjectId`, `string TrackingCode`, `DateTime DateReceivedUtc`
  * `string Channel`, `string Category`, `string Severity`, `string Description`
  * `bool IsAnonymous`, `string ContactDetails`
  * `FeedbackStatus Status` (`Received`, `Investigating`, `Resolved`, `Closed`)
  * `string ResolutionNotes`, `DateTime? ResolvedAtUtc`, `Guid? AssignedToUserId`

---

### 3.2 Layer 2: Core Automation Engines & Business Logic

#### 1. Form-to-Indicator Runtime Engine (`FormIndicatorContributionEngine.cs`)
* Connects survey response submissions in `SurveysController.Submit` and bulk imports in `ImportProcessor.cs` to routine indicator monitoring:
  1. Matches active `FormIndicatorMapping` rules.
  2. Extracts and casts answers via `AnswerValueProjector`.
  3. Records immutable audit rows in `IndicatorContribution`.
  4. Aggregates values per `(IndicatorId, EntityId, DateId, DisaggregationOptionId)` using `Sum`, `Average`, `Count`, or `Latest`.
  5. Updates indicator `Data` rows.
  6. Invokes `Calculation.SaveAsync` to recompute downstream topological formulas.

#### 2. Event-Driven Automated CAP Watcher
* Integrated directly into `FormsController.ApproveDataEntry`.
* Upon formal data approval, detects any indicators where approved actuals deviate beyond threshold ($\pm 15\%$).
* Automatically instantiates a `CorrectiveActionPlan` record with a mandatory 7-business-day resolution SLA.
* Dispatches real-time alerts via `NotificationDispatcher`.

#### 3. Supervisory Spot-Check Audit Service (`DataQualityAuditService.cs`)
* Implements tools4dev Step 5 quality control spot-checks:
  * Computes discrepancy error rate: $\frac{|\text{Reported} - \text{Audited}|}{\text{Audited}} \times 100\%$.
  * Assigns automated reliability ratings: Grade A ($\le 5\%$), Grade B ($5.1\% - 10\%$), Grade C ($> 10\%$).

---

### 3.3 Layer 3: UI & Interactive User Workflows

#### 1. Upgraded 4x4 LogFrame Matrix Component
* Updates to `logframe.component.html` and `logframe.row.component.html`:
  * Dynamic level labels based on project vocabulary.
  * Interactive indicator chips with baseline, periodic target, latest actual, and RAG status.
  * Means of Verification auto-populated from linked indicator PIRS metadata.
  * Structured multi-select linking to the project’s `RiskAssumption` register with color-coded risk heat badges.
  * Activity rows linking to workplan Tasks (with Gantt % complete badges) and BudgetLines.

#### 2. Disaggregated Target Management Grid
* Integrated into indicator target editing screens:
  * User-friendly grid allowing target entry per demographic option (e.g. Male/Female, Age groups).
  * Real-time sum parity checking with visual feedback.

#### 3. Unified PIRS Card Component (`pirs-card.component.ts`)
* Publication-grade card rendering:
  * Overview, Operational Definition, Measurement Protocol, Data Quality Protocol, Baseline & Periodic Targets.
  * One-click "Print PIRS Card" and "Export PDF Card".

#### 4. Expanded Survey Collection Controls
* Updates to `survey.component.ts` and `question.edit.component.ts`:
  * Number inputs with client-side range validation.
  * Calendar datepicker.
  * One-click "Capture Current GPS" button with OpenStreetMap preview pin.
  * Responsive 5-point Likert matrix.
  * Touch/mouse signature pad for beneficiary consent.

#### 5. Beneficiary Feedback / CFM Portal (`feedback-registry.component.ts`)
* Comprehensive registry of community grievances and suggestions with SLA tracking and confidential case management.

---

### 3.4 Layer 4: Document Generation, Offline PWA & Integrations

#### 1. Automated M&E Plan Document Builder (`MePlanDocumentBuilder.cs`)
* Generates complete, donor-ready M&E Plan documents in Microsoft Word (.docx) and PDF formats.
* Compiles Project Overview, Theory of Change, 4x4 LogFrame, PIRS Sheets, Data Flow & RACI Matrix, Risk Management Matrix, Workplan Gantt, and Budget Allocations.

#### 2. Standard Donor Reporting Exporters (`DonorExportService.cs`)
* **IATI XML (v2.03)**: Fully compliant XML structure for international aid reporting.
* **USAID Quarterly Performance Report (QPR)**: Standard progress table with target variance and deviation justifications.
* **EU ROM Report**: Formatted proforma aligned with European Commission guidelines.

#### 3. Offline Mobile Field Collection (PWA & IndexedDB)
* Angular Service Worker caching application shell and questionnaires.
* Dexie.js / IndexedDB local staging of offline survey submissions.
* Background synchronization service automatically flushing records upon online reconnection.

#### 4. Humanitarian Webhook Ingestion (KoboToolbox & ODK)
* Inbound endpoint `POST api/ExternalPush/kobo/{questionnaireId}` in `ExternalPushController.cs`.
* Ingests external JSON payloads directly into the `FormIndicatorContributionEngine`.

---

## 4. Verification & Testing Strategy

1. **Unit & Integration Tests (`WEB.Tests`)**:
   * Verify all 156 existing unit tests continue to pass.
   * Add dedicated test suites:
     * `DisaggregatedTargetTests.cs`: Target-to-disaggregation mapping and parity validation.
     * `FormIndicatorContributionEngineTests.cs`: Ingestion, aggregation, and auto-calculation triggering.
     * `CorrectiveActionAutomationTests.cs`: Automatic CAP triggering upon data approval.
     * `MePlanDocumentBuilderTests.cs`: Structural integrity of generated Word and PDF documents.
2. **End-to-End Workflow Validation**:
   * Complete lifecycle execution: Project Creation $\to$ LogFrame 4x4 configuration $\to$ Survey response submission $\to$ Auto-contribution $\to$ Approval $\to$ CAP generation $\to$ M&E Plan document export.
