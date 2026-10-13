# Bloque 7 — Editor de evaluación en seis bloques

**Estado:** `BLOCK7_STATUS=PENDING_CORRECTION`
**Última intervención:** COD — 2026-10-13, America/Tegucigalpa
**Alcance:** crear y editar borradores desde el editor de Matrices de Riesgos, sin migración de los 59 casos de referencia y sin escrituras productivas.

## Implementación de esta intervención

- Se conservó el modal de evaluación existente y se organizó su edición en seis bloques con rangos 01–19, 20–33, 34–39, 40–49, 50–69 y 70–82.
- Se agregaron `POST /api/matrices-riesgos/evaluaciones/borrador` y `PUT /api/matrices-riesgos/evaluaciones/{id}/borrador`. Aceptan respuestas estructuralmente válidas con campos obligatorios aún pendientes; continúan rechazando valores inválidos y las identidades de riesgo o versión manipuladas. Los cálculos enviados por el cliente no se toman como autoridad.
- Se agregó `POST /api/matrices-riesgos/evaluaciones/calculo-preview`, que reutiliza el runtime de cálculo del backend y no persiste. La UI solicita la vista previa con debounce y cancela la petición previa al editar otro valor.
- Después de guardar, el editor vuelve a leer el detalle y la proyección institucional 82/82. Las escrituras de controles, planes, actividades y seguimiento notifican al editor para refrescar esa proyección. Las entidades repetibles continúan usando sus DTOs y endpoints estructurados existentes.
- Se retiró la llamada de recálculo del frontend al cambiar una respuesta. Los campos calculados/sistema se vuelven de solo lectura a partir del contrato `MATRIX_FIELDS`; GTIC se mantiene opcional porque no se encontró una regla autoritativa para inferir su activación.
- `tools/validate_block7_ui_contract.js` compara las 82 etiquetas visibles y los seis rangos con el manifest backend. El workflow de Quality Gates ejecutará este validador.

## Evidencia ejecutada localmente

- Release build del API: PASS.
- Suite backend: 955/955 PASS.
- Contract validator: 82/82 PASS.
- Catalog validator: 14 catálogos / 91 elementos, drift 0.
- Frontend aislado, fuera del checkout compartido: `npm ci`, build, lint y auditoría npm (0 vulnerabilidades) PASS; tests completos 846/846 PASS.
- Playwright: el flujo de crear borrador parcial, continuar y recargar pasó contra respuestas de API simuladas. Esta prueba no ejecuta Oracle ni acredita persistencia de una API real.
- Validadores de UTF-8, estructura del repositorio, scripts de base de datos y enlaces de documentación: PASS.

## Pendientes para el cierre

- El checkout no tiene Oracle XE aislado. El workflow actual ejecuta integración Oracle XE del GET de Bloque 6, no el POST/PUT de borradores ni el editor Playwright contra una API conectada a XE. Por tanto, `E2E_CREATE_EDIT_ORACLE` y `ORACLE_XE_OR_CI_INTEGRATION` para las escrituras de Bloque 7 siguen sin evidencia.
- Falta una prueba de integración de los endpoints de borrador/preview contra el repositorio Oracle y un recorrido de navegador conectado a esa API.
- No se certificaron aún pruebas de responsive 1280×900/390×844, accesibilidad de errores inline/foco, conflictos concurrentes de edición ni todos los estados de validación por bloque. La vista previa y navegación sí tienen pruebas unitarias/build, pero estos gates no se consideran cubiertos por ello.
- El trigger de GTIC permanece sin resolver en las fuentes institucionales revisadas; los campos 17–19 no se hacen obligatorios ni se inventan valores.

No se declara `BLOCK7_STATUS=CLOSED` mientras permanezcan estos pendientes. Bloques 1–6 continúan cerrados; Bloque 8 no se inició. Producción no se consultó ni modificó: `PRODUCTION_DML_EXECUTED=0`, `PRODUCTION_DDL_EXECUTED=0`, `PRODUCTION_APPLY_EXECUTED=NO`.

## Seguimiento de CI y pruebas E2E

- Quality Gates run `38453709129` (SHA `1379b5fe72d5d7d7ecb081ee6230ede663357608`) ejecutó con éxito validadores, analizadores, build Release y la integración Oracle XE de Bloque 6. Falló en tres E2E que todavía buscaban el formulario anterior: el botón "Guardar cambios", el botón "Crear evaluación" dentro del formulario y el campo F39 visible sin navegar al Bloque 3.
- Se corrigió la navegación de edición para permitir acceso directo a cualquier bloque de una evaluación existente y se actualizaron los E2E al flujo de borrador/bloques. En copia frontend aislada pasaron tests 846/846, lint, build y las suites Playwright `login-and-routing.spec.ts` + `matrices-uat-integral.spec.ts`: 22/22.
- El CI correctivo `38455840579` del SHA `6f87a85059bbf64dc926d437eccfacca7ce934d2` terminó `completed/success`, incluyendo los E2E corregidos y el harness Oracle XE de Bloque 6. Estas pruebas Playwright usan API simulada; no acreditan escritura real Oracle. Por ello se mantiene `BLOCK7_STATUS=PENDING_CORRECTION` hasta incorporar y pasar el E2E browser→API→Oracle y los gates de accesibilidad/responsive pendientes.

## Continuacion Bloque 7B - correccion de version optimista y drafts parciales (COD) - 2026-10-13 14:09 -06:00

