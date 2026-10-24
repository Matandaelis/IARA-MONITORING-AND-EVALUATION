import { environment } from '../../../environments/environment';
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { IndicatorTarget, IndicatorVariance } from '../models/indicatortarget.model';

@Injectable({ providedIn: 'root' })
export class IndicatorTargetService {

    constructor(private http: HttpClient) { }

    getTargets(indicatorId: string, entityId?: string): Observable<IndicatorTarget[]> {
        let params = new HttpParams();
        if (entityId) {
            params = params.set('entityId', entityId);
        }
        return this.http.get<IndicatorTarget[]>(`${environment.baseApiUrl}indicators/${indicatorId}/targets`, { params });
    }

    saveTarget(indicatorId: string, target: IndicatorTarget): Observable<IndicatorTarget> {
        return this.http.post<IndicatorTarget>(`${environment.baseApiUrl}indicators/${indicatorId}/targets`, target);
    }

    batchSaveTargets(indicatorId: string, targets: IndicatorTarget[]): Observable<IndicatorTarget[]> {
        return this.http.post<IndicatorTarget[]>(`${environment.baseApiUrl}indicators/${indicatorId}/targets/batch`, targets);
    }

    deleteTarget(indicatorId: string, targetId: string): Observable<any> {
        return this.http.delete(`${environment.baseApiUrl}indicators/${indicatorId}/targets/${targetId}`);
    }

    getVariance(indicatorId: string, entityId?: string): Observable<IndicatorVariance> {
        let params = new HttpParams();
        if (entityId) {
            params = params.set('entityId', entityId);
        }
        return this.http.get<IndicatorVariance>(`${environment.baseApiUrl}indicators/${indicatorId}/variance`, { params });
    }
}
