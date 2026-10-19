# Feature Specification: Multi-Dimensional Indicator Disaggregations & Target Variance Tracking

**Feature Branch**: `001-indicator-disaggregations-and`

**Created**: 2026-10-19

**Status**: Draft

**Input**: User description: "borrow the concept from DHIS2 / DevResults (indicator disaggregations and target variance tracking) and use it to improve MONIC without breaking existing functionality"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Target Setting & Automated Milestone Progress Tracking (Priority: P1)

As a Monitoring & Evaluation (M&E) Manager, I need to define periodic target milestones for my indicators and immediately see automated variance scoring (% achievement and performance badges) when field data is reported, so I can track whether my project is on schedule without manual spreadsheet calculations.

**Why this priority**: Target tracking is the foundational requirement for results-based monitoring. It works immediately with all existing scalar indicators without requiring structural changes to data entry workflows.

**Independent Test**: Can be fully verified by creating a target for an existing indicator, entering actual data for that period, and observing that the system calculates the percentage achievement and assigns the correct status badge (On Track, Underperforming, Exceeding).

**Acceptance Scenarios**:

1. **Given** an indicator with a target of 1,000 beneficiaries for Q1 2026, **When** actual data of 850 is submitted and approved for Q1 2026, **Then** the system displays 85% achievement and categorizes performance as "On Track".
2. **Given** an indicator with a target of 500 for Q1 2026, **When** actual reported data is 250, **Then** the system calculates 50% achievement, flags the indicator as "Underperforming" (<75%), and generates an attention alert.
3. **Given** an existing indicator with no target defined, **When** data is entered, **Then** the indicator functions normally without variance scoring and without displaying any errors.

---

### User Story 2 - Multi-Dimensional Disaggregation Category Setup (Priority: P1)

As an M&E Specialist, I want to create reusable disaggregation categories (such as Sex: Male/Female, or Age Groups: Under 5 / 5-17 / 18-49 / 50+) and attach them to any indicator, so that my field reports reflect demographic and geographic breakdowns required by donors.

**Why this priority**: Disaggregation categories define the dimensional structure. Making them reusable across indicators reduces redundant configuration and guarantees consistency across projects.

**Independent Test**: Can be tested by creating a disaggregation category with two or more options, linking it to an indicator, and verifying that the indicator metadata shows the linked breakdown options while unlinked indicators remain standard scalar metrics.

**Acceptance Scenarios**:

1. **Given** an administrator in the indicator management screen, **When** they create a disaggregation group "Sex" with options "Female" and "Male", **Then** the group is saved and becomes available in the disaggregation selection dropdown for indicators.
2. **Given** a standard indicator, **When** the user leaves the disaggregation setting blank, **Then** the indicator remains a pure scalar indicator with zero disruption to existing workflows.
3. **Given** an indicator linked to a disaggregation group, **When** viewed in the indicator details, **Then** the active breakdown categories are listed alongside the indicator definition.

---

### User Story 3 - Disaggregated Data Entry & Mathematical Parity Validation (Priority: P2)

As a Field Data Entry Officer, when entering data for a disaggregated indicator, I want to enter values for each breakdown option and have the system automatically calculate the total, while enforcing mathematical parity so the sum of disaggregates matches the overall figure.

**Why this priority**: Ensures data integrity at the moment of entry. Eliminates human arithmetic errors and adheres to Constitutional Principle II (mathematical parity across dimensions).

**Independent Test**: Can be tested by opening data entry for a disaggregated indicator, entering values for each bucket, verifying auto-calculation of the total, and testing that mismatched manual totals trigger a validation prompt.

**Acceptance Scenarios**:

1. **Given** an indicator disaggregated by Sex (Female, Male), **When** a user enters 60 for Female and 40 for Male, **Then** the overall indicator total automatically sums to 100 and saves both the breakdown and aggregate.
2. **Given** an indicator with disaggregation where the user manually types an overall total of 120 but the disaggregates sum to 100, **When** the user attempts to submit, **Then** the system blocks submission and prompts the user to resolve the 20-unit parity discrepancy.
3. **Given** an existing scalar indicator with historical data, **When** new data is submitted without disaggregation, **Then** data entry proceeds smoothly as before.

---

### User Story 4 - Disaggregation & Milestone Variance Analytics Dashboard (Priority: P3)

As a Program Director or Donor Stakeholder, I want to view interactive visual dashboards showing target vs. actual variance bars and disaggregated demographic charts, so I can evaluate program effectiveness at both aggregate and sub-population levels.

**Why this priority**: Provides the presentation and reporting layer that transforms collected disaggregated data and target variance into actionable decision-making insights.

