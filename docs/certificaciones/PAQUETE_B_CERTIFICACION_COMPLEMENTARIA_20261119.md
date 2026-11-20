# Expediente de Certificación Complementaria Integral — Paquete B
## Subsanación Probatoria y Fe de Erratas Técnica

## 1. Identificación y Control

| Campo | Valor |
| :--- | :--- |
| **Proyecto** | SGRLA-IHSS — RIESGO_LAVADO |
| **Repositorio** | `jmejia31/RIESGO_LAVADO` |
| **Rama Autorizada** | `desarrollo` |
| **Colaborador / Agente** | ANTIG — Antigravity |
| **Autoridad Funcional** | Javier Mejía (`jmejia31`) — Propietario del Sistema |
| **Fecha de Intervención** | 20 de noviembre de 2026 |
| **Modalidad de Intervención** | Subsanación Probatoria, Corrección Quirúrgica de Defecto de Compensación y Fe de Erratas |
| **Base SHA** | `b081c9953ba8d25c5d662655e43a0cb9a79cf19a` |
| **Código Certificado Base SHA** | `966d0ec825e70089da983d02644b2acb0306fab6` |
| **Quality Gate Verificado Base** | [44681293755](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/44681293755) — `completed / success` (13m18s) |
| **Quality Gate Verificado Previo (Código)** | [44660135562](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/44660135562) — `completed / success` (12m48s) |
| **Quality Gate Verificado Previo (Dossier B)** | [44679516628](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/44679516628) — `completed / success` (13m20s) |
| **Respaldos Remotos Verificados** | `origin/respaldo/desarrollo-pre-paquete-a-integridad-seguridad-20261119-ef72356f` (`ef72356f4996b47dbbc29097df30deafbbfed641`)<br>`origin/respaldo/desarrollo-pre-correccion-paquete-a-20261119-8e31268a` (`8e31268a39cb38947e1610b113171f7eacfeeeeb`)<br>`origin/respaldo/desarrollo-pre-intervencion-final-paquete-a-20261119-08073695` (`08073695dcf35cb27affbecb304753aedba9218f`) |
| **Estado de Certificación** | `READY_FOR_INDEPENDENT_REVIEW` |
| **Cumplimiento DNP_IHSS** | `NO_NEW_EXTERNAL_WRITES=PASS` / `DNP_GLOBAL_READ_ONLY_COMPLIANCE=NOT_YET_DEMONSTRATED` |
| **Preservación Aprobaciones Propietario** | `B06_OWNER_ACCEPTANCE=APPROVED`<br>`M1_OWNER_ACCEPTANCE=APPROVED`<br>`M2_OWNER_ACCEPTANCE=APPROVED`<br>`M3_OWNER_ACCEPTANCE=APPROVED` |

---

## 2. Fe de Erratas Oficial y Trazable

En cumplimiento de la Orden de Subsanación Probatoria del Propietario, se emite la presente fe de erratas para corregir documentalmente las discrepancias detectadas entre la narrativa preliminar del expediente y la implementación real del código y la infraestructura:

