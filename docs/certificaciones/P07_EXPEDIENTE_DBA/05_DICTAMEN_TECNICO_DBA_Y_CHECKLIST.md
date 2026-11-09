# P07 — Dictamen técnico DBA y checklist

## Identificación del expediente

| Campo | Valor |
|---|---|
| Proyecto | RIESGO_LAVADO |
| Rama | desarrollo |
| SHA de fuente revisado | db701c7456ca83568479933175a4283e73d955f8 |
| Quality Gate de referencia | 42688650542 — SUCCESS, intento 1 |
| Tipo de intervención | Documentación y análisis estático |
| Oracle institucional tocado | NO |
| Código funcional modificado | NO |
| Estado P07 | READY_FOR_DBA_REVIEW |

## Separación de evidencias

La certificación P06 es histórica y separada de este expediente:

    P06_SOURCE_CODE_SHA=4872c537324c51982fb613808526a7f8743a6762
    P06_QUALITY_GATE=42682601643
    P06_REAL_CERTIFICATION=PASS
    P06_AUTH_SCENARIOS=6/6

P07 no reabre ni sustituye esa evidencia y tampoco la convierte en evidencia DBA.

## Dictamen técnico provisional

La preparación documental P07 está completa para revisión institucional. La evidencia estática confirma que el repositorio contiene controles de identidad, preflight, inventario, dependencias, respaldo y rollback previstos. No confirma el estado físico de Oracle, los privilegios efectivos, la restaurabilidad del respaldo ni la ejecución de rollback.

El hallazgo principal que bloquea cualquier certificación física es la diferencia entre el inventario de 17 tablas/17 secuencias del paquete de transición y el inventario de 25 tablas/25 secuencias esperado por el preflight de release. Debe emitirse una decisión técnica antes de ejecutar cualquier script.

### Actualización de reconciliación

La observación 17/17 frente a 25/25 queda reconciliada estáticamente: el DDL 06 define el grupo operativo y el DDL 15 define las ocho tablas y ocho secuencias de configuración de cálculo; el preflight de release enumera las 25 tablas. La certificación física sigue pendiente porque no se ha consultado Oracle. Esta actualización prevalece sobre la redacción provisional anterior.

## Checklist para DBA

## Evidencia vigente de esta intervenciÃ³n

    SOURCE_CODE_SHA=c0cb48be2a1812b98b56ec93cda1c1509f03c4c3
    BASE_QUALITY_GATE=42695677107
    P07_OBSERVATION_001=RESOLVED_STATICALLY
    P07_OBSERVATION_002=RESOLVED
    P07_OBSERVATION_003=RESOLVED_DOCUMENTARILY
    P07_PREORACLE_STATIC_VALIDATION=PASS
    P07_PREORACLE_NEGATIVE_REGRESSIONS=PASS
    P07_LIVE_ORACLE_VALIDATION=NOT_EXECUTED
    P07_DBA_SIGNOFF=PENDING
    P07_OWNER_ACCEPTANCE=PENDING
    P07_CERTIFICATION=NOT_CERTIFIED

La anotaciÃ³n histÃ³rica sobre `configuration.GetConnectionString("OracleDB")` queda supersedida por la verificaciÃ³n vigente: el contrato del validador reconoce `OracleTestDatabaseProfile.TryResolveConfiguredLocalXe(...)`, exige el perfil `LOCAL_XE` explÃ­cito y rechaza cualquier fallback genÃ©rico `OracleDB`. La ejecuciÃ³n actual del validador y sus regresiones negativas se registran como PASS/FAIL_CLOSED segÃºn corresponda.

