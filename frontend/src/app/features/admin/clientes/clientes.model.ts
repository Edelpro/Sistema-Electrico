export interface ClienteResumen {
  readonly id: number;
  readonly nombreCompleto: string;
  readonly tipoIdentificacion: string;
  readonly identificacion: string;
  readonly correo: string | null;
  readonly telefono: string | null;
  readonly direccionNotificacion: string | null;
  readonly activo: boolean;
  readonly fechaRegistro: string;
}

export interface ClientesPagina {
  readonly items: readonly ClienteResumen[];
  readonly total: number;
  readonly pagina: number;
  readonly tamanoPagina: number;
}

export type TipoIdentificacionCliente = 'CEDULA' | 'RUC' | 'PASAPORTE';

export interface ClienteDetalle {
  readonly id: number;
  readonly tipoIdentificacion: TipoIdentificacionCliente;
  readonly identificacion: string;
  readonly nombres: string;
  readonly apellidos: string;
  readonly correo: string | null;
  readonly telefono: string | null;
  readonly direccionNotificacion: string | null;
  readonly activo: boolean;
  readonly fechaRegistro: string;
}

export interface ClienteGuardar {
  readonly tipoIdentificacion: TipoIdentificacionCliente;
  readonly identificacion: string;
  readonly nombres: string;
  readonly apellidos: string;
  readonly correo: string | null;
  readonly telefono: string | null;
  readonly direccionNotificacion: string | null;
}

export interface CambiarEstadoCliente {
  readonly activo: boolean;
  readonly motivo: string;
}
