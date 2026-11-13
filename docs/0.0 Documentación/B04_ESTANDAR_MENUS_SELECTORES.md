# B04 — Estándar institucional de menús y selectores

## Estado de la intervención

`PHASE=B04_MENUS_AND_SELECTORS`
`REFERENCE_SHA=5574d4b2c215cf4cebf707f73d77218139f9c46c`
`B04_IMPLEMENTATION_AUTHORIZED=YES`
`B04_OWNER_VISUAL_ACCEPTANCE=PENDING`

B04 centraliza el comportamiento compartido de selectores extensos, menús de acciones y posicionamiento de capas sin reemplazar controles nativos pequeños que ya son compatibles con Angular Forms y el contrato visual institucional.

## Inventario estructural inicial

Auditoría ejecutada sobre `frontend/rl-app/src/app`, excluyendo pruebas y artefactos generados:

| Métrica | Resultado |
|---|---:|
| Selectores totales | 64 |
| `<select>` nativos | 49 |
| `app-bounded-select` | 3 |
| `app-page-size-selector` | 12 |
| Menús de acciones migrados al componente compartido | 2 |
| Menús contextuales independientes pendientes | 0 |
| `<details>/<summary>` de menú sin estandarizar | 0 |
| Componentes sin clasificar | 0 |

Clasificación:

- `NATIVE_SELECT_STANDARD`: 49 controles cortos, estáticos o ligados a Reactive Forms/`ngModel`; conservan su semántica nativa y el valor original.
- `BOUNDED_SELECT_REQUIRED`: 3 selectores largos o dinámicos en Matrices; conservan búsqueda, scroll, selección tipada, Escape, clic externo y contención de viewport.
- `NATIVE_PAGE_SIZE_STANDARD`: 12 controles centralizados por `app-page-size-selector`; no se alteran opciones ni tamaños autoritativos.
- `ACTION_MENU_REQUIRED`: 2 superficies del Form Builder, ahora atendidas por `app-institutional-action-menu`.
- `CONTEXT_MENU_REQUIRED`: 0 patrones independientes después de la migración del menú de acciones de sección.

Los `<select>` nativos no se migraron indiscriminadamente: hacerlo habría cambiado contratos `formControlName`, `ngModel`, valores numéricos, estados disabled y validaciones sin beneficio funcional. La excepción queda justificada por dominio y está cubierta por el inventario.

## Componentes compartidos

### `app-bounded-select`

Es el selector canónico para listas largas. Mantiene:

- `role="combobox"`, `aria-haspopup="listbox"`, `aria-expanded`, `aria-controls` y `aria-activedescendant`.
- Búsqueda interna desde el umbral configurado.
- Valores `string`, `number` y `null` sin conversión silenciosa.
- Opciones deshabilitadas.
- Navegación con flechas y confirmación con Enter.
- Cierre por Escape, clic externo, selección y destrucción.
- Reposicionamiento al hacer scroll, resize y cambios del viewport visual.
- Apertura arriba/abajo, ancho acotado y altura máxima con scroll interno.

### `app-page-size-selector`

Se conserva como fuente única de paginación compacta. B04 no cambia opciones, tamaños de página, consultas ni contratos de servidor.

### `app-institutional-action-menu`

Nuevo componente compartido para acciones secundarias y contextuales. Recibe el contenido funcional por proyección y no contiene reglas de negocio. Centraliza:

- Apertura y alternancia idempotente.
- Una sola capa activa mediante `InstitutionalOverlayRegistry`.
- Clic externo y Escape.
- Retorno de foco al disparador.
- `role="menu"` con botones nativos de acción, `aria-expanded`, `aria-controls` y `aria-haspopup`; se preserva el rol accesible `button` que requieren las acciones existentes.
- Navegación Home/End/ArrowUp/ArrowDown.
- Cierre después de seleccionar una acción.
- Posicionamiento compartido, contención horizontal y altura máxima.
- Listeners de viewport liberados al cerrar o destruir.

### Posicionamiento

`institutional-overlay-position.util.ts` es la fuente común para calcular dirección, límites, ancho, alto y coordenadas de las capas. El selector extenso y el menú de acciones utilizan la misma función; no existen algoritmos paralelos para cada consumidor.

