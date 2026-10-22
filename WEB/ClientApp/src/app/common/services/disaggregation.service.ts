import { environment } from '../../../environments/environment';
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DisaggregationGroup, DisaggregationOption } from '../models/disaggregation.model';

@Injectable({ providedIn: 'root' })
export class DisaggregationService {

    constructor(private http: HttpClient) { }

    getGroups(includeInactive: boolean = false): Observable<DisaggregationGroup[]> {
        const query = includeInactive ? '?includeInactive=true' : '';
        return this.http.get<DisaggregationGroup[]>(`${environment.baseApiUrl}disaggregationgroups${query}`);
    }

    getGroup(groupId: string): Observable<DisaggregationGroup> {
        return this.http.get<DisaggregationGroup>(`${environment.baseApiUrl}disaggregationgroups/${groupId}`);
    }

    saveGroup(group: DisaggregationGroup): Observable<DisaggregationGroup> {
        if (group.disaggregationGroupId) {
            return this.http.put<DisaggregationGroup>(`${environment.baseApiUrl}disaggregationgroups/${group.disaggregationGroupId}`, group);
        } else {
            return this.http.post<DisaggregationGroup>(`${environment.baseApiUrl}disaggregationgroups`, group);
        }
    }

    deleteGroup(groupId: string): Observable<any> {
        return this.http.delete(`${environment.baseApiUrl}disaggregationgroups/${groupId}`);
    }

    addOption(groupId: string, option: DisaggregationOption): Observable<DisaggregationOption> {
        return this.http.post<DisaggregationOption>(`${environment.baseApiUrl}disaggregationgroups/${groupId}/options`, option);
    }
}
