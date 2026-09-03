# Estado de colaboración y punto de continuidad

## Estado vigente - Fase 3 cerrada tecnicamente

- Fecha/hora local: 2026-08-28 (UTC-6). Rama `desarrollo`.
- Runtime local stale identificado y sustituido por backend RL.API actualizado; snapshots Oracle READ-ONLY sin crecimiento de legacy.
- Migración .NET parametrizada completada: baseline dinámico `596`, `MIGRATED_ROWS=596`, `POST_HASHED=596`, `POST_REQUIRES_MIGRATION=0`, `IDEMPOTENCY_SECOND_PASS_ROWS=0`.
- Oracle postflight: tokens `TOTAL=596`, `PLAINTEXT=0`, `HASHED=596`, `REQUIRES_MIGRATION=0`; invariantes de versiones, objetos y constraints sin errores.
- Validaciones: backend `518/518 PASS`, frontend `707/707 PASS`, lint/build `PASS`, E2E `29/29 PASS`, quality gates `PASS`.
- VER_ID 27/28 permanecen deuda funcional histórica documentada, sin modificar históricos. RBAC granular solo análisis; implementación `0`, cambios `0`.

## Estado vigente - Fase 3 en ejecucion

- Fecha/hora local: 2026-08-28 (UTC-6). Rama `desarrollo`. Base `2c70ee80a4c3088394d9195045920060f3140708`.
- Preflight fail-closed PASS: CodexGraph, rama correcta, worktree limpio, fetch posterior y sincronización HEAD/origin sin divergencia.
- Implementado localmente: administración backend de catálogos matrices, cliente Angular, workflow backend centralizado y hash SHA-256 de refresh tokens nuevos. RBAC granular documentado sin implementación.
- Oracle read-only disponible; auditoría de 24 versiones PASS para hashes/fórmulas; VER_ID 27/28 continúan deuda histórica intacta.
- Regresión completa, postflight final, commits y push pendientes.
- PRECHECK refresh tokens READ-ONLY: `TOTAL=593`, `PLAINTEXT=593`, `HASHED=0`, `EXPIRED=492`, `REVOKED=319`, `ACTIVE=26`, `REQUIRES_MIGRATION=593`. Compatibilidad focalizada `11/11 PASS`; no se ejecutó DML.

## CIERRE PUNTO 19 - AUDITORIA GLOBAL FUERA DE MATRICES/FORMULARIOS

- Fecha/hora local: 2026-08-27. Rama `desarrollo`. HEAD tecnico antes de documentar: `35d1d68840e073469e3dfdf05e9a64dc5d49fd39`.
- Definicion: no existe definicion textual explicita adicional localizada; se uso el alcance minimo literal de auditar componentes globales fuera de Matrices/Formularios.
- CodexGraph `PASS`. Scopes: frontend global (rutas, autenticacion, guards, interceptores, layout y servicios compartidos); backend global (JWT/RBAC, pipeline, controladores no Matrices, manejo de errores, health, rate limiting, configuracion y persistencia); pruebas/workflows.
- Exclusiones: funcionalidad de Matrices/Formularios, Nueva Evaluacion, Builder, Consolidado y Plantillas/Familias; no se modifico producto.
- Seguridad: `SECURITY_GLOBAL_AUDIT=PASS`, sin bypass observado, `P0=0`, `P1=0`, sin secretos expuestos.
- Hallazgos total `0`; correcciones tecnicas `0`; P0=`0`; P1=`0`.
- Validaciones: backend `494/494 PASS`; frontend `705/705 PASS`; E2E `29/29 PASS`; build, lint y Quality Gate local Release `PASS`; `git diff --check=PASS`. Cobertura backend lineas `26.85%`, ramas `28.66%`; frontend sentencias `61.65%`, ramas `56.41%`, funciones `57.62%`, lineas `61.98%`.
- Resultado: `GLOBAL_AUDIT_OUTSIDE_MATRICES=PASS`, `PUNTO_19=CERTIFICADO`, `SONAR=EXCLUIDO_POR_INSTRUCCION_DEL_USUARIO`.
- Pendiente: commit documental, push a `origin/desarrollo` y Quality Gates remoto del nuevo HEAD; sin PR, merge ni `main`.

## Estado vigente - Commit tecnico publicado localmente; documental pendiente

- Fecha/hora local: 2026-08-27. Rama `desarrollo`. Commit tecnico: `a3a71b9` (`fix(matrices): cerrar flujo dinamico y consistencia visual`).
- Implementacion y UAT certificadas: Nueva Evaluacion por Familia/version activa, limpieza completa al quitar Familia, proteccion async stale, Detalle de Familia contextual, layouts de Consolidado/Plantillas/Builder y labels visibles corregidos.
- Evaluaciones permanece congelada en su bloque aprobado de filtros; regresion `0`. UAT CDP real PASS en `127.0.0.1:54257`; browser UAT permanece abierto.
- Pruebas: 705 frontend PASS, E2E 29/29 PASS, build PASS, lint PASS, Quality Gate local PASS, backend 494/494 PASS.
- Commit tecnico contiene solo codigo, estilos, pruebas, E2E y herramienta UAT reutilizable. Bitacora, estado y PNG permanecen sin commit para el commit documental separado.
- Punto de continuacion: stage explicito de ambas bitacoras y `docs/11. Prototipos/REFERENCIAS_UAT_2026-08-27/`; crear commit documental y publicar unicamente en `origin/desarrollo`. Sonar diferido; no PR, merge ni `main`.

## Estado vigente - Nueva Evaluacion dinamica y UAT real completada sin Git

- Fecha/hora local: 2026-08-27. Rama `desarrollo`. HEAD inicial de la intervencion `b04987d`; no se ejecuto staging, commit ni push.
- Implementado: selector real de familia para Nueva Evaluacion, version activa por familia, renderer dinamico condicionado a contexto valido, limpieza atomica al deseleccionar, proteccion de respuestas async tardias, boton ojo y retorno contextual al mismo modal. Familias sin leyenda ni boton redundante. Consolidado/Plantillas con filtros superiores y paginacion inferior; Builder con controles dimensionados; etiquetas visibles corregidas.
- Evidencia CDP real en `127.0.0.1:54257`: mismo browser/context/page PASS, pestañas 5 ciclos PASS, un bloque de filtros por pestaña y sin overflow, Nueva Evaluacion empty/selected/cleared PASS, Detalle/Regresar PASS, Builder 5/5 y Editar Familia 5/5. Chromium UAT permanece abierto.
- Pruebas ejecutadas: frontend 705/705, backend 494/494, E2E 29/29, build PASS, lint PASS, Quality Gate local Release PASS. Cobertura frontend: 61.65% sentencias, 56.41% ramas, 57.62% funciones, 61.98% lineas. `git diff --check` PASS con avisos CRLF informativos.
- Restricciones: Evaluaciones permanece congelada en su bloque aprobado de filtros. No se leyeron passwords, tokens, cookies, localStorage/sessionStorage sensible. Sonar diferido por el usuario; sin PR, merge ni cambios en `main`.
- Pendiente: revision final del diff y cierre Git autorizado; commit tecnico y documental separados, seguido de push a `desarrollo` unicamente tras autorizacion explicita.

## Estado vigente - corrección coordinador global de foco

- Fecha/hora local: 2026-08-27. Rama: `desarrollo`. Commit técnico: `18d9574` (`wip(mcv): avanzar coordinación global de foco en modales`).
- Corrección aplicada: `MainLayoutComponent` no conserva `BODY/HTML` como foco previo y no sobrescribe el foco ya entregado a un modal visible. Se agregó regresión unitaria global.
- Evidencia: MainLayout `5/5 PASS`; build frontend `PASS` con advertencias existentes. E2E MCV.2 sigue `FAIL` en `[data-ui-fam-detail="modal"] :focus`, esperado `1`, recibido `0`.
- No se declara Quality Gate local/remoto ni Sonar verdes. No existe PR ni merge.

## Estado vigente - UAT real CDP / FINAL-D.1 pendiente por contrato de datos

- Fecha/hora: 2026-08-26. Autor: Codex. Rama `desarrollo`. Commit técnico pendiente; no se modificó `main`.
- CDP: PASS en el Chromium UAT interactivo existente mediante `connectOverCDP` al endpoint loopback dinámico; mismo browser/context/page, ruta autenticada `/matrices-riesgos` y contenido visible. Chromium no fue relanzado ni cerrado por el runner de UAT.
- P0: el 403 de la auditoría opcional quedó controlado sin redirección a `/sin-acceso`; el modal de versiones permanece visible y no aparece pantalla blanca. Las lecturas principales de Matrices observadas respondieron 200 y no produjeron `pageerror`.
- Hallazgo bloqueante reproducido: la versión publicada v14 permite renderizar el formulario, pero su creación real responde 400 por falta de `dueno_riesgo`; la versión v15 existente responde 400 por falta de `frecuencia/impacto_inherente`. No se aplicaron bypass, DDL/DML ni cambios C#/SQL para ocultar el contrato.
- Correcciones locales reales: el interceptor ya no redirige el 403 opcional de `/api/auditoria` y tiene regresión; el renderer único ofrece entrada segura para radio sin opciones, con regresión actualizada.
- Regresión fresca: frontend 696/696, E2E 23/23, build y lint PASS; backend 494/494 heredado de esta intervención. `git diff --check` PASS con advertencias CRLF informativas.
- Estado: `P0-AUTH-UAT` PASS y corrección P0 verificada; `P0-MATRICES-BLANK-SCREENS` no se declara 0 para todos los casos hasta resolver las definiciones incompatibles; `UI-FORM.FINAL-D.1` permanece NO CERRADA fail-closed. Pendiente definición válida mediante Builder y repetición completa de Create/Edit/View, N/N+1, histórico, catálogos y paridad.
- Seguridad: password, tokens, cookies y almacenamiento sensible no fueron leídos; no se tocó Chrome personal, Edge, firewall ni `main`.
- Punto de continuación: corregir desde Builder la definición de formulario de prueba para satisfacer el contrato canónico, sin cambios manuales en HTML/TS/C#/SQL, y repetir únicamente los gates afectados sobre el mismo Chromium UAT.

## Estado vigente - UI-FORM.6-R Vista Previa + JSON Técnico

- Fecha: 2026-08-25 21:53 (UTC-6). Autor: Codex. Rama `desarrollo`. HEAD inicial `cefb7de55d73bf5808175aa0dcb9a0612520d582`; commit técnico `78167611657428c3eefeb079933ae636a63a5844` publicado; commit documental de cierre se publica inmediatamente después.
- Estado funcional: implementación existente recertificada y reconciliada. Preview permanece dentro del mismo Builder y reutiliza exclusivamente `DynamicFieldRendererComponent`; JSON Técnico conserva `serializarBuilderModelAJson`, `normalizarJsonABuilderModel` y `validarFormBuilderModel`.
- Preview: muestra el modelo actual, secciones, columnas, controles reales, catálogos reales y fórmula como presentación segura; no ejecuta fórmulas/reglas, no muta `model()` y no persiste respuestas. No existe renderer paralelo.
- JSON Técnico: visible en editable y solo lectura; copiar usa el texto actual exacto; búsqueda literal case-insensitive con contador/anterior/siguiente; validar separa sintaxis y estructura de aplicar; sincronizar permanece explícito y bloqueado en solo lectura.
- Contrato: backend/DB/migraciones/endpoints/dependencias/tipos/propiedades JSON/serializer/normalizador nuevos = 0. `eval`, `new Function`, `innerHTML` inseguro y regex dinámica del usuario = 0 en el alcance revisado.
- Evidencia fresca: frontend 64/64 archivos y 690/690 pruebas; backend 494/494; E2E 21/21; focalizada 43/43 y Preview+JSON 2/2; lint/build/Quality Gates/BD/documentación PASS; coverage 61.98% / 56.98% / 58.07% / 62.18%. Capturas temporales a 1536x1024: `frontend/rl-app/test-results/ui-form6-preview-1536x1024.png` y `frontend/rl-app/test-results/ui-form6-json-1536x1024.png`.
- Visual: PNG aprobado cargado, observado y comparado lado a lado; Preview demuestra texto, selector-catalogo y formula segura; JSON demuestra toolbar y feedback. UI-FORM.2-R a UI-FORM.5-R no presentan regresión funcional ni visual en E2E.
- Restricciones: estructura mantiene el hallazgo heredado de `core/services/global-http-state.service.ts` fuera de alcance; SonarCloud remoto diferido. Browser in-app no disponible, Playwright autenticado local PASS.
- Punto exacto: commit documental de cierre en `origin/desarrollo`; después verificar `HEAD == origin/desarrollo`, ahead/behind `0/0`, worktree limpio. `main` sin modificaciones.

