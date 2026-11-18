#!/usr/bin/env python3
"""CI contract: a source-pinned mitigation never substitutes the complete audit."""
import json
from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[2]
APP = ROOT / "frontend" / "rl-app"
QUALITY = ROOT / ".github" / "workflows" / "quality-gates.yml"
FOCAL = ROOT / ".github" / "workflows" / "m1-regresion-focal.yml"


class PicomatchMitigationControlTests(unittest.TestCase):
    def test_verifiable_mitigation_runs_before_unmodified_full_audit(self):
        source = QUALITY.read_text(encoding="utf-8")
        patch = source.index("- name: Apply and test pinned picomatch ReDoS mitigation")
        prod = source.index("- name: Audit production npm dependencies (blocking)")
        full = source.index("- name: Audit complete npm dependency tree")
        enforce = source.index("- name: Enforce full npm audit result")
        self.assertLess(patch, prod)
        self.assertLess(prod, full)
        self.assertLess(full, enforce)
        self.assertIn("node scripts/security/picomatch-mitigation.mjs --apply", source)
        self.assertIn("node --test scripts/security/picomatch-mitigation.test.mjs", source)
        self.assertIn("run: npm audit", source)
        self.assertIn("SECURITY_AUDIT=FAIL", source)
        self.assertIn("exit 1", source)

    def test_immutable_local_backport_is_not_a_registry_override(self):
        pkg = json.loads((APP / "package.json").read_text(encoding="utf-8"))
        self.assertNotIn("picomatch", pkg.get("overrides", {}))
        expected = "node scripts/security/picomatch-mitigation.mjs --apply"
        for key in ("prebuild", "prewatch", "prestart", "pretest",
                    "pretest:coverage", "pree2e", "prelint", "preng"):
            self.assertEqual(expected, pkg["scripts"][key])
        script = (APP / "scripts/security/picomatch-mitigation.mjs").read_text(encoding="utf-8")
        self.assertIn("29207512c60183b4bb9bf5112be3b9b5f4a9a9fd", script)
        self.assertIn("9f968c0681bc08d7383c1be29f376a2caf6c60e4", script)
        self.assertIn("FAIL_UNRECOGNIZED_SOURCE_SHA", script)
        self.assertIn("FAIL_UNPATCHED_SOURCE", script)
        self.assertIn("pkg.version !== '4.0.7'", script)
        focal = FOCAL.read_text(encoding="utf-8")
        self.assertIn(expected, focal)
        self.assertIn("run: npm audit", focal)
        docker = (APP / "Dockerfile").read_text(encoding="utf-8")
        self.assertIn(expected, docker)
        self.assertIn("RUN npm run build -- --configuration production", docker)


if __name__ == "__main__":
    unittest.main()
