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

  it('consulta la familia predeterminada sin convertir configuracion ausente en 404', () => {
    const resultado = vi.fn();
    service.obtenerFamiliaPredeterminada().subscribe(resultado);
    const request = http.expectOne(`${apiUrl}/familias/predeterminada`);
    expect(request.request.method).toBe('GET');
    request.flush({ success: true, datos: { configurada: false, tieneVersionVigente: false } });
    expect(resultado).toHaveBeenCalledWith({ configurada: false, tieneVersionVigente: false });
  });

  it('establece una familia predeterminada con PUT y confirmacion institucional', () => {
    const resultado = vi.fn();
    service.establecerFamiliaPredeterminada(22).subscribe(resultado);
    const request = http.expectOne(`${apiUrl}/familias/22/predeterminada`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({});
    expect(request.request.headers.get(CONFIRMACION_CAMBIOS_HEADER)).toBe('1');
    request.flush({ success: true, datos: null });
    expect(resultado).toHaveBeenCalledWith(true);
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

  it('consulta consolidado paginado en servidor y conserva filtros y orden', () => {
    const resultado = vi.fn();
    service.obtenerConsolidadoPaginado({
      buscar: 'R-001',
      estadoEvaluacion: 'APROBADA',
      pagina: 2,
      tamanoPagina: 50,
      ordenarPor: 'vrr',
      orden: 'asc'
    }).subscribe(resultado);
    const request = http.expectOne(req => req.url === `${apiUrl}/consolidado/paginado`);
    expect(request.request.params.get('buscar')).toBe('R-001');
    expect(request.request.params.get('estadoEvaluacion')).toBe('APROBADA');
    expect(request.request.params.get('pagina')).toBe('2');
    expect(request.request.params.get('tamanoPagina')).toBe('50');
    expect(request.request.params.get('ordenarPor')).toBe('vrr');
    expect(request.request.params.get('orden')).toBe('asc');
    const pagina = { items: [], pagina: 2, tamanoPagina: 50, totalRegistros: 51, totalPaginas: 2, totales: { totalRiesgos: 1, totalConEvaluacionOficial: 1, totalSinEvaluacionOficial: 0, totalAltoCritico: 0 } };
    request.flush({ success: true, datos: pagina });
    expect(resultado).toHaveBeenCalledWith(pagina);
  });

  it('aplica los mismos filtros al exportar PDF y Excel', () => {
    const filtro = { buscar: 'R-002', estadoEvaluacion: 'CERRADA', ordenarPor: 'codigo', orden: 'desc' as const };
    service.descargarConsolidadoPdf(filtro).subscribe();
    const pdf = http.expectOne(req => req.url === `${apiUrl}/reportes/consolidado.pdf`);
    expect(pdf.request.params.get('buscar')).toBe('R-002');
    expect(pdf.request.params.get('estadoEvaluacion')).toBe('CERRADA');
    expect(pdf.request.params.get('ordenarPor')).toBe('codigo');
    pdf.flush(new Blob(['pdf']));

    service.descargarConsolidadoExcel(filtro).subscribe();
    const excel = http.expectOne(req => req.url === `${apiUrl}/reportes/consolidado.xlsx`);
    expect(excel.request.params.get('buscar')).toBe('R-002');
    expect(excel.request.params.get('estadoEvaluacion')).toBe('CERRADA');
    excel.flush(new Blob(['xlsx']));
  });

  it('lista riesgos maestros desde Oracle', () => {
    const observer = vi.fn();
    service.listarRiesgos().subscribe(observer);
    const request = http.expectOne(req => req.url === `${apiUrl}/riesgos`);
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('incluirInactivos')).toBe('false');
    request.flush({ success: true, datos: [{ rieId: 1, rieCodigo: 'R-001' }] });
    expect(observer).toHaveBeenCalledWith([{ rieId: 1, rieCodigo: 'R-001' }]);
  });

  it('crea riesgo maestro con confirmación de cambio', () => {
    const dto = { rieCodigo: 'R-001', rieNombre: 'Riesgo', rieDescripcion: 'Descripción', rieActivo: true };
    service.crearRiesgo(dto).subscribe();
    const request = http.expectOne(`${apiUrl}/riesgos`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(dto);
    expect(request.request.headers.get(CONFIRMACION_CAMBIOS_HEADER)).toBe('1');
    request.flush({ success: true, datos: 1 });
  });

  it('lista evaluaciones con paginación compatible con el backend', () => {
    service.listarEvaluaciones({ estado: 'BORRADOR', pagina: 2, registrosPorPagina: 25 }).subscribe();
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
    expect(request.request.headers.get('Content-Type')).toBe('application/json');
    request.flush({ success: true, datos: 22 });
    expect(resultado).toHaveBeenCalledWith(22);
  });

  it('crea control y plan mediante contratos reducidos de 17 tablas', () => {
    service.crearControl({
      conEvaluacionId: 15,
      conTipo: 'PREVENTIVO',
      conDescripcion: 'Control institucional',
      conAutomatizacion: 'MANUAL',
      conEstado: 'ACTIVO'
    }).subscribe();
    const control = http.expectOne(`${apiUrl}/mitigacion/controles`);
    expect(control.request.method).toBe('POST');
    expect(control.request.headers.get(CONFIRMACION_CAMBIOS_HEADER)).toBe('1');
    control.flush({ success: true, datos: 9 });

    service.crearPlan({
      plaEvaluacionId: 15,
      plaDescripcion: 'Mitigación',
      plaAvance: 0,
      plaPresupuesto: 1000,
      plaFechaInicio: '2026-08-07',
      plaFechaFin: '2026-09-07',
      plaEstado: 'ABIERTO'
    }).subscribe();
    const plan = http.expectOne(`${apiUrl}/mitigacion/planes`);
    expect(plan.request.method).toBe('POST');
    expect(plan.request.headers.get(CONFIRMACION_CAMBIOS_HEADER)).toBe('1');
    plan.flush({ success: true, datos: 4 });
  });

  it('opera alertas y automonitoreo por endpoints del Bloque 5', () => {
    service.crearAlerta({ aleEvaluacionId: 15, aleCodigo: 'ALE-01', aleIndicador: 'Indicador', aleEstado: 'ACTIVO' }).subscribe();
    const alerta = http.expectOne(`${apiUrl}/monitoreo/alertas`);
    expect(alerta.request.method).toBe('POST');
    expect(alerta.request.headers.get(CONFIRMACION_CAMBIOS_HEADER)).toBe('1');
    alerta.flush({ success: true, datos: 2 });

    service.registrarAutomonitoreo({
      monEvaluacionId: 15,
      monEstadoRiesgo: 'ALTO',
      monEstadoContr: 'EN_SEGUIMIENTO',
      monResultado: 'Sin novedades'
    }).subscribe();
    const monitor = http.expectOne(`${apiUrl}/monitoreo/automonitoreo`);
    expect(monitor.request.method).toBe('POST');
    expect(monitor.request.headers.get(CONFIRMACION_CAMBIOS_HEADER)).toBe('1');
    monitor.flush({ success: true, datos: 3 });
  });

  it('descarga los reportes consolidados como blobs Excel y PDF', () => {
    service.descargarConsolidadoExcel().subscribe();
    const excel = http.expectOne(`${apiUrl}/reportes/consolidado.xlsx`);
    expect(excel.request.method).toBe('GET');
    expect(excel.request.responseType).toBe('blob');
    excel.flush(new Blob(['xlsx']));

    service.descargarConsolidadoPdf().subscribe();
    const pdf = http.expectOne(`${apiUrl}/reportes/consolidado.pdf`);
    expect(pdf.request.method).toBe('GET');
    expect(pdf.request.responseType).toBe('blob');
    pdf.flush(new Blob(['pdf']));
  });

  it('carga y vincula una evidencia a una evaluación', () => {
    const archivo = new File(['evidencia'], 'evidencia.pdf', { type: 'application/pdf' });
    service.cargarEvidencia(archivo).subscribe();
    const carga = http.expectOne(`${apiUrl}/evidencias/cargar`);
    expect(carga.request.method).toBe('POST');
    expect(carga.request.body).toBeInstanceOf(FormData);
    expect(carga.request.headers.get(CONFIRMACION_CAMBIOS_HEADER)).toBe('1');
    carga.flush({ success: true, datos: { eviId: 8 } });

    service.vincularEvidencia({ evidenciaId: 8, tipoEntidad: 'evaluacion', entidadId: 15 }).subscribe();
    const vinculo = http.expectOne(`${apiUrl}/evidencias/vinculos`);
    expect(vinculo.request.method).toBe('POST');
    expect(vinculo.request.body).toEqual({ evidenciaId: 8, tipoEntidad: 'evaluacion', entidadId: 15 });
    expect(vinculo.request.headers.get(CONFIRMACION_CAMBIOS_HEADER)).toBe('1');
    vinculo.flush({ success: true });
  });

  it('no reintroduce métodos del modelo heredado retirado', () => {
    const metodos = service as unknown as Record<string, unknown>;
    expect('dashboard' in metodos).toBe(false);
    expect('listarCriterios' in metodos).toBe(false);
    expect('recalcular' in metodos).toBe(false);
    expect('guardarMatriz' in metodos).toBe(false);
  });
});
