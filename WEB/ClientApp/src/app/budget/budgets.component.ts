import { Component as NgComponent, OnInit, ViewChild } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';
import { PagingHeaders } from '../common/models/http.model';
import { BudgetLine, BudgetLineSearchOptions, BudgetLineSearchResponse, BudgetSummaryResponse, BudgetBurnPoint, BudgetCostPerOutcome, BudgetCategory, BudgetTotals } from '../common/models/budget.model';
import { BudgetService } from '../common/services/budget.service';
import { Project, ProjectSearchOptions } from '../common/models/project.model';
import { ProjectService } from '../common/services/project.service';
import { IndicatorService } from '../common/services/indicator.service';
import { Indicator, IndicatorSearchOptions } from '../common/models/indicator.model';
import { ErrorService } from '../common/services/error.service';
import { BudgetLineModal } from './budgetline.modal';
import { FadeThenShrink } from '../common/animations/fadethenshrink';

@NgComponent({
    selector: 'app-budgets',
    templateUrl: './budgets.component.html',
    animations: [FadeThenShrink],
    standalone: false
})
export class BudgetsComponent implements OnInit {

    public projects: Project[] = [];
    public selectedProjectId: string;
    public budgetLines: BudgetLine[] = [];
    public summary: BudgetSummaryResponse = new BudgetSummaryResponse();
    public burnSeries: BudgetBurnPoint[] = [];
    public costPerOutcome: BudgetCostPerOutcome;
    public indicators: Indicator[] = [];
    public selectedIndicatorId: string;
    public currencyMode: 'base' | 'original' = 'base';
    public totals: BudgetTotals = new BudgetTotals();
    public searchOptions = new BudgetLineSearchOptions();
    public headers = new PagingHeaders();
    public loading = false;

    // ECharts: planned vs cumulative expended (S-curve)
    public burnChartOptions: any;

    @ViewChild('budgetLineModal') budgetLineModal: BudgetLineModal;

    constructor(
        public route: ActivatedRoute,
        private router: Router,
        private modalService: NgbModal,
        private errorService: ErrorService,
        private budgetService: BudgetService,
        private projectService: ProjectService,
        private indicatorService: IndicatorService
    ) {
    }

    ngOnInit(): void {
        this.searchOptions.includeParents = true;
        this.searchOptions.pageSize = 20;

        this.loadProjects();

        const projectId = this.route.snapshot.paramMap.get('projectId');
        if (projectId) {
            this.selectedProjectId = projectId;
            this.load();
        }
    }

    loadProjects(): void {
        const searchOptions = new ProjectSearchOptions();
        searchOptions.pageSize = 0;

        this.projectService.search(searchOptions)
            .subscribe({
                next: response => {
                    this.projects = response.projects;
                    if (!this.selectedProjectId && this.projects.length > 0) {
                        this.selectedProjectId = this.projects[0].projectId;
                        this.load();
                    }
                },
                error: err => this.errorService.handleError(err, "Projects", "Load")
            });
    }

    projectChanged(): void {
        if (this.selectedProjectId) this.router.navigate(["/budget", this.selectedProjectId], { replaceUrl: true });
        this.load();
    }

    load(): void {
        if (!this.selectedProjectId) return;

        this.loading = true;
        this.searchOptions.projectId = this.selectedProjectId;
        this.searchOptions.pageIndex = 0;

        this.budgetService.searchLines(this.searchOptions)
            .subscribe({
                next: (response: BudgetLineSearchResponse) => {
                    this.budgetLines = response.budgetLines;
                    this.headers = response.headers;
                    this.loading = false;
                },
                error: err => {
                    this.errorService.handleError(err, "Budget", "Load");
                    this.loading = false;
                }
            });

        this.loadSummary();
        this.loadBurnSeries();
    }

    currencyModeChanged(): void {
        this.loadSummary();
    }

    private loadSummary(): void {
        this.budgetService.summary(this.selectedProjectId, this.currencyMode)
            .subscribe({
                next: response => {
                    this.summary = response;
                    this.totals.planned = response.rows.reduce((sum, r) => sum + r.planned, 0);
                    this.totals.committed = response.rows.reduce((sum, r) => sum + r.committed, 0);
                    this.totals.disbursed = response.rows.reduce((sum, r) => sum + r.disbursed, 0);
                    this.totals.actual = response.rows.reduce((sum, r) => sum + r.actual, 0);
                    this.totals.currency = this.currencyMode === 'base'
                        ? response.baseCurrency
                        : (response.rows.length > 0 ? (response.rows[0].currency || "") : "");
                },
                error: err => this.errorService.handleError(err, "Budget", "Load summary")
            });
    }

