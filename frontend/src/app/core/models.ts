export type DashboardView = 'overview' | 'usage' | 'billing' | 'team' | 'api-keys';

export interface SessionResponse {
  token: string;
  user: User;
  organization: Organization;
}

export interface User {
  id: string;
  email: string;
  fullName: string;
}

export interface Organization {
  id: string;
  name: string;
  role: string;
  plan: string;
}

export interface Subscription {
  plan: string;
  status: string;
  stripeSubscriptionId?: string;
  currentPeriodEnd?: string;
}

export interface TeamMember {
  id: string;
  email: string;
  fullName: string;
  role: string;
  createdAt: string;
}

export interface ApiKey {
  id: string;
  name: string;
  prefix: string;
  createdAt: string;
  lastUsedAt?: string;
  revokedAt?: string;
}

export interface CreatedApiKey {
  apiKey: ApiKey;
  plainTextKey: string;
}

export interface UsagePoint {
  date: string;
  units: number;
  requests: number;
}

export interface UsageEvent {
  id: string;
  path: string;
  method: string;
  statusCode: number;
  units: number;
  occurredAt: string;
}

export interface UsageSummary {
  totalUnits: number;
  totalRequests: number;
  errorCount: number;
  daily: UsagePoint[];
  recentEvents: UsageEvent[];
}
