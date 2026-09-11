# Auditoría Oracle pre-cambio — Monitoreo de Listas

Fecha: `2026-09-11`
Estado: `ORACLE_PRECHANGE_AUDIT_COMPLETED`
Modo: consultas de diccionario y lectura de planes existentes; no se ejecutó DDL, DML, `EXPLAIN PLAN` ni `DBMS_STATS`.

## Conexión y permisos

La auditoría reproducible [`audit-oracle-prechange.ps1`](../../tools/performance/audit-oracle-prechange.ps1)
confirmó conexión a Oracle real con los siguientes valores no secretos:

| Dato | Valor |
|---|---|
| Current schema | `RIESGO_LAVADO` |
| Session user | `RIESGO_LAVADO` |
| Database | `hpprod1` |
| `CREATE INDEX` / `CREATE ANY INDEX` visible en `SESSION_PRIVS` | No |
| `ANALYZE ANY` visible en `SESSION_PRIVS` | No |
| Grant visible de `DBMS_STATS` | No |
| Lectura de `V$SQL` / `ALL_SEGMENTS` | No (`ORA-00942`) |

Por seguridad, el script no imprime ni persiste la cadena de conexión, host,
contraseña, datos personales ni el texto de credenciales. El acceso actual no
permite estimar segmentos ni usar `DBMS_XPLAN.DISPLAY_CURSOR`; esas dos
actividades requieren una sesión DBA con privilegios de diccionario.

## Índices y constraints realmente observados

`DNP_IHSS.REPORTE_COINCIDENCIAS` tiene un solo índice válido:

| Índice | Tipo | Columnas |
|---|---|---|
| `PERPORTE_COINCIDENCIAS_PK` | PK/unique normal | `REPORTE_COINCIDENCIA_ID` |

La única constraint PK/UK del objeto es la misma PK. No existe un índice
equivalente, prefijo ni parcial que inicie con las columnas de las dos P0:

| Propuesta | Exact match | Prefix/covered match | Conclusión |
|---|---:|---|---|
| Natural: `(TIPO_CALIFICACION_ID, DNI, LISTA_CONCIDENCIA, FECHA_CALIFICO)` | No | `NONE` | Requerida para validación DBA |
| Jurídica: `(TIPO_CALIFICACION_ID, NUMERO_PATRONO, FECHA_CALIFICO)` | No | `NONE` | Requerida para validación DBA |

Los índices existentes de los objetos de apoyo no sustituyen esas P0:

- `DNP_IHSS.SOCIOS`: sólo `SOCIOS_PK(SOCIO_ID)`; no hay índice por
  `NUMERO_IDENTIFICACION`.
- `DNP_IHSS.REPRESENTANTES`: sólo `REPRESENTANTES_PK(REPRESENTANTE_ID)`; no
  hay índice por `NUMERO_IDENTIFICACION`.
- `DNP_IHSS.DATOS_EMPRESA`: PK y `IND_NUMERO_PATRONO(NUMERO_PATRONAL)`;
  este último sí cubre el acceso de la vista por número patronal.
- `MMATAMOROS.PATRONOS`: PK por `NUMEPATRO` e índice por `RTN`; no se
  recomienda duplicarlos.
- `RIESGO_LAVADO.RL_LISTA_POSITIVOS`: índices individuales de estado, nombre,
  origen y usuarios, PK, y UK `(LSP_TIPO_DOCUMENTO_ID, LSP_NO_DOCUMENTO)`.
  La P2 `(LSP_TIPO_POSITIVO_ID, LSP_ESTADO_REGISTRO, LSP_NO_DOCUMENTO)` no
  está cubierta como prefijo, pero con 6 filas no se recomienda ahora.

Las definiciones de vista visibles confirman que
`V_SOCIOS_REPRESENTANTES` expande `V_SOCIOS` y `V_REPRESENTANTES`; éstos a su
vez parten de las tablas `SOCIOS` y `REPRESENTANTES`. `V_DATOS_EMPRESA` usa
`DATOS_EMPRESA` y el sinónimo público `PATRONOS`, resuelto a
`MMATAMOROS.PATRONOS`.

## Estadísticas y selectividad

