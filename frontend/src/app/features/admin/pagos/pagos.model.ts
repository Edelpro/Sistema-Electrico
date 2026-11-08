export interface PagoResumen {
  id: number;
  numeroSuministro: string;
  titularActual: string;
  proveedor: string;
  referencia: string;
  monto: number;
  estado: string;
  fechaConfirmacionUtc: string;
  facturasAplicadas: number;
}
export interface PagoAsignacion {
  facturaId: number;
  numeroFactura: string;
  montoAplicado: number;
}
export interface PagoDetalle extends PagoResumen {
  eventoProveedorId: string;
  moneda: string;
  fechaProveedorUtc: string;
  fechaRecepcionUtc: string;
  motivoConciliacion: string;
  asignaciones: PagoAsignacion[];
}
export interface PagosPagina {
  items: PagoResumen[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}
export interface ConciliarPago {
  numeroSuministro: string;
  proveedor: string;
  eventoProveedorId: string;
  referencia: string;
  monto: number;
  fechaProveedorUtc: string;
  motivo: string;
  asignaciones: { numeroFactura: string; montoAplicado: number }[];
}
export interface ResultadoConciliacion {
  pago: PagoDetalle;
  yaExistia: boolean;
  resultadoOperativo: {
    tipo: string;
    mensaje: string;
    ordenId: number | null;
    codigoOrden: string | null;
  };
}
