export type EstadoReclamo = 'ABIERTO' | 'EN_REVISION' | 'RESUELTO' | 'CERRADO' | 'CANCELADO';
export interface ReclamoResumen {
  id: number;
  numeroCaso: string;
  numeroSuministro: string;
  titularActual: string;
  tipo: string;
  estado: EstadoReclamo;
  prioridad: string;
  fechaCreacionUtc: string;
  asignadoA: string | null;
}
export interface ReclamoHistorial {
  estadoAnterior: string | null;
  estadoNuevo: string;
  motivo: string;
  fechaUtc: string;
  actor: string | null;
}
export interface ReclamoDetalle extends ReclamoResumen {
  direccionServicio: string;
  descripcion: string;
  resolucion: string | null;
  pagoId: number | null;
  ordenTrabajoId: number | null;
  fechaActualizacionUtc: string;
  fechaCierreUtc: string | null;
  historial: ReclamoHistorial[];
}
export interface ReclamosPagina {
  items: ReclamoResumen[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}
export interface CrearReclamo {
  numeroSuministro: string;
  tipo: string;
  prioridad: string;
  descripcion: string;
  pagoId: number | null;
  ordenTrabajoId: number | null;
  correoAsignado: string | null;
}
