import { Component as NgComponent, OnInit, ViewChild, TemplateRef } from '@angular/core';
import { NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';
import { FxRate } from '../common/models/budget.model';
import { BudgetService } from '../common/services/budget.service';
import { ErrorService } from '../common/services/error.service';

@NgComponent({
    selector: 'app-fxrates',
    templateUrl: './fxrates.component.html',
    standalone: false
})
export class FxRatesComponent implements OnInit {

    public fxRates: FxRate[] = [];
    public editing: FxRate;
    public saving = false;
    public loading = false;

    private modal: NgbModalRef;

    @ViewChild('fxRateModal') fxRateModal: TemplateRef<unknown>;

    constructor(
        private modalService: NgbModal,
        private budgetService: BudgetService,
        private errorService: ErrorService
    ) {
    }

    ngOnInit(): void {
        this.load();
    }

    load(): void {
        this.loading = true;

        this.budgetService.searchFxRates()
            .subscribe({
                next: rates => {
                    this.fxRates = rates;
                    this.loading = false;
                },
                error: err => {
                    this.errorService.handleError(err, "FX Rates", "Load");
                    this.loading = false;
                }
            });
    }

    add(): void {
        this.editing = new FxRate();
        this.openModal();
    }

    edit(fxRate: FxRate): void {
        this.editing = new FxRate();
        Object.assign(this.editing, fxRate);
        this.openModal();
    }

    private openModal(): void {
        this.modal = this.modalService.open(this.fxRateModal, { centered: true });
    }

    save(): void {
        if (!this.editing.fromCurrency || !this.editing.toCurrency || !this.editing.rate) return;

        this.saving = true;

        this.budgetService.saveFxRate(this.editing)
            .subscribe({
                next: () => {
                    this.saving = false;
                    if (this.modal) this.modal.close();
                    this.load();
                },
                error: err => {
                    this.errorService.handleError(err, "FX Rate", "Save");
                    this.saving = false;
                }
            });
    }

    remove(fxRate: FxRate): void {
        if (!confirm(`Delete the rate ${fxRate.fromCurrency}/${fxRate.toCurrency} @ ${fxRate.rate}?`)) return;

        this.budgetService.deleteFxRate(fxRate.fxRateId)
            .subscribe({
                next: () => this.load(),
                error: err => this.errorService.handleError(err, "FX Rate", "Delete")
            });
    }

    cancel(): void {
        if (this.modal) this.modal.dismiss();
    }

}
