import { Routes } from '@angular/router';
import { AuthComponent } from './features/auth/auth.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { ApiKeysComponent } from './features/api-keys/api-keys.component';
import { BillingComponent } from './features/billing/billing.component';
import { OverviewComponent } from './features/overview/overview.component';
import { TeamComponent } from './features/team/team.component';
import { UsageComponent } from './features/usage/usage.component';

export const routes: Routes = [
  { path: 'auth', component: AuthComponent },
  {
    path: '',
    component: DashboardComponent,
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'overview' },
      { path: 'overview', component: OverviewComponent },
      { path: 'usage', component: UsageComponent },
      { path: 'billing', component: BillingComponent },
      { path: 'team', component: TeamComponent },
      { path: 'api-keys', component: ApiKeysComponent }
    ]
  },
  { path: '**', redirectTo: 'overview' }
];
