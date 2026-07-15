import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { CONFIRMACION_CAMBIOS_HEADER } from '../../../../core/interceptors/confirmacion-cambios.interceptor';
import { MatricesRiesgosService } from './matrices-riesgos.service';

describe('MatricesRiesgosService', () => {
  let service: MatricesRiesgosService;
  let http: HttpTestingController;
  const apiUrl = 'http://localhost:5043/api/matrices-riesgos';

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(MatricesRiesgosService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    TestBed.resetTestingModule();
  });

  it('construye el reporte omitiendo filtros vacios', () => {
    const result = vi.fn();
    service.reporte({ estado: 'ACTIVA', busqueda: '', fechaDesde: undefined } as never).subscribe(result);

    const request = http.expectOne(req => req.url === `${apiUrl}/reportes`);
    expect(request.request.params.keys()).toEqual(['estado']);
    expect(request.request.params.get('estado')).toBe('ACTIVA');
    request.flush({ success: true, datos: { total: 3 } });
    expect(result).toHaveBeenCalledWith({ total: 3 });
  });

  it('exporta el reporte como blob con formato y confirmacion previa', () => {
    const result = vi.fn();
    service.exportarReporte({ estado: 'ACTIVA' } as never, 'PDF').subscribe(result);

    const request = http.expectOne(req => req.url === `${apiUrl}/reportes/exportar`);
    expect(request.request.method).toBe('GET');
    expect(request.request.responseType).toBe('blob');
    expect(request.request.params.get('estado')).toBe('ACTIVA');
    expect(request.request.params.get('formato')).toBe('PDF');
    expect(request.request.headers.get(CONFIRMACION_CAMBIOS_HEADER)).toBe('1');
    const archivo = new Blob(['reporte'], { type: 'application/pdf' });
    request.flush(archivo);
    expect(result).toHaveBeenCalledWith(archivo);
  });

  it('actualiza una matriz y extrae el detalle de la respuesta', () => {
    const dto = { nombre: 'Matriz institucional' } as never;
    const detalle = { id: 14, nombre: 'Matriz institucional' };
    const result = vi.fn();
    service.actualizar(14, dto).subscribe(result);

    const request = http.expectOne(`${apiUrl}/14`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual(dto);
    expect(request.request.headers.get(CONFIRMACION_CAMBIOS_HEADER)).toBe('1');
    request.flush({ success: true, datos: detalle });
    expect(result).toHaveBeenCalledWith(detalle);
  });

  it('recalcula con motivo, tipo de calculo y confirmacion previa', () => {
    service.recalcular(14, 'Cambio de criterios', 'PARCIAL').subscribe();

    const request = http.expectOne(`${apiUrl}/14/recalcular`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      tipoCalculo: 'PARCIAL', motivoCalculo: 'Cambio de criterios'
    });
    expect(request.request.headers.get(CONFIRMACION_CAMBIOS_HEADER)).toBe('1');
    request.flush({ success: true, datos: { nivel: 'ALTO' } });
  });

  it('consulta criterios activos e inactivos mediante un parametro explicito', () => {
    const criterios = [{ id: 1, nombre: 'Pais' }];
    const result = vi.fn();
    service.listarCriterios(true).subscribe(result);

    const request = http.expectOne(req => req.url === `${apiUrl}/criterios`);
    expect(request.request.params.get('incluirInactivos')).toBe('true');
    request.flush({ success: true, datos: criterios });
    expect(result).toHaveBeenCalledWith(criterios);
  });

  it('propaga errores HTTP del listado al coordinador de la pantalla', () => {
    const error = vi.fn();
    service.listar({ estado: 'ACTIVA' } as never).subscribe({ error });

    const request = http.expectOne(req => req.url === apiUrl);
    request.flush({ mensaje: 'Consulta rechazada' }, { status: 500, statusText: 'Server Error' });

    expect(error).toHaveBeenCalledWith(expect.objectContaining({ status: 500 }));
  });
});
