import { BudgetBurnPoint, BudgetSummaryRow } from './budget.model';

export enum MonitoringTypes {
    Process = 0,
    Compliance = 1,
    Context = 2,
    Financial = 3,
    Administrative = 4,
    Assumption = 5,
    Organizational = 6,
    Results = 7,
    Technical = 8,
    Impact = 9
}

export enum RagStatuses {
    NotAssessed = 0,
    Red = 1,
    Amber = 2,
    Green = 3
}

export enum RiskTypes {
    Risk = 0,
    Assumption = 1
}

export enum RiskStatuses {
    Open = 0,
    Monitoring = 1,
    Mitigated = 2,
    Realized = 3,
    Closed = 4
}

export enum MonitoringEventTypes {
    FieldVisit = 0,
    Supervision = 1,
    DataQualityAudit = 2,
    ComplianceCheck = 3,
    StakeholderFeedback = 4,
    TechnicalReview = 5,
    ReviewMeeting = 6,
    Other = 7
}

export enum MonitoringEventStatuses {
    Planned = 0,
    Completed = 1,
    Cancelled = 2
}

export enum MonitoringActionStatuses {
    Open = 0,
    InProgress = 1,
    Completed = 2,
    Overdue = 3,
    Cancelled = 4
}

export enum MonitoringCheckTypes {
    Compliance = 0,
    Administrative = 1
}

export enum MonitoringCheckStatuses {
    Pending = 0,
    Compliant = 1,
    NonCompliant = 2,
    NotApplicable = 3
}

export enum MonitoringPlanStatuses {
    Draft = 0,
    Active = 1,
    Closed = 2
}

export const monitoringTypeLabels = [
    'Process',
    'Compliance',
    'Context',
    'Financial',
    'Administrative / Logistics',
    'Assumption',
    'Organisational',
    'Results',
    'Technical',
    'Impact'
];

export const monitoringTypeOptions = monitoringTypeLabels.map((label, value) => ({ value, label }));

export function monitoringTypeLabel(value: number): string {
    return monitoringTypeLabels[value] ?? 'Unknown';
}

export function ragLabel(value: number): string {
    switch (value) {
        case RagStatuses.Green: return 'On track';
        case RagStatuses.Amber: return 'At risk';
        case RagStatuses.Red: return 'Off track';
        default: return 'Not assessed';
    }
}

export function ragClass(value: number): string {
    switch (value) {
        case RagStatuses.Green: return 'success';
        case RagStatuses.Amber: return 'warning';
        case RagStatuses.Red: return 'danger';
        default: return 'secondary';
    }
}

export const riskTypeOptions = [
    { value: RiskTypes.Risk, label: 'Risk' },
    { value: RiskTypes.Assumption, label: 'Assumption' }
];

export const riskStatusOptions = [
    { value: RiskStatuses.Open, label: 'Open' },
    { value: RiskStatuses.Monitoring, label: 'Monitoring' },
    { value: RiskStatuses.Mitigated, label: 'Mitigated' },
    { value: RiskStatuses.Realized, label: 'Realized' },
    { value: RiskStatuses.Closed, label: 'Closed' }
];

export const eventTypeOptions = [
    { value: MonitoringEventTypes.FieldVisit, label: 'Field visit' },
    { value: MonitoringEventTypes.Supervision, label: 'Supervision' },
    { value: MonitoringEventTypes.DataQualityAudit, label: 'Data quality audit' },
    { value: MonitoringEventTypes.ComplianceCheck, label: 'Compliance check' },
    { value: MonitoringEventTypes.StakeholderFeedback, label: 'Stakeholder feedback' },
    { value: MonitoringEventTypes.TechnicalReview, label: 'Technical review' },
    { value: MonitoringEventTypes.ReviewMeeting, label: 'Review meeting' },
    { value: MonitoringEventTypes.Other, label: 'Other' }
];

export const eventStatusOptions = [
    { value: MonitoringEventStatuses.Planned, label: 'Planned' },
    { value: MonitoringEventStatuses.Completed, label: 'Completed' },
    { value: MonitoringEventStatuses.Cancelled, label: 'Cancelled' }
];

export const actionStatusOptions = [
    { value: MonitoringActionStatuses.Open, label: 'Open' },
    { value: MonitoringActionStatuses.InProgress, label: 'In progress' },
    { value: MonitoringActionStatuses.Completed, label: 'Completed' },
    { value: MonitoringActionStatuses.Overdue, label: 'Overdue' },
    { value: MonitoringActionStatuses.Cancelled, label: 'Cancelled' }
];

export const checkTypeOptions = [
    { value: MonitoringCheckTypes.Compliance, label: 'Compliance' },
    { value: MonitoringCheckTypes.Administrative, label: 'Administrative / logistics' }
];

