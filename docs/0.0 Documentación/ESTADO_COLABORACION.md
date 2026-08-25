# Estado de colaboración y punto de continuidad

## Estado vigente - UI-FORM.4-R Inspector profesional

- Fecha: 2026-08-25 17:37 (UTC-6). Rama: `desarrollo`. HEAD inicial: `32e393c316a20fd8bc1fb6ba9f6241492ec19a21`.
- Estado: implementación funcional y visual completada sobre el único `FormBuilderInspectorComponent`; UI-FORM.2-R y UI-FORM.3-R permanecen congeladas.
- Composición: panel derecho con título/subtítulo, empty state, filas visuales sin selección, contexto del campo, acordeones General, Validaciones, Catálogo / Datos, Apariencia, Condiciones inerte y Ayuda / Tooltip; scroll, densidad, bordes, colores y proporciones reconciliados al PNG.
- Contrato: 0 propiedades JSON nuevas; 0 tipos nuevos; 0 cambios en modelos, serializer, normalizador, backend, DB, migraciones, endpoints o dependencias. La matriz cubre exactamente los 9 tipos oficiales. Fórmula se conserva como texto y nunca se ejecuta en el Inspector.
- Modos: editable habilita solo mutaciones existentes; solo lectura mantiene el Inspector visible y deshabilita mutaciones; publicada/bloqueada/procesando siguen gobernadas por `bloqueadoParaMutacion` existente.
- Evidencia visual: PNG usado como fuente permanente; flujo autenticado con mocks E2E y capturas reales a 1536x1024 en editable para campo básico texto, selección `selector-catalogo` y campo avanzado `formula`, revisadas lado a lado. Capturas temporales eliminadas. Condiciones queda visualmente inerte por ausencia de soporte contractual.
- Gates frescos: focalizada 31/31 PASS; frontend 63/63 / 688/688 PASS; backend 494/494 PASS; E2E 17/17 PASS; coverage 62.03% / 56.94% / 58.04% / 62.16%; lint/build PASS; Quality Gates PASS; BD/documentación PASS; `git diff --check` PASS. Build mantiene advertencia SCSS del Inspector sobre presupuesto y CommonJS `exceljs` preexistente.
- Restricción heredada: `validate_repository_structure.ps1` sigue reportando `core/services/global-http-state.service.ts` y su carpeta, fuera de este alcance.
- Punto exacto de continuación: documentación terminada; ejecutar stage explícito, diff cached check, commit `fix(ui-form-4): reconciliar inspector con prototipo aprobado`, push a `origin/desarrollo`, fetch y verificación de sincronización. No iniciar UI-FORM.5-R/UI-FORM.6-R.

## Estado vigente - UI-FORM.3-R Reconciliación visual oficial

- Fecha: 2026-08-25. Rama: `desarrollo`. HEAD de entrada: `279e9ae75e84e58256866fee963c9b86aaa621f6`.
- Alcance: solo Lienzo, Secciones, Field Cards, columnas, selección visual y drop zones. UI-FORM.2-R permanece congelada; UI-FORM.4-R, UI-FORM.5-R y UI-FORM.6-R no fueron abiertas.
- Cambio vigente: Canvas sin marco duplicado, composición más densa y alineada al PNG, headers con badge/título/selector, Field Cards con previews existentes y aplicación visual de `anchoColumnas`; drop zone compacta integrada al final de cada sección poblada e inerte en solo lectura.
- Acciones: no se inventaron duplicar/menu porque no existen funcionalmente. Se preservaron eliminar/reordenar existentes y sus guardas de mutabilidad.
- Contrato: 0 cambios JSON, 0 propiedades nuevas, 0 tipos nuevos, serializer/normalizador intactos; backend, DB, migraciones, endpoints y dependencias sin cambios.
- Evidencia visual: PNG observado y usado como referencia; captura autenticada real en 1536x1024 con dos secciones/dos columnas comparada lado a lado. La captura temporal fue eliminada. El alto global 95.31% permanece como hallazgo UI-FORM.1 fuera de alcance.
- Gates: focalizadas 33/33 PASS; frontend 688/688 PASS; backend 494/494 PASS; E2E 17/17 PASS; lint/build PASS; coverage 62.03%/56.94%/58.04%/62.16%; quality gates y validaciones BD/documentación PASS. Estructura conserva el hallazgo heredado `core/services/global-http-state.service.ts` fuera de alcance.
- Próximo paso: commit/push fail-closed, luego `HEAD == origin/desarrollo`, ahead/behind `0/0` y worktree limpio.

## Estado vigente - UI-FORM.2-R Reapertura visual oficial

