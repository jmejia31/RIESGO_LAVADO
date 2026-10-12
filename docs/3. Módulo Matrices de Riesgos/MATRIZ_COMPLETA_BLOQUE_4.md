# Matriz completa — Bloque 4 (campos 40–49)

## Contrato vigente

El contrato funcional actual del Bloque 4 activa los campos institucionales 40–49. Esta decisión posterior supera el descargo histórico para el alcance actual del bloque; conserva intacto el registro de la Fase 6. `BLOCK_4_CURRENT_CONTRACT_SUPERSEDES_LEGACY_DESCOPE=YES`.

La pantalla **Mitigación** continúa administrando controles, planes y actividades. **Matriz completa** consulta una proyección agrupada y permanece de solo lectura. Bloque 5 no forma parte de esta entrega.

## Alcance de datos de muestra del Bloque 4

La matriz congelada de 59 riesgos es la referencia funcional completa para contrato, catálogos, cálculos, controles, mitigación, monitoreo y pruebas. No ordena una migración masiva: `FULL_59_RISK_MIGRATION_REQUIRED=NO` y `MANDATORY_MIGRATION_RISKS=0`. El pase de datos utiliza una muestra representativa de cinco riesgos, sin eliminar, desactivar ni recrear los otros registros productivos.

La solicitud diagnóstica de 215 atributos humanos queda `OBSOLETE_BY_SCOPE`; no se entrega a negocio ni bloquea Bloque 4. El generador de qualification se conserva como herramienta de auditoría. Los datos históricos de planes/actividades y monitoreo que no sean aptos para una carga segura se documentan como referencia no importada; no se inventan fechas, responsables ni valores operacionales.

Después de completar el módulo, la validación/capacitación final será la creación manual de un riesgo completo desde la UI y su lectura, edición y exportación. Ese escenario no forma parte de este pase.

### Muestra certificada del pase de alcance

El análisis de los 59 candidatos seleccionó, de forma determinista, `ROP-CUMP-59`, `ROTR-AFIL-1`, `ROP-CUMP-50`, `ROP-CUMP-55` y `ROTR-AFIL-2`. Los cinco existen en producción con una evaluación activa y una proyección cada uno. El plan de muestra produjo cero mutaciones, cero blockers y cero ambigüedades; por ello no se aplicó ni se necesitó simular DML en XE. Los otros 54 riesgos quedan preservados fuera del scope de muestra.

La muestra contiene casos preventivos, detectivos, correctivos y “No hay”; respuestas Aceptar, Evitar, Mitigar y Transferir/Compartir; dos riesgos con señales de alerta y dos con texto fuente de planes/actividades. Los textos F40/F42 son referencia y no se insertan como datos operacionales sin sus atributos obligatorios autorizados. El workbook no contiene fuentes GTIC pobladas en F17–F19. RUN_ID `20261012_222048_228`; preimage SHA-256 `3dd71d39499f2e1f3873504ca357bdddef6a4d0041327c867da8fface4534088`; plan canónico SHA-256 `4d4b0b2315068e1e869dfb0db65d512721f3889e40b5eedf9c9873d94f3065a8`; plan file SHA-256 `180097b638b30bca3fe4f99dbb9e00d91f086047b52a6236a84af8c421d9e885`. Los artefactos completos permanecen fuera de Git en `%USERPROFILE%\Documents\RIESGO_LAVADO\BLOCK4_SAMPLE_5`.

La consulta HPPROD1 fue `READ ONLY` y finalizó con `ROLLBACK`; DML=0, DDL=0 y apply no autorizado ni ejecutado. El estado de este pase es `READY_FOR_CLOSURE_REVIEW`; no constituye autorización de recarga ni cierre funcional otorgado por negocio.

| Campo | Etiqueta literal | Fuente / cálculo | Cardinalidad y persistencia | Superficie de edición / lectura |
|---:|---|---|---|---|
| 40 | Plan de Mitigación/Acciones Correctivas | `RL_MR_PLANES.PLA_DESCRIPCION` | Repetible por evaluación; un registro por plan | Mitigación / Matriz completa |
| 41 | No. Acciones de Mitigación | `COUNT(RL_MR_PLANES)` por `PLA_EVALUACION_ID` | Conteo de servidor; no persistido | Solo lectura en Matriz completa |
| 42 | Actividades | `RL_MR_ACTIVIDADES.ACT_DESCRIPCION` | Repetible dentro del plan; `ACT_PLAN_ID → PLA_ID` | Mitigación / Matriz completa, anidada por plan |
| 43 | Cantidad de Actividades | `COUNT(RL_MR_ACTIVIDADES)` por `ACT_PLAN_ID` | Conteo de servidor por plan; no persistido | Solo lectura en Matriz completa |
| 44 | Monitoreo/ Seguimiento | `RL_MR_PLANES.PLA_MONITOREO_SEGUIMIENTO` | Texto opcional propio del plan, `VARCHAR2(1000 CHAR)` | Mitigación / Matriz completa |
| 45 | Responsables | `RL_MR_PLANES.PLA_RESPONSABLES` | Texto opcional propio del plan, `VARCHAR2(1000 CHAR)` | Mitigación / Matriz completa |
| 46 | Fecha inicio | `RL_MR_PLANES.PLA_FECHA_INICIO` | Fecha del plan | Mitigación / Matriz completa |
| 47 | Fecha final | `RL_MR_PLANES.PLA_FECHA_FIN` | Fecha del plan; no anterior a inicio | Mitigación / Matriz completa |
| 48 | Recursos | `RL_MR_PLANES.PLA_RECURSOS` | Texto opcional propio del plan, `VARCHAR2(1000 CHAR)` | Mitigación / Matriz completa |
| 49 | Presupuesto | `RL_MR_PLANES.PLA_PRESUPUESTO` | Decimal existente `NUMBER(15,2)`, mínimo cero | Mitigación / Matriz completa |

