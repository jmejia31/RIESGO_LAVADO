# FASE 3.1.5 - Fórmulas institucionales y paridad Excel/DML

## Estado

- Baseline: `9308b9a670c7444b3f3355c58c4dc51ef1bd1a97`.
- Rama: `desarrollo`.
- Commit técnico: `82787eb30b175f47b6244b8adf345c0c6baeafde`.
- Estado: `CERRADA / CERTIFICADA / FINAL`.
- `FASE_3.1=CERRADA / CERTIFICADA / FINAL`.

## Alcance y paridad

La subfase implementa la paridad institucional de 34 fórmulas derivadas del dataset Excel semántico. La transformación conserva expresiones `MIN`, `AND` y `LOOKUP`, y usa los parámetros institucionales:

- `PESO_PREVENTIVO=0.70`.
- `PESO_DETECTIVO=0.15`.
- `PESO_CORRECTIVO=0.15`.

El dataset contiene exactamente 34 fórmulas y el script DML 25 contiene los mismos 34 códigos, en paridad `34/34`. Las pruebas mantienen un `expectedByField` explícito de 34 resultados, con faltantes `0` y extras `0`; no se redujeron precisiones ni tolerancias.

El runtime conserva pinning por `FormulaVersion` exacta y no sustituye una versión publicada por `latest`. Los handlers nativos anidados no se consideran ciclos; la recursión de funciones compuestas continúa siendo rechazada con `FORMULA_CYCLE`. Las comparaciones cubren `Blank == ""`, `"" == ""`, `Blank != valor` y comparación numérica normal; las operaciones aritméticas mantienen el rechazo de tipos inválidos.

## Controles técnicos

| Métrica | Resultado |
|---|---:|
| `FORMULAS_INSTITUCIONALES` | `34/34` |
| `FORMULA_DATASET_DML_PARITY` | `34/34` |
| `DEPENDENCY_CYCLES` | `0` |
| `INVALID_REFERENCES` | `0` |
| `UNPINNED_DEPENDENCIES` | `0` |
| `INVALID_NATIVE_HANDLERS` | `0` |
| `INVALID_ARGUMENT_TYPES` | `0` |
| `DB_EXECUTABLE_CODE` | `0` fuera del DML de carga autorizado |
| `DYNAMIC_EXECUTION` | `0` |
| `NEW_ENGINE` | `0` |
| `NEW_PUBLICATION_GATE` | `0` |
| `NEW_AUDIT_SYSTEM` | `0` |
| `NEW_CATALOG_SYSTEM` | `0` |
| `NEW_DEPENDENCY_TABLE` | `0` |
| `NEW_TABLES` | `0` |
| `RBAC_CHANGES` | `0` |
| `HISTORICAL_MUTATIONS` | `0` |
| `STANDARD_HASH_OCCURRENCES` | `0` |
| `ORACLE_11G_IDENTIFIER_LENGTH_VIOLATIONS` | `0` |

La corrección de hash evita `STANDARD_HASH`, incompatible con Oracle 11.2.0.1. El postcheck 26 está escrito para Oracle 11g y su SHA-256 requerido es `58823DFB93BED19EA0D347BC3668C3059E01534BA5A2635580A56C53C81F42A8`.

## Regresión local

- Focal backend: `49/49 PASS`.
- Backend completo: `591/591 PASS`.
- Cobertura backend: líneas `31.08%`, ramas `35.34%`.
- Frontend aislado: `716/716 PASS`.
- Cobertura frontend aislada: sentencias `60.50%`, ramas `54.19%`, funciones `55.27%`, líneas `61.15%`.
- Lint: `PASS`.
- Build frontend aislado: `PASS`.
- E2E: `29/29 PASS`.
- Validación de scripts de base de datos: `PASS`.
- Validación de enlaces documentales: `PASS`.
- Validador estructural: `FAIL` por hallazgos heredados fuera de 3.1.5 (`dynamic-form-layout` vacío y carpeta/archivo legacy en `core/services`); no se modificaron.
- `LOCAL_QUALITY_GATES=FAIL` por ese validador estructural heredado; cobertura, pruebas, lint, build, E2E, scripts de base de datos y enlaces pasaron.
- `NPM_CI_REPREP_EXITCODE=0`, instalación aislada reproducible y cache temporal eliminado.
- `NPM_AUDIT_EXITCODE=1`, `HIGH=1`, causa `fast-uri`; no se ejecutó `npm audit fix`, no se actualizaron dependencias y el resultado no se declara PASS.