## Estado vigente - UI-FORM.5-R Estados y ciclo de edición

- Fecha: 2026-08-25 18:16 (UTC-6). Autor: Codex. Rama `desarrollo`. HEAD inicial `e1e4baf47227fbe943ee5e40e59505b57a5fa69f`; commit final `eedad89d68cd8167545d11b24ae41587e97d3ff9` publicado.
- Estado: reconciliación visual y funcional local completada; toolbar conserva la navegación existente, incorpora `Editor Visual` en el nivel secundario y el statusbar refleja `EstadoFormulario` real sin alterar el ciclo de edición.
- Matriz: `DRAFT` no vigente + administrador = editable; apertura explícita, no administrador o estado distinto de `DRAFT` = solo lectura; `IN_REVIEW`, `APPROVED`, `PUBLISHED`, `RETIRED`, `ARCHIVED` permanecen contractuales y consultivos; `guardando`/`operacionBuilderEnCurso` bloquean mutaciones durante procesamiento. Backend sigue siendo autoridad de publicación, transición y autorización.
- Evidencia: focalizada 39/39; frontend 690/690; backend 494/494; E2E 18/18; coverage 62.05%/57.02%/58.07%/62.18%; lint/build/quality gates/BD/documentación PASS. Capturas editable y readonly a 1536x1024 revisadas contra el PNG aprobado.
- Contrato y regresión: UI-FORM.2-R, UI-FORM.3-R y UI-FORM.4-R sin cambios funcionales; backend/DB/migraciones/endpoints/dependencias/tipos/JSON/serialización/normalización sin cambios; sin motor de estados ni permisos paralelo.
- Restricción heredada: validador estructural sigue reportando `core/services/global-http-state.service.ts` y carpeta heredada, fuera de UI-FORM.5-R. No se corrigió en esta fase.
- Punto exacto: UI-FORM.5-R cerrada en `eedad89d68cd8167545d11b24ae41587e97d3ff9`, HEAD=origin/desarrollo, ahead/behind 0/0 y worktree limpio; no iniciar UI-FORM.6-R.

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
# Estado vigente - RECONCILIACIÓN VISUAL FINAL DEL CONSTRUCTOR

## Estado vigente - P0-UAT-CDP corregido (2026-08-26)

- Causa confirmada del fallo 9222: el perfil UAT estaba ocupado por un árbol Chromium iniciado con `--remote-debugging-pipe`; no existía listener TCP 9222 ni `DevToolsActivePort`.
- Launcher corregido: puerto efímero `--remote-debugging-port=0`, bind exclusivo `127.0.0.1`, perfil final-d1-2, detección fail-closed de lock, lectura de `DevToolsActivePort`, validación `/json/version` y endpoint temporal fuera del repositorio.
- Runner corregido: endpoint por `UAT_CDP_ENDPOINT` o `%TEMP%\\RIESGO_LAVADO_UAT\\cdp-endpoint.txt`; única conexión `chromium.connectOverCDP`; sin `launch` ni `launchPersistentContext`.
- Gates locales del tooling: PowerShell, Node, executable Chromium, perfil externo, loopback y connectOverCDP-only PASS. Los gates runtime `DevToolsActivePort`, `/json/version` y connectOverCDP real quedan pendientes de ejecución desde la PowerShell interactiva.
- No se ejecuta FINAL-D.1 ni UAT todavía. Sin commit/push por instrucción del propietario.

## Estado vigente - Infraestructura UAT CDP loopback (2026-08-26 14:35 UTC-6)

- `CDP_UAT_READY=YES`: creados `tools/uat/start-matrices-uat-browser.ps1` y `tools/uat/matrices-uat-cdp.mjs`, con documentación en `tools/uat/README.md`.
- El start script debe ejecutarse desde la PowerShell interactiva de Javier; usa exclusivamente `%TEMP%\\RIESGO_LAVADO_UAT\\playwright-profile-final-d1-2`, `--remote-debugging-address=127.0.0.1`, puerto preferido 9222 y fallback loopback libre sin matar procesos ajenos.
- El runner Codex usa únicamente `chromium.connectOverCDP`; no contiene `chromium.launch`, `launchPersistentContext`, otro perfil ni acceso a password/tokens/cookies.
- Validaciones frescas: parse PowerShell PASS, parse Node PASS, resolución de Chromium Playwright PASS y controles estáticos de seguridad PASS. La conexión CDP y la UAT FINAL-D.1 quedan pendientes de la estación interactiva.
- Siguiente acción única: Javier ejecuta `powershell -NoProfile -ExecutionPolicy Bypass -File tools/uat/start-matrices-uat-browser.ps1` y deja abierta la ventana; luego se conecta Codex sin cerrar ni relanzar browser/context/page.

## Estado vigente - UAT P0-MATRICES-BLANK-SCREENS / UI-FORM.FINAL-D.1 (2026-08-26 14:29 UTC-6)

- La ejecución intentó usar exactamente `%TEMP%\\RIESGO_LAVADO_UAT\\playwright-profile-final-d1-2`; el runner llegó a frontend/backend, pero la navegación terminó en `/login` (`LOGIN MANUAL REQUERIDO`). No se creó perfil adicional ni se solicitaron credenciales.
- Regresión local fresca: frontend 695/695, backend 494/494, E2E 23/23, build, lint, BD (19/16) y enlaces (95/163) PASS. Esto no sustituye UAT autenticada real.
- P0 blank/RBAC: E2E cubre smoke anti-blank autenticado, 403 y 404; UAT real de 401/409/500/200+blank queda pendiente por sesión no disponible. No se encontraron bugs reproducibles ni se modificó producto.
- UI-FORM.FINAL-D.1: permanece `NO CERRADA` fail-closed. N/N+1, borrador/publicación, Create/Edit/View histórico, change-without-code, long-form, catálogos/paridad, último campo/sección y título duplicado requieren repetir UAT con sesión válida.
- Estado Git pendiente de cierre documental: conservar el cambio preexistente en `tools/uat/matrices-uat-session.mjs`, registrar esta intervención, publicar solo `origin/desarrollo`, sin tocar `main`. Punto de continuación: renovar/adjuntar la sesión UAT autorizada y repetir el gate versionado.

## P0 vigente - UI-FORM.FINAL-A reabierta

- El cierre anterior queda reabierto por el incidente P0 reportado inicialmente. Actualización posterior del usuario: en su sesión real `/matrices-riesgos` carga, el módulo ya no queda en blanco y el Constructor renderiza completo con Biblioteca, Lienzo e Inspector. UI-FORM.FINAL-B permanece bloqueada.
- Investigación focalizada: `MatricesRiesgosComponent`, `FormBuilderComponent`, `FormBuilderToolbarComponent` y consumidores; diff exacto `01c9cd51..7bc2173` revisado. No se encontró error Angular reproducible en el template del toolbar.
- Reproducción local disponible: sin sesión, `/matrices-riesgos` redirige a `/login`; el API configurado `http://localhost:5043` no escucha y genera CORS/network. La pestaña Chrome autenticada del usuario no pudo ser inspeccionada por restricción del navegador conectado.
- Corrección aplicada en esta intervención: smoke E2E anti-regresión de ruta que verifica contenedor Matrices, encabezados, contenido no vacío, `pageerror=0` y console errors inesperados=0; captura `frontend/rl-app/test-results/p0-matrices-smoke-1536x1024.png`.
- Resultados locales: frontend 692/692, backend 494/494, E2E 22/22, quality gates PASS, coverage 61.95%/56.98%/58.00%/62.15%. Esto no sustituye UAT autenticada real.
- Estado reclasificado: `P0 MATRICES BLANK PAGE = CERRADO POR UAT REAL DEL USUARIO`; `UI-FORM.FINAL-A = CERRADA FUNCIONAL Y VISUALMENTE`; `UI-FORM.FINAL-B = HABILITADA / NO INICIADA`. La certificación automatizada pageerror/console/network queda pendiente como deuda no bloqueante por limitación de attach a Chrome.

## Actualización UAT — P0 blank no reproducido, certificación runtime pendiente

- El usuario confirmó visualmente en su sesión real que `http://localhost:4200/matrices-riesgos` carga, el contenido Matrices de Riesgos es visible y el Constructor renderiza Biblioteca, Lienzo e Inspector.
- Esta confirmación cambia el hecho registrado: el blank page queda **NO REPRODUCIDO EN UAT REAL DEL USUARIO**. No se registra como evidencia técnica reejecutada por Codex.
- La certificación runtime sigue pendiente porque esta intervención no contó con una superficie de navegador conectada para capturar pageerror, console.error/errores Angular, Network crítico y validar Editor Visual, Vista Previa, JSON Técnico y readonly.
- Estado vigente: `P0 = CERRADO POR UAT REAL DEL USUARIO`; `UI-FORM.FINAL-A = CERRADA`; `UI-FORM.FINAL-B = HABILITADA / NO INICIADA`.

## Reclasificación UAT — cierre funcional de P0 y habilitación de FINAL-B

- El usuario realizó UAT real y confirmó que `http://localhost:4200/matrices-riesgos` carga, el módulo Matrices de Riesgos renderiza y el Constructor de Formularios Dinámicos abre con Biblioteca, Lienzo e Inspector visibles.
- `P0 MATRICES BLANK PAGE = CERRADO POR UAT REAL DEL USUARIO`; el blank page ya no se reproduce.
- Codex no pudo adjuntarse a Google Chrome para capturar pageerror, console.error/errores Angular y Network. Esa limitación se clasifica como **DEUDA DE CERTIFICACIÓN AUTOMATIZADA NO BLOQUEANTE**, no como P0 funcional.
- No existen cambios runtime adicionales requeridos actualmente. `UI-FORM.FINAL-A` queda cerrada funcional y visualmente, con la certificación automatizada pendiente. `UI-FORM.FINAL-B` queda habilitada, pero no se inicia en esta intervención.

## Estado vigente - UI-FORM.FINAL-A navegación, acciones y ciclo visual

- Fecha/hora: 2026-08-26 08:13 (UTC-6). Autor Codex. Rama `desarrollo`. HEAD inicial `01c9cd51e8b305bb81ac1381ff9ec48fecc722fd`.
- Decisión de acciones: `Acciones` conserva las capacidades reales contextuales `Nueva Sección`/`Nuevo Catálogo`; publicación conserva botón simple porque solo existe `POST /formularios/{id}/publicar`, sin acciones secundarias contractuales.
- Decisión de configuración: `Configuración General` fue retirada del Builder porque no existe una superficie navegable en el contrato/modelo actual; no queda control muerto ni estado paralelo.
- Navegación: Editor Visual y Vista Previa son los únicos tabs reales y marcan un único activo mediante `aria-current`; Preview mantiene el renderer único ya certificado.
- Footer/estados: se mantienen Cancelar, Guardar Cambios y bloqueo readonly; dirty tracking y último guardado no se muestran porque no existen contractualmente y no se fabricaron timestamps.
- Evidencia visual: PNG consultado y capturas reales editable, Acciones, readonly y Preview a 1536x1024/100%, comparadas lado a lado. Diferencias restantes justificadas individualmente por capacidades contractuales o datos reales del fixture.
- Pruebas frescas: focalizadas 24/24; frontend 692/692; backend 494/494; E2E focal 3/3; lint/build PASS; coverage Statements 61.99%, Branches 56.98%, Functions 58.07%, Lines 62.20%. E2E completo y quality gates pendientes de cierre de esta intervención.
- Archivos actuales modificados: toolbar HTML, spec visual del Builder y `frontend/rl-app/e2e/modal-shell-lock.spec.ts`. Backend, DB, migraciones, endpoints, dependencias y contratos JSON sin cambios.

