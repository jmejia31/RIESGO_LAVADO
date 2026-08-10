import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { httpResilienceInterceptor } from './http-resilience.interceptor';

describe('httpResilienceInterceptor', () => {
  let client: HttpClient;
  let controller: HttpTestingController;

  beforeEach(() => {
    vi.useFakeTimers();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([httpResilienceInterceptor])),
        provideHttpClientTesting()
      ]
    });

    client = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    controller.verify();
    TestBed.resetTestingModule();
    vi.useRealTimers();
  });

  it('debe reintentar solicitudes GET cuando ocurre error 503 temporal', () => {
    let resultado: any = null;
    client.get('/api/test-get').subscribe(res => resultado = res);

    // Intento 1: falla con 503
    const req1 = controller.expectOne('/api/test-get');
    req1.flush('Service Unavailable', { status: 503, statusText: 'Service Unavailable' });

    // Avanzar temporizadores de Vitest
    vi.advanceTimersByTime(350);

    // Intento 2: reintento automático responde exitoso
    const req2 = controller.expectOne('/api/test-get');
    req2.flush({ ok: true });

    expect(resultado).toEqual({ ok: true });
  });

  it('NO debe reintentar solicitudes POST ante errores 503', () => {
    let errorRecibido: any = null;

    client.post('/api/test-post', { datos: 1 }).subscribe({
      next: () => {},
      error: (err) => errorRecibido = err
    });

    const req = controller.expectOne('/api/test-post');
    req.flush('Service Unavailable', { status: 503, statusText: 'Service Unavailable' });

    expect(errorRecibido).toBeTruthy();
    expect(errorRecibido.status).toBe(503);
  });

  it('NO debe reintentar solicitudes GET ante errores 400 Bad Request', () => {
    let errorRecibido: any = null;

    client.get('/api/test-400').subscribe({
      next: () => {},
      error: (err) => errorRecibido = err
    });

    const req = controller.expectOne('/api/test-400');
    req.flush({ title: 'Bad Request', detail: 'Datos inválidos' }, { status: 400, statusText: 'Bad Request' });

    expect(errorRecibido).toBeTruthy();
    expect(errorRecibido.status).toBe(400);
  });
});