## Migraciones realizadas

| Superficie | Antes | Después | Estado |
|---|---|---|---|
| Form Builder — toolbar | `<details>/<summary>` con popover local | `app-institutional-action-menu` | `STANDARDIZED` |
| Form Builder — acciones de sección | estado local, `@HostListener` y `position:absolute` | `app-institutional-action-menu` y registro compartido | `STANDARDIZED` |
| Matrices — Evaluación/Riesgo/Mitigación | `app-bounded-select` existente | posicionamiento compartido y registro de capa | `STANDARDIZED` |
| Paginación administrativa | `app-page-size-selector` existente | preservado | `ALREADY_COMPLIANT` |
| Selectores nativos cortos | `<select>` compatible | preservados con contrato de formulario | `JUSTIFIED_EXCEPTION` |

No se modificaron roles, permisos, backend, Oracle, fórmulas, cálculos, auditoría, autenticación ni datos institucionales.

## Accesibilidad, responsive y rendimiento

- Foco visible mediante el contrato global de `:focus-visible`.
- No se crean consultas HTTP al abrir o cerrar capas.
- Se evita la acumulación de listeners con instalación/remoción explícita.
- La capa se recalcula durante scroll y resize.
- Los paneles tienen scroll interno y no exceden el viewport disponible.
- Los formularios Angular existentes conservan `formControlName`, `ngModel`, `disabled`, validaciones y tipos.
- Las acciones siguen siendo icon-only con `aria-label` y `title` institucionales.

## Validador preventivo

`tools/validate_b04_menus_selectors.js` verifica:

- Inventario de selectores, paginadores y menús.
- Ausencia de `<details>/<summary>` no migrados.
- Presencia de contratos ARIA y cierre del selector acotado.
- Presencia del registro compartido, posicionamiento y cierre del menú institucional.
- Conteo de componentes sin clasificar.

Ejecutar:

```powershell
node tools/validate_b04_menus_selectors.js --self-test
node --test tools/validate_b04_menus_selectors.test.js
node tools/validate_b04_menus_selectors.js
```

## Regresión y límites

Pruebas frescas de esta intervención:

- `npm run build`: `PASS`, con warnings preexistentes de presupuesto/CommonJS.
- `npm test -- --watch=false`: `94` archivos y `984` pruebas `PASS`.
- `npm run lint`: `PASS`.
- Validador B04 y self-test: `PASS`; inventario `64` selectores, `2` menús, `0` sin clasificar.

Playwright real, Oracle institucional y pruebas de infraestructura requieren el runner/datos autorizados y se registran como pendientes hasta la ejecución remota correspondiente. B04 no declara aceptación visual del propietario.

## Restricciones preservadas

`ORACLE_INSTITUTIONAL_TOUCHED=NO`
`PRODUCTION_TOUCHED=NO`
`MAIN_TOUCHED=NO`
`MIGRATION_54_TOUCHED=NO`
`P06_PRESERVED=YES`
`P07_PRESERVED=YES`
`BLOCK10_AUTHORIZED=NO`

`B04_GLOBAL_INVENTORY=COMPLETE`
`B04_TECHNICAL_STATUS=PASS`
`B04_OWNER_VISUAL_ACCEPTANCE=PENDING`
`B04_FINAL_STATUS=PENDING_OWNER_ACCEPTANCE`

## Complemento obligatorio — Familias de formularios

El alcance B04 incluye también las superficies de Familias de Formularios, sin modificar la lógica CRUD, versionado, permisos, auditoría ni contratos de datos.

| Superficie | Cobertura | Estado técnico |
|---|---|---|
| `FAMILIA_CREAR` | modal de creación, foco inicial, ciclo Tab, Escape, validaciones, encabezado/pie y acciones | `STANDARDIZED` |
| `FAMILIA_CONSULTAR` | detalle, historial, versiones, acciones por versión, carga incremental y navegación | `STANDARDIZED` |
| `FAMILIA_EDITAR` | edición descriptiva, activación/desactivación, eliminación, confirmaciones y restricciones | `STANDARDIZED` |
| `FAMILIA_VERSIONES` | consulta, edición, clonación, publicación, vigencia y eliminación | `STANDARDIZED` |
| `FORM_BUILDER_SELECTORES` | tipo de control, fórmula/versiones, catálogo, columnas, menú de acciones y sección | `STANDARDIZED` |

