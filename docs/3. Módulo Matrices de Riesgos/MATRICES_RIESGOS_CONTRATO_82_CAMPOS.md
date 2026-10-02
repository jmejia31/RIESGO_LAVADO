# Contrato Canónico Institucional de los 82 Campos de la Matriz de Riesgos

> **MÓDULO:** Matrices de Riesgos (SGRLAFT IHSS)
> **FUENTE INSTITUCIONAL PRIMARIA:** `Matrices de Riesgos.xlsx`, Hoja `Matriz Consolidada`, Fila 1 (Rango A1:CD1)
> **SHA256 DEL WORKBOOK OFICIAL:** `5c3fc00864947afe1e34d3d6ffdfc6da008eaa3c8f1c6c764161014d5ef9a385`
> **AUTORIDAD:** Máxima fuente de verdad institucional para importación, conciliación, UI, cálculos, DTOs y exportaciones.
> **TOTAL DE CAMPOS:** Exactamente 82 campos inmutables en secuencia ordinal 01 a 82.

---

## 1. Distribución Oficial por Bloques Funcionales

| Bloque | Denominación Oficial | Rango Ordinal | Total Campos |
|:---:|:---|:---:|:---:|
| **1** | Identificación y Riesgo Inherente | 01 – 19 | 19 |
| **2** | Controles | 20 – 33 | 14 |
| **3** | Riesgo Residual y Respuesta | 34 – 39 | 6 |
| **4** | Plan de Mitigación / Acciones Correctivas | 40 – 49 | 10 |
| **5** | Cálculos Auxiliares y Verificaciones | 50 – 69 | 20 |
| **6** | Monitoreo, Efectividad y Observaciones | 70 – 82 | 13 |
| **TOTAL** | **Pila Completa Institucional** | **01 – 82** | **82** |

---

## 2. Decisiones Funcionales y Semántica de Estados Congeladas

### 2.1 Aliases Prohibidos y Claves Canónicas
- **Campo 08**: Etiqueta visible institucional única: **`Riesgo Inherente`**. El alias visible *"Nombre"* queda estrictamente prohibido en la interfaz institucional y exportaciones.
- **Campo 09**: Etiqueta visible institucional única: **`Evaluación`**. El alias visible *"Descripción"* queda estrictamente prohibido como nombre de este campo.
- **Deuda Técnica Documentada**: En backend/Oracle, `RL_MR_RIESGOS.RIE_NOMBRE` y `RL_MR_RIESGOS.RIE_DESCRIPCION` conservan provisionalmente sus identificadores físicos de columna, pero su proyección en DTOs, frontend y reportes debe ser respectivamente *Riesgo Inherente* y *Evaluación*.

### 2.2 Regla Determinista del Campo 01 ("No.") — FAIL_CLOSED
- **ordinalSource**: `INSTITUTIONAL_CODE_TO_NO_MAP` (Excel oficial `Matrices de Riesgos.xlsx`, hoja `Matriz Consolidada`, columna 01 `No.`, mapeada 1:1 biunívocamente al `Código de Riesgo` columna 02).
- **ordinalPersistence**: `VIRTUAL_DERIVED`. La tabla `RL_MR_RIESGOS` en schema HEAD no posee columna física de orden; contiene estrictamente `RIE_ID, RIE_CODIGO, RIE_NOMBRE, RIE_DESCRIPCION, RIE_ACTIVO, RIE_USR_CREACION, RIE_FECHA_CREACION`.
- **ordinalProjection**: `MAPEO_EXACTO_CODIGO_A_NO_1_59`. En las 59 filas de datos del Excel institucional, el código de riesgo (`ROTR-AFIL-1` a `ROP-CUMP-59`) coincide estrictamente con la secuencia ordinal 1..59.
- **ordinalFallback**: `FAIL_CLOSED`. Si aparece un código sin correspondencia en el mapa institucional inmutable, el sistema falla cerrado: estado `ORDINAL_UNRESOLVED`, error `CONTRACT_VALIDATION_ERROR`, sin fabricar posiciones ni inventar ordenamientos alfabéticos automáticos.

### 2.3 Regla Canónica de Mitigación en Bloque 4 (Campos 40-49)
- **catalogSource**: `RL_MR_CATALOGOS` con `CAT_CODIGO = 'MR_RESPUESTA_RIESGO'` (paridad con hoja `Listas` columna 4 del Excel y `RL_MR_PROYECCIONES_EVALUACION.PROY_RESPUESTA_RIESGO`).
- **catalogKey**: `MITIGAR`.
- **catalogDisplayValue**: `Mitigar`.
- **mitigationApplicabilityRule**:
  - **(A) Mitigación requerida por Respuesta al Riesgo**: `PROY_RESPUESTA_RIESGO == 'MITIGAR'` (`catalogKey = 'MITIGAR'` de `MR_RESPUESTA_RIESGO`).
  - **(B) Mitigación requerida por política institucional de tolerancia residual**: `PROY_NIVEL_RESIDUAL IN ('ALTO', 'CRITICO')` y `PROY_RESPUESTA_RIESGO != 'ACEPTAR'`.
  - **Resultado**: Si (A) o (B) aplica, la mitigación es exigible.
    - Si mitigación APLICA y existen datos persistidos: se proyectan planes y actividades (`RL_MR_PLANES`, `RL_MR_ACTIVIDADES`).
    - Si mitigación APLICA pero no existen planes registrados: `EMPTY_VALUE` (en UI: `"Sin dato"`, workflow: estado `VALIDATION_PENDING` impidiendo aprobación hasta completar el plan).
    - Si mitigación NO APLICA (respuesta institucional distinta de mitigar y dentro de tolerancia): `NOT_APPLICABLE` (en UI: `"No aplica (Respuesta no requiere mitigación)"`, exportación: `"No aplica"` o celda vacía según plantilla).

### 2.4 Campo 45 ("Responsables") — Regla Canónica Sin Ambigüedad
- **deduplicationRule**: `NO_DEDUPLICATION_PER_PLAN`. Se preserva la asignación específica de responsables por cada acción de mitigación; no se unifican ni descartan responsables entre distintos planes.
- **deduplicationKey**: `PLAN_ORDINAL_INDEX`. La tupla de orden es `(PLA_ORDEN, PLA_ID)`.
- **orderingRule**: `ORDER BY PLA_ORDEN ASC, PLA_ID ASC`.
- **displaySeparator**: `\n` (salto de línea).
- **excelSerialization**: Texto multilínea enumerado secuencialmente (`'{i}. {PLA_RESPONSABLES}'`) delimitado por saltos de línea (`\r\n`) dentro de la celda con `wrapText` activado. Si solo existe un plan, se emite el texto plano sin prefijo numérico.
- **pdfSerialization**: Lista estructurada enumerada secuencialmente (`'{i}. {PLA_RESPONSABLES}'`) bajo la columna de responsables del bloque de mitigación.

