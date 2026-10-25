import { SearchOptions, PagingHeaders } from './http.model';
import { Project } from './project.model';
import { User } from './user.model';
import { AppDate } from './date.model';

export class BudgetCategory {
    budgetCategoryId: string;
    name: string;
    description: string;
    sortOrder: number;
    createdOnUtc: Date;
    createdById: string;
    createdBy: User;
    lineCount: number;

    constructor() {
        this.budgetCategoryId = "00000000-0000-0000-0000-000000000000";
        this.sortOrder = 0;
    }
}

export class BudgetCategorySearchOptions extends SearchOptions {
    q: string;
}

export class BudgetCategorySearchResponse {
    budgetCategories: BudgetCategory[] = [];
    headers: PagingHeaders;
}

export class BudgetLine {
    budgetLineId: string;
    projectId: string;
    budgetCategoryId: string;
    description: string;
    plannedAmount: number;
    committedAmount: number;
    disbursedAmount: number;
    actualAmount: number;
    currency: string;
    dateId: string;
    taskId: string;
    logFrameNodeId: string;
    indicatorId: string;
    periodStart: Date;
    periodEnd: Date;
    createdOnUtc: Date;
    createdById: string;
    modifiedOnUtc: Date;
    modifiedById: string;
    project: Project;
    budgetCategory: BudgetCategory;
    date: AppDate;

    constructor() {
        this.budgetLineId = "00000000-0000-0000-0000-000000000000";
        this.currency = "USD";
        this.plannedAmount = 0;
        this.committedAmount = 0;
        this.disbursedAmount = 0;
        this.actualAmount = 0;
    }
}

export class BudgetLineSearchOptions extends SearchOptions {
    projectId: string;
    budgetCategoryId: string;
    q: string;
}

export class BudgetLineSearchResponse {
    budgetLines: BudgetLine[] = [];
    headers: PagingHeaders;
}

export class BudgetSummaryRow {
    budgetCategoryId: string;
    categoryName: string;
    planned: number;
    committed: number;
    disbursed: number;
    actual: number;
    variance: number;
    utilization: number;
    lineCount: number;
    currency: string;
}

export class BudgetSummaryResponse {
    baseCurrency: string;
    rows: BudgetSummaryRow[] = [];
    missingRates: string[] = [];
}

export class BudgetBurnPoint {
    dateId: string;
    label: string;
    startDate: Date;
    endDate: Date;
    planned: number;
    expended: number;
    cumulativeExpended: number;
}

export class BudgetCostPerOutcome {
    indicatorId: string;
    indicatorCode: string;
    indicatorName: string;
    expended: number;
    outcomeValue: number;
    outcomeDateUtc: Date;
    costPerOutcome: number;
    currency: string;
}

export class FxRate {
    fxRateId: string;
    fromCurrency: string;
    toCurrency: string;
    rate: number;
    effectiveFrom: Date;
    createdOnUtc: Date;

    constructor() {
        this.fxRateId = "00000000-0000-0000-0000-000000000000";
    }
}

export class BudgetTotals {
    planned = 0;
    committed = 0;
    disbursed = 0;
    actual = 0;
    currency = "";
}
