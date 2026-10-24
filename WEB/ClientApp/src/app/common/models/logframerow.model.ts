import { SearchOptions, PagingHeaders } from './http.model';
import { LogFrame } from './logframe.model';
import { LogFrameRowTypes } from './enums.model';
import { LogFrameRowComponent } from './logframerowcomponent.model';
import { LogFrameRowIndicator } from './logframerowindicator.model';

export class LogFrameRowRiskAssumption {
    id?: string;
    logFrameRowId?: string;
    riskAssumptionId: string;
    title?: string;
    description?: string;
    likelihood?: number;
    impact?: number;
    status?: string;
}

export class LogFrameRowTask {
    id?: string;
    logFrameRowId?: string;
    taskId: string;
    name?: string;
    percentCompleted?: number;
    startDate?: Date;
    endDate?: Date;
}

export class LogFrameRowBudgetLine {
    id?: string;
    logFrameRowId?: string;
    budgetLineId: string;
    description?: string;
    plannedAmount?: number;
    actualAmount?: number;
    currency?: string;
}

export class LogFrameLiveIndicatorMetric {
    indicatorId: string;
    code: string;
    name: string;
    targetValue?: number;
    currentValue?: number;
    percentageAchievement?: number;
    statusBadgeColor: string = 'secondary';
}

export class LogFrameRow {
    logFrameRowId: string;
    logFrameId: string;
    rowNumber: number;
    rowType: LogFrameRowTypes;
    description: string;
    indicators: string;
    meansOfVerification: string;
    risksAndAssumptions: string;
    logFrame: LogFrame;

    logFrameRowComponents: LogFrameRowComponent[];
    logFrameRowIndicators: LogFrameRowIndicator[];
    logFrameRowRiskAssumptions: LogFrameRowRiskAssumption[];
    logFrameRowTasks: LogFrameRowTask[];
    logFrameRowBudgetLines: LogFrameRowBudgetLine[];
    liveIndicatorMetrics: LogFrameLiveIndicatorMetric[];

    constructor() {
        this.logFrameRowId = "00000000-0000-0000-0000-000000000000";
        this.logFrameRowComponents = [];
        this.logFrameRowIndicators = [];
        this.logFrameRowRiskAssumptions = [];
        this.logFrameRowTasks = [];
        this.logFrameRowBudgetLines = [];
        this.liveIndicatorMetrics = [];
    }
}

export class LogFrameRowSearchOptions extends SearchOptions {
    q: string;
    logFrameId: string;
    rowType: LogFrameRowTypes;
}

export class LogFrameRowSearchResponse {
    logFrameRows: LogFrameRow[] = [];
    headers: PagingHeaders;
}
