import { Component as NgComponent, OnInit } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { forkJoin } from 'rxjs';
import {
    actionStatusOptions, checkStatusOptions, checkTypeOptions, eventStatusOptions, eventTypeOptions,
    LessonLearned, MonitoringAction, MonitoringCheck, MonitoringEvent, MonitoringEventStatuses,
    MonitoringCheckStatuses, MonitoringCheckTypes, MonitoringEventTypes, MonitoringProject,
    MonitoringTypes, RiskAssumption, RiskAssumptionUpdate, RiskStatuses, riskStatusOptions,
    riskTypeOptions, monitoringTypeOptions
} from '../common/models/monitoring.model';
import { MonitoringService } from '../common/services/monitoring.service';
import { ErrorService } from '../common/services/error.service';

type RegisterTab = 'risks' | 'events' | 'checks' | 'lessons';

@NgComponent({
    selector: 'app-monitoring-registers',
    templateUrl: './monitoring-registers.component.html',
    standalone: false
})
export class MonitoringRegistersComponent implements OnInit {

    projects: MonitoringProject[] = [];
    selectedProjectId = '';
    activeTab: RegisterTab = 'risks';
    loading = false;
    saving = false;

    risks: RiskAssumption[] = [];
    events: MonitoringEvent[] = [];
    checks: MonitoringCheck[] = [];
    lessons: LessonLearned[] = [];

    newRisk = new RiskAssumption();
    newEvent = new MonitoringEvent();
    newCheck = new MonitoringCheck();
    newLesson = new LessonLearned();
    newAction = new MonitoringAction();
    newRiskUpdate = new RiskAssumptionUpdate();

    showRiskForm = false;
    showEventForm = false;
    showCheckForm = false;
    showLessonForm = false;
    showActionForm = false;
    showRiskUpdateForm = false;

    eventForAction: MonitoringEvent = null;
    riskForUpdate: RiskAssumption = null;

    readonly riskTypeOptions = riskTypeOptions;
    readonly riskStatusOptions = riskStatusOptions;
    readonly eventTypeOptions = eventTypeOptions;
    readonly eventStatusOptions = eventStatusOptions;
    readonly actionStatusOptions = actionStatusOptions;
    readonly checkTypeOptions = checkTypeOptions;
    readonly checkStatusOptions = checkStatusOptions;
    readonly monitoringTypeOptions = monitoringTypeOptions;

    constructor(
        private monitoringService: MonitoringService,
        private errorService: ErrorService,
        private toastr: ToastrService
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
        this.resetForms();
        this.load();
    }

    load(showSpinner = true): void {
        if (!this.selectedProjectId) return;
        if (showSpinner) this.loading = true;

        forkJoin({
            risks: this.monitoringService.getRisks(this.selectedProjectId),
            events: this.monitoringService.getEvents(this.selectedProjectId),
            checks: this.monitoringService.getChecks(this.selectedProjectId),
            lessons: this.monitoringService.getLessons(this.selectedProjectId)
        }).subscribe({
            next: result => {
                this.risks = result.risks;
                this.events = result.events;
                this.checks = result.checks;
                this.lessons = result.lessons;
                this.loading = false;
            },
            error: err => {
                this.errorService.handleError(err, 'Monitoring', 'Load registers');
                this.loading = false;
            }
        });
    }

    setTab(tab: RegisterTab): void {
        this.activeTab = tab;
    }

    // ── risks & assumptions ────────────────────────────────────────────
    startRisk(): void {
        this.newRisk = new RiskAssumption();
        this.newRisk.projectId = this.selectedProjectId;
        this.newRisk.likelihood = 3;
        this.newRisk.impact = 3;
        this.newRisk.status = RiskStatuses.Open;
        this.showRiskForm = true;
    }

    cancelRisk(): void {
        this.showRiskForm = false;
    }

    addRisk(): void {
        if (!this.newRisk.title) {
            this.toastr.warning('Give the risk or assumption a title');
            return;
        }

        this.saving = true;
        this.monitoringService.saveRisk(this.newRisk).subscribe({
            next: saved => {
                this.risks.unshift(saved);
                this.showRiskForm = false;
                this.newRisk = new RiskAssumption();
                this.saving = false;
                this.toastr.success('Risk / assumption recorded');
            },
            error: err => {
                this.errorService.handleError(err, 'Monitoring', 'Save risk');
                this.saving = false;
            }
        });
    }

    riskStatusChanged(risk: RiskAssumption): void {
        this.monitoringService.saveRisk(risk).subscribe({
            next: saved => Object.assign(risk, saved),
            error: err => this.errorService.handleError(err, 'Monitoring', 'Update risk status')
        });
    }

