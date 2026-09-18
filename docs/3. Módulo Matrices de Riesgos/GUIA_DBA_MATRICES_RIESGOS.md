# Guía DBA — Matrices de Riesgos

## Conexión y orden

Conectarse mediante el alias institucional `hpprod1` al esquema `RIESGO_LAVADO` sin registrar credenciales. Orden: backup autorizado, `oracle/01_preflight_release.sql`, install/upgrade controlado, seeds autorizados, postflight. El entrypoint destructivo de transición no forma parte de una ejecución automática.

## Queries read-only esenciales

```sql
SELECT SYS_CONTEXT('USERENV','CURRENT_SCHEMA') FROM DUAL;
SELECT VER_ID, VER_CODIGO, VER_VERSION, VER_ESTADO, VER_VIGENTE, VER_HASH
  FROM RL_MR_VERSIONES_FORMULARIO WHERE VER_ID IN (61,63);
SELECT COUNT(*) FROM RL_MR_RIESGOS;
SELECT COUNT(*) FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_VERSION_ID=61;
SELECT COUNT(*) FROM USER_OBJECTS WHERE OBJECT_NAME LIKE 'RL_MR_%' AND STATUS <> 'VALID';
SELECT TABLE_NAME, CONSTRAINT_NAME, STATUS FROM USER_CONSTRAINTS
 WHERE TABLE_NAME LIKE 'RL_MR_%' AND STATUS <> 'ENABLED';
```

## Preflight/postflight

Los scripts de deployment detienen la ejecución ante schema incorrecto, objeto faltante, V1 alterada, V2 publicada, duplicados, huérfanos, JSON inválido u objetos inválidos. No contienen DML.

## Seeds y migración

Las semillas institucionales son distintas de `TEST_DATA`. Los placeholders `GTIC`/`MITIGAR` no deben entrar en un seed productivo. La utilidad aprobada es `tools/MatricesRiesgosMigrator`; usar dry-run, backup previo, `ENRICH_EXISTING` e idempotencia. No remigrar los 59 históricos durante Fase 7.

## Backup/restore

Usar Data Pump o mecanismo institucional con `CONTENT=ALL`, incluyendo metadatos y datos del esquema autorizado. Verificar archivo, checksum, log de exportación, tamaño y fecha. Restaurar únicamente en un objetivo aislado, validar objetos/constraints/versiones/conteos y ejecutar smoke. No se incluyen passwords ni wallets en el repositorio.

## Rollback

El rollback normal es lógico y conserva V1, sus 59 evaluaciones y los riesgos. `05_rollback_v2_draft_guarded.sql` solo puede retirar V2 si permanece DRAFT, vigente 0, hash esperado y sin bindings; requiere el argumento explícito y debe ejecutarse con respaldo validado.
