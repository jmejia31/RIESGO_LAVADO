# RIESGO_LAVADO — M1: recuperación de GitHub Actions y seguimiento jurídico

**Fecha y cierre de ejecución:** 17 de noviembre de 2026, 15:53:46 (UTC-06).  
**Colaborador:** CHAT / ChatGPT · **Cuenta GitHub exclusiva:** `jmejia31`.  
**Rama:** `desarrollo` · **SOURCE_CODE_SHA:** `d216edf4c581c5724bfb582f5e05bebea9ff8669`.  
**Dictamen:** `FUNCTIONAL_SUITE_PASS_SECURITY_AUDIT_BLOCKED`. El Quality Gate **NO es SUCCESS**.

## 1. Evidencia primaria GitHub del SHA funcional

- [Quality Gate 44181990671](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/44181990671) — SHA `d216edf4c581c5724bfb582f5e05bebea9ff8669`, `completed / failure`, intento 1. El job de controles detectó **44 pasos exitosos y 1 fallido**, exclusivamente `Enforce full npm audit result (blocking)`.
- `dotnet test` integral: **1170 PASS, 1 SKIPPED, 0 FAIL**, 1171 total en la suite de cobertura; el skip no se reetiqueta.
- Angular: **96 archivos PASS / 1023 pruebas PASS**; ESLint y compilación Release aprobados.
- Playwright: **79 PASS, 15 SKIPPED, 0 FAIL**. El escenario `e2e/institutional-file-upload.spec.ts`, anteriormente fallido al localizar `Dar seguimiento`, aprobó.
- Oracle **XE descartable**: pruebas de rollback, proyección, referencias de 59 riesgos, 30 transiciones, persistencia y exportación de 2006 resultados, fórmulas independientes, recuperación ante interrupción y comparadores, todos los pasos obligatorios del run ejecutados satisfactoriamente.
- `docker compose config`, construcciones Docker backend/frontend y comprobación de usuarios no-root: PASS.
- `npm ci --ignore-scripts` y **`npm audit --omit=dev`: PASS**. **`npm audit` integral: FAIL** por vulnerabilidad alta en `picomatch@4.0.7` (CVE-2026-112366 / GHSA-fc5q-39xc-vjpf, sin versión corregida oficialmente disponible al cierre). El paso final mantiene el Quality Gate rojo.

## 2. Causa funcional y parche

El [run anterior #44178196603](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/44178196603) pasó ya el tema institucional, pero falló una prueba E2E con `locator.click` (esperaba `Dar seguimiento` durante 60 s). Se identificó la causa real: `CoincidenciaJuridicaDto` **no exponía `estadoIdentidad` ni `estadoAsociacion`**, mientras Angular exige ambas `VERIFIED` para permitir el seguimiento. No se abrió la operación por bypass.

**Corrección en `d216edf4c581c5724bfb582f5e05bebea9ff8669`:**

- `backend/RL.API/Features/Listas/Contracts/ListasModels.cs`: las dos propiedades se serializan desde fuente de monitoreo jurídica **vinculada a `PATRONOS` + `DATOS_EMPRESA` por número patronal**. Las filas manuales sin asociación comprobada y los marcadores no obtienen `VERIFIED`.
- `frontend/rl-app/src/app/features/admin/listas/models/listas.models.ts`: contrato tipado para los estados jurídicos.
- `frontend/rl-app/e2e/institutional-file-upload.spec.ts`: el mock representa la respuesta autorizada completa y exige `toBeEnabled()` antes de abrir seguimiento.
- `backend/RL.API.Tests/Features/Listas/IdentidadMonitoreoPolicyTests.cs`: casos positivos, manuales, marcadores y vacíos, incluida serialización de estados.
- `frontend/rl-app/src/app/features/admin/listas/pages/monitoreo-listas/monitoreo-listas.component.spec.ts`: guard de no permitir seguimiento con identidad/relación ausente, ambigua o no verificada.

Cambios anteriores dentro de la misma cadena de recuperación: observabilidad de `npm audit` integral **bloqueante al final**, foco M1 manual complementario para evitar duplicidad, corrección de tres clases azules fijas del filtro de planilla, alineación del guard estructural SQL paginado M1 sin eliminar pruebas. Fuente principal: [expediente de CI](CI_DEPENDENCIAS_PICOMATCH_DIAGNOSTICO_20261117.md).

## 3. Fallo restante y restricciones

`npm audit` **no ha sido desactivado, omitido, silenciado ni rebajado de severidad**. El resultado `continue-on-error` del paso de auditoría integral sirve únicamente para permitir que los controles posteriores se ejecuten; `Enforce full npm audit result (blocking)` termina en error al encontrar el aviso.

La dependencia es **transitiva de desarrollo/construcción**; el audit de producción aprobó. Esto describe alcance, **no equivale a aceptación de riesgo ni a certificación global**. No se aplicará `npm audit fix --force`, una versión ficticia ni fork no verificado. Requiere parche oficial seguro y validación de instalación reproducible, suite completa y auditoría integral.

`MAIN_TOUCHED=NO`; `PRODUCTION_TOUCHED=NO`; `ORACLE_INSTITUTIONAL_WRITES=0`; `DNP_IHSS_WRITES=0`; `B07_TOUCHED=NO`; `BLOCK10_TOUCHED=NO`; `MIGRATION_54_TOUCHED=NO`.

**Limitaciones:** los 15 escenarios E2E omitidos siguen omitidos, Oracle institucional no fue consultado, reconciliación M1 `242/190/52` y `7 empleados` pendiente, aceptación visual del propietario pendiente. No se afirma equivalencia entre Oracle XE descartable y datos institucionales.

## 4. Siguiente punto exacto

Mantener `d216edf4c581c5724bfb582f5e05bebea9ff8669` como base funcional verificada; al publicarse una versión oficial segura de `picomatch`, actualizar lockfile y dependencias compatibles, ejecutar `npm ci` / `npm audit` integral y recertificar el SHA resultante. Si se muestran nuevos fallos, revisar el job y paso del mismo SHA sin deshabilitar controles. Separadamente, ejecutar diagnóstico autorizado de solo lectura para conciliar cifras M1 y aceptación visual de los flujos.

```ini
PROJECT=RIESGO_LAVADO
PHASE=M1_CI_FOCAL_RECOVERY
BRANCH=desarrollo
AUTHOR=CHAT
SOURCE_CODE_SHA=d216edf4c581c5724bfb582f5e05bebea9ff8669
QUALITY_GATE_RUN=44181990671
QUALITY_GATE_STATUS=COMPLETED_FAILURE
QUALITY_GATE_FUNCTIONAL_STEPS=44_SUCCESS
QUALITY_GATE_BLOCKER=FULL_NPM_AUDIT_PICOMATCH_CVE-2026-112366
BACKEND_TESTS=1170_PASS_1_SKIPPED_0_FAIL
ANGULAR_TESTS=1023_PASS_IN_96_FILES
PLAYWRIGHT_E2E=79_PASS_15_SKIPPED_0_FAIL
PRODUCTION_NPM_AUDIT=PASS
FULL_NPM_AUDIT=FAIL
ORACLE_XE_DISPOSABLE=PASS_REPORTED_BY_CI
ORACLE_INSTITUTIONAL_READS=NOT_EXECUTED
ORACLE_INSTITUTIONAL_WRITES=0
MAIN_TOUCHED=NO
OWNER_ACCEPTANCE=PENDING
```
