# Fase 7 — Despliegue, documentación y capacitación

> Estado vigente: `FASE_7=CERRADA_EN_DESARROLLO_CON_EXCEPCION_DOCUMENTADA`. El clean install, restore de datos y pre/postflight se ejecutaron realmente en `hpprod1/RIESGO_LAVADO` con autorización expresa del DBA y de Javier Mejía. El objetivo no fue físicamente aislado: `RIESGO_LAVADO` comparte la misma base con `DNP_IHSS`, por lo que el cierre registra esa excepción sin presentarla como aislamiento institucional. `PRODUCTION_DEPLOYED=FALSE`.

## Baseline y alcance

Baseline efectivo: `23637818731046d53c526dfc14cc9193d2b121de`, rama `desarrollo`. Fase 5.3 y Fase 6 permanecen cerradas e inmutables. Esta fase prepara y valida el release, continuidad, operación, documentación y capacitación; no publica V2 ni despliega producción.

## Release y Oracle

El inventario está en `FASE_7_INVENTARIO_RELEASE.md` y el manifest en `deployment/matrices-riesgos/release-manifest.json`. `01_preflight_release.sql` y `04_postflight_release.sql` son read-only y fail-closed para schema, objetos, familia, V1, V2, catálogos, regla, 59 riesgos, bindings, JSON, constraints y objetos inválidos. Install/upgrade son guardados e idempotentes: no ejecutan la transición destructiva ni publican V2.

## Continuidad

Rollback de código/configuración es redeploy/restauración aprobada. El rollback Oracle de V2 solo es lógico y condicionado; nunca elimina la historia V1. La prueba real de clean install/restore se ejecutó posteriormente en `hpprod1/RIESGO_LAVADO` con autorización expresa y excepción de aislamiento documentada; ver la actualización de cierre al final de este documento. La matriz de contingencia cubre Oracle, API, frontend, storage, exportación, configuración, disk full y estados de versión.

## Paquetes y configuración

Backend `net10.0` y frontend Angular/Node 24.18.0 se construyen con Dockerfiles existentes, usuarios no root y healthchecks. Secretos entran por variables/secret store. La configuración productiva usa API relativa `/api`; no se distribuyen contraseñas, tokens, wallets, source maps sensibles ni credenciales de prueba.

## Harness y pruebas

La suite OracleIntegration final se ejecuta verde con 5/5: la aserción histórica `B10_*` fue alineada al modelo vigente (cero tablas retiradas) y el harness serializa conexiones para evitar timeouts de pool. Los resultados de regresión, smoke, validadores y Quality Gate se anexan en la bitácora y en el cierre final.

## Documentación y capacitación

Se entregan manual técnico, funcional, operativo, guía DBA, soporte, plan/material/checklist y plantillas. `TRAINING_MATERIAL=PASS`; `INSTITUTIONAL_TRAINING_EXECUTED=FALSE` hasta Fase 8.

## Defectos y deuda

P0/P1/CRITICAL/HIGH deben ser cero. Permanecen explícitos como restricciones/deudas no bloqueantes del cierre de desarrollo: placeholders de prueba (2), RTO/RPO no definidos, excepción de aislamiento de la prueba real, latencia fría de Monitoreo contra el origen Oracle, VER_ID 27/28 y RBAC granular global. No constituyen una afirmación de producción desplegada.

## Conclusión

`DEPLOYMENT_PACKAGE_READY` y `DEPLOYMENT_VALIDATED` solo se declaran con evidencia ejecutada. `PRODUCTION_DEPLOYED` permanece `FALSE` salvo acta institucional posterior.

## Evidencia ejecutada en esta intervención

- Oracle preflight/postflight read-only: `PASS`; 25 tablas, 25 secuencias, familia 22, V1/V2 esperadas, 59 riesgos/evaluaciones/proyecciones, huérfanos/duplicados/bindings/JSON/objetos/constraints inválidos: `0`.
- Install guardado: `PASS` en modo `IDEMPOTENT_ALREADY_INSTALLED`; upgrade: `PASS` en modo `NO_SCHEMA_DELTA`; segunda ejecución de upgrade: `PASS`; V2 no fue publicada.
- Backend: build Release y publish `net10.0` PASS; smoke local `/healthz=200`, `/readyz=200` estable; frontend health `200`.
- Frontend: `npm ci`, lint, producción y `npm audit=0` PASS; tests `781/781`; E2E `36/36`.
- Backend: regresión completa `636/636`; OracleIntegration dedicado `5/5`; el harness ya no exige `B10_*` retiradas y serializa/limpia su pool.
- Validadores: estructura, SQL, documentación, manifest/config drift y release package PASS. Python no está instalado para `validate_agent_skills.py`; Docker no está disponible localmente.
- Estado histórico al momento del primer cierre documental: clean install/restore aislado estaba `NOT_EXECUTED_EXTERNAL_ENVIRONMENT`. Este punto quedó superado por la ejecución real autorizada descrita en la actualización 2026-09-23; el objetivo real no fue aislado y esa excepción queda explícita.
- Quality Gate del commit de release `70c1a7edffe614fa3cc36fab58b02077dbfe039b`: run `35393615771`, `completed/success`; el CI verificó también compose y builds de contenedores con usuarios non-root.


## Actualización de cierre real — 2026-09-23

Evidencia aportada por Javier Mejía durante la ejecución controlada y conservada como evidencia de sesión:

- Objetivo real: Oracle 11g, host `desdb`, servicio `hpprod1`, schema `RIESGO_LAVADO`.
- Autorización previa: respaldo/restore institucional disponible, DBA autorizó la prueba y Javier autorizó la eliminación temporal de los 465 registros RL_MR para probar reconstrucción y posterior restauración.
- Excepción de aislamiento: `PHYSICAL_ISOLATION=FALSE`. El schema comparte la base física con `DNP_IHSS`; no se declara un entorno aislado. La transición se limitó a objetos `RL_MR_*` y preservó `RL_USUARIOS`, `RL_AUDITORIA`, `SEQ_RL_AUDITORIA` y el schema `DNP_IHSS`.
- Clean install estructural verificado: reconstrucción base `17/17`, luego configuración de cálculo hasta `25 tablas / 25 secuencias`; `FAM_PREDETERMINADA` presente, constraint `CK_RL_MR_FUA_TYPE` habilitada, índice `UQ_RL_MR_FAMILIA_DEFAULT` válido, objetos inválidos `0`, constraints deshabilitadas `0`.
- Compatibilidad Oracle 11g corregida en `29_ddl_familia_predeterminada_fp1.sql` usando SQL dinámico para evitar referencia parse-time a la columna recién creada.
- Restore real validado: `RL_MR_TABLES=25`, `RL_MR_SEQUENCES=25`, `TOTAL_RL_MR_ROWS=465`, `RL_MR_RIESGOS=59`, `RL_MR_EVALUACIONES_RIESGO=59`, `RL_MR_PROYECCIONES_EVALUACION=59`, `RL_MR_SENALES_ALERTA=148`, `INVALID_OBJECTS=0`, `DISABLED_CONSTRAINTS=0`, `ORPHAN_USER_REFERENCES=0`, `FAMILY_22_DEFAULT=1`, `V1_61=1`, `V2_63=1`, `RESTORE_DATOS_RL_MR_V2=PASS`.
- Release preflight después del restore: `PREFLIGHT=PASS`; 25 tablas, 25 secuencias, familia 22, V1 61, V2 63, 4 catálogos, regla activa, 59 riesgos/evaluaciones/proyecciones, duplicados/huérfanos/bindings/JSON/objetos inválidos y constraints deshabilitadas en `0`.
- Release postflight después del restore: `POSTFLIGHT=PASS`.
- Diagnóstico read-only de dependencias de Monitoreo: acceso confirmado a `RL_LISTA_POSITIVOS`, objetos requeridos de `DNP_IHSS` y `MMATAMOROS`; no se ejecutó DDL sobre esos esquemas.
- Monitoreo de Listas: se retiró del backend la dependencia explícita del hint `IX_RCOINC_MON_TIPO_PATRONO` para mantener compatibilidad con producción. Se agregó caché corta e invalidable por página/filtros para Jurídicas, Naturales y Empleados sin modificar `DNP_IHSS` ni `MMATAMOROS`.
- Validación HTTP final del caché, todos `HTTP 200`: Jurídicas `12620 ms -> 24 ms`, Naturales `19128 ms -> 21 ms`, Empleados `22394 ms -> 20 ms`. La mejora de repetición queda verificada; la primera carga fría continúa limitada por el origen Oracle y no se reclasifica artificialmente como certificación production-like.
- Código funcional final de esta optimización: `aae6c8b19fbb0e89a8bcff77acb1a8a2c7fcbd18`; Quality Gate `#1505` completado en `success`.

### Estado de cierre

- `FASE_7=CERRADA_EN_DESARROLLO_CON_EXCEPCION_DOCUMENTADA`
- `RELEASE_READINESS=PASS`
- `CLEAN_INSTALL_REAL=PASS_WITH_NON_ISOLATED_ENVIRONMENT_EXCEPTION`
- `RESTORE_REAL=PASS`
- `PREFLIGHT_AFTER_RESTORE=PASS`
- `POSTFLIGHT_AFTER_RESTORE=PASS`
- `MONITOREO_WARM_PAGE_CACHE=PASS`
- `PRODUCTION_LIKE_PERFORMANCE_CERTIFICATION=FAIL_COLD_ORIGIN_LATENCY`
- `PRODUCTION_DEPLOYED=FALSE`
- `INSTITUTIONAL_TRAINING_EXECUTED=FALSE`

El trabajo de desarrollo/release de Fase 7 queda cerrado. Producción, capacitación institucional y cualquier optimización física sobre schemas ajenos siguen siendo actividades externas posteriores y no se presentan como ejecutadas.


### Decisión final sobre rendimiento de Monitoreo — 2026-09-23

Javier Mejía declara cerrado el punto de rendimiento de Monitoreo. Se acepta como restricción conocida la variabilidad de la primera carga contra el Oracle compartido y se considera satisfactoria la mitigación backend con caché corta e invalidable.

Última evidencia visible: Jurídicas `2800 -> 15 ms`, Naturales `1903 -> 14 ms`, Empleados `694 -> 13 ms`, todos HTTP 200.

```text
MONITOREO_PERFORMANCE_POINT=CLOSED_BY_OWNER
COLD_ORIGIN_LATENCY=ACCEPTED_KNOWN_RESTRICTION
MONITOREO_WARM_PAGE_CACHE=PASS
```

Este punto deja de ser pendiente del cierre de desarrollo/release. No autoriza cambios físicos en `DNP_IHSS` ni `MMATAMOROS`.
