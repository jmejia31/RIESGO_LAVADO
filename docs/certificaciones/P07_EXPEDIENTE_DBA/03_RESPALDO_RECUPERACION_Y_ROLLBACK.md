# P07 — Respaldo, recuperación y rollback

## Resultado de la revisión

La documentación y los scripts describen controles de respaldo, preflight, postflight y rollback, pero no existe en esta intervención una ejecución Oracle ni un respaldo verificable. Por lo tanto:

P07_BACKUP_RECOVERY_REVIEW=DOCUMENTARY_ONLY

P07_ROLLBACK_CERTIFICATION=NOT_EXECUTED

## Controles documentados

- GUIA_DBA_MATRICES_RIESGOS.md exige respaldo antes de actualizaciones y restauración únicamente sobre un destino aislado.
- database/00_MANIFIESTO_SCRIPTS_APROBADOS.md clasifica el script 06 como manual, destructivo y fuera de los maestros automáticos.
- database/19_matrices_riesgos/transicion/06_reconstruir_modelo_17_tablas.sql exige esquema, autorización literal y condiciones de ejecución.
- deployment/matrices-riesgos/oracle/05_rollback_v2_draft_guarded.sql contiene una ruta lógica y guardada de rollback, no una prueba ejecutada en este expediente.

## Evidencia mínima requerida para cerrar P07

El DBA debe entregar, con datos sensibles sanitizados:

1. Identificador del respaldo y fecha/hora.
2. Tipo y alcance del respaldo, incluyendo metadatos necesarios.
3. Verificación de restaurabilidad en una instancia no productiva o procedimiento equivalente aprobado.
4. Hash o identificador inmutable del artefacto de respaldo.
5. Registro de prueba de rollback, incluyendo estado antes y después.
6. Confirmación de que la restauración no afectó producción ni datos protegidos.
7. Duración, responsable, aprobación y resultado.

## Límites

No se autoriza inferir la existencia de un respaldo desde la presencia de un script. No se autoriza ejecutar DROP, TRUNCATE, DELETE, DDL, migraciones o rollback sobre Oracle institucional como parte de esta preparación.

## Dictamen provisional

La preparación documental es suficiente para solicitar revisión DBA, pero no para afirmar recuperación probada, RPO/RTO cumplidos ni rollback certificado.

## Matriz formal de respaldo y recuperación

| CONTROL_ID | BACKUP_TYPE | BACKUP_SCOPE | REQUIRED_EVIDENCE | VALIDATION_METHOD | RESTORE_REQUIREMENT | RESPONSIBLE_ROLE | RISK | STATUS |
|---|---|---|---|---|---|---|---|---|
| BAK-01 | Data Pump o respaldo institucional aprobado | Esquema RIESGO_LAVADO y metadatos autorizados | Identificador, fecha, alcance y hash | Revisión DBA del catálogo de respaldo | Restauración en destino controlado | DBA | R06 | PENDING_DBA |
| BAK-02 | Export lógico | Tablas, índices, constraints y secuencias del release | Log sanitizado de export y parámetros | Comparación con inventario aprobado | Importación en ambiente no productivo | DBA | R06 | PENDING_DBA |
| BAK-03 | Respaldo físico institucional | Base o tablespaces según política | Referencia del backup y retención | Verificación por custodio institucional | Procedimiento aprobado de restore | DBA/Backup | R06 | PENDING_DBA |
| BAK-04 | Copia de configuración | Scripts y manifiestos del release | SHA de commit y expediente | GitHub remoto y revisión de alcance | Reproducir paquete sin secretos | Responsable técnico | R01 | PASS_DOCUMENTARY_ONLY |
| REC-01 | Restauración controlada | Objetos del release restaurado | Log de restore y conteos | Preflight/postflight de solo lectura | Ambiente aislado | DBA | R07 | PENDING_DBA |
| REC-02 | Integridad post-restore | Constraints, índices y objetos válidos | Matriz física antes/después | ALL_CONSTRAINTS, ALL_INDEXES, ALL_OBJECTS | Cero inválidos o justificación | DBA | R04 | PENDING_DBA |
| REC-03 | Integridad referencial post-restore | Relaciones 17+8 | Conteos de huérfanos | Consultas NOT EXISTS por FK | Sin huérfanos no justificados | DBA | R04 | PENDING_DBA |
| REC-04 | Auditoría post-restore | RL_AUDITORIA y objetos relacionados | Conteos y continuidad de auditoría | Consulta de solo lectura | Registros preservados | DBA/Auditoría | R05 | PENDING_DBA |
| REC-05 | Fallo parcial | Operación interrumpida y estado residual | Log de incidente y estado final | Revisión de cleanup/rollback | Estado coherente sin datos parciales | DBA/Responsable técnico | R08 | PENDING_DBA |
| RBK-01 | Rollback transaccional | DML dentro de una transacción | Evidencia de BEGIN/ROLLBACK y conteos | Prueba controlada no productiva | No aplica a DDL confirmado | Backend/DBA | R08 | PENDING_DBA |
| RBK-02 | Rollback lógico | Objetos nuevos y cambios reversibles | Script guardado y aprobación | Ejecución en ambiente controlado | Resultado postflight | DBA | R08 | PENDING_DBA |
| RBK-03 | Restauración física | Cambios DDL no transaccionales | Backup previo y registro de restore | Restore institucional aprobado | RPO/RTO confirmados | DBA/Backup | R07 | PENDING_DBA |
| RBK-04 | Recuperación Data Pump | Esquema exportado | Dump custodiado y hash | Importación de prueba | Verificación 17+8 y auditoría | DBA | R07 | PENDING_DBA |
| RBK-05 | Contingencia | Falla de conexión, permisos o instancia | Runbook y contactos | Simulacro autorizado | Escalamiento sin cambios no aprobados | DBA/Operaciones | R06 | PENDING_DBA |

