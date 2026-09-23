const REEMPLAZOS_MOJIBAKE: ReadonlyArray<readonly [string, string]> = [
  ['\u00C3\u00A1', 'á'],
  ['\u00C3\u00A9', 'é'],
  ['\u00C3\u00AD', 'í'],
  ['\u00C3\u00B3', 'ó'],
  ['\u00C3\u00BA', 'ú'],
  ['\u00C3\u00B1', 'ñ'],
  ['\u00C3\u0081', 'Á'],
  ['\u00C3\u0089', 'É'],
  ['\u00C3\u008D', 'Í'],
  ['\u00C3\u0093', 'Ó'],
  ['\u00C3\u009A', 'Ú'],
  ['\u00C3\u0091', 'Ñ'],
  ['\u00C2\u00BF', '¿'],
  ['\u00C2\u00A1', '¡'],
  ['\u00C2\u00B0', '°'],
  ['\u00C2\u00BA', 'º'],
  ['\u00C2\u00AA', 'ª'],
  ['\u00E2\u20AC\u201C', '–'],
  ['\u00E2\u20AC\u201D', '—'],
  ['\u00E2\u20AC\u0153', '“'],
  ['\u00E2\u20AC\u009D', '”'],
  ['\u00E2\u20AC\u02DC', '‘'],
  ['\u00E2\u20AC\u2122', '’']
];

const TOKEN_REEMPLAZO = '(?:\\u00EF\\u00BF\\u00BD|\\uFFFD)';

const REPARACIONES_CONTEXTO: ReadonlyArray<readonly [RegExp, string]> = [
  [new RegExp('Identificaci' + TOKEN_REEMPLAZO + 'n', 'gi'), 'Identificación'],
  [new RegExp(TOKEN_REEMPLAZO + 'rea\\b', 'gi'), 'Área'],
  [new RegExp('Due' + TOKEN_REEMPLAZO + 'o', 'gi'), 'Dueño'],
  [new RegExp('estrat' + TOKEN_REEMPLAZO + 'gic', 'gi'), 'estratégic'],
  [new RegExp('R' + TOKEN_REEMPLAZO + 'gimen', 'gi'), 'Régimen'],
  [new RegExp('interrelaci' + TOKEN_REEMPLAZO + 'n', 'gi'), 'interrelación'],
  [new RegExp('Valoraci' + TOKEN_REEMPLAZO + 'n', 'gi'), 'Valoración'],
  [new RegExp('Evaluaci' + TOKEN_REEMPLAZO + 'n', 'gi'), 'Evaluación'],
  [new RegExp('Configuraci' + TOKEN_REEMPLAZO + 'n', 'gi'), 'Configuración'],
  [new RegExp('Definici' + TOKEN_REEMPLAZO + 'n', 'gi'), 'Definición'],
  [new RegExp('Selecci' + TOKEN_REEMPLAZO + 'n', 'gi'), 'Selección'],
  [new RegExp('secci' + TOKEN_REEMPLAZO + 'n', 'gi'), 'sección'],
  [new RegExp('Catastr' + TOKEN_REEMPLAZO + 'fico', 'gi'), 'Catastrófico'],
  [new RegExp('Cr' + TOKEN_REEMPLAZO + 'tico', 'gi'), 'Crítico'],
  [new RegExp('M' + TOKEN_REEMPLAZO + 'ltiple', 'gi'), 'Múltiple']
];

const REPARACIONES_ORTOGRAFICAS: ReadonlyArray<readonly [RegExp, string]> = [
  [/\bIdentificacion\b/gi, 'Identificación'],
  [/\bEvaluacion\b/gi, 'Evaluación'],
  [/\bConfiguracion\b/gi, 'Configuración'],
  [/\bDefinicion\b/gi, 'Definición'],
  [/\bSeleccion\b/gi, 'Selección'],
  [/\bVersion\b/gi, 'Versión'],
  [/\bArea\b/gi, 'Área']
];

export function contieneMojibakeVisible(valor: string): boolean {
  return valor.includes('\uFFFD')
    || valor.includes('\u00EF\u00BF\u00BD')
    || valor.includes('\u00C3')
    || valor.includes('\u00C2')
    || valor.includes('\u00E2\u20AC')
    || valor.includes('\u00F0\u0178');
}

export function normalizarTextoVisibleUtf8(valor: string): string {
  if (!valor) return valor;

  let resultado = valor;

  for (const [origen, destino] of REEMPLAZOS_MOJIBAKE) {
    resultado = resultado.split(origen).join(destino);
  }

  for (const [patron, reemplazo] of REPARACIONES_CONTEXTO) {
    resultado = resultado.replace(patron, reemplazo);
  }

  for (const [patron, reemplazo] of REPARACIONES_ORTOGRAFICAS) {
    resultado = resultado.replace(patron, reemplazo);
  }

  return resultado;
}
