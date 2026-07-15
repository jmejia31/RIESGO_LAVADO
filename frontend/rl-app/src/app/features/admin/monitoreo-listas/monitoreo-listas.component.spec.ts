import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DomSanitizer } from '@angular/platform-browser';
import { of, throwError } from 'rxjs';
import Swal from 'sweetalert2';
import { ConfiguracionService } from '../../../core/configuration/configuracion.service';
import { ListasService } from '../listas/data-access/listas.service';
import { MonitoreoListasComponent } from './monitoreo-listas.component';

vi.mock('sweetalert2', () => ({
  default: { fire: vi.fn(), showLoading: vi.fn() }
}));

describe('MonitoreoListasComponent', () => {
  let fixture: ComponentFixture<MonitoreoListasComponent>;
  let component: MonitoreoListasComponent;
  let service: Record<string, ReturnType<typeof vi.fn>>;
  const fire = vi.mocked(Swal.fire);

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
      actualizarSeguimiento: vi.fn(() => of({ mensaje: 'Seguimiento actualizado' }))
    };
    fire.mockReset();

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
});
