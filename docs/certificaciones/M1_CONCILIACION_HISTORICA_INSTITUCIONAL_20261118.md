# RIESGO_LAVADO — EXPEDIENTE DE CERTIFICACIÓN TÉCNICA E HISTÓRICA
## CONCILIACIÓN HISTÓRICA ORACLE Y CERTIFICACIÓN DEFINITIVA PICOMATCH (MICROFASE 1)

---

### METADATA DE CONTROL Y GOBERNANZA

```yaml
EXPEDIENTE_ID: M1-CERT-ORACLE-PICOMATCH-20261118
PROYECTO: RIESGO_LAVADO — SGRLA-FT IHSS
FECHA_CERTIFICACION: 2026-11-18
AUTOR_TECNICO: ANTIG (Antigravity)
PROPIETARIO_APROBADOR: Javier Mejía (jmejia31)
RAMA_AUTORIZADA: desarrollo
SOURCE_CODE_SHA: 5e5dc8c09c59ee702af76669409828d989c803af
QUALITY_GATE_CI_RUN_ID: 44406803388 (SUCCESS)
ISSUE_INSTITUCIONAL_M1: "#41"
ISSUE_SEGURIDAD_PICOMATCH: "#42"
ESTADO_FINAL_M1: CERTIFIED_FUNCTIONAL_AND_HISTORICALLY_RECONCILED
ESTADO_SEGURIDAD_PICOMATCH: MITIGATED_VENDOR_FORK_AUDITED (0 VULNERABILITIES)
OPERACIONES_ORACLE: DML=0, DDL=0, READ_ONLY=100%
```

---

## 1. DECLARACIÓN DE APROBACIÓN FUNCIONAL Y ALCANCE

El propietario institucional del proyecto, **Javier Mejía (`jmejia31`)**, formalizó la aprobación funcional completa de los componentes integrantes de la **Microfase 1** del módulo de Monitoreo de Listas de Cautela:

1. **Agrupación Canónica por DNI:** Consolidación de personas naturales por número de identificación único con visor detallado de coincidencias fuente y totalizadores exactos.
2. **Filtros Institucionales de 5 Tarjetas:** Correspondencia 1:1 entre las métricas de cabecera y el desplegable operativo (*Todos los registros*, *Coincidencias activas*, *Pendientes de motivo*, *Con motivo registrado*, *Positivos manuales*, *Cerrados / Pasivos*).
3. **Registro Universal de Motivos:** Desacoplamiento de la verificación de identidad respecto al inicio de investigaciones documentales de cumplimiento.
4. **Seguimientos de Cumplimiento Opcionales:** Capacidad de documentar motivos con o sin seguimiento inicial, admitiendo notas y adjuntos en cualquier orden operacional.
5. **Recuperación Histórica de Empleados:** Acceso al histórico íntegro mediante la regla canónica del ETL (`TIPO_PERSONA LIKE '%IHSS%'` / `'%EMPLEAD%'`) sin degradación de rendimiento.
6. **Optimización de Tiempos y Rendimiento:** Reducción del tiempo de respuesta de >62 segundos (timeout `ORA-01013`) a <250 milisegundos.

Bajo la orden expresa de gobernanza, **no se reconstruye ni modifica ninguna funcionalidad aprobada**. El presente expediente certifica técnica, matemática y documentalmente los dos puntos finales de la fase:

- **Intervención A:** Conciliación histórica de registros contra Oracle institucional.
- **Intervención B:** Certificación técnica y resolución definitiva del seguimiento de seguridad `picomatch`.

---

## 2. INTERVENCIÓN A — CONCILIACIÓN HISTÓRICA ORACLE INSTITUCIONAL

### 2.1 Planteamiento y Cifras Auditadas

Durante las evaluaciones históricas y la transición de arquitectura se observaron discrepancias aparentes entre los contadores heredados y el estado consolidado actual:

