#!/usr/bin/env node
'use strict';

const fs = require('node:fs');
const path = require('node:path');
const ts = require(path.join(__dirname, '..', 'frontend', 'rl-app', 'node_modules', 'typescript'));

const sourceRoot = path.join(__dirname, '..', 'frontend', 'rl-app', 'src', 'app');
const nativeNames = new Set(['alert', 'confirm', 'prompt']);

function findNativeDialogs(source, fileName = 'fixture.ts') {
  const sourceFile = ts.createSourceFile(fileName, source, ts.ScriptTarget.Latest, true);
  const matches = [];

  function visit(node) {
    if (ts.isCallExpression(node)) {
      const expression = node.expression;
      if (ts.isIdentifier(expression) && nativeNames.has(expression.text)) {
        matches.push({ name: expression.text, line: sourceFile.getLineAndCharacterOfPosition(node.getStart(sourceFile)).line + 1 });
      } else if (ts.isPropertyAccessExpression(expression) && nativeNames.has(expression.name.text)
        && ts.isIdentifier(expression.expression)
        && ['window', 'globalThis', 'self'].includes(expression.expression.text)) {
        matches.push({ name: `${expression.expression.text}.${expression.name.text}`, line: sourceFile.getLineAndCharacterOfPosition(node.getStart(sourceFile)).line + 1 });
      }
    }
    ts.forEachChild(node, visit);
  }

  visit(sourceFile);
  return matches;
}

function listSourceFiles(directory) {
  return fs.readdirSync(directory, { withFileTypes: true }).flatMap(entry => {
    const fullPath = path.join(directory, entry.name);
    if (entry.isDirectory()) return listSourceFiles(fullPath);
    if (!entry.isFile() || !/\.(?:ts|js|html)$/.test(entry.name) || /(?:\.spec|\.test|\.e2e)\.(?:ts|js)$/.test(entry.name)) return [];
    return [fullPath];
  });
}

function findTemplateNativeDialogs(source, fileName) {
  const withoutComments = source.replace(/<!--[\s\S]*?-->/g, '');
  const results = [];
  const eventAttribute = /(?:\([^)]+\)|on[a-z]+)\s*=\s*(["'])(.*?)\1/gs;
  for (const match of withoutComments.matchAll(eventAttribute)) {
    const line = withoutComments.slice(0, match.index).split(/\r?\n/).length;
    results.push(...findNativeDialogs(match[2], fileName).map(found => ({ ...found, line: line + found.line - 1 })));
  }
  return results;
}

function selfTest() {
  const cases = [
    ['alert("message")', 1],
    ['window.confirm("dirty")', 1],
    ['globalThis.prompt("value")', 1],
    ['// confirm("comment")\nconst text = "alert(\\"literal\\")"; service.confirm()', 0]
  ];
  for (const [source, expected] of cases) {
    const actual = findNativeDialogs(source).length;
    if (actual !== expected) throw new Error(`Fixture failed: expected ${expected} native dialog calls, found ${actual}`);
  }
  const templateCases = [
    ['<button (click)="window.confirm(\'dirty\')">Close</button>', 1],
    ['<!-- <button (click)="confirm(\'ignored\')">comment</button> -->', 0]
  ];
  for (const [source, expected] of templateCases) {
    const actual = findTemplateNativeDialogs(source, 'fixture.html').length;
    if (actual !== expected) throw new Error(`Template fixture failed: expected ${expected} native dialog calls, found ${actual}`);
  }
  const fixtureCount = cases.length + templateCases.length;
  process.stdout.write(`NATIVE_DIALOG_VALIDATOR_FIXTURES=${fixtureCount}/${fixtureCount} PASS\n`);
}

if (process.argv.includes('--self-test')) selfTest();

const findings = listSourceFiles(sourceRoot).flatMap(file => {
  const source = fs.readFileSync(file, 'utf8');
  const matches = file.endsWith('.html') ? findTemplateNativeDialogs(source, file) : findNativeDialogs(source, file);
  return matches.map(match => ({ file: path.relative(path.join(__dirname, '..'), file).replaceAll(path.sep, '/'), ...match }));
});

for (const name of ['alert', 'confirm', 'prompt']) {
  const count = findings.filter(finding => finding.name === name || finding.name.endsWith(`.${name}`)).length;
  process.stdout.write(`PRODUCTION_NATIVE_${name.toUpperCase()}_USAGE=${count}\n`);
}
process.stdout.write(`PRODUCTION_NATIVE_DIALOG_USAGE=${findings.length}\n`);
for (const finding of findings) process.stderr.write(`${finding.file}:${finding.line}: native ${finding.name} call\n`);
if (findings.length > 0) process.exitCode = 1;
