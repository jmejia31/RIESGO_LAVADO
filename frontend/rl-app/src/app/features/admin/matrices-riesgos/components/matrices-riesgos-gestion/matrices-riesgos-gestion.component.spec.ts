import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { InstitutionalConfirmationService } from '../../../../../shared/confirmation/institutional-confirmation.service';
import { InstitutionalNotificationService } from '../../../../../shared/notification/institutional-notification.service';
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
  let notification: { notify: ReturnType<typeof vi.fn> };

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
    confirmation = { confirm: vi.fn().mockResolvedValue(true) };
    notification = { notify: vi.fn().mockResolvedValue(undefined) };
    await TestBed.configureTestingModule({
      imports: [MatricesRiesgosGestionComponent],
      providers: [
        { provide: MatricesRiesgosService, useValue: service },
        { provide: InstitutionalConfirmationService, useValue: confirmation },
        { provide: InstitutionalNotificationService, useValue: notification }
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

  it('confirma institucionalmente y crea un riesgo válido con descripción nula si está vacía', async () => {
    component.codigo = 'R-010';
    component.nombre = 'Riesgo sin descripción';
    component.descripcion = '   ';
    component.activo = false;
    await component.guardar();

    expect(confirmation.confirm).toHaveBeenCalledWith(expect.objectContaining({
      title: '¿Crear el riesgo R-010?',
      confirmLabel: 'Crear riesgo',
      confirmIcon: 'save'
    }));
    expect(service.crearRiesgo).toHaveBeenCalledWith({
      rieCodigo: 'R-010',
      rieNombre: 'Riesgo sin descripción',
      rieDescripcion: null,
      rieActivo: false
    });
    expect(component.guardando()).toBe(false);
  });

  it('confirma institucionalmente y actualiza un riesgo existente', async () => {
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
    await component.guardar();

    expect(confirmation.confirm).toHaveBeenCalledWith(expect.objectContaining({
      title: '¿Guardar cambios del riesgo R-007?',
      confirmLabel: 'Guardar cambios',
      confirmIcon: 'save'
    }));
    expect(service.actualizarRiesgo).toHaveBeenCalledWith(7, expect.objectContaining({
      rieNombre: 'Riesgo modificado'
    }));
    expect(notification.notify).toHaveBeenCalledWith(expect.objectContaining({
      title: 'Riesgo actualizado',
      icon: 'success',
      confirmLabel: 'Aceptar',
      confirmIcon: 'confirm',
      confirmVariant: 'success'
    }));
  });

  it('devuelve el foco al botón Nuevo riesgo después de cerrar la notificación de guardado', async () => {
    const root = fixture.nativeElement as HTMLElement;
    const trigger = root.querySelector<HTMLButtonElement>('[data-ui-action="new-risk"]');
    expect(trigger).not.toBeNull();
    if (!trigger) throw new Error('Falta botón de Nuevo riesgo en la UI institucional.');

    let cerrarNotificacion!: () => void;
    notification.notify.mockReturnValue(new Promise<void>(resolve => { cerrarNotificacion = resolve; }));
    expect(trigger.isConnected).toBe(true);
    const focus = vi.spyOn(trigger, 'focus');
    trigger.click();
    fixture.detectChanges();
    component.codigo = 'R-080';
    component.nombre = 'Riesgo con retorno de foco';
    // Simulate the application being inert while the shared institutional alert is open.
    root.setAttribute('inert', '');
    try {
      await component.guardar();
      expect(component.modalRiesgoAbierto()).toBe(false);
      await new Promise(resolve => setTimeout(resolve, 0));
      expect(focus).not.toHaveBeenCalled();

      root.removeAttribute('inert');
      cerrarNotificacion();
      await vi.waitFor(() => {
        expect(focus).toHaveBeenCalledTimes(1);
        expect(document.activeElement).toBe(trigger);
      }, { timeout: 1000, interval: 10 });
    } finally {
      root.removeAttribute('inert');
    }
  });


  it('restaura el foco al botón Nuevo riesgo cuando el formulario se abre por código', async () => {
    const root = fixture.nativeElement as HTMLElement;
    const trigger = root.querySelector<HTMLButtonElement>('[data-ui-action="new-risk"]');
    expect(trigger).not.toBeNull();
    if (!trigger) throw new Error('Falta el botón Nuevo riesgo.');
    expect(trigger.isConnected).toBe(true);
    const focus = vi.spyOn(trigger, 'focus');

    let cerrarNotificacion!: () => void;
    notification.notify.mockReturnValue(new Promise<void>(resolve => { cerrarNotificacion = resolve; }));
    component.nuevo();
    fixture.detectChanges();
    component.codigo = 'R-081';
    component.nombre = 'Riesgo creado desde acción programática';
    await component.guardar();
    expect(component.modalRiesgoAbierto()).toBe(false);
    await new Promise(resolve => setTimeout(resolve, 0));
    expect(focus).not.toHaveBeenCalled();

    cerrarNotificacion();
    await vi.waitFor(() => {
      expect(focus).toHaveBeenCalledTimes(1);
      expect(document.activeElement).toBe(trigger);
    }, { timeout: 1000, interval: 10 });
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
  });

  it('valida campos obligatorios sin invocar el servicio si código o nombre están vacíos', async () => {
    component.codigo = '   ';
    component.nombre = '   ';
    await component.guardar();

    expect(service.crearRiesgo).not.toHaveBeenCalled();
    expect(component.errorFormulario()).toBe('Código y nombre son obligatorios.');
  });

  it('valida longitud máxima de código (>30)', async () => {
    component.codigo = 'C'.repeat(31);
    component.nombre = 'Nombre válido';
    await component.guardar();

    expect(service.crearRiesgo).not.toHaveBeenCalled();
    expect(component.errorFormulario()).toBe('Revise las longitudes máximas permitidas del riesgo.');
  });

  it('valida longitud máxima de nombre (>250)', async () => {
    component.codigo = 'R-001';
    component.nombre = 'N'.repeat(251);
    await component.guardar();

    expect(service.crearRiesgo).not.toHaveBeenCalled();
    expect(component.errorFormulario()).toBe('Revise las longitudes máximas permitidas del riesgo.');
  });

  it('valida longitud máxima de descripción (>2000)', async () => {
    component.codigo = 'R-001';
    component.nombre = 'Nombre válido';
    component.descripcion = 'D'.repeat(2001);
    await component.guardar();

    expect(service.crearRiesgo).not.toHaveBeenCalled();
    expect(component.errorFormulario()).toBe('Revise las longitudes máximas permitidas del riesgo.');
  });

  it('maneja error con mensaje fallback cuando falla el guardado', async () => {
    service.crearRiesgo.mockReturnValue(throwError(() => ({})));
    component.codigo = 'R-008';
    component.nombre = 'Riesgo 8';
    await component.guardar();

    expect(component.errorFormulario()).toBe('No se pudo guardar el riesgo.');
    expect(component.guardando()).toBe(false);
    expect(notification.notify).toHaveBeenCalledWith(expect.objectContaining({
      title: 'No se pudo guardar el riesgo',
      icon: 'error',
      confirmLabel: 'Aceptar',
      confirmIcon: 'cancel',
      confirmVariant: 'danger'
    }));
  });

  it('maneja error con propiedad message en objeto cuando falla la actualización', async () => {
    service.actualizarRiesgo.mockReturnValue(throwError(() => ({ message: 'Error de red en servidor' })));
    component.editar(riesgoActivo);
    await component.guardar();

    expect(component.errorFormulario()).toBe('Error de red en servidor');
    expect(component.guardando()).toBe(false);
  });

  it('no llama al API si el usuario cancela la confirmación institucional', async () => {
    confirmation.confirm.mockResolvedValue(false);
    component.codigo = 'R-011';
    component.nombre = 'Riesgo cancelado';

    await component.guardar();

    expect(confirmation.confirm).toHaveBeenCalled();
    expect(service.crearRiesgo).not.toHaveBeenCalled();
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
    expect(element.querySelector('table tbody app-action-icon[action="edit"]')).not.toBeNull();
    expect(element.querySelector('[aria-label="Tabla de riesgos"]')?.className).toContain('overflow-y-scroll');
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
    confirmation.confirm.mockResolvedValue(false);
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