- Fecha: 2026-08-25. Rama: `desarrollo`. HEAD inicial conocido: `d7eb6aa10d61e4e33ccd4e8937d2f3f1b8de5bb3`. El pre-flight remoto quedó limitado por permisos sobre `.git/index.lock` / `.git/FETCH_HEAD`; no se ejecutó reset, limpieza ni reescritura.
- Alcance: únicamente Biblioteca de Campos del FormBuilder contra `docs/11. Prototipos/CONSTRUCTOR DE FORMULARIO DINAMICOS.PNG`. UI-FORM.3-R, UI-FORM.4-R, UI-FORM.5-R y UI-FORM.6-R permanecen fuera de alcance.
- Cambio vigente: el panel izquierdo mantiene “Agregar campos” y la biblioteca de 9 tipos en editable y solo lectura. Búsqueda, agrupación, cards, iconos, descripciones y handle se presentan con la composición del prototipo; solo lectura conserva la identidad y bloquea mutaciones.
- Evidencia local: frontend **63/63 archivos y 686/686 pruebas PASS**, lint PASS, build PASS, `git diff --check` PASS. Build mantiene advertencia no bloqueante de presupuesto SCSS (117 bytes) y CommonJS `exceljs`.
- Estado de certificación: **NO CERRADA**. Gate funcional PASS; gate de regresión global pendiente; gate visual pendiente de captura autenticada del Constructor. Chrome headless sí renderizó `/login`, pero no se logró montar el flujo autenticado con mocks del E2E porque el navegador administrado de Playwright no está instalado en el entorno.
- Restricciones: 0 backend, 0 DB/SQL, 0 endpoints, 0 dependencias nuevas, 0 tipos nuevos, 0 cambios de contrato JSON, 0 biblioteca paralela, `main` sin modificaciones.
- Próximo paso exacto: ejecutar visual E2E autenticado en viewport 1536x1024 con evidencia lado a lado contra el PNG; luego completar backend/E2E/coverage/quality gates, documentar resultado real, commit y push a `origin/desarrollo`.

## Estado vigente - UI-FORM.6

- Fecha: 2026-08-25. Rama de trabajo: `desarrollo`.
- HEAD inicial de esta intervención: `dbe31e285fc0549a4a80434a6e6072b60c080162`.
- Quality Gate Run `32895118559` quedó corregido localmente: el fallo era el selector E2E obsoleto `.form-builder-modal-card`; el selector vigente es `.modal-container-card.modal-size-workspace`.
- UI-FORM.6 integrada en el FormBuilder existente: Preview read-only con `DynamicFieldRendererComponent`; JSON Técnico con fuente serializada contractual, copiar exacto, búsqueda literal, navegación de coincidencias y validación sin aplicar ni guardar.
- Evidencia local: suite frontend 63/63 archivos y 686/686 pruebas PASS; coverage Statements 61.99%, Branches 56.88%, Functions 57.93%, Lines 62.13%; lint, build, E2E 17/17, backend 494/494 y `tools/run_quality_gates.ps1` PASS.
- Restricciones confirmadas: no se modificó `main`, backend, Oracle, SQL, secretos, gates, exclusiones, dependencias ni contratos persistentes. UI-FORM.2-.5 quedan congeladas sin regresión.
- Próximo paso: commit exclusivo de UI-FORM.6/fix E2E y publicación normal en `origin/desarrollo`; no promover a `main` en esta ejecución.

**Actualización:** 2026-08-25 12:09 — Centralización de variantes de tamaño de modales por AntiG
**Proyecto:** RIESGO_LAVADO / SGRLA-IHSS  
**Rama autorizada:** `desarrollo`  
**PR rector:** #20 `desarrollo -> main` — OPEN / DRAFT / NOT MERGED  
**`main`:** protegida en `727082c6fcf90f95ce6db5eadf5c4b152397d080`; no modificar sin autorización expresa  
**Usuario QA oficial único vigente:** `cuentajavier419@gmail.com`  
**Oracle:** 0 DDL/DML manuales; 0 scripts manuales ejecutados; `B10_*` intactos  

---

## Actualizacion de cierre visual UI-FORM.2-R - 2026-08-25 15:35

- Gate visual de la Biblioteca PASS con captura autenticada real en viewport 1536x1024 contra el PNG aprobado. La desviacion 95.31% de alto del modal pertenece al contrato global del shell y queda fuera de esta reapertura.
- Gate funcional PASS: 686/686 frontend. Regresion PASS: 494/494 backend y 17/17 E2E. Coverage frontend: Statements 61.98%, Branches 56.94%, Functions 57.98%, Lines 62.12%. `tools/run_quality_gates.ps1` PASS; pendiente únicamente commit/push y verificación remota de sincronización.

## Estado ejecutivo correcto

