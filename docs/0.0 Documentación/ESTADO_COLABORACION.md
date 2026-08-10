# Estado de colaboración y punto de continuidad

> Actualización 2026-08-10: **BE-03 — Health & Readiness Probes** fue implementado y certificado en `desarrollo`. `/healthz` confirma exclusivamente liveness del proceso HTTP y no consulta Oracle ni servicios externos. `/readyz` valida la disponibilidad mínima de Oracle mediante una consulta de solo lectura `SELECT 1 FROM DUAL`, con timeout configurable y acotado, respuesta agregada mínima y sin exposición de cadenas de conexión, SQL, credenciales ni mensajes de excepción. El HEAD técnico `c095c437be544899186dd945bc1b3040c32f7156` fue certificado por GitHub Actions Quality Gates Run `31404261933` (#563) en **SUCCESS**: Backend 277/277, Frontend 162/162, E2E 13/13, build Release 0 errores/0 advertencias y `npm audit` con 0 vulnerabilidades. Oracle no fue conectado ni ejecutado por CI ni por esta intervención; `main` permanece intacta y el PR #20 debe continuar abierto y en borrador.

Documento vivo. Debe actualizarse al finalizar cada intervención. Los antecedentes históricos permanecen en [`BITACORA_COLABORACION.md`](../../BITACORA_COLABORACION.md).

---

## 1. Línea base vigente

- **Repositorio:** `jmejia31/RIESGO_LAVADO`
- **Rama obligatoria de trabajo:** `desarrollo`
- **HEAD técnico BE-03 certificado:** `c095c437be544899186dd945bc1b3040c32f7156`
- **Rama estable:** `main`
- **HEAD de `main`:** `727082c6fcf90f95ce6db5eadf5c4b152397d080`
- **Política `main`:** no modificar ni integrar sin autorización expresa de Javier Mejía
- **Ramas remotas permitidas por protocolo:** únicamente `main` y `desarrollo`
- **PR de revisión:** #20 — abierto, en borrador y sin autorización de fusión
- **Arquitectura:** monolito modular con Angular, ASP.NET Core y Oracle 11g
- **Modelo vigente de Matrices:** 17 tablas `RL_MR_*` y 17 secuencias

---

## 2. Última intervención — BE-03

- **Intervención:** BE-03 — `/healthz` + `/readyz`
- **Fecha:** 2026-08-10 (UTC-6)
- **Autor:** ChatGPT
- **Rama:** `desarrollo`
- **Base de inicio:** `fad9abd579a4aec76a2b174d8bb9edcb8d943d38`
- **HEAD técnico certificado:** `c095c437be544899186dd945bc1b3040c32f7156`
- **Quality Gate técnico:** Run `31404261933` (#563) — **SUCCESS**
- **Estado:** **BE-03 completado y certificado técnicamente**.

### Cambios certificados

1. **Liveness `/healthz`**
   - Ruta raíz `GET /healthz`.
   - Acceso anónimo para infraestructura/orquestadores.
   - Respuesta `200` con contrato mínimo `{ "status": "Healthy" }`.
   - No invoca readiness, Oracle ni servicios externos.

2. **Readiness `/readyz`**
   - Ruta raíz `GET /readyz`.
   - Acceso anónimo para infraestructura/orquestadores.
   - Ejecuta una comprobación mínima de Oracle en solo lectura mediante `SELECT 1 FROM DUAL`.
   - Responde `200` con `Healthy` cuando Oracle está disponible.
   - Responde `503` con `Unhealthy` cuando la dependencia no está disponible.
   - No expone host, service name, usuario, contraseña, connection string, SQL, `ORA-*`, stack traces ni mensajes de excepción.

3. **Timeout y resiliencia del probe**
   - Configuración `HealthChecks:OracleTimeoutSeconds`.
   - Valor por defecto: 3 segundos.
   - Límite efectivo: mínimo 1 segundo, máximo 10 segundos.
   - Cancelación del cliente se propaga; timeout interno produce readiness negativa segura.
   - Logging operativo registra únicamente el tipo de excepción, no el mensaje técnico.

4. **Pruebas BE-03**
   - `/healthz` devuelve 200/Healthy y demuestra que no llama al probe de readiness.
   - `/readyz` disponible devuelve 200/Healthy.
   - `/readyz` no disponible devuelve 503/Unhealthy sin detalle técnico.
   - Se verifica `AllowAnonymous` y rutas exactas `/healthz` y `/readyz`.
   - Se verifica el acotamiento del timeout Oracle.

5. **Documentación operativa**
   - `appsettings.example.json` documenta el timeout de readiness sin secretos reales.
   - `RL.API.http` incluye llamadas manuales a `/healthz` y `/readyz`.

---

## 3. Evidencia de verificación BE-03

### GitHub Actions — Quality Gates

- **Run:** `31404261933`
- **Número:** #563
- **Conclusión:** **SUCCESS**
- **Build Release:** 0 errores, 0 advertencias
- **Backend:** 277/277 pruebas aprobadas; 0 fallidas; 0 omitidas
- **Frontend:** 162/162 pruebas aprobadas en 25 archivos
- **E2E Playwright:** 13/13 aprobadas
- **NPM audit:** 0 vulnerabilidades
- **Cobertura Backend:** líneas 20.79%; ramas 23.44%
- **Cobertura Frontend:** sentencias 39.53%; ramas 35.24%; funciones 35.99%; líneas 39.15%
- **Validadores de BD, preparación Oracle, inventario 17/17, autorización y contrato UAT:** aprobados

### Oracle

Durante desarrollo y certificación CI de BE-03:

- **NO** se abrió conexión a Oracle real;
- **NO** se ejecutó el endpoint `/readyz` contra el ambiente Oracle;
- **NO** se ejecutó DDL;
- **NO** se ejecutó DML;
- **NO** se ejecutaron scripts de transición;
- **NO** se modificó el esquema ni los respaldos `B10_*`.

La consulta `SELECT 1 FROM DUAL` existe únicamente como lógica runtime de `/readyz`; será ejecutada solo cuando el endpoint sea invocado en un ambiente configurado.

---

## 4. Estado consolidado del Plan de Mejoras Integrales

| Orden | Código | Estado |
|---:|---|---|
| 1 | GOV-01 — Sincronización Bitácora / UAT | **Completado** |
| 2 | BE-01 + FE-02 — ProblemDetails + Interceptor HTTP | **Completado y certificado** |
| 3 | BE-03 — `/healthz` + `/readyz` | **Completado y certificado** |
| 4 | BE-04 — Rate Limiting | **Siguiente** |
| 5 | BE-02 — Caché con invalidación explícita | Pendiente |
| 6 | DB-03 — Profiling Oracle / `EXPLAIN PLAN` | Pendiente y sujeto a autorización/ambiente |
| 7 | DB-01 — Política de archivado de auditoría | Pendiente de diseño; sin borrado automático |
| 8 | FE-03 + FE-04 — Accesibilidad + Skeleton Loaders | Pendiente |
| 9 | FE-01 — Signals gradual | Pendiente |
| 10 | GOV-02 + GOV-03 — Linter/Sonar + Docker multietapa | Pendiente |

---

## 5. Estado consolidado de Matrices de Riesgos

| Bloque | Estado vigente |
|---|---|
| Modelo reducido Oracle | **17 tablas + 17 secuencias** |
| Fase 10 — transición física | **Completada según evidencia histórica del proyecto** |
| Fase 11 — certificación funcional/Oracle | **Completada y certificada según evidencia histórica registrada** |
| Fase 12 — hardening NPM | **Completada — 0 vulnerabilidades** |
| Fase 13 — contrato/UAT automatizado | **Certificación de repositorio completada; validación local residual pendiente según PR #20** |

La validación local residual de UAT permanece como actividad funcional/operativa independiente y no fue sustituida por BE-03.

---

## 6. Directrices activas e inviolables

1. Trabajar únicamente sobre `desarrollo`.
2. No modificar, fusionar ni publicar en `main` sin autorización expresa de Javier Mejía.
3. Mantener el PR #20 abierto y en borrador.
4. No habilitar auto-merge.
5. No ejecutar Oracle, DDL ni DML como parte de mejoras pendientes salvo autorización formal y alcance específico.
6. No exponer credenciales, cadenas de conexión, secretos, errores Oracle ni detalles internos al cliente.
7. `/healthz` debe permanecer independiente de Oracle y dependencias externas.
8. `/readyz` debe conservar respuesta agregada mínima y una comprobación de solo lectura.
9. Reintentos automáticos HTTP únicamente para `GET` y solo ante `0/503/504`.
10. Nunca reintentar automáticamente `POST`, `PUT`, `DELETE` o `PATCH`.
11. La caché futura deberá tener invalidación explícita ante publicación de versiones y cambios de catálogos/reglas.
12. Antes de crear índices Oracle, ejecutar y documentar profiling/`EXPLAIN PLAN` en el ambiente autorizado.
13. La bitácora es histórica e inmutable: las correcciones futuras se agregan mediante una nueva entrada, no reescribiendo registros previos.
14. `ESTADO_COLABORACION.md` es el documento vivo y puede consolidarse conforme cambie el estado real.

---

## 7. Pendientes independientes

- Validación UAT local residual indicada en PR #20.
- Si la cuenta Oracle de Desarrollo continúa bloqueada, el desbloqueo corresponde exclusivamente al DBA/administrador autorizado.
- Prueba operativa futura de `/readyz` contra Oracle Desarrollo solo cuando exista ambiente/autorización apropiados; no bloquea la certificación de repositorio de BE-03.

---

## 8. Punto exacto de continuación

**GOV-01, BE-01, FE-02 y BE-03 quedan cerrados técnicamente.**

El siguiente paso de la secuencia aprobada es:

### BE-04 — Rate Limiting

Priorizar protección de:

- autenticación/login;
- exportación de reportes;
- carga de evidencias;
- otros endpoints de alto costo o abuso potencial identificados durante la implementación.

No iniciar BE-02, DB-03, DDL/DML Oracle ni otras fases fuera de secuencia hasta cerrar y certificar BE-04.
