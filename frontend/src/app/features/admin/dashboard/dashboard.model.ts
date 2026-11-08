export interface PanelAdministrativo {
  indicadores: { clientesActivos: number; ordenesPendientes: number; cuadrillasDisponibles: number; reclamosAbiertos: number; slaPorVencer: number; slaVencidos: number };
  consumoMensual: { anio: number; mes: number; consumoKwh: number; totalFacturado: number }[];
  reconexionesPrioritarias: { id: number; codigo: string; numeroSuministro: string; titular: string; zona: string; estado: string; fechaLimiteSlaUtc: string | null }[];
  actividadReciente: { id: number; actor: string; accion: string; entidad: string; entidadId: string; motivo: string | null; fechaUtc: string }[];
}
export interface ResultadoBusquedaGlobal { tipo: string; titulo: string; subtitulo: string; ruta: string; }
