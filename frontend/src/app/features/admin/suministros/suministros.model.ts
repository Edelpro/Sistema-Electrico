export interface SuministroResumen {
  readonly id: number;
  readonly numeroSuministro: string;
  readonly numeroContrato: string;
  readonly cuen: string;
  readonly titularActual: string;
  readonly direccionServicio: string;
  readonly provincia: string | null;
  readonly canton: string | null;
  readonly parroquia: string | null;
  readonly planGeocodigo: string | null;
  readonly codigoTarifa: string | null;
  readonly nombreTarifa: string | null;
  readonly estadoServicio: string;
  readonly fechaRegistro: string;
}

export interface SuministrosPagina {
  readonly items: readonly SuministroResumen[];
  readonly total: number;
  readonly pagina: number;
  readonly tamanoPagina: number;
}

export interface SuministroDetalle extends SuministroResumen {
  readonly clienteTitularId: number;
  readonly clienteIdentificacion: string;
}

export interface CrearSuministro {
  readonly numeroSuministro: string;
  readonly numeroContrato: string;
  readonly cuen: string;
  readonly clienteIdentificacion: string;
  readonly direccionServicio: string;
  readonly provincia: string | null;
  readonly canton: string | null;
  readonly parroquia: string | null;
  readonly planGeocodigo: string | null;
  readonly codigoTarifa: string | null;
  readonly nombreTarifa: string | null;
}

export interface ActualizarSuministro extends CrearSuministro {
  readonly motivo: string;
}

export interface CambiarEstadoSuministro {
  readonly activo: boolean;
  readonly motivo: string;
}
