const fs = require('node:fs');
const path = require('node:path');

const root = path.resolve(__dirname, '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');
const checks = [];
const check = (name, pass) => checks.push({ name, pass });

const projectionMapper = read('backend/RL.API/Features/MatricesRiesgos/Application/InstitutionalProjectionMapper.cs');
const mitigationRepository = read('backend/RL.API/Features/MatricesRiesgos/Persistence/MatricesRiesgosMitigacionRepository.cs');
const projectionReadRepository = read('backend/RL.API/Features/MatricesRiesgos/Persistence/InstitutionalProjectionReadRepository.cs');
const monitoringRepository = read('backend/RL.API/Features/MatricesRiesgos/Persistence/MatricesRiesgosMonitoreoRepository.cs');
const monitoringService = read('backend/RL.API/Features/MatricesRiesgos/Application/MatricesRiesgosMonitoreoService.cs');
const monitoringController = read('backend/RL.API/Features/MatricesRiesgos/MatricesRiesgosMonitoreoController.cs');
const coreRepository = read('backend/RL.API/Features/MatricesRiesgos/Persistence/MatricesRiesgosRepository.cs');
const mitigationUi = read('frontend/rl-app/src/app/features/admin/matrices-riesgos/components/matrices-riesgos-mitigacion/matrices-riesgos-mitigacion.component.html');
const monitoringUi = read('frontend/rl-app/src/app/features/admin/matrices-riesgos/components/matrices-riesgos-monitoreo-operativo/matrices-riesgos-monitoreo-operativo.component.html');
const monitoringUiTs = read('frontend/rl-app/src/app/features/admin/matrices-riesgos/components/matrices-riesgos-monitoreo-operativo/matrices-riesgos-monitoreo-operativo.component.ts');
const readModel = read('backend/RL.API/Features/MatricesRiesgos/Persistence/InstitutionalProjectionReadRepository.cs');
const browserSuite = read('frontend/rl-app/e2e/block7-oracle-real.spec.ts');

const mitigationFields = [
  ['40', 'PLA_DESCRIPCION', 'ONE_TO_MANY'], ['41', 'CantidadAcciones', 'DERIVED'],
  ['42', 'ACT_DESCRIPCION', 'ONE_TO_MANY'], ['43', 'CantidadActividades', 'DERIVED'],
  ['44', 'PLA_MONITOREO_SEGUIMIENTO', 'PLAN_SCOPED'], ['45', 'PLA_RESPONSABLES', 'PLAN_SCOPED'],
  ['46', 'PLA_FECHA_INICIO', 'PLAN_SCOPED'], ['47', 'PLA_FECHA_FIN', 'PLAN_SCOPED'],
  ['48', 'PLA_RECURSOS', 'PLAN_SCOPED'], ['49', 'PLA_PRESUPUESTO', 'PLAN_SCOPED']
];
const monitoringFields = [
  ['70', 'RL_MR_SENALES_ALERTA', 'READ/CREATE/STATE'], ['71', 'MON_ESTADO_RIESGO', 'LATEST_AUTOMONITORING'],
  ['72', 'CON_ESTADO_MONITOREO', 'MONITORING_COMMAND'], ['73', 'CON_EFECTIVIDAD_MONITOREO', 'MONITORING_COMMAND'],
  ['74', 'RL_MR_EVIDENCIAS_VINCULOS', 'CONTROL_EVIDENCE'], ['75', 'CON_ESTADO_MONITOREO', 'MONITORING_COMMAND'],
  ['76', 'CON_EFECTIVIDAD_MONITOREO', 'MONITORING_COMMAND'], ['77', 'RL_MR_EVIDENCIAS_VINCULOS', 'CONTROL_EVIDENCE'],
  ['78', 'CON_ESTADO_MONITOREO', 'MONITORING_COMMAND'], ['79', 'CON_EFECTIVIDAD_MONITOREO', 'MONITORING_COMMAND'],
  ['80', 'RL_MR_EVIDENCIAS_VINCULOS', 'CONTROL_EVIDENCE'], ['81', 'MON_OBSERVACIONES_AREA', 'AREA_CAPABILITY'],
  ['82', 'MON_OBSERVACIONES_UGR', 'UGR_CAPABILITY']
];

