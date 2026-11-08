import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, ElementRef, ViewChild, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { AuthService } from '../../../core/services/auth';
import { Icon } from '../../../shared/icon/icon';

@Component({
  selector: 'app-solicitar-recuperacion',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './solicitar-recuperacion.html',
  styleUrl: './recuperacion.css',
})
export class SolicitarRecuperacion {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  readonly cargando = signal(false);
  readonly error = signal('');
  readonly enviado = signal(false);
  readonly formulario = this.fb.nonNullable.group({
    correo: ['', [Validators.required, Validators.email]],
  });

  @ViewChild('correoInput') private correoInput?: ElementRef<HTMLInputElement>;

  enviar(): void {
    if (this.cargando()) return;
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      queueMicrotask(() => this.correoInput?.nativeElement.focus());
      return;
    }
    this.cargando.set(true);
    this.error.set('');
    this.auth
      .solicitarRecuperacion(this.formulario.controls.correo.value)
      .pipe(finalize(() => this.cargando.set(false)))
      .subscribe({
        next: () => this.enviado.set(true),
        error: (err: HttpErrorResponse) =>
          this.error.set(
            err.status === 429
              ? 'Se realizaron demasiados intentos. Espera 15 minutos y vuelve a probar.'
              : err.status === 0
                ? 'No pudimos conectar con el servicio. Revisa la conexión.'
                : 'No pudimos procesar la solicitud. Inténtalo nuevamente.',
          ),
      });
  }
}
