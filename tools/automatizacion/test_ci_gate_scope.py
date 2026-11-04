#!/usr/bin/env python3
"""Independent regression checks for CI scope routing and state validation."""

import json
import tempfile
import unittest
from pathlib import Path

from ci_gate_scope import CONTROL_ONLY_PATHS, control_only, validate_control_json

BEFORE = "a" * 40
HEAD = "b" * 40
CTRL = "docs/automatizacion/ESTADO_TAREAS.json"
QUEUE = "docs/automatizacion/TAREAS_CODEX.json"
PROOF = "docs/automatizacion/ISSUE24_ACCEPTANCE_PROOF.txt"


class RoutingTests(unittest.TestCase):
    def route(self, changed=None, **kwargs):
        a = dict(event="push", ref="refs/heads/desarrollo", before=BEFORE,
                 head=HEAD, changed=[CTRL] if changed is None else changed,
                 is_ancestor=True, forced=False)
        a.update(kwargs)
        return control_only(**a)

    def test_single_control(self):
        self.assertTrue(self.route())

    def test_multiple_controls(self):
        self.assertTrue(self.route(changed=[CTRL, QUEUE, PROOF]))

    def test_only_allowlisted(self):
        self.assertEqual(len(CONTROL_ONLY_PATHS), 3)

    def test_main_preserved(self):
        self.assertFalse(self.route(ref="refs/heads/main"))

    def test_pull_request_preserved(self):
        self.assertFalse(self.route(event="pull_request"))

    def test_manual_dispatch_preserved(self):
        self.assertFalse(self.route(event="workflow_dispatch"))

    def test_functional_mixed_preserved(self):
        self.assertFalse(self.route(changed=[CTRL, "backend/RL.API/Program.cs"]))

    def test_workflow_changes_preserved(self):
        self.assertFalse(self.route(changed=[".github/workflows/quality-gates.yml"]))

    def test_security_schema_changes_preserved(self):
        self.assertFalse(self.route(changed=["docs/automatizacion/TAREAS_CODEX.schema.json"]))

    def test_documentary_certification_preserved(self):
        self.assertFalse(self.route(changed=["docs/certificaciones/P0-2/INDICE.md"]))

    def test_path_prefix_impostor_preserved(self):
        self.assertFalse(self.route(changed=[CTRL + ".bak"]))

    def test_force_push_preserved(self):
        self.assertFalse(self.route(forced=True))

    def test_non_ancestor_preserved(self):
        self.assertFalse(self.route(is_ancestor=False))

    def test_diff_error_preserved(self):
        self.assertFalse(self.route(changed=None, is_ancestor=False))

    def test_empty_diff_preserved(self):
        self.assertFalse(self.route(changed=[]))

    def test_unborn_branch_preserved(self):
        self.assertFalse(self.route(before="0" * 40))

    def test_invalid_sha_preserved(self):
        self.assertFalse(self.route(head="not-a-sha"))

    def test_same_commit_preserved(self):
        self.assertFalse(self.route(head=BEFORE))


class ControlJsonTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        p = self.root / "docs/automatizacion"
        p.mkdir(parents=True)
        self.queue = {"repository": "jmejia31/RIESGO_LAVADO", "branch": "desarrollo",
                      "queue_mode": "DRY_RUN", "tasks": []}
        self.state = {"repository": "jmejia31/RIESGO_LAVADO", "branch": "desarrollo",
                      "states": [], "controls": {"auto_approve": False}}
        self.write()

    def write(self):
        folder = self.root / "docs/automatizacion"
        (folder / "TAREAS_CODEX.json").write_text(json.dumps(self.queue), encoding="utf-8")
        (folder / "ESTADO_TAREAS.json").write_text(json.dumps(self.state), encoding="utf-8-sig")

    def test_valid_empty_state_with_utf8_bom(self):
        validate_control_json(self.root)

    def test_valid_pending_review(self):
        self.state["states"] = [{"task_id": "ISSUE24-SYNTHETIC-001", "attempt": 1,
                                  "status": "IMPLEMENTED_PENDING_REVIEW", "review_required": True}]
        self.write()
        validate_control_json(self.root)

    def test_autoapproval_forbidden(self):
        self.state["controls"]["auto_approve"] = True
        self.write()
        with self.assertRaises(ValueError):
            validate_control_json(self.root)

    def test_duplicate_state_forbidden(self):
        e = {"task_id": "X", "attempt": 1, "status": "RUNNING"}
        self.state["states"] = [e, e]
        self.write()
        with self.assertRaises(ValueError):
            validate_control_json(self.root)

    def test_invalid_state_forbidden(self):
        self.state["states"] = [{"task_id": "X", "attempt": 1, "status": "BOGUS"}]
        self.write()
        with self.assertRaises(ValueError):
            validate_control_json(self.root)

    def test_implemented_without_review_forbidden(self):
        self.state["states"] = [{"task_id": "X", "attempt": 1,
                                  "status": "IMPLEMENTED_PENDING_REVIEW", "review_required": False}]
        self.write()
        with self.assertRaises(ValueError):
            validate_control_json(self.root)

    def test_malformed_json_forbidden(self):
        (self.root / "docs/automatizacion/ESTADO_TAREAS.json").write_text("not json", encoding="utf-8")
        with self.assertRaises(ValueError):
            validate_control_json(self.root)


if __name__ == "__main__":
    unittest.main()
