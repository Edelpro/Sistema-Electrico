import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { PanelAdministrativo, ResultadoBusquedaGlobal } from './dashboard.model';
@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly url = 'http://localhost:5029/api/PanelAdministrativo';
  obtener() { return this.http.get<PanelAdministrativo>(this.url); }
  buscar(termino: string) { return this.http.get<ResultadoBusquedaGlobal[]>(`${this.url}/buscar`, { params: new HttpParams().set('termino', termino) }); }
}