| Fase | Estado | Evidencia principal |
|---|---|---|
| F5.1 — Núcleo del renderer | ✅ COMPLETA | Certificación previa |
| F5.2 — Certificación integral renderer | ✅ COMPLETA | Certificación previa |
| F5 — Cierre global | ✅ CERRADA | Baseline final `7692c5fd14c3058b17b6245ca596b931ac844009` |
| **F6.0 — Auditoría del contrato JSON/catálogos** | **✅ COMPLETA** | `F6.0_AUDITORIA_CONTRATO_JSON_CATALOGOS.md` + anexos/fixture |
| **F6.1 — Normalización/validación contractual lossless** | **✅ COMPLETA Y CERTIFICADA** | HEAD `4d6c905e067ca9733de56e5d5de099d8fe65178f`; Quality Gates #1121 SUCCESS |
| **F6.2 — Administración visual de catálogos** | **✅ COMPLETA Y CERTIFICADA** | HEAD auditado `f3b3057a78f0444960f40b975584ab344345d2dd`; Quality Gates #1138 / Run `32381878501` SUCCESS |
| **F6.3 — Persistencia bidireccional de plantilla** | **✅ COMPLETA Y CERTIFICADA** | HEAD auditado `f223f4d6e3ee9f77590709bb465a8d99e7946eb1`; Quality Gates #1155 / Run `32397277572` SUCCESS |
| **F6.4 — Publicación y ciclo de vida de versiones** | **✅ COMPLETA Y CERTIFICADA** | UAT en Navegador Real CERTIFICADA en `localhost`; 425/425 Backend PASS; 426/426 Frontend PASS; 14/14 Playwright PASS; 0 cambios Oracle/SQL |
| **F6.5 — Integridad de evaluaciones versionadas y respuestas de catálogo** | **✅ IMPLEMENTADA Y PROBADA LOCALMENTE** | 436/436 Backend PASS; 444/444 Frontend PASS; 14/14 Playwright PASS; Quality Gates Locales SUCCESS; SonarCloud Remoto pendiente/diferido al cierre global |
| **F6.5.FAM.1 — Garantías backend del gestor de familias** | **✅ CERRADA LOCALMENTE** | Cierre formal Codex en `0c4d29b`: 29/29 FAM PASS y 494/494 Release PASS. Activación idempotente, desactivación protegida, eliminación segura, auditoría y autorización verificadas. SonarCloud remoto queda diferido al cierre global; no se declara aprobado. |
| **F6.5.FAM.2 — Gestor visual de Familias de Formularios** | **✅ CERTIFICADA Y PUBLICADA** | Subpanel y modal Administrar Familias rediseñado profesionalmente: max-w-6xl, columnas optimizadas (sin descripción en tabla, disponible en Ver), fecha en español (dd/MM/yyyy), botones iconográficos compactos con aria-label, filtro con botón Limpiar. 441/441 Frontend PASS (48/48 suites); 14/14 Playwright E2E PASS; Build SUCCESS. |
| **UI-FAM.1 — Gestor principal de Familias** | **✅ CERTIFICADA LOCALMENTE** | Reemplaza la entrada principal de Plantillas por KPIs, búsqueda, filtros, paginación, tabla de familias y acciones contextuales conectadas al contrato existente. Corrección Codex sobre `cfae4cf`: 451/451 frontend, 14/14 E2E, 494/494 backend Release; cobertura local del componente principal: 86.99% líneas. SonarCloud remoto diferido al cierre global. |
| **UI-FAM.2 — Detalle de familia en modal** | **✅ CERTIFICADA LOCALMENTE** | Modal XL encapsulado, carga autoritativa por `famId`, historial real por `famCodigo`, estados loading/error/404/reintento, cancelación de respuestas tardías, foco y cierre accesible. `npm run test:coverage`: 50/50 suites y 461/461 pruebas; Playwright 17/17; Backend Release 494/494; Quality Gates locales SUCCESS. SonarCloud remoto queda diferido al cierre global. |
| **UI-FAM.3 — Crear familia en modal** | **✅ CERRADA Y CERTIFICADA LOCALMENTE** | Implementación publicada previamente; corrección mínima de tipado DI sin cambios visuales y regresión E2E alineada con UI-FAM.2 integrada. 51/51 suites y 473/473 frontend PASS; 17/17 Playwright PASS; 494/494 backend Release PASS; lint, build y Quality Gates locales PASS. SonarCloud remoto diferido al cierre global. |
| **UI-FAM.4 — Editar familia y ciclo de vida** | **⏳ PENDIENTE** | Código inmutable, edición de Nombre/Descripción y acciones explícitas de activar/desactivar/eliminar con confirmaciones y reglas del backend. |
| **UI-FAM.QA — Integración/certificación final** | **⏳ PENDIENTE** | Certificación conjunta de las cuatro interfaces, accesibilidad, responsive, errores, permisos y regresión. |
| **UI-FORM.1 — Integración Workspace V2 Shell y Layout** | **✅ COMPLETA Y CERTIFICADA** | Shell de 5 regiones V2 integrado en FormBuilderComponent productivo. 527/527 frontend, 494/494 backend, 17/17 Playwright, coverage y Quality Gates locales PASS. |
| **UI-FORM.2 — Biblioteca y estructura del formulario** | **✅ IMPLEMENTACIÓN COMPLETA Y VALIDADA LOCALMENTE** | Búsqueda insensible a mayúsculas/acentos, 3 categorías canónicas (Básicos, Selección, Avanzados), tarjetas pro con icono SVG/handle, drag & drop con payload seguro (`tipo`), drop-zones visuales por sección, validación segura en motor `FormBuilderComponent`, auto-selección en inspector, bloqueo ESC. 577/577 frontend PASS (59 suites), lint PASS, build PASS. |
| **UI-FORM.3 — Lienzo y secciones** | **✅ IMPLEMENTACIÓN COMPLETA Y VALIDADA LOCALMENTE** | Field Cards pro, selección visual inequívoca sincronizada con inspector, header de sección con badge y selector numérico de columnas por fila (1, 2, 3, 4, 6), acciones agrupadas con boundaries, drop-zones compactas, HARD GATE JSON lossless (0 propiedades UI persistidas, 9 tipos exactos). 596/596 frontend PASS (60 suites), lint PASS, build PASS. |
| **UI-FORM.4 — Inspector profesional y edición avanzada** | **✅ IMPLEMENTACIÓN COMPLETA Y VALIDADA LOCALMENTE** | Inspector estructurado en 4 grupos (`General`, `Reglas`, `Datos`, `Presentación`), empty-state alineado al prototipo, única fuente de verdad para capacidades en `TipoControlDefinicion` (0 fallbacks hardcodeados), preview en Canvas para `placeholder` y `opciones` reales de radio, fórmula con `soloLectura=true` bloqueado, regla `Hidden != Delete`, opciones `string[]`, HARD GATE JSON (0 propiedades inventadas, 0 propiedades UI serializadas). 630/630 frontend PASS (61 suites), coverage real (Sentencias: 61.34%, Ramas: 56.04%, Funciones: 57.49%, Líneas: 61.64%), lint PASS, build PASS. |
| **UI-FORM.5 — Estados y ciclo de edición** | **✅ IMPLEMENTACIÓN COMPLETA Y VALIDADA LOCALMENTE** | DRAFT no vigente editable real; solo lectura autoritativo en estados no DRAFT, DRAFT vigente y usuarios no-administradores; toolbar profesional OnPush con badge humano (`BORRADOR`, `EN REVISIÓN`, `APROBADA`, `PUBLICADA`, `RETIRADA`, `ARCHIVADA`), botón "Guardar Borrador" con verificación semántica post-guardado, botón "Publicar Versión" con confirmación única y bloqueo transitorio real de Palette, Canvas, Inspector, Catálogos y JSON durante operaciones; etiquetas de procesamiento precisas e independientes (`Guardando...` vs `Publicando...`); Tailwind 3.4 standard (`py-0.5`). 683/683 frontend PASS (63 suites), coverage real (Sentencias: 61.77%, Ramas: 56.68%, Funciones: 57.77%, Líneas: 61.95%), lint PASS, build PASS. |

