import { DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { DashboardStore } from '../../core/dashboard.store';

@Component({
  selector: 'app-usage',
  imports: [DatePipe],
  templateUrl: './usage.component.html'
})
export class UsageComponent {
  readonly dashboard = inject(DashboardStore);
}
