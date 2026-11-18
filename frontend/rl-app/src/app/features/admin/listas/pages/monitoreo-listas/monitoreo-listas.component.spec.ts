import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DomSanitizer } from '@angular/platform-browser';
import { of, throwError } from 'rxjs';
import Swal from 'sweetalert2';
import { ConfiguracionService } from '../../../../../core/configuration/configuracion.service';
import { ListasService } from '../../data-access/listas.service';
import { InstitutionalConfirmationService } from '../../../../../shared/confirmation/institutional-confirmation.service';
import { MonitoreoListasComponent } from './monitoreo-listas.component';

vi.mock('sweetalert2', () => ({
  default: { fire: vi.fn(), showLoading: vi.fn(), close: vi.fn() }
}));

describe('MonitoreoListasComponent', () => {
  let fixture: ComponentFixture<MonitoreoListasComponent>;
  let component: MonitoreoListasComponent;
  let service: Record<string, ReturnType<typeof vi.fn>>;
  let confirmation: { confirmWithInput: ReturnType<typeof vi.fn> };
  const fire = vi.mocked(Swal.fire);
  const close = vi.mocked(Swal.close);
  let createObjectUrl: ReturnType<typeof vi.fn>;
  let revokeObjectUrl: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    service = {
      getJuridicas: vi.fn(() => of([])),
      getNaturales: vi.fn(() => of([])),
      getEmpleados: vi.fn(() => of([])),
      getJuridicasPaginadas: vi.fn(() => of({ items: [], pagina: 1, tamanoPagina: 10, totalRegistros: 0, totalPaginas: 0, totales: { totalRegistros: 0, pendientes: 0, conMotivo: 0, manuales: 0, cerradosPasivos: 0 } })),
      getJuridicasParaExportar: vi.fn(() => of([])),
      getNaturalesPaginadas: vi.fn(() => of({ items: [], pagina: 1, tamanoPagina: 10, totalRegistros: 0, totalPaginas: 0, totales: { totalRegistros: 0, pendientes: 0, conMotivo: 0, manuales: 0, cerradosPasivos: 0 } })),
      getNaturalesParaExportar: vi.fn(() => of([])),
      getEmpleadosPaginadas: vi.fn(() => of({ items: [], pagina: 1, tamanoPagina: 10, totalRegistros: 0, totalPaginas: 0, totales: { totalRegistros: 0, pendientes: 0, conMotivo: 0, manuales: 0, cerradosPasivos: 0 } })),
      getEmpleadosParaExportar: vi.fn(() => of([])),
      getTiposDocumento: vi.fn(() => of([])),
      getTiposListasCautela: vi.fn(() => of([])),
      getPoliticaEvidencias: vi.fn(() => of({
        maximoMb: 10,
        maximoBytes: 10 * 1024 * 1024,
        extensionesPermitidas: [],
        tiposPermitidosTexto: 'Archivos permitidos'
      })),
      getSeguimientos: vi.fn(() => of([])),
      registrarPositivo: vi.fn(() => of({ mensaje: 'Registrado' })),
      registrarSeguimiento: vi.fn(() => of({ mensaje: 'Seguimiento registrado' })),
      actualizarSeguimiento: vi.fn(() => of({ mensaje: 'Seguimiento actualizado' })),
      eliminarEvidencia: vi.fn(() => of({ mensaje: 'Evidencia eliminada' })),
      eliminarSeguimiento: vi.fn(() => of({ mensaje: 'Seguimiento eliminado' })),
      descargarEvidenciaBlob: vi.fn(() => of(new Blob())),
      getPositivoPorDocumento: vi.fn(() => of(null)),
      getDetalleNatural: vi.fn(() => of([])),
      getDetalleEmpleado: vi.fn(() => of([])),
      registrarAuditoriaImpresion: vi.fn(() => of({ success: true })),
      registrarAuditoriaExportacion: vi.fn(() => of({ success: true }))
    };
    fire.mockReset();
    close.mockReset();
    confirmation = { confirmWithInput: vi.fn().mockResolvedValue(null) };
    createObjectUrl = vi.fn(() => 'blob:evidencia-prueba');
    revokeObjectUrl = vi.fn();
    Object.defineProperty(URL, 'createObjectURL', { configurable: true, value: createObjectUrl });
    Object.defineProperty(URL, 'revokeObjectURL', { configurable: true, value: revokeObjectUrl });

    await TestBed.configureTestingModule({
      imports: [MonitoreoListasComponent],
      providers: [
        { provide: ListasService, useValue: service },
        { provide: InstitutionalConfirmationService, useValue: confirmation },
        { provide: ConfiguracionService, useValue: { configSistema: vi.fn(() => null) } },
        { provide: DomSanitizer, useValue: { bypassSecurityTrustResourceUrl: vi.fn(value => value) } }
      ]
    })
      .overrideComponent(MonitoreoListasComponent, { set: { template: '' } })
      .compileComponents();

    fixture = TestBed.createComponent(MonitoreoListasComponent);
    component = fixture.componentInstance;
  });

  afterEach(() => {
    fixture.destroy();
    vi.restoreAllMocks();
    TestBed.resetTestingModule();
  });

  it('carga las coincidencias juridicas y finaliza el indicador', () => {
    const juridicas = [{ numeroPatrono: 'P-01', nombre: 'Empresa Uno' }];
    service['getJuridicasPaginadas'].mockReturnValue(of({ items: juridicas, pagina: 1, tamanoPagina: 10, totalRegistros: juridicas.length, totalPaginas: 1, totales: { totalRegistros: juridicas.length, pendientes: 0, conMotivo: 0, manuales: 0, cerradosPasivos: 0 } }));

    component.cargarDatos();

    expect(service['getJuridicasPaginadas']).toHaveBeenCalledOnce();
    expect(component.juridicasRaw()).toEqual(juridicas);
    expect(component.cargando()).toBe(false);
  });

  it('cambia a personas naturales, limpia la busqueda y recarga el servicio correcto', () => {
    const naturales = [{ numeroIdentificacion: '0801', nombre: 'Ana' }];
    service['getNaturalesPaginadas'].mockReturnValue(of({ items: naturales, pagina: 1, tamanoPagina: 10, totalRegistros: naturales.length, totalPaginas: 1, totales: { totalRegistros: naturales.length, pendientes: 0, conMotivo: 0, manuales: 0, cerradosPasivos: 0 } }));
    component.busqueda.set('anterior');
    component.paginaActual.set(4);

    component.cambiarTipo('natural');

    expect(component.tipoActivo()).toBe('natural');
    expect(component.busqueda()).toBe('');
    expect(component.paginaActual()).toBe(1);
    expect(service['getNaturalesPaginadas']).toHaveBeenCalledOnce();
    expect(component.naturalesRaw()).toEqual(naturales);
  });

  it('no repite la misma consulta al volver a la categoría con el mismo contexto', () => {
    const naturales = [{ numeroIdentificacion: '0801', nombre: 'Ana' }];
    service['getNaturalesPaginadas'].mockReturnValue(of({ items: naturales, pagina: 1, tamanoPagina: 10, totalRegistros: naturales.length, totalPaginas: 1, totales: { totalRegistros: naturales.length, pendientes: 0, conMotivo: 0, manuales: 0, cerradosPasivos: 0 } }));

    component.cambiarTipo('natural');
    component.busqueda.set('ana');
    component.cambiarTipo('natural');

    expect(service['getNaturalesPaginadas']).toHaveBeenCalledOnce();
    expect(component.naturalesRaw()).toEqual(naturales);
    expect(component.busqueda()).toBe('');
    expect(component.cargando()).toBe(false);
  });

  it('permite una recarga explícita después de una mutación', () => {
    component.cargarDatos();
    component.cargarDatos({ force: true, mostrarLoader: false });

    expect(service['getJuridicasPaginadas']).toHaveBeenCalledTimes(2);
    expect(component.cargando()).toBe(false);
  });

  it('M1: permite consultar por separado marcadores sin identificación y conserva el filtro en exportación', () => {
    component.cambiarTipo('natural');
    component.cambiarCategoriaIdentidad('sin_identificacion');

    expect(component.categoriaIdentidad()).toBe('sin_identificacion');
    expect(service['getNaturalesPaginadas']).toHaveBeenLastCalledWith(
      expect.objectContaining({ categoriaIdentidad: 'sin_identificacion' })
    );
    expect(component.obtenerConsultaMonitoreoActual().categoriaIdentidad).toBe('sin_identificacion');

    component.limpiarFiltrosPrincipales();
    expect(component.categoriaIdentidad()).toBe('identificadas');
  });

  it('M1: filtra empleados por planilla sin alterar el período de la coincidencia', () => {
    component.cambiarTipo('empleado');
    component.cambiarPeriodoPlanilla('2026-07');
    expect(component.periodoPlanilla()).toBe('2026-07');
    expect(service['getEmpleadosPaginadas']).toHaveBeenLastCalledWith(
      expect.objectContaining({ periodoPlanilla: '2026-07-01' })
    );
    expect(component.obtenerConsultaMonitoreoActual().periodoPlanilla).toBe('2026-07-01');

    component.cambiarPeriodoPlanilla('2026-99');
    expect(component.periodoPlanilla()).toBe('2026-07');
  });

  it('M1: rango historico inclusivo aplica a listado y exportacion sin multiplicar coincidencias', () => {
    component.cambiarTipo('empleado');
    component.cambiarRangoPlanilla('desde', '2026-01');
    component.cambiarRangoPlanilla('hasta', '2026-07');
    expect(component.rangoPlanillaInvalido()).toBe(false);
    expect(component.periodoPlanilla()).toBe('');
    expect(service['getEmpleadosPaginadas']).toHaveBeenLastCalledWith(
      expect.objectContaining({ periodoPlanilla: null, periodoPlanillaDesde: '2026-01-01', periodoPlanillaHasta: '2026-07-01' })
    );
    expect(component.obtenerConsultaMonitoreoActual()).toEqual(
      expect.objectContaining({ periodoPlanillaDesde: '2026-01-01', periodoPlanillaHasta: '2026-07-01' })
    );
    component.cambiarPeriodoPlanilla('2026-03');
    expect(component.periodoPlanillaDesde()).toBe('');
    expect(component.periodoPlanillaHasta()).toBe('');
    expect(component.obtenerConsultaMonitoreoActual().periodoPlanilla).toBe('2026-03-01');
  });

  it('M1: rango invertido no envia consulta ni devuelve lista vacia falsa', () => {
    component.cambiarTipo('empleado');
    component.cambiarRangoPlanilla('desde', '2026-09');
    const consultas = service['getEmpleadosPaginadas'].mock.calls.length;
    component.cambiarRangoPlanilla('hasta', '2026-07');
    expect(component.rangoPlanillaInvalido()).toBe(true);
    expect(component.errorMonitoreo()).toContain('mes inicial');
    expect(service['getEmpleadosPaginadas']).toHaveBeenCalledTimes(consultas);
    component.cambiarRangoPlanilla('hasta', '2026-10');
    expect(component.rangoPlanillaInvalido()).toBe(false);
    expect(component.errorMonitoreo()).toBeNull();
    expect(service['getEmpleadosPaginadas']).toHaveBeenLastCalledWith(
      expect.objectContaining({ periodoPlanillaDesde: '2026-09-01', periodoPlanillaHasta: '2026-10-01' })
    );
  });

  it('M1: la ficha manual conserva la referencia real de origen incluso con N/A(3)', () => {
    component.tipoActivo.set('natural');
    const fila = {
      numeroIdentificacion: 'N/A(3)',
      nombre: 'Registro fuente',
      idRegistroFuente: 72,
      tipoFuente: 'MANUAL_CUMPLIMIENTO',
      esManual: true
    } as never;
    service['getDetalleNatural'].mockReturnValue(of([
      { numeroIdentificacion: 'N/A(3)', nombresPersona: 'Registro fuente' }
    ]));

    component.abrirDetalle(fila);
    expect(service['getDetalleNatural']).toHaveBeenCalledWith(
      'N/A(3)', 'local', 72, 'MANUAL_CUMPLIMIENTO'
    );
    expect(component.modalDetalleAbierto()).toBe(true);
    expect(component.detallesNatural()).toHaveLength(1);
  });

  it('conserva resultados de empleados y expone el error cuando el servicio falla', () => {
    component.tipoActivo.set('empleado');
    component.empleadosRaw.set([{ identidad: '01' }] as never);
    service['getEmpleadosPaginadas'].mockReturnValue(throwError(() => new Error('API no disponible')));

    component.cargarDatos();

    expect(component.empleadosRaw()).toEqual([{ identidad: '01' }]);
    expect(component.errorMonitoreo()).toContain('No se pudieron cargar');
    expect(component.cargando()).toBe(false);
  });

  it('consulta seguimientos con el rango seleccionado y actualiza el historial', () => {
    const seguimientos = [{ detalleListaId: 12, motivoIngreso: 'Revision' }];
    component.filtroSeguimientoDesde.set('2026-07-01');
    component.filtroSeguimientoHasta.set('2026-07-15');
    service['getSeguimientos'].mockReturnValue(of(seguimientos));

    component.cargarSeguimientos('0801');

    expect(service['getSeguimientos']).toHaveBeenCalledWith('0801', '2026-07-01', '2026-07-15');
    expect(component.listaSeguimientos()).toEqual(seguimientos);
    expect(component.cargandoSeguimiento()).toBe(false);
  });

  it('conserva el historial y expone el error si falla la consulta de seguimientos', () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    component.listaSeguimientos.set([{ detalleListaId: 12 }] as never);
    service['getSeguimientos'].mockReturnValue(throwError(() => new Error('Fallo controlado')));

    component.cargarSeguimientos('0801');

    expect(component.listaSeguimientos()).toEqual([{ detalleListaId: 12 }]);
    expect(component.seguimientoError()).toContain('No se pudo cargar');
    expect(component.cargandoSeguimiento()).toBe(false);
    expect(consoleError).toHaveBeenCalled();
    consoleError.mockRestore();
  });

  it('inicializa datos, catalogos y conserva extensiones seguras por defecto', () => {
    const tiposDocumento = [{ tipoDocumentoId: 1, descripcion: 'Identidad' }];
    const tiposLista = [{ tipoListaCautelaId: 2, descripcion: 'PEP' }];
    service['getTiposDocumento'].mockReturnValue(of(tiposDocumento));
    service['getTiposListasCautela'].mockReturnValue(of(tiposLista));

    component.ngOnInit();

    expect(service['getJuridicasPaginadas']).toHaveBeenCalledOnce();
    expect(component.listaTiposDocumento()).toEqual(tiposDocumento);
    expect(component.listaTiposListasCautela()).toEqual(tiposLista);
    expect(component.politicaEvidencias().extensionesPermitidas).toContain('.pdf');
  });

  it('bloquea un registro manual incompleto antes de invocar el servicio', async () => {
    component.agregarPositivoManual();
    component.formManualNombre.set('Persona Manual');

    component.guardarMotivo();

    await vi.waitFor(() => expect(fire).toHaveBeenCalled());
    expect(service['registrarPositivo']).not.toHaveBeenCalled();
    expect(component.guardandoMotivo()).toBe(false);
    expect(fire).toHaveBeenCalledWith(expect.objectContaining({ title: 'Campos requeridos' }));
  });

  it('registra un positivo manual valido y refresca la lista', async () => {
    component.agregarPositivoManual();
    component.formManualNombre.set('Persona Manual');
    component.formManualNoDocumento.set('0801199900010');
    component.formManualTipoPositivoId.set(2);
    component.formTipoDocId.set(1);
    component.formTipoListaCautelaId.set(3);
    component.formMotivo.set('Revision de cumplimiento');

    component.guardarMotivo();

    await vi.waitFor(() => expect(service['registrarPositivo']).toHaveBeenCalledOnce());
    expect(service['registrarPositivo']).toHaveBeenCalledWith({
      tipoDocumentoId: 1,
      tipoPositivoId: 2,
      noDocumento: '0801199900010',
      nombreCompleto: 'Persona Manual',
      motivoIngreso: 'Revision de cumplimiento',
      tipoListaCautelaId: 3,
      origenRegistro: 'MANUAL_CUMPLIMIENTO'
    });
    expect(component.guardandoMotivo()).toBe(false);
    expect(component.modalMotivoAbierto()).toBe(false);
    expect(service['getJuridicasPaginadas']).toHaveBeenCalled();
  });

  it('registra un seguimiento nuevo y recarga su historial', async () => {
    component.entidadSeleccionada.set({ noDocumento: '0801', nombreCompleto: 'Ana', tipoPositivoId: 2 });
    component.formComentarioSeguimiento.set('Seguimiento mensual');
    const evidencia = new File(['contenido'], 'soporte.pdf', { type: 'application/pdf' });
    component.archivosSeleccionados.set([evidencia]);

    component.guardarSeguimiento();

    await vi.waitFor(() => expect(service['registrarSeguimiento']).toHaveBeenCalledOnce());
    expect(service['registrarSeguimiento']).toHaveBeenCalledWith('0801', 'Seguimiento mensual', [evidencia]);
    expect(service['getSeguimientos']).toHaveBeenCalledWith('0801', '', '');
    expect(component.guardandoSeguimiento()).toBe(false);
    expect(component.modoEdicion()).toBe(false);
  });

  it('recupera el indicador y conserva la edicion cuando falla una actualizacion', async () => {
    service['actualizarSeguimiento'].mockReturnValue(throwError(() => ({ error: { mensaje: 'Actualizacion rechazada' } })));
    component.entidadSeleccionada.set({ noDocumento: '0801', nombreCompleto: 'Ana', tipoPositivoId: 2 });
    component.formComentarioSeguimiento.set('Nota corregida');
    component.modoEdicion.set(true);
    component.seguimientoEditandoId.set(44);

    component.guardarSeguimiento();

    await vi.waitFor(() => expect(service['actualizarSeguimiento']).toHaveBeenCalledOnce());
    expect(service['actualizarSeguimiento']).toHaveBeenCalledWith(44, 'Nota corregida', []);
    expect(component.guardandoSeguimiento()).toBe(false);
    expect(component.modoEdicion()).toBe(true);
    expect(fire).toHaveBeenLastCalledWith(expect.objectContaining({
      title: 'Error', text: 'Actualizacion rechazada'
    }));
  });

  it('entrega la política de evidencias del backend al uploader compartido', () => {
    component.politicaEvidencias.set({
      maximoMb: 0.000005,
      maximoBytes: 5,
      extensionesPermitidas: ['.pdf'],
      tiposPermitidosTexto: 'PDF'
    });
    expect(component.politicaCargaArchivos).toEqual({
      allowedExtensions: ['.pdf'],
      allowedTypesText: 'PDF',
      maxFileSizeBytes: 5
    });
  });

  it('conserva la selección emitida por el uploader sin duplicar validación de archivos', () => {
    const selected = [new File(['12'], 'nota.pdf', { type: 'application/pdf' })];
    component.onSeguimientoFilesSelected(selected);
    component.onManualSeguimientoFilesSelected(selected);
    expect(component.archivosSeleccionados()).toEqual(selected);
    expect(component.archivosSeguimiento()).toEqual(selected);
    expect(fire).not.toHaveBeenCalled();
  });

  it('cancela la eliminacion de evidencia sin invocar el servicio', async () => {
    confirmation.confirmWithInput.mockResolvedValueOnce(null);

    component.eliminarEvidenciaExistente({ evidenciaId: 8, nombreArchivo: 'soporte.pdf' } as never);

    await vi.waitFor(() => expect(confirmation.confirmWithInput).toHaveBeenCalledOnce());
    expect(service['eliminarEvidencia']).not.toHaveBeenCalled();
  });

  it('elimina logicamente una evidencia y la retira de las colecciones locales', async () => {
    const eliminada = { evidenciaId: 8, nombreArchivo: 'duplicada.pdf' };
    const conservada = { evidenciaId: 9, nombreArchivo: 'vigente.pdf' };
    component.evidenciasExistentes.set([eliminada, conservada] as never);
    component.listaSeguimientos.set([{
      detalleListaId: 4, motivoIngreso: 'Revision', evidencias: [eliminada, conservada]
    }] as never);
    confirmation.confirmWithInput.mockResolvedValueOnce('Archivo duplicado');

    component.eliminarEvidenciaExistente(eliminada as never);

    await vi.waitFor(() => expect(service['eliminarEvidencia']).toHaveBeenCalledOnce());
    expect(service['eliminarEvidencia']).toHaveBeenCalledWith(8, 'Archivo duplicado');
    expect(confirmation.confirmWithInput).toHaveBeenCalledWith(expect.objectContaining({
      confirmIcon: 'delete',
      confirmVariant: 'danger',
      inputLabel: 'Motivo de eliminación',
      inputMaxLength: 1000
    }));
    expect(component.evidenciasExistentes()).toEqual([conservada]);
    expect(component.listaSeguimientos()[0].evidencias).toEqual([conservada]);
    expect(fire).toHaveBeenLastCalledWith(expect.objectContaining({ title: 'Eliminado' }));
  });

  it('elimina un seguimiento, cancela su edicion y refresca el historial', async () => {
    const seguimiento = { detalleListaId: 44, motivoIngreso: 'Nota anterior' };
    component.entidadSeleccionada.set({ noDocumento: '0801', nombreCompleto: 'Ana', tipoPositivoId: 2 });
    component.modoEdicion.set(true);
    component.seguimientoEditandoId.set(44);
    confirmation.confirmWithInput.mockResolvedValueOnce('Registro sustituido');

    component.eliminarSeguimiento(seguimiento as never);

    await vi.waitFor(() => expect(service['eliminarSeguimiento']).toHaveBeenCalledOnce());
    expect(service['eliminarSeguimiento']).toHaveBeenCalledWith(44, 'Registro sustituido');
    expect(component.modoEdicion()).toBe(false);
    expect(component.seguimientoEditandoId()).toBeNull();
    expect(service['getSeguimientos']).toHaveBeenCalledWith('0801', '', '');
  });

  it('conserva el seguimiento si la eliminacion logica es rechazada', async () => {
    service['eliminarSeguimiento'].mockReturnValue(throwError(() => ({ error: { mensaje: 'Seguimiento protegido' } })));
    const seguimiento = { detalleListaId: 55, motivoIngreso: 'Nota protegida' };
    component.modoEdicion.set(true);
    component.seguimientoEditandoId.set(55);
    confirmation.confirmWithInput.mockResolvedValueOnce('Depuracion');

    component.eliminarSeguimiento(seguimiento as never);

    await vi.waitFor(() => expect(service['eliminarSeguimiento']).toHaveBeenCalledOnce());
    expect(component.modoEdicion()).toBe(true);
    expect(component.seguimientoEditandoId()).toBe(55);
    expect(fire).toHaveBeenLastCalledWith(expect.objectContaining({
      title: 'Error', text: 'Seguimiento protegido'
    }));
  });

  it('abre una evidencia PDF usando una URL temporal simulada', async () => {
    const archivo = new Blob(['pdf'], { type: 'application/pdf' });
    const abrir = vi.spyOn(window, 'open').mockImplementation(() => null);
    service['descargarEvidenciaBlob'].mockReturnValue(of(archivo));

    component.descargarEvidencia({
      evidenciaId: 71, nombreArchivo: 'evidencia.pdf', tipoMime: 'application/pdf'
    } as never);

    await vi.waitFor(() => expect(service['descargarEvidenciaBlob']).toHaveBeenCalledOnce());
    expect(service['descargarEvidenciaBlob']).toHaveBeenCalledWith(71);
    expect(createObjectUrl).toHaveBeenCalledWith(archivo);
    expect(abrir).toHaveBeenCalledWith('blob:evidencia-prueba', '_blank');
    expect(close).toHaveBeenCalled();
  });

  it('descarga una evidencia no visualizable mediante un enlace temporal', async () => {
    const archivo = new Blob(['doc'], { type: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document' });
    const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => undefined);
    const abrir = vi.spyOn(window, 'open').mockImplementation(() => null);
    service['descargarEvidenciaBlob'].mockReturnValue(of(archivo));

    component.descargarEvidencia({
      evidenciaId: 72, nombreArchivo: 'informe.docx', tipoMime: archivo.type
    } as never);

    await vi.waitFor(() => expect(click).toHaveBeenCalledOnce());
    expect(createObjectUrl).toHaveBeenCalledWith(archivo);
    expect(abrir).not.toHaveBeenCalled();
    expect(close).toHaveBeenCalled();
  });

  it('muestra un error controlado cuando no puede descargar la evidencia', async () => {
    service['descargarEvidenciaBlob'].mockReturnValue(throwError(() => new Error('Descarga no disponible')));

    component.descargarEvidencia({
      evidenciaId: 73, nombreArchivo: 'fallida.pdf', tipoMime: 'application/pdf'
    } as never);

    await vi.waitFor(() => expect(service['descargarEvidenciaBlob']).toHaveBeenCalledOnce());
    expect(createObjectUrl).not.toHaveBeenCalled();
    expect(close).toHaveBeenCalled();
    expect(fire).toHaveBeenLastCalledWith(expect.objectContaining({
      title: 'Error', text: 'No se pudo cargar el archivo de evidencia.'
    }));
  });

  it('permite exportar un reporte vacío cuando la consulta terminó correctamente', () => {
    const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => undefined);
    component.juridicasRaw.set([]);

    component.exportarListaPrincipal();

    expect(service['registrarAuditoriaExportacion']).toHaveBeenCalledOnce();
  });

  it('audita y abre la vista previa de la lista principal juridica con los datos filtrados', async () => {
    const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => undefined);
    component.juridicasRaw.set([{
      numeroPatrono: 'P-100', rtn: '08019000123456', nombre: 'Empresa Segura',
      listaCoincidencia: 'Lista Uno', esProveedorIhss: 'Si', fechaEncontro: '2026-07-10'
    }] as never);
    service['getJuridicasParaExportar'].mockReturnValue(of(component.juridicasRaw()));

    component.exportarListaPrincipal();

    expect(service['registrarAuditoriaExportacion']).toHaveBeenCalledWith(
      'RL_LISTA_POSITIVOS',
      'juridica',
      'ExportacionMonitoreoListas',
      expect.objectContaining({ tipo: 'juridica', cantidadRegistros: 1 })
    );
    await vi.waitFor(() => expect((component as any).reportPreview.state()).not.toBeNull());
    expect((component as any).reportPreview.state().kind).toBe('excel');
    expect(click).not.toHaveBeenCalled();
  });

  it('cancela la exportacion principal si no puede registrar la auditoria', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => undefined);
    service['registrarAuditoriaExportacion'].mockReturnValue(throwError(() => new Error('Auditoria fuera de linea')));
    component.juridicasRaw.set([{
      numeroPatrono: 'P-101', rtn: '08019000123457', nombre: 'Empresa Dos', listaCoincidencia: 'Lista Uno'
    }] as never);
    service['getJuridicasParaExportar'].mockReturnValue(of(component.juridicasRaw()));

    component.exportarListaPrincipal();

    await vi.waitFor(() => expect(fire).toHaveBeenCalled());
    expect(click).not.toHaveBeenCalled();
    expect(component.cargando()).toBe(false);
    expect(fire).toHaveBeenLastCalledWith(expect.objectContaining({ title: 'Auditoría requerida' }));
    expect(consoleError).toHaveBeenCalled();
  });

  it('genera la vista previa de una ficha de patrono solamente despues de auditarla', async () => {
    const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => undefined);
    service['getPositivoPorDocumento'].mockReturnValue(of({
      motivoIngreso: 'Coincidencia validada', origenRegistro: 'DNP_LISTAS'
    }));
    service['getSeguimientos'].mockReturnValue(of([{
      detalleListaId: 1, motivoIngreso: 'Revision mensual', fechaCreacion: '2026-07-10', evidencias: []
    }]));
    const patrono = {
      numeroPatrono: 'P-200', rtn: '08019000123458', nombre: 'Patrono Auditado',
      listaCoincidencia: 'Lista Dos', tieneMotivo: true
    } as never;

    component.exportarFichaExcelPatrono(patrono);

    expect(service['registrarAuditoriaExportacion']).toHaveBeenCalledWith(
      'RL_LISTA_POSITIVOS',
      'P-200',
      'ExportacionFichaPerfil',
      expect.objectContaining({ tipo: 'juridica', cantidadSeguimientos: 1 })
    );
    await vi.waitFor(() => expect((component as any).reportPreview.state()).not.toBeNull());
    expect((component as any).reportPreview.state().fileName).toBe('Ficha_Patrono_P-200.xlsx');
    expect(click).not.toHaveBeenCalled();
    expect(component.cargando()).toBe(false);
  });

  it('genera el reporte de una persona con marcador N/A(3) usando la fila disponible', () => {
    const generarPdf = vi.spyOn(component, 'generarPdfNatural').mockImplementation(() => undefined);
    const natural = {
      numeroIdentificacion: 'N/A(3)',
      nombre: 'Persona sin identificación',
      listaCoincidencia: 'PEPS',
      totalRepetidos: 3,
      tieneMotivo: false,
      estadoIdentidad: 'AMBIGUOUS',
      estadoAsociacion: 'AMBIGUOUS'
    } as never;

    component.imprimirReporteNatural(natural);

    expect(generarPdf).toHaveBeenCalledWith(
      natural,
      [],
      null,
      [],
      expect.objectContaining({ texto: expect.any(String) })
    );
    expect(service['registrarAuditoriaImpresion']).toHaveBeenCalledWith(
      'N/A(3)',
      expect.objectContaining({ tipoReporte: 'PDF_PERFIL_FICHA' })
    );
    expect(component.cargando()).toBe(false);
  });

  it('solo habilita seguimiento juridico con identidad y asociación verificadas', () => {
    const patrono = {
      numeroPatrono: '08011999123456', rtn: '08011999123456', nombre: 'Empresa E2E',
      listaCoincidencia: 'Lista de prueba', tieneMotivo: true, esManual: false
    };

    expect(component.puedeDarSeguimiento({
      ...patrono, estadoIdentidad: 'VERIFIED', estadoAsociacion: 'VERIFIED'
    })).toBe(true);
    expect(component.puedeDarSeguimiento({
      ...patrono, estadoIdentidad: 'VERIFIED', estadoAsociacion: 'UNVERIFIED_ASSOCIATION'
    })).toBe(false);
    expect(component.puedeDarSeguimiento({
      ...patrono, estadoIdentidad: 'AMBIGUOUS', estadoAsociacion: 'AMBIGUOUS'
    })).toBe(false);
    expect(component.puedeDarSeguimiento(patrono)).toBe(false);
  });

  it('no genera la ficha de patrono cuando falla su auditoria obligatoria', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => undefined);
    service['registrarAuditoriaExportacion'].mockReturnValue(throwError(() => new Error('Auditoria rechazada')));
    const patrono = {
      numeroPatrono: 'P-201', rtn: '08019000123459', nombre: 'Patrono Restringido',
      listaCoincidencia: 'Lista Dos', tieneMotivo: false
    } as never;

    component.exportarFichaExcelPatrono(patrono);

    await vi.waitFor(() => expect(fire).toHaveBeenCalled());
    expect(click).not.toHaveBeenCalled();
    expect(component.cargando()).toBe(false);
    expect(fire).toHaveBeenLastCalledWith(expect.objectContaining({ title: 'Auditoría requerida' }));
    expect(consoleError).toHaveBeenCalled();
  });
  it('explica el bloqueo de seguimiento sin atribuir un motivo faltante a identidades ambiguas o manuales', () => {
    const validado = { tieneMotivo: true, estadoIdentidad: 'VERIFIED', estadoAsociacion: 'VERIFIED' };
    expect(component.obtenerTextoAccionSeguimiento(validado)).toBe('Dar seguimiento');
    expect(component.obtenerTextoAccionSeguimiento({ ...validado, tieneMotivo: false })).toBe('Debe registrar un motivo antes del seguimiento');
    expect(component.obtenerTextoAccionSeguimiento({ ...validado, estadoIdentidad: 'AMBIGUOUS' })).toBe('Seguimiento bloqueado: identidad ambigua');
    expect(component.obtenerTextoAccionSeguimiento({ ...validado, estadoAsociacion: 'AMBIGUOUS' })).toBe('Seguimiento bloqueado: asociación ambigua entre coincidencias de origen');
    expect(component.obtenerEtiquetaIdentidad({ ...validado, estadoAsociacion: 'AMBIGUOUS' })).toBe('Asociación ambigua');
    expect(component.obtenerTextoAccionSeguimiento({ ...validado, estadoAsociacion: 'UNVERIFIED_ASSOCIATION' })).toBe('Seguimiento bloqueado: identidad o asociación no verificada');
    expect(component.obtenerTextoAccionSeguimiento({ ...validado, esManual: true })).toBe('Debe registrar un motivo antes del seguimiento');
  });

  it('permite reporte PDF de un natural no identificado sin solicitar asociaciones por documento', () => {
    const row = {
      numeroIdentificacion: 'N/A(3)', nombre: 'Origen N/A', listaCoincidencia: 'LISTA X',
      idRegistroFuente: 501, tipoFuente: 'DNP_COINCIDENCIA', tieneMotivo: true,
      estadoIdentidad: 'AMBIGUOUS', estadoAsociacion: 'AMBIGUOUS'
    };
    const generar = vi.spyOn(component, 'generarPdfNatural').mockImplementation(() => undefined);

    component.imprimirReporteNatural(row as never);

    expect(service['getDetalleNatural']).toHaveBeenCalledWith('N/A(3)', 'global', 501, 'DNP_COINCIDENCIA');
    expect(service['getPositivoPorDocumento']).not.toHaveBeenCalled();
    expect(service['getSeguimientos']).not.toHaveBeenCalled();
    expect(service['registrarAuditoriaImpresion']).toHaveBeenCalledOnce();
    expect(generar).toHaveBeenCalledWith(row, [], null, [], expect.anything());
    expect(component.cargando()).toBe(false);
  });

  it('no incorpora motivo ni seguimiento de un empleado sin asociacion verificada en su PDF', () => {
    const row = {
      identidad: '08011999123456', nombre: 'Empleado de origen', listaCoincidencia: 'LISTA Y',
      idRegistroFuente: 502, tipoFuente: 'DNP_COINCIDENCIA', tieneMotivo: true,
      estadoIdentidad: 'VERIFIED', estadoAsociacion: 'UNVERIFIED_ASSOCIATION'
    };
    const generar = vi.spyOn(component, 'generarPdfEmpleado').mockImplementation(() => undefined);

    component.imprimirReporteEmpleado(row as never);

    expect(service['getDetalleEmpleado']).toHaveBeenCalledWith('08011999123456', 'global', 502, 'DNP_COINCIDENCIA', undefined);
    expect(service['getPositivoPorDocumento']).not.toHaveBeenCalled();
    expect(service['getSeguimientos']).not.toHaveBeenCalled();
    expect(service['registrarAuditoriaImpresion']).toHaveBeenCalledOnce();
    expect(generar).toHaveBeenCalledWith(row, [], null, [], expect.anything());
    expect(component.cargando()).toBe(false);
  });

});
