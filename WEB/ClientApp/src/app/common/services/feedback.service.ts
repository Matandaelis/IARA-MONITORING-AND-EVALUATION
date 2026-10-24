import { environment } from '../../../environments/environment';
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { BeneficiaryFeedback, FeedbackSearchOptions, FeedbackSearchResponse, FeedbackStats } from '../models/feedback.model';
import { SearchQuery, PagingHeaders } from '../models/http.model';

@Injectable({ providedIn: 'root' })
export class FeedbackService extends SearchQuery {

    constructor(private http: HttpClient) {
        super();
    }

    search(params: FeedbackSearchOptions): Observable<FeedbackSearchResponse> {
        const queryParams: HttpParams = this.buildQueryParams(params);
        return this.http.get(`${environment.baseApiUrl}beneficiaryfeedback`, { params: queryParams, observe: 'response' })
            .pipe(
                map(response => {
                    const headers = JSON.parse(response.headers.get("x-pagination") || '{}') as PagingHeaders;
                    const items = response.body as BeneficiaryFeedback[];
                    return { items: items, headers: headers };
                })
            );
    }

    get(id: string): Observable<BeneficiaryFeedback> {
        return this.http.get<BeneficiaryFeedback>(`${environment.baseApiUrl}beneficiaryfeedback/${id}`);
    }

    save(feedback: BeneficiaryFeedback): Observable<BeneficiaryFeedback> {
        if (!feedback.id || feedback.id === '00000000-0000-0000-0000-000000000000') {
            return this.http.post<BeneficiaryFeedback>(`${environment.baseApiUrl}beneficiaryfeedback`, feedback);
        } else {
            return this.http.put<BeneficiaryFeedback>(`${environment.baseApiUrl}beneficiaryfeedback/${feedback.id}`, feedback);
        }
    }

    delete(id: string): Observable<void> {
        return this.http.delete<void>(`${environment.baseApiUrl}beneficiaryfeedback/${id}`);
    }

    getStats(projectId?: string): Observable<FeedbackStats> {
        let params = new HttpParams();
        if (projectId) {
            params = params.set('projectId', projectId);
        }
        return this.http.get<FeedbackStats>(`${environment.baseApiUrl}beneficiaryfeedback/stats`, { params });
    }
}
