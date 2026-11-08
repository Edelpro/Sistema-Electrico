import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { CrearReclamo, ReclamoDetalle, ReclamosPagina } from './reclamos.model';
@Injectable({ providedIn: 'root' })
export class ReclamosService {
  private h = inject(HttpClient);
  private u = 'http://localhost:5029/api/Reclamos';
  listar(buscar: string, pagina: number) {
    let p = new HttpParams().set('pagina', pagina).set('tamanoPagina', 10);
    if (buscar) p = p.set('buscar', buscar);
    return this.h.get<ReclamosPagina>(this.u, { params: p });
  }
  obtener(id: number) {
    return this.h.get<ReclamoDetalle>(`${this.u}/${id}`);
  }
  crear(x: CrearReclamo) {
    return this.h.post<ReclamoDetalle>(this.u, x);
  }
  gestionar(
    id: number,
    estadoNuevo: string,
    motivo: string,
    resolucion: string,
    correoAsignado: string,
  ) {
    return this.h.post<ReclamoDetalle>(`${this.u}/${id}/gestionar`, {
      estadoNuevo,
      motivo,
      resolucion: resolucion || null,
      correoAsignado: correoAsignado || null,
    });
  }
}