El árbol frontend aislado y sus caches temporales fueron eliminados al finalizar las pruebas. Se preservaron `node_modules`, `dist`, `coverage` y `.angular` del checkout original.

## Oracle y scripts de transición

La ejecución Oracle no se reintentó en esta intervención. Se preserva la evidencia heredada:

```text
DDL_28_ALREADY_APPLIED=TRUE
DML_25_ALREADY_APPLIED=TRUE
DML_25_EXITCODE=0
DDL_28_REEXECUTED=NO
DML_25_REEXECUTED=NO
RECOVERY_27_EXECUTED=NO
POSTCHECK_26_EXECUTED_FINAL=NO_CONNECTION
ORACLE_315_BLOCKED_EXTERNAL_ENVIRONMENT=TRUE
ORACLE_315_CERTIFIED=FALSE
```

El bloqueo fue `System.Net.Sockets.SocketException / AccessDenied` al abrir el socket, antes de iniciar SQL*Plus/Postcheck. Recovery jamás se ejecutó. No se hicieron commits Oracle, DDL, DML, recovery ni mutaciones de base de datos en esta intervención.

## Continuación

Queda pendiente resolver puntualmente la vulnerabilidad `HIGH` de `fast-uri` antes del Quality Gate remoto final y repetir exclusivamente el postcheck 26 read-only cuando Oracle permita conexión. Hasta entonces, 3.1.5 no se declara cerrada y FASE 3.1 permanece `EN_PROGRESO`.

## Actualización de cierre Oracle y FASE 3.1

- Fecha/hora local: 2026-09-03 08:41 (UTC-06).
- Seguridad npm completada: `fast-uri` pasó de `3.1.5` a `3.1.6`; `NPM_AUDIT_HIGH=0`; `NPM_AUDIT_CRITICAL=0`; Quality Gate previo `33766574874` terminó `completed/success` sobre `804bff16b81255d661a8f52279fea285e7218784`.
- Conectividad: `TCP_CONNECTIVITY=PASS`; SQL*Plus `11.2.0.3.0` conectó con `SQLPLUS_CONNECTIVITY_EXITCODE=0`; no se modificaron políticas del sistema ni red.
- Postcheck: SHA-256 `58823DFB93BED19EA0D347BC3668C3059E01534BA5A2635580A56C53C81F42A8`; `POSTCHECK_26_EXITCODE=0`; `POSTCHECK_26_STATUS=PASS`; ejecución única read-only.
- Evidencia institucional: `FORMULAS=34`, `FORMULA_VERSIONES=34`, `FORMULA_USOS=0`; `FUNCIONES=10`, `FUNCION_VERSIONES=10`, `FUNCION_ARGUMENTOS=18`; `PARAMETROS=3`, `PARAMETRO_VERSIONES=3`. `CK_RL_MR_FUA_TYPE=ENABLED`; hashes de fórmulas/funciones/parámetros válidos `34/34`, `10/10`, `3/3`; `INVALID_HASHES=0`.
- Integridad: `INVALID_NATIVE_HANDLERS=0`, `INVALID_FUNCTION_ARGUMENTS=0`, `INVALID_ARGUMENT_TYPES=0`; todos los huérfanos y duplicados `0`; `INVALID_SEMANTIC_SOURCE_REFERENCES=0`; `INVALID_OBJECTS=0`; `DISABLED_CONSTRAINTS=0`; `HISTORICAL_MUTATIONS=0`; `STANDARD_HASH_OCCURRENCES=0`; `ORACLE_11G_IDENTIFIER_LENGTH_VIOLATIONS=0`.
- Transición Oracle: DDL 28 aplicado una sola vez; el primer intento de DML 25 falló antes de persistir por `STANDARD_HASH` incompatible con Oracle 11.2.0.1; DML 25 corregido terminó `ExitCode=0`; el primer postcheck falló por identificador Oracle 11g demasiado largo y fue corregido. No se reejecutaron DDL/DML y recovery 27 jamás se ejecutó.
- Regresión publicada: frontend `716/716`, E2E `29/29`, lint/build PASS, `NPM_CI_EXITCODE=0`, audit HIGH/CRITICAL `0/0`. No se repitieron suites funcionales completas porque el código no cambió después del SHA certificado; el cierre final queda sujeto al Quality Gate del commit documental final.
- Estado final: `FASE_3.1.5=CERRADA/CERTIFICADA/FINAL`; `FASE_3.1=CERRADA/CERTIFICADA/FINAL`; `ORACLE_315_CERTIFIED=TRUE`.
