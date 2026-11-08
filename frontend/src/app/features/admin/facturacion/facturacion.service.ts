import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  FacturaDetalle,
  FacturasPagina,
  ImportarFactura,
  ResultadoImportacionFactura,
} from './facturacion.model';

interface FacturasConsulta {
  readonly buscar: string;
  readonly pagina: number;
  readonly tamanoPagina: number;
}

@Injectable({ providedIn: 'root' })
export class FacturacionService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5029/api/Facturas';

  listar(consulta: FacturasConsulta): Observable<FacturasPagina> {
    let params = new HttpParams()
      .set('pagina', consulta.pagina)
      .set('tamanoPagina', consulta.tamanoPagina);
    if (consulta.buscar) params = params.set('buscar', consulta.buscar);
    return this.http.get<FacturasPagina>(this.apiUrl, { params });
  }

  obtener(id: number): Observable<FacturaDetalle> {
    return this.http.get<FacturaDetalle>(`${this.apiUrl}/${id}`);
  }

  importar(factura: ImportarFactura): Observable<ResultadoImportacionFactura> {
    return this.http.post<ResultadoImportacionFactura>(`${this.apiUrl}/importar`, factura);
  }
}
