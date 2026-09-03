#!/usr/bin/env bash
set -euo pipefail

if ! command -v git >/dev/null 2>&1; then
  echo 'ERROR: git no esta disponible en PATH.' >&2
  exit 1
fi

repo_root="$(git rev-parse --show-toplevel 2>/dev/null || true)"
if [[ -z "$repo_root" ]]; then
  echo 'ERROR: ejecuta este script dentro del checkout de RIESGO_LAVADO.' >&2
  exit 1
fi
cd "$repo_root"

if [[ ! -d .agents/skills ]]; then
  echo 'ERROR: no existe .agents/skills. Actualiza origin/desarrollo.' >&2
  exit 1
fi

origin_url="$(git remote get-url origin)"
if [[ ! "$origin_url" =~ jmejia31[/:]RIESGO_LAVADO(\.git)?$ ]]; then
  echo "ERROR: origin no apunta a jmejia31/RIESGO_LAVADO: $origin_url" >&2
  exit 1
fi

branch="$(git branch --show-current)"
if [[ "$branch" != 'desarrollo' ]]; then
  echo "ERROR: rama actual '$branch'. Cambia a 'desarrollo'." >&2
  exit 1
fi

git fetch origin desarrollo

ahead="$(git rev-list --count origin/desarrollo..HEAD)"
behind="$(git rev-list --count HEAD..origin/desarrollo)"
dirty="$(git status --porcelain)"

if [[ "$behind" -gt 0 ]]; then
  if [[ -n "$dirty" || "$ahead" -gt 0 ]]; then
    echo "ERROR: sincronizacion no segura: ahead=$ahead behind=$behind dirty=yes. Integra el trabajo manualmente." >&2
    exit 1
  fi
  git pull --ff-only origin desarrollo
  ahead="$(git rev-list --count origin/desarrollo..HEAD)"
  behind="$(git rev-list --count HEAD..origin/desarrollo)"
fi

git config core.hooksPath .githooks

if command -v python3 >/dev/null 2>&1; then
  python3 tools/validate_agent_skills.py
elif command -v python >/dev/null 2>&1; then
  python tools/validate_agent_skills.py
else
  echo 'ERROR: Python 3 es requerido para validar Agent Skills.' >&2
  exit 1
fi

skill_count="$(find .agents/skills -mindepth 2 -maxdepth 2 -name SKILL.md -type f | wc -l | tr -d ' ')"
head_sha="$(git rev-parse HEAD)"
hooks_path="$(git config --get core.hooksPath)"

echo
echo '=== RIESGO_LAVADO Agent Skills ACTIVADO ==='
echo "Repositorio : $repo_root"
echo "Rama        : $branch"
echo "HEAD        : $head_sha"
echo "Ahead/Behind: $ahead/$behind"
echo "Skills      : $skill_count"
echo "Git hooks   : $hooks_path"
echo
echo 'Siguiente paso: reinicia la sesion del agente y abre Codex o Antigravity desde la raiz del repositorio.'
