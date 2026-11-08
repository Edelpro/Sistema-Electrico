import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ConciliarPago, PagoDetalle, PagosPagina, ResultadoConciliacion } from './pagos.model';

@Injectable({ providedIn: 'root' })
export class PagosService {
  private http = inject(HttpClient);
  private url = 'http://localhost:5029/api/Pagos';
  listar(buscar: string, pagina: number): Observable<PagosPagina> {
    let p = new HttpParams().set('pagina', pagina).set('tamanoPagina', 10);
    if (buscar) p = p.set('buscar', buscar);
    return this.http.get<PagosPagina>(this.url, { params: p });
  }
  obtener(id: number) {
    return this.http.get<PagoDetalle>(`${this.url}/${id}`);
  }
  conciliar(dto: ConciliarPago) {
    return this.http.post<ResultadoConciliacion>(`${this.url}/conciliar`, dto);
  }
}
