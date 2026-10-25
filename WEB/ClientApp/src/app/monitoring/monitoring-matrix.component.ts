import { Component as NgComponent, OnInit } from '@angular/core';
import {
    MonitoringMatrix, MonitoringMatrixRow, MonitoringProject,
    monitoringTypeLabel, monitoringTypeOptions, ragClass, ragLabel, RagStatuses
} from '../common/models/monitoring.model';
import { MonitoringService } from '../common/services/monitoring.service';
import { ErrorService } from '../common/services/error.service';

@NgComponent({
    selector: 'app-monitoring-matrix',
    templateUrl: './monitoring-matrix.component.html',
    standalone: false,
    styles: [`
        .matrix-table th.sticky-col,
        .matrix-table td.sticky-col { position: sticky; left: 0; background: #fff; z-index: 1; }
        .matrix-table thead th.sticky-col { background: #f8f9fa; z-index: 2; }
        .matrix-table td.cell-rag { min-width: 92px; }
        .matrix-table .cell-actual { font-weight: 600; }
    `]
})
export class MonitoringMatrixComponent implements OnInit {

    projects: MonitoringProject[] = [];
    matrix = new MonitoringMatrix();
    selectedProjectId = '';
    selectedType: number | '' = '';
    q = '';
    loading = false;

    readonly typeOptions = monitoringTypeOptions;
    readonly rag = RagStatuses;
    readonly ragClass = ragClass;
    readonly ragLabel = ragLabel;

    constructor(
        private monitoringService: MonitoringService,
        private errorService: ErrorService
    ) {
    }

    ngOnInit(): void {
        this.selectedProjectId = this.monitoringService.getSelectedProjectId();
        this.loadProjects();
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

    projectChanged(): void {
        this.monitoringService.setSelectedProjectId(this.selectedProjectId);
        this.load();
    }

    load(): void {
        if (!this.selectedProjectId) return;

        this.loading = true;
        this.monitoringService.getMatrix(this.selectedProjectId).subscribe({
            next: matrix => {
                this.matrix = matrix;
                this.loading = false;
            },
            error: err => {
                this.errorService.handleError(err, 'Monitoring', 'Load matrix');
                this.loading = false;
            }
        });
    }

    get filteredRows(): MonitoringMatrixRow[] {
        const q = (this.q || '').trim().toLowerCase();

        return this.matrix.rows.filter(row => {
            if (this.selectedType !== '' && row.monitoringType !== this.selectedType) return false;
            if (!q) return true;
            return (row.name || '').toLowerCase().includes(q) ||
                (row.code || '').toLowerCase().includes(q) ||
                (row.responsibleUserName || '').toLowerCase().includes(q);
        });
    }

    get assessedCount(): number {
        return this.filteredRows.filter(row => row.coverage !== null).length;
    }

    get greenCount(): number {
        return this.filteredRows.filter(row => this.latestRag(row) === RagStatuses.Green).length;
    }

    get amberCount(): number {
        return this.filteredRows.filter(row => this.latestRag(row) === RagStatuses.Amber).length;
    }

    get redCount(): number {
        return this.filteredRows.filter(row => this.latestRag(row) === RagStatuses.Red).length;
    }

    latestRag(row: MonitoringMatrixRow): number {
        for (let i = row.cells.length - 1; i >= 0; i--) {
            if (row.cells[i].rag !== RagStatuses.NotAssessed) return row.cells[i].rag;
        }
        return RagStatuses.NotAssessed;
    }

    cell(row: MonitoringMatrixRow, index: number) {
        return row.cells[index];
    }

    typeLabel(value: number): string {
        return monitoringTypeLabel(value);
    }

    directionLabel(direction: number): string {
        return direction === 1 ? 'Lower is better' : 'Higher is better';
    }

    displayNumber(value: number, decimals = 1): string {
        if (value === null || value === undefined) return '—';
        return value.toLocaleString(undefined, { minimumFractionDigits: 0, maximumFractionDigits: decimals });
    }

    exportCsv(): void {
        const rows = this.filteredRows;
        const header = ['Code', 'Indicator', 'Monitoring type', 'Baseline', ...this.matrix.periods.map(o => o.label), 'Coverage %', 'Responsible'];
        const lines = [header.map(this.csvCell).join(',')];

        rows.forEach(row => {
            const values = [
                row.code,
                row.name,
                row.monitoringTypeLabel,
                row.baseline,
                ...row.cells.map(cell => cell.actual),
                row.coverage,
                row.responsibleUserName
            ];
            lines.push(values.map(this.csvCell).join(','));
        });

        const blob = new Blob([lines.join('\r\n')], { type: 'text/csv;charset=utf-8;' });
        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = `monitoring-matrix-${this.matrix.projectName || 'project'}.csv`;
        link.click();
        URL.revokeObjectURL(link.href);
    }

    private csvCell(value: any): string {
        const text = value === null || value === undefined ? '' : String(value);
        return `"${text.replace(/"/g, '""')}"`;
    }
}