    deleteRisk(risk: RiskAssumption): void {
        if (!window.confirm(`Delete "${risk.title}"?`)) return;

        this.monitoringService.deleteRisk(risk.riskAssumptionId).subscribe({
            next: () => {
                this.risks = this.risks.filter(o => o.riskAssumptionId !== risk.riskAssumptionId);
                this.toastr.success('Risk / assumption deleted');
            },
            error: err => this.errorService.handleError(err, 'Monitoring', 'Delete risk')
        });
    }

    startRiskUpdate(risk: RiskAssumption): void {
        this.riskForUpdate = risk;
        this.newRiskUpdate = new RiskAssumptionUpdate();
        this.newRiskUpdate.riskAssumptionId = risk.riskAssumptionId;
        this.newRiskUpdate.likelihood = risk.likelihood;
        this.newRiskUpdate.impact = risk.impact;
        this.newRiskUpdate.status = risk.status;
        this.showRiskUpdateForm = true;
        this.showRiskForm = false;
    }

    cancelRiskUpdate(): void {
        this.showRiskUpdateForm = false;
        this.riskForUpdate = null;
    }

    addRiskUpdate(): void {
        if (!this.newRiskUpdate.riskAssumptionId) return;

        this.saving = true;
        this.monitoringService.saveRiskUpdate(this.newRiskUpdate).subscribe({
            next: () => {
                this.showRiskUpdateForm = false;
                this.riskForUpdate = null;
                this.newRiskUpdate = new RiskAssumptionUpdate();
                this.saving = false;
                this.toastr.success('Risk assessment recorded');
                this.load(false);
            },
            error: err => {
                this.errorService.handleError(err, 'Monitoring', 'Save risk assessment');
                this.saving = false;
            }
        });
    }

    riskScoreClass(score: number): string {
        if (score >= 15) return 'danger';
        if (score >= 8) return 'warning text-dark';
        return 'success';
    }

    // ── monitoring events & actions ───────────────────────────────────
    startEvent(): void {
        this.newEvent = new MonitoringEvent();
        this.newEvent.projectId = this.selectedProjectId;
        this.newEvent.type = MonitoringEventTypes.FieldVisit;
        this.newEvent.status = MonitoringEventStatuses.Completed;
        this.newEvent.eventDate = new Date().toISOString().substring(0, 10);
        this.showEventForm = true;
    }

    cancelEvent(): void {
        this.showEventForm = false;
    }

    addEvent(): void {
        if (!this.newEvent.title) {
            this.toastr.warning('Give the monitoring event a title');
            return;
        }

        this.saving = true;
        this.monitoringService.saveEvent(this.newEvent).subscribe({
            next: saved => {
                this.events.unshift(saved);
                this.showEventForm = false;
                this.newEvent = new MonitoringEvent();
                this.saving = false;
                this.toastr.success('Monitoring event recorded');
            },
            error: err => {
                this.errorService.handleError(err, 'Monitoring', 'Save event');
                this.saving = false;
            }
        });
    }

    eventStatusChanged(monitoringEvent: MonitoringEvent): void {
        this.monitoringService.saveEvent(monitoringEvent).subscribe({
            next: saved => Object.assign(monitoringEvent, saved),
            error: err => this.errorService.handleError(err, 'Monitoring', 'Update event status')
        });
    }

    deleteEvent(monitoringEvent: MonitoringEvent): void {
        if (!window.confirm(`Delete "${monitoringEvent.title}"?`)) return;

        this.monitoringService.deleteEvent(monitoringEvent.monitoringEventId).subscribe({
            next: () => {
                this.events = this.events.filter(o => o.monitoringEventId !== monitoringEvent.monitoringEventId);
                this.toastr.success('Monitoring event deleted');
            },
            error: err => this.errorService.handleError(err, 'Monitoring', 'Delete event')
        });
    }

    startAction(monitoringEvent: MonitoringEvent): void {
        this.eventForAction = monitoringEvent;
        this.newAction = new MonitoringAction();
        this.newAction.projectId = this.selectedProjectId;
        this.newAction.monitoringEventId = monitoringEvent.monitoringEventId;
        this.newAction.status = 0;
        this.showActionForm = true;
        this.showEventForm = false;
    }

    cancelAction(): void {
        this.showActionForm = false;
        this.eventForAction = null;
    }

    addAction(): void {
        if (!this.newAction.description) {
            this.toastr.warning('Describe the follow-up action');
            return;
        }

        this.saving = true;
        this.monitoringService.saveAction(this.newAction).subscribe({
            next: () => {
                this.showActionForm = false;
                this.eventForAction = null;
                this.newAction = new MonitoringAction();
                this.saving = false;
                this.toastr.success('Follow-up action recorded');
                this.load(false);
            },
            error: err => {
                this.errorService.handleError(err, 'Monitoring', 'Save action');
                this.saving = false;
            }
        });
    }

