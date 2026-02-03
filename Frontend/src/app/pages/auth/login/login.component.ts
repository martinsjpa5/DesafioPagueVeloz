import { Component } from '@angular/core';
import { ReactiveFormsModule, Validators, FormGroup, FormBuilder } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
import { Router, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { LoadingService } from '../../../core/services/loading.service';
import { ToastService } from '../../../core/services/toast.service';
import { ApiErrorHelper } from '../../../core/helpers/api-error.helper';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './login.component.html'
})
export class LoginComponent {

  form: FormGroup;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private loadingService: LoadingService,
    private toastService: ToastService
  ) {
    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      senha: ['', Validators.required]
    });
  }

  async login() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loadingService.show();

    try {
      await this.authService.login(
        this.form.value.email,
        this.form.value.senha
      );

      this.authService.saveUserEmail(this.form.value.email);
      this.toastService.success('Login efetuado com sucesso! Você será redirecionado...');

      setTimeout(() => {
        this.router.navigate(['/home']);
      }, 3501);

    } catch (error) {
      this.toastService.error(ApiErrorHelper.getApiErrorMessage(error));

    } finally {
      this.loadingService.hide();
    }
  }
}
