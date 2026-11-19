import { provideHttpClient } from '@angular/common/http';
import { vi } from 'vitest';
import { throwError } from 'rxjs';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { HTTP_ERROR_SCOPE } from '../http/http-error-scope';
import { ConfiguracionService } from './configuracion.service';
import { ConfigSistema } from './configuracion.models';
import { getInstitutionalReportBranding } from '../reporting/institutional-report-standard';

const propiedadesTema = [
  '--ihss-primary-color', '--ihss-accent-color',
  '--ihss-primary-rgb', '--ihss-accent-rgb', '--ihss-on-primary-rgb', '--ihss-on-accent-rgb',
  '--ihss-modal-primary-rgb', '--ihss-modal-accent-rgb', '--ihss-on-modal-rgb', '--modal-close-bg',
  '--ihss-modal-primary-start-rgb', '--ihss-modal-primary-mid-rgb', '--ihss-modal-primary-end-rgb',
  '--ihss-modal-secondary-start-rgb', '--ihss-modal-secondary-mid-rgb', '--ihss-modal-secondary-end-rgb',
  '--ihss-modal-highlight-rgb',
  '--modal-close-bg-hover', '--modal-close-border', '--modal-close-border-hover', '--modal-close-color',
  '--modal-close-color-hover', '--modal-close-focus-rgb',
  ...[50, 100, 200, 300, 400, 500, 600, 700, 800, 900, 950].flatMap(level => [`--ihss-${level}-rgb`, `--ihss-on-${level}-rgb`])
];

