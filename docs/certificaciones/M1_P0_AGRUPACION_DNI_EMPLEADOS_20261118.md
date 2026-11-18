# Expediente Técnico P0 — Microfase 1: Restauración de Agrupación por DNI y Recuperación de Empleados Históricos

- **Fecha:** 2026-11-18 (-06:00)
- **Autor:** `ANTIG` (Antigravity)
- **Repositorio:** `jmejia31/RIESGO_LAVADO`
- **Rama autorizada:** `desarrollo`
- **Base SHA:** `3ce30913d6801c707de62db27d3a437d20391ca4` (Quality Gate previo `44342957625` — certifica solo ese SHA anterior)
- **Issue institucional de seguimiento:** [Issue #41](https://github.com/jmejia31/RIESGO_LAVADO/issues/41) (permanece **ABIERTO**)
- **Issue de seguridad de dependencias:** [Issue #42](https://github.com/jmejia31/RIESGO_LAVADO/issues/42) (`SECURITY_AUDIT=PASS_FULL_TREE` preservado intacto)
- **Aprobación funcional del propietario:** `PENDING`
- **Escrituras en Oracle institucional / `DNP_IHSS` / `main` / producción:** `0` (`NO_TOUCHED`)

---

## 1. Diagnóstico Forense (Fase A)

### 1.1 Verificación de entorno ejecutado (Sección 3.3)
- El repositorio local `C:\RIESGO_LAVADO` se encontraba exactamente en `3ce30913d6801c707de62db27d3a437d20391ca4` (sincronizado con `origin/desarrollo`).
- El proceso local `RL.API.dll` fue reiniciado por el propietario a las `2026-11-18 10:40:26` **después** de sincronizar a `3ce30913`, confirmando que los valores observados (`PERSONAS NATURALES = 5,742`, `EMPLEADOS = 0`) provenían del código real de `3ce30913` y no de binarios antiguos.

### 1.2 Causa raíz A — Pérdida de agrupación por DNI y contador `5,742` en Personas Naturales
1. En la versión previa `d7d3556958176108ed41bebd045f2de42547ad9e`, `ConstruirConsultaMonitoreoNaturales()` agrupaba `DNP_IHSS.REPORTE_COINCIDENCIAS` (`TIPO_CALIFICACION_ID = 1`) mediante `REPORTE_AGG` (`GROUP BY DNI, LISTA_CONCIDENCIA`), mostrando ~242 entradas agrupadas (y ~190 identificadas).
2. El procedimiento ETL institucional `DNP_IHSS` (`docs/1. Bases de Datos/Base de Datos DNP_IHSS.sql`, líneas 2634–2661) inserta una nueva fila en `REPORTE_COINCIDENCIAS` por cada corrida del proceso para cada socio/representante (`INSERT INTO REPORTE_COINCIDENCIAS (NOMBRE, DNI, NACIONALIDAD, TIPO_PERSONA, LISTA_CONCIDENCIA, FECHA_ENCONTRO, DATA_ID, OBSERVACION_LISTA, TIPO_CALIFICACION_ID)`), acumulando miles de coincidencias históricas para el mismo DNI y dejando **`NUMERO_PATRONO = NULL`** en registros de personas naturales.
3. En `3ce30913` (`f744afa6` y sucesivos), se eliminó toda cláusula `GROUP BY` en `ConstruirConsultaMonitoreoNaturales()` emitiendo 1 fila por `REPORTE_COINCIDENCIA_ID` con `1 TOTAL_REPETIDOS` fijo. Al cargar Naturales con el filtro por defecto `categoriaIdentidad = 'identificadas'`, cada coincidencia histórica individual apareció como fila independiente, elevando el contador a **5,742**.
4. Adicionalmente, `ObtenerDetalleNaturalAsync` filtraba únicamente por `WHERE R.REPORTE_COINCIDENCIA_ID = :registroId` (devolviendo 1 sola coincidencia) y unía `V_SOCIOS_REPRESENTANTES` con `TRIM(S.NUMERO_PATRONAL) = TRIM(TO_CHAR(R.NUMERO_PATRONO))`, condición que fallaba cuando `R.NUMERO_PATRONO IS NULL` (el caso estándar del ETL de personas naturales).

### 1.3 Causa raíz B — `EMPLEADOS = 0`
1. En `d7d35569` (donde aparecían históricamente los 7 empleados), `ConstruirConsultaMonitoreoEmpleados()` identificaba empleados mediante coincidencia calificada (`R.TIPO_CALIFICACION_ID = 1`) cuyo `R.DNI` existía en `DNP_IHSS.V_EMPLEADOS_IHSS_PLANILLAS`, sin exigir `V.TIPO_PERSONA LIKE '%IHSS'`.
2. En `3ce30913`, se eliminó el cruce por `V_EMPLEADOS_IHSS_PLANILLAS` como criterio de elegibilidad y se impuso exclusivamente `EXISTS (SELECT 1 FROM DNP_IHSS.V_REPORTE_COINCIDENCIA V WHERE V.REPORTE_COINCIDENCIA_ID = R.REPORTE_COINCIDENCIA_ID AND V.TIPO_PERSONA LIKE '%IHSS')`.
3. Dado que los registros históricos calificados (`TIPO_CALIFICACION_ID = 1`) de esos empleados no cumplen esa cadena literal sensible a mayúsculas (`'%IHSS'`) —al haber ingresado bajo otro `TIPO_PERSONA` o clasificación histórica—, el predicado descartó el universo completo y produjo **`EMPLEADOS = 0`**.
4. Además, `ObtenerDetalleEmpleadoAsync`, `ResolverIdentidadAsync`, `ResolverCoincidenciaUnicaAsync` y `AppendMonitoringFilters` exigían `V.TIPO_PERSONA LIKE '%IHSS'` y/o igualdad estricta `TRIM(NUMEPATRO) = TRIM(TO_CHAR(R.NUMERO_PATRONO))` y nombre exacto (pese a que el ETL inserta `NUMERO_PATRONO = NULL` para empleados en la línea 2710 de `Base de Datos DNP_IHSS.sql`).

### 1.4 Separación conceptual de Empleados (Sección 6.4)
- **Inventario institucional (`DNP_IHSS.V_EMPLEADOS_IHSS_PLANILLAS`):** directorio completo histórico de planillas de empleados IHSS (todos los períodos).
- **Monitoreo de Listas — Empleados:** muestra exclusivamente empleados con coincidencias calificadas en listas cautelares (`REPORTE_COINCIDENCIAS.TIPO_CALIFICACION_ID = 1` vinculados por DNI válido a `V_EMPLEADOS_IHSS_PLANILLAS` o clasificados por `TIPO_PERSONA` como IHSS/Empleado) más positivos activos de tipo empleado (`RL_LISTA_POSITIVOS.LSP_TIPO_POSITIVO_ID = 3`). Nunca se etiqueta a un empleado sin coincidencia como positivo.

---

## 2. Cambios Implementados (Fases B, C, D, F, G, H)

### 2.1 Backend (`backend/RL.API/Features/Listas/`)
- **`Contracts/ConsultasPaginadasDtos.cs`:** se agregó `TotalCoincidenciasFuente` a `MonitoreoTotalesDto` (retrocompatible) para separar explícitamente el conteo de grupos visibles (`TotalRegistros`) del volumen total de coincidencias fuente (`TotalCoincidenciasFuente`).
- **`Contracts/ListasModels.cs`:** se agregaron `IdRegistroFuente`, `TipoFuente` y `EstadoAsociacion` a `DetalleCoincidenciaNaturalDto` y `DetalleCoincidenciaEmpleadoDto`.
- **`Persistence/ListasRepository.cs`:**
  - `ConstruirConsultaMonitoreoNaturales()`: agrupación segura en SQL **antes de paginar** mediante CTE `REPORTE_GRUPOS` en un único escaneo de `DNP_IHSS.REPORTE_COINCIDENCIAS`:
    - DNI válido y atribuible → clave `'DNI:' || TRIM(R.DNI)` (una fila agrupada por DNI con `COUNT(*) TOTAL_REPETIDOS`, resumen seguro multi-lista y multi-patrono compatible con Oracle 11g XE sin `LISTAGG` riesgoso, nombre de referencia visible y detección de ambigüedad `NOMBRES_DISTINTOS` + `IDENTIDAD_AGG`).
    - Marcadores no atribuibles (`N/A`, `N/A(3)`, `NULL`, `VACIO`, vacío/nulo) → clave `'SRC:' || TO_CHAR(R.REPORTE_COINCIDENCIA_ID)`, permaneciendo aislados individualmente sin agruparse nunca bajo un marcador genérico.
    - Excluye explícitamente filas clasificadas como empleado (`UPPER(TRIM(R.TIPO_PERSONA)) LIKE '%IHSS%'` / `'%EMPLEAD%'`) sin descartar `TIPO_PERSONA IS NULL`.
    - Conserva cero `COUNT(*) OVER`, cero `V_SOCIOS_REPRESENTANTES`, cero `V_EMPLEADOS_IHSS_PLANILLAS` y cero escaneo duplicado de `REPORTE_COINCIDENCIAS`.
  - `ConstruirConsultaMonitoreoEmpleados()`: recupera todos los empleados históricos mediante elegibilidad unificada (`I.IDENTIDAD IS NOT NULL` contra `IDENTIDAD_AGG` de `V_EMPLEADOS_IHSS_PLANILLAS` sobre **todos** los períodos sin `SYSDATE` ni multiplicación por planillas, **OR** `UPPER(TRIM(NVL(R.TIPO_PERSONA, ''))) LIKE '%IHSS%'` / `'%EMPLEAD%'`), con agrupación segura por DNI válido y aislamiento de marcadores por PK, sin `COUNT(*) OVER`.
  - `ObtenerDetalleNaturalAsync()`: para DNI válido y atribuible devuelve **todas** las coincidencias individuales del grupo (ordenadas deterministamente con su propio `REPORTE_COINCIDENCIA_ID`) y enriquece empresas desde `V_SOCIOS_REPRESENTANTES` soportando `R.NUMERO_PATRONO IS NULL` cuando el sujeto es único o coincide en nombre normalizado (evitando atribuciones cruzadas si `SUJETOS_DNI > 1` y preservando coincidencias sin empresa). Para marcadores/nulos/manuales consulta estrictamente por PK de origen.
  - `ObtenerDetalleEmpleadoAsync()`, `ResolverIdentidadAsync()` y `ResolverCoincidenciaUnicaAsync()`: alineados al criterio unificado de elegibilidad de empleados y a períodos históricos de planilla sin multiplicación ni dependencia frágil de `R.NUMERO_PATRONO`.
  - `AppendMonitoringFilters()`: soporta búsqueda sobre miembros secundarios (lista/nombre/patrono) dentro de un grupo DNI cuando `:buscar` está activo, y corrige el filtro mensual exacto / rango inclusivo de planilla (`EXISTS` sobre `TRIM(M1P.IDENTIDAD) = TRIM(f.IDENTIDAD)` con resguardo de nombre normalizado ante ambigüedad).
  - `ObtenerMetadataMonitoreoAsync()`: calcula `TOTAL_REGISTROS` (grupos) y `TOTAL_COINCIDENCIAS_FUENTE` en la misma consulta agregada cacheada.

### 2.2 Frontend y Reportes PDF/Excel (`frontend/rl-app/src/app/features/admin/listas/`)
- **`models/listas.models.ts`:** extensión no disruptiva de `MonitoreoTotales`, `DetalleCoincidenciaNatural` y `DetalleCoincidenciaEmpleado`.
- **`monitoreo-listas.component.html` y `monitoreo-listas.component.impl.ts`:**
  - Tarjeta KPI activa mantiene las 5 tarjetas institucionales y muestra el conteo de grupos acompañado del subtítulo diferenciado `Grupos por DNI (N coincidencias de origen)` cuando aplica.
  - Tablas de Personas Naturales y Empleados muestran badge institucional dinámico (`bg-ihss-50 text-ihss-800`) con `N coincidencias` cuando `totalRepetidos > 1`.
  - Modal «Ver detalle» muestra todas las coincidencias individuales del grupo con trazabilidad `#idRegistroFuente` y variante de nombre fuente cuando difiere del nombre de referencia (preservando la regla de propietario visual único).
  - Paridad PDF/Excel general: `exportarListaPrincipal()` consulta `obtenerDatosMonitoreoParaExportar()` (universo filtrado completo, no solo los 10 registros de la página actual) e incluye columnas `Coincidencias` e `Identidad / Asociación` en ambos formatos.

---

## 3. Matriz de Pruebas M1-01 a M1-25 y Evidencia Ejecutada

| Alcance | Escenarios cubiertos | Evidencia local ejecutada (2026-11-18) | Estado en CI (Oracle 11g XE + E2E) |
|---|---|---|---|
| Backend unitario + guardas estructurales (`MonitoreoFuenteM1Tests`, `DbPaginationRegressionGuardTests`, `IdentidadMonitoreoPolicyTests`, `ListasApplicationTests`, `ListasControllerTests`) | M1-01..M1-21, M1-25 | **PASS** (`1,182` pruebas no-Oracle aprobadas, `0` fallos funcionales; `9` pruebas `MatricesRiesgos` `LOCAL_XE` omitidas/bloqueadas por perfil `PRODUCTION` local) | Pendiente verificación del SHA publicado |
| Oracle 11g XE descartable (`MonitoreoListasM1OracleIntegrationTests`) | M1-01..M1-16, M1-19, M1-22 | Omitida localmente (`RL_P0F_ORACLE_INTEGRATION_REQUIRED` no activo; `0` acceso a Oracle institucional) | Se ejecuta automáticamente en CI `Quality Gates` contra `gvenzl/oracle-xe:11.2.0.2-faststart` |
| Frontend Angular (`Vitest` + `ESLint` + `ng build`) | M1-02, M1-05, M1-06, M1-07, M1-13, M1-16, M1-17, M1-18, M1-20, M1-23, M1-24, M1-25 | **PASS** (`96/96` archivos, `1,032/1,032` pruebas; `ng build` OK; `ESLint` OK) | Se ejecuta en CI `Quality Gates` |
| Playwright E2E (`monitoreo-listas-m1-agrupacion.spec.ts` + `monitoreo-listas-motivo-layout.spec.ts`) | Agrupación DNI, detalle múltiple con IDs fuente, cambio a Empleados, layout modal | **PASS** (verificado localmente tras ajuste estricto de locators) | Suite E2E completa se ejecuta en CI `Quality Gates` |
| Validadores institucionales (BD, encoding, FE-01, FE-03/FE-04, modales, tema dinámico, contraste iconos, diálogos nativos, confirmaciones, B04, enlaces) | Integridad transversal | **PASS** (`EXIT=0`) | Se ejecuta en CI `Quality Gates` |
| Seguridad (`npm audit --omit=dev` + `npm audit`) | `SECURITY_AUDIT=PASS_FULL_TREE` | **PASS** (`0` vulnerabilidades en prod y árbol completo) | Se verifica con bloqueo en CI `Quality Gates` |

---

## 4. Preservación de Rendimiento (Fase G)

- **Línea base histórica registrada (`artifacts/performance/MONITOREO_API_PERFORMANCE_CERTIFICATION.md`):**
  - Jurídicas: primera `2,800 ms`, repetida cacheada `15 ms` (`4` registros).
  - Naturales: primera `1,903 ms`, repetida cacheada `14 ms` (`242` registros previos).
  - Empleados: primera `694 ms`, repetida cacheada `13 ms` (`7` registros previos).
- **Cambios en costo SQL (`3ce30913` → corrección P0):**
  - Naturales reduce de **2 escaneos** (`REPORTE_COINCIDENCIAS` + `REPORTE_DOC_AGG`) a **1 único escaneo** agrupado (`REPORTE_GRUPOS`) sin `COUNT(*) OVER`, sin `V_SOCIOS_REPRESENTANTES` y con paginación Oracle `ROWNUM` + caché cálida intacta.
  - Empleados elimina `COUNT(*) OVER` y agrega `V_EMPLEADOS_IHSS_PLANILLAS` en un único `IDENTIDAD_AGG` (1 fila por DNI) antes de agrupar `REPORTE_COINCIDENCIAS`, sin JOIN multiplicador por planillas.
  - El detalle continúa cargándose de forma diferida solo al pulsar «Ver detalle» (sin consultas N+1).
- **Medición Oracle institucional (producción/desarrollo real):** `PENDING_AUTHORIZED_ORACLE_MEASUREMENT` (no se consultó Oracle institucional sin autorización). Las métricas sintéticas reales en Oracle 11g XE quedan emitidas en la corrida de GitHub Actions (`M1_ORACLE_XE_NATURALES_MS`, `M1_ORACLE_XE_EMPLEADOS_MS`, `M1_ORACLE_XE_DETALLE_MS`).

---

## 5. Matriz de Conciliación de Cifras (Fase J)

```text
NATURALES_FILAS_FUENTE=PENDING_AUTHORIZED_ORACLE_Q01_Q12 (observado por propietario en UI 3ce30913 sin agrupar: 5742 filas en categoria identificadas)
NATURALES_GRUPOS_DNI=PENDING_AUTHORIZED_ORACLE_Q01_Q12 (pendiente ejecucion autorizada solo lectura Q01-Q12 sobre DNP_IHSS)
NATURALES_SIN_IDENTIFICACION=PENDING_AUTHORIZED_ORACLE_Q01_Q12
NATURALES_MANUALES=PENDING_AUTHORIZED_ORACLE_Q01_Q12
NATURALES_RELACIONES_EMPRESA=PENDING_AUTHORIZED_ORACLE_Q01_Q12

EMPLEADOS_INVENTARIO=PENDING_AUTHORIZED_ORACLE_Q01_Q12 (universo completo V_EMPLEADOS_IHSS_PLANILLAS; separado de Monitoreo de coincidencias)
EMPLEADOS_COINCIDENCIAS=PENDING_AUTHORIZED_ORACLE_Q01_Q12
EMPLEADOS_HISTORICOS=PENDING_AUTHORIZED_ORACLE_Q01_Q12
EMPLEADOS_GRUPOS=PENDING_AUTHORIZED_ORACLE_Q01_Q12
EMPLEADOS_SIN_COINCIDENCIA=PENDING_AUTHORIZED_ORACLE_Q01_Q12

COINCIDENCIAS_HISTORICAS_REFERENCIA=242
COINCIDENCIAS_VISIBLES_ANTERIORES=190
DIFERENCIA_HISTORICA_REFERENCIA=52

CONCILIACION_242_190_52=PENDING_AUTHORIZED_ORACLE_Q01_Q12 (hipotesis estructural probada en codigo: 242 provenia de GROUP BY DNI, LISTA_CONCIDENCIA incluyendo marcadores/listas multiples y 190 de identificadas; pendiente confirmacion numerica con SELECT Q01-Q12 autorizado)
JUSTIFICACION_5742=VERIFIED_IN_CODE_AND_SCHEMA (3ce30913 elimino GROUP BY en ConstruirConsultaMonitoreoNaturales y mostro 1 fila por cada REPORTE_COINCIDENCIA_ID acumulado por el ETL diario P_BUSCAR_COINCIDENCIAS; pendiente desglose exacto institucional con Q01-Q12)
CONFIRMACION_7_EMPLEADOS=PENDING_AUTHORIZED_ORACLE_Q01_Q12 (causa raiz de EMPLEADOS=0 probada en codigo: 3ce30913 sustituyo el cruce con V_EMPLEADOS_IHSS_PLANILLAS por V.TIPO_PERSONA LIKE '%IHSS'; pendiente conteo exacto institucional con Q01-Q12 y verificacion visual del propietario)
```

---

## 6. Procedimiento Autorizado de Conciliación Institucional (Pendiente Operador/DBA)

Utilizar exclusivamente en modo **solo lectura** (sin DDL/DML ni PII en logs) el guion versionado [`docs/qa/M1_DIAGNOSTICO_SOLO_LECTURA_MONITOREO_20261117.sql`](../qa/M1_DIAGNOSTICO_SOLO_LECTURA_MONITOREO_20261117.sql) (consultas Q01–Q12), verificando además la distribución de `TIPO_PERSONA` en `DNP_IHSS.REPORTE_COINCIDENCIAS WHERE TIPO_CALIFICACION_ID = 1` para cerrar el registro en [Issue #41](https://github.com/jmejia31/RIESGO_LAVADO/issues/41).
