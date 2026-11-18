# Expediente Técnico P0 — Microfase 1: Corrección Definitiva de Filtros y Tarjetas del Monitoreo de Listas

- **Fecha:** 2026-11-18 (-06:00)
- **Autor:** `ANTIG` (Antigravity)
- **Proyecto:** `RIESGO_LAVADO` — SGRLA-FT IHSS
- **Repositorio:** `jmejia31/RIESGO_LAVADO`
- **Rama autorizada:** `desarrollo`
- **Base SHA de referencia:** `f1ec7c6241c3e037d320851f54251704bd1ab22d`
- **Source Code SHA:** `109b40c1d2cb79529183285060a908bbd6cb2677`
- **Quality Gate certificado:** [44387854878](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/44387854878) (`CONCLUSION=success`, `STATUS=completed`)
- **Issue institucional:** [Issue #41](https://github.com/jmejia31/RIESGO_LAVADO/issues/41) (permanece **ABIERTO**)
- **Aprobación funcional final:** Exclusiva del propietario (`jmejia31`)
- **Escrituras en Oracle institucional / `DNP_IHSS` / `main` / producción:** `0` (`NO_TOUCHED`)
- **Tratamiento de PII:** `0` registros con datos personales publicados o registrados (`NO_PII`)

---

## 1. Objetivo y Alcance

Corregir definitivamente la falta de correspondencia funcional y matemática entre las 5 tarjetas institucionales de estado del módulo **Monitoreo de Listas** y las opciones del menú desplegable de filtros, garantizando simetría total, estabilidad de contadores y preservación de todas las optimizaciones de agrupamiento, rendimiento y seguridad de Microfase 1.

---

## 2. Diagnóstico y Causa Raíz

1. **Discrepancia en Opciones de Filtro:**
   - Las 5 tarjetas KPI mostraban:
     1. Coincidencias activas
     2. Pendientes de motivo
     3. Con motivo registrado
     4. Positivos manuales
     5. Cerrados / Pasivos
   - El desplegable únicamente exponía 4 opciones desalineadas: `Todos los registros`, `Pendiente`, `Con motivo`, `Cerrado / Pasivo`. Faltaban las opciones `Coincidencias activas` y `Positivos manuales`, con etiquetas no coincidentes.
2. **Colapso de Tarjetas al Filtrar por Estado:**
   - La consulta de metadatos `ObtenerMetadataMonitoreoAsync` se ejecutaba sobre `filteredSql`, el cual ya incluía `AND ESTADO_MONITOREO = :estado`. Al seleccionar un filtro como `con_motivo`, el conteo de pendientes y manuales colapsaba a 0 en las tarjetas.
3. **Acoplamiento de Estado y Origen:**
   - En el SQL previo, `CASE WHEN q.ES_MANUAL = 1 THEN 'manual' ...` forzaba a un positivo manual a una categoría mutuamente excluyente, de modo que si un positivo manual recibía un motivo de ingreso, dejaba de contarse o filtrarse con motivo o viceversa.

---

## 3. Solución Técnica Implementada

### 3.1 Backend ASP.NET Core (`RL.API`)
- **Contrato DTO (`ConsultasPaginadasDtos.cs`):**
  - Se agregó `public int CoincidenciasActivas { get; init; }` a `MonitoreoTotalesDto`.
- **Desacoplamiento Contextual (`ListasRepository.cs`):**
  - Implementación de `ConstruirConsultaMonitoreoContextual(baseSql, consulta)`: Preserva los filtros generales (búsqueda de texto, rango de fechas, categoría de identidad, períodos de planilla), pero fija `Estado = "todos"`.
  - `ObtenerMetadataMonitoreoAsync` evalúa `contextualSql`, calculando simultáneamente los 6 agregados:
    - `TOTAL_REGISTROS`: total del universo contextual.
    - `COINCIDENCIAS_ACTIVAS`: `NVL(SUM(CASE WHEN NVL(ES_CERRADO_PASIVO, 0) = 0 THEN 1 ELSE 0 END), 0)`.
    - `PENDIENTES`: `NVL(SUM(CASE WHEN TIENE_MOTIVO = 0 AND NVL(ES_CERRADO_PASIVO, 0) = 0 THEN 1 ELSE 0 END), 0)`.
    - `CON_MOTIVO`: `NVL(SUM(CASE WHEN TIENE_MOTIVO = 1 AND NVL(ES_CERRADO_PASIVO, 0) = 0 THEN 1 ELSE 0 END), 0)`.
    - `MANUALES`: `NVL(SUM(CASE WHEN ES_MANUAL = 1 AND NVL(ES_CERRADO_PASIVO, 0) = 0 THEN 1 ELSE 0 END), 0)`.
    - `CERRADOS_PASIVOS`: `NVL(SUM(CASE WHEN NVL(ES_CERRADO_PASIVO, 0) = 1 THEN 1 ELSE 0 END), 0)`.
- **Simetría y Correspondencia Matemática:**
  - `filteredTotal` se resuelve directamente de `metadata` según el estado seleccionado (`activos`, `pendiente`, `con_motivo`, `manual`, `cerrado_pasivo` o `todos`).
  - `TOTAL_TARJETA_ESTADO == TOTAL_REGISTROS_FILTRADOS_ESTADO` con 100% de precisión y 0 consultas extra `COUNT(*)`.
  - Retorno inmediato con lista vacía si `filteredTotal == 0` (evitando llamadas innecesarias a Oracle para estados pasivos sin registros).
- **Filtros Específicos e Independencia de Dimensiones (`AppendMonitoringFilters`):**
  - `case "activos": sql += " AND NVL(f.ES_CERRADO_PASIVO, 0) = 0";`
  - `case "pendiente": sql += " AND f.TIENE_MOTIVO = 0 AND NVL(f.ES_CERRADO_PASIVO, 0) = 0";`
  - `case "con_motivo": sql += " AND f.TIENE_MOTIVO = 1 AND NVL(f.ES_CERRADO_PASIVO, 0) = 0";`
  - `case "manual": sql += " AND f.ES_MANUAL = 1 AND NVL(f.ES_CERRADO_PASIVO, 0) = 0";`
  - `case "cerrado_pasivo": sql += " AND NVL(f.ES_CERRADO_PASIVO, 0) = 1";`
  - `default: sql += " AND ESTADO_MONITOREO = :estado";` (preserva compatibilidad con pruebas de guardas `DbPaginationRegressionGuardTests`).
- **Optimización de Caché:**
  - `CrearClaveMetadataMonitoreo` excluye `consulta.Estado`, permitiendo que el cambio de opciones de filtro en la interfaz consulte instantáneamente la metadata en caché sin costo en base de datos.
  - `CrearClavePaginaMonitoreo` incluye `consulta.Estado`, garantizando páginas aisladas por estado.

### 3.2 Frontend Angular (`rl-app`)
- **Modelos (`listas.models.ts`):**
  - `MonitoreoTotales` incluye `coincidenciasActivas?: number;`.
  - `FiltroEstado` ampliado a `'todos' | 'activos' | 'pendiente' | 'con_motivo' | 'manual' | 'cerrado_pasivo'`.
- **Componente e Implementación (`monitoreo-listas.component.impl.ts`):**
  - Señales `juridicasTotalRegistros`, `naturalesTotalRegistros`, `empleadosTotalRegistros`.
  - Señal computada `totalRegistrosTabla` para sincronizar el paginador con la cantidad exacta de registros filtrados.
  - `coincidenciasActivasActual` y `coincidenciasActivasGeneral` alimentan la tarjeta 1 de forma reactiva.
  - Métodos `coincideEstado()` y `obtenerEtiquetaEstadoFiltro()` actualizados con los 6 estados.
- **Plantilla HTML (`monitoreo-listas.component.html`):**
  - Tarjeta 1 muestra `{{ coincidenciasActivasGeneral() }}`.
  - Desplegable de filtros con las 6 opciones exactas y sus etiquetas canónicas.
  - Paginador `<app-data-pagination>` enlazado a `[totalRecords]="totalRegistrosTabla()"`.

---

## 4. Matriz de Correspondencia Funcional

| Opción Desplegable | Valor `Estado` | Predicado SQL Aplicado | Tarjeta Correspondiente | Valor Matemático |
| :--- | :--- | :--- | :--- | :--- |
| **Todos los registros** | `todos` | *(Sin filtro de estado)* | (Contexto general) | `TOTAL_REGISTROS` |
| **Coincidencias activas** | `activos` | `NVL(f.ES_CERRADO_PASIVO, 0) = 0` | Coincidencias activas | `COINCIDENCIAS_ACTIVAS` |
| **Pendientes de motivo** | `pendiente` | `f.TIENE_MOTIVO = 0 AND NVL(f.ES_CERRADO_PASIVO, 0) = 0` | Pendientes de motivo | `PENDIENTES` |
| **Con motivo registrado**| `con_motivo` | `f.TIENE_MOTIVO = 1 AND NVL(f.ES_CERRADO_PASIVO, 0) = 0` | Con motivo registrado | `CON_MOTIVO` |
| **Positivos manuales** | `manual` | `f.ES_MANUAL = 1 AND NVL(f.ES_CERRADO_PASIVO, 0) = 0` | Positivos manuales | `MANUALES` |
| **Cerrados / Pasivos** | `cerrado_pasivo` | `NVL(f.ES_CERRADO_PASIVO, 0) = 1` | Cerrados / Pasivos | `CERRADOS_PASIVOS` |

---

## 5. Verificación y Calidad

- **Pruebas Unitarias Backend (.NET):** 1,191 superadas, 0 errores, 1 omitida (requiere Oracle XE).
- **Pruebas Unitarias Frontend (Vitest):** 1,033 superadas en 96 suites, 0 errores.
- **Compilación de Producción Angular (`ng build`):** EXIT 0, bundles optimizados sin errores.
- **Linter Frontend (`npx eslint`):** EXIT 0, código limpio.
- **Estructura del Repositorio (`validate_repository_structure.ps1`):** EXIT 0 (118 rutas, 1,286 archivos rastreados).
- **Scripts de Base de Datos (`validate_database_scripts.ps1`):** EXIT 0 (281 catálogos, sin brechas Unicode ni DDL destructivos).
- **Enlaces de Documentación (`validate_documentation_links.ps1`):** EXIT 0 (250 documentos, 261 enlaces relativos).
- **Agent Skills (`validate_agent_skills.py`):** EXIT 0 (14 skills válidas).
