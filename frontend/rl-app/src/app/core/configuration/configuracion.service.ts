import { Injectable, inject, signal } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { catchError, from, map, Observable, of, switchMap, tap } from 'rxjs';
import { ConfigSistema, LoginSlide } from './configuracion.models';
import { bestContrastForeground, createSystemTheme, DEFAULT_SYSTEM_ACCENT, DEFAULT_SYSTEM_PRIMARY, parseHexColor } from './system-theme.util';
import { createHttpErrorScopeContext, HttpErrorScope } from '../http/http-error-scope';
import { CONFIRMACION_CAMBIOS_HEADER } from '../interceptors/confirmacion-cambios.interceptor';
import { setInstitutionalReportBranding } from '../reporting/institutional-report-standard';

export interface ReportLogo {
  dataUrl: string;
  aspectRatio: number;
  status: 'sin-logo' | 'disponible' | 'invalido' | 'inaccesible';
  errorMessage?: string;
}

export const NO_REPORT_LOGO: ReportLogo = {
  dataUrl: '',
  aspectRatio: 1,
  status: 'sin-logo'
};

@Injectable({
  providedIn: 'root'
})
export class ConfiguracionService {
  private http = inject(HttpClient);
  private titleService = inject(Title);
  private apiUrl = `${environment.apiUrl}/configuracion`;

  // Signal global para la identidad del sistema
  configSistema = signal<ConfigSistema | null>(null);
  private ultimoColorPrimarioValido = DEFAULT_SYSTEM_PRIMARY;
  private ultimoColorSecundarioValido = DEFAULT_SYSTEM_ACCENT;
  private secuenciaOperacion = 0;
  private versionFavicon = 0;

