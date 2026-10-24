import { SearchOptions, PagingHeaders } from './http.model';

export enum FeedbackStatus {
    Received = 0,
    Investigating = 1,
    Resolved = 2,
    Closed = 3
}

export enum FeedbackSeverity {
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

export class BeneficiaryFeedback {
    id: string;
    projectId: string;
    projectName?: string;
    trackingCode?: string;
    dateReceivedUtc: Date;
    channel: string;
    category: string;
    severity: FeedbackSeverity;
    severityLabel?: string;
    description: string;
    isAnonymous: boolean;
    contactDetails?: string;
    status: FeedbackStatus;
    statusLabel?: string;
    resolutionNotes?: string;
    resolvedAtUtc?: Date;
    assignedToUserId?: string;
    assignedToUserName?: string;

    constructor() {
        this.id = '00000000-0000-0000-0000-000000000000';
        this.dateReceivedUtc = new Date();
        this.channel = 'Community Meeting';
        this.category = 'General';
        this.severity = FeedbackSeverity.Low;
        this.status = FeedbackStatus.Received;
        this.isAnonymous = false;
    }
}

export class FeedbackSearchOptions extends SearchOptions {
    projectId?: string;
    status?: FeedbackStatus;
    severity?: FeedbackSeverity;
    channel?: string;
    q?: string;
}

export class FeedbackSearchResponse {
    items: BeneficiaryFeedback[] = [];
    headers: PagingHeaders;
}

export interface FeedbackStats {
    total: number;
    received: number;
    investigating: number;
    resolved: number;
    closed: number;
    critical: number;
    high: number;
    medium: number;
    low: number;
}
