import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormBuilderInspectorComponent } from './form-builder-inspector.component';
import { normalizarJsonABuilderModel, serializarBuilderModelAJson } from '../../../models/form-builder.models';
import { validarFormBuilderModel } from '../../../utils/form-builder-validator.util';

describe('FormBuilderInspectorComponent — fórmula central pinneada', () => {
  let fixture: ComponentFixture<FormBuilderInspectorComponent>;
  let component: FormBuilderInspectorComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [FormBuilderInspectorComponent] }).compileComponents();
    fixture = TestBed.createComponent(FormBuilderInspectorComponent);
    component = fixture.componentInstance;
    component.campoActivo = { id: 'formula_1', clave: 'total', etiqueta: 'Total', tipo: 'formula', obligatorio: false, soloLectura: true, anchoColumnas: 1 };
    component.formulaVersionesDisponibles = [{ formulaId: 7, formulaVersionId: 8, codigo: 'F_TOTAL', nombre: 'Total', version: 2, estado: 'APPROVED', tipoResultado: 'DECIMAL', hash: 'a'.repeat(64), expresion: 'a + b' }];
    fixture.detectChanges();
  });

  it('guarda la versión exacta y no una referencia latest', () => {
    component.seleccionarFormulaCentral('8');
    expect(component.campoActivo?.formulaId).toBe(7);
    expect(component.campoActivo?.formulaVersionId).toBe(8);
    expect(component.campoActivo?.formulaVersion).toBe(2);
    expect(component.campoActivo?.formula).toBe('a + b');
  });

  it('expone opciones acotadas con búsqueda por código, versión, estado y expresión', () => {
    const option = component.formulaVersionSelectOptions[0];
    expect(option).toEqual(expect.objectContaining({
      value: 8,
      label: 'F_TOTAL · v2 · APPROVED',
      searchText: expect.stringContaining('a + b')
    }));
  });

  it('rechaza identificadores no enteros sin alterar la fórmula fijada', () => {
    component.seleccionarFormulaCentral(8);
    component.seleccionarFormulaCentral('8.5');
    expect(component.campoActivo?.formulaVersionId).toBe(8);
    expect(component.campoActivo?.formula).toBe('a + b');
  });
  it('permite desvincular una fórmula central sin resolver otra versión', () => {
    component.seleccionarFormulaCentral(8);
    component.seleccionarFormulaCentral(null);
    expect(component.campoActivo?.formulaVersionId).toBeUndefined();
    expect(component.campoActivo?.formulaId).toBeUndefined();
    expect(component.campoActivo?.formula).toBeUndefined();
  });

  it('elimina del JSON los metadatos centrales al desvincular', () => {
    const model = normalizarJsonABuilderModel(JSON.stringify({
      secciones: [{ clave: 'general', campos: [{ clave: 'total', tipo: 'formula', formula: 'a + b', formulaFuente: 'formula', formulaId: 7, formulaVersionId: 8, formulaCodigo: 'F_TOTAL', formulaVersion: 2 }] }]
    }));
    const campo = model.secciones[0].campos[0];
    component.campoActivo = campo;
    component.seleccionarFormulaCentral(null);

    const serialized = JSON.parse(serializarBuilderModelAJson(model)) as { secciones: Array<{ campos: Array<Record<string, unknown>> }> };
    expect(serialized.secciones[0].campos[0]['formulaVersionId']).toBeUndefined();
    expect(serialized.secciones[0].campos[0]['formulaId']).toBeUndefined();
    expect(serialized.secciones[0].campos[0]['formula']).toBeNull();
  });

  it('persiste solo la vinculación central y permite validar el borrador sin fórmula embebida', () => {
    const model = normalizarJsonABuilderModel(JSON.stringify({
      secciones: [{ clave: 'riesgo', titulo: 'Riesgo', campos: [
        { clave: 'frecuencia_inherente', etiqueta: 'Frecuencia', tipo: 'numero' },
        { clave: 'impacto_inherente', etiqueta: 'Impacto', tipo: 'numero' },
        { clave: 'valor_riesgo_inherente', etiqueta: 'VRI', tipo: 'formula', formulaVersionId: 8, formulaCodigo: 'F01_VALOR_RIESGO_INHERENTE', formulaVersion: 1 }
      ] }]
    }));

    const serialized = JSON.parse(serializarBuilderModelAJson(model)) as { secciones: Array<{ campos: Array<Record<string, unknown>> }> };
    const vri = serialized.secciones[0].campos[2];

    expect(vri['formulaVersionId']).toBe(8);
    expect(vri['formulaCodigo']).toBe('F01_VALOR_RIESGO_INHERENTE');
    expect(vri['formula']).toBeUndefined();
    expect(validarFormBuilderModel(model)).toEqual([]);
  });
});
