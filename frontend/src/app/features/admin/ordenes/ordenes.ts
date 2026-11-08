import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
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
import { Icon } from '../../../shared/icon/icon';
import { OrdenDetalle, OrdenResumen } from './ordenes.model';
import { OrdenesService } from './ordenes.service';
import { CatalogosAsignacion } from '../../../core/models/catalogos.model';
import { CatalogosService } from '../../../core/services/catalogos';

@Component({
  selector: 'app-admin-ordenes',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, Icon],
  templateUrl: './ordenes.html',
  styleUrl: './ordenes.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Ordenes {
  private readonly s = inject(OrdenesService);
  private readonly d = inject(DestroyRef);
  private readonly catalogosService = inject(CatalogosService);
  private readonly cargar$ = new Subject<void>();
  readonly ordenes = signal<OrdenResumen[]>([]);
  readonly total = signal(0);
  readonly pagina = signal(1);
  readonly cargando = signal(true);
  readonly guardando = signal(false);
  readonly editor = signal(false);
  readonly detalle = signal<OrdenDetalle | null>(null);
  readonly accion = signal<'asignar' | 'estado' | null>(null);
  readonly error = signal('');
  readonly exito = signal('');
  readonly catalogos = signal<CatalogosAsignacion | null>(null);
  readonly catalogosError = signal('');
  readonly busqueda = new FormControl('', { nonNullable: true });
  readonly form = new FormGroup({
    numeroSuministro: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    tipo: new FormControl('RECONEXION', { nonNullable: true }),
    causa: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    prioridad: new FormControl('NORMAL', { nonNullable: true }),
    fechaLimiteSlaUtc: new FormControl('', { nonNullable: true }),
    correoTecnico: new FormControl('', { nonNullable: true, validators: [Validators.email] }),
    codigoCuadrilla: new FormControl('', { nonNullable: true }),
    motivo: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(5)],
    }),
  });
  readonly accionForm = new FormGroup({
    correoTecnico: new FormControl('', { nonNullable: true, validators: [Validators.email] }),
    codigoCuadrilla: new FormControl('', { nonNullable: true }),
    estadoNuevo: new FormControl('', { nonNullable: true }),
    motivo: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(5)],
    }),
    observacion: new FormControl('', { nonNullable: true }),
  });
  constructor() {
    this.catalogosService.asignacionOperativa()
      .pipe(takeUntilDestroyed(this.d))
      .subscribe({
        next: (datos) => this.catalogos.set(datos),
        error: () => this.catalogosError.set('No se pudieron cargar los suministros, técnicos y cuadrillas.'),
      });
    this.cargar$
      .pipe(
        switchMap(() => {
          this.cargando.set(true);
          this.error.set('');
          return this.s.listar(this.busqueda.value.trim(), this.pagina()).pipe(
            map((r) => ({ r, error: false })),
            catchError(() => of({ r: null, error: true })),
            finalize(() => this.cargando.set(false)),
          );
        }),
        takeUntilDestroyed(this.d),
      )
      .subscribe((x) => {
        if (x.error || !x.r) {
          this.error.set('No fue posible consultar las órdenes. Verifica la API.');
          return;
        }
        this.ordenes.set(x.r.items);
        this.total.set(x.r.total);
      });
    this.busqueda.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.d))
      .subscribe(() => {
        this.pagina.set(1);
        this.cargar();
      });
    this.cargar();
  }
  cargar() {
    this.cargar$.next();
  }
  limpiar() {
    this.busqueda.setValue('');
  }
  crear() {
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      this.error.set('Corrige los campos marcados antes de crear la orden.');
      this.enfocarPrimerInvalido('.drawer form');
      return;
    }
    const v = this.form.getRawValue();
    this.guardando.set(true);
    this.s
      .crear({
        ...v,
        numeroSuministro: v.numeroSuministro.trim().toUpperCase(),
        causa: v.causa.trim().toUpperCase(),
        correoTecnico: v.correoTecnico.trim().toLowerCase() || null,
        codigoCuadrilla: v.codigoCuadrilla.trim().toUpperCase() || null,
        fechaLimiteSlaUtc: v.fechaLimiteSlaUtc ? new Date(v.fechaLimiteSlaUtc).toISOString() : null,
        motivo: v.motivo.trim(),
      })
      .pipe(
        finalize(() => this.guardando.set(false)),
        takeUntilDestroyed(this.d),
      )
      .subscribe({
        next: () => {
          this.editor.set(false);
          this.exito.set('Orden creada y registrada en auditoría.');
          this.form.reset({
            numeroSuministro: '',
            tipo: 'RECONEXION',
            causa: '',
            prioridad: 'NORMAL',
            fechaLimiteSlaUtc: '',
            correoTecnico: '',
            codigoCuadrilla: '',
            motivo: '',
          });
          this.cargar();
        },
        error: (e) => this.error.set(this.mensaje(e, 'No se pudo crear la orden.')),
      });
  }
  ver(o: OrdenResumen) {
    this.s
      .obtener(o.id)
      .pipe(takeUntilDestroyed(this.d))
      .subscribe({
        next: (x) => this.detalle.set(x),
        error: () => this.error.set('No se pudo abrir el detalle.'),
      });
  }
  abrirAccion(tipo: 'asignar' | 'estado') {
    this.accion.set(tipo);
    this.accionForm.reset({
      correoTecnico: '',
      codigoCuadrilla: '',
      estadoNuevo: '',
      motivo: '',
      observacion: '',
    });
  }
  guardarAccion() {
    const o = this.detalle();
    if (!o) return;
    this.accionForm.markAllAsTouched();
    const v = this.accionForm.getRawValue();
    if (
      this.accionForm.controls.motivo.invalid ||
      (this.accion() === 'asignar' && !v.correoTecnico.trim() && !v.codigoCuadrilla.trim()) ||
      (this.accion() === 'estado' && !v.estadoNuevo)
    ) {
      this.error.set('Completa los datos obligatorios de la gestión.');
      return;
    }
    this.guardando.set(true);
    const req =
      this.accion() === 'asignar'
        ? this.s.asignar(
            o.id,
            v.correoTecnico.trim().toLowerCase(),
            v.codigoCuadrilla.trim().toUpperCase(),
            v.motivo.trim(),
          )
        : this.s.transicionar(o.id, v.estadoNuevo, v.motivo.trim(), v.observacion.trim());
    req
      .pipe(
        finalize(() => this.guardando.set(false)),
        takeUntilDestroyed(this.d),
      )
      .subscribe({
        next: (x) => {
          this.detalle.set(x);
          this.accion.set(null);
          this.exito.set('Orden actualizada correctamente.');
          this.cargar();
        },
        error: (e) => this.error.set(this.mensaje(e, 'No se pudo actualizar la orden.')),
      });
  }
  opciones(o: OrdenDetalle) {
    const m: Record<string, string[]> = {
      CREADA: ['CANCELADA'],
      ASIGNADA: ['ACEPTADA', 'CANCELADA'],
      ACEPTADA: ['EN_RUTA', 'CANCELADA'],
      EN_RUTA: ['EN_SITIO', 'CANCELADA'],
      EN_SITIO: ['EJECUTADA', 'FALLIDA'],
      FALLIDA: ['REPROGRAMADA'],
      REPROGRAMADA: [],
      EJECUTADA: ['CERRADA'],
    };
    return m[o.estado] ?? [];
  }
  fecha(x: string | null) {
    return x
      ? new Date(x).toLocaleString('es-EC', { timeZone: 'America/Guayaquil' })
      : 'Sin SLA configurado';
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
  private mensaje(e: any, f: string) {
    const campos = Object.values(e?.error?.errors ?? {})
      .flat()
      .join(' ');
    return e?.error?.mensaje || campos || f;
  }

  private enfocarPrimerInvalido(contenedor: string): void {
    queueMicrotask(() =>
      document
        .querySelector<HTMLElement>(`${contenedor} .ng-invalid:not(form)`)
        ?.focus(),
    );
  }
}
