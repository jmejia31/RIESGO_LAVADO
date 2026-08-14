import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { MatricesRiesgosGestionComponent } from './matrices-riesgos-gestion.component';

describe('MatricesRiesgosGestionComponent', () => {
  let fixture: ComponentFixture<MatricesRiesgosGestionComponent>;
  let component: MatricesRiesgosGestionComponent;
  let service: {
    listarRiesgos: ReturnType<typeof vi.fn>;
    crearRiesgo: ReturnType<typeof vi.fn>;
    actualizarRiesgo: ReturnType<typeof vi.fn>;
  };

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
      crearRiesgo: vi.fn().mockReturnValue(of(8)),
      actualizarRiesgo: vi.fn().mockReturnValue(of({ success: true }))
    };
    await TestBed.configureTestingModule({
      imports: [MatricesRiesgosGestionComponent],
      providers: [{ provide: MatricesRiesgosService, useValue: service }]
    }).compileComponents();
    fixture = TestBed.createComponent(MatricesRiesgosGestionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('carga riesgos activos e inactivos al inicializar', () => {
    expect(service.listarRiesgos).toHaveBeenCalledWith(true);
    expect(component.riesgos()).toHaveLength(2);
    expect(component.cargando()).toBe(false);
    expect(component.error()).toBeNull();
  });

  it('maneja error al listar riesgos y propaga mensaje por defecto si no viene del backend', () => {
    service.listarRiesgos.mockReturnValue(throwError(() => ({})));
    component.cargar();
    expect(component.error()).toBe('No se pudieron cargar los riesgos.');
    expect(component.cargando()).toBe(false);
  });

  it('maneja error con mensaje institucional al listar riesgos', () => {
    service.listarRiesgos.mockReturnValue(throwError(() => ({ error: { mensaje: 'Error de conexión' } })));
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
    component.editar(riesgoActivo);
    expect(component.editandoId()).toBe(7);
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
    component.nuevo(true);

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
    expect(component.error()).toBe('Código y nombre son obligatorios.');
  });

  it('valida longitud máxima de código (>30)', () => {
    component.codigo = 'C'.repeat(31);
    component.nombre = 'Nombre válido';
    component.guardar();

    expect(service.crearRiesgo).not.toHaveBeenCalled();
    expect(component.error()).toBe('Revise las longitudes máximas permitidas del riesgo.');
  });

  it('valida longitud máxima de nombre (>250)', () => {
    component.codigo = 'R-001';
    component.nombre = 'N'.repeat(251);
    component.guardar();

    expect(service.crearRiesgo).not.toHaveBeenCalled();
    expect(component.error()).toBe('Revise las longitudes máximas permitidas del riesgo.');
  });

  it('valida longitud máxima de descripción (>2000)', () => {
    component.codigo = 'R-001';
    component.nombre = 'Nombre válido';
    component.descripcion = 'D'.repeat(2001);
    component.guardar();

    expect(service.crearRiesgo).not.toHaveBeenCalled();
    expect(component.error()).toBe('Revise las longitudes máximas permitidas del riesgo.');
  });

  it('maneja error con mensaje fallback cuando falla el guardado', () => {
    service.crearRiesgo.mockReturnValue(throwError(() => ({})));
    component.codigo = 'R-008';
    component.nombre = 'Riesgo 8';
    component.guardar();

    expect(component.error()).toBe('No se pudo guardar el riesgo.');
    expect(component.guardando()).toBe(false);
  });

  it('maneja error con propiedad message en objeto cuando falla la actualización', () => {
    service.actualizarRiesgo.mockReturnValue(throwError(() => ({ message: 'Error de red en servidor' })));
    component.editar(riesgoActivo);
    component.guardar();

    expect(component.error()).toBe('Error de red en servidor');
    expect(component.guardando()).toBe(false);
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
  });

  it('renderiza mensaje cuando no existen riesgos registrados', () => {
    component.riesgos.set([]);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    expect(element.textContent).toContain('Sin riesgos registrados.');
  });
});
