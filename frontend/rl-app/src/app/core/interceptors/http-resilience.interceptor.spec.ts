import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { GlobalHttpStateService } from '../services/global-http-state.service';
import { httpResilienceInterceptor } from './http-resilience.interceptor';

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

  it('debe reintentar GET ante 503 usando el primer delay exponencial de 300 ms', () => {
    let resultado: unknown = null;
    client.get('/api/test-get-503').subscribe(res => resultado = res);

    const req1 = controller.expectOne('/api/test-get-503');
    req1.flush('Service Unavailable', { status: 503, statusText: 'Service Unavailable' });

    vi.advanceTimersByTime(299);
    controller.expectNone('/api/test-get-503');

    vi.advanceTimersByTime(1);
    const req2 = controller.expectOne('/api/test-get-503');
    req2.flush({ ok: true });

    expect(resultado).toEqual({ ok: true });
    expect(globalState.cargando()).toBe(false);
  });

  it('debe reintentar GET ante error de red status 0', () => {
    let resultado: unknown = null;
    client.get('/api/test-network').subscribe(res => resultado = res);

    const req1 = controller.expectOne('/api/test-network');
    req1.error(new ProgressEvent('network-error'));

    vi.advanceTimersByTime(300);
    const req2 = controller.expectOne('/api/test-network');
    req2.flush({ ok: true });

    expect(resultado).toEqual({ ok: true });
    expect(globalState.cargando()).toBe(false);
  });

  it('debe reintentar GET ante 504 Gateway Timeout', () => {
    let resultado: unknown = null;
    client.get('/api/test-504').subscribe(res => resultado = res);

    const req1 = controller.expectOne('/api/test-504');
    req1.flush('Gateway Timeout', { status: 504, statusText: 'Gateway Timeout' });

    vi.advanceTimersByTime(300);
    const req2 = controller.expectOne('/api/test-504');
    req2.flush({ ok: true });

    expect(resultado).toEqual({ ok: true });
    expect(globalState.cargando()).toBe(false);
  });

  it('debe limitar GET a exactamente dos reintentos con delays 300 ms y 600 ms', () => {
    let errorRecibido: any = null;
    client.get('/api/test-max-retries').subscribe({
      error: err => errorRecibido = err
    });

    const req1 = controller.expectOne('/api/test-max-retries');
    req1.flush('Service Unavailable', { status: 503, statusText: 'Service Unavailable' });

    vi.advanceTimersByTime(300);
    const req2 = controller.expectOne('/api/test-max-retries');
    req2.flush('Service Unavailable', { status: 503, statusText: 'Service Unavailable' });

    vi.advanceTimersByTime(599);
    controller.expectNone('/api/test-max-retries');

    vi.advanceTimersByTime(1);
    const req3 = controller.expectOne('/api/test-max-retries');
    req3.flush({ detail: 'Servicio temporalmente no disponible' }, { status: 503, statusText: 'Service Unavailable' });

    expect(errorRecibido).toBeTruthy();
    expect(errorRecibido.status).toBe(503);
    vi.advanceTimersByTime(5000);
    controller.expectNone('/api/test-max-retries');
    expect(globalState.cargando()).toBe(false);
  });

  it.each([400, 500, 502])('NO debe reintentar GET ante status %s', (status) => {
    let errorRecibido: any = null;
    client.get(`/api/test-no-retry-${status}`).subscribe({
      error: err => errorRecibido = err
    });

    const req = controller.expectOne(`/api/test-no-retry-${status}`);
    req.flush(
      { detail: `Error controlado ${status}` },
      { status, statusText: 'Error' }
    );

    expect(errorRecibido?.status).toBe(status);
    vi.advanceTimersByTime(5000);
    controller.expectNone(`/api/test-no-retry-${status}`);
    expect(globalState.cargando()).toBe(false);
  });

  it.each(['POST', 'PUT', 'DELETE', 'PATCH'])('NO debe reintentar %s ante 503', (method) => {
    let errorRecibido: any = null;
    client.request(method, `/api/test-mutacion-${method.toLowerCase()}`, { body: { datos: 1 } }).subscribe({
      error: err => errorRecibido = err
    });

    const url = `/api/test-mutacion-${method.toLowerCase()}`;
    const req = controller.expectOne(url);
    expect(req.request.method).toBe(method);
    req.flush(
      { detail: 'No se pudo procesar la operación' },
      { status: 503, statusText: 'Service Unavailable' }
    );

    expect(errorRecibido?.status).toBe(503);
    vi.advanceTimersByTime(5000);
    controller.expectNone(url);
    expect(globalState.cargando()).toBe(false);
  });

  it('debe extraer detail de ProblemDetails para la notificación global', () => {
    let errorRecibido: any = null;
    client.get('/api/test-problem-details').subscribe({
      error: err => errorRecibido = err
    });

    const req = controller.expectOne('/api/test-problem-details');
    req.flush(
      { title: 'Bad Request', detail: 'Datos inválidos en el formulario' },
      { status: 400, statusText: 'Bad Request' }
    );

    expect(errorRecibido?.status).toBe(400);
    expect(globalState.ultimoError()).toBe('Datos inválidos en el formulario');
    expect(globalState.cargando()).toBe(false);
  });

  it('debe mantener cargando=true hasta finalizar todas las peticiones concurrentes', () => {
    client.get('/api/concurrente-a').subscribe();
    client.get('/api/concurrente-b').subscribe();

    expect(globalState.cargando()).toBe(true);

    const reqA = controller.expectOne('/api/concurrente-a');
    const reqB = controller.expectOne('/api/concurrente-b');

    reqA.flush({ ok: true });
    expect(globalState.cargando()).toBe(true);

    reqB.flush({ ok: true });
    expect(globalState.cargando()).toBe(false);
  });

  it.each([401, 403, 499])('NO debe publicar banner global para status %s', (status) => {
    let errorRecibido: any = null;
    client.get(`/api/test-ignorado-${status}`).subscribe({
      error: err => errorRecibido = err
    });

    const req = controller.expectOne(`/api/test-ignorado-${status}`);
    req.flush(
      { detail: 'Mensaje que no debe mostrarse en el banner global' },
      { status, statusText: 'Ignored by global banner' }
    );

    expect(errorRecibido?.status).toBe(status);
    expect(globalState.ultimoError()).toBeNull();
    expect(globalState.cargando()).toBe(false);
  });
});