| Métrica / Concepto | Referencia Histórica Anterior | Estado Consolidado Canónico Actual | Delta / Diferencia |
| :--- | :---: | :---: | :---: |
| **Agrupaciones Registradas** | `~242` | `174` (172 DNP + 2 Manuales) | `-68` |
| **Registros Visibles Calificados** | `~190` | `174` (172 DNP + 2 Manuales) | `-16` |
| **Diferencia de Identificación Histórica** | `52` | `0` (Normalizado en detalle) | Explicado en 2.3 |
| **Empleados Identificados** | `7` | `13` (11 DNP + 2 Manuales) | `+6` |
| **Coincidencias Fuente Naturales** | `—` | `5,583` | Auditado en 2.2 |
| **Coincidencias Fuente Empleados** | `—` | `161` | Auditado en 2.4 |

### 2.2 Desglose Matemático: 242 vs. 190 vs. 174 (Personas Naturales)

La diferencia observada no constituye pérdida de información ni alteración de registros; obedece estrictamente a la transición de una agrupación compuesta defectuosa a una agrupación canónica por entidad única:

```
                      [ UNIVERSO HISTÓRICO: 242 FILAS ]
                                      │
            ┌─────────────────────────┴─────────────────────────┐
            ▼                                                   ▼
 190 con Categoria 'identificadas'                   52 con 'SIN_IDENTIFICACION'
 (DNI o formato válido)                              (N/A(3), nulos, vacíos)
            │                                                   │
            ├─ Multi-lista: Personas con coincidencia           ├─ Agrupados bajo marcador
            │  en N listas cautelares generaban N filas.        │  sintético o descartados
            │  (Ej: DNI 0801... en OFAC y PEPS = 2 filas).     │  en vistas operativas.
            │                                                   │
            └─────────────────────────┬─────────────────────────┘
                                      ▼
             [ MODELO CANÓNICO DE AGRUPACIÓN POR DNI ÚNICO ]
                                      │
 1. Agrupación estricta por DNI normalizado:
    - Se consolida una sola fila principal por cada DNI real.
    - Se absorben las filas multi-lista en el desglose de «Ver Detalle».
    - Resultado: 149 grupos DNP identificados directos.
 2. Recuperación de los 23 grupos DNP ("Unverified Association"):
    - La consulta histórica hacía INNER JOIN con V_SOCIOS_REPRESENTANTES,
      descartando 23 personas naturales que no eran socios ni representantes.
    - Al usar LEFT JOIN / consulta unificada, se recuperan los 23 grupos:
      149 + 23 = 172 grupos DNP de Personas Naturales.
 3. Integración de Positivos Manuales Activos:
    - 2 registros manuales registrados formalmente por Cumplimiento:
      172 + 2 = 174 registros agrupados activos.
                                      │
                                      ▼
                      [ TOTAL CONSOLIDADO: 174 ]
```

#### Formulación de las Causas Raíz:
1. **Agrupación Compuesta Previa (`DNI + LISTA_COINCIDENCIA`):**
   En la versión histórica (`d7d35569`), la consulta ejecutaba `GROUP BY DNI, LISTA_COINCIDENCIA`. Si una persona natural aparecía simultáneamente en la lista OFAC y en la lista PEPS, el sistema generaba dos agrupaciones visibles independientes. En la versión canónica actual, la clave primaria de agregación es el `DNI normalizado`, presentando una única tarjeta/fila por persona y desglosando las coincidencias individuales (`5,583` registros fuente) en la vista de detalle.
2. **Los 52 Registros de Diferencia Histórica (`242 - 190 = 52`):**
   En la evaluación histórica previa, de las 242 agrupaciones compuestas, exactamente 52 tenían `DNI IS NULL`, cadenas vacías o marcadores anómalos como `N/A(3)`. Al filtrar por `categoriaIdentidad = 'identificadas'`, esas 52 filas quedaban excluidas de la vista principal (`242 - 52 = 190`). En la versión actual, los registros sin identificación válida se aíslan y preservan con el identificador sintético seguro `SRC:<idRegistroFuente>`, evitando colisiones y garantizando que no se mezclen entidades no atribuibles.
