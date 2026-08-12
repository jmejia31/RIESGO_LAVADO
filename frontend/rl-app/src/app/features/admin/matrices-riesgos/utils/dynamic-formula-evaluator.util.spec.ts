import { CampoFormulario, RespuestasFormulario } from '../models/matrices-riesgos.models';
import { evaluarFormulaCampo, recalcularFormulasEvaluacion } from './dynamic-formula-evaluator.util';

describe('Motor de Evaluación de Fórmulas Dinámicas Seguro (Fase 5)', () => {
  const camposPrueba: CampoFormulario[] = [
    { clave: 'probabilidad', etiqueta: 'Probabilidad', tipo: 'numero', obligatorio: true, soloLectura: false },
    { clave: 'impacto', etiqueta: 'Impacto', tipo: 'numero', obligatorio: true, soloLectura: false },
    { clave: 'vri_calculado', etiqueta: 'VRI', tipo: 'formula', formula: 'probabilidad * impacto', obligatorio: false, soloLectura: true },
    { clave: 'vrr_calculado', etiqueta: 'VRR', tipo: 'formula', formula: 'vri_calculado - 5', obligatorio: false, soloLectura: true }
  ];

  it('evalúa correctamente una fórmula matemática simple entre dos campos sin usar eval', () => {
    const respuestas: RespuestasFormulario = { probabilidad: 4, impacto: 5 };
    const resultado = evaluarFormulaCampo('probabilidad * impacto', respuestas, 'vri_calculado');

    expect(resultado.exito).toBe(true);
    expect(resultado.valorCalculado).toBe(20);
  });

  it('resuelve dependencias encadenadas entre múltiples fórmulas', () => {
    const respuestasIniciales: RespuestasFormulario = { probabilidad: 3, impacto: 4 };
    const { respuestasActualizadas, calculosJson } = recalcularFormulasEvaluacion(camposPrueba, respuestasIniciales);

    expect(respuestasActualizadas['vri_calculado']).toBe(12);
    expect(respuestasActualizadas['vrr_calculado']).toBe(7);
    expect(calculosJson['vri_calculado'].resultado).toBe(12);
    expect(calculosJson['vrr_calculado'].resultado).toBe(7);
  });

  it('detecta referencias circulares en fórmulas', () => {
    const visitados = new Set<string>(['campo_a']);
    const resultado = evaluarFormulaCampo('campo_a + 1', { campo_a: 10 }, 'campo_a', visitados);

    expect(resultado.exito).toBe(false);
    expect(resultado.error).toContain('Referencia circular detectada');
  });

  it('soporta la fórmula predeterminada VRI/VRR', () => {
    const respuestas: RespuestasFormulario = { probabilidad: 2, impacto: 5 };
    const resultado = evaluarFormulaCampo('VRI/VRR', respuestas, 'vri');

    expect(resultado.exito).toBe(true);
    expect(resultado.valorCalculado).toBe(10);
  });

  it('maneja con seguridad errores de división por cero y notación incompleta', () => {
    const respuestas: RespuestasFormulario = { probabilidad: 10, impacto: 0 };
    const resultado = evaluarFormulaCampo('probabilidad / impacto', respuestas, 'vri');

    expect(resultado.exito).toBe(false);
    expect(resultado.error).toContain('División por cero');
  });
});
