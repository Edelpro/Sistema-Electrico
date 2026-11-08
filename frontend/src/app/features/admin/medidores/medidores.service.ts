import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  ActualizarMedidor,
  CambiarEstadoMedidor,
  CrearMedidor,
  MedidorDetalle,
  MedidoresPagina,
} from './medidores.model';

interface MedidoresConsulta {
  readonly buscar: string;
  readonly pagina: number;
  readonly tamanoPagina: number;
}

@Injectable({ providedIn: 'root' })
export class MedidoresService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5029/api/Medidores';

  listar(consulta: MedidoresConsulta): Observable<MedidoresPagina> {
    let params = new HttpParams()
      .set('pagina', consulta.pagina)
      .set('tamanoPagina', consulta.tamanoPagina);
    if (consulta.buscar) params = params.set('buscar', consulta.buscar);
    return this.http.get<MedidoresPagina>(this.apiUrl, { params });
  }

  obtener(id: number): Observable<MedidorDetalle> {
    return this.http.get<MedidorDetalle>(`${this.apiUrl}/${id}`);
  }

  crear(medidor: CrearMedidor): Observable<MedidorDetalle> {
    return this.http.post<MedidorDetalle>(this.apiUrl, medidor);
  }

  actualizar(id: number, medidor: ActualizarMedidor): Observable<MedidorDetalle> {
    return this.http.put<MedidorDetalle>(`${this.apiUrl}/${id}`, medidor);
  }

  cambiarEstado(id: number, solicitud: CambiarEstadoMedidor): Observable<MedidorDetalle> {
    return this.http.patch<MedidorDetalle>(`${this.apiUrl}/${id}/estado`, solicitud);
  }
}
