# DB-03 — Profiling Oracle / `EXPLAIN PLAN`

**Fecha:** 2026-08-10  
**Repositorio:** `jmejia31/RIESGO_LAVADO`  
**Rama de trabajo:** `desarrollo`  
**Base de inicio:** `ff1cc95c72566223274b23574d4ff4db3e310fe1`  
**Autorización funcional de Javier Mejía para iniciar DB-03:** **OTORGADA**.  
**Ambiente Oracle accesible desde esta intervención:** **NO DISPONIBLE**.  
**Ejecución física Oracle:** **PENDIENTE**.  
**DDL de índices:** **NO EJECUTADO**.  
**DML sobre tablas de negocio:** **NO EJECUTADO**.

---

## 1. Objetivo

DB-03 establece evidencia objetiva antes de modificar índices o SQL. El propósito es determinar, sobre el ambiente Oracle autorizado, cómo el optimizador resuelve las consultas de mayor impacto del sistema y separar cuatro escenarios:

1. plan adecuado, sin cambio;
2. estadísticas insuficientes o obsoletas;
3. SQL que requiere reescritura;
4. candidato real a índice, sujeto a evidencia física.

**No se aprueba ningún índice nuevo sin evidencia física** de cardinalidad, estadísticas y `EXPLAIN PLAN` del ambiente autorizado.

---

## 2. Alcance técnico preparado

El paquete está en:

`database/19_matrices_riesgos/performance/`

Archivos:

- `00_db03_ejecutar_profiling_autorizado.sql` — entrada manual con guardas de esquema y autorización;
- `01_db03_inventario_estadisticas_solo_lectura.sql` — estadísticas, cardinalidad e índices existentes;
- `02_db03_explain_plan_consultas_criticas.sql` — 11 planes de ejecución basados en SQL real del backend;
- `README.md` — procedimiento, restricciones y evidencia esperada.

El paquete no forma parte de los maestros de instalación/actualización y no invoca scripts de transición.

---

## 3. Consultas críticas seleccionadas

| ID | Consulta / origen funcional | Riesgo de rendimiento a comprobar |
|---|---|---|
| `DB03_Q01` | Versión vigente de formulario por familia | join familia-versión + vigencia/estado |
| `DB03_Q02` | Evaluaciones paginadas sin filtros opcionales | `ROW_NUMBER()` del último flujo + joins + sort + ROWNUM |
| `DB03_Q03` | Evaluaciones paginadas con riesgo/estado/área/residual | combinación de filtros sobre evaluación, flujo y proyección |
| `DB03_Q04` | Consolidado tipado de Matrices | recorrido de evaluaciones/proyecciones + orden por fecha |
| `DB03_Q05` | Historial de flujos de evaluación | filtro por evaluación + orden fecha/ID descendente |
| `DB03_Q06` | Resumen operativo / dashboard | ocho agregaciones/conteos sobre tablas funcionales |
| `DB03_Q07` | Alertas por evaluación | filtro por evaluación + orden con `NVL(fecha)` |
| `DB03_Q08` | Automonitoreo por evaluación | filtro por evaluación + orden fecha/ID |
| `DB03_Q09` | Auditoría paginada con filtros exactos/fecha | crecimiento histórico + sort + paginación Oracle 11g |
| `DB03_Q10` | Auditoría con búsqueda de subcadena | `LOWER(...) LIKE '%texto%'` y subconsulta de usuario |
| `DB03_Q11` | Metodología dinámica vigente | vigencia/estado + Top-1 por fecha/ID |

La selección prioriza consultas con paginación, analítica/window functions, joins, agregaciones, ordenamientos y tablas con crecimiento acumulativo.

---

## 4. Índices del modelo actual que deben evaluarse antes de proponer otros

El modelo reducido ya define, entre otros:

