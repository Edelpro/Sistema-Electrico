import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { EvidenciaTecnica, PortalTecnicoResumen, TecnicoOrdenDetalle } from './dashboard.model';

@Injectable({ providedIn: 'root' })
export class PortalTecnicoService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5029/api/PortalTecnico';

  resumen(buscar = '', estado = 'TODOS'): Observable<PortalTecnicoResumen> {
    let params = new HttpParams();
    if (buscar.trim()) params = params.set('buscar', buscar.trim());
    if (estado !== 'TODOS') params = params.set('estado', estado);
    return this.http.get<PortalTecnicoResumen>(this.apiUrl, { params });
  }

  detalle(id: number): Observable<TecnicoOrdenDetalle> {
    return this.http.get<TecnicoOrdenDetalle>(`${this.apiUrl}/ordenes/${id}`);
  }

  transicionar(
    id: number,
    estadoNuevo: string,
    motivo: string,
    observacion: string,
  ): Observable<TecnicoOrdenDetalle> {
    return this.http.post<TecnicoOrdenDetalle>(`${this.apiUrl}/ordenes/${id}/transiciones`, {
      estadoNuevo,
      motivo,
      observacion: observacion.trim() || null,
    });
  }

  registrarEvidencia(id: number, datos: FormData): Observable<EvidenciaTecnica> {
    return this.http.post<EvidenciaTecnica>(`${this.apiUrl}/ordenes/${id}/evidencias`, datos);
  }

  descargarEvidencia(id: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/evidencias/${id}/archivo`, { responseType: 'blob' });
  }
}