  CargarConfiguracion(errorScope: HttpErrorScope = 'global') {
    const operacionActual = ++this.secuenciaOperacion;
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
          if (operacionActual < this.secuenciaOperacion) return;
          this.configSistema.set(cfg);
          this.aplicarTemaSistema(cfg);
          this.aplicarMetadatosNavegador(cfg);
        }),
        switchMap(cfg => this.prepararLogoReporte(cfg).pipe(
          tap(logo => {
            if (operacionActual < this.secuenciaOperacion) return;
            this.aplicarBrandingReportes(cfg, logo);
          }),
          map(() => cfg),
          catchError(() => of(cfg))
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

  GuardarConfiguracion(config: ConfigSistema, confirmadaInstitucionalmente = false) {
    const operacionActual = ++this.secuenciaOperacion;
    this.versionFavicon++;
    // Only a caller that already obtained an explicit institutional confirmation may
    // suppress the global HTTP interceptor's additional confirmation. All other callers
    // retain the default fail-closed confirmation behavior.
    return this.http.put<{ success: boolean, mensaje: string }>(`${this.apiUrl}/sistema`, config, {
      ...(confirmadaInstitucionalmente ? { headers: { [CONFIRMACION_CAMBIOS_HEADER]: '1' } } : {}),
      context: createHttpErrorScopeContext('local')
    })
      .pipe(
        tap(() => {
          if (operacionActual < this.secuenciaOperacion) return;
          this.configSistema.set(config);
          this.aplicarTemaSistema(config);
          this.aplicarMetadatosNavegador(config);
        }),
        switchMap(response => this.prepararLogoReporte(config).pipe(
          tap(logo => {
            if (operacionActual < this.secuenciaOperacion) return;
            this.aplicarBrandingReportes(config, logo);
          }),
          map(() => response),
          catchError(() => of(response))
        ))
      );
  }

  obtenerUrlFaviconPublicado(): string {
    const icono = this.configSistema()?.iconoUrl?.trim();
    return icono
      ? `${this.apiUrl}/sistema/favicon?v=${this.generarHuellaFavicon(icono)}`
      : 'favicon.ico';
  }

  private aplicarMetadatosNavegador(config: ConfigSistema): void {
    this.aplicarTituloInstitucional(config);
    this.aplicarFaviconInstitucional(config);
  }

  private aplicarTituloInstitucional(config: ConfigSistema): void {
    const SUFIJO_TITULO = 'Gestión de Riesgos LA/FT';
    const NOMBRE_DEFECTO = 'SGRLA-IHSS';
    const nombre = config?.nombreSistema?.trim();
    const nombreLimpio = nombre && nombre.length > 0 ? nombre : NOMBRE_DEFECTO;
    const tituloFinal = `${nombreLimpio} | ${SUFIJO_TITULO}`;

    if (this.titleService.getTitle() !== tituloFinal) {
      this.titleService.setTitle(tituloFinal);
    }
  }

  private aplicarFaviconInstitucional(config: ConfigSistema): void {
    if (typeof document === 'undefined') return;

    const icono = config?.iconoUrl?.trim();
    const doc = document;
    const links = Array.from(doc.head.querySelectorAll<HTMLLinkElement>('link[rel*="icon"]'));

    if (!icono) {
      const faviconDefecto = 'favicon.ico';
      const linkActual = links[0];
      if (linkActual && linkActual.getAttribute('href') === faviconDefecto && linkActual.type === 'image/x-icon' && links.length === 1) {
        return;
      }
      links.forEach(l => l.remove());
      const defaultLink = doc.createElement('link');
      defaultLink.rel = 'icon';
      defaultLink.type = 'image/x-icon';
      defaultLink.href = faviconDefecto;
      doc.head.appendChild(defaultLink);
      return;
    }

    const mimeType = this.obtenerMimeFavicon(icono);
    const v = this.generarHuellaFavicon(icono);
    const targetHref = `${this.apiUrl}/sistema/favicon?v=${v}`;

    const primerLink = links[0];
    if (primerLink && primerLink.getAttribute('href') === targetHref && primerLink.type === mimeType && links.length === 1) {
      return;
    }

    const nuevoLink = doc.createElement('link');
    nuevoLink.rel = 'icon';
    nuevoLink.type = mimeType;
    nuevoLink.href = targetHref;

    links.forEach(l => l.remove());
    doc.head.appendChild(nuevoLink);
  }

  private obtenerMimeFavicon(url: string): string {
    const lower = url.toLowerCase();
    if (lower.endsWith('.png')) return 'image/png';
    if (lower.endsWith('.jpg') || lower.endsWith('.jpeg')) return 'image/jpeg';
    if (lower.endsWith('.gif')) return 'image/gif';
    if (lower.endsWith('.webp')) return 'image/webp';
    return 'image/x-icon';
  }

  private generarHuellaFavicon(url: string): string {
    let hash = 0;
    for (let i = 0; i < url.length; i++) {
      hash = ((hash << 5) - hash) + url.charCodeAt(i);
      hash |= 0;
    }
    return `${Math.abs(hash)}_${this.versionFavicon}`;
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
      headers: { [CONFIRMACION_CAMBIOS_HEADER]: '1' },
      context: createHttpErrorScopeContext('local')
    });
  }

  subirFavicon(file: File) {
    const formData = new FormData();
    formData.append('archivo', file);
    return this.http.post<{ success: boolean; url: string; mimeType?: string; status?: string; mensaje?: string }>(
      `${this.apiUrl}/sistema/favicon/upload`,
      formData,
      {
        headers: { [CONFIRMACION_CAMBIOS_HEADER]: '1' },
        context: createHttpErrorScopeContext('local')
      }
    );
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
      logoStatus: logo.status,
      logoErrorMessage: logo.errorMessage,
      headerColor: [header.r, header.g, header.b],
      headerTextColor: [foreground.r, foreground.g, foreground.b]
    });
  }

  private clasificarOrigenLogo(rawUrl?: string): 'SIN_LOGO' | 'LOGO_LOCAL' | 'LOGO_HTTPS' | 'LOGO_INVALIDO' {
    if (!rawUrl || !rawUrl.trim()) return 'SIN_LOGO';
    const url = rawUrl.trim();
    if (/^\/?uploads\/[A-Za-z0-9._-]+\.(png|jpg|jpeg|gif|webp)$/i.test(url)) return 'LOGO_LOCAL';
    if (/^https:\/\/[A-Za-z0-9.-]+(:\d+)?(\/.*)?$/i.test(url)) return 'LOGO_HTTPS';
    return 'LOGO_INVALIDO';
  }

  private prepararLogoReporte(config: ConfigSistema): Observable<ReportLogo> {
    const origen = this.clasificarOrigenLogo(config.logoUrl);
    if (origen === 'SIN_LOGO') {
      return of(NO_REPORT_LOGO);
    }
    if (origen === 'LOGO_INVALIDO') {
      return of({
        dataUrl: '',
        aspectRatio: 1,
        status: 'invalido',
        errorMessage: 'El formato de la URL o ruta del logotipo configurado no es válido.'
      });
    }

    // Tanto LOGO_LOCAL como LOGO_HTTPS se recuperan de forma segura a través del endpoint backend institucional
    return this.http.get(`${this.apiUrl}/sistema/logo-reportes`, {
      responseType: 'blob',
      observe: 'response',
      context: createHttpErrorScopeContext('local')
    }).pipe(
      switchMap(response => {
        if (response.status === 204 || !response.body || response.body.size === 0) {
          return of({
            dataUrl: '',
            aspectRatio: 1,
            status: 'inaccesible' as const,
            errorMessage: 'No fue posible recuperar el logotipo institucional configurado.'
          });
        }
        return from(this.convertirLogoEnPng(response.body));
      }),
      catchError(() => of({
        dataUrl: '',
        aspectRatio: 1,
        status: 'inaccesible' as const,
        errorMessage: 'No fue posible recuperar el logotipo institucional configurado.'
      }))
    );
  }

  private async convertirLogoEnPng(blob: Blob | null): Promise<ReportLogo> {
    if (!blob || blob.size === 0 || blob.size > 10 * 1024 * 1024)
      return {
        dataUrl: '',
        aspectRatio: 1,
        status: 'inaccesible',
        errorMessage: 'El contenido del logotipo está vacío o supera el límite permitido.'
      };

    if (!/^image\/(png|jpeg|jpg|gif|webp)$/i.test(blob.type))
      return {
        dataUrl: '',
        aspectRatio: 1,
        status: 'invalido',
        errorMessage: 'El tipo de contenido del logotipo no es una imagen válida.'
      };

    if (typeof Image !== 'undefined' && typeof document !== 'undefined' && typeof URL.createObjectURL === 'function') {
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
          return {
            dataUrl: '',
            aspectRatio: 1,
            status: 'invalido',
            errorMessage: 'Las dimensiones del logotipo no están permitidas.'
          };

        const canvas = document.createElement('canvas');
        canvas.width = width;
        canvas.height = height;
        const context = canvas.getContext('2d');
        if (context) {
          context.drawImage(img, 0, 0);
          return {
            dataUrl: canvas.toDataURL('image/png'),
            aspectRatio: width / height,
            status: 'disponible'
          };
        }
      } catch {
        // Fallback a lectura directa de dataUrl si canvas 2D no está disponible en el entorno
      } finally {
        URL.revokeObjectURL(objectUrl);
      }
    }

    const dataUrl = await this.blobToDataUrl(blob);
    return {
      dataUrl,
      aspectRatio: 1,
      status: dataUrl ? 'disponible' : 'inaccesible'
    };
  }

  private blobToDataUrl(blob: Blob): Promise<string> {
    return new Promise<string>((resolve) => {
      if (typeof FileReader !== 'undefined') {
        const reader = new FileReader();
        reader.onload = () => resolve(typeof reader.result === 'string' ? reader.result : '');
        reader.onerror = () => resolve('');
        reader.readAsDataURL(blob);
        return;
      }
      if (typeof (blob as any).arrayBuffer === 'function') {
        (blob as any).arrayBuffer().then((buf: ArrayBuffer) => {
          const bytes = new Uint8Array(buf);
          let binary = '';
          for (let i = 0; i < bytes.byteLength; i++) {
            binary += String.fromCharCode(bytes[i]);
          }
          const b64 = typeof btoa === 'function' ? btoa(binary) : '';
          resolve(`data:${blob.type || 'image/png'};base64,${b64}`);
        }).catch(() => resolve(''));
        return;
      }
      resolve('');
    });
  }

}
