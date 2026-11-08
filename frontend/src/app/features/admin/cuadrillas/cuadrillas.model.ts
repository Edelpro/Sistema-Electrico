export type EstadoCuadrilla = 'DISPONIBLE' | 'OCUPADA' | 'FUERA_SERVICIO' | 'INACTIVA';
export interface MiembroCuadrilla {
  id: number;
  usuarioId: number;
  nombre: string;
  correo: string;
  fechaAsignacionUtc: string;
}
export interface CuadrillaResumen {
  id: number;
  codigo: string;
  nombre: string;
  zona: string;
  capacidadMaxima: number;
  miembrosActivos: number;
  estado: EstadoCuadrilla;
  activa: boolean;
  ordenesActivas: number;
}
export interface CuadrillaDetalle extends CuadrillaResumen {
  observaciones: string | null;
  fechaCreacionUtc: string;
  fechaActualizacionUtc: string;
  miembros: MiembroCuadrilla[];
}
export interface CuadrillasPagina {
  items: CuadrillaResumen[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}
export interface GuardarCuadrilla {
  codigo: string;
  nombre: string;
  zona: string;
  capacidadMaxima: number;
  estado: EstadoCuadrilla;
  observaciones: string | null;
  motivo: string;
}