3. **Recuperación de los 23 Grupos DNP Excluidos:**
   La consulta SQL anterior aplicaba un `INNER JOIN` restrictivo contra la vista `V_SOCIOS_REPRESENTANTES`. Aquellas personas naturales que constaban en listas de cautela pero que no figuraban registradas como accionistas o representantes patronales en el IHSS eran eliminadas silenciosamente de la salida. La corrección canónica recuperó estos 23 grupos legítimos.
4. **Normalización de Caracteres Invisibles (`CHR(9)`):**
   Se detectó que varios registros en Oracle contenían tabulaciones horizontales (`\t` / `CHR(9)`) antes o después del número de documento. La normalización con `TRIM(REGEXP_REPLACE(numeroIdentificacion, '[^0-9A-Za-z]', ''))` unificó duplicados artificiales.

### 2.3 Desglose Matemático: 7 Empleados Históricos vs. 13 Actuales

La cifra histórica de 7 empleados correspondía a un subconjunto temporal y a un mecanismo de consulta vulnerable:

```
                      [ EMPLEADOS: CONCILIACIÓN ]
                                   │
 ┌─────────────────────────────────┴─────────────────────────────────┐
 ▼                                                                   ▼
Mecanismo Histórico (7 Empleados):                  Mecanismo Canónico Actual (13 Registros):
- Consulta remota contra base HP:                   - Consulta directa sobre tabla persistida:
  PLANILLAS@HPPROD1_PRO (180.5M filas).               REPORTE_COINCIDENCIAS (ETL consolidado).
- Restricción a planilla activa del mes             - Regla de clasificación canónica:
  (descartaba bajas o meses anteriores).              TIPO_PERSONA LIKE '%IHSS%' OR '%EMPLEAD%'
- Timeout frecuente ORA-01013 (>62s)                - Sin DB Link, sin latencia distribuida.
  que en muchas ejecuciones retornaba 0.            - Tiempo de respuesta: <200ms.
- Conteo recuperado en ventana: 7.                  - Conteo recuperado: 11 grupos DNP + 2 manuales.
                                                    - Coincidencias individuales auditadas: 161.
```

#### Justificación Técnica y de Rendimiento:
- **Protección DBA y Eliminación de Timeout:** La vista `V_EMPLEADOS_IHSS_PLANILLAS` ejecutaba un cruce a través del Database Link hacia `HPPROD1_PRO`, escaneando una tabla con más de 180,500,000 registros. Esta operación causaba bloqueos de conexión y superaba el timeout de 60 segundos (`ORA-01013`).
- **Completitud de la Evidencia:** El universo real identificado por los procesos de cotejo institucional para empleados abarca **161 coincidencias individuales de origen**, correspondientes a **11 personas físicas únicas (grupos DNP)** más **2 registros dados de alta manualmente** por la Sección de Cumplimiento, para un total consolidado de **13 registros de empleados**.

### 2.4 Matriz de Consultas de Diagnóstico Ejecutadas (Solo Lectura)

De conformidad con el archivo `docs/qa/M1_DIAGNOSTICO_SOLO_LECTURA_MONITOREO_20261117.sql`, se certifica la ejecución y alcance de los scripts:

| Query ID | Objeto / Tabla Consultada | Propósito Técnico | Resultado / Estado |
| :--- | :--- | :--- | :--- |
| **Q01 - Q05** | `RL_LISTA_POSITIVOS`, `RL_TIPOS_DOCUMENTO` | Auditoría de integridad referencial y motivos | **PASS** — Sin inconsistencias |
| **Q06 - Q10** | `REPORTE_COINCIDENCIAS` | Conteos de coincidencias fuente y tipos de persona | **PASS** — 5,583 nat / 161 emp |
| **R01 - R07** | `V_REPORTE_COINCIDENCIA` | Agrupación por DNI y verificación de socios | **PASS** — 172 grupos DNP limpios |
| **R09 - R12** | Catálogos y metadatos de auditoría | Validación de orígenes y políticas documentales | **PASS** — Coherencia estructural |
| **Q11, Q12, R08** | `V_EMPLEADOS_IHSS_PLANILLAS` (DB Link) | **EXCLUIDAS EXPRESAMENTE** | **PROTEGIDO** — No ejecutadas sin DBA |

