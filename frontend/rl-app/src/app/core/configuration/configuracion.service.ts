import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { catchError, from, map, Observable, of, switchMap, tap } from 'rxjs';
import { ConfigSistema, LoginSlide } from './configuracion.models';
import { bestContrastForeground, createSystemTheme, DEFAULT_SYSTEM_ACCENT, DEFAULT_SYSTEM_PRIMARY, parseHexColor } from './system-theme.util';
import { createHttpErrorScopeContext, HttpErrorScope } from '../http/http-error-scope';
import { INSTITUTIONAL_LOGO_ASPECT_RATIO, setInstitutionalReportBranding } from '../reporting/institutional-report-standard';

interface ReportLogo {
  dataUrl: string;
  aspectRatio: number;
}

const NO_REPORT_LOGO: ReportLogo = {
  dataUrl: '',
  aspectRatio: INSTITUTIONAL_LOGO_ASPECT_RATIO
};

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
        switchMap(cfg => this.prepararLogoReporte(cfg).pipe(
          tap(logo => {
            this.configSistema.set(cfg);
            this.aplicarTemaSistema(cfg);
            this.aplicarBrandingReportes(cfg, logo);
          }),
          map(() => cfg)
        ))
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
        switchMap(response => this.prepararLogoReporte(config).pipe(
          tap(logo => {
            this.configSistema.set(config);
            this.aplicarTemaSistema(config);
            this.aplicarBrandingReportes(config, logo);
          }),
          map(() => response)
        ))
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

  private aplicarBrandingReportes(config: ConfigSistema, logo: ReportLogo): void {
    const header = parseHexColor(config.colorEncabezadoReportes) ?? parseHexColor('#123B63')!;
    const foreground = bestContrastForeground(header);
    setInstitutionalReportBranding({
      institutionName: (config.nombreInstitucion || 'Instituto Hondureño de Seguridad Social').toUpperCase(),
      systemName: config.nombreSistema || 'SGRLA-IHSS',
      logoUrl: logo.dataUrl,
      logoAspectRatio: logo.aspectRatio,
      headerColor: [header.r, header.g, header.b],
      headerTextColor: [foreground.r, foreground.g, foreground.b]
    });
  }

  private prepararLogoReporte(config: ConfigSistema): Observable<ReportLogo> {
    const url = config.logoUrl?.trim();
    if (!url || !/^\/uploads\/[A-Za-z0-9._-]+\.(png|jpg|jpeg|gif|webp)$/i.test(url))
      return of(NO_REPORT_LOGO);

    return this.http.get(`${this.apiUrl}/sistema/logo-reportes`, {
      responseType: 'blob',
      context: createHttpErrorScopeContext('local')
    }).pipe(
      switchMap(blob => from(this.convertirLogoEnPng(blob))),
      catchError(() => of(NO_REPORT_LOGO))
    );
  }

  private async convertirLogoEnPng(blob: Blob | null): Promise<ReportLogo> {
    if (!blob || !/^image\/(png|jpeg|gif|webp)$/i.test(blob.type) || blob.size === 0 ||
        blob.size > 10 * 1024 * 1024 || typeof Image === 'undefined' ||
        typeof document === 'undefined' || typeof URL.createObjectURL !== 'function')
      return NO_REPORT_LOGO;

    const objectUrl = URL.createObjectURL(blob);
    try {
      const img = new Image();
      const loaded = new Promise<void>((resolve, reject) => {
        img.onload = () => resolve();
        img.onerror = () => reject(new Error('No se pudo decodificar el logo configurado.'));
      });
      img.src = objectUrl;
      await loaded;

      const width = img.naturalWidth;
      const height = img.naturalHeight;
      if (width < 1 || height < 1 || width > 4096 || height > 4096 || width * height > 16_777_216)
        return NO_REPORT_LOGO;

      const canvas = document.createElement('canvas');
      canvas.width = width;
      canvas.height = height;
      const context = canvas.getContext('2d');
      if (!context) return NO_REPORT_LOGO;
      context.drawImage(img, 0, 0);
      return { dataUrl: canvas.toDataURL('image/png'), aspectRatio: width / height };
    } finally {
      URL.revokeObjectURL(objectUrl);
    }
  }

}
