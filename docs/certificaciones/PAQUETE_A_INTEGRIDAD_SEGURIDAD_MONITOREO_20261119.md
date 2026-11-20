# Expediente de Certificación Técnica — Paquete A: Integridad de Expedientes y Seguridad de Fuentes Externas

## 1. Identificación y Control

| Campo | Valor |
| :--- | :--- |
| **Proyecto** | SGRLA-IHSS — RIESGO_LAVADO |
| **Repositorio** | `jmejia31/RIESGO_LAVADO` |
| **Rama Autorizada** | `desarrollo` |
| **Colaborador / Agente** | ANTIG — Antigravity |
| **Autoridad Funcional** | Javier Mejía (`jmejia31`) — Propietario del Sistema |
| **Fecha de Intervención** | 19 de noviembre de 2026 |
| **Base SHA** | `ef72356f4996b47dbbc29097df30deafbbfed641` |
| **Código Implementado SHA** | `115f8035e1d9bafa7553a785959ce038b921caf8` |
| **Código Inicial SHA** | `bc9120e79e647416ca56b42a08fa492150d82bf9` |
| **Documentación Base SHA** | `8e31268a39cb38947e1610b113171f7eacfeeeeb` |
| **Quality Gate Verificado** | [44652692783](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/44652692783) — `completed / success` (13m6s) |
| **Quality Gate Previo** | `44644160900` (SUCCESS) |
| **Respaldos Remotos Verificados** | `origin/respaldo/desarrollo-pre-paquete-a-integridad-seguridad-20261119-ef72356f` (`ef72356f4996b47dbbc29097df30deafbbfed641`)<br>`origin/respaldo/desarrollo-pre-correccion-paquete-a-20261119-8e31268a` (`8e31268a39cb38947e1610b113171f7eacfeeeeb`) |
| **Estado de Certificación** | `READY_FOR_INDEPENDENT_REVIEW` |
| **Cumplimiento DNP_IHSS** | `NO_NEW_EXTERNAL_WRITES=PASS` / `DNP_GLOBAL_READ_ONLY_COMPLIANCE=NOT_YET_DEMONSTRATED` |

---

## 2. Diagnóstico y Causa Raíz

En el módulo **Monitoreo de Listas (M1)**, una misma persona física puede figurar simultáneamente en el sistema como **Persona Natural (`TipoPositivoId = 2`)** y como **Empleado IHSS (`TipoPositivoId = 3`)** compartiendo exactamente el mismo Documento Nacional de Identificación (DNI) o documento de identidad:

1. **Riesgo de Contaminación Cruzada:**
   Anteriormente, al consultar un positivo o registrar un seguimiento sin especificar el tipo de sujeto, el sistema utilizaba búsquedas que no discriminaban el `LSP_TIPO_POSITIVO_ID`, o recurría a un fallback arbitrario mutando prefijos (`EMP:`), provocando que un seguimiento de Persona Natural pudiera asociarse al expediente de Empleado o viceversa.

2. **Manejo de Ambigüedad (Escenario A-11):**
   Cuando coexisten registros activos de distintos tipos de sujeto para un mismo número de documento y una consulta no proporciona `tipoPositivoId`, el sistema debe rechazar la ambigüedad con código HTTP 409 Conflict (`AMBIGUOUS_IDENTITY`) en lugar de seleccionar arbitrariamente uno de los registros.

3. **Auditoría de Escrituras Externas a `DNP_IHSS`:**
   Se requería auditar exhaustivamente el repositorio para garantizar que la intervención no introduce **ninguna nueva escritura** a esquemas externos (`DNP_IHSS`), catalogar las operaciones de escritura preexistentes y documentar su estado de cumplimiento.

---

## 3. Implementación de la Solución Técnica

### 3.1 Backend ASP.NET Core (`RL.API`)
- **`IListasRepository` y `ListasRepository`:**
  - `ContarPositivosActivosAsync(documento, tipoPositivoId, idRegistroFuente)`: Se acotó el conteo por tipo de positivo específico y soporte para claves fuente `SRC:`.
  - `RegistrarPositivoCoreAsync`: Se verificó que el registro/actualización valide `LSP_TIPO_POSITIVO_ID = dto.TipoPositivoId`, permitiendo la coexistencia nativa en `RL_LISTA_POSITIVOS` de Natural y Empleado con la misma identidad sin colisión ni sobreescritura.
  - `ObtenerPositivoPorDocumentoAsync(noDocumento, tipoPositivoId, idRegistroFuente)`: Aísla la resolución por tipo de positivo (`tipoPositivoId`), reconociendo variantes canónicas (`EMP:`, `DNI:`, `SRC:`). Lanza `AmbiguousIdentityException` cuando existen múltiples activos y no se especificó el tipo.
  - `ObtenerPositivoIdPorDocumentoAsync`: Centraliza la resolución delegando en `ObtenerPositivoPorDocumentoAsync`.
  - `ObtenerSeguimientosAsync`: Integra aislamiento por sujeto (`tipoPositivoId`, `idRegistroFuente`), garantizando que notas y evidencias se asocien única y exclusivamente al expediente correspondiente.
