import { DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DashboardStore } from '../../core/dashboard.store';
import { SessionStore } from '../../core/session.store';

@Component({
  selector: 'app-team',
  imports: [DatePipe, ReactiveFormsModule],
  templateUrl: './team.component.html'
})
export class TeamComponent {
  readonly dashboard = inject(DashboardStore);
  readonly session = inject(SessionStore);
  private readonly fb = inject(FormBuilder);

  readonly inviteForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    role: ['Member', [Validators.required]]
  });

  invite(): void {
    if (this.inviteForm.invalid) return;

    this.dashboard.invite(this.inviteForm.getRawValue(), () => {
      this.inviteForm.reset({ email: '', role: 'Member' });
    });
  }
}
