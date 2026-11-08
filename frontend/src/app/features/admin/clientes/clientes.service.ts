import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  CambiarEstadoCliente,
  ClienteDetalle,
  ClienteGuardar,
  ClientesPagina,
} from './clientes.model';

interface ClientesConsulta {
  readonly buscar: string;
  readonly pagina: number;
  readonly tamanoPagina: number;
}

@Injectable({
  providedIn: 'root',
})
export class ClientesService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5029/api/Clientes';

  listar(consulta: ClientesConsulta): Observable<ClientesPagina> {
    let params = new HttpParams()
      .set('pagina', consulta.pagina)
      .set('tamanoPagina', consulta.tamanoPagina);

    if (consulta.buscar) {
      params = params.set('buscar', consulta.buscar);
    }

    return this.http.get<ClientesPagina>(this.apiUrl, { params });
  }

  obtener(id: number): Observable<ClienteDetalle> {
    return this.http.get<ClienteDetalle>(`${this.apiUrl}/${id}`);
  }

  crear(cliente: ClienteGuardar): Observable<ClienteDetalle> {
    return this.http.post<ClienteDetalle>(this.apiUrl, cliente);
  }

  actualizar(id: number, cliente: ClienteGuardar): Observable<ClienteDetalle> {
    return this.http.put<ClienteDetalle>(`${this.apiUrl}/${id}`, cliente);
  }

  cambiarEstado(id: number, solicitud: CambiarEstadoCliente): Observable<ClienteDetalle> {
    return this.http.patch<ClienteDetalle>(`${this.apiUrl}/${id}/estado`, solicitud);
  }
}