### Cierre de verificaciÃ³n

- E2E completo 21/21; `run_quality_gates.ps1` PASS con backend 494/494, frontend 692/692 y coverage frontend 61.99%/56.98%/58.07%/62.20%.
- ValidaciÃ³n de BD y enlaces documentales PASS; validaciÃ³n estructural mantiene solo el hallazgo heredado de `core/services/global-http-state.service.ts` y su carpeta, fuera del alcance.
- QA visual final ejecutado como PNG vs captura real lado a lado a 1536x1024/100%; resultado PASS para arquitectura superior y diferencias contractuales documentadas.
- Siguiente paso exacto: stage explÃ­cito, commit tÃ©cnico `fix(ui-form): cerrar navegacion acciones y ciclo visual`, commit documental, push a `origin/desarrollo` y verificaciÃ³n final 0/0 con worktree limpio.

- Fecha/hora: 2026-08-25 22:33:23 (UTC-6). Autor Codex. Rama `desarrollo`. HEAD inicial `1beb7752f18a7d07afe59fd1bd66f05813c55dfa`; commit técnico final `9ec231ea234fc324f161574c1241afcec6212f11` publicado; commit documental final pendiente.
- Resultado: Preview en toolbar secundaria; Editor Visual, Vista Previa y Configuración General deshabilitada en la navegación aprobada; Acciones solo con capacidades reales; footer Cancelar/Guardar Cambios con bloqueo readonly.
- Diferencias contractuales: control auxiliar superior, dirty tracking y timestamp de último guardado no existen; no se falsificaron. Los datos E2E difieren en cantidad de secciones/campos por fixture real.
- Evidencia: PNG observado y comparación lado a lado inicial/intermedia/final a 1536x1024; frontend 690/690, backend 494/494, E2E 21/21, lint/build/coverage/BD/documentación/Quality Gates PASS. Coverage 61.99/56.98/58.07/62.20. Estructura conserva fallo heredado de `core/services/global-http-state.service.ts`.
- Contrato 0 cambios backend/DB/migraciones/endpoints/dependencias/tipos JSON/serializer/normalizador/engines; sin ejecución dinámica.
- Punto exacto: commit técnico `9ec231ea234fc324f161574c1241afcec6212f11` ya está en `origin/desarrollo`; crear y publicar el commit documental final, luego verificar 0/0 y worktree limpio. `main` sin modificaciones.
## Estado vigente - UI-FORM.FINAL-B Secciones y acciones contextuales

- Fecha/hora: 2026-08-26 09:45 (UTC-6). Autor: Codex. Rama: desarrollo. HEAD inicial cc2a133. UI-FORM.FINAL-A permanece cerrada.
- Implementado: duplicación profunda de sección con nuevos IDs/claves sin colisión, sección origen intacta, orden y columnas preservados, menú contextual con duplicar/mover arriba/mover abajo/eliminar, confirmación SweetAlert2, readonly y procesamiento bloqueados.
- Validado: selector 1/2/3/4/6, field cards, selección visual, drop zones, Inspector contextual, Preview y JSON Técnico preservados; round-trip serialize/normalize PASS.
- Evidencia fresca: frontend 64 archivos/694 pruebas PASS; focalizadas 74/74 PASS; backend 494/494 PASS; E2E visual focal 4/4 PASS y prueba específica de duplicación/menú 1/1 PASS; lint/build PASS; DB y documentación PASS; coverage 61.86% statements, 56.75% branches, 58.10% functions, 62.16% lines.
- Certificación visual: PNG y capturas finales a 1536x1024/100% revisadas para header, toolbars, Biblioteca, secciones, columnas, duplicar, menú abierto, cards, drop zone, Inspector, footer, Preview y JSON. No quedan diferencias estructurales no justificadas; las restantes son datos, estado/permisos o capacidades no contractuales.
- Hard gates: backend 0, DB 0, migraciones 0, endpoints nuevos 0, dependencias nuevas 0, propiedades JSON nuevas 0, tipos nuevos 0, renderer/JSON/state/permission engines paralelos 0.
- Limitaciones: sincronización Git no reproducible por permisos de .git/FETCH_HEAD e index.lock; run_quality_gates.ps1 perdió la sesión al entrar a su subproceso E2E, aunque backend/frontend finalizaron y las E2E focales pasaron. validate_repository_structure.ps1 conserva únicamente el hallazgo heredado de core/services/global-http-state.service.ts.
- Commit técnico confirmado: 4add256ddfd5ee742492984227146912217cde1c (fix(ui-form): cerrar acciones de seccion y certificacion visual final).
- Punto de cierre: publicar ambos commits exclusivamente en origin/desarrollo y verificar HEAD=origin/desarrollo, ahead/behind 0/0, worktree limpio y main intacta.

## Estado vigente - UI-FORM.FINAL-C Runtime Dynamic Form Parity

- Fecha/hora: 2026-08-26 10:19-10:22 (UTC-6). Rama: desarrollo. HEAD inicial `9b7f4a7094eaad76a58aac9c899003c7cf8f47fa`.
- Resultado fail-closed: `UI-FORM.FINAL-C = NO CERRADA`. El ajuste tecnico queda implementado y probado, pero la certificacion visual lado a lado Preview vs Nueva Evaluacion y la reproduccion del titulo duplicado no estan demostradas en este checkout.
- Runtime: Nueva Evaluacion usa `seccionesModal()` y el `verJson` de la version vigente; aplica columnas y anchos de campo; usa `DynamicFieldRendererComponent`, igual que Preview. Las evaluaciones historicas conservan `evaVersionId` y resuelven su metodologia por version.
- Catalogos: se corrigio la divergencia entre metodologia separada y JSON de version; `opcionesCatalogo` prioriza catalogos de la version vigente/historica asociada y conserva fallback historico. No se hardcodearon opciones.
- Scroll: Preview conserva shell con headers/footer fuera del area y contenido central con `min-height: 0`, `overflow-y: auto`, `overscroll-behavior: contain` y `scrollbar-gutter: stable`.
- Titulo duplicado: `IdentificaIdentificacion` no se encontro en codigo, fixtures ni documentacion local; no se aplico replace visual. Requiere reproduccion con datos reales.
- Pruebas ejecutadas: focalizada 9/9; frontend 695/695; E2E 23/23; backend 494/494; lint/build PASS; coverage frontend 61.87%/56.81%/58.10%/62.17%; DB y enlaces documentales PASS; quality gates PASS; `git diff --check` PASS.
- Limitaciones: validacion estructural NO PASS por el hallazgo heredado fuera de alcance `core/services/global-http-state.service.ts` y su carpeta. No hubo UAT real reproducible v10/v11 ni captura dedicada de paridad visual runtime.
- Contrato y gates: backend/DB/migraciones/endpoints/dependencias/propiedades JSON nuevas/tipos contractuales nuevos/serializer paralelo/normalizador paralelo/renderer paralelo = 0.
- Archivos tecnicos: FormBuilder SCSS; plantilla y componente `MatricesRiesgosComponent`; prueba renderer dinamico. Se generaran commits tecnico y documental separados y se publicaran solo en `origin/desarrollo`.
- Commit tecnico: `e6dd0a94c745e1db47ad35553862cfbcc1ff797f` (`fix(ui-form): ampliar modal y unificar nueva evaluacion con version vigente`).
- Continuacion obligatoria: ejecutar UAT/capturas reales de Preview y Nueva Evaluacion para una misma version publicada; verificar draft no afecta nuevas evaluaciones, publish si afecta, historica conserva version; reproducir y corregir causa real del titulo si aparece.

## Estado vigente - UI-FORM.FINAL-D Modal grande y UAT runtime final

- Fecha/hora: 2026-08-26 10:35-10:47 (UTC-6). Rama: desarrollo. HEAD inicial `2857c7d1be64034109b8bdc766c451d058cddbf0`.
- Estado fail-closed: `UI-FORM.FINAL-D = NO CERRADA`. Se implemento el modal grande y se verifico el flujo controlado, pero no existe UAT autenticada reproducible de v10/v11 ni certificacion visual dedicada Preview vs Nueva Evaluacion con formulario extenso.
- Modal: Nueva Evaluacion reutiliza `modal-size-workspace`, el mayor patron institucional existente, con 98.3vw, maximo 1510px y altura `100dvh - 28px`; mantiene header/body scrollable/footer y responsive.
- Runtime: usa `DynamicFieldRendererComponent`, `seccionesModal()` y la definicion versionada vigente; catalogos priorizan `verJson`; historicos conservan `evaVersionId`; Preview conserva scroll interno y renderer unico.
- Evidencia UAT controlada: E2E oficial 23/23 PASS; captura `frontend/rl-app/test-results/ui-form-final-d-nueva-evaluacion-1536x1024.png` revisada a 1536x1024. La fixture tiene dos campos y no prueba 90 campos.
- Pruebas: focalizada 9/9; frontend 695/695; backend 494/494; coverage frontend 61.87%/56.81%/58.10%/62.17%; lint/build PASS; DB/enlaces PASS; quality gates PASS; `git diff --check` PASS.
- Limitacion: validacion estructural NO PASS por hallazgo heredado fuera de alcance en `core/services/global-http-state.service.ts` y su carpeta. El titulo `IdentificaIdentificacion` no se reproduce localmente; no se aplico replace.
- Contrato: propiedades JSON nuevas 0, tipos nuevos 0, serializer/normalizador sin cambios, renderer paralelo 0, backend/DB/migraciones/endpoints/dependencias 0.
- Archivos tecnicos: template de MatricesRiesgos, prueba renderer dinamico y E2E login/routing. La documentacion de FINAL-D se mantiene separada del commit tecnico.
- Continuacion obligatoria: UAT autenticada extensa de Preview/Nueva Evaluacion, draft vs publish, historico y comparacion lado a lado; no declarar cierre definitivo sin esa evidencia.

## Estado vigente - UI-FORM.FINAL-D.1 Runtime dinamico universal

- Fecha/hora: 2026-08-26 11:20-11:26 (UTC-6). Rama `desarrollo`. HEAD inicial `6f57fc9a24873a2a24d9e1367a8b6f4f5ac0fde3`.
- CodexGraph confirma un `DynamicFieldRendererComponent` unico. Create usa la version publicada vigente; Edit/View cargan la definicion por `evaVersionId`; Preview conserva el renderer del Builder.
- View/Edit pasan al patron institucional `modal-size-workspace` y respetan columnas y spans dinamicos como Create. No se modifico el contrato JSON ni la persistencia.
- Pruebas frescas: focalizada 9/9, frontend 695/695, backend 494/494, E2E 23/23, lint PASS, build PASS y quality gates PASS.
- UAT disponible: E2E controlada. No se certifican aun borrador/publicacion N/N+1, historical Edit/View despues de publicar, change-without-code, long form de 90 campos ni titulo duplicado real. FINAL-D.1 permanece NO CERRADA bajo fail-closed.
- Backend 0, DB 0, migraciones 0, endpoints 0, dependencias 0; propiedades JSON nuevas 0; tipos contractuales nuevos 0; serializer/normalizador sin cambios; renderer paralelo 0.
- Archivo tecnico: `frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.html`. Siguiente punto: UAT autenticada y capturas dedicadas por version antes de cualquier cierre.

