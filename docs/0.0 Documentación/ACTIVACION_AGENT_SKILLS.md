# Activación operativa de Agent Skills — RIESGO_LAVADO

## Objetivo

Hacer que cada checkout local y cada colaborador compatible consuma la misma biblioteca `.agents/skills/`, aplique `AGENTS.md` y valide las reglas antes de publicar cambios.

## 1. Activación una sola vez por checkout

### Windows / PowerShell

```powershell
cd C:\RIESGO_LAVADO
git switch desarrollo
git pull --ff-only origin desarrollo
powershell -NoProfile -ExecutionPolicy Bypass -File tools/activate_agent_skills.ps1
```

### macOS / Linux

```bash
cd /ruta/RIESGO_LAVADO
git switch desarrollo
git pull --ff-only origin desarrollo
bash tools/activate_agent_skills.sh
```

El activador:

1. verifica que el remote `origin` sea el repositorio oficial;
2. exige la rama `desarrollo`;
3. sincroniza por `fast-forward` cuando es seguro;
4. configura `git config core.hooksPath .githooks`;
5. ejecuta `tools/validate_agent_skills.py`;
6. informa HEAD, divergencia y cantidad de skills.

Si existe trabajo local, divergencia o un estado que pueda causar pérdida, falla de forma segura y exige reconciliación manual.

## 2. Qué hacen los Git hooks

### `pre-commit`

- bloquea commits ordinarios en `main`;
- exige que `AGENTS.md` y `.agents/AGENTS.md` se actualicen juntos;
- valida todas las Agent Skills antes del commit.

### `pre-push`

- valida todas las Agent Skills antes de publicar;
- bloquea pushes directos a `main`.

Los hooks son defensa local. La autorización extraordinaria se puede omitir técnicamente con `--no-verify`, pero el protocolo solo permite hacerlo con autorización expresa de Javier Mejía.

## 3. Activación por cliente

### Codex / Codex CLI

Después de ejecutar el activador, iniciar una **sesión nueva desde la raíz del repositorio**. `AGENTS.md` es la instrucción transversal y `.agents/skills/` contiene las skills del proyecto. No copiar las skills a una carpeta privada como fuente maestra.

Al iniciar una intervención, Codex debe aplicar `riesgo-lavado-continuidad` y las skills cuyo `description` coincida con la tarea.

### Antigravity

Abrir `C:\RIESGO_LAVADO` como workspace y comenzar una sesión nueva. Antigravity descubre las skills de workspace alojadas en `<workspace-root>/.agents/skills/` y carga las instrucciones completas cuando son relevantes.

### ChatGPT

ChatGPT trabajando mediante este repositorio debe respetar `AGENTS.md` y leer las `SKILL.md` aplicables. La instalación nativa y compartida de Skills dentro de la interfaz de ChatGPT depende del plan/workspace; no debe confundirse con las skills versionadas del repositorio que usan Codex/Antigravity.

## 4. Comprobación rápida al abrir una sesión

El colaborador puede recibir esta instrucción de control:

```text
Antes de trabajar en RIESGO_LAVADO: confirma rama y HEAD, lee AGENTS.md, recupera el punto de continuidad y enumera las Agent Skills de .agents/skills que aplican a esta tarea. Después ejecuta el trabajo sin reescanear innecesariamente el repositorio.
```

No es un prompt maestro: solo verifica que el cliente está consumiendo el protocolo versionado.

## 5. Protección remota GitHub

Los hooks protegen los checkouts donde se ejecutó el activador, pero no sustituyen una regla del servidor. Para protección fuerte de `main`, GitHub debe tener un Ruleset/Branch Protection activo que impida pushes directos y exija PR/checks autorizados.

## 6. Flujo diario

```text
git fetch origin
git switch desarrollo
git pull --ff-only origin desarrollo
↓
abrir/iniciar agente desde la raíz
↓
AGENTS.md + continuidad + skills aplicables
↓
trabajo quirúrgico
↓
pruebas / quality gates
↓
bitácora + estado
↓
commit
↓
push origin desarrollo
```

La fuente de verdad permanece en `origin/desarrollo`; ningún colaborador debe mantener una versión privada divergente de las skills.
