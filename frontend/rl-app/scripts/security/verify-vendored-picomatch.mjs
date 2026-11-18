#!/usr/bin/env node
/**
 * Fail-closed verification of the repo-owned picomatch security fork:
 * package alias -> local immutable SRI tarball -> original upstream MIT source
 * with reviewed upstream candidate PR #238 backport (not an official release).
 * This MUST NOT turn a failed native npm audit into a passed security scan.
 */
import { createHash } from 'node:crypto';
import { readFile } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '../..');
const tarballRel = 'vendor/picomatch-hardened-4.0.7-rl.2.tgz';
const tarballSRI = 'sha256-VDCMkkdQYME/sgbN6Ioy/uJZYlNDgFEBlj4XcOBGm5o=';
const packageName = '@riesgo-lavado/picomatch-hardened';
const originalGitBlob = '29207512c60183b4bb9bf5112be3b9b5f4a9a9fd';
const fixedGitBlob = 'e6c4da2ea4e686848e2bb8b3f9ad65e4b3a236fc';

function gitBlobHash(data) {
  const content = Buffer.from(data, 'utf8');
  return createHash('sha1')
    .update(Buffer.from('blob ' + content.length + '\0', 'utf8'))
    .update(content)
    .digest('hex');
}

const tarball = await readFile(resolve(root, tarballRel));
const actualSRI = 'sha256-' + createHash('sha256').update(tarball).digest('base64');
if (actualSRI !== tarballSRI) {
  throw new Error('PICOMATCH_FORK=FAIL_UNEXPECTED_VENDOR_TARBALL_SHA256');
}

const pkg = JSON.parse(await readFile(resolve(root, 'node_modules/picomatch/package.json'), 'utf8'));
const lock = JSON.parse(await readFile(resolve(root, 'package-lock.json'), 'utf8'));
const rootPackage = JSON.parse(await readFile(resolve(root, 'package.json'), 'utf8'));
const entry = lock.packages?.['node_modules/picomatch'];

if (pkg.name !== packageName || pkg.version !== '4.0.7-rl.2'
  || pkg.rlProvenance?.upstreamTag !== '4.0.7'
  || pkg.rlProvenance?.upstreamParseGitBlob !== originalGitBlob
  || pkg.rlProvenance?.backportParseGitBlob !== fixedGitBlob
  || rootPackage.overrides?.picomatch !== 'file:' + tarballRel
  || entry?.name !== packageName
  || entry?.version !== pkg.version
  || entry?.resolved !== 'file:' + tarballRel
  || entry?.integrity !== tarballSRI) {
  throw new Error('PICOMATCH_FORK=FAIL_UNEXPECTED_PACKAGE_OR_LOCK_PROVENANCE');
}

const js = await readFile(resolve(root, 'node_modules/picomatch/lib/parse.js'), 'utf8');
if (gitBlobHash(js) !== fixedGitBlob || !js.includes('hasUnsafeLeadingQuantifier') || !js.includes('return i > 0;')) {
  throw new Error('PICOMATCH_FORK=FAIL_SOURCE_NOT_FIXED_OR_NOT_APPROVED');
}

console.log('PICOMATCH_FORK=PASS_ORIGIN_AND_TARBALL_VERIFIED');
console.log('PICOMATCH_FORK_SOURCE=micromatch/picomatch@4.0.7+unmerged_PR_238');
console.log('PICOMATCH_FORK_ADVISORY=GHSA-fc5q-39xc-vjpf');
