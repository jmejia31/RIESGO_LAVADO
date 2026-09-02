#!/usr/bin/env python3
"""Validate project Agent Skills against the core agentskills.io specification.

No third-party dependency is required. This intentionally validates the subset used
by this repository: required YAML frontmatter fields plus naming/size conventions.
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SKILLS_ROOT = ROOT / ".agents" / "skills"
NAME_RE = re.compile(r"^[a-z0-9]+(?:-[a-z0-9]+)*$")
MAX_NAME = 64
MAX_DESCRIPTION = 1024
MAX_COMPATIBILITY = 500
MAX_RECOMMENDED_LINES = 500


def parse_frontmatter(path: Path) -> tuple[dict[str, str], list[str]]:
    errors: list[str] = []
    text = path.read_text(encoding="utf-8")
    lines = text.splitlines()
    if not lines or lines[0].strip() != "---":
        return {}, ["must start with YAML frontmatter delimiter '---'"]

    try:
        end = next(i for i in range(1, len(lines)) if lines[i].strip() == "---")
    except StopIteration:
        return {}, ["frontmatter is not closed with '---'"]

    fields: dict[str, str] = {}
    for lineno, raw in enumerate(lines[1:end], start=2):
        if not raw.strip() or raw.lstrip().startswith("#"):
            continue
        if raw[:1].isspace():
            continue
        if ":" not in raw:
            errors.append(f"line {lineno}: invalid top-level frontmatter entry")
            continue
        key, value = raw.split(":", 1)
        key = key.strip()
        value = value.strip()
        if key in fields:
            errors.append(f"line {lineno}: duplicate frontmatter key '{key}'")
        fields[key] = value

    if len(lines) > MAX_RECOMMENDED_LINES:
        errors.append(
            f"has {len(lines)} lines; project policy requires <= {MAX_RECOMMENDED_LINES}"
        )
    return fields, errors


def validate_skill(skill_dir: Path) -> list[str]:
    errors: list[str] = []
    skill_file = skill_dir / "SKILL.md"
    rel = skill_dir.relative_to(ROOT)

    if not skill_file.is_file():
        return [f"{rel}: missing SKILL.md"]

    fields, parse_errors = parse_frontmatter(skill_file)
    errors.extend(f"{rel}/SKILL.md: {e}" for e in parse_errors)

    name = fields.get("name", "").strip("'\"")
    description = fields.get("description", "").strip("'\"")
    compatibility = fields.get("compatibility", "").strip("'\"")

    if not name:
        errors.append(f"{rel}/SKILL.md: missing required 'name'")
    else:
        if len(name) > MAX_NAME:
            errors.append(f"{rel}/SKILL.md: name exceeds {MAX_NAME} characters")
        if not NAME_RE.fullmatch(name):
            errors.append(
                f"{rel}/SKILL.md: name must contain only lowercase letters, numbers and single hyphens"
            )
        if name != skill_dir.name:
            errors.append(
                f"{rel}/SKILL.md: name '{name}' must match parent directory '{skill_dir.name}'"
            )

    if not description:
        errors.append(f"{rel}/SKILL.md: missing required 'description'")
    elif len(description) > MAX_DESCRIPTION:
        errors.append(
            f"{rel}/SKILL.md: description exceeds {MAX_DESCRIPTION} characters"
        )

    if compatibility and len(compatibility) > MAX_COMPATIBILITY:
        errors.append(
            f"{rel}/SKILL.md: compatibility exceeds {MAX_COMPATIBILITY} characters"
        )

    return errors


def main() -> int:
    if not SKILLS_ROOT.is_dir():
        print(f"ERROR: skills root does not exist: {SKILLS_ROOT}")
        return 1

    skill_dirs = sorted(
        p for p in SKILLS_ROOT.iterdir()
        if p.is_dir() and not p.name.startswith(".")
    )
    if not skill_dirs:
        print("ERROR: no Agent Skills found")
        return 1

    errors: list[str] = []
    names: set[str] = set()
    for skill_dir in skill_dirs:
        skill_errors = validate_skill(skill_dir)
        errors.extend(skill_errors)
        if not skill_errors:
            name = skill_dir.name
            if name in names:
                errors.append(f"duplicate skill name: {name}")
            names.add(name)

    if errors:
        print("Agent Skills validation: FAIL")
        for error in errors:
            print(f" - {error}")
        return 1

    print(f"Agent Skills validation: PASS ({len(skill_dirs)} skills)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