---

## UI-FORM.5 — Estados y Ciclo de Edición del Form Builder — 2026-08-25

- **Ciclo y Estados Autoritativos**:
  - **DRAFT No Vigente (Admin)**: Builder totalmente editable (Palette, Canvas, Inspector, Catálogos, JSON).
  - **Solo Lectura Real Contractual**: `soloLecturaDefinicion` bloquea mutaciones locales en estados contractuales no DRAFT (`IN_REVIEW`, `APPROVED`, `PUBLISHED`, `RETIRED`, `ARCHIVED`), cuando la versión es vigente o para usuarios no-administradores (`!esAdministrador()`).
  - **Bloqueo Transitorio Real (`bloqueadoParaMutacion`)**: Derivación UI local `this.soloLectura || this.procesando || this.operacion !== null` que protege temporalmente Palette, Canvas, Inspector, Catálogos y JSON técnico durante operaciones de guardado y publicación (incluyendo la ventana HTTP POST -> GET) sin desvirtuar el estado contractual del badge.
  - **Toolbar Profesional**: Reflejo del estado real de versión mediante badge con traducción institucional humana (`BORRADOR`, `EN REVISIÓN`, `APROBADA`, `PUBLICADA`, `RETIRADA`, `ARCHIVADA`), sufijo `· SOLO LECTURA` cuando aplica, `ChangeDetectionStrategy.OnPush` y clase estándar Tailwind 3.4 `py-0.5`.
  - **Guardar Borrador**: Renombrado de acción a "Guardar Borrador", uso estricto del endpoint existente `actualizarBorradorFormulario`, validación previa de modelo y verificación semántica post-guardado con recuperación fresca del servidor. Label dinámico preciso ("Guardando...").
  - **Publicación y Reconciliación Autoritativa**: Emisión de intención de publicar desde el Builder hacia el orquestador `MatricesRiesgosComponent`, ejecución de `publicarVersionFormulario` tras confirmación única SweetAlert2, bloqueo de procesamiento durante todo el re-fetch autoritativo vía `obtenerVersionFormulario` para transición inmediata a modo solo lectura si la versión estaba abierta en el modal.
  - **Estado de Proceso Preciso**: Señal `operacionBuilderEnCurso` ('guardar' | 'publicar' | null) que previene etiquetas cruzadas ("Guardando..." vs "Publicando...") y limpia correctamente en cancelaciones, errores y éxito.
