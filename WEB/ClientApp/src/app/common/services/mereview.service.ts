import { environment } from '../../../environments/environment';
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import {
    MeReview,
    MeReviewRecommendation,
    MeReviewSearchOptions,
    MeReviewSearchResponse,
    ReviewDomainInfo,
    ReviewDashboardStats,
    EvaluateReviewRequest
} from '../models/mereview.model';
import { SearchQuery, PagingHeaders } from '../models/http.model';

@Injectable({ providedIn: 'root' })
export class MeReviewService extends SearchQuery {

    constructor(private http: HttpClient) {
        super();
    }

    search(params: MeReviewSearchOptions): Observable<MeReviewSearchResponse> {
        const queryParams: HttpParams = this.buildQueryParams(params);
        return this.http.get(`${environment.baseApiUrl}mereviews`, { params: queryParams, observe: 'response' })
            .pipe(
                map(response => {
                    const paginationHeader = response.headers.get('x-pagination');
                    const headers = paginationHeader ? JSON.parse(paginationHeader) as PagingHeaders : null;
                    const reviews = response.body as MeReview[];
                    return { reviews: reviews, headers: headers };
                })
            );
    }

    get(meReviewId: string): Observable<MeReview> {
        return this.http.get<MeReview>(`${environment.baseApiUrl}mereviews/${meReviewId}`);
    }

    getDomains(): Observable<ReviewDomainInfo[]> {
        return this.http.get<ReviewDomainInfo[]>(`${environment.baseApiUrl}mereviews/domains`);
    }

    getStats(): Observable<ReviewDashboardStats> {
        return this.http.get<ReviewDashboardStats>(`${environment.baseApiUrl}mereviews/stats`);
    }

    evaluate(req: EvaluateReviewRequest): Observable<MeReview> {
        return this.http.post<MeReview>(`${environment.baseApiUrl}mereviews/evaluate`, req);
    }

    evaluatePdf(file: File, domain: string, title: string): Observable<MeReview> {
        const formData = new FormData();
        formData.append('file', file);
        formData.append('domain', domain);
        formData.append('title', title);

        return this.http.post<MeReview>(`${environment.baseApiUrl}mereviews/evaluate-pdf`, formData);
    }

    save(review: MeReview): Observable<MeReview> {
        return this.http.post<MeReview>(`${environment.baseApiUrl}mereviews/${review.meReviewId}`, review);
    }

    toggleRecommendation(recommendationId: string): Observable<MeReviewRecommendation> {
        return this.http.post<MeReviewRecommendation>(`${environment.baseApiUrl}mereviews/recommendations/${recommendationId}/toggle`, {});
    }

    delete(meReviewId: string): Observable<void> {
        return this.http.delete<void>(`${environment.baseApiUrl}mereviews/${meReviewId}`);
    }
}
