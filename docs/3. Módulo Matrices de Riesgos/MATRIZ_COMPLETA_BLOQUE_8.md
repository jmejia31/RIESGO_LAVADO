# Bloque 8 — Responsabilidades de Riesgos, Evaluaciones y Consolidado

**Estado:** `BLOCK8_STATUS=CLOSED`.
**Rama:** `desarrollo`.
**Alcance:** separar las vistas de catálogo maestro, ciclo transaccional y consulta institucional; eliminar acciones de matriz completa fuera del Consolidado y unificar la selección de la evaluación oficial.

## Responsabilidades

| Vista | Responsabilidad | Funciones permitidas | Fuera de su responsabilidad |
| --- | --- | --- | --- |
| Riesgos | `MASTER_CATALOG` | Consultar y administrar la identidad maestra del riesgo según permisos. | Evaluaciones, consolidado y matriz completa de 82 campos. |
| Evaluaciones | `TRANSACTIONAL_WORKFLOW` | Crear, editar borradores, ver una evaluación transaccional, historial, workflow y seguimiento. | Entrada a la matriz completa institucional. |
| Consolidado | `OFFICIAL_INSTITUTIONAL_VIEW` | Consulta institucional global, matriz completa, exportación Excel y reporte PDF. | Edición de borradores o modificación del workflow. |

La definición tipada se encuentra en `MATRICES_VIEW_RESPONSIBILITIES` y se verifica mediante `tools/validate_block8_view_responsibilities.js`.

## Acciones y terminología

- Evaluaciones conserva una única acción `Ver evaluación` por fila y la acción de edición permitida por workflow. Se retiró de esa tabla `Ver Matriz completa`.
- Riesgos conserva la administración del catálogo sin acción de matriz completa.
- Consolidado ofrece una acción canónica `Ver Matriz completa` por fila. Se retiró `Ver detalle del consolidado`, cuya información se solapaba con la misma fila/proyección institucional.
- La matriz completa se carga bajo demanda desde `GET /api/matrices-riesgos/evaluaciones/{evaluationId}/proyeccion-institucional`; se valida la identidad de evaluación y los 82 ordinales antes de presentarla. No se descarga una matriz completa por cada fila del listado.
- En contexto de riesgo, `rieNombre` se presenta como **Riesgo Inherente** y `rieDescripcion` como **Evaluación**. La identidad se etiqueta **Código de Riesgo**. No se renombraron DTOs, columnas Oracle ni esquema.
- La búsqueda de Evaluaciones expresa `Código de Riesgo`, `Riesgo Inherente` y `Evaluación`. Se conservaron nombres administrativos que no representan F08/F09.

## Selección oficial del Consolidado

La consulta central del repositorio se comparte entre el listado paginado, el conteo y la exportación. Por cada evaluación se obtiene el estado vigente del workflow desde su evento más reciente (`FLU_FECHA DESC, FLU_ID DESC`). Sólo las evaluaciones cuyo estado vigente es `APROBADA` participan. Por riesgo se selecciona la aprobación más reciente con desempate determinista por fecha del evento, ID de flujo y, finalmente, ID de evaluación, todos descendentes.

Un borrador, revisión, rechazo o evaluación cerrada posterior no desplaza una aprobación. No se documentó equivalencia autorizada entre `CERRADA` y `APROBADA`; por eso `CERRADA` queda excluida. Los riesgos sin evaluación aprobada no se presentan como si un borrador fuera oficial. El estado vacío del listado informa que no hay evaluaciones aprobadas para los filtros seleccionados.

La UI, el export Excel y la fuente del PDF parten de la misma selección central del backend; la selección no se realiza en Angular. El dashboard institucional consume la misma fuente oficial.

## Pruebas y verificación

La prueba Oracle XE opt-in `ConsolidatedApiAndReportUseLatestCurrentApprovedEvaluationPerRisk` crea candidatos de distintos estados para un mismo riesgo, dos aprobaciones con empate temporal, y una aprobación después cerrada. Verifica la selección mediante el endpoint paginado real y que la exportación utilice los mismos IDs. Se ejecuta en el Quality Gates existente, usando el mismo Oracle XE desechable que la integración del Bloque 6.

El validador `node tools/validate_block8_view_responsibilities.js --inventory` comprueba responsabilidades, labels del riesgo, ausencia de acciones prohibidas, único contexto de matriz completa, proyección canónica lazy, ausencia de selector oficial en el cliente e inventario de acciones. El validador UI del Bloque 7 verifica cobertura y labels 82/82. Las suites frontend y Playwright conservan las pruebas de Bloques 1–7 y agregan navegación por teclado, acción única, carga de la proyección 82/82 y mediciones responsive.

La integración local no ejecutó escrituras de Oracle. El Quality Gates remoto `38591158796`, SHA `fb7fe02a870d23c52aac000f2890dab594b41dfa`, concluyó `completed/success`; el job `Run Block 8 official consolidated selection against disposable Oracle XE` ejecutó 1/1 test sin omisión y emitió `BLOCK8_CONSOLIDATED_ORACLE_INTEGRATION=PASS`. También pasaron en el mismo run la regresión Oracle de Bloque 6 (3 tests), la suite backend (963 tests), Playwright (48 tests), frontend y contenedores. El commit documental final queda sujeto a su propio Quality Gates.

## Esquema, seguridad y continuidad

No se requirió cambio de esquema ni DDL. No se usó producción. `PRODUCTION_DML_EXECUTED=0`, `PRODUCTION_DDL_EXECUTED=0`, `PRODUCTION_APPLY_EXECUTED=NO`, `PRODUCTION_APPLY_AUTHORIZED=NO`. Los cambios locales ajenos se preservaron fuera de staging. Los Bloques 1–7 permanecen cerrados; el bloque siguiente queda sin iniciar.
