export type EstadoFactura = 'EMITIDA' | 'PARCIALMENTE_PAGADA' | 'PAGADA' | 'VENCIDA' | 'ANULADA';

export type MetodoLectura = 'REAL' | 'ESTIMADA' | 'PROMEDIO' | 'REMOTA' | 'AJUSTADA';

export type CategoriaFactura =
  | 'ENERGIA'
  | 'COMERCIALIZACION'
  | 'ALUMBRADO_PUBLICO'
  | 'DESCUENTO'
  | 'SUBSIDIO'
  | 'INTERES_MORA'
  | 'SALDO_ANTERIOR'
  | 'TERCERO'
  | 'FINANCIAMIENTO'
  | 'CREDITO'
  | 'OTRO';

export interface FacturaResumen {
  readonly id: number;
  readonly numeroFactura: string;
  readonly numeroSuministro: string;
  readonly numeroContrato: string;
  readonly titularActual: string;
  readonly fechaEmision: string;
  readonly fechaVencimiento: string;
  readonly periodoDesde: string;
  readonly periodoHasta: string;
  readonly consumoFacturado: number;
  readonly estado: EstadoFactura;
  readonly totalFactura: number;
  readonly saldoPendiente: number;
  readonly tieneDiferencia: boolean;
}

export interface FacturaLinea {
  readonly numeroLinea: number;
  readonly codigoOriginal: string;
  readonly descripcionOriginal: string;
  readonly categoria: CategoriaFactura;
  readonly monto: number;
  readonly esIngresoElectrico: boolean;
}

export interface FacturaDetalle extends FacturaResumen {
  readonly cuen: string;
  readonly numeroMedidor: string | null;
  readonly sistemaOrigen: string;
  readonly identificadorExterno: string;
  readonly nombreEmisor: string;
  readonly rucEmisor: string;
  readonly moneda: string;
  readonly metodoLectura: MetodoLectura;
  readonly lecturaAnterior: number;
  readonly lecturaActual: number;
  readonly unidadConsumo: string;
  readonly subtotalServicioElectrico: number;
  readonly subtotalAlumbradoPublico: number;
  readonly otrosCargos: number;
  readonly saldoAnterior: number;
  readonly recaudacionTerceros: number;
  readonly subsidios: number;
  readonly sincronizadaEnUtc: string;
  readonly lineas: readonly FacturaLinea[];
}

export interface FacturasPagina {
  readonly items: readonly FacturaResumen[];
  readonly total: number;
  readonly pagina: number;
  readonly tamanoPagina: number;
}

export interface ImportarFacturaLinea {
  readonly codigoOriginal: string;
  readonly descripcionOriginal: string;
  readonly categoria: CategoriaFactura;
  readonly monto: number;
  readonly esIngresoElectrico: boolean;
}

export interface ImportarFactura {
  readonly numeroSuministro: string;
  readonly numeroMedidor: string;
  readonly sistemaOrigen: string;
  readonly identificadorExterno: string;
  readonly numeroFactura: string;
  readonly nombreEmisor: string;
  readonly rucEmisor: string;
  readonly fechaEmision: string;
  readonly fechaVencimiento: string;
  readonly estado: EstadoFactura;
  readonly periodoDesde: string;
  readonly periodoHasta: string;
  readonly metodoLectura: MetodoLectura;
  readonly lecturaAnterior: number;
  readonly lecturaActual: number;
  readonly consumoFacturado: number;
  readonly subtotalServicioElectrico: number;
  readonly subtotalAlumbradoPublico: number;
  readonly otrosCargos: number;
  readonly saldoAnterior: number;
  readonly recaudacionTerceros: number;
  readonly subsidios: number;
  readonly totalFactura: number;
  readonly saldoPendiente: number;
  readonly lineas: readonly ImportarFacturaLinea[];
}

export interface ResultadoImportacionFactura {
  readonly factura: FacturaDetalle;
  readonly yaExistia: boolean;
}
