# Bloque 9 — Mitigación y monitoreo

**Estado de implementación:** `PENDING_CI_ORACLE_BROWSER`.
**Rama:** `desarrollo`.
**Alcance:** ciclo de vida de F40–F49 y F70–F82, evidencias, capacidades Área/UGR y proyección institucional 82/82.

## Responsabilidades y fuentes

| Campos | Fuente de lectura | Escritura | Regla |
|---|---|---|---|
| F40 | `RL_MR_PLANES.PLA_DESCRIPCION` | CRUD de planes existente | Varios planes por evaluación; `PLA_ID ASC`. |
| F41 | Conteo de planes reales | Derivado | No existe input ni fuente JSON manual. |
| F42 | `RL_MR_ACTIVIDADES.ACT_DESCRIPCION` | CRUD de actividades existente | Por plan, `PLA_ID ASC, ACT_ID ASC`. |
| F43 | Conteo de actividades reales | Derivado | No existe input ni conteo cliente autoritativo. |
| F44–F49 | Columnas del plan | CRUD de planes existente | Alineados por ordinal del plan; presupuesto no negativo y fechas coherentes. |
| F70 | `RL_MR_SENALES_ALERTA` | Crear/listar/cambiar ACTIVO/INACTIVO | Sólo señales activas integran la proyección F70. Código duplicado por evaluación se rechaza. |
| F71 | `RL_MR_AUTOMONITOREO.MON_ESTADO_RIESGO` | Automonitoreo append-only | Último ciclo por `MON_FECHA DESC, MON_ID DESC`; nunca usa workflow de evaluación. |
| F72–F80 | Columnas de seguimiento en `RL_MR_CONTROLES_RIESGO` y vínculos de evidencia | PUT dedicado de monitoreo; cargar/vincular/desvincular evidencia | Control ordenado por `CON_ID ASC`; efectividad de 0 a 100; evidencia por control y fecha/ID ascendente. |
| F81 | `RL_MR_AUTOMONITOREO.MON_OBSERVACIONES_AREA` | PUT de observación Área | Sólo `MATRICES_RIESGO_OBSERVACIONES_AREA_EDITAR`. |
| F82 | `RL_MR_AUTOMONITOREO.MON_OBSERVACIONES_UGR` | PUT de observación UGR | Sólo `MATRICES_RIESGO_OBSERVACIONES_UGR_EDITAR`. |

`F81` y `F82` tienen almacenamiento, lectura, escritura y autorización separados. El acceso de edición no oculta el valor que la otra unidad puede leer. El backend bloquea por 403 los intentos directos sin la capacidad correspondiente.

## Ciclo de vida

- **Planes:** crear, leer y actualizar por endpoints existentes. Retiro lógico mediante estado `INACTIVO`; no se elimina historia ni se agrega borrado físico.
- **Actividades:** crear, leer y actualizar; retiro lógico con estado `CANCELADA`. Se mantiene su relación con el plan padre.
- **Señales de alerta:** ciclo `ACTIVO` / `INACTIVO`, sin borrado físico.
- **Automonitoreo:** `APPEND_ONLY`; historial consultable y selección vigente determinista.
- **Evidencias:** carga con nombre/extensión/tamaño/hash, vínculo por entidad, lectura en el modelo de Bloque 6 y desvinculación transaccional. Desvincular conserva el archivo. Borrado físico sólo se admite huérfano; si mantiene vínculos se responde validación 400.

El seguimiento posterior de estado/efectividad del control tiene una única superficie de escritura: `PUT /api/matrices-riesgos/monitoreo/controles/{controlId}`. La edición genérica de controles no acepta esos valores como autoridad, y el PUT específico sólo puede cambiar los campos posteriores, tras validar evaluación y catálogo.

## UI y proyección

El panel operativo usa `GET /api/matrices-riesgos/monitoreo/evaluaciones/{id}/bloque6` como snapshot de alertas, ciclo más reciente, controles, evidencias, observaciones y capacidades. Controles se agrupan por tipo y conservan identidad individual. Después de cada escritura se refrescan el snapshot y `GET /api/matrices-riesgos/evaluaciones/{id}/proyeccion-institucional`. El frontend no reconstruye los campos institucionales ni manda F41/F43.

## Verificación ejecutada localmente

- `dotnet build RIESGO_LAVADO.sln --configuration Release --no-restore`: PASS, 0 errores.
- `dotnet test backend/RL.API.Tests/RL.API.Tests.csproj --configuration Release --no-restore`: PASS, 968/968.
- `npm test -- --watch=false`: PASS, 852/852.
- `npm run lint`, `npm run build`, `npm audit --audit-level=moderate`: PASS; 0 vulnerabilidades.
- Validadores 82 campos, catálogos, UI Block 7/8 y contrato Block 9: PASS; proyección 82/82, catálogo 14/91, drift 0.
- `bash -n tools/run_block7_oracle_e2e.sh` y `git diff --check`: PASS.

## Certificación requerida en CI

El Quality Gates reutiliza el único Oracle XE desechable existente. El host de prueba exige entorno `E2E`, opt-in explícito y que `DatabaseEnvironmentGuard` acepte `LOCAL_XE`. En ese host, el test browser real cubre planes y actividades, F41/F43, alertas/duplicados, automonitoreos, los cuatro escenarios de capacidades, CRUD de seguimiento por tipo, carga/vínculo/orden/desvínculo/protección de evidencia, auditoría, F70–F82, Angular y viewport desktop/móvil. Las rutas críticas se dejan pasar a la API real.

El cierre final se actualizará sólo después de que el Quality Gates del SHA publicado termine `completed/success` y los logs muestren `BLOCK9_*_ORACLE_INTEGRATION=PASS` y `BLOCK9_BROWSER_API_ORACLE_E2E=PASS` con conteos mayores que cero.

**Seguridad:** no se ejecutó DML/DDL en producción; no se cambió el esquema; la autenticación de prueba sólo existe en el host E2E protegido por flags y el guard de base de datos. `BLOCK8_STATUS=CLOSED`; el bloque siguiente permanece sin iniciar.
