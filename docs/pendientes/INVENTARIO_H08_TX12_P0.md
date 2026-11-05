# Inventario de evidencias faltantes: H08 / TX12 / P0-2

**Tarea:** `P0-H08-TX12-PLAN-001`  
**Revisión estática:** 2026-11-05, `COD`, rama `desarrollo`  
**HEAD local observado:** `843ede88a408b6a6c9750d2a2009d7b7ebda6573`  
**Estado de revisión:** `IMPLEMENTED_PENDING_REVIEW`; no constituye aprobación ni certificación.

## 1. Alcance y límites

Este inventario se construyó leyendo el repositorio, el expediente documental de la Issue #23, el estado de colaboración y los artefactos versionados de H08. No se conectó a Oracle, QA, producción, API, navegador ni GitHub, y no se ejecutó Playwright. No se reconstruyeron ZIP, capturas ni trazas primarias faltantes.

No se modifican backend, frontend, migración 54, Oracle, GTIC, QA, producción, `main`, ni archivos de certificación. `BLOCK10_AUTHORIZED=NO` se conserva expresamente.

## 2. Separación de estados

| Área | Estado correcto | Qué respalda | Qué no respalda |
|---|---|---|---|
| P0-2 backend | `P0_2_BACKEND_SIGNOFF=DOCUMENTAL_WITH_OBSERVATIONS` | Dictamen independiente archivado para integridad transaccional backend, con observaciones y revisión documental del expediente | No es conexión a Oracle vivo, no certifica frontend, H08, GTIC ni Bloque 10 |
| TX12 frontend | `TX12_FRONTEND=NOT_VERIFIED` | Existe evidencia documental de TX-12 backend dentro del expediente P0-2 | No existe verificación independiente del flujo Angular/HTTP real de TX12 |
| H08-M03 | `H08_M03=PENDING_USER_RETEST` | La guía manual y el estado colaborativo identifican el retest del ciclo publicación/retirada de vigencia | No hay resultado manual, captura, usuario validador ni aceptación funcional |
| Aceptación manual H08 | `H08_MANUAL_ACCEPTANCE=PENDING_USER_EXECUTION` | La guía enumera M01–M08 y sus resultados esperados | La automatización H08 no sustituye la inspección manual ni la evidencia del usuario |
| Bloque 10 | `BLOCK10_AUTHORIZED=NO` | Estado explícito del expediente y del estado colaborativo | Ningún gate histórico, fast gate o plan autoriza Bloque 10 |

## 3. Trazabilidad de fuente y expediente

| Referencia | SHA / procedencia | Lectura estática realizada |
|---|---|---|
| Código objeto de P0-2 | `168ffa783942d11097a2ebaad5bba74f4454de9b` | SHA declarado por `docs/certificaciones/P0_2_Integridad_Transaccional/README.md` y el dictamen; no se re-certifica en este trabajo |
| Expediente P0-2 | `EVIDENCE_COMMIT_SHA=87a48cb0` | Índice, matriz, dictamen, manifiesto y evidencia primaria textual localizados |
| Verificación remota histórica del expediente | `01dc9716fb3e00800a74752b68893673dae474cd` | Se conserva como dato reportado por el índice; no se consultó el servicio externo en esta intervención |
| Fuente H08 automatizada | último cambio funcional observado: `d00716f724526705a506a1238626f3b39de252df` | `frontend/rl-app/e2e/h08-family-lifecycle-real.spec.ts`, `h08-version-delete-real.spec.ts` y host/runner H08 fueron inspeccionados; el SHA no equivale a una ejecución actual |
| Estado de trabajo | `843ede88a408b6a6c9750d2a2009d7b7ebda6573` | Commit de control de cola que dejó la tarea en `RUNNING`; no es un SHA de certificación funcional |

### Rutas funcionales que deben conservarse

