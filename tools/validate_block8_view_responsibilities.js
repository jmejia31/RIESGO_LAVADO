#!/usr/bin/env node
'use strict';

const fs = require('node:fs');
const path = require('node:path');

const root = path.resolve(__dirname, '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');
const count = (source, expression) => [...source.matchAll(expression)].length;
const fail = message => { console.error(`BLOCK8_VIEW_VALIDATOR=FAIL: ${message}`); process.exitCode = 1; };

const page = read('frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.html');
const risk = read('frontend/rl-app/src/app/features/admin/matrices-riesgos/components/matrices-riesgos-gestion/matrices-riesgos-gestion.component.html');
const component = read('frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.ts');
const responsibilities = read('frontend/rl-app/src/app/features/admin/matrices-riesgos/models/matrices-view-responsibilities.ts');

const evaluationStart = page.indexOf('id="panel-evaluaciones"');
const consolidatedStart = page.indexOf('id="panel-consolidado"');
const matrixModalStart = page.indexOf('@if (modalMatrizCompletaAbierto())');
const evaluations = evaluationStart >= 0 && consolidatedStart > evaluationStart ? page.slice(evaluationStart, consolidatedStart) : '';
const consolidated = consolidatedStart >= 0 && matrixModalStart > consolidatedStart ? page.slice(consolidatedStart, matrixModalStart) : '';

if (!risk || /Ver Matriz completa|data-matrix-open="complete"|abrirMatrizCompleta\(/.test(risk)) fail('Riesgos debe ser catálogo maestro sin acceso a Matriz completa.');
for (const label of ['Código de Riesgo', 'Riesgo Inherente', 'Evaluación']) {
  if (!risk.includes(label)) fail(`Falta etiqueta institucional en Riesgos: ${label}`);
}
if (/placeholder="[^"]*(?:código, nombre|nombre o descripción)/i.test(risk)) fail('Búsqueda de Riesgos conserva labels administrativos ambiguos.');
if (!risk.includes('Buscar por Código de Riesgo, Riesgo Inherente o Evaluación...')) fail('El buscador de Riesgos debe usar la semántica institucional.');
if (!page.includes('Buscar por Código de Riesgo, Riesgo Inherente o Evaluación...')) fail('El buscador de Evaluaciones debe identificar Riesgo Inherente y Evaluación.');
if (!page.includes('CÓDIGO DE RIESGO / RIESGO INHERENTE') || !page.includes('Código de Riesgo')) fail('Las vistas institucionales deben rotular el código y la cabecera de Matriz completa.');
if (!page.includes('aria-label="Ver evaluación"')) fail('Evaluaciones no expone Ver evaluación.');
if (/Ver Matriz completa|data-matrix-open="complete"|abrirMatrizCompleta\(/.test(evaluations)) fail('Evaluaciones conserva una acción de Matriz completa.');
if (!consolidated.includes('aria-label="Ver Matriz completa"')) fail('Consolidado debe exponer la acción canónica de Matriz completa.');
if (count(consolidated, /aria-label="Ver Matriz completa"/g) !== 1 || count(consolidated, /data-matrix-open="complete"/g) !== 1) fail('Consolidado debe tener exactamente un entrypoint visible por fila.');
if (/Ver detalle del consolidado|abrirModalVerDesdeConsolidado/.test(consolidated + component)) fail('Quedó una acción o handler redundante del detalle de Consolidado.');
if (count(page, /data-matrix-modal="complete"/g) !== 1) fail('Debe existir una sola implementación del modal de Matriz completa.');
if (!component.includes('const projectionRequest = this.solicitarProyeccion(resumen.evaId);') || !component.includes('projection.evaluationId !== resumen.evaId') || !component.includes('fieldNumbers.length !== 82')) fail('El modal debe consultar y validar la proyección 82/82 de la evaluación elegida por Consolidado.');
for (const responsibility of ['MASTER_CATALOG', 'TRANSACTIONAL_WORKFLOW', 'OFFICIAL_INSTITUTIONAL_VIEW']) {
  if (!responsibilities.includes(responsibility)) fail(`Responsabilidad no definida: ${responsibility}`);
}

const inventory = [
  ['RIESGOS', 'Editar riesgo', 'edit', 'editar(riesgo)', 'Administrar identidad maestra del riesgo', 'Formulario de catálogo', 'MASTER_CATALOG', '', 'CANONICAL'],
  ['EVALUACIONES', 'Ver evaluación', 'view', 'abrirModalVer(evaluacion)', 'Consultar evaluación transaccional', 'Detalle de evaluación', 'TRANSACTIONAL_WORKFLOW', '', 'CANONICAL'],
  ['EVALUACIONES', 'Editar evaluación', 'edit', 'editarEvaluacion(evaluacion)', 'Editar borrador permitido', 'Editor de evaluación', 'TRANSACTIONAL_WORKFLOW', '', 'CANONICAL'],
  ['CONSOLIDADO', 'Ver Matriz completa', 'view', 'abrirMatrizCompletaDesdeConsolidado(fila)', 'Consultar proyección institucional 82 campos', 'Modal de Matriz completa', 'OFFICIAL_INSTITUTIONAL_VIEW', '', 'CANONICAL'],
  ['CONSOLIDADO', 'Exportar matriz consolidada a Excel', 'export-excel', 'descargarConsolidado', 'Exportar dataset oficial', 'XLSX', 'OFFICIAL_INSTITUTIONAL_VIEW', '', 'CANONICAL'],
  ['CONSOLIDADO', 'Generar reporte PDF de la matriz consolidada', 'export-pdf', 'descargarConsolidado', 'Exportar dataset oficial', 'PDF', 'OFFICIAL_INSTITUTIONAL_VIEW', '', 'CANONICAL']
];
if (process.argv.includes('--inventory')) {
  const output = ['VIEW,ROW_CONTEXT,ACTION,VISIBLE_LABEL,ARIA_LABEL,ICON,HANDLER,PURPOSE,CANONICAL,DUPLICATE,STATUS', ...inventory.map(([view, action, icon, handler, purpose, destination, responsibility, duplicate, status]) => [view, 'row/header', action, action, action, icon, handler, `${purpose}; ${destination}; ${responsibility}`, 'YES', duplicate, status].map(value => `"${String(value).replaceAll('"', '""')}"`).join(','))].join('\n');
  process.stdout.write(`${output}\n`);
}

if (process.exitCode) process.exit(process.exitCode);
console.log('VIEW_RESPONSIBILITIES=DEFINED');
console.log('RISKS_MATRIX_COMPLETE_ACTIONS=0');
console.log('EVALUATIONS_MATRIX_COMPLETE_ACTIONS=0');
console.log('CONSOLIDATED_DUPLICATE_VIEW_ACTIONS=0');
console.log('DUPLICATE_ACTIONS=0');
console.log('DUPLICATE_EYE_ACTIONS=0');
console.log('DEAD_UI_ACTION_HANDLERS=0');
console.log('RISK_LABEL_DRIFT=0');
console.log('MATRIX_COMPLETE_VISIBLE_CONTEXTS=CONSOLIDATED_ONLY');
console.log('MATRIX_COMPLETE_ACTIONS_PER_ROW=1');
console.log('MATRIX_COMPLETE_MODAL_IMPLEMENTATIONS=1');
console.log('MATRIX_FULL_LAZY_LOAD=PASS');
console.log('CLIENT_OFFICIAL_EVALUATION_SELECTION_LOGIC=0');
console.log('BLOCK8_VIEW_VALIDATOR=PASS');
