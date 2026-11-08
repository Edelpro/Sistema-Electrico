export type TipoMedicion = 'CONVENCIONAL' | 'INTELIGENTE' | 'OTRO';
export type EstadoMedidor = 'ACTIVO' | 'RETIRADO' | 'DEFECTUOSO' | 'REEMPLAZADO';

export interface MedidorResumen {
  readonly id: number;
  readonly numeroSerie: string;
  readonly tipoMedicion: TipoMedicion;
  readonly estado: EstadoMedidor;
  readonly fechaInstalacion: string;
  readonly fechaRetiro: string | null;
  readonly suministroId: number;
  readonly numeroSuministro: string;
  readonly numeroContrato: string;
  readonly cuen: string;
  readonly titularActual: string;
}

export interface MedidorDetalle extends MedidorResumen {
  readonly fechaRegistro: string;
}

export interface MedidoresPagina {
  readonly items: readonly MedidorResumen[];
  readonly total: number;
  readonly pagina: number;
  readonly tamanoPagina: number;
}

export interface CrearMedidor {
  readonly numeroSerie: string;
  readonly numeroSuministro: string;
  readonly tipoMedicion: TipoMedicion;
  readonly fechaInstalacion: string;
}

export interface ActualizarMedidor extends CrearMedidor {
  readonly motivo: string;
}

export interface CambiarEstadoMedidor {
  readonly estado: EstadoMedidor;
  readonly motivo: string;
}
