import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { httpResilienceInterceptor } from './http-resilience.interceptor';
import { GlobalHttpStateService } from '../services/global-http-state.service';

describe('httpResilienceInterceptor', () => {
  let client: HttpClient;
  let controller: HttpTestingController;
  let globalState: GlobalHttpStateService;

  beforeEach(() => {
    vi.useFakeTimers();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([httpResilienceInterceptor])),
        provideHttpClientTesting(),
        GlobalHttpStateService
      ]
    });

    client = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
    globalState = TestBed.inject(GlobalHttpStateService);
  });

  afterEach(() => {
    controller.verify();
    TestBed.resetTestingModule();
    vi.useRealTimers();
  });

  it('debe reintentar solicitudes GET cuando ocurre error 503 temporal', () => {
    let resultado: any = null;
    client.get('/api/test-get').subscribe(res => resultado = res);

    const req1 = controller.expectOne('/api/test-get');
    req1.flush('Service Unavailable', { status: 503, statusText: 'Service Unavailable' });

    vi.advanceTimersByTime(350);

    const req2 = controller.expectOne('/api/test-get');
    req2.flush({ ok: true });

    expect(resultado).toEqual({ ok: true });
    expect(globalState.cargando()).toBe(false);
  });

  it('NO debe reintentar solicitudes POST ante errores 503 y debe registrar la notificación de error global', () => {
    let errorRecibido: any = null;

    client.post('/api/test-post', { datos: 1 }).subscribe({
      next: () => {},
      error: (err) => errorRecibido = err
    });

    const req = controller.expectOne('/api/test-post');
    req.flush({ detail: 'No se pudo guardar el registro' }, { status: 503, statusText: 'Service Unavailable' });

    expect(errorRecibido).toBeTruthy();
    expect(errorRecibido.status).toBe(503);
    expect(globalState.ultimoError()).toBe('No se pudo guardar el registro');
    expect(globalState.cargando()).toBe(false);
  });

  it('NO debe reintentar solicitudes GET ante errores 400 Bad Request y extraer el detalle ProblemDetails', () => {
    let errorRecibido: any = null;

    client.get('/api/test-400').subscribe({
      next: () => {},
      error: (err) => errorRecibido = err
    });

    const req = controller.expectOne('/api/test-400');
    req.flush({ title: 'Bad Request', detail: 'Datos inválidos en el formulario' }, { status: 400, statusText: 'Bad Request' });

    expect(errorRecibido).toBeTruthy();
    expect(errorRecibido.status).toBe(400);
    expect(globalState.ultimoError()).toBe('Datos inválidos en el formulario');
    expect(globalState.cargando()).toBe(false);
  });
});
