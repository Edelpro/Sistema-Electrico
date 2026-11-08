export interface OrdenResumen {
  id: number;
  codigo: string;
  numeroSuministro: string;
  titularActual: string;
  zona: string;
  tipo: string;
  causa: string;
  estado: string;
  prioridad: string;
  fechaLimiteSlaUtc: string | null;
  fechaCreacionUtc: string;
  tecnico: string | null;
  cuadrilla: string | null;
}
export interface OrdenHistorial {
  estadoAnterior: string | null;
  estadoNuevo: string;
  fechaUtc: string;
  motivo: string;
  actor: string | null;
}
export interface OrdenDetalle extends OrdenResumen {
  direccionServicio: string;
  fechaActualizacionUtc: string;
  activa: boolean;
  observacion: string | null;
  pagoId: number | null;
  historial: OrdenHistorial[];
}
export interface OrdenesPagina {
  items: OrdenResumen[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}
export interface CrearOrden {
  numeroSuministro: string;
  tipo: string;
  causa: string;
  prioridad: string;
  fechaLimiteSlaUtc: string | null;
  correoTecnico: string | null;
  codigoCuadrilla: string | null;
  motivo: string;
}
