#!/usr/bin/env python3
"""Fail-closed institutional control for vendored CVE-patched picomatch fork."""
import hashlib
import io
import json
from pathlib import Path
import tarfile
import unittest


ROOT = Path(__file__).resolve().parents[2]
APP = ROOT / "frontend" / "rl-app"
QUALITY = ROOT / ".github" / "workflows" / "quality-gates.yml"
FOCAL = ROOT / ".github" / "workflows" / "m1-regresion-focal.yml"
TARBALL = APP / "vendor" / "picomatch-hardened-4.0.7-rl.2.tgz"
SHA256 = "54308c92475060c13fb206cde88a32fee259625343805101963e1770e0469b9a"
PATCHED_GIT_BLOB = "e6c4da2ea4e686848e2bb8b3f9ad65e4b3a236fc"
VERIFY = "node scripts/security/verify-vendored-picomatch.mjs"


class PicomatchMitigationControlTests(unittest.TestCase):
    def test_vendored_fork_has_exact_content_hash_and_source_provenance(self):
        raw = TARBALL.read_bytes()
        self.assertEqual(SHA256, hashlib.sha256(raw).hexdigest())
        with tarfile.open(fileobj=io.BytesIO(raw), mode="r:gz") as archive:
            entries = archive.getmembers()
            self.assertGreaterEqual(len(entries), 8)
            self.assertTrue(all(x.isfile() and x.name.startswith("package/")
                                and ".." not in x.name.split("/") for x in entries))
            pkg = json.load(archive.extractfile("package/package.json"))
            self.assertEqual("@riesgo-lavado/picomatch-hardened", pkg["name"])
            self.assertEqual("4.0.7-rl.2", pkg["version"])
            parse = archive.extractfile("package/lib/parse.js").read()
            obj = b"blob " + str(len(parse)).encode("ascii") + b"\0" + parse
            self.assertEqual(PATCHED_GIT_BLOB, hashlib.sha1(obj).hexdigest())
            self.assertIn(b"hasUnsafeLeadingQuantifier", parse)
            self.assertIn(b"return i > 0;", parse)
            self.assertIn(b"Copyright", archive.extractfile("package/LICENSE").read())

    def test_override_and_lock_use_exact_local_tarball_with_integrity(self):
        pkg = json.loads((APP / "package.json").read_text(encoding="utf-8"))
        lock = json.loads((APP / "package-lock.json").read_text(encoding="utf-8"))
        replacement = "file:vendor/picomatch-hardened-4.0.7-rl.2.tgz"
        self.assertEqual(replacement, pkg["overrides"]["picomatch"])
        replacement_entry = lock["packages"]["node_modules/picomatch"]
        self.assertEqual("@riesgo-lavado/picomatch-hardened", replacement_entry["name"])
        self.assertEqual("4.0.7-rl.2", replacement_entry["version"])
        self.assertEqual(replacement, replacement_entry["resolved"])
        self.assertEqual("sha256-VDCMkkdQYME/sgbN6Ioy/uJZYlNDgFEBlj4XcOBGm5o=",
                         replacement_entry["integrity"])
        self.assertFalse(any(
            x.get("name") == "picomatch"
            for x in lock["packages"].values()))
        for name in ("prebuild", "prewatch", "prestart", "pretest",
                     "pretest:coverage", "pree2e", "prelint", "preng"):
            self.assertEqual(VERIFY, pkg["scripts"][name])
        self.assertIn(VERIFY, pkg["scripts"]["security:picomatch:verify"])

    def test_full_native_audit_remains_mandatory_and_blocking(self):
        source = QUALITY.read_text(encoding="utf-8")
        verify = source.index("- name: Verify source-pinned vendored picomatch security fork")
        prod = source.index("- name: Audit production npm dependencies (blocking)")
        full = source.index("- name: Audit complete npm dependency tree")
        enforce = source.index("- name: Enforce full npm audit result")
        self.assertLess(verify, prod)
        self.assertLess(prod, full)
        self.assertLess(full, enforce)
        self.assertIn(VERIFY, source)
        self.assertIn("run: npm audit", source)
        self.assertIn("run: npm audit --omit=dev", source)
        self.assertIn("SECURITY_AUDIT=FAIL", source)
        self.assertIn("exit 1", source)
        self.assertNotIn("audit-level", source)
        self.assertNotIn("audit=false", source)
        focal = FOCAL.read_text(encoding="utf-8")
        self.assertIn(VERIFY, focal)
        self.assertIn("run: npm audit", focal)
        docker = (APP / "Dockerfile").read_text(encoding="utf-8")
        self.assertIn("COPY frontend/rl-app/vendor/picomatch-hardened-4.0.7-rl.2.tgz", docker)
        self.assertIn(VERIFY, docker)
        self.assertLess(docker.index("COPY frontend/rl-app/vendor/"), docker.index("RUN npm ci"))
        self.assertIn("RUN npm run build -- --configuration production", docker)


if __name__ == "__main__":
    unittest.main()