`PLA_MONITOREO_SEGUIMIENTO`, `PLA_RESPONSABLES` y `PLA_RECURSOS` permiten `NULL` para conservar filas históricas; no se realiza backfill. Los valores opcionales se recortan y espacios en blanco se almacenan como `NULL`. No se crean catálogos de responsables o recursos.

## Semántica y source of truth

- `FIELD_41_DECISION=PLAN_COUNT_PER_EVALUATION`; la fuente autoritativa es la cantidad de filas persistidas de plan para la evaluación.
- `FIELD_43_DECISION=ACTIVITY_COUNT_PER_PLAN`; la cantidad es por plan y se deriva de actividades persistidas.
- `FIELD_44_DECISION=PLAN_LEVEL_PERSISTENCE`; `RL_MR_AUTOMONITOREO.MON_RESULTADO` es un evento histórico ligado a evaluación y no equivale al seguimiento del plan. `MON_RESULTADO_EQUIVALENT=NO`.
- `FIELD_45_DECISION=PLAN_LEVEL_PERSISTENCE`; responsables del plan permanecen independientes de `RL_MR_ACTIVIDADES.ACT_RESPONSABLE`. `ACT_RESPONSABLE_PRESERVED=YES`.
- `FIELD_48_DECISION=PLAN_LEVEL_PERSISTENCE`; `RESOURCE_TYPE=TEXT`; longitud máxima 1000 caracteres; opcional.
- Planes y actividades permanecen normalizados en sus tablas operativas. El read model del Bloque 4 obtiene evaluación, planes y actividades con una consulta agrupada, sin llamadas N+1 desde Angular.
- Presupuesto conserva `decimal` en backend y `NUMBER(15,2)` en Oracle; no se agrega almacenamiento monetario alterno.

## Esquema y recuperación

La actualización incremental está versionada en `database/19_matrices_riesgos/transicion/46_precheck_bloque4_plan_campos.sql`, `47_ddl_bloque4_plan_campos.sql`, `48_postcheck_bloque4_plan_campos.sql` y `49_rollback_bloque4_plan_campos.sql`. Precheck, postcheck y validador Fase 11 son de solo lectura. El rollback es manual, retira exclusivamente las tres columnas nuevas y verifica su tipo/nullability antes de dropearlas.

La reconstrucción para instalación limpia y los comentarios institucionales incorporan las mismas columnas. No se agrega tabla: el modelo reducido conserva sus 17 tablas. Los scripts están preparados para revisión/ejecución posterior autorizada; **no fueron aplicados** a Oracle institucional.

## Estado de certificación

El bloque solo se declara cerrado después de pruebas backend/frontend/E2E completas, validadores, publicación en `desarrollo` y Quality Gate remoto exitoso sobre el SHA final exacto. El gate exitoso de implementación corresponde a SHA `981a854a34eff5ed055e71787ef6bc0693abd3f8`; el commit documental que registra estos resultados tendrá un gate propio para certificar el SHA final.

- `BASE_SHA=484257a6b2b33cc9f7310e492c1687948e90be18` (confirmado antes de editar).
- `IMPLEMENTATION_SHA=981a854a34eff5ed055e71787ef6bc0693abd3f8`.
- `REMOTE_QUALITY_GATE_RUN_ID=36659335704`; `RUN_NUMBER=1678`; `HEAD_SHA=981a854a34eff5ed055e71787ef6bc0693abd3f8`; `STATUS=completed`; `CONCLUSION=success`.
- `BACKEND_TESTS=747`; `FRONTEND_TEST_FILES=82`; `FRONTEND_TESTS=823`; `E2E_PASSED=43`; `NPM_AUDIT_VULNERABILITIES=0`.
- Cobertura backend: líneas 37.00%, ramas 40.33%. Cobertura frontend: sentencias 62.42%, ramas 55.87%, funciones 58.68%, líneas 63.33%.
- `ORACLE_CONNECTION_ATTEMPTED=NO`; `ORACLE_DML_EXECUTED=NO`; `ORACLE_DDL_EXECUTED=NO`.
- `MAIN_TOUCHED=NO`; `BLOCK_5_STARTED=NO`.
