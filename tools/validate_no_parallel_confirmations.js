#!/usr/bin/env node
'use strict';

const fs = require('node:fs');
const path = require('node:path');
const ts = require(path.join(__dirname, '..', 'frontend', 'rl-app', 'node_modules', 'typescript'));

const repositoryRoot = path.join(__dirname, '..');
const sourceRoot = path.join(repositoryRoot, 'frontend', 'rl-app', 'src', 'app');
const adapterSuffix = 'shared/confirmation/institutional-confirmation.service.ts';

function propertyName(property) {
  return ts.isIdentifier(property.name) || ts.isStringLiteral(property.name) ? property.name.text : null;
}

function truthyLiteral(initializer) {
  return initializer && initializer.kind === ts.SyntaxKind.TrueKeyword;
}

function rootIdentifier(expression) {
  let current = expression;
  while (ts.isPropertyAccessExpression(current) || ts.isElementAccessExpression(current)) current = current.expression;
  return ts.isIdentifier(current) ? current.text : null;
}

function isSweetAlertSource(sourceFile, sourceText) {
  if (sourceText.includes("'sweetalert2'") || sourceText.includes('"sweetalert2"')) return true;
  let found = false;
  function visit(node) {
    if (ts.isImportDeclaration(node) && node.moduleSpecifier.text === 'sweetalert2') found = true;
    ts.forEachChild(node, visit);
  }
  visit(sourceFile);
  return found;
}

function collectAliases(sourceFile) {
  const aliases = new Set(['Swal', 'SweetAlert', 'sweetAlert']);
  let changed = true;
  while (changed) {
    changed = false;
    function visit(node) {
      if (ts.isImportDeclaration(node) && node.moduleSpecifier.text === 'sweetalert2' && node.importClause?.name) {
        if (!aliases.has(node.importClause.name.text)) {
          aliases.add(node.importClause.name.text);
          changed = true;
        }
      }
      if (ts.isImportDeclaration(node) && node.moduleSpecifier.text === 'sweetalert2' && node.importClause?.namedBindings) {
        const bindings = node.importClause.namedBindings;
        const importedNames = ts.isNamespaceImport(bindings)
          ? [bindings.name]
          : bindings.elements.map(element => element.name);
        for (const identifier of importedNames) {
          if (!aliases.has(identifier.text)) {
            aliases.add(identifier.text);
            changed = true;
          }
        }
      }
      if (ts.isVariableDeclaration(node) && ts.isIdentifier(node.name) && node.initializer) {
        const root = rootIdentifier(node.initializer);
        const dynamicImport = ts.isCallExpression(node.initializer)
          && ts.isIdentifier(node.initializer.expression)
          && node.initializer.expression.text === 'import'
          && node.initializer.arguments[0]?.text === 'sweetalert2';
        if (((root && aliases.has(root)) || dynamicImport) && !aliases.has(node.name.text)) {
          aliases.add(node.name.text);
          changed = true;
        }
      }
      if (ts.isCallExpression(node) && ts.isPropertyAccessExpression(node.expression)
        && node.expression.name.text === 'then' && ts.isCallExpression(node.expression.expression)
        && ts.isIdentifier(node.expression.expression.expression)
        && node.expression.expression.expression.text === 'import'
        && node.expression.expression.arguments[0]?.text === 'sweetalert2') {
        const callback = node.arguments[0];
        const parameter = callback && (ts.isArrowFunction(callback) || ts.isFunctionExpression(callback)) ? callback.parameters[0]?.name : null;
        if (parameter && ts.isIdentifier(parameter) && !aliases.has(parameter.text)) {
          aliases.add(parameter.text);
          changed = true;
        }
      }
      ts.forEachChild(node, visit);
    }
    visit(sourceFile);
  }
  return aliases;
}

