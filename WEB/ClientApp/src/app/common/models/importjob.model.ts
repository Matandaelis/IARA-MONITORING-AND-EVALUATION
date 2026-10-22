import { SearchOptions, PagingHeaders } from './http.model';
import { User } from './user.model';

export class BudgetImportRowReport {
    rowNumber: number;
    project: string;
    category: string;
    description: string;
    planned: number;
    committed: number;
    disbursed: number;
    expended: number;
    currency: string;
    periodStart: Date;
    periodEnd: Date;
    errors: string[] = [];
}

export class BudgetImportReport {
    validCount: number;
    errorCount: number;
    rows: BudgetImportRowReport[] = [];
}

export class RecordsImportRowReport {
    rowNumber: number;
    entityCode: string;
    dateCode: string;
    responseCode: string;
    answersCount: number;
    errors: string[] = [];
}

export class RecordsImportReport {
    validCount: number;
    errorCount: number;
    rows: RecordsImportRowReport[] = [];
}

export class ImportJob {
    importJobId: string;
    importType: string; // "Records" | "Budget" | "IndicatorValues" | "LogFrame"
    fileName: string;
    targetId: string;
    totalRows: number;
    validRows: number;
    errorRows: number;
    status: string; // "Uploaded" | "Validated" | "Committed" | "Failed"
    createdOnUtc: Date;
    createdBy: User;
    committedOnUtc: Date;
    committedBy: User;
    validationReport: BudgetImportReport;
    recordsValidationReport: RecordsImportReport;

    // request-only
    fileContents: string;

    constructor() {
        this.importJobId = "00000000-0000-0000-0000-000000000000";
        this.importType = "Budget";
        this.status = "Uploaded";
    }
}

export class ImportJobSearchOptions extends SearchOptions {
    importType: string;
    status: string;
}

export class ImportJobSearchResponse {
    importJobs: ImportJob[] = [];
    headers: PagingHeaders;
}
