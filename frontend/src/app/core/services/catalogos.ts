import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, shareReplay } from 'rxjs';

import { CatalogosAsignacion } from '../models/catalogos.model';

@Injectable({ providedIn: 'root' })
export class CatalogosService {
  private readonly http = inject(HttpClient);
  private readonly url = 'http://localhost:5029/api/Catalogos/asignacion-operativa';
  private cache?: Observable<CatalogosAsignacion>;

  asignacionOperativa(refrescar = false): Observable<CatalogosAsignacion> {
    if (refrescar || !this.cache) {
      this.cache = this.http.get<CatalogosAsignacion>(this.url).pipe(shareReplay(1));
    }
    return this.cache;
  }
}