export const checkStatusOptions = [
    { value: MonitoringCheckStatuses.Pending, label: 'Pending' },
    { value: MonitoringCheckStatuses.Compliant, label: 'Compliant' },
    { value: MonitoringCheckStatuses.NonCompliant, label: 'Non-compliant' },
    { value: MonitoringCheckStatuses.NotApplicable, label: 'Not applicable' }
];

export class MonitoringProject {
    projectId = '';
    name = '';
}

export class MonitoringPeriod {
    dateId = '';
    name = '';
    dateType = 0;
    openFrom = '';
    openTo = '';
    sortOrder = 0;
}

export class MonitoringLogFrame {
    logFrameId = '';
    name = '';
    projectId: string = null;
}

export class MonitoringLogFrameNode {
    logFrameNodeId = '';
    logFrameId = '';
    logFrameName = '';
    code = '';
    title = '';
    levelName = '';
}

export class MonitoringTypeSummary {
    monitoringType = 0;
    label = '';
    total = 0;
    green = 0;
    amber = 0;
    red = 0;
    noData = 0;
    averageAchievement: number = null;
}

export class MonitoringIndicatorPerformance {
    indicatorId = '';
    code = '';
    name = '';
    units = '';
    monitoringType = 0;
    monitoringTypeLabel = '';
    direction = 0;
    isCalculated = false;
    baseline: number = null;
    target: number = null;
    actual: number = null;
    percentageAchievement: number = null;
    absoluteVariance: number = null;
    statusBadge = '';
    rag = 0;
    greenThreshold: number = null;
    amberThreshold: number = null;
    dataPoints = 0;
    approvedDataPoints = 0;
    coverage: number = null;
    timeliness: number = null;
    approvalRate: number = null;
    latestDateId: string = null;
    latestPeriod = '';
    responsibleUserName = '';
}

export class MonitoringCostPerOutput {
    indicatorId = '';
    code = '';
    name = '';
    expended = 0;
    outputValue: number = null;
    costPerOutput: number = null;
    currency = '';
}

export class MonitoringFinancial {
    projectId = '';
    baseCurrency = '';
    planned = 0;
    committed = 0;
    disbursed = 0;
    actual = 0;
    variance = 0;
    utilization = 0;
    commitmentRate = 0;
    rows: BudgetSummaryRow[] = [];
    missingRates: string[] = [];
    burnSeries: BudgetBurnPoint[] = [];
    costPerOutput: MonitoringCostPerOutput[] = [];
}

export class MonitoringSummary {
    projectId = '';
    projectName = '';
    dateId: string = null;
    periodLabel = '';
    generatedOnUtc = '';
    totalIndicators = 0;
    assessedIndicators = 0;
    green = 0;
    amber = 0;
    red = 0;
    noData = 0;
    overallAchievement: number = null;
    overallStatus = '';
    overallRag = 0;
    coverage: number = null;
    timeliness: number = null;
    approvalRate: number = null;
    compositeScore: number = null;
    resultsScore: number = null;
    financialScore: number = null;
    workplanScore: number = null;
    dataQualityScore: number = null;
    financial = new MonitoringFinancial();
    byType: MonitoringTypeSummary[] = [];
    underperforming: MonitoringIndicatorPerformance[] = [];
}

export class MonitoringMatrixPeriod {
    dateId = '';
    label = '';
    openFrom = '';
    openTo = '';
}

export class MonitoringMatrixCell {
    dateId = '';
    periodLabel = '';
    target: number = null;
    actual: number = null;
    percentageAchievement: number = null;
    rag = 0;
}

export class MonitoringMatrixRow {
    indicatorId = '';
    code = '';
    name = '';
    units = '';
    monitoringType = 0;
    monitoringTypeLabel = '';
    direction = 0;
    baseline: number = null;
    coverage: number = null;
    responsibleUserName = '';
    cells: MonitoringMatrixCell[] = [];
}

export class MonitoringMatrix {
    projectId = '';
    projectName = '';
    dateId: string = null;
    periodLabel = '';
    periods: MonitoringMatrixPeriod[] = [];
    rows: MonitoringMatrixRow[] = [];
}

export class MonitoringResultNode {
    logFrameNodeId = '';
    parentNodeId: string = null;
    code = '';
    title = '';
    levelName = '';
    depth = 0;
    sortOrder = 0;
    weightedAchievement: number = null;
    rag = 0;
    coverage: number = null;
    indicatorCount = 0;
    green = 0;
    amber = 0;
    red = 0;
    noData = 0;
    openRisks = 0;
    risksAndAssumptions = '';
    indicators: MonitoringIndicatorPerformance[] = [];
    children: MonitoringResultNode[] = [];
    expanded = true;
}

export class MonitoringResults {
    logFrameId = '';
    logFrameName = '';
    dateId: string = null;
    periodLabel = '';
    overallAchievement: number = null;
    overallRag = 0;
    coverage: number = null;
    nodes: MonitoringResultNode[] = [];
}

