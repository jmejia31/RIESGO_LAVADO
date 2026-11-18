#!/usr/bin/env node
/**
 * Reproducible, fail-closed developer-tooling mitigation for CVE-2026-112366.
 * Source: picomatch 4.0.7 (MIT), micromatch/picomatch PR #238 (unmerged).
 * Do not update package metadata or weaken the separate full npm audit.
 */
import { createHash } from 'node:crypto';
import { readFile, rename, unlink, writeFile } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const ORIGINAL_BLOB = '29207512c60183b4bb9bf5112be3b9b5f4a9a9fd';
const BACKPORT_BLOB = '9f968c0681bc08d7383c1be29f376a2caf6c60e4';
const root = resolve(dirname(fileURLToPath(import.meta.url)), '../..');
const packageDirectory = resolve(root, 'node_modules/picomatch');
const sourcePath = resolve(packageDirectory, 'lib/parse.js');
const packagePath = resolve(packageDirectory, 'package.json');

function gitBlobHash(content) {
  const data = Buffer.from(content, 'utf8');
  const header = Buffer.from('blob ' + data.length + '\0', 'utf8');
  return createHash('sha1').update(header).update(data).digest('hex');
}

function replaceExactlyOnce(content, marker, replacement) {
  if (content.split(marker).length !== 2) {
    throw new Error('PICOMATCH_MITIGATION=FAIL_UNEXPECTED_SOURCE_STRUCTURE');
  }
  return content.replace(marker, replacement);
}

function applyUpstreamCandidate(source) {
  // Backport the narrowly scoped logic of PR #238; license and attribution
  // remain with picomatch's upstream MIT-licensed authors.
  const helper = [
    '// Restricted backport of micromatch/picomatch PR #238 (not yet an upstream release).',
    "// CVE-2026-112366: literalize repeated extglob branches with an unanchored",
    "// leading bare '*' or '?' followed by additional pattern content.",
    'const hasUnsafeLeadingQuantifier = branch => {',
    '  let i = 0;',
    '',
    "  while (i < branch.length && (branch[i] === '*' || branch[i] === '?') && branch[i + 1] !== '(') {",
    '    i++;',
    '  }',
    '',
    '  return i > 0 && i < branch.length;',
    '};',
    '',
    ''
  ].join('\n');

  const loop = [
    '    combinable = false;',
    '',
    '    if (repeatedExtglobRecursion(branch) > max) {'
  ].join('\n');
  const hardenedLoop = [
    '    combinable = false;',
    '',
    '    if (hasUnsafeLeadingQuantifier(branch)) {',
    '      return { risky: true };',
    '    }',
    '',
    '    if (repeatedExtglobRecursion(branch) > max) {'
  ].join('\n');

  const withHelper = replaceExactlyOnce(
    source, 'const repeatedExtglobRecursion = pattern => {',
    helper + 'const repeatedExtglobRecursion = pattern => {'
  );
  return replaceExactlyOnce(withHelper, loop, hardenedLoop);
}

async function main() {
  const mode = process.argv[2];
  if (!['--apply', '--verify'].includes(mode) || process.argv.length !== 3) {
    throw new Error('Expected --apply or --verify; no bypass mode exists.');
  }

  const pkg = JSON.parse(await readFile(packagePath, 'utf8'));
  if (pkg.name !== 'picomatch' || pkg.version !== '4.0.7') {
    throw new Error('PICOMATCH_MITIGATION=FAIL_UNEXPECTED_PACKAGE_OR_VERSION');
  }

  const source = await readFile(sourcePath, 'utf8');
  const actual = gitBlobHash(source);
  if (actual === BACKPORT_BLOB) {
    console.log('PICOMATCH_MITIGATION=PASS_PINNED_BACKPORT_VERIFIED');
    return;
  }
  if (actual !== ORIGINAL_BLOB) {
    throw new Error('PICOMATCH_MITIGATION=FAIL_UNRECOGNIZED_SOURCE_SHA ' + actual);
  }
  if (mode === '--verify') {
    throw new Error('PICOMATCH_MITIGATION=FAIL_UNPATCHED_SOURCE');
  }

  const patched = applyUpstreamCandidate(source);
  if (gitBlobHash(patched) !== BACKPORT_BLOB) {
    throw new Error('PICOMATCH_MITIGATION=FAIL_PATCH_SHA_MISMATCH');
  }

  const tempPath = sourcePath + '.mitigation-tmp-' + process.pid;
  try {
    await writeFile(tempPath, patched, { encoding: 'utf8', flag: 'wx' });
    await rename(tempPath, sourcePath);
  } finally {
    await unlink(tempPath).catch(error => {
      if (error?.code !== 'ENOENT') throw error;
    });
  }

  if (gitBlobHash(await readFile(sourcePath, 'utf8')) !== BACKPORT_BLOB) {
    throw new Error('PICOMATCH_MITIGATION=FAIL_POST_WRITE_VERIFY');
  }
  console.log('PICOMATCH_MITIGATION=PASS_PINNED_BACKPORT_APPLIED');
  console.log('PICOMATCH_WARNING=UPSTREAM_PR_238_UNMERGED_FULL_NPM_AUDIT_REMAINS_BLOCKING');
}

await main();