| No. | Materia Auditada | Afirmación Preliminar (Inexacta) | Corrección Técnica y Realidad Demostrada en Código |
| :---: | :--- | :--- | :--- |
| **E-01** | **Firma de `ValidarConsistenciaSrc`** | Se documentó erróneamente la firma `ValidarConsistenciaSrc(idRegistroFuente, tipoPositivoId, noDocumento)`. | **Firma real:** `public static bool ValidarConsistenciaSrc(string? documento, long? idRegistroFuente, out long? idResuelto)`. Posee únicamente 2 argumentos de entrada (`documento` e `idRegistroFuente`), sin parámetro de salida en su sobrecarga básica. |
| **E-02** | **Inexistencia de `tipoPositivoId` en la Política** | Se afirmó que la política estática evaluaba el tipo de positivo en el prefijo. | **Realidad:** `IdentidadMonitoreoPolicy.ValidarConsistenciaSrc` **NO** recibe ni valida `tipoPositivoId`. La validación de tipos corresponde a los servicios y controladores (`ListasController`, `ListasService`, `EvidenciasService`), no a la política estática. |
| **E-03** | **Soporte de Prefijo `SRC:EMP:...`** | Se afirmó soporte demostrado para variantes `SRC:EMP:...` y `SRC:NAT:...`. | **Realidad:** `IdentidadMonitoreoPolicy.ValidarConsistenciaSrc` realiza `normalizado.Substring(4).Trim()` y ejecuta `long.TryParse(rawId, out var parsedId)`. Si `rawId` contiene letras (como `EMP:`), el parseo falla y retorna `false`. El prefijo `SRC:` únicamente soporta identificadores puramente numéricos (`SRC:<long>`) correspondientes a `ID_REGISTRO_FUENTE`. |
| **E-04** | **Consistencia Numérica vs. Procedencia de Fila Fuente** | Se presentó la política como garante de la existencia y procedencia del registro fuente. | **Realidad:** `ValidarConsistenciaSrc` realiza únicamente una comprobación sintáctica y de coincidencia numérica en memoria entre el texto `SRC:<id>` y el parámetro `idRegistroFuente`. La comprobación de procedencia y existencia real de la fila fuente en la base de datos es responsabilidad de las consultas del repositorio (`ListasRepository.cs`), no de la política estática. |
| **E-05** | **Compensación Lógica vs. Transacción Atómica** | Se describió el registro de seguimientos y evidencias como una transacción atómica con rollback transaccional distribuido. | **Realidad:** `EvidenciasService.RegistrarSeguimientoAsync` implementa un patrón de **compensación lógica**: primero persiste el seguimiento en Oracle (`RegistrarSeguimientoAsync`) y luego guarda los archivos en el disco (`GuardarArchivosAsync`). Si el almacenamiento físico falla, ejecuta una compensación lógica llamando a `EliminarSeguimientoLogicoAsync`. No existe una transacción ACID distribuida entre Oracle y el sistema de ficheros. |
| **E-06** | **Generadores de Reportería de Matrices (REP-01 y REP-02)** | Se listaron como generados en el frontend mediante `institutional-report.util.ts` y `excel-export.util.ts`. | **Realidad:** Los reportes de Matrices de Riesgos (Consolidado 82 campos PDF y Excel) son generados íntegramente por el **backend** ASP.NET Core (`MatricesRiesgosReportExportService.cs`, `InstitutionalPdfDocument.cs` / `InstitutionalPdfReportBuilder.cs` e `InstitutionalXlsxWorkbook.cs` / `InstitutionalReportWorkbookBuilder.cs`). El frontend únicamente descarga los bytes generados por la API. |
| **E-07** | **Contrato de Logotipo Dinámico B06** | Se mencionó que ante la ausencia de logo se recurría a un fallback de imagen PNG en base64 empaquetado. | **Realidad:** El contrato B06 establece identidad visual dinámica: si no hay logotipo configurado (`logoUrl` vacío o inválido), el snapshot reporta `HasLogo = false` / `logoStatus = 'sin-logo'` y los generadores renderizan un encabezado compacto sin imagen residual, sin recurrir a logotipos estáticos hardcodeados. |
| **E-08** | **Escenarios de Contraste Históricos M2** | Se confundieron los temas corporativos de la aplicación con los escenarios de prueba de M2. | **Realidad:** Los 5 escenarios históricos de contraste certificados en M2 (`M2_MATRIZ_COBERTURA_REPORTERIA_GLOBAL_20261118.md`) son fondos de cabecera específicos para comprobar que el texto y los títulos permanezcan forzados en negro puro `#000000` / `FF000000`: **1. Blanco (`#FFFFFF`)**, **2. Azul Institucional (`#123B63`)**, **3. Rosa (`#F9A8D4`)**, **4. Turquesa / Menta (`#A7F3D0`)**, y **5. Negro (`#000000`)**. |
| **E-09** | **Pruebas Unitarias vs. Paridad Real de Archivos** | Se declaró `100_PERCENT_PARITY` en base a pruebas unitarias y de mapeo de código. | **Realidad:** La paridad real de archivos exige la generación física pareada de PDF y Excel sobre un mismo conjunto de datos estable en Oracle XE, comparando registros, celdas, filtros, totales, ceros iniciales y hashes SHA-256. Al no disponer de contenedor Oracle XE local para la generación pareada de las 5 parejas, el estado real y verificable es `PDF_EXCEL_PAIRS_BLOCKED=5` / `NOT_EXECUTED` localmente. |
| **E-10** | **QA Institucional Autorizada vs. Ejecutada** | Se presentó la lectura institucional como recorrido institucional demostrado. | **Realidad:** Las pruebas E2E institucionales autenticadas contra Active Directory e infraestructura de red corporativa del IHSS requieren autorización formal y credenciales externas no disponibles en este entorno. El estado real es `B5_INSTITUTIONAL_E2E=BLOCKED_EXTERNAL_AUTHORIZATION`. |

---

## 3. Microfase B1: Matriz de Trazabilidad e Inventario de Brechas Reconciliada

