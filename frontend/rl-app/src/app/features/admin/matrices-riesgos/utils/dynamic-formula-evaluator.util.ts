import { CampoFormulario, RespuestasFormulario } from '../models/matrices-riesgos.models';

export interface ResultadoEvaluacionFormula {
  valorCalculado: number | null;
  exito: boolean;
  error?: string;
}

/**
 * Evaluador matematico seguro Shunting-Yard (sin eval ni new Function).
 * Soporta +, -, *, /, parentesis, constantes y variables numericas.
 */
function tokenizarExpresion(expresion: string): string[] {
  const tokens: string[] = [];
  for (let i = 0; i < expresion.length;) {
    const ch = expresion[i];
    if (/\s/.test(ch)) { i++; continue; }
    if (/[0-9.]/.test(ch)) {
      let numero = '';
      while (i < expresion.length && /[0-9.]/.test(expresion[i])) numero += expresion[i++];
      tokens.push(numero);
      continue;
    }
    if (['+', '-', '*', '/', '(', ')'].includes(ch)) { tokens.push(ch); i++; continue; }
    throw new Error(`Caracter no permitido en expresion matematica: '${ch}'`);
  }
  return tokens;
}

function convertirARpn(tokens: string[]): string[] {
  const salida: string[] = [];
  const operadores: string[] = [];
  const precedencia: Record<string, number> = { '+': 1, '-': 1, '*': 2, '/': 2 };
  for (const token of tokens) {
    if (!Number.isNaN(Number(token))) salida.push(token);
    else if (['+', '-', '*', '/'].includes(token)) {
      while (operadores.at(-1) !== undefined && operadores.at(-1) !== '(' && precedencia[operadores.at(-1)!] >= precedencia[token]) salida.push(operadores.pop()!);
      operadores.push(token);
    } else if (token === '(') operadores.push(token);
    else {
      while (operadores.at(-1) !== undefined && operadores.at(-1) !== '(') salida.push(operadores.pop()!);
      if (operadores.pop() !== '(') throw new Error('Parentesis desbalanceados en la formula.');
    }
  }
  while (operadores.length) {
    const op = operadores.pop()!;
    if (op === '(' || op === ')') throw new Error('Parentesis desbalanceados en la formula.');
    salida.push(op);
  }
  return salida;
}

function evaluarRpn(rpn: string[]): number {
  const pila: number[] = [];
  for (const token of rpn) {
    if (!Number.isNaN(Number(token))) { pila.push(Number(token)); continue; }
    if (pila.length < 2) throw new Error('Expresion matematica incompleta.');
    const b = pila.pop()!;
    const a = pila.pop()!;
    if (token === '/' && b === 0) throw new Error('Division por cero en el calculo.');
    pila.push(token === '+' ? a + b : token === '-' ? a - b : token === '*' ? a * b : a / b);
  }
  if (pila.length !== 1) throw new Error('Error al evaluar el resultado de la formula.');
  return pila[0];
}

function evaluarExpresionMatematicaSegura(expresion: string): number {
  return evaluarRpn(convertirARpn(tokenizarExpresion(expresion)));
}

/**
 * Extrae las variables dependientes (claves de campos) presentes en la formula.
 */
export function obtenerDependenciasDeFormula(formula: string): string[] {
  if (!formula || formula.trim() === '') return [];
  const regexClaves = /[A-Za-z_]\w*/g;
  const coincidencias = formula.match(regexClaves) || [];
  const reservadas = new Set(['VRI', 'VRR', 'MATH', 'ABS', 'MIN', 'MAX']);
  return Array.from(new Set(coincidencias.filter(c => !reservadas.has(c.toUpperCase()))));
}

/**
 * Detecta si existe un ciclo de dependencias directas o indirectas (ej. A -> B -> A).
 */
export function detectarCicloEnFormulas(
  claveCampoActual: string,
  camposMap: Map<string, CampoFormulario>,
  visitados: Set<string> = new Set()
): boolean {
  const claveLower = claveCampoActual.toLowerCase();
  if (visitados.has(claveLower)) {
    return true; // Ciclo encontrado
  }

  visitados.add(claveLower);
  const campo = camposMap.get(claveLower);
  if (!campo?.formula) return false;

  const dependencias = obtenerDependenciasDeFormula(campo.formula);
  return dependencias.some(dep => detectarCicloEnFormulas(dep, camposMap, new Set(visitados)));
}

