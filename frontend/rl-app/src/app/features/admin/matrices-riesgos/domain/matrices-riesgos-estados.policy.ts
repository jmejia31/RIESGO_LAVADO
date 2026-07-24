export const ESTADOS_MATRIZ_VISIBLES = [
  'EN_REVISION',
  'APROBADA',
  'CERRADA',
  'INACTIVA'
] as const;

export type EstadoMatrizVisible = typeof ESTADOS_MATRIZ_VISIBLES[number];

const ESTADOS_TECNICOS_A_OPERATIVOS: Readonly<Record<string, EstadoMatrizVisible>> = {
  BORRADOR: 'EN_REVISION',
  EN_EVALUACION: 'EN_REVISION',
  CALCULADA: 'EN_REVISION',
  OBSERVADA: 'EN_REVISION',
  EN_REVISION: 'EN_REVISION',
  APROBADA: 'APROBADA',
  CERRADA: 'CERRADA',
  INACTIVA: 'INACTIVA'
};

const TRANSICIONES_PERMITIDAS: Readonly<Record<EstadoMatrizVisible, readonly EstadoMatrizVisible[]>> = {
  EN_REVISION: ['APROBADA', 'INACTIVA'],
  APROBADA: ['CERRADA', 'INACTIVA'],
  CERRADA: [],
  INACTIVA: ['EN_REVISION']
};

const ETIQUETAS_ESTADO: Readonly<Record<EstadoMatrizVisible, string>> = {
  EN_REVISION: 'En Revisión',
  APROBADA: 'Aprobada',
  CERRADA: 'Cerrada',
  INACTIVA: 'Inactiva'
};

/**
 * Normaliza estados históricos o técnicos a los cuatro estados funcionales
 * aprobados para la operación diaria del módulo Matrices de Riesgos.
 */
export function normalizarEstadoMatriz(estado?: string | null): EstadoMatrizVisible {
  const normalizado = `${estado ?? ''}`.trim().toUpperCase();
  return ESTADOS_TECNICOS_A_OPERATIVOS[normalizado] ?? 'EN_REVISION';
}

export function etiquetaEstadoMatriz(estado?: string | null): string {
  return ETIQUETAS_ESTADO[normalizarEstadoMatriz(estado)];
}

export function transicionesPermitidasMatriz(estadoActual?: string | null): readonly EstadoMatrizVisible[] {
  return TRANSICIONES_PERMITIDAS[normalizarEstadoMatriz(estadoActual)];
}

export function puedeEditarMatriz(estadoActual?: string | null): boolean {
  return normalizarEstadoMatriz(estadoActual) === 'EN_REVISION';
}

export function puedeEliminarMatriz(estadoActual?: string | null): boolean {
  return normalizarEstadoMatriz(estadoActual) === 'EN_REVISION';
}

export function puedeGestionarTratamientoMatriz(estadoActual?: string | null): boolean {
  const estado = normalizarEstadoMatriz(estadoActual);
  return estado === 'EN_REVISION' || estado === 'APROBADA';
}

export function esTransicionMatrizPermitida(estadoActual: string | null | undefined, estadoNuevo: string | null | undefined): boolean {
  const destino = normalizarEstadoMatriz(estadoNuevo);
  return transicionesPermitidasMatriz(estadoActual).includes(destino);
}
