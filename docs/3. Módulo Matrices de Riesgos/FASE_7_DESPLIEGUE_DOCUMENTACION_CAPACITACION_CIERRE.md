# Fase 7 — Despliegue, documentación y capacitación

> Estado de esta intervención: `FASE_7=BLOCKED_EXTERNAL_ACTION` únicamente por clean install y restore Oracle en objetivo aislado no disponible localmente. No es un bloqueo funcional; el paquete y los procedimientos están preparados.

## Baseline y alcance

Baseline efectivo: `23637818731046d53c526dfc14cc9193d2b121de`, rama `desarrollo`. Fase 5.3 y Fase 6 permanecen cerradas e inmutables. Esta fase prepara y valida el release, continuidad, operación, documentación y capacitación; no publica V2 ni despliega producción.

## Release y Oracle

El inventario está en `FASE_7_INVENTARIO_RELEASE.md` y el manifest en `deployment/matrices-riesgos/release-manifest.json`. `01_preflight_release.sql` y `04_postflight_release.sql` son read-only y fail-closed para schema, objetos, familia, V1, V2, catálogos, regla, 59 riesgos, bindings, JSON, constraints y objetos inválidos. Install/upgrade son guardados e idempotentes: no ejecutan la transición destructiva ni publican V2.

## Continuidad

Rollback de código/configuración es redeploy/restauración aprobada. El rollback Oracle de V2 solo es lógico y condicionado; nunca elimina la historia V1. Backup/restore se documentan sin ejecutar restore destructivo en `hpprod1`. La matriz de contingencia cubre Oracle, API, frontend, storage, exportación, configuración, disk full y estados de versión.

## Paquetes y configuración

Backend `net10.0` y frontend Angular/Node 24.18.0 se construyen con Dockerfiles existentes, usuarios no root y healthchecks. Secretos entran por variables/secret store. La configuración productiva usa API relativa `/api`; no se distribuyen contraseñas, tokens, wallets, source maps sensibles ni credenciales de prueba.

## Harness y pruebas

La suite OracleIntegration final se ejecuta verde con 5/5: la aserción histórica `B10_*` fue alineada al modelo vigente (cero tablas retiradas) y el harness serializa conexiones para evitar timeouts de pool. Los resultados de regresión, smoke, validadores y Quality Gate se anexan en la bitácora y en el cierre final.

## Documentación y capacitación

Se entregan manual técnico, funcional, operativo, guía DBA, soporte, plan/material/checklist y plantillas. `TRAINING_MATERIAL=PASS`; `INSTITUTIONAL_TRAINING_EXECUTED=FALSE` hasta Fase 8.

## Defectos y deuda

P0/P1/CRITICAL/HIGH deben ser cero. Permanecen explícitos: placeholders de prueba (2), RTO/RPO no definidos, restore institucional sin objetivo aislado, deuda Jurídicas, VER_ID 27/28 y RBAC granular global. No constituyen una afirmación de producción desplegada.

## Conclusión

`DEPLOYMENT_PACKAGE_READY` y `DEPLOYMENT_VALIDATED` solo se declaran con evidencia ejecutada. `PRODUCTION_DEPLOYED` permanece `FALSE` salvo acta institucional posterior.

## Evidencia ejecutada en esta intervención

- Oracle preflight/postflight read-only: `PASS`; 25 tablas, 25 secuencias, familia 22, V1/V2 esperadas, 59 riesgos/evaluaciones/proyecciones, huérfanos/duplicados/bindings/JSON/objetos/constraints inválidos: `0`.
- Install guardado: `PASS` en modo `IDEMPOTENT_ALREADY_INSTALLED`; upgrade: `PASS` en modo `NO_SCHEMA_DELTA`; segunda ejecución de upgrade: `PASS`; V2 no fue publicada.
- Backend: build Release y publish `net10.0` PASS; smoke local `/healthz=200`, `/readyz=200` estable; frontend health `200`.
- Frontend: `npm ci`, lint, producción y `npm audit=0` PASS; tests `781/781`; E2E `36/36`.
- Backend: regresión completa `636/636`; OracleIntegration dedicado `5/5`; el harness ya no exige `B10_*` retiradas y serializa/limpia su pool.
- Validadores: estructura, SQL, documentación, manifest/config drift y release package PASS. Python no está instalado para `validate_agent_skills.py`; Docker no está disponible localmente.
- Restore real y clean install sobre un objetivo Oracle aislado: `NOT_EXECUTED_EXTERNAL_ENVIRONMENT`; no se declara PASS artificial ni se tocó `hpprod1` destructivamente.
