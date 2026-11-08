import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  ViewChild,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { AuthService } from '../../../core/services/auth';
import { Icon } from '../../../shared/icon/icon';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  @ViewChild('correoInput')
  private correoInput?: ElementRef<HTMLInputElement>;

  @ViewChild('passwordInput')
  private passwordInput?: ElementRef<HTMLInputElement>;

  readonly cargando = signal(false);
  readonly mensajeError = signal('');
  readonly mostrarPassword = signal(false);
  readonly mostrarAyuda = signal(false);

  readonly formulario = this.fb.nonNullable.group({
    correo: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  constructor() {
    this.formulario.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.mensajeError.set(''));
  }

  alternarPassword(): void {
    this.mostrarPassword.update((visible) => !visible);
    queueMicrotask(() => this.passwordInput?.nativeElement.focus());
  }

  alternarAyuda(): void {
    this.mostrarAyuda.update((visible) => !visible);
  }

  iniciarSesion(): void {
    if (this.cargando()) {
      return;
    }

    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      this.enfocarPrimerError();
      return;
    }

    this.cargando.set(true);
    this.mensajeError.set('');

    this.authService
      .login(this.formulario.getRawValue())
      .pipe(finalize(() => this.cargando.set(false)))
      .subscribe({
        next: (respuesta) => {
          const destinoPorRol: Record<string, string> = {
            Administrador: '/admin',
            Tecnico: '/tecnico',
            Usuario: '/usuario',
          };
          const destino = destinoPorRol[respuesta.rol];

          if (!destino) {
            this.mensajeError.set('Tu cuenta no tiene un rol habilitado para este sistema.');
            return;
          }

          void this.router.navigate([destino]);
        },
        error: (error: HttpErrorResponse | Error) => {
          this.formulario.controls.password.setValue('');
          this.mensajeError.set(
            error instanceof Error && error.message === 'SESSION_STORAGE_UNAVAILABLE'
              ? 'El navegador no permitió guardar la sesión. Habilita el almacenamiento del sitio e inténtalo de nuevo.'
              : error instanceof HttpErrorResponse && error.status === 0
                ? 'No pudimos conectar con el servicio. Revisa la conexión e inténtalo de nuevo.'
                : 'El correo o la contraseña no son correctos.',
          );
          queueMicrotask(() => this.passwordInput?.nativeElement.focus());
        },
      });
  }

  private enfocarPrimerError(): void {
    queueMicrotask(() => {
      if (this.formulario.controls.correo.invalid) {
        this.correoInput?.nativeElement.focus();
        return;
      }

      this.passwordInput?.nativeElement.focus();
    });
  }
}
