export interface EventoAuditoriaResumen { id: number; actor: string; accion: string; tipoEntidad: string; entidadId: string; motivo: string | null; ocurridoEnUtc: string; }
export interface AuditoriaPagina { items: EventoAuditoriaResumen[]; total: number; pagina: number; tamanoPagina: number; }
export interface ReporteOperativo { clientesActivos: number; suministrosActivos: number; facturasPendientes: number; saldoPendiente: number; ordenesActivas: number; ordenesSlaVencido: number; reclamosAbiertos: number; pagosConfirmadosMes: number; generadoEnUtc: string; }
