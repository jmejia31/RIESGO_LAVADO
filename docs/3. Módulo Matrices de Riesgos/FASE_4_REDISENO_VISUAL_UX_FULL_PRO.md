# FASE 4 - Rediseno visual y UX Full Pro

## Identificacion

- Autor: `COD` / `CODEX`; cliente: `CLI`.
- Rama: `desarrollo`.
- Baseline operativo: `4728dd5cc7bb5b24c00d95d5de38ebde35fe7b73`.
- Ancla tecnica congelada FASE 3.1: `b2fd0b0fa4b99a863793cb6f50bb1f5f58c96800`.
- Commit tecnico FASE 4: `d198e5825923c0240088a20f3ebcd182d5264f19`.
- Alcance: shell del ciclo integral, Matrices de Riesgos, workspace de Configuracion de Calculo y consistencia visual del Builder existente.

## Reconciliacion funcional

| Interfaz | Estado real | Deuda visual/UX | Reconciliacion aplicada | Riesgo |
| --- | --- | --- | --- | --- |
| Ciclo integral | `IMPLEMENTED_VALID` | Navegacion superior competia con el contenido y no tenia tratamiento responsive uniforme | Shell sticky en desktop, tabs desplazables en viewport menor, foco/hover y estado activo consistente | Bajo |
| Matrices y evaluaciones | `IMPLEMENTED_VALID` | Header, KPI, tabs, filtros y tablas tenian jerarquia fragmentada | Hero institucional, KPI compactos, tabs de producto, superficies de filtro/tabla y focus ring | Bajo |
| Consolidado | `IMPLEMENTED_VALID` | Tabla y filtros no compartian lenguaje visual con evaluaciones | Patron de panel/filtro/tabla reutilizado sin cambiar bindings ni acciones | Bajo |
| Plantillas/familias | `IMPLEMENTED_VALID` | Exceso de contenedores y lectura debil del master/detail | Encabezado contextual y superficies coherentes para filtros, listado y detalle | Bajo |
| Configuracion de calculo | `IMPLEMENTED_VALID` | Workspace denso con cabecera y tabs poco diferenciados | Cabecera unica, navegacion interna coherente, tablas/input/estados normalizados | Bajo |
| Formulas, funciones, parametros, reglas y catalogos | `IMPLEMENTED_VALID` | Variantes visuales repetidas dentro del workspace | Estilos scoped compartidos por superficie; se conservan DSL, allowlist, tipos, estados, hashes y usos | Bajo |
| Builder | `IMPLEMENTED_VALID` | Deuda residual menor fuera del cambio | No se reconstruyo: QA E2E confirma editor, preview, JSON, menu contextual, readonly y foco modal | Bajo |

El DOCX historico de Fase 4 se uso como insumo funcional. Cuando su representacion previa no coincidio con los contratos de Fase 3.1, prevalecieron el backend, el versionado exacto, el Publication Gate y el Builder certificado. No se eliminaron capacidades reales ni se inventaron endpoints.

## Implementacion

- Se agregaron hojas de estilo scoped a los tres niveles de la experiencia: ciclo integral, Matrices y Configuracion de Calculo.
- Se normalizaron hero, titulo, contexto, tabs, KPI, filtros, tablas, estados hover/focus y densidad sin incorporar framework CSS ni dependencias.
- Se agregaron breakpoints para navegacion y superficies menores; se respeto `prefers-reduced-motion`.
- Se conservaron rutas, selectores E2E, bindings Angular, servicios, modales, permisos, renderer, serializer, normalizador y contratos existentes.
- No hubo cambios backend, Oracle, `package.json`, `package-lock.json`, `tsconfig` ni datos historicos.

Archivos tecnicos modificados:

- `frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.html`
- `frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.ts`
- `frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.scss`
- `frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/configuracion-calculo/configuracion-calculo.component.html`
- `frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/configuracion-calculo/configuracion-calculo.component.ts`
- `frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/configuracion-calculo/configuracion-calculo.component.scss`
- `frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos-ciclo-integral/matrices-riesgos-ciclo-integral.component.html`
- `frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos-ciclo-integral/matrices-riesgos-ciclo-integral.component.ts`
- `frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos-ciclo-integral/matrices-riesgos-ciclo-integral.component.scss`

## Invariantes y controles

`NEW_ENGINE=0`, `NEW_PUBLICATION_GATE=0`, `NEW_AUDIT_SYSTEM=0`, `NEW_CATALOG_SYSTEM=0`, `NEW_RULE_SYSTEM=0`, `NEW_TABLES=0`, `RBAC_CHANGES=0`, `DYNAMIC_EXECUTION=0`, `FRONTEND_EVAL=0`, `FRONTEND_NEW_FUNCTION=0`, `PINNING_BY_LATEST_VERSION=0`, `RL_MR_CAMPOS_FORMULARIO_REINTRODUCED=0`.

`VER_ID_27_28_MUTATIONS=0`; `VER_ID_24_53_REGRESSION=0` en la regresion frontend existente. El Builder mantiene `ROUND_TRIP`, `RENDERER_PARALLEL=0`, `SERIALIZER_PARALLEL=0`, `NORMALIZER_PARALLEL=0` y `FORMULA_PINNING`.

## Validacion ejecutada

