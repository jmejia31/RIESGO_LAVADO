# RIESGO_LAVADO — P04

## Auditoría funcional de los nueve módulos

Fuente de código auditada: `07293984d3655e6cf6ab685d1821fe1f795a8729`  
Rama: `desarrollo`  
Quality Gate funcional vigente: [42154789302](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/42154789302), SUCCESS, attempt 1.

### Evidencia ejecutada en esta intervención

- Backend focalizado: 71/71 PASS, cubriendo Identidad, Configuración, Listas y Auditoría.
- Angular focalizado con `ng test`: 46/46 PASS en 7 archivos aplicables.
- E2E focalizado: 18/18 PASS para login, navegación, Bitácora y layout de Monitoreo.
- Contratos de límites/autorización de módulos: 2/2 PASS.
- Validadores de estructura, scripts de base de datos, documentación y `git diff --check`: PASS.
- Las pruebas Oracle XE de Matrices, publicación, eliminación y rollback TX12 se conservan como evidencia CI preexistente del mismo HEAD; no se repitieron.

### Matriz funcional única

| ID | Módulo | Contratos revisados | Evidencia | Defectos confirmados | Corrección | Estado |
|---|---|---|---|---|---|---|
| 2 | Usuarios del Sistema | `/usuarios`, guard `moduloGuard(2)`, login, refresh, claims y errores seguros | `app.routes.ts`; `IdentidadModuleCharacterizationTests`, `RefreshTokenSecurityTests`, `RefreshTokenMigrationPolicyTests`; E2E de login/navegación. SHA `07293984…`. Ejecutada + CI preexistente | Ninguno | No aplica | PASS con limitación: no se usaron usuarios institucionales reales ni se ejecutó persistencia Oracle nueva |
| 3 | Configuración del Sistema | `/configuracion`, guard `moduloGuard(3)`, consulta/guardado, caché, auditoría y carga de archivos | `ConfiguracionModuleCharacterizationTests`, `CachedConfiguracionServiceTests`, `ConfiguracionFileUploadTests`; Angular focalizado 46/46. SHA `07293984…`. Ejecutada | Ninguno | No aplica | PASS con limitación: no se modificaron parámetros institucionales |
| 4 | Monitoreo de Listas | `/monitoreo-listas`, filtros, paginación, estados, motivo y manejo de archivos | `ListasApplicationTests`, `ListasControllerTests`, `EvidenciasServiceFilePolicyTests`; `monitoreo-listas-motivo-layout.spec.ts`; Angular focalizado y E2E 18/18. Ejecutada | Ninguno | No aplica | PASS con limitación: layout E2E usa fixtures; no se cargaron archivos reales |
| 5 | Bitácora del Sistema | `/bitacora`, guard `moduloGuard(5)`, filtros/paginación, detalle, exportaciones y errores seguros | `AuditoriaModuleCharacterizationTests`, `AuditoriaTransaccionalContractTests`; `bitacora.spec.ts`; E2E 18/18. Ejecutada | Ninguno | No aplica | PASS con limitación: E2E de interfaz usa respuestas simuladas; Oracle se conserva como evidencia histórica |
| 6 | Tipo de Listas | `/tipo-listas`, autorización módulo 6, consulta/alta/actualización/eliminación y validaciones | `ListasControllerTests`, `ListasApplicationTests`; `ListasController` y rutas revisados estáticamente. SHA `07293984…`. Ejecutada | Ninguno | No aplica | PASS contractual; persistencia Oracle nueva no ejecutada |
| 7 | Cargar Listas | `/cargar-listas`, upload, validación de formato, resumen y errores | `ListasControllerTests`, `ListasApplicationTests`, `EvidenciasServiceFilePolicyTests`; rutas y contratos revisados. Ejecutada | Ninguno | No aplica | PASS con limitación: solo fixtures/superficies sintéticas; no se cargaron listas institucionales |
| 8 | Coincidencias Patrono | `/coincidencias-patrono`, resumen/detalle paginado, filtros y calificación | `ListasApplicationTests`, `ListasControllerTests`; `ListasController` y rutas revisados estáticamente. Ejecutada | Ninguno | No aplica | PASS contractual; calificación sobre datos reales no ejecutada |
| 9 | Coincidencias Empleado | `/coincidencias-empleado`, resumen/detalle paginado, filtros y calificación | `ListasApplicationTests`, `ListasControllerTests`; lógica compartida y rutas revisadas. Ejecutada | Ninguno | No aplica | PASS contractual; calificación sobre datos reales no ejecutada |
| 10 | Matrices de Riesgos | `/matrices-riesgos`, familias/versiones, constructor, publicación, evaluación, cálculo, mitigación, auditoría y eliminación | Quality Gates `42115854233` y `42154789302`; E2E Oracle/XE de publicación, eliminación y TX12; 46 Angular focalizadas. CI preexistente del mismo HEAD; no repetida | Ninguno | No aplica | PASS por evidencia CI/Oracle XE existente; no se reabren P01–P03 |

### Limitaciones y cierre

La auditoría no certifica persistencia Oracle runtime de los módulos no-Matrices porque P04 no autoriza Oracle institucional y el esquema XE disponible no contiene el universo completo de tablas de Listas/Usuarios/Configuración. Esto es una limitación de evidencia, no un defecto confirmado. No se modificaron código funcional, producción, Oracle institucional, `main`, migración 54 ni `PRUEBA_FORMULARIO`.

```ini
P04_CANONICAL_MODULES=9
P04_MODULES_REVIEWED=9/9
P04_FUNCTIONAL_AUDIT=PASS
P04_CONFIRMED_FUNCTIONAL_BLOCKERS_UNRESOLVED=0
P04_DUPLICATED_TEST_CAMPAIGNS=0
P04_UNNECESSARY_NEW_REQUIREMENTS=0
P04_COVERAGE_LIMITATIONS=DOCUMENTED
```
