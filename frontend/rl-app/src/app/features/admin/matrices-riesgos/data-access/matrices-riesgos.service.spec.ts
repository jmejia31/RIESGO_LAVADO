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

  it('consulta la metodología dinámica vigente', () => {
    const resultado = vi.fn();
    const metodologia = {
      versionFormularioId: 10,
      codigo: 'FORM_A',
      version: 2,
      secciones: [],
      catalogos: [],
      reglas: []
    };

    service.metodologiaVigente().subscribe(resultado);

    const request = http.expectOne(`${apiUrl}/metodologia/vigente`);
    expect(request.request.method).toBe('GET');
    request.flush({ success: true, datos: metodologia });
    expect(resultado).toHaveBeenCalledWith(metodologia);
  });

  it('consulta el consolidado mediante filas tipadas', () => {
    const resultado = vi.fn();
    const filas = [{
      riesgoId: 1,
      evaluacionId: 2,
      versionFormularioId: 10,
      codigoRiesgo: 'R-001',
      areaPrincipal: 'Cumplimiento',
      duenoRiesgo: 'Responsable',
      vri: 7,
      nivelInherente: 'ALTO',
      vrr: 4,
      nivelResidual: 'MODERADO',
      respuestaRiesgo: 'MITIGAR',
      estadoEvaluacion: 'APROBADA',
      fechaEvaluacion: '2026-08-03T10:00:00'
    }];

    service.obtenerConsolidado().subscribe(resultado);

    const request = http.expectOne(`${apiUrl}/consolidado`);
    expect(request.request.method).toBe('GET');
    request.flush({ success: true, datos: filas });
    expect(resultado).toHaveBeenCalledWith(filas);
  });

  it('lista evaluaciones con paginación compatible con el backend', () => {
    service.listarEvaluaciones({
      estado: 'BORRADOR',
      pagina: 2,
      registrosPorPagina: 25
    }).subscribe();

    const request = http.expectOne(req => req.url === `${apiUrl}/evaluaciones`);
    expect(request.request.params.get('estado')).toBe('BORRADOR');
    expect(request.request.params.get('pagina')).toBe('2');
    expect(request.request.params.get('registrosPorPagina')).toBe('25');
    request.flush({ success: true, datos: [] });
  });

  it('crea una evaluación con confirmación de operación sensible', () => {
    const dto = {
      evaId: 0,
      evaRiesgoId: 4,
      evaVersionId: 10,
      evaEstado: 'BORRADOR',
      evaDataJson: '{}',
      evaDataCalcJson: '{}',
      evaFechaEval: '2026-08-03T10:00:00',
      evaUsrEval: 1,
      evaVersionRow: 1,
      evaActivo: true
    };

    service.crearEvaluacion(dto).subscribe();

    const request = http.expectOne(`${apiUrl}/evaluaciones`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(dto);
    expect(request.request.headers.get(CONFIRMACION_CAMBIOS_HEADER)).toBe('1');
    request.flush({ success: true, datos: 15 });
  });

  it('transiciona una evaluación mediante el endpoint canónico', () => {
    service.transicionarEvaluacion(15, 'EN_REVISION', 'Captura completada').subscribe();

    const request = http.expectOne(req => req.url === `${apiUrl}/evaluaciones/15/transiciones`);
    expect(request.request.method).toBe('POST');
    expect(request.request.params.get('nuevoEstado')).toBe('EN_REVISION');
    expect(request.request.params.get('motivo')).toBe('Captura completada');
    expect(request.request.headers.get(CONFIRMACION_CAMBIOS_HEADER)).toBe('1');
    request.flush({ success: true });
  });

  it('crea un borrador de formulario con familia y código', () => {
    const resultado = vi.fn();
    service.crearBorradorFormulario(1, 'FORM_A', '{"secciones":[]}').subscribe(resultado);

    const request = http.expectOne(req => req.url === `${apiUrl}/formularios/borrador`);
    expect(request.request.method).toBe('POST');
    expect(request.request.params.get('familiaId')).toBe('1');
    expect(request.request.params.get('codigoFormulario')).toBe('FORM_A');
    expect(request.request.headers.get(CONFIRMACION_CAMBIOS_HEADER)).toBe('1');
    request.flush({ success: true, datos: 22 });
    expect(resultado).toHaveBeenCalledWith(22);
  });

  it('carga y vincula una evidencia a una evaluación', () => {
    const archivo = new File(['evidencia'], 'evidencia.pdf', { type: 'application/pdf' });
    service.cargarEvidencia(archivo).subscribe();

    const carga = http.expectOne(`${apiUrl}/evidencias/cargar`);
    expect(carga.request.method).toBe('POST');
    expect(carga.request.body instanceof FormData).toBe(true);
    expect(carga.request.headers.get(CONFIRMACION_CAMBIOS_HEADER)).toBe('1');
    carga.flush({ success: true, datos: { eviId: 8 } });

    service.vincularEvidencia({ evidenciaId: 8, tipoEntidad: 'evaluacion', entidadId: 15 }).subscribe();
    const vinculo = http.expectOne(`${apiUrl}/evidencias/vinculos`);
    expect(vinculo.request.method).toBe('POST');
    expect(vinculo.request.body).toEqual({ evidenciaId: 8, tipoEntidad: 'evaluacion', entidadId: 15 });
    expect(vinculo.request.headers.get(CONFIRMACION_CAMBIOS_HEADER)).toBe('1');
    vinculo.flush({ success: true });
  });

  it('no expone métodos del modelo heredado', () => {
    const metodos = service as unknown as Record<string, unknown>;
    expect('dashboard' in metodos).toBe(false);
    expect('listarCriterios' in metodos).toBe(false);
    expect('crearPlan' in metodos).toBe(false);
    expect('recalcular' in metodos).toBe(false);
  });
});
