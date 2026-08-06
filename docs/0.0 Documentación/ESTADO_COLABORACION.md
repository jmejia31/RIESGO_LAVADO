# Estado de colaboración y punto de continuidad

> Actualización 2026-08-06: completada la Fase 8 del Módulo Matrices de Riesgos. La revisión pre-Oracle detectó y corrigió una ruta automática que todavía podía instalar el modelo heredado de 34 tablas. El paquete 19 quedó fuera de ambos maestros, su punto de entrada fue bloqueado, los instaladores heredados fueron retirados y se agregó una puerta pre-Oracle vinculante. El Quality Gate aprobó 222 pruebas Backend, 123 Frontend y 8 E2E, con Release en 0 advertencias y 0 errores. Oracle y los scripts `05` y `06` no fueron ejecutados. `main` permanece intacta y el PR #20 continúa abierto y en borrador.

Documento vivo. Debe actualizarse al finalizar cada intervención.

---

## 1. Línea base vigente

- **Repositorio:** `jmejia31/RIESGO_LAVADO`
- **Rama obligatoria:** `desarrollo`
- **Rama estable:** `main` — no modificar ni integrar sin autorización expresa de Javier Mejía
- **Ramas remotas permitidas:** únicamente `main` y `desarrollo`
- **PR de revisión:** #20, abierto, borrador y sin autorización de fusión
- **Arquitectura:** monolito modular con Angular, ASP.NET Core y Oracle 11g
- **Modelo objetivo de Matrices:** 17 tablas y 17 secuencias

---

## 2. Última intervención

- **Intervención:** Fase 8 — Revisión final no Oracle previa a autorización física
- **Fecha:** 2026-08-06
- **Rama:** `desarrollo`
- **Estado:** completada y validada sin Oracle
- **Documento:** [`FASE_8_REVISION_PRE_ORACLE_MODELO_17_TABLAS_2026-08-06.md`](../3.%20Módulo%20Matrices%20de%20Riesgos/FASE_8_REVISION_PRE_ORACLE_MODELO_17_TABLAS_2026-08-06.md)

### Hallazgo crítico resuelto

Los dos maestros de base de datos todavía incluían el paquete 19, cuyo punto de entrada llamaba scripts activos que construían el modelo heredado de 34 tablas y 24 secuencias.

La remediación aplicada fue:

- retirar el paquete 19 de `00_EJECUCION_PRIMERA_VEZ.sql`;
- retirar el paquete 19 de `00_EJECUCION_ACTUALIZACIONES_SEGURAS.sql`;
- bloquear `19_matrices_riesgos/00_APLICAR_MODULO_MATRICES_RIESGOS.sql` sin includes, DDL ni DML;
- eliminar de la ruta activa los instaladores heredados `01` y `02`;
- actualizar README y manifiesto de base de datos;
- reforzar `tools/validate_database_scripts.ps1`;
- agregar `validate_matrices_preoracle_readiness.ps1`;
- hacer obligatoria la cuarentena pre-Oracle dentro del Quality Gate.

### Resultado técnico verificado

```text
Quality Gate: 31114220642
Commit técnico: 540a958fc1fb96018f6d88d9046c4d714130f5e8
Resultado: success
```

- validación general de base de datos: correcta;
- preparación pre-Oracle: correcta;
- alineación dinámica: correcta;
- inventario: 17 tablas y 17 secuencias;
- pruebas negativas del inventario: 9 aprobadas;
- Backend: 222 pruebas aprobadas;
- Frontend: 123 pruebas aprobadas;
- E2E: 8 recorridos aprobados;
- cobertura Backend: líneas 16.72 %, ramas 17.18 %;
- cobertura Frontend: sentencias 34.41 %, ramas 31.52 %, funciones 31.69 %, líneas 33.87 %;
- Release: 0 advertencias y 0 errores.

---

## 3. Estado de fases del Módulo Matrices de Riesgos

| Fase | Descripción | Estado real |
|---|---|---|
| **Fase 0-R** | Aprobación funcional del modelo reducido | **Aprobada** |
| **Fase 0-C** | Congelamiento técnico y línea base | **Completada** |
| **Fase 1** | Alineación de columnas JSON | **Completada** |
| **Fase 2** | Retiro de trazas de cálculo | **Completada** |
| **Fase 3** | Auditoría institucional | **Completada** |
| **Fase 4** | Retiro de adaptadores y contratos heredados | **Completada** |
| **Fase 5** | Inventario exacto de 17 tablas y 17 secuencias | **Completada** |
| **Fase 6** | Pruebas automatizadas no Oracle | **Completada** |
| **Fase 7** | Suite Oracle del modelo reducido | **Completada en código; certificación física pendiente** |
| **Fase 8** | Revisión final no Oracle y cuarentena | **Completada** |
| **Fase 9** | Ambiente Oracle exclusivo y expediente de autorización | **Siguiente fase; sin ejecución** |
| **Fase 10** | Transición física controlada | **Bloqueada; requiere autorización expresa** |
| **Fase 11** | Certificación física y funcional Oracle | **Bloqueada** |
| **Fase 12** | Documentación y cierre técnico | **Pendiente** |

