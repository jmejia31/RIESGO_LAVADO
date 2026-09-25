# Cierre institucional para producción — estado y decisiones pendientes

Fecha de consolidación: **2026-09-23**  
Rama de trabajo: `desarrollo`  
Seguimiento: [GitHub issue #22](https://github.com/jmejia31/RIESGO_LAVADO/issues/22)

## Estado ya cerrado

- `FASE_5_3=CERRADA`.
- `FASE_6=CERRADA`.
- `FASE_7=CERRADA_EN_DESARROLLO_CON_EXCEPCION_DOCUMENTADA`.
- Oracle restaurado y validado: `25/25`, `RESTORE_DATOS_RL_MR_V2=PASS`, `PREFLIGHT_AFTER_RESTORE=PASS`, `POSTFLIGHT_AFTER_RESTORE=PASS`.
- `MONITOREO_PERFORMANCE_POINT=CLOSED_BY_OWNER`.
- `MONITOREO_WARM_PAGE_CACHE=PASS`.
- `COLD_ORIGIN_LATENCY=ACCEPTED_KNOWN_RESTRICTION`.
- No se autorizan ni requieren cambios físicos adicionales sobre `DNP_IHSS` o `MMATAMOROS`.
- `main` no se modifica por este cierre institucional.
- Reparación física Unicode Oracle: CERRADA (`ORACLE_UNICODE_PHYSICAL_REPAIR=PASS`, `FULL_MATRICES_UNICODE_DATABASE_REPAIR=PASS`, `PROJECTION_JSON_PARITY=PASS`, `MATRICES_UNICODE_RESIDUAL=0`).

## 1. Valores de prueba y contrato del responsable — CERRADO

Decisión expresa del propietario, **2026-09-23**:

```text
PRODUCTION_TEST_PLACEHOLDERS=2
PRODUCTION_TEST_PLACEHOLDERS_BLOCKING=0
PRODUCTION_VALUES_PENDING=0
ROTR-ALMACENBIENE-23.dueno_riesgo=GTIC       # TEST_DATA_ONLY
RCUMP-COMPRAS-37.respuesta_riesgo=MITIGAR    # TEST_DATA_ONLY
PRODUCTION_VALUES_DECISION=CLOSED_AS_TEST_DATA
```

Los dos valores anteriores pertenecen a datos de prueba y **no se promueven a valores institucionales productivos ni a seeds de producción**. Por tanto, no bloquean release ni despliegue.

El criterio funcional queda definido para los **dos campos de Identificación del riesgo mostrados al usuario**:

- **Área principal**: lista desplegable administrable mediante `MR_AREA_PRINCIPAL`, con opción `Otro / Escribir manualmente…`. El catálogo es independiente y nace vacío para no inventar áreas institucionales.
- **Responsable o dueño del riesgo / área responsable**: lista desplegable administrable mediante `MR_AREA_RESPONSABLE`, también con opción `Otro / Escribir manualmente…`. El catálogo nace vacío para no convertir `GTIC` ni otro dato de prueba en valor institucional por inferencia.
- Ambos usan el tipo oficial `selector-catalogo` con `permiteValorManual=true`; por tanto conservan los 9 tipos oficiales del Form Builder y no introducen un control nuevo.
- Cada campo tiene su **propio mantenimiento de catálogo** dentro del Form Builder, aunque una institución pueda cargar valores similares en ambos.
- Al clonar una versión existente para crear un nuevo borrador, la aplicación adapta automáticamente `area_principal` y `dueno_riesgo` al patrón flexible sin modificar la versión origen.
- `respuesta_riesgo` no forma parte de esta mejora. `RCUMP-COMPRAS-37.respuesta_riesgo=MITIGAR` continúa únicamente como dato de prueba no bloqueante y el contrato vigente del campo de respuesta no se altera.
- V1/V2 históricas no se mutan retroactivamente ni cambian sus hashes. La mejora aplica a nuevos borradores/versiones administrables.

## 2. RTO y RPO institucionales — CERRADOS

Decisión expresa del propietario del proyecto, **2026-09-24**:

- Documento canónico aprobado: `docs/3. Módulo Matrices de Riesgos/Cierre Institucional/ACTA_RTO_RPO_INSTITUCIONAL_SGRLA_IHSS_v2_0.md`.
- Copia física: `docs/3. Módulo Matrices de Riesgos/Cierre Institucional/Acta_RTO_RPO_Institucional_SGRLA_IHSS_v2_0.pdf`.
- La propuesta RTO v1.1 se conserva como antecedente histórico.

```text
RTO_DEFINED=TRUE
RTO_VALUE=4 horas
RTO_MAXIMUM_TOLERABLE=4 horas
INCIDENT_RESPONSE_TARGET=0 minutos
RTO_STATUS=CLOSED

RPO_DEFINED=TRUE
RPO_VALUE=15 minutos
RPO_TARGET=15 minutos
RPO_MAXIMUM_TOLERABLE=60 minutos
RPO_STATUS=CLOSED

RTO_RPO_PENDING_ITEMS=0
RTO_RPO_PROJECT_BLOCKERS=0
```

El eventual archivo posterior de firmas administrativas no constituye bloqueo ni pendiente de proyecto para estos dos puntos, salvo que una autoridad institucional decida formalmente modificar los valores.

## Reparación Unicode Oracle — CERRADA

Fecha de cierre técnico y ejecución física: **2026-09-25**
Ejecución: manual sobre base de datos Oracle institucional por propietario (`jmejia31`)

```text
ORACLE_UNICODE_PHYSICAL_REPAIR=PASS
FULL_MATRICES_UNICODE_DATABASE_REPAIR=PASS
UNICODE_PRECHECK_41=PASS
UNICODE_BACKUP_42=PASS
UNICODE_CORRECTION_43=PASS
UNICODE_POSTCHECK_44=PASS
UNICODE_BACKUP_ROWS=169
UNICODE_CORRECTION_UPDATES=275
MATRICES_SUSPICIOUS_COLUMNS=0
MATRICES_SUSPICIOUS_ROWS=0
MATRICES_UNICODE_RESIDUAL=0
PROJECTION_JSON_PARITY=PASS
DATA_INTEGRITY=PASS
MANUAL_UNICODE_EXECUTION_PENDING=NO
UNICODE_ROLLBACK_REQUIRED=NO
```

Detalle de ejecución y cierre:

- **Script 41 (Precheck)**: ejecutado con éxito (`FULL_MODULE_TOKEN_INVENTORY=PASS`, `AMBIGUOUS_TOKENS=0`, `UNMAPPED_TOKENS=0`).
- **Script 42 (Backup)**: ejecutado con éxito (`BACKUP_TABLE=RL_MR_UNI_BKP_20260924`, `MATRICES_UNICODE_BACKUP_ROWS=169`, `MATRICES_UNICODE_BACKUP_STATUS=PASS`). La tabla física de respaldo `RL_MR_UNI_BKP_20260924` se conserva intacta en el esquema institucional.
- **Script 43 (Corrección DML)**: ejecutado con éxito bajo transacción atómica bounded por backup (`CURRENT_SUSPICIOUS_CELLS=169`, `BACKUP_CELLS=169`, `BACKUP_COVERAGE=PASS`, `AMBIGUOUS_TOKENS=0`, `UNBACKED_MAPPING_TARGETS=0`, `CURRENT_SUSPICIOUS_CELLS_POST=0`, `MATRICES_UNICODE_CORRECTION_UPDATES=275`, `MATRICES_UNICODE_CORRECTION_STATUS=PASS`).
- **Script 44 (Postcheck final)**: ejecutado con éxito con paridad completa de campos (`PARITY_BAD_ROWS=0`, `PARITY_BAD_CODIGO=0`, `PARITY_BAD_AREA=0`, `PARITY_BAD_DUENO=0`, `PARITY_BAD_RESPUESTA=0`, `PARITY_BAD_INHERENTE=0`, `PARITY_BAD_RESIDUAL=0`, `PROJECTION_JSON_PARITY=PASS`, `MATRICES_UNICODE_RESIDUAL=0`, `MATRICES_UNICODE_POSTCHECK_STATUS=PASS`).
- **Script 45 (Rollback)**: script de contingencia disponible; **NO fue requerido** al no registrarse residuos ni fallos de integridad.
- **Instrucción operacional**: no volver a ejecutar 41, 42 ni 43. El saneamiento físico Oracle del módulo Matrices de Riesgos queda formal y técnicamente **CERRADO**.

Pendientes institucionales que continúan separados:

```text
TRAINING_MATERIAL=PASS
INSTITUTIONAL_TRAINING_EXECUTED=FALSE
RELEASE_READINESS=PASS
PRODUCTION_DEPLOYED=FALSE
```

## 3. Capacitación institucional

Material disponible:

- plan de capacitación;
- material de capacitación;
- checklist;
- plantilla de asistencia;
- plantilla de evaluación.

Estado:

```text
TRAINING_MATERIAL=PASS
INSTITUTIONAL_TRAINING_EXECUTED=FALSE
```

Para cerrar este punto se debe registrar al menos fecha, responsable, asistentes y evidencia de aceptación/evaluación.

## 4. Despliegue productivo

Estado:

```text
RELEASE_READINESS=PASS
PRODUCTION_DEPLOYED=FALSE
```

Cuando exista autorización institucional, la operación debe conservar evidencia de:

1. backup previo;
2. preflight;
3. despliegue del SHA autorizado;
4. postflight;
5. smoke final;
6. resultado y responsables.

No se cambia `PRODUCTION_DEPLOYED` a `TRUE` por documentación o intención; requiere ejecución real.

## Deudas no bloqueantes

- auditoría read-only de DB links en ambos schemas;
- revisión histórica `VER_ID 27/28`;
- RBAC granular global.

Estas deudas no reabren Fase 7 ni bloquean el cierre de desarrollo ya certificado.

## No reabrir

- reconstrucción o restore de Fase 7;
- índices, estadísticas o DDL en `DNP_IHSS` / `MMATAMOROS`;
- punto de rendimiento de Monitoreo;
- Fase 5.3, Fase 6 o Fase 7 por los cuatro pendientes institucionales anteriores.

## Impacto de esta consolidación

```text
INTERFACES_CAMBIAN=NO
DATOS_CAMBIAN=NO
TENANCY_CAMBIA=NO
SEGURIDAD_CAMBIA=NO
JOBS_EVENTOS_CAMBIAN=NO
CACHE_SESION_CAMBIA=NO
MEDIA_DOCUMENTOS_CAMBIAN=NO
OBSERVABILIDAD_CAMBIA=NO
MAPA_ARQUITECTURA=NO_APLICA
```

Es una consolidación documental y de gobierno de release; no modifica código, Oracle, contratos REST ni arquitectura.