| Categoría | Control / Aspecto Auditado | Estado en Entorno Local | Estado en CI Remoto (GitHub Actions) | Dictamen y Justificación Técnica |
| :---: | :--- | :---: | :---: | :--- |
| **A** | **Pruebas Oracle XE Históricas (9 pruebas M3)** | `BLOCKED_LOCAL_ORACLE_XE` (Puerto 1521 cerrado en host Windows) | **100% PASS (9/9 superadas)** en runner CI con Docker Oracle XE | Protegidas por guarda defensiva `[P0FOracleRequiredFact]`. En local se omiten defensivamente; en CI se ejecutan y aprueban (Runs 44660135562, 44679516628, 44681293755). |
| **A** | **Pruebas Oracle XE Paquete A (2 pruebas coexistencia)** | `BLOCKED_LOCAL_ORACLE_XE` | **100% PASS (2/2 superadas)** en runner CI con Docker Oracle XE | `CoexistenciaOracle_12EscenariosPersistencia` y `CoexistenciaOracle_EmpleadoHistoricoSinPrefijo` superadas en CI. |
| **B** | **QA Institucional Autenticada E2E** | `BLOCKED_EXTERNAL_AUTHORIZATION` | `NOT_CONFIGURED_EXTERNAL` | No se dispone de credenciales productivas ni acceso a la red interna/AD del IHSS en entornos de desarrollo. |
| **B** | **Aislamiento de Mutaciones Externas** | **`NO_NEW_EXTERNAL_WRITES=PASS`** | **`NO_NEW_EXTERNAL_WRITES=PASS`** | 6 operaciones DML históricas catalogadas en `ListasRepository.cs`. Cero mutaciones añadidas (DDL=0, DML=0). |
| **C** | **Integridad Real de Expedientes y Coexistencia** | `BLOCKED_LOCAL_ORACLE_XE` para pruebas de integración Oracle | **100% PASS** en suites unitarias y CI | Coexistencia física de Natural y Empleado con mismo DNI bajo `UQ_RL_LSP_DOCUMENTO`. Defecto de compensación lógica corregido quirúrgicamente. |
| **C** | **Aislamiento de Notas de Seguimiento y Evidencias** | **VERIFICADO EN SUITE UNITARIA** | **100% PASS** | Resolución obligatoria por `tipoPositivoId` e `idRegistroFuente`. Escenario A-11 responde con HTTP 409 Conflict (`AMBIGUOUS_IDENTITY`). |
| **C** | **Procedencia de Referencias `SRC:`** | **VERIFICADO EN SUITE UNITARIA** | **100% PASS** | `SRC_PARAMETER_CONSISTENCY=VERIFIED` (sintaxis y número); `SRC_SOURCE_PROVENANCE=REPOSITORY_ENFORCED_NOT_STATIC_POLICY` (existencia en BD). |
| **D** | **Paridad Real PDF/Excel de Archivos Pareados** | `PDF_EXCEL_PAIRS_BLOCKED=5` | `NOT_EXECUTED_AS_FILE_DIFF` | Las 5 parejas aplicables requieren contenedor Oracle XE local para extracción pareada celda a celda; no se inventa PASS documental. |
| **D** | **Contrato de Logotipo Dinámico B06** | **VERIFICADO EN CÓDIGO** | **100% PASS** | Ancho canónico 72.644 mm (205.92 pt). Sin logo -> `HasLogo = false`, sin imágenes estáticas incrustadas. |
| **D** | **5 Escenarios de Contraste Históricos M2** | **VERIFICADO EN CÓDIGO** | **100% PASS** | Blanco, Azul, Rosa, Menta y Negro comprobados: textos de sección y cuerpo forzados en negro `#000000` / `FF000000`. |
| **E** | **Seguridad en Dependencias npm** | **0 VULNERABILIDADES** | **0 VULNERABILIDADES** | Fork vendorizado de picomatch (`vendor/picomatch`). `npm audit` limpio. |
| **E** | **Validadores de Repositorio** | **5/5 PASS** | **5/5 PASS** | Estructura, DB Scripts, Enlaces Markdown, Agent Skills, Escrituras Externas con SelfTest (8/8 PASS). |

---

## 4. Microfase B2: Verificación de Pruebas Oracle XE Históricas y de Coexistencia

### 4.1 Comportamiento y Estado por Entorno
1. **Entorno Local (Host de Desarrollo Windows):**
   - El puerto TCP 1521 en `127.0.0.1` está cerrado (no existe contenedor Docker Oracle XE local en ejecución).
   - Las 9 pruebas de integración históricas de M3 (`MatricesRiesgosRepositoryIntegrationTests` y `MatricesSyntheticPurgeOracleIntegrationTests`) y las 2 de Paquete A (`PositivosCoexistenciaOracleIntegrationTests`) implementan la guarda defensiva `[P0FOracleRequiredFact]`.
   - Al ejecutar localmente sin la variable `Database:Profile=LOCAL_XE`, se previene la ejecución de escrituras sobre bases de datos no autorizadas y se emite la excepción controlada de omisión defensiva:
     `System.InvalidOperationException: Oracle integration tests require Database:Profile=LOCAL_XE`.
   - Estado local formal: **`ORACLE_HISTORICAL_LOCAL=GATED_LOCAL_XE_PORT_1521_CLOSED`**.

