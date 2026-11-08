import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  HostListener,
  ViewChild,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';

import { AuthService } from '../../../core/services/auth';
import { ThemeService } from '../../../core/services/theme';
import { Icon } from '../../../shared/icon/icon';
import { PortalTecnicoResumen, TecnicoOrdenDetalle, TecnicoOrdenResumen } from './dashboard.model';
import { PortalTecnicoService } from './dashboard.service';

@Component({
  imports: [CommonModule, ReactiveFormsModule, Icon],
  selector: 'app-tecnico-dashboard',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './dashboard.css',
  templateUrl: './dashboard.html',
})
export class Dashboard {
  private readonly servicio = inject(PortalTecnicoService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  readonly theme = inject(ThemeService);

  readonly cargando = signal(true);
  readonly guardando = signal(false);
  readonly error = signal('');
  readonly panelError = signal('');
  readonly archivoError = signal('');
  readonly exito = signal('');
  readonly resumen = signal<PortalTecnicoResumen | null>(null);
  readonly detalle = signal<TecnicoOrdenDetalle | null>(null);
  readonly menuAbierto = signal(false);
  readonly panelEvidencia = signal(false);
  readonly ubicando = signal(false);
  readonly archivo = signal<File | null>(null);
  readonly descargandoEvidencia = signal<number | null>(null);
  readonly nombreUsuario = this.auth.getUsuario()?.nombreCompleto || 'Técnico';
  readonly iniciales = this.inicialesDe(this.nombreUsuario);

  readonly filtros = this.fb.nonNullable.group({ buscar: [''], estado: ['TODOS'] });
  readonly transicion = this.fb.nonNullable.group({
    estadoNuevo: ['', Validators.required],
    motivo: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(300)]],
    observacion: ['', Validators.maxLength(500)],
  });
  readonly evidencia = this.fb.nonNullable.group({
    fechaDispositivoUtc: [this.fechaLocal(), Validators.required],
    latitud: [''],
    longitud: [''],
    precisionMetros: [''],
    lecturaMedidor: [''],
    observacion: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(1000)]],
  });

  @ViewChild('archivoInput') private archivoInput?: ElementRef<HTMLInputElement>;
  @ViewChild('motivoInput') private motivoInput?: ElementRef<HTMLInputElement>;
  @ViewChild('observacionTransicion')
  private observacionTransicion?: ElementRef<HTMLTextAreaElement>;
  @ViewChild('observacionEvidencia')
  private observacionEvidencia?: ElementRef<HTMLTextAreaElement>;
  @ViewChild('cerrarDetalleButton')
  private cerrarDetalleButton?: ElementRef<HTMLButtonElement>;
  private disparadorDetalle?: HTMLElement;

  constructor() {
    this.transicion.controls.estadoNuevo.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((estado) => {
        const observacion = this.transicion.controls.observacion;
        observacion.setValidators(
          estado === 'FALLIDA'
            ? [Validators.required, Validators.minLength(5), Validators.maxLength(500)]
            : [Validators.maxLength(500)],
        );
        observacion.updateValueAndValidity({ emitEvent: false });
        this.panelError.set('');
      });
    this.cargar();
  }

  cargar(): void {
    this.cargando.set(true);
    this.error.set('');
    const { buscar, estado } = this.filtros.getRawValue();
    this.servicio
      .resumen(buscar, estado)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (respuesta) => {
          this.resumen.set(respuesta);
          this.cargando.set(false);
        },
        error: (error) => {
          this.error.set(this.mensajeError(error, 'No fue posible cargar tus órdenes asignadas.'));
          this.cargando.set(false);
        },
      });
  }

  limpiarFiltros(): void {
    this.filtros.setValue({ buscar: '', estado: 'TODOS' });
    this.cargar();
  }

  abrirOrden(orden: TecnicoOrdenResumen, evento?: Event): void {
    this.disparadorDetalle = evento?.currentTarget as HTMLElement | undefined;
    this.error.set('');
    this.panelError.set('');
    this.archivoError.set('');
    this.servicio
      .detalle(orden.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (detalle) => {
          this.detalle.set(detalle);
          this.panelEvidencia.set(false);
          this.transicion.reset({
            estadoNuevo: detalle.transicionesPermitidas[0] || '',
            motivo: '',
            observacion: '',
          });
          setTimeout(() => this.cerrarDetalleButton?.nativeElement.focus());
        },
        error: (error) => this.error.set(this.mensajeError(error, 'No se pudo abrir la orden.')),
      });
  }

  cerrarDetalle(): void {
    this.detalle.set(null);
    this.panelEvidencia.set(false);
    this.panelError.set('');
    this.archivoError.set('');
    queueMicrotask(() => this.disparadorDetalle?.focus());
  }

  @HostListener('document:keydown.escape')
  cerrarOverlayConEscape(): void {
    if (this.detalle()) this.cerrarDetalle();
    else this.menuAbierto.set(false);
  }

  atraparFoco(evento: KeyboardEvent): void {
    if (evento.key !== 'Tab') return;
    const contenedor = evento.currentTarget as HTMLElement;
    const controles = Array.from(
      contenedor.querySelectorAll<HTMLElement>(
        'button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [href]',
      ),
    ).filter((elemento) => elemento.offsetParent !== null);
    if (!controles.length) return;
    const primero = controles[0];
    const ultimo = controles[controles.length - 1];
    if (evento.shiftKey && document.activeElement === primero) {
      evento.preventDefault();
      ultimo.focus();
    } else if (!evento.shiftKey && document.activeElement === ultimo) {
      evento.preventDefault();
      primero.focus();
    }
  }

  guardarTransicion(): void {
    const detalle = this.detalle();
    if (!detalle || this.transicion.invalid) {
      this.transicion.markAllAsTouched();
      queueMicrotask(() => {
        if (this.transicion.controls.motivo.invalid) this.motivoInput?.nativeElement.focus();
        else this.observacionTransicion?.nativeElement.focus();
      });
      return;
    }
    const valores = this.transicion.getRawValue();
    this.guardando.set(true);
    this.panelError.set('');
    this.servicio
      .transicionar(detalle.orden.id, valores.estadoNuevo, valores.motivo, valores.observacion)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.guardando.set(false)),
      )
      .subscribe({
        next: (actualizado) => {
          this.detalle.set(actualizado);
          this.transicion.reset({
            estadoNuevo: actualizado.transicionesPermitidas[0] || '',
            motivo: '',
            observacion: '',
          });
          this.exito.set(`Orden actualizada a ${this.etiqueta(actualizado.orden.estado)}.`);
          this.cargar();
        },
        error: (error) => {
          this.panelError.set(this.mensajeError(error, 'No se pudo actualizar la orden.'));
        },
      });
  }

  seleccionarArchivo(evento: Event): void {
    const input = evento.target as HTMLInputElement;
    const archivo = input.files?.[0] || null;
    this.archivoError.set('');
    if (!archivo) {
      this.archivo.set(null);
      return;
    }
    if (!['image/jpeg', 'image/png', 'image/webp'].includes(archivo.type)) {
      this.archivo.set(null);
      input.value = '';
      this.archivoError.set('Selecciona una fotografía JPG, PNG o WebP.');
      return;
    }
    if (archivo.size > 5 * 1024 * 1024) {
      this.archivo.set(null);
      input.value = '';
      this.archivoError.set('La fotografía no puede superar 5 MB.');
      return;
    }
    this.archivo.set(archivo);
  }

  capturarUbicacion(): void {
    if (!navigator.geolocation) {
      this.panelError.set(
        'Este dispositivo no permite obtener la ubicación. Puedes ingresarla manualmente.',
      );
      return;
    }
    this.ubicando.set(true);
    navigator.geolocation.getCurrentPosition(
      (posicion) => {
        this.evidencia.patchValue({
          latitud: posicion.coords.latitude.toFixed(6),
          longitud: posicion.coords.longitude.toFixed(6),
          precisionMetros: posicion.coords.accuracy.toFixed(1),
        });
        this.ubicando.set(false);
      },
      () => {
        this.panelError.set(
          'No fue posible obtener la ubicación. Revisa el permiso del navegador.',
        );
        this.ubicando.set(false);
      },
      { enableHighAccuracy: true, timeout: 10000 },
    );
  }

  guardarEvidencia(): void {
    const detalle = this.detalle();
    const archivo = this.archivo();
    if (!detalle || this.evidencia.invalid || !archivo) {
      this.evidencia.markAllAsTouched();
      if (!archivo) {
        this.archivoError.set('Selecciona una fotografía de la evidencia.');
        queueMicrotask(() => this.archivoInput?.nativeElement.focus());
      } else {
        queueMicrotask(() => this.observacionEvidencia?.nativeElement.focus());
      }
      return;
    }
    if (archivo.size > 5 * 1024 * 1024) {
      this.archivoError.set('La fotografía no puede superar 5 MB.');
      return;
    }
    const valores = this.evidencia.getRawValue();
    const datos = new FormData();
    datos.append('fotografia', archivo);
    datos.append('fechaDispositivoUtc', new Date(valores.fechaDispositivoUtc).toISOString());
    datos.append('observacion', valores.observacion);
    this.agregarSiExiste(datos, 'latitud', valores.latitud);
    this.agregarSiExiste(datos, 'longitud', valores.longitud);
    this.agregarSiExiste(datos, 'precisionMetros', valores.precisionMetros);
    this.agregarSiExiste(datos, 'lecturaMedidor', valores.lecturaMedidor);
    this.guardando.set(true);
    this.panelError.set('');
    this.archivoError.set('');
    this.servicio
      .registrarEvidencia(detalle.orden.id, datos)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.guardando.set(false)),
      )
      .subscribe({
        next: () => {
          this.exito.set('Evidencia protegida y registrada correctamente.');
          this.archivo.set(null);
          if (this.archivoInput) this.archivoInput.nativeElement.value = '';
          this.evidencia.reset({
            fechaDispositivoUtc: this.fechaLocal(),
            latitud: '',
            longitud: '',
            precisionMetros: '',
            lecturaMedidor: '',
            observacion: '',
          });
          this.servicio
            .detalle(detalle.orden.id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
              next: (actualizado) => this.detalle.set(actualizado),
              error: (error) =>
                this.panelError.set(
                  this.mensajeError(error, 'La evidencia se guardó, pero no se pudo refrescar.'),
                ),
            });
          this.cargar();
        },
        error: (error) => {
          this.panelError.set(this.mensajeError(error, 'No se pudo registrar la evidencia.'));
        },
      });
  }

  descargarEvidencia(id: number, nombreArchivo: string): void {
    if (this.descargandoEvidencia() !== null) return;
    this.descargandoEvidencia.set(id);
    this.panelError.set('');
    this.servicio
      .descargarEvidencia(id)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.descargandoEvidencia.set(null)),
      )
      .subscribe({
        next: (archivo) => this.guardarArchivo(archivo, nombreArchivo),
        error: (error) =>
          this.panelError.set(this.mensajeError(error, 'No se pudo descargar la evidencia.')),
      });
  }

  cerrarSesion(): void {
    this.auth.logout();
    void this.router.navigate(['/login']);
  }

  etiqueta(valor: string): string {
    return valor
      .replaceAll('_', ' ')
      .toLocaleLowerCase('es-EC')
      .replace(/^./, (letra) => letra.toUpperCase());
  }

  claseEstado(valor: string): string {
    if (['EJECUTADA', 'CERRADA'].includes(valor)) return 'status--success';
    if (['FALLIDA', 'CANCELADA'].includes(valor)) return 'status--danger';
    if (['EN_RUTA', 'EN_SITIO', 'ACEPTADA'].includes(valor)) return 'status--info';
    return 'status--warning';
  }

  private agregarSiExiste(datos: FormData, nombre: string, valor: string): void {
    if (valor.trim()) datos.append(nombre, valor.trim());
  }
  private guardarArchivo(archivo: Blob, nombre: string): void {
    const url = URL.createObjectURL(archivo);
    const enlace = document.createElement('a');
    enlace.href = url;
    enlace.download = nombre;
    enlace.click();
    URL.revokeObjectURL(url);
  }
  private fechaLocal(): string {
    return new Date(Date.now() - new Date().getTimezoneOffset() * 60000).toISOString().slice(0, 16);
  }
  private inicialesDe(nombre: string): string {
    return nombre
      .split(' ')
      .filter(Boolean)
      .slice(0, 2)
      .map((parte) => parte[0])
      .join('')
      .toUpperCase();
  }
  private mensajeError(error: unknown, respaldo: string): string {
    if (error instanceof HttpErrorResponse) {
      const cuerpo = error.error as {
        mensaje?: string;
        title?: string;
        errors?: Record<string, string[]>;
      } | null;
      return (
        cuerpo?.mensaje || Object.values(cuerpo?.errors || {})[0]?.[0] || cuerpo?.title || respaldo
      );
    }
    return respaldo;
  }
}
