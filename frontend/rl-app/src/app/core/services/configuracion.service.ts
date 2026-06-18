import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { map, tap } from 'rxjs';

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

@Injectable({
  providedIn: 'root'
})
export class ConfiguracionService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/configuracion`;

  // Signal global para la identidad del sistema
  configSistema = signal<ConfigSistema | null>(null);

  CargarConfiguracion() {
    return this.http.get<{ success: boolean, datos: ConfigSistema }>(`${this.apiUrl}/sistema`)
      .pipe(
        map(res => {
          const cfg = res.datos;
          if (!cfg.colorPrimario) cfg.colorPrimario = '#1e3a8a';
          if (!cfg.colorSecundario) cfg.colorSecundario = '#1d4ed8';
          if (!cfg.timeoutSesion || cfg.timeoutSesion <= 0) cfg.timeoutSesion = 30;
          return cfg;
        }),
        tap(cfg => {
          this.configSistema.set(cfg);
          if (cfg.colorPrimario) {
            const rgb = this.hexToRgb(cfg.colorPrimario);
            if (rgb) document.documentElement.style.setProperty('--ihss-primary-rgb', `${rgb.r} ${rgb.g} ${rgb.b}`);
          }
          if (cfg.colorSecundario) {
            const rgb = this.hexToRgb(cfg.colorSecundario);
            if (rgb) document.documentElement.style.setProperty('--ihss-accent-rgb', `${rgb.r} ${rgb.g} ${rgb.b}`);
          }
        })
      );
  }

  private hexToRgb(hex: string) {
    const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
    return result ? {
      r: parseInt(result[1], 16),
      g: parseInt(result[2], 16),
      b: parseInt(result[3], 16)
    } : null;
  }

  GuardarConfiguracion(config: ConfigSistema) {
    return this.http.put<{ success: boolean, mensaje: string }>(`${this.apiUrl}/sistema`, config)
      .pipe(
        tap(() => {
          this.configSistema.set(config);
          if (config.colorPrimario) {
            const rgb = this.hexToRgb(config.colorPrimario);
            if (rgb) document.documentElement.style.setProperty('--ihss-primary-rgb', `${rgb.r} ${rgb.g} ${rgb.b}`);
          }
          if (config.colorSecundario) {
            const rgb = this.hexToRgb(config.colorSecundario);
            if (rgb) document.documentElement.style.setProperty('--ihss-accent-rgb', `${rgb.r} ${rgb.g} ${rgb.b}`);
          }
        })
      );
  }

  ObtenerSlides() {
    return this.http.get<{ success: boolean, datos: LoginSlide[] }>(`${this.apiUrl}/login`)
      .pipe(
        map(res => res.datos)
      );
  }

  getTodosSlides() {
    return this.http.get<{ success: boolean, datos: LoginSlide[] }>(`${this.apiUrl}/slides`)
      .pipe(
        map(res => res.datos)
      );
  }

  crearSlide(slide: LoginSlide) {
    return this.http.post<{ success: boolean, mensaje: string }>(`${this.apiUrl}/slides`, slide);
  }

  actualizarSlide(id: number, slide: LoginSlide) {
    return this.http.put<{ success: boolean, mensaje: string }>(`${this.apiUrl}/slides/${id}`, slide);
  }

  eliminarSlide(id: number) {
    return this.http.delete<{ success: boolean, mensaje: string }>(`${this.apiUrl}/slides/${id}`);
  }

  subirImagen(file: File) {
    const formData = new FormData();
    formData.append('archivo', file);
    return this.http.post<{ success: boolean, url: string }>(`${this.apiUrl}/slides/upload`, formData);
  }

  resolverUrlImagen(url?: string): string {
    if (!url) return 'assets/login/slide1.png';
    if (url.startsWith('http://') || url.startsWith('https://')) return url;
    if (url.startsWith('assets/')) return url;
    const base = environment.apiUrl.replace('/api', '');
    return `${base}${url.startsWith('/') ? '' : '/'}${url}`;
  }
}
