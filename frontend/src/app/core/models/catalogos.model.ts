export interface PersonaCatalogo {
  id: number;
  nombre: string;
  correo: string;
}

export interface ClienteCatalogo {
  id: number;
  identificacion: string;
  tipoIdentificacion: string;
  nombre: string;
}

export interface CuadrillaCatalogo {
  id: number;
  codigo: string;
  nombre: string;
  zona: string;
  estado: string;
}

export interface SuministroCatalogo {
  id: number;
  numeroSuministro: string;
  titular: string;
  direccionServicio: string;
}

export interface MedidorCatalogo {
  id: number;
  numeroSerie: string;
  numeroSuministro: string;
  tipoMedicion: string;
}

export interface FacturaCatalogo {
  id: number;
  numeroFactura: string;
  numeroSuministro: string;
  saldoPendiente: number;
  estado: string;
}

export interface PagoCatalogo {
  id: number;
  referencia: string;
  numeroSuministro: string;
  monto: number;
  estado: string;
}

export interface OrdenCatalogo {
  id: number;
  codigo: string;
  numeroSuministro: string;
  tipo: string;
  estado: string;
}

export interface CatalogosAsignacion {
  clientes: ClienteCatalogo[];
  tecnicos: PersonaCatalogo[];
  administradores: PersonaCatalogo[];
  cuadrillas: CuadrillaCatalogo[];
  suministros: SuministroCatalogo[];
  medidores: MedidorCatalogo[];
  facturas: FacturaCatalogo[];
  pagos: PagoCatalogo[];
  ordenes: OrdenCatalogo[];
}
