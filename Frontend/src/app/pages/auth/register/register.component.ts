import { Component } from '@angular/core';
import { ReactiveFormsModule, Validators, FormGroup, FormBuilder, AbstractControl, ValidatorFn } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
import { Router, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ToastService } from '../../../core/services/toast.service';
import { ApiErrorHelper } from '../../../core/helpers/api-error.helper';
import { LoadingService } from '../../../core/services/loading.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './register.component.html'
})
export class RegisterComponent {

  form: FormGroup;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private toastService: ToastService,
    private loadingService: LoadingService
  ) {
    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      nome: ['', Validators.required],
      senha: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(100)]],
      confirmarSenha: ['', Validators.required]
    }, { validators: this.senhasIguais('senha', 'confirmarSenha') });
  }

  senhasIguais(senhaKey: string, confirmarKey: string): ValidatorFn {
    return (group: AbstractControl) => {
      const senha = group.get(senhaKey)?.value;
      const confirmar = group.get(confirmarKey)?.value;
      return senha === confirmar ? null : { senhasDiferentes: true };
    };
  }

  async registrar() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loadingService.show();

    try {
      await this.authService.register(this.form.value);
      this.toastService.success("'Registro efetuado com sucesso! Você será redirecionado...'");

      setTimeout(() => {
        this.router.navigate(['/login']);
      }, 3001);

    } catch (error: any) {
      this.toastService.error(ApiErrorHelper.getApiErrorMessage(error));
    } finally {
      this.loadingService.hide();
    }
  }
}
