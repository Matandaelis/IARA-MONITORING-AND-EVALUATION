import { SearchOptions, PagingHeaders } from './http.model';

export class MeReview {
    meReviewId: string;
    title: string;
    domain: string;
    targetType: number;
    targetEntityId?: string;
    createdDateUtc: string;
    createdById?: string;
    createdByName?: string;
    overallScore: number;
    status: string;
    summary?: string;
    documentSource?: string;

    sections: MeReviewSection[] = [];
    findings: MeReviewFinding[] = [];
    recommendations: MeReviewRecommendation[] = [];

    constructor() {
        this.meReviewId = '00000000-0000-0000-0000-000000000000';
        this.overallScore = 0;
        this.status = 'Draft';
        this.sections = [];
        this.findings = [];
        this.recommendations = [];
    }
}

export class MeReviewSection {
    meReviewSectionId: string;
    meReviewId: string;
    sectionKey: string;
    title: string;
    score: number;
    weight: number;
    status: string;
    comments?: string;
    order: number;
    findings: MeReviewFinding[] = [];
}

export class MeReviewFinding {
    meReviewFindingId: string;
    meReviewId: string;
    meReviewSectionId?: string;
    code?: string;
    severity: string;
    criterion?: string;
    message: string;
    details?: string;
    locationRef?: string;
}

export class MeReviewRecommendation {
    meReviewRecommendationId: string;
    meReviewId: string;
    meReviewFindingId?: string;
    priority: string;
    category?: string;
    recommendationText: string;
    actionSteps?: string;
    isImplemented: boolean;
}

export class ReviewDomainInfo {
    key: string;
    name: string;
    description: string;
    standard: string;
    category: string;
    criteria: string[] = [];
    supportsEntityEvaluation: boolean;
    supportsPdfUpload: boolean;
}

export class ReviewDashboardStats {
    totalReviews: number = 0;
    averageScore: number = 0;
    criticalFindingsCount: number = 0;
    highFindingsCount: number = 0;
    openRecommendationsCount: number = 0;
    implementedRecommendationsCount: number = 0;
    reviewsByDomain: { [key: string]: number } = {};
    averageScoreByDomain: { [key: string]: number } = {};
}

export class SamplingPlanRequest {
    title?: string;
    populationSize: number = 10000;
    marginOfError: number = 0.05;
    confidenceLevel: number = 0.95;
    estimatedProportion: number = 0.50;
    nonResponseBufferRate: number = 0.10;
    proposedSampleSize?: number;
    samplingMethod?: string = 'SimpleRandom';
    strataDetails?: string;
}

export class EvaluateReviewRequest {
    domain: string;
    targetType?: number;
    targetEntityId?: string;
    title?: string;
    narrativeText?: string;
    samplingPlan?: SamplingPlanRequest;
}

export class MeReviewSearchOptions extends SearchOptions {
    q?: string;
    domain?: string;
    status?: string;
    targetType?: number;
    targetEntityId?: string;
    createdById?: string;
    minScore?: number;
    maxScore?: number;
}

export class MeReviewSearchResponse {
    reviews: MeReview[] = [];
    headers: PagingHeaders;
}