- Inicio: rama `desarrollo`, `HEAD=origin/desarrollo=306cd0430efb9718a5d3dacd8ee960c7f1107535`; `git pull --ff-only` quedo al dia. Se preservaron sin stage los cambios ajenos de `DatabaseEnvironmentGuard.cs`, sus pruebas, `frontend/rl-app/tsconfig.json` y todos los untracked previos.
- El servicio de borradores ya no sustituye el `EvaVersionRow` esperado por el cliente. Ahora compara con la fila persistida y devuelve 409 con mensaje de recarga al detectar version obsoleta; el repositorio conserva `FOR UPDATE` y `WHERE EVA_VERSION_ROW = :versionRow`. Se agregaron dos pruebas de servicio para version obsoleta y version vigente.
- Se corrigio un impedimento al draft parcial: la tabla de proyeccion resume valores calculados obligatorios, por lo que crear una fila resumen ficticia fallaba cuando el draft aun no tenia VRI/VRR. El repositorio ahora omite esa fila mientras faltan ambos resultados canonicos y la elimina si una edicion vuelve incompleto el draft; cuando ya existe calculo, conserva/crea el resumen dentro de la misma transaccion. Sin cambios DDL. Agregadas tres pruebas unitarias de elegibilidad de resumen.
- El frontend mapea HTTP 409 a un mensaje de conflicto seguro que indica que la captura actual permanece en pantalla; una prueba unitaria lo verifica.
- Verificado en esta intervencion: Release build de `RIESGO_LAVADO.sln` PASS, 0 errores; suite backend 960/960; frontend aislado `npm ci` (0 vulnerabilidades), 847/847, lint y build PASS; Playwright de regresion existente 45/45 PASS (suite con API mock; no es Oracle). Contrato 82/82; catalogos 14/91, drift 0; validador UI/manifest 82/82; UTF-8/mojibake PASS, 0 hallazgos; estructura (118 rutas, 1052 archivos), base de datos y enlaces (175 Markdown, 201 enlaces) PASS.
- Quality gate local integrado ejecutado: backend/cobertura 960/960 PASS; la etapa frontend integrada fallo porque el `node_modules` compartido no contiene `@angular/build:unit-test`. El frontend completo paso en una copia aislada temporal; no se altero ni elimino `node_modules` compartido.
- Oracle XE y el E2E Browser->API->Oracle NO se ejecutaron: `docker ps` no mostro contenedores y no estaban disponibles las variables de conexion/opt-in. El run historico `38456944058` valida el GET de Bloque 6, no escrituras de Bloque 7. Concurrencia Oracle, POST/PUT real, preview sin mutacion, E2E real, multi-control/multi-plan, a11y con browser assertions y medidas responsive siguen sin certificarse.
- GTIC: se conserva el tratamiento opcional/condicional sin generar valores ni inferir trigger. El validator estatico sigue en 82/82; round-trip de valores GTIC no fue probado en API/Oracle real. No se declara `GTIC_GOVERNANCE=PASS` integral.
- Artefactos externos: RUN_ID `20261013_140917_140`, `%USERPROFILE%\Documents\RIESGO_LAVADO\BLOCK7_EVALUATION_FLOW\20261013_140917_140`; 4 archivos enumerados, hashes recalculados y verificados (`SHA256SUMS_VERIFIED=PASS`).
- No se consulto ni escribio produccion: DML=0, DDL=0, apply=NO; no se cambio esquema ni se agrego bypass de autenticacion. `BLOCK7_STATUS=PENDING_CORRECTION`, `BLOCK8_STATUS=NOT_STARTED`. El cierre requiere integrar y pasar el harness Oracle de escrituras y el browser real, mas los gates de accesibilidad/responsive; seguir con integracion y CI sin reimplementar el editor.

### Seguimiento 7B - CI del arreglo y estado (2026-10-13)

- Commit `5e5069bd9557574a1742a84a00369741d6a36791` publicado; Quality Gates `38460737573` concluyo `completed/success`. Pasaron validadores, build, quality gates, E2E vigente y la integracion Oracle XE del GET canonico del Bloque 6.
- La workflow aun no incluye pruebas de escritura de borradores/preview del Bloque 7 ni browser conectado a Oracle. No atribuir el CI como evidencia de esos gates. POST/PUT Oracle, preview sin mutacion, concurrencia real, browser create/edit/reload, multi-control/plan, accesibilidad browser, responsive y round-trip GTIC siguen pendientes.
- Se corrigio la preservacion de `EvaVersionRow` enviada por cliente y la elegibilidad de la fila de resumen para drafts parciales; frontend informa 409 de forma segura. Verificaciones locales: backend 960/960, frontend aislado 847/847, Playwright existente 45/45, build/lint frontend aislados PASS. El gate integrado local frontend sigue bloqueado por `@angular/build:unit-test` ausente en dependencias compartidas.
- GTIC no tiene trigger autoritativo identificado: F17-F19 permanecen opcionales/condicionales, no generan datos y no bloquean avance vacíos. No se certificó su round-trip Oracle.
- Estado honesto: `BLOCK7_STATUS=PENDING_CORRECTION`; `BLOCK8_STATUS=NOT_STARTED`. Cero escrituras de producción y sin cambio de esquema. Para cerrar, agregar y pasar las pruebas opt-in Oracle write/browser real al workflow existente y completar a11y/responsive.
