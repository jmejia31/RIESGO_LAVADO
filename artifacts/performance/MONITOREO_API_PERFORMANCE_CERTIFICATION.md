# Monitoreo de Listas - certificacion HTTP production-like

**Resultado: FAIL.** No se certifica `PRODUCTION_LIKE_PERFORMANCE_CERTIFICATION=PASS`.

La corrida final reproducible se ejecuto con `RL.API` compilada en `Release`,
`ASPNETCORE_ENVIRONMENT=Production`, Oracle real, JWT del proyecto y HTTP
completo hasta consumir `response.text()`. El timeout contractual fue 10,000 ms.
El artefacto completo es [`monitoreo-api-performance-2026-09-10T22-30-31-631Z.json`](monitoreo-api-performance-2026-09-10T22-30-31-631Z.json).

| Tipo | Cold ms | Warm median ms | Warm P90 ms | Warm P95 ms | Max ms | Total | Failed/timeouts | Resultado |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| Juridicas | timeout | 4943.57 | 8582.68 | 8582.68 | 8582.68 | 4 | 10 | FAIL |
| Naturales | timeout | 4131.33 | 8170.45 | 8170.45 | 8170.45 | 242 | 28 | FAIL |
| Empleados | timeout | 188.15 | 471.58 | 732.15 | 732.15 | 7 | 2 | FAIL |

Resultado global: **FAIL**; `FAILED_REQUESTS=40`; `TIMEOUTS=39` y un fallo
de concurrencia reportado como error por timeout. Los p95 se muestran sólo
como estadistica de muestras exitosas; la presencia de fallos impide usarlos
como p95 global certificable.

## Evidencia del cuello

- La latencia permanece dominada por la ejecucion/fetch Oracle de las vistas DNP y por la espera de conexiones bajo concurrencia.
- En la evidencia aislada de Naturales con timeout diagnostico de 30 s: cold `28104.81 ms`, warm median `4678.82 ms`, pagina 2 median `6817.64 ms`, page size 25 median `1429.08 ms` y filtro median `23581.55 ms`.
- La evidencia de fases registro en cold `connectionOpenMs=3454`, `pageExecuteMs=11003` y `metadataLookupMs=13310`; en warm, page `4663 ms` y pagina 2 `6806 ms`.
- Los `ORA-01013` observados tienen `requestAborted=true`; son consecuencia de la cancelacion HTTP al vencer 10 s, no la causa primaria declarada.

## Auditoria de indices y escalamiento

La consulta read-only de `ALL_IND_COLUMNS` no encontro soporte suficiente en
las claves DNP usadas por joins/filtros, mas alla de las claves primarias
observadas. No se ejecuto DDL, DML, indice, tabla ni vista materializada.

Se requiere una optimizacion fisica DBA separada, con planes y medicion antes/
despues, para `DNP_IHSS.REPORTE_COINCIDENCIAS`, las fuentes de socios y
representantes y las claves de `RL_LISTA_POSITIVOS`.

**`DATABASE_PHYSICAL_OPTIMIZATION_REQUIRED=TRUE`**

## Cambios preservados

La implementacion mantiene paginacion DB-side, metadata cacheada unicamente,
pagina sin conteo preventivo, lookups set-based, cancelacion ODP.NET y rutas
dedicadas de exportacion. La evidencia final demuestra que esas mejoras no
alcanzan los umbrales full-stack exigidos contra el Oracle disponible.
