export interface LoginRequest {
  correo: string;
  password: string;
}

export interface LoginResponse {
  usuarioId: number;
  nombreCompleto: string;
  correo: string;
  rol: string;
  token: string;
}

export interface MensajeResponse {
  mensaje: string;
}
