export interface TecnicoOrdenResumen {
  id: number;
  codigo: string;
  numeroSuministro: string;
  direccionServicio: string;
  zona: string;
  tipo: string;
  causa: string;
  estado: string;
  prioridad: string;
  fechaLimiteSlaUtc: string | null;
  fechaActualizacionUtc: string;
  tieneEvidencia: boolean;
}

export interface PortalTecnicoResumen {
  total: number;
  pendientes: number;
  enCurso: number;
  criticas: number;
  ordenes: TecnicoOrdenResumen[];
}

export interface HistorialOrden {
  estadoAnterior: string | null;
  estadoNuevo: string;
  fechaUtc: string;
  motivo: string;
  actor: string;
}

export interface EvidenciaTecnica {
  id: number;
  fechaDispositivoUtc: string;
  fechaServidorUtc: string;
  precisionMetros: number | null;
  lecturaMedidor: number | null;
  observacion: string;
  nombreArchivo: string;
}

export interface TecnicoOrdenDetalle {
  orden: TecnicoOrdenResumen;
  titular: string;
  observacion: string | null;
  transicionesPermitidas: string[];
  historial: HistorialOrden[];
  evidencias: EvidenciaTecnica[];
}
