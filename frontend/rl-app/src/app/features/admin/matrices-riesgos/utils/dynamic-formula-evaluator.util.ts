import { CampoFormulario, RespuestasFormulario } from '../models/matrices-riesgos.models';

export type FormulaErrorCode = 'FORMULA_SYNTAX_INVALID' | 'FORMULA_OPERATOR_UNSUPPORTED' | 'FORMULA_FUNCTION_UNSUPPORTED' | 'FORMULA_REFERENCE_UNKNOWN' | 'FORMULA_SELF_REFERENCE' | 'FORMULA_CYCLE' | 'FORMULA_TYPE_MISMATCH' | 'FORMULA_DIVISION_BY_ZERO' | 'FORMULA_ARGUMENT_INVALID' | 'FORMULA_LIMIT_EXCEEDED';
export interface ResultadoEvaluacionFormula { valorCalculado: number | string | boolean | null; exito: boolean; codigo?: FormulaErrorCode; error?: string; }
type Value = number | string | boolean;
type Node = { kind: 'literal'; value: Value } | { kind: 'ref'; name: string } | { kind: 'unary'; op: string; operand: Node } | { kind: 'binary'; op: string; left: Node; right: Node } | { kind: 'call'; name: string; args: Node[] };
class FormulaError extends Error { constructor(public readonly code: FormulaErrorCode, message: string) { super(message); } }
const FUNCTIONS = new Set(['IF', 'IFERROR', 'ROUND', 'ROUNDDOWN', 'MAX', 'MOD', 'OR', 'AND']);

/** Alias explícito del contrato Excel institucional al payload persistible. */
export const INSTITUTIONAL_FORMULA_REFERENCE_ALIASES: Readonly<Record<string, string>> = Object.freeze({
  frecuencia: 'frecuencia_inherente',
  impacto: 'impacto_inherente'
});
const INSTITUTIONAL_CONTEXT_KEYS = new Set(['control_preventivo', 'control_detectivo', 'control_correctivo']);

function tokenize(source: string): Token[] {
  if (source.length > 4096) throw new FormulaError('FORMULA_LIMIT_EXCEEDED', 'Longitud de fórmula fuera de límites.');
  const out: Token[] = [];
  let i = source.startsWith('=') ? 1 : 0;
  while (i < source.length) {
    const c = source[i];
    if (/\s/.test(c)) { i++; continue; }
    if (/[0-9.]/.test(c)) {
      const start = i++;
      while (i < source.length && /[0-9.]/.test(source[i])) i++;
      const text = source.slice(start, i);
      if (Number.isNaN(Number(text))) throw new FormulaError('FORMULA_SYNTAX_INVALID', 'Número inválido.');
      out.push({ kind: 'number', text });
      continue;
    }
    if (/[A-Za-z_]/.test(c)) {
      const start = i++;
      while (i < source.length && /[A-Za-z0-9_]/.test(source[i])) i++;
      out.push({ kind: 'identifier', text: source.slice(start, i) });
      continue;
    }
    if (c === '"') {
      const start = ++i;
      while (i < source.length && source[i] !== '"') i++;
      if (i >= source.length) throw new FormulaError('FORMULA_SYNTAX_INVALID', 'Cadena sin cierre.');
      out.push({ kind: 'string', text: source.slice(start, i++) });
      continue;
    }
    const two = source.slice(i, i + 2);
    const op = ['<>', '<=', '>='].includes(two) ? two : c;
    i += op.length;
    if (!'+-*/^=(),<>'.includes(c)) throw new FormulaError('FORMULA_OPERATOR_UNSUPPORTED', `Operador '${c}' no soportado.`);
    out.push({ kind: 'operator', text: op });
  }
  if (out.length > 512) throw new FormulaError('FORMULA_LIMIT_EXCEEDED', 'Cantidad de tokens excedida.');
  out.push({ kind: 'end', text: '' });
  return out;
}

type Token = { kind: 'number' | 'identifier' | 'string' | 'operator' | 'end'; text: string };