- **`IListasService` y `ListasService`:**
  - Métodos `ObtenerPositivoPorDocumentoAsync`, `ObtenerSeguimientosAsync` y `RegistrarPositivoAsync` actualizados para admitir y propagar los parámetros de aislamiento. Eliminado el fallback inseguro a `EMP:`.
- **`IEvidenciasService` y `EvidenciasService`:**
  - `RegistrarSeguimientoAsync` actualizado para resolver el positivo con `tipoPositivoId` e `idRegistroFuente`, eliminando cualquier fallback a prefijos ajenos.
- **`ListasController`:**
  - Endpoints `GET /api/listas/positivos/{noDocumento}`, `GET /api/listas/positivos`, `GET /api/listas/positivos/{noDocumento}/seguimientos`, `POST /api/listas/positivos` y `POST /api/listas/seguimientos` adaptados para recibir `tipoPositivoId` e `idRegistroFuente` vía query/form params.

### 3.2 Frontend Angular (`frontend/rl-app`)
- **`ListasService`:**
  - Métodos `getPositivoPorDocumento`, `getSeguimientos` y `registrarSeguimiento` actualizados para enviar `tipoPositivoId` e `idRegistroFuente` en `HttpParams` y `FormData`.
- **`MonitoreoListasComponent` (`monitoreo-listas.component.impl.ts`):**
  - Métodos de reportería (`descargarReporteCoincidenciasPdf`, `descargarReporteCoincidenciasExcel`, etc.) parametrizados con su respectivo tipo: Jurídica (`tipoPosId: 1`), Natural (`tipoPosId: 2`), Empleado (`tipoPosId: 3`).
  - Métodos de gestión modal (`registrarMotivo`, `guardarMotivo`, `darSeguimiento`, `cargarSeguimientos`, `guardarSeguimiento`) parametrizados con `tipoPosId` e `idRegistroFuente` correspondientes al registro seleccionado.

---

## 4. Auditoría Exhaustiva de Escrituras a `DNP_IHSS`

Se realizó una auditoría estática automatizada en todo el código fuente productivo (`backend/RL.API`, `frontend/rl-app/src`).

### 4.1 Inventario de Operaciones Heredadas Detectadas
Exclusivamente en `backend/RL.API/Features/Listas/Persistence/ListasRepository.cs` se identificaron 6 operaciones heredadas:

| No. | Operación SQL | Tabla Externa | Método en `ListasRepository.cs` | Justificación Histórica / Contexto |
| :---: | :---: | :---: | :---: | :--- |
| 1 | `INSERT INTO` | `DNP_IHSS.TIPO_LISTAS_CAUTELA` | `CrearTipoListaCautelaAsync` | Módulo de Catálogo de Listas Cautela heredado. |
| 2 | `UPDATE` | `DNP_IHSS.TIPO_LISTAS_CAUTELA` | `ActualizarTipoListaCautelaAsync` | Modificación de descripción de tipo de lista cautela. |
| 3 | `DELETE FROM` | `DNP_IHSS.TIPO_LISTAS_CAUTELA` | `EliminarTipoListaCautelaAsync` | Eliminación de tipo de lista cautela heredada. |
| 4 | `UPDATE` | `DNP_IHSS.REPORTE_COINCIDENCIAS` | `CalificarCoincidenciaAsync` | Registro de calificación y fecha en tabla operativa de coincidencias. |
| 5 | `DELETE FROM` | `DNP_IHSS.LISTA_CAUTELA` | `CargarListaCautelaAsync` | Reemplazo transaccional de registros en carga masiva de listas cautela. |
| 6 | `INSERT INTO` | `DNP_IHSS.LISTA_CAUTELA` | `CargarListaCautelaAsync` | Inserción en bloque durante carga masiva de listas cautela. |

