export interface ConfiguracionGeneral {
  id: number;
  nombreEmpresa: string;
  ruc: string;
  correoSoporte: string;
  telefonoSoporte: string;
  direccion: string;
  zonaHoraria: string;
  tamanoMaximoEvidenciaMb: number;
  notificacionesCorreoActivas: boolean;
  notificacionesWhatsAppActivas: boolean;
  fechaActualizacionUtc: string;
}
export interface PoliticaSla {
  id: number;
  version: number;
  tipoOrden: 'RECONEXION' | 'CORTE';
  tipoZona: 'URBANA' | 'RURAL' | 'TODAS';
  horasObjetivo: number;
  horaInicioLaboral: string;
  horaFinLaboral: string;
  incluyeFinDeSemana: boolean;
  vigenteDesdeUtc: string;
  vigenteHastaUtc: string | null;
  activa: boolean;
  motivo: string;
}
export interface ConfiguracionCompleta {
  general: ConfiguracionGeneral;
  politicasSla: PoliticaSla[];
}
export type ActualizarConfiguracion = Omit<ConfiguracionGeneral, 'id' | 'fechaActualizacionUtc'> & {
  motivo: string;
};
export interface CrearPoliticaSla {
  tipoOrden: string;
  tipoZona: string;
  horasObjetivo: number;
  horaInicioLaboral: string;
  horaFinLaboral: string;
  incluyeFinDeSemana: boolean;
  motivo: string;
}
