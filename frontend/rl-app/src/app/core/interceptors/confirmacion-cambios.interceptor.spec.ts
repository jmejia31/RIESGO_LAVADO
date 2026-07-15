import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import Swal from 'sweetalert2';
import {
  CONFIRMACION_BOTON_HEADER,
  CONFIRMACION_CAMBIOS_HEADER,
  CONFIRMACION_ICONO_HEADER,
  CONFIRMACION_TEXTO_HEADER,
  CONFIRMACION_TITULO_HEADER,
  confirmacionCambiosInterceptor
} from './confirmacion-cambios.interceptor';

vi.mock('sweetalert2', () => ({
  default: { fire: vi.fn() }
}));

describe('confirmacionCambiosInterceptor', () => {
  let http: HttpClient;
  let testing: HttpTestingController;
  const fire = vi.mocked(Swal.fire);

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([confirmacionCambiosInterceptor])),
        provideHttpClientTesting()
      ]
    });
    http = TestBed.inject(HttpClient);
    testing = TestBed.inject(HttpTestingController);
    fire.mockReset();
  });

  afterEach(() => {
    testing.verify();
    TestBed.resetTestingModule();
  });

  it('deja pasar consultas GET sin solicitar confirmacion', () => {
    http.get('/api/catalogo').subscribe();
    const request = testing.expectOne('/api/catalogo');
    expect(fire).not.toHaveBeenCalled();
    request.flush({ success: true });
  });

  it('excluye las operaciones de autenticacion', () => {
    http.post('/api/auth/login', { usuario: 'analista' }).subscribe();
    const request = testing.expectOne('/api/auth/login');
    expect(fire).not.toHaveBeenCalled();
    request.flush({ success: true });
  });

  it('respeta una confirmacion previa y elimina todas las cabeceras internas', () => {
    http.put('/api/recurso/7', { nombre: 'Actualizado' }, {
      headers: {
        [CONFIRMACION_CAMBIOS_HEADER]: '1',
        [CONFIRMACION_TITULO_HEADER]: 'Titulo interno',
        [CONFIRMACION_TEXTO_HEADER]: 'Texto interno',
        [CONFIRMACION_BOTON_HEADER]: 'Guardar',
        [CONFIRMACION_ICONO_HEADER]: 'warning'
      }
    }).subscribe();

    const request = testing.expectOne('/api/recurso/7');
    [
      CONFIRMACION_CAMBIOS_HEADER,
      CONFIRMACION_TITULO_HEADER,
      CONFIRMACION_TEXTO_HEADER,
      CONFIRMACION_BOTON_HEADER,
      CONFIRMACION_ICONO_HEADER
    ].forEach(header => expect(request.request.headers.has(header)).toBe(false));
    expect(fire).not.toHaveBeenCalled();
    request.flush({ success: true });
  });

  it('envia la operacion cuando el usuario confirma y usa los textos configurados', async () => {
    fire.mockResolvedValue({ isConfirmed: true } as never);
    const response = firstValueFrom(http.delete('/api/recurso/9', {
      headers: {
        [CONFIRMACION_TITULO_HEADER]: 'Eliminar evidencia',
        [CONFIRMACION_TEXTO_HEADER]: 'Confirme la eliminacion',
        [CONFIRMACION_BOTON_HEADER]: 'Eliminar ahora',
        [CONFIRMACION_ICONO_HEADER]: 'warning'
      }
    }));

    await vi.waitFor(() => expect(fire).toHaveBeenCalledOnce());
    expect(fire).toHaveBeenCalledWith(expect.objectContaining({
      title: 'Eliminar evidencia',
      text: 'Confirme la eliminacion',
      confirmButtonText: 'Eliminar ahora',
      icon: 'warning',
      confirmButtonColor: '#dc2626'
    }));

    const request = testing.expectOne('/api/recurso/9');
    expect(request.request.headers.has(CONFIRMACION_TITULO_HEADER)).toBe(false);
    request.flush({ success: true });
    await expect(response).resolves.toEqual({ success: true });
  });

  it('cancela localmente la operacion cuando el usuario no confirma', async () => {
    fire.mockResolvedValue({ isConfirmed: false } as never);

    await expect(firstValueFrom(http.post('/api/recurso', { nombre: 'Nuevo' })))
      .rejects.toMatchObject({ status: 499, statusText: 'Operacion cancelada' });

    testing.expectNone('/api/recurso');
  });
});
