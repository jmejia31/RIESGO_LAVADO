import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DomSanitizer } from '@angular/platform-browser';
import { of, throwError } from 'rxjs';
import Swal from 'sweetalert2';
import { ConfiguracionService } from '../../../../../core/configuration/configuracion.service';
import { ListasService } from '../../data-access/listas.service';
import { MonitoreoListasComponent } from './monitoreo-listas.component';

vi.mock('sweetalert2', () => ({
  default: { fire: vi.fn(), showLoading: vi.fn(), close: vi.fn() }
}));

describe('MonitoreoListasComponent', () => {
  let fixture: ComponentFixture<MonitoreoListasComponent>;
  let component: MonitoreoListasComponent;
  let service: Record<string, ReturnType<typeof vi.fn>>;
  const fire = vi.mocked(Swal.fire);
  const close = vi.mocked(Swal.close);
  let createObjectUrl: ReturnType<typeof vi.fn>;
  let revokeObjectUrl: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    service = {
      getJuridicas: vi.fn(() => of([])),
      getNaturales: vi.fn(() => of([])),
      getEmpleados: vi.fn(() => of([])),
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
      registrarAuditoriaExportacion: vi.fn(() => of({ success: true }))
    };
    fire.mockReset();
    close.mockReset();
    createObjectUrl = vi.fn(() => 'blob:evidencia-prueba');
    revokeObjectUrl = vi.fn();
    Object.defineProperty(URL, 'createObjectURL', { configurable: true, value: createObjectUrl });
    Object.defineProperty(URL, 'revokeObjectURL', { configurable: true, value: revokeObjectUrl });

    await TestBed.configureTestingModule({
      imports: [MonitoreoListasComponent],
      providers: [
        { provide: ListasService, useValue: service },
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
    service['getJuridicas'].mockReturnValue(of(juridicas));

    component.cargarDatos();

    expect(service['getJuridicas']).toHaveBeenCalledOnce();
    expect(component.juridicasRaw()).toEqual(juridicas);
    expect(component.cargando()).toBe(false);
  });

  it('cambia a personas naturales, limpia la busqueda y recarga el servicio correcto', () => {
    const naturales = [{ numeroIdentificacion: '0801', nombre: 'Ana' }];
    service['getNaturales'].mockReturnValue(of(naturales));
    component.busqueda.set('anterior');
    component.paginaActual.set(4);

    component.cambiarTipo('natural');

    expect(component.tipoActivo()).toBe('natural');
    expect(component.busqueda()).toBe('');
    expect(component.paginaActual()).toBe(1);
    expect(service['getNaturales']).toHaveBeenCalledOnce();
    expect(component.naturalesRaw()).toEqual(naturales);
  });

  it('reutiliza datos cargados al volver a la misma categoria sin consultar nuevamente la API', () => {
    const naturales = [{ numeroIdentificacion: '0801', nombre: 'Ana' }];
    service['getNaturales'].mockReturnValue(of(naturales));

    component.cambiarTipo('natural');
    component.busqueda.set('ana');
    component.cambiarTipo('natural');

    expect(service['getNaturales']).toHaveBeenCalledOnce();
    expect(component.naturalesRaw()).toEqual(naturales);
    expect(component.busqueda()).toBe('');
    expect(component.cargando()).toBe(false);
  });

  it('limpia resultados de empleados y detiene la carga cuando el servicio falla', () => {
    component.tipoActivo.set('empleado');
    component.empleadosRaw.set([{ identidad: '01' }] as never);
    service['getEmpleados'].mockReturnValue(throwError(() => new Error('API no disponible')));

    component.cargarDatos();

    expect(component.empleadosRaw()).toEqual([]);
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

  it('deja el historial vacio y recupera el estado si falla la consulta de seguimientos', () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    component.listaSeguimientos.set([{ detalleListaId: 12 }] as never);
    service['getSeguimientos'].mockReturnValue(throwError(() => new Error('Fallo controlado')));

    component.cargarSeguimientos('0801');

    expect(component.listaSeguimientos()).toEqual([]);
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

    expect(service['getJuridicas']).toHaveBeenCalledOnce();
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
    expect(service['getJuridicas']).toHaveBeenCalled();
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

  it('acepta archivos permitidos y rechaza los que superan el tamano maximo', async () => {
    component.politicaEvidencias.set({
      maximoMb: 0.000005,
      maximoBytes: 5,
      extensionesPermitidas: ['.pdf'],
      tiposPermitidosTexto: 'PDF'
    });
    const valido = new File(['1234'], 'soporte.pdf', { type: 'application/pdf' });
    const grande = new File(['123456'], 'grande.pdf', { type: 'application/pdf' });

    component.onFileSelected({ target: { files: [valido, grande] } });

    await vi.waitFor(() => expect(fire).toHaveBeenCalledOnce());
    expect(component.archivosSeleccionados()).toEqual([valido]);
    expect(fire).toHaveBeenCalledWith(expect.objectContaining({ title: 'Archivo no permitido' }));
  });

  it('rechaza archivos cuya extension no esta autorizada', async () => {
    const extensionInvalida = new File(['12'], 'nota.txt', { type: 'text/plain' });

    component.onFileSelected({ target: { files: [extensionInvalida] } });

    await vi.waitFor(() => expect(fire).toHaveBeenCalledOnce());
    expect(component.archivosSeleccionados()).toEqual([]);
    expect(fire).toHaveBeenCalledWith(expect.objectContaining({
      title: 'Archivo no permitido',
      text: expect.stringContaining('no tiene una extensión permitida')
    }));
  });

  it('cancela la eliminacion de evidencia sin invocar el servicio', async () => {
    fire.mockResolvedValueOnce({ isConfirmed: false } as never);

    component.eliminarEvidenciaExistente({ evidenciaId: 8, nombreArchivo: 'soporte.pdf' } as never);

    await vi.waitFor(() => expect(fire).toHaveBeenCalledOnce());
    expect(service['eliminarEvidencia']).not.toHaveBeenCalled();
  });

  it('elimina logicamente una evidencia y la retira de las colecciones locales', async () => {
    const eliminada = { evidenciaId: 8, nombreArchivo: 'duplicada.pdf' };
    const conservada = { evidenciaId: 9, nombreArchivo: 'vigente.pdf' };
    component.evidenciasExistentes.set([eliminada, conservada] as never);
    component.listaSeguimientos.set([{
      detalleListaId: 4, motivoIngreso: 'Revision', evidencias: [eliminada, conservada]
    }] as never);
    fire.mockResolvedValueOnce({ isConfirmed: true, value: 'Archivo duplicado' } as never);

    component.eliminarEvidenciaExistente(eliminada as never);

    await vi.waitFor(() => expect(service['eliminarEvidencia']).toHaveBeenCalledOnce());
    expect(service['eliminarEvidencia']).toHaveBeenCalledWith(8, 'Archivo duplicado');
    expect(component.evidenciasExistentes()).toEqual([conservada]);
    expect(component.listaSeguimientos()[0].evidencias).toEqual([conservada]);
    expect(fire).toHaveBeenLastCalledWith(expect.objectContaining({ title: 'Eliminado' }));
  });

  it('elimina un seguimiento, cancela su edicion y refresca el historial', async () => {
    const seguimiento = { detalleListaId: 44, motivoIngreso: 'Nota anterior' };
    component.entidadSeleccionada.set({ noDocumento: '0801', nombreCompleto: 'Ana', tipoPositivoId: 2 });
    component.modoEdicion.set(true);
    component.seguimientoEditandoId.set(44);
    fire.mockResolvedValueOnce({ isConfirmed: true, value: 'Registro sustituido' } as never);

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
    fire.mockResolvedValueOnce({ isConfirmed: true, value: 'Depuracion' } as never);

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

  it('no exporta la lista principal cuando no existen resultados filtrados', () => {
    const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => undefined);
    component.juridicasRaw.set([]);

    component.exportarListaPrincipal();

    expect(service['registrarAuditoriaExportacion']).not.toHaveBeenCalled();
    expect(click).not.toHaveBeenCalled();
  });

  it('audita y exporta la lista principal juridica con los datos filtrados', () => {
    let nombreDescarga = '';
    const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function (this: HTMLAnchorElement) {
      nombreDescarga = this.download;
    });
    component.juridicasRaw.set([{
      numeroPatrono: 'P-100', rtn: '08019000123456', nombre: 'Empresa Segura',
      listaCoincidencia: 'Lista Uno', esProveedorIhss: 'Si', fechaEncontro: '2026-07-10'
    }] as never);

    component.exportarListaPrincipal();

    expect(service['registrarAuditoriaExportacion']).toHaveBeenCalledWith(
      'RL_LISTA_POSITIVOS',
      'juridica',
      'ExportacionMonitoreoListas',
      expect.objectContaining({ tipo: 'juridica', cantidadRegistros: 1 })
    );
    expect(click).toHaveBeenCalledOnce();
    expect(nombreDescarga).toMatch(/^Reporte_Juridicas_\d{4}-\d{2}-\d{2}\.xls$/);
  });

  it('cancela la exportacion principal si no puede registrar la auditoria', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => undefined);
    service['registrarAuditoriaExportacion'].mockReturnValue(throwError(() => new Error('Auditoria fuera de linea')));
    component.juridicasRaw.set([{
      numeroPatrono: 'P-101', rtn: '08019000123457', nombre: 'Empresa Dos', listaCoincidencia: 'Lista Uno'
    }] as never);

    component.exportarListaPrincipal();

    await vi.waitFor(() => expect(fire).toHaveBeenCalled());
    expect(click).not.toHaveBeenCalled();
    expect(component.cargando()).toBe(false);
    expect(fire).toHaveBeenLastCalledWith(expect.objectContaining({ title: 'Auditoría requerida' }));
    expect(consoleError).toHaveBeenCalled();
  });

  it('genera una ficha de patrono solamente despues de auditarla', () => {
    let nombreDescarga = '';
    const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function (this: HTMLAnchorElement) {
      nombreDescarga = this.download;
    });
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
    expect(click).toHaveBeenCalledOnce();
    expect(nombreDescarga).toBe('Ficha_Patrono_P-200.xls');
    expect(component.cargando()).toBe(false);
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
});
