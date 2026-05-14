import { HttpHeaders } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ApiClient } from './api-client.service';
import { Organization, SessionResponse, User } from './models';

@Injectable({ providedIn: 'root' })
export class SessionStore {
  private readonly api = inject(ApiClient);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly message = signal('');
  readonly token = signal(localStorage.getItem('token') ?? '');
  readonly user = signal<User | null>(null);
  readonly organization = signal<Organization | null>(null);
  readonly isAuthed = computed(() => Boolean(this.token() && this.user() && this.organization()));
  readonly canManage = computed(() => ['Owner', 'Admin'].includes(this.organization()?.role ?? ''));

  restore(): void {
    if (!this.token() || this.user()) return;

    this.api.me(this.authHeaders()).subscribe({
      next: session => this.setSession(session, false),
      error: () => this.logout()
    });
  }

  login(payload: { email: string; password: string }): void {
    this.loading.set(true);
    this.api.login(payload).subscribe({
      next: session => this.setSession(session),
      error: () => this.fail('Login failed. Check your email and password.')
    });
  }

  register(payload: { email: string; password: string; fullName: string; organizationName: string }): void {
    this.loading.set(true);
    this.api.register(payload).subscribe({
      next: session => this.setSession(session),
      error: () => this.fail('Registration failed. Try a different email.')
    });
  }

  logout(): void {
    localStorage.removeItem('token');
    this.token.set('');
    this.user.set(null);
    this.organization.set(null);
    this.router.navigateByUrl('/auth');
  }

  authHeaders(): HttpHeaders {
    let headers = new HttpHeaders({ Authorization: `Bearer ${this.token()}` });
    const organization = this.organization();

    if (organization) {
      headers = headers.set('X-Organization-ID', organization.id);
    }

    return headers;
  }

  private setSession(session: SessionResponse, navigate = true): void {
    localStorage.setItem('token', session.token);
    this.token.set(session.token);
    this.user.set(session.user);
    this.organization.set(session.organization);
    this.loading.set(false);
    this.message.set('');

    if (navigate) {
      this.router.navigateByUrl('/overview');
    }
  }

  private fail(message: string): void {
    this.loading.set(false);
    this.message.set(message);
  }
}
