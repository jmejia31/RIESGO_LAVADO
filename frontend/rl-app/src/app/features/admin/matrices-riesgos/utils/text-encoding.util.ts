const REEMPLAZOS_MOJIBAKE: ReadonlyArray<readonly [string, string]> = [
  ['\u00C3\u00A1', '\u00E1'], ['\u00C3\u00A9', '\u00E9'], ['\u00C3\u00AD', '\u00ED'],
  ['\u00C3\u00B3', '\u00F3'], ['\u00C3\u00BA', '\u00FA'], ['\u00C3\u00B1', '\u00F1'],
  ['\u00C3\u0081', '\u00C1'], ['\u00C3\u0089', '\u00C9'], ['\u00C3\u008D', '\u00CD'],
  ['\u00C3\u0093', '\u00D3'], ['\u00C3\u009A', '\u00DA'], ['\u00C3\u0091', '\u00D1'],
  ['\u00C2\u00BF', '\u00BF'], ['\u00C2\u00A1', '\u00A1'], ['\u00C2\u00B0', '\u00B0'],
  ['\u00C2\u00BA', '\u00BA'], ['\u00C2\u00AA', '\u00AA']
];

const REPARACIONES_CONTEXTO: ReadonlyArray<readonly [string, string]> = [
  ['Identificaci\u00BFn', 'Identificaci\u00F3n'], ['Evaluaci\u00BFn', 'Evaluaci\u00F3n'],
  ['Configuraci\u00BFn', 'Configuraci\u00F3n'], ['Definici\u00BFn', 'Definici\u00F3n'],
  ['\u00BFrea', '\u00C1rea'], ['estrat\u00BFgic', 'estrat\u00E9gic'],
  ['R\u00BFgimen', 'R\u00E9gimen'],
  ['Cr\u00BFtico', 'Cr\u00EDtico'], ['Catastr\u00BFfico', 'Catastr\u00F3fico'],
  ['interrelaci\u00BFn', 'interrelaci\u00F3n'],
  ['informaci\u00BFn', 'informaci\u00F3n'], ['evaluaci\u00BFn', 'evaluaci\u00F3n'],
  ['gesti\u00BFn', 'gesti\u00F3n'], ['aprobaci\u00BFn', 'aprobaci\u00F3n'],
  ['adjudicaci\u00BFn', 'adjudicaci\u00F3n'], ['contrataci\u00BFn', 'contrataci\u00F3n'],
  ['licitaci\u00BFn', 'licitaci\u00F3n'], ['pensi\u00BFn', 'pensi\u00F3n'],
  ['prestaci\u00BFn', 'prestaci\u00F3n'], ['definici\u00BFn', 'definici\u00F3n'],
  ['ejecuci\u00BFn', 'ejecuci\u00F3n'], ['supervisi\u00BFn', 'supervisi\u00F3n'],
  ['prevenci\u00BFn', 'prevenci\u00F3n'], ['Due\u00BFo', 'Due\u00F1o'], ['due\u00BFo', 'due\u00F1o'],
  ['v\u00BFnculo', 'v\u00EDnculo'], ['t\u00BFrmin', 't\u00E9rmin'], ['t\u00BFcnica', 't\u00E9cnica'],
  ['p\u00BAblica', 'p\u00FAblica'], ['m\u00BFs', 'm\u00E1s'],
  ['Descripci\u00BFn', 'Descripción'], ['descripci\u00BFn', 'descripción'],
  ['vinculaci\u00BFn', 'vinculación'], ['P\u00BFrdidas', 'Pérdidas'],
  ['p\u00BFrdidas', 'pérdidas'], ['econ\u00BFmicas', 'económicas'],
  ['verificaci\u00BFn', 'verificación'], ['validaci\u00BFn', 'validación'],
  ['instituci\u00BFn', 'institución'], ['autom\u00BFticos', 'automáticos'],
  ['il\u00BFcitas', 'ilícitas'], ['capacitaci\u00BFn', 'capacitación'],
  ['documentaci\u00BFn', 'documentación'], ['organizaci\u00BFn', 'organización'],
  ['operaci\u00BFn', 'operación'], ['protecci\u00BFn', 'protección'],
  ['situaci\u00BFn', 'situación'], ['funci\u00BFn', 'función'],
  ['administraci\u00BFn', 'administración'], ['identificaci\u00BFn', 'identificación'],
  ['calificaci\u00BFn', 'calificación'], ['relaci\u00BFn', 'relación']
];

const SIGNO_PREGUNTA_INCRUSTADO = /[\p{L}]\u00BF(?=[\p{L}])/u;

export function contieneMojibakeVisible(valor: string): boolean {
  return valor.includes('\uFFFD') || valor.includes('\u00EF\u00BF\u00BD')
    || valor.includes('\u00C3') || valor.includes('\u00C2') || valor.includes('\u00E2\u20AC')
    || valor.includes('\u00F0\u0178') || SIGNO_PREGUNTA_INCRUSTADO.test(valor);
}

export function normalizarMojibakeVisibleUtf8(valor: string): string {
  if (!valor) return valor;
  let resultado = valor;
  for (const [origen, destino] of REEMPLAZOS_MOJIBAKE) resultado = resultado.split(origen).join(destino);
  resultado = resultado.replace(/([\p{L}])\uFFFD(?=[\p{L}])/gu, '$1\u00BF');
  resultado = resultado.split('\u00EF\u00BF\uFFFD').join('\u00BF');
  resultado = resultado.replace(/\u00EF\u00BF\u00BD/g, '\u00BF')
    .replace(/\u00EF\u00BFo/g, '\u00F1o').replace(/\uFFFDo/g, '\u00F1o');
  for (const [origen, destino] of REPARACIONES_CONTEXTO) resultado = resultado.split(origen).join(destino);
  return resultado;
}

export function normalizarTextoVisibleUtf8(valor: string): string {
  return normalizarMojibakeVisibleUtf8(valor);
}