- **Defensa en Profundidad y Aislamiento Arquitectural**:
  - Guardas defensivas en todos los handlers mutables de `FormBuilderComponent` con `if (this.bloqueadoParaMutacion) return;`.
  - Guardas defensivas en handlers administrativos de `MatricesRiesgosComponent` con `if (!this.esAdministrador()) return;`.
  - `FormBuilderToolbarComponent` y `FormBuilderComponent` se mantienen 100% presentacionales y libres de dependencias a servicios HTTP / `MatricesRiesgosService`.
  - HARD GATE: 0 reglas de backend duplicadas, 0 state machines en Angular, 0 endpoints nuevos, 0 estados nuevos, 0 permisos inventados, 0 propiedades de workflow serializadas a JSON.
- **Evidencia propia**:
  - 63/63 suites y 683/683 pruebas frontend PASS (0 errores).
  - Cobertura frontend real (`npm run test:coverage`): **Sentencias = 61.77%, Ramas = 56.68%, Funciones = 57.77%, Líneas = 61.95%**.
  - `npm run lint` y `npm run build` PASS.
- **Punto de continuación**: Siguientes fases del Form Builder según hoja de ruta.

---

## UI-FORM.4 — Inspector Profesional por Propiedades Existentes — 2026-08-25

- **Arquitectura de 4 Grupos**:
  - **General**: `etiqueta`, `clave` (JSON Key), `tipo` (9 tipos oficiales de `TIPOS_CONTROLES_DISPONIBLES`), `descripcion`, badge read-only de categoría derivada.
  - **Reglas**: `obligatorio`, `soloLectura` (invariante de fórmula: forzado a `true` y bloqueado), `formula` (visible únicamente si `requiereFormula`).
  - **Datos**: Catálogo asociado (`codigoCatalogo`) y navegación a catálogos si `requiereCatalogo`; lista de opciones (`opciones: string[]`) con agregar/eliminar si `requiereOpciones`; nota informativa para tipos sin datos adicionales.
  - **Presentación**: `placeholder` (para tipos aplicables: `texto`, `numero`, `texto-largo`), `textoAyuda`, `anchoColumnas` (1 a 6 columnas).
  - **Empty State**: Icono de selección y mensaje orientativo ("Seleccione un campo en el lienzo para editar sus propiedades").
  - **Footer de Inspector**: Identificador (`ID del campo`) y tipo técnico en solo lectura.
- **Única Fuente de Verdad y Previews**:
  - `requiereCatalogo`, `requiereOpciones`, `requiereFormula` derivan exclusivamente de `definicionTipoActual` (`TipoControlDefinicion`).
  - Integración en Canvas de `cmp.placeholder` para `texto`, `numero`, `texto-largo` y renderizado de opciones reales de `radio` a partir de `cmp.opciones`.
- **Reglas de Integridad Contractual**:
  - `Hidden != Delete`: Cambiar de tipo no borra silenciosamente propiedades contractuales previas (`codigoCatalogo`, `opciones`, `formula`, etc.).
  - HARD GATE: 0 propiedades inventadas, 0 propiedades UI serializadas a JSON (`seccionesAbiertas` reside exclusivamente en estado UI del componente).
- **Evidencia propia**:
  - 61/61 suites y 630/630 pruebas frontend PASS (0 errores).
  - Cobertura frontend real (`npm run test:coverage`): **Sentencias = 61.34%, Ramas = 56.04%, Funciones = 57.49%, Líneas = 61.64%**.
  - `npm run lint` y `npm run build` PASS.
- **Punto de continuación**: Siguientes fases del Form Builder según hoja de ruta.

---

## UI-FORM.3 — Implementación, Lienzo y Hard Gate JSON — 2026-08-25

- **Field Cards Profesionales**:
  - Renderizado limpio con badge `CLAVE · TIPO`, etiqueta con indicador de obligatorio (`*`) y preview visual adaptado para los 9 tipos soportados.
  - Selección visual activa inequívoca (`border-blue-500`, `ring-2`, `ring-blue-500/20`) y sincronización inmediata con `campoActivo` e Inspector.
- **Secciones y Columnas**:
  - Encabezado con badge numérico de orden, título editable (`sec.titulo`), selector de columnas por fila (`sec.columnasPorFila`) con opciones 1, 2, 3, 4, 6 con emisión numérica y botón de eliminación condicional.
- **Acciones Agrupadas**:
  - Botones de mover arriba `▲`, mover abajo `▼` y eliminar `✕` compactos y agrupados, con boundaries estrictos (deshabilitados en límites) y ocultos en modo `soloLectura`.
- **Drop-Zones Compactas**:
  - Zona vacía orientativa limpia ("Arrastra un campo desde el panel izquierdo...") y drop-zone reactiva durante dragover ("Suelta el campo en «...»").
- **HARD GATE de Integridad JSON**:
  - 0 propiedades UI serializadas (`selected`, `expanded`, `dragging`, `uiState`, etc.).
  - Preservación estricta de `tipoOriginal`, `metadatosOriginales`, `anchoColumnas`, `columnasPorFila`.
  - 9 tipos exactos soportados (0 tipos nuevos inventados).
  - Round-trip 100% lossless.
