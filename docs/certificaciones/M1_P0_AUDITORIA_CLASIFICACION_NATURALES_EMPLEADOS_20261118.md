# Expediente Técnico P0 — Microfase 1: Auditoría Quirúrgica de Personas Naturales, Empleados y Clasificación Institucional

- **Fecha:** 2026-11-18 (-06:00)
- **Autor:** `ANTIG` (Antigravity)
- **Proyecto:** `RIESGO_LAVADO` — SGRLA-FT IHSS
- **Repositorio:** `jmejia31/RIESGO_LAVADO`
- **Rama autorizada:** `desarrollo`
- **Base SHA de referencia:** `c2170fbf5ec7c5e3e8963c141d86a9f1ee5a97ed` (Quality Gate `44360374287`)
- **Source Code SHA:** `dec8c88942530ac2ba6802653ea9d794ab957a6f`
- **Quality Gate certificado:** [44380767197](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/44380767197) (`CONCLUSION=success`, `STATUS=completed`)
- **Issue institucional:** [Issue #41](https://github.com/jmejia31/RIESGO_LAVADO/issues/41) (permanece **ABIERTO**)
- **Aprobación funcional del propietario:** `PENDING`
- **Escrituras en Oracle institucional / `DNP_IHSS` / `main` / producción:** `0` (`NO_TOUCHED`)
- **Tratamiento de PII:** `0` DNIs, nombres o datos sensibles publicados o registrados.

---

## 1. Contexto y Objetivos de la Intervención P0

La intervención previa (`c2170fbf`) restauró la agrupación por DNI y aprobó los Quality Gates automatizados. Sin embargo, la verificación visual del propietario presentó dos discrepancias que motivaron esta auditoría forense quirúrgica:

1. **Personas Naturales:** El contador visual mostraba 176 registros (agrupados) frente a una referencia histórica previa de 142 (diferencia de 34).
2. **Empleados:** El contador visual mostraba 0 registros, a pesar de existir antecedentes de empleados históricos con coincidencias en listas cautelares.

Adicionalmente, se detectó un defecto latente en el detalle de personas naturales: multiplicación por producto cartesiano en sujetos con múltiples empresas cuando `NUMERO_PATRONO IS NULL`.

---

## 2. Diagnóstico Forense y Causa Raíz

### 2.1 Causa Raíz: Discrepancia de Personas Naturales (176 vs 142)

Se analizó la composición matemática de los 176 registros devueltos por el endpoint `GET /api/listas/naturales/paginado?categoriaIdentidad=identificadas`:
- **Universo actual:** 174 grupos provenientes de `DNP_IHSS.REPORTE_COINCIDENCIAS` + 2 registros manuales activos de `RL_LISTA_POSITIVOS` (tipo natural) = **176 registros**.
- **Normalización de caracteres de control:** Se detectaron 2 registros con tabulación inicial (`CHR(9)` / `%09`) en `DNI` en la base de datos institucional. Al aplicar `TRIM(TRANSLATE(col, CHR(9)||CHR(10)||CHR(13), '   '))`, estos 2 grupos se consolidan automáticamente con sus DNI limpios correspondientes, reduciendo el conteo de grupos DNP de 174 a 172 (+ 2 manuales = 174).
- **Origen de la cifra histórica 142:**
  - En la consulta histórica heredada (`6493bce6~1`), se aplicaba un `INNER JOIN` restrictivo contra `DNP_IHSS.V_SOCIOS_REPRESENTANTES` filtrando por `FECHA_CALIFICO IS NOT NULL`.
  - Del universo de grupos DNP de personas naturales:
    - Exactamente **151** grupos DNP poseen coincidencia en `V_SOCIOS_REPRESENTANTES`.
    - Exactamente **23** grupos DNP **no** poseen coincidencia en `V_SOCIOS_REPRESENTANTES` (`ESTADO_ASOCIACION = 'UNVERIFIED_ASSOCIATION'`).
  - La consulta histórica descartaba silenciosamente los 23 grupos sin vínculo empresarial y dependía del filtro de fecha de calificación.
  - La directriz de cumplimiento institucional prohíbe descartar coincidencias de listas cautelares solo porque el sujeto no esté vinculado como socio/representante patronal. Por ello, el sistema actual preserva estos 23 grupos bajo `ESTADO_ASOCIACION = 'UNVERIFIED_ASSOCIATION'`.
  - **Conclusión:** No existe pérdida ni duplicación arbitraria de datos. La diferencia entre 176 y 142 se debe estrictamente a la conservación de los 23 grupos no asociados a empresas patronales, los 2 positivos manuales y variantes de tabulación en DNI.

### 2.2 Causa Raíz: Empleados = 0

- Al inspeccionar la telemetría del servidor local (`rl-20261118.log`) para `GET /api/listas/empleados/paginado`, se evidenció:
  ```text
  Oracle.ManagedDataAccess.Client.OracleException (0x80004005): ORA-01013: user requested cancel of current operation
  ORA-02063: preceding line from HPPROD1_PRO
  at ListasRepository.ConstruirConsultaMonitoreoEmpleados [...] duration: 62714 ms
  ```
- **Mecanismo del fallo:**
  - En el commit `c2170fbf`, `ConstruirConsultaMonitoreoEmpleados` realizaba un cruce mediante CTE agregada sobre `DNP_IHSS.V_EMPLEADOS_IHSS_PLANILLAS`.
  - Esta vista remota consulta `PLANILLAS@HPPROD1_PRO` (180.5 millones de filas a lo largo de 407 períodos históricos).
  - Al no contar con un predicado de período empujable (`pushdown`), Oracle ejecutaba un hash aggregate remoto de 180.5M filas sobre el DB Link, superando el timeout de 60 segundos (`ORA-01013`).
  - La API retornaba HTTP 500 y el componente frontend Angular capturaba el error asignando `0` al contador por defecto.
- **Solución implementada:**
  - Se desacopló el listado principal de la consulta sobre `V_EMPLEADOS_IHSS_PLANILLAS`.
  - Se adoptó la regla canónica del proceso ETL institucional (`Base de Datos DNP_IHSS.sql`, línea 2704), donde `REPORTE_COINCIDENCIAS.TIPO_PERSONA LIKE '%IHSS%'` / `'%EMPLEAD%'` clasifica inequívocamente a los empleados identificados por el proceso de carga.
  - Se sincronizó la condición complementaria `CondicionNoEsEmpleadoIhss` en Personas Naturales para garantizar solapamiento cero entre ambas secciones (`OVERLAP = 0`).
  - **Resultado:** Tiempo de respuesta reducido de **>62,700 ms (timeout)** a **1,274 ms** (HTTP 200).
  - Se recuperaron **13 registros de empleados** (11 grupos DNP históricos + 2 manuales), correspondientes a **161 coincidencias individuales de origen**.

### 2.3 Causa Raíz: Producto Cartesiano en Detalle de Personas Naturales

- En `ObtenerDetalleNaturalAsync()`, al consultar una persona con múltiples empresas registradas en `V_SOCIOS_REPRESENTANTES` cuando `R.NUMERO_PATRONO IS NULL`, la consulta unía cada coincidencia contra todas las empresas asociadas al DNI.
- En el caso testigo DNI `33724` (70 coincidencias de origen y 10 vínculos empresariales), la API generaba **700 filas** (70 × 10).
- **Solución implementada:**
  - Refactorización de las CTEs `SOCIOS_CAND` y `SOCIOS_RESUMEN` con funciones de ventana (`ROW_NUMBER() OVER (PARTITION BY R.REPORTE_COINCIDENCIA_ID ORDER BY ...)`).
  - Cuando existe multiplicidad de empresas y `NUMERO_PATRONO` es nulo, se emite exactamente **1 fila por coincidencia** con `ESTADO_ASOCIACION = 'AMBIGUOUS'` y texto descriptivo, eliminando completamente la multiplicación cartesiana.
  - Para el caso testigo `33724`, ahora devuelve exactamente **70 filas** para sus 70 coincidencias en **239 ms**.

---

## 3. Modificaciones de Código y SQL

### 3.1 Backend (`backend/RL.API/Features/Listas/Persistence/ListasRepository.cs`)
1. **Normalización universal de documentos:** Implementación de `TRIM(TRANSLATE(col, CHR(9)||CHR(10)||CHR(13), '   '))` en agrupaciones, proyecciones y filtros por DNI.
2. **Desacoplamiento de escaneo remoto en empleados:** Consulta rápida basada en `TIPO_PERSONA` institucional y soporte de detalle rápido.
3. **Guarda anti-cartesiano en detalle:** Emisión estricta de 1 fila por `REPORTE_COINCIDENCIA_ID` mediante ventana analítica.

### 3.2 SQL de Diagnóstico (`docs/qa/M1_DIAGNOSTICO_SOLO_LECTURA_MONITOREO_20261117.sql`)
- Adición de las consultas de auditoría de solo lectura `R01` a `R12` para verificación independiente por el DBA / Propietario.

### 3.3 Pruebas de Regresión Actualizadas
- `backend/RL.API.Tests/Features/Listas/MonitoreoFuenteM1Tests.cs`: 3 nuevas pruebas unitarias (`P0_ClasificacionNaturalEmpleado_EsComplementariaYSinVistaRemotaEnListado`, `P0_NormalizacionDocumento_EliminaTabulacionesYSaltosDeLinea`, `P0_DetalleNatural_UnaFilaPorCoincidenciaSinProductoCartesianoPorEmpresas`).
- `backend/RL.API.Tests/Infrastructure/DbPaginationRegressionGuardTests.cs`: Verificación de sintaxis normalizada.
- `backend/RL.API.Tests/Features/Listas/MonitoreoListasM1OracleIntegrationTests.cs`: Aserciones de clasificación y anti-cartesiano.

---

## 4. Telemetría y Evidencia Ejecutada en Vivo

| Endpoint | Parámetros | Código HTTP | Tiempo | Registros / Grupos | Coincidencias Totales |
|---|---|---|---|---|---|
| `GET /api/listas/empleados/paginado` | Por defecto | **200 OK** | **1,274 ms** | **13** (11 DNP + 2 manuales) | **161** |
| `GET /api/listas/naturales/paginado` | `categoriaIdentidad=identificadas` | **200 OK** | **720 ms** | **174** (172 DNP + 2 manuales) | **5,583** |
| `GET /api/listas/naturales/paginado` | `categoriaIdentidad=sin_identificacion` | **200 OK** | **567 ms** | **12** (12 DNP) | **12** |
| `GET /api/listas/naturales/detalle` | DNI `33724` | **200 OK** | **239 ms** | **70** (anti-cartesiano verificado) | **70** |

- **Solapamiento Naturales vs Empleados:** **0** coincidencias compartidas.
- **Rendimiento:** Todos los endpoints responden en menos de 1.3 segundos (reducción de 98% frente al timeout de 62.7s en Empleados).

---

## 5. Estado de Cumplimiento

- `ORACLE_INSTITUTIONAL_WRITES`: `0` (ningún DDL ni DML ejecutado en BD institucional).
- `DNP_IHSS_WRITES`: `0` (esquema intacto).
- `MAIN_BRANCH_TOUCHED`: `NO` (todo el trabajo se realiza en `desarrollo`).
- `SECURITY_AUDIT`: `PASS_FULL_TREE` (0 vulnerabilidades, fork de picomatch verificado).
- `OWNER_ACCEPTANCE`: `PENDING_REVIEW` (a ser revisado visualmente por `jmejia31`).