### Tooltip institucional reutilizable

`appInstitutionalTooltip` (`InstitutionalTooltipDirective`) se importa en las tres familias, el gestor de matrices y las superficies del Form Builder. Se activa sobre botones con `title`, conserva el texto institucional y crea una sola capa `role="tooltip"` en `document.body` para evitar recorte por `modal-body-scrollable`, tablas o bordes del viewport.

El contrato compartido cubre:

- posición fija con contención en los cuatro bordes del viewport;
- reposicionamiento ante `resize` y `scroll`;
- activación por hover y foco, `aria-describedby` y cierre al perder foco/Escape;
- contraste, prioridad de capa y `pointer-events: none`;
- ocultamiento de tooltips inline heredados sin alterar sus botones ni acciones;
- destrucción limpia al desmontar el modal.

Los filtros `Estado` y `Archivo` del gestor de familias son selectores nativos cortos, clasificados como `JUSTIFIED_EXCEPTION`; conservan `ngModel`, valores y opciones originales. No se migraron indiscriminadamente a un componente largo.

### Triple auditoría del complemento

- `AUDIT_1_STATIC=PASS`: validador B04 con `64` selectores, `2` menús de acciones, `22` archivos de superficies de familias/Form Builder y `10` archivos con tooltip institucional.
- `AUDIT_2_BROWSER=BLOCKED_EXTERNAL`: las cuatro viewport (`390x844`, `768x1024`, `1366x768`, `1536x864`), temas y capturas Crear/Consultar/Editar requieren navegador real, servidor y datos autorizados del runner; no se simulan localmente.
- `AUDIT_3_REGRESSION=PASS_LOCAL`: suite Angular canónica `95` archivos y `986` pruebas PASS; build y lint PASS; se conserva la prueba QA de detalle y las regresiones existentes de familias/versiones/Form Builder.

`FAMILY_CREATE_UI=PASS`
`FAMILY_READ_UI=PASS`
`FAMILY_EDIT_UI=PASS`
`FAMILY_VERSIONS_UI=PASS`
`FORM_BUILDER_SELECTORS=PASS`
`FAMILY_TOOLTIPS=PASS`
`FAMILY_MODAL_GEOMETRY=PASS_LOCAL_PENDING_BROWSER`
`FAMILY_OUTSIDE_CLICK=PASS`
`FAMILY_ESCAPE_BEHAVIOR=PASS`
`FAMILY_KEYBOARD_ACCESSIBILITY=PASS`
`FAMILY_FOCUS_MANAGEMENT=PASS`
`FAMILY_RESPONSIVE=BLOCKED_EXTERNAL`
`SHARED_UI_STANDARD=PASS`
`CRUD_FUNCTIONAL_REGRESSION=PASS_LOCAL`
`B01_REGRESSION=NOT_REEXECUTED_IN_SCOPE`
`B02_REGRESSION=NOT_REEXECUTED_IN_SCOPE`
`B03_REGRESSION=PASS_INHERITED_CONTRACT_PRESERVED`
`B04_OWNER_VISUAL_ACCEPTANCE=PENDING`
`B04_FINAL_STATUS=OPEN`

### Publicación y verificación remota del complemento

- `SOURCE_CODE_SHA=35d1e1a8e4bfde8ec15a7423aecb92aa26f5f54f`.
- `EVIDENCE_COMMIT_SHA=35d1e1a8e4bfde8ec15a7423aecb92aa26f5f54f`.
- `QUALITY_GATE_RUN=43272797472` — [Quality Gates](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/43272797472), `STATUS=completed`, `CONCLUSION=success`.
- `GITHUB_REMOTE_VERIFIED=YES`; `SECURITY_SCAN=PASS_GITHUB_ACTIONS`; `REVIEW_READY=YES` para revisión técnica.

## Corrección P0 — Selector de fórmula/versiones del Form Builder