## No equivalencias

- Rollback transaccional revierte DML de una transacción; no demuestra reversión de DDL.
- Rollback lógico aplica acciones compensatorias guardadas; no es restauración física.
- Restauración física recupera una copia de base o tablespace; requiere backup y ventana autorizada.
- Data Pump restaura un export lógico; no equivale automáticamente a un backup físico.
- Un respaldo institucional identificado no demuestra por sí solo que sea restaurable.

## RPO y RTO

RPO y RTO son requisitos que debe confirmar el responsable institucional. Este expediente no inventa objetivos ni declara cumplimiento. El DBA debe aportar los objetivos aprobados, el tiempo medido y la evidencia de aceptación.

## Lista exacta de evidencia DBA faltante

1. Identificador inmutable del backup y hash.
2. Alcance de objetos y metadatos incluidos.
3. Fecha/hora, custodio y política de retención.
4. Log sanitizado de restauración.
5. Inventario 25 tablas y 25 secuencias después de restore.
6. Estado de índices, constraints y objetos inválidos.
7. Conteo de huérfanos y prueba de auditoría preservada.
8. Resultado de rollback lógico y límites de DDL.
9. RPO/RTO aprobados y medidos.
10. Firma o aprobación DBA independiente.

P07_BACKUP_VERIFIED=PENDING_DBA

P07_RESTORE_VERIFIED=PENDING_DBA

P07_ROLLBACK_VERIFIED=PENDING_DBA

## Evidencia de respaldo reportada por el DBA

El DBA comunico: "En calidad y desarrollo no ejecutamos RMAN; solo Data Pump manuales". El propietario aporto evidencia visual de archivos de exportacion Data Pump asociados a hpprod1 y fechados el 21 de octubre de 2026. Esta declaracion y la existencia visual de archivos no equivalen a una prueba de exportacion exitosa ni de restaurabilidad.

```ini
BACKUP_METHOD=ORACLE_DATAPUMP_MANUAL
BACKUP_FILES_EVIDENCE=PROVIDED_BY_DBA
BACKUP_REFERENCE_DATE=2026-10-21
BACKUP_EXPORT_SUCCESS=NOT_INDEPENDENTLY_VERIFIED
BACKUP_RESTORE_TEST=NOT_EXECUTED
OWNER_ADDITIONAL_BACKUP_REQUEST=NO
DMP_FILES_PUBLISHED=NO
RMAN_EXECUTED_BY_COD=NO
EXPDP_IMPDP_EXECUTED_BY_COD=NO
```

No se verifico un respaldo especifico previo a la eliminacion de las tablas historicas. No se solicito RMAN, no se ejecutaron EXPDP/IMPDP y no se accedio a un servidor de respaldos en esta intervencion.
