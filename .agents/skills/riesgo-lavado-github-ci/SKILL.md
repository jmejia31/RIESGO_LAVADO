---
name: riesgo-lavado-github-ci
description: Diagnostica y modifica GitHub Actions de RIESGO_LAVADO. Usar para workflows, quality gates, runs fallidos, jobs, permisos, artefactos, dependencias de CI y comprobación de que el SHA publicado es el realmente validado.
---

# GitHub CI

## Objetivo

Resolver gates rojos por causa raíz y certificar el commit correcto, sin confundir ejecución local con GitHub Actions.

## Workflows principales

- `.github/workflows/quality-gates.yml`.
- `.github/workflows/sonar-analysis.yml`.
- Workflows adicionales deben ser independientes y mínimos salvo necesidad explícita.

## Diagnóstico

1. Confirmar SHA exacto del run.
2. Identificar workflow, run, job y step fallido.
3. Leer el log técnico exacto antes de modificar código.
4. Clasificar causa: código, test, entorno, permisos, secreto, dependencia externa o configuración.
5. Corregir solo lo que realmente corresponde al repositorio.
6. Si el problema exige secreto/permisos externos, marcar `ACCIÓN EXTERNA REQUERIDA`; no fabricar una corrección en código.
7. Reejecutar/verificar el gate del nuevo SHA.

## Prohibiciones

- No desactivar tests/gates para conseguir verde.
- No exponer secretos.
- No declarar CI verde usando un run de otro commit.
- No modificar `main` sin autorización.
