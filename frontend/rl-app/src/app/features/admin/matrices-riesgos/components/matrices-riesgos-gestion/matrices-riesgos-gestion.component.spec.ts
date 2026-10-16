import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { InstitutionalConfirmationService } from '../../../../../shared/confirmation/institutional-confirmation.service';
import { MatricesRiesgosGestionComponent } from './matrices-riesgos-gestion.component';

describe('MatricesRiesgosGestionComponent', () => {
  let fixture: ComponentFixture<MatricesRiesgosGestionComponent>;
  let component: MatricesRiesgosGestionComponent;
  let service: {
    listarRiesgos: ReturnType<typeof vi.fn>;
    listarRiesgosPaginados: ReturnType<typeof vi.fn>;
    crearRiesgo: ReturnType<typeof vi.fn>;
    actualizarRiesgo: ReturnType<typeof vi.fn>;
  };
  let confirmation: { confirm: ReturnType<typeof vi.fn> };

  const riesgoActivo = {
    rieId: 7,
    rieCodigo: 'R-007',
    rieNombre: 'Riesgo UAT',
    rieDescripcion: 'Descripción UAT',
    rieActivo: true,
    rieUsrCreacion: 1,
    rieFechaCreacion: '2026-08-07T12:00:00Z'
  };

  const riesgoInactivo = {
    rieId: 9,
    rieCodigo: 'R-009',
    rieNombre: 'Riesgo Inactivo',
    rieDescripcion: null,
    rieActivo: false,
    rieUsrCreacion: 1,
    rieFechaCreacion: '2026-08-08T12:00:00Z'
  };

  beforeEach(async () => {
    service = {
      listarRiesgos: vi.fn().mockReturnValue(of([riesgoActivo, riesgoInactivo])),
      listarRiesgosPaginados: vi.fn().mockImplementation((_incluirInactivos: boolean, pagina: number, tamanoPagina: number) => of({ items: [riesgoActivo, riesgoInactivo], pagina, tamanoPagina, totalRegistros: 2, totalPaginas: 1 })),
      crearRiesgo: vi.fn().mockReturnValue(of(8)),
      actualizarRiesgo: vi.fn().mockReturnValue(of({ success: true }))
    };
    confirmation = { confirm: vi.fn().mockResolvedValue(false) };
    await TestBed.configureTestingModule({
      imports: [MatricesRiesgosGestionComponent],
      providers: [
        { provide: MatricesRiesgosService, useValue: service },
        { provide: InstitutionalConfirmationService, useValue: confirmation }
      ]
    }).compileComponents();
    fixture = TestBed.createComponent(MatricesRiesgosGestionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('carga riesgos activos e inactivos al inicializar', () => {
    expect(service.listarRiesgosPaginados).toHaveBeenCalledWith(true, 1, 10, '', undefined, 'local');
    expect(component.riesgos()).toHaveLength(2);
    expect(component.cargando()).toBe(false);
    expect(component.error()).toBeNull();
  });

  it('aplica el filtro estándar por búsqueda, estado y limpieza con paginación de servidor', () => {
    vi.useFakeTimers();
    service.listarRiesgosPaginados.mockClear();

    component.cambiarBuscar('proveedor');
    vi.advanceTimersByTime(300);
    expect(service.listarRiesgosPaginados).toHaveBeenLastCalledWith(true, 1, 10, 'proveedor', undefined, 'local');

    component.cambiarEstadoFiltro('INACTIVOS');
    expect(service.listarRiesgosPaginados).toHaveBeenLastCalledWith(true, 1, 10, 'proveedor', false, 'local');

    component.limpiarFiltros();
    expect(component.filtroBuscar()).toBe('');
    expect(component.filtroEstado()).toBe('TODOS');
    expect(service.listarRiesgosPaginados).toHaveBeenLastCalledWith(true, 1, 10, '', undefined, 'local');
    vi.useRealTimers();
  });

  it('sincroniza el tamaño mostrado con el tamaño realmente solicitado al servidor', () => {
    service.listarRiesgosPaginados.mockClear();
    component.cambiarTamanoPagina(25);

    expect(service.listarRiesgosPaginados).toHaveBeenLastCalledWith(true, 1, 25, '', undefined, 'local');
    expect(component.tamanoPagina()).toBe(25);
  });

  it('renderiza la superficie de filtro completa y no un selector de tamaño aislado', () => {
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    const filtros = root.querySelector('[data-ui-filter-block="riesgos"]');
    expect(filtros?.getAttribute('data-ui-filter-standard')).toBe('full');
    expect(filtros?.querySelector('#riesgos-buscar')).not.toBeNull();
    expect(filtros?.querySelector('#riesgos-estado')).not.toBeNull();
    expect(filtros?.querySelector('[aria-label="Limpiar filtros de riesgos"]')).not.toBeNull();
  });

  it('presenta los datos del catálogo con semántica institucional de riesgo', () => {
    const root = fixture.nativeElement as HTMLElement;
    expect(root.textContent).toContain('Código de Riesgo');
    expect(root.textContent).toContain('Riesgo Inherente');
    expect(root.textContent).toContain('Evaluación');
    expect(root.querySelector('#riesgos-buscar')?.getAttribute('placeholder')).toBe(
      'Buscar por Código de Riesgo, Riesgo Inherente o Evaluación...'
    );
    expect(root.querySelector('[aria-label="Ver Matriz completa"]')).toBeNull();
  });

  it('maneja error al listar riesgos y propaga mensaje por defecto si no viene del backend', () => {
    service.listarRiesgosPaginados.mockReturnValue(throwError(() => ({})));
    component.cargar();
    expect(component.error()).toBe('No se pudieron cargar los riesgos.');
    expect(component.cargando()).toBe(false);
  });

  it('maneja error con mensaje institucional al listar riesgos', () => {
    service.listarRiesgosPaginados.mockReturnValue(throwError(() => ({ error: { mensaje: 'Error de conexión' } })));
    component.cargar();
    expect(component.error()).toBe('Error de conexión');
    expect(component.cargando()).toBe(false);
  });

  it('crea un riesgo válido con descripción nula si está vacía', () => {
    component.codigo = 'R-010';
    component.nombre = 'Riesgo sin descripción';
    component.descripcion = '   ';
    component.activo = false;
    component.guardar();

    expect(service.crearRiesgo).toHaveBeenCalledWith({
      rieCodigo: 'R-010',
      rieNombre: 'Riesgo sin descripción',
      rieDescripcion: null,
      rieActivo: false
    });
    expect(component.mensaje()).toBe('Riesgo creado correctamente.');
    expect(component.guardando()).toBe(false);
  });

  it('edita y actualiza un riesgo existente', () => {
    const root = fixture.nativeElement as HTMLElement;
    component.editar(riesgoActivo);
    expect(component.editandoId()).toBe(7);
    fixture.detectChanges();
    expect(root.querySelector('[data-risk-crud-modal] .modal-size-lg')).not.toBeNull();
    expect(root.querySelector('section > form.grid')).toBeNull();
    expect(component.codigo).toBe('R-007');
    expect(component.nombre).toBe('Riesgo UAT');
    expect(component.descripcion).toBe('Descripción UAT');
    expect(component.activo).toBe(true);

    component.nombre = 'Riesgo modificado';
    component.guardar();

    expect(service.actualizarRiesgo).toHaveBeenCalledWith(7, expect.objectContaining({
      rieNombre: 'Riesgo modificado'
    }));
    expect(component.mensaje()).toBe('Riesgo actualizado correctamente.');
  });

  it('edita un riesgo con descripción null inicializando string vacío', () => {
    component.editar(riesgoInactivo);
    expect(component.editandoId()).toBe(9);
    expect(component.descripcion).toBe('');
    expect(component.activo).toBe(false);
  });

  it('cancela la edición reseteando los campos al llamar a nuevo()', () => {
    component.editar(riesgoActivo);
    component.nuevo();

    expect(component.editandoId()).toBe(0);
    expect(component.codigo).toBe('');
    expect(component.nombre).toBe('');
    expect(component.descripcion).toBe('');
    expect(component.activo).toBe(true);
    expect(component.error()).toBeNull();
    expect(component.mensaje()).toBeNull();
  });

  it('valida campos obligatorios sin invocar el servicio si código o nombre están vacíos', () => {
    component.codigo = '   ';
    component.nombre = '   ';
    component.guardar();

    expect(service.crearRiesgo).not.toHaveBeenCalled();
    expect(component.errorFormulario()).toBe('Código y nombre son obligatorios.');
  });

  it('valida longitud máxima de código (>30)', () => {
    component.codigo = 'C'.repeat(31);
    component.nombre = 'Nombre válido';
    component.guardar();

    expect(service.crearRiesgo).not.toHaveBeenCalled();
    expect(component.errorFormulario()).toBe('Revise las longitudes máximas permitidas del riesgo.');
  });

  it('valida longitud máxima de nombre (>250)', () => {
    component.codigo = 'R-001';
    component.nombre = 'N'.repeat(251);
    component.guardar();

    expect(service.crearRiesgo).not.toHaveBeenCalled();
    expect(component.errorFormulario()).toBe('Revise las longitudes máximas permitidas del riesgo.');
  });

  it('valida longitud máxima de descripción (>2000)', () => {
    component.codigo = 'R-001';
    component.nombre = 'Nombre válido';
    component.descripcion = 'D'.repeat(2001);
    component.guardar();

    expect(service.crearRiesgo).not.toHaveBeenCalled();
    expect(component.errorFormulario()).toBe('Revise las longitudes máximas permitidas del riesgo.');
  });

  it('maneja error con mensaje fallback cuando falla el guardado', () => {
    service.crearRiesgo.mockReturnValue(throwError(() => ({})));
    component.codigo = 'R-008';
    component.nombre = 'Riesgo 8';
    component.guardar();

    expect(component.errorFormulario()).toBe('No se pudo guardar el riesgo.');
    expect(component.guardando()).toBe(false);
  });

  it('maneja error con propiedad message en objeto cuando falla la actualización', () => {
    service.actualizarRiesgo.mockReturnValue(throwError(() => ({ message: 'Error de red en servidor' })));
    component.editar(riesgoActivo);
    component.guardar();

    expect(component.errorFormulario()).toBe('Error de red en servidor');
    expect(component.guardando()).toBe(false);
  });

  it('distingue visual y semánticamente Nuevo de Guardar/Actualizar riesgo', () => {
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-ui-action="new-risk"]')?.getAttribute('aria-label')).toBe('Nuevo riesgo');
    component.nuevo();
    fixture.detectChanges();
    expect(root.querySelector('[data-ui-action="save-risk"]')?.getAttribute('aria-label')).toBe('Guardar nuevo riesgo');

    component.editar(riesgoActivo);
    fixture.detectChanges();
    expect(root.querySelector('[data-ui-action="save-risk"]')?.getAttribute('aria-label')).toBe('Guardar cambios del riesgo');
  });

  it('renderiza la lista de riesgos en el DOM y permite accionar el botón de editar', () => {
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    expect(element.textContent).toContain('R-007');
    expect(element.textContent).toContain('Riesgo UAT');
    expect(element.textContent).toContain('R-009');
    expect(element.textContent).toContain('Inactivo');

    const botonesEditar = element.querySelectorAll('table tbody button');
    expect(botonesEditar.length).toBe(2);
    (botonesEditar[0] as HTMLButtonElement).click();
    expect(component.editandoId()).toBe(7);
    fixture.detectChanges();
    expect(element.querySelector('[data-risk-crud-modal] .modal-size-lg')).not.toBeNull();
    expect(element.querySelector('section > form.grid')).toBeNull();
  });

  it('opens create in a large institutional modal and keeps the table mounted', () => {
    const root = fixture.nativeElement as HTMLElement;
    component.nuevo();
    fixture.detectChanges();

    const modal = root.querySelector('[data-risk-crud-modal]');
    expect(modal?.querySelector('.modal-size-lg')).not.toBeNull();
    expect(modal?.querySelector('.modal-header-institutional')).not.toBeNull();
    expect(modal?.querySelector('.modal-footer-institutional')).not.toBeNull();
    expect(root.querySelector('table')).not.toBeNull();
    expect(root.querySelector('section > form.grid')).toBeNull();
  });

  it('keeps a dirty risk modal open when discard confirmation is cancelled', async () => {
    const root = fixture.nativeElement as HTMLElement;
    component.editar(riesgoActivo);
    component.nombre = 'Unsaved change';

    await component.solicitarCierre();

    expect(confirmation.confirm).toHaveBeenCalledWith(expect.objectContaining({
      title: 'Descartar cambios del riesgo',
      confirmLabel: 'Descartar cambios',
      cancelLabel: 'Continuar editando',
      confirmIcon: 'reject',
      confirmVariant: 'danger'
    }));
    expect(component.modalRiesgoAbierto()).toBe(true);
    fixture.detectChanges();
    expect(root.querySelector('[data-risk-crud-modal]')).not.toBeNull();
  });

  it('discards dirty risk values only after institutional confirmation', async () => {
    confirmation.confirm.mockResolvedValue(true);
    component.editar(riesgoActivo);
    component.nombre = 'Unsaved change';

    await component.solicitarCierre();

    expect(component.modalRiesgoAbierto()).toBe(false);
    expect(component.nombre).toBe('');
  });

  it('closes a pristine risk modal without confirmation', async () => {
    component.editar(riesgoActivo);

    await component.solicitarCierre();

    expect(confirmation.confirm).not.toHaveBeenCalled();
    expect(component.modalRiesgoAbierto()).toBe(false);
  });
  it('renderiza mensaje cuando no existen riesgos registrados', () => {
    component.riesgos.set([]);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    expect(element.textContent).toContain('Sin riesgos registrados.');
  });
});
