import { SearchOptions, PagingHeaders } from './http.model';
import { User } from './user.model';

export class Notification {
    notificationId: string;
    userId: string;
    type: string;
    title: string;
    body: string;
    link: string;
    isRead: boolean;
    createdOnUtc: Date;
    createdById: string;
    createdBy: User;

    constructor() {
        this.notificationId = "00000000-0000-0000-0000-000000000000";
    }
}

export class NotificationSearchOptions extends SearchOptions {
    q: string;
    isRead: boolean;
    type: string;
}

export class NotificationSearchResponse {
    notifications: Notification[] = [];
    headers: PagingHeaders;
}

export class UnreadCountResponse {
    count: number;
}

export class MarkNotificationsReadRequest {
    ids: string[] = [];
}
