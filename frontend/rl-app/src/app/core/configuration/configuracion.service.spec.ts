import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ConfiguracionService } from './configuracion.service';
import { ConfigSistema } from './configuracion.models';

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
    document.documentElement.style.removeProperty('--ihss-primary-rgb');
    document.documentElement.style.removeProperty('--ihss-accent-rgb');
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
