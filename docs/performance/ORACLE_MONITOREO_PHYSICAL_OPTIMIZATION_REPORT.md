# Oracle Monitoreo de Listas - diagnóstico físico para aprobación DBA

Estado: `DBA_PHYSICAL_OPTIMIZATION_READY_FOR_APPROVAL`.

Esta intervención no ejecutó DDL, DML, `DBMS_STATS`, índices, tablas, vistas
materializadas ni cambios de datos. El SQL propuesto está completamente
comentado en [`ORACLE_MONITOREO_PHYSICAL_OPTIMIZATION_PLAN.sql`](ORACLE_MONITOREO_PHYSICAL_OPTIMIZATION_PLAN.sql)
y contiene rollback por cada índice.

## Evidencia ejecutada

Fecha del diagnóstico: `2026-09-11`.

- Fuente: Oracle real conectado mediante SQL*Plus 11.2, sesión read-only.
- Planes: `EXPLAIN PLAN` de las formas actuales de página y salida mediante `DBMS_XPLAN.DISPLAY`; no se alteró la base.
- Vistas visibles: `DNP_IHSS.V_SOCIOS_REPRESENTANTES`, `V_DATOS_EMPRESA` y `V_EMPLEADOS_IHSS_PLANILLAS`.
- El plan muestra expansión de las vistas a tablas base; `V_EMPLEADOS_IHSS_PLANILLAS` además consulta `MMATAMOROS.PATRONOS@HPPROD1_PRO` y `MMATAMOROS.PLANILLAS@HPPROD1_PRO`.

## Inventario y estadísticas relevantes

| Objeto | Filas | Blocks | Last analyzed | Índices relevantes observados |
|---|---:|---:|---|---|
| `DNP_IHSS.REPORTE_COINCIDENCIAS` | 31,767 | 768 | 2026-09-01 | Sólo `PERPORTE_COINCIDENCIAS_PK(REPORTE_COINCIDENCIA_ID)` |
| `DNP_IHSS.SOCIOS` | 34,713 | 706 | 2026-05-21 | Sólo `SOCIOS_PK(SOCIO_ID)` |
| `DNP_IHSS.REPRESENTANTES` | 19,275 | 611 | 2026-05-21 | Sólo `REPRESENTANTES_PK(REPRESENTANTE_ID)` |
| `DNP_IHSS.DATOS_EMPRESA` | 26,429 | 338 | 2026-05-21 | PK y `IND_NUMERO_PATRONO(NUMERO_PATRONAL)` |
| `MMATAMOROS.PATRONOS` | 94,588 | 8,149 | 2026-07-09 | PK `NUMEPATRO`, `RTN`, índice funcional `SUBSTR(NUMEPATRO,1,11)` |
| `MMATAMOROS.PLANILLAS` | 180,592,953 | 3,745,679 | 2024-12-12 | `PERIODO`, `IDENTIDAD`, `PERIODO/NUMEPATRONO/IDENTIDAD` |
| `RIESGO_LAVADO.RL_LISTA_POSITIVOS` | 6 | 5 | 2026-07-13 | estado, nombre, origen, PK y UK `(LSP_TIPO_DOCUMENTO_ID,LSP_NO_DOCUMENTO)` |

Estadísticas de selectividad observadas: `REPORTE_COINCIDENCIAS.DNI=597`,
`NUMERO_PATRONO=3,818`, `TIPO_CALIFICACION_ID=3`; `SOCIOS.NUMERO_IDENTIFICACION=18,576`;
`REPRESENTANTES.NUMERO_IDENTIFICACION=12,445`; `PATRONOS.NUMEPATRO=94,383`.
Las estadísticas de `MMATAMOROS.PLANILLAS` son antiguas para un objeto de
180 millones de filas. `DBMS_STATS_REQUIRED=TRUE` como actividad DBA separada;
no se ejecutó `DBMS_STATS`.

## Planes actuales

### Naturales

`Plan hash=893357362`. La forma capturada contiene `REPORTE_AGG`, reducción de
IDs, expansión de `V_SOCIOS_REPRESENTANTES` y `ROWNUM <= 10`.

- `TABLE ACCESS FULL REPORTE_COINCIDENCIAS`: estimación 234 filas después de filtro, costo 210.
- `SORT ORDER BY STOPKEY` y `HASH JOIN`: costo aproximado 5,956-5,957, estimación 72 s.
- La expansión de `V_SOCIOS_REPRESENTANTES` incluye `SORT UNIQUE`, `HASH GROUP BY`, `TABLE ACCESS FULL SOCIOS` (34,713), `TABLE ACCESS FULL REPRESENTANTES` (19,275) y `TABLE ACCESS FULL PATRONOS` (94,588; costo 2,213, estimación 27 s).

