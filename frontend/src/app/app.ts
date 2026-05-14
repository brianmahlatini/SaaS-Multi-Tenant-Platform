import { CommonModule } from '@angular/common';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

type View = 'overview' | 'usage' | 'billing' | 'team' | 'keys';

interface SessionResponse {
  token: string;
  user: User;
  organization: Organization;
}

interface User {
  id: string;
  email: string;
  fullName: string;
}

interface Organization {
  id: string;
  name: string;
  role: string;
  plan: string;
}

interface Subscription {
  plan: string;
  status: string;
  stripeSubscriptionId?: string;
  currentPeriodEnd?: string;
}

interface TeamMember {
  id: string;
  email: string;
  fullName: string;
  role: string;
  createdAt: string;
}

interface ApiKey {
  id: string;
  name: string;
  prefix: string;
  createdAt: string;
  lastUsedAt?: string;
  revokedAt?: string;
}

interface CreatedApiKey {
  apiKey: ApiKey;
  plainTextKey: string;
}

interface UsagePoint {
  date: string;
  units: number;
  requests: number;
}

interface UsageEvent {
  id: string;
  path: string;
  method: string;
  statusCode: number;
  units: number;
  occurredAt: string;
}

interface UsageSummary {
  totalUnits: number;
  totalRequests: number;
  errorCount: number;
  daily: UsagePoint[];
  recentEvents: UsageEvent[];
}