- **Evidencia propia**:
  - 60/60 suites y 596/596 pruebas frontend PASS (0 errores).
  - Cobertura frontend: 61.08% sentencias, 55.77% ramas, 57.01% funciones, 61.34% líneas.
  - `npm run lint` y `npm run build` PASS.
- **Punto de continuación**: Fase UI-FORM.4 (Inspector de Propiedades y Edición Avanzada).

---

## UI-FORM.2 — Implementación, Corrección Modal y Cierre — 2026-08-25

- **Modal Geometry & Sizes**: Estandarización canónica global de modales en `src/styles.css` con variantes semánticas `.modal-size-sm`, `.modal-size-md`, `.modal-size-lg`, `.modal-size-xl`, `.modal-size-workspace`. Tecla Escape bloqueada institucionalmente con `(keydown.escape)="$event.preventDefault(); $event.stopPropagation()"`.
- **Biblioteca de Controles**:
  - Buscador reactivo por etiqueta, descripción, tipo y categoría normalizada, con botón de limpieza `✕`, contador de coincidencias y empty state ("No se encontraron campos compatibles").
  - 3 categorías oficiales: **BÁSICOS** (`texto`, `numero`, `fecha`, `texto-largo`), **SELECCIÓN** (`selector-catalogo`, `radio`, `catalogo-multiple`, `checkbox`), **AVANZADOS** (`formula`). Exactamente 9 tipos oficiales soportados, 0 tipos nuevos inventados.
  - Tarjetas compactas con SVG icon/handle, clave técnica, hover y cursor grab.
  - Estados editable vs solo lectura: en solo lectura muestra árbol de "Estructura del formulario" sin acciones de modificación; en editable sin sección activa muestra advertencia ("Selecciona una sección en el lienzo para agregar campos").
  - Drag & Drop: Palette transfiere únicamente el string `tipo`; Canvas detecta dragover/dragleave/drop y muestra drop-zones visuales por sección; `FormBuilderComponent` valida contra `TIPOS_CONTROLES_DISPONIBLES` y selecciona automáticamente el nuevo campo en el Inspector.
- **Evidencia propia**:
  - 59/59 suites y 577/577 pruebas frontend PASS (0 errores).
  - Cobertura frontend: 61.08% sentencias, 55.79% ramas, 57.01% funciones, 61.34% líneas.
  - `npm run lint` y `npm run build` PASS.
- **Punto de continuación**: Fase UI-FORM.3 (Lienzo, Reordenamiento y Operaciones de Sección).


---

## UI-FORM.1 — cierre local y preparación de certificación remota — 2026-08-25

- El commit de infraestructura CodexGraph se conservó aislado, se rebasó sin conflictos y se publicó como `e26c2ce149b1e834f0a51d357c799e7ac845fcae`.
- El primer fallo reproducible fue `matrices-riesgos.component.ciclo-vida.spec.ts:159`: la prueba esperaba texto visible en botones SVG y recibió cadenas vacías; el contrato productivo vigente usa `aria-label`.
- Las cinco expectativas unitarias restantes y el timeout E2E provenían de selectores/flujo heredados anteriores al gestor principal y a los modales standalone. El cambio 96vw × 94dvh no fue la causa.
- El fix `dfa85b3` modifica únicamente cuatro specs y valida acciones accesibles, vista principal real, delegación standalone y roles del Workspace V2.
- Evidencia local propia: 47/47 focalizadas, 527/527 frontend, 494/494 backend, 17/17 E2E, coverage frontend 57.32%/52.54%/53.80%/57.46%, coverage backend 26.85%/28.66%, build frontend PASS, ESLint PASS, Roslyn 0 errores y `run_quality_gates.ps1` PASS.
- Cero cambios en código productivo, backend, Oracle, contratos, workflows, umbrales, exclusiones, `main` o UI-FORM.2.

**Estado:** cierre local completo; no declarar UI-FORM.1 certificada ni habilitar Fase 1 Punto 3 hasta que Quality Gates y Sonar Analysis sean SUCCESS sobre el SHA final publicado.

---

## UI-FORM.1 — auditoría y corrección Codex — 2026-08-24

- Se preservó `FormBuilderComponent` como único motor de estado, normalización, serialización, catálogos y persistencia.
- Las cinco regiones V2 continúan siendo componentes presentacionales conectados mediante entradas y salidas tipadas.
- El workspace interno ya no declara un segundo diálogo; el modal exterior de Matrices conserva la autoridad semántica, el bloqueo del shell y la gestión de foco.
- El tamaño institucional del Form Builder se expresa mediante `form-builder-modal-card`, sin selectores dependientes del texto de un atributo `style`.
- Evidencia propia: 57/57 pruebas focalizadas PASS, lint PASS, build PASS y 17/17 E2E PASS. La suite completa registra 519/525 por seis specs UI-FAM heredados que todavía esperan la presentación anterior; no se modificaron por estar fuera de UI-FORM.1.
- Cero cambios en backend, Oracle/SQL, contratos JSON, workflows, SonarCloud, `main` o UI-FORM.2.