- [ ] Identificar ambiente, servicio, DB_UNIQUE_NAME, esquema y sesión.
- [ ] Confirmar que API y DBA apuntan al mismo destino.
- [ ] Resolver la discrepancia 17/17 frente a 25/25.
- [ ] Ejecutar únicamente el preflight autorizado y conservar salida sanitizada.
- [ ] Confirmar objetos válidos y restricciones habilitadas.
- [ ] Confirmar dependencias, índices, claves únicas y referencias externas.
- [ ] Revisar grants directos, roles efectivos y privilegios mínimos.
- [ ] Identificar triggers/procedimientos de auditoría aplicables.
- [ ] Registrar respaldo verificable antes de cualquier cambio.
- [ ] Probar restauración/rollback en el contexto autorizado.
- [ ] Obtener aprobación DBA y aprobación funcional separadas.
- [ ] Publicar solo evidencia sanitizada, nunca secretos, dumps ni datos identificables.

## Estado y límites

    P07_DOSSIER_PREPARATION=COMPLETE
    P07_DOSSIER_STATUS=READY_FOR_DBA_REVIEW
    P07_LIVE_ORACLE_VALIDATION=NOT_EXECUTED
    P07_BACKUP_EVIDENCE=PENDING_DBA
    P07_ROLLBACK_EVIDENCE=PENDING_DBA
    P07_DBA_SIGNOFF=PENDING
    P07_OWNER_ACCEPTANCE=PENDING
    P07_CERTIFICATION=NOT_CERTIFIED
    P07_STATUS=OPEN

## Matriz de aceptación P07

| CONTROL_ID | CONTROL | DOCUMENT_REFERENCE | VERIFICATION_METHOD | EVIDENCE_STATUS | RESULT | REMAINING_ACTION | RESPONSIBLE_ROLE |
|---|---|---|---|---|---|---|---|
| P07-01 | Identidad Oracle | Documento 01 | Consulta DBA de solo lectura | PENDING_DBA | NOT_VERIFIED | Aportar sesión, servicio y DB_UNIQUE_NAME | DBA |
| P07-02 | Inventario físico | Documento 01 | USER_TABLES/USER_SEQUENCES | PENDING_DBA | NOT_VERIFIED | Confirmar 25/25 | DBA |
| P07-03 | Alineación de modelos | Documento 01 | Comparación DDL 06 + DDL 15 + preflight | STATIC_VERIFIED | PASS_STATIC | Confirmar físicamente | Responsable técnico/DBA |
| P07-04 | Integridad de objetos | Documento 02 | ALL_OBJECTS, constraints e índices | PENDING_DBA | NOT_VERIFIED | Cero inválidos/deshabilitados | DBA |
| P07-05 | Integridad referencial | Documento 02 | Metadatos y NOT EXISTS de huérfanos | PENDING_DBA | NOT_VERIFIED | Confirmar FKs y huérfanos | DBA |
| P07-06 | Dependencias entre módulos | Documento 02 | Revisión de objetos compartidos y vínculos | PENDING_DBA | NOT_VERIFIED | Confirmar RL_USUARIOS y auditoría | DBA |
| P07-07 | Auditoría protegida | Documento 02/04 | RL_AUDITORIA, triggers y registros | PENDING_DBA | NOT_VERIFIED | Evidencia sanitizada | DBA/Auditoría |
| P07-08 | Privilegios efectivos | Documento 04 | Grants y roles efectivos | PENDING_DBA | NOT_VERIFIED | Confirmar mínimos | DBA/Seguridad |
| P07-09 | Respaldo verificado | Documento 03 | Identificador, hash y alcance | PENDING_DBA | NOT_VERIFIED | Entregar evidencia | DBA/Backup |
| P07-10 | Restauración verificada | Documento 03 | Restore aislado y postflight | PENDING_DBA | NOT_VERIFIED | Ejecutar prueba autorizada | DBA |
| P07-11 | Rollback documentado | Documento 03 | Rollback lógico/físico diferenciado | PENDING_DBA | NOT_VERIFIED | Probar en ambiente autorizado | DBA |
| P07-12 | Validadores estáticos | Este documento | PowerShell, regresiones y diff | EXECUTED | PASS | Mantener en CI | COD |
| P07-13 | Quality Gate | GitHub Actions | Run sobre SHA final | EXECUTED | SUCCESS | Ninguna para este commit | COD/CI |
| P07-14 | Revisión DBA | Todo el expediente | Firma independiente | PENDING_DBA | NOT_VERIFIED | Dictamen DBA | DBA |
| P07-15 | Aceptación propietario | Todo el expediente | Aprobación expresa | PENDING_OWNER | NOT_VERIFIED | Aceptación funcional | Propietario |

