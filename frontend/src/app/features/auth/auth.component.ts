import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { SessionStore } from '../../core/session.store';

@Component({
  selector: 'app-auth',
  imports: [ReactiveFormsModule],
  templateUrl: './auth.component.html'
})
export class AuthComponent {
  readonly session = inject(SessionStore);
  readonly authMode = signal<'login' | 'register'>('login');
  private readonly fb = inject(FormBuilder);

  readonly loginForm = this.fb.nonNullable.group({
    email: ['owner@example.com', [Validators.required, Validators.email]],
    password: ['ChangeMe123!', [Validators.required]]
  });

  readonly registerForm = this.fb.nonNullable.group({
    fullName: ['New Owner', [Validators.required]],
    email: ['owner@example.com', [Validators.required, Validators.email]],
    password: ['ChangeMe123!', [Validators.required, Validators.minLength(8)]],
    organizationName: ['Acme Cloud', [Validators.required]]
  });

  login(): void {
    if (this.loginForm.invalid) return;
    this.session.login(this.loginForm.getRawValue());
  }

  register(): void {
    if (this.registerForm.invalid) return;
    this.session.register(this.registerForm.getRawValue());
  }
}
