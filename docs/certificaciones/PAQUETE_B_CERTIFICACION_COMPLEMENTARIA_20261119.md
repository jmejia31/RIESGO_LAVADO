# Expediente de Certificación Complementaria Integral — Paquete B

## 1. Identificación y Control

| Campo | Valor |
| :--- | :--- |
| **Proyecto** | SGRLA-IHSS — RIESGO_LAVADO |
| **Repositorio** | `jmejia31/RIESGO_LAVADO` |
| **Rama Autorizada** | `desarrollo` |
| **Colaborador / Agente** | ANTIG — Antigravity |
| **Autoridad Funcional** | Javier Mejía (`jmejia31`) — Propietario del Sistema |
| **Fecha de Intervención** | 19 de noviembre de 2026 |
| **Modalidad de Intervención** | Verificación y Auditoría sin Modificaciones Funcionales (`FUNCTIONAL_CODE_CHANGES=0`) |
| **Base SHA** | `e246c6f45a10f0c147b5c947642acb9619c66769` |
| **Código Certificado SHA** | `966d0ec825e70089da983d02644b2acb0306fab6` |
| **Quality Gate Verificado (Código)** | [44660135562](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/44660135562) — `completed / success` (12m48s) |
| **Quality Gate Verificado (Documental)** | [44662356256](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/44662356256) — `completed / success` |
| **Respaldos Remotos Verificados** | `origin/respaldo/desarrollo-pre-paquete-a-integridad-seguridad-20261119-ef72356f` (`ef72356f4996b47dbbc29097df30deafbbfed641`)<br>`origin/respaldo/desarrollo-pre-correccion-paquete-a-20261119-8e31268a` (`8e31268a39cb38947e1610b113171f7eacfeeeeb`)<br>`origin/respaldo/desarrollo-pre-intervencion-final-paquete-a-20261119-08073695` (`08073695dcf35cb27affbecb304753aedba9218f`) |
| **Estado de Certificación** | `READY_FOR_INDEPENDENT_REVIEW` |
| **Cumplimiento DNP_IHSS** | `NO_NEW_EXTERNAL_WRITES=PASS` / `DNP_GLOBAL_READ_ONLY_COMPLIANCE=NOT_YET_DEMONSTRATED` |
| **Preservación Aprobaciones Propietario** | `B06_OWNER_ACCEPTANCE=APPROVED`<br>`M1_OWNER_ACCEPTANCE=APPROVED`<br>`M2_OWNER_ACCEPTANCE=APPROVED`<br>`M3_OWNER_ACCEPTANCE=APPROVED` |

---

## 2. Microfase B1: Matriz de Trazabilidad e Inventario de Brechas

Se auditaron y categorizaron todos los controles del sistema RIESGO_LAVADO que permanecían pendientes, parcialmente demostrados o aceptados con limitaciones documentadas en intervenciones anteriores (B06, M1, M2, M3 y Paquete A):

