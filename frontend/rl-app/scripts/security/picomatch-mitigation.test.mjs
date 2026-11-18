import assert from 'node:assert/strict';
import { execFileSync, spawnSync } from 'node:child_process';
import { dirname, resolve } from 'node:path';
import { test } from 'node:test';
import { fileURLToPath } from 'node:url';
import picomatch from 'picomatch';

const projectRoot = resolve(dirname(fileURLToPath(import.meta.url)), '../..');
const mitigationScript = resolve(projectRoot, 'scripts/security/picomatch-mitigation.mjs');

test('only pinned upstream 4.0.7 with the exact reviewed backport is accepted', () => {
  const output = execFileSync(process.execPath, [mitigationScript, '--verify'], {
    cwd: projectRoot, encoding: 'utf8', timeout: 4000
  });
  assert.match(output, /PICOMATCH_MITIGATION=PASS_PINNED_BACKPORT_VERIFIED/);
});

test('CVE-2026-112366: risky single-branch repeated extglobs become safe literals', () => {
  for (const pattern of ['+(*a)', '*(*a)', '+(a|*a)']) {
    const expression = picomatch.makeRe(pattern);
    assert.equal(expression.test(pattern), true, 'preserves literal form: ' + pattern);
    assert.equal(expression.test('a'.repeat(25) + '!'), false, 'no crafted match: ' + pattern);
    assert.equal(expression.test('aaa'), false, 'not an unbounded nested regex: ' + pattern);
    assert.equal(expression.source.includes('[^/]*?a)+'), false, 'no known vulnerable source: ' + pattern);
  }
});

test('safe glob shapes used by build/test tooling retain their behavior', () => {
  for (const [input, pattern, expected] of [
    ['abc', '+(*)', true],
    ['abc', '+(a*)', true],
    ['aaa', '+(a*)', true],
    ['component.ts', '*.ts', true],
    ['src/component.ts', '**/*.ts', true],
    ['component.js', '*.ts', false]
  ]) {
    assert.equal(picomatch.isMatch(input, pattern), expected, input + ' / ' + pattern);
  }
});

test('untrusted ReDoS patterns do not block a separate Node event loop', () => {
  const smoke = [
    "const pm = require('picomatch');",
    "for (const pattern of ['+(*a)', '*(*a)', '+(a|*a)']) {",
    "  if (pm.isMatch('a'.repeat(100) + '!', pattern)) process.exit(3);",
    "}",
  ].join('\n');
  const child = spawnSync(process.execPath, ['-e', smoke], {
    cwd: projectRoot, encoding: 'utf8', timeout: 5000, maxBuffer: 1024 * 1024
  });
  assert.ifError(child.error);
  assert.equal(child.status, 0, child.stderr || child.stdout);
});
