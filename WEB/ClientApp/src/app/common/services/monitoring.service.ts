import { environment } from '../../../environments/environment';
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
    CapTriggerRequest, LessonLearned, MonitoringAction, MonitoringCheck, MonitoringCoverage, MonitoringEvent,
    MonitoringFinancial, MonitoringIndicatorPerformance, MonitoringLogFrame, MonitoringLogFrameNode, MonitoringMatrix,
    MonitoringPeriod, MonitoringPlan, MonitoringPlanItem, MonitoringProject, MonitoringResults,
    MonitoringSummary, ProjectIndicator, RiskAssumption, RiskAssumptionUpdate
} from '../models/monitoring.model';

@Injectable({ providedIn: 'root' })
export class MonitoringService {

    private readonly api = `${environment.baseApiUrl}monitoring`;
    private readonly storageKey = 'monic.monitoring.projectId';

    constructor(private http: HttpClient) {
    }

    // ── project context ────────────────────────────────────────────────
    getSelectedProjectId(): string {
        try {
            return typeof localStorage === 'undefined' ? '' : (localStorage.getItem(this.storageKey) || '');
        } catch {
            return '';
        }
    }

    setSelectedProjectId(projectId: string): void {
        try {
            if (typeof localStorage !== 'undefined' && projectId) localStorage.setItem(this.storageKey, projectId);
        } catch {
            // storage may be unavailable; selection simply won't persist
        }
    }

    getProjects(): Observable<MonitoringProject[]> {
        return this.http.get<MonitoringProject[]>(`${this.api}/projects`);
    }

    getPeriods(): Observable<MonitoringPeriod[]> {
        return this.http.get<MonitoringPeriod[]>(`${this.api}/periods`);
    }

    // ── computed monitoring ────────────────────────────────────────────
    getSummary(projectId: string, dateId?: string): Observable<MonitoringSummary> {
        return this.http.get<MonitoringSummary>(`${this.api}/summary`, { params: this.periodParams(projectId, dateId) });
    }

    getMatrix(projectId: string, dateId?: string): Observable<MonitoringMatrix> {
        return this.http.get<MonitoringMatrix>(`${this.api}/matrix`, { params: this.periodParams(projectId, dateId) });
    }

    getIndicators(projectId: string, monitoringType?: number, dateId?: string): Observable<MonitoringIndicatorPerformance[]> {
        let params = this.periodParams(projectId, dateId);
        if (monitoringType !== undefined && monitoringType !== null) params = params.set('monitoringType', monitoringType.toString());
        return this.http.get<MonitoringIndicatorPerformance[]>(`${this.api}/indicators`, { params: params });
    }

    getResults(logFrameId: string, dateId?: string): Observable<MonitoringResults> {
        let params = new HttpParams().set('logFrameId', logFrameId);
        if (dateId) params = params.set('dateId', dateId);
        return this.http.get<MonitoringResults>(`${this.api}/results`, { params: params });
    }

    getFinancial(projectId: string, dateId?: string): Observable<MonitoringFinancial> {
        return this.http.get<MonitoringFinancial>(`${this.api}/financial`, { params: this.periodParams(projectId, dateId) });
    }

    getCoverage(projectId: string): Observable<MonitoringCoverage> {
        return this.http.get<MonitoringCoverage>(`${this.api}/coverage`, {
            params: new HttpParams().set('projectId', projectId)
        });
    }

    getLogFrames(projectId: string): Observable<MonitoringLogFrame[]> {
        return this.http.get<MonitoringLogFrame[]>(`${this.api}/logframes`, {
            params: new HttpParams().set('projectId', projectId)
        });
    }

    getLogFrameNodes(projectId: string): Observable<MonitoringLogFrameNode[]> {
        return this.http.get<MonitoringLogFrameNode[]>(`${this.api}/logframenodes`, {
            params: new HttpParams().set('projectId', projectId)
        });
    }

    // ── monitoring plans ───────────────────────────────────────────────
    getPlans(projectId: string): Observable<MonitoringPlan[]> {
        return this.http.get<MonitoringPlan[]>(`${environment.baseApiUrl}monitoringplans`, {
            params: new HttpParams().set('projectId', projectId)
        });
    }

    savePlan(plan: MonitoringPlan): Observable<MonitoringPlan> {
        return this.save('monitoringplans', plan.monitoringPlanId, plan);
    }

    deletePlan(id: string): Observable<void> {
        return this.http.delete<void>(`${environment.baseApiUrl}monitoringplans/${id}`);
    }

    savePlanItem(item: MonitoringPlanItem): Observable<MonitoringPlanItem> {
        return this.save('monitoringplans/items', item.monitoringPlanItemId, item);
    }

    deletePlanItem(id: string): Observable<void> {
        return this.http.delete<void>(`${environment.baseApiUrl}monitoringplans/items/${id}`);
    }

    // ── risk & assumption register ─────────────────────────────────────
    getRisks(projectId: string): Observable<RiskAssumption[]> {
        return this.http.get<RiskAssumption[]>(`${environment.baseApiUrl}riskassumptions`, {
            params: new HttpParams().set('projectId', projectId)
        });
    }

