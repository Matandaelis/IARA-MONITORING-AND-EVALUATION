import { Component, OnInit } from '@angular/core';
import { NgForm } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { BreadcrumbService } from '../common/services/breadcrumb.service';
import { ToastrService } from 'ngx-toastr';
import { forkJoin } from 'rxjs';
import { ConfirmModalComponent, ConfirmModalOptions } from '../common/components/confirm.component';
import { Enums, LogFrameRowTypes } from '../common/models/enums.model';
import { LogFrame, LogFrameRollup, LogFrameRowRollup } from '../common/models/logframe.model';
import { LogFrameRow, LogFrameRowSearchOptions } from '../common/models/logframerow.model';
import { ErrorService } from '../common/services/error.service';
import { LogFrameService } from '../common/services/logframe.service';
import { LogFrameRowService } from '../common/services/logframerow.service';
import { DownloadService } from '../common/services/download.service';
import { LogFrameRowViewComponent } from './logframe.row.component';
import { tap } from 'rxjs';
import { MeReviewService } from '../common/services/mereview.service';

@Component({
    selector: 'logframe',
    templateUrl: './logframe.component.html',
    styleUrls: ['./logframe.component.css'],
    standalone: false
})
export class LogFrameComponent implements OnInit {

    public logFrame = new LogFrame();
    public logFrameId: string; // required for ngModelChange
    public isNew = false;
    public loaded = false;
    public auditing = false;

    public logFrameRowTypesActivities = LogFrameRowTypes.Activities;
    public logFrameRowTypesOutputs = LogFrameRowTypes.Outputs;
    public logFrameRowTypesOutcomes = LogFrameRowTypes.Outcomes;
    public logFrameRowTypesGoals = LogFrameRowTypes.Goals;
    protected logFrameRows: LogFrameRow[] = [];
    public logFrameRowTypes = Enums.LogFrameRowTypes;

