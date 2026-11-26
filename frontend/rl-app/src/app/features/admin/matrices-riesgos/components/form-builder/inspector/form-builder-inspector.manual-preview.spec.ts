import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormBuilderInspectorComponent } from './form-builder-inspector.component';
import { CampoBuilderModel, TIPOS_CONTROLES_DISPONIBLES } from '../../../models/form-builder.models';

describe('FormBuilderInspectorComponent — fórmulas manuales gobernadas', () => {
  let fixture: ComponentFixture<FormBuilderInspectorComponent>;
  let component: FormBuilderInspectorComponent;

  const crearCampo = (formula: string, clave = 'valor_riesgo_inherente'): CampoBuilderModel => ({
    id: 'formula_manual',
    clave,
    etiqueta: 'Valor de riesgo inherente',
    tipo: 'formula',
    formula,
    formulaModo: 'manual',
    formulaNombre: 'Fórmula manual',
    formulaTipoResultado: 'DECIMAL',
    obligatorio: false,
    soloLectura: true,
    anchoColumnas: 1
  });

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [FormBuilderInspectorComponent] }).compileComponents();
    fixture = TestBed.createComponent(FormBuilderInspectorComponent);
    component = fixture.componentInstance;
    component.tiposControles = [...TIPOS_CONTROLES_DISPONIBLES];
  });

  it('evalúa una expresión válida con entradas sintéticas controladas y muestra su resultado', () => {
    const campo = crearCampo('frecuencia + impacto');
    component.campoActivo = campo;
    component.camposFormulario = [
      campo,
      { ...crearCampo('', 'frecuencia_inherente'), tipo: 'numero', formula: undefined },
      { ...crearCampo('', 'impacto_inherente'), tipo: 'numero', formula: undefined }
    ];

    expect(component.formulaPreviewResult).toEqual(expect.objectContaining({ exito: true, valorCalculado: 5 }));
    component.validarFormulaManual();
    expect(campo.formulaValidacion).toBe('valida');

    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Resultado: 5');
    expect(fixture.nativeElement.textContent).toContain('Pendiente: se resolverá al guardar');
  });

  it('explica referencias inexistentes en lugar de aceptar una vista previa inválida', () => {
    const campo = crearCampo('frecuencia_inherente + campo_que_no_existe');
    component.campoActivo = campo;

    expect(component.formulaPreviewResult?.codigo).toBe('FORMULA_REFERENCE_UNKNOWN');
    component.validarFormulaManual();
    expect(campo.formulaValidacion).toBe('invalida');
    expect(component.mensajeFormulaPreview(component.formulaPreviewResult)).toContain('campo inexistente');
  });

  it('detecta autorreferencias y expone el estado real de una coincidencia registrada', () => {
    const campo = crearCampo('valor_riesgo_inherente + 1');
    component.campoActivo = campo;
    component.camposFormulario = [campo];

    expect(component.formulaPreviewResult?.codigo).toBe('FORMULA_CYCLE');
    component.formulaCoincidencias = { valor_riesgo_inherente: 'creada' };
    expect(component.formulaCoincidenciaActual).toBe('creada');
    expect(component.formulaCoincidenciaTexto).toContain('DRAFT');
  });
});