    private loadBurnSeries(): void {
        this.budgetService.burnSeries(this.selectedProjectId)
            .subscribe({
                next: points => {
                    this.burnSeries = points;
                    this.buildBurnChart();
                },
                error: err => this.errorService.handleError(err, "Budget", "Load burn series")
            });
    }

    private buildBurnChart(): void {
        if (!this.burnSeries || this.burnSeries.length === 0) {
            this.burnChartOptions = null;
            return;
        }

        // cumulative planned for the S-curve baseline
        let cumulativePlanned = 0;
        const cumulativePlannedSeries = this.burnSeries.map(p => cumulativePlanned += p.planned);

        this.burnChartOptions = {
            tooltip: { trigger: 'axis' },
            legend: { data: ['Planned (cumulative)', 'Expended (cumulative)'] },
            grid: { left: 60, right: 20, top: 40, bottom: 60 },
            xAxis: {
                type: 'category',
                data: this.burnSeries.map(p => p.label),
                axisLabel: { rotate: 30 }
            },
            yAxis: { type: 'value' },
            series: [
                {
                    name: 'Planned (cumulative)',
                    type: 'line',
                    smooth: true,
                    data: cumulativePlannedSeries
                },
                {
                    name: 'Expended (cumulative)',
                    type: 'line',
                    smooth: true,
                    areaStyle: { opacity: 0.15 },
                    data: this.burnSeries.map(p => p.cumulativeExpended)
                }
            ]
        };
    }

    loadIndicators(): void {
        const searchOptions = new IndicatorSearchOptions();
        searchOptions.pageSize = 0;

        this.indicatorService.search(searchOptions)
            .subscribe({
                next: response => {
                    this.indicators = response.indicators;
                },
                error: err => this.errorService.handleError(err, "Indicators", "Load")
            });
    }

    indicatorsLoaded = false;

    openIndicatorPicker(): void {
        if (!this.indicatorsLoaded) {
            this.loadIndicators();
            this.indicatorsLoaded = true;
        }
    }

    indicatorChanged(): void {
        if (!this.selectedIndicatorId) {
            this.costPerOutcome = null;
            return;
        }

        this.budgetService.costPerOutcome(this.selectedProjectId, this.selectedIndicatorId)
            .subscribe({
                next: result => {
                    this.costPerOutcome = result;
                },
                error: err => this.errorService.handleError(err, "Cost per Outcome", "Load")
            });
    }

    get overallUtilization(): number {
        if (!this.totals.planned) return 0;
        return Math.round((this.totals.actual / this.totals.planned) * 100);
    }

    addLine(): void {
        if (!this.selectedProjectId) return;

        const line = new BudgetLine();
        line.projectId = this.selectedProjectId;

        const modalRef = this.modalService.open(BudgetLineModal, { centered: true });
        (modalRef.componentInstance as BudgetLineModal).initialise(line, this.categoriesFromSummary(), this.reportingCurrency());
        modalRef.result.then(
            () => this.load(),
            () => { /* dismissed */ }
        );
    }

    editLine(line: BudgetLine): void {
        const toEdit = new BudgetLine();
        Object.assign(toEdit, line);

        const modalRef = this.modalService.open(BudgetLineModal, { centered: true });
        (modalRef.componentInstance as BudgetLineModal).initialise(toEdit, this.categoriesFromSummary(), this.reportingCurrency());
        modalRef.result.then(
            () => this.load(),
            () => { /* dismissed */ }
        );
    }

    deleteLine(line: BudgetLine): void {
        if (!confirm(`Delete the budget line "${line.description || line.budgetCategory?.name}"?`)) return;

        this.budgetService.deleteLine(line.budgetLineId)
            .subscribe({
                next: () => this.load(),
                error: err => this.errorService.handleError(err, "Budget Line", "Delete")
            });
    }

    private reportingCurrency(): string {
        if (this.currencyMode === 'base' && this.summary.baseCurrency) return this.summary.baseCurrency;
        return this.totals.currency || "USD";
    }

    private categoriesFromSummary(): BudgetCategory[] {
        return (this.summary.rows || []).map(row => {
            const category = new BudgetCategory();
            category.budgetCategoryId = row.budgetCategoryId;
            category.name = row.categoryName;
            return category;
        });
    }

    progressBarClass(utilization: number): string {
        if (utilization > 100) return 'bg-danger';
        if (utilization > 85) return 'bg-warning';
        return 'bg-success';
    }

}
