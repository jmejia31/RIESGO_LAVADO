#!/usr/bin/env node
'use strict';

const fs = require('node:fs');
const path = require('node:path');

const root = path.resolve(__dirname, '..');
const appRoot = path.join(root, 'frontend', 'rl-app', 'src', 'app');
const sharedPath = 'shared/components/institutional-file-upload';
const failures = [];
const files = [];

function walk(directory) {
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    const absolute = path.join(directory, entry.name);
    if (entry.isDirectory()) walk(absolute);
    else if (/\.(html|ts)$/.test(entry.name)) files.push(absolute);
  }
}

walk(appRoot);
const rawInputs = [];
const canonicalTextFiles = [];
const componentFiles = files.filter(file => path.relative(appRoot, file).split(path.sep).join('/').startsWith(`${sharedPath}/`));

for (const file of files) {
  const relative = path.relative(appRoot, file).split(path.sep).join('/');
  const content = fs.readFileSync(file, 'utf8');
  const inputMatches = content.match(/<input\b(?:(?!>).)*\btype\s*=\s*["']file["'](?:(?!>).)*>/gis) || [];
  if (inputMatches.length && !relative.startsWith(`${sharedPath}/`)) rawInputs.push({ relative, count: inputMatches.length });
  if (file.endsWith('.html') && content.includes('Arrastra archivos aquí o haz clic')) canonicalTextFiles.push(relative);
}

const selectorDeclarations = files.flatMap(file => {
  const content = fs.readFileSync(file, 'utf8');
  return [...content.matchAll(/selector\s*:\s*['"]app-institutional-file-upload['"]/g)].map(() => path.relative(appRoot, file));
});
const requiredFiles = [
  `${sharedPath}/institutional-file-upload.component.ts`,
  `${sharedPath}/institutional-file-upload.component.html`,
  `${sharedPath}/institutional-file-upload.component.spec.ts`,
  `${sharedPath}/institutional-file-upload-policy.ts`,
].map(relative => path.join(appRoot, ...relative.split('/')));
for (const file of requiredFiles) if (!fs.existsSync(file)) failures.push(`Missing shared uploader file: ${path.relative(root, file)}`);

const consumers = [
  'features/admin/listas/pages/cargar-listas/cargar-listas.component.html',
  'features/admin/listas/pages/monitoreo-listas/monitoreo-listas.component.html',
  'features/admin/matrices-riesgos/components/matrices-riesgos-monitoreo-operativo/matrices-riesgos-monitoreo-operativo.component.html',
  'features/admin/configuracion/pages/configuracion/configuracion.component.html',
];
for (const relative of consumers) {
  const content = fs.readFileSync(path.join(appRoot, ...relative.split('/')), 'utf8');
  if (!content.includes('<app-institutional-file-upload')) failures.push(`Expected shared uploader consumer is missing: ${relative}`);
}

if (rawInputs.length) failures.push(`Raw file inputs outside shared component: ${JSON.stringify(rawInputs)}`);
if (canonicalTextFiles.length !== 1 || canonicalTextFiles[0] !== `${sharedPath}/institutional-file-upload.component.html`) {
  failures.push(`Canonical dropzone text must exist in one shared template: ${JSON.stringify(canonicalTextFiles)}`);
}
if (selectorDeclarations.length !== 1 || !selectorDeclarations[0].replaceAll('\\', '/').startsWith(`${sharedPath}/`)) {
  failures.push(`Expected exactly one shared uploader component declaration; found ${JSON.stringify(selectorDeclarations)}`);
}

console.log(`INSTITUTIONAL_FILE_UPLOAD_COMPONENTS=${selectorDeclarations.length}`);
console.log(`USER_UPLOAD_SURFACE_CONSUMER_TEMPLATES=${consumers.length}`);
const surfaceCount = consumers.reduce((total, relative) => {
  const content = fs.readFileSync(path.join(appRoot, ...relative.split('/')), 'utf8');
  return total + (content.match(/<app-institutional-file-upload\b/g) || []).length;
}, 0);
console.log(`USER_UPLOAD_SURFACES_FOUND=${surfaceCount}`);
console.log(`RAW_FILE_INPUTS_OUTSIDE_SHARED_COMPONENT=${rawInputs.length}`);
console.log(`CANONICAL_DROPZONE_TEXT_IMPLEMENTATIONS=${canonicalTextFiles.length}`);
console.log('MONITOREO_LISTAS_USES_SHARED_FILE_UPLOAD=' + (fs.readFileSync(path.join(appRoot, ...consumers[1].split('/')), 'utf8').includes('<app-institutional-file-upload') ? 'PASS' : 'FAIL'));
console.log('MATRICES_USES_SHARED_FILE_UPLOAD=' + (fs.readFileSync(path.join(appRoot, ...consumers[2].split('/')), 'utf8').includes('<app-institutional-file-upload') ? 'PASS' : 'FAIL'));

if (failures.length) {
  for (const failure of failures) console.error(`FAIL: ${failure}`);
  process.exit(1);
}
console.log('ALL_USER_FILE_UPLOAD_SURFACES_AUDITED=PASS');
console.log('INSTITUTIONAL_FILE_UPLOAD_STANDARD=PASS');
