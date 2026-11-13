# B05 — Estándar del constructor de formularios dinámicos

## Expediente de la intervención

- `PHASE=B05_COLUMNS_SELECTOR_INDICATOR_REFINEMENT`
- `BASE_SHA=4bdd986d08db8bd0514571a57bb0baf857d99ab1`
- `BRANCH=desarrollo`
- `AUTHOR=COD / CODEX`
- `CLIENT=CLI`
- `OWNER_SIGNOFF_B05=https://github.com/jmejia31/RIESGO_LAVADO/issues/40`
- `B05_ACTION_MENU_OWNER_VISUAL_ACCEPTANCE=APPROVED`
- `B05_CLOSE_TOOLTIP_OWNER_VISUAL_ACCEPTANCE=APPROVED`
- `B05_COLUMNS_SELECTOR_VISUAL_ACCEPTANCE=PENDING`
- `B05_OVERALL_OWNER_VISUAL_ACCEPTANCE=PENDING`
- `B05_FINAL_STATUS=PENDING_OWNER_ACCEPTANCE`

Esta intervención continúa el B05 P0 de expresiones largas y mantiene sus SHA históricos. El alcance aplicado es la corrección quirúrgica del menú superior «Acciones» del Form Builder y del tooltip institucional «Cerrar constructor», sin reconstruir el Form Builder ni alterar su contrato.

## Correcciones aplicadas

- El disparador superior usa la iconografía SVG institucional `more-actions` y `collapse`, con un único chevrón sincronizado con `aria-expanded`; se retiraron los caracteres Unicode y el CSS obsoleto del antiguo `details`/popover.
- Las acciones conservan sus IDs, condiciones, eventos, títulos y valores, pero ahora exponen las etiquetas visibles `Crear nueva sección` y `Crear nuevo catálogo` junto al icono y con `role="menuitem"`.
- `app-institutional-action-menu` conserva el posicionamiento fijo institucional, scroll y límites de viewport. Además, el `panelId` se deriva del `triggerId` real para que `aria-controls` apunte al panel correcto, y `Escape` cierra el menú incluso dentro del bloqueo de eventos del modal.
- `InstitutionalTooltipDirective` distingue foco programático, pointer y teclado: el foco programático ya no abre tooltips negros invasivos; hover y navegación por teclado continúan mostrando el nombre accesible; `Escape`, `focusout` y destrucción limpian la capa y `aria-describedby`.
- Las regresiones cubren el menú compartido, toolbar, tooltip, foco/teclado y Chromium real. No se modificaron fórmulas, bindings, backend, Oracle, auditoría, autorizaciones ni persistencia.

## Regla visual canónica

El contenido técnico extenso usa la clase compartida `.form-builder-long-content`. La regla:

- conserva el texto original completo;
- permite encogimiento seguro dentro de grids y flex (`min-width: 0`);
- distribuye palabras y tokens largos (`white-space: normal`, `overflow-wrap: anywhere`, `word-break: break-word`);
- no usa `text-overflow: ellipsis` ni `overflow: hidden` para ocultar expresiones;
- mantiene la geometría del contenedor y evita desbordamiento horizontal del documento.

La misma regla se consume en la tarjeta de fórmula del lienzo y en `DynamicFieldRendererComponent`. El inspector conserva su preview y bounded select B04; no se duplicó estado, renderer, serializador ni motor matemático.

## Contratos preservados

No se modificaron valores, eventos, identificadores, persistencia ni contratos REST. En particular permanecen sin transformación:

`formulaVersionId`, `formulaId`, `formulaCodigo`, `formulaVersion`, `formula`, `formulaFuente`.

Tampoco se modificaron backend, Oracle institucional, producción, `main`, migración 54, B06 ni Block 10. Los cambios locales ajenos al alcance permanecieron fuera del stage.

## Evidencia fresca de esta intervención

### Pruebas automatizadas

- Unit focalizadas finales del menú compartido: `3/3 PASS`.
- Suite Angular completa posterior a la corrección: `95 archivos / 995 pruebas PASS`.
- ESLint: `PASS`.
- Build Angular: `PASS`; permanecen únicamente warnings preexistentes de presupuesto SCSS y CommonJS (`exceljs`), sin error de compilación.
- B04 selector/menu: `2/2 PASS`; self-test y auditoría estática: `PASS`, `63` selectores y `2` menús revisados.
- Estructura del repositorio: `PASS` (`118` rutas obligatorias, `1237` archivos rastreados).
- Enlaces documentales: `PASS` (`227` documentos, `237` enlaces locales).
- Skills institucionales: `14/14 PASS`.
- Validador de diálogos nativos: `6/6 PASS`; usos productivos `0`.

