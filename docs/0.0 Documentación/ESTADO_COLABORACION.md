# Estado de colaboración y punto de continuidad

> Actualización 2026-08-10: los hallazgos pendientes de **BE-01 + FE-02** fueron corregidos y certificados en `desarrollo` mediante el commit técnico `50067cfccebac85527f94ab8a97ba8aa03fea21e`. El Backend dejó de exponer mensajes técnicos mediante heurísticas Regex: solo una excepción pública explícita (`PublicProblemException`) puede transportar detalle funcional al cliente; `InvalidOperationException` genérica vuelve a 500. El Frontend mantiene reintentos exclusivamente para `GET` ante status `0/503/504`, con backoff exponencial explícito y cobertura ampliada. GitHub Actions Quality Gates Run `31400466132` (#538) terminó en **SUCCESS**: Backend 269/269, Frontend 162/162, E2E 13/13 y `npm audit` con 0 vulnerabilidades. Oracle no fue conectado ni ejecutado en esta intervención; `main` permanece intacta y el PR #20 debe continuar abierto y en borrador.

Documento vivo. Debe actualizarse al finalizar cada intervención. Los antecedentes históricos permanecen en [`BITACORA_COLABORACION.md`](../../BITACORA_COLABORACION.md).

---

## 1. Línea base vigente

- **Repositorio:** `jmejia31/RIESGO_LAVADO`
- **Rama obligatoria de trabajo:** `desarrollo`
- **Commit técnico BE-01 + FE-02 certificado:** `50067cfccebac85527f94ab8a97ba8aa03fea21e`
- **Rama estable:** `main`
- **HEAD de `main` verificado antes del cierre:** `727082c6fcf90f95ce6db5eadf5c4b152397d080`
- **Política `main`:** no modificar ni integrar sin autorización expresa de Javier Mejía
- **Ramas remotas permitidas por protocolo:** únicamente `main` y `desarrollo`
- **PR de revisión:** #20 — abierto, en borrador y sin autorización de fusión
- **Arquitectura:** monolito modular con Angular, ASP.NET Core y Oracle 11g
- **Modelo vigente de Matrices:** 17 tablas `RL_MR_*` y 17 secuencias

---

## 2. Última intervención

- **Intervención:** cierre técnico de hallazgos BE-01 + FE-02 posterior a revisión
- **Fecha:** 2026-08-10 (UTC-6)
- **Autor:** ChatGPT
- **Rama:** `desarrollo`
- **Commit inicial efectivo:** `dbf9a72d4af9cda530029a819d545e0c617e8e26`
- **Commit técnico publicado:** `50067cfccebac85527f94ab8a97ba8aa03fea21e`
- **Quality Gate remoto:** Run `31400466132` (#538) — **SUCCESS**
- **Estado:** **BE-01 + FE-02 técnicamente cerrados con evidencia CI**. No se otorga autorización implícita para fusionar a `main`, ejecutar Oracle ni iniciar trabajos fuera de la secuencia priorizada.

### Cambios certificados

1. **BE-01 — ProblemDetails / exposición segura**
   - Se eliminó la heurística `EsMensajeFuncionalSeguro` basada en Regex/lista negra.
   - Se creó `PublicProblemException` como única vía explícita para publicar un mensaje funcional de excepción.
   - `ArgumentException` genérica devuelve 400 con mensaje fijo.
   - `KeyNotFoundException` genérica devuelve 404 con mensaje fijo.
   - `UnauthorizedAccessException` devuelve 403 con mensaje fijo.
   - `InvalidOperationException` genérica ya no se clasifica universalmente como 400; cae en 500.
   - Los 500 muestran detalle técnico únicamente en ambiente Development; fuera de Development usan mensaje público fijo.
   - El log del servidor conserva la excepción y el `traceId` para diagnóstico.

2. **BE-01 — regresión adversarial**
   - Se añadieron pruebas para mensajes con `ORA-00942`, `SELECT`, SQL en minúsculas, nombres de tablas, timeouts y procedimientos.
   - Se verifica que esos mensajes no aparecen en la respuesta pública.
   - Se verifica expresamente que `InvalidOperationException` produce 500 y no 400.

3. **FE-02 — resiliencia HTTP**
   - Máximo de 2 reintentos.
   - Solo métodos `GET`.
   - Solo errores de red/status `0`, `503` o `504`.
   - `POST`, `PUT`, `DELETE` y `PATCH` no se reintentan automáticamente.
   - Backoff exponencial explícito: 300 ms y 600 ms para los dos reintentos permitidos.

4. **FE-02 — estado global y pruebas**
   - Se conserva el contador concurrente de peticiones activas de `GlobalHttpStateService`.
   - Se añadieron pruebas de concurrencia para evitar apagado prematuro del indicador global.
   - `401`, `403` y `499` quedan fuera del banner global porque sus flujos se gestionan por autenticación/cancelación.
   - Se añadieron pruebas de no-reintento para GET 400/500/502 y todos los verbos mutantes relevantes.

5. **Gobernanza**
   - La intervención se registra como una nueva entrada de `BITACORA_COLABORACION.md` sin reescribir el registro histórico anterior de Antigravity.
   - Este documento vivo se consolida al estado real vigente y deja los antecedentes en la bitácora.

---

## 3. Evidencia de verificación de esta intervención

### GitHub Actions — Quality Gates

- **Run:** `31400466132`
- **Número:** #538
- **Conclusión:** **SUCCESS**
- **Build Release:** 0 errores, 0 advertencias
- **Backend:** 269/269 pruebas aprobadas; 0 fallidas; 0 omitidas
- **Frontend:** 162/162 pruebas aprobadas en 25 archivos
- **Suite específica FE-02:** `http-resilience.interceptor.spec.ts` — 16/16 aprobadas
- **E2E Playwright:** 13/13 aprobadas
- **NPM audit:** 0 vulnerabilidades
- **Cobertura Backend:** líneas 20.68%; ramas 23.34%
- **Cobertura Frontend:** sentencias 39.53%; ramas 35.24%; funciones 35.99%; líneas 39.15%
- **Validadores de estructura, BD, inventario 17/17, autorización y contrato UAT:** aprobados

### Oracle

En esta intervención:

- **NO** se abrió conexión Oracle;
- **NO** se ejecutó DDL;
- **NO** se ejecutó DML;
- **NO** se ejecutaron scripts de transición;
- **NO** se modificó el esquema ni los respaldos `B10_*`.

Los validadores de preparación Oracle ejecutados por CI son validaciones estáticas/controladas y no equivalen a una nueva ejecución física de Oracle.

---

## 4. Estado consolidado de Matrices de Riesgos

| Bloque | Estado vigente |
|---|---|
| Modelo reducido Oracle | **17 tablas + 17 secuencias** |
| Fase 10 — transición física | **Completada según evidencia histórica del proyecto** |
| Fase 11 — certificación funcional/Oracle | **Completada y certificada según evidencia histórica registrada** |
| Fase 12 — hardening NPM | **Completada — 0 vulnerabilidades** |
| Fase 13 — contrato/UAT automatizado | **Certificación de repositorio completada; validación local residual pendiente según PR #20** |
| GOV-01 | **Completado** |
| BE-01 | **Completado y corregido** |
| FE-02 | **Completado y corregido** |
| BE-03 | **Siguiente elemento priorizado; no iniciado en esta intervención** |

### Validación local residual de UAT registrada en PR #20

Permanece como actividad funcional/operativa independiente:

1. login con roles institucionales autorizados;
2. recorrido real Plantillas → Riesgos → Evaluaciones → Flujos → Mitigación → Monitoreo;
3. evidencia real controlada;
4. descarga real Excel/PDF;
5. confirmación visual/UX sin errores bloqueantes;
6. confirmación de conservación de respaldos y restricciones operativas aplicables.

La corrección BE-01 + FE-02 no sustituye esa validación UAT local.

---

## 5. Secuencia priorizada vigente del Plan de Mejoras Integrales

| Orden | Código | Estado |
|---:|---|---|
| 1 | GOV-01 — Sincronización Bitácora / UAT | **Completado** |
| 2 | BE-01 + FE-02 — ProblemDetails + Interceptor HTTP | **Completado y certificado tras correcciones** |
| 3 | BE-03 — `/healthz` + `/readyz` | **Siguiente** |
| 4 | BE-04 — Rate Limiting | Pendiente |
| 5 | BE-02 — Caché con invalidación explícita | Pendiente |
| 6 | DB-03 — Profiling Oracle / `EXPLAIN PLAN` | Pendiente y sujeto a autorización/ambiente |
| 7 | DB-01 — Política de archivado de auditoría | Pendiente de diseño; sin borrado automático |
| 8 | FE-03 + FE-04 — Accesibilidad + Skeleton Loaders | Pendiente |
| 9 | FE-01 — Signals gradual | Pendiente |
| 10 | GOV-02 + GOV-03 — Linter/Sonar + Docker multietapa | Pendiente |

---

## 6. Directrices activas e inviolables

1. Trabajar únicamente sobre `desarrollo`.
2. No modificar, fusionar ni publicar en `main` sin autorización expresa de Javier Mejía.
3. Mantener el PR #20 abierto y en borrador.
4. No habilitar auto-merge.
5. No ejecutar Oracle, DDL ni DML como parte de las mejoras pendientes salvo autorización formal y alcance específico.
6. No exponer credenciales, cadenas de conexión, secretos, errores Oracle ni detalles internos al cliente.
7. Reintentos automáticos HTTP únicamente para `GET` y solo ante `0/503/504`.
8. Nunca reintentar automáticamente `POST`, `PUT`, `DELETE` o `PATCH`.
9. La caché futura deberá tener invalidación explícita ante publicación de versiones y cambios de catálogos/reglas.
10. Antes de crear índices Oracle, ejecutar y documentar profiling/`EXPLAIN PLAN` en el ambiente autorizado.
11. La bitácora es histórica e inmutable: las correcciones futuras se agregan mediante una nueva entrada, no reescribiendo registros previos.
12. `ESTADO_COLABORACION.md` es el documento vivo y puede consolidarse conforme cambie el estado real.

---

## 7. Pendientes independientes que no bloquean el cierre BE-01 + FE-02

- Validación visual manual del login tras el endurecimiento realizado por Codex.
- Si la cuenta Oracle de Desarrollo continúa bloqueada, el desbloqueo corresponde exclusivamente al DBA/administrador autorizado.
- Validación UAT local residual indicada en PR #20.

Ninguno de estos pendientes autoriza cambios en Producción ni en `main`.

---

## 8. Punto exacto de continuación

**BE-01 + FE-02 quedan cerrados técnicamente.**

El siguiente paso de la secuencia aprobada es:

### BE-03 — Health Checks

- `/healthz`: liveness del proceso API, sin dependencia de Oracle.
- `/readyz`: readiness de persistencia/servicios críticos, sin exponer credenciales, detalles Oracle o información interna.
- La implementación debe incluir pruebas unitarias/integración apropiadas y pasar nuevamente los Quality Gates.

No iniciar DB-03, DDL/DML Oracle ni otras fases fuera de secuencia sin la autorización correspondiente.