La lista dinámica extensa de `Fórmula central y versión exacta` ya no se clasifica como excepción nativa. El inspector usa el componente institucional `app-bounded-select`, con búsqueda, scroll interno, posicionamiento arriba/abajo, límites de viewport, selección tipada y cierre externo. La fórmula vinculada permanece contenida mediante ajuste de palabras y scroll vertical, sin alterar el valor persistido.

```ini
B04_FORMULA_SELECTOR_STANDARD=PASS
B04_FORMULA_DROPDOWN_POSITION=PASS_FOCUSED
B04_FORMULA_VERSION_BINDING=PASS
B04_FORMULA_TEXT_CONTAINMENT=PASS_FOCUSED
B04_EXTENDED_SELECTORS_AUDIT=PASS_STATIC
B04_FORM_BUILDER_PLAYWRIGHT=PASS_FOCUSED
B04_OWNER_VISUAL_ACCEPTANCE=PENDING
B04_FINAL_STATUS=OPEN
```

- `SOURCE_CODE_SHA=242b884a235510b89731bc1f0ce6bb26eeb97255`; `EVIDENCE_COMMIT_SHA=PENDING_COMMIT`.
- Inventario posterior: `TOTAL_SELECTORS=63`, `TOTAL_NATIVE_SELECTS=48`, `VISIBLE_NATIVE_SELECTS=46`, `HIDDEN_SYNC_SELECTS=2`, `BOUNDED_SELECTS=5`, `PAGE_SIZE_SELECTORS=12`, `ACTION_MENUS=2`, `UNREVIEWED_COMPONENTS=0`.
- Regresión fresca: validador B04/self-test y Node tests `PASS`; fórmula central `5/5`; inspector, presentation y bounded select `49/49`; Playwright enfocado `1/1 PASS` en `1366x768`, incluyendo 24 versiones, búsqueda, selección y contención.
- La auditoría visual completa de las cuatro viewports y la aceptación del propietario siguen pendientes; este expediente no declara cierre global por esa limitación.

## Orden complementaria — Cobertura global preventiva

La revisión B04 se amplió a todas las superficies Angular aplicables: Matrices, Evaluaciones, Consolidadas, Plantillas,
Familias, Form Builder, Configuración de cálculo, Gestión de Riesgos, Mitigación, Monitoreo operativo, Monitoreo de
Listas, Coincidencias Empleado/Patrono, Cargar Listas, Tipo de Listas, Usuarios, Bitácora, Configuración del Sistema,
Login, layout, report preview, modales y componentes compartidos.

### Resultado estructural fresco

La cifra histórica `TOTAL_SELECTORS=64` incluía un `<select>` nativo oculto de sincronización como si fuera un control
visible. La auditoría global ahora distingue ambas categorías:

```ini
LEGACY_TOTAL_SELECTORS=64
VISIBLE_SELECTORS=63
TOTAL_NATIVE_SELECTS=49
VISIBLE_NATIVE_SELECTS=47
HIDDEN_SYNC_SELECTS=2
BOUNDED_SELECTS=4
PAGE_SIZE_SELECTORS=12
ACTION_MENUS=2
TOOLTIP_BUTTONS=280
TOOLTIP_SURFACES=26
NATIVE_SELECT_REVIEWS=16
UNREVIEWED_COMPONENTS=0
```

El selector de Familia de Nueva Evaluación usa `app-bounded-select` como owner visual, con un `<select>` oculto de
sincronización para preservar el contrato Angular/QA. Las excepciones nativas restantes están registradas con motivo en
`tools/b04-control-review.json`; no se aceptan por el simple hecho de ser nativas.

### Certificación global limitada

```ini
B04_GLOBAL_COVERAGE=PARTIAL
B04_SHARED_COMPONENTS=PASS
B04_ALL_MODULES_REVIEWED=YES
AUDIT_1=PASS
AUDIT_2=BLOCKED
AUDIT_3=PASS
B04_OWNER_VISUAL_ACCEPTANCE=PENDING
B04_FINAL_STATUS=OPEN
```

`AUDIT_1` fue ejecutada con el inventario y el validador preventivo. `AUDIT_3` incluye lint, build, suite Angular completa
y regresión focalizada de Familias; la ejecución Playwright real de todas las superficies y viewport queda pendiente por
servidor, autenticación y datos autorizados. Por esa limitación `B04_GLOBAL_COVERAGE` permanece `PARTIAL` y no se declara
certificación global cerrada.

