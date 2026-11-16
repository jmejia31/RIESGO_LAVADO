import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { map, tap } from 'rxjs';
import { ConfigSistema, LoginSlide } from './configuracion.models';
import { bestContrastForeground, createSystemTheme, DEFAULT_SYSTEM_ACCENT, DEFAULT_SYSTEM_PRIMARY, parseHexColor } from './system-theme.util';
import { createHttpErrorScopeContext, HttpErrorScope } from '../http/http-error-scope';
import { INSTITUTIONAL_LOGO_ASSET_PATH, setInstitutionalReportBranding } from '../reporting/institutional-report-standard';

@Injectable({
  providedIn: 'root'
})
export class ConfiguracionService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/configuracion`;

  // Signal global para la identidad del sistema
  configSistema = signal<ConfigSistema | null>(null);
  private ultimoColorPrimarioValido = DEFAULT_SYSTEM_PRIMARY;
  private ultimoColorSecundarioValido = DEFAULT_SYSTEM_ACCENT;

  CargarConfiguracion(errorScope: HttpErrorScope = 'global') {
    return this.http.get<{ success: boolean, datos: ConfigSistema }>(`${this.apiUrl}/sistema`, {
      context: createHttpErrorScopeContext(errorScope)
    })
      .pipe(
        map(res => {
          const cfg = res.datos;
          if (!cfg.colorPrimario) cfg.colorPrimario = DEFAULT_SYSTEM_PRIMARY;
          if (!cfg.colorSecundario) cfg.colorSecundario = DEFAULT_SYSTEM_ACCENT;
          if (!cfg.colorEncabezadoReportes) cfg.colorEncabezadoReportes = '#123B63';
          if (!cfg.timeoutSesion || cfg.timeoutSesion <= 0) cfg.timeoutSesion = 30;
          return cfg;
        }),
        tap(cfg => {
          this.configSistema.set(cfg);
          this.aplicarTemaSistema(cfg);
          this.aplicarBrandingReportes(cfg);
        })
      );
  }

  private aplicarTemaSistema(config: ConfigSistema): void {
    if (parseHexColor(config.colorPrimario)) this.ultimoColorPrimarioValido = config.colorPrimario!;
    if (parseHexColor(config.colorSecundario)) this.ultimoColorSecundarioValido = config.colorSecundario!;
    const tema = createSystemTheme(this.ultimoColorPrimarioValido, this.ultimoColorSecundarioValido);
    const root = document.documentElement;
    for (const [property, value] of Object.entries(tema.properties)) root.style.setProperty(property, value);
  }

  GuardarConfiguracion(config: ConfigSistema) {
    return this.http.put<{ success: boolean, mensaje: string }>(`${this.apiUrl}/sistema`, config, {
      context: createHttpErrorScopeContext('local')
    })
      .pipe(
        tap(() => {
          this.configSistema.set(config);
          this.aplicarTemaSistema(config);
          this.aplicarBrandingReportes(config);
        })
      );
  }

  ObtenerSlides() {
    return this.http.get<{ success: boolean, datos: LoginSlide[] }>(`${this.apiUrl}/login`, {
      context: createHttpErrorScopeContext('local')
    })
      .pipe(
        map(res => res.datos)
      );
  }

  getTodosSlides() {
    return this.http.get<{ success: boolean, datos: LoginSlide[] }>(`${this.apiUrl}/slides`, {
      context: createHttpErrorScopeContext('local')
    })
      .pipe(
        map(res => res.datos)
      );
  }

  crearSlide(slide: LoginSlide) {
    return this.http.post<{ success: boolean, mensaje: string }>(`${this.apiUrl}/slides`, slide, {
      context: createHttpErrorScopeContext('local')
    });
  }

  actualizarSlide(id: number, slide: LoginSlide) {
    return this.http.put<{ success: boolean, mensaje: string }>(`${this.apiUrl}/slides/${id}`, slide, {
      context: createHttpErrorScopeContext('local')
    });
  }

  eliminarSlide(id: number) {
    return this.http.delete<{ success: boolean, mensaje: string }>(`${this.apiUrl}/slides/${id}`, {
      context: createHttpErrorScopeContext('local')
    });
  }

  subirImagen(file: File) {
    const formData = new FormData();
    formData.append('archivo', file);
    return this.http.post<{ success: boolean, url: string }>(`${this.apiUrl}/slides/upload`, formData, {
      context: createHttpErrorScopeContext('local')
    });
  }

  resolverUrlImagen(url?: string): string {
    if (!url) return 'assets/login/slide1.png';
    if (url.startsWith('http://') || url.startsWith('https://')) return url;
    if (url.startsWith('assets/')) return url;
    const base = environment.apiUrl.replace('/api', '');
    return `${base}${url.startsWith('/') ? '' : '/'}${url}`;
  }

  private aplicarBrandingReportes(config: ConfigSistema): void {
    const header = parseHexColor(config.colorEncabezadoReportes) ?? parseHexColor('#123B63')!;
    const foreground = bestContrastForeground(header);
    const logoUrl = this.resolverLogoParaReportes(config.logoUrl);
    setInstitutionalReportBranding({
      institutionName: (config.nombreInstitucion || 'Instituto Hondureño de Seguridad Social').toUpperCase(),
      systemName: config.nombreSistema || 'SGRLA-IHSS',
      logoUrl,
      headerColor: [header.r, header.g, header.b],
      headerTextColor: [foreground.r, foreground.g, foreground.b]
    });
  }

  /**
   * La reportería del navegador necesita un recurso local embebible. El backend
   * no descarga URLs externas por seguridad; mantener la misma regla aquí evita
   * que PDF/Excel fallen aunque el logo remoto se vea en una etiqueta <img>.
   */
  private resolverLogoParaReportes(url?: string | null): string {
    if (!url || /^(https?:|data:)/i.test(url)) return INSTITUTIONAL_LOGO_ASSET_PATH;
    return this.resolverUrlImagen(url);
  }
}
