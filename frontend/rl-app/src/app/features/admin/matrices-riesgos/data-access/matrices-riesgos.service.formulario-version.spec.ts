import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { MatricesRiesgosService } from './matrices-riesgos.service';
import { VersionFormularioDto } from '../models/matrices-riesgos.models';
import { environment } from '../../../../../environments/environment';

describe('MatricesRiesgosService — obtención autoritativa de versión por ID (F6.3)', () => {
  let service: MatricesRiesgosService;
  let httpMock: HttpTestingController;
  const baseUrl = `${environment.apiUrl}/matrices-riesgos`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        MatricesRiesgosService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(MatricesRiesgosService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('consulta GET /formularios/{id} y mapea response.datos correctamente', () => {
    const mockVersion: VersionFormularioDto = {
      verId: 42,
      verFamiliaId: 1,
      verCodigo: 'FORM_TEST',
      verVersion: 1,
      verJson: JSON.stringify({
        codigoFormulario: 'FORM_TEST',
        nombreFormulario: 'Formulario Test',
        secciones: [],
        catalogos: [
          {
            codigo: 'CAT_TEST',
            nombre: 'Catálogo Test',
            elementos: [{ codigo: '001', valor: 'Primero', orden: 1 }]
          }
        ]
      }),
      verHash: 'sha_test_42',
      verEstado: 'DRAFT',
      verVigente: false,
      verFechaCreacion: '2026-08-20T10:00:00Z',
      verUsrCreacion: 1
    };

    service.obtenerVersionFormulario(42).subscribe(version => {
      expect(version).toEqual(mockVersion);
      expect(version.verId).toBe(42);
      expect(version.verEstado).toBe('DRAFT');
    });

    const req = httpMock.expectOne(`${baseUrl}/formularios/42`);
    expect(req.request.method).toBe('GET');
    req.flush({
      success: true,
      datos: mockVersion,
      mensaje: 'Versión obtenida correctamente.'
    });
  });

  it('propaga errores HTTP (404 / 500) cuando la versión no existe o falla el servidor', () => {
    service.obtenerVersionFormulario(999).subscribe({
      next: () => {
        expect.unreachable('No debía tener éxito');
      },
      error: error => {
        expect(error.status).toBe(404);
      }
    });

    const req = httpMock.expectOne(`${baseUrl}/formularios/999`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: false, mensaje: 'Versión no encontrada.' }, { status: 404, statusText: 'Not Found' });
  });
});
