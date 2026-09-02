# Agent Skills compartidas — RIESGO_LAVADO

Esta carpeta es la fuente versionada de **Agent Skills del proyecto** para ChatGPT, Codex/Codex CLI, Antigravity y otros clientes compatibles.

## Estándar

La referencia normativa es `https://github.com/agentskills/agentskills` / `https://agentskills.io/specification`.

No se copia ni se vendoriza ese repositorio dentro de RIESGO_LAVADO. Nuestras skills implementan su formato abierto: una carpeta por skill con `SKILL.md`, frontmatter YAML y las instrucciones del dominio.

El proyecto valida automáticamente los requisitos esenciales del estándar con:

```bash
python tools/validate_agent_skills.py
```

Además existe `.github/workflows/agent-skills.yml`, que bloquea errores estructurales en pushes/PRs que modifiquen Agent Skills.

> `skills-ref` del repositorio oficial es una librería de referencia/demostración y su propio README indica que no está destinada a producción. Por eso el CI de RIESGO_LAVADO no depende de ella.

## Reglas de uso

- Fuente de verdad: `origin/desarrollo`.
- `AGENTS.md` sigue siendo la autoridad transversal.
- Las skills complementan el protocolo; nunca lo reemplazan.
- Un agente debe activar todas las skills cuyo `description` coincida con la tarea.
- No duplicar una skill para Codex y otra para Antigravity: la versión oficial es la misma carpeta versionada.
- No copiar estas skills a una ubicación privada como fuente maestra; una copia local solo puede ser caché/consumo.
- Toda nueva skill debe pasar `python tools/validate_agent_skills.py`.

## Skills transversales

- `riesgo-lavado-continuidad`: inicio, handoff y prevención de pérdida/desincronización.
- `riesgo-lavado-cambio-quirurgico`: cambios incrementales sin reescaneo innecesario.
- `riesgo-lavado-quality-gates`: build, tests, CI y gates.
- `riesgo-lavado-cierre-fase`: certificación estricta de fases/UI.

## Skills especializadas

- `riesgo-lavado-frontend-angular`: Angular, rutas, componentes, servicios, formularios y E2E.
- `riesgo-lavado-backend-aspnet`: ASP.NET Core, APIs, servicios, seguridad y pruebas.
- `riesgo-lavado-oracle-database`: Oracle, SQL, DDL/DML, migraciones e integridad.
- `riesgo-lavado-ui-ux-ihss`: contrato visual, prototipos aprobados, responsive y accesibilidad.
- `riesgo-lavado-matrices-riesgo`: Matrices, familias, versiones, formularios JSON, scoring y evaluaciones.
- `riesgo-lavado-testing-regresion`: estrategia y evidencia de regresión.
- `riesgo-lavado-documentacion`: bitácora, estado colaborativo y handoff.
- `riesgo-lavado-github-ci`: GitHub Actions, runs, jobs y diagnóstico de CI.
- `riesgo-lavado-sonarcloud`: análisis Sonar, Quality Gate y permisos.
- `riesgo-lavado-pdf-excel`: reportes institucionales y paridad PDF/Excel.

## Activación combinada

Una tarea puede activar varias skills. Ejemplos:

- Corrección visual de Matrices: `continuidad` + `cambio-quirurgico` + `matrices-riesgo` + `frontend-angular` + `ui-ux-ihss` + `testing-regresion`.
- Error 500 Oracle: `continuidad` + `cambio-quirurgico` + `backend-aspnet` + `oracle-database` + `testing-regresion`.
- Sonar rojo: `continuidad` + `github-ci` + `sonarcloud` + `quality-gates`.
- Cierre de fase: `continuidad` + skills del dominio + `testing-regresion` + `quality-gates` + `cierre-fase` + `documentacion`.

## Política anti-pérdida

El conocimiento operativo vive en Git. Todo colaborador obtiene la misma versión con `git pull --ff-only origin desarrollo`. Los cambios de skills deben versionarse y publicarse igual que el código.
