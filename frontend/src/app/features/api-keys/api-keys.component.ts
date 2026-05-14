import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DashboardStore } from '../../core/dashboard.store';
import { ApiKey } from '../../core/models';

@Component({
  selector: 'app-api-keys',
  imports: [ReactiveFormsModule],
  templateUrl: './api-keys.component.html'
})
export class ApiKeysComponent {
  readonly dashboard = inject(DashboardStore);
  private readonly fb = inject(FormBuilder);

  readonly apiKeyForm = this.fb.nonNullable.group({
    name: ['Production key', [Validators.required]]
  });

  createApiKey(): void {
    if (this.apiKeyForm.invalid) return;

    this.dashboard.createApiKey(this.apiKeyForm.getRawValue(), () => {
      this.apiKeyForm.reset({ name: 'Production key' });
    });
  }

  revokeKey(key: ApiKey): void {
    this.dashboard.revokeApiKey(key);
  }
}