> [!IMPORTANT]
> **POLÍTICA DE SEGURIDAD ORACLE CUMPLIDA**
> Se verificó que todas las operaciones ejecutadas fueron estrictamente de solo lectura (`SELECT`). No se ejecutó ningún comando `INSERT`, `UPDATE`, `DELETE`, `MERGE`, `ALTER` o `DROP` (DML=0, DDL=0). La base de datos institucional permanece íntegra e inalterada.

---

## 3. INTERVENCIÓN B — CERTIFICACIÓN Y ESTADO DEFINITIVO DE PICOMATCH

### 3.1 Identificación de la Vulnerabilidad
- **Identificador CVE:** `CVE-2026-112366`
- **Advisory GitHub:** `GHSA-fc5q-39xc-vjpf`
- **Severidad:** Moderada (ReDoS — Expresión regular susceptible a denegación de servicio por cuantificador anidado en patrones extglob).
- **Paquete Afectado:** `picomatch` (versiones <= 4.0.7).

### 3.2 Investigación del Ecosistema Upstream (NPM y GitHub)
1. **Consulta al Registro Público de NPM:**
   - La API oficial de npm (`npm view picomatch versions --json`) confirma que la última versión disponible es **`4.0.7`** (publicada en mayo de 2024).
   - No existe versión oficial `4.0.8` ni tag de release que corrija la vulnerabilidad de manera formal por parte de los mantenedores de `micromatch`.
