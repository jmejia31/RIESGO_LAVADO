import {
  ESTADOS_MATRIZ_VISIBLES,
  esTransicionMatrizPermitida,
  etiquetaEstadoMatriz,
  normalizarEstadoMatriz,
  puedeEditarMatriz,
  puedeEliminarMatriz,
  puedeGestionarTratamientoMatriz,
  transicionesPermitidasMatriz
} from './matrices-riesgos-estados.policy';

describe('Política operativa de estados de Matrices de Riesgos', () => {
  it('expone únicamente los cuatro estados funcionales aprobados', () => {
    expect(ESTADOS_MATRIZ_VISIBLES).toEqual([
      'EN_REVISION',
      'APROBADA',
      'CERRADA',
      'INACTIVA'
    ]);
  });

  it('normaliza estados técnicos e históricos a En Revisión', () => {
    expect(normalizarEstadoMatriz('CALCULADA')).toBe('EN_REVISION');
    expect(normalizarEstadoMatriz('BORRADOR')).toBe('EN_REVISION');
    expect(normalizarEstadoMatriz('EN_EVALUACION')).toBe('EN_REVISION');
    expect(normalizarEstadoMatriz('OBSERVADA')).toBe('EN_REVISION');
  });

  it('conserva los estados funcionales aprobados', () => {
    expect(normalizarEstadoMatriz('APROBADA')).toBe('APROBADA');
    expect(normalizarEstadoMatriz('CERRADA')).toBe('CERRADA');
    expect(normalizarEstadoMatriz('INACTIVA')).toBe('INACTIVA');
  });

  it('presenta etiquetas institucionales legibles', () => {
    expect(etiquetaEstadoMatriz('CALCULADA')).toBe('En Revisión');
    expect(etiquetaEstadoMatriz('APROBADA')).toBe('Aprobada');
    expect(etiquetaEstadoMatriz('CERRADA')).toBe('Cerrada');
    expect(etiquetaEstadoMatriz('INACTIVA')).toBe('Inactiva');
  });

  it('restringe transiciones según el ciclo operativo', () => {
    expect(transicionesPermitidasMatriz('EN_REVISION')).toEqual(['APROBADA', 'INACTIVA']);
    expect(transicionesPermitidasMatriz('APROBADA')).toEqual(['CERRADA', 'INACTIVA']);
    expect(transicionesPermitidasMatriz('CERRADA')).toEqual([]);
    expect(transicionesPermitidasMatriz('INACTIVA')).toEqual(['EN_REVISION']);
  });

  it('impide saltos de estado no autorizados', () => {
    expect(esTransicionMatrizPermitida('EN_REVISION', 'CERRADA')).toBe(false);
    expect(esTransicionMatrizPermitida('APROBADA', 'EN_REVISION')).toBe(false);
    expect(esTransicionMatrizPermitida('CERRADA', 'INACTIVA')).toBe(false);
  });

  it('permite edición y eliminación únicamente en En Revisión', () => {
    expect(puedeEditarMatriz('CALCULADA')).toBe(true);
    expect(puedeEditarMatriz('APROBADA')).toBe(false);
    expect(puedeEliminarMatriz('EN_REVISION')).toBe(true);
    expect(puedeEliminarMatriz('CERRADA')).toBe(false);
  });

  it('permite tratamiento en revisión o aprobada, pero no en cerrada o inactiva', () => {
    expect(puedeGestionarTratamientoMatriz('EN_REVISION')).toBe(true);
    expect(puedeGestionarTratamientoMatriz('APROBADA')).toBe(true);
    expect(puedeGestionarTratamientoMatriz('CERRADA')).toBe(false);
    expect(puedeGestionarTratamientoMatriz('INACTIVA')).toBe(false);
  });
});