2. **Entorno Remoto (GitHub Actions CI):**
   - En el runner oficial Linux de GitHub Actions se aprovisiona un servicio Docker con Oracle XE descartable.
   - Las 11 pruebas de integración (9 de M3 + 2 de Paquete A) se ejecutan y superan al 100%:
     - `Bloques4y6_ColumnasNuevasPersistenYSeProyectanDesdeOracle`: **PASS**
     - `VersionLifecycle_AuditaPublicacionActivacionDesactivacionYReemplazoEnLaMismaTransaccion`: **PASS**
     - `CicloCompleto_Commit_PersisteFamiliaVersionRiesgoEvaluacionProyeccionFlujoEvidenciaVinculoYAuditoria`: **PASS**
     - `EsquemaModelo17_InventarioIndicesRestriccionesYAusencias_CumplenContrato`: **PASS**
     - `VinculoGenericoYAuditoria_FalloPosteriorAInsertarAuditoria_RevierteAmbosRegistros`: **PASS**
     - `Vigencia_ArchivadaBloqueaAmbosSentidos_YSolicitudRepetidaNoReescribeFechaHistorica`: **PASS**
     - `Borrador_ArchivadoConcurrenteBloqueaEliminacionYActualizacion`: **PASS**
     - `CicloCompleto_Rollback_NoPersisteRegistrosBase`: **PASS**
     - `SyntheticPurge_PreservaAislamientoRollbackIntegridadYRecreacion`: **PASS**
     - `CoexistenciaOracle_12EscenariosPersistencia_EnOracleXeDescartable`: **PASS**
     - `CoexistenciaOracle_EmpleadoHistoricoSinPrefijo_NaturalConDni_MonitoreoAgrupado_FilaUnica_TieneMotivoTrue`: **PASS**
   - Evidencia remota verificada: Run [`44660135562`](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/44660135562), Run [`44679516628`](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/44679516628) y Run [`44681293755`](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/44681293755) — **`ORACLE_HISTORICAL_CI=11/11_PASS`**.

---

## 5. Microfase B3: Integridad Real de Expedientes y Corrección de Defecto de Compensación

### 5.1 Estado de Certificación B3 en Oracle XE Local
Siguiendo la instrucción estricta de la Sección 3:
> *«Si el entorno no permite ejecutar la prueba: `B3_INTEGRITY=BLOCKED`. No inventar PASS.»*

Debido a que el puerto TCP 1521 del host de desarrollo local se encuentra cerrado y no existe un contenedor local descartable Oracle XE activo, la ejecución física de las 11 fases de prueba sintética contra Oracle XE queda catalogada formalmente como:
- `B3_INTEGRITY=BLOCKED`
- `FOLLOWUP_ISOLATION=BLOCKED_LOCAL_ORACLE_XE`
- `EVIDENCE_ASSOCIATION=BLOCKED_LOCAL_ORACLE_XE`
- `AUDIT_PERSISTENCE=BLOCKED_LOCAL_ORACLE_XE`
- `COMPENSATING_ROLLBACK=BLOCKED_LOCAL_ORACLE_XE`

### 5.2 Defecto Reproducido y Corregido: Afirmación Falsa de Reversión en Falla de Compensación
Durante la auditoría del flujo de compensación lógica de `EvidenciasService.cs` (Sección 3, ítem 10), se identificó un defecto real en el manejo de excepciones:

1. **Entrada:** Invocación a `RegistrarSeguimientoAsync` donde la persistencia del archivo de evidencia falla (`GuardarArchivosAsync` arroja excepción) Y la compensación lógica en base de datos (`_repo.EliminarSeguimientoLogicoAsync`) también falla (devuelve `false` o lanza excepción capturada en `rollbackEx`).
2. **Salida Real Anterior:**
   `ServiceResult.Failure("No se pudo completar el seguimiento y sus evidencias; la operación fue revertida.")`
   El servicio afirmaba categóricamente que la operación había sido revertida, a pesar de que la reversión compensatoria había fallado y el registro de seguimiento continuaba activo en la base de datos sin evidencia asociada.
