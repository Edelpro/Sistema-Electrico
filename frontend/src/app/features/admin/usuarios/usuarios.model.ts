export type RolCuenta = 'Tecnico' | 'Usuario';

export interface UsuarioResumen {
  readonly id: number;
  readonly nombreCompleto: string;
  readonly identificacion: string;
  readonly correo: string;
  readonly telefono: string | null;
  readonly rol: string;
  readonly clienteVinculado: string | null;
  readonly activo: boolean;
  readonly ultimoAcceso: string | null;
}

export interface UsuariosPagina {
  readonly items: readonly UsuarioResumen[];
  readonly total: number;
  readonly pagina: number;
  readonly tamanoPagina: number;
}

export interface UsuarioDetalle {
  readonly id: number;
  readonly nombres: string;
  readonly apellidos: string;
  readonly identificacion: string;
  readonly correo: string;
  readonly telefono: string | null;
  readonly rol: string;
  readonly identificacionCliente: string | null;
  readonly clienteVinculado: string | null;
  readonly activo: boolean;
  readonly fechaCreacion: string;
  readonly ultimoAcceso: string | null;
}

export interface CrearUsuario {
  readonly nombres: string;
  readonly apellidos: string;
  readonly identificacion: string;
  readonly correo: string;
  readonly telefono: string | null;
  readonly rol: RolCuenta;
  readonly identificacionCliente: string | null;
  readonly activo: boolean;
  readonly enviarInvitacion: boolean;
}

export interface CrearUsuarioResultado {
  readonly usuario: UsuarioDetalle;
  readonly invitacionEnviada: boolean;
  readonly mensaje: string;
}

export interface EnvioInvitacionUsuario {
  readonly invitacionEnviada: boolean;
  readonly mensaje: string;
}

export interface ActualizarUsuario {
  readonly nombres: string;
  readonly apellidos: string;
  readonly identificacion: string;
  readonly correo: string;
  readonly telefono: string | null;
  readonly rol: RolCuenta;
  readonly identificacionCliente: string | null;
  readonly motivo: string;
}

export interface CambiarEstadoUsuario {
  readonly activo: boolean;
  readonly motivo: string;
}
