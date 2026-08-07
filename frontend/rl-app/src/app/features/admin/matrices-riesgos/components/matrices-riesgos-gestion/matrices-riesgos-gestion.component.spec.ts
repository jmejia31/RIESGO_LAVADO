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

  const riesgo = {
    rieId: 7,
    rieCodigo: 'R-007',
    rieNombre: 'Riesgo UAT',
    rieDescripcion: 'Descripción UAT',
    rieActivo: true,
    rieUsrCreacion: 1,
    rieFechaCreacion: '2026-08-07T12:00:00Z'
  };

  beforeEach(async () => {
    service = {
      listarRiesgos: vi.fn().mockReturnValue(of([riesgo])),
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

  it('carga riesgos activos e inactivos para mantenimiento', () => {
    expect(service.listarRiesgos).toHaveBeenCalledWith(true);
    expect(component.riesgos()).toHaveLength(1);
  });

  it('crea un riesgo válido y recarga la lista', () => {
    component.codigo = 'R-008';
    component.nombre = 'Nuevo riesgo';
    component.descripcion = 'Prueba UAT';
    component.guardar();
    expect(service.crearRiesgo).toHaveBeenCalledWith(expect.objectContaining({ rieCodigo: 'R-008', rieNombre: 'Nuevo riesgo' }));
    expect(component.mensaje()).toContain('creado');
  });

  it('edita y actualiza un riesgo existente', () => {
    component.editar(riesgo);
    component.nombre = 'Riesgo actualizado';
    component.guardar();
    expect(service.actualizarRiesgo).toHaveBeenCalledWith(7, expect.objectContaining({ rieNombre: 'Riesgo actualizado' }));
  });

  it('rechaza datos obligatorios o longitudes inválidas sin invocar backend', () => {
    component.codigo = '';
    component.nombre = '';
    component.guardar();
    expect(service.crearRiesgo).not.toHaveBeenCalled();
    expect(component.error()).toContain('obligatorios');

    component.codigo = 'X'.repeat(31);
    component.nombre = 'Nombre';
    component.guardar();
    expect(component.error()).toContain('longitudes');
  });

  it('muestra el mensaje del backend cuando falla el guardado', () => {
    service.crearRiesgo.mockReturnValue(throwError(() => ({ error: { mensaje: 'Código duplicado' } })));
    component.codigo = 'R-007';
    component.nombre = 'Duplicado';
    component.guardar();
    expect(component.error()).toBe('Código duplicado');
    expect(component.guardando()).toBe(false);
  });
});