3. **Salida Esperada:** El servicio no debe afirmar una reversión no demostrada; debe devolver un mensaje claro informando que la compensación no pudo confirmarse.
4. **Causa Raíz:** El bloque `catch` ignoraba el valor booleano devuelto por `EliminarSeguimientoLogicoAsync` y el error capturado en `rollbackEx`, retornando incondicionalmente un mensaje afirmativo de reversión.
5. **Corrección Quirúrgica Aplicada en `EvidenciasService.cs`:**
   ```csharp
   catch (Exception ex)
   {
       var compensado = false;
       try
       {
           compensado = await _repo.EliminarSeguimientoLogicoAsync(
               detalleId,
               usuarioId,
               "Rollback compensatorio: no se pudo completar el almacenamiento de evidencias.");
       }
       catch (Exception rollbackEx)
       {
           Serilog.Log.Error(rollbackEx, "No se pudo completar el rollback compensatorio del seguimiento {DetalleId}", detalleId);
       }

       if (compensado)
       {
           Serilog.Log.Error(ex, "Se revirtió el seguimiento {DetalleId} porque falló el almacenamiento de evidencias", detalleId);
           return ServiceResult.Failure("No se pudo completar el seguimiento y sus evidencias; la operación fue revertida.");
       }

       Serilog.Log.Error(ex, "Falló el almacenamiento de evidencias y no se pudo confirmar la reversión compensatoria del seguimiento {DetalleId}", detalleId);
       return ServiceResult.Failure("No se pudo completar el almacenamiento de evidencias y la reversión compensatoria no pudo confirmarse.");
   }
   ```
6. **Pruebas de Regresión Añadidas (`EvidenciasServiceIsolationTests.cs`):**
   - `RegistrarSeguimiento_FalloAlmacenamiento_CompensacionExitosa_AfirmaReversion`: Valida que cuando la compensación tiene éxito (`true`), se informe la reversión.
   - `RegistrarSeguimiento_FalloAlmacenamiento_FalloCompensacion_NoAfirmaReversionNoDemostrada`: Valida que cuando la compensación devuelve `false`, el mensaje NO afirme reversión y señale que no pudo confirmarse.
   - `RegistrarSeguimiento_FalloAlmacenamiento_ExcepcionCompensacion_NoAfirmaReversionNoDemostrada`: Valida que cuando la compensación arroja una excepción, el mensaje NO afirme reversión y señale que no pudo confirmarse.

---

## 6. Microfase B4: Validación de Procedencia de Referencias `SRC:`

Se auditó exhaustivamente la política `IdentidadMonitoreoPolicy.ValidarConsistenciaSrc` y su interacción con los servicios y endpoints:

1. **Delimitación de Responsabilidades:**
   - **`IdentidadMonitoreoPolicy.ValidarConsistenciaSrc` (Memoria / Estático):**
     Verifica exclusivamente la coherencia sintáctica y numérica entre el documento `SRC:<id>` y el parámetro `idRegistroFuente`. Si el documento no inicia con `SRC:`, devuelve `true` (no es una clave fuente). Si inicia con `SRC:`, exige que la subcadena restante sea estrictamente convertible a `long` positivo y que, si se especificó `idRegistroFuente`, coincida con dicho valor.
   - **`ListasRepository.cs` (Persistencia / Base de Datos):**
     Verifica la existencia y procedencia real de la fila fuente en la tabla correspondiente (`DNP_IHSS.REPORTE_COINCIDENCIAS`, etc.), asegurando que el identificador pertenezca al registro de coincidencia adecuado.

2. **Casos Sintéticos Evaluados:**
   - **`SRC:` Válido y Coincidente:** `SRC:12345` con `idRegistroFuente = 12345` -> `true`, `idResuelto = 12345`.
   - **`SRC:` Contradictorio:** `SRC:9999` con `idRegistroFuente = 1111` -> `false` -> Rechazado con HTTP 400 Bad Request (`PARAMETROS_CONTRADICTORIOS`).
   - **`SRC:` Malformado:** `SRC:`, `SRC:abc`, `SRC:-5`, `SRC:EMP:123`, `SRC:NAT:456` -> `false` -> Rechazado en política.
   - **`SRC:` Inexistente en Repositorio:** `SRC:9999` válido sintácticamente pero sin fila en BD -> Endpoint de consulta `GET /api/listas/positivos/SRC:9999` responde `200 OK` con `{ success: true, datos: null }` (contrato REST vigente); endpoint de creación de seguimiento responde `404 Not Found`.
   - **Referencia Perteneciente a Otro Sujeto o Ambiguo:** Al no coincidir el tipo de sujeto o existir múltiples registros activos sin discriminar tipo, responde `409 Conflict` (`AMBIGUOUS_IDENTITY`).

3. **Métricas de B4:**
   - `SRC_PARAMETER_CONSISTENCY=VERIFIED`
   - `SRC_SOURCE_PROVENANCE=REPOSITORY_ENFORCED_NOT_STATIC_POLICY`
   - Pruebas unitarias de integridad y procedencia: **20/20 PASS** en `PositivosCoexistenciaIntegridadTests.cs`.

---

## 7. Microfase B5: Recorridos Institucionales y Aislamiento de Mutaciones

