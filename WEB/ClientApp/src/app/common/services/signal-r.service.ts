import { environment } from '../../../environments/environment';
import { Injectable, OnDestroy } from '@angular/core';
import { BehaviorSubject, Subject, Subscription } from 'rxjs';
import { filter } from 'rxjs/operators';
import * as signalR from '@microsoft/signalr';
import { AuthService } from './auth.service';

/** A real-time notification pushed from the server. */
export interface RealtimeNotification {
    notificationId: string;
    type: string;
    title: string;
    body: string;
    link: string;
}

/**
 * Live connection to the server via SignalR (long-polling transport — the Authorization
 * header works there, so no OpenIddict query-string token support is required).
 * Replaces notification polling and enables live comment updates.
 */
@Injectable({ providedIn: 'root' })
export class SignalRService implements OnDestroy {

    private connection: signalR.HubConnection;
    private started = false;
    private startSubscription: Subscription;

    private notificationsSubject = new Subject<RealtimeNotification>();
    readonly notification$ = this.notificationsSubject.asObservable();

    private commentsSubject = new Subject<{ subjectType: string; subjectId: string; comment: any }>();
    readonly commentAdded$ = this.commentsSubject.asObservable();

    readonly connected$ = new BehaviorSubject<boolean>(false);

    constructor(private authService: AuthService) {
        // connect once the session is ready; reconnect on token refresh (state change)
        this.startSubscription = this.authService.state$
            .pipe(filter(state => !!state?.authReady))
            .subscribe(state => {
                const token = state?.tokens?.access_token;
                if (token) this.ensureStarted(token);
            });
    }

    private ensureStarted(accessToken: string): void {
        if (this.started) {
            // token may have been refreshed — SignalR uses the token only at start,
            // long-polling requests include the header per request via accessTokenFactory.
            return;
        }
        this.started = true;

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(`${environment.baseAuthUrl}hubs/notifications`, {
                // long polling: the Authorization header is sent on every request,
                // which is compatible with OpenIddict validation out of the box
                transport: signalR.HttpTransportType.LongPolling,
                accessTokenFactory: () => {
                    const stored = localStorage.getItem("auth-tokens");
                    const tokens = stored ? JSON.parse(stored) : null;
                    return tokens?.access_token ?? "";
                }
            })
            .withAutomaticReconnect([0, 5000, 15000, 30000])
            .configureLogging(signalR.LogLevel.Warning)
            .build();

        this.connection.on("notification", (notification: RealtimeNotification) => {
            this.notificationsSubject.next(notification);
        });

        this.connection.on("commentAdded", (comment: any) => {
            // the server broadcasts to subject groups; the payload carries the subject
            if (comment?.subjectType && comment?.subjectId) {
                this.commentsSubject.next({ subjectType: comment.subjectType, subjectId: comment.subjectId, comment: comment });
            }
        });

        this.connection.onreconnected(() => this.connected$.next(true));
        this.connection.onclose(() => this.connected$.next(false));

        this.connection.start()
            .then(() => this.connected$.next(true))
            .catch(() => {
                // server unreachable — the app still works via HTTP; retry on next state change
                this.started = false;
                this.connected$.next(false);
            });
    }

    watchComments(subjectType: string, subjectId: string): void {
        if (this.connection && this.connected$.value) {
            this.connection.invoke("WatchComments", subjectType, subjectId).catch(() => { /* best effort */ });
        }
    }

    unwatchComments(subjectType: string, subjectId: string): void {
        if (this.connection) {
            this.connection.invoke("UnwatchComments", subjectType, subjectId).catch(() => { /* best effort */ });
        }
    }

    ngOnDestroy(): void {
        if (this.startSubscription) this.startSubscription.unsubscribe();
        if (this.connection) this.connection.stop();
    }

}
