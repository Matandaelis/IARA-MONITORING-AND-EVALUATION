import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, fromEvent, merge } from 'rxjs';
import { map } from 'rxjs/operators';
import { Questionnaire } from '../models/questionnaire.model';

export interface OfflineSubmission {
    clientGeneratedId: string;
    questionnaireId: string;
    entityId?: string;
    entityCode?: string;
    dateId?: string;
    dateCode?: string;
    createdOnUtc: string;
    submittedOnUtc: string;
    answers: {
        questionId: string;
        questionCode?: string;
        value: string;
        selectedOptionIds?: string[];
    }[];
}

@Injectable({
    providedIn: 'root'
})
export class OfflineSurveyService {
    private dbName = 'MonicOfflineStore';
    private dbVersion = 1;
    private dbPromise: Promise<IDBDatabase> | null = null;

    private isOnlineSubject = new BehaviorSubject<boolean>(typeof navigator !== 'undefined' ? navigator.onLine : true);
    public isOnline$: Observable<boolean> = this.isOnlineSubject.asObservable();

    public pendingCount$ = new BehaviorSubject<number>(0);

    constructor(private http: HttpClient) {
        if (typeof window !== 'undefined') {
            merge(
                fromEvent(window, 'online').pipe(map(() => true)),
                fromEvent(window, 'offline').pipe(map(() => false))
            ).subscribe(online => {
                this.isOnlineSubject.next(online);
                if (online) {
                    this.syncPendingResponses().catch(() => {});
                }
            });
        }
        this.updatePendingCount();
    }

    public get isOnline(): boolean {
        return this.isOnlineSubject.value;
    }

    private getDb(): Promise<IDBDatabase> {
        if (this.dbPromise) return this.dbPromise;

        this.dbPromise = new Promise((resolve, reject) => {
            if (typeof indexedDB === 'undefined') {
                return reject(new Error('IndexedDB not supported in this environment'));
            }

            const request = indexedDB.open(this.dbName, this.dbVersion);

            request.onupgradeneeded = (event: any) => {
                const db = event.target.result;
                if (!db.objectStoreNames.contains('questionnaires')) {
                    db.createObjectStore('questionnaires', { keyPath: 'questionnaireId' });
                }
                if (!db.objectStoreNames.contains('pendingSubmissions')) {
                    db.createObjectStore('pendingSubmissions', { keyPath: 'clientGeneratedId' });
                }
            };

            request.onsuccess = () => resolve(request.result);
            request.onerror = () => reject(request.error);
        });

        return this.dbPromise;
    }

    // Questionnaire Caching
    public async cacheQuestionnaire(questionnaire: Questionnaire): Promise<void> {
        const db = await this.getDb();
        return new Promise((resolve, reject) => {
            const tx = db.transaction('questionnaires', 'readwrite');
            const store = tx.objectStore('questionnaires');
            const req = store.put(questionnaire);
            req.onsuccess = () => resolve();
            req.onerror = () => reject(req.error);
        });
    }

    public async getCachedQuestionnaire(questionnaireId: string): Promise<Questionnaire | null> {
        const db = await this.getDb();
        return new Promise((resolve, reject) => {
            const tx = db.transaction('questionnaires', 'readonly');
            const store = tx.objectStore('questionnaires');
            const req = store.get(questionnaireId);
            req.onsuccess = () => resolve(req.result || null);
            req.onerror = () => reject(req.error);
        });
    }

    public async getCachedQuestionnaires(): Promise<Questionnaire[]> {
        const db = await this.getDb();
        return new Promise((resolve, reject) => {
            const tx = db.transaction('questionnaires', 'readonly');
            const store = tx.objectStore('questionnaires');
            const req = store.getAll();
            req.onsuccess = () => resolve(req.result || []);
            req.onerror = () => reject(req.error);
        });
    }

    // Offline Survey Response Submissions
    public async queueOfflineSubmission(submission: OfflineSubmission): Promise<string> {
        if (!submission.clientGeneratedId) {
            submission.clientGeneratedId = this.generateUuid();
        }
        if (!submission.createdOnUtc) {
            submission.createdOnUtc = new Date().toISOString();
        }
        if (!submission.submittedOnUtc) {
            submission.submittedOnUtc = new Date().toISOString();
        }

        const db = await this.getDb();
        await new Promise<void>((resolve, reject) => {
            const tx = db.transaction('pendingSubmissions', 'readwrite');
            const store = tx.objectStore('pendingSubmissions');
            const req = store.put(submission);
            req.onsuccess = () => resolve();
            req.onerror = () => reject(req.error);
        });

        this.updatePendingCount();
        return submission.clientGeneratedId;
    }

    public async getPendingSubmissions(): Promise<OfflineSubmission[]> {
        const db = await this.getDb();
        return new Promise((resolve, reject) => {
            const tx = db.transaction('pendingSubmissions', 'readonly');
            const store = tx.objectStore('pendingSubmissions');
            const req = store.getAll();
            req.onsuccess = () => resolve(req.result || []);
            req.onerror = () => reject(req.error);
        });
    }

    public async removePendingSubmission(clientGeneratedId: string): Promise<void> {
        const db = await this.getDb();
        await new Promise<void>((resolve, reject) => {
            const tx = db.transaction('pendingSubmissions', 'readwrite');
            const store = tx.objectStore('pendingSubmissions');
            const req = store.delete(clientGeneratedId);
            req.onsuccess = () => resolve();
            req.onerror = () => reject(req.error);
        });
        this.updatePendingCount();
    }

    public async updatePendingCount(): Promise<number> {
        try {
            const pending = await this.getPendingSubmissions();
            const count = pending.length;
            this.pendingCount$.next(count);
            return count;
        } catch {
            return 0;
        }
    }

    // Synchronize pending offline responses to backend
    public async syncPendingResponses(): Promise<{ synced: number; duplicates: number }> {
        const pending = await this.getPendingSubmissions();
        if (pending.length === 0) return { synced: 0, duplicates: 0 };

        const payload = {
            clientDeviceId: this.getClientDeviceId(),
            submissions: pending
        };

        try {
            const res: any = await this.http.post('api/external/offline-sync', payload).toPromise();

            // Clear successfully synced items from IndexedDB
            for (const sub of pending) {
                await this.removePendingSubmission(sub.clientGeneratedId);
            }

            this.updatePendingCount();
            return {
                synced: res?.synced || 0,
                duplicates: res?.duplicates || 0
            };
        } catch (err) {
            console.warn('Offline sync failed, will retry when connection stabilizes:', err);
            throw err;
        }
    }

    private getClientDeviceId(): string {
        const key = 'monic-client-device-id';
        let id = typeof localStorage !== 'undefined' ? localStorage.getItem(key) : null;
        if (!id) {
            id = this.generateUuid();
            if (typeof localStorage !== 'undefined') localStorage.setItem(key, id);
        }
        return id;
    }

    private generateUuid(): string {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, c => {
            const r = Math.random() * 16 | 0;
            const v = c === 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
    }
}
