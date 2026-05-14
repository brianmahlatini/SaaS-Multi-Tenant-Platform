import { DatePipe, TitleCasePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { DashboardStore } from '../../core/dashboard.store';

@Component({
  selector: 'app-overview',
  imports: [DatePipe, TitleCasePipe],
  templateUrl: './overview.component.html'
})
export class OverviewComponent {
  readonly dashboard = inject(DashboardStore);
}
