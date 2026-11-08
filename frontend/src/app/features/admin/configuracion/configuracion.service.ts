import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import {
  ActualizarConfiguracion,
  ConfiguracionCompleta,
  ConfiguracionGeneral,
  CrearPoliticaSla,
  PoliticaSla,
} from './configuracion.model';

@Injectable({ providedIn: 'root' })
export class ConfiguracionService {
  private readonly http = inject(HttpClient);
  private readonly url = 'http://localhost:5029/api/Configuracion';
  obtener() { return this.http.get<ConfiguracionCompleta>(this.url); }
  actualizarGeneral(dto: ActualizarConfiguracion) {
    return this.http.put<ConfiguracionGeneral>(`${this.url}/general`, dto);
  }
  crearPolitica(dto: CrearPoliticaSla) {
    return this.http.post<PoliticaSla>(`${this.url}/politicas-sla`, dto);
  }
  desactivarPolitica(id: number, motivo: string) {
    return this.http.patch<PoliticaSla>(`${this.url}/politicas-sla/${id}/estado`, { activa: false, motivo });
  }
}