## Estado vigente - P0-AUTH-UAT / P0-BLANK-SCREENS / FINAL-D.1

- Fecha/hora: 2026-08-26 13:13 (UTC-6). Rama `desarrollo`. HEAD inicial `fa2e30cced291b0ab3919093e229c5e13e258503`.
- P0-AUTH-UAT: bootstrap persistente creado en `tools/uat/matrices-uat-session.mjs`; perfil fuera del repositorio en `%TEMP%\\RIESGO_LAVADO_UAT\\playwright-profile` por restricción de escritura en `%LOCALAPPDATA%`. Chromium y Playwright disponibles. La sesión UAT se reutilizó; login manual queda requerido únicamente ante expiración definitiva. Password persistida/versionada: NO. Tokens versionados: NO.
- P0-BLANK-SCREENS: mapa CodexGraph y contrato revisados. Frontend usa `moduloGuard(10)` y salida `/sin-acceso`; backend usa `[Authorize]` + `[ModuloAuthorize(10)]`; interceptor maneja 401/403. No se introdujo bypass ni email special-case. No se reprodujo una pantalla blanca autorizada en las pruebas existentes.
- Autorización: las acciones administrativas de formularios/familias conservan `SystemRoles.Administrador`; el módulo general conserva autorización por claim de módulo. Permisos efectivos de la cuenta UAT no se imprimieron ni pudieron certificarse desde Browser conectado en esta sesión.
- Pruebas frescas: backend 494/494 PASS; frontend 695/695 PASS; E2E del quality gate 23/23 PASS; build PASS; lint PASS; quality gates PASS; cobertura frontend 61.87/56.81/58.10/62.17. Una ejecución concurrente anterior reportó 19/23 por interferencia de runners y fue superada por el rerun aislado 23/23.
- FINAL-D.1: NO CERRADA bajo fail-closed. Pendientes: UAT real N/N+1 publicada/borrador/histórica, change-without-code, long-form, título duplicado y catálogos/paridad real.
- Restricciones: `git fetch/pull` iniciales fueron bloqueados por permisos de `.git/FETCH_HEAD`/`.git/index.lock`; no se modificó `main`. La verificación posterior de Git quedó pendiente de commit/push documental.
- Continuación obligatoria: ejecutar UAT runtime versionada con el perfil persistente, documentar resultados reales, crear el commit documental y solicitar autorización separada para publicar en `origin/desarrollo`.
## Estado vigente - cierre unico UI-FORM

- Fecha/hora: 2026-08-26 (UTC-6). Rama: desarrollo. Commit base: 2c66eb0.
- UI-FORM.1 = CERRADA; UI-FORM.2 = CERRADA; UI-FORM.3 = CERRADA; UI-FORM.4 = CERRADA; UI-FORM.5 = CERRADA; UI-FORM.6 = CERRADA; UI-FORM.FINAL-D.1 = CERRADA; UI-FORM.7 = CERRADA.
- P0 UI-FORM = 0; P1 UI-FORM = 0. Evidencia nueva: long-form E2E test-only con 90 campos como escenario de estres representativo y renderer productivo, 1/1 PASS. La cantidad no es fija ni es requisito contractual. Gates: FIRST_FIELD_VISIBLE, MIDDLE_CONTENT_REACHABLE, LAST_FIELD_REACHABLE, LAST_SECTION_REACHABLE, VERTICAL_SCROLL, HEADER_STABLE, FOOTER_ACTIONS_REACHABLE, NO_DESTRUCTIVE_HORIZONTAL_OVERFLOW y NO_CRITICAL_CLIPPING PASS. E2E total 24/24 PASS; frontend 696/696; backend 494/494; build/lint/git diff --check PASS.
- Evidencia UAT acumulada: CDP/auth PASS, v17 draft isolation/publicacion/Create/Edit/View/Hydration PASS, historico v16 PASS, catalogos/endpoints PASS, titulo DOM count 1 PASS. No se cerraron Chromium, CDP, context ni page.
- No se crearon nuevas fases ni subfases. No hubo cambios productivos de backend, DB o contratos; no se leyeron passwords, tokens ni cookies.
- Punto de continuacion: no crear UI-FORM.8, UI-FORM.7.1 ni FINAL-E. El plan UI-FORM queda cerrado al 100%.
## Estado vigente - correccion visual final UI-FORM.7

- Fecha/hora: 2026-08-26 (UTC-6). Rama: desarrollo. Commit inicial: 7ed2284.
- Hallazgo corregido: estado activo ambiguo entre Editor Visual y Vista Previa.
- Implementacion: toolbar existente con estado activo inequívoco, accent institucional, icono heredando el estado, underline persistente y movimiento real del indicador al cambiar de vista. La semantica existente `aria-current` y `focus-visible` se conserva; no hay backend, API, permisos, Oracle ni contrato JSON modificados.
- Evidencia: Editor Visual activo PASS; Vista Previa activa PASS; switch visual PASS; estado opuesto neutral PASS; focus-visible conservado PASS; modo editable y Vista Previa automatizados PASS. Regresion frontend 696/696, E2E 24/24, build, lint y git diff --check PASS. Backend conserva 494/494 PASS.
- UI-FORM.1 = CERRADA; UI-FORM.2 = CERRADA; UI-FORM.3 = CERRADA; UI-FORM.4 = CERRADA; UI-FORM.5 = CERRADA; UI-FORM.6 = CERRADA; UI-FORM.FINAL-D.1 = CERRADA; UI-FORM.7 = CERRADA.
- P0 UI-FORM = 0; P1 UI-FORM = 0. No crear UI-FORM.8, UI-FORM.7.1 ni FINAL-E.
## Estado vigente - estados activos completos del constructor

- Fecha/hora: 2026-08-26 (UTC-6). Rama: desarrollo. Commit base: 7ed2284.
- UI-FORM.7: correccion final aplicada al toolbar existente para que Editor Visual/Vista Previa y Lienzo de Formulario/Catalogos usen el mismo lenguaje visual de seleccion.
- Evidencia: EDITOR_VISUAL_ACTIVE, VISTA_PREVIA_ACTIVE, LIENZO_FORMULARIO_ACTIVE y CATALOGOS_ACTIVE PASS; estados inactivos, indicador movil, consistencia de navegacion y focus-visible PASS. La semantica existente `aria-current` continua identificando la vista activa.
- Regresion: frontend 696/696 PASS; E2E 24/24 PASS; build, lint y git diff --check PASS. Backend no fue modificado y conserva 494/494 PASS.
- UI-FORM.1 = CERRADA; UI-FORM.2 = CERRADA; UI-FORM.3 = CERRADA; UI-FORM.4 = CERRADA; UI-FORM.5 = CERRADA; UI-FORM.6 = CERRADA; UI-FORM.FINAL-D.1 = CERRADA; UI-FORM.7 = CERRADA. P0 UI-FORM = 0; P1 UI-FORM = 0.
## Estado vigente - MCV.1 sesion y Escape

- Fecha/hora: 2026-08-27 (UTC-6). Rama: desarrollo. Commit inicial: e7306d3.
- MCV.1 = CERRADA. La actividad humana y el refresh JWT conservan relojes independientes; la politica de inactividad y los fallos de refresh mantienen el comportamiento contractual.
- Escape ya no cierra modales de Matrices/Formularios: Gestor, Detalle, Crear Familia, Editar Familia, Constructor, Versiones, Evaluaciones, View, Edit, Seguimiento y Nueva Evaluacion conservan el modal; el cierre explicito por boton permanece. El menu contextual interno de seccion puede cerrarse de forma local.
- Evidencia: frontend 697/697 PASS; E2E 25/25 PASS en ejecucion aislada; build, lint y git diff --check PASS; backend no modificado, ultimo gate 494/494 PASS.
- No se creo MCV.10, MCV.1.1 ni otra subfase. UI-FORM permanece baseline cerrado y main intacta. Siguiente punto: MCV.2, navegacion contextual.

## Estado vigente - MCV.2 navegacion contextual

- Fecha/hora local: 2026-08-26 18:37 (UTC-6). Autor: Codex. Rama: `desarrollo`. Commit tecnico `16d18fb`; documentacion de esta intervencion pendiente de commit.
- MCV.2 implementada: el Detalle de Familia permanece como contexto padre al abrir Editar Familia o Constructor; Regresar cierra solo el hijo y devuelve la misma familia. El contexto Versiones permanece visible al volver desde Constructor. El acceso directo conserva su flujo existente.
- Foco y politica: retorno al control de origen cuando sigue conectado o al control principal del Detalle; ESC sigue sin cerrar modales conforme a MCV.1.
- Pruebas frescas: MCV.2 dirigida 2/2 PASS; E2E completa 27/27 PASS; frontend 697/697 PASS; lint PASS; build PASS con advertencias preexistentes; `git diff --check` PASS. Backend sin cambios; ultimo gate certificado 494/494 PASS.
- UAT Chromium/CDP: attach al mismo browser/context/page PASS; autenticacion, `/matrices-riesgos` y contenido visible PASS. No se leyeron passwords, tokens ni cookies. Chromium UAT sigue abierto.
- Archivos tecnicos: `frontend/rl-app/e2e/matrices-familias-detalle.spec.ts`; `frontend/rl-app/src/app/features/admin/matrices-riesgos/components/familia-detalle-modal/familia-detalle-modal.component.ts`; `frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.html`; `frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.ts`; `frontend/rl-app/src/styles.css`.
- Git: staging tecnico fue verificado con exactamente esos 5 archivos; commit `16d18fb` creado. El primer add fue bloqueado por ACL con `.git/index.lock` ausente y luego se ejecuto de forma elevada. Push y sincronizacion final pendientes.
- Estado: `DETAIL_EDIT_RETURN=PASS`, `DETAIL_BUILDER_RETURN=PASS`, `SAME_FAMILY_CONTEXT=PASS`, `VERSION_CONTEXT=PASS`, `FOCUS_RESTORED=PASS`, `ESC_POLICY_PRESERVED=PASS`, `CHROMIUM_CDP_UAT=PASS`, `TESTS=PASS`, `BUILD=PASS`, `LINT=PASS`, `DIFF_CHECK=PASS`. MCV.2 aun no se marca cerrada hasta publicar y verificar Git.
- Continuacion exacta: stage documental explicito, commit documental, push `origin/desarrollo`, verificar branch/ahead/behind/worktree/main; luego marcar MCV.2 cerrada y comenzar MCV.3.

- Cierre verificado: commits `16d18fb` y `4f84cec` publicados en `origin/desarrollo`; branch `desarrollo`, HEAD igual al remoto, ahead=0, behind=0, worktree limpio y main intacta. MCV.2 = CERRADA. MCV.3 queda como siguiente fase y no fue iniciada.
## Estado vigente - MCV.3 gestor unico de versiones