@Component({
  selector: 'app-root',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  private readonly http = inject(HttpClient);
  private readonly fb = inject(FormBuilder);
  private readonly apiUrl = 'http://localhost:5000/api';

  view = signal<View>('overview');
  authMode = signal<'login' | 'register'>('login');
  loading = signal(false);
  message = signal('');
  token = signal(localStorage.getItem('token') ?? '');
  user = signal<User | null>(null);
  organization = signal<Organization | null>(null);
  subscription = signal<Subscription | null>(null);
  team = signal<TeamMember[]>([]);
  apiKeys = signal<ApiKey[]>([]);
  usage = signal<UsageSummary | null>(null);
  oneTimeKey = signal('');

  isAuthed = computed(() => Boolean(this.token() && this.user() && this.organization()));
  maxDailyUnits = computed(() => Math.max(1, ...((this.usage()?.daily ?? []).map(point => point.units))));

  loginForm = this.fb.nonNullable.group({
    email: ['owner@example.com', [Validators.required, Validators.email]],
    password: ['ChangeMe123!', [Validators.required]]
  });

  registerForm = this.fb.nonNullable.group({
    fullName: ['New Owner', [Validators.required]],
    email: ['owner@example.com', [Validators.required, Validators.email]],
    password: ['ChangeMe123!', [Validators.required, Validators.minLength(8)]],
    organizationName: ['Acme Cloud', [Validators.required]]
  });

  inviteForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    role: ['Member', [Validators.required]]
  });

  apiKeyForm = this.fb.nonNullable.group({
    name: ['Production key', [Validators.required]]
  });

  constructor() {
    if (this.token()) {
      this.loadMe();
    }
  }

  login(): void {
    if (this.loginForm.invalid) return;
    this.loading.set(true);
    this.http.post<SessionResponse>(`${this.apiUrl}/auth/login`, this.loginForm.getRawValue()).subscribe({
      next: session => this.setSession(session),
      error: () => this.fail('Login failed. Check your email and password.')
    });
  }

  register(): void {
    if (this.registerForm.invalid) return;
    this.loading.set(true);
    this.http.post<SessionResponse>(`${this.apiUrl}/auth/register`, this.registerForm.getRawValue()).subscribe({
      next: session => this.setSession(session),
      error: () => this.fail('Registration failed. Try a different email.')
    });
  }

  logout(): void {
    localStorage.removeItem('token');
    this.token.set('');
    this.user.set(null);
    this.organization.set(null);
    this.subscription.set(null);
    this.team.set([]);
    this.apiKeys.set([]);
    this.usage.set(null);
  }

  setView(view: View): void {
    this.view.set(view);
  }

  createCheckout(plan: string): void {
    this.http.post<{ url: string }>(`${this.apiUrl}/billing/checkout`, { plan }, { headers: this.headers() }).subscribe({
      next: response => window.open(response.url, '_blank', 'noopener'),
      error: () => this.fail('Could not create checkout session.')
    });
  }

  invite(): void {
    if (this.inviteForm.invalid) return;
    this.http.post(`${this.apiUrl}/users/invite`, this.inviteForm.getRawValue(), { headers: this.headers() }).subscribe({
      next: () => {
        this.message.set('Invitation queued.');
        this.inviteForm.reset({ email: '', role: 'Member' });
      },
      error: () => this.fail('Only owners and admins can invite users.')
    });
  }

  createApiKey(): void {
    if (this.apiKeyForm.invalid) return;
    this.http.post<CreatedApiKey>(`${this.apiUrl}/api-keys`, this.apiKeyForm.getRawValue(), { headers: this.headers() }).subscribe({
      next: created => {
        this.oneTimeKey.set(created.plainTextKey);
        this.apiKeys.update(keys => [created.apiKey, ...keys]);
        this.apiKeyForm.reset({ name: 'Production key' });
      },
      error: () => this.fail('Could not create API key.')
    });
  }

  revokeKey(key: ApiKey): void {
    this.http.delete(`${this.apiUrl}/api-keys/${key.id}`, { headers: this.headers() }).subscribe({
      next: () => this.apiKeys.update(keys => keys.map(item => item.id === key.id ? { ...item, revokedAt: new Date().toISOString() } : item)),
      error: () => this.fail('Could not revoke API key.')
    });
  }

  sendSampleUsage(): void {
    const activeKey = this.apiKeys().find(key => !key.revokedAt);
    if (!this.oneTimeKey() || !activeKey) {
      this.message.set('Create an API key first. The secret is shown once.');
      return;
    }

    const headers = new HttpHeaders({ 'X-API-Key': this.oneTimeKey() });
    this.http.post(`${this.apiUrl}/usage/ingest`, {
      path: '/v1/events',
      method: 'POST',
      statusCode: 202,
      units: 3
    }, { headers }).subscribe({
      next: () => this.loadUsage(),
      error: () => this.fail('Usage ingestion failed.')
    });
  }

  private setSession(session: SessionResponse): void {
    localStorage.setItem('token', session.token);
    this.token.set(session.token);
    this.user.set(session.user);
    this.organization.set(session.organization);
    this.loading.set(false);
    this.message.set('');
    this.refreshDashboard();
  }

  private loadMe(): void {
    this.http.get<SessionResponse>(`${this.apiUrl}/auth/me`, { headers: this.headers() }).subscribe({
      next: session => this.setSession(session),
      error: () => this.logout()
    });
  }

  private refreshDashboard(): void {
    this.loadSubscription();
    this.loadTeam();
    this.loadKeys();
    this.loadUsage();
  }

  private loadSubscription(): void {
    this.http.get<Subscription>(`${this.apiUrl}/billing/subscription`, { headers: this.headers() }).subscribe(response => this.subscription.set(response));
  }

  private loadTeam(): void {
    this.http.get<TeamMember[]>(`${this.apiUrl}/users`, { headers: this.headers() }).subscribe(response => this.team.set(response));
  }

  private loadKeys(): void {
    this.http.get<ApiKey[]>(`${this.apiUrl}/api-keys`, { headers: this.headers() }).subscribe(response => this.apiKeys.set(response));
  }

  private loadUsage(): void {
    this.http.get<UsageSummary>(`${this.apiUrl}/usage`, { headers: this.headers() }).subscribe(response => this.usage.set(response));
  }

  private headers(): HttpHeaders {
    let headers = new HttpHeaders({ Authorization: `Bearer ${this.token()}` });
    const organization = this.organization();
    if (organization) {
      headers = headers.set('X-Organization-ID', organization.id);
    }
    return headers;
  }

  private fail(message: string): void {
    this.loading.set(false);
    this.message.set(message);
  }
}
