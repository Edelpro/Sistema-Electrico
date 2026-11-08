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
import { ActualizarUsuario, CrearUsuario, RolCuenta, UsuarioResumen } from './usuarios.model';
import { UsuariosService } from './usuarios.service';

type EstadoCarga = 'cargando' | 'listo' | 'error';
type CampoUsuario =
  | 'rol'
  | 'identificacionCliente'
  | 'identificacion'
  | 'nombres'
  | 'apellidos'
  | 'correo'
  | 'telefono'
  | 'motivo';

interface SolicitudUsuarios {
  readonly buscar: string;
  readonly pagina: number;
  readonly tamanoPagina: number;
}

interface ConfirmacionEstado {
  readonly tipo: 'estado';
  readonly usuario: UsuarioResumen;
  readonly activar: boolean;
}

interface ConfirmacionDescartar {
  readonly tipo: 'descartar';
}

type Confirmacion = ConfirmacionEstado | ConfirmacionDescartar;

@Component({
  selector: 'app-admin-usuarios',
  standalone: true,
  imports: [DatePipe, ReactiveFormsModule, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './usuarios.html',
  styleUrls: ['./usuarios.css', '../clientes/clientes.css'],
})
export class Usuarios {
  private readonly usuariosService = inject(UsuariosService);
  private readonly catalogosService = inject(CatalogosService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly solicitudes = new Subject<SolicitudUsuarios>();
  private disparadorEditor?: HTMLElement;
  private disparadorConfirmacion?: HTMLElement;

  readonly tamanoPagina = 10;
  readonly usuarios = signal<readonly UsuarioResumen[]>([]);
  readonly total = signal(0);
  readonly pagina = signal(this.obtenerPaginaInicial());
  readonly estado = signal<EstadoCarga>('cargando');
  readonly mensajeError = signal('');
  readonly mensajeErrorAccion = signal('');
  readonly mensajeExito = signal('');
  readonly editorAbierto = signal(false);
  readonly cargandoDetalle = signal(false);
  readonly guardando = signal(false);
  readonly usuarioEditandoId = signal<number | null>(null);
  readonly formularioEnviado = signal(false);
  readonly mensajeFormulario = signal('');
  readonly confirmacion = signal<Confirmacion | null>(null);
  readonly procesandoEstado = signal(false);
  readonly mensajeConfirmacion = signal('');
  readonly enviandoInvitacionId = signal<number | null>(null);
  readonly catalogos = signal<CatalogosAsignacion | null>(null);
  readonly catalogosError = signal('');

  readonly busqueda = new FormControl(this.route.snapshot.queryParamMap.get('q') ?? '', {
    nonNullable: true,
  });

  readonly formulario = new FormGroup({
    rol: new FormControl<RolCuenta>('Usuario', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    identificacion: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(/^\d{10}$/)],
    }),
    identificacionCliente: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(20)],
    }),
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
      validators: [Validators.required, Validators.email, Validators.maxLength(160)],
    }),
    telefono: new FormControl('', {
      nonNullable: true,
      validators: [Validators.pattern(/^\+?[0-9 ()-]{7,20}$/), Validators.maxLength(20)],
    }),
    activo: new FormControl(true, { nonNullable: true }),
    enviarInvitacion: new FormControl(true, { nonNullable: true }),
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
  readonly editando = computed(() => this.usuarioEditandoId() !== null);

  @ViewChild('searchInput')
  private searchInput?: ElementRef<HTMLInputElement>;

  @ViewChild('createButton')
  private createButton?: ElementRef<HTMLButtonElement>;

  @ViewChild('userForm')
  private userForm?: ElementRef<HTMLFormElement>;

  @ViewChild('userFormHeading')
  private userFormHeading?: ElementRef<HTMLHeadingElement>;

  @ViewChild('successMessage')
  private successMessage?: ElementRef<HTMLElement>;

  @ViewChild('confirmationDialog')
  private confirmationDialog?: ElementRef<HTMLElement>;

  @ViewChild('confirmationCancel')
  private confirmationCancel?: ElementRef<HTMLButtonElement>;

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

          return this.usuariosService.listar(solicitud).pipe(
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
            'No fue posible consultar las cuentas. Verifica que la API esté disponible e inténtalo nuevamente.',
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

        this.usuarios.set(respuesta.items);
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

    this.formulario.controls.rol.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((rol) => this.configurarValidadorCliente(rol));

    this.formulario.controls.activo.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((activo) => {
        const invitacion = this.formulario.controls.enviarInvitacion;
        if (!activo) {
          invitacion.setValue(false, { emitEvent: false });
        }
      });

    this.cargar();
    this.destroyRef.onDestroy(() => document.body.style.removeProperty('overflow'));
  }

  abrirCrear(event?: Event): void {
    this.disparadorEditor =
      (event?.currentTarget as HTMLElement | null) ?? this.createButton?.nativeElement;
    this.usuarioEditandoId.set(null);
    this.formulario.reset({
      rol: 'Usuario',
      identificacion: '',
      identificacionCliente: '',
      nombres: '',
      apellidos: '',
      correo: '',
      telefono: '',
      activo: true,
      enviarInvitacion: true,
      motivo: '',
    });
    this.configurarValidadoresSegunModo(false);
    this.prepararEditor();
  }

  abrirEditar(usuario: UsuarioResumen, event: Event): void {
    if (this.esAdministrador(usuario)) return;

    this.disparadorEditor = event.currentTarget as HTMLElement;
    this.usuarioEditandoId.set(usuario.id);
    this.configurarValidadoresSegunModo(true);
    this.formularioEnviado.set(false);
    this.mensajeFormulario.set('');
    this.mensajeExito.set('');
    this.mensajeErrorAccion.set('');
    this.cargandoDetalle.set(true);
    this.editorAbierto.set(true);

    this.usuariosService
      .obtener(usuario.id)
      .pipe(
        finalize(() => this.cargandoDetalle.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (detalle) => {
          this.formulario.reset({
            rol: detalle.rol === 'Tecnico' ? 'Tecnico' : 'Usuario',
            identificacion: detalle.identificacion,
            identificacionCliente: detalle.identificacionCliente ?? '',
            nombres: detalle.nombres,
            apellidos: detalle.apellidos,
            correo: detalle.correo,
            telefono: detalle.telefono ?? '',
            activo: detalle.activo,
            enviarInvitacion: false,
            motivo: '',
          });
          this.formulario.markAsPristine();
          setTimeout(() => this.userFormHeading?.nativeElement.focus(), 0);
        },
        error: () => {
          this.mensajeFormulario.set(
            'No fue posible cargar los datos de la cuenta. Cierra el formulario e inténtalo nuevamente.',
          );
          setTimeout(() => this.userFormHeading?.nativeElement.focus(), 0);
        },
      });
  }

  guardarUsuario(): void {
    if (this.guardando() || this.cargandoDetalle()) return;

    this.formularioEnviado.set(true);
    this.mensajeFormulario.set('');
    this.formulario.markAllAsTouched();

    if (this.formulario.invalid) {
      this.enfocarPrimerError();
      return;
    }

    const valor = this.formulario.getRawValue();
    const base = {
      nombres: valor.nombres.trim(),
      apellidos: valor.apellidos.trim(),
      identificacion: valor.identificacion.trim(),
      correo: valor.correo.trim().toLowerCase(),
      telefono: this.normalizarOpcional(valor.telefono),
      rol: valor.rol,
      identificacionCliente:
        valor.rol === 'Usuario' ? this.normalizarOpcional(valor.identificacionCliente) : null,
    };
    const id = this.usuarioEditandoId();
    const operacion =
      id === null
        ? this.usuariosService
            .crear({
              ...base,
              activo: valor.activo,
              enviarInvitacion: valor.activo && valor.enviarInvitacion,
            } satisfies CrearUsuario)
            .pipe(
              map((resultado) => ({
                mensaje: resultado.mensaje,
                esErrorEnvio: valor.enviarInvitacion && !resultado.invitacionEnviada,
              })),
            )
        : this.usuariosService
            .actualizar(id, {
              ...base,
              motivo: valor.motivo.trim(),
            } satisfies ActualizarUsuario)
            .pipe(
              map(() => ({
                mensaje: 'Cuenta actualizada correctamente.',
                esErrorEnvio: false,
              })),
            );

    this.guardando.set(true);
    operacion
      .pipe(
        finalize(() => this.guardando.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (resultado) => {
          this.formulario.markAsPristine();
          this.cerrarEditor(false);
          if (resultado.esErrorEnvio) {
            this.mensajeErrorAccion.set(resultado.mensaje);
          } else {
            this.mensajeExito.set(resultado.mensaje);
          }
          this.cargar();
          setTimeout(() => this.successMessage?.nativeElement.focus(), 0);
        },
        error: (error: HttpErrorResponse) => this.procesarErrorFormulario(error),
      });
  }

  solicitarCerrarEditor(): void {
    if (this.guardando()) return;
    if (this.formulario.dirty) {
      this.abrirConfirmacion({ tipo: 'descartar' }, this.userFormHeading?.nativeElement);
      return;
    }
    this.cerrarEditor();
  }

  solicitarCambioEstado(usuario: UsuarioResumen, event: Event): void {
    if (this.esAdministrador(usuario)) return;
    this.motivoEstado.reset('');
    this.mensajeConfirmacion.set('');
    this.abrirConfirmacion(
      { tipo: 'estado', usuario, activar: !usuario.activo },
      event.currentTarget as HTMLElement,
    );
  }

  enviarInvitacion(usuario: UsuarioResumen): void {
    if (!usuario.activo || this.enviandoInvitacionId() !== null) return;

    this.mensajeExito.set('');
    this.enviandoInvitacionId.set(usuario.id);
    this.usuariosService
      .reenviarInvitacion(usuario.id)
      .pipe(
        finalize(() => this.enviandoInvitacionId.set(null)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (resultado) => {
          if (resultado.invitacionEnviada) {
            this.mensajeExito.set(resultado.mensaje);
          } else {
            this.mensajeErrorAccion.set(resultado.mensaje);
          }
          setTimeout(() => this.successMessage?.nativeElement.focus(), 0);
        },
        error: (error: HttpErrorResponse) => {
          this.mensajeErrorAccion.set(
            error.error?.mensaje ??
              'No fue posible enviar el enlace de acceso. Verifica el correo y la configuración SMTP.',
          );
        },
      });
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
    this.mensajeConfirmacion.set('');
    if (this.motivoEstado.invalid) return;

    this.procesandoEstado.set(true);
    this.usuariosService
      .cambiarEstado(actual.usuario.id, {
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
            ? 'Cuenta reactivada correctamente.'
            : 'Cuenta desactivada. Su sesión y nuevos accesos quedan bloqueados.';
          this.cerrarConfirmacion(false, true);
          this.mensajeExito.set(mensaje);
          this.cargar();
          setTimeout(() => this.successMessage?.nativeElement.focus(), 0);
        },
        error: (error: HttpErrorResponse) => {
          this.mensajeConfirmacion.set(
            error.error?.mensaje ??
              'No fue posible cambiar el estado. Conservamos el motivo para que puedas reintentar.',
          );
        },
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

  ocultarErrorAccion(): void {
    this.mensajeErrorAccion.set('');
  }

  reintentar(): void {
    this.cargar();
  }

  cambiarPagina(nuevaPagina: number): void {
    if (nuevaPagina < 1 || nuevaPagina > this.totalPaginas() || nuevaPagina === this.pagina())
      return;
    this.pagina.set(nuevaPagina);
    this.actualizarUrl();
    this.cargar();
  }

  mostrarError(campo: CampoUsuario): boolean {
    const control = this.formulario.controls[campo];
    return control.invalid && (control.touched || this.formularioEnviado());
  }

  errorCampo(campo: CampoUsuario): string {
    const errores = this.formulario.controls[campo].errors;
    if (!errores) return '';
    if (errores['required']) return 'Este campo es obligatorio.';
    if (errores['email']) return 'Escribe un correo válido.';
    if (errores['maxlength']) return 'El valor supera la longitud permitida.';
    if (errores['minlength']) return 'Escribe al menos 5 caracteres.';
    if (errores['duplicada']) {
      return campo === 'correo'
        ? 'Ya existe una cuenta con este correo.'
        : 'Ya existe una cuenta con esta identificación.';
    }
    if (errores['pattern']) {
      if (campo === 'identificacion') return 'Ingresa exactamente 10 dígitos.';
      if (campo === 'telefono') return 'Escribe un teléfono válido de 7 a 20 caracteres.';
    }
    if (errores['servidor']) return String(errores['servidor']);
    return 'Revisa este valor.';
  }

  etiquetaRol(rol: string): string {
    if (rol === 'Tecnico') return 'Técnico';
    if (rol === 'Usuario') return 'Cliente / usuario';
    return rol;
  }

  esAdministrador(usuario: UsuarioResumen): boolean {
    return usuario.rol === 'Administrador';
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
    setTimeout(() => this.userFormHeading?.nativeElement.focus(), 0);
  }

  private configurarValidadoresSegunModo(editando: boolean): void {
    const motivo = this.formulario.controls.motivo;
    motivo.setValidators(
      editando ? [Validators.required, Validators.minLength(5), Validators.maxLength(250)] : [],
    );
    motivo.updateValueAndValidity({ emitEvent: false });
    this.configurarValidadorCliente(this.formulario.controls.rol.value);
  }

  private configurarValidadorCliente(rol: RolCuenta): void {
    const cliente = this.formulario.controls.identificacionCliente;
    cliente.setValidators(
      rol === 'Usuario'
        ? [Validators.required, Validators.maxLength(20)]
        : [Validators.maxLength(20)],
    );
    if (rol === 'Tecnico') cliente.setValue('', { emitEvent: false });
    cliente.updateValueAndValidity({ emitEvent: false });
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
    this.usuarioEditandoId.set(null);
    this.formularioEnviado.set(false);
    this.mensajeFormulario.set('');
    if (restaurarFoco) {
      setTimeout(() => (this.disparadorEditor ?? this.createButton?.nativeElement)?.focus(), 0);
    }
  }

  private procesarErrorFormulario(error: HttpErrorResponse): void {
    if (error.status === 409 && error.error?.codigo === 'IDENTIFICACION_DUPLICADA') {
      this.marcarErrorDuplicado('identificacion');
      return;
    }
    if (error.status === 409 && error.error?.codigo === 'CORREO_DUPLICADO') {
      this.marcarErrorDuplicado('correo');
      return;
    }

    const errores = error.error?.errors as Record<string, string[]> | undefined;
    if (errores) {
      const mapa: Record<string, CampoUsuario> = {
        Rol: 'rol',
        Identificacion: 'identificacion',
        IdentificacionCliente: 'identificacionCliente',
        Nombres: 'nombres',
        Apellidos: 'apellidos',
        Correo: 'correo',
        Telefono: 'telefono',
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
        'No fue posible guardar la cuenta. Conservamos la información para que puedas reintentar.',
    );
    setTimeout(() => this.userFormHeading?.nativeElement.focus(), 0);
  }

  private marcarErrorDuplicado(campo: 'identificacion' | 'correo'): void {
    this.formulario.controls[campo].setErrors({ duplicada: true });
    this.formulario.controls[campo].markAsTouched();
    this.mensajeFormulario.set('Revisa los datos duplicados antes de guardar.');
    this.enfocarPrimerError();
  }

  private enfocarPrimerError(): void {
    setTimeout(() => {
      this.userForm?.nativeElement.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus();
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
