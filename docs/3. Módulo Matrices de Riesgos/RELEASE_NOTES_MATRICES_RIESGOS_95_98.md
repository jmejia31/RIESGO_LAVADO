# Release notes — Matrices de Riesgos 95% → 98%

## Baseline

`23637818731046d53c526dfc14cc9193d2b121de` en `desarrollo`. El SHA final y Quality Gate se registran en el cierre de Fase 7.

## Incluye

- Manifest de release, preflight/postflight Oracle read-only y wrappers fail-closed.
- Procedimientos documentados de instalación, upgrade, rollback, backup/restore y smoke.
- Empaquetado reproducible con SHA256SUMS.
- Manuales técnico, funcional, operativo, DBA y soporte.
- Material, checklist y plantillas de capacitación.
- Corrección del harness OracleIntegration: arquitectura `B10_*` retirada no se exige y la suite serializa conexiones ODP.NET para evitar agotamiento concurrente del pool.

## Estado V1/V2

V1 ID 61 continúa `PUBLISHED`, vigente 1 e inmutable. V2 ID 63 continúa `DRAFT`, vigente 0; Fase 7 no publica V2.

## Restricciones conocidas

`PRODUCTION_TEST_PLACEHOLDERS=2`: `GTIC` y `MITIGAR` siguen siendo datos de prueba y bloquean aceptación productiva si no se resuelven. La capacitación institucional aún no fue ejecutada y RTO/RPO no están definidos institucionalmente. El clean install/restore real ya fue ejecutado y validado con la excepción de aislamiento documentada en Fase 7; no es un pendiente abierto. El rendimiento de Monitoreo también quedó cerrado por decisión del propietario, con la latencia fría aceptada como restricción conocida. El seguimiento institucional restante está en issue #22.

## Instalación y soporte

Seguir el manifest, la [guía DBA](GUIA_DBA_MATRICES_RIESGOS.md) y el [manual operativo](MANUAL_OPERATIVO_MATRICES_RIESGOS.md). No incluir secretos en commits ni paquetes.
