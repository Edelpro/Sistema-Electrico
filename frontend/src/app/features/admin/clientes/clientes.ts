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
import { ClienteGuardar, ClienteResumen, TipoIdentificacionCliente } from './clientes.model';
import { ClientesService } from './clientes.service';

type EstadoCarga = 'cargando' | 'listo' | 'error';
type CampoCliente =
  | 'tipoIdentificacion'
  | 'identificacion'
  | 'nombres'
  | 'apellidos'
  | 'correo'
  | 'telefono'
  | 'direccionNotificacion';

interface SolicitudClientes {
  readonly buscar: string;
  readonly pagina: number;
  readonly tamanoPagina: number;
}

interface ConfirmacionEstado {
  readonly tipo: 'estado';
  readonly cliente: ClienteResumen;
  readonly activar: boolean;
}

interface ConfirmacionDescartar {
  readonly tipo: 'descartar';
}

type Confirmacion = ConfirmacionEstado | ConfirmacionDescartar;

@Component({
  selector: 'app-admin-clientes',
  standalone: true,
  imports: [DatePipe, ReactiveFormsModule, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './clientes.html',
  styleUrls: ['../usuarios/usuarios.css', './clientes.css'],
})
export class Clientes {
  private readonly clientesService = inject(ClientesService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly solicitudes = new Subject<SolicitudClientes>();
  private disparadorEditor?: HTMLElement;
  private disparadorConfirmacion?: HTMLElement;

  readonly tamanoPagina = 10;
  readonly clientes = signal<readonly ClienteResumen[]>([]);
  readonly total = signal(0);
  readonly pagina = signal(this.obtenerPaginaInicial());
  readonly estado = signal<EstadoCarga>('cargando');
  readonly mensajeError = signal('');
  readonly mensajeExito = signal('');
  readonly editorAbierto = signal(false);
  readonly cargandoDetalle = signal(false);
  readonly guardando = signal(false);
  readonly clienteEditandoId = signal<number | null>(null);
  readonly formularioEnviado = signal(false);
  readonly mensajeFormulario = signal('');
  readonly confirmacion = signal<Confirmacion | null>(null);
  readonly procesandoEstado = signal(false);
  readonly mensajeConfirmacion = signal('');

  readonly busqueda = new FormControl(this.route.snapshot.queryParamMap.get('q') ?? '', {
    nonNullable: true,
  });

  readonly formulario = new FormGroup({
    tipoIdentificacion: new FormControl<TipoIdentificacionCliente>('CEDULA', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    identificacion: new FormControl('', { nonNullable: true }),
    nombres: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(100)],
    }),
    apellidos: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(100)],
    }),
    correo: new FormControl('', {
      nonNullable: true,
      validators: [Validators.email, Validators.maxLength(160)],
    }),
    telefono: new FormControl('', {
      nonNullable: true,
      validators: [Validators.pattern(/^\+?[0-9 ()-]{7,20}$/), Validators.maxLength(20)],
    }),
    direccionNotificacion: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(300)],
    }),
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
  readonly editando = computed(() => this.clienteEditandoId() !== null);

  @ViewChild('searchInput')
  private searchInput?: ElementRef<HTMLInputElement>;

  @ViewChild('createButton')
  private createButton?: ElementRef<HTMLButtonElement>;

  @ViewChild('clientForm')
  private clientForm?: ElementRef<HTMLFormElement>;

  @ViewChild('clientFormHeading')
  private clientFormHeading?: ElementRef<HTMLHeadingElement>;

  @ViewChild('successMessage')
  private successMessage?: ElementRef<HTMLElement>;

  @ViewChild('confirmationDialog')
  private confirmationDialog?: ElementRef<HTMLElement>;

  @ViewChild('confirmationCancel')
  private confirmationCancel?: ElementRef<HTMLButtonElement>;

  constructor() {
    this.solicitudes
      .pipe(
        switchMap((solicitud) => {
          this.estado.set('cargando');
          this.mensajeError.set('');

          return this.clientesService.listar(solicitud).pipe(
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
            'No fue posible consultar los clientes. Verifica que la API esté disponible e inténtalo nuevamente.',
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

        this.clientes.set(respuesta.items);
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

    this.formulario.controls.tipoIdentificacion.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((tipo) => this.aplicarValidadoresPorTipo(tipo));

    this.aplicarValidadoresPorTipo('CEDULA');
    this.cargar();

    this.destroyRef.onDestroy(() => document.body.style.removeProperty('overflow'));
  }

  abrirCrear(event?: Event): void {
    this.disparadorEditor =
      (event?.currentTarget as HTMLElement | null) ?? this.createButton?.nativeElement;
    this.clienteEditandoId.set(null);
    this.formulario.reset({
      tipoIdentificacion: 'CEDULA',
      identificacion: '',
      nombres: '',
      apellidos: '',
      correo: '',
      telefono: '',
      direccionNotificacion: '',
    });
    this.aplicarValidadoresPorTipo('CEDULA');
    this.formulario.markAsPristine();
    this.formularioEnviado.set(false);
    this.mensajeFormulario.set('');
    this.mensajeExito.set('');
    this.cargandoDetalle.set(false);
    this.editorAbierto.set(true);
    setTimeout(() => this.clientFormHeading?.nativeElement.focus(), 0);
  }

  abrirEditar(cliente: ClienteResumen, event: Event): void {
    this.disparadorEditor = event.currentTarget as HTMLElement;
    this.clienteEditandoId.set(cliente.id);
    this.formularioEnviado.set(false);
    this.mensajeFormulario.set('');
    this.mensajeExito.set('');
    this.cargandoDetalle.set(true);
    this.editorAbierto.set(true);

    this.clientesService
      .obtener(cliente.id)
      .pipe(
        finalize(() => this.cargandoDetalle.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (detalle) => {
          this.formulario.reset({
            tipoIdentificacion: detalle.tipoIdentificacion,
            identificacion: detalle.identificacion,
            nombres: detalle.nombres,
            apellidos: detalle.apellidos,
            correo: detalle.correo ?? '',
            telefono: detalle.telefono ?? '',
            direccionNotificacion: detalle.direccionNotificacion ?? '',
          });
          this.aplicarValidadoresPorTipo(detalle.tipoIdentificacion);
          this.formulario.markAsPristine();
          setTimeout(() => this.clientFormHeading?.nativeElement.focus(), 0);
        },
        error: () => {
          this.mensajeFormulario.set(
            'No fue posible cargar los datos completos del cliente. Cierra el formulario e inténtalo nuevamente.',
          );
          setTimeout(() => this.clientFormHeading?.nativeElement.focus(), 0);
        },
      });
  }

  guardarCliente(): void {
    if (this.guardando() || this.cargandoDetalle()) {
      return;
    }

    this.formularioEnviado.set(true);
    this.mensajeFormulario.set('');
    this.formulario.markAllAsTouched();

    if (this.formulario.invalid) {
      this.enfocarPrimerError();
      return;
    }

    const valor = this.formulario.getRawValue();
    const solicitud: ClienteGuardar = {
      tipoIdentificacion: valor.tipoIdentificacion,
      identificacion: valor.identificacion.trim().toUpperCase(),
      nombres: valor.nombres.trim(),
      apellidos: valor.apellidos.trim(),
      correo: this.normalizarOpcional(valor.correo)?.toLowerCase() ?? null,
      telefono: this.normalizarOpcional(valor.telefono),
      direccionNotificacion: this.normalizarOpcional(valor.direccionNotificacion),
    };

    const id = this.clienteEditandoId();
    const operacion =
      id === null
        ? this.clientesService.crear(solicitud)
        : this.clientesService.actualizar(id, solicitud);

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
            id === null ? 'Cliente creado correctamente.' : 'Cambios guardados correctamente.',
          );
          this.cargar();
          setTimeout(() => this.successMessage?.nativeElement.focus(), 0);
        },
        error: (error: HttpErrorResponse) => this.procesarErrorFormulario(error),
      });
  }

  solicitarCerrarEditor(): void {
    if (this.guardando()) {
      return;
    }

    if (this.formulario.dirty) {
      this.abrirConfirmacion({ tipo: 'descartar' }, this.clientFormHeading?.nativeElement);
      return;
    }

    this.cerrarEditor();
  }

  solicitarCambioEstado(cliente: ClienteResumen, event: Event): void {
    this.motivoEstado.reset('');
    this.mensajeConfirmacion.set('');
    this.abrirConfirmacion(
      { tipo: 'estado', cliente, activar: !cliente.activo },
      event.currentTarget as HTMLElement,
    );
  }

  confirmarAccion(): void {
    const actual = this.confirmacion();
    if (!actual || this.procesandoEstado()) {
      return;
    }

    if (actual.tipo === 'descartar') {
      this.formulario.markAsPristine();
      this.cerrarConfirmacion(false);
      this.cerrarEditor();
      return;
    }

    this.motivoEstado.markAsTouched();
    this.mensajeConfirmacion.set('');
    if (this.motivoEstado.invalid) {
      return;
    }

    this.procesandoEstado.set(true);
    this.clientesService
      .cambiarEstado(actual.cliente.id, {
        activo: actual.activar,
        motivo: this.motivoEstado.value.trim(),
      })
      .pipe(
        finalize(() => this.procesandoEstado.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          const mensaje = actual.activar
            ? 'Cliente reactivado correctamente.'
            : 'Cliente desactivado correctamente.';
          this.cerrarConfirmacion(false, true);
          this.mensajeExito.set(mensaje);
          this.cargar();
          setTimeout(() => this.successMessage?.nativeElement.focus(), 0);
        },
        error: (error: HttpErrorResponse) => {
          this.mensajeConfirmacion.set(
            error.error?.mensaje ??
              'No fue posible cambiar el estado. Conservamos el motivo para que puedas corregirlo o reintentar.',
          );
        },
      });
  }

  cerrarConfirmacion(restaurarFoco = true, forzar = false): void {
    if (this.procesandoEstado() && !forzar) {
      return;
    }

    this.confirmacion.set(null);
    this.mensajeConfirmacion.set('');
    document.body.style.removeProperty('overflow');
    if (restaurarFoco) {
      setTimeout(() => this.disparadorConfirmacion?.focus(), 0);
    }
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

  cambiarPagina(nuevaPagina: number): void {
    if (nuevaPagina < 1 || nuevaPagina > this.totalPaginas() || nuevaPagina === this.pagina()) {
      return;
    }

    this.pagina.set(nuevaPagina);
    this.actualizarUrl();
    this.cargar();
  }

  mostrarError(campo: CampoCliente): boolean {
    const control = this.formulario.controls[campo];
    return control.invalid && (control.touched || this.formularioEnviado());
  }

  errorCampo(campo: CampoCliente): string {
    const errores = this.formulario.controls[campo].errors;
    if (!errores) return '';
    if (errores['required']) return 'Este campo es obligatorio.';
    if (errores['email']) return 'Escribe un correo válido.';
    if (errores['maxlength']) return 'El valor supera la longitud permitida.';
    if (errores['pattern']) {
      return campo === 'telefono'
        ? 'Escribe un teléfono válido de 7 a 20 caracteres.'
        : this.ayudaIdentificacion();
    }
    if (errores['duplicada']) return 'Ya existe un cliente con esta identificación.';
    if (errores['servidor']) return String(errores['servidor']);
    return 'Revisa este valor.';
  }

  ayudaIdentificacion(): string {
    const tipo = this.formulario.controls.tipoIdentificacion.value;
    if (tipo === 'CEDULA') return 'Ingresa exactamente 10 dígitos.';
    if (tipo === 'RUC') return 'Ingresa exactamente 13 dígitos.';
    return 'Usa de 5 a 20 letras, números o guiones.';
  }

  @HostListener('window:beforeunload', ['$event'])
  prevenirSalidaConCambios(event: BeforeUnloadEvent): void {
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

    if (event.key !== 'Tab' || !this.confirmacion() || !this.confirmationDialog) {
      return;
    }

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

  private abrirConfirmacion(confirmacion: Confirmacion, retorno?: HTMLElement): void {
    this.disparadorConfirmacion = retorno;
    this.confirmacion.set(confirmacion);
    document.body.style.overflow = 'hidden';
    setTimeout(() => this.confirmationCancel?.nativeElement.focus(), 0);
  }

  private cerrarEditor(restaurarFoco = true): void {
    this.editorAbierto.set(false);
    this.cargandoDetalle.set(false);
    this.clienteEditandoId.set(null);
    this.formularioEnviado.set(false);
    this.mensajeFormulario.set('');
    if (restaurarFoco) {
      setTimeout(() => (this.disparadorEditor ?? this.createButton?.nativeElement)?.focus(), 0);
    }
  }

  private aplicarValidadoresPorTipo(tipo: TipoIdentificacionCliente): void {
    const identificacion = this.formulario.controls.identificacion;
    const apellidos = this.formulario.controls.apellidos;

    const patron =
      tipo === 'CEDULA' ? /^\d{10}$/ : tipo === 'RUC' ? /^\d{13}$/ : /^[A-Za-z0-9-]{5,20}$/;

    identificacion.setValidators([
      Validators.required,
      Validators.maxLength(20),
      Validators.pattern(patron),
    ]);
    apellidos.setValidators(
      tipo === 'RUC'
        ? [Validators.maxLength(100)]
        : [Validators.required, Validators.maxLength(100)],
    );
    identificacion.updateValueAndValidity({ emitEvent: false });
    apellidos.updateValueAndValidity({ emitEvent: false });
  }

  private procesarErrorFormulario(error: HttpErrorResponse): void {
    if (error.status === 409 && error.error?.codigo === 'IDENTIFICACION_DUPLICADA') {
      this.formulario.controls.identificacion.setErrors({ duplicada: true });
      this.formulario.controls.identificacion.markAsTouched();
      this.mensajeFormulario.set('Revisa la identificación antes de guardar.');
      this.enfocarPrimerError();
      return;
    }

    const errores = error.error?.errors as Record<string, string[]> | undefined;
    if (errores) {
      const mapa: Record<string, CampoCliente> = {
        TipoIdentificacion: 'tipoIdentificacion',
        Identificacion: 'identificacion',
        Nombres: 'nombres',
        Apellidos: 'apellidos',
        Correo: 'correo',
        Telefono: 'telefono',
        DireccionNotificacion: 'direccionNotificacion',
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
        'No fue posible guardar el cliente. Conservamos la información para que puedas reintentar.',
    );
    setTimeout(() => this.clientFormHeading?.nativeElement.focus(), 0);
  }

  private enfocarPrimerError(): void {
    setTimeout(() => {
      const control =
        this.clientForm?.nativeElement.querySelector<HTMLElement>('[aria-invalid="true"]');
      control?.focus();
    }, 0);
  }

  private normalizarOpcional(valor: string): string | null {
    const normalizado = valor.trim();
    return normalizado ? normalizado : null;
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
      queryParams: {
        q: termino || null,
        pagina: this.pagina() > 1 ? this.pagina() : null,
      },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  private obtenerPaginaInicial(): number {
    const valor = Number(this.route.snapshot.queryParamMap.get('pagina'));
    return Number.isInteger(valor) && valor > 0 ? valor : 1;
  }
}
