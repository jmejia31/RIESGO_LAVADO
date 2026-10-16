import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { HTTP_ERROR_SCOPE } from '../http/http-error-scope';
import { AuthService } from './auth.service';

describe('AuthService local validation requests', () => {
  let service: AuthService;
  let http: HttpTestingController;
  const apiUrl = 'http://localhost:5043/api/auth';

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    });
    service = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    TestBed.resetTestingModule();
  });

  it('keeps password recovery and password change errors in their validation dialogs', () => {
    service.recuperarPassword('persona@ihss.hn').subscribe();
    const recovery = http.expectOne(`${apiUrl}/recuperar-password`);
    expect(recovery.request.method).toBe('POST');
    expect(recovery.request.body).toEqual({ email: 'persona@ihss.hn' });
    expect(recovery.request.context.get(HTTP_ERROR_SCOPE)).toBe('local');
    recovery.flush({ success: true, mensaje: 'enviado' });

    service.cambiarPassword('temporal', 'NuevaClave123!').subscribe();
    const change = http.expectOne(`${apiUrl}/password`);
    expect(change.request.method).toBe('PUT');
    expect(change.request.body).toEqual({ passwordActual: 'temporal', nuevoPassword: 'NuevaClave123!' });
    expect(change.request.context.get(HTTP_ERROR_SCOPE)).toBe('local');
    change.flush({ success: true, mensaje: 'actualizada' });
  });
});
