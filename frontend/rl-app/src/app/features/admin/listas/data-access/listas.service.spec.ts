import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { HTTP_ERROR_SCOPE } from '../../../../core/http/http-error-scope';
import {
  CONFIRMACION_BOTON_HEADER,
  CONFIRMACION_CAMBIOS_HEADER,
  CONFIRMACION_TITULO_HEADER
} from '../../../../core/interceptors/confirmacion-cambios.interceptor';
import { ListasService } from './listas.service';

describe('ListasService', () => {
  let service: ListasService;
  let http: HttpTestingController;
  const apiUrl = 'http://localhost:5043/api/listas';

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(ListasService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    try {
      http.verify();
    } finally {
      TestBed.resetTestingModule();
    }
  });

  it('obtiene únicamente la página de coincidencias jurídicas', () => {
    const juridicas = { items: [{ numeroPatrono: 'P-01' }], pagina: 1, tamanoPagina: 10, totalRegistros: 1, totalPaginas: 1 } as never;
    const result = vi.fn();
    service.getJuridicasPaginadas({ pagina: 1, tamanoPagina: 10 }).subscribe(result);

    const request = http.expectOne(req => req.url === `${apiUrl}/juridicas/paginado`);
    expect(request.request.method).toBe('GET');
    request.flush({ success: true, datos: juridicas });
    expect(result).toHaveBeenCalledWith(juridicas);
  });

  it('envia el rango opcional al consultar seguimientos', () => {
    service.getSeguimientos('ID 10', '2026-07-01', '2026-07-15').subscribe();

    const request = http.expectOne(req => req.url === `${apiUrl}/positivos/ID%2010/seguimientos`);
    expect(request.request.params.get('noDocumento')).toBeNull();
    expect(request.request.params.get('desde')).toBe('2026-07-01');
    expect(request.request.params.get('hasta')).toBe('2026-07-15');
    request.flush({ success: true, datos: [] });
  });

  it('preserves global default and local overrides for shared detail queries', () => {
    service.getDetalleNatural('0801').subscribe();
    const globalDetail = http.expectOne(req => req.url === `${apiUrl}/naturales/0801/detalle`);
    expect(globalDetail.request.context.get(HTTP_ERROR_SCOPE)).toBe('global');
    globalDetail.flush({ success: true, datos: [] });

    service.getDetalleNatural('0801', 'local').subscribe();
    const localDetail = http.expectOne(req => req.url === `${apiUrl}/naturales/0801/detalle`);
    expect(localDetail.request.context.get(HTTP_ERROR_SCOPE)).toBe('local');
    localDetail.flush({ success: true, datos: [] });

    service.getTiposListasCautela('local').subscribe();
    const localTypes = http.expectOne(`${apiUrl}/tipos-listas-cautela`);
    expect(localTypes.request.context.get(HTTP_ERROR_SCOPE)).toBe('local');
    localTypes.flush({ success: true, datos: [] });
  });

  it('uses local scope for list exports and contextual mutations with local error feedback', () => {
    const requests = [
      { run: () => service.getJuridicasParaExportar({ pagina: 1, tamanoPagina: 10 }), url: `${apiUrl}/juridicas/exportar`, method: 'GET' },
      { run: () => service.getNaturalesParaExportar({ pagina: 1, tamanoPagina: 10 }), url: `${apiUrl}/naturales/exportar`, method: 'GET' },
      { run: () => service.getEmpleadosParaExportar({ pagina: 1, tamanoPagina: 10 }), url: `${apiUrl}/empleados/exportar`, method: 'GET' },
      { run: () => service.actualizarSeguimiento(4, 'Actualización', []), url: `${apiUrl}/seguimientos/4`, method: 'PUT' },
      { run: () => service.crearTipoListaCautela('Tipo', null, null), url: `${apiUrl}/tipos-listas-cautela`, method: 'POST' },
      { run: () => service.actualizarTipoListaCautela(4, 'Tipo', null, null), url: `${apiUrl}/tipos-listas-cautela/4`, method: 'PUT' }
    ];

    for (const item of requests) {
      item.run().subscribe();
      const request = http.expectOne(req => req.url === item.url);
      expect(request.request.method).toBe(item.method);
      expect(request.request.context.get(HTTP_ERROR_SCOPE)).toBe('local');
      request.flush(item.method === 'GET' ? { success: true, datos: [] } : { success: true, mensaje: 'ok' });
    }
  });

  it('construye el formulario documental y configura la confirmacion del seguimiento', () => {
    const evidencia = new File(['contenido'], 'evidencia.txt', { type: 'text/plain' });
    service.registrarSeguimiento('0801', 'Revision mensual', [evidencia]).subscribe();

    const request = http.expectOne(req => req.url === `${apiUrl}/positivos/0801/seguimientos`);
    const formData = request.request.body as FormData;
    expect(request.request.method).toBe('POST');
    expect(request.request.context.get(HTTP_ERROR_SCOPE)).toBe('local');
    expect(formData.get('motivoIngreso')).toBe('Revision mensual');
    expect((formData.get('archivos') as File).name).toBe('evidencia.txt');
    expect(request.request.headers.get(CONFIRMACION_TITULO_HEADER)).toBe('Confirmar seguimiento');
    expect(request.request.headers.get(CONFIRMACION_BOTON_HEADER)).toBeTruthy();
    request.flush({ success: true, mensaje: 'Registrado' });
  });

  it('consulta el detalle fuente de N/A(3), pero mantiene bloqueadas las operaciones sensibles', () => {
    const detalle = vi.fn();
    const positivo = vi.fn();
    const seguimientos = vi.fn();

    service.getDetalleNatural('N/A(3)').subscribe(detalle);
    const detalleRequest = http.expectOne(req => req.url === `${apiUrl}/naturales/detalle`);
    expect(detalleRequest.request.params.get('numeroIdentificacion')).toBe('N/A(3)');
    detalleRequest.flush({ success: true, datos: [{ numeroIdentificacion: 'N/A(3)' }] });

    service.getPositivoPorDocumento('N/A(3)').subscribe(positivo);
    service.getSeguimientos('N/A(3)').subscribe(seguimientos);

    expect(detalle).toHaveBeenCalledWith([{ numeroIdentificacion: 'N/A(3)' }]);
    expect(positivo).toHaveBeenCalledWith(null);
    expect(seguimientos).toHaveBeenCalledWith([]);
  });

  it('consulta una fuente exacta aunque el DNI sea N/A(3)', () => {
    service.getDetalleNatural('N/A(3)', 'local', 91, 'DNP_COINCIDENCIA').subscribe();
    const request = http.expectOne(req => req.url === `${apiUrl}/naturales/detalle`);
    expect(request.request.params.get('idRegistroFuente')).toBe('91');
    expect(request.request.params.get('tipoFuente')).toBe('DNP_COINCIDENCIA');
    expect(request.request.params.get('numeroIdentificacion')).toBe('N/A(3)');
    expect(request.request.context.get(HTTP_ERROR_SCOPE)).toBe('local');
    request.flush({ success: true, datos: [] });
  });

  it('recupera por ID un registro de origen con documento nulo o vacío', () => {
    service.getDetalleNatural('', 'local', 93, 'DNP_COINCIDENCIA').subscribe();
    const request = http.expectOne(req => req.url === `${apiUrl}/naturales/detalle`);
    expect(request.request.params.get('numeroIdentificacion')).toBe('');
    expect(request.request.params.get('idRegistroFuente')).toBe('93');
    expect(request.request.params.get('tipoFuente')).toBe('DNP_COINCIDENCIA');
    request.flush({ success: true, datos: [{ nombresPersona: 'Fuente sin identificación' }] });
  });

  it('preserva la referencia de empleado y su periodo historico', () => {
    service.getDetalleEmpleado('0801', 'local', 27, 'DNP_COINCIDENCIA', '2026-07-01').subscribe();
    const request = http.expectOne(req => req.url === `${apiUrl}/empleados/detalle`);
    expect(request.request.params.get('idRegistroFuente')).toBe('27');
    expect(request.request.params.get('periodoPlanilla')).toBe('2026-07-01');
    request.flush({ success: true, datos: [] });
  });

  it('filtra la categoria de naturales y planilla historica por separado', () => {
    service.getNaturalesPaginadas({ pagina: 1, tamanoPagina: 10, categoriaIdentidad: 'sin_identificacion' }).subscribe();
    const naturales = http.expectOne(req => req.url === `${apiUrl}/naturales/paginado`);
    expect(naturales.request.params.get('categoriaIdentidad')).toBe('sin_identificacion');
    expect(naturales.request.params.has('periodoPlanilla')).toBe(false);
    naturales.flush({ success: true, datos: { items: [], pagina: 1, totalRegistros: 0, totalPaginas: 0 } });

    service.getEmpleadosPaginadas({ pagina: 1, tamanoPagina: 10, periodoPlanilla: '2026-07-01' }).subscribe();
    const empleados = http.expectOne(req => req.url === `${apiUrl}/empleados/paginado`);
    expect(empleados.request.params.get('periodoPlanilla')).toBe('2026-07-01');
    expect(empleados.request.params.has('categoriaIdentidad')).toBe(false);
    empleados.flush({ success: true, datos: { items: [], pagina: 1, totalRegistros: 0, totalPaginas: 0 } });
  });

  it('transmite el rango histórico tanto en paginación como en exportación de empleados', () => {
    const consulta = { pagina: 1, tamanoPagina: 10, periodoPlanillaDesde: '2026-01-01', periodoPlanillaHasta: '2026-07-01' };
    service.getEmpleadosPaginadas(consulta).subscribe();
    const lista = http.expectOne(req => req.url === `${apiUrl}/empleados/paginado`);
    expect(lista.request.params.get('periodoPlanillaDesde')).toBe('2026-01-01');
    expect(lista.request.params.get('periodoPlanillaHasta')).toBe('2026-07-01');
    expect(lista.request.params.has('periodoPlanilla')).toBe(false);
    lista.flush({ success: true, datos: { items: [], pagina: 1, totalRegistros: 0, totalPaginas: 0 } });

    service.getEmpleadosParaExportar(consulta).subscribe();
    const exportacion = http.expectOne(req => req.url === `${apiUrl}/empleados/exportar`);
    expect(exportacion.request.params.get('periodoPlanillaDesde')).toBe('2026-01-01');
    expect(exportacion.request.params.get('periodoPlanillaHasta')).toBe('2026-07-01');
    exportacion.flush({ success: true, datos: [] });
  });

  it('usa la ruta canónica para consultar el positivo de un documento real', () => {
    service.getPositivoPorDocumento('101196100011').subscribe();

    const request = http.expectOne(`${apiUrl}/positivos/101196100011`);
    expect(request.request.method).toBe('GET');
    request.flush({ success: true, datos: null });
  });

  it('mantiene la auditoria de reportes para N/A(3) mediante el contrato de consulta', () => {
    service.registrarAuditoriaImpresion('N/A(3)', { tipoReporte: 'PDF_PERFIL_FICHA' }).subscribe();

    const request = http.expectOne(req => req.url === `${apiUrl}/reporte-impreso`);
    expect(request.request.params.get('noDocumento')).toBe('N/A(3)');
    request.flush({ success: true });
  });

  it('elimina evidencia con motivo y marca la solicitud como confirmada', () => {
    service.eliminarEvidencia(27, 'Documento duplicado').subscribe();

    const request = http.expectOne(`${apiUrl}/evidencias/27`);
    expect(request.request.method).toBe('DELETE');
    expect(request.request.body).toEqual({ motivoEliminacion: 'Documento duplicado' });
    expect(request.request.headers.get(CONFIRMACION_CAMBIOS_HEADER)).toBe('1');
    request.flush({ success: true });
  });

  it('registra la auditoria de exportacion sin una segunda confirmacion', () => {
    const detalle = { formato: 'PDF', cantidad: 4 };
    service.registrarAuditoriaExportacion('RL_LISTA', '18', 'Listas', detalle).subscribe();

    const request = http.expectOne('http://localhost:5043/api/auditoria/exportacion');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      tabla: 'RL_LISTA', registroId: '18', modulo: 'Listas', detalle
    });
    expect(request.request.headers.get(CONFIRMACION_CAMBIOS_HEADER)).toBe('1');
    request.flush({ success: true });
  });

  it('propaga un error HTTP de consulta para que el componente pueda recuperarse', () => {
    const error = vi.fn();
    service.getJuridicasPaginadas({ pagina: 1, tamanoPagina: 10 }).subscribe({ error });

    const request = http.expectOne(req => req.url === `${apiUrl}/juridicas/paginado`);
    request.flush({ mensaje: 'Servicio no disponible' }, { status: 503, statusText: 'Unavailable' });

    expect(error).toHaveBeenCalledWith(expect.objectContaining({ status: 503 }));
  });
});
