export interface LoginRequest {
  email:    string;
  password: string;
}

export interface LoginResponse {
  accessToken:  string;
  refreshToken: string;
  expiresAt:    string;
  usuario:      UsuarioInfo;
}

export interface UsuarioInfo {
  id:       number;
  uid:      string;
  nombre:   string;
  apellido: string;
  email:    string;
  rol:      string;
  rolId:    number;
  esUsuarioDominio: number;
  usuarioDominio?:  string;
  dominio?:  string;
  dominioId?: number;
  dni?:      string;
  modulosIds?: number[];
  debeCambiarPassword?: boolean;
}

export interface TokenPayload {
  nameid?:   string;
  email?:    string;
  unique_name?: string;
  role?:     string;
  rol_id?:   string;
  uid?:      string;
  es_dom?:   string;
  exp:       number;
}
