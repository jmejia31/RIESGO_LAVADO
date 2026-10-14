# Bloque 8 — Responsabilidades de Riesgos, Evaluaciones y Consolidado

**Estado:** `BLOCK8_STATUS=CLOSED`.
**Rama:** `desarrollo`.
**Alcance:** separar las vistas de catálogo maestro, ciclo transaccional y consulta institucional; eliminar acciones de matriz completa fuera del Consolidado y exponer todas las evaluaciones activas registradas.

## Responsabilidades

| Vista | Responsabilidad | Funciones permitidas | Fuera de su responsabilidad |
| --- | --- | --- | --- |
| Riesgos | `MASTER_CATALOG` | Consultar y administrar la identidad maestra del riesgo según permisos. | Evaluaciones, consolidado y matriz completa de 82 campos. |
| Evaluaciones | `TRANSACTIONAL_WORKFLOW` | Crear, editar borradores, ver una evaluación transaccional, historial, workflow y seguimiento. | Entrada a la matriz completa institucional. |
| Consolidado | `INSTITUTIONAL_GLOBAL_READONLY_VIEW_OF_REGISTERED_EVALUATIONS` | Consulta global de todas las evaluaciones activas registradas, matriz completa, exportación Excel y reporte PDF. | Edición de borradores o modificación del workflow. |

La definición tipada se encuentra en `MATRICES_VIEW_RESPONSIBILITIES` y se verifica mediante `tools/validate_block8_view_responsibilities.js`.

## Acciones y terminología

- Evaluaciones conserva una única acción `Ver evaluación` por fila y la acción de edición permitida por workflow. Se retiró de esa tabla `Ver Matriz completa`.
- Riesgos conserva la administración del catálogo sin acción de matriz completa.
- Consolidado ofrece una acción canónica `Ver Matriz completa` por fila. Se retiró `Ver detalle del consolidado`, cuya información se solapaba con la misma fila/proyección institucional.
- La matriz completa se carga bajo demanda desde `GET /api/matrices-riesgos/evaluaciones/{evaluationId}/proyeccion-institucional`; se valida la identidad de evaluación y los 82 ordinales antes de presentarla. No se descarga una matriz completa por cada fila del listado.
- En contexto de riesgo, `rieNombre` se presenta como **Riesgo Inherente** y `rieDescripcion` como **Evaluación**. La identidad se etiqueta **Código de Riesgo**. No se renombraron DTOs, columnas Oracle ni esquema.
- La búsqueda de Evaluaciones expresa `Código de Riesgo`, `Riesgo Inherente` y `Evaluación`. Se conservaron nombres administrativos que no representan F08/F09.

## Consulta global del Consolidado

La semántica anterior `LATEST_APPROVED_ONLY` queda superada por el requerimiento vigente: el Consolidado es una consulta institucional global de matrices registradas y muestra una fila por cada evaluación activa, sin reducirlas a una por riesgo ni exigir estado `APROBADA` o una proyección completa.

La fuente parte de `RL_MR_EVALUACIONES_RIESGO`, incorpora la identidad del riesgo y obtiene la proyección con `LEFT JOIN`. El estado mostrado viene del último evento de workflow por evaluación (`FLU_FECHA DESC, FLU_ID DESC`). La ausencia de un evento se presenta como `BORRADOR`; los campos sin proyección conservan semántica nula. No se inventan valores ni se aprueban registros para hacerlos visibles.

El filtro `TODOS` incluye todos los estados vigentes; `BORRADOR`, `EN_REVISION`, `OBSERVADA`, `APROBADA`, `RECHAZADA` y `CERRADA` filtran su estado correspondiente. El listado, conteo y exportaciones Excel/PDF comparten el mismo dataset y filtros. Cada fila mantiene `EVA_ID` como identidad y abre la matriz completa de esa evaluación exacta, incluso si está en borrador.

## Pruebas y verificación

La regresión Oracle XE `ConsolidatedApiAndReportIncludeEveryActiveEvaluationAndFilterByLatestState` cubre los seis estados, un borrador sin proyección, varias evaluaciones del mismo riesgo, estado más reciente y paridad entre API y exportación. El checkout local no debe declarar Oracle ejecutado cuando no está configurada la conexión; CI usa Oracle XE desechable.

El validador `node tools/validate_block8_view_responsibilities.js --inventory` comprueba responsabilidades, labels del riesgo, ausencia de acciones prohibidas, único contexto de matriz completa, proyección canónica lazy, ausencia de selector oficial en el cliente e inventario de acciones. El validador UI del Bloque 7 verifica cobertura y labels 82/82. Las suites frontend y Playwright conservan las pruebas de Bloques 1–7 y agregan navegación por teclado, acción única, carga de la proyección 82/82 y mediciones responsive.

La integración local no ejecutó escrituras de Oracle. El Quality Gates remoto `38591158796`, SHA `fb7fe02a870d23c52aac000f2890dab594b41dfa`, concluyó `completed/success`; el job `Run Block 8 official consolidated selection against disposable Oracle XE` ejecutó 1/1 test sin omisión y emitió `BLOCK8_CONSOLIDATED_ORACLE_INTEGRATION=PASS`. También pasaron en el mismo run la regresión Oracle de Bloque 6 (3 tests), la suite backend (963 tests), Playwright (48 tests), frontend y contenedores. El commit documental final queda sujeto a su propio Quality Gates.

## Esquema, seguridad y continuidad

No se requirió cambio de esquema ni DDL. No se usó producción. `PRODUCTION_DML_EXECUTED=0`, `PRODUCTION_DDL_EXECUTED=0`, `PRODUCTION_APPLY_EXECUTED=NO`, `PRODUCTION_APPLY_AUTHORIZED=NO`. Los cambios locales ajenos se preservaron fuera de staging. Los Bloques 1–7 permanecen cerrados; el bloque siguiente queda sin iniciar.
