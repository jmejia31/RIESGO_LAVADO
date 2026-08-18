# F3 — Tabla de Evaluaciones: Semántica de Datos y Renderizado Robusto

## Estado

**F3.2 COMPLETADA Y CERTIFICADA FÍSICAMENTE — Tabla de Evaluaciones completamente funcional y semánticamente coherente.**

- Fecha: 2026-08-18 (UTC-6).
- Rama autorizada: `desarrollo`.
- Baseline inicial F3.2: `59cf013fc782289be32dd6e2bd1788355dcbb1fa`.
- Commit de implementación final F3.2: Por generar (`fix(matrices): completar cierre semantico F3.2`).
- PR: #20, permanece **Draft / No merged**.
- `main`: fuera de alcance.
- Backend: 0 modificaciones (406/406 PASS).
- Oracle / SQL: 0 modificaciones.

## Alcance rector recuperado de F1/F1-R

F3 corresponde a la **tabla de Evaluaciones**, su **semántica de datos** y su **renderizado robusto**. También recibe el residual de `DEF-02`: durante la vista inicial el contenedor `MatricesRiesgosCicloIntegralComponent` ejecutaba una consulta operativa de 200 registros al mismo tiempo que `MatricesRiesgosComponent` consultaba la página visible (10/20 registros), generando dos cargas con propósitos diferentes sobre el mismo endpoint.

## F3.1 — Cambios implementados

1. Se elimina la precarga automática de 200 evaluaciones cuando la vista inicial es `matriz`.
2. La consulta operativa de 200 registros queda reservada a las vistas que realmente consumen ese arreglo: `mitigacion` y `monitoreo`.
3. La respuesta operativa normaliza defensivamente `items` mediante `Array.isArray(...)`.
4. Ante error de la carga operativa se limpia el arreglo para no conservar datos obsoletos como si fueran vigentes.
5. Se actualiza la cobertura del contenedor integral para verificar ausencia de doble carga inicial, lazy-load por vista, normalización y manejo de error.
6. Se agrega una suite dedicada `matrices-riesgos.component.f3.spec.ts` para certificar:
   - una sola consulta paginada de 10 registros en la vista inicial;
   - las nueve columnas de la tabla de Evaluaciones;
   - semántica de filas y acciones por estado;
   - separación entre filas visibles y metadatos server-side (`totalRegistros`, `totalPaginas`);
   - normalización de `items = null` a `[]`.

## F3.2 — Cierre Funcional y Semántico Implementado

1. **Normalización defensiva de items**: En `cargarEvaluaciones()` y `contarEvaluacionesPorEstado()` de `MatricesRiesgosComponent`, se evalúa estrictamente `Array.isArray(paginado?.items)` para garantizar que el signal `evaluaciones` nunca almacene objetos truthy no-array ni provoque regresiones de `filter is not a function`.
2. **Limpieza completa de metadatos ante error**: En el bloque de error de `cargarEvaluaciones()`, además de vaciar el arreglo de filas `evaluaciones.set([])`, se restablecen a `0` las señales `totalRegistros` y `totalPaginas`, eliminando metadatos obsoletos de consultas previas.
3. **KPI Total Evaluaciones**: Se actualizó `matrices-riesgos.component.html` para que el KPI Total Evaluaciones renderice la señal `totalRegistros()` proveniente de la metadato server-side (en lugar del `length` de la página visible) con la descripción `Total según la consulta actual`.
4. **Semántica de KPIs por Estado**: Se ajustaron las descripciones de los KPIs *En Borrador*, *En Revisión* y *Aprobadas* para explicitar que sus conteos corresponden a la *página actual* (`Pendientes en la página actual`, `En análisis en la página actual`, `Oficiales en la página actual`).
5. **Cobertura Automatizada F3.2**: Se implementaron completamente las dos pruebas unitarias pendientes (`it.todo`) en `matrices-riesgos.component.f3.spec.ts`, alcanzando **31/31 test files passed** y **277/277 unit tests passed**.

## Criterios de aceptación F3.2

- ✅ Normalización defensiva contra `items` truthy no-array.
- ✅ Limpieza de metadatos `totalRegistros` y `totalPaginas` en caso de error.
- ✅ KPI Total Evaluaciones utiliza `totalRegistros()`.
- ✅ KPIs por estado comunican explícitamente su alcance sobre la página actual.
- ✅ Suite frontend Angular **277/277 (100% PASS)** y build `dist/rl-app` exitoso.
- ✅ Suite backend .NET Core **406/406 (100% PASS)**.
- ✅ 0 modificaciones Oracle / SQL.
- ✅ PR #20 continúa Draft y `main` no se fusiona.
