# P06 — Decisión de seguridad: ejecución QA aislada

**Fecha:** 2026-11-06. **Dictamen:** `ARQUITECTURA_PROPUESTA_PENDIENTE_APROBACION_INSTITUCIONAL`. **Origen:** [Issue #25](https://github.com/jmejia31/RIESGO_LAVADO/issues/25).

## Hechos verificados

- `desarrollo` está **sin protección efectiva** (`protected=false`); la API de rulesets devolvió HTTP 403 por elegibilidad de la cuenta/plan. El environment lógico QA y workflows manuales no impiden exfiltración desde un workflow alterable.
- Quality Gate integral P06: [42266632011](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/42266632011), `SUCCESS` sobre `edd8328b389f63ba7c9cbac586590eb0c5cc792b`. La validación documental no certifica identidades ni accesos.
- La cuenta de QA, las variables del environment y el destino institucional **no han sido verificados por esta revisión**; jamás inferir valores técnicos ni permisos de sus nombres.
- Oracle institucional para QA **no** es una base descartable.

## Decisión arquitectónica preferente: ruta local aislada, sin secretos en GitHub

**Regla central: ningún proceso que ejecute código de `desarrollo`, Codex, el watcher autónomo o GitHub Actions tendrá acceso a contraseñas, tokens o conectividad privilegiada de QA.**

1. El propietario y el custodio de infraestructura aprueban **una estación/identidad de sistema operativo separada** para QA, sin Codex, Antigravity, watcher, checkout mutable ni credenciales de escritura GitHub. El usuario que opera Codex no puede acceder al almacén de secretos del ejecutor QA.
2. El ejecutor QA utiliza un **paquete de pruebas independiente**, previamente revisado y autorizado por un responsable humano, fijado mediante SHA-256 y preferentemente firmado, instalado offline con ACL de solo lectura. Un hash solo verifica integridad, **no demuestra seguridad**. Nunca descargar ni ejecutar dinámicamente código, dependencias, scripts o instrucciones desde `desarrollo` después de introducir credenciales.
3. Antes del primer login, infraestructura verifica el **origen HTTPS, certificado TLS, despliegue/release aprobado y perímetro de red** del frontend y API QA. No introducir credenciales en una SPA de procedencia no comprobada; JavaScript malicioso del sitio podría exfiltrarlas.
4. El propietario/custodio introduce credenciales **solo por el almacén corporativo o entrada protegida del ejecutor separado**, nunca en Git, Issues, acciones, comandos CLI visibles, variables de entorno compartidas, historial, logs, capturas ni conversación. Definir vencimiento, rotación y revocación. No pedir contraseñas por ChatGPT.
5. El DBA proporciona una cuenta **Oracle distinta de Javier QA, de solo lectura y mínimo privilegio**: `SELECT` únicamente en objetos/vistas autorizadas. No `SYS`, `SYSTEM`, DBA, `SELECT ANY TABLE`, `INSERT/UPDATE/DELETE`, DDL ni procedimientos mutantes. El DBA verifica por canal restringido servicio, `DB_UNIQUE_NAME`, base, esquema y grants antes de consultar datos. Si faltan permisos de observación, solicitar atestación DBA, nunca elevar privilegios.
6. La cuenta **Javier QA** se valida como identidad real, con su rol y módulos efectivos, sin asumir `ADMINISTRADOR`. Las primeras pruebas son de **navegación y lectura**. Matrices permanece sin mutaciones hasta una nueva autorización formal de fixtures/IDs, auditoría, dependencias, limpieza selectiva y recuperación.
7. El ejecutor genera únicamente **evidencia sanitizada**: ID de revisión/ejecución, SHA fijo, controles booleanos, conteos agregados y dictamen. Nunca logs de sesión, cookies, JWT, cabeceras Authorization, PII, dumps ni direcciones internas. Evidencias restringidas permanecen bajo custodia institucional. No publicar desde la estación QA mediante GitHub tokens de escritura.
8. La ruta GitHub Actions sigue en modo **sin secretos**. El workflow existente `.github/workflows/qa-config-preflight.yml` permanece manual y estático. No afirmar que un upgrade por sí solo habilita revisores obligatorios de environment en repos privados. Plan, visibilidad y reglas no se cambian automáticamente.

## Criterio de habilitación

Esta decisión **no** habilita el acceso por sí sola. Antes de retirar `QA_SECRET_ACCESS_GATE=BLOCKED`, se requiere autorización institucional documentada del modelo, equipo/identidad segregados, paquete revisado, despliegue/TLS confiable, custodia de credenciales, identidad Oracle exacta, grants mínimos y consentimiento para pruebas reales. Cualquier discrepancia es STOP; no ejecutar SQL de escritura para comprobar permisos.

La alternativa futura de un CI con secretos solo puede estudiarse cuando existan **protección real comprobada** de rama y workflows, permisos efectivos, mecanismo de revisión y análisis de amenazas independiente. No se presupone disponibilidad por el plan.

```ini
QA_SECURITY_DESIGN=ISOLATED_MANUAL_EXECUTOR_PROPOSED
QA_OWNER_INFRA_APPROVAL=PENDING
QA_GITHUB_ACTIONS_WITH_QA_SECRETS=DISABLED
QA_TRUSTED_EXECUTOR=NOT_VERIFIED
QA_TRUSTED_TEST_PACKAGE=NOT_AVAILABLE
QA_SECRET_ACCESS_GATE=BLOCKED_PENDING_EXTERNAL_CONTROLS
QA_SECRETS_INSTALLED=NOT_VERIFIED
QA_DB_TARGET_VERIFIED=NO
QA_DB_READONLY_ACCESS=NOT_VERIFIED
QA_WEB_LOGIN_VERIFIED=NO
QA_AUTH_REAL_E2E=NOT_EXECUTED
QA_MUTATIONS=DISABLED
BLOCK10_AUTHORIZED=NO
```

Siguiente control: [Checklist de aceptación](CONTROL_ACEPTACION_SEGURIDAD_QA.md). La preparación documental previa permanece en [Plan P06](PLAN_PREPARACION_QA.md).
