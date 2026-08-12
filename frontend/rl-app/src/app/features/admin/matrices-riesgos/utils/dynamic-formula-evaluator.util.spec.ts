import { CampoFormulario, RespuestasFormulario } from '../models/matrices-riesgos.models';
import { evaluarFormulaCampo, recalcularFórmulasEvaluacion } from './dynamic-formula-evaluator.util';

describe('Motor de Evaluación de Fórmulas Dinámicas (Fase 5)', () => {
  const camposPrueba: CampoFormulario[] = [
    { clave: 'probabilidad', etiqueta: 'Probabilidad', tipo: 'numero', obligatorio: true, soloLectura: false },
    { clave: 'impacto', etiqueta: 'Impacto', tipo: 'numero', obligatorio: true, soloLectura: false },
    { clave: 'vri_calculado', etiqueta: 'VRI', tipo: 'formula', formula: 'probabilidad * impacto', obligatorio: false, soloLectura: true }
  ];

  it('evalúa correctamente una fórmula matemática simple entre dos campos', () => {
    const respuestas: RespuestasFormulario = { probabilidad: 4, impacto: 5 };
    const resultado = evaluarFormulaCampo('probabilidad * impacto', respuestas, camposPrueba);

    expect(resultado.exito).toBe(true);
    expect(resultado.valorCalculado).toBe(20);
  });

  it('recalcula automáticamente los campos calculados cuando cambian los dependientes', () => {
    const respuestasIniciales: RespuestasFormulario = { probabilidad: 3, impacto: 4 };
    const { respuestasActualizadas, calculosJson } = recalcularFórmulasEvaluacion(camposPrueba, respuestasIniciales);

    expect(respuestasActualizadas['vri_calculado']).toBe(12);
    expect(calculosJson['vri_calculado']).toBeDefined();
    expect(calculosJson['vri_calculado'].resultado).toBe(12);
  });

  it('soporta la fórmula predeterminada VRI/VRR', () => {
    const respuestas: RespuestasFormulario = { probabilidad: 2, impacto: 5 };
    const resultado = evaluarFormulaCampo('VRI/VRR', respuestas, camposPrueba);

    expect(resultado.exito).toBe(true);
    expect(resultado.valorCalculado).toBe(10);
  });

  it('maneja con seguridad errores sintácticos o referencias nulas', () => {
    const respuestas: RespuestasFormulario = { probabilidad: null, impacto: 4 };
    const resultado = evaluarFormulaCampo('probabilidad * impacto', respuestas, camposPrueba);

    expect(resultado.exito).toBe(true);
    expect(resultado.valorCalculado).toBe(0);
  });
});