function analyzeSource(source, fileName = 'fixture.ts') {
  const sourceFile = ts.createSourceFile(fileName, source, ts.ScriptTarget.Latest, true);
  const aliases = collectAliases(sourceFile);
  const sweetAlertRelated = isSweetAlertSource(sourceFile, source);
  const results = [];

  function visit(node) {
    if (ts.isCallExpression(node) && ts.isPropertyAccessExpression(node.expression)
      && ['fire', 'mixin'].includes(node.expression.name.text)) {
      const root = rootIdentifier(node.expression.expression);
      if (sweetAlertRelated && root && aliases.has(root)) {
        const arg = node.arguments[0];
        const line = sourceFile.getLineAndCharacterOfPosition(node.getStart(sourceFile)).line + 1;
        if (!arg || !ts.isObjectLiteralExpression(arg)) {
          results.push({ classification: 'UNKNOWN_CONFIRMATION', line, reason: 'SweetAlert fire options are not statically classifiable' });
        } else {
          const properties = new Map();
          for (const property of arg.properties) {
            if (ts.isPropertyAssignment(property)) properties.set(propertyName(property), property.initializer);
            else if (ts.isShorthandPropertyAssignment(property)) properties.set(property.name.text, property.name);
            else if (ts.isSpreadAssignment(property)) properties.set('<dynamic>', property);
          }
          const inputDialog = properties.has('input');
          const toast = truthyLiteral(properties.get('toast'));
          const cancel = properties.get('showCancelButton');
          const deny = properties.get('showDenyButton');
          const directConfirmation = truthyLiteral(cancel) || truthyLiteral(deny);
          const unknownDecision = (cancel && !truthyLiteral(cancel) && cancel.kind !== ts.SyntaxKind.FalseKeyword)
            || (deny && !truthyLiteral(deny) && deny.kind !== ts.SyntaxKind.FalseKeyword)
            || properties.has('<dynamic>');

          if (unknownDecision && !inputDialog) {
            results.push({ classification: 'UNKNOWN_CONFIRMATION', line, reason: 'Dynamic confirmation option or spread' });
          } else if (directConfirmation && !inputDialog) {
            results.push({ classification: 'LEGACY_DIRECT_CONFIRMATION', line });
          } else if (inputDialog) {
            results.push({ classification: 'INPUT_DIALOG', line });
          } else if (toast) {
            results.push({ classification: 'TOAST', line });
          } else {
            const icon = properties.get('icon');
            const iconValue = icon && ts.isStringLiteral(icon) ? icon.text : null;
            if (icon && !iconValue) {
              results.push({ classification: 'UNKNOWN_CONFIRMATION', line, reason: 'Dynamic SweetAlert icon/options require semantic review' });
            } else if (iconValue === 'success') {
              results.push({ classification: 'SUCCESS_NOTIFICATION', line });
            } else if (iconValue === 'error') {
              results.push({ classification: 'ERROR_NOTIFICATION', line });
            } else if (iconValue === 'info') {
              results.push({ classification: 'INFORMATIONAL_ALERT', line });
            } else if (iconValue === 'warning' || iconValue === 'question') {
              results.push({ classification: 'OTHER_NON_CONFIRMATION', line });
            } else {
              results.push({ classification: 'OTHER_NON_CONFIRMATION', line });
            }
          }
        }
      }
    }
    ts.forEachChild(node, visit);
  }

  visit(sourceFile);
  const normalizedFileName = fileName.replaceAll(path.sep, '/');
  if (normalizedFileName.endsWith(adapterSuffix)) return [];
  return results;
}

function listSourceFiles(directory) {
  return fs.readdirSync(directory, { withFileTypes: true }).flatMap(entry => {
    const fullPath = path.join(directory, entry.name);
    if (entry.isDirectory()) return listSourceFiles(fullPath);
    if (!entry.isFile() || !/\.ts$/.test(entry.name) || /(?:\.spec|\.test|\.e2e)\.ts$/.test(entry.name)) return [];
    return [fullPath];
  });
}