La regresión unitaria heredada mantiene las fórmulas de 50, 100, 250 y 500 caracteres y la preservación exacta de valores. Las nuevas regresiones comprueban iconografía, etiquetas, `role="menuitem"`, `aria-controls`, cierre Escape y ausencia de tooltip con foco programático.

### Navegador real

Playwright Chromium real verificó el menú, tooltip y selector compartido. La aserción B05 mide texto completo, `scrollWidth <= clientWidth`, wrapping, límites del panel y ausencia de overflow horizontal en:

- `390x844`
- `768x1024`
- `1366x768`
- `1536x864`

Resultado local final: `2/2 PASS` en los escenarios B05 focalizados; la auditoría compartida recorrió los cuatro viewports y registró `4/4 PASS` internamente. El caso editable de acciones/tooltip en `1536x1024` terminó `1/1 PASS`.

### Quality Gate local

`tools/run_quality_gates.ps1` avanzó en verde por los validadores institucionales y compiló el backend, pero no pudo completar la cobertura por una dependencia externa del checkout: `Database:Profile=LOCAL_XE` no está configurado. Resultado observado: `1099` pruebas backend superadas, `8` fallidas por ausencia de perfil Oracle local y `2` omitidas; no se modificó Oracle ni se alteró ninguna configuración para ocultar el bloqueo. La certificación integral queda delegada al Quality Gate de GitHub Actions, que dispone del Oracle XE desechable.

## Resultado B05

```ini
B05_ACTION_MENU_ICONOGRAPHY=PASS
B05_ACTION_MENU_LABELS=PASS
B05_ACTION_MENU_POSITIONING=PASS
B05_ACTION_MENU_KEYBOARD_ESCAPE=PASS
B05_PROGRAMMATIC_FOCUS_TOOLTIP=0
B05_NATIVE_TOOLTIP_INTERFERENCE=0
B05_OPTION_HORIZONTAL_OVERFLOW=0
B05_SHARED_ACTION_MENU_REGRESSION=PASS
B05_CLOSE_TOOLTIP_REGRESSION=PASS
B05_PLAYWRIGHT_VIEWPORTS=390x844,768x1024,1366x768,1536x864
B05_PLAYWRIGHT_VISUAL=PASS
B05_LONG_FORMULA_FULL_TEXT=PASS
B05_FORMULA_VALUE_PRESERVED=PASS
B05_FORMULA_HORIZONTAL_OVERFLOW=0
B05_DOCUMENT_HORIZONTAL_OVERFLOW=0
B05_SHARED_LONG_CONTENT_RULE=PASS
B05_FORM_BUILDER_REGRESSION=PASS
B05_FORMULA_BINDINGS_PRESERVED=PASS
B05_UNIT_LENGTHS_50_100_250_500=PASS
B01_PRESERVED=YES
B02_PRESERVED=YES
B03_PRESERVED=YES
B04_PRESERVED=YES
B05_TECHNICAL_STATUS=PARTIAL
B05_OWNER_VISUAL_ACCEPTANCE=PENDING
B05_FINAL_STATUS=PENDING_OWNER_ACCEPTANCE
```

`PARTIAL` identifica que esta entrega certifica técnicamente las correcciones P0 de contenido largo, menú y tooltip; no sustituye la revisión visual del propietario. No se emite cierre visual ni aprobación por parte de Codex.

## Publicación y verificación remota