**Estado:** `UI-FORM.1 = ✅ CORREGIDA Y LISTA PARA REVISIÓN DE CHATGPT`.

---

## UI-FAM.3 — cierre técnico local — 2026-08-23

- El modal **Nueva Familia** conserva intactos su diseño y comportamiento funcional.
- Se corrigió únicamente el tipado de `ElementRef` en la inyección usada por la trampa de foco, que impedía compilar `test:coverage`.
- Se actualizó una expectativa E2E heredada que todavía exigía el puente antiguo de UI-FAM.2; la prueba ahora valida la integración real de versiones ya presente, sin modificar producción.
- Evidencia: `test:coverage` **473/473 PASS**, lint PASS, build PASS, Playwright **17/17 PASS**, backend Release **494/494 PASS**, validadores BD/documentación y Quality Gates locales PASS.
- `validate_repository_structure.ps1` mantiene un hallazgo preexistente fuera del alcance en `core/services/global-http-state.service.ts`.
- SonarCloud remoto sigue diferido al cierre global y no se declara aprobado.

**Estado:** UI-FAM.3 queda cerrada y certificada localmente. UI-FAM.4 permanece pendiente y no fue iniciada.

---

## UI-FAM.2 — cierre local y retiro de Captura dinámica redundante — 2026-08-22

Alcance implementado sobre el baseline certificado de UI-FAM.1:

- componente independiente `FamiliaDetalleModalComponent`, evitando reescribir el HTML monolítico de Matrices;
- modal institucional XL/casi fullscreen, con un único diálogo interactivo;
- `GET /api/matrices-riesgos/familias/{id}` como fuente autoritativa del detalle;
- historial real mediante `GET /api/matrices-riesgos/formularios/historial?familiaCodigo=...`;
- estados independientes de carga, éxito, 404/no encontrado, error y reintento;
- cancelación de solicitudes al cerrar/cambiar de familia y protección contra respuestas tardías;
- restauración de foco y navegación modal contenida;
- resumen, información general, reglas de ciclo de vida derivadas únicamente del DTO vigente y tabla de versiones reales;
- ausencia deliberada de “Actividad reciente”, “Última actividad” o “Actualizado por”, porque el DTO de familia no los expone;
- bridge de gestión de versiones preservado temporalmente hasta la fase de integración final;
- nuevas pruebas unitarias dedicadas y tres escenarios E2E: éxito con carga por ID, 404 y error temporal con reintento.

### Corrección de certificación local

- se corrigió el tipado estricto del foco atrapado en `FamiliaDetalleModalComponent`; el selector ahora se filtra a `HTMLElement` antes de enfocar, sin cast inseguro;
- `npm run test:coverage`: **50/50 suites y 461/461 pruebas PASS**; cobertura global informativa: 55.34% sentencias, 50.88% ramas, 51.37% funciones y 55.41% líneas;
- `npm run build`: **PASS**, con advertencia preexistente no bloqueante de `exceljs` CommonJS;
- `npm run lint`: **PASS**;
- Playwright: **17/17 PASS**, incluido detalle UI-FAM.2, reintento, 404 y devolución de foco;
- Backend Release: **494/494 PASS**;
- `validate_database_scripts.ps1`, `validate_documentation_links.ps1` y `run_quality_gates.ps1`: **PASS**. El validador estructural continúa señalando el servicio heredado `core/services/global-http-state.service.ts`, fuera de este alcance.

### Simplificación de Matrices

La pestaña y pantalla redundante **Captura dinámica** fueron retiradas del módulo. La única entrada para crear una evaluación es ahora **Evaluaciones → Nueva evaluación**, que abre el modal con el mismo renderer dinámico y conserva los datos reales de plantilla. Las pestañas visibles quedan en **Evaluaciones**, **Consolidado** y **Plantillas**. No se eliminó el motor dinámico ni se modificaron contratos REST.

**Estado de esta entrada:** UI-FAM.2 queda certificada localmente. SonarCloud/Quality Gate remoto se mantiene explícitamente diferido al cierre global por decisión del propietario; no se declara aprobado en esta intervención.

---

## F6.3 — cierre consolidado

F6.3 quedó cerrada y certificada definitivamente antes de habilitar F6.4.

Evidencia principal:

- GET autoritativo de versión por ID;
- flujo `PUT -> GET` del mismo `verId`;
- comparación semántica y fail-closed;
- preservación de `0`, `false`, `null`, `"001"` y `"G-IVM"`;
- UAT residual en navegador real completada;
- Quality Gates #1155 / Run `32397277572` SUCCESS sobre el HEAD auditado `f223f4d6e3ee9f77590709bb465a8d99e7946eb1`.

---

## F6.4 — implementación AntiG auditada por ChatGPT

### Commit técnico

`48dec5ed2f27ca5fe34d6f2d6c55f22261da5feb`

### Commit documental AntiG recibido

`5789eedc555d4bba45b742e3f7aac7c6291b8d8c`

### Invariantes C01–C05 verificados en código