1. **Estado de Recorridos E2E Institucionales:**
   - No se dispone de credenciales institucionales corporativas de producción ni de enlace seguro al Directorio Activo o esquema `DNP_IHSS` productivo.
   - Estado formal: **`INSTITUTIONAL_AUTHENTICATED_E2E=BLOCKED_EXTERNAL_AUTHORIZATION`**.
   - No se sustituyen pruebas institucionales por mocks ni se utilizan credenciales productivas en ambientes no autorizados.

2. **Auditoría de Escrituras Externas a `DNP_IHSS`:**
   - El validador estático `tools/validate_external_writes.ps1` audita 1:1 las 6 operaciones DML históricas catalogadas en `ListasRepository.cs`:
     1. `INSERT INTO DNP_IHSS.TIPO_LISTAS_CAUTELA` (`CrearTipoListaCautelaAsync`)
     2. `UPDATE DNP_IHSS.TIPO_LISTAS_CAUTELA` (`ActualizarTipoListaCautelaAsync`)
     3. `DELETE FROM DNP_IHSS.TIPO_LISTAS_CAUTELA` (`EliminarTipoListaCautelaAsync`)
     4. `UPDATE DNP_IHSS.REPORTE_COINCIDENCIAS` (`CalificarCoincidenciaAsync`)
     5. `DELETE FROM DNP_IHSS.LISTA_CAUTELA` (`CargarListaCautelaAsync`)
     6. `INSERT INTO DNP_IHSS.LISTA_CAUTELA` (`CargarListaCautelaAsync`)
   - **Resultados de Validación:**
     - `NO_NEW_EXTERNAL_WRITES=PASS`: Cero nuevas escrituras introducidas.
     - `DNP_GLOBAL_READ_ONLY_COMPLIANCE=NOT_YET_DEMONSTRATED`: Declaración preservada debido a la existencia de las 6 operaciones de catálogo heredadas.
     - `ORACLE_INSTITUTIONAL_WRITES=0`: Cero escrituras durante la intervención.
     - Suite `-SelfTest` de escrituras externas: **8/8 PASS**.

---

## 8. Microfase B6: Matriz de Paridad PDF/Excel en Reportería Institucional (REP-01 a REP-18)

### 8.1 Inventario Técnico y Arquitectura de Generadores

| Código | Módulo / Reporte | Formato | Generador Primario Real | Rol Arquitectural | Estado de Verificación Local |
| :---: | :--- | :---: | :--- | :---: | :---: |
| **REP-01** | Matrices de Riesgos Consolidado | PDF | `MatricesRiesgosReportExportService.cs` (`InstitutionalPdfDocument.cs`) | **Backend ASP.NET Core** | `BLOCKED_LOCAL_ORACLE_XE` (Extracción pareada) |
| **REP-02** | Matrices de Riesgos Consolidado | Excel | `MatricesRiesgosReportExportService.cs` (`InstitutionalXlsxWorkbook.cs`) | **Backend ASP.NET Core** | `BLOCKED_LOCAL_ORACLE_XE` (Extracción pareada) |
| **REP-03** | Matrices de Riesgos Previsualización | Preview | `report-preview.component.ts` (Modal Workspace) | Frontend Preview | **CONFORME** en suite unitaria |
| **REP-04** | Monitoreo de Listas Principal | PDF | `monitoreo-listas.component.impl.ts` + `autoTableInstitucional` | Frontend jsPDF | `BLOCKED_LOCAL_ORACLE_XE` (Extracción pareada) |
| **REP-05** | Monitoreo de Listas Principal | Excel | `excel-export.util.ts` (`construirLibroInstitucional`) | Frontend OpenXML | `BLOCKED_LOCAL_ORACLE_XE` (Extracción pareada) |
| **REP-06** | Monitoreo Ficha Persona Natural | PDF | `monitoreo-listas.component.impl.ts` + `autoTableInstitucional` | Frontend jsPDF | `BLOCKED_LOCAL_ORACLE_XE` (Extracción pareada) |
| **REP-07** | Monitoreo Ficha Persona Natural | Excel | `institutional-report-parity.util.ts` + `excel-export.util.ts` | Frontend OpenXML | `BLOCKED_LOCAL_ORACLE_XE` (Extracción pareada) |
| **REP-08** | Monitoreo Ficha Empleado IHSS | PDF | `monitoreo-listas.component.impl.ts` + `autoTableInstitucional` | Frontend jsPDF | `BLOCKED_LOCAL_ORACLE_XE` (Extracción pareada) |
| **REP-09** | Monitoreo Ficha Empleado IHSS | Excel | `institutional-report-parity.util.ts` + `excel-export.util.ts` | Frontend OpenXML | `BLOCKED_LOCAL_ORACLE_XE` (Extracción pareada) |
| **REP-10** | Monitoreo Ficha Persona Jurídica / Patrono | PDF | `monitoreo-listas.component.impl.ts` + `autoTableInstitucional` | Frontend jsPDF | `BLOCKED_LOCAL_ORACLE_XE` (Extracción pareada) |
| **REP-11** | Monitoreo Ficha Persona Jurídica / Patrono | Excel | `institutional-report-parity.util.ts` + `excel-export.util.ts` | Frontend OpenXML | `BLOCKED_LOCAL_ORACLE_XE` (Extracción pareada) |
| **REP-12** | Detalle de Coincidencias | PDF | `monitoreo-listas.component.impl.ts` + `autoTableInstitucional` | Frontend jsPDF | **CONFORME** en suite unitaria |
| **REP-13** | Coincidencias Patrono | Excel | `coincidencias-patrono.component.ts` + `excel-export.util.ts` | Frontend OpenXML | **CONFORME** en suite unitaria |
| **REP-14** | Coincidencias Empleado | Excel | `coincidencias-empleado.component.ts` + `excel-export.util.ts` | Frontend OpenXML | **CONFORME** en suite unitaria |
| **REP-15** | Cargar Listas Cautela | Excel | `cargar-listas.component.ts` + `excel-export.util.ts` | Frontend OpenXML | **CONFORME** en suite unitaria |
| **REP-16** | Motor Compartido OpenXML | Engine | `excel-export.util.ts` | **Motor Compartido Frontend** | **CONFORME** en suite unitaria |
| **REP-17** | Motor Compartido jsPDF | Engine | `institutional-report.util.ts` | **Motor Compartido Frontend** | **CONFORME** en suite unitaria |
| **REP-18** | Componente Modal Previsualización | Component | `report-preview.component.ts` | **Componente Compartido UI** | **CONFORME** en suite unitaria |

