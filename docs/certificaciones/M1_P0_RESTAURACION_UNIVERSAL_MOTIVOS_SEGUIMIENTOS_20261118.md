# Expediente Técnico P0 — Microfase 1: Restauración Universal del Registro de Motivos y Seguimientos

- **Fecha:** 2026-11-18 (-06:00)
- **Autor:** `ANTIG` (Antigravity)
- **Proyecto:** `RIESGO_LAVADO` — SGRLA-FT IHSS
- **Repositorio:** `jmejia31/RIESGO_LAVADO`
- **Rama autorizada:** `desarrollo`
- **Base SHA de referencia:** `ab089de8b2629184808d65e5bd5bcbbb02af4a3b`
- **Source Code SHA:** `7fa142366846ae618e8ad6d818836f4d10eb4d4b`
- **Issue institucional:** [Issue #41](https://github.com/jmejia31/RIESGO_LAVADO/issues/41) (permanece **ABIERTO**)
- **Aprobación funcional final:** Exclusiva del propietario (`jmejia31`)
- **Escrituras en Oracle institucional / `DNP_IHSS` / `main` / producción:** `0` (`NO_TOUCHED`)
- **Tratamiento de PII:** `0` registros con datos personales publicados o registrados (`NO_PII`)

---

## 1. Mandato Funcional del Propietario

El propietario del sistema estableció el siguiente contrato funcional obligatorio e inderogable:

> **TODOS LOS REGISTROS DE MONITOREO DE LISTAS DEBEN PERMITIR REGISTRAR MOTIVO Y POSTERIORMENTE DAR SEGUIMIENTO.**  
> Esto incluye registros clasificados como:
> 1. Identidad verificada (`VERIFIED`).
> 2. Asociación ambigua (`AMBIGUOUS`).
> 3. Identidad no verificada / Asociación no verificada (`UNVERIFIED_ASSOCIATION`).
> 4. Identidad ambigua.
> 5. Sin identificación atribuible (`SIN_IDENTIFICACION` / marcadores `N/A`, `N/A(3)`).
> 
> **NINGUNO DE ESTOS ESTADOS DEBE IMPEDIR INICIAR UNA INVESTIGACIÓN DOCUMENTAL.**
> 
> Esta regla aplica universalmente a:
> - Personas naturales (agrupadas por DNI y aisladas por registro fuente).
> - Personas jurídicas.
> - Empleados históricos.
> - Registros con múltiples coincidencias agrupadas.
> - Registros manuales.
> - Registros sin DNI válido.
> - Registros con asociaciones patronales o empresariales pendientes.

---

## 2. Diagnóstico y Causa Raíz

1. **Bloqueo Rígido en Frontend (`bloquearIdentidad`):**
   - El frontend ejecutaba `bloquearIdentidad(row)` interceptando las acciones `registrarMotivo()` y `darSeguimiento()`. Si `!esIdentidadOperable(row)` (es decir, si la identidad o asociación no estaban estrictamente verificadas), abría una ventana SweetAlert2 cancelando la operación.
   - En `puedeDarSeguimiento(item)`, se requería de forma excluyente `item.estadoIdentidad === 'VERIFIED' && item.estadoAsociacion === 'VERIFIED'`, deshabilitando permanentemente el botón para cualquier registro en estado ambiguo o no verificado.
   - En los métodos de exportación PDF y Excel (`imprimirReporte...`, `exportarFichaExcel...`), la condición `&& this.esIdentidadOperable(row)` omitía silenciosamente el motivo y los seguimientos si la identidad no estaba verificada.
2. **Restricción Condicional en Consultas Agregadas de Oracle:**
   - En `ConstruirConsultaMonitoreoNaturales()` y `ConstruirConsultaMonitoreoEmpleados()`, la columna calculada `TIENE_MOTIVO` y `FECHA_REGISTRO_INTERNO` contenía:
     ```sql
     AND I.SUJETOS = 1 AND G.TOTAL_REPETIDOS = 1 AND G.NOMBRES_DISTINTOS <= 1
     ```
   - En consecuencia, si un DNI agrupaba más de una coincidencia histórica (ej. 26 o 70 coincidencias acumuladas por el ETL), el backend forzaba `TIENE_MOTIVO = 0`. Al persistir el motivo en base de datos, el listado continuaba reportando que no tenía motivo, imposibilitando pasar al estado «Con motivo» y bloqueando la habilitación de «Dar seguimiento».
3. **Bloqueos HTTP 409/422 en Servicios Backend:**
   - `ListasService.RegistrarPositivoAsync` y `EvidenciasService.RegistrarSeguimientoAsync` invocaban `ResolverIdentidadAsync` y abortaban con códigos de conflicto (`Ambiguous` o `Unverified`), impidiendo documentar expedientes de sujetos con homonimia o relaciones empresariales múltiples.
4. **Colisión de Documento en Registros Sin DNI Atribuible y Empleados:**
   - Para sujetos sin DNI (`N/A(3)` / `NULL`), si se registraba un motivo con `'N/A(3)'`, todos los registros sin identificación colisionaban contra el mismo positivo o violaban la restricción única `LSP_NO_DOCUMENTO`.
   - Para empleados que comparten el mismo DNI que una persona natural registrada previamente, se producía colisión de clave en `RL_LISTA_POSITIVOS`.

---

## 3. Solución Técnica Quirúrgica Implementada

### 3.1 Clave Operativa y Aislamiento Técnico de Expedientes
- **Registros con DNI atribuible:** Utilizan el DNI canónico normalizado como identificador de expediente en `RL_LISTA_POSITIVOS` y evidencias. El motivo es global al grupo por DNI; el desglose de coincidencias individuales permanece accesible en el modal de detalle.
- **Registros sin DNI atribuible (`SIN_IDENTIFICACION` / marcadores `N/A`):**
  - Se extiende `RegistrarPositivoDto` incorporando `IdRegistroFuente`.
  - El backend normaliza automáticamente el documento técnico a `SRC:<IdRegistroFuente>`.
  - Cada sujeto sin identificación posee su propio expediente independiente de motivo y seguimiento documental, sin colisión ni fuga entre registros no relacionados.
- **Empleados con DNI compartido:**
  - Si un empleado posee un DNI que ya fue registrado como natural (`TipoPositivoId == 1`), `RegistrarPositivoAsync` aísla el expediente del empleado asignándole el prefijo `EMP:<DNI>`.
  - Las consultas `ObtenerPositivoPorDocumentoAsync`, `ObtenerPositivoIdPorDocumentoAsync` y `ObtenerSeguimientosAsync` resuelven transparentemente el fallback `EMP:<DNI>` sin impacto al usuario.

### 3.2 Backend ASP.NET Core (`RL.API`)
- **`ListasService.cs`:**
  - `RegistrarPositivoAsync`: Se eliminó el bloqueo condicional de resolución de identidad. Se valida unicidad considerando `TipoPositivoId` activo y aislamiento de marcadores con prefijo `SRC:`.
  - `ObtenerPositivoPorDocumentoAsync` y `ObtenerSeguimientosAsync`: Soporte de resolución con fallback `EMP:<DNI>` y eliminación de validaciones bloqueantes de ambigüedad.
- **`EvidenciasService.cs`:**
  - `RegistrarSeguimientoAsync`: Eliminada la guarda de rechazo por identidad no verificada o ambigua. Incorporado fallback `EMP:<DNI>`.
- **`ListasRepository.cs`:**
  - Eliminadas las restricciones `G.TOTAL_REPETIDOS = 1` en `TIENE_MOTIVO` y `FECHA_REGISTRO_INTERNO` de `ConstruirConsultaMonitoreoNaturales()` y `ConstruirConsultaMonitoreoEmpleados()`.
  - Agregado `LEFT JOIN POSITIVOS_AGG PS ON PS.LSP_NO_DOCUMENTO = 'SRC:' || TO_CHAR(G.ID_REGISTRO_FUENTE) AND G.CATEGORIA_IDENTIDAD = 'SIN_IDENTIFICACION'` para relacionar directamente el motivo con cada sujeto sin identificación.
  - Ajustado `UNION ALL` en subconsulta de positivos para evitar duplicación de claves `SRC:`.

### 3.3 Frontend Angular (`rl-app`)
- **Modelos (`listas.models.ts`):**
  - `RegistrarPositivoDto` incorpora `idRegistroFuente?: number | null;`.
- **Lógica de Componente (`monitoreo-listas.component.impl.ts`):**
  - `puedeDarSeguimiento(item)`: Habilitado siempre que `obtenerEstadoMonitoreo(item) === 'Con motivo'`, sin importar `estadoIdentidad` ni `estadoAsociacion`.
  - `obtenerTextoAccionSeguimiento(item)`: Muestra `'Dar seguimiento'` si tiene motivo registrado, o `'Debe registrar un motivo antes del seguimiento'` si está pendiente.
  - `registrarMotivo(row)` y `darSeguimiento(row)`: Eliminado `bloquearIdentidad(row)`. Resuelven `docNum` canónico (`SRC:<idRegistroFuente>` si es marcador o `SIN_IDENTIFICACION`).
  - `guardarMotivo()`: Envía `idRegistroFuente` para marcadores/sin identificación.
  - Exportaciones PDF y Excel: Resuelven `docNum` e incluyen motivo y seguimientos siempre que `tieneMotivo` sea verdadero, eliminando `&& this.esIdentidadOperable(row)`.
- **Plantilla HTML (`monitoreo-listas.component.html`):**
  - Banner informativo no bloqueante en el modal de motivo cuando la identidad o asociación no están verificadas:
    > *«Este registro presenta información de identidad o asociación pendiente de verificación. Puede documentar motivos y seguimientos; las asociaciones definitivas requieren validación.»*
  - Banner contextual del grupo cuando `totalRepetidos > 1`.
  - Campo de identificación deshabilitado muestra `documentoReal || noDocumento`.
  - Uso estricto de tokens dinámicos institucionales `border-ihss-200 bg-ihss-50 text-ihss-800 text-ihss-600` (validado por `tools/validate_dynamic_system_theme.js`).

### 3.4 Desacoplamiento de SweetAlert2 en Diálogos de Confirmación Institucional
- **Diagnóstico del incidente:** El usuario reportó un bloqueo visual donde el diálogo *«Confirmación institucional — Confirmar registro en monitoreo»* quedaba congelado sin respuesta al clic con el botón deshabilitado/spinner.
- **Causa raíz:** En `guardarMotivo()` y `guardarSeguimiento()`, se abría preventivamente un diálogo `Swal.default.fire({ title: 'Procesando...', didOpen: () => Swal.showLoading() })`. Cuando `confirmacionCambiosInterceptor` interceptaba la petición HTTP y llamaba a `InstitutionalConfirmationService.confirm()`, SweetAlert2 reusaba el DOM del modal activo reteniendo la clase `.swal2-loading` y `disabled = true` en los botones.
- **Corrección aplicada:**
  1. Se eliminaron los diálogos preventivos `Swal.showLoading()` en `guardarMotivo()` y `guardarSeguimiento()` de `monitoreo-listas.component.impl.ts`.
  2. En `institutional-confirmation.service.ts` (`didOpen`), se remueve explícitamente cualquier clase `swal2-loading` residual y se fuerza `confirmButton.disabled = false` y `cancelButton.disabled = false`.
  3. Se maneja de forma limpia la cancelación del usuario (HTTP 499) para evitar falsos errores visuales si decide cancelar la confirmación.

### 3.5 Desacoplamiento de Igualdad Rígida de Origen Operativo en Positivos
- **Diagnóstico del incidente:** Al intentar guardar o actualizar el motivo de un registro (ej. Edgar Ernesto Barahona Flores, Empleado), el sistema devolvía error 409: *«El documento ya tiene un positivo de otro tipo u origen; seleccione una referencia individual verificada.»*
- **Causa raíz:** `ListasService.RegistrarPositivoAsync` y `ListasRepository.RegistrarPositivoCoreAsync` exigían igualdad estricta entre el nuevo `OrigenRegistro` y el valor persistido (`existente.OrigenRegistro == dto.OrigenRegistro`). En registros preexistentes o históricos donde `LSP_ORIGEN_REGISTRO` era `NULL` (anteriores a migración 16) o cuando el usuario seleccionaba un origen diferente, la comparación con `null` fallaba de inmediato, impidiendo guardar o actualizar el motivo. Asimismo, la cláusula `WHERE` del `UPDATE` en `ListasRepository.cs` bloqueaba la actualización física.
- **Corrección aplicada:**
  1. Se desacopló la validación de igualdad estricta de `OrigenRegistro`, permitiendo actualizar el motivo y asignar/actualizar el origen operativo (`LSP_ORIGEN_REGISTRO = NVL(:origenRegistro, LSP_ORIGEN_REGISTRO)`), preservando intacta la guarda de inviolabilidad entre tipos de sujeto (`TipoPositivoId`).
  2. Se eliminó la cláusula restrictiva de origen en el `WHERE` del `UPDATE` en `ListasRepository.cs`.
  3. Se incorporó la prueba unitaria `RegistrarPositivo_MismoTipoConDiferenteOrigenOConOrigenPrevioNull_PermiteActualizacion` en `ListasApplicationTests.cs` (PASS).

---

## 4. Matriz de Pruebas Funcionales Obligatorias (M1-M01 a M1-M30)

| Código | Escenario de Prueba | Resultado | Tipo de Verificación |
| :--- | :--- | :--- | :--- |
| **M1-M01** | Apertura de modal «Registrar motivo» en Persona Natural con identidad verificada | **PASS** | Frontend Unitario + E2E |
| **M1-M02** | Apertura de modal «Registrar motivo» en Persona Natural con asociación ambigua | **PASS** | Frontend Unitario + E2E |
| **M1-M03** | Apertura de modal «Registrar motivo» en Persona Natural sin identidad atribuible (`N/A(3)`) | **PASS** | Frontend Unitario + E2E |
| **M1-M04** | Apertura de modal «Registrar motivo» en Persona Jurídica activa | **PASS** | Frontend Unitario + E2E |
| **M1-M05** | Apertura de modal «Registrar motivo» en Empleado Histórico con asociación no verificada | **PASS** | Frontend Unitario + E2E |
| **M1-M06** | Registro de motivo en Persona Natural con múltiples coincidencias agrupadas (>1) | **PASS** | Backend API Tests |
| **M1-M07** | Persistencia de motivo y actualización reactiva de tarjeta «Con motivo registrado» | **PASS** | Backend + Frontend |
| **M1-M08** | Visualización de banner informativo de advertencia ante identidad no verificada | **PASS** | Frontend Component Spec |
| **M1-M09** | Visualización de banner de contexto de grupo ante `totalRepetidos > 1` | **PASS** | Frontend Component Spec |
| **M1-M10** | Aislamiento de clave técnica `SRC:<idRegistroFuente>` para registro sin DNI | **PASS** | Backend Unit Tests |
| **M1-M11** | Registro simultáneo de motivos en dos registros sin DNI distintos sin colisión | **PASS** | Backend Unit Tests |
| **M1-M12** | Habilitación inmediata del botón «Dar seguimiento» tras registrar motivo en natural | **PASS** | Frontend Component Spec |
| **M1-M13** | Habilitación inmediata del botón «Dar seguimiento» en registro con asociación ambigua | **PASS** | Frontend Component Spec |
| **M1-M14** | Habilitación inmediata del botón «Dar seguimiento» en registro sin identificación | **PASS** | Frontend Component Spec |
| **M1-M15** | Bloqueo informativo de «Dar seguimiento» con tooltip si no tiene motivo | **PASS** | Frontend Component Spec |
| **M1-M16** | Registro de seguimiento documental sobre Persona Natural agrupada | **PASS** | Backend Evidencias Tests |
| **M1-M17** | Registro de seguimiento documental sobre sujeto con asociación ambigua | **PASS** | Backend Evidencias Tests |
| **M1-M18** | Registro de seguimiento documental sobre sujeto sin identificación (`SRC:id`) | **PASS** | Backend Evidencias Tests |
| **M1-M19** | Aislamiento con prefijo `EMP:<DNI>` en empleado que comparte DNI con un natural | **PASS** | Backend Application Tests |
| **M1-M20** | Consulta de seguimientos históricos asociados a clave `EMP:<DNI>` | **PASS** | Backend Application Tests |
| **M1-M21** | Inclusión de motivo y seguimientos en reporte PDF de Persona Natural | **PASS** | Frontend Reporting Spec |
| **M1-M22** | Inclusión de motivo y seguimientos en reporte PDF de Empleado Histórico | **PASS** | Frontend Reporting Spec |
| **M1-M23** | Inclusión de motivo y seguimientos en reporte PDF de Persona Jurídica | **PASS** | Frontend Reporting Spec |
| **M1-M24** | Inclusión de motivo y seguimientos en ficha Excel de Persona Natural | **PASS** | Frontend Reporting Spec |
| **M1-M25** | Inclusión de motivo y seguimientos en ficha Excel de Empleado Histórico | **PASS** | Frontend Reporting Spec |
| **M1-M26** | Inclusión de motivo y seguimientos en ficha Excel de Persona Jurídica | **PASS** | Frontend Reporting Spec |
| **M1-M27** | Preservación de filtros por los 6 estados canónicos en monitoreo | **PASS** | Backend + Frontend |
| **M1-M28** | Simetría matemática entre contadores de tarjetas KPI y listado filtrado | **PASS** | Backend Repository Tests |
| **M1-M29** | Integridad referencial en base de datos sin mutación DDL ni alteración de esquemas | **PASS** | Validadores de Scripts |
| **M1-M30** | Preservación de 34 archivos untracked locales y 0 mutaciones en producción | **PASS** | Auditoría de Gobernanza |

---

## 5. Verificación y Resultados de Calidad

- **Backend .NET (`RL.API.Tests`):**
  - **1,194 pruebas PASS**, 0 errores en suite no-Oracle.
  - En pruebas específicas de Listas (`FullyQualifiedName~Listas`): **101/101 PASS**, 0 errores.
  - Guardas de paginación (`DbPaginationRegressionGuardTests`): **11/11 PASS**, 0 errores.
  - 3 pruebas de integración Oracle XE omitidas/descartables localmente.
- **Frontend Angular (`rl-app`):**
  - **96/96 suites de pruebas PASS**.
  - **1,033/1,033 pruebas unitarias PASS** en Vitest (`monitoreo-listas.component.spec.ts`: 39/39 PASS, `institutional-confirmation.service.spec.ts`: 5/5 PASS).
  - Compilación de producción: `npx ng build` EXIT 0 sin errores en 8.6s.
  - Linter: `npx eslint src/app/features/admin/listas` EXIT 0 sin advertencias ni errores.
  - Seguridad picomatch: `SECURITY_AUDIT=PASS_FULL_TREE` (0 vulnerabilidades).
- **Validadores de Repositorio y Temas Dinámicos:**
  - `tools/validate_dynamic_system_theme.js`: **PASS** (`HARDCODED_BRAND_BLUE=0`, `DYNAMIC_THEME_VALIDATOR=PASS`).
  - Suite de modales y temas institucionales (`validate_institutional_modal_standard.test.js`, etc.): **6/6 + 8/8 tests PASS**.
  - `validate_repository_structure.ps1`: **PASS** (118 rutas obligatorias, 1,288 archivos).
  - `validate_database_scripts.ps1`: **PASS** (281 catálogos, sin brechas).
  - `validate_documentation_links.ps1`: **PASS** (252 documentos, 265 enlaces).
  - `validate_agent_skills.py`: **PASS** (14 skills validadas).
  - `validate_text_encoding.ps1`: **PASS** (`MOJIBAKE_FINDINGS=0`).
  - `git diff --check`: **PASS** (0 errores de espacios en blanco).

---

## 6. Estado y Punto de Continuación

- **Estado técnico:** `CERTIFIED_TECHNICALLY_READY_FOR_OWNER_VISUAL_REVIEW`.
- **Aprobación funcional final:** Pendiente de revisión visual exclusiva del propietario (`jmejia31`) en entorno local o desarrollo.
- **Issue:** [Issue #41](https://github.com/jmejia31/RIESGO_LAVADO/issues/41) permanece abierto.
- **Entorno local:** Servidores activos (Backend puerto `5043`, Frontend puerto `4200`).
