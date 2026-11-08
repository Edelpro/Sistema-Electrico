export interface SuministroResumen {
  id: number;
  numeroSuministro: string;
  numeroContrato: string;
  direccionServicio: string;
  estadoServicio: string;
  saldoPendiente: number;
  facturasPendientes: number;
  estadoOrdenActiva: string | null;
  fechaLimiteSlaUtc: string | null;
  reclamosAbiertos: number;
}

export interface PortalUsuarioResumen {
  nombreCompleto: string;
  cliente: string;
  suministros: SuministroResumen[];
}

export interface FacturaPortal {
  id: number;
  numeroFactura: string;
  fechaEmision: string;
  fechaVencimiento: string;
  periodoDesde: string;
  periodoHasta: string;
  consumoFacturado: number;
  unidadConsumo: string;
  estado: string;
  totalFactura: number;
  saldoPendiente: number;
}

export interface PagoPortal {
  id: number;
  referencia: string;
  monto: number;
  moneda: string;
  estado: string;
  fechaConfirmacionUtc: string;
}

export interface OrdenPortal {
  id: number;
  codigo: string;
  tipo: string;
  estado: string;
  prioridad: string;
  fechaCreacionUtc: string;
  fechaLimiteSlaUtc: string | null;
  evidenciaFechaUtc: string | null;
  evidenciaId: number | null;
}

export interface ReclamoPortal {
  id: number;
  numeroCaso: string;
  tipo: string;
  estado: string;
  prioridad: string;
  descripcion: string;
  resolucion: string | null;
  fechaCreacionUtc: string;
  fechaCierreUtc: string | null;
}

export interface EventoPortal {
  tipo: string;
  titulo: string;
  descripcion: string;
  fechaUtc: string;
  estado: string;
}

export interface SuministroDetalle {
  suministro: SuministroResumen;
  facturas: FacturaPortal[];
  pagos: PagoPortal[];
  ordenes: OrdenPortal[];
  reclamos: ReclamoPortal[];
  lineaTiempo: EventoPortal[];
}

export interface FacturaDetalle {
  id: number;
  numeroFactura: string;
  numeroSuministro: string;
  numeroContrato: string;
  titularActual: string;
  fechaEmision: string;
  fechaVencimiento: string;
  periodoDesde: string;
  periodoHasta: string;
  consumoFacturado: number;
  unidadConsumo: string;
  estado: string;
  moneda: string;
  lecturaAnterior: number;
  lecturaActual: number;
  subtotalServicioElectrico: number;
  subtotalAlumbradoPublico: number;
  otrosCargos: number;
  totalFactura: number;
  saldoPendiente: number;
  lineas: Array<{
    numeroLinea: number;
    descripcionOriginal: string;
    categoria: string;
    monto: number;
  }>;
}
