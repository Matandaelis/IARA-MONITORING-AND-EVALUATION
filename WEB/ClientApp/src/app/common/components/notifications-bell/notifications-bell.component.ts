import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Notification, NotificationSearchOptions } from '../../models/notification.model';
import { NotificationService } from '../../services/notification.service';
import { SignalRService, RealtimeNotification } from '../../services/signal-r.service';
import { ErrorService } from '../../services/error.service';

@Component({
    selector: 'app-notifications-bell',
    templateUrl: './notifications-bell.component.html',
    styleUrls: ['./notifications-bell.component.css'],
    standalone: false
})
export class NotificationsBellComponent implements OnInit, OnDestroy {

    public notifications: Notification[] = [];
    public unreadCount = 0;
    public loading = false;

    constructor(
        private notificationService: NotificationService,
        private signalRService: SignalRService,
        private router: Router,
        private errorService: ErrorService
    ) {
    }

    ngOnInit(): void {
        this.refresh();

        // real-time push (replaces polling)
        this.signalRService.notification$.subscribe(notification => {
            this.unreadCount++;
            this.notifications.unshift({
                notificationId: notification.notificationId,
                userId: null,
                type: notification.type,
                title: notification.title,
                body: notification.body,
                link: notification.link,
                isRead: false,
                createdOnUtc: new Date(),
                createdById: null,
                createdBy: null
            } as Notification);
        });
    }

    ngOnDestroy(): void {
    }

    refresh(): void {
        this.refreshUnreadCount();
        this.loadNotifications();
    }

    refreshUnreadCount(): void {
        this.notificationService.unreadCount()
            .subscribe({
                next: response => {
                    this.unreadCount = response.count;
                },
                error: err => {
                    this.errorService.handleError(err, "Notifications", "Load unread count");
                }
            });
    }

    loadNotifications(): void {
        this.loading = true;

        const searchOptions = new NotificationSearchOptions();
        searchOptions.includeParents = true;
        searchOptions.pageSize = 20;

        this.notificationService.search(searchOptions)
            .subscribe({
                next: response => {
                    this.notifications = response.notifications;
                    this.loading = false;
                },
                error: err => {
                    this.errorService.handleError(err, "Notifications", "Load");
                    this.loading = false;
                }
            });
    }

    open(notification: Notification): void {
        if (!notification.isRead) {
            this.notificationService.markRead([notification.notificationId])
                .subscribe({
                    next: () => {
                        notification.isRead = true;
                        this.unreadCount = Math.max(0, this.unreadCount - 1);
                    },
                    error: err => {
                        this.errorService.handleError(err, "Notification", "Mark read");
                    }
                });
        }

        if (notification.link) {
            this.router.navigateByUrl(notification.link);
        }
    }

    markAllRead(): void {
        this.notificationService.markRead([])
            .subscribe({
                next: () => {
                    this.notifications.forEach(o => o.isRead = true);
                    this.unreadCount = 0;
                },
                error: err => {
                    this.errorService.handleError(err, "Notifications", "Mark all read");
                }
            });
    }

    iconFor(notification: Notification): string {
        switch (notification.type) {
            case "Mention": return "fa-at";
            case "DataReview": return "fa-clipboard-check";
            default: return "fa-bell";
        }
    }

}