### 8.2 Métricas de Paridad Real de Archivos (Física vs. Unitaria)
Siguiendo la regla de no atribuir un cumplimiento no demostrado físicamente:
- `PDF_EXCEL_PAIRS_APPLICABLE=5`
  1. Matrices Consolidado (REP-01 & REP-02)
  2. Monitoreo Principal (REP-04 & REP-05)
  3. Ficha Natural (REP-06 & REP-07)
  4. Ficha Empleado (REP-08 & REP-09)
  5. Ficha Patrono (REP-10 & REP-11)
- `PDF_EXCEL_PAIRS_EXECUTED=0` (La generación física pareada y extracción celda a celda requiere un contenedor Oracle XE activo con dataset fijo).
- `PDF_EXCEL_PAIRS_PASSED=0`
- `PDF_EXCEL_PAIRS_BLOCKED=5` (`BLOCKED_LOCAL_ORACLE_XE`)

### 8.3 Preservación del Estándar B06 y Escenarios de Contraste M2
1. **Geometría B06:** Ancho canónico de **72.644 mm** (**205.92 pt**) y márgenes de 14 mm preservados en todos los encabezados PDF.
2. **Logotipo Dinámico:** Contrato B06 preservado sin hardcoding de imágenes estáticas base64. Ante ausencia de logo configurado -> `HasLogo = false`, altura compacta de cabecera.
3. **Escenarios de Contraste M2:** Se auditaron y preservaron los 5 escenarios históricos de contraste de cabecera:
   - Blanco (`#FFFFFF`)
   - Azul Institucional (`#123B63`)
   - Rosa (`#F9A8D4`)
   - Turquesa / Menta (`#A7F3D0`)
   - Negro (`#000000`)
   Garantizando que títulos de sección, etiquetas clave/valor y celdas de datos permanezcan forzados en negro puro `#000000` / `FF000000`.

---

## 9. Microfase B7: Validación Transversal y Seguridad

1. **Auditoría de Vulnerabilidades npm:**
   - Dependencia `picomatch` resuelta y aislada en `vendor/picomatch`.
   - `npm audit`: **0 vulnerabilidades reportadas**.
2. **Linters y Compilación:**
   - `npm run lint`: **0 errores, 0 advertencias**.
   - Compilación frontend: exitosa en 11.0s.
   - Compilación backend: exitosa, **0 errores**.
3. **Validadores del Repositorio:**
   - `tools/validate_repository_structure.ps1`: **PASS** (118 rutas obligatorias, 1,315 archivos rastreados).
   - `tools/validate_database_scripts.ps1`: **PASS** (3 maestros SQL, paridad de esquemas).
   - `tools/validate_documentation_links.ps1`: **PASS** (266 documentos Markdown, 284 enlaces locales).
   - `tools/validate_external_writes.ps1 -SelfTest`: **PASS** (8/8 pruebas sintéticas superadas).
   - `python tools/validate_agent_skills.py`: **PASS** (14 Agent Skills validadas).
   - `git diff --check`: **PASS** (0 conflictos o problemas de espacios).
