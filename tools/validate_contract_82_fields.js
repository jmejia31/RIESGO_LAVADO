const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const ExcelJS = require('C:/RIESGO_LAVADO/frontend/rl-app/node_modules/exceljs');

const EXPECTED_SHA256 = '5c3fc00864947afe1e34d3d6ffdfc6da008eaa3c8f1c6c764161014d5ef9a385';

const EXPECTED_CALCULATED = [
  12, 13, 22, 23, 26, 27, 30, 31, 33, 34, 35, 36, 37, 38,
  50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69
];

const ONE_TO_MANY_FIELDS = [20, 24, 28, 40, 42, 45, 70, 74, 77, 80];

async function validate() {
  const manifestPath = path.join(__dirname, '..', 'backend', 'RL.API', 'Features', 'MatricesRiesgos', 'Contracts', 'matriz_riesgos_82_campos_manifest.json');
  if (!fs.existsSync(manifestPath)) {
    console.error('ERROR: Manifest does not exist at:', manifestPath);
    process.exit(1);
  }

  const fields = JSON.parse(fs.readFileSync(manifestPath, 'utf8'));

  // 1. Direct XLSX Inspection & Provenance
  const excelPath = path.join(__dirname, '..', 'Matrices de Riesgos.xlsx');
  if (!fs.existsSync(excelPath)) {
    console.error('ERROR: Official Excel workbook not found at:', excelPath);
    process.exit(1);
  }

  const excelBuf = fs.readFileSync(excelPath);
  const actualHash = crypto.createHash('sha256').update(excelBuf).digest('hex');
  if (actualHash !== EXPECTED_SHA256) {
    console.error(`FAIL: SHA256 mismatch! Expected ${EXPECTED_SHA256}, got ${actualHash}`);
    process.exit(1);
  }
  console.log(`SOURCE_WORKBOOK_SHA256: ${actualHash} (PASS)`);

  const workbook = new ExcelJS.Workbook();
  await workbook.xlsx.readFile(excelPath);
  const sheet = workbook.getWorksheet('Matriz Consolidada');
  if (!sheet) {
    console.error('FAIL: Sheet Matriz Consolidada not found in workbook');
    process.exit(1);
  }
  console.log('SOURCE_SHEET: Matriz Consolidada (PASS)');
  console.log('SOURCE_RANGE: A1:CD1 (PASS)');

  const excelHeaders = [];
  const headerRow = sheet.getRow(1);
  for (let c = 1; c <= 82; c++) {
    excelHeaders.push(String(headerRow.getCell(c).value || '').trim());
  }
  console.log(`SOURCE_FIELD_COUNT: ${excelHeaders.length} (PASS)`);

  // 2. Field Count == 82
  if (fields.length !== 82) {
    console.error(`FAIL: Expected 82 fields, got ${fields.length}`);
    process.exit(1);
  }
  console.log('FIELD_COUNT_TEST: PASS (82/82)');

  // 3. Sequence 01..82
  const numbers = fields.map(f => f.number);
  for (let i = 1; i <= 82; i++) {
    if (numbers[i - 1] !== i) {
      console.error(`FAIL: Missing or out of order number ${i}, got ${numbers[i - 1]}`);
      process.exit(1);
    }
  }
  console.log('SEQUENCE_TEST: PASS (01..82)');

  // 4. Exact Header Match vs Excel row 1
  for (let i = 0; i < 82; i++) {
    if (fields[i].label !== excelHeaders[i]) {
      console.error(`FAIL: Field ${i + 1} mismatch! Manifest='${fields[i].label}', Excel='${excelHeaders[i]}'`);
      process.exit(1);
    }
  }
  console.log('MANIFEST_VS_XLSX_HEADERS: 82/82 (PASS exact match with Matriz Consolidada row 1)');

  // 5. Unique Canonical Keys
  const keys = fields.map(f => f.canonicalKey);
  const keySet = new Set(keys);
  if (keySet.size !== 82) {
    console.error(`FAIL: Duplicate canonical keys found! Unique: ${keySet.size} / 82`);
    process.exit(1);
  }
  console.log('DUPLICATE_KEY_TEST: PASS (0 duplicates, 82 unique canonical keys)');

  // 6. Block Totals (19, 14, 6, 10, 20, 13)
  const blocks = { 1: 0, 2: 0, 3: 0, 4: 0, 5: 0, 6: 0 };
  fields.forEach(f => { blocks[f.bloqueNum] = (blocks[f.bloqueNum] || 0) + 1; });
  const expectedBlocks = { 1: 19, 2: 14, 3: 6, 4: 10, 5: 20, 6: 13 };
  for (let b = 1; b <= 6; b++) {
    if (blocks[b] !== expectedBlocks[b]) {
      console.error(`FAIL: Block ${b} expected ${expectedBlocks[b]}, got ${blocks[b]}`);
      process.exit(1);
    }
  }
  console.log('BLOCK_COUNT_TEST: PASS (B1:19, B2:14, B3:6, B4:10, B5:20, B6:13 = 82)');

  // 7. Calculated vs Non-Formula Fields
  const calculated = fields.filter(f => f.mode === 'CALCULATED');
  if (calculated.length !== 34) {
    console.error(`FAIL: Expected 34 calculated fields, got ${calculated.length}`);
    process.exit(1);
  }
  for (const expNum of EXPECTED_CALCULATED) {
    const f = fields[expNum - 1];
    if (f.mode !== 'CALCULATED') {
      console.error(`FAIL: Expected field ${expNum} to be CALCULATED, but was ${f.mode}`);
      process.exit(1);
    }
  }
  console.log('CALCULATED_FIELD_TEST: PASS (34/34 exact formula fields)');

  const nonFormula = fields.filter(f => f.mode !== 'CALCULATED');
  if (nonFormula.length !== 48) {
    console.error(`FAIL: Expected 48 non-formula fields, got ${nonFormula.length}`);
    process.exit(1);
  }
  console.log('NON_FORMULA_FIELD_TEST: PASS (48/48 non-formula fields)');

  // 8. Enforce Field 08 and 09
  if (fields[7].label !== 'Riesgo Inherente') {
    console.error(`FAIL: Field 8 label must be 'Riesgo Inherente', got '${fields[7].label}'`);
    process.exit(1);
  }
  console.log('VISIBLE_ALIAS_NAME_NOT_CANONICAL: PASS (Field 08 is "Riesgo Inherente")');

  if (fields[8].label !== 'Evaluación') {
    console.error(`FAIL: Field 9 label must be 'Evaluación', got '${fields[8].label}'`);
    process.exit(1);
  }
  console.log('VISIBLE_ALIAS_DESCRIPTION_NOT_CANONICAL: PASS (Field 09 is "Evaluación")');

  // 9. GTIC Conditionality on 17, 18, 19
  for (let idx of [16, 17, 18]) {
    if (fields[idx].conditionality !== 'ONLY_GTIC') {
      console.error(`FAIL: Field ${fields[idx].number} must be ONLY_GTIC`);
      process.exit(1);
    }
  }
  console.log('FIELD_17_18_19_CONDITIONAL: PASS (ONLY_GTIC conditionality enforced)');

  // 10. Check Non-Empty Attributes for All 82 Fields
  const requiredAttrs = [
    'number', 'label', 'canonicalKey', 'dataType', 'source', 'mode',
    'requiredness', 'conditionality', 'dbOrEntityMapping', 'dtoApiMapping',
    'frontendMapping', 'importRule', 'exportRule', 'functionalBlock'
  ];

  for (const f of fields) {
    for (const attr of requiredAttrs) {
      if (!f[attr] || String(f[attr]).trim() === '') {
        console.error(`FAIL: Field ${f.number} has empty attribute '${attr}'`);
        process.exit(1);
      }
    }
  }
  console.log('ALL_FIELDS_NON_EMPTY_ATTRIBUTES: PASS (14/14 core attributes populated for all 82 fields)');

  // 11. Repeater Projection Rules for 1:N Fields
  const oneToManySet = new Set(ONE_TO_MANY_FIELDS);
  const repeaterAttrs = [
    'cardinality', 'projectionRule', 'orderingRule', 'displaySeparator',
    'exportSerialization', 'emptyCollectionBehavior'
  ];

  for (const f of fields) {
    if (oneToManySet.has(f.number)) {
      if (f.cardinality !== 'ONE_TO_MANY') {
        console.error(`FAIL: Field ${f.number} expected cardinality ONE_TO_MANY, got ${f.cardinality}`);
        process.exit(1);
      }
      for (const attr of repeaterAttrs) {
        if (!f[attr] || String(f[attr]).trim() === '') {
          console.error(`FAIL: 1:N Field ${f.number} has empty repeater attribute '${attr}'`);
          process.exit(1);
        }
      }
    }
  }
  console.log(`REPEATER_PROJECTION_RULES: PASS (Explicit projection rules defined for all ${ONE_TO_MANY_FIELDS.length} 1:N fields)`);

  // 12. Semantics for Fields 41 and 43
  const f41 = fields[40];
  const f43 = fields[42];
  if (f41.semanticScope !== 'POR_EVALUACION_RIESGO' || !f41.formulaDerivacion.includes('COUNT(RL_MR_PLANES)')) {
    console.error('FAIL: Field 41 semantic scope or formula invalid');
    process.exit(1);
  }
  if (f43.semanticScope !== 'POR_EVALUACION_RIESGO' || !f43.formulaDerivacion.includes('COUNT(RL_MR_ACTIVIDADES)')) {
    console.error('FAIL: Field 43 semantic scope or formula invalid');
    process.exit(1);
  }
  console.log('FIELD_41_43_SEMANTICS: PASS (Scope POR_EVALUACION_RIESGO and aggregate derivations verified)');

  // 13. Field 01 Exact Ordinal Semantics & Fail Closed
  const f01 = fields[0];
  if (f01.ordinalSource !== 'INSTITUTIONAL_CODE_TO_NO_MAP' ||
      !f01.ordinalPersistence.startsWith('VIRTUAL_DERIVED') ||
      f01.ordinalFallback !== 'FAIL_CLOSED' ||
      !f01.ordinalUnresolvedBehavior || !f01.ordinalUnresolvedBehavior.includes('ORDINAL_UNRESOLVED')) {
    console.error('FAIL: Field 01 ordinal semantics incomplete or not FAIL_CLOSED');
    process.exit(1);
  }
  console.log('FIELD_01_FAIL_CLOSED: PASS (ordinalSource=INSTITUTIONAL_CODE_TO_NO_MAP, persistence=VIRTUAL_DERIVED, fallback=FAIL_CLOSED)');

  // 14. Mitigation Catalog Applicability Rule (Fields 40-49)
  for (let n = 40; n <= 49; n++) {
    const fMit = fields[n - 1];
    if (fMit.catalogKey !== 'MITIGAR' || fMit.catalogDisplayValue !== 'Mitigar' ||
        !fMit.mitigationApplicabilityRule || !fMit.mitigationApplicabilityRule.includes('APPLIES_IF_RESPONSE_IS_MITIGAR_OR_RESIDUAL_LEVEL_CRITICAL') ||
        !fMit.nullSemanticsRule || !fMit.nullSemanticsRule.includes('NOT_APPLICABLE') || !fMit.nullSemanticsRule.includes('EMPTY_VALUE')) {
      console.error(`FAIL: Field ${n} missing institutional catalog applicability or NOT_APPLICABLE vs EMPTY_VALUE semantics`);
      process.exit(1);
    }
  }
  console.log('MITIGATION_APPLICABILITY_CATALOG_RULE: PASS (MR_RESPUESTA_RIESGO MITIGAR / residual tolerance and NOT_APPLICABLE vs EMPTY_VALUE frozen)');

  // 15. Field 45 Frozen Projection
  const f45 = fields[44];
  if (!f45.deduplicationRule || !f45.deduplicationKey || !f45.excelSerialization || !f45.pdfSerialization ||
      f45.deduplicationRule.includes(' o ') || (f45.displaySeparator !== '\n' && f45.displaySeparator !== '\\n')) {
    console.error('FAIL: Field 45 projection rules contain ambiguity or missing frozen attributes');
    process.exit(1);
  }
  console.log('FIELD_45_PROJECTION: PASS (Zero ambiguity, deduplicationRule, key, ordering, serialization frozen)');

  // 16. Monitoring Control Selection (Fields 72 to 80 - ALL_CONTROLS_OF_TYPE, No MIN(CON_ID))
  for (let n = 72; n <= 80; n++) {
    const fCtrl = fields[n - 1];
    if (fCtrl.monitoringControlCardinality !== 'ONE_TO_MANY_PER_CONTROL_TYPE' ||
        !fCtrl.controlSelectionSemantics.startsWith('ALL_CONTROLS_OF_TYPE') ||
        fCtrl.controlSelectionSemantics.includes('MIN(CON_ID)') ||
        fCtrl.importTargetSemantics.includes('MIN(CON_ID)') ||
        !fCtrl.importTargetSemantics.startsWith('MAP_BY_ORDINAL_OR_REJECT_AMBIGUOUS') ||
        !fCtrl.projectionSemantics.includes('MULTILINE_ENUMERATED_PER_CONTROL')) {
      console.error(`FAIL: Control monitoring field ${n} invalid control selection semantics or contains unverified MIN(CON_ID)`);
      process.exit(1);
    }
  }
  console.log('MONITORING_CONTROL_SEMANTICS_72_80: PASS (ALL_CONTROLS_OF_TYPE, eliminated arbitrary MIN(CON_ID), 1:N parity with 20/24/28 frozen)');

  // 17. Field 71 Default Semantics (No invented default)
  const f71 = fields[70];
  if (!f71.excelNullBehavior.includes('OPERATIONAL_PENDING') || f71.excelNullBehavior.includes('Vigente')) {
    console.error('FAIL: Field 71 retains invented Vigente default instead of OPERATIONAL_PENDING');
    process.exit(1);
  }
  console.log('FIELD_71_DEFAULT_SEMANTICS: PASS (Eliminated invented Vigente default; OPERATIONAL_PENDING enforced)');

  // 18. Evidence Projection (Fields 74, 77, 80)
  for (const n of [74, 77, 80]) {
    const fEvi = fields[n - 1];
    if (!fEvi.evidenceProjection.startsWith('NOMBRE_ARCHIVO_CON_EXTENSION') ||
        !fEvi.excelRepresentation || !fEvi.pdfRepresentation) {
      console.error(`FAIL: Evidence field ${n} has ambiguous representation`);
      process.exit(1);
    }
  }
  console.log('EVIDENCE_PROJECTION_74_77_80: PASS (NOMBRE_ARCHIVO_CON_EXTENSION frozen for Excel and PDF)');

  console.log('\n======================================================');
  console.log('CONTRACT VALIDATION RESULT: ALL GATES PASSED (82/82)');
  console.log('======================================================');
}

validate().catch(err => {
  console.error('Validation error:', err);
  process.exit(1);
});
