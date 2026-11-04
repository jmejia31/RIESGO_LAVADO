#!/usr/bin/env python3
"""Fail-closed path router for the RIESGO_LAVADO GitHub Actions quality gate.

Only exact, non-executable control artifacts on development pushes may skip
full Oracle/.NET/Angular/container CI. Main, PRs, manual runs, unknown or
ambiguous diffs always receive the full gate.
"""

from __future__ import annotations

import json
import os
import re
import subprocess
import sys
from pathlib import Path

CONTROL_ONLY_PATHS = frozenset(
    {
        "docs/automatizacion/TAREAS_CODEX.json",
        "docs/automatizacion/ESTADO_TAREAS.json",
        "docs/automatizacion/ISSUE24_ACCEPTANCE_PROOF.txt",
    }
)
SHA = re.compile(r"[0-9a-f]{40}\Z")
ALLOWED_STATES = frozenset(
    {
        "PENDING_AUTHORIZATION", "READY", "CLAIMED", "RUNNING",
        "IMPLEMENTED_PENDING_REVIEW", "CHANGES_REQUESTED", "APPROVED",
        "BLOCKED", "FAILED", "CANCELLED",
    }
)


def control_only(event: str, ref: str, before: str, head: str,
                 changed: list[str] | None, is_ancestor: bool,
                 forced: bool = False) -> bool:
    """Never suppress the full gate unless every condition is known-safe."""
    return (
        event == "push"
        and ref == "refs/heads/desarrollo"
        and not forced
        and bool(SHA.fullmatch(before or ""))
        and bool(SHA.fullmatch(head or ""))
        and before != "0" * 40
        and head != "0" * 40
        and before != head
        and is_ancestor
        and bool(changed)
        and all(path in CONTROL_ONLY_PATHS for path in changed)
    )


def git_changes(before: str, head: str) -> tuple[list[str] | None, bool]:
    """Unknown history, diff failures and undecodable paths fail to full CI."""
    try:
        ancestor = subprocess.run(
            ["git", "merge-base", "--is-ancestor", before, head],
            check=False, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
        )
        if ancestor.returncode != 0:
            return None, False
        diff = subprocess.run(
            ["git", "diff", "--no-ext-diff", "--no-renames",
             "--name-only", "-z", before, head],
            check=True, capture_output=True,
        )
        paths = [p.decode("utf-8") for p in diff.stdout.split(b"\0") if p]
        return paths, True
    except (OSError, subprocess.SubprocessError, UnicodeError):
        return None, False


def validate_control_json(root: Path) -> None:
    """Quick structural checks; the canonical PowerShell queue validator also runs."""
    def read_json(rel: str) -> dict:
        with (root / rel).open("r", encoding="utf-8-sig") as handle:
            data = json.load(handle)
        if not isinstance(data, dict):
            raise ValueError(f"{rel}: expected JSON object")
        return data

    queue = read_json("docs/automatizacion/TAREAS_CODEX.json")
    state = read_json("docs/automatizacion/ESTADO_TAREAS.json")
    if queue.get("repository") != "jmejia31/RIESGO_LAVADO" or queue.get("branch") != "desarrollo":
        raise ValueError("queue repository/branch mismatch")
    if queue.get("queue_mode") not in {"DRY_RUN", "CONTROLLED"}:
        raise ValueError("queue mode invalid")
    if not isinstance(queue.get("tasks"), list):
        raise ValueError("tasks must be array")
    if state.get("repository") != "jmejia31/RIESGO_LAVADO" or state.get("branch") != "desarrollo":
        raise ValueError("state repository/branch mismatch")
    controls = state.get("controls")
    if not isinstance(controls, dict) or controls.get("auto_approve") is not False:
        raise ValueError("automatic approval must remain disabled")
    entries = state.get("states")
    if not isinstance(entries, list):
        raise ValueError("states must be array")
    seen = set()
    for entry in entries:
        if not isinstance(entry, dict) or not isinstance(entry.get("task_id"), str):
            raise ValueError("invalid persisted task")
        if entry["task_id"] in seen:
            raise ValueError("duplicate persisted task")
        seen.add(entry["task_id"])
        if entry.get("status") not in ALLOWED_STATES:
            raise ValueError("invalid persisted task status")
        if type(entry.get("attempt")) is not int or entry["attempt"] < 1:
            raise ValueError("invalid persisted attempt")
        if entry["status"] == "IMPLEMENTED_PENDING_REVIEW" and entry.get("review_required") is not True:
            raise ValueError("implemented task must require review")


def main() -> int:
    root = Path(os.environ.get("GITHUB_WORKSPACE", os.getcwd()))
    event = os.environ.get("GITHUB_EVENT_NAME", "")
    ref = os.environ.get("GITHUB_REF", "")
    before = os.environ.get("PUSH_BEFORE_SHA", "")
    head = os.environ.get("GITHUB_SHA", "")
    forced = os.environ.get("PUSH_FORCED", "").lower() == "true"

    if "--validate-control" in sys.argv:
        validate_control_json(root)
        print("CONTROL_JSON_VALIDATION=PASS")
        return 0

    changed, ancestor = (None, False)
    if event == "push" and ref == "refs/heads/desarrollo" and not forced \
            and SHA.fullmatch(before) and SHA.fullmatch(head):
        changed, ancestor = git_changes(before, head)

    fast = control_only(event, ref, before, head, changed, ancestor, forced)
    scope = "CONTROL_ONLY" if fast else "FULL"
    print(f"CI_SCOPE={scope}")
    print(f"FULL_GATE_REQUIRED={'NO' if fast else 'YES'}")
    output = os.environ.get("GITHUB_OUTPUT")
    if not output:
        raise RuntimeError("GITHUB_OUTPUT is required in Actions mode")
    with open(output, "a", encoding="utf-8") as handle:
        handle.write(f"run_full={'false' if fast else 'true'}\n")
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (OSError, ValueError, RuntimeError) as error:
        print(f"CI_SCOPE_VALIDATION=FAIL: {type(error).__name__}: {error}", file=sys.stderr)
        sys.exit(2)