4. **Gobernanza:**
   - Rama `main` no tocada (`MAIN_MODIFIED=NO`).
   - Producción no tocada (`PRODUCTION_MODIFIED=NO`).
   - Cero mutaciones contra Oracle institucional (`ORACLE_INSTITUTIONAL_WRITES=0`).
   - Respaldos remotos previos preservados íntegros en GitHub.

---

## 10. Microfase B8: Métricas de Calidad y No Regresión Consolidadas

| Métrica / Dimensión | Valor Planificado | Valor Real Verificado | Estado |
| :--- | :---: | :---: | :---: |
| **Pruebas Unitarias Backend (`RL.API.Tests`)** | 1,332 | **1,330** (suite sin integración) | **100% PASS** |
| **Pruebas de Aislamiento y Compensación (`EvidenciasServiceIsolationTests`)** | 3 | **6** (3 previas + 3 nuevas de compensación) | **100% PASS** |
| **Pruebas Coexistencia Integridad (`PositivosCoexistenciaIntegridadTests`)** | 17 | **20** (17 previas + 3 nuevas B4) | **100% PASS** |
| **Pruebas Institucionales Backend** | 159 | **158 PASS / 1 SKIP** | **CONFORME** (1 omitida por Local XE) |
| **Pruebas Unitarias Frontend (`frontend/rl-app`)** | 1,059 (96 suites) | **1,059 (96 suites)** | **100% PASS (0 fallos)** |
| **Pruebas de Integración Oracle XE en CI Remoto** | 11 | **11** (9 M3 + 2 Paquete A) | **100% PASS** (Runs 44660135562, 44679516628, 44681293755) |
| **Parejas Físicas de Reportes Pareados Evaluadas** | 5 | **0 Ejecutadas / 5 Bloqueadas** | **`PDF_EXCEL_PAIRS_BLOCKED=5`** |
| **Validadores de Repositorio** | 5 | **5/5 PASS** (SelfTest 8/8 PASS) | **100% PASS** |
| **Cambios Funcionales de Código** | 0 | **1 (Corrección quirúrgica de compensación)** | **`FUNCTIONAL_CODE_CHANGES=1`** |
| **Documentación Corregida** | Completa | **10 Puntos de Fe de Erratas Reconciliados** | **`DOCUMENTATION_CORRECTED=YES`** |

---

## 11. Microfase B9: Dictamen Técnico y Salvedades Aceptadas

### 11.1 Dictamen Técnico
Se emite el dictamen de **`READY_FOR_INDEPENDENT_REVIEW`** para el expediente reconciliado y subsanado del Paquete B.
El sistema SGRLA-IHSS — RIESGO_LAVADO cuenta con:
- Corrección probada del defecto de compensación lógica en `EvidenciasService.cs`, garantizando que el servicio nunca afirme una reversión falsa cuando falla el rollback.
- Coexistencia física y documental blindada para personas naturales y empleados compartiendo DNI.
- Fe de erratas exhaustiva y trazable que alinea al 100% las declaraciones documentales con el código fuente real.
- Cero regresiones en los módulos previamente aprobados (B06, M1, M2, M3).

### 11.2 Salvedades y Bloqueos Documentados
1. **`ORACLE_HISTORICAL_LOCAL=GATED_LOCAL_XE_PORT_1521_CLOSED`:** Pruebas de integración Oracle XE ejecutadas exclusivamente en CI remoto debido a falta de contenedor local.
2. **`B3_INTEGRITY=BLOCKED` / `PDF_EXCEL_PAIRS_BLOCKED=5`:** Las pruebas físicas de persistencia y la extracción pareada física de reportes requieren un entorno Oracle XE local.
3. **`B5_INSTITUTIONAL_E2E=BLOCKED_EXTERNAL_AUTHORIZATION`:** Los recorridos E2E institucionales autenticados quedan supeditados a la provisión formal de credenciales corporativas e infraestructura de red interna por parte del IHSS.
4. **`DNP_GLOBAL_READ_ONLY_COMPLIANCE=NOT_YET_DEMONSTRATED`:** Supeditada a la migración planificada de las 6 operaciones heredadas de catálogo.

### 11.3 Preservación Formal de Aprobaciones del Propietario
Se ratifica la preservación plena de las aceptaciones formales emitidas por el Propietario del Sistema (`jmejia31`):
- `B06_OWNER_ACCEPTANCE=APPROVED` (Acta de Cierre Funcional M1, M2, M3).
- `M1_OWNER_ACCEPTANCE=APPROVED` (Monitoreo de Listas e Identidad).
- `M2_OWNER_ACCEPTANCE=APPROVED` (Reportería PDF/Excel y Temas Institucionales).
- `M3_OWNER_ACCEPTANCE=APPROVED` (Identidad Dinámica de Navegador y Favicon Institucional).
- `PACKAGE_A_PRESERVED=YES` (Integridad de Expedientes y Coexistencia Oracle).
