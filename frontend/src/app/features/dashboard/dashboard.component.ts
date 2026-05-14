import { Component, OnInit, effect, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { DashboardStore } from '../../core/dashboard.store';
import { RealtimeService } from '../../core/realtime.service';
import { SessionStore } from '../../core/session.store';

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit {
  readonly session = inject(SessionStore);
  readonly dashboard = inject(DashboardStore);
  readonly realtime = inject(RealtimeService);
  private readonly router = inject(Router);
  private hydrated = false;

  constructor() {
    effect(() => {
      if (this.session.isAuthed() && !this.hydrated) {
        this.hydrated = true;
        this.dashboard.refresh();
        this.realtime.connect();
      }
    });
  }

  ngOnInit(): void {
    this.session.restore();

    if (!this.session.token()) {
      this.router.navigateByUrl('/auth');
    }
  }
}