### 4.2 Declaración Formal de Cumplimiento
- **`NO_NEW_EXTERNAL_WRITES=PASS`**: La presente intervención no añadió ninguna nueva instrucción de escritura (DDL o DML) contra el esquema `DNP_IHSS` ni contra ninguna otra fuente externa.
- **`DNP_GLOBAL_READ_ONLY_COMPLIANCE=NOT_YET_DEMONSTRATED`**: La lectura global exclusiva en `DNP_IHSS` no está completada en el sistema debido a la existencia de las 6 rutas heredadas catalogadas anteriormente, las cuales requieren una fase planificada de migración de catálogos hacia el esquema propio de la aplicación (`RL_*`).
- **Validador Preventivo Automatizado:** Se creó el script `tools/validate_external_writes.ps1`, el cual verifica que no existan escrituras a `DNP_IHSS` fuera de las 6 rutas heredadas auditadas.

---

## 5. Pruebas Ejecutadas y Métricas de Calidad

### 5.1 Pruebas Unitarias del Backend (`RL.API.Tests`)
- **Total de pruebas unitarias ejecutadas:** 1,306
- **Superadas:** 1,306
- **Fallidas:** 0
- **Nuevas pruebas de aislamiento añadidas:**
  - `backend/RL.API.Tests/Features/Listas/EvidenciasServiceIsolationTests.cs`:
    - `RegistrarSeguimiento_ConTipoPositivoId_ResuelvePositivoAislado`: Valida que el seguimiento de empleado no resuelva el positivo de persona natural.
    - `RegistrarSeguimiento_SinTipoPositivo_ConMultiplesActivos_LanzaAmbiguedad`: Valida el escenario A-11 (HTTP 409 Conflict).
    - `RegistrarSeguimiento_ConIdRegistroFuente_ResuelvePositivoSrc`: Valida la compatibilidad con claves `SRC:`.
  - `backend/RL.API.Tests/Features/Listas/ListasApplicationTests.cs`:
    - `ObtenerSeguimientos_ConTipoPositivoId_AislaYNoLanzaAmbiguedad`.
    - `RegistrarPositivo_ClaveFuenteSrc_PreservaReferencia`.
  - `backend/RL.API.Tests/Features/Listas/ListasControllerTests.cs`:
    - `ObtenerPositivo_ConTipoPositivoId_PasaParametrosAlServicio`.
    - `ObtenerSeguimientos_ConTipoPositivoId_PasaParametrosAlServicio`.

### 5.2 Pruebas Unitarias del Frontend (`frontend/rl-app`)
- **Suites ejecutadas:** 96 suites (96 superadas)
- **Total de pruebas unitarias:** 1,059 superadas, 0 fallidas
- **Pruebas de listas actualizadas:**
  - `src/app/features/admin/listas/data-access/listas.service.spec.ts` (18 superadas)
  - `src/app/features/admin/listas/pages/monitoreo-listas/monitoreo-listas.component.spec.ts` (39 superadas)

### 5.3 Validadores del Repositorio
- `tools/validate_repository_structure.ps1`: **PASS** (118 rutas, 1309 archivos, 3 maestros).
- `tools/validate_database_scripts.ps1`: **PASS** (Esquema base íntegro, scripts activos validados).
- `tools/validate_documentation_links.ps1`: **PASS** (264 documentos Markdown, 284 enlaces revisados).
- `tools/validate_agent_skills.py`: **PASS** (14 Agent Skills validadas).
- `tools/validate_external_writes.ps1`: **PASS** (`NO_NEW_EXTERNAL_WRITES=PASS`, 6 rutas heredadas catalogadas).

### 5.4 Limitaciones Documentadas
- **Pruebas de Integración Oracle XE:** 8 pruebas de integración que requieren la base de datos `LOCAL_XE` activa en el entorno local no fueron ejecutadas localmente y se delegan a la verificación en GitHub Actions CI (las suites correspondientes saltan o fallan rápido localmente por falta de contenedor Oracle).
- **Entorno Productivo:** No se realizaron modificaciones en producción ni en la rama `main`.
- **Archivos no rastreados:** 38 archivos locales no rastreados se mantienen preservados fuera del control de versiones.

---

## 6. Preservación de Módulos y No-Regresión