Cuello: el conjunto de DNI se reduce lógicamente, pero la vista no obtiene un
acceso indexado suficientemente temprano y Oracle materializa/sanea la
expansión completa antes del join. `P0_INDEX_RECOMMENDATIONS=2` y `P1=2`.

### Jurídicas

`Plan hash=3681022051`.

- `TABLE ACCESS FULL REPORTE_COINCIDENCIAS`: estimación 2,745 filas, costo 210.
- `TABLE ACCESS FULL PATRONOS`: 94,588 filas, costo 2,213, estimación 27 s.
- `HASH JOIN` y `SORT ORDER BY STOPKEY`: costo aproximado 2,425-2,426, estimación 30 s.
- `DATOS_EMPRESA` sí tiene `IND_NUMERO_PATRONO`; no se propone duplicar ese índice.

Cuello: el filtro `TIPO_CALIFICACION_ID=1` y la clave
`NUMERO_PATRONO` no tienen un índice compuesto en `REPORTE_COINCIDENCIAS`.

### Empleados

`Plan hash=1437433635`.

- `REMOTE` sobre el origen de empleados y `TABLE ACCESS FULL REPORTE_COINCIDENCIAS`: estimación 2,574 filas, costo 210.
- `HASH JOIN`, `HASH GROUP BY` y `SORT ORDER BY STOPKEY`: costo aproximado 222-224, estimación 3 s.
- `MMATAMOROS.PLANILLAS` ya posee índices por `PERIODO` e `IDENTIDAD`; no se incluye un nuevo índice P0 para Empleados.

Empleados permanece como control de no regresión; cualquier cambio P0/P1
debe medirse para demostrar que no lo perjudica.

## Recomendaciones priorizadas

| Prioridad | Propuesta | Query/predicados | Plan before | Riesgo | Rollback |
|---|---|---|---|---|---|
| P0 | `DNP_IHSS.IX_RCOINC_MON_NAT_DNI` | `TIPO_CALIFICACION_ID=1`, `FECHA_CALIFICO IS NOT NULL`, join/group `DNI`, `LISTA_CONCIDENCIA` | Full scan de `REPORTE_COINCIDENCIAS` y sort/hash | índice adicional y posible cambio de join order | `DROP INDEX` comentado |
| P0 | `DNP_IHSS.IX_RCOINC_MON_PATRONO` | `TIPO_CALIFICACION_ID=1`, join `NUMERO_PATRONO` | Full scan de `REPORTE_COINCIDENCIAS` y hash join a `PATRONOS` | índice adicional; validar clustering | `DROP INDEX` comentado |
| P1 | `DNP_IHSS.IX_SOCIOS_MON_IDENTIFICACION` | semi-join `NUMERO_IDENTIFICACION = DNI` | Full scan de `SOCIOS` dentro de la vista | puede no usarse si el optimizador prefiere scan | `DROP INDEX` comentado |
| P1 | `DNP_IHSS.IX_REPRESENTANTES_MON_IDENTIFICACION` | semi-join `NUMERO_IDENTIFICACION = DNI` | Full scan de `REPRESENTANTES` dentro de la vista | puede no usarse si el optimizador prefiere scan | `DROP INDEX` comentado |
| P2 | `RIESGO_LAVADO.IX_RL_LSP_MON_TIPO_EST_DOC` | tipo/estado/documento de positivos | tabla actual de 6 filas; no es cuello actual | sólo justificado con crecimiento | `DROP INDEX` comentado |

No se propone índice para `MMATAMOROS.PATRONOS.NUMEPATRO`, `RTN` ni
`DNP_IHSS.DATOS_EMPRESA.NUMERO_PATRONAL`: ya existe soporte PK/UK/funcional
observado. Tampoco se propone indexar columnas de baja selectividad como
primer componente sin una prueba posterior.

## Pool y estadísticas

La cadena local no declara explícitamente parámetros de pool/statement cache;
aplican los defaults del proveedor y no se modificaron. La instrumentación
HTTP previa observó `connectionOpenMs=3454` en cold, por lo que conexión/pool
es un co-cuello, pero no hay evidencia suficiente para imponer `Min Pool Size`
o `Statement Cache Size` en producción. Medir después de la revisión física
sin cambiar timeouts.

## Aprobación y siguiente medición

`ORACLE_DDL_PLAN_CREATED=PASS`; `ORACLE_DDL_EXECUTED=0`;
`ORACLE_DML_EXECUTED=0`; `ROLLBACK_PLAN=PASS`.

Después de aprobación DBA, aplicar sólo el conjunto aprobado, capturar
`DBMS_XPLAN.DISPLAY_CURSOR(..., 'ALLSTATS LAST')`, refrescar estadísticas si
el DBA lo autoriza y repetir el benchmark HTTP contractual con timeout de
10 segundos. Hasta entonces:

`PRODUCTION_LIKE_PERFORMANCE_CERTIFICATION=FAIL_PENDING_DBA_CHANGE`