    saveRisk(risk: RiskAssumption): Observable<RiskAssumption> {
        return this.save('riskassumptions', risk.riskAssumptionId, risk);
    }

    deleteRisk(id: string): Observable<void> {
        return this.http.delete<void>(`${environment.baseApiUrl}riskassumptions/${id}`);
    }

    saveRiskUpdate(update: RiskAssumptionUpdate): Observable<RiskAssumptionUpdate> {
        return this.http.post<RiskAssumptionUpdate>(`${environment.baseApiUrl}riskassumptions/updates`, update);
    }

    // ── monitoring events & actions ────────────────────────────────────
    getEvents(projectId: string): Observable<MonitoringEvent[]> {
        return this.http.get<MonitoringEvent[]>(`${environment.baseApiUrl}monitoringevents`, {
            params: new HttpParams().set('projectId', projectId)
        });
    }

    saveEvent(monitoringEvent: MonitoringEvent): Observable<MonitoringEvent> {
        return this.save('monitoringevents', monitoringEvent.monitoringEventId, monitoringEvent);
    }

    deleteEvent(id: string): Observable<void> {
        return this.http.delete<void>(`${environment.baseApiUrl}monitoringevents/${id}`);
    }

    saveAction(action: MonitoringAction): Observable<MonitoringAction> {
        return this.save('monitoringevents/actions', action.monitoringActionId, action);
    }

    deleteAction(id: string): Observable<void> {
        return this.http.delete<void>(`${environment.baseApiUrl}monitoringevents/actions/${id}`);
    }

    // ── corrective action plans (CAP) ──────────────────────────────────
    getCaps(projectId: string, status?: number, triggerSource?: string): Observable<MonitoringAction[]> {
        let params = new HttpParams().set('projectId', projectId);
        if (status !== undefined && status !== null) params = params.set('status', status.toString());
        if (triggerSource) params = params.set('triggerSource', triggerSource);
        return this.http.get<MonitoringAction[]>(`${environment.baseApiUrl}caps`, { params });
    }

    getCap(id: string): Observable<MonitoringAction> {
        return this.http.get<MonitoringAction>(`${environment.baseApiUrl}caps/${id}`);
    }

    triggerCaps(req: CapTriggerRequest): Observable<MonitoringAction[]> {
        return this.http.post<MonitoringAction[]>(`${environment.baseApiUrl}caps/trigger`, req);
    }

    saveCap(cap: MonitoringAction): Observable<MonitoringAction> {
        return this.save('caps', cap.monitoringActionId, cap);
    }

    deleteCap(id: string): Observable<void> {
        return this.http.delete<void>(`${environment.baseApiUrl}caps/${id}`);
    }

    // ── compliance & administrative checks ─────────────────────────────
    getChecks(projectId: string): Observable<MonitoringCheck[]> {
        return this.http.get<MonitoringCheck[]>(`${environment.baseApiUrl}monitoringchecks`, {
            params: new HttpParams().set('projectId', projectId)
        });
    }

    saveCheck(check: MonitoringCheck): Observable<MonitoringCheck> {
        return this.save('monitoringchecks', check.monitoringCheckId, check);
    }

    deleteCheck(id: string): Observable<void> {
        return this.http.delete<void>(`${environment.baseApiUrl}monitoringchecks/${id}`);
    }

    // ── lessons learned ────────────────────────────────────────────────
    getLessons(projectId: string): Observable<LessonLearned[]> {
        return this.http.get<LessonLearned[]>(`${environment.baseApiUrl}lessonslearned`, {
            params: new HttpParams().set('projectId', projectId)
        });
    }

    saveLesson(lesson: LessonLearned): Observable<LessonLearned> {
        return this.save('lessonslearned', lesson.lessonLearnedId, lesson);
    }

    deleteLesson(id: string): Observable<void> {
        return this.http.delete<void>(`${environment.baseApiUrl}lessonslearned/${id}`);
    }

    // ── project indicator scope ────────────────────────────────────────
    getProjectIndicators(projectId: string): Observable<ProjectIndicator[]> {
        return this.http.get<ProjectIndicator[]>(`${environment.baseApiUrl}projectindicators`, {
            params: new HttpParams().set('projectId', projectId)
        });
    }

    addProjectIndicator(projectId: string, indicatorId: string): Observable<ProjectIndicator> {
        return this.http.post<ProjectIndicator>(`${environment.baseApiUrl}projectindicators`, {
            projectId: projectId,
            indicatorId: indicatorId,
            isMandatory: false,
            sortOrder: 0
        });
    }

    removeProjectIndicator(projectId: string, indicatorId: string): Observable<void> {
        return this.http.delete<void>(`${environment.baseApiUrl}projectindicators/${projectId}/${indicatorId}`);
    }

    // ── helpers ────────────────────────────────────────────────────────
    private periodParams(projectId: string, dateId?: string): HttpParams {
        let params = new HttpParams().set('projectId', projectId);
        if (dateId) params = params.set('dateId', dateId);
        return params;
    }

    private save<T>(path: string, id: string, entity: T): Observable<T> {
        const url = `${environment.baseApiUrl}${path}`;
        return id
            ? this.http.put<T>(`${url}/${id}`, entity)
            : this.http.post<T>(url, entity);
    }
}