describe('ConfiguracionService', () => {
  let service: ConfiguracionService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(ConfiguracionService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    vi.restoreAllMocks();
    propiedadesTema.forEach(property => document.documentElement.style.removeProperty(property));
    TestBed.resetTestingModule();
  });

  it('carga configuración y aplica valores seguros por defecto', () => {
    let resultado: ConfigSistema | undefined;
    service.CargarConfiguracion().subscribe(value => resultado = value);
    const request = http.expectOne('http://localhost:5043/api/configuracion/sistema');
    expect(request.request.context.get(HTTP_ERROR_SCOPE)).toBe('global');
    request.flush({
      success: true,
      datos: { nombreInstitucion: 'IHSS', nombreSistema: 'SGRLA', timeoutSesion: 0 }
    });

    expect(resultado?.colorPrimario).toBe('#1e3a8a');
    expect(resultado?.colorSecundario).toBe('#1d4ed8');
    expect(resultado?.timeoutSesion).toBe(30);
    expect(service.configSistema()).toEqual(resultado);
    expect(getInstitutionalReportBranding().logoUrl).toBe('');
    expect(document.documentElement.style.getPropertyValue('--ihss-primary-rgb')).toBe('30 58 138');
    expect(document.documentElement.style.getPropertyValue('--ihss-accent-rgb')).toBe('29 78 216');
    expect(document.documentElement.style.getPropertyValue('--ihss-50-rgb')).not.toBe('');
    expect(document.documentElement.style.getPropertyValue('--ihss-950-rgb')).not.toBe('');
    expect(document.documentElement.style.getPropertyValue('--ihss-modal-primary-rgb')).toBe('30 58 138');
    expect(document.documentElement.style.getPropertyValue('--ihss-modal-primary-start-rgb')).not.toBe('');
    expect(document.documentElement.style.getPropertyValue('--ihss-modal-secondary-end-rgb')).not.toBe('');
  });

  it('guarda configuración y actualiza identidad visual local', () => {
    const config: ConfigSistema = {
      nombreInstitucion: 'IHSS', nombreSistema: 'SGRLA', timeoutSesion: 45,
      colorPrimario: '#112233', colorSecundario: '#abcdef'
    };
    service.GuardarConfiguracion(config).subscribe();
    const request = http.expectOne('http://localhost:5043/api/configuracion/sistema');
    expect(request.request.method).toBe('PUT');
    expect(request.request.context.get(HTTP_ERROR_SCOPE)).toBe('local');
    expect(request.request.body).toEqual(config);
    request.flush({ success: true, mensaje: 'Actualizada' });

    expect(service.configSistema()).toEqual(config);
    expect(document.documentElement.style.getPropertyValue('--ihss-primary-rgb')).toBe('17 34 51');
    expect(document.documentElement.style.getPropertyValue('--ihss-accent-rgb')).toBe('171 205 239');
    expect(document.documentElement.style.getPropertyValue('--ihss-700-rgb')).toBe('17 34 51');
    expect(document.documentElement.style.getPropertyValue('--ihss-modal-accent-rgb')).not.toBe('171 205 239');
  });

  it('permite scope local por consumidor sin cambiar el default global', () => {
    service.CargarConfiguracion('local').subscribe();
    const localRequest = http.expectOne('http://localhost:5043/api/configuracion/sistema');
    expect(localRequest.request.context.get(HTTP_ERROR_SCOPE)).toBe('local');
    localRequest.flush({ success: true, datos: { nombreInstitucion: 'IHSS', nombreSistema: 'SGRLA', timeoutSesion: 30 } });

    service.ObtenerSlides().subscribe();
    const slidesRequest = http.expectOne('http://localhost:5043/api/configuracion/login');
    expect(slidesRequest.request.context.get(HTTP_ERROR_SCOPE)).toBe('local');
    slidesRequest.flush({ success: true, datos: [] });
  });

  it('mantiene local el feedback de operaciones administrativas de slides', () => {
    const slide = { id: 1, imagenUrl: '/uploads/slide.png', titulo: 'Slide', descripcion: '', imagenIcono: '', orden: 1, activo: true };
    service.getTodosSlides().subscribe();
    const list = http.expectOne('http://localhost:5043/api/configuracion/slides');
    expect(list.request.context.get(HTTP_ERROR_SCOPE)).toBe('local');
    list.flush({ success: true, datos: [] });

    service.crearSlide(slide).subscribe();
    const create = http.expectOne('http://localhost:5043/api/configuracion/slides');
    expect(create.request.context.get(HTTP_ERROR_SCOPE)).toBe('local');
    create.flush({ success: true, mensaje: 'ok' });

    service.actualizarSlide(1, slide).subscribe();
    const update = http.expectOne('http://localhost:5043/api/configuracion/slides/1');
    expect(update.request.context.get(HTTP_ERROR_SCOPE)).toBe('local');
    update.flush({ success: true, mensaje: 'ok' });

    service.eliminarSlide(1).subscribe();
    const remove = http.expectOne('http://localhost:5043/api/configuracion/slides/1');
    expect(remove.request.context.get(HTTP_ERROR_SCOPE)).toBe('local');
    remove.flush({ success: true, mensaje: 'ok' });
  });

  it('conserva el último tema válido si la configuración recibida trae HEX inválido', () => {
    service.CargarConfiguracion().subscribe();
    http.expectOne('http://localhost:5043/api/configuracion/sistema').flush({
      success: true,
      datos: { nombreInstitucion: 'IHSS', nombreSistema: 'SGRLA', colorPrimario: '#991b1b', colorSecundario: '#dc2626', timeoutSesion: 30 }
    });
    service.CargarConfiguracion().subscribe();
    http.expectOne('http://localhost:5043/api/configuracion/sistema').flush({
      success: true,
      datos: { nombreInstitucion: 'IHSS', nombreSistema: 'SGRLA', colorPrimario: 'rojo', colorSecundario: '#12xz00', timeoutSesion: 30 }
    });

    expect(document.documentElement.style.getPropertyValue('--ihss-primary-rgb')).toBe('153 27 27');
    expect(document.documentElement.style.getPropertyValue('--ihss-accent-rgb')).toBe('220 38 38');
  });

  it.each([
    [undefined, 'assets/login/slide1.png'],
    ['https://cdn.example/slide.png', 'https://cdn.example/slide.png'],
    ['assets/login/local.png', 'assets/login/local.png'],
    ['/uploads/slide.png', 'http://localhost:5043/uploads/slide.png'],
    ['uploads/slide.png', 'http://localhost:5043/uploads/slide.png']
  ])('resuelve URL de imagen %s', (entrada, esperada) => {
    expect(service.resolverUrlImagen(entrada)).toBe(esperada);
  });

  it('solicita la imagen validada al backend institucional cuando la configuración contiene una URL HTTPS válida', async () => {
    const expected = 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJ';
    vi.spyOn(service as any, 'convertirLogoEnPng').mockResolvedValue({ dataUrl: expected, aspectRatio: 2.5, status: 'disponible' });

    service.CargarConfiguracion().subscribe();
    http.expectOne('http://localhost:5043/api/configuracion/sistema').flush({
      success: true,
      datos: {
        nombreInstitucion: 'IHSS',
        nombreSistema: 'SGRLA',
        logoUrl: 'https://www.ihss.hn/wp-content/uploads/2020/06/logo-del-IHSS.jpg',
        timeoutSesion: 30
      }
    });

    // Demuestra que Angular no descarga directamente la URL remota sino que solicita la imagen validada al backend
    const logoReq = http.expectOne('http://localhost:5043/api/configuracion/sistema/logo-reportes');
    expect(logoReq.request.context.get(HTTP_ERROR_SCOPE)).toBe('local');
    logoReq.flush(new Blob(['synthetic-jpeg-data'], { type: 'image/jpeg' }));

    await vi.waitFor(() => expect(getInstitutionalReportBranding().logoUrl).toBe(expected));
    expect(getInstitutionalReportBranding().logoAspectRatio).toBe(2.5);
    expect(getInstitutionalReportBranding().logoStatus).toBe('disponible');
  });

  it('si la URL HTTPS remota es inaccesible o falla en backend, conserva reportes sin logo y marca estado inaccesible', () => {
    service.CargarConfiguracion().subscribe();
    http.expectOne('http://localhost:5043/api/configuracion/sistema').flush({
      success: true,
      datos: {
        nombreInstitucion: 'IHSS',
        nombreSistema: 'SGRLA',
        logoUrl: 'https://example.invalid/logo.png',
        timeoutSesion: 30
      }
    });

    const logoReq = http.expectOne('http://localhost:5043/api/configuracion/sistema/logo-reportes');
    logoReq.flush(null, { status: 204, statusText: 'No Content' });

    expect(getInstitutionalReportBranding().logoUrl).toBe('');
    expect(getInstitutionalReportBranding().logoStatus).toBe('inaccesible');
  });

  it('marca estado invalido sin consultar backend si el formato del logo no es local ni HTTPS', () => {
    service.CargarConfiguracion().subscribe();
    http.expectOne('http://localhost:5043/api/configuracion/sistema').flush({
      success: true,
      datos: {
        nombreInstitucion: 'IHSS',
        nombreSistema: 'SGRLA',
        logoUrl: 'javascript:alert(1)',
        timeoutSesion: 30
      }
    });

    http.expectNone('http://localhost:5043/api/configuracion/sistema/logo-reportes');
    expect(getInstitutionalReportBranding().logoUrl).toBe('');
    expect(getInstitutionalReportBranding().logoStatus).toBe('invalido');
  });

  it('carga el logo de /uploads mediante la API autenticada y conserva la imagen preparada', async () => {
    const expected = 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJ';
    vi.spyOn(service as any, 'convertirLogoEnPng').mockResolvedValue({ dataUrl: expected, aspectRatio: 2 });

    service.CargarConfiguracion().subscribe();
    http.expectOne('http://localhost:5043/api/configuracion/sistema').flush({
      success: true,
      datos: {
        nombreInstitucion: 'IHSS',
        nombreSistema: 'SGRLA',
        logoUrl: '/uploads/synthetic-logo.png',
        timeoutSesion: 30
      }
    });

    const logo = http.expectOne('http://localhost:5043/api/configuracion/sistema/logo-reportes');
    expect(logo.request.context.get(HTTP_ERROR_SCOPE)).toBe('local');
    logo.flush(new Blob(['synthetic'], { type: 'image/png' }));

    await vi.waitFor(() => expect(getInstitutionalReportBranding().logoUrl).toBe(expected));
    expect(getInstitutionalReportBranding().logoAspectRatio).toBe(2);
  });

  it('si falta el archivo cargado conserva reportes sin logo y sin excepciones', () => {
    service.CargarConfiguracion().subscribe();
    http.expectOne('http://localhost:5043/api/configuracion/sistema').flush({
      success: true,
      datos: {
        nombreInstitucion: 'IHSS',
        nombreSistema: 'SGRLA',
        logoUrl: '/uploads/no-existe.png',
        timeoutSesion: 30
      }
    });
    http.expectOne('http://localhost:5043/api/configuracion/sistema/logo-reportes')
      .flush(new Blob(['No existe'], { type: 'text/plain' }), { status: 404, statusText: 'Not Found' });
    expect(getInstitutionalReportBranding().logoUrl).toBe('');
  });

  it('aplica el titulo del navegador dinamico con nombreSistema configurado', () => {
    service.CargarConfiguracion().subscribe();
    http.expectOne('http://localhost:5043/api/configuracion/sistema').flush({
      success: true,
      datos: {
        nombreInstitucion: 'IHSS',
        nombreSistema: 'Sistema Institucional LA/FT',
        timeoutSesion: 30
      }
    });

    expect(document.title).toBe('Sistema Institucional LA/FT | Gestión de Riesgos LA/FT');
  });

  it('normaliza espacios en blanco en nombreSistema y aplica el titulo seguro', () => {
    service.CargarConfiguracion().subscribe();
    http.expectOne('http://localhost:5043/api/configuracion/sistema').flush({
      success: true,
      datos: {
        nombreInstitucion: 'IHSS',
        nombreSistema: '   SGRLA-IHSS Personalizado   ',
        timeoutSesion: 30
      }
    });

    expect(document.title).toBe('SGRLA-IHSS Personalizado | Gestión de Riesgos LA/FT');
  });

  it('aplica titulo por defecto cuando nombreSistema esta vacio o no existe', () => {
    service.CargarConfiguracion().subscribe();
    http.expectOne('http://localhost:5043/api/configuracion/sistema').flush({
      success: true,
      datos: {
        nombreInstitucion: 'IHSS',
        nombreSistema: '   ',
        timeoutSesion: 30
      }
    });

    expect(document.title).toBe('SGRLA-IHSS | Gestión de Riesgos LA/FT');
  });

  it('actualiza el titulo inmediatamente al guardar configuracion exitosamente', () => {
    const config: ConfigSistema = {
      nombreInstitucion: 'IHSS',
      nombreSistema: 'Nuevo Nombre Sistema',
      timeoutSesion: 45
    };

    service.GuardarConfiguracion(config).subscribe();
    http.expectOne('http://localhost:5043/api/configuracion/sistema').flush({
      success: true,
      mensaje: 'Configuración actualizada'
    });

    expect(document.title).toBe('Nuevo Nombre Sistema | Gestión de Riesgos LA/FT');
  });

  it('no altera el titulo ni favicon si el guardado falla', () => {
    document.title = 'Titulo Previo | Gestión de Riesgos LA/FT';
    const config: ConfigSistema = {
      nombreInstitucion: 'IHSS',
      nombreSistema: 'Nombre Invalido No Guardado',
      timeoutSesion: 45
    };

    let errorOcurrido = false;
    service.GuardarConfiguracion(config).subscribe({
      error: () => errorOcurrido = true
    });

    http.expectOne('http://localhost:5043/api/configuracion/sistema').flush(
      { success: false, mensaje: 'Error al persistir' },
      { status: 500, statusText: 'Server Error' }
    );

    expect(errorOcurrido).toBe(true);
    expect(document.title).toBe('Titulo Previo | Gestión de Riesgos LA/FT');
  });

  it('aplica favicon seguro apuntando al endpoint institucional cuando iconoUrl esta configurado', () => {
    service.CargarConfiguracion().subscribe();
    http.expectOne('http://localhost:5043/api/configuracion/sistema').flush({
      success: true,
      datos: {
        nombreInstitucion: 'IHSS',
        nombreSistema: 'SGRLA',
        iconoUrl: '/uploads/favicon-nuevo.png',
        timeoutSesion: 30
      }
    });

    const links = document.head.querySelectorAll<HTMLLinkElement>('link[rel*="icon"]');
    expect(links.length).toBe(1);
    expect(links[0].href).toContain('/api/configuracion/sistema/favicon?v=');
    expect(links[0].type).toBe('image/png');
  });

  it('restaura favicon.ico por defecto cuando iconoUrl esta vacio o se elimina', () => {
    service.CargarConfiguracion().subscribe();
    http.expectOne('http://localhost:5043/api/configuracion/sistema').flush({
      success: true,
      datos: {
        nombreInstitucion: 'IHSS',
        nombreSistema: 'SGRLA',
        iconoUrl: '',
        timeoutSesion: 30
      }
    });

    const links = document.head.querySelectorAll<HTMLLinkElement>('link[rel*="icon"]');
    expect(links.length).toBe(1);
    expect(links[0].getAttribute('href')).toBe('favicon.ico');
    expect(links[0].type).toBe('image/x-icon');
  });

  it('elimina etiquetas link duplicadas conservando un unico favicon en DOM', () => {
    const extra1 = document.createElement('link'); extra1.rel = 'icon'; document.head.appendChild(extra1);
    const extra2 = document.createElement('link'); extra2.rel = 'shortcut icon'; document.head.appendChild(extra2);

    service.CargarConfiguracion().subscribe();
    http.expectOne('http://localhost:5043/api/configuracion/sistema').flush({
      success: true,
      datos: {
        nombreInstitucion: 'IHSS',
        nombreSistema: 'SGRLA',
        iconoUrl: '/uploads/icono.png',
        timeoutSesion: 30
      }
    });

    const links = document.head.querySelectorAll<HTMLLinkElement>('link[rel*="icon"]');
    expect(links.length).toBe(1);
  });

  it('desacopla metadatos web (titulo y favicon) del logo de reporteria ante fallo o retardo en el logo', () => {
    // Simulamos fallo en prepararLogoReporte
    vi.spyOn(service as any, 'prepararLogoReporte').mockReturnValue(
      throwError(() => new Error('Fallo de red en logo'))
    );

    let resultado: ConfigSistema | undefined;
    service.CargarConfiguracion().subscribe(val => resultado = val);

    http.expectOne('http://localhost:5043/api/configuracion/sistema').flush({
      success: true,
      datos: {
        nombreInstitucion: 'IHSS',
        nombreSistema: 'Sistema Resiliente',
        iconoUrl: '/uploads/icono-desacoplado.png',
        logoUrl: '/uploads/logo-falla.png',
        timeoutSesion: 30
      }
    });

    // Verificamos que el título y favicon se aplicaron inmediatamente a pesar del fallo en el logo
    expect(document.title).toBe('Sistema Resiliente | Gestión de Riesgos LA/FT');
    const links = document.head.querySelectorAll<HTMLLinkElement>('link[rel*="icon"]');
    expect(links.length).toBe(1);
    expect(links[0].href).toContain('/api/configuracion/sistema/favicon?v=');
    expect(resultado?.nombreSistema).toBe('Sistema Resiliente');
  });

  it('sube favicon a /api/configuracion/sistema/favicon/upload y devuelve url y estado', () => {
    const file = new File(['synthetic-favicon'], 'favicon.png', { type: 'image/png' });
    let response: any;
    service.subirFavicon(file).subscribe(res => response = res);

    const req = http.expectOne('http://localhost:5043/api/configuracion/sistema/favicon/upload');
    expect(req.request.method).toBe('POST');
    expect(req.request.context.get(HTTP_ERROR_SCOPE)).toBe('local');
    expect(req.request.body instanceof FormData).toBe(true);

    req.flush({
      success: true,
      url: '/uploads/favicon-test.ico',
      mimeType: 'image/x-icon',
      status: 'ready-to-save',
      mensaje: 'Favicon convertido correctamente.'
    });

    expect(response?.success).toBe(true);
    expect(response?.url).toBe('/uploads/favicon-test.ico');
    expect(response?.status).toBe('ready-to-save');
  });
});