    actionStatusChanged(action: MonitoringAction): void {
        this.monitoringService.saveAction(action).subscribe({
            next: saved => Object.assign(action, saved),
            error: err => this.errorService.handleError(err, 'Monitoring', 'Update action status')
        });
    }

    deleteAction(action: MonitoringAction): void {
        if (!window.confirm('Delete this follow-up action?')) return;

        this.monitoringService.deleteAction(action.monitoringActionId).subscribe({
            next: () => {
                this.events.forEach(o => o.actions = o.actions.filter(a => a.monitoringActionId !== action.monitoringActionId));
                this.toastr.success('Action deleted');
            },
            error: err => this.errorService.handleError(err, 'Monitoring', 'Delete action')
        });
    }

    actionStatusClass(status: number): string {
        switch (status) {
            case 2: return 'success';
            case 3: return 'danger';
            case 4: return 'secondary';
            case 1: return 'info';
            default: return 'warning text-dark';
        }
    }

    // ── compliance & administrative checks ────────────────────────────
    startCheck(type: MonitoringCheckTypes): void {
        this.newCheck = new MonitoringCheck();
        this.newCheck.projectId = this.selectedProjectId;
        this.newCheck.type = type;
        this.newCheck.status = MonitoringCheckStatuses.Pending;
        this.showCheckForm = true;
    }

    cancelCheck(): void {
        this.showCheckForm = false;
    }

    addCheck(): void {
        if (!this.newCheck.title) {
            this.toastr.warning('Give the check a title');
            return;
        }

        this.saving = true;
        this.monitoringService.saveCheck(this.newCheck).subscribe({
            next: saved => {
                this.checks.push(saved);
                this.showCheckForm = false;
                this.newCheck = new MonitoringCheck();
                this.saving = false;
                this.toastr.success('Check recorded');
            },
            error: err => {
                this.errorService.handleError(err, 'Monitoring', 'Save check');
                this.saving = false;
            }
        });
    }

    checkStatusChanged(check: MonitoringCheck): void {
        this.monitoringService.saveCheck(check).subscribe({
            next: saved => Object.assign(check, saved),
            error: err => this.errorService.handleError(err, 'Monitoring', 'Update check status')
        });
    }

    deleteCheck(check: MonitoringCheck): void {
        if (!window.confirm(`Delete "${check.title}"?`)) return;

        this.monitoringService.deleteCheck(check.monitoringCheckId).subscribe({
            next: () => {
                this.checks = this.checks.filter(o => o.monitoringCheckId !== check.monitoringCheckId);
                this.toastr.success('Check deleted');
            },
            error: err => this.errorService.handleError(err, 'Monitoring', 'Delete check')
        });
    }

    checkStatusClass(status: number): string {
        switch (status) {
            case 1: return 'success';
            case 2: return 'danger';
            case 3: return 'secondary';
            default: return 'warning text-dark';
        }
    }

    // ── lessons learned ───────────────────────────────────────────────
    startLesson(): void {
        this.newLesson = new LessonLearned();
        this.newLesson.projectId = this.selectedProjectId;
        this.newLesson.monitoringType = MonitoringTypes.Results;
        this.showLessonForm = true;
    }

    cancelLesson(): void {
        this.showLessonForm = false;
    }

    addLesson(): void {
        if (!this.newLesson.title) {
            this.toastr.warning('Give the lesson a title');
            return;
        }

        this.saving = true;
        this.monitoringService.saveLesson(this.newLesson).subscribe({
            next: saved => {
                this.lessons.unshift(saved);
                this.showLessonForm = false;
                this.newLesson = new LessonLearned();
                this.saving = false;
                this.toastr.success('Lesson recorded');
            },
            error: err => {
                this.errorService.handleError(err, 'Monitoring', 'Save lesson');
                this.saving = false;
            }
        });
    }

    deleteLesson(lesson: LessonLearned): void {
        if (!window.confirm(`Delete "${lesson.title}"?`)) return;

        this.monitoringService.deleteLesson(lesson.lessonLearnedId).subscribe({
            next: () => {
                this.lessons = this.lessons.filter(o => o.lessonLearnedId !== lesson.lessonLearnedId);
                this.toastr.success('Lesson deleted');
            },
            error: err => this.errorService.handleError(err, 'Monitoring', 'Delete lesson')
        });
    }

    private resetForms(): void {
        this.showRiskForm = false;
        this.showEventForm = false;
        this.showCheckForm = false;
        this.showLessonForm = false;
        this.showActionForm = false;
        this.showRiskUpdateForm = false;
        this.eventForAction = null;
        this.riskForUpdate = null;
    }
}