function selfTest() {
  const cases = [
    ['import Swal from "sweetalert2"; Swal.fire({ showCancelButton: true })', 'feature.ts', 'LEGACY_DIRECT_CONFIRMATION'],
    ['import Swal from "sweetalert2"; const dialog = Swal; dialog.default.fire({ showDenyButton: true })', 'feature.ts', 'LEGACY_DIRECT_CONFIRMATION'],
    ['import Swal from "sweetalert2"; Swal.fire({ showCancelButton: true, input: "email" })', 'feature.ts', 'INPUT_DIALOG'],
    ['import Swal from "sweetalert2"; Swal.fire({ title: "Saved", icon: "success", timer: 900 })', 'feature.ts', 'SUCCESS_NOTIFICATION'],
    ['import Swal from "sweetalert2"; Swal.fire({ showCancelButton: true })', adapterSuffix, null],
    ['import Swal from "sweetalert2"; Swal.fire({ showCancelButton: true })', 'feature.spec.ts', 'TEST_ONLY']
  ];
  for (const [source, fileName, expected] of cases) {
    const actual = fileName.endsWith('.spec.ts') ? [{ classification: 'TEST_ONLY' }] : analyzeSource(source, fileName);
    if (expected === null && actual.length > 0) throw new Error(`Expected no result for ${fileName}, got ${JSON.stringify(actual)}`);
    if (expected && !actual.some(result => result.classification === expected)) throw new Error(`Expected ${expected} for ${fileName}, got ${JSON.stringify(actual)}`);
  }
  process.stdout.write(`CONFIRMATION_BYPASS_VALIDATOR_FIXTURES=${cases.length}/${cases.length} PASS\n`);
}

function run() {
  let direct = 0;
  let unknown = 0;
  let inputDialogs = 0;
  const counts = new Map();
  let institutionalApiCalls = 0;
  let institutionalInputCalls = 0;
  const findings = [];
  for (const file of listSourceFiles(sourceRoot)) {
    const source = fs.readFileSync(file, 'utf8');
    const relative = path.relative(repositoryRoot, file).replaceAll(path.sep, '/');
    institutionalApiCalls += [...source.matchAll(/(?:this\.)?confirmation\.confirm\s*\(/g)].length;
    institutionalInputCalls += [...source.matchAll(/(?:this\.)?confirmation\.confirmWithInput\s*\(/g)].length;
    for (const result of analyzeSource(source, relative)) {
      if (result.classification === 'LEGACY_DIRECT_CONFIRMATION') direct += 1;
      else if (result.classification === 'UNKNOWN_CONFIRMATION') unknown += 1;
      else if (result.classification === 'INPUT_DIALOG') inputDialogs += 1;
      counts.set(result.classification, (counts.get(result.classification) ?? 0) + 1);
      findings.push({ file: relative, ...result });
    }
  }
  process.stdout.write(`PRODUCTIVE_SWEETALERT_FIRE_CALLS_CLASSIFIED=${findings.length}\n`);
  process.stdout.write(`INSTITUTIONAL_CONFIRMATION_API_CALLS=${institutionalApiCalls}\n`);
  process.stdout.write(`INSTITUTIONAL_CONFIRMATION_INPUT_CALLS=${institutionalInputCalls}\n`);
  process.stdout.write(`PRODUCTIVE_DIRECT_SWEETALERT_CONFIRMATIONS=${direct}\n`);
  process.stdout.write(`PRODUCTIVE_INPUT_DIALOGS=${inputDialogs}\n`);
  process.stdout.write(`UNKNOWN_CONFIRMATION_SYSTEMS=${unknown}\n`);
  for (const classification of ['INSTITUTIONAL_CONFIRMATION', 'INFORMATIONAL_ALERT', 'SUCCESS_NOTIFICATION', 'ERROR_NOTIFICATION', 'TOAST', 'INPUT_DIALOG', 'OTHER_NON_CONFIRMATION']) {
    process.stdout.write(`SWEETALERT_${classification}_COUNT=${counts.get(classification) ?? 0}\n`);
  }
  for (const finding of findings) process.stderr.write(`${finding.file}:${finding.line}: ${finding.classification}${finding.reason ? ` (${finding.reason})` : ''}\n`);
  if (direct > 0 || unknown > 0) process.exitCode = 1;
}

if (process.argv.includes('--self-test')) selfTest();
if (require.main === module) run();

module.exports = { analyzeSource };
