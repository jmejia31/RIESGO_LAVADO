# Estado de colaboración y punto de continuidad

> Actualización 2026-08-10: **DB-01 — Política de archivado de `RL_AUDITORIA`** fue implementada y certificada técnicamente en `desarrollo`. Se estableció una política `COPY_ONLY`, sin borrado automático, sin purga manual autorizada por DB-01, sin tabla histórica creada y sin movimiento físico de registros. La retención institucional permanece **NO DEFINIDA** hasta aprobación expresa de Cumplimiento/Legal. El HEAD técnico `ce2193cd60ff441ebfba4920be7df20c0ca8b29e` fue certificado por Quality Gates Run `31418050903` (#633) en **SUCCESS**: DB-01 Validator correcto, Backend 304/304, Frontend 162/162, E2E 13/13, build Release 0 errores/0 advertencias y `npm audit` 0 vulnerabilidades. Oracle no fue conectado ni ejecutado durante DB-01.

Documento vivo. Los antecedentes históricos permanecen en [`BITACORA_COLABORACION.md`](../../BITACORA_COLABORACION.md).

---

## 1. Línea base vigente

- **Repositorio:** `jmejia31/RIESGO_LAVADO`
- **Rama obligatoria:** `desarrollo`
- **Base DB-01:** `ba8aaa9429aff7357bec12f0e8f1bd4e9eb94aac`
- **HEAD técnico DB-01 certificado:** `ce2193cd60ff441ebfba4920be7df20c0ca8b29e`
- **Rama estable:** `main`
- **HEAD de `main`:** `727082c6fcf90f95ce6db5eadf5c4b152397d080`
- **PR #20:** debe permanecer abierto, en borrador y sin fusión
- **Modelo Matrices:** 17 tablas `RL_MR_*` + 17 secuencias
- **DB-03:** cerrado físicamente; 11 planes ejecutados; sin índices nuevos
- **DB-01:** política y controles de repositorio completados; no hubo ejecución física Oracle

---

## 2. DB-01 — estado certificado

### Política aprobada técnicamente

1. `RL_AUDITORIA` continúa siendo la fuente de verdad de auditoría.
2. La primera implementación física futura deberá seguir `COPY_ONLY`:
   - identificar lote candidato con fecha de corte aprobada;
   - excluir retenciones extraordinarias / `LEGAL_HOLD`;
   - copiar a destino histórico previamente autorizado;
   - reconciliar origen e histórico;
   - certificar el lote;
   - conservar intacta la fuente.
3. **Borrado automático: PROHIBIDO.**
4. DB-01 tampoco autoriza purga manual de la fuente.
5. No se configura `DBMS_SCHEDULER`, `DBMS_JOB`, trigger ni tarea periódica de limpieza.
6. No se crea tabla histórica ni esquema histórico durante DB-01.
7. No se crea ningún índice nuevo; DB-03 determinó que no se justifica con el volumen actual.
8. No se presupone Oracle Partitioning ni su licenciamiento.

### Retención

- **Plazo institucional:** NO DEFINIDO.
- **Fecha de corte:** NO DEFINIDA.
- Hasta que Cumplimiento/Legal apruebe ambos elementos, ningún registro se considera elegible para purga.
- Una investigación, requerimiento legal, incidente, litigio o auditoría puede imponer retención extraordinaria aunque el registro sea antiguo.

### Reconciliación futura obligatoria

Todo lote físico futuro deberá registrar y validar, como mínimo:

- identificador de lote;
- fecha de corte aprobada;
- cantidad candidata y cantidad copiada;
- `MIN/MAX(AUD_ID)`;
- `MIN/MAX(AUD_FECHA)`;
- ausencia de IDs faltantes;
- ausencia de duplicados;
- resultado `CONCILIADO` o `RECHAZADO`;
- responsable técnico y aprobador funcional.

Una copia finalizada sin error **no** equivale por sí sola a un lote certificado.

---

## 3. Artefactos DB-01

### Política y diseño

`docs/4. Base de Datos/DB_01_POLITICA_ARCHIVADO_RL_AUDITORIA_2026-08-10.md`

Contiene:

- estado físico y contrato actual de `RL_AUDITORIA`;
- política de retención;
- `COPY_ONLY`;
- `LEGAL_HOLD`;
- reconciliación;
- seguridad y privacidad;
- destino histórico futuro;
- reversibilidad;
- matriz de autorizaciones;
- criterios de cierre.

### Diagnóstico agregado de solo lectura

`database/auditoria/archivado/01_db01_diagnostico_rl_auditoria_solo_lectura.sql`

Mide únicamente:

- total de registros;
- fecha mínima/máxima;
- crecimiento mensual;
- distribución por acción;
- distribución por módulo;
- top 20 tablas auditadas;
- longitud agregada de CLOB.

No proyecta correos, IP ni contenido de CLOB y no ejecuta DDL/DML.

### Validador bloqueante

`scripts/validation/validate_db01_auditoria_archiving.ps1`

Quality Gates verifica que:

- exista la política y el diagnóstico;
- el paquete SQL permanezca de solo lectura;
- no exista DDL/DML de archivo;
- no exista scheduler/job Oracle;
- no se alcance transición 05/06 ni `B10_*`;
- se mantenga el contrato físico esperado de `RL_AUDITORIA`;
- se mantenga el contrato Backend de inserción + consulta/paginación;
- no se versionen secretos.

---

## 4. Evidencia CI DB-01

**Quality Gates Run:** `31418050903` (#633) — **SUCCESS**

- DB-01 Validator: **CORRECTO**.
- Política `COPY_ONLY`, sin borrado automático, DDL/DML físico ni scheduler: **CORRECTA**.
- Retención: **NO DEFINIDA** hasta aprobación institucional.
- Build Release: **0 errores / 0 advertencias**.
- Backend: **304/304** pruebas aprobadas.
- Frontend: **162/162** pruebas aprobadas en 25 archivos.
- E2E Playwright: **13/13** aprobadas.
- `npm audit`: **0 vulnerabilidades**.
- Cobertura Backend: **22.19% líneas / 24.83% ramas**.
- Cobertura Frontend: **39.53% sentencias / 35.24% ramas / 35.99% funciones / 39.15% líneas**.
- Inventario exacto Matrices: **17 tablas / 17 secuencias**.
- Autorización/UAT Matrices: **correctos**.

### Oracle durante DB-01

- **NO** se abrió conexión Oracle.
- **NO** se ejecutó DDL.
- **NO** se ejecutó DML.
- **NO** se creó tabla histórica.
- **NO** se movió ni eliminó un registro de `RL_AUDITORIA`.
- **NO** se ejecutaron scripts 05/06.
- **NO** se modificaron `B10_*`.

---

## 5. Estado consolidado del Plan de Mejoras Integrales

| Orden | Código | Estado |
|---:|---|---|
| 1 | GOV-01 — Sincronización Bitácora / UAT | **Completado** |
| 2 | BE-01 + FE-02 — ProblemDetails + Interceptor HTTP | **Completado y certificado** |
| 3 | BE-03 — `/healthz` + `/readyz` | **Completado y certificado** |
| 4 | BE-04 — Rate Limiting | **Completado y certificado** |
| 5 | BE-02 — Caché con invalidación explícita | **Completado y certificado** |
| 6 | DB-03 — Profiling Oracle / `EXPLAIN PLAN` | **Completado físicamente; sin índices nuevos** |
| 7 | DB-01 — Política de archivado de auditoría | **Completado y certificado técnicamente** |
| 8 | FE-03 + FE-04 — Accesibilidad + Skeleton Loaders | **Siguiente** |
| 9 | FE-01 — Signals gradual | Pendiente |
| 10 | GOV-02 + GOV-03 — Analyzers/Sonar + Docker multietapa | Pendiente |

---

## 6. Directrices activas

1. Trabajar exclusivamente sobre `desarrollo`.
2. No modificar/fusionar `main` sin autorización expresa de Javier Mejía.
3. Mantener PR #20 abierto y en borrador; no auto-merge.
4. No ejecutar transición 05/06 ni modificar/eliminar `B10_*`.
5. No versionar secretos ni cadenas de conexión.
6. DB-01 no autoriza eliminación automática ni manual de `RL_AUDITORIA`.
7. Cualquier plazo de retención/fecha de corte requiere aprobación formal de Cumplimiento/Legal.
8. Cualquier destino histórico requiere autorización DDL separada.
9. Cualquier copia física requiere autorización DML separada y reconciliación obligatoria.
10. Cualquier futura purga requiere una política separada, archivo reconciliado y autorización específica.
11. Si la cardinalidad degrada Q09/Q10, volver a perfilar conforme DB-03 antes de crear índices.
12. La bitácora histórica es append-only; las correcciones se agregan, no reescriben entradas anteriores.

---

## 7. Punto exacto de continuación

**DB-01 queda cerrada a nivel de política, diseño y controles de repositorio sin modificar físicamente Oracle.**

La siguiente fase de la secuencia aprobada es:

### FE-03 + FE-04 — Accesibilidad / WAI-ARIA + Skeleton Loaders

Debe mejorar accesibilidad y estados de carga de forma transversal sin alterar contratos funcionales ni degradar las pruebas UAT existentes.
