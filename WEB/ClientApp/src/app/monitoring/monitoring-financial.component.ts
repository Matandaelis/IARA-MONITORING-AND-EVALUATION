import { Component as NgComponent, OnInit } from '@angular/core';
import { BudgetBurnPoint, BudgetSummaryRow } from '../common/models/budget.model';
import { MonitoringFinancial, MonitoringPeriod, MonitoringProject } from '../common/models/monitoring.model';
import { MonitoringService } from '../common/services/monitoring.service';
import { ErrorService } from '../common/services/error.service';

@NgComponent({
    selector: 'app-monitoring-financial',
    templateUrl: './monitoring-financial.component.html',
    standalone: false
})
export class MonitoringFinancialComponent implements OnInit {

    projects: MonitoringProject[] = [];
    periods: MonitoringPeriod[] = [];
    financial = new MonitoringFinancial();

    selectedProjectId = '';
    selectedDateId = '';
    loading = false;

    constructor(
        private monitoringService: MonitoringService,
        private errorService: ErrorService
    ) {
    }

    ngOnInit(): void {
        this.selectedProjectId = this.monitoringService.getSelectedProjectId();
        this.loadProjects();
        this.loadPeriods();
    }

    loadProjects(): void {
        this.monitoringService.getProjects().subscribe({
            next: projects => {
                this.projects = projects;
                if (!this.selectedProjectId && projects.length > 0) {
                    this.selectedProjectId = projects[0].projectId;
                }
                if (this.selectedProjectId) {
                    this.monitoringService.setSelectedProjectId(this.selectedProjectId);
                    this.load();
                }
            },
            error: err => this.errorService.handleError(err, 'Monitoring', 'Load projects')
        });
    }

    loadPeriods(): void {
        this.monitoringService.getPeriods().subscribe({
            next: periods => this.periods = periods,
            error: err => this.errorService.handleError(err, 'Monitoring', 'Load periods')
        });
    }

    projectChanged(): void {
        this.monitoringService.setSelectedProjectId(this.selectedProjectId);
        this.load();
    }

    periodChanged(): void {
        this.load();
    }

    load(): void {
        if (!this.selectedProjectId) return;

        this.loading = true;
        this.monitoringService.getFinancial(this.selectedProjectId, this.selectedDateId || undefined).subscribe({
            next: financial => {
                this.financial = financial;
                this.loading = false;
            },
            error: err => {
                this.errorService.handleError(err, 'Monitoring', 'Load financial monitoring');
                this.loading = false;
            }
        });
    }

    get utilizationHealth(): number {
        if (!this.financial.utilization) return null;
        return Math.max(0, 100 - Math.abs(100 - this.financial.utilization));
    }

    get maxBurn(): number {
        if (!this.financial.burnSeries || this.financial.burnSeries.length === 0) return 0;
        return Math.max(...this.financial.burnSeries.map(o => Math.max(o.planned || 0, o.cumulativeExpended || 0)));
    }

    burnWidth(value: number): number {
        if (!this.maxBurn || !value) return 0;
        return Math.max(0, Math.min(100, (value / this.maxBurn) * 100));
    }

    percentOfPlan(actual: number, planned: number): number {
        if (!planned || planned <= 0) return 0;
        return Math.max(0, Math.min(100, (actual / planned) * 100));
    }

    utilizationClass(value: number): string {
        if (value === null || value === undefined) return 'secondary';
        const health = 100 - Math.abs(100 - value);
        if (health >= 90) return 'success';
        if (health >= 70) return 'warning';
        return 'danger';
    }

    varianceClass(value: number): string {
        if (value === null || value === undefined) return 'text-muted';
        return value < 0 ? 'text-danger' : 'text-success';
    }

    displayNumber(value: number, decimals = 1): string {
        if (value === null || value === undefined) return '—';
        return value.toLocaleString(undefined, { minimumFractionDigits: 0, maximumFractionDigits: decimals });
    }

    categoryVariance(row: BudgetSummaryRow): number {
        return (row.planned || 0) - (row.actual || 0);
    }

    burnVariance(point: BudgetBurnPoint): number {
        return (point.planned || 0) - (point.cumulativeExpended || 0);
    }

    exportCsv(): void {
        const header = ['Category', 'Planned', 'Committed', 'Disbursed', 'Actual', 'Variance', 'Utilization %', 'Currency'];
        const lines = [header.map(this.csvCell).join(',')];

        this.financial.rows.forEach(row => {
            lines.push([
                row.categoryName, row.planned, row.committed, row.disbursed, row.actual,
                this.categoryVariance(row), row.utilization, row.currency
            ].map(this.csvCell).join(','));
        });

        const blob = new Blob([lines.join('\r\n')], { type: 'text/csv;charset=utf-8;' });
        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = `financial-monitoring-${this.financial.projectId}.csv`;
        link.click();
        URL.revokeObjectURL(link.href);
    }

    private csvCell(value: any): string {
        const text = value === null || value === undefined ? '' : String(value);
        return `"${text.replace(/"/g, '""')}"`;
    }
}
