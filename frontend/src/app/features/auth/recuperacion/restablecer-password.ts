import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, ElementRef, ViewChild, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { AuthService } from '../../../core/services/auth';
import { Icon } from '../../../shared/icon/icon';

@Component({
  selector: 'app-restablecer-password',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './restablecer-password.html',
  styleUrl: './recuperacion.css',
})
export class RestablecerPassword {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly cargando = signal(false);
  readonly error = signal('');
  readonly completado = signal(false);
  readonly mostrar = signal(false);
  readonly enlaceValido = signal(true);
  private token = '';
  private correo = '';

  readonly formulario = this.fb.nonNullable.group({
    nuevaPassword: ['', [Validators.required, Validators.minLength(10), Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).+$/)]],
    confirmarPassword: ['', Validators.required],
  });

  @ViewChild('passwordInput') private passwordInput?: ElementRef<HTMLInputElement>;

  constructor() {
    const fragmento = new URLSearchParams(window.location.hash.replace(/^#/, ''));
    this.token = fragmento.get('token') || '';
    this.correo = fragmento.get('correo') || '';
    this.enlaceValido.set(!!this.token && /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.correo));
    history.replaceState(null, '', this.router.url.split('#')[0]);
  }

  guardar(): void {
    if (this.cargando()) return;
    this.error.set('');
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      queueMicrotask(() => this.passwordInput?.nativeElement.focus());
      return;
    }
    const valores = this.formulario.getRawValue();
    if (valores.nuevaPassword !== valores.confirmarPassword) {
      this.formulario.controls.confirmarPassword.setErrors({ noCoincide: true });
      return;
    }
    this.cargando.set(true);
    this.auth
      .restablecerPassword({ correo: this.correo, token: this.token, nuevaPassword: valores.nuevaPassword })
      .pipe(finalize(() => this.cargando.set(false)))
      .subscribe({
        next: () => {
          this.token = '';
          this.formulario.reset();
          this.completado.set(true);
        },
        error: (err: HttpErrorResponse) =>
          this.error.set(
            err.error?.mensaje ||
              (err.status === 0 ? 'No pudimos conectar con el servicio.' : 'No se pudo actualizar la contraseña.'),
          ),
      });
  }
}
