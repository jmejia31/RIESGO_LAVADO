---
name: riesgo-lavado-matrices-riesgo
description: Implementa y revisa el módulo Matrices de Riesgo de RIESGO_LAVADO. Usar para familias, formularios dinámicos JSON, evaluaciones, factores, scoring, probabilidad, impacto, riesgo inherente/residual, controles, versiones, catálogos y flujos relacionados.
---

# Matrices de Riesgo

## Objetivo

Mantener un único modelo funcional coherente desde definición dinámica hasta evaluación, persistencia, cálculo, consulta y reporte.

## Reglas de dominio

1. Recuperar primero la fase/subfase vigente y el punto exacto de continuidad.
2. Identificar contrato de datos antes de tocar UI o backend.
3. Preservar separación entre:
   - familias y versiones;
   - definición de formulario;
   - captura/evaluación;
   - catálogos;
   - cálculo;
   - resultados e históricos.
4. No duplicar renderers, motores de cálculo, serializadores o normalizadores.
5. Una versión histórica no debe cambiar retroactivamente por editar una definición nueva.
6. Cambios de catálogo deben considerar referencias históricas y vigencia.
7. Las fórmulas y reglas deben validarse de forma segura; prohibido introducir `eval` o ejecución dinámica insegura.
8. Mantener paridad entre vista, JSON técnico, backend y persistencia.

## Cambio de formulario dinámico

Verificar como mínimo:

- serialización y normalización;
- validación estructural;
- campos requeridos;
- catálogos;
- visibilidad/condiciones;
- modo editable y solo lectura;
- versión activa e histórico;
- creación, edición y consulta;
- manejo de respuestas async obsoletas;
- pruebas del renderer único.

## Cierre

Activar `riesgo-lavado-testing-regresion`, `riesgo-lavado-quality-gates` y `riesgo-lavado-cierre-fase`. Una pantalla visualmente correcta no cierra la fase si el contrato de datos o la persistencia siguen fallando.