### 2.5 Campos 72-80 — Semántica de Monitoreo sobre Controles 1:N (Sin MIN(CON_ID))
Para los tres grupos (Preventivo 72-74, Detectivo 75-77, Correctivo 78-80), en coherencia y paridad con los campos 20, 24 y 28:
- **monitoringControlCardinality**: `ONE_TO_MANY_PER_CONTROL_TYPE`. Una evaluación puede contener 0, 1 o N controles por tipo (`PREVENTIVO`, `DETECTIVO`, `CORRECTIVO`).
- **controlSelectionSemantics**: `ALL_CONTROLS_OF_TYPE`. Representa la totalidad de controles del tipo respectivo ordenados canónicamente por `CON_ID ASC`. Se rechaza cualquier filtro arbitrario técnico tipo `MIN(CON_ID)`. En Angular UI (`matrices-riesgos.component.html`), se itera sobre todos los controles del grupo mediante `@for (control of grupo)`.
- **stateProjectionSemantics** (72, 75, 78): Si 0 controles -> `EMPTY_VALUE` (`'—'`). Si 1 control -> texto plano de `CON_ESTADO_MONITOREO`. Si N > 1 controles -> texto multilínea enumerado secuencialmente `'{i}. {CON_ESTADO_MONITOREO}'` en orden 1..N. En UI -> visualización individual por tarjeta de control.
- **effectivenessProjectionSemantics** (73, 76, 79): Si 0 controles -> `EMPTY_VALUE` (`'—'`). Si 1 control -> porcentaje decimal `CON_EFECTIVIDAD_MONITOREO`. Si N > 1 controles -> texto multilínea enumerado `'{i}. {CON_EFECTIVIDAD_MONITOREO}%'`. En UI -> renderizado individual por control.
- **evidenceProjectionSemantics** (74, 77, 80): `NOMBRE_ARCHIVO_CON_EXTENSION_PER_CONTROL`. Lista de nombres de archivo físicos (`EVI_NOMBRE_ARCHIVO.EVI_EXTENSION`) vinculados al control vía `RL_MR_EVIDENCIAS_VINCULOS`. Si N > 1 controles, agrupados por control enumerado `'{i}. {archivos}'`. En UI -> lista `<ul><li>` de evidencias por tarjeta.
- **importTargetSemantics**: `MAP_BY_ORDINAL_OR_REJECT_AMBIGUOUS`. En línea base de 59 riesgos, celdas de Excel son 100% vacías (0/59). En importaciones posteriores:
  - Si 0 controles en BD: `SKIP_NO_CONTROL_OF_TYPE`.
  - Si 1 control en BD: `ASSIGN_TO_UNIQUE_CONTROL`.
  - Si N > 1 controles y celda contiene enumeración multilínea 1..N: `MAP_BY_ORDINAL_INDEX`.
  - Si N > 1 controles y celda es texto plano único no desglosable: `REJECT_AMBIGUOUS_MULTI_CONTROL_IMPORT` (fail-closed, sin asignación arbitraria al primer control).
  - En runtime operativo: captura directa por control vía UI/API, nunca aplanada en una celda única ambigua.

### 2.6 Campo 71 y Evidencias 74/77/80
- **Campo 71 ("Estado del Riesgo")**:
  - Se elimina cualquier default arbitrario. La columna `RL_MR_AUTOMONITOREO.MON_ESTADO_RIESGO` no posee `DEFAULT` en base de datos.
  - Si no existe registro de monitoreo o la celda viene vacía, el estado semántico es `OPERATIONAL_PENDING` (*"Pendiente de monitoreo"*).
- **Campos 74, 77, 80 ("Evidencia(s)")**:
  - **evidenceProjection**: `NOMBRE_ARCHIVO_CON_EXTENSION` (nombre oficial del archivo cargado, ej. `politica_laft_2026.pdf`).
  - **excelRepresentation**: Lista enumerada de nombres de archivo (`'1. archivo1.pdf\n2. archivo2.xlsx'`) con saltos de línea (`\r\n`) y `wrapText` en celda de Excel.
  - **pdfRepresentation**: Lista estructurada de nombres de archivo con hipervínculo institucional en anexo documental.

---

## 3. Matriz Técnica Canónica 82/82