- Fecha/hora local: 2026-08-27. Rama: `desarrollo`. Punto de partida: `2298b5d789dcc2c9c332de47b4ce212565077d39`.
- MCV.1 y MCV.2 permanecen cerradas. MCV.3 integra en el bloque Versiones de Detalle las acciones contractuales existentes: ver, editar borrador, nueva version, clonar, publicar, vigencia y eliminar borrador. El acceso legacy se conserva deliberadamente para MCV.4/MCV.5.
- Arquitectura: una sola ruta de servicio/DTO/estados/handlers; sin backend, API, BD, contrato JSON, permisos ni gestor paralelo nuevos.
- Evidencia automatizada: frontend 698/698 PASS; E2E 28/28 PASS; build PASS; lint PASS; `git diff --check` PASS. Backend sin cambios, ultimo gate 494/494 PASS.
- Evidencia UAT CDP: mismo browser/context/page, autenticacion, ruta y contenido PASS; frontend HTTP 200 en 4200 y backend HTTP 200 en 5043. Familia real: 5 versiones, orden, badges, vigencia y acciones coherentes por estado PASS. Ver definición abrió el renderer real y regresó al mismo Detalle/contexto Versiones; Editar definición abrió un borrador real editable y regresó al mismo contexto. Requests, familia/versión, renderer, no blank, no loading, navegación MCV.2 y política ESC MCV.1: PASS.
- Archivos tecnicos modificados: `frontend/rl-app/e2e/matrices-familias-detalle.spec.ts`, `frontend/rl-app/e2e/modal-shell-lock.spec.ts`, `frontend/rl-app/src/app/features/admin/matrices-riesgos/components/familia-detalle-modal/familia-detalle-modal.component.html`, `frontend/rl-app/src/app/features/admin/matrices-riesgos/components/familia-detalle-modal/familia-detalle-modal.component.spec.ts`, `frontend/rl-app/src/app/features/admin/matrices-riesgos/components/familia-detalle-modal/familia-detalle-modal.component.ts`, `frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.ts`.
- Seguridad: no se leyeron passwords, tokens, cookies ni localStorage; Chromium UAT permanece abierto. MCV.4-MCV.9 no iniciadas.
- Estado: MCV.3 implementacion automatizada y UAT PASS; commit tecnico `bff915b60a39f894191229e289887baba054ff1c` publicado en `origin/desarrollo`. Cierre documental pendiente de este registro. Git de la entrega previa: ahead=0, behind=0, worktree limpio, main intacta.
## Estado vigente - MCV.4 accesos alternos

- Fecha/hora local: 2026-08-27. Rama: `desarrollo`. MCV.1, MCV.2 y MCV.3 permanecen cerradas.
- `Ver versiones` desde las familias ahora abre directamente el Detalle correcto y contextualiza Versiones. `Gestionar version` fue retirado del bloque Versiones porque era redundante y llevaba al gestor alterno.
- `OLD_VERSION_MANAGER_ENTRYPOINTS=0`; la infraestructura legacy residual no se elimina todavía y queda reservada para MCV.5. No hubo cambios backend, API, DTO, contratos JSON, BD ni permisos.
- Pruebas: frontend 698/698 PASS; E2E 29/29 PASS; build PASS; lint PASS; `git diff --check` PASS; backend 494/494 certificado.
- UAT CDP: mismo browser/context/page PASS. Familia con historial: Detalle, Versiones, acciones MCV.3, no gestor legacy, no vista transitoria, modal único y ESC PASS. Segunda familia: Detalle correcto y empty state coherente por no tener versiones; no stale family.
- Archivos modificados: `frontend/rl-app/e2e/matrices-familias-detalle.spec.ts`, `frontend/rl-app/src/app/features/admin/matrices-riesgos/components/familia-detalle-modal/familia-detalle-modal.component.html`, `frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.html`, `frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.ts`.
- Estado: MCV.4 implementada, pruebas y UAT PASS; pendiente commit/push documental. MCV.5 no iniciada.

## Estado vigente - MCV.5 retiro de interfaces legacy

- Fecha/hora local: 2026-08-27. Autor: Codex. Rama: `desarrollo`. Inicio: `636ffc8`.
- MCV.5: retiro dirigido de las interfaces legacy `Versiones del Formulario` y `Nuevo Formulario de Matriz`. Consumidores identificados y migrados antes de borrar: `Ver versiones` llega a Detalle/Versiones por MCV.4; `Nueva version` del Detalle usa directamente `crearNuevaVersionDesdeDetalle(familia)` con el servicio/contrato existente, conserva familia y contexto, refresca el historial y evita doble submit.
- Eliminado fisicamente: markup transitorio, modal legacy, estados, handlers, bindings y output exclusivo sin consumidores. No se elimino `FamiliaCrearModalComponent`, porque es el flujo vigente de crear familias. La infraestructura no relacionada queda fuera del alcance y reservada para MCV.6/MCV.7/MCV.8.
- Post-eliminacion: `LEGACY_VERSION_TRANSITIONAL_UI=0`, `LEGACY_NEW_FORM_UI=0`, `LEGACY_UI_CONSUMERS=0`, `LEGACY_FUNCTIONAL_REFERENCES=0`, `STALE_LEGACY_UI_TEXT=0` en el scope funcional retirado. El texto `Versiones del formulario` restante corresponde al bloque consolidado y sus pruebas, no a la interfaz transitoria.
- UAT real Chromium/CDP: endpoint dinamico desde `DevToolsActivePort`; attach, mismo browser/context/page, `/matrices-riesgos`, Plantillas, `Ver versiones`, Detalle/Versiones y `Nueva version` sin modal legacy: PASS. Detalle y Versiones permanecieron visibles y la nueva version aparecio en el historial. Chromium permanecio abierto. No se leyeron passwords, tokens, cookies ni localStorage sensible.
- Regresion ejecutada: frontend `698/698 PASS`; E2E `29/29 PASS`; build PASS con advertencias preexistentes; lint PASS; `git diff --check` PASS. Backend sin cambios; `494/494` permanece como certificacion previa.
- Archivos tecnicos: nueve archivos frontend listados en la bitacora de esta intervencion. Documentacion actualizada: `BITACORA_COLABORACION.md` y este archivo.
- Resultado: `MCV.1`, `MCV.2`, `MCV.3` y `MCV.4` sin regresion; `MCV.5 = CERRADA`; `P0 MCV.5 = 0`; `P1 MCV.5 = 0`. MCV.6 no se inicia en esta intervencion.

## Estado vigente - MCV.6 confirmaciones institucionales

- Fecha/hora local: 2026-08-27. Autor: Codex. Rama: `desarrollo`. Inicio: `a4f9081`.
- MCV.6: se elimino la unica llamada `window.confirm` funcional del scope Matrices/Familias. La accion activar/desactivar familia ahora reutiliza SweetAlert2, unico patron vigente del modulo, con contexto real, foco en Cancelar, Escape/backdrop bloqueados y retorno de foco.
- Gates: `WINDOW_CONFIRM=0`; `NATIVE_CONFIRM_ENTRYPOINTS=0`; confirmacion solo donde corresponde; cancelacion segura; request unica; busy/error y permisos conservados; lifecycle y contratos sin cambios. No se agregaron confirmaciones artificiales a acciones no destructivas.
- Pruebas y UAT: frontend `699/699 PASS`; E2E `29/29 PASS`; build PASS con advertencias preexistentes; lint PASS; `git diff --check` PASS. Chromium/CDP real, mismo browser/context/page: confirmacion visible, contexto correcto, foco seguro, Escape sin cierre, Cancelar sin cambio/request y foco restaurado: PASS. Mutacion real no ejecutada para preservar dato UAT; pruebas existentes cubren transiciones institucionales.
- Backend: sin cambios; `494/494` permanece certificado previamente. MCV.1, MCV.2, MCV.3, MCV.4 y MCV.5 sin regresion. MCV.7 no se inicia en esta intervencion.
- Resultado: `MCV.6 = CERRADA`; `P0 MCV.6 = 0`; `P1 MCV.6 = 0`.
## Estado vigente - MCV.7 limpieza frontend controlada

- Fecha/hora local: 2026-08-27 (UTC-6). Autor: Codex. Rama: `desarrollo`. Inicio: `f8e1e15`.
- `CODEXGRAPH_SCOPE=PASS` y `CODEXGRAPH_IMPACT_ANALYSIS=PASS`. El analisis dirigido confirmo que los estados, componentes dinamicos, handlers y templates restantes del feature tienen consumidores vigentes; los simbolos legacy retirados por MCV.5 y `window.confirm` no tienen referencias funcionales activas.
- No se eliminaron candidatos inciertos. Conteos: `REMOVED_CONFIRMED_ORPHANS=0`, `REMOVED_ORPHAN_STATES=0`, `REMOVED_ORPHAN_METHODS=0`, `REMOVED_DEAD_TEMPLATE_BLOCKS=0`, `REMOVED_DEAD_STYLES=0`, `REMOVED_UNUSED_IMPORTS=0`, `RENAMED_STALE_LEGACY_SYMBOLS=0`, `UNCERTAIN_CANDIDATES=0`. `FRONTEND_CONFIRMED_DEAD_CODE_MCV_SCOPE=0` se refiere al inventario confirmado, no a una auditoria global.
- Regresion corregida: restauracion de foco al volver del Builder al Detalle. El destino focusable del Detalle y la restauracion post-render evitan que el foco termine en `body`; MCV.2 queda preservada.
- Pruebas frescas: frontend `699/699 PASS`, E2E `29/29 PASS`, prueba dirigida MCV.2 `1/1 PASS`, build PASS, lint PASS y `git diff --check` PASS. Backend no cambio; `494/494` permanece certificado.
- UAT: Chromium/CDP existente, attach dinamico desde `DevToolsActivePort`, mismo browser/context/page, `/matrices-riesgos`, contenido visible y ausencia de UI legacy: PASS. Chromium permanecio abierto; no se leyeron passwords, tokens, cookies ni localStorage sensible.
- Gates preservados: `OLD_VERSION_MANAGER_ENTRYPOINTS=0`, `LEGACY_FUNCTIONAL_REFERENCES=0`, `WINDOW_CONFIRM=0`, MCV.1-MCV.6 sin regresion, backend sin cambios.
- Archivos modificados: tres archivos frontend del ajuste de foco y estos dos documentos. `MCV.7 = CERRADA`; `P0 MCV.7 = 0`; `P1 MCV.7 = 0`. MCV.8 permanece no iniciada.
## Estado vigente - MCV.8 depuracion backend controlada

- Fecha/hora local: 2026-08-27 (UTC-6). Autor: Codex. Rama: `desarrollo`. Inicio: `d6595ba`.
- MCV.8 se limito al backend de Matrices/Familias/Versiones y definiciones dinamicas. No hubo cambios en backend, API, DTO, contratos, DB, Oracle, autorizacion ni frontend funcional.
- CodexGraph fue consultado primero. El grafo disponible no contiene nodos C#; el mapa backend se completo con inspeccion dirigida y compilable de controllers, services, repositories, DI, DTOs, mappings, tests y consumidores frontend. `CODEXGRAPH_SCOPE=PASS`, `CODEXGRAPH_BACKEND_IMPACT=PASS`, `BACKEND_DEPENDENCY_MAP=PASS`.
- Los candidatos revisados tienen consumidores activos o son frontera publica/persistente: `MatricesRiesgosAppService`, `FamiliasFormularioLifecycleService`, `SafeMatricesRiesgosRepository`, `FormularioValidador`, `PaginacionEvaluacionesHelper`, endpoints y DTOs del scope. No se borraron elementos inciertos ni se hizo limpieza ficticia.
- Conteos: `REMOVED_ORPHAN_ENDPOINTS=0`, `REMOVED_ORPHAN_SERVICE_METHODS=0`, `REMOVED_ORPHAN_DTOS=0`, `REMOVED_ORPHAN_MAPPINGS=0`, `REMOVED_PRIVATE_HELPERS=0`, `REMOVED_UNUSED_USINGS=0`, `REMOVED_DUPLICATE_LEGACY_PATHS=0`, `MIGRATED_CONSUMERS=0`, `CONSERVE_UNCERTAIN=0`, `NO_SAFE_BACKEND_DELETION_IDENTIFIED=YES`.
- Pruebas frescas: backend `494/494 PASS`; build backend PASS; frontend `699/699 PASS`; E2E `29/29 PASS`; build frontend PASS; lint PASS; `git diff --check` PASS.
- UAT Chromium/CDP: attach dinamico desde `DevToolsActivePort`, mismo browser/context/page, carga de Matrices, integracion backend, sin 404/500 nuevos, sin blank ni loading infinito: PASS. Chromium permanece abierto; no se leyeron passwords, tokens, cookies ni localStorage sensible.
- MCV.1-MCV.7: sin regresion. `DB_CHANGED=NO`, `ORACLE_CHANGED=NO`, `AUTHORIZATION_CHANGED=NO`, `API_CONTRACT_BROKEN=NO`, `BACKEND_UNCHANGED=YES`.
- Estado: `MCV.8 = CERRADA`, `P0 MCV.8 = 0`, `P1 MCV.8 = 0`. MCV.9 permanece no iniciada.