export function evaluarFormulaCampo(
  formula: string | undefined | null,
  respuestas: RespuestasFormulario,
  claveCampoActual?: string,
  camposMap: Map<string, CampoFormulario> = new Map()
): ResultadoEvaluacionFormula {
  if (!formula || formula.trim() === '') {
    return { valorCalculado: null, exito: true };
  }

  // Deteccion previa de referencias circulares directas e indirectas
  if (claveCampoActual && camposMap.size > 0) {
    if (detectarCicloEnFormulas(claveCampoActual, camposMap)) {
      return {
        valorCalculado: null,
        exito: false,
        error: `Referencia circular detectada en el campo '${claveCampoActual}'.`
      };
    }
  }

  const exprLimpia = formula.trim();

  // Validar si la formula contiene referencias a campos inexistentes en el formulario
  if (camposMap.size > 0) {
    const dependencias = obtenerDependenciasDeFormula(exprLimpia);
    for (const dep of dependencias) {
      if (!camposMap.has(dep.toLowerCase())) {
        return {
          valorCalculado: null,
          exito: false,
          error: `Referencia a campo inexistente '${dep}' en la formula.`
        };
      }
    }
  }

  // Caso especial predeterminado VRI / VRR
  if (exprLimpia.toUpperCase() === 'VRI' || exprLimpia.toUpperCase() === 'VRR' || exprLimpia.toUpperCase() === 'VRI/VRR') {
    const prob = Number(respuestas['probabilidad'] ?? respuestas['PROBABILIDAD'] ?? 0);
    const imp = Number(respuestas['impacto'] ?? respuestas['IMPACTO'] ?? 0);

    if (prob > 0 && imp > 0) {
      return { valorCalculado: prob * imp, exito: true };
    }
  }

  try {
    let expresionSustituida = exprLimpia;
    const regexClaves = /[A-Za-z_]\w*/g;

    expresionSustituida = expresionSustituida.replace(regexClaves, (match) => {
      const val = respuestas[match] ?? respuestas[match.toLowerCase()] ?? respuestas[match.toUpperCase()];
      if (val === null || val === undefined || val === '') {
        return '0';
      }

      const num = Number(val);
      return Number.isNaN(num) ? '0' : num.toString();
    });

    const resultadoNum = evaluarExpresionMatematicaSegura(expresionSustituida);
    const valorRedondeado = Math.round(resultadoNum * 100) / 100;

    return { valorCalculado: valorRedondeado, exito: true };
  } catch (err) {
    return {
      valorCalculado: null,
      exito: false,
      error: err instanceof Error ? err.message : 'Error en calculo'
    };
  }
}

export function recalcularFormulasEvaluacion(
  campos: CampoFormulario[],
  respuestas: RespuestasFormulario
): { respuestasActualizadas: RespuestasFormulario; calculosJson: Record<string, any> } {
  const respuestasNuevas = { ...respuestas };
  const calculosMap: Record<string, any> = {};

  const camposMap = new Map<string, CampoFormulario>();
  campos.forEach(c => camposMap.set(c.clave.toLowerCase(), c));

  const camposFormula = campos.filter(c => c.tipo === 'formula' || (c.formula && c.formula.trim() !== ''));
  if (camposFormula.length === 0) {
    return { respuestasActualizadas: respuestasNuevas, calculosJson: calculosMap };
  }

  // Resolver en pasadas iterativas con limite maximo
  let huboCambios = true;
  let iteracion = 0;
  const maxIteraciones = camposFormula.length * 2;

  while (huboCambios && iteracion < maxIteraciones) {
    huboCambios = false;
    iteracion++;

    for (const campo of camposFormula) {
      const res = evaluarFormulaCampo(campo.formula, respuestasNuevas, campo.clave, camposMap);
      if (res.exito && res.valorCalculado !== null) {
        if (respuestasNuevas[campo.clave] !== res.valorCalculado) {
          respuestasNuevas[campo.clave] = res.valorCalculado;
          huboCambios = true;
        }
        // Registrar siempre la traza incondicional de calculo exitoso en calculosMap
        calculosMap[campo.clave] = {
          formula: campo.formula,
          resultado: res.valorCalculado,
          fechaCalculo: new Date().toISOString()
        };
      } else if (!res.exito) {
        calculosMap[campo.clave] = {
          formula: campo.formula,
          error: res.error,
          fechaCalculo: new Date().toISOString()
        };
      }
    }
  }

  return { respuestasActualizadas: respuestasNuevas, calculosJson: calculosMap };
}
