#!/usr/bin/env node
'use strict';

const fs = require('node:fs');
const path = require('node:path');

const root = path.resolve(__dirname, '..');
const contractPath = path.join(root, 'frontend/rl-app/src/app/features/admin/matrices-riesgos/models/matriz-institucional.contract.ts');
const manifestPath = path.join(root, 'backend/RL.API/Features/MatricesRiesgos/Contracts/matriz_riesgos_82_campos_manifest.json');
const source = fs.readFileSync(contractPath, 'utf8');
const manifest = JSON.parse(fs.readFileSync(manifestPath, 'utf8'));

const seedsStart = source.indexOf('const SEEDS:');
const seedsEnd = source.indexOf('\n];', seedsStart);
const rangesMatch = source.match(/const BLOCK_RANGES\s*=\s*\[([^\]]+)\]/);
if (seedsStart < 0 || seedsEnd < 0 || !rangesMatch) {
  throw new Error('No se pudo leer SEEDS/BLOCK_RANGES del contrato institucional del frontend.');
}
const seedSource = source.slice(seedsStart, seedsEnd);
const seeds = [...seedSource.matchAll(/\{\s*label:\s*'((?:[^'\\]|\\.)*)',\s*mode:\s*'([^']+)'/g)]
  .map((match, index) => ({ ordinal: index + 1, label: match[1].replace(/\\'/g, "'"), mode: match[2] }));
const ranges = rangesMatch[1].split(',').map(item => Number(item.trim()));

const errors = [];
if (seeds.length !== 82 || manifest.length !== 82) errors.push(`FIELD_COUNT=${seeds.length}/${manifest.length}; esperado 82/82`);
for (let index = 0; index < Math.min(seeds.length, manifest.length); index++) {
  const ui = seeds[index];
  const official = manifest[index];
  const block = ranges.findIndex(last => ui.ordinal <= last) + 1;
  if (official.number !== ui.ordinal) errors.push(`ORDINAL_DRIFT=${ui.ordinal}; manifest=${official.number}`);
  if (official.label !== ui.label) errors.push(`LABEL_DRIFT=F${ui.ordinal}; manifest="${official.label}"; UI="${ui.label}"`);
  if (official.bloqueNum !== block) errors.push(`BLOCK_DRIFT=F${ui.ordinal}; manifest=${official.bloqueNum}; UI=${block}`);
  if (!['MASTER', 'INPUT', 'COMPUTED', 'REPEATER', 'MONITORING', 'EVIDENCE'].includes(ui.mode)) errors.push(`UNKNOWN_UI_MODE=F${ui.ordinal}:${ui.mode}`);
}

if (errors.length) {
  console.error(errors.join('\n'));
  process.exit(1);
}
console.log('FIELD_COUNT=82/82');
console.log('EXACT_VISIBLE_LABELS=82/82');
console.log('BLOCK_FIELD_PARTITION=82/82');
console.log('UI_FIELD_CLASSIFICATION=82/82');
console.log('UNKNOWN_UI_FIELDS=0');