## Estado vigente - cierre formal Subfase 3.1.3

- Fecha/hora local: 2026-09-01 13:31 (UTC-06). Rama: `desarrollo`. Baseline original: `8813f539ca280aeb6bf6c81c10331f38f39e4107`.
- `SUBFASE_3_1_3=CERRADA/CERTIFICADA`. Commit técnico de la implementación: `0688a6696640bfdd618880186755fc4e8dd60092`. La documentación de cierre se consolida en el commit documental de esta intervención.
- Alcance cerrado: Formula Engine único, registry DB-driven, diez funciones nativas/registrables (`IF`, `IFERROR`, `ROUND`, `ROUNDDOWN`, `MAX`, `MOD`, `OR`, `MIN`, `AND`, `LOOKUP`), funciones compuestas con DSL común, grafo/ciclos, límites, pinning y único Publication Gate.
- Gates: `NEW_ENGINE=0`, `NEW_PUBLICATION_GATE=0`, `NEW_AUDIT_SYSTEM=0`, `NEW_CATALOG_SYSTEM=0`, `NEW_DEPENDENCY_TABLE=0`, `NEW_TABLES=0`, `PINNING_BY_LATEST_VERSION=0`, `DYNAMIC_CODE_EXECUTION=0`, histórico sin mutaciones por diff/arquitectura, `P0=0`, `P1=0`.
- Evidencia: focal comportamiento `54/54 PASS`; contratos `7/7 PASS`; `CalculationRuntimeTests=25/25 PASS`; backend `577/577 PASS`; frontend `707/707 PASS`; E2E `29/29 PASS`; lint, builds y Quality Gates locales PASS.
- Oracle: no ejecutado en 3.1.3 (`ORACLE_EXECUTED=NO`, `DDL=0`, `DML=0`, `RECOVERY=0`). No se presenta un postcheck Oracle nuevo.
- Untracked ambientales: visibles `.vscode/`, `agosto_rest.txt` y el PDF de requisitos; `agosto_capturas/` no está presente. `PREEXISTING_ENVIRONMENT_DRIFT=1`, `INTERVENTION_ATTRIBUTION=NONE`, `BLOCKER=NO`.
- Estado estable: `FASE 3.1=EN PROGRESO`; `3.1.1=CERRADA`; `3.1.2=CERRADA/RECERTIFICADA/FINAL`; `3.1.3=CERRADA/CERTIFICADA`; `3.1.4=HABILITADA/NO INICIADA`; `3.1.5=NO INICIADA`. La evidencia remota del commit final se verifica externamente sobre el SHA publicado; no se persisten estados transitorios de CI que requieran otro cambio documental.

## Estado vigente - cierre Quality Gates y bloqueo de integracion externa

- Fecha/hora local: 2026-08-26 22:19 (UTC-6). Rama `desarrollo`. HEAD `76a6aae182f41e1651b821289fa2a0eeac85eb08`, igual a `origin/desarrollo`; worktree limpio; `AHEAD=0`, `BEHIND=0`; `origin/main=c76271dc6433ab780f14d0b1cf3ce123335c63d3`.
- MCV.2: prueba dirigida `1/1 PASS` y suite E2E `29/29 PASS`; el fallo de foco no se reprodujo. Se conserva la restauracion inmediata y post-render de MCV.7.
- Quality Gate local exacto: `PASS / EXIT CODE 0`; backend `494/494`, frontend `699/699`, E2E `29/29`, cobertura dentro de umbrales.
- SonarCloud, CI, PR y merge permanecen `PENDIENTES`: GitHub CLI tiene token invalido y la API esta bloqueada por red. No se declara verde remoto sin evidencia.
- Continuacion: reautenticar GitHub CLI y habilitar acceso API; consultar runs del SHA, gestionar PR `desarrollo -> main`, esperar checks/Sonar verdes, hacer merge por PR y certificar CI post-merge.

## Estado vigente - bloqueo por Quality Gates remoto del SHA publicado

- SHA `2642423c0e73d7508440fcacaa1c8dad2b53bd59` esta publicado y sincronizado previamente, pero GitHub Actions run `33039392842` termino `failure` en `Run repository quality gates`.
- Causal exacto remoto: E2E `28 passed`, MCV.2 fallo en primer intento y retry por foco ausente en `[data-ui-fam-detail="modal"] :focus` (expected 1, received 0). No se declara CI/Quality Gate/Sonar verde ni se crea/mergea PR.
- Los cambios experimentales de correccion no satisfactoria fueron retirados; worktree vuelve al codigo publicado. Quality Gate local anterior permanece `PASS`: backend `494/494`, frontend `699/699`, E2E `29/29`.
- Punto de continuacion: corregir la coordinacion real entre MainLayout y las vistas dinamicas Detalle/Builder, ejecutar prueba dirigida, suite 29, Quality Gate y nuevo CI; solo entonces crear/reutilizar PR y evaluar Sonar/merge.

## Estado vigente - MCV.9 certificacion integral y cierre

- Fecha/hora local: 2026-08-27 (UTC-6). Autor: Codex. Rama: `desarrollo`. Inicio: `55358ea`.
- Precheck Git: `BRANCH=desarrollo`, HEAD inicial esperado, `HEAD==origin/desarrollo`, worktree limpio y `MAIN_UNTOUCHED=YES`.
- MCV.1 a MCV.8 conservan estado `CERRADA` y sus gates documentados. MCV.7 no justifico eliminaciones frontend adicionales; MCV.8 no identifico eliminaciones backend seguras.
- Comprobaciones finales: `FINAL_CODEXGRAPH_FRONTEND_CHECK=PASS`; `FINAL_BACKEND_REFERENCE_CHECK=PASS` con mapa dirigido compilable. El grafo disponible no contiene nodos C# backend completos.
- Búsqueda funcional: `WINDOW_CONFIRM=0`; `NATIVE_CONFIRM_ENTRYPOINTS=0`; `OLD_VERSION_MANAGER_ENTRYPOINTS=0`; `LEGACY_FUNCTIONAL_REFERENCES=0`.
- Regresion final secuencial: backend `494/494 PASS`; frontend `699/699 PASS`; E2E `29/29 PASS`; build backend/frontend PASS; lint PASS; `git diff --check` PASS. El fallo inicial durante ejecución concurrente se descarto como interferencia de procesos; prueba aislada de foco `2/2 PASS` y E2E secuencial `29/29 PASS`.
- UAT final: Chromium/CDP existente, endpoint dinamico, mismo browser/context/page, `/matrices-riesgos` funcional y sin blank screen, loading infinito ni error fatal: `CHROMIUM_CDP_UAT=PASS`. Chromium permanece abierto; no se leyeron secretos.
- Integridad final: arquitectura dinamica y modelo unico de renderizado preservados; definiciones versionadas e historico vinculados; API, DI, persistencia, autorizacion, DB y Oracle sin cambios.
- `MCV.9 = CERRADA`; `P0=0`; `P1=0`; `PLAN_MCV_STATUS=CERRADO`; no se crean nuevas fases. Esta intervencion solo agrega certificacion documental; commit y push pendientes de verificacion.

## Estado vigente - MCV.2 coordinacion de modales externos

- Fecha/hora local: 2026-08-27. Rama `desarrollo`. Commit tecnico `33c37ad`; commit inicial `8be892a`.
- MainLayout ahora observa `document.body`, reconoce modales propios externos con `data-app-modal="true"`, filtra estados no visibles y protege el foco ya entregado. Se agrego regresion unitaria general; FormBuilder no fue modificado.
- Verificacion: MainLayout `6/6 PASS`; build frontend `PASS` con advertencias preexistentes; MCV.2 `FAIL` en `[data-ui-fam-detail="modal"] :focus` (expected 1, received 0). No se afirma Quality Gate verde.
- Estado: avance tecnico coherente pendiente de push y commit documental. No crear PR ni hacer merge a `main` mientras MCV.2 continue rojo.
## Estado vigente - alcance visual Matrices de Riesgos

- Fecha/hora local: 2026-08-27 12:16 (UTC-6). Autor: Codex. Rama `desarrollo`. Inicio `b04987d`; commit final pendiente.
- Evaluaciones conserva su bloque de filtros aprobado; pruebas de pestañas bloquean filtros cruzados. Consolidado y Plantillas usan filtros propios con lenguaje visual adaptado. Builder ajustado en `Acciones` y `2 columnas`. KPIs son un unico bloque superior contextual.
- Evidencia fresca: frontend `703/703`, backend `494/494`, E2E `29/29`, build/lint/Quality Gate local PASS; cobertura frontend `61.53%` sentencias y `56.46%` ramas.
- Pendiente externo: UAT Chromium visible + CDP no ejecutada por ausencia de endpoint/sesion (`cdp-endpoint.txt`). Sonar `DEFERRED_BY_USER`; no PR, merge ni `main`.
- Continuacion: ejecutar launcher UAT interactivo, correr `node tools/uat/matrices-uat-cdp.mjs`, certificar las tres pestañas y luego comprobar `HEAD == origin/desarrollo`, `AHEAD=0`, `BEHIND=0`, worktree limpio.

## Estado vigente - correccion final de ownership de foco

- Fecha/hora local: 2026-08-27. Rama `desarrollo`. Commit tecnico `3992e6a`.
- MCV.2 `5/5 PASS`; E2E `29/29 PASS`; Quality Gate local `PASS / EXIT CODE 0`; backend `494/494`; frontend `701/701`; MainLayout `6/6 PASS`.
- FormBuilder no fue modificado. Sonar, Quality Gate remoto, PR, merge y CI post-merge quedan pendientes de verificacion sobre el SHA publicado.

## Estado vigente - Fase 2 motor DSL y diagnóstico Oracle

- Fecha/hora local: 2026-08-28 (UTC-6). Rama `desarrollo`. Base `2a6b65ed503150c5a305062be7de35a31974f906`; commit final pendiente.
- Implementado: DSL seguro compartido, cálculo backend autoritativo, version pinning, detección de ciclos, Publication Gate semántico, mappings de proyección allowlisted y auditor fail-closed.
- Evidencia fresca: backend `504/504 PASS`; frontend `705/705 PASS`; lint/build PASS; database/documentation validators PASS. Auditor Oracle terminó exit code `2` con `ERROR=50201` y `ORACLE_SEMANTIC_AUDIT=EXTERNAL_BLOCKER`; no se declara postflight Oracle.
- E2E: `26/29 PASS` en ejecución integral; 3 fallos reportados por timeout/estado del servidor de pruebas. Requiere nueva ejecución con infraestructura estable.
- Históricos VER_ID 24, 27, 28 y 53 intactos; no DDL/DML; RBAC granular pendiente de análisis con el jefe; `CAMBIOS_RBAC=0`. VER_ID 27/28 mantienen deuda catalogal funcional exacta para Fase 3.
- Continuación: completar Oracle postflight/E2E cuando estén disponibles, revisión y staging explícitos, commits técnico/documental y push únicamente a `origin/desarrollo`.

- Actualización: E2E integral posterior `29/29 PASS`. Commit técnico publicado: `a842880`. Oracle continúa externo bloqueado con `ERROR=50201`; no se declara postflight PASS.

- Estado actualizado: Oracle disponible y validado en solo lectura. Auditoría `24/24` versiones, `HASH_INVALID=0`, referencias desconocidas/operadores/funciones no soportados `0`; VER_ID 24 y 53 `CLASS=VALID`. Postflight sin vigentes múltiples, solapamientos, filas de versión inválidas, objetos inválidos ni constraints deshabilitadas. Corrección publicada en `39e34df`.