### Quality Gate final del expediente

- `SOURCE_CODE_SHA=35d1e1a8e4bfde8ec15a7423aecb92aa26f5f54f`.
- `EVIDENCE_COMMIT_SHA=7585fd12cc637d76b7ad87c96ad626ea14594c3f`.
- `QUALITY_GATE_RUN=43279214059` — [Quality Gates](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/43279214059), `STATUS=completed`, `CONCLUSION=success`.
- `GITHUB_REMOTE_VERIFIED=YES`; `SECURITY_SCAN=PASS_GITHUB_ACTIONS`; `REVIEW_READY=YES` para revisión técnica.
- `GITHUB_SIGNOFF_URL=https://github.com/jmejia31/RIESGO_LAVADO/issues/23`.
- `AUDIT_2_BROWSER=BLOCKED_EXTERNAL`; la aceptación visual del propietario permanece pendiente y por eso `B04_FINAL_STATUS=OPEN`.

### Quality Gate posterior a la corrección E2E

- `SOURCE_CODE_SHA=dfb2f20247906d4c30c77e5692f4c15835cf4452`.
- `EVIDENCE_COMMIT_SHA=dfb2f20247906d4c30c77e5692f4c15835cf4452`.
- `QUALITY_GATE_RUN=43291627396` — [Quality Gates](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/43291627396), `STATUS=completed`, `CONCLUSION=success`.
- El run verificó `BLOCK7_BROWSER_API_ORACLE_E2E=PASS`, H08 A–E `PASS`, `BLOCK7_UNEXPECTED_ORACLE_ERRORS=0` y el resto de jobs integrales en verde.
- `SECURITY_SCAN=PASS_GITHUB_ACTIONS`; `REVIEW_READY=YES`; `GITHUB_REMOTE_VERIFIED=YES`; `GITHUB_EVIDENCE_INDEX_URL=https://github.com/jmejia31/RIESGO_LAVADO/blob/fee973a2c61236296e70fdc3d45948cac3f9ccc0/docs/0.0%20Documentaci%C3%B3n/B04_ESTANDAR_MENUS_SELECTORES.md`; `GITHUB_SIGNOFF_URL=https://github.com/jmejia31/RIESGO_LAVADO/issues/23`.
- `AUDIT_2_BROWSER=BLOCKED_EXTERNAL` para la revisión visual global autorizada; `B04_OWNER_VISUAL_ACCEPTANCE=PENDING`; `B04_FINAL_STATUS=OPEN`.

### SHA documental final

- `SOURCE_CODE_SHA=35d1e1a8e4bfde8ec15a7423aecb92aa26f5f54f`.
- `EVIDENCE_COMMIT_SHA=5582c1e8c9078a391850a9785764873314447cbb`.
- `QUALITY_GATE_RUN=43275934595` — [Quality Gates](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/43275934595), `STATUS=completed`, `CONCLUSION=success`.
- `GITHUB_REMOTE_VERIFIED=YES`; `SECURITY_SCAN=PASS_GITHUB_ACTIONS`; `REVIEW_READY=YES` para revisión técnica.
-
## Cierre técnico remoto — corrección P0 del selector de fórmulas

- `SOURCE_CODE_SHA=6019a53032ec4a8775bffccc2afce476b42b28f`; `EVIDENCE_COMMIT_SHA=6019a53032ec4a8775bffccc2afce476b42b28f`.
- Quality Gate [43303886918](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/43303886918): `STATUS=completed`; `CONCLUSION=success`; `77 passed`, `15 skipped`, `0 failed`.
- `H08_EXTENDED_A_TO_E=PASS`; `BLOCK7_BROWSER_API_ORACLE_E2E=PASS`; `BLOCK7_UNEXPECTED_ORACLE_ERRORS=0`; el escenario Playwright B04 reportó `PASS` para búsqueda, selección exacta y contención de expresión.
- `GITHUB_REMOTE_VERIFIED=YES`; `GITHUB_SIGNOFF_URL=https://github.com/jmejia31/RIESGO_LAVADO/issues/23`; `SECURITY_SCAN=PASS_GITHUB_ACTIONS`; `REVIEW_READY=YES` para revisión técnica.
- `B04_OWNER_VISUAL_ACCEPTANCE=PENDING`; `B04_FINAL_STATUS=OPEN`: la aceptación visual completa del propietario en cuatro viewports no se sustituye por el gate automatizado.
-
## Mejora global de presentación de opciones extensas — 2026-11-12

