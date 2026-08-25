/**
 * Utilidad de comparación semántica y canonicalización para definiciones JSON
 * de plantillas y versiones de matrices de riesgo.
 *
 * Reglas rectoras:
 * 1. Objetos: el orden de propiedades no altera la semántica (claves ordenadas recursivamente).
 * 2. Arrays: el orden de los elementos es contractual y se preserva estrictamente.
 * 3. Tipos primitivos: comparación estricta (0 !== null !== false !== "0").
 * 4. Strings: preservación exacta (ej. "001", "G-IVM").
 */

export function canonicalizarValor(valor: unknown): unknown {
  if (valor === null || valor === undefined) {
    return valor;
  }

  if (typeof valor !== 'object') {
    return valor;
  }

  if (Array.isArray(valor)) {
    return valor.map(item => canonicalizarValor(item));
  }

  const obj = valor as Record<string, unknown>;
  const clavesOrdenadas = Object.keys(obj).sort((a, b) => a.localeCompare(b));
  const resultado: Record<string, unknown> = {};

  for (const clave of clavesOrdenadas) {
    resultado[clave] = canonicalizarValor(obj[clave]);
  }

  return resultado;
}

export function sonJsonSemanticamenteEquivalentes(
  jsonA: string | unknown,
  jsonB: string | unknown
): boolean {
  if (jsonA === jsonB) {
    return true;
  }

  let objA: unknown;
  let objB: unknown;

  try {
    objA = typeof jsonA === 'string' ? JSON.parse(jsonA) : jsonA;
  } catch {
    return false;
  }

  try {
    objB = typeof jsonB === 'string' ? JSON.parse(jsonB) : jsonB;
  } catch {
    return false;
  }

  const canonA = canonicalizarValor(objA);
  const canonB = canonicalizarValor(objB);

  return JSON.stringify(canonA) === JSON.stringify(canonB);
}