## Estado vigente - Fase 1 integridad/publication gate Oracle

- Fecha/hora local: 2026-08-27 (UTC-6). Rama `desarrollo`. HEAD inicial: `85227671715441bafc3b36782a7f5cb17ee26d8c`.
- H1 PASS: 24 CLOB completos certificados con SHA-256 UTF-8; 0 inválidos y 0 incalculables.
- H2/H3 quedan como `FAIL_LEGACY_BASELINE`, sin bloquear Fase 1: VER_ID 24, 27, 28 y 53 permanecen intactos y se asignan a Fase 2. `LEGACY_SEMANTIC_DEBT=4`, `DEFERRED_TO_PHASE_2=TRUE`.
- H4 PASS tras saneamiento exacto de VER_ID 1, 17 y 25: solapamientos 0 y demás invariantes temporales 0.
- Hardening Oracle aplicado: constraints de vigencia/publicación, fechas, EVA_VERSION_ROW y formato hash; índice funcional único por familia vigente `VALID/UNIQUE`. Postflight sin objetos inválidos ni constraints deshabilitadas.
- H8/H9 implementados localmente: lock de familia previo a MAX(VER_VERSION)+1 y Publication Gate server-side. Backend final `494/494 PASS` con TRX.
- Histórico preservado: sin cambios de VER_JSON, VER_HASH o EVA_VERSION_ID. Recovery H4 disponible en `database/19_matrices_riesgos/transicion/13_rollback_temporal_h4.sql` y no fue ejecutado.
- RBAC granular: pendiente de análisis con el jefe; cambios 0; RL_PERMISOS no creada; RL_ROL_PERMISOS no creada. Sonar excluido por instrucción del usuario.
- Punto de continuación: ejecutar validaciones finales, revisar diff, staging explícito, commits separados y publicar únicamente en `origin/desarrollo`.
- Punto de continuación: ejecutar validaciones finales, revisar diff, staging explícito, commits separados y publicar únicamente en `origin/desarrollo`.

## Estado vigente - Subfase 3.1.1 arquitectura y modelo de persistencia

- Fecha/hora local: 2026-08-31 (UTC-6). Autor: Codex. Rama `desarrollo`. BASE_SHA `1adca71caa4b4581df731d1c7a6d2d4cfdd2e183`; pre-flight con `HEAD=origin/desarrollo`, AHEAD=0, BEHIND=0. El worktree conserva cuatro untracked preexistentes ajenos a esta subfase y no se tocaron.
- Estado: `SUBFASE_3_1_1=CERRADA`; `FASE_3.1=EN PROGRESO`; 3.1.2 queda habilitada, sin implementación en esta intervención.
- Evidencia fresca: CodexGraph dirigido PASS; Oracle read-only SQL*Plus por ProcessStartInfo PASS (`SQLPLUS_EXIT_CODE=0`); 18 tablas, 24 versiones, 14 evaluaciones, 4 catálogos, 18 elementos, 1 regla, 1.290 auditorías; DDL=0, DML=0, cambios Oracle=0.
- Arquitectura cerrada: una sola administración de Fórmulas, Funciones, Parámetros, Reglas y Catálogos; un solo Formula Engine, Publication Gate y sistema de auditoría. Se reutilizan familias/versiones, evaluaciones, reglas, catálogos, `VER_JSON`, `VER_HASH`, `EVA_VERSION_ID`, `EVA_CALCULOS_JSON` y `RL_AUDITORIA`.
- Persistencia propuesta: `RL_MR_FORMULAS`, `RL_MR_FORMULA_VERSIONES`, `RL_MR_FORMULA_USOS`, `RL_MR_FUNCIONES`, `RL_MR_FUNCION_VERSIONES`, `RL_MR_FUNCION_ARGUMENTOS`, `RL_MR_PARAMETROS_CALCULO` y `RL_MR_PARAMETRO_VERSIONES`. No se propone `RL_MR_DEPENDENCIAS_CALCULO`, `RL_MR_REGLAS_CALCULO_V2`, catálogos paralelos ni auditoría nueva.
- Histórico y seguridad: JSON/hash/versiones/evaluaciones históricas no se reescriben; VER_ID 24/53 impacto ninguno; VER_ID 27/28 mutación histórica 0; código ejecutable en BD=0, C#/JavaScript/SQL dinámico=0, eval/new Function=0. `MODEL_SUPPORTS_34_FORMULAS=YES` arquitectónico; paridad Excel se difiere a 3.1.5.
- Documento oficial: `docs/3. Módulo Matrices de Riesgos/FASE_3_1_1_ARQUITECTURA_MODELO_PERSISTENCIA.md`.
- Cierre Git verificado: `HEAD==origin/desarrollo`, AHEAD=0, BEHIND=0, tracked worktree limpio, staged changes=0 y diff tracked vacío. `WORKTREE_FULL_CLEAN=FALSE` únicamente por cuatro untracked preexistentes; `PREEXISTING_UNTRACKED_PRESERVED=4`, `SCOPE_WORKTREE_CLEAN=TRUE`. No fueron creados, modificados, stageados, committeados ni publicados por 3.1.1 y no son P0/P1, blocker, regression ni deuda propia. No hacer DDL/DML ni crear endpoints/tablas en esta subfase.

## Estado vigente - Subfase 3.1.2 Oracle y backend administrativo

- Fecha/hora local: 2026-08-31 (UTC-6). Autor: Codex. Rama `desarrollo`. Base SHA `5cf62dc80fc22729a3ad3bb52317ff5a1f8fe60a`.
- `SUBFASE_3_1_1=CERRADA` y congelada. `SUBFASE_3_1_2` implementa únicamente persistencia Oracle 11g y backend administrativo de Fórmulas, Funciones y Parámetros; no inicia 3.1.3, 3.1.4 ni 3.1.5.
- Oracle real: precheck PASS; DDL PASS; seed PASS; postcheck PASS, todos con transporte SQL*Plus seguro, credenciales solo en memoria y ExitCode real 0. Se crearon exactamente ocho tablas y ocho secuencias nuevas autorizadas; no se alteraron tablas existentes y recovery 18 no se ejecutó.
- Seed exacto: siete funciones NATIVAS (`IF`, `IFERROR`, `ROUND`, `ROUNDDOWN`, `MAX`, `MOD`, `OR`), siete versiones y trece argumentos. `AND`, `MIN`, `LOOKUP`, funciones compuestas, parámetros de negocio y 34 fórmulas permanecen diferidos.
- Histórico intacto: 24 versiones de formulario, 14 evaluaciones con versión/resultado y los valores verificados de VER_ID 24, 27, 28 y 53 permanecen sin cambios. `VER_JSON`, `VER_HASH`, `EVA_VERSION_ID` y `EVA_CALCULOS_JSON` no fueron actualizados.
- Backend: contratos tipados, validación, repositorio Oracle con binds, servicio, controller único y DI dentro de MatricesRiesgos. Se reutilizan reglas, catálogos, auditoría, Formula Engine y Publication Gate; `NEW_AUDIT_SYSTEMS=0`, RBAC changes=0 y código ejecutable en BD=0.
- Pruebas frescas: focalizadas 17/17, backend 535/535, frontend 707/707, E2E 29/29, builds y lint PASS, Oracle postcheck PASS. No se debilitaron quality gates.
- Documentación oficial: `docs/3. Módulo Matrices de Riesgos/FASE_3_1_2_ORACLE_BACKEND_ADMINISTRATIVO.md`. P0=0, P1 propios=0. Commit técnico `cb63807` creado y publicado en `origin/desarrollo`; queda pendiente el commit documental y la certificación CI remota final.
- Punto de continuación: publicar únicamente en `origin/desarrollo`, verificar workflow por el SHA final y detenerse con 3.1.3 habilitada pero no iniciada.

## Estado vigente - corrección de comentarios Oracle 3.1.2

- Fecha/hora local: 2026-08-31 (UTC-6). Autor: Codex. La aplicación inicial de comentarios 19 fue correcta; el postcheck 20 se detuvo fail-closed por `ORA-00932` al contar directamente el CLOB `EVA_CALCULOS_JSON`.
- Sin reescribir 19/20, se agregaron scripts 21/22. Con wrapper SQL*Plus seguro y `NLS_LANG=.AL32UTF8` aislado, 21 terminó `ExitCode=0` y 22 corregido terminó `ExitCode=0`.
- Postcheck Oracle real: 8/8 tablas y 86/86 columnas comentadas; texto de tablas 8/8; comentarios corruptos 0; diacríticos españoles PASS; Oracle 11g PASS; `ORA_00932=RESOLVED`; cambios estructurales 0; datos históricos cambiados 0; `HISTORICAL_INTEGRITY=PASS`.
- Inspección visual read-only de `USER_TAB_COMMENTS` confirmó `Catálogo`, `fórmulas`, `configuración`, `cálculo`, `Definición`, `versión`, `parámetros` e `históricamente` correctamente almacenados.
- Recovery no ejecutado. No hubo staging, commit ni push en esta corrección. Continúa el cierre técnico/documental de 3.1.2, con 3.1.3 habilitada pero no iniciada.

## Estado vigente - recertificación de Subfase 3.1.2

- Fecha/hora local: 2026-09-01 (UTC-6). Autor: Codex. Rama `desarrollo`. Commit técnico publicado: `99337b41f054ce83f7691d4c5441a456f9b60df8` (`fix(matrices): harden calculation admin lifecycle`). La documentación de cierre se publica en el commit documental de esta intervención.
- Estado: `SUBFASE_3_1_2=CERRADA/RECERTIFICADA`; `FASE_3.1=EN PROGRESO`; `SUBFASE_3_1_3=HABILITADA/NO INICIADA`. `PENDING_COMMIT=0` y `PENDING_CI=0` al completar la publicación y verificación final.

## Estado vigente - Hotfix ORA-01745 de Subfase 3.1.4

- Fecha/hora local: 2026-09-02 (UTC-6). Rama `desarrollo`. Base `5a5c7b114cdb13917648e84c7f303690fe4b3aae`; commit técnico `a5fc64814b69cf2043e3e20ba177a0af35358f64`.
- Defecto corregido: `GET /api/matrices-riesgos/configuracion-calculo/formulas` usaba el bind Oracle reservado `:all`, causando `ORA-01745`. Se corrigieron también los listados de Funciones y Parámetros al bind seguro `:p_incluir_inactivas`.
- Evidencia: contrato/validaciones `36/36 PASS`, build backend aislado PASS, regresión backend `579/579 PASS`, FAIL=0, `INVALID_ORACLE_BINDS=0`, sin DDL/DML ni mutaciones históricas. No se dispone de listener HTTP local para una nueva llamada autenticada; esa limitación queda explícita y no se maquilla como HTTP 200.
- Estado: `FASE_3.1=EN PROGRESO`; 3.1.1 cerrada/congelada; 3.1.2 cerrada/recertificada/final; 3.1.3 cerrada/certificada; 3.1.4 pendiente de recertificación remota del hotfix; 3.1.5 habilitada/no iniciada; `P0=0`, `P1=0`.

## Estado vigente - parche de seguridad qs posterior a 3.1.4

## Estado vigente - Fase 3.1.5 fórmulas institucionales

## Estado vigente - Cierre final FASE 3.1.5 y FASE 3.1