La corrección se implementó una sola vez en `app-bounded-select`, sin variantes particulares por consumidor. Las opciones ya no tienen `title` nativo; muestran su etiqueta completa dentro del panel, conservan `aria-label`, distribuyen nombres extensos en varias líneas y permanecen dentro del ancho y alto controlados del overlay.

```ini
B04_OPTION_FULL_TEXT=PASS
B04_NATIVE_TOOLTIP_INTERFERENCE=0
B04_OPTION_HORIZONTAL_OVERFLOW=0
B04_SHARED_SELECTOR_REGRESSION=PASS
B04_FORMULA_VERSION_BINDING=PASS
B04_PLAYWRIGHT_VISUAL=BLOCKED
B01_PRESERVED=YES
B02_PRESERVED=YES
B03_PRESERVED=YES
B04_OWNER_VISUAL_ACCEPTANCE=PENDING
B04_FINAL_STATUS=OPEN
```

- Pruebas focalizadas: bounded-select y fórmula central `16/16 PASS`; validador B04 `PASS`; lint `PASS`; build `PASS` con warnings no bloqueantes ya conocidos; suite Angular completa `exit 0`.
- Chromium real: `1366x768` y `1536x864` PASS para texto visible, ausencia de `title`, wrapping, geometría y selección; `390x844` y `768x1024` se registran como `BLOCKED_INSPECTOR_NOT_VISIBLE` porque el Form Builder oculta el inspector, sin convertir esa ausencia en PASS visual.
- Se conservaron los contratos funcionales y el posicionamiento institucional. La aceptación visual del propietario permanece pendiente; B04 no se cierra.
- `SOURCE_CODE_SHA=47a1fe1d4e6cc7121a81fcbe89a01291db3033f9`; `EVIDENCE_COMMIT_SHA=47a1fe1d4e6cc7121a81fcbe89a01291db3033f9`.
- Quality Gate exacto [43316052382](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/43316052382): `STATUS=completed`; `CONCLUSION=success`; `GITHUB_REMOTE_VERIFIED=YES`; `SECURITY_SCAN=PASS_GITHUB_ACTIONS`; `REVIEW_READY=YES` para revisión técnica.

## Corrección responsive del Inspector de propiedades — 2026-11-12

La ocultación del Inspector a `max-width: 980px` fue reemplazada por un único panel responsive sobre la misma instancia y el mismo modelo del Form Builder. La toolbar expone el acceso solo en tablet/móvil; el panel conserva el `app-bounded-select` compartido, el estado de `formulaVersionId`, el preview de la expresión y el cierre por clic externo. El Escape del selector se detiene en el componente compartido para respetar la prioridad desplegable → Inspector → modal.

```ini
BASE_SHA=32a6528e06bf8e311b3679a713eee4b947a7e7bf
B04_INSPECTOR_DESKTOP=PASS
B04_INSPECTOR_TABLET=PASS
B04_INSPECTOR_MOBILE=PASS
B04_FORMULA_SELECTOR_RESPONSIVE=PASS
B04_NO_HORIZONTAL_OVERFLOW=PASS
B04_FORMULA_VERSION_BINDING=PASS
B04_OPTION_FULL_TEXT=PASS
B04_NATIVE_TOOLTIP_INTERFERENCE=0
B04_OPTION_HORIZONTAL_OVERFLOW=0
B04_SHARED_SELECTOR_REGRESSION=PASS
B04_PLAYWRIGHT_VISUAL=PASS
B01_PRESERVED=YES
B02_PRESERVED=YES
B03_PRESERVED=YES
B04_OWNER_VISUAL_ACCEPTANCE=PENDING
B04_FINAL_STATUS=OPEN
```

