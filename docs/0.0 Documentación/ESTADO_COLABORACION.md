# Estado de colaboración y punto de continuidad

> Actualización 2026-08-10: **BE-04 — Rate Limiting** fue implementado y certificado en `desarrollo`. El API aplica límites centralizados únicamente sobre operaciones sensibles: login, recuperación de contraseña, refresh token, exportaciones Excel/PDF de Matrices y carga física de evidencias. Las operaciones anónimas se particionan por `RemoteIpAddress` y las autenticadas por identificador de usuario con fallback a IP; no se confía en `X-Forwarded-For`/`X-Real-IP` sin una configuración previa de proxies confiables. El rechazo usa HTTP `429`, `Retry-After` cuando está disponible y un `ProblemDetails` seguro sin secretos ni detalle técnico. El HEAD técnico `f7225a243642b510727a663aaa0576120f5b0280` fue certificado por GitHub Actions Quality Gates Run `31406175762` (#582) en **SUCCESS**: Backend 295/295, Frontend 162/162, E2E 13/13, build Release 0 errores/0 advertencias y `npm audit` con 0 vulnerabilidades. Oracle no fue conectado ni ejecutado; `main` permanece intacta y el PR #20 debe continuar abierto y en borrador.

Documento vivo. Debe actualizarse al finalizar cada intervención. Los antecedentes históricos permanecen en [`BITACORA_COLABORACION.md`](../../BITACORA_COLABORACION.md).

---

## 1. Línea base vigente

- **Repositorio:** `jmejia31/RIESGO_LAVADO`
- **Rama obligatoria de trabajo:** `desarrollo`
- **HEAD técnico BE-04 certificado:** `f7225a243642b510727a663aaa0576120f5b0280`
- **Rama estable:** `main`
- **HEAD de `main`:** `727082c6fcf90f95ce6db5eadf5c4b152397d080`
- **Política `main`:** no modificar ni integrar sin autorización expresa de Javier Mejía
- **Ramas remotas permitidas por protocolo:** únicamente `main` y `desarrollo`
- **PR de revisión:** #20 — abierto, en borrador y sin autorización de fusión
- **Arquitectura:** monolito modular con Angular, ASP.NET Core y Oracle 11g
- **Modelo vigente de Matrices:** 17 tablas `RL_MR_*` y 17 secuencias

---

## 2. Última intervención — BE-04

- **Intervención:** BE-04 — Rate Limiting
- **Fecha:** 2026-08-10 (UTC-6)
- **Autor:** ChatGPT
- **Rama:** `desarrollo`
- **Base de inicio:** `97563cad0344121acb23ce179a42c2557063fa3e`
- **HEAD técnico certificado:** `f7225a243642b510727a663aaa0576120f5b0280`
- **Quality Gate técnico:** Run `31406175762` (#582) — **SUCCESS**
- **Estado:** **BE-04 completado y certificado técnicamente**.

### Cambios certificados

1. **Rate limiter centralizado**
   - Implementado con `System.Threading.RateLimiting` / middleware nativo ASP.NET Core.
   - `GlobalLimiter` identifica por método + ruta únicamente las operaciones sensibles aprobadas.
   - El resto del API usa una partición sin límite BE-04 y no queda penalizado por esta fase.
   - `QueueLimit = 0`: al superar el cupo se rechaza inmediatamente; no se acumulan solicitudes sensibles en memoria.

2. **Operaciones anónimas protegidas por IP de conexión**
   - `POST /api/auth/login`: 5 solicitudes / 60 segundos.
   - `POST /api/auth/recuperar-password`: 3 solicitudes / 900 segundos.
   - `POST /api/auth/refresh`: 20 solicitudes / 60 segundos.
   - La clave de partición usa `HttpContext.Connection.RemoteIpAddress`.
   - BE-04 no toma directamente `X-Forwarded-For` ni `X-Real-IP`, evitando que cabeceras no verificadas permitan eludir el límite.
   - En despliegues futuros detrás de proxy inverso, la IP real solo debe adoptarse después de configurar `ForwardedHeaders` con `KnownProxies`/`KnownNetworks` controlados.

3. **Operaciones autenticadas protegidas por usuario**
   - `GET /api/matrices-riesgos/reportes/consolidado.xlsx`: 6 solicitudes / 60 segundos.
   - `GET /api/matrices-riesgos/reportes/consolidado.pdf`: comparte la misma política de exportación y partición por usuario.
   - `POST /api/matrices-riesgos/evidencias/cargar`: 10 solicitudes / 60 segundos.
   - Se prioriza `ClaimTypes.NameIdentifier`; si no existe identidad válida, se usa IP como fallback seguro.
   - Usuarios autenticados distintos mantienen ventanas independientes.

4. **Respuesta HTTP 429 segura**
   - Código HTTP `429 Too Many Requests`.
   - `application/problem+json` como contrato previsto.
   - Título público `Demasiadas solicitudes`.
   - Mensaje genérico sin exponer configuración interna, credenciales, SQL, Oracle ni información sensible.
   - Incluye `traceId`.
   - Incluye `Retry-After` cuando el limiter proporciona metadato de reintento.

5. **Configuración controlada**
   - Se añadió sección `RateLimiting` a `appsettings.example.json`.
   - Los límites y ventanas se normalizan con mínimos/máximos defensivos para impedir configuraciones cero, negativas o desproporcionadas.
   - No se añadieron secretos ni valores dependientes de Oracle.

6. **Pruebas BE-04**
   - Rutas sensibles exactas y comparación case-insensitive.
   - Métodos/rutas fuera de alcance permanecen sin límite BE-04.
   - `/healthz` y `/readyz` permanecen fuera del rate limit de esta fase.
   - Cabeceras `X-Forwarded-For`/`X-Real-IP` no sustituyen la IP de conexión.
   - Identidad autenticada prevalece para reportes/evidencias.
   - Se valida límite exacto, rechazo sin cola y metadato `RetryAfter`.
   - Se valida normalización de configuración inválida.
   - Se valida aislamiento entre usuarios autenticados.

### Incidencia intermedia resuelta

La primera ejecución CI del bloque de pruebas, Run `31405971032` (#580), falló únicamente porque el archivo nuevo de pruebas omitió `using Xunit;`. El API productivo ya compilaba; se corrigió la importación en `f7225a243642b510727a663aaa0576120f5b0280` y se repitió la certificación completa. El Run #582 es la evidencia vigente y aprobada.

---

## 3. Evidencia de verificación BE-04

### GitHub Actions — Quality Gates

- **Run:** `31406175762`
- **Número:** #582
- **Conclusión:** **SUCCESS**
- **Build Release:** 0 errores, 0 advertencias
- **Backend:** 295/295 pruebas aprobadas; 0 fallidas; 0 omitidas
- **Frontend:** 162/162 pruebas aprobadas en 25 archivos
- **E2E Playwright:** 13/13 aprobadas
- **NPM audit:** 0 vulnerabilidades
- **Cobertura Backend:** líneas 21.40%; ramas 24.11%
- **Cobertura Frontend:** sentencias 39.53%; ramas 35.24%; funciones 35.99%; líneas 39.15%
- **Validadores de BD, preparación Oracle, inventario 17/17, autorización y contrato UAT:** aprobados

### Oracle

Durante desarrollo y certificación CI de BE-04:

- **NO** se abrió conexión a Oracle real;
- **NO** se ejecutó DDL;
- **NO** se ejecutó DML;
- **NO** se ejecutaron scripts de transición;
- **NO** se modificó el esquema;
- **NO** se modificaron respaldos `B10_*`.

Los validadores Oracle ejecutados por CI son controles estáticos/de preparación y no constituyen una ejecución física de Oracle.

---

## 4. Estado consolidado del Plan de Mejoras Integrales

| Orden | Código | Estado |
|---:|---|---|
| 1 | GOV-01 — Sincronización Bitácora / UAT | **Completado** |
| 2 | BE-01 + FE-02 — ProblemDetails + Interceptor HTTP | **Completado y certificado** |
| 3 | BE-03 — `/healthz` + `/readyz` | **Completado y certificado** |
| 4 | BE-04 — Rate Limiting | **Completado y certificado** |
| 5 | BE-02 — Caché con invalidación explícita | **Siguiente** |
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

La validación local residual de UAT permanece como actividad funcional/operativa independiente y no fue sustituida por BE-04.

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
9. Las políticas BE-04 deben conservar particiones separadas y no confiar en cabeceras de forwarding hasta configurar proxies confiables.
10. Reintentos automáticos HTTP únicamente para `GET` y solo ante `0/503/504`.
11. Nunca reintentar automáticamente `POST`, `PUT`, `DELETE` o `PATCH`.
12. La caché BE-02 deberá tener invalidación explícita ante publicación de versiones y cambios de catálogos/reglas.
13. Antes de crear índices Oracle, ejecutar y documentar profiling/`EXPLAIN PLAN` en el ambiente autorizado.
14. La bitácora es histórica e inmutable: las correcciones futuras se agregan mediante una nueva entrada, no reescribiendo registros previos.
15. `ESTADO_COLABORACION.md` es el documento vivo y puede consolidarse conforme cambie el estado real.

---

## 7. Pendientes independientes

- Validación UAT local residual indicada en PR #20.
- Si la cuenta Oracle de Desarrollo continúa bloqueada, el desbloqueo corresponde exclusivamente al DBA/administrador autorizado.
- Configurar proxies confiables antes de habilitar `ForwardedHeaders` como fuente de IP cliente en despliegues detrás de proxy inverso.

---

## 8. Punto exacto de continuación

**GOV-01, BE-01, FE-02, BE-03 y BE-04 quedan cerrados técnicamente.**

El siguiente paso de la secuencia aprobada es:

### BE-02 — Caché con invalidación explícita

La implementación deberá priorizar datos de lectura apropiados, definir TTL y claves de caché, y garantizar invalidación explícita ante publicación de versiones, cambios de catálogos/reglas y cualquier mutación que pueda volver obsoleta la información cacheada.

No iniciar DB-03, DDL/DML Oracle ni otras fases fuera de secuencia hasta cerrar y certificar BE-02.
