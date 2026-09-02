---
name: riesgo-lavado-continuidad
description: Preserva continuidad multiagente en RIESGO_LAVADO. Usar al iniciar, retomar, transferir o cerrar cualquier intervención para evitar pérdida, duplicación, sobrescritura o trabajo desincronizado.
---

# Continuidad multiagente

## Autoridad

Esta skill complementa `AGENTS.md`. Si existe conflicto, prevalece `AGENTS.md` y luego la instrucción expresa más reciente de Javier Mejía.

## Inicio obligatorio

1. Confirmar rama `desarrollo`; no modificar `main` sin autorización expresa.
2. Leer `AGENTS.md`.
3. Leer el tramo más reciente relevante de `BITACORA_COLABORACION.md`.
4. Leer `docs/0.0 Documentación/ESTADO_COLABORACION.md`.
5. Identificar HEAD remoto, último colaborador, último objetivo, archivos afectados, pruebas y punto exacto de continuación.
6. Sincronizar con `origin/desarrollo` antes de editar.

## Regla anti-pérdida

- Nunca asumir que el estado local es más reciente que GitHub.
- Nunca sobrescribir cambios ajenos sin revisar el diff y el historial.
- Nunca dejar trabajo terminado solo localmente.
- Si el HEAD remoto cambió durante la intervención, integrar/revisar antes de publicar.
- No declarar verificado algo heredado que no se ejecutó en la intervención actual.

## Handoff obligatorio

Al finalizar:

1. Registrar archivos creados/modificados.
2. Registrar pruebas realmente ejecutadas y las no ejecutadas con motivo.
3. Actualizar bitácora y estado colaborativo cuando la intervención produzca cambios del proyecto.
4. Commit claro y push a `origin/desarrollo`.
5. Confirmar SHA final y que remoto contiene el commit.
6. Entregar punto exacto de continuación.

## Resultado mínimo

Toda salida de cierre debe incluir: rama, SHA inicial/final, archivos, validaciones, estado de publicación, riesgos/pendientes y siguiente punto exacto.
