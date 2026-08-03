import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { MatricesRiesgosComponent } from './matrices-riesgos.component';

describe('MatricesRiesgosComponent', () => {
  let fixture: ComponentFixture<MatricesRiesgosComponent>;
  let component: MatricesRiesgosComponent;
  let service: {
    obtenerVersionVigenteFormulario: ReturnType<typeof vi.fn>;
    metodologiaVigente: ReturnType<typeof vi.fn>;
    listarEvaluaciones: ReturnType<typeof vi.fn>;
    obtenerConsolidado: ReturnType<typeof vi.fn>;
    listarHistorialVersionesFormulario: ReturnType<typeof vi.fn>;
    crearEvaluacion: ReturnType<typeof vi.fn>;
    actualizarEvaluacion: ReturnType<typeof vi.fn>;
    obtenerRevisiones: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    service = {
      obtenerVersionVigenteFormulario: vi.fn().mockReturnValue(of({
        verId: 10,
        verFamiliaId: 1,
        verCodigo: 'FORM_A',
        verVersion: 2,
        verJson: JSON.stringify({
          codigoFormulario: 'FORM_A',
          nombreFormulario: 'Formulario A',
          secciones: [{
            clave: 'identificacion',
            titulo: 'Identificación',
            orden: 1,
            campos: [{
              clave: 'area_principal',
              etiqueta: 'Área principal',
              tipo: 'texto',
              obligatorio: true,
              soloLectura: false
            }]
          }]
        }),
        verHash: 'hash',
        verEstado: 'PUBLISHED',
        verVigente: true,
        verFechaCreacion: '2026-08-03T10:00:00',
        verUsrCreacion: 1
      })),
      metodologiaVigente: vi.fn().mockReturnValue(of({
        versionFormularioId: 10,
        codigo: 'FORM_A',
        version: 2,
        secciones: [],
        catalogos: [],
        reglas: [{ codigo: 'CALCULO_VRI_VRR', version: '1.0', algoritmoId: 'MATRICES_VRI_ADITIVO_1_9' }]
      })),
      listarEvaluaciones: vi.fn().mockReturnValue(of([])),
      obtenerConsolidado: vi.fn().mockReturnValue(of([])),
      listarHistorialVersionesFormulario: vi.fn().mockReturnValue(of([])),
      crearEvaluacion: vi.fn().mockReturnValue(of(20)),
      actualizarEvaluacion: vi.fn().mockReturnValue(of({ success: true })),
      obtenerRevisiones: vi.fn().mockReturnValue(of([]))
    };

    await TestBed.configureTestingModule({
      imports: [MatricesRiesgosComponent],
      providers: [{ provide: MatricesRiesgosService, useValue: service }]
    }).compileComponents();

    fixture = TestBed.createComponent(MatricesRiesgosComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('crea el componente y carga la versión dinámica', () => {
    expect(component).toBeTruthy();
    expect(service.obtenerVersionVigenteFormulario).toHaveBeenCalled();
    expect(service.metodologiaVigente).toHaveBeenCalled();
    expect(component.versionVigente()?.verId).toBe(10);
  });

  it('construye secciones y campos desde la versión del formulario', () => {
    expect(component.secciones()).toHaveLength(1);
    expect(component.secciones()[0].campos[0].clave).toBe('area_principal');
    expect(component.totalCampos()).toBe(1);
  });

  it('bloquea el guardado hasta completar riesgo y campos obligatorios', () => {
    component.nuevaEvaluacion();
    expect(component.puedeGuardar()).toBe(false);

    component.riesgoId = 5;
    component.actualizarRespuesta(component.secciones()[0].campos[0], 'Cumplimiento');

    expect(component.puedeGuardar()).toBe(true);
  });

  it('crea una evaluación enviando respuestas dinámicas', () => {
    component.nuevaEvaluacion();
    component.riesgoId = 5;
    component.actualizarRespuesta(component.secciones()[0].campos[0], 'Cumplimiento');
    component.guardarEvaluacion();

    expect(service.crearEvaluacion).toHaveBeenCalledWith(expect.objectContaining({
      evaRiesgoId: 5,
      evaVersionId: 10,
      evaDataJson: JSON.stringify({ area_principal: 'Cumplimiento' })
    }));
  });

  it('no expone señales ni formularios del modelo heredado', () => {
    const instancia = component as unknown as Record<string, unknown>;
    expect('variablesPorFactor' in instancia).toBe(false);
    expect('nuevaMatriz' in instancia).toBe(false);
    expect('criteriosForm' in instancia).toBe(false);
    expect('nivelResidualLocal' in instancia).toBe(false);
  });
});