class Parser {
  private pos = 0;
  private depth = 0;
  constructor(private readonly tokens: Token[]) { }
  parse(): Node {
    const node = this.comparison();
    if (this.current.kind !== 'end') throw new FormulaError('FORMULA_SYNTAX_INVALID', 'Token inesperado.');
    return node;
  }
  private comparison(): Node {
    let node = this.additive();
    while (['=', '<>', '<', '<=', '>', '>='].includes(this.current.text)) node = { kind: 'binary', op: this.next().text, left: node, right: this.additive() };
    return node;
  }
  private additive(): Node {
    let node = this.multiplicative();
    while (['+', '-'].includes(this.current.text)) node = { kind: 'binary', op: this.next().text, left: node, right: this.multiplicative() };
    return node;
  }
  private multiplicative(): Node {
    let node = this.power();
    while (['*', '/'].includes(this.current.text)) node = { kind: 'binary', op: this.next().text, left: node, right: this.power() };
    return node;
  }
  private power(): Node {
    const left = this.unary();
    return this.current.text === '^' ? { kind: 'binary', op: this.next().text, left, right: this.power() } : left;
  }
  private unary(): Node { return ['+', '-'].includes(this.current.text) ? { kind: 'unary', op: this.next().text, operand: this.unary() } : this.primary(); }
  private primary(): Node {
    if (++this.depth > 64) throw new FormulaError('FORMULA_LIMIT_EXCEEDED', 'Profundidad AST excedida.');
    try {
      if (this.current.kind === 'number') return { kind: 'literal', value: Number(this.next().text) };
      if (this.current.kind === 'string') return { kind: 'literal', value: this.next().text };
      if (this.current.kind === 'identifier') {
        const name = this.next().text;
        if (this.current.text !== '(') return { kind: 'ref', name };
        this.next();
        const args: Node[] = [];
        if (String(this.current.text) !== ')') {
          do { args.push(this.comparison()); } while (String(this.current.text) === ',' && (this.next(), true));
        }
        this.expect(')');
        if (!FUNCTIONS.has(name.toUpperCase())) throw new FormulaError('FORMULA_FUNCTION_UNSUPPORTED', `Función '${name}' no soportada.`);
        return { kind: 'call', name: name.toUpperCase(), args };
      }
      if (this.current.text === '(') { this.next(); const node = this.comparison(); this.expect(')'); return node; }
      throw new FormulaError('FORMULA_SYNTAX_INVALID', 'Se esperaba literal, referencia o paréntesis.');
    } finally { this.depth--; }
  }
  private get current(): Token { return this.tokens[this.pos]; }
  private next(): Token { return this.tokens[this.pos++]; }
  private expect(text: string): void { if (this.current.text !== text) throw new FormulaError('FORMULA_SYNTAX_INVALID', 'Paréntesis o separador inválido.'); this.next(); }
}

function refs(node: Node): string[] {
  switch (node.kind) {
    case 'ref': return [node.name];
    case 'unary': return refs(node.operand);
    case 'binary': return [...refs(node.left), ...refs(node.right)];
    case 'call': return node.args.flatMap(refs);
    default: return [];
  }
}

function knownName(reference: string, names: Iterable<string>): string {
  const all = [...names];
  const direct = all.find(name => name.toLowerCase() === reference.trim().toLowerCase());
  if (direct) return direct.toLowerCase();
  if (reference.trim().toLowerCase() === 'c1') {
    const c = all.find(name => name.toLowerCase() === 'c');
    if (c) return c.toLowerCase();
  }
  const canonical = INSTITUTIONAL_FORMULA_REFERENCE_ALIASES[reference.trim().toLowerCase()];
  if (canonical && all.some(name => name.toLowerCase() === canonical.toLowerCase())) return canonical.toLowerCase();
  return reference.trim().toLowerCase();
}

function asNumber(value: Value): number { const n = typeof value === 'number' ? value : Number(value); if (!Number.isFinite(n)) throw new FormulaError('FORMULA_TYPE_MISMATCH', 'Se esperaba un número.'); return n; }
function asBoolean(value: Value): boolean { return typeof value === 'boolean' ? value : typeof value === 'number' ? value !== 0 : value.length > 0; }

