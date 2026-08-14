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
    listarRiesgos: ReturnType<typeof vi.fn>;
    listarEvaluaciones: ReturnType<typeof vi.fn>;
    obtenerConsolidado: ReturnType<typeof vi.fn>;
    listarHistorialVersionesFormulario: ReturnType<typeof vi.fn>;
    listarFamiliasFormulario: ReturnType<typeof vi.fn>;
    obtenerFamiliaFormularioPorId: ReturnType<typeof vi.fn>;
    crearFamiliaFormulario: ReturnType<typeof vi.fn>;
    actualizarFamiliaFormulario: ReturnType<typeof vi.fn>;
    desactivarFamiliaFormulario: ReturnType<typeof vi.fn>;
    crearEvaluacion: ReturnType<typeof vi.fn>;
    actualizarEvaluacion: ReturnType<typeof vi.fn>;
    transicionarEvaluacion: ReturnType<typeof vi.fn>;
    obtenerFlujos: ReturnType<typeof vi.fn>;
    crearBorradorFormulario: ReturnType<typeof vi.fn>;
    actualizarBorradorFormulario: ReturnType<typeof vi.fn>;
    clonarVersionFormulario: ReturnType<typeof vi.fn>;
    cargarEvidencia: ReturnType<typeof vi.fn>;
    vincularEvidencia: ReturnType<typeof vi.fn>;
    eliminarEvidenciaHuerfana: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    service = {
      listarFamiliasFormulario: vi.fn().mockReturnValue(of([
        { famId: 1, famCodigo: 'MATRIZ_RIESGOS_LAFT', famNombre: 'Matriz de Riesgos LAFT', famDescripcion: '', famActivo: true }
      ])),
      obtenerFamiliaFormularioPorId: vi.fn().mockReturnValue(of({ famId: 1 })),
      crearFamiliaFormulario: vi.fn().mockReturnValue(of(2)),
      actualizarFamiliaFormulario: vi.fn().mockReturnValue(of(true)),
      desactivarFamiliaFormulario: vi.fn().mockReturnValue(of(true)),
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
      listarRiesgos: vi.fn().mockReturnValue(of([{
        rieId: 5,
        rieCodigo: 'R-005',
        rieNombre: 'Riesgo institucional',
        rieActivo: true,
        rieUsrCreacion: 1,
        rieFechaCreacion: '2026-08-07T08:00:00'
      }])),
      listarEvaluaciones: vi.fn().mockReturnValue(of([])),
      obtenerConsolidado: vi.fn().mockReturnValue(of([])),
      listarHistorialVersionesFormulario: vi.fn().mockReturnValue(of([])),
      crearEvaluacion: vi.fn().mockReturnValue(of(20)),
      actualizarEvaluacion: vi.fn().mockReturnValue(of({ success: true })),
      transicionarEvaluacion: vi.fn().mockReturnValue(of({ success: true })),
      obtenerFlujos: vi.fn().mockReturnValue(of([])),
      crearBorradorFormulario: vi.fn().mockReturnValue(of(21)),
      actualizarBorradorFormulario: vi.fn().mockReturnValue(of({ success: true })),
      clonarVersionFormulario: vi.fn().mockReturnValue(of(22)),
      cargarEvidencia: vi.fn().mockReturnValue(of({ eviId: 32 })),
      vincularEvidencia: vi.fn().mockReturnValue(of({ success: true })),
      eliminarEvidenciaHuerfana: vi.fn().mockReturnValue(of({ success: true }))
    };

    await TestBed.configureTestingModule({
      imports: [MatricesRiesgosComponent],
      providers: [{ provide: MatricesRiesgosService, useValue: service }]
    }).compileComponents();

    fixture = TestBed.createComponent(MatricesRiesgosComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('crea el componente y carga la versión dinámica y los riesgos reales', () => {
    expect(component).toBeTruthy();
    expect(service.obtenerVersionVigenteFormulario).toHaveBeenCalled();
    expect(service.metodologiaVigente).toHaveBeenCalled();
    expect(service.listarRiesgos).toHaveBeenCalled();
    expect(component.versionVigente()?.verId).toBe(10);
    expect(component.riesgos()[0].rieCodigo).toBe('R-005');
  });

  it('construye secciones y campos desde la versión del formulario', () => {
    expect(component.secciones()).toHaveLength(1);
    expect(component.secciones()[0].campos[0].clave).toBe('area_principal');
    expect(component.totalCampos()).toBe(1);
  });

  it('bloquea el guardado hasta seleccionar un riesgo real y completar campos obligatorios', () => {
    component.nuevaEvaluacion();
    expect(component.puedeGuardar()).toBe(false);

    component.riesgoId.set(5);
    component.actualizarRespuesta(component.secciones()[0].campos[0], 'Cumplimiento');

    expect(component.puedeGuardar()).toBe(true);
  });

  it('crea una evaluación enviando respuestas dinámicas', () => {
    component.nuevaEvaluacion();
    component.riesgoId.set(5);
    component.actualizarRespuesta(component.secciones()[0].campos[0], 'Cumplimiento');
    component.guardarEvaluacion();

    expect(service.crearEvaluacion).toHaveBeenCalledWith(expect.objectContaining({
      evaRiesgoId: 5,
      evaVersionId: 10,
      evaDataJson: JSON.stringify({ area_principal: 'Cumplimiento' })
    }));
  });

  it('no expone formularios ni cálculos locales retirados', () => {
    const instancia = component as unknown as Record<string, unknown>;
    expect('nuevaMatriz' in instancia).toBe(false);
    expect('criteriosForm' in instancia).toBe(false);
    expect('nivelResidualLocal' in instancia).toBe(false);
  });

  it('permite crear, editar y desactivar familias desde la interfaz', () => {
    component.abrirModalCrearFamilia();
    component.nuevaFamiliaCodigo = 'FORM_B';
    component.nuevaFamiliaNombre = 'Formulario B';
    component.nuevaFamiliaDescripcion = 'Definicion institucional';
    component.guardarFamilia();

    expect(service.crearFamiliaFormulario).toHaveBeenCalledWith({
      famCodigo: 'FORM_B',
      famNombre: 'Formulario B',
      famDescripcion: 'Definicion institucional'
    });
    expect(component.modalFamiliaAbierto()).toBe(false);

    const familia = component.familias()[0];
    component.abrirModalEditarFamilia(familia);
    component.nuevaFamiliaNombre = 'Formulario LAFT actualizado';
    component.guardarFamilia();
    expect(service.actualizarFamiliaFormulario).toHaveBeenCalledWith(1, expect.objectContaining({
      famNombre: 'Formulario LAFT actualizado'
    }));

    component.desactivarFamilia(familia);
    expect(service.desactivarFamiliaFormulario).toHaveBeenCalledWith(1);
  });

  it('navega por pestanas con teclado y carga el consolidado al activarlo', () => {
    const evento = new KeyboardEvent('keydown', { key: 'End' });
    const preventDefault = vi.spyOn(evento, 'preventDefault');
    component.onKeydownTab(evento, 'evaluaciones');

    expect(preventDefault).toHaveBeenCalled();
    expect(component.tab()).toBe('plantillas');

    component.seleccionarTab('consolidado');
    expect(component.tab()).toBe('consolidado');
    expect(service.obtenerConsolidado).toHaveBeenCalled();
  });

  it('edita una evaluacion existente, transiciona su estado y recarga sus flujos', () => {
    const evaluacion = {
      evaId: 20,
      evaRiesgoId: 5,
      evaVersionId: 10,
      evaEstado: 'BORRADOR',
      evaDataJson: '{"area_principal":"Cumplimiento"}',
      evaFechaEval: '2026-08-14T00:00:00',
      evaUsrEval: 1,
      evaVersionRow: 1,
      evaActivo: true
    };

    component.editarEvaluacion(evaluacion);
    expect(component.tab()).toBe('captura');
    expect(component.riesgoId()).toBe(5);
    expect(component.valorRespuesta(component.secciones()[0].campos[0])).toBe('Cumplimiento');
    expect(service.obtenerFlujos).toHaveBeenCalledWith(20);

    component.nuevoEstado = 'EN_REVISION';
    component.motivoTransicion = 'Revision completa';
    component.transicionarEvaluacion(evaluacion);
    expect(service.transicionarEvaluacion).toHaveBeenCalledWith(20, 'EN_REVISION', 'Revision completa');
    expect(component.motivoTransicion).toBe('');
  });

  it('guarda un borrador de formulario valido y bloquea JSON invalido', () => {
    const version = { ...component.versionVigente()!, verVigente: false, verEstado: 'DRAFT' as const };
    component.abrirDefinicion(version);
    component.definicionTecnica = '{invalido';
    component.guardarDefinicion();
    expect(service.actualizarBorradorFormulario).not.toHaveBeenCalled();
    expect(component.error()).toContain('JSON');

    component.definicionTecnica = '{"codigoFormulario":"FORM_A","secciones":[]}';
    component.guardarDefinicion();
    expect(service.actualizarBorradorFormulario).toHaveBeenCalledWith(10, component.definicionTecnica);
    expect(component.versionEditando()).toBeNull();
  });

  it('crea el borrador base de una familia seleccionada y vincula evidencia', () => {
    component.abrirModalCrearFormulario();
    component.nuevoFormularioCodigo = 'FORM_NUEVO';
    component.nuevoFormularioNombre = 'Formulario nuevo';
    component.guardarNuevoFormulario();
    expect(service.crearBorradorFormulario).toHaveBeenCalledWith(
      1,
      'FORM_NUEVO',
      expect.stringContaining('FORM_NUEVO')
    );

    const archivo = new File(['evidencia'], 'evidencia.pdf', { type: 'application/pdf' });
    component.seleccionarArchivo({ target: { files: { item: () => archivo } } } as unknown as Event);
    component.cargarYVincularEvidencia({ evaId: 20 } as never);
    expect(service.cargarEvidencia).toHaveBeenCalledWith(archivo);
    expect(service.vincularEvidencia).toHaveBeenCalledWith({
      entidadId: 20,
      evidenciaId: 32,
      tipoEntidad: 'evaluacion'
    });
  });
});
