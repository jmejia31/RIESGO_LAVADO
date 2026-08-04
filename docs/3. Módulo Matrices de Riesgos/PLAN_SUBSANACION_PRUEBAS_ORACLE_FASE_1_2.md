# Plan de Subsanación: 14 Hallazgos Bloqueantes en Pruebas de Integración Oracle (Fase 1.2)

- **Fecha**: 2026-08-04
- **Módulo**: Matrices de Riesgos
- **Estado de Ejecución**: **Paso 1: NO APROBADO** | **Fase 1.2: ABIERTA**
- **Restricción Oracle**: **NO EJECUTAR CONTRA BASE DE DATOS FÍSICA (`RL_ORACLE_INTEGRATION_REQUIRED=false`)**

---

## 1. Contexto y Objetivo

La revisión del commit `6e77ee3` identificó 14 hallazgos bloqueantes en la codificación de las pruebas de integración Oracle de la Fase 1.2. Este documento consolida la estrategia técnica aprobada para subsanar cada uno de los 14 hallazgos sin realizar ejecuciones de pruebas destructivas contra el entorno físico de Oracle.

---

## 2. Detalle de los 14 Hallazgos y Estrategia de Subsanación

### Hallazgo 1: Las pruebas no se omiten realmente
- **Diagnóstico**: El uso de `if (!ValidarEntorno()) return;` hace que xUnit registre la prueba como exitosa (193 aprobadas, 0 omitidas).
- **Estrategia**: Clasificar la suite con `[Trait("Category", "OracleIntegration")]`. En la suite ordinaria de CI/CD se filtrará con `--filter "Category!=OracleIntegration"`, excluyendo las pruebas del conteo. En el modo certificación (`RL_ORACLE_INTEGRATION_REQUIRED=true`), se usará una precondición explícita que falle si la variable no está presente.

### Hallazgo 2: Atomicidad con `RL_AUDITORIA` no probada
- **Diagnóstico**: `AuditoriaRepositoryStub` solo marcaba un flag sin insertar en `RL_AUDITORIA`.
- **Estrategia**: El caso exitoso debe inyectar `AuditoriaRepository` real compartiendo la misma `OracleConnection` y `OracleTransaction` físicas y consultar directamente `RL_AUDITORIA`. Los stubs transaccionales se reservarán únicamente para inducir fallos de rollback.

### Hallazgo 3: Tabla Oracle incorrecta
- **Diagnóstico**: Las pruebas apuntaban a `RL_MR_APROBACIONES`.
- **Estrategia**: Reemplazar todas las sentencias SQL de verificación e inserción por `RL_MR_APROBACIONES_FORMULARIO`, alineado al DDL definitivo.

### Hallazgo 4: Columnas inexistentes en reglas y trazas
- **Diagnóstico**: Se consultaban `TRA_FORMULA_APLICADA` y `REG_ALGORITMO`.
- **Estrategia**: Reemplazar por las columnas del DDL definitivo:
  - `RL_MR_REGLAS_CALCULO`: `REG_ALGORITMO_ID`
  - `RL_MR_TRAZAS_CALCULO`: `TRA_ENTRADAS_JSON` y `TRA_RESULTADOS_JSON`

### Hallazgo 5: Datos de preparación sin commit
- **Diagnóstico**: La preparación insertaba registros con una conexión sin realizar `CommitAsync()`, impidiendo que la conexión del repositorio los visualizara.
- **Estrategia**: Implementar un método fixture que abra una transacción dedicada de preparación, inserte los padres, ejecute `await trans.CommitAsync()` y devuelva los IDs generados antes de la prueba.

### Hallazgo 6: Limpieza incompleta y sin commit
- **Diagnóstico**: `DisposeAsync` no confirmaba con `CommitAsync()`, no limpiaba auditorías transversales ni las 9 tablas puente, y capturaba excepciones mostrando solo advertencias.
- **Estrategia**: Implementar una rutina de limpieza integral en bloque `finally` con `CommitAsync()`, cubriendo las 9 tablas puente (`RL_MR_EVI_*`), `RL_AUDITORIA` y los registros maestros de prueba. En modo certificación, cualquier fallo de limpieza provocará la falla de la prueba.

