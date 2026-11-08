import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  ViewChild,
  inject,
  signal,
} from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
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
import { CrearReclamo, EstadoReclamo, ReclamoDetalle, ReclamoResumen } from './reclamos.model';
import { ReclamosService } from './reclamos.service';
import {
  CatalogosAsignacion,
  OrdenCatalogo,
  PagoCatalogo,
} from '../../../core/models/catalogos.model';
import { CatalogosService } from '../../../core/services/catalogos';
@Component({
  selector: 'app-admin-reclamos',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, Icon],
  templateUrl: './reclamos.html',
  styleUrl: './reclamos.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Reclamos {
  private s = inject(ReclamosService);
  private d = inject(DestroyRef);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private catalogosService = inject(CatalogosService);
  private cargar$ = new Subject<void>();
  items = signal<ReclamoResumen[]>([]);
  total = signal(0);
  pagina = signal(this.paginaInicial());
  cargando = signal(true);
  guardando = signal(false);
  editor = signal(false);
  editorError = signal('');
  detalle = signal<ReclamoDetalle | null>(null);
  gestion = signal(false);
  gestionError = signal('');
  error = signal('');
  exito = signal('');
  catalogos = signal<CatalogosAsignacion | null>(null);
  enviado = signal(false);
  busqueda = new FormControl(this.route.snapshot.queryParamMap.get('q') ?? '', {
    nonNullable: true,
  });
  form = new FormGroup({
    numeroSuministro: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    tipo: new FormControl('PAGO_NO_REFLEJADO', { nonNullable: true }),
    prioridad: new FormControl('NORMAL', { nonNullable: true }),
    descripcion: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(10), Validators.maxLength(1200)],
    }),
    pagoId: new FormControl<number | null>(null),
    ordenTrabajoId: new FormControl<number | null>(null),
    correoAsignado: new FormControl('', { nonNullable: true, validators: [Validators.email] }),
  });
  gestionForm = new FormGroup({
    estadoNuevo: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    motivo: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(5)],
    }),
    resolucion: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(1200)],
    }),
    correoAsignado: new FormControl('', { nonNullable: true, validators: [Validators.email] }),
  });
  @ViewChild('searchInput') searchInput?: ElementRef<HTMLInputElement>;
  constructor() {
    this.catalogosService.asignacionOperativa()
      .pipe(takeUntilDestroyed(this.d))
      .subscribe({ next: (datos) => this.catalogos.set(datos) });
    this.form.controls.numeroSuministro.valueChanges
      .pipe(takeUntilDestroyed(this.d))
      .subscribe(() => {
        const pagos = new Set(this.pagosDisponibles().map((pago) => pago.id));
        const ordenes = new Set(this.ordenesDisponibles().map((orden) => orden.id));
        if (!pagos.has(this.form.controls.pagoId.value ?? -1)) {
          this.form.controls.pagoId.setValue(null);
        }
        if (!ordenes.has(this.form.controls.ordenTrabajoId.value ?? -1)) {
          this.form.controls.ordenTrabajoId.setValue(null);
        }
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
          this.error.set('No fue posible consultar los reclamos.');
          return;
        }
        this.items.set(x.r.items);
        this.total.set(x.r.total);
      });
    this.busqueda.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.d))
      .subscribe(() => {
        this.pagina.set(1);
        this.url();
        this.cargar();
      });
    this.cargar();
  }
  cargar() {
    this.cargar$.next();
  }
  limpiar() {
    this.busqueda.setValue('');
    queueMicrotask(() => this.searchInput?.nativeElement.focus());
  }
  abrir() {
    this.enviado.set(false);
    this.editorError.set('');
    this.form.reset({
      numeroSuministro: '',
      tipo: 'PAGO_NO_REFLEJADO',
      prioridad: 'NORMAL',
      descripcion: '',
      pagoId: null,
      ordenTrabajoId: null,
      correoAsignado: '',
    });
    this.editor.set(true);
  }
  crear() {
    this.enviado.set(true);
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      this.enfocarPrimerInvalido('.drawer form');
      return;
    }
    const v = this.form.getRawValue();
    const dto: CrearReclamo = {
      ...v,
      numeroSuministro: v.numeroSuministro.trim().toUpperCase(),
      descripcion: v.descripcion.trim(),
      pagoId: v.pagoId || null,
      ordenTrabajoId: v.ordenTrabajoId || null,
      correoAsignado: v.correoAsignado.trim().toLowerCase() || null,
    };
    this.guardando.set(true);
    this.s
      .crear(dto)
      .pipe(
        finalize(() => this.guardando.set(false)),
        takeUntilDestroyed(this.d),
      )
      .subscribe({
        next: (x) => {
          this.editor.set(false);
          this.detalle.set(x);
          this.exito.set('Reclamo registrado con número ' + x.numeroCaso + '.');
          this.cargar();
        },
        error: (e) => this.editorError.set(this.mensaje(e, 'No se pudo crear el reclamo.')),
      });
  }
  ver(x: ReclamoResumen) {
    this.s
      .obtener(x.id)
      .pipe(takeUntilDestroyed(this.d))
      .subscribe({
        next: (r) => this.detalle.set(r),
        error: () => this.error.set('No se pudo abrir el reclamo.'),
      });
  }
  abrirGestion() {
    this.gestionError.set('');
    this.gestionForm.reset({
      estadoNuevo: '',
      motivo: '',
      resolucion: this.detalle()?.resolucion ?? '',
      correoAsignado: '',
    });
    this.gestion.set(true);
  }
  guardarGestion() {
    const r = this.detalle();
    this.gestionForm.markAllAsTouched();
    if (!r || this.gestionForm.invalid) {
      this.enfocarPrimerInvalido('.management');
      return;
    }
    const v = this.gestionForm.getRawValue();
    if ((v.estadoNuevo === 'RESUELTO' || v.estadoNuevo === 'CERRADO') && !v.resolucion.trim()) {
      this.gestionError.set('Escribe la resolución antes de resolver o cerrar el reclamo.');
      setTimeout(() => document.querySelector<HTMLElement>('#claim-resolution')?.focus());
      return;
    }
    this.guardando.set(true);
    this.s
      .gestionar(
        r.id,
        v.estadoNuevo,
        v.motivo.trim(),
        v.resolucion.trim(),
        v.correoAsignado.trim().toLowerCase(),
      )
      .pipe(
        finalize(() => this.guardando.set(false)),
        takeUntilDestroyed(this.d),
      )
      .subscribe({
        next: (x) => {
          this.detalle.set(x);
          this.gestion.set(false);
          this.exito.set('Reclamo actualizado.');
          this.cargar();
        },
        error: (e) => this.gestionError.set(this.mensaje(e, 'No se pudo actualizar el reclamo.')),
      });
  }
  opciones(e: EstadoReclamo) {
    return (
      {
        ABIERTO: ['EN_REVISION', 'CANCELADO'],
        EN_REVISION: ['RESUELTO', 'CANCELADO'],
        RESUELTO: ['CERRADO', 'EN_REVISION'],
        CERRADO: [],
        CANCELADO: [],
      } as Record<EstadoReclamo, string[]>
    )[e];
  }
  pagosDisponibles(): PagoCatalogo[] {
    const suministro = this.form.controls.numeroSuministro.value;
    return (this.catalogos()?.pagos ?? []).filter(
      (pago) => pago.numeroSuministro === suministro,
    );
  }
  ordenesDisponibles(): OrdenCatalogo[] {
    const suministro = this.form.controls.numeroSuministro.value;
    return (this.catalogos()?.ordenes ?? []).filter(
      (orden) => orden.numeroSuministro === suministro,
    );
  }
  fecha(x: string | null) {
    return x ? new Date(x).toLocaleString('es-EC', { timeZone: 'America/Guayaquil' }) : '—';
  }
  anterior() {
    if (this.pagina() > 1) {
      this.pagina.update((x) => x - 1);
      this.url();
      this.cargar();
    }
  }
  siguiente() {
    if (this.pagina() * 10 < this.total()) {
      this.pagina.update((x) => x + 1);
      this.url();
      this.cargar();
    }
  }
  invalido(n: keyof typeof this.form.controls) {
    const c = this.form.controls[n];
    return c.invalid && (c.touched || this.enviado());
  }
  private enfocarPrimerInvalido(contenedor: string) {
    setTimeout(() =>
      document
        .querySelector<HTMLElement>(
          `${contenedor} [aria-invalid="true"], ${contenedor} .ng-invalid:not(form)`,
        )
        ?.focus(),
    );
  }
  private url() {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        q: this.busqueda.value.trim() || null,
        pagina: this.pagina() > 1 ? this.pagina() : null,
      },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }
  private paginaInicial() {
    const p = Number(this.route.snapshot.queryParamMap.get('pagina'));
    return Number.isInteger(p) && p > 0 ? p : 1;
  }
  private mensaje(e: any, f: string) {
    const m = Object.values(e?.error?.errors ?? {})
      .flat()
      .join(' ');
    return e?.error?.mensaje || m || f;
  }
}