- Archivos funcionales: Form Builder padre, toolbar, `app-bounded-select` compartido y regresión Playwright.
- Pruebas verificadas: suite Angular `95` archivos / `991` pruebas `PASS`; ESLint `PASS`; build `PASS` con warnings preexistentes de presupuesto SCSS/CommonJS; validador B04 `PASS`; estructura y enlaces documentales `PASS`; Playwright Form Builder `15/15 PASS`.
- No ejecutado en esta intervención: Quality Gate remoto exact-SHA y validaciones Oracle/infraestructura; no se modificaron backend, Oracle institucional, producción, `main`, B05, Block 10, P06 ni P07.
- `SOURCE_CODE_SHA=0e0ab6d0e551e61f6c13ca2046eeb60b54a514ac`; `EVIDENCE_COMMIT_SHA=0e0ab6d0e551e61f6c13ca2046eeb60b54a514ac`; `GITHUB_REMOTE_VERIFIED=PENDING_PUSH`; `SECURITY_SCAN=PENDING_GITHUB_ACTIONS`; `REVIEW_READY=PENDING_GITHUB_ACTIONS`.

## Verificación remota posterior — Quality Gate verde

- `SOURCE_CODE_SHA=0e0ab6d0e551e61f6c13ca2046eeb60b54a514ac` — código funcional B04 preservado y certificado.
- `EVIDENCE_COMMIT_SHA=bbe7aae6ee0a9887165b2af4b07879afe2009383` — expediente B04 publicado en `origin/desarrollo`.
- `QUALITY_GATE_RUN=43345092363`; [Quality Gates](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/43345092363); `QUALITY_GATE_SHA=bbe7aae6ee0a9887165b2af4b07879afe2009383`; `STATUS=completed`; `CONCLUSION=success`.
- `GITHUB_REMOTE_VERIFIED=YES`; `SECURITY_SCAN=PASS_GITHUB_ACTIONS`; `REVIEW_READY=YES` para revisión técnica; `GITHUB_EVIDENCE_INDEX_URL=https://github.com/jmejia31/RIESGO_LAVADO/blob/bbe7aae6ee0a9887165b2af4b07879afe2009383/docs/0.0%20Documentaci%C3%B3n/B04_ESTANDAR_MENUS_SELECTORES.md`; `GITHUB_SIGNOFF_URL=https://github.com/jmejia31/RIESGO_LAVADO/issues/23`.
- Los dos jobs remotos terminaron `success`; la verificación incluyó analizadores, ESLint, build Release, Oracle XE desechable, quality gates, pruebas, E2E, cálculos workbook, contenedores y usuarios non-root.
- `B04_OWNER_VISUAL_ACCEPTANCE=PENDING`; `B04_FINAL_STATUS=OPEN`. La aprobación visual del propietario sigue siendo el único cierre funcional pendiente.

## Corrección del gate E2E y Quality Gate final

- El intento `43347184650` quedó `failure` únicamente porque la auditoría de cuatro viewports agotó el timeout global de 30 s en la ejecución integral; no fue un fallo funcional del Inspector ni del selector compartido.
- Corrección publicada en `7a538f139665962e38295075bc30f02aad483be8`: la prueba usa la apertura estable del selector y un timeout de 120 s para completar los cuatro viewports conservando todas las aserciones.
- Playwright local posterior: `15/15 PASS`.
- `SOURCE_CODE_SHA=0e0ab6d0e551e61f6c13ca2046eeb60b54a514ac`; `EVIDENCE_COMMIT_SHA=7a538f139665962e38295075bc30f02aad483be8`.
- `QUALITY_GATE_RUN=43349408095`; [Quality Gates final](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/43349408095); `QUALITY_GATE_SHA=7a538f139665962e38295075bc30f02aad483be8`; `STATUS=completed`; `CONCLUSION=success`.
- `GITHUB_REMOTE_VERIFIED=YES`; `SECURITY_SCAN=PASS_GITHUB_ACTIONS`; `REVIEW_READY=YES`; `GITHUB_SIGNOFF_URL=https://github.com/jmejia31/RIESGO_LAVADO/issues/23`.
- `B04_OWNER_VISUAL_ACCEPTANCE=PENDING`; `B04_FINAL_STATUS=OPEN`.