| Objeto | Filas | Último análisis | Evaluación |
|---|---:|---|---|
| `DNP_IHSS.REPORTE_COINCIDENCIAS` | 31,767 | 2026-09-01 | Actual |
| `DNP_IHSS.SOCIOS` | 34,713 | 2026-05-21 | Revisión DBA requerida |
| `DNP_IHSS.REPRESENTANTES` | 19,275 | 2026-05-21 | Revisión DBA requerida |
| `DNP_IHSS.DATOS_EMPRESA` | 26,429 | 2026-05-21 | Revisión DBA requerida |
| `MMATAMOROS.PATRONOS` | 94,588 | 2026-07-09 | Revisar columnas: su estadística de clave fue 2025-05-13 |
| `MMATAMOROS.PLANILLAS` | 180,592,953 | 2024-12-12 | Stale |
| `RIESGO_LAVADO.RL_LISTA_POSITIVOS` | 6 | 2026-07-13 | Actual |

La cardinalidad observada explica por qué `TIPO_CALIFICACION_ID` no debe ser
tratado como único criterio de selectividad: tiene 3 valores, frente a 597
para `DNI` y 3,818 para `NUMERO_PATRONO`. La selección final del orden de
columnas P0 debe hacerse por el DBA con el plan posterior y estadísticas
renovadas; no se cambia el DDL propuesto sin esa medición.

## Planes leídos sin mutación

La cuenta no puede consultar cursores compartidos, de modo que no fue posible
usar `DISPLAY_CURSOR(..., 'ALLSTATS LAST')`. Como alternativa read-only, se
leyeron los planes ya existentes de `PLAN_TABLE` con fecha `2026-09-11`; no se
insertaron ni reemplazaron filas de plan durante esta intervención.

| Consulta | Plan hash | Operaciones relevantes | Cuello primario |
|---|---:|---|---|
| Naturales | `3681079459` | Full scan `REPORTE_COINCIDENCIAS`, `SOCIOS`, `REPRESENTANTES`, `DATOS_EMPRESA`, `PATRONOS`; hash joins, sort unique/group by y stopkey | Expansión completa de `V_SOCIOS_REPRESENTANTES`, especialmente `PATRONOS` (coste 2,213) y ramas sin índice por identificación; coste total ~5,957 |
| Jurídicas | `3681022051` | Full scan `REPORTE_COINCIDENCIAS` (2,745 filas estimadas) y `PATRONOS` (94,588); hash join y stopkey | `PATRONOS` (coste 2,213) junto con el full scan de coincidencias; `DATOS_EMPRESA` sí usa `IND_NUMERO_PATRONO` |
| Empleados | `1437433635` | Remote, hash group/join y full scan `REPORTE_COINCIDENCIAS` | No es candidato de optimización física independiente: mantiene buen comportamiento relativo y sólo comparte la brecha de coincidencias |

Los planes demuestran que la lenta respuesta no se resuelve con paginación
adicional ni con cambios de UI. La fuente ya está paginada en base de datos;
faltan caminos físicos para reducir la expansión antes de los joins.

## Decisión pre-cambio

- `P0_INDEXES_RECOMMENDED_AFTER_REAL_AUDIT=2`: ambos índices de
  `REPORTE_COINCIDENCIAS` siguen faltando y son los candidatos mínimos para
  probar con aprobación DBA.
- `P1_INDEXES_RECOMMENDED_AFTER_REAL_AUDIT=2`: índices por
  `NUMERO_IDENTIFICACION` en `SOCIOS` y `REPRESENTANTES`; necesarios para
  validar que la vista pueda iniciar desde el conjunto reducido de DNI.
- `P2_INDEXES_RECOMMENDED_AFTER_REAL_AUDIT=0`: no justificar índice adicional
  en `RL_LISTA_POSITIVOS` mientras tiene 6 filas.
- `DBMS_STATS_REQUIRED=TRUE`: es una recomendación DBA separada para los
  objetos stale; no se ejecutó ninguna recolección.
- `NEXT_ACTION=DBA_REQUIRED`: el usuario conectado no tiene privilegio visible
  para crear índices ni grant visible para `DBMS_STATS`.

La propuesta exacta, íntegramente comentada con rollback, permanece en
[`ORACLE_MONITOREO_PHYSICAL_OPTIMIZATION_PLAN.sql`](ORACLE_MONITOREO_PHYSICAL_OPTIMIZATION_PLAN.sql).
La aprobación debe aplicar sólo las prioridades aceptadas, refrescar
estadísticas bajo ventana DBA y repetir los planes y el benchmark HTTP de 10 s.

```
ORACLE_DDL_EXECUTED=0
ORACLE_DML_EXECUTED=0
DBMS_STATS_EXECUTED=0
PRODUCTION_LIKE_PERFORMANCE_CERTIFICATION=FAIL_PENDING_PHYSICAL_OPTIMIZATION
FASE_5_3_REANUDADA=FALSE
```