- `SOURCE_CODE_SHA=0f0cc7220b7347f2d588a2e90c3383ccb7a2f31f`
- `EVIDENCE_COMMIT_SHA=299773a5811b6898681cb364d5133b6882244506`
- `GITHUB_EVIDENCE_INDEX_URL=https://github.com/jmejia31/RIESGO_LAVADO/blob/299773a5811b6898681cb364d5133b6882244506/docs/0.0%20Documentaci%C3%B3n/B05_ESTANDAR_CONSTRUCTOR_FORMULARIOS_DINAMICOS.md`
- `GITHUB_SIGNOFF_URL=https://github.com/jmejia31/RIESGO_LAVADO/issues/40`
- `GITHUB_REMOTE_VERIFIED=YES`
- `QUALITY_GATE_RUN=43448960282`
- `QUALITY_GATE_SHA=0f0cc7220b7347f2d588a2e90c3383ccb7a2f31f`
- `QUALITY_GATE_STATUS=completed`
- `QUALITY_GATE_CONCLUSION=success`
- `SECURITY_SCAN=PASS_GITHUB_ACTIONS`
- `REVIEW_READY=YES`
- `DOCUMENTAL_QUALITY_GATE_RUN=43452216100`
- `DOCUMENTAL_QUALITY_GATE_SHA=299773a5811b6898681cb364d5133b6882244506`
- `DOCUMENTAL_QUALITY_GATE_STATUS=completed`
- `DOCUMENTAL_QUALITY_GATE_CONCLUSION=success`

El Quality Gate remoto exacto [43448960282](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/43448960282) terminó verde sobre `SOURCE_CODE_SHA`, y el gate documental [43452216100](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/43452216100) terminó verde sobre `EVIDENCE_COMMIT_SHA`; ambos jobs en `success`. El expediente queda publicado para revisión técnica; la aprobación visual del propietario continúa pendiente y por eso B05 no se cierra.

## B05 P0 — Indicador duplicado del selector de columnas (2026-11-13)

- `EVENT_ID=RL-B05-COLUMNS-SELECTOR-INDICATOR-20261113`; `INITIAL_SHA=22a24bc9ae85daf4eb045fe1954798a9b619ff9c`; `SOURCE_CODE_SHA=f9441e9a2772f2421c91a095b2139fb97f737c14`; rama `desarrollo`; autor `COD / CODEX`; cliente `CLI`.
- Reproducción fresca en Chromium antes de editar: el `<select>` reportó `appearance=none` y `-webkit-appearance=none`, pero `backgroundImage` todavía contenía el SVG de Tailwind Forms; el wrapper ya mostraba el SVG institucional propio. Esa combinación era la causa concreta del doble chevron observado.
- Corrección quirúrgica: el selector de columnas conserva `[ngModel]="sec.columnasPorFila"`, `(ngModelChange)="columnasSeccionChange.emit({ seccionId: sec.id, columnas: +$event })"`, opciones `1/2/3/4/6`, grilla, modo solo lectura y teclado. La regla scoped `.form-builder-canvas__columns-control` fija `appearance`, `-webkit-appearance` y `background-image: none`; el SVG institucional único queda marcado como decorativo y accesible con `aria-hidden`.
- No se modificaron menú Acciones ni tooltip Cerrar, formulas, IDs, serialización, persistencia, backend, Oracle institucional, producción, `main`, migración 54, B06 ni Bloque 10.
- Archivos funcionales publicados en el commit `f9441e9a2772f2421c91a095b2139fb97f737c14`: canvas HTML/SCSS/spec y regresión Playwright `modal-shell-lock.spec.ts`.

### Verificación fresca de esta corrección

- Unit focalizada del canvas: `1 archivo / 25 pruebas PASS`.
- Suite Angular completa: `95 archivos / 995 pruebas PASS`.
- ESLint: `PASS`; build: `PASS`, con warnings preexistentes de presupuesto SCSS y CommonJS `exceljs`.
- Playwright Chromium real: `1/1 PASS` del selector de columnas en `390x844`, `768x1024`, `1366x768` y `1536x864`; la regresión ampliada relacionada terminó `4/4 PASS` incluyendo fórmula, auditoría compartida y menú contextual.
- El caso Playwright verifica valores exactos, `ngModel` numérico, navegación `ArrowDown`, actualización `repeat(3, ...)` de la grilla, `appearance=none`, `-webkit-appearance=none`, `backgroundImage=none`, un único SVG visible alineado y ausencia de overflow horizontal.
- Validadores frescos: B04 `PASS` (`63` selectores, `2` menús, `48` nativos, `5` bounded, `0` no revisados); estructura `PASS` (`118` rutas, `1237` archivos); base de datos `PASS`; enlaces `PASS` (`227` documentos, `237` enlaces); diálogos nativos `6/6 PASS`; Agent Skills `14/14 PASS`.
- Quality Gate local: `BLOCKED_EXTERNAL`, no por esta corrección: `1099` pruebas backend pasaron, `8` fallaron al no existir `Database:Profile=LOCAL_XE` y `2` fueron omitidas. No se modificó configuración Oracle para ocultar el bloqueo.