    public rollup: LogFrameRollup | null = null;
    public rowRollupMap: { [rowId: string]: LogFrameRowRollup } = {};
    public exportingCsv = false;

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private errorService: ErrorService,
        private modalService: NgbModal,
        private logFrameRowService: LogFrameRowService,
        private logFrameService: LogFrameService,
        private breadcrumbService: BreadcrumbService,
        private toastr: ToastrService,
        private meReviewService: MeReviewService,
        private downloadService: DownloadService
    ) {
    }

    ngOnInit(): void {

        this.route.params.subscribe(params => {

            const logFrameId = params["logFrameId"];
            this.isNew = logFrameId === "add";

            if (!this.isNew) {

                this.logFrame.logFrameId = logFrameId;
                this.logFrameId = logFrameId;
                this.loadLogFrame();

            }

        });

    }

    public save(form: NgForm): void {

        if (form.invalid) {

            this.toastr.error("The form has not been completed correctly.", "Form Error");
            return;

        }

        this.logFrameService.save(this.logFrame)
            .subscribe({
                next: logFrame => {
                    this.toastr.success("The logical framework has been saved", "Save Logical Framework");
                    if (this.isNew)
                        this.router.navigate(["../", logFrame.logFrameId], { relativeTo: this.route });
                },
                error: err => {
                    this.errorService.handleError(err, "Logical Framework", "Save");
                }
            });
    }

    public delete(): void {
        let modalRef = this.modalService.open(ConfirmModalComponent, { centered: true });
        (modalRef.componentInstance as ConfirmModalComponent).options = { title: "Delete Logical Framework", text: "Are you sure you want to delete this logical framework?", deleteStyle: true, ok: "Delete" } as ConfirmModalOptions;
        modalRef.result.then(
            () => {

                this.logFrameService.delete(this.logFrame.logFrameId)
                    .subscribe({
                        next: () => {
                            this.toastr.success("The logical framework has been deleted", "Delete Logical Framework");
                            this.router.navigate(["/logframe"]);
                        },
                        error: err => {
                            this.errorService.handleError(err, "Logical Framework", "Delete");
                        }
                    });

            }, () => { });
    }

    public auditLogFrame(): void {
        if (!this.logFrameId) return;
        this.auditing = true;
        this.meReviewService.evaluate({
            domain: 'logframe',
            targetEntityId: this.logFrameId
        }).subscribe({
            next: (review) => {
                this.auditing = false;
                this.toastr.success(`LogFrame audited! Score: ${review.overallScore}% (${review.status})`, 'M&E Review');
                this.router.navigate(['/mereviews', review.meReviewId]);
            },
            error: (err) => {
                this.auditing = false;
                this.errorService.handleError(err, 'M&E Review', 'Audit');
            }
        });
    }

    private loadLogFrame(): void {

        forkJoin(
            {
                logFrame: this.logFrameService.get(this.logFrame.logFrameId)
                    .pipe(
                        tap(logFrame => {
                            this.logFrame = logFrame;
                            this.loaded = true;
                        })
                    ),
                logFrameRows: this.logFrameRowService.search({ logFrameId: this.logFrame.logFrameId, pageSize: 0, includeChildren: true } as LogFrameRowSearchOptions)
                    .pipe(
                        tap(logFrameRowResponse => {
                            this.logFrameRows = logFrameRowResponse.logFrameRows;
                            this.loaded = true;
                        })
                    )
            }
        ).subscribe(
            {
                next: () => {
                    this.loadRollup();
                },
                error: err => this.errorService.handleError(err, "LogFrame Data", "Load")
            }
        );

    }

    public loadRollup(): void {
        if (!this.logFrameId || this.isNew) return;
        this.logFrameService.getRollup(this.logFrameId).subscribe({
            next: (rollup) => {
                this.rollup = rollup;
                this.rowRollupMap = {};
                if (rollup && rollup.rows) {
                    for (const row of rollup.rows) {
                        this.rowRollupMap[row.logFrameRowId] = row;
                        if (row.logFrameRowId) {
                            this.rowRollupMap[row.logFrameRowId.toLowerCase()] = row;
                        }
                    }
                }
            },
            error: () => {
                // Non-blocking roll-up fetch
            }
        });
    }

    public downloadDonorCsv(): void {
        if (!this.logFrameId) return;
        this.exportingCsv = true;
        this.downloadService.exportLogFrameCSV(this.logFrameId).subscribe({
            next: () => {
                this.exportingCsv = false;
                this.toastr.success("Donor LogFrame CSV exported successfully", "Export Matrix");
            },
            error: (err) => {
                this.exportingCsv = false;
                this.errorService.handleError(err, "Donor LogFrame", "Export");
            }
        });
    }

    public getRollupStatusBadgeClass(status?: string): string {
        switch (status?.toLowerCase()) {
            case 'ontrack': return 'bg-success text-white';
            case 'underperforming': return 'bg-danger text-white';
            case 'exceeding': return 'bg-primary text-white';
            default: return 'bg-secondary text-white';
        }
    }

    public getRollupStatusLabel(status?: string): string {
        switch (status?.toLowerCase()) {
            case 'ontrack': return 'On Track';
            case 'underperforming': return 'Underperforming';
            case 'exceeding': return 'Exceeding';
            default: return 'Pending Data';
        }
    }

    private openLogFrameRowModal(rowType: LogFrameRowTypes, logFrameRow?: LogFrameRow): void {
        let modalRef = this.modalService.open(LogFrameRowViewComponent, { size: 'xl', centered: true, scrollable: true });
        (modalRef.componentInstance as LogFrameRowViewComponent).setLogFrameRow(this.logFrame, rowType, logFrameRow);
        modalRef.result.then(
            () => {
                this.loadLogFrame();
                // todo: handle errors on save?
            }, () => { });
    }

    public rowClick(row: LogFrameRow, rowType: LogFrameRowTypes): void {
        this.openLogFrameRowModal(rowType, row);
    }

    public addRow(rowType: LogFrameRowTypes): void {
        this.openLogFrameRowModal(rowType);
    }

    public changeBreadcrumb(): void {
        this.breadcrumbService.changeBreadcrumb(this.route.snapshot, this.logFrame.name !== undefined ? this.logFrame.name.substring(0, 25) : "(new project)");
    }
}
