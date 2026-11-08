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
import {
  CuadrillaDetalle,
  CuadrillaResumen,
  EstadoCuadrilla,
  GuardarCuadrilla,
  MiembroCuadrilla,
} from './cuadrillas.model';
import { CuadrillasService } from './cuadrillas.service';
import { PersonaCatalogo } from '../../../core/models/catalogos.model';
import { CatalogosService } from '../../../core/services/catalogos';

@Component({
  selector: 'app-admin-cuadrillas',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, Icon],
  templateUrl: './cuadrillas.html',
  styleUrl: './cuadrillas.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Cuadrillas {
  private readonly service = inject(CuadrillasService);
  private readonly destroy = inject(DestroyRef);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly catalogosService = inject(CatalogosService);
  private readonly cargar$ = new Subject<void>();
  readonly items = signal<CuadrillaResumen[]>([]);
  readonly total = signal(0);
  readonly pagina = signal(this.paginaInicial());
  readonly cargando = signal(true);
  readonly guardando = signal(false);
  readonly error = signal('');
  readonly exito = signal('');
  readonly editor = signal(false);
  readonly editorError = signal('');
  readonly detalle = signal<CuadrillaDetalle | null>(null);
  readonly editando = signal<CuadrillaDetalle | null>(null);
  readonly enviado = signal(false);
  readonly miembroError = signal('');
  readonly confirmacion = signal<MiembroCuadrilla | null>(null);
  readonly tecnicos = signal<PersonaCatalogo[]>([]);
  readonly busqueda = new FormControl(this.route.snapshot.queryParamMap.get('q') ?? '', {
    nonNullable: true,
  });
  readonly form = new FormGroup({
    codigo: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(/^[A-Za-z0-9-]{3,30}$/)],
    }),
    nombre: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(3), Validators.maxLength(120)],
    }),
    zona: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(2), Validators.maxLength(120)],
    }),
    capacidadMaxima: new FormControl(1, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(1), Validators.max(20)],
    }),
    estado: new FormControl<EstadoCuadrilla>('DISPONIBLE', { nonNullable: true }),
    observaciones: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(500)],
    }),
    motivo: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(5), Validators.maxLength(300)],
    }),
  });
  readonly miembroForm = new FormGroup({
    correoTecnico: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email],
    }),
    motivo: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(5)],
    }),
  });
  @ViewChild('searchInput') searchInput?: ElementRef<HTMLInputElement>;
  constructor() {
    this.catalogosService.asignacionOperativa()
      .pipe(takeUntilDestroyed(this.destroy))
      .subscribe({ next: (datos) => this.tecnicos.set(datos.tecnicos) });
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
          this.error.set('No fue posible consultar las cuadrillas.');
          return;
        }
        this.items.set(x.r.items);
        this.total.set(x.r.total);
      });
    this.busqueda.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroy))
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
  abrir(item?: CuadrillaDetalle) {
    this.editorError.set('');
    this.editando.set(item ?? null);
    this.enviado.set(false);
    this.error.set('');
    this.form.reset(
      item
        ? {
            codigo: item.codigo,
            nombre: item.nombre,
            zona: item.zona,
            capacidadMaxima: item.capacidadMaxima,
            estado: item.estado,
            observaciones: item.observaciones ?? '',
            motivo: '',
          }
        : {
            codigo: '',
            nombre: '',
            zona: '',
            capacidadMaxima: 1,
            estado: 'DISPONIBLE',
            observaciones: '',
            motivo: '',
          },
    );
    this.editor.set(true);
  }
  guardar() {
    this.enviado.set(true);
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      this.enfocarPrimerInvalido('.drawer form');
      return;
    }
    const v = this.form.getRawValue();
    const dto: GuardarCuadrilla = {
      ...v,
      codigo: v.codigo.trim().toUpperCase(),
      nombre: v.nombre.trim(),
      zona: v.zona.trim(),
      observaciones: v.observaciones.trim() || null,
      motivo: v.motivo.trim(),
    };
    this.guardando.set(true);
    const req = this.editando()
      ? this.service.actualizar(this.editando()!.id, dto)
      : this.service.crear(dto);
    req
      .pipe(
        finalize(() => this.guardando.set(false)),
        takeUntilDestroyed(this.destroy),
      )
      .subscribe({
        next: (d) => {
          this.editor.set(false);
          this.exito.set(this.editando() ? 'Cuadrilla actualizada.' : 'Cuadrilla creada.');
          this.detalle.set(d);
          this.cargar();
        },
        error: (e) =>
          this.editorError.set(this.mensajeError(e, 'No se pudo guardar la cuadrilla.')),
      });
  }
  ver(item: CuadrillaResumen) {
    this.service
      .obtener(item.id)
      .pipe(takeUntilDestroyed(this.destroy))
      .subscribe({
        next: (d) => this.detalle.set(d),
        error: () => this.error.set('No fue posible abrir la cuadrilla.'),
      });
  }
  editarDetalle() {
    const d = this.detalle();
    if (d) {
      this.detalle.set(null);
      this.abrir(d);
    }
  }
  cambiarEstado(estado: EstadoCuadrilla) {
    const d = this.detalle();
    if (!d) return;
    const motivo =
      estado === 'INACTIVA'
        ? 'Desactivación administrativa de la cuadrilla.'
        : 'Reactivación administrativa de la cuadrilla.';
    this.guardando.set(true);
    this.service
      .cambiarEstado(d.id, estado, motivo)
      .pipe(
        finalize(() => this.guardando.set(false)),
        takeUntilDestroyed(this.destroy),
      )
      .subscribe({
        next: (x) => {
          this.detalle.set(x);
          this.exito.set(estado === 'INACTIVA' ? 'Cuadrilla desactivada.' : 'Cuadrilla activada.');
          this.cargar();
        },
        error: (e) => this.error.set(this.mensajeError(e, 'No se pudo cambiar el estado.')),
      });
  }
  agregarMiembro() {
    const d = this.detalle();
    this.miembroForm.markAllAsTouched();
    if (!d || this.miembroForm.invalid) {
      this.enfocarPrimerInvalido('.member-form');
      return;
    }
    const v = this.miembroForm.getRawValue();
    this.guardando.set(true);
    this.miembroError.set('');
    this.service
      .agregarMiembro(d.id, v.correoTecnico.trim().toLowerCase(), v.motivo.trim())
      .pipe(
        finalize(() => this.guardando.set(false)),
        takeUntilDestroyed(this.destroy),
      )
      .subscribe({
        next: (x) => {
          this.detalle.set(x);
          this.miembroForm.reset({ correoTecnico: '', motivo: '' });
          this.exito.set('Técnico agregado a la cuadrilla.');
          this.cargar();
        },
        error: (e) => this.miembroError.set(this.mensajeError(e, 'No se pudo agregar el técnico.')),
      });
  }
  retirar() {
    const d = this.detalle(),
      m = this.confirmacion();
    if (!d || !m) return;
    this.guardando.set(true);
    this.service
      .retirarMiembro(
        d.id,
        m.id,
        'Retiro administrativo solicitado desde la gestión de cuadrillas.',
      )
      .pipe(
        finalize(() => this.guardando.set(false)),
        takeUntilDestroyed(this.destroy),
      )
      .subscribe({
        next: (x) => {
          this.detalle.set(x);
          this.confirmacion.set(null);
          this.exito.set('Técnico retirado de la cuadrilla.');
          this.cargar();
        },
        error: (e) => {
          this.confirmacion.set(null);
          this.error.set(this.mensajeError(e, 'No se pudo retirar el técnico.'));
        },
      });
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
  invalido(nombre: keyof typeof this.form.controls) {
    const c = this.form.controls[nombre];
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
  private mensajeError(e: any, fallback: string) {
    const campos = Object.values(e?.error?.errors ?? {})
      .flat()
      .join(' ');
    return e?.error?.mensaje || campos || fallback;
  }
}