- P0/TX12 backend: `PUT /api/matrices-riesgos/formularios/{id}` → `ActualizarBorradorFormulario` → servicio de matrices → repositorio transaccional; publicación por `POST /api/matrices-riesgos/formularios/{id}/publicar`; lectura por `GET /api/matrices-riesgos/formularios/{id}`; eliminación por `DELETE /api/matrices-riesgos/formularios/{id}`.
- Sincronización de fórmulas asociada al builder: `PUT /api/matrices-riesgos/configuracion-calculo/formula-usos/versiones-formulario/{versionFormularioId}`. La secuencia debe ocurrir antes de publicar cuando el escenario modifique la definición.
- H08 ciclo de familia: `PUT /api/matrices-riesgos/familias/{id}/desactivar`, `PUT /api/matrices-riesgos/familias/{id}/archivar`, `PUT /api/matrices-riesgos/familias/{id}/restaurar`, `PUT /api/matrices-riesgos/familias/{id}/activar`, más `GET /api/auditoria/familia?familiaId=...`.
- H08 dependencias y lecturas: `GET /api/matrices-riesgos/familias/{id}/purga-precheck`, `POST /api/matrices-riesgos/evaluaciones/borrador` y las lecturas de familia, versión, evaluación, snapshot y auditoría usadas por el host disposable. El precheck es solo lectura; no autoriza purga.

## 4. Evidencia disponible y faltante

### Disponible como antecedente, no reejecutado

- El dictamen de Issue #23 reporta `68/68` aserciones independientes y `12/12` negativos sobre material recibido, cinco Quality Gates históricos `41469328699`, `41472933613`, `41477196537`, `41480850420` y `41483797292`, y observaciones para TX-07 y TX-10A/B.
- El expediente publicado contiene transcripciones sanitizadas y una entrada `TX-12-backend`; el propio expediente declara que no demuestra frontend ni una instancia Oracle viva.
- El E2E H08 versionado exige opt-in explícito `RL_H08_FAMILY_LIFECYCLE_CERTIFICATION_REQUIRED=true`, Oracle XE disposable y runner dedicado. El E2E de eliminación P0 exige `RL_P0_DELETE_REAL_E2E_REQUIRED=true`. La existencia de esos guardas no es una ejecución.

### Faltante para cierre reproducible

1. TX12 frontend: una ejecución Angular/Playwright contra la API real de un Oracle XE disposable, con request/response, lectura posterior de JSON/hash y auditoría; incluir también el caso negativo de rollback si forma parte del criterio de TX12.
2. H08-M03: retest de publicación y retiro de vigencia con una definición no vacía, lectura posterior de estados `PUBLICADA`/`VIGENTE` y `NO VIGENTE`, y captura o registro de usuario/fecha.
3. H08 automatizado: run integral del host H08 con familia, versión, evaluación, evidencia y auditoría sintéticas; debe conservar snapshots antes/después, no duplicar filas y demostrar las dos transiciones del ciclo.
4. H08 aceptación manual: M01–M08 ejecutados por el usuario autorizado, con referencias de evidencia y validación independiente; cualquier discrepancia debe detener la aceptación.
5. Calidad: un Quality Gate integral del SHA exacto del código probado. Los cinco gates de P0-2 son históricos y no cuentan para este cierre. Un Quality Gate fast, si la clasificación de CI lo selecciona para un delta administrativo, solo valida contrato/alcance y no sustituye builds, suites, Oracle XE, browser ni los cinco gates funcionales requeridos históricamente.

## 5. No confundir estas señales

- `READY_FOR_INDEPENDENT_REVIEW` del expediente P0-2 es un estado de revisión documental, no una certificación funcional nueva.
- `PASS` en un reporte histórico o en una transcripción sanitaria no prueba que el mismo escenario se haya ejecutado sobre `843ede88`.
- `QUALITY_GATES=0/5` y `P0_TECHNICAL_CERTIFICATION=BLOCKED` permanecen como límites del estado colaborativo para el nuevo trabajo.
- `IMPLEMENTED_PENDING_REVIEW` es el estado de esta tarea en cola; Codex no lo convierte en `APPROVED`.

## 6. Cierre de esta intervención

Validaciones previstas por el contrato: cola/schema, `git diff --check` y escaneo de secretos del delta. La sincronización Git inicial no pudo escribir `.git/FETCH_HEAD` ni `.git/index.lock` por permisos del entorno. No se ejecutaron pruebas funcionales, Oracle, Playwright, Quality Gates, CI ni servicios externos. La revisión humana y externa queda pendiente.
