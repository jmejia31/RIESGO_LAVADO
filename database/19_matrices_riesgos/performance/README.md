# DB-03 — Profiling Oracle / EXPLAIN PLAN

Paquete de diagnóstico controlado para el módulo Matrices de Riesgos y consultas relacionadas de alto crecimiento.

## Objetivo

Medir antes de optimizar. Este paquete permite levantar evidencia de estadísticas, cardinalidad, índices existentes y planes de ejecución de las consultas críticas reales del backend sin crear índices, modificar tablas de negocio ni ejecutar scripts de transición.

## Archivos

- `00_db03_ejecutar_profiling_autorizado.sql`: punto de entrada manual, exige esquema `RIESGO_LAVADO` y token `EJECUTAR_DB03`.
- `01_db03_inventario_estadisticas_solo_lectura.sql`: inventario exclusivamente de lectura sobre estadísticas, cardinalidades e índices.
- `02_db03_explain_plan_consultas_criticas.sql`: genera `EXPLAIN PLAN` para consultas críticas y muestra `DBMS_XPLAN`; la única escritura diagnóstica es la realizada por Oracle en `PLAN_TABLE`, encapsulada en transacción y revertida al finalizar.

## Ejecución autorizada

Desde SQL*Plus, con credenciales suministradas por canal seguro y nunca versionadas:

```sql
@database/19_matrices_riesgos/performance/00_db03_ejecutar_profiling_autorizado.sql EJECUTAR_DB03
```

No colocar usuario, contraseña, host ni cadena de conexión en scripts, capturas o bitácoras.

## Salvaguardas

1. El punto de entrada aborta si `CURRENT_SCHEMA` no es `RIESGO_LAVADO`.
2. El token manual debe ser exactamente `EJECUTAR_DB03`.
3. No se ejecuta `06_reconstruir_modelo_17_tablas.sql` ni ningún script de transición.
4. No contiene `CREATE INDEX`, `ALTER TABLE`, `DROP`, `TRUNCATE`, DML sobre tablas `RL_*` ni `COMMIT`.
5. `EXPLAIN PLAN` usa la `PLAN_TABLE` existente. El paquete no la crea automáticamente.
6. Los planes son diagnósticos: ningún índice se aprueba únicamente por costo estimado. Deben revisarse cardinalidad, selectividad, predicados, ordenamientos, estadísticas y volumen real.
7. Un `TABLE ACCESS FULL` no implica por sí mismo un defecto: puede ser óptimo para tablas pequeñas o predicados de baja selectividad.
8. Las búsquedas con `LOWER(...) LIKE '%texto%'` de Auditoría se documentan como caso especial; no se debe imponer un índice B-tree convencional sin evidencia.

## Evidencia esperada

Conservar fuera del repositorio cualquier salida que incluya datos operativos sensibles. Para la documentación técnica basta registrar de forma saneada:

- fecha/hora de ejecución;
- ambiente y esquema, sin credenciales;
- `NUM_ROWS`, `LAST_ANALYZED`, `STALE_STATS` de tablas críticas;
- índices existentes relevantes;
- operación principal de cada plan (`INDEX RANGE SCAN`, `TABLE ACCESS FULL`, `SORT ORDER BY`, etc.);
- costo/cardinalidad estimada;
- predicados relevantes;
- hallazgos y decisión: `SIN_CAMBIO`, `REQUIERE_REESCRITURA`, `CANDIDATO_INDICE`, `REQUIERE_ESTADISTICAS`.

La creación de índices o cambios SQL pertenece a una intervención posterior y requiere evidencia física del ambiente Oracle autorizado.