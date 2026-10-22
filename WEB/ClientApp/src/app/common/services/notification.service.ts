import { environment } from '../../../environments/environment';
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { Notification, NotificationSearchOptions, NotificationSearchResponse, UnreadCountResponse, MarkNotificationsReadRequest } from '../models/notification.model';
import { SearchQuery, PagingHeaders } from '../models/http.model';

@Injectable({ providedIn: 'root' })
export class NotificationService extends SearchQuery {

    constructor(private http: HttpClient) {
        super();
    }

    search(params: NotificationSearchOptions): Observable<NotificationSearchResponse> {
        const queryParams: HttpParams = this.buildQueryParams(params);
        return this.http.get(`${environment.baseApiUrl}notifications`, { params: queryParams, observe: 'response' })
            .pipe(
                map(response => {
                    const headers = JSON.parse(response.headers.get("x-pagination")) as PagingHeaders;
                    const notifications = response.body as Notification[];
                    return { notifications: notifications, headers: headers };
                })
            );
    }

    unreadCount(): Observable<UnreadCountResponse> {
        return this.http.get<UnreadCountResponse>(`${environment.baseApiUrl}notifications/unreadcount`);
    }

    markRead(ids: string[] = []): Observable<void> {
        const request = new MarkNotificationsReadRequest();
        request.ids = ids;
        return this.http.post<void>(`${environment.baseApiUrl}notifications/markread`, request);
    }

    delete(notificationId: string): Observable<void> {
        return this.http.delete<void>(`${environment.baseApiUrl}notifications/${notificationId}`);
    }

}