```ini
B05_COLUMNS_SELECTOR_ROOT_CAUSE=TAILWIND_BACKGROUND_IMAGE_WITH_CUSTOM_SVG
B05_COLUMNS_SELECTOR_NATIVE_BACKGROUND=REMOVED
B05_COLUMNS_SELECTOR_SINGLE_CHEVRON=PASS
B05_COLUMNS_SELECTOR_OPTIONS=1,2,3,4,6
B05_COLUMNS_SELECTOR_NUMERIC_BINDING=PASS
B05_COLUMNS_SELECTOR_GRID_PERSISTENCE_CONTRACT=PASS
B05_COLUMNS_SELECTOR_READONLY=PASS
B05_COLUMNS_SELECTOR_KEYBOARD=PASS
B05_COLUMNS_SELECTOR_HORIZONTAL_OVERFLOW=0
B05_COLUMNS_SELECTOR_VIEWPORTS=390x844,768x1024,1366x768,1536x864
B05_COLUMNS_SELECTOR_PLAYWRIGHT=PASS
B05_ACTION_MENU_OWNER_VISUAL_ACCEPTANCE=APPROVED
B05_CLOSE_TOOLTIP_OWNER_VISUAL_ACCEPTANCE=APPROVED
B05_COLUMNS_SELECTOR_VISUAL_ACCEPTANCE=PENDING
B05_OVERALL_OWNER_VISUAL_ACCEPTANCE=PENDING
B05_ACTION_MENU_REWORK_AUTHORIZED=NO
B05_CLOSE_TOOLTIP_REWORK_AUTHORIZED=NO
B06_AUTHORIZED=NO
B05_TECHNICAL_STATUS=PARTIAL
B05_OWNER_VISUAL_ACCEPTANCE=PENDING
B05_FINAL_STATUS=PENDING_OWNER_ACCEPTANCE
```

`EVIDENCE_COMMIT_SHA`, el run remoto exacto del commit funcional y el enlace del expediente final se completarán después del push documental. La aprobación explícita recibida aplica únicamente a Acciones y Cerrar; B05 no se cierra sin aceptación visual global del propietario.

## Verificación remota y publicación final de esta intervención

- `SOURCE_CODE_SHA=f9441e9a2772f2421c91a095b2139fb97f737c14`.
- `EVIDENCE_COMMIT_SHA=802a5d80a8ac4a99af3a574cfed4841cfcf1d378`.
- Quality Gate exacto del expediente publicado: [run 43464664638](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/43464664638), `QUALITY_GATE_SHA=802a5d80a8ac4a99af3a574cfed4841cfcf1d378`, `STATUS=completed`, `CONCLUSION=success`; ambos jobs y todos los pasos de la matriz integral terminaron `success`.
- `GITHUB_EVIDENCE_INDEX_URL=https://github.com/jmejia31/RIESGO_LAVADO/blob/802a5d80a8ac4a99af3a574cfed4841cfcf1d378/docs/0.0%20Documentaci%C3%B3n/B05_ESTANDAR_CONSTRUCTOR_FORMULARIOS_DINAMICOS.md`.
- `GITHUB_SIGNOFF_URL=https://github.com/jmejia31/RIESGO_LAVADO/issues/40`; `GITHUB_REMOTE_VERIFIED=YES`; `SECURITY_SCAN=PASS_GITHUB_ACTIONS`; `REVIEW_READY=YES` para revisión técnica.
- El run funcional anterior `43462444453` fue cancelado por GitHub al entrar el push documental posterior; no se usa como certificación. La certificación vigente es exclusivamente `43464664638` sobre `EVIDENCE_COMMIT_SHA`.
- `B05_ACTION_MENU_OWNER_VISUAL_ACCEPTANCE=APPROVED`; `B05_CLOSE_TOOLTIP_OWNER_VISUAL_ACCEPTANCE=APPROVED`; `B05_COLUMNS_SELECTOR_VISUAL_ACCEPTANCE=PENDING`; `B05_OVERALL_OWNER_VISUAL_ACCEPTANCE=PENDING`; `B05_FINAL_STATUS=PENDING_OWNER_ACCEPTANCE`.