## Separación formal de certificaciones

STATIC_REVIEW: ejecutada por COD sobre fuentes versionadas y validadores no conectados.

DBA_PHYSICAL_VERIFICATION: pendiente; requiere acceso autorizado y solo lectura o procedimientos aprobados.

DBA_SIGNOFF: pendiente; COD no lo emite.

OWNER_ACCEPTANCE: pendiente; COD no lo emite.

## Resultado actualizado

    P07_OBSERVATION_001=RESOLVED_STATICALLY
    P07_OBSERVATION_002=RESOLVED
    P07_OBSERVATION_003=RESOLVED_DOCUMENTARILY
    P07_MODEL_STATIC_RECONCILIATION=PASS
    P07_PREORACLE_VALIDATOR_ROOT_CAUSE=VERIFIED
    P07_PREORACLE_VALIDATOR_CONTRACT=UPDATED
    P07_LEGACY_CONNECTION_EXPECTATION=REMOVED
    P07_GENERIC_ORACLEDB_FALLBACK=FORBIDDEN
    P07_PREORACLE_STATIC_VALIDATION=PASS
    P07_PREORACLE_NEGATIVE_REGRESSIONS=PASS
    P07_DOSSIER_PREPARATION=COMPLETE
    P07_DOSSIER_STATUS=READY_FOR_DBA_REVIEW
    P07_LIVE_ORACLE_VALIDATION=NOT_EXECUTED
    P07_DBA_SIGNOFF=PENDING
    P07_OWNER_ACCEPTANCE=PENDING
    P07_CERTIFICATION=NOT_CERTIFIED
    P07_STATUS=OPEN

## Referencias del expediente

- [Identidad e inventario](01_IDENTIDAD_E_INVENTARIO_ORACLE.md)
- [Integridad referencial](02_INTEGRIDAD_REFERENCIAL_Y_DEPENDENCIAS.md)
- [Respaldo y rollback](03_RESPALDO_RECUPERACION_Y_ROLLBACK.md)
- [Seguridad y privilegios](04_SEGURIDAD_PRIVILEGIOS_Y_RIESGOS.md)

Este dictamen no constituye aprobación DBA, autorización de ejecución ni certificación física.

## Validaciones ejecutadas en esta preparación

- validate_matrices_phase9_oracle_dossier.ps1: PASS.
- validate_matrices_17_object_inventory.ps1: PASS; 17 tablas y 17 secuencias.
- test_matrices_17_object_inventory.ps1: PASS; 9 casos.
- validate_matrices_dynamic_ddl_alignment.ps1: PASS.
- validate_matrices_phase10_transition_package.ps1: PASS.
- tools/validate_database_scripts.ps1: PASS.
- tools/validate_documentation_links.ps1: PASS.
- test_matrices_preoracle_readiness.ps1: PASS; 8 casos, incluido canonical PASS y 7 regresiones fail-closed.
- git diff --check: PASS.
- validate_matrices_preoracle_readiness.ps1: BLOCKED por un hallazgo preexistente del contrato: la suite Oracle no contiene el token configuration.GetConnectionString("OracleDB"). No se modificó código para silenciarlo y no se presenta como PASS.

Estas validaciones son estáticas. Ninguna sustituye la ejecución DBA ni aporta evidencia de conexión, privilegios, respaldo, restauración o estado físico de Oracle.
