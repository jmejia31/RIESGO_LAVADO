---
name: riesgo-lavado-testing-regresion
description: Planifica y ejecuta pruebas de regresión de RIESGO_LAVADO. Usar ante correcciones, nuevas funcionalidades, refactors, cambios de seguridad, UI, API, Oracle o antes de declarar una fase técnicamente cerrada.
---

# Testing y regresión

## Principio

Cada afirmación de calidad debe corresponder a una ejecución real de la intervención actual o estar marcada expresamente como heredada.

## Estrategia

1. Identificar riesgo e impacto con CodexGraph cuando esté disponible.
2. Ejecutar primero pruebas focalizadas del área modificada.
3. Corregir causa raíz; no reducir assertions, excluir tests o bajar cobertura para obtener verde.
4. Ejecutar regresión ampliada proporcional al impacto.
5. Para cambios transversales, ejecutar suites completas.

## Matriz mínima

- Backend: unitarias/integración de `backend/RL.API.Tests`.
- Frontend: unitarias, lint y build en `frontend/rl-app`.
- Flujos críticos: E2E.
- SQL: validadores del repositorio y Oracle real cuando exista acceso autorizado.
- Documentación: enlaces/estructura si se modificó documentación.
- Seguridad/CI: workflow correspondiente cuando aplique.

## Reporte

Registrar:

- comando exacto;
- resultado PASS/FAIL;
- conteo real;
- fallos y causa;
- pruebas no ejecutadas y motivo;
- si el resultado es fresco o heredado.

No usar frases como “todo pasa” sin evidencia concreta.