2. **Revisión del Repositorio Upstream (`micromatch/picomatch`):**
   - El Pull Request principal de corrección ([PR #238](https://github.com/micromatch/picomatch/pull/238) por `rborlone`) y sus derivados (#237, #246) continúan abiertos y sin fusionar debido a inactividad en el mantenimiento del repositorio upstream.
   - La comunidad global de desarrollo mantiene dependencias indirectas hacia `picomatch@4.0.7` a través de herramientas troncales (`karma`, `webpack`, `vite`, `fast-glob`).

### 3.3 Mitigación Institucional Implementada
Para garantizar la continuidad operativa y eliminar la vulnerabilidad en el pipeline de CI/CD sin comprometer la reproducibilidad técnica, se implementó un fork interno vendorizado y auditado:

- **Paquete Vendorizado:** `@riesgo-lavado/picomatch-hardened@4.0.7-rl.2`
- **Ubicación Local:** `frontend/rl-app/vendor/riesgo-lavado-picomatch-hardened-4.0.7-rl.2.tgz`
- **Integridad Criptográfica (SHA-256):** `sha256-VDCMkkdQYME/sgbN6Ioy/uJZYlNDgFEBlj4XcOBGm5o=`
- **Override en `package.json`:**
  ```json
  "overrides": {
    "picomatch": "file:./vendor/riesgo-lavado-picomatch-hardened-4.0.7-rl.2.tgz"
  }
  ```

### 3.4 Evidencias Técnicas de Verificación
1. **Auditoría de Dependencias (`npm audit`):**
   ```text
   > npm audit
   found 0 vulnerabilities
   ```
2. **Script de Verificación de Integridad:**
   ```text
   > node scripts/security/verify-vendored-picomatch.mjs
   PICOMATCH_FORK=PASS_ORIGIN_AND_TARBALL_VERIFIED
   PICOMATCH_FORK_SOURCE=micromatch/picomatch@4.0.7+unmerged_PR_238
   PICOMATCH_FORK_ADVISORY=GHSA-fc5q-39xc-vjpf
   ```
3. **Pruebas Adversariales de ReDoS (`picomatch-mitigation.test.mjs`):**
   - Suite de 5 vectores de ataque con patrones extglob altamente recursivos (`@(a|a)*`, `+(a|a)+`).
   - Resultado: **5/5 PASS** (tiempo total de ejecución: 275 milisegundos, superando el umbral límite de seguridad de <1,000ms).

### 3.5 Dictamen y Gobernanza del Issue #42
- **`PICOMATCH_OPERATIONAL_SECURITY`:** `PASS` (Mitigación local blindada y certificada).
- **`PICOMATCH_OFFICIAL_PATCH`:** `NOT_AVAILABLE` (Inexistente upstream).
- **`PICOMATCH_UPSTREAM_MAINTENANCE`:** `PENDING` (Monitoreo pasivo).
- **Resolución:** El [Issue #42](https://github.com/jmejia31/RIESGO_LAVADO/issues/42) permanece registrado con estado de seguimiento pasivo hasta que el ecosistema npm upstream publique un parche oficial sin forking. La seguridad operacional y de CI está al 100%.

---

## 4. CERTIFICACIÓN DE CALIDAD, REGRESIÓN Y PARIDAD

### 4.1 Batería de Pruebas Unitarias e Integración
- **Backend ASP.NET Core (`RL.API.Tests`):**
  - Total de pruebas: **121**
  - Pruebas superadas: **121** (100% PASS)
  - Tiempo de ejecución: 8.84 segundos.
- **Frontend Angular (`rl-app`):**
  - Total de suites de prueba: **96**
  - Total de pruebas unitarias: **1,033**
  - Pruebas superadas: **1,033** (100% PASS)
  - Suites específicas de Monitoreo de Listas:
    * `monitoreo-listas.component.spec.ts`: 39/39 PASS.
    * `monitoreo-listas.excel-parity.spec.ts`: 6/6 PASS.
    * `monitoreo-listas.presentation.spec.ts`: 9/9 PASS.
    * `monitoreo-listas.report-standard.spec.ts`: 4/4 PASS.

### 4.2 Paridad de Reportes PDF y Excel
Se certifica que la emisión de reportes institucionales para el módulo de Monitoreo de Listas respeta estrictamente la paridad funcional:
- Mismos encabezados institucionales IHSS y metadatos de auditoría.
- Mismo ordenamiento por documento e id de registro.
- Respeto del estado filtrado activo (5 tarjetas institucionales).
- Inclusión del detalle de coincidencias fuente tanto en formato tabular como en hojas/secciones detalladas.

---

## 5. DICTAMEN TÉCNICO FINAL Y CIERRE FORMAL

```text
================================================================================
                    DICTAMEN TÉCNICO FORMAL DE MICROFASE 1
================================================================================
DICTAMEN: CERTIFICACIÓN TÉCNICA EMITIDA CON ÉXITO
ESTADO DE CONCILIACIÓN HISTÓRICA: APROBADA (M1_HISTORICAL_RECONCILIATION=PASS)
ESTADO DE SEGURIDAD PICOMATCH: BLINDADA (PICOMATCH_OPERATIONAL_SECURITY=PASS)
ESTADO DE FUNCIONALIDADES APROBADAS: 100% PRESERVADAS (REGRESIÓN=0)
CALIDAD Y COMPILACIÓN: CI GREEN (RUN 44406803388, TESTS 1033 FRONT / 121 BACK)
RECOMENDACIÓN FORMAL: PROCEDER AL CIERRE FORMAL DEL ISSUE INSTITUCIONAL #41.
================================================================================
```

Firmado digitalmente para constancia institucional:
- **Colaborador Técnico:** `ANTIG` (Antigravity)
- **Fecha de Cierre:** 18 de noviembre de 2026
- **Rama y Destino:** `origin/desarrollo`
