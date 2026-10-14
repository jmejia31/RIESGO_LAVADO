import { MATRICES_VIEW_RESPONSIBILITIES } from './matrices-view-responsibilities';

describe('responsabilidades de las vistas de Matrices de Riesgos', () => {
  it('separa catálogo maestro, flujo transaccional y consulta institucional oficial', () => {
    expect(MATRICES_VIEW_RESPONSIBILITIES).toEqual({
      risks: 'MASTER_CATALOG',
      evaluations: 'TRANSACTIONAL_WORKFLOW',
      consolidated: 'OFFICIAL_INSTITUTIONAL_VIEW'
    });
  });
});
