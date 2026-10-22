import { environment } from '../../../environments/environment';
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { Tag, TagSearchOptions, TagSearchResponse } from '../models/tag.model';
import { SearchQuery, PagingHeaders } from '../models/http.model';

@Injectable({ providedIn: 'root' })
export class TagService extends SearchQuery {

    constructor(private http: HttpClient) {
        super();
    }

    search(params: TagSearchOptions): Observable<TagSearchResponse> {
        const queryParams: HttpParams = this.buildQueryParams(params);
        return this.http.get(`${environment.baseApiUrl}tags`, { params: queryParams, observe: 'response' })
            .pipe(
                map(response => {
                    const headers = JSON.parse(response.headers.get("x-pagination")) as PagingHeaders;
                    const tags = response.body as Tag[];
                    return { tags: tags, headers: headers };
                })
            );
    }

    get(tagId: string): Observable<Tag> {
        return this.http.get<Tag>(`${environment.baseApiUrl}tags/${tagId}`);
    }

    save(tag: Tag): Observable<Tag> {
        return this.http.post<Tag>(`${environment.baseApiUrl}tags/${tag.tagId}`, tag);
    }

    delete(tagId: string): Observable<void> {
        return this.http.delete<void>(`${environment.baseApiUrl}tags/${tagId}`);
    }

}
