import { environment } from '../../../environments/environment';
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export class KestraSettings {
    kestraBaseUrl: string;
    kestraApiToken: string;
    kestraNamespace: string;
    externalApiKey: string;
}

export class KestraStatus {
    configured: boolean;
    reachable: boolean;
    statusCode?: number;
    detail: string;
}

export class KestraConnectorFlow {
    id: string;
    name: string;
    sourceSystem: string;
    description: string;
    template: string;
    pushUrl: string;
}

@Injectable({ providedIn: 'root' })
export class KestraService {

    constructor(private http: HttpClient) {
    }

    getSettings(): Observable<KestraSettings> {
        return this.http.get<KestraSettings>(`${environment.baseApiUrl}kestra/settings`);
    }

    saveSettings(settings: KestraSettings): Observable<KestraSettings> {
        return this.http.post<KestraSettings>(`${environment.baseApiUrl}kestra/settings`, settings);
    }

    status(): Observable<KestraStatus> {
        return this.http.get<KestraStatus>(`${environment.baseApiUrl}kestra/status`);
    }

    flows(): Observable<KestraConnectorFlow[]> {
        return this.http.get<KestraConnectorFlow[]>(`${environment.baseApiUrl}kestra/flows`);
    }

    regenerateKey(): Observable<{ externalApiKey: string }> {
        return this.http.post<{ externalApiKey: string }>(`${environment.baseApiUrl}kestra/regeneratekey`, {});
    }

}