| Gate | Resultado |
| --- | --- |
| `npx tsc -p e2e/tsconfig.json --noEmit` | PASS |
| `npx tsc -p tsconfig.spec.json --noEmit` | PASS |
| `npm run lint` | PASS |
| `npm run build` | PASS; warnings existentes de budget del inspector y CommonJS `exceljs` |
| Vitest | `716/716 PASS` |
| Playwright E2E | `29/29 PASS` |
| Backend Release | `591/591 PASS`, `FAIL=0` |
| Quality gates local | PASS; cobertura frontend lineas `61.19%`, ramas `54.19%`; backend lineas `31.08%`, ramas `35.34%` |
| `npm audit --audit-level=high` | PASS; `HIGH=0`, `CRITICAL=0` |
| Validacion de documentacion | PASS; 119 Markdown y 168 enlaces |
| Validacion de scripts DB | PASS; sin ejecucion Oracle |
| Agent Skills | PASS; 14 skills |
| `git diff --check` | PASS |

El primer intento de `npm audit` fallo por red/cache y fue repetido con acceso aprobado. La validacion `python tools/validate_agent_skills.py` requirio el runtime Python local temporal documentado por el protocolo; `PATH` fue restaurado.

## QA visual

QA real con capturas E2E frescas en `1536x1024`: shell del ciclo integral, Matrices/evaluaciones, consolidado y Builder editable, preview y JSON tecnico. Se revisaron spacing, alineacion, densidad, tablas, tabs, modal/foco, estados readonly y ausencia de overflow global. La prueba E2E de foco confirma el focus trap del modal del Builder. No se generaron capturas versionadas.

La validacion automatica responsive cubre el comportamiento existente del Builder a `1536x1024` y los breakpoints scoped para viewport menor; queda pendiente una matriz visual dedicada para `1920x1080`, `1366x768` y tablet si se requiere evidencia pixel-level adicional.

## Riesgos y fuera de alcance

- `validate_repository_structure.ps1` mantiene tres hallazgos heredados fuera de Fase 4: carpeta fuente vacia `dynamic-form-layout`, archivo frontend heredado `global-http-state.service.ts` y carpeta heredada `core/services`. No fueron creados ni modificados por esta intervencion.
- El fetch de Git no pudo actualizar `.git/FETCH_HEAD` por permiso del entorno; la igualdad de HEAD y `origin/desarrollo` fue confirmada antes de intervenir. No se ejecuto `git pull`.
- No se ejecuto Oracle y no hubo DDL, DML, recovery ni mutaciones historicas.
- No se inicia Fase 5.

## Estado de cierre

La implementacion tecnica Fase 4 esta validada localmente y publicada en el commit tecnico indicado. El cierre formal de fase depende de confirmar el push y el Quality Gate remoto del SHA final; el resultado se registrara en la bitacora y el estado colaborativo sin reutilizar runs de otro SHA.

## Reapertura exclusiva de aceptación visual - corrección UX

- Fecha/hora local: `2026-09-03 14:49 UTC-06`. Autor: `COD` / `CODEX`; cliente: `CLI`.
- Motivo: la UAT visual posterior determinó que la entrega anterior aún presentaba apariencia CRUD, panel de detalle vacío, contenedores redundantes, espacio muerto en Reglas y una cuadrícula de tarjetas en Catálogos. Esta reapertura no reabre la arquitectura FASE 3.1 ni la funcionalidad certificada.
- Baseline de trabajo: `367fb478f2b1657ba360a0ce69334018abcb231b`. Commit correctivo publicado: `9d0588327d8e6b2ef5d32a5df6d4cd564f58cc07`.
- Corrección aplicada únicamente en Configuración de Cálculo: toolbar contextual por pestaña e inclusión de inactivas contextualizada; listado adaptativo que ocupa el ancho cuando no hay selección; detalle dinámico solo con selección; identidad de Funciones sin repetir código/nombre; dossier compacto para Reglas; master/detail para Catálogos y sus elementos; estados vacíos proporcionales; responsive y focus states scoped.
- Archivos correctivos: `frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/configuracion-calculo/configuracion-calculo.component.html`, `.scss`, `.ts` y `.spec.ts`.
- Invariantes: `NEW_ENGINE=0`, `NEW_PUBLICATION_GATE=0`, `NEW_AUDIT_SYSTEM=0`, `NEW_CATALOG_SYSTEM=0`, `NEW_RULE_SYSTEM=0`, `NEW_TABLES=0`, `NEW_ENDPOINTS=0`, `RBAC_CHANGES=0`, `DYNAMIC_EXECUTION=0`, `HISTORICAL_MUTATIONS=0`; `VER_ID_27_28_MUTATIONS=0`; `VER_ID_24_53_REGRESSION=0`; Builder sin renderer/serializer/normalizer paralelo y FormulaVersion preservada.
- Validación técnica ejecutada: TSC E2E/spec PASS; Vitest `718/718 PASS`; lint PASS; build PASS; Playwright `29/29 PASS`; backend Release `591/591 PASS`, `FAIL=0`; DB scripts PASS; documentación PASS; Agent Skills `14/14 PASS`; `git diff --check` PASS; npm audit repetido con acceso aprobado, `HIGH=0`, `CRITICAL=0`.
- QA visual: se generaron capturas internas de layout con fixture para revisar la intervención, pero no se consideran UAT ni aceptación. La navegación contra el backend local real respondió `401` en los endpoints de Matrices sin una sesión autorizada; por tanto las capturas reales con datos institucionales y la aprobación visual del usuario quedan pendientes.
- Estado vigente y fail-closed: `FASE_4_TECNICA_CORRECTIVA=PASS`; `FASE_4_REGRESION_FUNCIONAL=PASS`; `FASE_4_UX_VISUAL=PENDIENTE_APROBACION_USUARIO`; `FASE_4_ESTADO=EN_VALIDACION_VISUAL`; `FASE_4_FINAL=NO`; `FASE_5_HABILITADA=FALSE`.
