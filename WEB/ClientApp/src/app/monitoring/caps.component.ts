import { Component as NgComponent, OnInit } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import {
    CapTriggerRequest, MonitoringAction, MonitoringActionStatuses, actionStatusOptions
} from '../common/models/monitoring.model';
import { MonitoringService } from '../common/services/monitoring.service';
import { ErrorService } from '../common/services/error.service';

@NgComponent({
    selector: 'app-caps',
    templateUrl: './caps.component.html',
    standalone: false
})
export class CapsComponent implements OnInit {

    projects: any[] = [];
    selectedProjectId = '';
    caps: MonitoringAction[] = [];
    loading = false;
    triggering = false;

    newCap = new MonitoringAction();
    showCapForm = false;
    editingCap: MonitoringAction = null;

    readonly actionStatusOptions = actionStatusOptions;

    // Deviation threshold for the auto-trigger watcher (±15% default per M&E framework Tier 4)
    deviationThreshold = 15;
    slaBusinessDays = 7;

    constructor(
        private monitoringService: MonitoringService,
        private errorService: ErrorService,
        private toastr: ToastrService
    ) {
    }

    ngOnInit(): void {
        this.loadProjects();
    }

    loadProjects(): void {
        this.monitoringService.getProjects().subscribe({
            next: projects => {
                this.projects = projects;
                if (this.projects.length > 0 && !this.selectedProjectId) {
                    this.selectedProjectId = this.projects[0].projectId;
                    this.loadCaps();
                }
            },
            error: err => this.errorService.handleError(err, 'CAP', 'Load projects')
        });
    }

    projectChanged(): void {
        this.loadCaps();
    }

    loadCaps(): void {
        if (!this.selectedProjectId) return;
        this.loading = true;
        this.monitoringService.getCaps(this.selectedProjectId).subscribe({
            next: caps => {
                this.caps = caps;
                this.loading = false;
            },
            error: err => {
                this.errorService.handleError(err, 'CAP', 'Load corrective actions');
                this.loading = false;
            }
        });
    }

    // ── trigger watcher ────────────────────────────────────────────────
    triggerWatcher(): void {
        if (!this.selectedProjectId) return;

        this.triggering = true;
        const req: CapTriggerRequest = {
            projectId: this.selectedProjectId,
            dateId: null,
            deviationThresholdPercent: this.deviationThreshold,
            slaBusinessDays: this.slaBusinessDays
        };

        this.monitoringService.triggerCaps(req).subscribe({
            next: created => {
                this.triggering = false;
                this.loadCaps();
                if (created.length > 0) {
                    this.toastr.success(`${created.length} corrective action(s) triggered for off-track indicators.`);
                } else {
                    this.toastr.info('No new corrective actions triggered — all indicators within tolerance or already have open CAPs.');
                }
            },
            error: err => {
                this.triggering = false;
                this.errorService.handleError(err, 'CAP', 'Trigger deviation watcher');
            }
        });
    }

    // ── CAP form ───────────────────────────────────────────────────────
    startCap(): void {
        this.newCap = new MonitoringAction();
        this.newCap.projectId = this.selectedProjectId;
        this.newCap.status = MonitoringActionStatuses.Open;
        this.newCap.triggerSource = 'Manual';
        this.showCapForm = true;
    }

    cancelCap(): void {
        this.showCapForm = false;
        this.newCap = new MonitoringAction();
    }

    addCap(): void {
        if (!this.newCap.description) {
            this.toastr.warning('Describe the corrective action');
            return;
        }

        this.monitoringService.saveCap(this.newCap).subscribe({
            next: saved => {
                this.caps.unshift(saved);
                this.showCapForm = false;
                this.newCap = new MonitoringAction();
                this.toastr.success('Corrective action recorded');
            },
            error: err => this.errorService.handleError(err, 'CAP', 'Save corrective action')
        });
    }

    startEdit(cap: MonitoringAction): void {
        this.editingCap = { ...cap };
    }

    cancelEdit(): void {
        this.editingCap = null;
    }

    saveEdit(cap: MonitoringAction): void {
        if (!cap.description) {
            this.toastr.warning('Describe the corrective action');
            return;
        }
        this.monitoringService.saveCap(cap).subscribe({
            next: saved => {
                Object.assign(cap, saved);
                this.editingCap = null;
                this.toastr.success('Corrective action updated');
            },
            error: err => this.errorService.handleError(err, 'CAP', 'Update corrective action')
        });
    }

    deleteCap(cap: MonitoringAction): void {
        if (!window.confirm(`Delete this corrective action?`)) return;

        this.monitoringService.deleteCap(cap.monitoringActionId).subscribe({
            next: () => {
                this.caps = this.caps.filter(o => o.monitoringActionId !== cap.monitoringActionId);
                this.toastr.success('Corrective action deleted');
            },
            error: err => this.errorService.handleError(err, 'CAP', 'Delete corrective action')
        });
    }

    capStatusClass(status: number): string {
        switch (status) {
            case 2: return 'success';
            case 3: return 'danger';
            case 4: return 'secondary';
            case 1: return 'info';
            default: return 'warning text-dark';
        }
    }

    deviationClass(deviation: number): string {
        if (deviation == null) return 'secondary';
        return deviation < 0 ? 'danger' : 'warning text-dark';
    }
}