---

## 4. Estado actual de base de datos

### Flujos automáticos

El paquete Matrices de Riesgos no pertenece actualmente a:

```text
database/00_EJECUCION_PRIMERA_VEZ.sql
database/00_EJECUCION_ACTUALIZACIONES_SEGURAS.sql
```

El punto de entrada:

```text
database/19_matrices_riesgos/00_APLICAR_MODULO_MATRICES_RIESGOS.sql
```

permanece bloqueado y no contiene includes, DDL ni DML.

### Transición manual

La definición física objetivo continúa exclusivamente en:

```text
database/19_matrices_riesgos/transicion/06_reconstruir_modelo_17_tablas.sql
```

El script `06`:

- es manual y destructivo;
- exige `CURRENT_SCHEMA = RIESGO_LAVADO`;
- exige el parámetro `EJECUTAR`;
- exige `RL_USUARIOS`;
- crea exactamente 17 tablas y 17 secuencias;
- no está integrado en ningún flujo automático;
- no ha sido ejecutado.

---

## 5. Bloqueantes vigentes

1. No se ha identificado ni aprobado formalmente una base Oracle exclusiva de pruebas.
2. No existe todavía evidencia de respaldo completo y restauración validada para la transición.
3. No se ha otorgado autorización expresa para ejecutar el script `06`.
4. El modelo no ha sido instalado ni certificado físicamente en Oracle.
5. La suite Oracle no se ha ejecutado con `RL_ORACLE_INTEGRATION_REQUIRED=true`.
6. `npm ci` continúa informando 13 vulnerabilidades globales: 6 moderadas, 6 altas y 1 crítica. Requieren una intervención separada antes de producción; no aplicar `--force` sin análisis.

---

## 6. Directrices activas

1. Trabajar únicamente en `desarrollo`.
2. No modificar ni fusionar `main`.
3. Mantener el PR #20 abierto y en borrador.
4. No habilitar auto-merge.
5. No ejecutar Oracle.
6. No ejecutar los scripts `05` o `06`.
7. No ejecutar `CREATE`, `DROP`, `TRUNCATE` ni migraciones.
8. No incorporar el paquete 19 a los maestros automáticos.
9. No restaurar los instaladores heredados de 34 tablas.
10. No versionar credenciales o cadenas de conexión.
11. No declarar certificado el modelo antes de las pruebas Oracle reales.
12. Mantener commits identificables, validación y documentación por fase.

---

## 7. Punto exacto de continuación

La siguiente intervención es la **Fase 9 — preparación del ambiente Oracle exclusivo y expediente de autorización**, sin ejecutar todavía el script `06`.

Debe producir y validar:

1. identificación de la instancia y esquema exclusivos de pruebas;
2. confirmación escrita de ausencia de datos productivos;
3. responsable DBA y participantes de la ventana;
4. respaldo completo previo;
5. prueba de restauración del respaldo;
6. inventario físico previo del esquema;
7. capacidad y permisos mínimos necesarios;
8. método seguro para proporcionar la cadena Oracle;
9. plan de ejecución paso a paso del script `06`;
10. plan de contingencia ante fallo parcial de DDL;
11. checklist de evidencias sin secretos;
12. criterios de entrada y salida;
13. autorización expresa y separada de Javier Mejía antes de ejecutar cualquier cambio.

La Fase 9 debe terminar con un expediente preparado para aprobación. La ejecución física pertenece a la Fase 10 y continúa bloqueada.

---

## 8. Commits principales de la última intervención

```text
7fc8c5eee0284dd5947dddc7368ec02d091571f8
fix(database): excluir matrices del maestro de actualizaciones

e2f00ccefe0ab7755481c55033a634667fa8d818
fix(database): excluir matrices del maestro de primera instalacion

080be8b56e032093d9c8f33fa72e2a46f40ed682
fix(matrices): bloquear punto de entrada automatico pre Oracle

5618aa46774c6bf9ddf0ce7c575891673318e70e
refactor(matrices): retirar instalador heredado de 34 tablas

9caa255c2810d54bd1d3289ed8ff628f8918960d
refactor(matrices): retirar indices del modelo heredado

de5d0746935c4083e8d2e620d98ebea7992bbcdb
test(database): bloquear reintroduccion automatica del modelo heredado

9b8de8ff15963093a4718085ea398afaf6edd90b
test(matrices): agregar puerta de preparacion pre Oracle

6903b1a84ab8527e8f487d3153107e63989d206e
ci(matrices): exigir cuarentena pre Oracle en Quality Gates

540a958fc1fb96018f6d88d9046c4d714130f5e8
test(matrices): alinear puerta pre Oracle con contratos reales

bd5b11814194a891d53ccbca7920466958c59f0f
docs(matrices): emitir dictamen de fase 8 pre Oracle
```