| # | Col | Etiqueta Exacta Excel | Clave Canónica | Tipo Dato | Origen | Modo | Obligatoriedad | Condicionalidad | Tabla / Columna / Entidad | Cardinalidad | Regla Proyección 1:N | Regla Importación | Bloque |
|:---:|:---:|:---|:---|:---|:---:|:---:|:---:|:---|:---|:---:|:---|:---|:---:|
| 01 | A | **No.** | `numero` | INTEGER | SYSTEM | SYSTEM | SYSTEM | `APPLIES_ALWAYS` | `RL_MR_RIESGOS.RIE_CODIGO -> mapeo ordinal determinista 1..59` | `1:1` | Directa | `SYSTEM_GENERATED` | B1 |
| 02 | B | **Código de Riesgo** | `codigoRiesgo` | TEXT | RISK_MASTER | READ_ONLY | REQUIRED | `APPLIES_ALWAYS` | `RL_MR_RIESGOS.RIE_CODIGO` | `1:1` | Directa | `IMPORT_DIRECT` | B1 |
| 03 | C | **Área** | `area` | CATALOG | EVALUATION | INPUT | REQUIRED | `APPLIES_ALWAYS` | `RL_MR_EVALUACIONES.EVA_DATA_JSON -> area_principal` | `1:1` | Directa | `IMPORT_DIRECT` | B1 |
| 04 | D | **Área Consolidada** | `areaConsolidada` | CATALOG | EVALUATION | INPUT | OPTIONAL | `APPLIES_WHEN_DECLARED_IN_VERSION` | `RL_MR_EVALUACIONES.EVA_DATA_JSON -> area_consolidada` | `1:1` | Directa | `IMPORT_DIRECT` | B1 |
| 05 | E | **Tipo de Riesgo** | `tipoRiesgo` | CATALOG | EVALUATION | INPUT | REQUIRED | `APPLIES_ALWAYS` | `RL_MR_EVALUACIONES.EVA_DATA_JSON -> tipo_riesgo` | `1:1` | Directa | `IMPORT_DIRECT` | B1 |
| 06 | F | **Procedimiento** | `procedimiento` | TEXT | EVALUATION | INPUT | OPTIONAL | `APPLIES_WHEN_DECLARED_IN_VERSION` | `RL_MR_EVALUACIONES.EVA_DATA_JSON -> procedimiento` | `1:1` | Directa | `IMPORT_DIRECT` | B1 |
| 07 | G | **Objetivo(s) Estratégico(s)** | `objetivosEstrategicos` | LONG_TEXT | EVALUATION | INPUT | OPTIONAL | `APPLIES_WHEN_DECLARED_IN_VERSION` | `RL_MR_EVALUACIONES.EVA_DATA_JSON -> objetivos_estrategicos` | `1:1` | Directa | `IMPORT_DIRECT` | B1 |
| 08 | H | **Riesgo Inherente** | `riesgoInherente` | TEXT | RISK_MASTER | READ_ONLY | REQUIRED | `APPLIES_ALWAYS` | `RL_MR_RIESGOS.RIE_NOMBRE` | `1:1` | Directa | `IMPORT_DIRECT` | B1 |
| 09 | I | **Evaluación** | `evaluacionNarrativa` | LONG_TEXT | RISK_MASTER | READ_ONLY | OPTIONAL | `APPLIES_ALWAYS` | `RL_MR_RIESGOS.RIE_DESCRIPCION` | `1:1` | Directa | `IMPORT_DIRECT` | B1 |
| 10 | J | **Frecuencia** | `frecuenciaInherente` | CATALOG | EVALUATION | INPUT | REQUIRED | `APPLIES_ALWAYS` | `RL_MR_EVALUACIONES.EVA_DATA_JSON -> frecuencia_inherente` | `1:1` | Directa | `IMPORT_DIRECT` | B1 |
| 11 | K | **Impacto** | `impactoInherente` | CATALOG | EVALUATION | INPUT | REQUIRED | `APPLIES_ALWAYS` | `RL_MR_EVALUACIONES.EVA_DATA_JSON -> impacto_inherente` | `1:1` | Directa | `IMPORT_DIRECT` | B1 |
| 12 | L | **Valor del Riesgo Inherente** | `valorRiesgoInherente` | INTEGER | EXCEL_FORMULA | CALCULATED | CALCULATED | `APPLIES_ALWAYS` | `RL_MR_EVALUACIONES.EVA_VRI / EVA_DATA_CALC_JSON -> valor_riesgo_inherente` | `1:1` | Directa | `DO_NOT_IMPORT_CALCULATE` | B1 |
| 13 | M | **Nivel de Riesgo Inherente** | `nivelRiesgoInherente` | CATALOG | EXCEL_FORMULA | CALCULATED | CALCULATED | `APPLIES_ALWAYS` | `RL_MR_EVALUACIONES.EVA_DATA_CALC_JSON -> nivel_riesgo_inherente` | `1:1` | Directa | `DO_NOT_IMPORT_CALCULATE` | B1 |
| 14 | N | **Responsable o dueño del riesgo** | `duenoRiesgo` | TEXT | EVALUATION | INPUT | REQUIRED | `APPLIES_ALWAYS` | `RL_MR_EVALUACIONES.EVA_DATA_JSON -> dueno_riesgo` | `1:1` | Directa | `IMPORT_DIRECT` | B1 |
| 15 | O | **Régimen afectado** | `regimenAfectado` | CATALOG | EVALUATION | INPUT | OPTIONAL | `APPLIES_WHEN_DECLARED_IN_VERSION` | `RL_MR_EVALUACIONES.EVA_DATA_JSON -> regimen_afectado` | `1:1` | Directa | `IMPORT_DIRECT` | B1 |
| 16 | P | **Transversalidad o Interrelación con otros Riesgos** | `transversalidad` | LONG_TEXT | EVALUATION | INPUT | OPTIONAL | `APPLIES_WHEN_DECLARED_IN_VERSION` | `RL_MR_EVALUACIONES.EVA_DATA_JSON -> transversalidad` | `1:1` | Directa | `IMPORT_DIRECT` | B1 |
| 17 | Q | **Amenazas (Solo para riesgos de GTIC)** | `amenazasGtic` | LONG_TEXT | EVALUATION | CONDITIONAL | CONDITIONAL_REQUIRED | `ONLY_GTIC` | `RL_MR_EVALUACIONES.EVA_DATA_JSON -> amenazas_gtic` | `1:1` | Directa | `IMPORT_DIRECT` | B1 |
| 18 | R | **Vulnerabilidades (Solo para riesgos de GTIC)** | `vulnerabilidadesGtic` | LONG_TEXT | EVALUATION | CONDITIONAL | CONDITIONAL_REQUIRED | `ONLY_GTIC` | `RL_MR_EVALUACIONES.EVA_DATA_JSON -> vulnerabilidades_gtic` | `1:1` | Directa | `IMPORT_DIRECT` | B1 |
| 19 | S | **Activos de Información (Solo para riesgos de GTIC)** | `activosInformacionGtic` | LONG_TEXT | EVALUATION | CONDITIONAL | CONDITIONAL_REQUIRED | `ONLY_GTIC` | `RL_MR_EVALUACIONES.EVA_DATA_JSON -> activos_informacion_gtic` | `1:1` | Directa | `IMPORT_DIRECT` | B1 |
| 20 | T | **Descripción de Control(es) Preventivo(s)** | `descripcionControlPreventivo` | TEXT | CONTROLS | INPUT | OPTIONAL | `APPLIES_ALWAYS` | `RL_MR_CONTROLES_RIESGO.CON_DESCRIPCION (CON_TIPO=PREVENTIVO)` | `1:N` | Concatenación enumerada ('{i}. {CON_DESCRIPCI... | `IMPORT_DIRECT` | B2 |
| 21 | U | **Escala de efectividad de control(es) preventivo(s)** | `escalaControlPreventivo` | CATALOG | CONTROLS | INPUT | OPTIONAL | `ONLY_WHEN_PREVENTIVE_CONTROL_EXISTS` | `RL_MR_EVALUACIONES.EVA_DATA_JSON -> escala_preventivo` | `1:1` | Directa | `IMPORT_DIRECT` | B2 |
| 22 | V | **Nivel de efectividad de control(es) preventivo(s)** | `nivelEfectividadControlPreventivo` | CATALOG | EXCEL_FORMULA | CALCULATED | CALCULATED | `ONLY_WHEN_PREVENTIVE_CONTROL_EXISTS` | `RL_MR_EVALUACIONES.EVA_DATA_CALC_JSON -> nivel_control_preventivo` | `1:1` | Directa | `DO_NOT_IMPORT_CALCULATE` | B2 |
| 23 | W | **% efectividad de control(es) preventivo(s)** | `porcentajeEfectividadControlPreventivo` | PERCENTAGE | EXCEL_FORMULA | CALCULATED | CALCULATED | `ONLY_WHEN_PREVENTIVE_CONTROL_EXISTS` | `RL_MR_EVALUACIONES.EVA_DATA_CALC_JSON -> porcentaje_control_preventivo` | `1:1` | Directa | `DO_NOT_IMPORT_CALCULATE` | B2 |
| 24 | X | **Descripción de Control(es) Detectivo(s)** | `descripcionControlDetectivo` | TEXT | CONTROLS | INPUT | OPTIONAL | `APPLIES_ALWAYS` | `RL_MR_CONTROLES_RIESGO.CON_DESCRIPCION (CON_TIPO=DETECTIVO)` | `1:N` | Concatenación enumerada ('{i}. {CON_DESCRIPCI... | `IMPORT_DIRECT` | B2 |
| 25 | Y | **Escala de efectividad de control(es) detectivo(s)** | `escalaControlDetectivo` | CATALOG | CONTROLS | INPUT | OPTIONAL | `ONLY_WHEN_DETECTIVE_CONTROL_EXISTS` | `RL_MR_EVALUACIONES.EVA_DATA_JSON -> escala_detectivo` | `1:1` | Directa | `IMPORT_DIRECT` | B2 |
| 26 | Z | **Nivel de efectividad de control(es) detectivo(s)** | `nivelEfectividadControlDetectivo` | CATALOG | EXCEL_FORMULA | CALCULATED | CALCULATED | `ONLY_WHEN_DETECTIVE_CONTROL_EXISTS` | `RL_MR_EVALUACIONES.EVA_DATA_CALC_JSON -> nivel_control_detectivo` | `1:1` | Directa | `DO_NOT_IMPORT_CALCULATE` | B2 |
| 27 | AA | **% efectividad de control detectivo** | `porcentajeEfectividadControlDetectivo` | PERCENTAGE | EXCEL_FORMULA | CALCULATED | CALCULATED | `ONLY_WHEN_DETECTIVE_CONTROL_EXISTS` | `RL_MR_EVALUACIONES.EVA_DATA_CALC_JSON -> porcentaje_control_detectivo` | `1:1` | Directa | `DO_NOT_IMPORT_CALCULATE` | B2 |
| 28 | AB | **Descripción de Control(es) Correctivo(s)** | `descripcionControlCorrectivo` | TEXT | CONTROLS | INPUT | OPTIONAL | `APPLIES_ALWAYS` | `RL_MR_CONTROLES_RIESGO.CON_DESCRIPCION (CON_TIPO=CORRECTIVO)` | `1:N` | Concatenación enumerada ('{i}. {CON_DESCRIPCI... | `IMPORT_DIRECT` | B2 |
| 29 | AC | **Escala de efectividad de control(es) correctivo(s)** | `escalaControlCorrectivo` | CATALOG | CONTROLS | INPUT | OPTIONAL | `ONLY_WHEN_CORRECTIVE_CONTROL_EXISTS` | `RL_MR_EVALUACIONES.EVA_DATA_JSON -> escala_correctivo` | `1:1` | Directa | `IMPORT_DIRECT` | B2 |
| 30 | AD | **Nivel de efectividad de control(es) correctivo(s)** | `nivelEfectividadControlCorrectivo` | CATALOG | EXCEL_FORMULA | CALCULATED | CALCULATED | `ONLY_WHEN_CORRECTIVE_CONTROL_EXISTS` | `RL_MR_EVALUACIONES.EVA_DATA_CALC_JSON -> nivel_control_correctivo` | `1:1` | Directa | `DO_NOT_IMPORT_CALCULATE` | B2 |
| 31 | AE | **% efectividad de control correctivo** | `porcentajeEfectividadControlCorrectivo` | PERCENTAGE | EXCEL_FORMULA | CALCULATED | CALCULATED | `ONLY_WHEN_CORRECTIVE_CONTROL_EXISTS` | `RL_MR_EVALUACIONES.EVA_DATA_CALC_JSON -> porcentaje_control_correctivo` | `1:1` | Directa | `DO_NOT_IMPORT_CALCULATE` | B2 |
| 32 | AF | **Nivel de Automatización de los Controles** | `nivelAutomatizacionControles` | CATALOG | CONTROLS | OPERATIONAL | OPTIONAL | `ONLY_WHEN_CONTROLS_EXIST` | `RL_MR_CONTROLES_RIESGO.CON_AUTOMATIZACION` | `1:1` | Directa | `IMPORT_DIRECT` | B2 |
| 33 | AG | **Efectividad Total Ponderada de los Controles** | `efectividadTotalPonderada` | PERCENTAGE | EXCEL_FORMULA | CALCULATED | CALCULATED | `APPLIES_ALWAYS` | `RL_MR_EVALUACIONES.EVA_DATA_CALC_JSON -> efectividad_total_ponderada` | `1:1` | Directa | `DO_NOT_IMPORT_CALCULATE` | B2 |
| 34 | AH | **Riesgo Residual** | `riesgoResidualDescripcion` | TEXT | EXCEL_FORMULA | CALCULATED | CALCULATED | `APPLIES_ALWAYS` | `RL_MR_EVALUACIONES.EVA_DATA_CALC_JSON -> riesgo_residual_descripcion` | `1:1` | Directa | `DO_NOT_IMPORT_CALCULATE` | B3 |
| 35 | AI | **Frecuencia Residual** | `frecuenciaResidual` | INTEGER | EXCEL_FORMULA | CALCULATED | CALCULATED | `APPLIES_ALWAYS` | `RL_MR_EVALUACIONES.EVA_DATA_CALC_JSON -> frecuencia_residual` | `1:1` | Directa | `DO_NOT_IMPORT_CALCULATE` | B3 |
| 36 | AJ | **Impacto Residual** | `impactoResidual` | INTEGER | EXCEL_FORMULA | CALCULATED | CALCULATED | `APPLIES_ALWAYS` | `RL_MR_EVALUACIONES.EVA_DATA_CALC_JSON -> impacto_residual` | `1:1` | Directa | `DO_NOT_IMPORT_CALCULATE` | B3 |
| 37 | AK | **Valor del Riesgo Residual** | `valorRiesgoResidual` | INTEGER | EXCEL_FORMULA | CALCULATED | CALCULATED | `APPLIES_ALWAYS` | `RL_MR_EVALUACIONES.EVA_VRR` | `1:1` | Directa | `DO_NOT_IMPORT_CALCULATE` | B3 |
| 38 | AL | **Nivel del Riesgo Residual** | `nivelRiesgoResidual` | CATALOG | EXCEL_FORMULA | CALCULATED | CALCULATED | `APPLIES_ALWAYS` | `RL_MR_EVALUACIONES.EVA_DATA_CALC_JSON -> nivel_riesgo_residual` | `1:1` | Directa | `DO_NOT_IMPORT_CALCULATE` | B3 |
| 39 | AM | **Respuesta al riesgo** | `respuestaRiesgo` | CATALOG | EVALUATION | INPUT | REQUIRED | `APPLIES_ALWAYS` | `RL_MR_EVALUACIONES.EVA_DATA_JSON -> respuesta_riesgo` | `1:1` | Directa | `IMPORT_DIRECT` | B3 |
| 40 | AN | **Plan de Mitigación/Acciones Correctivas** | `planMitigacionDescripcion` | TEXT | MITIGATION_PLAN | OPERATIONAL | CONDITIONAL_REQUIRED | `ONLY_WHEN_MITIGATION_REQUIRED` | `RL_MR_PLANES.PLA_DESCRIPCION` | `1:N` | Concatenación enumerada ('{i}. {PLA_DESCRIPCI... | `IMPORT_WITH_MERGE_POLICY` | B4 |
| 41 | AO | **No. Acciones de Mitigación** | `cantidadAccionesMitigacion` | INTEGER | DERIVED_AGGREGATE | READ_ONLY | SYSTEM | `ONLY_WHEN_MITIGATION_REQUIRED` | `Agregado COUNT(RL_MR_PLANES) por evaluación` | `1:1` | Directa | `DO_NOT_IMPORT_CALCULATE` | B4 |
| 42 | AP | **Actividades** | `actividadesDescripcion` | TEXT | ACTIVITY | OPERATIONAL | OPTIONAL | `ONLY_WHEN_MITIGATION_REQUIRED` | `RL_MR_ACTIVIDADES.ACT_DESCRIPCION` | `1:N` | Concatenación estructurada o jerárquica ('{pl... | `IMPORT_WITH_MERGE_POLICY` | B4 |
| 43 | AQ | **Cantidad de Actividades** | `cantidadActividades` | INTEGER | DERIVED_AGGREGATE | READ_ONLY | SYSTEM | `ONLY_WHEN_MITIGATION_REQUIRED` | `Agregado COUNT(RL_MR_ACTIVIDADES) por plan` | `1:1` | Directa | `DO_NOT_IMPORT_CALCULATE` | B4 |
| 44 | AR | **Monitoreo/ Seguimiento** | `planMonitoreoSeguimiento` | LONG_TEXT | MITIGATION_PLAN | OPERATIONAL | OPTIONAL | `ONLY_WHEN_MITIGATION_REQUIRED` | `RL_MR_PLANES.PLA_MONITOREO_SEGUIMIENTO` | `1:1` | Directa | `IMPORT_DIRECT` | B4 |
| 45 | AS | **Responsables** | `planResponsables` | TEXT | MITIGATION_PLAN | OPERATIONAL | OPTIONAL | `ONLY_WHEN_MITIGATION_REQUIRED` | `RL_MR_PLANES.PLA_RESPONSABLES` | `1:N` | Concatenación secuencial de los responsables ... | `IMPORT_DIRECT` | B4 |
| 46 | AT | **Fecha inicio** | `planFechaInicio` | DATE | MITIGATION_PLAN | OPERATIONAL | OPTIONAL | `ONLY_WHEN_MITIGATION_REQUIRED` | `RL_MR_PLANES.PLA_FECHA_INICIO` | `1:1` | Directa | `IMPORT_DIRECT` | B4 |
| 47 | AU | **Fecha final** | `planFechaFinal` | DATE | MITIGATION_PLAN | OPERATIONAL | OPTIONAL | `ONLY_WHEN_MITIGATION_REQUIRED` | `RL_MR_PLANES.PLA_FECHA_FIN` | `1:1` | Directa | `IMPORT_DIRECT` | B4 |
| 48 | AV | **Recursos** | `planRecursos` | TEXT | MITIGATION_PLAN | OPERATIONAL | OPTIONAL | `ONLY_WHEN_MITIGATION_REQUIRED` | `RL_MR_PLANES.PLA_RECURSOS` | `1:1` | Directa | `IMPORT_DIRECT` | B4 |
| 49 | AW | **Presupuesto** | `planPresupuesto` | DECIMAL | MITIGATION_PLAN | OPERATIONAL | OPTIONAL | `ONLY_WHEN_MITIGATION_REQUIRED` | `RL_MR_PLANES.PLA_PRESUPUESTO` | `1:1` | Directa | `IMPORT_DIRECT` | B4 |
| 50 | AX | **Frecuencia Residual (AUX)** | `frecuenciaResidualAux` | DECIMAL | EXCEL_FORMULA | CALCULATED | CALCULATED | `APPLIES_ALWAYS` | `RL_MR_EVALUACIONES.EVA_DATA_CALC_JSON -> frecuencia_residual_aux` | `1:1` | Directa | `DO_NOT_IMPORT_CALCULATE` | B5 |
| 51 | AY | **Impacto Residual (AUX)** | `impactoResidualAux` | DECIMAL | EXCEL_FORMULA | CALCULATED | CALCULATED | `APPLIES_ALWAYS` | `RL_MR_EVALUACIONES.EVA_DATA_CALC_JSON -> impacto_residual_aux` | `1:1` | Directa | `DO_NOT_IMPORT_CALCULATE` | B5 |
| 52 | AZ | **Suma Residual redondeada (AUX)** | `sumaResidualRedondeadaAux` | INTEGER | EXCEL_FORMULA | CALCULATED | CALCULATED | `APPLIES_ALWAYS` | `RL_MR_EVALUACIONES.EVA_DATA_CALC_JSON -> suma_residual_redondeada_aux` | `1:1` | Directa | `DO_NOT_IMPORT_CALCULATE` | B5 |
| 53 | BA | **F_base (AUX)** | `fBaseAux` | INTEGER | EXCEL_FORMULA | CALCULATED | CALCULATED | `APPLIES_ALWAYS` | `RL_MR_EVALUACIONES.EVA_DATA_CALC_JSON -> f_base` | `1:1` | Directa | `DO_NOT_IMPORT_CALCULATE` | B5 |
| 54 | BB | **I_base (AUX)** | `iBaseAux` | INTEGER | EXCEL_FORMULA | CALCULATED | CALCULATED | `APPLIES_ALWAYS` | `RL_MR_EVALUACIONES.EVA_DATA_CALC_JSON -> i_base` | `1:1` | Directa | `DO_NOT_IMPORT_CALCULATE` | B5 |
| 55 | BC | **Tope F (AUX)** | `topeFAux` | INTEGER | EXCEL_FORMULA | CALCULATED | CALCULATED | `APPLIES_ALWAYS` | `RL_MR_EVALUACIONES.EVA_DATA_CALC_JSON -> tope_f` | `1:1` | Directa | `DO_NOT_IMPORT_CALCULATE` | B5 |
| 56 | BD | **Tope I (AUX)** | `topeIAux` | INTEGER | EXCEL_FORMULA | CALCULATED | CALCULATED | `APPLIES_ALWAYS` | `RL_MR_EVALUACIONES.EVA_DATA_CALC_JSON -> tope_i` | `1:1` | Directa | `DO_NOT_IMPORT_CALCULATE` | B5 |
| 57 | BE | **Capacidad F (AUX)** | `capacidadFAux` | INTEGER | EXCEL_FORMULA | CALCULATED | CALCULATED | `APPLIES_ALWAYS` | `RL_MR_EVALUACIONES.EVA_DATA_CALC_JSON -> capacidad_f_aux` | `1:1` | Directa | `DO_NOT_IMPORT_CALCULATE` | B5 |
| 58 | BF | **Capacidad I (AUX)** | `capacidadIAux` | INTEGER | EXCEL_FORMULA | CALCULATED | CALCULATED | `APPLIES_ALWAYS` | `RL_MR_EVALUACIONES.EVA_DATA_CALC_JSON -> capacidad_i_aux` | `1:1` | Directa | `DO_NOT_IMPORT_CALCULATE` | B5 |
| 59 | BG | **Resto (AUX)** | `restoAux` | INTEGER | EXCEL_FORMULA | CALCULATED | CALCULATED | `APPLIES_ALWAYS` | `RL_MR_EVALUACIONES.EVA_DATA_CALC_JSON -> resto_aux` | `1:1` | Directa | `DO_NOT_IMPORT_CALCULATE` | B5 |
| 60 | BH | **Prefiere I (AUX)** | `prefiereIAux` | BOOLEAN | EXCEL_FORMULA | CALCULATED | CALCULATED | `APPLIES_ALWAYS` | `RL_MR_EVALUACIONES.EVA_DATA_CALC_JSON -> prefiere_i_aux` | `1:1` | Directa | `DO_NOT_IMPORT_CALCULATE` | B5 |
| 61 | BI | **Inc_I (AUX)** | `incIAux` | INTEGER | EXCEL_FORMULA | CALCULATED | CALCULATED | `APPLIES_ALWAYS` | `RL_MR_EVALUACIONES.EVA_DATA_CALC_JSON -> incremento_i_aux` | `1:1` | Directa | `DO_NOT_IMPORT_CALCULATE` | B5 |
| 62 | BJ | **Inc_F (AUX)** | `incFAux` | INTEGER | EXCEL_FORMULA | CALCULATED | CALCULATED | `APPLIES_ALWAYS` | `RL_MR_EVALUACIONES.EVA_DATA_CALC_JSON -> incremento_f_aux` | `1:1` | Directa | `DO_NOT_IMPORT_CALCULATE` | B5 |
| 63 | BK | **Valor del Riesgo Residual (AUX)** | `valorRiesgoResidualAux` | INTEGER | EXCEL_FORMULA | CALCULATED | CALCULATED | `APPLIES_ALWAYS` | `RL_MR_EVALUACIONES.EVA_DATA_CALC_JSON -> valor_riesgo_residual_aux` | `1:1` | Directa | `DO_NOT_IMPORT_CALCULATE` | B5 |
| 64 | BL | **Verificación** | `verificacionResidual` | INTEGER | EXCEL_FORMULA | CALCULATED | CALCULATED | `APPLIES_ALWAYS` | `RL_MR_EVALUACIONES.EVA_DATA_CALC_JSON -> verificacion` | `1:1` | Directa | `DO_NOT_IMPORT_CALCULATE` | B5 |
| 65 | BM | **VRR 2** | `vrr2` | INTEGER | EXCEL_FORMULA | CALCULATED | CALCULATED | `APPLIES_ALWAYS` | `RL_MR_EVALUACIONES.EVA_DATA_CALC_JSON -> vrr_2` | `1:1` | Directa | `DO_NOT_IMPORT_CALCULATE` | B5 |
| 66 | BN | **Verificar VRR 2** | `verificarVrr2` | INTEGER | EXCEL_FORMULA | CALCULATED | CALCULATED | `APPLIES_ALWAYS` | `RL_MR_EVALUACIONES.EVA_DATA_CALC_JSON -> verificar_vrr_2` | `1:1` | Directa | `DO_NOT_IMPORT_CALCULATE` | B5 |
| 67 | BO | **Verificar Frec** | `verificarFrecuencia` | INTEGER | EXCEL_FORMULA | CALCULATED | CALCULATED | `APPLIES_ALWAYS` | `RL_MR_EVALUACIONES.EVA_DATA_CALC_JSON -> verificar_frecuencia` | `1:1` | Directa | `DO_NOT_IMPORT_CALCULATE` | B5 |
| 68 | BP | **Verificar Impact** | `verificarImpacto` | INTEGER | EXCEL_FORMULA | CALCULATED | CALCULATED | `APPLIES_ALWAYS` | `RL_MR_EVALUACIONES.EVA_DATA_CALC_JSON -> verificar_impacto` | `1:1` | Directa | `DO_NOT_IMPORT_CALCULATE` | B5 |
| 69 | BQ | **VRI-VRR** | `diferenciaVriVrr` | INTEGER | EXCEL_FORMULA | CALCULATED | CALCULATED | `APPLIES_ALWAYS` | `RL_MR_EVALUACIONES.EVA_DATA_CALC_JSON -> diferencia_vri_vrr` | `1:1` | Directa | `DO_NOT_IMPORT_CALCULATE` | B5 |
| 70 | BR | **Señales de Alerta** | `senalesAlerta` | TEXT | MONITORING | OPERATIONAL | OPTIONAL | `ONLY_MONITORING_CYCLE` | `RL_MR_SENALES_ALERTA.ALE_DESCRIPCION` | `1:N` | Concatenación enumerada ('{i}. {ALE_DESCRIPCI... | `PRESERVE_OPERATIONAL_DB` | B6 |
| 71 | BS | **Estado del Riesgo** | `estadoRiesgoMonitoreo` | CATALOG | MONITORING | OPERATIONAL | OPTIONAL | `ONLY_MONITORING_CYCLE` | `RL_MR_AUTOMONITOREO.MON_ESTADO_RIESGO` | `1:1` | Directa | `PRESERVE_OPERATIONAL_DB` | B6 |
| 72 | BT | **Estado del Control Preventivo** | `estadoControlPreventivo` | CATALOG | MONITORING | OPERATIONAL | OPTIONAL | `ONLY_WHEN_PREVENTIVE_CONTROL_EXISTS` | `RL_MR_CONTROLES_RIESGO.CON_ESTADO_MONITOREO (CON_TIPO=PREVENTIVO)` | `1:1` | Directa | `PRESERVE_OPERATIONAL_DB` | B6 |
| 73 | BU | **Evaluación de la Efectividad del Control Preventivo** | `evaluacionEfectividadControlPreventivo` | DECIMAL | MONITORING | OPERATIONAL | OPTIONAL | `ONLY_WHEN_PREVENTIVE_CONTROL_EXISTS` | `RL_MR_CONTROLES_RIESGO.CON_EFECTIVIDAD_MONITOREO (CON_TIPO=PREVENTIVO)` | `1:1` | Directa | `PRESERVE_OPERATIONAL_DB` | B6 |
| 74 | BV | **Evidencia(s) del Control Preventivo** | `evidenciasControlPreventivo` | EVIDENCE | EVIDENCE | OPERATIONAL | OPTIONAL | `ONLY_WHEN_PREVENTIVE_CONTROL_EXISTS` | `RL_MR_EVIDENCIAS_VINCULOS vinculado a CONTROL PREVENTIVO` | `1:N` | Lista consolidada de nombres de archivo ofici... | `PRESERVE_OPERATIONAL_DB` | B6 |
| 75 | BW | **Estado del Control Detectivo** | `estadoControlDetectivo` | CATALOG | MONITORING | OPERATIONAL | OPTIONAL | `ONLY_WHEN_DETECTIVE_CONTROL_EXISTS` | `RL_MR_CONTROLES_RIESGO.CON_ESTADO_MONITOREO (CON_TIPO=DETECTIVO)` | `1:1` | Directa | `PRESERVE_OPERATIONAL_DB` | B6 |
| 76 | BX | **Evaluación de la Efectividad del Control Detectivo** | `evaluacionEfectividadControlDetectivo` | DECIMAL | MONITORING | OPERATIONAL | OPTIONAL | `ONLY_WHEN_DETECTIVE_CONTROL_EXISTS` | `RL_MR_CONTROLES_RIESGO.CON_EFECTIVIDAD_MONITOREO (CON_TIPO=DETECTIVO)` | `1:1` | Directa | `PRESERVE_OPERATIONAL_DB` | B6 |
| 77 | BY | **Evidencia(s) del Control Detectivo** | `evidenciasControlDetectivo` | EVIDENCE | EVIDENCE | OPERATIONAL | OPTIONAL | `ONLY_WHEN_DETECTIVE_CONTROL_EXISTS` | `RL_MR_EVIDENCIAS_VINCULOS vinculado a CONTROL DETECTIVO` | `1:N` | Lista consolidada de nombres de archivo ofici... | `PRESERVE_OPERATIONAL_DB` | B6 |
| 78 | BZ | **Estado del Control Correctivo** | `estadoControlCorrectivo` | CATALOG | MONITORING | OPERATIONAL | OPTIONAL | `ONLY_WHEN_CORRECTIVE_CONTROL_EXISTS` | `RL_MR_CONTROLES_RIESGO.CON_ESTADO_MONITOREO (CON_TIPO=CORRECTIVO)` | `1:1` | Directa | `PRESERVE_OPERATIONAL_DB` | B6 |
| 79 | CA | **Evaluación de la Efectividad del Control Correctivo** | `evaluacionEfectividadControlCorrectivo` | DECIMAL | MONITORING | OPERATIONAL | OPTIONAL | `ONLY_WHEN_CORRECTIVE_CONTROL_EXISTS` | `RL_MR_CONTROLES_RIESGO.CON_EFECTIVIDAD_MONITOREO (CON_TIPO=CORRECTIVO)` | `1:1` | Directa | `PRESERVE_OPERATIONAL_DB` | B6 |
| 80 | CB | **Evidencia(s) del Control Correctivo** | `evidenciasControlCorrectivo` | EVIDENCE | EVIDENCE | OPERATIONAL | OPTIONAL | `ONLY_WHEN_CORRECTIVE_CONTROL_EXISTS` | `RL_MR_EVIDENCIAS_VINCULOS vinculado a CONTROL CORRECTIVO` | `1:N` | Lista consolidada de nombres de archivo ofici... | `PRESERVE_OPERATIONAL_DB` | B6 |
| 81 | CC | **Observaciones del Área** | `observacionesArea` | LONG_TEXT | MONITORING | INPUT | OPTIONAL | `APPLIES_ALWAYS` | `RL_MR_AUTOMONITOREO.MON_OBSERVACIONES_AREA` | `1:1` | Directa | `PRESERVE_OPERATIONAL_DB` | B6 |
| 82 | CD | **Observaciones UGR** | `observacionesUgr` | LONG_TEXT | MONITORING | INPUT | OPTIONAL | `APPLIES_WHEN_UGR_AUTHORIZED` | `RL_MR_AUTOMONITOREO.MON_OBSERVACIONES_UGR` | `1:1` | Directa | `PRESERVE_OPERATIONAL_DB` | B6 |

---

## 4. Fórmulas Canónicas del Excel (34 Campos Calculados)

| Campo # | Etiqueta | Fórmula Canónica Excel | Dependencias |
|:---:|:---|:---|:---|
| 12 | Valor del Riesgo Inherente | `IF((Frecuencia+Impacto-1)=-1,"",Frecuencia+Impacto-1)` | Frecuencia (10), Impacto (11) |
| 13 | Nivel de Riesgo Inherente | `IFERROR(VLOOKUP(VRI, t_nivel_riesgo, 2, FALSE), "")` | VRI (12), tabla t_nivel_riesgo |
| 22 | Nivel de efectividad preventivo | `IFERROR(VLOOKUP(Escala_Prev, t_efectividad, 2, FALSE), "")` | Escala Prev (21), tabla t_efectividad |
| 23 | % efectividad preventivo | `IFERROR(VLOOKUP(Escala_Prev, t_efectividad, 3, FALSE), "")` | Escala Prev (21), tabla t_efectividad |
| 26 | Nivel de efectividad detectivo | `IFERROR(VLOOKUP(Escala_Det, t_efectividad, 2, FALSE), "")` | Escala Det (25), tabla t_efectividad |
| 27 | % efectividad detectivo | `IFERROR(VLOOKUP(Escala_Det, t_efectividad, 3, FALSE), "")` | Escala Det (25), tabla t_efectividad |
| 30 | Nivel de efectividad correctivo | `IFERROR(VLOOKUP(Escala_Corr, t_efectividad, 2, FALSE), "")` | Escala Corr (29), tabla t_efectividad |
| 31 | % efectividad correctivo | `IFERROR(VLOOKUP(Escala_Corr, t_efectividad, 3, FALSE), "")` | Escala Corr (29), tabla t_efectividad |
| 33 | Efectividad Total Ponderada | `IF(AND(T2="",X2="",AB2=""),"",(B3*W2 + B4*AA2 + B5*AE2))` | Controles (20,24,28), % (23,27,31), Pesos B3:B5 |
| 34 | Riesgo Residual | `IF(Riesgo Inherente="","",Riesgo Inherente)` | Riesgo Inherente (08) |
| 35 | Frecuencia Residual | F11 con topes y bases auxiliares | Frec (10), Imp (11), VRI (12), VRR (37), AUX |
| 36 | Impacto Residual | F12 con topes y bases auxiliares | Frec (10), Imp (11), VRI (12), VRR (37), AUX |
| 37 | Valor del Riesgo Residual | `IFERROR(ROUND(MAX(1, VRI * (1 - ETP)), 0), "")` | VRI (12), ETP (33) |
| 38 | Nivel del Riesgo Residual | `IFERROR(VLOOKUP(VRR, t_nivel_riesgo, 2, FALSE), "")` | VRR (37), tabla t_nivel_riesgo |
| 50 | Frecuencia Residual (AUX) | `IFERROR((1 - ETP) * Frecuencia, "")` | Frec (10), ETP (33) |
| 51 | Impacto Residual (AUX) | `IFERROR((1 - ETP) * Impacto, "")` | Imp (11), ETP (33) |
| 52 | Suma Residual redondeada (AUX) | `IFERROR(VRR + 1, "")` | VRR (37) |
| 53 | F_base (AUX) | `IFERROR(MAX(1, ROUNDDOWN(Frec_AUX, 0)), "")` | Frec_AUX (50) |
| 54 | I_base (AUX) | `IFERROR(MAX(1, ROUNDDOWN(Imp_AUX, 0)), "")` | Imp_AUX (51) |
| 55 | Tope F (AUX) | `IF(Frecuencia="", "", Frecuencia)` | Frecuencia (10) |
| 56 | Tope I (AUX) | `IF(Impacto="", "", Impacto)` | Impacto (11) |
| 57 | Capacidad F (AUX) | `MAX(0, Tope_F - F_base)` | F_base (53), Tope_F (55) |
| 58 | Capacidad I (AUX) | `MAX(0, Tope_I - I_base)` | I_base (54), Tope_I (56) |
| 59 | Resto (AUX) | `MAX(0, Suma_Res - (F_base + I_base))` | Suma_Res (52), F_base (53), I_base (54) |
| 60 | Prefiere I (AUX) | `IF(MOD(Imp_AUX,1) >= MOD(Frec_AUX,1), 1, 0)` | Frec_AUX (50), Imp_AUX (51) |
| 61 | Inc_I (AUX) | Asignación de incremento según Resto y Capacidad | Capacidad_I (58), Resto (59), Prefiere_I (60) |
| 62 | Inc_F (AUX) | `MIN(Capacidad_F, MAX(0, Resto - Inc_I))` | Capacidad_F (57), Resto (59), Inc_I (61) |
| 63 | Valor del Riesgo Residual (AUX) | `IFERROR(MAX(ROUND(Frec_AUX + Imp_AUX - 1 + ETP, 0), 1), "")` | ETP (33), Frec_AUX (50), Imp_AUX (51) |
| 64 | Verificación | `IFERROR(VRR - VRR_AUX, "")` | VRR (37), VRR_AUX (63) |
| 65 | VRR 2 | `IFERROR(Frec_Res + Imp_Res - 1, "")` | Frec_Res (35), Imp_Res (36) |
| 66 | Verificar VRR 2 | `IFERROR(VRR - VRR_2, "")` | VRR (37), VRR_2 (65) |
| 67 | Verificar Frec | `IFERROR(Frecuencia - Frec_Res, "")` | Frec (10), Frec_Res (35) |
| 68 | Verificar Impact | `IFERROR(Impacto - Imp_Res, "")` | Imp (11), Imp_Res (36) |
| 69 | VRI-VRR | `IFERROR(VRI - VRR, "")` | VRI (12), VRR (37) |

---

## 5. Deuda Técnica y Plan de Remediación Futuro
1. **Campos 08 y 09**: En bloques posteriores se debe normalizar el mapping visible de las vistas CRUD de riesgos para que las etiquetas sean *"Riesgo Inherente"* y *"Evaluación"*.
2. **Fallbacks Genéricos**: Eliminar la condición `!this.catalogoEscalaEfectividad()` en `matrices-riesgos.component.ts` que arrojaba erróneamente *"No disponible en esta versión"* cuando un catálogo no cargaba, reemplazándolo por carga robusta y estado de error/vacío.
3. **Persistencia de Fórmulas**: Garantizar que el motor de importación nunca sobrescriba fórmulas con valores crudos, dejando los 34 campos calculados bajo la autoridad estricta del backend runtime.
