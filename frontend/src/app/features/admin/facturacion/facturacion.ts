import { CurrencyPipe, DatePipe } from '@angular/common';
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
import {
  FormArray,
  FormControl,
  FormGroup,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
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
import { CatalogosAsignacion, MedidorCatalogo } from '../../../core/models/catalogos.model';
import { CatalogosService } from '../../../core/services/catalogos';
import {
  CategoriaFactura,
  EstadoFactura,
  FacturaDetalle,
  FacturaResumen,
  ImportarFactura,
  MetodoLectura,
} from './facturacion.model';
import { FacturacionService } from './facturacion.service';

type EstadoCarga = 'cargando' | 'listo' | 'error';

type LineaForm = FormGroup<{
  codigoOriginal: FormControl<string>;
  descripcionOriginal: FormControl<string>;
  categoria: FormControl<CategoriaFactura>;
  monto: FormControl<number>;
  esIngresoElectrico: FormControl<boolean>;
}>;

@Component({
  selector: 'app-admin-facturacion',
  standalone: true,
  imports: [CurrencyPipe, DatePipe, ReactiveFormsModule, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './facturacion.html',
  styleUrls: [
    '../usuarios/usuarios.css',
    '../clientes/clientes.css',
    '../suministros/suministros.css',
    '../medidores/medidores.css',
    './facturacion.css',
  ],
})
export class Facturacion {
  private readonly service = inject(FacturacionService);
  private readonly catalogosService = inject(CatalogosService);
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly solicitudes = new Subject<{
    buscar: string;
    pagina: number;
    tamanoPagina: number;
  }>();
  private disparadorEditor?: HTMLElement;
  private disparadorDetalle?: HTMLElement;

  readonly tamanoPagina = 10;
  readonly fechaMaxima = this.fechaLocalActual();
  readonly facturas = signal<readonly FacturaResumen[]>([]);
  readonly total = signal(0);
  readonly pagina = signal(this.obtenerPaginaInicial());
  readonly estado = signal<EstadoCarga>('cargando');
  readonly mensajeError = signal('');
  readonly mensajeExito = signal('');
  readonly editorAbierto = signal(false);
  readonly detalle = signal<FacturaDetalle | null>(null);
  readonly cargandoDetalle = signal(false);
  readonly guardando = signal(false);
  readonly formularioEnviado = signal(false);
  readonly mensajeFormulario = signal('');
  readonly confirmarDescarte = signal(false);
  readonly totalRubros = signal(0);
  readonly catalogos = signal<CatalogosAsignacion | null>(null);
  readonly catalogosError = signal('');

  readonly busqueda = new FormControl(this.route.snapshot.queryParamMap.get('q') ?? '', {
    nonNullable: true,
  });

  readonly formulario = this.fb.group({
    numeroSuministro: ['', [Validators.required, Validators.pattern(/^[A-Za-z0-9-]{3,30}$/)]],
    numeroMedidor: ['', [Validators.pattern(/^[A-Za-z0-9-]{0,50}$/)]],
    sistemaOrigen: [
      'SISTEMA_COMERCIAL',
      [Validators.required, Validators.minLength(2), Validators.maxLength(80)],
    ],
    identificadorExterno: [
      '',
      [Validators.required, Validators.minLength(3), Validators.maxLength(100)],
    ],
    numeroFactura: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(60)]],
    nombreEmisor: [
      'Empresa eléctrica',
      [Validators.required, Validators.minLength(3), Validators.maxLength(160)],
    ],
    rucEmisor: ['', [Validators.required, Validators.pattern(/^\d{13}$/)]],
    fechaEmision: [this.fechaMaxima, Validators.required],
    fechaVencimiento: [this.fechaMaxima, Validators.required],
    estado: this.fb.control<EstadoFactura>('EMITIDA', Validators.required),
    periodoDesde: [this.inicioMesAnterior(), Validators.required],
    periodoHasta: [this.finMesAnterior(), Validators.required],
    metodoLectura: this.fb.control<MetodoLectura>('REAL', Validators.required),
    lecturaAnterior: [0, [Validators.required, Validators.min(0)]],
    lecturaActual: [0, [Validators.required, Validators.min(0)]],
    consumoFacturado: [0, [Validators.required, Validators.min(0)]],
    subtotalServicioElectrico: [0, [Validators.required, Validators.min(0)]],
    subtotalAlumbradoPublico: [0, [Validators.required, Validators.min(0)]],
    otrosCargos: [0, [Validators.required, Validators.min(0)]],
    saldoAnterior: [0, [Validators.required, Validators.min(0)]],
    recaudacionTerceros: [0, [Validators.required, Validators.min(0)]],
    subsidios: [0, [Validators.required, Validators.min(0)]],
    totalFactura: [0, [Validators.required, Validators.min(0)]],
    saldoPendiente: [0, [Validators.required, Validators.min(0)]],
    lineas: new FormArray<LineaForm>([]),
  });

  readonly lineas = this.formulario.controls.lineas;
  readonly totalPaginas = computed(() => Math.max(1, Math.ceil(this.total() / this.tamanoPagina)));
  readonly rangoInicio = computed(() =>
    this.total() ? (this.pagina() - 1) * this.tamanoPagina + 1 : 0,
  );
  readonly rangoFin = computed(() => Math.min(this.pagina() * this.tamanoPagina, this.total()));

  @ViewChild('searchInput') private searchInput?: ElementRef<HTMLInputElement>;
  @ViewChild('importButton') private importButton?: ElementRef<HTMLButtonElement>;
  @ViewChild('invoiceForm') private invoiceForm?: ElementRef<HTMLFormElement>;
  @ViewChild('editorHeading') private editorHeading?: ElementRef<HTMLHeadingElement>;
  @ViewChild('detailHeading') private detailHeading?: ElementRef<HTMLHeadingElement>;
  @ViewChild('successMessage') private successMessage?: ElementRef<HTMLElement>;
  @ViewChild('discardDialog') private discardDialog?: ElementRef<HTMLElement>;
  @ViewChild('discardCancel') private discardCancel?: ElementRef<HTMLButtonElement>;

  constructor() {
    this.catalogosService
      .asignacionOperativa()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (datos) => this.catalogos.set(datos),
        error: () =>
          this.catalogosError.set('No se pudieron cargar los suministros y medidores activos.'),
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
            'No fue posible consultar las facturas. Verifica la API e inténtalo nuevamente.',
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
        this.facturas.set(respuesta.items);
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

    this.formulario.controls.numeroSuministro.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((numeroSuministro) => {
        const medidor = this.formulario.controls.numeroMedidor;
        const sigueDisponible = this.medidoresDisponibles().some(
          (item) => item.numeroSerie === medidor.value,
        );
        if (medidor.value && (!numeroSuministro || !sigueDisponible)) {
          medidor.setValue('');
        }
      });

    this.lineas.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((lineas) => {
      this.totalRubros.set(lineas.reduce((total, linea) => total + Number(linea.monto ?? 0), 0));
    });

    this.agregarRubro(false);
    this.formulario.markAsPristine();
    this.cargar();
    this.destroyRef.onDestroy(() => document.body.style.removeProperty('overflow'));
  }

  abrirImportar(event?: Event): void {
    this.disparadorEditor =
      (event?.currentTarget as HTMLElement | null) ?? this.importButton?.nativeElement;
    this.detalle.set(null);
    this.formulario.reset({
      numeroSuministro: '',
      numeroMedidor: '',
      sistemaOrigen: 'SISTEMA_COMERCIAL',
      identificadorExterno: '',
      numeroFactura: '',
      nombreEmisor: 'Empresa eléctrica',
      rucEmisor: '',
      fechaEmision: this.fechaMaxima,
      fechaVencimiento: this.fechaMaxima,
      estado: 'EMITIDA',
      periodoDesde: this.inicioMesAnterior(),
      periodoHasta: this.finMesAnterior(),
      metodoLectura: 'REAL',
      lecturaAnterior: 0,
      lecturaActual: 0,
      consumoFacturado: 0,
      subtotalServicioElectrico: 0,
      subtotalAlumbradoPublico: 0,
      otrosCargos: 0,
      saldoAnterior: 0,
      recaudacionTerceros: 0,
      subsidios: 0,
      totalFactura: 0,
      saldoPendiente: 0,
    });
    this.lineas.clear();
    this.agregarRubro(false);
    this.formulario.markAsPristine();
    this.formularioEnviado.set(false);
    this.mensajeFormulario.set('');
    this.mensajeExito.set('');
    this.editorAbierto.set(true);
    setTimeout(() => this.editorHeading?.nativeElement.focus(), 0);
  }

  medidoresDisponibles(): MedidorCatalogo[] {
    const numeroSuministro = this.formulario.controls.numeroSuministro.value;
    return (this.catalogos()?.medidores ?? []).filter(
      (item) => item.numeroSuministro === numeroSuministro,
    );
  }

  agregarRubro(marcarCambio = true): void {
    this.lineas.push(
      this.fb.group({
        codigoOriginal: ['', [Validators.required, Validators.maxLength(50)]],
        descripcionOriginal: [
          '',
          [Validators.required, Validators.minLength(2), Validators.maxLength(200)],
        ],
        categoria: this.fb.control<CategoriaFactura>('ENERGIA', Validators.required),
        monto: [0, Validators.required],
        esIngresoElectrico: [true],
      }),
    );
    if (marcarCambio) this.formulario.markAsDirty();
  }

  quitarRubro(indice: number): void {
    if (this.lineas.length === 1 || this.guardando()) return;
    this.lineas.removeAt(indice);
    this.formulario.markAsDirty();
  }

  importar(): void {
    if (this.guardando()) return;
    this.formularioEnviado.set(true);
    this.mensajeFormulario.set('');
    this.formulario.markAllAsTouched();
    if (this.formulario.invalid || !this.validarFechas()) {
      this.enfocarPrimerError();
      return;
    }

    const valor = this.formulario.getRawValue();
    const solicitud: ImportarFactura = {
      ...valor,
      numeroSuministro: valor.numeroSuministro.trim().toUpperCase(),
      numeroMedidor: valor.numeroMedidor.trim().toUpperCase(),
      sistemaOrigen: valor.sistemaOrigen.trim().toUpperCase(),
      identificadorExterno: valor.identificadorExterno.trim().toUpperCase(),
      numeroFactura: valor.numeroFactura.trim().toUpperCase(),
      nombreEmisor: valor.nombreEmisor.trim(),
      rucEmisor: valor.rucEmisor.trim(),
      lineas: valor.lineas.map((linea) => ({
        ...linea,
        codigoOriginal: linea.codigoOriginal.trim().toUpperCase(),
        descripcionOriginal: linea.descripcionOriginal.trim(),
      })),
    };

    this.guardando.set(true);
    this.service
      .importar(solicitud)
      .pipe(
        finalize(() => this.guardando.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (resultado) => {
          this.formulario.markAsPristine();
          this.cerrarEditor(false);
          this.mensajeExito.set(
            resultado.yaExistia
              ? 'La factura ya estaba sincronizada; no se creó un duplicado.'
              : 'Factura importada correctamente como documento de solo lectura.',
          );
          this.cargar();
          setTimeout(() => this.successMessage?.nativeElement.focus(), 0);
        },
        error: (error: HttpErrorResponse) => this.procesarError(error),
      });
  }

  abrirDetalle(item: FacturaResumen, event: Event): void {
    this.disparadorDetalle = event.currentTarget as HTMLElement;
    this.editorAbierto.set(false);
    this.detalle.set(null);
    this.cargandoDetalle.set(true);
    this.service
      .obtener(item.id)
      .pipe(
        finalize(() => this.cargandoDetalle.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (detalle) => {
          this.detalle.set(detalle);
          setTimeout(() => this.detailHeading?.nativeElement.focus(), 0);
        },
        error: () =>
          this.mensajeExito.set(
            'No fue posible abrir el detalle de la factura. Inténtalo nuevamente.',
          ),
      });
  }

  cerrarDetalle(): void {
    this.detalle.set(null);
    setTimeout(() => this.disparadorDetalle?.focus(), 0);
  }

  solicitarCerrarEditor(): void {
    if (this.guardando()) return;
    if (this.formulario.dirty) {
      this.confirmarDescarte.set(true);
      document.body.style.overflow = 'hidden';
      setTimeout(() => this.discardCancel?.nativeElement.focus(), 0);
      return;
    }
    this.cerrarEditor();
  }

  cerrarConfirmacion(): void {
    this.confirmarDescarte.set(false);
    document.body.style.removeProperty('overflow');
    setTimeout(() => this.editorHeading?.nativeElement.focus(), 0);
  }

  descartarCambios(): void {
    this.formulario.markAsPristine();
    this.confirmarDescarte.set(false);
    document.body.style.removeProperty('overflow');
    this.cerrarEditor();
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

  etiquetaEstado(estado: EstadoFactura): string {
    return {
      EMITIDA: 'Emitida',
      PARCIALMENTE_PAGADA: 'Pago parcial',
      PAGADA: 'Pagada',
      VENCIDA: 'Vencida',
      ANULADA: 'Anulada',
    }[estado];
  }

  etiquetaMetodo(metodo: MetodoLectura): string {
    return {
      REAL: 'Real',
      ESTIMADA: 'Estimada',
      PROMEDIO: 'Promedio',
      REMOTA: 'Remota',
      AJUSTADA: 'Ajustada',
    }[metodo];
  }

  etiquetaCategoria(categoria: CategoriaFactura): string {
    return {
      ENERGIA: 'Energía',
      COMERCIALIZACION: 'Comercialización',
      ALUMBRADO_PUBLICO: 'Alumbrado público',
      DESCUENTO: 'Descuento',
      SUBSIDIO: 'Subsidio',
      INTERES_MORA: 'Interés de mora',
      SALDO_ANTERIOR: 'Saldo anterior',
      TERCERO: 'Recaudación de terceros',
      FINANCIAMIENTO: 'Financiamiento',
      CREDITO: 'Crédito',
      OTRO: 'Otro',
    }[categoria];
  }

  mostrarError(nombre: keyof typeof this.formulario.controls): boolean {
    const control = this.formulario.controls[nombre];
    return control.invalid && (control.touched || this.formularioEnviado());
  }

  mostrarErrorLinea(indice: number, nombre: keyof LineaForm['controls']): boolean {
    const control = this.lineas.at(indice).controls[nombre];
    return control.invalid && (control.touched || this.formularioEnviado());
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
      if (this.confirmarDescarte()) {
        event.preventDefault();
        this.cerrarConfirmacion();
      } else if (this.editorAbierto() && !this.guardando()) {
        event.preventDefault();
        this.solicitarCerrarEditor();
      } else if (this.detalle()) {
        event.preventDefault();
        this.cerrarDetalle();
      }
      return;
    }
    if (event.key !== 'Tab' || !this.confirmarDescarte() || !this.discardDialog) return;
    const controles = Array.from(
      this.discardDialog.nativeElement.querySelectorAll<HTMLElement>(
        'button:not([disabled]), [tabindex]:not([tabindex="-1"])',
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

  private crearErrorServidor(campo: keyof typeof this.formulario.controls, mensaje: string): void {
    const control = this.formulario.controls[campo];
    control.setErrors({ servidor: mensaje });
    control.markAsTouched();
  }

  private procesarError(error: HttpErrorResponse): void {
    const errores = error.error?.errors as Record<string, string[]> | undefined;
    if (errores) {
      const mapa: Record<string, keyof typeof this.formulario.controls> = {
        NumeroSuministro: 'numeroSuministro',
        NumeroMedidor: 'numeroMedidor',
        SistemaOrigen: 'sistemaOrigen',
        IdentificadorExterno: 'identificadorExterno',
        NumeroFactura: 'numeroFactura',
        NombreEmisor: 'nombreEmisor',
        RucEmisor: 'rucEmisor',
        FechaEmision: 'fechaEmision',
        FechaVencimiento: 'fechaVencimiento',
        PeriodoHasta: 'periodoHasta',
        ConsumoFacturado: 'consumoFacturado',
        TotalFactura: 'totalFactura',
      };
      Object.entries(errores).forEach(([clave, mensajes]) => {
        const campo = mapa[clave];
        if (campo && mensajes.length) this.crearErrorServidor(campo, mensajes[0]);
      });
      this.mensajeFormulario.set('Corrige los campos señalados y vuelve a importar.');
      this.enfocarPrimerError();
      return;
    }
    this.mensajeFormulario.set(
      error.error?.mensaje ??
        'No fue posible importar la factura. Conservamos la información para reintentar.',
    );
    setTimeout(() => this.editorHeading?.nativeElement.focus(), 0);
  }

  private validarFechas(): boolean {
    const emision = this.formulario.controls.fechaEmision.value;
    const vencimiento = this.formulario.controls.fechaVencimiento.value;
    const desde = this.formulario.controls.periodoDesde.value;
    const hasta = this.formulario.controls.periodoHasta.value;
    let valido = true;
    if (vencimiento < emision) {
      this.formulario.controls.fechaVencimiento.setErrors({ rango: true });
      valido = false;
    }
    if (hasta < desde) {
      this.formulario.controls.periodoHasta.setErrors({ rango: true });
      valido = false;
    }
    return valido;
  }

  private enfocarPrimerError(): void {
    setTimeout(
      () =>
        this.invoiceForm?.nativeElement
          .querySelector<HTMLElement>('[aria-invalid="true"]')
          ?.focus(),
      0,
    );
  }

  private cerrarEditor(restaurarFoco = true): void {
    this.editorAbierto.set(false);
    this.formularioEnviado.set(false);
    this.mensajeFormulario.set('');
    if (restaurarFoco)
      setTimeout(() => (this.disparadorEditor ?? this.importButton?.nativeElement)?.focus(), 0);
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

  private fechaLocalActual(): string {
    return this.formatearFecha(new Date());
  }

  private inicioMesAnterior(): string {
    const fecha = new Date();
    return this.formatearFecha(new Date(fecha.getFullYear(), fecha.getMonth() - 1, 1));
  }

  private finMesAnterior(): string {
    const fecha = new Date();
    return this.formatearFecha(new Date(fecha.getFullYear(), fecha.getMonth(), 0));
  }

  private formatearFecha(fecha: Date): string {
    const mes = String(fecha.getMonth() + 1).padStart(2, '0');
    const dia = String(fecha.getDate()).padStart(2, '0');
    return `${fecha.getFullYear()}-${mes}-${dia}`;
  }
}