### Hallazgo 7: Ausencia de la matriz de 9 asociaciones
- **Diagnóstico**: Solo se probaba una asociación y el nuevo `[Theory]` solo verificaba la existencia de tablas en `ALL_TABLES`.
- **Estrategia**: Implementar métodos de prueba explícitos para cada uno de los 9 vínculos:
  1. `VincularEvidenciaRiesgoAsync` (`RL_MR_EVI_RIESGO`)
  2. `VincularEvidenciaEvaluacionAsync` (`RL_MR_EVI_EVALUACION`)
  3. `VincularEvidenciaControlAsync` (`RL_MR_EVI_CONTROL`)
  4. `VincularEvidenciaPlanAsync` (`RL_MR_EVI_PLAN`)
  5. `VincularEvidenciaActividadAsync` (`RL_MR_EVI_ACTIVIDAD`)
  6. `VincularEvidenciaAlertaAsync` (`RL_MR_EVI_ALERTA`)
  7. `VincularEvidenciaAutomonitoreoAsync` (`RL_MR_EVI_AUTOMONITOREO`)
  8. `VincularEvidenciaRevisionAsync` (`RL_MR_EVI_REVISION`)
  9. `VincularEvidenciaAprobacionAsync` (`RL_MR_EVI_APROBACION`)

### Hallazgo 8: Concurrencia usando IDs institucionales fijos
- **Diagnóstico**: Se utilizaban IDs estáticos como `1` o `99999`.
- **Estrategia**: Generar todos los registros padre dinámicamente mediante las secuencias Oracle del DDL (`SEQ_RL_MR_*`) o consultas dinámicas de usuario existente.

### Hallazgo 9: Concurrencia optimista con aserción errónea
- **Diagnóstico**: Se esperaba `Assert.False(...)` en lugar de la excepción del repositorio.
- **Estrategia**: Exigir explícitamente `await Assert.ThrowsAsync<DBConcurrencyException>(...)`.

### Hallazgo 10: Evaluación creada fuera de limpieza
- **Diagnóstico**: `CrearEvaluacionAsync` generaba un `createdId` que no se agregaba al inventario `_evaluacionesCreadas`.
- **Estrategia**: Registrar inmediatamente `createdId` en el inventario de limpieza al crearse la evaluación.

### Hallazgo 11: Excepciones exponen infraestructura
- **Diagnóstico**: Se imprimía `ex.Message` de `OracleException`, pudiendo revelar host o servicio.
- **Estrategia**: Registrar mensajes sanitizados sin cadenas de conexión ni detalles sensibles.

### Hallazgo 12: Aserción excesivamente amplia
- **Diagnóstico**: Se usaba `Assert.ThrowsAnyAsync<Exception>`.
- **Estrategia**: Especificar `Assert.ThrowsAsync<OracleException>` comprobando el código de error `ORA-00001` (violación de clave única).

### Hallazgo 13: Declaración documental de Fase 1.3
- **Diagnóstico**: Se declaró aprobación funcional sin acta firmada.
- **Estrategia**: Corregir en `ESTADO_COLABORACION.md` a: *"Fase 1.3: Certificada técnicamente en CI, pendiente de ejecución y firma del acta funcional."*

### Hallazgo 14: Alteración masiva de la bitácora
- **Diagnóstico**: Se afectaron ~3700 líneas por reescritura de codificación UTF-8.
- **Estrategia**: Restaurar `BITACORA_COLABORACION.md` desde el ancestro limpio (`3029f2b`) y agregar únicamente la nueva entrada conservando UTF-8 sin BOM.

---

## 3. Plan de Verificación

1. **Backend Build**: `dotnet build RL.API.Tests/RL.API.Tests.csproj -c Release --no-restore`
2. **Backend Unit Suite**: `dotnet test RL.API.Tests/RL.API.Tests.csproj --filter "Category!=OracleIntegration" -c Release --no-restore`
3. **Validadores PowerShell**:
   - `powershell -NoProfile -ExecutionPolicy Bypass -File tools/validate_repository_structure.ps1`
   - `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/validation/validate_matrices_dynamic_ddl_alignment.ps1`
   - `powershell -NoProfile -ExecutionPolicy Bypass -File tools/validate_documentation_links.ps1`
4. **Verificación Git**: `git diff --check`
