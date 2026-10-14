/** Canonical ownership boundaries for the Matrices de Riesgos views. */
export const MATRICES_VIEW_RESPONSIBILITIES = {
  risks: 'MASTER_CATALOG',
  evaluations: 'TRANSACTIONAL_WORKFLOW',
  consolidated: 'OFFICIAL_INSTITUTIONAL_VIEW'
} as const;