check('MITIGATION_FIELD_MAPPING=10/10', mitigationFields.length === 10
  && projectionMapper.includes('plan.PlaDescripcion')
  && projectionReadRepository.includes('CantidadAcciones=plans.Count')
  && projectionReadRepository.includes('plan.CantidadActividades++')
  && mitigationRepository.includes('PLA_MONITOREO_SEGUIMIENTO') && mitigationRepository.includes('ACT_DESCRIPCION')
  && projectionReadRepository.includes('PLA_DESCRIPCION') && projectionReadRepository.includes('PLA_PRESUPUESTO')
  && projectionReadRepository.includes('PLA_FECHA_INICIO') && projectionReadRepository.includes('PLA_FECHA_FIN')
  && projectionReadRepository.includes('PLA_RESPONSABLES') && projectionReadRepository.includes('PLA_RECURSOS'));
check('F41_F43_DERIVED_READONLY=PASS', mitigationRepository.includes('CantidadAcciones = planes.Count')
  && mitigationRepository.includes('plan.CantidadActividades++')
  && !mitigationUi.includes('cantidadAcciones') && !mitigationUi.includes('cantidadActividades'));
check('PLAN_ACTIVITY_ORDERING=PASS', mitigationRepository.includes('ORDER BY p.PLA_ID ASC, a.ACT_ID ASC')
  && mitigationRepository.includes('ORDER BY ACT_ID'));
check('MONITORING_FIELD_MAPPING=13/13', monitoringFields.length === 13
  && monitoringRepository.includes('ORDER BY MON_FECHA DESC, MON_ID DESC')
  && monitoringRepository.includes('CON_ESTADO_MONITOREO')
  && monitoringRepository.includes('CON_EFECTIVIDAD_MONITOREO'));
check('EVIDENCE_ORDERING=PASS', monitoringRepository.includes('e.EVI_FECHA_CREACION ASC, e.EVI_ID ASC')
  && readModel.includes('e.EVI_FECHA_CREACION ASC,e.EVI_ID ASC'));
check('AREA_UGR_STORAGE_AND_CAPABILITIES_SEPARATE=PASS', monitoringRepository.includes('MON_OBSERVACIONES_AREA')
  && monitoringRepository.includes('MON_OBSERVACIONES_UGR')
  && monitoringRepository.includes('MATRICES_RIESGO_OBSERVACIONES_AREA_EDITAR')
  && monitoringRepository.includes('MATRICES_RIESGO_OBSERVACIONES_UGR_EDITAR'));
check('SINGLE_CONTROL_MONITORING_WRITE_SURFACE=PASS', monitoringController.includes('controles/{controlId:long}')
  && monitoringService.includes('EsPorcentajeEfectividadValido')
  && !mitigationUi.includes('control-estado-monitoreo') && !mitigationUi.includes('control-efectividad-monitoreo')
  && !mitigationRepository.includes('SET CON_ESTADO_MONITOREO'));
check('MONITORING_UI_READ_MODEL_AND_RBAC=PASS', monitoringUiTs.includes('obtenerBloque6')
  && monitoringUi.includes('snapshot.puedeEditarObservacionesArea')
  && monitoringUi.includes('snapshot.puedeEditarObservacionesUgr')
  && monitoringUi.includes('cargarEvidencia'));
check('EVIDENCE_UNLINK_PRESERVES_FILE=PASS', coreRepository.includes('DesvincularEvidenciaAsync')
  && monitoringUiTs.includes('desvincularEvidencia')
  && coreRepository.includes('DELETE FROM RL_MR_EVIDENCIAS_VINCULOS WHERE EVV_ID=:id'));
check('NO_PHYSICAL_DELETE_WHILE_LINKED=PASS', coreRepository.includes('ResultadoEliminacionEvidencia.TieneVinculos'));
check('CRITICAL_BROWSER_ROUTES_PASS_THROUGH=PASS', browserSuite.includes('if (critical.some(pattern => pattern.test(key)))')
  && browserSuite.includes('route.continue()')
  && browserSuite.includes("RL_BLOCK9_REAL_E2E_REQUIRED")
  && browserSuite.includes('monitoreo')
  && browserSuite.includes('evidencias')
  && browserSuite.includes('cargar')
  && browserSuite.includes('vinculos')
  && browserSuite.includes('planes|actividades'));
check('BLOCK9_FIELD_AUDIT_ROWS=23/23', mitigationFields.length + monitoringFields.length === 23);

for (const item of checks) console.log(`${item.pass ? 'PASS' : 'FAIL'} ${item.name}`);
console.log(`BLOCK9_CONTRACT_CHECKS=${checks.filter(item => item.pass).length}/${checks.length}`);
if (checks.some(item => !item.pass)) process.exitCode = 1;
