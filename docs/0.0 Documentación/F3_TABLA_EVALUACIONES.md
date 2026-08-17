# F3 — Tabla de Evaluaciones: Semántica de Datos y Renderizado Robusto

## Estado

**INICIADA — implementación F3.1 preparada para validación CI.**

- Fecha: 2026-08-17 (UTC-6).
- Rama autorizada: `desarrollo`.
- Baseline F3: `f7992250ee1beed1d2a35a0f7e140b2bf97a7471`.
- PR: #20, debe permanecer **Draft / No merged**.
- `main`: fuera de alcance.
- Backend: fuera de alcance en F3.1.
- Oracle / SQL: fuera de alcance en F3.1.

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

## Criterios de aceptación F3.1

- La entrada a `Matriz y evaluaciones` no debe disparar la consulta operativa `registrosPorPagina=200`.
- La tabla debe mantener nueve columnas y renderizar las filas desde `EvaluacionRiesgoResumenDto[]`.
- `totalRegistros` y `totalPaginas` deben provenir de la respuesta paginada, independientemente del tamaño de la página visible.
- Un `items` inválido/nulo no debe romper la tabla ni convertir el signal `evaluaciones` en un objeto no-array.
- Mitigación y Monitoreo deben continuar recibiendo su carga operativa cuando el usuario entra a esas vistas.
- PR #20 continúa Draft y `main` no se fusiona.

## Validación pendiente de esta intervención

Después de publicar el commit F3.1 se debe revisar el workflow de GitHub Actions asociado al nuevo SHA. F3 no se declarará cerrada solo por crear el commit: requiere resultado de validación automatizada y, si corresponde según el plan rector, QA visual residual antes de avanzar a F4.