1. **C01 — Inmutabilidad histórica:**
   - solo `DRAFT` no vigente es eliminable;
   - `PUBLISHED` vigente e histórica quedan protegidas;
   - guard defensivo frontend + AppService + filtro SQL `VER_ESTADO = 'DRAFT' AND VER_VIGENTE = 0`.

2. **C02 — Unicidad de vigente por familia:**
   - publicación y activación bloquean la fila de `RL_MR_FAMILIAS_FORMULARIO` mediante `SELECT ... FOR UPDATE` dentro de la transacción;
   - la vigente anterior se desactiva antes de activar/publicar la nueva.

3. **C03 — Familia activa:**
   - una versión no puede publicarse cuando su familia está inactiva.

4. **C04 — Cambio de vigencia:**
   - solo versiones `PUBLISHED` pueden activarse/desactivarse.

5. **C05 — UX de publicación:**
   - el diálogo informa sustitución de vigente, preservación histórica, inmutabilidad y necesidad de clonar para cambios futuros.

### Pruebas reportadas por AntiG y coherentes con los cambios

- Backend: 425/425 PASS.
- Frontend: 419/419 PASS.
- Playwright: 14/14 PASS.
- `tools/run_quality_gates.ps1`: SUCCESS local.

### Auditoría remota ChatGPT

- Quality Gates #1158 / Run `32398821782` sobre el handoff ChatGPT `d63d14b83ab59e0a198cb8a4f4550a3d59cec5ff`: SUCCESS.
- Quality Gates #1160 del commit técnico `48dec5ed...`: CANCELLED por el push documental posterior; por tanto no constituye certificación remota del commit técnico.
- Quality Gates #1162 / Run `32400576017` sobre `5789eed...`: se encontraba en ejecución durante la auditoría y no puede considerarse SUCCESS hasta su conclusión real.

### Estado de UAT en Navegador Real F6.4

UAT real en navegador ejecutada y **CERTIFICADA** en `localhost` con el usuario QA Oficial (`cuentajavier419@gmail.com`):

- `PUBLISHED` vigente: estrictamente solo lectura (solo opciones Ver, Clonar y Desactivar; sin Editar, Eliminar ni Guardar).
- Clonación de `PUBLISHED`: genera correctamente un nuevo borrador `DRAFT`.
- `DRAFT`: permite Editar, Guardar, Eliminar y Publicar.
- Publicación real: la nueva versión pasa a `PUBLISHED` y `Vigente / Activa`; la anterior pasa automáticamente a `PUBLISHED` e `Inactiva / Histórica`.
- **Reactivación de versión histórica**: Al activar la versión histórica `v7` (`PUBLISHED + Inactivo`), esta pasa a `PUBLISHED + Vigente`, y la versión vigente previa `v10` pasa a `PUBLISHED + Histórica / Inactiva`. Confirmado en respuesta HTTP (200 OK) e interfaz visual.
- **Garantía de Unicidad**: En todo momento existe exactamente **1 sola versión vigente** por familia.
- **Inmutabilidad Histórica**: Ninguna versión publicada (`v7` ni `v10`) ofrece o permite opciones de Editar o Eliminar.
- **Restauración de Datos QA**: Se ejecutó la reactivación inversa sobre `v10`, restaurando los datos del ambiente QA local exactamente a su estado inicial.
- **Cero Cambios de Esquema SQL / DDL / DML Manuales**: 0 scripts SQL manuales, 0 modificaciones DDL/DML a tablas Oracle. Las escrituras correspondieron únicamente a llamadas REST API de prueba en la UAT.
- **SonarCloud Remoto**: Queda **PENDIENTE Y DIFERIDO** para el Cierre Global del proyecto por decisión y directriz explícita del propietario.
- UX & Modales: modal SweetAlert2 bloquea el fondo, contiene el foco y recupera la navegabilidad al cerrarse.
- Seguridad & Consola: 0 excepciones JS, 0 fallos HTTP inesperados, 0 fuga de secretos.

---

## Próximo punto exacto

1. Mantener UI-FAM.2 como baseline local certificado y no abrir UI-FAM.3 hasta que este cierre quede publicado en `origin/desarrollo`.
2. Iniciar **UI-FAM.3 — Nueva Familia** en un commit separado, sujeto a su revisión técnica previa.
3. Mantener SonarCloud remoto diferido hasta el cierre global, sin alterar workflows, umbrales o exclusiones.

---

## Restricciones vigentes

- No tocar `main`.
- No fusionar/cerrar PR #20.
- No crear ramas.
- No ejecutar DDL/DML/scripts Oracle manuales sin autorización expresa.
- No modificar/eliminar `B10_*`.
- No bajar cobertura ni Quality Gates.
- No eliminar/omitir pruebas para obtener verde.
- No exponer credenciales, JWT, cookies, tokens o secretos.
- No modificar Backend para UI-FAM.2: los contratos y endpoints necesarios ya existen.
- No iniciar UI-FAM.3 hasta cerrar factual y técnicamente UI-FAM.2.
