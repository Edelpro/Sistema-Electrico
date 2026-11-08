import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  ActualizarUsuario,
  CambiarEstadoUsuario,
  CrearUsuario,
  CrearUsuarioResultado,
  EnvioInvitacionUsuario,
  UsuarioDetalle,
  UsuariosPagina,
} from './usuarios.model';

interface UsuariosConsulta {
  readonly buscar: string;
  readonly pagina: number;
  readonly tamanoPagina: number;
}

@Injectable({
  providedIn: 'root',
})
export class UsuariosService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5029/api/Usuarios';

  listar(consulta: UsuariosConsulta): Observable<UsuariosPagina> {
    let params = new HttpParams()
      .set('pagina', consulta.pagina)
      .set('tamanoPagina', consulta.tamanoPagina);

    if (consulta.buscar) {
      params = params.set('buscar', consulta.buscar);
    }

    return this.http.get<UsuariosPagina>(this.apiUrl, { params });
  }

  obtener(id: number): Observable<UsuarioDetalle> {
    return this.http.get<UsuarioDetalle>(`${this.apiUrl}/${id}`);
  }

  crear(usuario: CrearUsuario): Observable<CrearUsuarioResultado> {
    return this.http.post<CrearUsuarioResultado>(this.apiUrl, usuario);
  }

  actualizar(id: number, usuario: ActualizarUsuario): Observable<UsuarioDetalle> {
    return this.http.put<UsuarioDetalle>(`${this.apiUrl}/${id}`, usuario);
  }

  cambiarEstado(id: number, solicitud: CambiarEstadoUsuario): Observable<UsuarioDetalle> {
    return this.http.patch<UsuarioDetalle>(`${this.apiUrl}/${id}/estado`, solicitud);
  }

  reenviarInvitacion(id: number): Observable<EnvioInvitacionUsuario> {
    return this.http.post<EnvioInvitacionUsuario>(`${this.apiUrl}/${id}/invitacion`, {});
  }
}
