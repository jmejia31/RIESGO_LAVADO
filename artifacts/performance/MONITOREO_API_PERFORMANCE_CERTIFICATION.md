# Monitoreo de Listas - certificacion HTTP production-like

**Resultado contractual histórico (corrida 2026-09-10): FAIL.** No se certificó `PRODUCTION_LIKE_PERFORMANCE_CERTIFICATION=PASS` para esa corrida. **Estado vigente desde 2026-09-23:** `MONITOREO_PERFORMANCE_POINT=CLOSED_BY_OWNER`, `COLD_ORIGIN_LATENCY=ACCEPTED_KNOWN_RESTRICTION` y `MONITOREO_WARM_PAGE_CACHE=PASS`; el FAIL histórico se conserva como evidencia y ya no representa un pendiente abierto.

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

**Estado histórico de la auditoría:** `DATABASE_PHYSICAL_OPTIMIZATION_REQUIRED=TRUE`. La decisión DBA posterior no autorizó cambios físicos no replicables en `DNP_IHSS`/`MMATAMOROS`; el addendum de 2026-09-23 registra la mitigación backend adoptada en su lugar.

## Cambios preservados

La implementación evaluada en la certificación histórica mantenía paginación DB-side, caché de metadata, página sin conteo preventivo, lookups set-based, cancelación ODP.NET y rutas dedicadas de exportación. Esa corrida no alcanzó los umbrales full-stack. El addendum posterior amplía el caché a páginas de Monitoreo y registra su efecto sin reclasificar la latencia fría como PASS.


## Addendum 2026-09-23 — mitigación backend sin tocar schemas externos

El DBA indicó que no deben crearse índices nuevos en `DNP_IHSS` ni `MMATAMOROS` que no puedan reproducirse en producción. En consecuencia:

- no se ejecutó DDL ni `DBMS_STATS` sobre esos schemas;
- se eliminó la dependencia explícita del hint `IX_RCOINC_MON_TIPO_PATRONO` del SQL productivo;
- se conservó la optimización SQL de Naturales;
- se añadió caché corta por tipo/página/tamaño/filtros en el backend, usando el alcance de Monitoreo ya invalidado por mutaciones locales.

Evidencia HTTP final sobre el backend F7, todos `HTTP 200`:

| Tipo | Primera corrida ms | Segunda corrida ms | Repetición |
|---|---:|---:|---|
| Jurídicas | 12,620 | 24 | PASS |
| Naturales | 19,128 | 21 | PASS |
| Empleados | 22,394 | 20 | PASS |

La instrumentación aisló en Jurídicas un caso real con `connectionOpenMs=0`, `pageExecuteMs=6559`, `firstRowMs=0`, `rowsReadMs=2`, `mappingMs=2`, `metadataLookupMs=0`, `totalRepositoryMs=6563`: el costo frío está dominado por ejecución Oracle, no por serialización ni metadata.

Estado actualizado:

```text
MONITOREO_WARM_PAGE_CACHE=PASS
EXTERNAL_SCHEMA_DDL=0
DNP_IHSS_MODIFIED=FALSE
MMATAMOROS_MODIFIED=FALSE
PRODUCTION_LIKE_PERFORMANCE_CERTIFICATION=FAIL_COLD_ORIGIN_LATENCY
```

El resultado production-like global continúa en `FAIL` porque la primera carga fría supera el contrato de 10 s. La mitigación sí elimina el costo repetido dentro del TTL del caché y queda cubierta por invalidación explícita ante cambios realizados por la aplicación.


## Cierre funcional del punto de rendimiento — decisión de propietario 2026-09-23

Javier Mejía declara cerrado el punto de rendimiento de Monitoreo con la evidencia vigente. La aceptación aplica al alcance de desarrollo y release actual y no reescribe la evidencia histórica de latencia fría.

Evidencia final observada en navegador, todos HTTP 200:

```text
JURIDICAS  primera=2800 ms   repetida=15 ms
NATURALES primera=1903 ms   repetida=14 ms
EMPLEADOS primera=694 ms    repetida=13 ms
```

Evidencia previa equivalente también mostró repetición cacheada en el rango 20–24 ms. La variabilidad de la primera carga se atribuye al origen/servidor Oracle compartido; el backend evita repetir ese costo dentro del TTL mediante caché invalidable por tipo, página, tamaño y filtros.

Estado acordado:

```text
MONITOREO_PERFORMANCE_POINT=CLOSED_BY_OWNER
WARM_PAGE_CACHE=PASS
COLD_ORIGIN_LATENCY=ACCEPTED_KNOWN_RESTRICTION
EXTERNAL_SCHEMA_DDL=0
DNP_IHSS_MODIFIED=FALSE
MMATAMOROS_MODIFIED=FALSE
```

La certificación histórica `PRODUCTION_LIKE_PERFORMANCE_CERTIFICATION=FAIL` se conserva como registro de la corrida contractual original; ya no constituye un bloqueo abierto del proyecto porque el propietario acepta explícitamente la restricción fría y cierra este punto.
