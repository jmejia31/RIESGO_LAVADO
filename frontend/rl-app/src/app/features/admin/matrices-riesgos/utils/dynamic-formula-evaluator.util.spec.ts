import { CampoFormulario, RespuestasFormulario } from '../models/matrices-riesgos.models';
import { evaluarFormulaCampo, recalcularFormulasEvaluacion, detectarCicloEnFormulas } from './dynamic-formula-evaluator.util';

describe('Motor de Evaluacion de Formulas Dinamicas Seguro (Fase 5)', () => {
  const camposPrueba: CampoFormulario[] = [
    { clave: 'probabilidad', etiqueta: 'Probabilidad', tipo: 'numero', obligatorio: true, soloLectura: false },
    { clave: 'impacto', etiqueta: 'Impacto', tipo: 'numero', obligatorio: true, soloLectura: false },
    { clave: 'vri_calculado', etiqueta: 'VRI', tipo: 'formula', formula: 'probabilidad * impacto', obligatorio: false, soloLectura: true },
    { clave: 'vrr_calculado', etiqueta: 'VRR', tipo: 'formula', formula: 'vri_calculado - 5', obligatorio: false, soloLectura: true }
  ];

  it('evalua correctamente una formula matematica simple entre dos campos sin usar eval', () => {
    const respuestas: RespuestasFormulario = { probabilidad: 4, impacto: 5 };
    const resultado = evaluarFormulaCampo('probabilidad * impacto', respuestas, 'vri_calculado');

    expect(resultado.exito).toBe(true);
    expect(resultado.valorCalculado).toBe(20);
  });

  it('resuelve dependencias encadenadas entre multiples formulas (A -> B -> C)', () => {
    const respuestasIniciales: RespuestasFormulario = { probabilidad: 3, impacto: 4 };
    const { respuestasActualizadas, calculosJson } = recalcularFormulasEvaluacion(camposPrueba, respuestasIniciales);

    expect(respuestasActualizadas['vri_calculado']).toBe(12);
    expect(respuestasActualizadas['vrr_calculado']).toBe(7);
    expect(calculosJson['vri_calculado'].resultado).toBe(12);
    expect(calculosJson['vrr_calculado'].resultado).toBe(7);
  });

  it('detecta ciclos de referencias circulares directas e indirectas (A -> B -> A)', () => {
    const camposConCiclo: CampoFormulario[] = [
      { clave: 'campo_a', etiqueta: 'Campo A', tipo: 'formula', formula: 'campo_b + 10', obligatorio: false, soloLectura: true },
      { clave: 'campo_b', etiqueta: 'Campo B', tipo: 'formula', formula: 'campo_a * 2', obligatorio: false, soloLectura: true }
    ];

    const camposMap = new Map<string, CampoFormulario>();
    camposConCiclo.forEach(c => camposMap.set(c.clave.toLowerCase(), c));

    const tieneCiclo = detectarCicloEnFormulas('campo_a', camposMap);
    expect(tieneCiclo).toBe(true);

    const resultado = evaluarFormulaCampo('campo_b + 10', {}, 'campo_a', camposMap);
    expect(resultado.exito).toBe(false);
    expect(resultado.error).toContain('Referencia circular detectada');
  });

  it('rechaza expliticamente referencias a campos inexistentes en la formula', () => {
    const camposMap = new Map<string, CampoFormulario>();
    camposMap.set('probabilidad', { clave: 'probabilidad', etiqueta: 'Probabilidad', tipo: 'numero', obligatorio: true, soloLectura: false });

    const resultado = evaluarFormulaCampo('probabilidad + campo_fantasma', { probabilidad: 5 }, 'formula_invalida', camposMap);
    expect(resultado.exito).toBe(false);
    expect(resultado.error).toContain("Referencia a campo inexistente 'campo_fantasma'");
  });

  it('soporta la formula predeterminada VRI/VRR', () => {
    const respuestas: RespuestasFormulario = { probabilidad: 2, impacto: 5 };
    const resultado = evaluarFormulaCampo('VRI/VRR', respuestas, 'vri');

    expect(resultado.exito).toBe(true);
    expect(resultado.valorCalculado).toBe(10);
  });

  it('maneja con seguridad errores de division por cero y notacion incompleta', () => {
    const respuestas: RespuestasFormulario = { probabilidad: 10, impacto: 0 };
    const resultado = evaluarFormulaCampo('probabilidad / impacto', respuestas, 'vri');

    expect(resultado.exito).toBe(false);
    expect(resultado.codigo).toBe('FORMULA_DIVISION_BY_ZERO');
  });
});