- Fecha/hora local: 2026-09-03 08:41 (UTC-06). Rama `desarrollo`. Base de cierre `804bff16b81255d661a8f52279fea285e7218784`.
- Oracle final: TCP PASS; SQL*Plus read-only PASS; POSTCHECK 26 ejecutado una sola vez, SHA `58823DFB93BED19EA0D347BC3668C3059E01534BA5A2635580A56C53C81F42A8`, ExitCode `0`, status `PASS`.
- Conteos: fórmulas `34/34`, usos `0`; funciones `10`, versiones `10`, argumentos `18`; parámetros `3`, versiones `3`; hashes válidos `34/34`, `10/10`, `3/3`; inválidos/huérfanos/duplicados/referencias/objetos/constraints/histórico `0`.
- DDL 28 y DML 25 permanecen sin reejecución; DML 25 conserva ExitCode 0; recovery 27 jamás ejecutado. `ORACLE_315_CERTIFIED=TRUE`.
- Seguridad y regresión: `fast-uri=3.1.6`; npm audit `HIGH=0`, `CRITICAL=0`; frontend `716/716`; E2E `29/29`; lint/build PASS. Pendiente únicamente publicar el commit documental final y certificar su Quality Gate exacto.
- Estado objetivo: `3.1.5=CERRADA/CERTIFICADA/FINAL`; `FASE_3.1=CERRADA/CERTIFICADA/FINAL`, solo después de verificar publicación y Quality Gate final.

- Fecha/hora local: 2026-09-02 15:59 (UTC-06). Rama `desarrollo`. Baseline `9308b9a670c7444b3f3355c58c4dc51ef1bd1a97`; commit técnico `82787eb30b175f47b6244b8adf345c0c6baeafde`.
- Estado: `3.1.5=IMPLEMENTADA/VALIDADA_LOCALMENTE/BLOQUEADA_EXTERNAMENTE_EN_CERTIFICACION_ORACLE`; `FASE_3.1=EN_PROGRESO`. No se declara cierre.
- Paridad: `FORMULAS_INSTITUCIONALES=34/34`; `FORMULA_DATASET_DML_PARITY=34/34`; expectedByField exacto de 34 resultados; `MIN`, `AND`, `LOOKUP`; pesos `PESO_PREVENTIVO=0.70`, `PESO_DETECTIVO=0.15`, `PESO_CORRECTIVO=0.15`.
- Pruebas: focal backend `49/49`; backend completo `591/591`; frontend aislado `716/716`; E2E `29/29`; lint/build PASS. Cobertura backend `31.08%`/`35.34%`; frontend `60.50%`/`54.19%`/`55.27%`/`61.15%`. NPM CI aislado `0`, cache eliminado. NPM audit `1`, `HIGH=1`, `fast-uri`; no audit fix ni actualización de dependencias.
- Gates: DB scripts y enlaces PASS; structural validator FAIL por condiciones heredadas fuera de scope; `LOCAL_QUALITY_GATES=FAIL` aunque cobertura/regresión/lint/build/E2E pasaron.
- Controles: ciclos, referencias, dependencias despinneadas, handlers/tipos inválidos, código DB, ejecución dinámica, nuevas tablas/engine/gate/auditoría/catálogo/dependencias, RBAC, mutaciones históricas, STANDARD_HASH y violaciones Oracle 11g: todos `0`.
- Oracle: DDL 28 aplicado; DML 25 aplicado `ExitCode=0`; `DDL_28_REEXECUTED=NO`; `DML_25_REEXECUTED=NO`; `RECOVERY_27_EXECUTED=NO`; `POSTCHECK_26_EXECUTED_FINAL=NO_CONNECTION`; `ORACLE_315_BLOCKED_EXTERNAL_ENVIRONMENT=TRUE`; `ORACLE_315_CERTIFIED=FALSE`; `SocketException / AccessDenied` antes de SQL*Plus; hash `58823DFB93BED19EA0D347BC3668C3059E01534BA5A2635580A56C53C81F42A8`.
- Punto de continuación: resolver `fast-uri` antes del Quality Gate remoto final y repetir exclusivamente `26_postcheck_formula_institucional_315_solo_lectura.sql` cuando Oracle permita conexión. Recovery jamás ejecutado; untracked preexistentes preservados.

- Fecha/hora local: 2026-09-02 (UTC-6). Rama `desarrollo`. Base `1dd720143ddf685a0c660dc0f1ee7d4e68346154`; commit técnico `f87568c692aad1eb95fd5290d109377795ed028b`.
- Quality Gate causal: `33651107541` falló únicamente en la auditoría npm reproducible por `qs@6.15.3`, introducido por `karma@6.4.4 -> body-parser@1.20.6`, bajo los advisories `GHSA-x5fp-wj9c-mxmx` y `GHSA-4mjr-xmp4-gh2g`.
- Corrección: override transitivo `qs=6.16.0` y lock regenerado exclusivamente con `npm install --package-lock-only --ignore-scripts`. Verificación aislada: `npm ci` PASS, `npm audit --audit-level=high` PASS con HIGH=0 y CRITICAL=0; frontend `716/716`, lint, build y E2E `29/29` PASS.
- Alcance: `DDL=0`, `DML=0`, `RECOVERY=0`, Oracle no ejecutado; `ORA-01745` permanece corregido; arquitectura, `main` y datos históricos no fueron modificados. Se preservan `.vscode/`, `agosto_rest.txt` y el PDF de requisitos como untracked preexistentes.
- Estado estable: `FASE_3.1=EN PROGRESO`; `SUBFASE_3_1_1=CERRADA/CONGELADA`; `SUBFASE_3_1_2=CERRADA/RECERTIFICADA/FINAL`; `SUBFASE_3_1_3=CERRADA/CERTIFICADA`; `SUBFASE_3_1_4=CERRADA/CERTIFICADA`; `SUBFASE_3_1_5=HABILITADA/NO_INICIADA`; `P0=0`; `P1=0`. La verificación remota del parche corresponde únicamente al Quality Gate exacto del SHA final publicado.
- P1 resueltos: `P1-312-01=RESOLVED` con allowlist de 8/8 secuencias; `P1-312-02=RESOLVED` con matriz central de lifecycle e inmutabilidad publicada; `P1-312-03=RESOLVED` con `FOR UPDATE` en los masters de fórmula, función y parámetro. `STALE_VERSION_ROW=CONFLICT`.
- Evidencia de pruebas: focalizadas `33/33 PASS`; backend `551/551 PASS`; frontend `707/707 PASS`; E2E `29/29 PASS`; lint, builds y Quality Gates locales PASS.
-
## Estado vigente - Fase 3.1.4 Angular administrabilidad de cálculo y Builder

- Fecha/hora local: 2026-09-02 (UTC-6). Rama `desarrollo`. Base `47e6782acac38627860a1aad7a9e90aa2ccd2eaf`; commit técnico `dc3f78eded6381fa62e5dbd6e6b8e0e55198449f`.
- Estado: `FASE_3.1=EN PROGRESO`; `SUBFASE_3_1_1=CERRADA/CONGELADA`; `SUBFASE_3_1_2=CERRADA/RECERTIFICADA/FINAL`; `SUBFASE_3_1_3=CERRADA/CERTIFICADA`; `SUBFASE_3_1_4=CERRADA/CERTIFICADA`; `SUBFASE_3_1_5=HABILITADA/NO_INICIADA`.
- Implementado: workspace único de Configuración de Cálculo, pestañas de Fórmulas/Funciones/Parámetros y vistas de Reglas/Catálogos reutilizadas; servicio Angular tipado; lifecycle y estados; Builder con selección de `FormulaVersion` exacta, pinning sin latest, preview de DSL y preservación de campos legacy; reemplazo atómico de usos únicamente sobre formulario DRAFT.
- Arquitectura: `NEW_ENGINE=0`, `NEW_PUBLICATION_GATE=0`, `NEW_AUDIT_SYSTEM=0`, `NEW_CATALOG_SYSTEM=0`, `NEW_RULE_SYSTEM=0`, `NEW_DEPENDENCY_TABLE=0`, `NEW_TABLES=0`, `RBAC_CHANGES=0`, `DYNAMIC_CODE_EXECUTION=0`, `HISTORICAL_MUTATIONS=0` por diff/arquitectura.
- Evidencia: frontend focal `8/8`, backend focal `46/46`, frontend `716/716`, backend `578/578`, E2E `29/29`, lint/build/npm ci/npm audit y validación de scripts de base de datos PASS. Oracle no ejecutado, DDL=0, DML=0, recovery=0.
- Entorno: se observan tres untracked preexistentes: `.vscode/`, `agosto_rest.txt` y el PDF de requisitos. `agosto_capturas/` no está presente; `PREEXISTING_ENVIRONMENT_DRIFT=1`, `INTERVENTION_ATTRIBUTION=NONE`, `BLOCKER=NO`. Se preservan fuera de scope.
- Cierre: `P0=0`, `P1=0`. La evidencia de CI remoto se consultará para el commit documental final exacto; no se registran estados transitorios que requieran otra modificación documental posterior.
- Punto de continuación: no iniciar 3.1.5 hasta autorización expresa y cierre de esta entrega en `desarrollo`.
- Oracle: postchecks 17 y 22 permanecen válidos y PASS, con 8/8 tablas, 86/86 columnas, texto/codificación/integridad histórica PASS y `ORA_00932=RESOLVED`. El postcheck complementario 23 queda `POSTCHECK_23_STATUS=BLOCKED_EXTERNAL_ENVIRONMENT`, `POSTCHECK_23_EXECUTED=NO`, `POSTCHECK_23_CODE_DEFECT=NO`, `POSTCHECK_23_BLOCKER=NO`, por ORA-12546/restricción externa de red. No se reintentó Oracle; `DDL_ADDITIONAL=0`, `DML_ADDITIONAL=0`, `RECOVERY=0`.
- Histórico: `VER_JSON`, `VER_HASH`, `EVA_VERSION_ID` y `EVA_CALCULOS_JSON` intactos; VER_ID 24, 27, 28 y 53 sin mutación. `P0=0`, `P1_PROPIOS_3_1_2=0`, `NEW_AUDIT_SYSTEMS=0`, `RBAC_CHANGES=0`.
- Entorno Git: se observan tres untracked preexistentes: `.vscode/`, `agosto_rest.txt` y el PDF de requisitos. `agosto_capturas/` no está presente. Se clasifica `PREEXISTING_ENVIRONMENT_DRIFT=1`, `INTERVENTION_ATTRIBUTION=NONE`; no es blocker ni deuda de 3.1.2. Tracked limpio, staged=0, unexpected untracked=0, archivos propios 3.1.2 pendientes=0 y `SCOPE_WORKTREE_CLEAN=TRUE`.
- Evidencia remota de cierre: GitHub Actions run `33526999211` sobre `2996a6342aebbe231256e612db4f941d681b54b0` terminó `status=completed`, `conclusion=success`, con `head_sha` coincidente. No se persisten estados transitorios de CI que requieran un commit documental posterior. No se inicia 3.1.3.
## Estado vigente - Agent Skills cerradas y operativas

- Fecha/hora local: 2026-09-03 (UTC-06). Rama `desarrollo`. La infraestructura de Agent Skills queda documentada y operativa.
- `AGENT_SKILLS_IMPLEMENTED=TRUE`; `AGENT_SKILLS_COUNT=14`; `AGENT_SKILLS_ACTIVATED=TRUE`; `AGENT_SKILLS_VALIDATION=PASS`.
- `GIT_HOOKS=ACTIVE` (`core.hooksPath=.githooks`); `AGENT_SKILLS_CI=SUCCESS`.
- `MAIN_RULESET_ID=22139842`; `MAIN_RULESET=ACTIVE`; target `refs/heads/main`.
- `FAST_URI=3.1.6`; `NPM_AUDIT_HIGH=0`; `NPM_AUDIT_CRITICAL=0`.
- `QUALITY_GATE_RUN=33768618747`; `QUALITY_GATE_CONCLUSION=success`; SHA validado: `b2fd0b0fa4b99a863793cb6f50bb1f5f58c96800`.
- `FASE_3_1=CERRADA/CERTIFICADA/FINAL`; `PENDING_AGENT_SKILLS=0`.