- `IDX_RL_MR_VER_VIG (VER_FAMILIA_ID, VER_VIGENTE, VER_ESTADO)`;
- `IDX_RL_MR_EVA_RIE (EVA_RIESGO_ID)`;
- `IDX_RL_MR_EVA_VER (EVA_VERSION_ID)`;
- `IDX_RL_MR_FLU_EVA_FEC (FLU_EVALUACION_ID, FLU_FECHA)`;
- `IDX_RL_MR_PROY_BUSQ (PROY_ESTADO_EVALUACION, PROY_NIVEL_RESIDUAL, PROY_FECHA_EVAL)`;
- `IDX_RL_MR_PROY_AREA (PROY_AREA_PRINCIPAL)`;
- `IDX_RL_MR_PROY_DUENO (PROY_DUENO_RIESGO)`;
- `IDX_RL_MR_CON_EVA (CON_EVALUACION_ID)`;
- `IDX_RL_MR_ECO_CON (ECO_CONTROL_ID)`;
- `IDX_RL_MR_PLA_EVA (PLA_EVALUACION_ID)`;
- `IDX_RL_MR_ACT_PLAN (ACT_PLAN_ID)`;
- `IDX_RL_MR_EVV_ENTIDAD (EVV_TIPO_ENTIDAD, EVV_ENTIDAD_ID)`;
- `IDX_RL_MR_EVV_EVIDENCIA (EVV_EVIDENCIA_ID)`;
- `IDX_RL_MR_ALE_EVAL (ALE_EVALUACION_ID, ALE_ESTADO)`;
- `IDX_RL_MR_MON_EVAL_FEC (MON_EVALUACION_ID, MON_FECHA)`.

Por tanto, DB-03 **no parte de la premisa de que faltan índices**. Primero debe comprobar si los actuales son elegibles, selectivos y realmente usados por el optimizador.

---

## 5. Hipótesis que DB-03 debe validar, no asumir

### H1 — último flujo por evaluación

La subconsulta analítica usa:

`PARTITION BY FLU_EVALUACION_ID ORDER BY FLU_FECHA DESC, FLU_ID DESC`.

Existe `IDX_RL_MR_FLU_EVA_FEC`, pero el plan real debe mostrar si reduce el trabajo del `WINDOW SORT` y si la ausencia de `FLU_ID` en el índice tiene impacto material. **No se propone ampliar el índice sin evidencia.**

### H2 — paginación de evaluaciones

El orden principal es `EVA_FECHA_REGISTRO DESC, EVA_ID DESC`, mientras los filtros pueden llegar por riesgo, estado, área y nivel residual. La mejor estrategia depende de selectividad y distribución real. Un índice único que intente cubrir todas las variantes podría aumentar costo de escritura sin beneficio estable.

### H3 — proyecciones

`IDX_RL_MR_PROY_BUSQ` comienza por estado y residual; `IDX_RL_MR_PROY_AREA` cubre área. Se debe comprobar el orden de predicados y cardinalidad antes de considerar índices compuestos alternativos.

### H4 — dashboard

Los estados/flags suelen ser de baja cardinalidad. Un `TABLE ACCESS FULL` puede ser correcto, especialmente con tablas pequeñas. No crear índices únicamente porque el dashboard use `COUNT(*)`.

### H5 — Auditoría

Auditoría tiene dos perfiles distintos:

- filtros exactos/fecha, potencialmente indexables;
- búsqueda de subcadena con `LOWER(...) LIKE '%texto%'`, que normalmente no obtiene beneficio de un B-tree convencional por el comodín inicial.

DB-03 debe separar ambos casos antes de cualquier decisión. DB-01, posterior, abordará además la política de archivado y crecimiento histórico.

### H6 — estadísticas

Si `LAST_ANALYZED` está ausente/antiguo o `STALE_STATS = 'YES'`, el costo/cardinalidad del plan puede no ser representativo. En DB-03 se registra el hallazgo, pero **no se ejecuta `DBMS_STATS` automáticamente**.

---

## 6. Criterios de evaluación de cada plan

Por cada `DB03_Qxx` registrar:

| Campo | Evidencia |
|---|---|
| Operación dominante | `INDEX RANGE SCAN`, `TABLE ACCESS FULL`, `HASH JOIN`, `NESTED LOOPS`, `WINDOW SORT`, etc. |
| Cardinalidad estimada | filas del optimizador |
| Costo estimado | costo total y nodos dominantes |
| Access predicates | predicados usados para acceso |
| Filter predicates | filtros aplicados después del acceso |
| Sort/window | presencia y costo relativo |
| Índice utilizado | nombre, si aplica |
| Estadísticas | `LAST_ANALYZED`, `STALE_STATS`, `NUM_ROWS` |
| Cardinalidad real de tabla | conteo levantado por el inventario |
| Dictamen | `SIN_CAMBIO`, `REQUIERE_ESTADISTICAS`, `REQUIERE_REESCRITURA`, `CANDIDATO_INDICE` |

El costo estimado no es una métrica suficiente para aprobar cambios por sí solo.

---

## 7. Seguridad operacional

1. El entrypoint exige `CURRENT_SCHEMA = RIESGO_LAVADO`.
2. Requiere el token explícito `EJECUTAR_DB03`.
3. No contiene credenciales ni cadena de conexión.
4. No ejecuta `06_reconstruir_modelo_17_tablas.sql`.
5. No modifica/elimina `B10_*`.
6. No contiene `CREATE INDEX`, `ALTER TABLE`, `DROP` ni `TRUNCATE`.
7. No ejecuta `INSERT`, `UPDATE`, `DELETE` o `MERGE` sobre tablas `RL_*`.
8. `EXPLAIN PLAN` escribe únicamente información diagnóstica en `PLAN_TABLE`; el script finaliza con `ROLLBACK` y no realiza `COMMIT`.
9. CI solo valida estáticamente el paquete; no recibe ni utiliza credenciales Oracle.

---

## 8. Limitación de `EXPLAIN PLAN`

`EXPLAIN PLAN` representa la estimación del optimizador para la sentencia explicada. El comportamiento de binds y estadísticas puede hacer que un cursor ejecutado por la aplicación difiera del plan estimado. Por eso una recomendación de alto impacto debe, cuando el ambiente y privilegios lo permitan, contrastarse posteriormente con evidencia de cursor real (`DBMS_XPLAN.DISPLAY_CURSOR`) o trazas aprobadas, sin incorporar secretos ni datos sensibles al repositorio.

DB-03 no ejecutará automáticamente las consultas funcionales solo para obtener métricas reales; se preserva el principio de mínimo impacto sobre el ambiente institucional.

---

## 9. Estado de la ejecución física

**Ejecución física Oracle:** **PENDIENTE**.

Motivo: esta intervención tiene autorización funcional para DB-03, pero el entorno operativo de ChatGPT/GitHub no expone una conexión Oracle institucional autorizada ni credenciales seguras que permitan ejecutar SQL*Plus contra el ambiente. No se inventan costos, cardinalidades ni planes.

Para cerrar la evidencia física debe ejecutarse manualmente en el ambiente autorizado:

```sql
@database/19_matrices_riesgos/performance/00_db03_ejecutar_profiling_autorizado.sql EJECUTAR_DB03
```

La salida debe sanearse antes de versionar cualquier conclusión.

---

## 10. Criterio de cierre DB-03

### Cierre técnico de repositorio

Se considera cumplido cuando:

- el paquete SQL está versionado;
- el validador está en Quality Gates;
- CI demuestra que no se incorporó DDL/DML de negocio ni secretos;
- las 11 consultas corresponden al SQL real del backend;
- no se crea ningún índice especulativo.

### Cierre físico Oracle

Permanece pendiente hasta disponer de:

- salida saneada de inventario/estadísticas;
- 11 `EXPLAIN PLAN` reales;
- dictamen por consulta;
- decisión documentada de `SIN_CAMBIO` o acciones posteriores.

Hasta ese momento, cualquier cambio de índice queda bloqueado.