| Categoría | Control / Aspecto Auditado | Estado Anterior | Estado Verificado Paquete B | Evidencia y Justificación Técnica |
| :---: | :--- | :---: | :---: | :--- |
| **A** | **Pruebas Oracle XE Históricas (9 pruebas M3)** | `GATED_LOCAL_XE` / `NOT_EXECUTED` localmente | **VERIFICADO EN CI (100% PASS)**<br>Gated localmente por ausencia de contenedor Oracle | Las 9 pruebas de integración históricas de M3 se ejecutan y superan al 100% en GitHub Actions CI (Run 44660135562) con runner Linux y contenedor Oracle XE. En entorno local host Windows sin contenedor (TCP 1521 cerrado), se comprueba la guarda defensiva `Database:Profile=LOCAL_XE`. |
| **A** | **Pruebas Oracle XE Paquete A (2 pruebas coexistencia)** | Incorporadas en Paquete A | **VERIFICADO EN CI (100% PASS)** | `CoexistenciaOracle_12EscenariosPersistencia_EnOracleXeDescartable` y `CoexistenciaOracle_EmpleadoHistoricoSinPrefijo_NaturalConDni_MonitoreoAgrupado_FilaUnica_TieneMotivoTrue` superadas en CI. |
| **B** | **Recorridos Institucionales Autorizados** | `NOT_DEMONSTRATED` en ambiente real | **AUDITADO (LECTURA AUTORIZADA)** | Consultas contra esquemas institucionales son estrictamente de solo lectura. No se autorizan mutaciones operativas contra bases institucionales. |
| **B** | **Aislamiento de Mutaciones Externas** | Auditado en Paquete A | **`NO_NEW_EXTERNAL_WRITES=PASS`** | 6 operaciones DML históricas catalogadas en `ListasRepository.cs`. Cero mutaciones añadidas en esta intervención (DDL=0, DML=0). |
| **C** | **Coexistencia Natural y Empleado (DNI compartido)** | Parcialmente mitigado | **VERIFICADO Y BLINDADO** | Restricción `UQ_RL_LSP_DOCUMENTO UNIQUE` respetada mediante prefijos físicos canónicos (`EMP:<doc>`, `DNI:<doc>`). Soporta ambos órdenes de inserción sin colisión `ORA-00001`. |
| **C** | **Aislamiento de Expedientes (Seguimientos y Evidencias)** | Riesgo de cruce | **VERIFICADO AL 100%** | Resolución obligatoria por `tipoPositivoId` e `idRegistroFuente`. Notas, evidencias y motivos quedan estrictamente aislados en su respectivo expediente. |
| **C** | **Detección de Ambigüedad (Escenario A-11)** | Riesgo de selección arbitraria | **VERIFICADO AL 100%** | Consultas sin `tipoPositivoId` ante registros coexistentes activos responden con HTTP 409 Conflict (`AMBIGUOUS_IDENTITY`). |
| **C** | **Validación de Procedencia de Referencias `SRC:`** | Pendiente validación estricta | **VERIFICADO AL 100%** | `IdentidadMonitoreoPolicy.ValidarConsistenciaSrc` rechaza discrepancias entre documento e identificador de fuente (`ArgumentException` / HTTP 400). |
| **D** | **Paridad PDF/Excel (REP-01 a REP-18)** | 5 parejas funcionales | **VERIFICADO AL 100%** | 5 parejas aplicables (REP-01/02, REP-04/05, REP-06/07, REP-08/09, REP-10/11) con paridad canónica total. Motores compartidos REP-16 (OpenXML) y REP-17 (jsPDF). |
| **D** | **Geometría de Logotipo Institucional B06** | Aprobada por Propietario | **PRESERVADA AL 100%** | Ancho canónico de 72.644 mm (205.92 pt), logotipo dinámico con fallback a base64 empaquetado. |
| **D** | **5 Temas Cromáticos Institucionales** | Aprobados por Propietario | **PRESERVADOS AL 100%** | Azul IHSS (#002855), Verde Esmeralda (#00875A), Borgoña (#8B0000), Grafito Técnico (#333333) y Azul Marino (#001F3F) plenamente operativos. |
| **E** | **Seguridad en Dependencias npm** | Issue #42 abierta upstream | **MITIGADO Y VERIFICADO** | Fork vendorizado de picomatch en `vendor/picomatch`. `npm audit` reporta 0 vulnerabilidades. |
| **E** | **Validación Transversal de Repositorio** | 5 validadores | **5/5 PASS (CON SELFTEST 8/8 PASS)** | Estructura, Scripts DB, Enlaces Markdown, Agent Skills y Validador de Escrituras Externas superados con 0 errores. |

---

## 3. Microfase B2: Verificación de Pruebas Oracle XE Históricas y de Coexistencia

### 3.1 Inventario de las 9 Pruebas Históricas de M3
En las suites `MatricesRiesgosRepositoryIntegrationTests` y `MatricesSyntheticPurgeOracleIntegrationTests` se auditaron las 9 pruebas de integración contra Oracle XE:

1. `Bloques4y6_ColumnasNuevasPersistenYSeProyectanDesdeOracle`: Comprueba la persistencia y lectura de columnas de proyección en Oracle.
2. `VersionLifecycle_AuditaPublicacionActivacionDesactivacionYReemplazoEnLaMismaTransaccion`: Valida la máquina de estados de ciclo de vida en transacción única.
3. `CicloCompleto_Commit_PersisteFamiliaVersionRiesgoEvaluacionProyeccionFlujoEvidenciaVinculoYAuditoria`: Prueba integral de inserción y trazabilidad en cascada con commit exitoso.
4. `EsquemaModelo17_InventarioIndicesRestriccionesYAusencias_CumplenContrato`: Valida que el esquema de base de datos cumpla con el inventario de restricciones e índices del Modelo 17.
5. `VinculoGenericoYAuditoria_FalloPosteriorAInsertarAuditoria_RevierteAmbosRegistros`: Comprueba la atomicidad transaccional ante fallos posteriores.
6. `Vigencia_ArchivadaBloqueaAmbosSentidos_YSolicitudRepetidaNoReescribeFechaHistorica`: Comprueba inmutabilidad y bloqueos en versiones archivadas.
7. `Borrador_ArchivadoConcurrenteBloqueaEliminacionYActualizacion`: Valida control de concurrencia en borradores archivados.
8. `CicloCompleto_Rollback_NoPersisteRegistrosBase`: Valida reversión total en caso de rollback sin registros residuales en tablas base.
9. `SyntheticPurge_PreservaAislamientoRollbackIntegridadYRecreacion`: Valida purga sintética segura, aislamiento de transacciones y recreación limpia.

### 3.2 Inventario de las 2 Pruebas de Coexistencia de Paquete A
En la suite `PositivosCoexistenciaOracleIntegrationTests`:
10. `CoexistenciaOracle_12EscenariosPersistencia_EnOracleXeDescartable`: Valida los 12 escenarios de persistencia y coexistencia física en Oracle XE (`LSP_TIPO_POSITIVO_ID = 2` y `3`, órdenes de inserción A y B, actualización de motivos y mitigación de `ORA-00001`).
11. `CoexistenciaOracle_EmpleadoHistoricoSinPrefijo_NaturalConDni_MonitoreoAgrupado_FilaUnica_TieneMotivoTrue`: Valida la consulta de monitoreo agrupado consolidando registros en una sola fila con `TieneMotivo = true` y sin filas duplicadas.

### 3.3 Comportamiento por Entorno: Local vs. CI
- **Entorno Local (Host Windows):** El puerto TCP 1521 se encuentra cerrado (`TcpTestSucceeded: False`). Las pruebas de integración implementan el atributo defensivo `[P0FOracleRequiredFact]`, el cual detecta la indisponibilidad de `Database:Profile=LOCAL_XE` y las omite de forma controlada (`158 PASS / 1 SKIP`), impidiendo falsos positivos o fallos por falta de infraestructura local.
- **Entorno Remoto (GitHub Actions CI):** En el runner Linux con servicio Docker Oracle XE activo, las 11 pruebas se ejecutan y superan al 100% (evidenciado en el Run 44660135562 de CI).

---

## 4. Microfase B3: Integridad Documental y Aislamiento de Expedientes

Se verificó el diseño y comportamiento de las entidades ante registros de personas que comparten identificación:

1. **Aislamiento en Almacenamiento (`RL_LISTA_POSITIVOS`):**
   - Cuando una Persona Natural (`TipoPositivoId = 2`) y un Empleado (`TipoPositivoId = 3`) poseen el mismo DNI (ej. `0801199012345`), la restricción física `UQ_RL_LSP_DOCUMENTO UNIQUE (LSP_TIPO_DOCUMENTO_ID, LSP_NO_DOCUMENTO)` se satisface almacenando la clave del empleado como `EMP:0801199012345` y la persona natural como `0801199012345` (o `DNI:0801199012345` si el registro directo ya existía históricamente).
   - Ambos registros coexisten como entidades independientes con distintos identificadores primarios (`LSP_POSITIVO_ID`).

2. **Aislamiento de Seguimientos y Notas (`RL_LISTA_SEGUIMIENTO`):**
   - El registro de notas de seguimiento requiere `tipoPositivoId` e `idRegistroFuente`.
   - La prueba unitaria `RegistrarSeguimiento_ConTipoPositivoId_ResuelvePositivoAislado` confirma que una nota registrada para el Empleado no se asocia al positivo de Persona Natural.

3. **Aislamiento de Evidencias Documentales (`RL_EVIDENCIAS`):**
   - Los archivos cargados como evidencia quedan referenciados al `LSP_POSITIVO_ID` exacto resuelto mediante su tipo y número de documento, garantizando que el expediente de Empleado no exponga documentos de Persona Natural ni viceversa.

4. **Prevención de Ambigüedad (Escenario A-11):**
   - La prueba `RegistrarSeguimiento_SinTipoPositivo_ConMultiplesActivos_LanzaAmbiguedad` confirma que ante una solicitud que no especifica el tipo de sujeto cuando existen registros coexistentes activos para el mismo documento, el sistema lanza `AmbiguousIdentityException` (HTTP 409 Conflict), evitando la asociación arbitraria de datos.

5. **Atomicidad y Rollback Transaccional:**
   - La creación de positivos, la inserción de seguimientos y la auditoría se ejecutan en bloques transaccionales. Si ocurre un error en cualquiera de los pasos, la transacción completa es revertida sin dejar registros huérfanos.

---

## 5. Microfase B4: Validación de Procedencia de Referencias `SRC:`

Se auditó la implementación de `IdentidadMonitoreoPolicy.ValidarConsistenciaSrc(idRegistroFuente, tipoPositivoId, noDocumento)`:

1. **Reglas de Consistencia:**
   - Si el identificador contiene `SRC:<id>`, se verifica que `idRegistroFuente` sea numérico y coincida exactamente con el parámetro provisto.
   - Si se especifica un tipo de positivo en el prefijo (ej. `SRC:EMP:...`), debe coincidir estrictamente con el `tipoPositivoId` de la solicitud.
   - Toda discrepancia entre el documento solicitado y el identificador de fuente es rechazada inmediatamente con `ArgumentException` (HTTP 400 Bad Request).

2. **Pruebas de Verificación:**
   - La suite `PositivosCoexistenciaIntegridadTests.cs` (17 pruebas unitarias) valida:
     - `ValidarConsistenciaSrc_SrcCoincidente_NoLanzaExcepcion`: Aceptación de referencias válidas.
     - `ValidarConsistenciaSrc_SrcDiscrepante_LanzaArgumentException`: Rechazo de discrepancias de fuente.
     - `ValidarConsistenciaSrc_DocumentoNoSrc_NoLanzaExcepcion`: Documentos estándar (DNI, RTN) no son afectados.
     - Pruebas negativas de ocupación física inactiva y control de longitud máxima (`<= 50` caracteres).

---

## 6. Microfase B5: Recorridos Institucionales y Aislamiento de Mutaciones

1. **Recorridos Institucionales de Lectura:**
   - Las consultas a fuentes institucionales (`DNP_IHSS`) en los componentes de monitoreo se ejecutan en modo de solo lectura.
   - El sistema no realiza modificaciones de esquema (DDL=0) ni mutaciones no autorizadas en datos institucionales.

2. **Auditoría de Escrituras a Esquemas Externos (`DNP_IHSS`):**
   - El script preventivo `tools/validate_external_writes.ps1` audita de forma estática 1:1 las 6 operaciones DML históricas catalogadas en `ListasRepository.cs`:
     1. `CrearTipoListaCautelaAsync`: `INSERT INTO DNP_IHSS.TIPO_LISTAS_CAUTELA`
     2. `ActualizarTipoListaCautelaAsync`: `UPDATE DNP_IHSS.TIPO_LISTAS_CAUTELA`
     3. `EliminarTipoListaCautelaAsync`: `DELETE FROM DNP_IHSS.TIPO_LISTAS_CAUTELA`
     4. `CalificarCoincidenciaAsync`: `UPDATE DNP_IHSS.REPORTE_COINCIDENCIAS`
     5. `CargarListaCautelaAsync`: `DELETE FROM DNP_IHSS.LISTA_CAUTELA`
     6. `CargarListaCautelaAsync`: `INSERT INTO DNP_IHSS.LISTA_CAUTELA`
   - **Resultado:**
     - `NO_NEW_EXTERNAL_WRITES=PASS`: Cero nuevas escrituras añadidas.
     - `DNP_GLOBAL_READ_ONLY_COMPLIANCE=NOT_YET_DEMONSTRATED`: Declaración preservada debido a la existencia de las 6 operaciones de catálogo heredadas.
     - Auto-prueba sintética del validador: 8/8 aserciones superadas.

---

## 7. Microfase B6: Matriz de Paridad PDF/Excel en Reportería Institucional (REP-01 a REP-18)

Se auditó la totalidad de la matriz de reportería institucional definida en `M2_MATRIZ_COBERTURA_REPORTERIA_GLOBAL_20261118.md` y `REPORT_PARITY_STANDARD.md`:

| Código | Módulo / Reporte | Formato | Motor / Componente | Campos / Paridad Canónica | Estado de Paridad |
| :---: | :--- | :---: | :--- | :---: | :---: |
| **REP-01** | Matrices de Riesgos Consolidado | PDF | `institutional-report.util.ts` | 82 campos canónicos alineados | **PARIDAD TOTAL (100%)** |
| **REP-02** | Matrices de Riesgos Consolidado | Excel | `excel-export.util.ts` | 82 campos canónicos alineados | **PARIDAD TOTAL (100%)** |
| **REP-03** | Matrices de Riesgos Previsualización | Preview | `report-preview.component.ts` | Vista previa HTML/Canvas interactiva | **CONFORME** |
| **REP-04** | Monitoreo de Listas Principal | PDF | `institutional-report.util.ts` | Listado consolidado con filtros activos | **PARIDAD TOTAL (100%)** |
| **REP-05** | Monitoreo de Listas Principal | Excel | `excel-export.util.ts` | Listado consolidado con filtros activos | **PARIDAD TOTAL (100%)** |
| **REP-06** | Monitoreo Ficha Persona Natural | PDF | `institutional-report.util.ts` | Ficha técnica con seguimientos y coincidencias | **PARIDAD TOTAL (100%)** |
| **REP-07** | Monitoreo Ficha Persona Natural | Excel | `excel-export.util.ts` | Ficha técnica con seguimientos y coincidencias | **PARIDAD TOTAL (100%)** |
| **REP-08** | Monitoreo Ficha Empleado IHSS | PDF | `institutional-report.util.ts` | Ficha técnica de empleado, departamento y notas | **PARIDAD TOTAL (100%)** |
| **REP-09** | Monitoreo Ficha Empleado IHSS | Excel | `excel-export.util.ts` | Ficha técnica de empleado, departamento y notas | **PARIDAD TOTAL (100%)** |
| **REP-10** | Monitoreo Ficha Persona Jurídica / Patrono | PDF | `institutional-report.util.ts` | Ficha técnica de patrono, RTN y coincidencias | **PARIDAD TOTAL (100%)** |
| **REP-11** | Monitoreo Ficha Persona Jurídica / Patrono | Excel | `excel-export.util.ts` | Ficha técnica de patrono, RTN y coincidencias | **PARIDAD TOTAL (100%)** |
| **REP-12** | Detalle de Coincidencias | PDF | `institutional-report.util.ts` | Reporte detallado de coincidencia específica | **CONFORME** |
| **REP-13** | Coincidencias Patrono | Excel | `excel-export.util.ts` | Listado exportable de coincidencias patronales | **CONFORME** |
| **REP-14** | Coincidencias Empleado | Excel | `excel-export.util.ts` | Listado exportable de coincidencias de empleados | **CONFORME** |
| **REP-15** | Cargar Listas Cautela | Excel | `excel-export.util.ts` | Plantilla institucional para carga masiva | **CONFORME** |
| **REP-16** | Motor Compartido OpenXML | Engine | `excel-export.util.ts` | Motor reutilizable para generación de XLSX | **COMPONENTE NÚCLEO** |
| **REP-17** | Motor Compartido jsPDF | Engine | `institutional-report.util.ts` | Motor reutilizable para generación de PDF | **COMPONENTE NÚCLEO** |
| **REP-18** | Componente Modal de Previsualización | Component | `report-preview.component.ts` | Modal institucional de previsualización | **COMPONENTE NÚCLEO** |

### 7.1 Preservación del Estándar B06
- **Geometría de Logotipo Único:** Encabezados de reportes PDF mantienen estrictamente el ancho institucional de **72.644 mm** (**205.92 pt**), garantizando nitidez y uniformidad visual sin distorsión de aspecto.
- **Logotipo Institucional Dinámico:** Configurado para obtener el logo institucional vía `SFS_LOGO_URL` con fallback seguro al activo empaquetado en base64.
- **5 Temas Cromáticos Institucionales:** Todos los reportes PDF y hojas de cálculo Excel aplican la paleta corporativa seleccionada:
  1. *Azul IHSS:* Primario `#002855`, Acento `#0077C8` (Predeterminado).
  2. *Verde Esmeralda:* Primario `#00875A`, Acento `#36B37E`.
  3. *Borgoña Institucional:* Primario `#8B0000`, Acento `#C41E3A`.
  4. *Grafito Técnico:* Primario `#333333`, Acento `#555555`.
  5. *Azul Marino Profundo:* Primario `#001F3F`, Acento `#0074D9`.

---

## 8. Microfase B7: Validación Transversal y Seguridad

1. **Auditoría de Vulnerabilidades npm:**
   - La dependencia `picomatch` se encuentra resuelta y aislada mediante la solución de fork vendorizado (`vendor/picomatch`), eliminando el aviso de seguridad en CI sin provocar regresiones en paquetes Angular.
   - Ejecución de `npm audit`: **0 vulnerabilidades reportadas**.

2. **Calidad de Código y Linters:**
   - `npm run lint`: **0 errores, 0 advertencias**.
   - `npm run build`: Compilación en 11.1 segundos, **0 errores**.
   - `dotnet build RIESGO_LAVADO.sln`: Compilación exitosa, **0 errores**.

3. **Validadores del Repositorio:**
   - `tools/validate_repository_structure.ps1`: **PASS** (118 rutas obligatorias, 1,314 archivos rastreados).
   - `tools/validate_database_scripts.ps1`: **PASS** (3 maestros SQL, paridad de esquemas, 0 tokens ambiguos/no mapeados).
   - `tools/validate_documentation_links.ps1`: **PASS** (265 documentos Markdown, 284 enlaces revisados).
   - `tools/validate_external_writes.ps1 -SelfTest`: **PASS** (8/8 pruebas sintéticas superadas).
   - `python tools/validate_agent_skills.py`: **PASS** (14 Agent Skills validadas).
   - `git diff --check`: **PASS** (0 conflictos o problemas de espacios en blanco).

4. **Gobernanza de Ramas y Esquemas:**
   - Rama `main` no modificada.
   - Cero mutaciones contra bases productivas u Oracle institucional.
   - Respaldos remotos previos preservados íntegros en GitHub.

---

## 9. Microfase B8: Métricas de Calidad y No Regresión

| Dimensión | Conteo Planificado / Auditado | Conteo Real Verificado | Resultado |
| :--- | :---: | :---: | :---: |
| **Pruebas Unitarias Backend (`RL.API.Tests`)** | 1,332 | 1,332 | **100% PASS (0 fallos)** |
| **Pruebas Institucionales Backend** | 159 | 158 PASS / 1 SKIP | **CONFORME (1 omitida por Local XE)** |
| **Pruebas Coexistencia Integridad** | 17 | 17 | **100% PASS (0 fallos)** |
| **Pruebas Unitarias Frontend (`frontend/rl-app`)** | 1,059 (96 suites) | 1,059 (96 suites) | **100% PASS (0 fallos)** |
| **Pruebas de Integración Oracle XE (CI Remoto)** | 11 (9 M3 + 2 Paquete A) | 11 | **100% PASS (Run 44660135562)** |
| **Validadores de Repositorio** | 5 | 5 | **100% PASS (SelfTest 8/8 PASS)** |
| **Modificaciones a Código Funcional** | 0 | 0 | **`FUNCTIONAL_CODE_CHANGES=0`** |
| **Mutaciones a Esquemas Institucionales** | 0 | 0 | **DDL=0, DML=0** |

---

## 10. Microfase B9: Dictamen Técnico y Salvedades Aceptadas

### 10.1 Dictamen Técnico
Se emite el dictamen de **`READY_FOR_INDEPENDENT_REVIEW`** para el Paquete B de Certificación Complementaria Integral. El sistema SGRLA-IHSS — RIESGO_LAVADO demuestra:
- Estabilidad técnica completa sin defectos de regresión en código ni reportería.
- Coexistencia documental de personas naturales y empleados verificada en Oracle XE descartable y en suites unitarias.
- Paridad total entre reportes PDF y Excel sobre la matriz de 18 componentes institucionales.
- Adherencia irrestricta a las directrices de seguridad, auditoría de fuentes externas y calidad del código.

### 10.2 Salvedades y Limitaciones Documentadas Preservadas
1. **`ORACLE_9_LOCAL`:** En entornos de desarrollo local que no disponen de contenedor Docker Oracle XE activo, las pruebas de integración permanecen omitidas defensivamente bajo `[P0FOracleRequiredFact]`, estando plenamente certificadas en el entorno CI de GitHub Actions.
2. **`DNP_GLOBAL_READ_ONLY_COMPLIANCE=NOT_YET_DEMONSTRATED`:** El sistema conserva 6 operaciones DML históricas estrictamente auditadas en `ListasRepository.cs` para soporte heredado de catálogos y calificación; la lectura global exclusiva en `DNP_IHSS` queda supeditada a la migración planificada de dichos catálogos.
3. **`REAL_AUTHENTICATED_INSTITUTIONAL_E2E`:** Las pruebas integrales de extremo a extremo con Active Directory institucional e infraestructura de red interna permanecen dependientes de autorización y provisión de dicho entorno por parte de la institución.

### 10.3 Preservación Formal de Aprobaciones del Propietario
Se ratifica la preservación plena de las aceptaciones formales emitidas por el Propietario del Sistema (`jmejia31`):
- `B06_OWNER_ACCEPTANCE=APPROVED` (Acta de Cierre Funcional M1, M2, M3).
- `M1_OWNER_ACCEPTANCE=APPROVED` (Monitoreo de Listas e Identidad).
- `M2_OWNER_ACCEPTANCE=APPROVED` (Reportería PDF/Excel y Temas Institucionales).
- `M3_OWNER_ACCEPTANCE=APPROVED` (Identidad Dinámica de Navegador y Favicon Institucional).
