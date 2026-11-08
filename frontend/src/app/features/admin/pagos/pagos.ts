import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { FormArray, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  Subject,
  catchError,
  debounceTime,
  distinctUntilChanged,
  finalize,
  map,
  of,
  switchMap,
} from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Icon } from '../../../shared/icon/icon';
import { CatalogosAsignacion, FacturaCatalogo } from '../../../core/models/catalogos.model';
import { CatalogosService } from '../../../core/services/catalogos';
import { PagoDetalle, PagoResumen } from './pagos.model';
import { PagosService } from './pagos.service';

@Component({
  selector: 'app-admin-pagos',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, Icon],
  templateUrl: './pagos.html',
  styleUrl: './pagos.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Pagos {
  private service = inject(PagosService);
  private catalogosService = inject(CatalogosService);
  private destroy = inject(DestroyRef);
  private readonly cargar$ = new Subject<void>();
  pagos = signal<PagoResumen[]>([]);
  total = signal(0);
  pagina = signal(1);
  cargando = signal(true);
  guardando = signal(false);
  editor = signal(false);
  detalle = signal<PagoDetalle | null>(null);
  mensaje = signal('');
  error = signal('');
  resultado = signal('');
  catalogos = signal<CatalogosAsignacion | null>(null);
  catalogosError = signal('');
  busqueda = new FormControl('', { nonNullable: true });
  formulario = new FormGroup({
    numeroSuministro: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    proveedor: new FormControl('BANCO/PASARELA', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    eventoProveedorId: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    referencia: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    monto: new FormControl(0, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(0.01)],
    }),
    fechaProveedorUtc: new FormControl(this.fechaActual(), {
      nonNullable: true,
      validators: [Validators.required],
    }),
    motivo: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(5)],
    }),
    asignaciones: new FormArray([this.nuevaAsignacion()]),
  });
  constructor() {
    this.catalogosService
      .asignacionOperativa()
      .pipe(takeUntilDestroyed(this.destroy))
      .subscribe({
        next: (datos) => this.catalogos.set(datos),
        error: () =>
          this.catalogosError.set(
            'No fue posible cargar los suministros y facturas disponibles.',
          ),
      });
    this.formulario.controls.numeroSuministro.valueChanges
      .pipe(takeUntilDestroyed(this.destroy))
      .subscribe(() => {
        const disponibles = new Set(this.facturasDisponibles().map((factura) => factura.numeroFactura));
        this.formulario.controls.asignaciones.controls.forEach((asignacion) => {
          if (!disponibles.has(asignacion.controls.numeroFactura.value)) {
            asignacion.controls.numeroFactura.setValue('');
          }
        });
      });
    this.cargar$
      .pipe(
        switchMap(() => {
          this.cargando.set(true);
          this.error.set('');
          return this.service.listar(this.busqueda.value.trim(), this.pagina()).pipe(
            map((r) => ({ r, error: false })),
            catchError(() => of({ r: null, error: true })),
            finalize(() => this.cargando.set(false)),
          );
        }),
        takeUntilDestroyed(this.destroy),
      )
      .subscribe((x) => {
        if (x.error || !x.r) {
          this.error.set(
            'No fue posible consultar los pagos. Verifica que la API esté ejecutándose.',
          );
          return;
        }
        this.pagos.set(x.r.items);
        this.total.set(x.r.total);
      });
    this.busqueda.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroy))
      .subscribe(() => {
        this.pagina.set(1);
        this.cargar();
      });
    this.cargar();
  }
  nuevaAsignacion() {
    return new FormGroup({
      numeroFactura: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
      montoAplicado: new FormControl(0, {
        nonNullable: true,
        validators: [Validators.required, Validators.min(0.01)],
      }),
    });
  }
  facturasDisponibles(): FacturaCatalogo[] {
    const suministro = this.formulario.controls.numeroSuministro.value;
    return (this.catalogos()?.facturas ?? []).filter(
      (factura) => factura.numeroSuministro === suministro,
    );
  }
  agregar() {
    this.formulario.controls.asignaciones.push(this.nuevaAsignacion());
  }
  quitar(i: number) {
    if (this.formulario.controls.asignaciones.length > 1)
      this.formulario.controls.asignaciones.removeAt(i);
  }
  abrir() {
    this.detalle.set(null);
    this.editor.set(true);
    this.mensaje.set('');
    this.resultado.set('');
  }
  cerrar() {
    if (!this.guardando()) this.editor.set(false);
  }
  cargar() {
    this.cargar$.next();
  }
  limpiar() {
    this.busqueda.setValue('');
  }
  ver(p: PagoResumen) {
    this.service
      .obtener(p.id)
      .pipe(takeUntilDestroyed(this.destroy))
      .subscribe({
        next: (d) => this.detalle.set(d),
        error: () => this.error.set('No se pudo abrir el pago.'),
      });
  }
  conciliar() {
    this.formulario.markAllAsTouched();
    if (this.formulario.invalid) {
      this.mensaje.set('Corrige los campos marcados antes de confirmar.');
      return;
    }
    const v = this.formulario.getRawValue();
    const aplicado = v.asignaciones.reduce((t, a) => t + Number(a.montoAplicado), 0);
    if (aplicado > v.monto) {
      this.mensaje.set('El total aplicado a facturas no puede superar el monto recibido.');
      return;
    }
    this.guardando.set(true);
    this.mensaje.set('');
    this.service
      .conciliar({
        ...v,
        numeroSuministro: v.numeroSuministro.trim().toUpperCase(),
        proveedor: v.proveedor.trim().toUpperCase(),
        eventoProveedorId: v.eventoProveedorId.trim().toUpperCase(),
        referencia: v.referencia.trim().toUpperCase(),
        fechaProveedorUtc: new Date(v.fechaProveedorUtc).toISOString(),
        motivo: v.motivo.trim(),
        asignaciones: v.asignaciones.map((a) => ({
          ...a,
          numeroFactura: a.numeroFactura.trim().toUpperCase(),
        })),
      })
      .pipe(
        finalize(() => this.guardando.set(false)),
        takeUntilDestroyed(this.destroy),
      )
      .subscribe({
        next: (r) => {
          this.editor.set(false);
          this.resultado.set(r.resultadoOperativo.mensaje);
          this.formulario.reset({
            numeroSuministro: '',
            proveedor: 'BANCO/PASARELA',
            eventoProveedorId: '',
            referencia: '',
            monto: 0,
            fechaProveedorUtc: this.fechaActual(),
            motivo: '',
          });
          while (this.formulario.controls.asignaciones.length > 1)
            this.formulario.controls.asignaciones.removeAt(1);
          this.formulario.controls.asignaciones
            .at(0)
            .reset({ numeroFactura: '', montoAplicado: 0 });
          this.cargar();
        },
        error: (e) => {
          const campos = Object.values(e.error?.errors ?? {})
            .flat()
            .join(' ');
          this.mensaje.set(e.error?.mensaje || campos || 'No se pudo conciliar el pago.');
        },
      });
  }
  anterior() {
    if (this.pagina() > 1) {
      this.pagina.update((x) => x - 1);
      this.cargar();
    }
  }
  siguiente() {
    if (this.pagina() * 10 < this.total()) {
      this.pagina.update((x) => x + 1);
      this.cargar();
    }
  }
  privada(fecha: string) {
    return new Date(fecha).toLocaleString('es-EC');
  }
  private fechaActual() {
    const d = new Date();
    d.setMinutes(d.getMinutes() - d.getTimezoneOffset());
    return d.toISOString().slice(0, 16);
  }
}
