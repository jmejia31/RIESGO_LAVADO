import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ConfiguracionService } from './configuracion.service';
import { ConfigSistema } from './configuracion.models';

const propiedadesTema = [
  '--ihss-primary-color', '--ihss-accent-color',
  '--ihss-primary-rgb', '--ihss-accent-rgb', '--ihss-on-primary-rgb', '--ihss-on-accent-rgb',
  '--ihss-modal-primary-rgb', '--ihss-modal-accent-rgb', '--ihss-on-modal-rgb', '--modal-close-bg',
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
    propiedadesTema.forEach(property => document.documentElement.style.removeProperty(property));
    TestBed.resetTestingModule();
  });

  it('carga configuración y aplica valores seguros por defecto', () => {
    let resultado: ConfigSistema | undefined;
    service.CargarConfiguracion().subscribe(value => resultado = value);
    const request = http.expectOne('http://localhost:5043/api/configuracion/sistema');
    request.flush({
      success: true,
      datos: { nombreInstitucion: 'IHSS', nombreSistema: 'SGRLA', timeoutSesion: 0 }
    });

    expect(resultado?.colorPrimario).toBe('#1e3a8a');
    expect(resultado?.colorSecundario).toBe('#1d4ed8');
    expect(resultado?.timeoutSesion).toBe(30);
    expect(service.configSistema()).toEqual(resultado);
    expect(document.documentElement.style.getPropertyValue('--ihss-primary-rgb')).toBe('30 58 138');
    expect(document.documentElement.style.getPropertyValue('--ihss-accent-rgb')).toBe('29 78 216');
    expect(document.documentElement.style.getPropertyValue('--ihss-50-rgb')).not.toBe('');
    expect(document.documentElement.style.getPropertyValue('--ihss-950-rgb')).not.toBe('');
    expect(document.documentElement.style.getPropertyValue('--ihss-modal-primary-rgb')).toBe('30 58 138');
  });

  it('guarda configuración y actualiza identidad visual local', () => {
    const config: ConfigSistema = {
      nombreInstitucion: 'IHSS', nombreSistema: 'SGRLA', timeoutSesion: 45,
      colorPrimario: '#112233', colorSecundario: '#abcdef'
    };
    service.GuardarConfiguracion(config).subscribe();
    const request = http.expectOne('http://localhost:5043/api/configuracion/sistema');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual(config);
    request.flush({ success: true, mensaje: 'Actualizada' });

    expect(service.configSistema()).toEqual(config);
    expect(document.documentElement.style.getPropertyValue('--ihss-primary-rgb')).toBe('17 34 51');
    expect(document.documentElement.style.getPropertyValue('--ihss-accent-rgb')).toBe('171 205 239');
    expect(document.documentElement.style.getPropertyValue('--ihss-700-rgb')).toBe('17 34 51');
    expect(document.documentElement.style.getPropertyValue('--ihss-modal-accent-rgb')).not.toBe('171 205 239');
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
});
