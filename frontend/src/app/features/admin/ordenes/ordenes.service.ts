import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { CrearOrden, OrdenDetalle, OrdenesPagina } from './ordenes.model';
@Injectable({ providedIn: 'root' })
export class OrdenesService {
  private h = inject(HttpClient);
  private u = 'http://localhost:5029/api/OrdenesTrabajo';
  listar(buscar: string, pagina: number) {
    let p = new HttpParams().set('pagina', pagina).set('tamanoPagina', 10);
    if (buscar) p = p.set('buscar', buscar);
    return this.h.get<OrdenesPagina>(this.u, { params: p });
  }
  obtener(id: number) {
    return this.h.get<OrdenDetalle>(`${this.u}/${id}`);
  }
  crear(x: CrearOrden) {
    return this.h.post<OrdenDetalle>(this.u, x);
  }
  asignar(id: number, correoTecnico: string, codigoCuadrilla: string, motivo: string) {
    return this.h.post<OrdenDetalle>(`${this.u}/${id}/asignar`, {
      correoTecnico: correoTecnico || null,
      codigoCuadrilla: codigoCuadrilla || null,
      motivo,
    });
  }
  transicionar(id: number, estadoNuevo: string, motivo: string, observacion: string) {
    return this.h.post<OrdenDetalle>(`${this.u}/${id}/transiciones`, {
      estadoNuevo,
      motivo,
      observacion,
    });
  }
}
