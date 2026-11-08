import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  FacturaDetalle,
  PortalUsuarioResumen,
  ReclamoPortal,
  SuministroDetalle,
} from './dashboard.model';

@Injectable({ providedIn: 'root' })
export class PortalUsuarioService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5029/api/PortalUsuario';

  resumen(): Observable<PortalUsuarioResumen> {
    return this.http.get<PortalUsuarioResumen>(this.apiUrl);
  }

  suministro(id: number): Observable<SuministroDetalle> {
    return this.http.get<SuministroDetalle>(`${this.apiUrl}/suministros/${id}`);
  }

  factura(id: number): Observable<FacturaDetalle> {
    return this.http.get<FacturaDetalle>(`${this.apiUrl}/facturas/${id}`);
  }

  crearReclamo(datos: {
    numeroSuministro: string;
    tipo: string;
    descripcion: string;
    pagoId: number | null;
    ordenTrabajoId: number | null;
  }): Observable<ReclamoPortal> {
    return this.http.post<ReclamoPortal>(`${this.apiUrl}/reclamos`, datos);
  }

  descargarEvidencia(id: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/evidencias/${id}/archivo`, { responseType: 'blob' });
  }
}
