import { AppDate } from './date.model';
import { Entity } from './entity.model';
import { DisaggregationDatum } from './disaggregation.model';

export class IndicatorTarget {
    indicatorTargetId?: string;
    indicatorId: string;
    dateId: string;
    entityId?: string;
    disaggregationOptionId?: string;
    disaggregationOptionName?: string;
    targetValue: number;
    baselineValue?: number;
    notes?: string;
    updatedAt?: string;
    date?: AppDate;
    entity?: Entity;
}

export class PeriodVariance {
    dateId: string;
    periodName: string;
    targetValue?: number;
    baselineValue?: number;
    actualValue?: number;
    percentageAchievement?: number;
    statusBadge: string;
    disaggregations: DisaggregationDatum[] = [];
}

export class IndicatorVariance {
    indicatorId: string;
    indicatorName: string;
    direction: number;
    periods: PeriodVariance[] = [];
}
