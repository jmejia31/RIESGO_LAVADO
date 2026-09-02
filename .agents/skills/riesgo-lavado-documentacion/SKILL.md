---
name: riesgo-lavado-documentacion
description: Mantiene documentación y handoff de RIESGO_LAVADO. Usar para BITACORA_COLABORACION.md, ESTADO_COLABORACION.md, README, documentos técnicos, decisiones, cierre de fases y transferencia entre ChatGPT, Codex y Antigravity.
---

# Documentación y handoff

## Objetivo

Evitar pérdida de contexto y asegurar que cualquier colaborador pueda continuar desde el estado real del repositorio.

## Fuentes obligatorias

- `AGENTS.md`.
- `BITACORA_COLABORACION.md`: historial cronológico; no reescribir hechos anteriores.
- `docs/0.0 Documentación/ESTADO_COLABORACION.md`: estado vivo y punto de continuidad.
- Documentación específica del módulo.

## Reglas

1. Separar hechos ejecutados de afirmaciones heredadas.
2. Nunca inventar pruebas, SHAs, runs, aprobaciones ni resultados Oracle.
3. Registrar rama y SHA inicial/final.
4. Enumerar archivos modificados y propósito.
5. Registrar pruebas ejecutadas y no ejecutadas.
6. Mantener pendientes, bloqueos externos y punto exacto de continuación.
7. Si cambia el protocolo de agentes, mantener sincronizados `AGENTS.md` y `.agents/AGENTS.md`.
8. No incrustar rutas `file:///C:/...`; usar rutas relativas del repositorio.
9. Documentación de cierre debe coincidir con el código y CI del mismo SHA.

## Handoff mínimo

El siguiente colaborador debe poder responder sin reconstruir el proyecto completo: qué se hizo, qué falta, dónde continuar, qué no tocar y qué evidencia existe.