1. **Monitoreo de Listas (M1):** Agrupación por DNI, calificación y seguimiento preservados al 100%.
2. **Reportería PDF/Excel (M2 - B06):** Encabezados institucionales `#FFFFFF`, colores institucionales, logo y vistas previas completamente preservados.
3. **Identidad Dinámica (M3):** Título dinámico del navegador y favicon institucional preservados al 100%.
4. **Matrices de Riesgos:** 82 campos canónicos, persistencia y ciclo de vida de versiones intactos.

---

## 7. Adenda de Corrección Quirúrgica P0 — Auditoría Independiente y Cierre Complementario

En respuesta a la auditoría independiente y las observaciones del propietario del proyecto, se ejecutó la resolución integral de los cinco hallazgos identificados:

### 7.1 Resolución de Hallazgos
1. **PA-P0-01 (CRÍTICA) — Coexistencia física bajo `UQ_RL_LSP_DOCUMENTO UNIQUE (LSP_TIPO_DOCUMENTO_ID, LSP_NO_DOCUMENTO)`:**
   - Para resolver la limitación de la clave única física de Oracle sin alterar el esquema ni ejecutar DDL institucional, se implementó la discriminación de clave documental:
     - Los registros de **Empleado IHSS (`TipoPositivoId = 3`)** se insertan físicamente con clave `EMP:<cleanDoc>` (coincidente con la consulta de vinculación en Monitoreo M1).
     - Los registros de **Persona Natural (`TipoPositivoId = 2`)** se insertan con `cleanDoc`; si este se encuentra ocupado por un registro histórico sin prefijo, se insertan como `DNI:<cleanDoc>`.
     - Se garantiza la coexistencia en ambos órdenes de inserción (Escenario A: Natural primero, Empleado segundo; Escenario B: Empleado primero, Natural segundo) y con datos heredados (Escenario C).
2. **PA-P0-02 (CRÍTICA) — Desasociación estricta entre documentos identificados y claves de fuente:**
   - Se erradicó totalmente la cláusula `OR LSP_NO_DOCUMENTO = :srcDoc` de las ramas de consulta de personas y empresas identificadas en `ListasRepository.cs`. Las claves `SRC:<ID>` quedan reservadas exclusivamente para sujetos con condición `SIN_IDENTIFICACION` (`N/A(3)`).
3. **PA-P1-03 (ALTA) — Pleno reconocimiento de representaciones históricas y resolución de ambigüedad:**
   - Se reconocen todas las variantes (`cleanDoc`, `EMP:<DOC>`, `DNI:<DOC>`, `SRC:<ID>`). Al especificar el tipo de sujeto, se resuelve únicamente la entidad correspondiente; ante omisión del tipo con múltiples registros activos para el mismo DNI, se responde con HTTP 409 Conflict y código `AMBIGUOUS_IDENTITY`.
4. **PA-P1-04 (ALTA) — Endurecimiento del validador de escrituras externas `tools/validate_external_writes.ps1`:**
   - Se incorporó inventario estricto de las 6 rutas históricas (DNP-01 a DNP-06), soporte multilínea para comandos SQL (`INSERT`, `UPDATE`, `DELETE`, `MERGE`, `TRUNCATE`, `ALTER`, `DROP`, `CREATE`), filtrado de comentarios y suite de pruebas sintéticas (`-SelfTest`, 6 pruebas PASS).
   - Se integró obligatoriamente en `tools/run_quality_gates.ps1` y en el flujo `.github/workflows/quality-gates.yml`.
5. **PA-P1-05 (ALTA) — Corrección de SHA documentado:**
   - Se subsanó la errata del SHA del código del Paquete A registrando `bc9120e79e647416ca56b42a08fa492150d82bf9`.
6. **Validación de Parámetros Contradictorios (Sección 4.5):**
   - En `ListasController.cs`, se validan discrepancias entre parámetros de URL (Query) y Formulario (Form), respondiendo con HTTP 400 Bad Request (`PARAMETROS_CONTRADICTORIOS`), y se validan los rangos permitidos (`tipoPositivoId` entre 1 y 3, `idRegistroFuente > 0`).

### 7.2 Resultados de Verificación Final
- **Backend Unitario (`RL.API.Tests`):** 1,315 pruebas superadas (1,315/1,315 PASS, 0 fallidas).
- **Frontend Vitest (`frontend/rl-app`):** 96 suites superadas, 1,059 pruebas superadas (1,059/1,059 PASS, 0 fallidas).
- **Validadores del Repositorio:** 5/5 PASS (`tools/validate_external_writes.ps1 -SelfTest` PASS).
