import { environment } from '../../../environments/environment';
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { Comment, CommentSearchOptions, CommentSearchResponse } from '../models/comment.model';
import { SearchQuery, PagingHeaders } from '../models/http.model';

@Injectable({ providedIn: 'root' })
export class CommentService extends SearchQuery {

    constructor(private http: HttpClient) {
        super();
    }

    search(params: CommentSearchOptions): Observable<CommentSearchResponse> {
        const queryParams: HttpParams = this.buildQueryParams(params);
        return this.http.get(`${environment.baseApiUrl}comments`, { params: queryParams, observe: 'response' })
            .pipe(
                map(response => {
                    const headers = JSON.parse(response.headers.get("x-pagination")) as PagingHeaders;
                    const comments = response.body as Comment[];
                    return { comments: comments, headers: headers };
                })
            );
    }

    get(commentId: string): Observable<Comment> {
        return this.http.get<Comment>(`${environment.baseApiUrl}comments/${commentId}`);
    }

    save(comment: Comment): Observable<Comment> {
        return this.http.post<Comment>(`${environment.baseApiUrl}comments/${comment.commentId}`, comment);
    }

    react(commentId: string, reaction: string): Observable<Comment> {
        return this.http.post<Comment>(`${environment.baseApiUrl}comments/${commentId}/react`, { reaction: reaction });
    }

    delete(commentId: string): Observable<void> {
        return this.http.delete<void>(`${environment.baseApiUrl}comments/${commentId}`);
    }

}
