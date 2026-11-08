import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  HostListener,
  ViewChild,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
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
import { CatalogosAsignacion } from '../../../core/models/catalogos.model';
import { CatalogosService } from '../../../core/services/catalogos';
import {
  ActualizarMedidor,
  CrearMedidor,
  EstadoMedidor,
  MedidorResumen,
  TipoMedicion,
} from './medidores.model';
import { MedidoresService } from './medidores.service';

type EstadoCarga = 'cargando' | 'listo' | 'error';
type CampoMedidor =
  'numeroSerie' | 'numeroSuministro' | 'tipoMedicion' | 'fechaInstalacion' | 'motivo';
type Confirmacion =
  { readonly tipo: 'estado'; readonly medidor: MedidorResumen } | { readonly tipo: 'descartar' };

@Component({
  selector: 'app-admin-medidores',
  standalone: true,
  imports: [DatePipe, ReactiveFormsModule, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './medidores.html',
  styleUrls: [
    '../usuarios/usuarios.css',
    '../clientes/clientes.css',
    '../suministros/suministros.css',
    './medidores.css',
  ],
})
export class Medidores {
  private readonly service = inject(MedidoresService);
  private readonly catalogosService = inject(CatalogosService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly solicitudes = new Subject<{
    buscar: string;
    pagina: number;
    tamanoPagina: number;
  }>();
  private disparadorEditor?: HTMLElement;
  private disparadorConfirmacion?: HTMLElement;

  readonly tamanoPagina = 10;
  readonly fechaMaxima = this.fechaLocalActual();
  readonly medidores = signal<readonly MedidorResumen[]>([]);
  readonly total = signal(0);
  readonly pagina = signal(this.obtenerPaginaInicial());
  readonly estado = signal<EstadoCarga>('cargando');
  readonly mensajeError = signal('');
  readonly mensajeExito = signal('');
  readonly editorAbierto = signal(false);
  readonly cargandoDetalle = signal(false);
  readonly guardando = signal(false);
  readonly editandoId = signal<number | null>(null);
  readonly formularioEnviado = signal(false);
  readonly mensajeFormulario = signal('');
  readonly confirmacion = signal<Confirmacion | null>(null);
  readonly procesandoEstado = signal(false);
  readonly mensajeConfirmacion = signal('');
  readonly catalogos = signal<CatalogosAsignacion | null>(null);
  readonly catalogosError = signal('');

  readonly busqueda = new FormControl(this.route.snapshot.queryParamMap.get('q') ?? '', {
    nonNullable: true,
  });
  readonly formulario = new FormGroup({
    numeroSerie: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(/^[A-Za-z0-9-]{3,50}$/)],
    }),
    numeroSuministro: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(/^[A-Za-z0-9-]{3,30}$/)],
    }),
    tipoMedicion: new FormControl<TipoMedicion>('CONVENCIONAL', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    fechaInstalacion: new FormControl(this.fechaMaxima, {
      nonNullable: true,
      validators: [Validators.required],
    }),
    motivo: new FormControl('', { nonNullable: true }),
  });
  readonly estadoDestino = new FormControl<EstadoMedidor>('RETIRADO', {
    nonNullable: true,
    validators: [Validators.required],
  });
  readonly motivoEstado = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.minLength(5), Validators.maxLength(250)],
  });

  readonly totalPaginas = computed(() => Math.max(1, Math.ceil(this.total() / this.tamanoPagina)));
  readonly rangoInicio = computed(() =>
    this.total() ? (this.pagina() - 1) * this.tamanoPagina + 1 : 0,
  );
  readonly rangoFin = computed(() => Math.min(this.pagina() * this.tamanoPagina, this.total()));
  readonly editando = computed(() => this.editandoId() !== null);

  @ViewChild('searchInput') private searchInput?: ElementRef<HTMLInputElement>;
  @ViewChild('createButton') private createButton?: ElementRef<HTMLButtonElement>;
  @ViewChild('meterForm') private meterForm?: ElementRef<HTMLFormElement>;
  @ViewChild('editorHeading') private editorHeading?: ElementRef<HTMLHeadingElement>;
  @ViewChild('successMessage') private successMessage?: ElementRef<HTMLElement>;
  @ViewChild('confirmationDialog') private confirmationDialog?: ElementRef<HTMLElement>;
  @ViewChild('confirmationCancel') private confirmationCancel?: ElementRef<HTMLButtonElement>;

  constructor() {
    this.catalogosService
      .asignacionOperativa()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (datos) => this.catalogos.set(datos),
        error: () =>
          this.catalogosError.set('No se pudo cargar la lista de suministros activos.'),
      });

    this.solicitudes
      .pipe(
        switchMap((solicitud) => {
          this.estado.set('cargando');
          this.mensajeError.set('');
          return this.service.listar(solicitud).pipe(
            map((respuesta) => ({ respuesta, error: false as const })),
            catchError(() => of({ respuesta: null, error: true as const })),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((resultado) => {
        if (resultado.error || !resultado.respuesta) {
          this.estado.set('error');
          this.mensajeError.set(
            'No fue posible consultar los medidores. Verifica la API e inténtalo nuevamente.',
          );
          return;
        }
        const respuesta = resultado.respuesta;
        const ultima = Math.max(1, Math.ceil(respuesta.total / this.tamanoPagina));
        if (this.pagina() > ultima) {
          this.pagina.set(ultima);
          this.actualizarUrl();
          this.cargar();
          return;
        }
        this.medidores.set(respuesta.items);
        this.total.set(respuesta.total);
        this.estado.set('listo');
      });
    this.busqueda.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.pagina.set(1);
        this.actualizarUrl();
        this.cargar();
      });
    this.cargar();
    this.destroyRef.onDestroy(() => document.body.style.removeProperty('overflow'));
  }

  abrirCrear(event?: Event): void {
    this.disparadorEditor =
      (event?.currentTarget as HTMLElement | null) ?? this.createButton?.nativeElement;
    this.editandoId.set(null);
    this.formulario.reset({
      numeroSerie: '',
      numeroSuministro: '',
      tipoMedicion: 'CONVENCIONAL',
      fechaInstalacion: this.fechaMaxima,
      motivo: '',
    });
    this.configurarMotivo(false);
    this.prepararEditor();
  }

  abrirEditar(item: MedidorResumen, event: Event): void {
    this.disparadorEditor = event.currentTarget as HTMLElement;
    this.editandoId.set(item.id);
    this.configurarMotivo(true);
    this.formularioEnviado.set(false);
    this.mensajeFormulario.set('');
    this.mensajeExito.set('');
    this.cargandoDetalle.set(true);
    this.editorAbierto.set(true);
    this.service
      .obtener(item.id)
      .pipe(
        finalize(() => this.cargandoDetalle.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (detalle) => {
          this.formulario.reset({
            numeroSerie: detalle.numeroSerie,
            numeroSuministro: detalle.numeroSuministro,
            tipoMedicion: detalle.tipoMedicion,
            fechaInstalacion: detalle.fechaInstalacion.slice(0, 10),
            motivo: '',
          });
          this.formulario.markAsPristine();
          setTimeout(() => this.editorHeading?.nativeElement.focus(), 0);
        },
        error: () =>
          this.mensajeFormulario.set(
            'No fue posible cargar el medidor. Cierra el formulario e inténtalo nuevamente.',
          ),
      });
  }

  guardar(): void {
    if (this.guardando() || this.cargandoDetalle()) return;
    this.formularioEnviado.set(true);
    this.mensajeFormulario.set('');
    this.formulario.markAllAsTouched();
    if (this.formulario.invalid) {
      this.enfocarPrimerError();
      return;
    }
    const valor = this.formulario.getRawValue();
    const base: CrearMedidor = {
      numeroSerie: valor.numeroSerie.trim().toUpperCase(),
      numeroSuministro: valor.numeroSuministro.trim().toUpperCase(),
      tipoMedicion: valor.tipoMedicion,
      fechaInstalacion: valor.fechaInstalacion,
    };
    const id = this.editandoId();
    const operacion =
      id === null
        ? this.service.crear(base)
        : this.service.actualizar(id, {
            ...base,
            motivo: valor.motivo.trim(),
          } satisfies ActualizarMedidor);
    this.guardando.set(true);
    operacion
      .pipe(
        finalize(() => this.guardando.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.formulario.markAsPristine();
          this.cerrarEditor(false);
          this.mensajeExito.set(
            id === null
              ? 'Medidor registrado correctamente.'
              : 'Medidor actualizado correctamente.',
          );
          this.cargar();
          setTimeout(() => this.successMessage?.nativeElement.focus(), 0);
        },
        error: (error: HttpErrorResponse) => this.procesarError(error),
      });
  }

  solicitarCerrarEditor(): void {
    if (this.guardando()) return;
    if (this.formulario.dirty) {
      this.abrirConfirmacion({ tipo: 'descartar' }, this.editorHeading?.nativeElement);
      return;
    }
    this.cerrarEditor();
  }

  solicitarCambioEstado(item: MedidorResumen, event: Event): void {
    this.estadoDestino.reset(item.estado === 'ACTIVO' ? 'RETIRADO' : 'ACTIVO');
    this.motivoEstado.reset('');
    this.mensajeConfirmacion.set('');
    this.abrirConfirmacion({ tipo: 'estado', medidor: item }, event.currentTarget as HTMLElement);
  }

  confirmarAccion(): void {
    const actual = this.confirmacion();
    if (!actual || this.procesandoEstado()) return;
    if (actual.tipo === 'descartar') {
      this.formulario.markAsPristine();
      this.cerrarConfirmacion(false);
      this.cerrarEditor();
      return;
    }
    this.estadoDestino.markAsTouched();
    this.motivoEstado.markAsTouched();
    if (this.estadoDestino.invalid || this.motivoEstado.invalid) return;
    this.procesandoEstado.set(true);
    this.service
      .cambiarEstado(actual.medidor.id, {
        estado: this.estadoDestino.value,
        motivo: this.motivoEstado.value.trim(),
      })
      .pipe(
        finalize(() => this.procesandoEstado.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.cerrarConfirmacion(false, true);
          this.mensajeExito.set(
            `Medidor actualizado al estado ${this.etiquetaEstado(this.estadoDestino.value).toLowerCase()}.`,
          );
          this.cargar();
          setTimeout(() => this.successMessage?.nativeElement.focus(), 0);
        },
        error: (error: HttpErrorResponse) =>
          this.mensajeConfirmacion.set(
            error.error?.mensaje ??
              'No fue posible cambiar el estado. Conservamos los datos para reintentar.',
          ),
      });
  }

  cerrarConfirmacion(restaurarFoco = true, forzar = false): void {
    if (this.procesandoEstado() && !forzar) return;
    this.confirmacion.set(null);
    this.mensajeConfirmacion.set('');
    document.body.style.removeProperty('overflow');
    if (restaurarFoco) setTimeout(() => this.disparadorConfirmacion?.focus(), 0);
  }

  etiquetaTipo(tipo: TipoMedicion): string {
    return tipo === 'INTELIGENTE'
      ? 'Inteligente'
      : tipo === 'CONVENCIONAL'
        ? 'Convencional'
        : 'Otro';
  }
  etiquetaEstado(estado: EstadoMedidor): string {
    return {
      ACTIVO: 'Activo',
      RETIRADO: 'Retirado',
      DEFECTUOSO: 'Defectuoso',
      REEMPLAZADO: 'Reemplazado',
    }[estado];
  }
  limpiarBusqueda(): void {
    this.busqueda.setValue('');
    setTimeout(() => this.searchInput?.nativeElement.focus(), 0);
  }
  ocultarMensajeExito(): void {
    this.mensajeExito.set('');
  }
  reintentar(): void {
    this.cargar();
  }
  cambiarPagina(nueva: number): void {
    if (nueva < 1 || nueva > this.totalPaginas() || nueva === this.pagina()) return;
    this.pagina.set(nueva);
    this.actualizarUrl();
    this.cargar();
  }
  mostrarError(campo: CampoMedidor): boolean {
    const control = this.formulario.controls[campo];
    return control.invalid && (control.touched || this.formularioEnviado());
  }
  errorCampo(campo: CampoMedidor): string {
    const errores = this.formulario.controls[campo].errors;
    if (!errores) return '';
    if (errores['required']) return 'Este campo es obligatorio.';
    if (errores['minlength']) return 'Escribe al menos 5 caracteres.';
    if (errores['maxlength']) return 'El valor supera la longitud permitida.';
    if (errores['duplicado'] || errores['servidor'])
      return String(errores['duplicado'] ?? errores['servidor']);
    if (errores['pattern']) return 'Usa únicamente letras, números y guiones.';
    return 'Revisa este valor.';
  }

  @HostListener('window:beforeunload', ['$event'])
  prevenirSalida(event: BeforeUnloadEvent): void {
    if (this.editorAbierto() && this.formulario.dirty) {
      event.preventDefault();
      event.returnValue = '';
    }
  }

  @HostListener('document:keydown', ['$event'])
  gestionarTeclado(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      if (this.confirmacion() && !this.procesandoEstado()) {
        event.preventDefault();
        this.cerrarConfirmacion();
      } else if (this.editorAbierto() && !this.guardando()) {
        event.preventDefault();
        this.solicitarCerrarEditor();
      }
      return;
    }
    if (event.key !== 'Tab' || !this.confirmacion() || !this.confirmationDialog) return;
    const controles = Array.from(
      this.confirmationDialog.nativeElement.querySelectorAll<HTMLElement>(
        'button:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])',
      ),
    );
    if (!controles.length) return;
    const primero = controles[0];
    const ultimo = controles[controles.length - 1];
    if (event.shiftKey && document.activeElement === primero) {
      event.preventDefault();
      ultimo.focus();
    } else if (!event.shiftKey && document.activeElement === ultimo) {
      event.preventDefault();
      primero.focus();
    }
  }

  private prepararEditor(): void {
    this.formulario.markAsPristine();
    this.formularioEnviado.set(false);
    this.mensajeFormulario.set('');
    this.mensajeExito.set('');
    this.cargandoDetalle.set(false);
    this.editorAbierto.set(true);
    setTimeout(() => this.editorHeading?.nativeElement.focus(), 0);
  }
  private configurarMotivo(editando: boolean): void {
    const control = this.formulario.controls.motivo;
    control.setValidators(
      editando ? [Validators.required, Validators.minLength(5), Validators.maxLength(250)] : [],
    );
    control.updateValueAndValidity({ emitEvent: false });
  }
  private cerrarEditor(restaurarFoco = true): void {
    this.editorAbierto.set(false);
    this.cargandoDetalle.set(false);
    this.editandoId.set(null);
    this.formularioEnviado.set(false);
    this.mensajeFormulario.set('');
    if (restaurarFoco)
      setTimeout(() => (this.disparadorEditor ?? this.createButton?.nativeElement)?.focus(), 0);
  }
  private abrirConfirmacion(valor: Confirmacion, retorno?: HTMLElement): void {
    this.disparadorConfirmacion = retorno;
    this.confirmacion.set(valor);
    document.body.style.overflow = 'hidden';
    setTimeout(() => this.confirmationCancel?.nativeElement.focus(), 0);
  }

  private procesarError(error: HttpErrorResponse): void {
    if (error.status === 409 && error.error?.codigo === 'SERIE_DUPLICADA') {
      this.formulario.controls.numeroSerie.setErrors({ duplicado: error.error.mensaje });
      this.formulario.controls.numeroSerie.markAsTouched();
      this.mensajeFormulario.set('Revisa la serie duplicada antes de guardar.');
      this.enfocarPrimerError();
      return;
    }
    const errores = error.error?.errors as Record<string, string[]> | undefined;
    if (errores) {
      const mapa: Record<string, CampoMedidor> = {
        NumeroSerie: 'numeroSerie',
        NumeroSuministro: 'numeroSuministro',
        TipoMedicion: 'tipoMedicion',
        FechaInstalacion: 'fechaInstalacion',
        Motivo: 'motivo',
      };
      Object.entries(errores).forEach(([clave, mensajes]) => {
        const campo = mapa[clave];
        if (campo && mensajes.length) {
          this.formulario.controls[campo].setErrors({ servidor: mensajes[0] });
          this.formulario.controls[campo].markAsTouched();
        }
      });
      this.mensajeFormulario.set('Corrige los campos señalados y vuelve a guardar.');
      this.enfocarPrimerError();
      return;
    }
    this.mensajeFormulario.set(
      error.error?.mensaje ??
        'No fue posible guardar el medidor. Conservamos la información para reintentar.',
    );
    setTimeout(() => this.editorHeading?.nativeElement.focus(), 0);
  }

  private enfocarPrimerError(): void {
    setTimeout(
      () =>
        this.meterForm?.nativeElement.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus(),
      0,
    );
  }
  private fechaLocalActual(): string {
    const fecha = new Date();
    const mes = String(fecha.getMonth() + 1).padStart(2, '0');
    const dia = String(fecha.getDate()).padStart(2, '0');
    return `${fecha.getFullYear()}-${mes}-${dia}`;
  }
  private cargar(): void {
    this.solicitudes.next({
      buscar: this.busqueda.value.trim(),
      pagina: this.pagina(),
      tamanoPagina: this.tamanoPagina,
    });
  }
  private actualizarUrl(): void {
    const termino = this.busqueda.value.trim();
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { q: termino || null, pagina: this.pagina() > 1 ? this.pagina() : null },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }
  private obtenerPaginaInicial(): number {
    const valor = Number(this.route.snapshot.queryParamMap.get('pagina'));
    return Number.isInteger(valor) && valor > 0 ? valor : 1;
  }
}
