import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { CONFIRMACION_CAMBIOS_HEADER } from '../../../../core/interceptors/confirmacion-cambios.interceptor';
import { MatricesRiesgosService } from './matrices-riesgos.service';

describe('MatricesRiesgosService cobertura complementaria', () => {
  let service: MatricesRiesgosService;
  let http: HttpTestingController;
  const apiUrl = 'http://localhost:5043/api/matrices-riesgos';

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(MatricesRiesgosService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    TestBed.resetTestingModule();
  });

  it('obtiene la versión vigente con la familia solicitada', () => {
    const observer = vi.fn();
    service.obtenerVersionVigenteFormulario('FORM_A').subscribe(observer);

    const request = http.expectOne(req => req.url === `${apiUrl}/formulario/version-vigente`);
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('familiaCodigo')).toBe('FORM_A');
    request.flush({ success: true, datos: { verId: 10 } });
    expect(observer).toHaveBeenCalledWith({ verId: 10 });
  });

  it('lista el historial de versiones', () => {
    const observer = vi.fn();
    service.listarHistorialVersionesFormulario('FORM_A').subscribe(observer);

    const request = http.expectOne(req => req.url === `${apiUrl}/formularios/historial`);
    expect(request.request.params.get('familiaCodigo')).toBe('FORM_A');
    request.flush({ success: true, datos: [{ verId: 10 }] });
    expect(observer).toHaveBeenCalledWith([{ verId: 10 }]);
  });

  it('clona una versión con confirmación', () => {
    const observer = vi.fn();
    service.clonarVersionFormulario(10).subscribe(observer);

    const request = http.expectOne(`${apiUrl}/formularios/10/clonar`);
    expect(request.request.method).toBe('POST');
    expect(request.request.headers.get(CONFIRMACION_CAMBIOS_HEADER)).toBe('1');
    request.flush({ success: true, datos: 11 });
    expect(observer).toHaveBeenCalledWith(11);
  });

  it('actualiza un borrador de formulario', () => {
    service.actualizarBorradorFormulario(10, '{"secciones":[]}').subscribe();

    const request = http.expectOne(`${apiUrl}/formularios/10`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toBe('{"secciones":[]}');
    expect(request.request.headers.get(CONFIRMACION_CAMBIOS_HEADER)).toBe('1');
    request.flush({ success: true });
  });

  it('publica una versión de formulario', () => {
    service.publicarVersionFormulario(10).subscribe();

    const request = http.expectOne(`${apiUrl}/formularios/10/publicar`);
    expect(request.request.method).toBe('POST');
    expect(request.request.headers.get(CONFIRMACION_CAMBIOS_HEADER)).toBe('1');
    request.flush({ success: true });
  });

  it('cambia la vigencia de una versión', () => {
    service.cambiarVigenciaFormulario(10, false).subscribe();

    const request = http.expectOne(req => req.url === `${apiUrl}/formularios/10/estado`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.params.get('vigente')).toBe('false');
    expect(request.request.headers.get(CONFIRMACION_CAMBIOS_HEADER)).toBe('1');
    request.flush({ success: true });
  });

  it('obtiene una evaluación por identificador', () => {
    const observer = vi.fn();
    service.obtenerEvaluacion(15).subscribe(observer);

    const request = http.expectOne(`${apiUrl}/evaluaciones/15`);
    expect(request.request.method).toBe('GET');
    request.flush({ success: true, datos: { evaId: 15 } });
    expect(observer).toHaveBeenCalledWith({ evaId: 15 });
  });

  it('actualiza una evaluación con confirmación', () => {
    const dto = {
      evaId: 15,
      evaRiesgoId: 5,
      evaVersionId: 10,
      evaEstado: 'BORRADOR',
      evaDataJson: '{}',
      evaFechaEval: '2026-08-03T10:00:00',
      evaUsrEval: 1,
      evaVersionRow: 2,
      evaActivo: true
    };

    service.actualizarEvaluacion(15, dto).subscribe();

    const request = http.expectOne(`${apiUrl}/evaluaciones/15`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual(dto);
    expect(request.request.headers.get(CONFIRMACION_CAMBIOS_HEADER)).toBe('1');
    request.flush({ success: true });
  });

  it('transiciona sin motivo cuando no se proporciona', () => {
    service.transicionarEvaluacion(15, 'EN_REVISION').subscribe();

    const request = http.expectOne(req => req.url === `${apiUrl}/evaluaciones/15/transiciones`);
    expect(request.request.params.get('nuevoEstado')).toBe('EN_REVISION');
    expect(request.request.params.has('motivo')).toBe(false);
    request.flush({ success: true });
  });

  it('obtiene el historial de flujos de una evaluacion', () => {
    const observer = vi.fn();
    service.obtenerFlujos(15).subscribe(observer);

    const request = http.expectOne(`${apiUrl}/evaluaciones/15/flujos`);
    request.flush({ success: true, datos: [{ fluId: 1, fluEstado: 'APROBADA' }] });
    expect(observer).toHaveBeenCalledWith([{ fluId: 1, fluEstado: 'APROBADA' }]);
  });

  const vinculaciones = [
    ['riesgo', 'vincularEvidenciaRiesgo', { evrRiesgoId: 1, evrEvidenciaId: 8 }],
    ['control', 'vincularEvidenciaControl', { evcControlId: 2, evcEvidenciaId: 8 }],
    ['plan', 'vincularEvidenciaPlan', { evpPlanId: 3, evpEvidenciaId: 8 }],
    ['actividad', 'vincularEvidenciaActividad', { evaActividadId: 4, evaEvidenciaId: 8 }],
    ['alerta', 'vincularEvidenciaAlerta', { evaAlertaId: 5, evaEvidenciaId: 8 }],
    ['automonitoreo', 'vincularEvidenciaAutomonitoreo', { evmMonitoreoId: 6, evmEvidenciaId: 8 }],
    ['aprobacion', 'vincularEvidenciaAprobacion', { evapAprobacionId: 9, evapEvidenciaId: 8 }]
  ] as const;

  it.each(vinculaciones)('vincula evidencia a %s', (tipo, metodo, dto) => {
    const invocacion = service[metodo] as (valor: typeof dto) => ReturnType<MatricesRiesgosService[typeof metodo]>;
    invocacion.call(service, dto).subscribe();

    const request = http.expectOne(`${apiUrl}/evidencias/vincular/${tipo}`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(dto);
    expect(request.request.headers.get(CONFIRMACION_CAMBIOS_HEADER)).toBe('1');
    request.flush({ success: true });
  });

  it('elimina una evidencia huérfana con confirmación', () => {
    service.eliminarEvidenciaHuerfana(8).subscribe();

    const request = http.expectOne(`${apiUrl}/evidencias/8`);
    expect(request.request.method).toBe('DELETE');
    expect(request.request.headers.get(CONFIRMACION_CAMBIOS_HEADER)).toBe('1');
    request.flush({ success: true });
  });

  it('consulta la política institucional de evidencias', () => {
    const observer = vi.fn();
    service.obtenerPoliticaEvidencias().subscribe(observer);

    const request = http.expectOne('http://localhost:5043/api/listas/evidencias/politica');
    request.flush({ success: true, datos: { maximoMb: 10 } });
    expect(observer).toHaveBeenCalledWith({ maximoMb: 10 });
  });
});
