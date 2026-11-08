import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

import { LoginRequest, LoginResponse, MensajeResponse } from '../models/auth.model';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly http = inject(HttpClient);

  private readonly apiUrl = 'http://localhost:5029/api/Auth';

  login(datos: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, datos).pipe(
      tap((respuesta) => {
        try {
          sessionStorage.setItem('token', respuesta.token);

          sessionStorage.setItem('usuario', JSON.stringify(respuesta));
        } catch {
          throw new Error('SESSION_STORAGE_UNAVAILABLE');
        }
      }),
    );
  }

  solicitarRecuperacion(correo: string): Observable<MensajeResponse> {
    return this.http.post<MensajeResponse>(`${this.apiUrl}/solicitar-recuperacion`, { correo });
  }

  restablecerPassword(datos: {
    correo: string;
    token: string;
    nuevaPassword: string;
  }): Observable<MensajeResponse> {
    return this.http.post<MensajeResponse>(`${this.apiUrl}/restablecer-password`, datos);
  }

  getToken(): string | null {
    try {
      return sessionStorage.getItem('token');
    } catch {
      return null;
    }
  }

  getUsuario(): LoginResponse | null {
    let usuario: string | null;

    try {
      usuario = sessionStorage.getItem('usuario');
    } catch {
      return null;
    }

    if (!usuario) {
      return null;
    }

    try {
      return JSON.parse(usuario) as LoginResponse;
    } catch {
      this.logout();
      return null;
    }
  }

  estaAutenticado(): boolean {
    return !!this.getToken();
  }

  logout(): void {
    try {
      sessionStorage.removeItem('token');
      sessionStorage.removeItem('usuario');
    } catch {
      // The local session is already inaccessible; nothing else to clear.
    }
  }
}
