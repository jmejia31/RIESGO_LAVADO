#!/usr/bin/env python3
"""Fail-closed regression guard for the comprehensive npm dependency audit."""

from pathlib import Path
import re
import unittest


ROOT = Path(__file__).resolve().parents[2]
QUALITY = ROOT / ".github" / "workflows" / "quality-gates.yml"
FOCAL = ROOT / ".github" / "workflows" / "m1-regresion-focal.yml"


class AuditContinuationTests(unittest.TestCase):
    def test_full_audit_keeps_blocking_final_result(self):
        source = QUALITY.read_text(encoding="utf-8")
        self.assertIn("name: Quality Gates", source)
        self.assertIn("run: npm ci --ignore-scripts", source)
        self.assertIn("run: npm audit --omit=dev", source)
        self.assertIn("run: npm audit", source)
        audit = source.index("- name: Audit complete npm dependency tree")
        enforce = source.index("- name: Enforce full npm audit result")
        self.assertLess(audit, enforce)
        self.assertLess(enforce, source.index("- name: Publish Block 7 browser screenshots"))
        self.assertLess(audit, source.index("- name: Run repository quality gates"))
        audit_body = source[audit:source.index("\n      - name: ", audit + 1)]
        self.assertIn("id: full_npm_audit", audit_body)
        self.assertIn("continue-on-error: true", audit_body)
        self.assertIn("run: npm audit", audit_body)
        enforce_body = source[enforce:source.index("\n      - name: ", enforce + 1)]
        for expected in ("if: always()", "steps.full_npm_audit.outcome", "SECURITY_AUDIT=FAIL", "exit 1"):
            self.assertIn(expected, enforce_body)
        self.assertNotIn("audit-level", source)
        self.assertNotIn("audit=false", source)

    def test_supplementary_m1_is_not_duplicating_full_gate_pushes(self):
        source = FOCAL.read_text(encoding="utf-8")
        trigger = source.split("\non:", 1)[1].split("\npermissions:", 1)[0]
        self.assertIn("workflow_dispatch:", trigger)
        self.assertNotIn("push:", trigger)
        self.assertIn("run: npm audit", source)


if __name__ == "__main__":
    unittest.main()
