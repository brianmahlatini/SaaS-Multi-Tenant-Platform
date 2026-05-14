import { Injectable, computed, inject, signal } from '@angular/core';
import { ApiClient } from './api-client.service';
import { ApiKey, Subscription, TeamMember, UsageSummary } from './models';
import { SessionStore } from './session.store';

@Injectable({ providedIn: 'root' })
export class DashboardStore {
  private readonly api = inject(ApiClient);
  private readonly session = inject(SessionStore);

  readonly message = signal('');
  readonly subscription = signal<Subscription | null>(null);
  readonly team = signal<TeamMember[]>([]);
  readonly apiKeys = signal<ApiKey[]>([]);
  readonly usage = signal<UsageSummary | null>(null);
  readonly oneTimeKey = signal('');
  readonly maxDailyUnits = computed(() => Math.max(1, ...((this.usage()?.daily ?? []).map(point => point.units))));

  refresh(): void {
    this.loadSubscription();
    this.loadTeam();
    this.loadApiKeys();
    this.loadUsage();
  }

  loadSubscription(): void {
    this.api.subscription(this.session.authHeaders()).subscribe(response => this.subscription.set(response));
  }

  loadTeam(): void {
    this.api.team(this.session.authHeaders()).subscribe(response => this.team.set(response));
  }

  loadApiKeys(): void {
    this.api.apiKeys(this.session.authHeaders()).subscribe(response => this.apiKeys.set(response));
  }

  loadUsage(): void {
    this.api.usage(this.session.authHeaders()).subscribe(response => this.usage.set(response));
  }

  createCheckout(plan: string): void {
    this.api.checkout(plan, this.session.authHeaders()).subscribe({
      next: response => window.open(response.url, '_blank', 'noopener'),
      error: () => this.message.set('Could not create checkout session.')
    });
  }

  invite(payload: { email: string; role: string }, onSuccess: () => void): void {
    this.api.invite(payload, this.session.authHeaders()).subscribe({
      next: () => {
        this.message.set('Invitation queued.');
        onSuccess();
      },
      error: () => this.message.set('Only owners and admins can invite users.')
    });
  }

  createApiKey(payload: { name: string }, onSuccess: () => void): void {
    this.api.createApiKey(payload, this.session.authHeaders()).subscribe({
      next: created => {
        this.oneTimeKey.set(created.plainTextKey);
        this.apiKeys.update(keys => [created.apiKey, ...keys]);
        onSuccess();
      },
      error: () => this.message.set('Could not create API key.')
    });
  }

  revokeApiKey(key: ApiKey): void {
    this.api.revokeApiKey(key.id, this.session.authHeaders()).subscribe({
      next: () => this.apiKeys.update(keys => keys.map(item => item.id === key.id ? { ...item, revokedAt: new Date().toISOString() } : item)),
      error: () => this.message.set('Could not revoke API key.')
    });
  }

  sendSampleUsage(): void {
    const activeKey = this.apiKeys().find(key => !key.revokedAt);
    if (!this.oneTimeKey() || !activeKey) {
      this.message.set('Create an API key first. The secret is shown once.');
      return;
    }

    this.api.ingestSampleUsage(this.oneTimeKey()).subscribe({
      next: () => this.loadUsage(),
      error: () => this.message.set('Usage ingestion failed.')
    });
  }
}