function evalNode(node: Node, values: Map<string, Value>, formulas: Map<string, Node>, active: Set<string>): Value {
  if (node.kind === 'literal') return node.value;
  if (node.kind === 'ref') {
    const key = knownName(node.name, [...formulas.keys(), ...values.keys()]);
    if (formulas.has(key)) {
      if (active.has(key)) throw new FormulaError('FORMULA_CYCLE', 'Dependencia circular detectada.');
      active.add(key);
      const value = evalNode(formulas.get(key)!, values, formulas, active);
      active.delete(key);
      values.set(key, value);
      return value;
    }
    if (values.has(key)) return values.get(key)!;
    throw new FormulaError('FORMULA_REFERENCE_UNKNOWN', `Referencia desconocida '${node.name}'.`);
  }
  if (node.kind === 'unary') {
    const n = asNumber(evalNode(node.operand, values, formulas, active));
    return node.op === '-' ? -n : n;
  }
  if (node.kind === 'binary') {
    const left = evalNode(node.left, values, formulas, active);
    const right = evalNode(node.right, values, formulas, active);
    if (['=', '<>', '<', '<=', '>', '>='].includes(node.op)) {
      const comparison = (typeof left === 'number' || typeof right === 'number') ? asNumber(left) - asNumber(right) : String(left).localeCompare(String(right));
      return node.op === '=' ? comparison === 0 : node.op === '<>' ? comparison !== 0 : node.op === '<' ? comparison < 0 : node.op === '<=' ? comparison <= 0 : node.op === '>' ? comparison > 0 : comparison >= 0;
    }
    const a = asNumber(left); const b = asNumber(right);
    if (node.op === '/' && b === 0) throw new FormulaError('FORMULA_DIVISION_BY_ZERO', 'División por cero.');
    return ({ '+': a + b, '-': a - b, '*': a * b, '/': a / b, '^': a ** b } as Record<string, number>)[node.op];
  }
  if (node.name === 'IF') {
    if (node.args.length !== 3) throw new FormulaError('FORMULA_ARGUMENT_INVALID', 'IF requiere 3 argumentos.');
    return evalNode(asBoolean(evalNode(node.args[0], values, formulas, active)) ? node.args[1] : node.args[2], values, formulas, active);
  }
  if (node.name === 'IFERROR') {
    if (node.args.length !== 2) throw new FormulaError('FORMULA_ARGUMENT_INVALID', 'IFERROR requiere 2 argumentos.');
    try { return evalNode(node.args[0], values, formulas, active); }
    catch (error) {
      if (error instanceof FormulaError && ['FORMULA_DIVISION_BY_ZERO', 'FORMULA_TYPE_MISMATCH', 'FORMULA_ARGUMENT_INVALID'].includes(error.code)) return evalNode(node.args[1], values, formulas, active);
      throw error;
    }
  }
  const args = node.args.map(arg => evalNode(arg, values, formulas, active));
  if (node.name === 'OR') return args.some(asBoolean);
  if (node.name === 'AND') return args.every(asBoolean);
  if (node.name === 'MAX') return Math.max(...args.map(asNumber));
  if (node.name === 'ROUND' || node.name === 'ROUNDDOWN') {
    if (args.length !== 2) throw new FormulaError('FORMULA_ARGUMENT_INVALID', `${node.name} requiere 2 argumentos.`);
    const factor = 10 ** asNumber(args[1]);
    return node.name === 'ROUND' ? Math.sign(asNumber(args[0])) * Math.round(Math.abs(asNumber(args[0])) * factor) / factor : Math.trunc(asNumber(args[0]) * factor) / factor;
  }
  if (node.name === 'MOD') {
    const divisor = asNumber(args[1]);
    if (divisor === 0) throw new FormulaError('FORMULA_DIVISION_BY_ZERO', 'MOD no acepta divisor cero.');
    return asNumber(args[0]) - divisor * Math.floor(asNumber(args[0]) / divisor);
  }
  throw new FormulaError('FORMULA_FUNCTION_UNSUPPORTED', `Función '${node.name}' no soportada.`);
}

function parse(formula: string): Node { return new Parser(tokenize(formula.trim())).parse(); }

export function obtenerDependenciasDeFormula(formula: string): string[] { try { return [...new Set(refs(parse(formula)))]; } catch { return []; } }

