export interface LoginSlide {
  id: number;
  imagenUrl: string;
  titulo?: string;
  descripcion?: string;
  orden: number;
  activo: boolean;
  imagenIcono?: string;
}

export interface ConfigSistema {
  nombreInstitucion: string;
  nombreSistema: string;
  logoUrl?: string;
  iconoUrl?: string;
  colorPrimario?: string;
  colorSecundario?: string;
  timeoutSesion: number;
  acuerdoLegal?: string;
  maxIntentos?: number;
}
