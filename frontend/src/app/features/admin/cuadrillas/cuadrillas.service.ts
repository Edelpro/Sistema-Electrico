import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { CuadrillaDetalle, CuadrillasPagina, GuardarCuadrilla } from './cuadrillas.model';
@Injectable({ providedIn: 'root' })
export class CuadrillasService {
  private readonly http = inject(HttpClient);
  private readonly url = 'http://localhost:5029/api/Cuadrillas';
  listar(buscar: string, pagina: number) {
    let p = new HttpParams().set('pagina', pagina).set('tamanoPagina', 10);
    if (buscar) p = p.set('buscar', buscar);
    return this.http.get<CuadrillasPagina>(this.url, { params: p });
  }
  obtener(id: number) {
    return this.http.get<CuadrillaDetalle>(`${this.url}/${id}`);
  }
  crear(dto: GuardarCuadrilla) {
    return this.http.post<CuadrillaDetalle>(this.url, dto);
  }
  actualizar(id: number, dto: GuardarCuadrilla) {
    return this.http.put<CuadrillaDetalle>(`${this.url}/${id}`, dto);
  }
  cambiarEstado(id: number, estado: string, motivo: string) {
    return this.http.post<CuadrillaDetalle>(`${this.url}/${id}/estado`, { estado, motivo });
  }
  agregarMiembro(id: number, correoTecnico: string, motivo: string) {
    return this.http.post<CuadrillaDetalle>(`${this.url}/${id}/miembros`, {
      correoTecnico,
      motivo,
    });
  }
  retirarMiembro(id: number, miembroId: number, motivo: string) {
    return this.http.post<CuadrillaDetalle>(`${this.url}/${id}/miembros/${miembroId}/retirar`, {
      motivo,
    });
  }
}
