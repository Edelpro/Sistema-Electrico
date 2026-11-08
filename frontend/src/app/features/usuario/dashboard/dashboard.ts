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
import { Icon, IconName } from '../../../shared/icon/icon';
import {
  FacturaDetalle,
  FacturaPortal,
  PortalUsuarioResumen,
  SuministroDetalle,
} from './dashboard.model';
import { PortalUsuarioService } from './dashboard.service';

type VistaUsuario = 'resumen' | 'facturas' | 'pagos' | 'seguimiento' | 'reclamos';
type MetodoPago = 'TARJETA' | 'TRANSFERENCIA';

@Component({
  imports: [CommonModule, ReactiveFormsModule, Icon],
  selector: 'app-usuario-dashboard',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './dashboard.css',
  templateUrl: './dashboard.html',
})
export class Dashboard {
  private readonly servicio = inject(PortalUsuarioService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  readonly theme = inject(ThemeService);

  readonly cargando = signal(true);
  readonly guardando = signal(false);
  readonly error = signal('');
  readonly exito = signal('');
  readonly sinVinculo = signal(false);
  readonly menuAbierto = signal(false);
  readonly vista = signal<VistaUsuario>('resumen');
  readonly resumen = signal<PortalUsuarioResumen | null>(null);
  readonly detalle = signal<SuministroDetalle | null>(null);
  readonly factura = signal<FacturaDetalle | null>(null);
  readonly panelReclamo = signal(false);
  readonly panelPago = signal(false);
  readonly pagoError = signal('');
  readonly metodoPago = signal<MetodoPago>('TARJETA');
  readonly facturaPagoId = signal<number | null>(null);
  readonly bancoPago = signal('');
  readonly reclamoError = signal('');
  readonly descargandoEvidencia = signal<number | null>(null);
  readonly suministroId = signal<number | null>(null);
  readonly nombreUsuario = this.auth.getUsuario()?.nombreCompleto || 'Usuario';
  readonly iniciales = this.inicialesDe(this.nombreUsuario);
  readonly navegacion: Array<{ vista: VistaUsuario; etiqueta: string; icono: IconName }> = [
    { vista: 'resumen', etiqueta: 'Mi servicio', icono: 'home' },
    { vista: 'facturas', etiqueta: 'Facturas', icono: 'file-text' },
    { vista: 'pagos', etiqueta: 'Pagos', icono: 'card' },
    { vista: 'seguimiento', etiqueta: 'Seguimiento', icono: 'activity' },
    { vista: 'reclamos', etiqueta: 'Reclamos', icono: 'message' },
  ];
  readonly reclamo = this.fb.nonNullable.group({
    tipo: ['RECONEXION_DEMORADA', Validators.required],
    descripcion: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(1200)]],
    pagoId: [''],
    ordenTrabajoId: [''],
  });

  @ViewChild('descripcionReclamo')
  private descripcionReclamo?: ElementRef<HTMLTextAreaElement>;
  @ViewChild('cerrarOverlayButton')
  private cerrarOverlayButton?: ElementRef<HTMLButtonElement>;
  private disparadorOverlay?: HTMLElement;

  constructor() {
    this.cargarResumen();
  }

  cargarResumen(): void {
    this.cargando.set(true);
    this.error.set('');
    this.sinVinculo.set(false);
    this.servicio
      .resumen()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (respuesta) => {
          this.resumen.set(respuesta);
          const actual = this.suministroId();
          const id = respuesta.suministros.some((item) => item.id === actual)
            ? actual
            : respuesta.suministros[0]?.id;
          if (id) this.seleccionarSuministro(id);
          else this.cargando.set(false);
        },
        error: (error) => {
          this.sinVinculo.set(
            error instanceof HttpErrorResponse && error.error?.codigo === 'CLIENTE_NO_VINCULADO',
          );
          this.error.set(this.mensajeError(error, 'No fue posible cargar tu servicio.'));
          this.cargando.set(false);
        },
      });
  }

  cambiarSuministro(evento: Event): void {
    const id = Number((evento.target as HTMLSelectElement).value);
    if (id) this.seleccionarSuministro(id);
  }

  seleccionarSuministro(id: number): void {
    this.suministroId.set(id);
    this.cargando.set(true);
    this.servicio
      .suministro(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (respuesta) => {
          if (this.suministroId() !== id) return;
          this.detalle.set(respuesta);
          this.cargando.set(false);
        },
        error: (error) => {
          if (this.suministroId() !== id) return;
          this.error.set(this.mensajeError(error, 'No se pudo cargar el suministro.'));
          this.cargando.set(false);
        },
      });
  }

  cambiarVista(vista: VistaUsuario): void {
    this.vista.set(vista);
    this.menuAbierto.set(false);
  }

  abrirFactura(item: FacturaPortal, evento?: Event): void {
    this.disparadorOverlay = evento?.currentTarget as HTMLElement | undefined;
    this.servicio
      .factura(item.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (detalle) => {
          this.factura.set(detalle);
          setTimeout(() => this.cerrarOverlayButton?.nativeElement.focus());
        },
        error: (error) => this.error.set(this.mensajeError(error, 'No se pudo abrir la factura.')),
      });
  }

  abrirReclamo(evento?: Event): void {
    this.disparadorOverlay = evento?.currentTarget as HTMLElement | undefined;
    this.reclamoError.set('');
    this.reclamo.reset({
      tipo: 'RECONEXION_DEMORADA',
      descripcion: '',
      pagoId: '',
      ordenTrabajoId: '',
    });
    this.panelReclamo.set(true);
    setTimeout(() => this.cerrarOverlayButton?.nativeElement.focus());
  }

  abrirPago(evento?: Event): void {
    this.disparadorOverlay = evento?.currentTarget as HTMLElement | undefined;
    const primera = this.detalle()?.facturas.find((item) => item.saldoPendiente > 0);
    this.facturaPagoId.set(primera?.id ?? null);
    this.metodoPago.set('TARJETA');
    this.bancoPago.set('');
    this.pagoError.set('');
    this.panelPago.set(true);
    setTimeout(() => this.cerrarOverlayButton?.nativeElement.focus());
  }

  cerrarPago(): void {
    this.panelPago.set(false);
    this.pagoError.set('');
    queueMicrotask(() => this.disparadorOverlay?.focus());
  }

  cambiarFacturaPago(evento: Event): void {
    this.facturaPagoId.set(Number((evento.target as HTMLSelectElement).value) || null);
    this.pagoError.set('');
  }

  cambiarMetodoPago(metodo: MetodoPago): void {
    this.metodoPago.set(metodo);
    this.pagoError.set('');
  }

  facturaSeleccionadaPago(): FacturaPortal | undefined {
    return this.detalle()?.facturas.find((item) => item.id === this.facturaPagoId());
  }

  continuarPago(): void {
    if (!this.facturaSeleccionadaPago()) {
      this.pagoError.set('Selecciona una factura con saldo pendiente.');
      return;
    }
    if (this.metodoPago() === 'TRANSFERENCIA' && !this.bancoPago()) {
      this.pagoError.set('Selecciona el banco desde el que realizarás la transferencia.');
      return;
    }
    this.pagoError.set(
      'La pasarela de cobro todavía no está conectada. Configura un proveedor autorizado antes de aceptar dinero real.',
    );
  }

  cerrarReclamo(): void {
    if (this.guardando()) return;
    this.panelReclamo.set(false);
    this.reclamoError.set('');
    queueMicrotask(() => this.disparadorOverlay?.focus());
  }

  cerrarFactura(): void {
    this.factura.set(null);
    queueMicrotask(() => this.disparadorOverlay?.focus());
  }

  @HostListener('document:keydown.escape')
  cerrarOverlayConEscape(): void {
    if (this.panelPago()) this.cerrarPago();
    else if (this.panelReclamo()) this.cerrarReclamo();
    else if (this.factura()) this.cerrarFactura();
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

  crearReclamo(): void {
    const detalle = this.detalle();
    if (!detalle || this.reclamo.invalid) {
      this.reclamo.markAllAsTouched();
      queueMicrotask(() => this.descripcionReclamo?.nativeElement.focus());
      return;
    }
    const valores = this.reclamo.getRawValue();
    this.guardando.set(true);
    this.reclamoError.set('');
    this.servicio
      .crearReclamo({
        numeroSuministro: detalle.suministro.numeroSuministro,
        tipo: valores.tipo,
        descripcion: valores.descripcion,
        pagoId: valores.pagoId ? Number(valores.pagoId) : null,
        ordenTrabajoId: valores.ordenTrabajoId ? Number(valores.ordenTrabajoId) : null,
      })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.guardando.set(false)),
      )
      .subscribe({
        next: (creado) => {
          this.panelReclamo.set(false);
          this.exito.set(`Reclamo ${creado.numeroCaso} creado correctamente.`);
          queueMicrotask(() => this.disparadorOverlay?.focus());
          this.seleccionarSuministro(detalle.suministro.id);
        },
        error: (error) => {
          this.reclamoError.set(this.mensajeError(error, 'No se pudo crear el reclamo.'));
        },
      });
  }

  descargarEvidencia(id: number, codigo: string): void {
    if (this.descargandoEvidencia() !== null) return;
    this.descargandoEvidencia.set(id);
    this.error.set('');
    this.servicio
      .descargarEvidencia(id)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.descargandoEvidencia.set(null)),
      )
      .subscribe({
        next: (archivo) => this.guardarArchivo(archivo, `evidencia-${codigo}`),
        error: (error) =>
          this.error.set(this.mensajeError(error, 'No se pudo descargar la evidencia.')),
      });
  }

  alturaConsumo(item: FacturaPortal): number {
    const maximo = Math.max(
      ...(this.detalle()?.facturas.map((factura) => factura.consumoFacturado) || [1]),
      1,
    );
    return Math.max(12, Math.round((item.consumoFacturado / maximo) * 100));
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
    if (['PAGADA', 'ACTIVO', 'EJECUTADA', 'CERRADO', 'CONFIRMADO'].includes(valor))
      return 'status--success';
    if (['VENCIDA', 'SUSPENDIDO', 'FALLIDA', 'RECHAZADO'].includes(valor)) return 'status--danger';
    if (['EN_RUTA', 'EN_SITIO', 'EN_REVISION'].includes(valor)) return 'status--info';
    return 'status--warning';
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
  private guardarArchivo(archivo: Blob, nombre: string): void {
    const url = URL.createObjectURL(archivo);
    const enlace = document.createElement('a');
    enlace.href = url;
    enlace.download = nombre;
    enlace.click();
    URL.revokeObjectURL(url);
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
