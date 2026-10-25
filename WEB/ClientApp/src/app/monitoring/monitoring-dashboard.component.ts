import { Component as NgComponent, OnInit } from '@angular/core';
import {
    MonitoringFinancial, MonitoringLogFrame, MonitoringPeriod, MonitoringProject, MonitoringResultNode,
    MonitoringResults, MonitoringSummary, monitoringTypeLabel, ragClass, ragLabel, RagStatuses
} from '../common/models/monitoring.model';
import { MonitoringService } from '../common/services/monitoring.service';
import { ErrorService } from '../common/services/error.service';

@NgComponent({
    selector: 'app-monitoring-dashboard',
    templateUrl: './monitoring-dashboard.component.html',
    standalone: false
})
export class MonitoringDashboardComponent implements OnInit {

    projects: MonitoringProject[] = [];
    periods: MonitoringPeriod[] = [];
    logFrames: MonitoringLogFrame[] = [];

    selectedProjectId = '';
    selectedDateId = '';
    selectedLogFrameId = '';

    summary = new MonitoringSummary();
    financial = new MonitoringFinancial();
    results: MonitoringResults = null;
    flatResults: MonitoringResultNode[] = [];

    loading = false;
    resultsLoading = false;

    readonly types = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
    readonly rag = RagStatuses;
    readonly typeLabel = monitoringTypeLabel;
    readonly ragLabel = ragLabel;
    readonly ragClass = ragClass;

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
                    this.loadLogFrames();
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
        this.selectedLogFrameId = '';
        this.results = null;
        this.flatResults = [];
        this.load();
        this.loadLogFrames();
    }

    periodChanged(): void {
        this.load();
        if (this.selectedLogFrameId) this.loadResults();
    }

    load(): void {
        if (!this.selectedProjectId) return;

        this.loading = true;
        this.monitoringService.getSummary(this.selectedProjectId, this.selectedDateId || undefined).subscribe({
            next: summary => {
                this.summary = summary;
                this.financial = summary.financial;
                this.loading = false;
            },
            error: err => {
                this.errorService.handleError(err, 'Monitoring', 'Load summary');
                this.loading = false;
            }
        });
    }

    loadLogFrames(): void {
        if (!this.selectedProjectId) return;

        this.monitoringService.getLogFrames(this.selectedProjectId).subscribe({
            next: logFrames => {
                this.logFrames = logFrames;
                if (logFrames.length > 0) {
                    if (!this.selectedLogFrameId || !logFrames.some(o => o.logFrameId === this.selectedLogFrameId)) {
                        this.selectedLogFrameId = logFrames[0].logFrameId;
                    }
                    this.loadResults();
                }
            },
            error: err => this.errorService.handleError(err, 'Monitoring', 'Load logframes')
        });
    }

    loadResults(): void {
        if (!this.selectedLogFrameId) return;

        this.resultsLoading = true;
        this.monitoringService.getResults(this.selectedLogFrameId, this.selectedDateId || undefined).subscribe({
            next: results => {
                this.results = results;
                this.flatResults = [];
                this.flatten(results.nodes, 0);
                this.resultsLoading = false;
            },
            error: err => {
                this.errorService.handleError(err, 'Monitoring', 'Load results');
                this.resultsLoading = false;
            }
        });
    }

    logFrameChanged(): void {
        this.loadResults();
    }

    flatten(nodes: MonitoringResultNode[], depth: number): void {
        nodes.forEach(node => {
            node.depth = depth;
            node.expanded = node.expanded !== false;
            this.flatResults.push(node);
            if (node.expanded && node.children && node.children.length) {
                this.flatten(node.children, depth + 1);
            }
        });
    }

    toggle(node: MonitoringResultNode): void {
        node.expanded = !node.expanded;
        this.flatResults = [];
        if (this.results) this.flatten(this.results.nodes, 0);
    }

    scoreClass(value: number): string {
        if (value === null || value === undefined) return 'secondary';
        if (value >= 80) return 'success';
        if (value >= 60) return 'warning';
        return 'danger';
    }

    progressWidth(value: number): number {
        if (value === null || value === undefined) return 0;
        return Math.max(0, Math.min(100, value));
    }

    barWidth(part: number, total: number): number {
        if (!total || total <= 0) return 0;
        return Math.round((part / total) * 100);
    }

    displayNumber(value: number, decimals = 1): string {
        if (value === null || value === undefined) return '—';
        return value.toLocaleString(undefined, { minimumFractionDigits: decimals, maximumFractionDigits: decimals });
    }
}