export function detectarCicloEnFormulas(clave: string, campos: Map<string, CampoFormulario>, visitados = new Set<string>()): boolean {
  const key = knownName(clave, campos.keys());
  if (visitados.has(key)) return true;
  const field = campos.get(key);
  if (!field?.formula) return false;
  const next = new Set(visitados).add(key);
  return obtenerDependenciasDeFormula(field.formula)
    .map(dependency => knownName(dependency, campos.keys()))
    .some(dependency => detectarCicloEnFormulas(dependency, campos, next));
}

function valoresConContexto(respuestas: RespuestasFormulario): Map<string, Value> {
  const values = new Map(Object.entries(respuestas).map(([key, value]) => [key.toLowerCase(), value as Value]));
  for (const key of INSTITUTIONAL_CONTEXT_KEYS) if (!values.has(key)) values.set(key, '');
  return values;
}

export function evaluarFormulaCampo(formula: string | undefined | null, respuestas: RespuestasFormulario, claveCampoActual?: string, camposMap = new Map<string, CampoFormulario>): ResultadoEvaluacionFormula {
  if (!formula?.trim()) return { valorCalculado: null, exito: true };
  try {
    const ast = parse(formula);
    const dependencies = refs(ast);
    if (camposMap.size > 0) {
      if (claveCampoActual && detectarCicloEnFormulas(claveCampoActual, camposMap)) throw new FormulaError('FORMULA_CYCLE', `Referencia circular detectada en el campo '${claveCampoActual}'.`);
      const known = [...camposMap.keys(), ...INSTITUTIONAL_CONTEXT_KEYS];
      for (const dependency of dependencies) {
        const resolved = knownName(dependency, known);
        if (!camposMap.has(resolved) && !INSTITUTIONAL_CONTEXT_KEYS.has(resolved)) throw new FormulaError('FORMULA_REFERENCE_UNKNOWN', `Referencia a campo inexistente '${dependency}' en la formula.`);
      }
      if (claveCampoActual && dependencies.some(dependency => knownName(dependency, known) === claveCampoActual.toLowerCase())) throw new FormulaError('FORMULA_SELF_REFERENCE', 'La fórmula se referencia a sí misma.');
    }
    if (['VRI', 'VRR', 'VRI/VRR'].includes(formula.trim().toUpperCase())) {
      const probability = Number(respuestas['probabilidad'] ?? respuestas['PROBABILIDAD'] ?? 0);
      const impact = Number(respuestas['impacto'] ?? respuestas['IMPACTO'] ?? respuestas['impacto_inherente'] ?? 0);
      if (probability > 0 && impact > 0) return { valorCalculado: probability * impact, exito: true };
    }
    return { valorCalculado: evalNode(ast, valoresConContexto(respuestas), new Map(), new Set()), exito: true };
  } catch (error) {
    const e = error instanceof FormulaError ? error : new FormulaError('FORMULA_SYNTAX_INVALID', 'Fórmula inválida.');
    return { valorCalculado: null, exito: false, codigo: e.code, error: e.message };
  }
}

export function recalcularFormulasEvaluacion(campos: CampoFormulario[], respuestas: RespuestasFormulario): { respuestasActualizadas: RespuestasFormulario; calculosJson: Record<string, any> } {
  const updated = { ...respuestas };
  const formulas = new Map<string, Node>();
  for (const field of campos.filter(field => field.formula?.trim())) {
    try { formulas.set(field.clave.toLowerCase(), parse(field.formula!)); } catch { /* el feedback tipado se obtiene al evaluar */ }
  }
  const values = valoresConContexto(updated);
  const calc: Record<string, any> = {};
  for (const field of campos.filter(field => formulas.has(field.clave.toLowerCase()))) {
    try {
      const value = evalNode(formulas.get(field.clave.toLowerCase())!, values, formulas, new Set([field.clave.toLowerCase()]));
      updated[field.clave] = value;
      calc[field.clave] = { formula: field.formula, resultado: value };
    } catch (error) {
      const e = error instanceof FormulaError ? error : new FormulaError('FORMULA_SYNTAX_INVALID', 'Fórmula inválida.');
      calc[field.clave] = { formula: field.formula, error: e.code };
    }
  }
  return { respuestasActualizadas: updated, calculosJson: calc };
}
