# Agent Skills compartidas — RIESGO_LAVADO

Esta carpeta es la fuente versionada de skills de proyecto para colaboradores compatibles con Agent Skills.

## Regla de uso

- La fuente de verdad es `origin/desarrollo`.
- Todo agente debe leer `AGENTS.md` antes de trabajar.
- Las skills complementan `AGENTS.md`; nunca lo reemplazan.
- Cuando una tarea coincida con una skill, debe activarse antes de modificar archivos.
- No copiar estas skills a carpetas privadas del usuario como fuente maestra: la versión oficial vive en este repositorio.

## Skills disponibles

- `riesgo-lavado-continuidad`: evita pérdida de trabajo y obliga a recuperar el punto exacto de continuación.
- `riesgo-lavado-cambio-quirurgico`: cambios pequeños, dirigidos y sin reescaneo innecesario del repositorio.
- `riesgo-lavado-quality-gates`: validación técnica, pruebas, build, CI y SonarCloud sin falsos positivos.
- `riesgo-lavado-cierre-fase`: criterios estrictos para declarar una fase/UI cerrada.

## Compatibilidad

La ubicación `.agents/skills/<skill>/SKILL.md` está diseñada para ser consumida como skill de proyecto por herramientas compatibles con Agent Skills, incluyendo Codex y Antigravity. Para herramientas que no hagan descubrimiento automático, `AGENTS.md` sigue siendo el protocolo obligatorio y estas skills pueden leerse explícitamente.
