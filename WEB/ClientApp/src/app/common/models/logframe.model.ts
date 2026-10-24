import { SearchOptions, PagingHeaders } from './http.model';
import { LogFrameRow } from './logframerow.model';

export class LogFrame {
    logFrameId: string;
    name: string;
    projectId?: string;
    theoryOfChangeId?: string;

    logFrameRows: LogFrameRow[];

    constructor() {
        this.logFrameId = "00000000-0000-0000-0000-000000000000";
        this.logFrameRows = [];
    }
}

export class LogFrameSearchOptions extends SearchOptions {
    q: string;
}

export class LogFrameSearchResponse {
    logFrames: LogFrame[] = [];
    headers: PagingHeaders;
}

export interface LogFrameIndicatorRollup {
    indicatorId: string;
    code: string;
    name: string;
    units: string;
    direction: number;
    baseline: number | null;
    target: number | null;
    actual: number | null;
    percentageAchievement: number | null;
    statusBadge: string;
    absoluteVariance: number | null;
}

export interface LogFrameRowRollup {
    logFrameRowId: string;
    logFrameId: string;
    rowNumber: number;
    rowType: number;
    rowTypeName: string;
    description: string;
    meansOfVerification: string;
    risksAndAssumptions: string;
    totalIndicators: number;
    averageAchievement: number | null;
    rowStatus: string;
    totalOnTrack: number;
    totalUnderperforming: number;
    totalExceeding: number;
    totalNoData: number;
    indicators: LogFrameIndicatorRollup[];
}

export interface LogFrameRollup {
    logFrameId: string;
    logFrameName: string;
    totalRows: number;
    totalIndicators: number;
    overallAchievement: number | null;
    overallStatus: string;
    totalOnTrack: number;
    totalUnderperforming: number;
    totalExceeding: number;
    totalNoData: number;
    rows: LogFrameRowRollup[];
}
