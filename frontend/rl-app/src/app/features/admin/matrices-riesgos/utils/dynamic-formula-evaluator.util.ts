import { CampoFormulario, RespuestasFormulario } from '../models/matrices-riesgos.models';

export interface ResultadoEvaluacionFormula {
  valorCalculado: number | string | null;
  exito: boolean;
  error?: string;
}

export function evaluarFormulaCampo(
  formula: string | undefined | null,
  respuestas: RespuestasFormulario,
  campos: CampoFormulario[]
): ResultadoEvaluacionFormula {
  if (!formula || formula.trim() === '') {
    return { valorCalculado: null, exito: true };
  }

  const exprLimpia = formula.trim();

  // Caso especial predeterminado VRI / VRR (multiplicación o combinación probabilística si aplica)
  if (exprLimpia.toUpperCase() === 'VRI' || exprLimpia.toUpperCase() === 'VRR' || exprLimpia.toUpperCase() === 'VRI/VRR') {
    const prob = Number(respuestas['probabilidad'] ?? respuestas['PROBABILIDAD'] ?? 0);
    const imp = Number(respuestas['impacto'] ?? respuestas['IMPACTO'] ?? 0);

    if (prob > 0 && imp > 0) {
      return { valorCalculado: prob * imp, exito: true };
    }
  }

  try {
    // Reemplazar claves técnicas por sus valores numéricos en respuestas
    let expresionSustituida = exprLimpia;
    const regexClaves = /[a-zA-Z_][a-zA-Z0-9_]*/g;

    expresionSustituida = expresionSustituida.replace(regexClaves, (match) => {
      // Ignorar palabras clave matematicas reservadas si las hubiera
      if (['Math', 'abs', 'min', 'max', 'round'].includes(match)) {
        return match;
      }

      const val = respuestas[match] ?? respuestas[match.toLowerCase()] ?? respuestas[match.toUpperCase()];
      if (val === null || val === undefined || val === '') {
        return '0';
      }

      const num = Number(val);
      return isNaN(num) ? '0' : num.toString();
    });

    // Sanitizar caracteres permitidos para evaluacion matematica segura (numeros, +, -, *, /, (, ), ., espacio)
    if (!/^[0-9+\-*/().\s]+$/.test(expresionSustituida)) {
      return { valorCalculado: null, exito: false, error: 'Expresión contiene caracteres no numéricos desautorizados' };
    }

    // Evaluacion matematica segura usando Function
    const func = new Function(`"use strict"; return (${expresionSustituida});`);
    const res = func();

    if (typeof res === 'number' && !isNaN(res) && isFinite(res)) {
      return { valorCalculado: Math.round(res * 100) / 100, exito: true };
    }

    return { valorCalculado: res ?? null, exito: true };
  } catch (err) {
    return {
      valorCalculado: null,
      exito: false,
      error: err instanceof Error ? err.message : 'Error en cálculo'
    };
  }
}

export function recalcularFórmulasEvaluacion(
  campos: CampoFormulario[],
  respuestas: RespuestasFormulario
): { respuestasActualizadas: RespuestasFormulario; calculosJson: Record<string, any> } {
  const respuestasNuevas = { ...respuestas };
  const calculosMap: Record<string, any> = {};

  const camposFormula = campos.filter(c => c.tipo === 'formula' || (c.formula && c.formula.trim() !== ''));

  for (const campo of camposFormula) {
    const res = evaluarFormulaCampo(campo.formula, respuestasNuevas, campos);
    if (res.exito && res.valorCalculado !== null) {
      respuestasNuevas[campo.clave] = res.valorCalculado;
      calculosMap[campo.clave] = {
        formula: campo.formula,
        resultado: res.valorCalculado,
        fechaCalculo: new Date().toISOString()
      };
    }
  }

  return { respuestasActualizadas: respuestasNuevas, calculosJson: calculosMap };
}
