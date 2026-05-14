import { Injectable, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { DashboardStore } from './dashboard.store';
import { SessionStore } from './session.store';

@Injectable({ providedIn: 'root' })
export class RealtimeService {
  private readonly session = inject(SessionStore);
  private readonly dashboard = inject(DashboardStore);
  private connection?: signalR.HubConnection;

  connect(): void {
    const organization = this.session.organization();
    if (!organization || this.connection) return;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:5000/hubs/realtime')
      .withAutomaticReconnect()
      .build();

    this.connection.on('usageUpdated', () => this.dashboard.loadUsage());
    this.connection
      .start()
      .then(() => this.connection?.invoke('JoinOrganization', organization.id))
      .catch(() => {
        this.connection = undefined;
      });
  }

  disconnect(): void {
    this.connection?.stop();
    this.connection = undefined;
  }
}