**Independent Test**: Can be tested by navigating to the indicator analytics dashboard and verifying that disaggregated indicators render breakdown charts (stacked bars or donuts) and variance indicators display progress bars with target lines.

**Acceptance Scenarios**:

1. **Given** an indicator with targets and disaggregated actuals across 4 quarters, **When** viewing the indicator analytics tab, **Then** the system renders a milestone comparison chart showing target vs actual progression over time.
2. **Given** a disaggregated indicator, **When** exporting data to Excel or CSV, **Then** the export includes both aggregate totals and individual disaggregated dimension columns.

---

### Edge Cases

- **Target is zero**: For reduction indicators (e.g., zero transmission cases), percentage achievement must handle zero targets gracefully without divide-by-zero errors.
- **Directionality (Lower is Better)**: Indicators such as "Mortality Rate" or "Dropout Count" where lower numbers indicate better performance must calculate variance inverted (`Target / Actual` or `1 + (Target - Actual) / Target`).
- **Historical Data Backfill**: Historical records created before disaggregation was enabled for an indicator MUST remain intact with null/empty disaggregation details without invalidating previous reports.
- **Partial Breakdown Entry**: If an enumerator only has partial breakdown data, the system must allow specifying an "Unspecified / Unknown" bucket or indicate partial disaggregation without losing the aggregate total.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow defining periodic target milestones (Monthly, Quarterly, Annual, or Project-End) for any indicator.
- **FR-002**: System MUST calculate milestone percentage achievement as `(Actual / Target) * 100` for standard indicators where higher actuals reflect positive progress.
- **FR-003**: System MUST support inverted target calculations for reduction indicators where lower values reflect positive progress.
- **FR-004**: System MUST classify milestone performance into three standardized bands:
  - **Underperforming**: < 75% of target
  - **On Track**: 75% to 110% of target
  - **Exceeding**: > 110% of target
- **FR-005**: System MUST allow creating reusable Disaggregation Groups containing two or more Disaggregation Options (e.g., "Sex": Female, Male; "Age Bracket": <5, 5-17, 18-49, 50+).
- **FR-006**: System MUST allow associating an optional Disaggregation Group with an Indicator without modifying existing scalar indicator functionality.
- **FR-007**: System MUST capture disaggregated values alongside parent period data (`Datum`) while preserving the parent value as the primary aggregate.
- **FR-008**: System MUST enforce mathematical parity during disaggregated entry: the sum of disaggregated option values MUST equal the parent indicator value.
- **FR-009**: System MUST support automatic computation of the parent aggregate by summing disaggregated option inputs.
- **FR-010**: System MUST maintain full backward compatibility: all existing scalar indicators without targets or disaggregations MUST function without regression.
- **FR-011**: System MUST export disaggregated breakdown data in tabular reports (Excel, CSV) with dedicated dimension columns.
- **FR-012**: System MUST display visual target progress bars and disaggregated demographic charts within the indicator analytics view.

### Key Entities *(include if feature involves data)*

- **IndicatorTarget**: Represents a scheduled milestone target for a specific indicator, period (`IndicatorDate`), and optional geographic/organizational `Entity`. Key attributes: TargetValue, PeriodId, BaselineValue, Notes.
- **DisaggregationGroup**: Represents a reusable dimensional category schema. Key attributes: Name, Description, IsActive.
- **DisaggregationOption**: An individual category bucket belonging to a DisaggregationGroup. Key attributes: Name, Code, SortOrder.
- **DisaggregationDatum**: The recorded numerical value for a specific DisaggregationOption tied to a parent `Datum` entry. Key attributes: DatumId, DisaggregationOptionId, Value.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can configure a target milestone for an indicator in under 30 seconds.
- **SC-002**: Milestone variance scoring and performance status badges update instantaneously upon datum entry or approval.
- **SC-003**: 100% of existing historical indicator and survey data remains completely unchanged, accessible, and queryable with zero regressions.
- **SC-004**: Disaggregated data entry enforces 100% mathematical consistency with zero tolerance for undetected parity discrepancies.

## Assumptions

- **Non-Breaking Invariant**: Adding disaggregations and targets must be strictly additive. Existing database tables and API contracts remain fully functional.
- **Primary Aggregation**: Most quantitative development indicators aggregate disaggregates via summation (`Sum`). Non-sum aggregation (e.g., weighted averages) can be added as an extension without altering the core model.
- **Role Permissions**: Target definition is restricted to `Administrator` and `Manager` roles; `DataEntry` users enter actual disaggregated values during routine reporting.
