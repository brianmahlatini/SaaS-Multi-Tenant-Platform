import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ApiKey, CreatedApiKey, SessionResponse, Subscription, TeamMember, UsageSummary } from './models';

@Injectable({ providedIn: 'root' })
export class ApiClient {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5000/api';

  login(payload: { email: string; password: string }) {
    return this.http.post<SessionResponse>(`${this.apiUrl}/auth/login`, payload);
  }

  register(payload: { email: string; password: string; fullName: string; organizationName: string }) {
    return this.http.post<SessionResponse>(`${this.apiUrl}/auth/register`, payload);
  }

  me(headers: HttpHeaders) {
    return this.http.get<SessionResponse>(`${this.apiUrl}/auth/me`, { headers });
  }

  subscription(headers: HttpHeaders) {
    return this.http.get<Subscription>(`${this.apiUrl}/billing/subscription`, { headers });
  }

  checkout(plan: string, headers: HttpHeaders) {
    return this.http.post<{ url: string }>(`${this.apiUrl}/billing/checkout`, { plan }, { headers });
  }

  team(headers: HttpHeaders) {
    return this.http.get<TeamMember[]>(`${this.apiUrl}/users`, { headers });
  }

  invite(payload: { email: string; role: string }, headers: HttpHeaders) {
    return this.http.post(`${this.apiUrl}/users/invite`, payload, { headers });
  }

  apiKeys(headers: HttpHeaders) {
    return this.http.get<ApiKey[]>(`${this.apiUrl}/api-keys`, { headers });
  }

  createApiKey(payload: { name: string }, headers: HttpHeaders) {
    return this.http.post<CreatedApiKey>(`${this.apiUrl}/api-keys`, payload, { headers });
  }

  revokeApiKey(id: string, headers: HttpHeaders) {
    return this.http.delete(`${this.apiUrl}/api-keys/${id}`, { headers });
  }

  usage(headers: HttpHeaders) {
    return this.http.get<UsageSummary>(`${this.apiUrl}/usage`, { headers });
  }

  ingestSampleUsage(apiKey: string) {
    const headers = new HttpHeaders({ 'X-API-Key': apiKey });
    return this.http.post(`${this.apiUrl}/usage/ingest`, {
      path: '/v1/events',
      method: 'POST',
      statusCode: 202,
      units: 3
    }, { headers });
  }
}
