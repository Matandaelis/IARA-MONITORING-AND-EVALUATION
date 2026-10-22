export class DisaggregationOption {
    disaggregationOptionId: string;
    disaggregationGroupId: string;
    name: string;
    code: string;
    sortOrder: number;
    _tempId?: string;
}

export class DisaggregationGroup {
    disaggregationGroupId: string;
    name: string;
    description: string;
    isActive: boolean = true;
    createdAt: string;
    options: DisaggregationOption[] = [];
}

export class DisaggregationDatum {
    disaggregationDatumId?: string;
    datumId?: string;
    indicatorId?: string;
    entityId?: string;
    dateId?: string;
    disaggregationOptionId: string;
    optionName?: string;
    optionCode?: string;
    value: number;
    disaggregationOption?: DisaggregationOption;
}
