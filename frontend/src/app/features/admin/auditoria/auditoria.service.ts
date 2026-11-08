import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { AuditoriaPagina, ReporteOperativo } from './auditoria.model';
@Injectable({ providedIn: 'root' })
export class AuditoriaService {
  private readonly http = inject(HttpClient); private readonly url = 'http://localhost:5029/api/Auditoria';
  listar(buscar: string, pagina: number, desde: string, hasta: string) {
    let p = new HttpParams().set('pagina', pagina).set('tamanoPagina', 10);
    if (buscar) p = p.set('buscar', buscar); if (desde) p = p.set('desdeUtc', new Date(`${desde}T00:00:00-05:00`).toISOString());
    if (hasta) p = p.set('hastaUtc', new Date(`${hasta}T23:59:59-05:00`).toISOString());
    return this.http.get<AuditoriaPagina>(this.url, { params: p });
  }
  resumen() { return this.http.get<ReporteOperativo>(`${this.url}/resumen`); }
  exportar(desde: string, hasta: string) {
    let p = new HttpParams(); if (desde) p = p.set('desdeUtc', new Date(`${desde}T00:00:00-05:00`).toISOString());
    if (hasta) p = p.set('hastaUtc', new Date(`${hasta}T23:59:59-05:00`).toISOString());
    return this.http.get(`${this.url}/exportar`, { params: p, responseType: 'blob', observe: 'response' });
  }
}