export class MonitoringCoverageRow {
    indicatorId = '';
    code = '';
    name = '';
    monitoringType = 0;
    monitoringTypeLabel = '';
    frequency = 0;
    expected = 0;
    reported = 0;
    approved = 0;
    coverage: number = null;
    timeliness: number = null;
    approvalRate: number = null;
    responsibleUserName = '';
}

export class MonitoringCoverage {
    projectId = '';
    projectName = '';
    overallCoverage: number = null;
    overallTimeliness: number = null;
    overallApprovalRate: number = null;
    rows: MonitoringCoverageRow[] = [];
}

export class MonitoringPlanItem {
    monitoringPlanItemId = '';
    monitoringPlanId = '';
    indicatorId = '';
    monitoringType = 0;
    dataSource = '';
    collectionMethod = '';
    frequency = 1;
    responsibleUserId: string = null;
    responsibleUserName = '';
    responsibleEntityId: string = null;
    responsibleEntityName = '';
    meansOfVerification = '';
    isMandatory = true;
    notes = '';
    sortOrder = 0;
    indicatorCode = '';
    indicatorName = '';
}

export class MonitoringPlan {
    monitoringPlanId = '';
    projectId = '';
    name = '';
    description = '';
    fromDateId: string = null;
    toDateId: string = null;
    fromDateName = '';
    toDateName = '';
    status = 0;
    ownerUserId: string = null;
    ownerUserName = '';
    createdOnUtc = '';
    modifiedOnUtc: string = null;
    items: MonitoringPlanItem[] = [];
}

export class RiskAssumptionUpdate {
    riskAssumptionUpdateId = '';
    riskAssumptionId = '';
    dateId: string = null;
    dateName = '';
    likelihood = 3;
    impact = 3;
    riskScore = 9;
    status = RiskStatuses.Monitoring;
    comment = '';
    evidenceDocumentId: string = null;
    recordedOnUtc = '';
}

export class RiskAssumption {
    riskAssumptionId = '';
    projectId = '';
    logFrameNodeId: string = null;
    logFrameNodeTitle = '';
    type = RiskTypes.Risk;
    typeLabel = '';
    title = '';
    description = '';
    likelihood = 3;
    impact = 3;
    riskScore = 9;
    status = RiskStatuses.Open;
    statusLabel = '';
    ownerUserId: string = null;
    ownerUserName = '';
    mitigation = '';
    reviewDate: string = null;
    createdOnUtc = '';
    updates: RiskAssumptionUpdate[] = [];
}

export class MonitoringAction {
    monitoringActionId = '';
    monitoringEventId: string = null;
    riskAssumptionId: string = null;
    indicatorId: string = null;
    indicatorCode = '';
    indicatorName = '';
    projectId = '';
    description = '';
    ownerUserId: string = null;
    ownerUserName = '';
    dueDate: string = null;
    status = MonitoringActionStatuses.Open;
    completedDate: string = null;
    notes = '';
    linkedTaskId: string = null;
    linkedTaskName = '';
    createdOnUtc = '';
    triggerSource = '';
    deviationPercent: number = null;
    triggeredOnUtc: string = null;
    isCap = false;
}

export class CapTriggerRequest {
    projectId = '';
    dateId: string = null;
    deviationThresholdPercent: number = null;
    slaBusinessDays: number = null;
}

export class MonitoringEvent {
    monitoringEventId = '';
    projectId = '';
    entityId: string = null;
    entityName = '';
    type = MonitoringEventTypes.FieldVisit;
    typeLabel = '';
    title = '';
    eventDate = '';
    periodDateId: string = null;
    periodDateName = '';
    conductedById: string = null;
    conductedByName = '';
    participants = '';
    findings = '';
    recommendations = '';
    status = MonitoringEventStatuses.Completed;
    meReviewId: string = null;
    documentId: string = null;
    createdOnUtc = '';
    actions: MonitoringAction[] = [];
}

export class MonitoringCheck {
    monitoringCheckId = '';
    projectId = '';
    entityId: string = null;
    entityName = '';
    type = MonitoringCheckTypes.Compliance;
    typeLabel = '';
    title = '';
    requirementSource = '';
    description = '';
    dueDate: string = null;
    periodDateId: string = null;
    periodDateName = '';
    status = MonitoringCheckStatuses.Pending;
    evidenceDocumentId: string = null;
    notes = '';
    createdOnUtc = '';
}

export class LessonLearned {
    lessonLearnedId = '';
    projectId = '';
    componentId: string = null;
    componentName = '';
    logFrameNodeId: string = null;
    logFrameNodeTitle = '';
    monitoringType = MonitoringTypes.Results;
    monitoringTypeLabel = '';
    title = '';
    description = '';
    recommendation = '';
    recordedOnUtc = '';
}

export class ProjectIndicator {
    projectId = '';
    indicatorId = '';
    isMandatory = false;
    sortOrder = 0;
    indicator: any = null;
}
