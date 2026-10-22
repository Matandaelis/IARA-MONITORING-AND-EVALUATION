import { Component, Input } from '@angular/core';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import { BudgetService } from '../common/services/budget.service';
import { BudgetLine, BudgetCategory } from '../common/models/budget.model';
import { DateService } from '../common/services/date.service';
import { DateSearchOptions, AppDate } from '../common/models/date.model';
import { ErrorService } from '../common/services/error.service';

@Component({
    selector: 'app-budgetline-modal',
    templateUrl: './budgetline.modal.html',
    standalone: false
})
export class BudgetLineModal {

    public title: string;
    public saving = false;
    public dates: AppDate[] = [];

    @Input() line: BudgetLine = new BudgetLine();
    @Input() categories: BudgetCategory[] = [];

    constructor(
        protected modal: NgbActiveModal,
        private budgetService: BudgetService,
        private dateService: DateService,
        private errorService: ErrorService
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
