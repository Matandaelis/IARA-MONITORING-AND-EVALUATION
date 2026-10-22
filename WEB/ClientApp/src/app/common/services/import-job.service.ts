import { environment } from '../../../environments/environment';
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ImportJob, ImportJobSearchOptions, ImportJobSearchResponse } from '../models/importjob.model';
import { SearchQuery, PagingHeaders } from '../models/http.model';

@Injectable({ providedIn: 'root' })
export class ImportJobService extends SearchQuery {

    constructor(private http: HttpClient) {
        super();
    }

    search(params: ImportJobSearchOptions): Observable<ImportJobSearchResponse> {
        const queryParams: HttpParams = this.buildQueryParams(params);
        return this.http.get(`${environment.baseApiUrl}imports`, { params: queryParams, observe: 'response' })
            .pipe(
                map(response => {
                    const headers = JSON.parse(response.headers.get("x-pagination")) as PagingHeaders;
                    const importJobs = response.body as ImportJob[];
                    return { importJobs: importJobs, headers: headers };
                })
            );
    }

    get(importJobId: string): Observable<ImportJob> {
        return this.http.get<ImportJob>(`${environment.baseApiUrl}imports/${importJobId}`);
    }

    upload(importJob: ImportJob): Observable<ImportJob> {
        return this.http.post<ImportJob>(`${environment.baseApiUrl}imports/${importJob.importJobId}`, importJob);
    }

    commit(importJobId: string, submitAfterCommit = false): Observable<ImportJob> {
        let url = `${environment.baseApiUrl}imports/${importJobId}/commit`;
        if (submitAfterCommit) url += "?submitAfterCommit=true";
        return this.http.post<ImportJob>(url, {});
    }

    template(questionnaireId: string): Observable<{ fileName: string; contents: string; columns: string[] }> {
        const queryParams: HttpParams = this.buildQueryParams({ importType: "Records", questionnaireId: questionnaireId });
        return this.http.get<{ fileName: string; contents: string; columns: string[] }>(
            `${environment.baseApiUrl}imports/template`, { params: queryParams });
    }

    delete(importJobId: string): Observable<void> {
        return this.http.delete<void>(`${environment.baseApiUrl}imports/${importJobId}`);
    }

}
