# Estado de colaboración y punto de continuidad

> Actualización 2026-08-10: **DB-03 — Profiling Oracle / `EXPLAIN PLAN`** quedó preparado y certificado a nivel de repositorio en `desarrollo`. El paquete identifica 11 consultas críticas reales del backend, levanta estadísticas/cardinalidad/índices existentes, genera `EXPLAIN PLAN` con `DBMS_XPLAN` y está protegido contra DDL de índices, DML sobre tablas `RL_*`, scripts de transición, limpieza `B10_*` y credenciales versionadas. El HEAD técnico `8c34b62bce9a962b160129419a54125391922360` fue certificado por Quality Gates Run `31411370593` (#619) en **SUCCESS**. **La ejecución física Oracle permanece pendiente** porque esta intervención no dispone de una conexión institucional autorizada ni secretos Oracle; no se inventaron planes, costes ni cardinalidades.

Documento vivo. Los antecedentes históricos permanecen en [`BITACORA_COLABORACION.md`](../../BITACORA_COLABORACION.md).

---

## 1. Línea base vigente

- **Repositorio:** `jmejia31/RIESGO_LAVADO`
- **Rama obligatoria:** `desarrollo`
- **Base DB-03:** `ff1cc95c72566223274b23574d4ff4db3e310fe1`
- **HEAD técnico DB-03 certificado:** `8c34b62bce9a962b160129419a54125391922360`
- **Rama estable:** `main`
- **HEAD de `main`:** `727082c6fcf90f95ce6db5eadf5c4b152397d080`
- **PR #20:** debe permanecer abierto, en borrador y sin fusión
- **Modelo Matrices:** 17 tablas `RL_MR_*` + 17 secuencias
- **Oracle físico ejecutado en DB-03:** **NO**
- **DDL/DML de negocio ejecutado en DB-03:** **NO**

---

## 2. DB-03 — estado certificado

### Paquete técnico

Ubicación: `database/19_matrices_riesgos/performance/`

- `00_db03_ejecutar_profiling_autorizado.sql`
  - exige `CURRENT_SCHEMA = RIESGO_LAVADO`;
  - exige token manual `EJECUTAR_DB03`;
  - ejecuta únicamente inventario + profiling DB-03;
  - no invoca transición 06 ni limpieza B10.
- `01_db03_inventario_estadisticas_solo_lectura.sql`
  - identidad del ambiente sin credenciales;
  - `USER_TAB_STATISTICS`;
  - cardinalidad real de tablas críticas;
  - `USER_INDEXES` / `USER_IND_COLUMNS`;
  - estadísticas de columnas relevantes.
- `02_db03_explain_plan_consultas_criticas.sql`
  - 11 `EXPLAIN PLAN`;
  - 11 salidas `DBMS_XPLAN.DISPLAY`;
  - binds tipados de referencia;
  - `ROLLBACK` final de las filas diagnósticas de `PLAN_TABLE`;
  - sin `COMMIT`.

### Consultas críticas incluidas

| ID | Alcance |
|---|---|
| DB03_Q01 | Versión vigente de formulario por familia |
| DB03_Q02 | Evaluaciones paginadas sin filtros opcionales |
| DB03_Q03 | Evaluaciones paginadas con riesgo/estado/área/residual |
| DB03_Q04 | Consolidado tipado de Matrices |
| DB03_Q05 | Historial de flujos por evaluación |
| DB03_Q06 | Resumen operativo / dashboard |
| DB03_Q07 | Alertas por evaluación |
| DB03_Q08 | Automonitoreo por evaluación |
| DB03_Q09 | Auditoría paginada con filtros exactos/fecha |
| DB03_Q10 | Auditoría con búsqueda de subcadena |
| DB03_Q11 | Metodología dinámica vigente |

### Política de optimización

- **No se creó ningún índice nuevo.**
- Los índices existentes se evalúan antes de proponer otros.
- `TABLE ACCESS FULL` no se considera automáticamente un defecto.
- Estados/flags de baja cardinalidad no se indexan por intuición.
- La búsqueda `LOWER(...) LIKE '%texto%'` de Auditoría se trata como caso especial y no como candidato automático a B-tree.
- Si estadísticas están ausentes u obsoletas, DB-03 registra el hallazgo; no ejecuta `DBMS_STATS` automáticamente.
- Un cambio futuro de índice requiere evidencia física Oracle saneada.

---

## 3. Evidencia CI DB-03

**Quality Gates Run:** `31411370593` (#619) — **SUCCESS**

- Validador DB-03: **CORRECTO**.
- Paquete protegido contra DDL/DML de negocio: **CORRECTO**.
- Build Release: **0 errores / 0 advertencias**.
- Backend: **304/304** pruebas aprobadas.
- Frontend: **162/162** pruebas aprobadas en 25 archivos.
- E2E Playwright: **13/13** aprobadas.
- `npm audit`: **0 vulnerabilidades**.
- Cobertura Backend: **22.19% líneas / 24.83% ramas**.
- Cobertura Frontend: **39.53% sentencias / 35.24% ramas / 35.99% funciones / 39.15% líneas**.
- Inventario exacto Matrices: **17 tablas / 17 secuencias**.
- Contrato UAT/autorización Matrices: **correcto**.
- CI declara expresamente que **no ejecuta Oracle real ni genera planes físicos**.

---

## 4. Estado consolidado del Plan de Mejoras Integrales

| Orden | Código | Estado |
|---:|---|---|
| 1 | GOV-01 — Sincronización Bitácora / UAT | **Completado** |
| 2 | BE-01 + FE-02 — ProblemDetails + Interceptor HTTP | **Completado y certificado** |
| 3 | BE-03 — `/healthz` + `/readyz` | **Completado y certificado** |
| 4 | BE-04 — Rate Limiting | **Completado y certificado** |
| 5 | BE-02 — Caché con invalidación explícita | **Completado y certificado** |
| 6 | DB-03 — Profiling Oracle / `EXPLAIN PLAN` | **Paquete/CI completado; ejecución física pendiente** |
| 7 | DB-01 — Política de archivado de auditoría | **Pendiente; no iniciar hasta resolver continuidad DB-03** |
| 8 | FE-03 + FE-04 — Accesibilidad + Skeleton Loaders | Pendiente |
| 9 | FE-01 — Signals gradual | Pendiente |
| 10 | GOV-02 + GOV-03 — Analyzers/Sonar + Docker multietapa | Pendiente |

---

## 5. Directrices activas

1. Trabajar solo sobre `desarrollo`.
2. No modificar/fusionar `main` sin autorización expresa de Javier Mejía.
3. Mantener PR #20 abierto y en borrador; no auto-merge.
4. No ejecutar transición 05/06 ni modificar/eliminar `B10_*`.
5. No versionar secretos ni cadenas de conexión.
6. DB-03 puede ejecutar `EXPLAIN PLAN` únicamente en el ambiente Oracle institucional autorizado y mediante el paquete versionado.
7. DB-03 **no autoriza** `CREATE INDEX`, `ALTER TABLE`, DML de negocio ni `DBMS_STATS` automático.
8. Toda evidencia Oracle debe sanearse antes de incorporarse a documentación/versionado.
9. La bitácora es histórica e inmutable: nuevas correcciones se agregan, no reescriben entradas anteriores.

---

## 6. Punto exacto de continuación

### DB-03 — ejecución física pendiente

El repositorio ya está preparado y certificado. Para completar la evidencia física se debe ejecutar, desde un cliente SQL*Plus autorizado contra el esquema institucional correcto:

```sql
@database/19_matrices_riesgos/performance/00_db03_ejecutar_profiling_autorizado.sql EJECUTAR_DB03
```

Después deben registrarse de forma saneada los 11 planes, estadísticas/cardinalidades y un dictamen por consulta: `SIN_CAMBIO`, `REQUIERE_ESTADISTICAS`, `REQUIERE_REESCRITURA` o `CANDIDATO_INDICE`.

**No avanzar a creación de índices ni declarar DB-03 físicamente cerrada sin esa evidencia real.**
