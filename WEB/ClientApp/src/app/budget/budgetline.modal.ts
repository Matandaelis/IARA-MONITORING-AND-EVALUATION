import { Component, Input } from '@angular/core';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import { BudgetService } from '../common/services/budget.service';
import { BudgetLine, BudgetCategory } from '../common/models/budget.model';
import { DateService } from '../common/services/date.service';
import { DateSearchOptions, AppDate } from '../common/models/date.model';
import { ErrorService } from '../common/services/error.service';
import { IndicatorService } from '../common/services/indicator.service';
import { Indicator, IndicatorSearchOptions } from '../common/models/indicator.model';
import { MonitoringService } from '../common/services/monitoring.service';
import { MonitoringLogFrameNode } from '../common/models/monitoring.model';

@Component({
    selector: 'app-budgetline-modal',
    templateUrl: './budgetline.modal.html',
    standalone: false
})
export class BudgetLineModal {

    public title: string;
    public saving = false;
    public dates: AppDate[] = [];
    public indicators: Indicator[] = [];
    public logFrameNodes: MonitoringLogFrameNode[] = [];
    private indicatorsLoaded = false;
    private logFrameNodesLoaded = false;

    @Input() line: BudgetLine = new BudgetLine();
    @Input() categories: BudgetCategory[] = [];

    constructor(
        protected modal: NgbActiveModal,
        private budgetService: BudgetService,
        private dateService: DateService,
        private errorService: ErrorService,
        private indicatorService: IndicatorService,
        private monitoringService: MonitoringService
    ) {
    }

    initialise(line: BudgetLine, categories: BudgetCategory[], reportingCurrency: string): void {
        this.line = line;
        this.categories = categories;
        if (!this.line.currency || this.line.currency === "USD") this.line.currency = reportingCurrency || "USD";
        this.title = line.budgetLineId && line.budgetLineId !== "00000000-0000-0000-0000-000000000000" ? "Edit Budget Line" : "Add Budget Line";

        const dateSearchOptions = new DateSearchOptions();
        dateSearchOptions.pageSize = 0;
        this.dateService.search(dateSearchOptions)
            .subscribe({
                next: response => {
                    this.dates = response.dates;
                },
                error: (err: any) => this.errorService.handleError(err, "Reporting Periods", "Load")
            });

        this.loadIndicators();
        this.loadLogFrameNodes();
    }

    private loadIndicators(): void {
        if (this.indicatorsLoaded) return;
        this.indicatorsLoaded = true;

        const searchOptions = new IndicatorSearchOptions();
        searchOptions.pageSize = 0;

        this.indicatorService.search(searchOptions)
            .subscribe({
                next: response => this.indicators = response.indicators || [],
                // Optional lookup: do not block budget editing when the user cannot browse indicators.
                error: () => this.indicators = []
            });
    }

    private loadLogFrameNodes(): void {
        if (this.logFrameNodesLoaded || !this.line.projectId) return;
        this.logFrameNodesLoaded = true;

        this.monitoringService.getLogFrameNodes(this.line.projectId)
            .subscribe({
                next: nodes => this.logFrameNodes = nodes || [],
                // Optional lookup: monitoring may not be visible to every finance user.
                error: () => this.logFrameNodes = []
            });
    }

    get isNew(): boolean {
        return !this.line.budgetLineId || this.line.budgetLineId === "00000000-0000-0000-0000-000000000000";
    }

    save(): void {
        if (!this.line.budgetCategoryId || this.line.budgetCategoryId === "00000000-0000-0000-0000-000000000000") return;

        this.saving = true;

        this.budgetService.saveLine(this.line)
            .subscribe({
                next: saved => {
                    this.modal.close(saved);
                },
                error: err => {
                    this.errorService.handleError(err, "Budget Line", "Save");
                    this.saving = false;
                }
            });
    }

    cancel(): void {
        this.modal.dismiss();
    }

}
