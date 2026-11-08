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
import { ActualizarSuministro, CrearSuministro, SuministroResumen } from './suministros.model';
import { SuministrosService } from './suministros.service';

type EstadoCarga = 'cargando' | 'listo' | 'error';
type CampoSuministro =
  | 'numeroSuministro'
  | 'numeroContrato'
  | 'cuen'
  | 'clienteIdentificacion'
  | 'direccionServicio'
  | 'provincia'
  | 'canton'
  | 'parroquia'
  | 'planGeocodigo'
  | 'codigoTarifa'
  | 'nombreTarifa'
  | 'motivo';

interface SolicitudSuministros {
  readonly buscar: string;
  readonly pagina: number;
  readonly tamanoPagina: number;
}

type Confirmacion =
  | { readonly tipo: 'estado'; readonly suministro: SuministroResumen; readonly activar: boolean }
  | { readonly tipo: 'descartar' };

@Component({
  selector: 'app-admin-suministros',
  standalone: true,
  imports: [ReactiveFormsModule, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './suministros.html',
  styleUrls: ['../usuarios/usuarios.css', '../clientes/clientes.css', './suministros.css'],
})
export class Suministros {
  private readonly service = inject(SuministrosService);
  private readonly catalogosService = inject(CatalogosService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly solicitudes = new Subject<SolicitudSuministros>();
  private disparadorEditor?: HTMLElement;
  private disparadorConfirmacion?: HTMLElement;

  readonly tamanoPagina = 10;
  readonly suministros = signal<readonly SuministroResumen[]>([]);
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
    numeroSuministro: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(/^[A-Za-z0-9-]{3,30}$/)],
    }),
    numeroContrato: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(/^[A-Za-z0-9-]{3,30}$/)],
    }),
    cuen: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(/^[A-Za-z0-9-]{5,50}$/)],
    }),
    clienteIdentificacion: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(5), Validators.maxLength(20)],
    }),
    direccionServicio: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(5), Validators.maxLength(300)],
    }),
    provincia: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(80)] }),
    canton: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(80)] }),
    parroquia: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(80)] }),
    planGeocodigo: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(50)],
    }),
    codigoTarifa: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(40)],
    }),
    nombreTarifa: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(100)],
    }),
    motivo: new FormControl('', { nonNullable: true }),
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
  @ViewChild('supplyForm') private supplyForm?: ElementRef<HTMLFormElement>;
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
          this.catalogosError.set('No se pudo cargar la lista de clientes activos.'),
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
            'No fue posible consultar los suministros. Verifica la API e inténtalo nuevamente.',
          );
          return;
        }
        const respuesta = resultado.respuesta;
        const ultimaPagina = Math.max(1, Math.ceil(respuesta.total / this.tamanoPagina));
        if (this.pagina() > ultimaPagina) {
          this.pagina.set(ultimaPagina);
          this.actualizarUrl();
          this.cargar();
          return;
        }
        this.suministros.set(respuesta.items);
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
    this.formulario.reset(this.valoresVacios());
    this.configurarMotivo(false);
    this.prepararEditor();
  }

  abrirEditar(item: SuministroResumen, event: Event): void {
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
            numeroSuministro: detalle.numeroSuministro,
            numeroContrato: detalle.numeroContrato,
            cuen: detalle.cuen,
            clienteIdentificacion: detalle.clienteIdentificacion,
            direccionServicio: detalle.direccionServicio,
            provincia: detalle.provincia ?? '',
            canton: detalle.canton ?? '',
            parroquia: detalle.parroquia ?? '',
            planGeocodigo: detalle.planGeocodigo ?? '',
            codigoTarifa: detalle.codigoTarifa ?? '',
            nombreTarifa: detalle.nombreTarifa ?? '',
            motivo: '',
          });
          this.formulario.markAsPristine();
          setTimeout(() => this.editorHeading?.nativeElement.focus(), 0);
        },
        error: () =>
          this.mensajeFormulario.set(
            'No fue posible cargar el suministro. Cierra el formulario e inténtalo nuevamente.',
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
    const base: CrearSuministro = {
      numeroSuministro: valor.numeroSuministro.trim().toUpperCase(),
      numeroContrato: valor.numeroContrato.trim().toUpperCase(),
      cuen: valor.cuen.trim().toUpperCase(),
      clienteIdentificacion: valor.clienteIdentificacion.trim().toUpperCase(),
      direccionServicio: valor.direccionServicio.trim(),
      provincia: this.opcional(valor.provincia),
      canton: this.opcional(valor.canton),
      parroquia: this.opcional(valor.parroquia),
      planGeocodigo: this.opcional(valor.planGeocodigo),
      codigoTarifa: this.opcional(valor.codigoTarifa)?.toUpperCase() ?? null,
      nombreTarifa: this.opcional(valor.nombreTarifa),
    };
    const id = this.editandoId();
    const operacion =
      id === null
        ? this.service.crear(base)
        : this.service.actualizar(id, {
            ...base,
            motivo: valor.motivo.trim(),
          } satisfies ActualizarSuministro);
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
              ? 'Suministro creado correctamente.'
              : 'Suministro actualizado correctamente.',
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

  solicitarCambioEstado(item: SuministroResumen, event: Event): void {
    this.motivoEstado.reset('');
    this.mensajeConfirmacion.set('');
    this.abrirConfirmacion(
      { tipo: 'estado', suministro: item, activar: !this.esActivo(item.estadoServicio) },
      event.currentTarget as HTMLElement,
    );
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
    this.motivoEstado.markAsTouched();
    if (this.motivoEstado.invalid) return;
    this.procesandoEstado.set(true);
    this.service
      .cambiarEstado(actual.suministro.id, {
        activo: actual.activar,
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
            actual.activar
              ? 'Suministro reactivado correctamente.'
              : 'Suministro desactivado sin borrar su historial.',
          );
          this.cargar();
          setTimeout(() => this.successMessage?.nativeElement.focus(), 0);
        },
        error: (error: HttpErrorResponse) =>
          this.mensajeConfirmacion.set(
            error.error?.mensaje ??
              'No fue posible cambiar el estado. Conservamos el motivo para reintentar.',
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
  esActivo(estado: string): boolean {
    return estado.toUpperCase() === 'ACTIVO';
  }
  ubicacion(item: SuministroResumen): string {
    return [item.parroquia, item.canton, item.provincia].filter(Boolean).join(' · ');
  }
  cambiarPagina(nueva: number): void {
    if (nueva < 1 || nueva > this.totalPaginas() || nueva === this.pagina()) return;
    this.pagina.set(nueva);
    this.actualizarUrl();
    this.cargar();
  }

  mostrarError(campo: CampoSuministro): boolean {
    const control = this.formulario.controls[campo];
    return control.invalid && (control.touched || this.formularioEnviado());
  }

  errorCampo(campo: CampoSuministro): string {
    const errores = this.formulario.controls[campo].errors;
    if (!errores) return '';
    if (errores['required']) return 'Este campo es obligatorio.';
    if (errores['minlength']) return 'Escribe un valor más completo.';
    if (errores['maxlength']) return 'El valor supera la longitud permitida.';
    if (errores['duplicado']) return String(errores['duplicado']);
    if (errores['servidor']) return String(errores['servidor']);
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
        'button:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])',
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
    const duplicados: Record<string, CampoSuministro> = {
      SUMINISTRO_DUPLICADO: 'numeroSuministro',
      CONTRATO_DUPLICADO: 'numeroContrato',
      CUEN_DUPLICADO: 'cuen',
    };
    const campoDuplicado = duplicados[error.error?.codigo];
    if (campoDuplicado) {
      this.formulario.controls[campoDuplicado].setErrors({ duplicado: error.error.mensaje });
      this.formulario.controls[campoDuplicado].markAsTouched();
      this.mensajeFormulario.set('Revisa el identificador duplicado antes de guardar.');
      this.enfocarPrimerError();
      return;
    }
    const errores = error.error?.errors as Record<string, string[]> | undefined;
    if (errores) {
      const mapa: Record<string, CampoSuministro> = {
        NumeroSuministro: 'numeroSuministro',
        NumeroContrato: 'numeroContrato',
        Cuen: 'cuen',
        ClienteIdentificacion: 'clienteIdentificacion',
        DireccionServicio: 'direccionServicio',
        Provincia: 'provincia',
        Canton: 'canton',
        Parroquia: 'parroquia',
        PlanGeocodigo: 'planGeocodigo',
        CodigoTarifa: 'codigoTarifa',
        NombreTarifa: 'nombreTarifa',
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
        'No fue posible guardar el suministro. Conservamos la información para reintentar.',
    );
    setTimeout(() => this.editorHeading?.nativeElement.focus(), 0);
  }

  private enfocarPrimerError(): void {
    setTimeout(
      () =>
        this.supplyForm?.nativeElement.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus(),
      0,
    );
  }
  private opcional(valor: string): string | null {
    const limpio = valor.trim();
    return limpio || null;
  }
  private valoresVacios() {
    return {
      numeroSuministro: '',
      numeroContrato: '',
      cuen: '',
      clienteIdentificacion: '',
      direccionServicio: '',
      provincia: '',
      canton: '',
      parroquia: '',
      planGeocodigo: '',
      codigoTarifa: '',
      nombreTarifa: '',
      motivo: '',
    };
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
