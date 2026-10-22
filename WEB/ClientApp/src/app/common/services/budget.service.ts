import { environment } from '../../../environments/environment';
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import {
    BudgetCategory, BudgetCategorySearchOptions, BudgetCategorySearchResponse,
    BudgetLine, BudgetLineSearchOptions, BudgetLineSearchResponse,
    BudgetSummaryResponse, BudgetBurnPoint, BudgetCostPerOutcome, FxRate
} from '../models/budget.model';
import { SearchQuery, PagingHeaders } from '../models/http.model';

@Injectable({ providedIn: 'root' })
export class BudgetService extends SearchQuery {

    constructor(private http: HttpClient) {
        super();
    }

    searchCategories(params: BudgetCategorySearchOptions): Observable<BudgetCategorySearchResponse> {
        const queryParams: HttpParams = this.buildQueryParams(params);
        return this.http.get(`${environment.baseApiUrl}budgetcategories`, { params: queryParams, observe: 'response' })
            .pipe(
                map(response => {
                    const headers = JSON.parse(response.headers.get("x-pagination")) as PagingHeaders;
                    const budgetCategories = response.body as BudgetCategory[];
                    return { budgetCategories: budgetCategories, headers: headers };
                })
            );
    }

    saveCategory(category: BudgetCategory): Observable<BudgetCategory> {
        return this.http.post<BudgetCategory>(`${environment.baseApiUrl}budgetcategories/${category.budgetCategoryId}`, category);
    }

    deleteCategory(budgetCategoryId: string): Observable<void> {
        return this.http.delete<void>(`${environment.baseApiUrl}budgetcategories/${budgetCategoryId}`);
    }

    searchLines(params: BudgetLineSearchOptions): Observable<BudgetLineSearchResponse> {
        const queryParams: HttpParams = this.buildQueryParams(params);
        return this.http.get(`${environment.baseApiUrl}budgetlines`, { params: queryParams, observe: 'response' })
            .pipe(
                map(response => {
                    const headers = JSON.parse(response.headers.get("x-pagination")) as PagingHeaders;
                    const budgetLines = response.body as BudgetLine[];
                    return { budgetLines: budgetLines, headers: headers };
                })
            );
    }

    getLine(budgetLineId: string): Observable<BudgetLine> {
        return this.http.get<BudgetLine>(`${environment.baseApiUrl}budgetlines/${budgetLineId}`);
    }

    saveLine(line: BudgetLine): Observable<BudgetLine> {
        return this.http.post<BudgetLine>(`${environment.baseApiUrl}budgetlines/${line.budgetLineId}`, line);
    }

    deleteLine(budgetLineId: string): Observable<void> {
        return this.http.delete<void>(`${environment.baseApiUrl}budgetlines/${budgetLineId}`);
    }

    summary(projectId: string, currency: string = "base"): Observable<BudgetSummaryResponse> {
        const queryParams: HttpParams = this.buildQueryParams({ projectId: projectId, currency: currency });
        return this.http.get<BudgetSummaryResponse>(`${environment.baseApiUrl}budgetlines/summary`, { params: queryParams });
    }

    burnSeries(projectId: string): Observable<BudgetBurnPoint[]> {
        const queryParams: HttpParams = this.buildQueryParams({ projectId: projectId });
        return this.http.get<BudgetBurnPoint[]>(`${environment.baseApiUrl}budgetlines/burnseries`, { params: queryParams });
    }

    costPerOutcome(projectId: string, indicatorId: string): Observable<BudgetCostPerOutcome> {
        const queryParams: HttpParams = this.buildQueryParams({ projectId: projectId, indicatorId: indicatorId });
        return this.http.get<BudgetCostPerOutcome>(`${environment.baseApiUrl}budgetlines/costperoutcome`, { params: queryParams });
    }

    searchFxRates(fromCurrency?: string, toCurrency?: string): Observable<FxRate[]> {
        const queryParams: HttpParams = this.buildQueryParams({ fromCurrency: fromCurrency, toCurrency: toCurrency });
        return this.http.get<FxRate[]>(`${environment.baseApiUrl}fxrates`, { params: queryParams });
    }

    saveFxRate(fxRate: FxRate): Observable<FxRate> {
        return this.http.post<FxRate>(`${environment.baseApiUrl}fxrates/${fxRate.fxRateId}`, fxRate);
    }

    deleteFxRate(fxRateId: string): Observable<void> {
        return this.http.delete<void>(`${environment.baseApiUrl}fxrates/${fxRateId}`);
    }

}
