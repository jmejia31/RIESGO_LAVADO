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

  it('lista los planes de una matriz', () => {
    const planes = [{ planId: 4, actividad: 'Revisar expediente' }];
    const result = vi.fn();
    service.listarPlanes(12).subscribe(result);

    const request = http.expectOne(`${apiUrl}/12/planes`);
    expect(request.request.method).toBe('GET');
    request.flush({ success: true, datos: planes });
    expect(result).toHaveBeenCalledWith(planes);
  });

  it('crea un plan con confirmacion previa y extrae la respuesta', () => {
    const dto = { actividad: 'Revisar expediente', responsable: 'Cumplimiento' };
    const result = vi.fn();
    service.crearPlan(12, dto).subscribe(result);

    const request = http.expectOne(`${apiUrl}/12/planes`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(dto);
    expect(request.request.headers.get(CONFIRMACION_CAMBIOS_HEADER)).toBe('1');
    request.flush({ success: true, datos: { planId: 4, ...dto } });
    expect(result).toHaveBeenCalledWith(expect.objectContaining({ planId: 4 }));
  });

  it('actualiza y cambia el estado de un plan con motivo', () => {
    const dto = { actividad: 'Seguimiento mensual', responsable: 'Cumplimiento' } as never;
    service.actualizarPlan(12, 4, dto).subscribe();
    service.cambiarEstadoPlan(12, 4, 'CERRADO', 'Evidencia aprobada').subscribe();

    const update = http.expectOne(`${apiUrl}/12/planes/4`);
    expect(update.request.method).toBe('PUT');
    expect(update.request.body).toEqual(dto);
    expect(update.request.headers.get(CONFIRMACION_CAMBIOS_HEADER)).toBe('1');
    update.flush({ success: true, datos: { planId: 4 } });

    const estado = http.expectOne(`${apiUrl}/12/planes/4/estado`);
    expect(estado.request.method).toBe('PUT');
    expect(estado.request.body).toEqual({ estado: 'CERRADO', motivo: 'Evidencia aprobada' });
    expect(estado.request.headers.get(CONFIRMACION_CAMBIOS_HEADER)).toBe('1');
    estado.flush({ success: true });
  });

  it('inactiva un plan con confirmacion y motivo', () => {
    service.inactivarPlan(12, 4, 'Plan sustituido').subscribe();

    const request = http.expectOne(`${apiUrl}/12/planes/4/inactivar`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ motivo: 'Plan sustituido' });
    expect(request.request.headers.get(CONFIRMACION_CAMBIOS_HEADER)).toBe('1');
    request.flush({ success: true });
  });

  it('reactiva un plan con confirmacion y motivo', () => {
    service.reactivarPlan(12, 4, 'Reapertura autorizada').subscribe();

    const request = http.expectOne(`${apiUrl}/12/planes/4/reactivar`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ motivo: 'Reapertura autorizada' });
    expect(request.request.headers.get(CONFIRMACION_CAMBIOS_HEADER)).toBe('1');
    request.flush({ success: true });
  });

  it('lista las evidencias de una matriz', () => {
    const evidencias = [{ evidenciaId: 8, nombreOriginal: 'reporte.pdf' }];
    const result = vi.fn();
    service.listarEvidencias(12).subscribe(result);

    const request = http.expectOne(`${apiUrl}/12/evidencias`);
    expect(request.request.method).toBe('GET');
    request.flush({ success: true, datos: evidencias });
    expect(result).toHaveBeenCalledWith(evidencias);
  });

  it('carga evidencia como FormData vinculada a plan y control', () => {
    const archivo = new File(['%PDF-1.7'], 'reporte.pdf', { type: 'application/pdf' });
    service.cargarEvidencia(12, archivo, 3, 4).subscribe();

    const request = http.expectOne(`${apiUrl}/12/evidencias`);
    expect(request.request.method).toBe('POST');
    expect(request.request.headers.get(CONFIRMACION_CAMBIOS_HEADER)).toBe('1');
    const form = request.request.body as FormData;
    expect(form.get('archivo')).toBe(archivo);
    expect(form.get('controlId')).toBe('3');
    expect(form.get('planId')).toBe('4');
    request.flush({ success: true, datos: { evidenciaId: 8 } });
  });

  it('descarga una evidencia como blob auditado', () => {
    const result = vi.fn();
    service.descargarEvidencia(12, 8).subscribe(result);

    const request = http.expectOne(`${apiUrl}/12/evidencias/8/descargar`);
    expect(request.request.method).toBe('GET');
    expect(request.request.responseType).toBe('blob');
    expect(request.request.headers.get(CONFIRMACION_CAMBIOS_HEADER)).toBe('1');
    const archivo = new Blob(['reporte'], { type: 'application/pdf' });
    request.flush(archivo);
    expect(result).toHaveBeenCalledWith(archivo);
  });

  it('inactiva una evidencia con confirmacion y motivo', () => {
    service.inactivarEvidencia(12, 8, 'Documento sustituido').subscribe();

    const request = http.expectOne(`${apiUrl}/12/evidencias/8/inactivar`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ motivo: 'Documento sustituido' });
    expect(request.request.headers.get(CONFIRMACION_CAMBIOS_HEADER)).toBe('1');
    request.flush({ success: true });
  });


  it('reactiva un criterio con confirmacion y motivo', () => {
    service.reactivarCriterio(9, 'Rango nuevamente vigente').subscribe();

    const request = http.expectOne(`${apiUrl}/criterios/9/reactivar`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ motivo: 'Rango nuevamente vigente' });
    expect(request.request.headers.get(CONFIRMACION_CAMBIOS_HEADER)).toBe('1');
    request.flush({ success: true });
  });

  it('propaga errores HTTP del listado al coordinador de la pantalla', () => {
    const error = vi.fn();
    service.listar({ estado: 'ACTIVA' } as never).subscribe({ error });

    const request = http.expectOne(req => req.url === apiUrl);
    request.flush({ mensaje: 'Consulta rechazada' }, { status: 500, statusText: 'Server Error' });

    expect(error).toHaveBeenCalledWith(expect.objectContaining({ status: 500 }));
  });
});
