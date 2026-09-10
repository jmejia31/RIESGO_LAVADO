import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { AuditoriaService } from '../../data-access/auditoria.service';
import { AuditoriaDto } from '../../models/auditoria.models';
import { BitacoraComponent } from './bitacora.component';

describe('BitacoraComponent — rediseño institucional', () => {
  let fixture: ComponentFixture<BitacoraComponent>;
  let component: BitacoraComponent;
  let auditoriaService: { getBitacora: ReturnType<typeof vi.fn> };

  const evento: AuditoriaDto = {
    audId: 42,
    tabla: 'RL_USUARIOS',
    registroId: '123',
    accion: 'UPDATE',
    datosAnt: '{"activo":false}',
    datosNvo: '{"activo":true,"perfil":{"nivel":"admin"}}',
    usrId: 102,
    usrEmail: 'javier.mejia@ihss.hn',
    ip: '10.0.0.8',
    fecha: '2026-08-26T11:48:36',
    modulo: 'AdminUsuarios'
  };

  beforeEach(async () => {
    auditoriaService = {
      getBitacora: vi.fn(() => of({ datos: [], totalRegistros: 0 }))
    };

    await TestBed.configureTestingModule({
      imports: [BitacoraComponent],
      providers: [{ provide: AuditoriaService, useValue: auditoriaService }]
    }).compileComponents();

    fixture = TestBed.createComponent(BitacoraComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('presenta la consola con copy contextual y sólo filtros soportados', () => {
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('h1')?.textContent).toContain('Bitácora General del Sistema');
    expect(root.textContent).toContain('Auditoría y registro de acciones, eventos y cambios de datos del SGRLA-IHSS.');
    expect(root.querySelector('#bitacora-search')?.getAttribute('placeholder')).toBe('Usuario, tabla, IP o ID de registro');
    expect(root.querySelector('#bitacora-table')?.getAttribute('placeholder')).toBe('Ej. RL_USUARIOS');
    expect(root.querySelector('#bitacora-module')).not.toBeNull();
    expect(root.querySelector('#bitacora-action')).not.toBeNull();
    expect(root.textContent).not.toContain('Resultado');
    expect(root.textContent).not.toContain('Correlation ID');
    expect(root.textContent).not.toContain('Número Patrono');
  });

  it('conserva filtros reales, debounce y filtro rápido de documentos eliminados', () => {
    component.filtroBuscar = 'javier';
    component.filtroAccion = 'VER';
    component.filtroModulo = 'Bitacora';
    component.filtroTabla = 'RL_AUDITORIA';
    component.filtroFechaInicio = '2026-08-01';
    component.filtroFechaFin = '2026-08-31';
    component.cargarDatos();

    expect(auditoriaService.getBitacora).toHaveBeenLastCalledWith({
      pagina: 1,
      limite: 10,
      buscar: 'javier',
      accion: 'VER',
      modulo: 'Bitacora',
      tabla: 'RL_AUDITORIA',
      fechaInicio: '2026-08-01',
      fechaFin: '2026-08-31'
    });

    component.filtrarDocumentosEliminados();
    expect(component.filtroDocumentosEliminadosActivo()).toBe(true);
    expect(auditoriaService.getBitacora).toHaveBeenLastCalledWith(expect.objectContaining({
      accion: 'DELETE',
      modulo: 'MonitoreoListas',
      tabla: 'RL_DETALLE_EVIDENCIA'
    }));
  });

  it('muestra la tabla compacta con identidad, descripción derivada y acción de detalle canónica', () => {
    component.datos.set([evento]);
    component.totalRegistros.set(1);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    const table = root.querySelector('table');
    const row = root.querySelector('tbody tr');

    expect(table?.textContent).toContain('Fecha');
    expect(table?.textContent).toContain('Entidad / registro');
    expect(table?.textContent).toContain('Descripción del evento');
    expect(row?.textContent).toContain('Gestión de Usuarios');
    expect(row?.textContent).toContain('RL_USUARIOS');
    expect(row?.textContent).toContain('#123');
    expect(row?.textContent).toContain('Modificación del registro #123 en RL_USUARIOS.');
    expect(row?.querySelector('app-action-icon')?.getAttribute('action')).toBe('view');
  });

  it('mantiene detalle JSON, comparación de valores y cierre icon-only', () => {
    component.verDetalle(evento);
    fixture.detectChanges();

    const dialog = fixture.nativeElement.querySelector('[role="dialog"]') as HTMLElement;
    expect(dialog.querySelector('h2')?.textContent).toContain('Detalle de Registro #42');
    expect(dialog.textContent).toContain('Descripción del Evento');
    expect(dialog.textContent).toContain('Valor Anterior');
    expect(dialog.textContent).toContain('Valor Nuevo');
    expect(dialog.textContent).toContain('Método HTTP');
    expect(dialog.textContent).toContain('Correlation ID');
    expect(dialog.textContent).toContain('User Agent');
    expect(dialog.textContent).toContain('activo');
    expect(dialog.textContent).toContain('"nivel"');
    expect(dialog.textContent).not.toContain('[object Object]');
    expect(dialog.querySelectorAll('button')).toHaveLength(2);
    expect(Array.from(dialog.querySelectorAll('button')).every(button => !(button.textContent ?? '').trim())).toBe(true);
  });

  it('presenta resultado y descripción específica cuando el payload de auditoría los contiene', () => {
    component.verDetalle({
      ...evento,
      accion: 'LOGIN',
      datosAnt: undefined,
      datosNvo: '{"Resultado":"EXITOSO","Identificador":"javier.mejia@ihss.hn"}'
    });
    fixture.detectChanges();

    const dialog = fixture.nativeElement.querySelector('[role="dialog"]') as HTMLElement;
    expect(dialog.textContent).toContain('Inicio de sesión exitoso para usuario: javier.mejia@ihss.hn');
    expect(dialog.textContent).toContain('EXITOSO');
    expect(dialog.textContent).toContain('Regional:');
    expect(dialog.textContent).toContain('N/D');
    expect(component.getRegistroDetalleLabel(evento)).toBe('Usuario (#123)');
  });

  it('usa un estado vacío específico para eventos de auditoría', () => {
    const root = fixture.nativeElement as HTMLElement;
    expect(root.textContent).toContain('No se encontraron eventos de auditoría con los filtros seleccionados.');
    expect(root.textContent).toContain('Ajuste la búsqueda, el módulo, la acción, la entidad o el período.');
  });

  it('deriva descripciones sin afirmar resultados no presentes en el contrato', () => {
    expect(component.getDescripcionEvento({ ...evento, accion: 'LOGIN' })).toBe('Inicio de sesión registrado para el usuario.');
    expect(component.getDescripcionEvento({ ...evento, accion: 'DELETE' })).toBe('Eliminación o inactivación registrada para RL_USUARIOS #123.');
    expect(component.getModuloLabel('MonitoreoListas')).toBe('Monitoreo de Listas');
    expect(component.getModuloLabel('ExportacionMonitoreoListas')).toBe('Monitoreo de Listas');
    expect(component.getModuloLabel('ExportacionCoincidenciasPatrono')).toBe('Coincidencias Patrono');
    expect(component.getModuloLabel('ExportacionCoincidenciasEmpleado')).toBe('Coincidencias Empleado');
    expect(component.getModuloLabel('ExportacionFichaPerfil')).toBe('Ficha de Perfil');
    expect(component.getModuloLabel('ExportacionListas')).toBe('Listas de Cautela');
    expect(component.getModuloLabel('ModuloNoCatalogado')).toBe('ModuloNoCatalogado');
  });

  it('renderiza una ventana de paginación compacta y calcula correctamente el rango vacío', () => {
    component.totalRegistros.set(1310);
    component.datos.set([evento]);
    fixture.detectChanges();

    const pagination = fixture.nativeElement.querySelector('app-data-pagination') as HTMLElement;
    expect(pagination.querySelectorAll('button[aria-label^="Ir a página"]').length).toBeLessThanOrEqual(7);
    expect(pagination.querySelector('span[aria-hidden="true"]')?.textContent).toContain('…');
    expect(component.showingRange()).toEqual({ start: 1, end: 10, total: 1310 });

    component.totalRegistros.set(0);
    fixture.detectChanges();
    expect(component.showingRange()).toEqual({ start: 0, end: 0, total: 0 });
  });

  it('normaliza IP mapeada y loopback sin fabricar una dirección histórica', () => {
    expect(component.getIpDisplay('::ffff:172.19.0.214')).toBe('172.19.0.214');
    expect(component.getIpDisplay('::1')).toBe('Local (127.0.0.1)');
    expect(component.getIpDisplay('2001:db8::8')).toBe('2001:db8::8');
    expect(component.getIpDisplay()).toBe('-');
  });

  it('ubica el selector de tamaño junto a las acciones y no en el footer', () => {
    component.datos.set([evento]);
    component.totalRegistros.set(1);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    const pageSize = root.querySelector('#bitacora-page-size');
    expect(pageSize?.closest('section')?.getAttribute('aria-labelledby')).toBe('bitacora-filters-title');
    expect(pageSize?.closest('section')?.querySelector('[aria-label="Limpiar filtros de bitácora"]')).not.toBeNull();
    expect(root.querySelector('[aria-label="Paginación de eventos de auditoría"] #bitacora-page-size')).toBeNull();
    expect(root.querySelector('[aria-label="Paginación de eventos de auditoría"].overflow-x-auto')).toBeNull();
    expect(root.querySelector('table')?.classList.contains('w-full')).toBe(true);
  });
});
