import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  ActualizarSuministro,
  CambiarEstadoSuministro,
  CrearSuministro,
  SuministroDetalle,
  SuministrosPagina,
} from './suministros.model';

interface SuministrosConsulta {
  readonly buscar: string;
  readonly pagina: number;
  readonly tamanoPagina: number;
}

@Injectable({
  providedIn: 'root',
})
export class SuministrosService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5029/api/Suministros';

  listar(consulta: SuministrosConsulta): Observable<SuministrosPagina> {
    let params = new HttpParams()
      .set('pagina', consulta.pagina)
      .set('tamanoPagina', consulta.tamanoPagina);

    if (consulta.buscar) {
      params = params.set('buscar', consulta.buscar);
    }

    return this.http.get<SuministrosPagina>(this.apiUrl, { params });
  }

  obtener(id: number): Observable<SuministroDetalle> {
    return this.http.get<SuministroDetalle>(`${this.apiUrl}/${id}`);
  }

  crear(suministro: CrearSuministro): Observable<SuministroDetalle> {
    return this.http.post<SuministroDetalle>(this.apiUrl, suministro);
  }

  actualizar(id: number, suministro: ActualizarSuministro): Observable<SuministroDetalle> {
    return this.http.put<SuministroDetalle>(`${this.apiUrl}/${id}`, suministro);
  }

  cambiarEstado(id: number, solicitud: CambiarEstadoSuministro): Observable<SuministroDetalle> {
    return this.http.patch<SuministroDetalle>(`${this.apiUrl}/${id}/estado`, solicitud);
  }
}
