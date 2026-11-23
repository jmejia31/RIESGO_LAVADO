#!/usr/bin/env python3
"""Fail-closed Oracle API host log audit for Block 7 disposable E2E.

TX12 intentionally raises ORA-20991 in one rollback test. Oracle can log
that exception with ORA-06512/ORA-04088 wrapper codes and OracleException.
Only those signatures in the *same log event* as the exact TX12 fault are
expected; unrelated Oracle errors and connection/timeouts remain blocking.
"""

from __future__ import annotations

import argparse
from collections import Counter
from dataclasses import dataclass
from pathlib import Path
import re
import sys

_LOG_EVENT_START = re.compile(
    r"^\s*(?:trace|trce|debug|dbug|information|info|warning|warn|error|fail|critical|crit):\s",
    re.IGNORECASE,
)
_ORACLE_TOKEN = re.compile(
    r"\bORA-(?P<ora>\d{5})\b|\b(?P<exception>OracleException)\b"
    r"|\b(?P<connect>(?:Oracle|database)[^\r\n]{0,80}(?:timeout|connection error))\b",
    re.IGNORECASE,
)
_EXPECTED_TX12 = re.compile(r"\bORA-20991:\s*TX12 disposable audit failure\b", re.IGNORECASE)
_EXPECTED_ORA = frozenset({"20991", "06512", "04088"})


@dataclass(frozen=True)
class Finding:
    signature: str
    line_number: int


def _events(text: str) -> list[list[tuple[int, str]]]:
    events: list[list[tuple[int, str]]] = []
    current: list[tuple[int, str]] = []
    for index, line in enumerate(text.splitlines(), start=1):
        if current and _LOG_EVENT_START.match(line):
            events.append(current)
            current = []
        current.append((index, line))
    if current:
        events.append(current)
    return events


def scan_text(text: str) -> tuple[int, list[Finding]]:
    """Return count of expected fault events and token-level unexpected hits."""
    expected_count = 0
    unexpected: list[Finding] = []
    for event in _events(text):
        expected = any(_EXPECTED_TX12.search(line) for _, line in event)
        if expected:
            expected_count += 1
        for line_number, line in event:
            for match in _ORACLE_TOKEN.finditer(line):
                # Timeout/connection errors are *never* expected, even in TX12.
                if match.group("connect"):
                    unexpected.append(Finding("DB_CONNECTION_OR_TIMEOUT", line_number))
                elif match.group("ora"):
                    code = match.group("ora")
                    if not (expected and code in _EXPECTED_ORA):
                        unexpected.append(Finding(f"ORA-{code}", line_number))
                elif not expected:
                    unexpected.append(Finding("OracleException", line_number))
    return expected_count, unexpected


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("api_logs", nargs="+", type=Path,
                        help="Disposable API host logs, as separate files")
    args = parser.parse_args(argv)
    all_findings: list[Finding] = []
    expected_events = 0
    for ordinal, path in enumerate(args.api_logs, 1):
        try:
            text = path.read_text(encoding="utf-8", errors="replace")
        except OSError as error:
            print(f"BLOCK7_ORACLE_LOG_READ=FAIL;HOST={ordinal};ERROR_TYPE={type(error).__name__}",
                  file=sys.stderr)
            return 1
        count, findings = scan_text(text)
        expected_events += count
        all_findings.extend(findings)
    print(f"BLOCK7_EXPECTED_TX12_ORACLE_EVENTS={expected_events}")
    if all_findings:
        signatures = Counter(f.signature for f in all_findings)
        print(f"BLOCK7_UNEXPECTED_ORACLE_ERRORS={len(all_findings)}", file=sys.stderr)
        print("BLOCK7_UNEXPECTED_ORACLE_SIGNATURES=" + ",".join(
            f"{key}:{signatures[key]}" for key in sorted(signatures)), file=sys.stderr)
        # Do not print SQL, exception messages, connection strings or request data.
        return 1
    print("BLOCK7_UNEXPECTED_ORACLE_ERRORS=0")
    return 0


if __name__ == "__main__":
    sys.exit(main())
