import { TitleCasePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { DashboardStore } from '../../core/dashboard.store';

@Component({
  selector: 'app-billing',
  imports: [TitleCasePipe],
  templateUrl: './billing.component.html'
})
export class BillingComponent {
  readonly dashboard = inject(DashboardStore);
  readonly plans = ['free', 'pro', 'enterprise'];
}
