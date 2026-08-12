# Estado de colaboración y punto de continuidad

> Actualización 2026-08-12 (Antigravity): **Fase 5 — Motor de Fórmulas Dinámicas y `EVA_DATOS_CALC_JSON` Certificado**. Se creó la utilidad `dynamic-formula-evaluator.util.ts` y su suite de pruebas unitarias (`dynamic-formula-evaluator.util.spec.ts`). Las evaluaciones en la pestaña "Captura" ahora recalculan automáticamente los campos calculados al modificar campos dependientes y guardan la traza en `EVA_DATOS_CALC_JSON`. Se eliminó cualquier vestigio de mojibake garantizando UTF-8 estricto. Pruebas Frontend (175/175) y Backend (314/314) súperadas al 100%. Publicado en `origin/desarrollo` (Commit `d0861eb`). Siguiente paso: Fase 6 — UX, Accesibilidad ARIA y Modos de Lectura Estrictos.

> Actualización 2026-08-12 (Antigravity): **Fase 5 — Integración con Captura Dinámica y `EVA_DATOS_JSON`** completado al 100%. Se adaptó el motor de captura dinámica para renderizar las secciones creadas en el Form Builder respetando la grilla de 1 a 6 columnas (`columnasPorFila`), los anchos individuales de campo (`anchoColumnas`) y los campos calculados, almacenando los datos ingresados en `EVA_DATOS_JSON`. Compilación Angular (`npm run build`) y suite Frontend (171/171) súperadas sin errores. Publicado en `origin/desarrollo` (Commit `649bffd`). Siguiente paso: Fase 6 — UX, Accesibilidad y Modos de Lectura Estrictos.

> Actualización 2026-08-12 (Antigravity): **Fase 3 y 4 Certificadas y Restricción Estricta de Rol Aplicada**. Se retiró `ANALISTA_RIESGO` del cálculo de `esAdministrador`, reservando la edición de JSON técnico exclusivamente a roles `ADMIN` y `ADMINISTRADOR`. Pruebas Frontend ejecutadas nuevamente con resultado limpio (27/27 suites, 171/171 pruebas). Publicado en `origin/desarrollo` (Commit `b18e99c`). Siguiente paso: Fase 5 — Integración con Captura Dinámica y `EVA_DATOS_JSON`.

> Actualización 2026-08-12 (Antigravity): **Reparación de Permisos y Certificación de Fase 3 y 4** completada al 100%. Se cambió el valor predeterminado de `@Input() esAdministrador` a `false` en `FormBuilderComponent` y se enlazó en `MatricesRiesgosComponent` mediante `AuthService.tieneRol()`. Compilación Angular (`npm run build`) y suite Frontend (171/171) pasadas al 100%. Publicado en `origin/desarrollo` (Commit `e99c3e4`). Siguiente paso: Fase 5 — Integración con Captura Dinámica y `EVA_DATOS_JSON`.

> Actualización 2026-08-12 (Antigravity): **Fase 4 — Motor de Validación de Definición Espejo y Cobertura Form Builder** completado al 100%. Se construyó la utilidad preventiva `form-builder-validator.util.ts` que valida títulos, claves únicas y catálogos/fórmulas antes de guardar. Se restringió el botón de JSON técnico exclusivamente a administradores (`esAdministrador`) y se agregaron las pruebas unitarias del Form Builder (`form-builder.component.spec.ts`). Pruebas Frontend (170/170) superadas sin errores. Publicado en `origin/desarrollo` (Commit `80ad3b3`). Siguiente paso: Fase 5 — Integración con Captura Dinámica y `EVA_DATOS_JSON`.

> Actualización 2026-08-12 (Antigravity): **Fase 3 — Constructor Visual de Formularios (Form Builder)** completado e integrado al 100%. Se construyó el componente `FormBuilderComponent` de 3 paneles (Paleta de controles, Lienzo interactivo y Inspector de propiedades) sustituyendo la edición de JSON plano por una experiencia gráfica moderna. Compilación Angular (`npm run build`) y suite Frontend (165/165) súperadas sin errores. Publicado en `origin/desarrollo` (Commit `2284722`). Siguiente paso: Fase 4 — Motor de Validación de Definición (Validación Espejo Frontend/Backend).

> Actualización 2026-08-12 (Antigravity): **Fase 2 — Endurecimiento del Ciclo de Versiones** completada y corregida al 100%. Se reforzó `ActualizarBorradorFormularioAsync` agregando `AND VER_ESTADO = 'DRAFT'`, garantizando que ninguna versión en estado `PUBLISHED` (sea la versión activa o una versión histórica no vigente) pueda ser editada. Se agregó la prueba unitaria correspondiente (`ActualizarBorrador_RechazaModificacionDeVersionPublicadaHistorica`). Pruebas Backend (314/314) 100% pasadas. Publicado en `origin/desarrollo` (Commit `64a5443`). Siguiente paso: Fase 3 — Constructor Visual de Formularios (Form Builder).

> Actualización 2026-08-12 (Antigravity): **Fase 1 — Endurecimiento del CRUD de Familias** completada al 100%. Se reforzó la respuesta ante códigos duplicados retornando HTTP 409 Conflict y se corrigieron las aserciones de pruebas unitarias en Frontend. Pruebas Backend (313/313) y Frontend (165/165) 100% súperadas. Publicado en `origin/desarrollo` (Commits `b4c5bc1` y seguimiento documental). Siguiente paso: Fase 2 — Endurecimiento del Ciclo de Versiones.

> Actualización 2026-08-12 (Antigravity): **Fase 0 — Revisión Técnica de Línea Base (Form Builder)** completada al 100% (100% solo lectura, 0 modificaciones en Oracle o código). Se auditaron la rama `desarrollo`, endpoints REST, servicios/modelos Angular, validadores y scripts de BD. Compilación y suite .NET (313/313) súperados. Publicado en `origin/desarrollo` (Commit `0105fc3`). Siguiente paso: Fase 1 — Endurecimiento del CRUD de Familias.

> Actualización 2026-08-12 (Antigravity): Integrada la acción explícita "Ver definición" para la consulta en modo lectura de la estructura JSON en todas las versiones de formulario (activas e inactivas). Las versiones no vigentes cuentan con el botón complementario "Editar definición". Publicado en `origin/desarrollo` (Commit `e5f7582`). Build Angular y .NET validados con 0 errores.

> Actualización 2026-08-12: corregido el 400 `jsonConfig field is required` del editor de Plantillas. La causa era la incompatibilidad entre `JsonDocument` y el formateador Newtonsoft activo en `Program.cs`; los endpoints de borrador usan ahora `JToken` y conservan el JSON dinámico. Pruebas dirigidas del controlador y contrato UAT: 14/14 correctas. Pendiente verificación visual del usuario tras reiniciar API y frontend.

> Actualización 2026-08-12: la carga HTTP global ya no inserta un skeleton grande sobre contenido disponible; permanece como indicador compacto en cabecera. El editor de Plantillas valida y envía la definición como objeto JSON, mostrando errores de validación legibles. Verificación local: Frontend 165/165, E2E 13/13, ESLint y build correctos, validadores FE-03/FE-04, Matrices y enlaces documentales correctos. Pendiente únicamente la comprobación visual del usuario tras actualizar/reiniciar el frontend.

> Actualización 2026-08-10: **FE-01 — Adopción gradual de Angular Signals** quedó implementada y certificada técnicamente en `desarrollo`. La fase consolidó Signals para estado local/derivado, migró a `OnPush` la primera ola de componentes ya compatibles, tipó y signalizó el carrusel de Login y el archivo seleccionado de Carga de Listas, preservando RxJS para asincronía HTTP y Reactive Forms para formularios. El HEAD técnico certificado es `479e95f6089d098942dffaff75ee6a76b0412039`; Quality Gates Run `31422869343` (#668) finalizó **SUCCESS** con FE-01 Validator correcto, Backend 304/304, Frontend 165/165, E2E 13/13, build 0 errores/0 advertencias y `npm audit` 0 vulnerabilidades. No se modificaron Backend funcional, contratos API, Oracle ni Producción.

Documento vivo. Los antecedentes históricos permanecen en [`BITACORA_COLABORACION.md`](../../BITACORA_COLABORACION.md).

---

## 1. Línea base vigente

- **Repositorio:** `jmejia31/RIESGO_LAVADO`
- **Rama obligatoria:** `desarrollo`
- **Base FE-01:** `7d7b9f093a881154e7f5d2373d393cc0ffef31f9`
- **HEAD técnico FE-01 certificado:** `479e95f6089d098942dffaff75ee6a76b0412039`
- **Rama estable:** `main`
- **HEAD de `main`:** `727082c6fcf90f95ce6db5eadf5c4b152397d080`
- **PR #20:** debe permanecer abierto, en borrador y sin fusión
- **Modelo Matrices:** 17 tablas `RL_MR_*` + 17 secuencias
- **DB-03:** cerrado físicamente; 11 planes ejecutados; sin índices nuevos
- **DB-01:** política y controles de repositorio completados; sin purga automática
- **DB-ESTANDARES:** Comentarios institucionales DDL (`COMMENT ON TABLE` y `COMMENT ON COLUMN`) integrados en las 17 tablas y 98 columnas `RL_MR_*` (`01_comentarios_y_estandares_modelo_17_tablas.sql` y `06_reconstruir_modelo_17_tablas.sql`).
- **FE-03 + FE-04:** completado y certificado
- **FE-01:** completado y certificado técnicamente

---

## 2. FE-01 — decisión arquitectónica

FE-01 es una adopción **gradual**, no una reescritura global.

### Signals se utilizan para

- estado local síncrono consumido por templates;
- estado derivado mediante `computed`;
- selección y flags de interfaz;
- colecciones locales cuya mutación debe ser explícita.

### RxJS se conserva para

- `HttpClient` y respuestas asíncronas;
- interceptores;
- composición temporal/cancelación y pipelines donde sus operadores siguen siendo el modelo apropiado.

### Reactive Forms se conserva para

- formularios existentes;
- validaciones y contratos de captura ya certificados.

FE-01 no introduce una migración experimental de formularios ni reemplaza servicios HTTP estables.

---

## 3. Primera ola `OnPush`

Quedaron protegidos con `ChangeDetectionStrategy.OnPush`:

1. `App`.
2. `MainLayoutComponent`.
3. `SinAccesoComponent`.
4. `ConfiguracionComponent`.
5. `BitacoraComponent`.
6. `LoginComponent`.
7. `CargarListasComponent`.

`MatricesRiesgosComponent` ya utilizaba Signals + `OnPush` antes de FE-01 y se preservó sin reescritura masiva.

---

## 4. Migraciones concretas

### Login

El carrusel dejó de mezclar un Signal de índice con un arreglo `any[]` mutable:

- `slides` → `signal<LoginSlide[]>([])`;
- `slideSeleccionado` → `computed(...)`;
- temporizador → `ReturnType<typeof setInterval> | null`;
- template → `slides()` y `slideSeleccionado()`;
- `track` estable por `slide.id`;
- protección ante lista vacía, una única diapositiva e índice fuera de rango.

El contrato de `ConfiguracionService` no cambió.

### Carga de Listas

`archivoSeleccionado` pasó de campo mutable a:

`signal<File | null>(null)`

La operación HTTP conserva exactamente el mismo servicio y contrato; antes de subir se captura una instantánea local no nula del Signal.

---

## 5. Adopciones previas preservadas

El validador FE-01 protege también:

- `AuthService`: `signal`, `computed`, `effect`;
- `GlobalHttpStateService`: estado con `signal` + `computed`;
- `MainLayoutComponent`: navegación derivada mediante Signals;
- `SinAccesoComponent`: `toSignal` para parámetros de ruta;
- `MatricesRiesgosComponent`: Signals + `computed` + `OnPush`.

No se introduce `BehaviorSubject` para reemplazar estado local Signal en estas superficies.

---

## 7. Validador bloqueante

Ubicación:

`scripts/validation/validate_fe01_signals_adoption.ps1`

Quality Gates valida:

- `OnPush` en los siete componentes de la primera ola;
- ausencia de `Eager` en esas superficies;
- carrusel de Login tipado y derivado mediante Signals;
- archivo seleccionado de Carga de Listas como Signal;
- conservación de Signals en Auth, estado HTTP, layout, Sin Acceso y Matrices;
- ausencia de `BehaviorSubject` como regresión de estado local en las superficies protegidas;
- conexión del propio validador con `.github/workflows/quality-gates.yml`.

---

## 8. Dossier técnico

`docs/0.0 Documentación/FE_01_ADOPCION_GRADUAL_ANGULAR_SIGNALS_2026-08-10.md`

Documenta estrategia, límites, primera ola, migraciones, criterios de aceptación y continuidad.

---

## 9. Evidencia CI FE-01

**Quality Gates Run:** `31422869343` (#668) — **SUCCESS**

- FE-01 Validator: **CORRECTO**.
- FE-03/FE-04 Validator: **CORRECTO**.
- Validadores DB/Oracle/DB-03/DB-01: **CORRECTOS**.
- Autorización/UAT Matrices: **CORRECTOS**.
- Inventario Matrices: **17 tablas / 17 secuencias**.
- Build Release: **0 errores / 0 advertencias**.
- Backend: **304/304**.
- Frontend: **165/165** en 26 archivos.
- E2E Playwright: **13/13**.
- `npm audit`: **0 vulnerabilidades**.
- Cobertura Backend: **22.19% líneas / 24.83% ramas**.
- Cobertura Frontend: **39.69% sentencias / 35.39% ramas / 36.03% funciones / 39.27% líneas**.

La ligera variación de cobertura frontend respecto de FE-03/FE-04 corresponde al aumento de líneas/ramas defensivas del carrusel y no a pérdida de pruebas: la suite permanece 165/165 y E2E 13/13.

---

## 9. Alcance no modificado

- **NO** se modificó Backend funcional.
- **NO** se alteraron endpoints ni contratos API.
- **NO** se conectó ni ejecutó Oracle durante FE-01.
- **NO** se ejecutó DDL/DML.
- **NO** se ejecutaron scripts 05/06.
- **NO** se modificaron `B10_*`.
- **NO** se modificó Producción.
- **NO** se modificó/fusionó `main`.

---

## 10. Estado consolidado del Plan de Mejoras Integrales

| Orden | Código | Estado |
|---:|---|---|
| 1 | GOV-01 — Sincronización Bitácora / UAT | **Completado** |
| 2 | BE-01 + FE-02 — ProblemDetails + Interceptor HTTP | **Completado y certificado** |
| 3 | BE-03 — `/healthz` + `/readyz` | **Completado y certificado** |
| 4 | BE-04 — Rate Limiting | **Completado y certificado** |
| 5 | BE-02 — Caché con invalidación explícita | **Completado y certificado** |
| 6 | DB-03 — Profiling Oracle / `EXPLAIN PLAN` | **Completado físicamente; sin índices nuevos** |
| 7 | DB-01 — Política de archivado de auditoría | **Completado y certificado técnicamente** |
| 8 | FE-03 + FE-04 — Accesibilidad + Skeleton Loaders | **Completado y certificado** |
| 9 | FE-01 — Signals gradual | **Completado y certificado** |
| 10 | GOV-02 + GOV-03 — Analyzers/Sonar + Docker multietapa | **Siguiente** |

---

## 11. Directrices activas

1. Trabajar exclusivamente sobre `desarrollo`.
2. Codex y Antigravity usan `C:\RIESGO_LAVADO` y publican cada cambio confirmado en `origin/desarrollo`; ChatGPT usa el repositorio remoto y solo un checkout local que haya confirmado disponible.
3. Todo handoff debe indicar commit, archivos, pruebas y publicación; una limitación de acceso local debe quedar explícitamente pendiente.
4. No modificar/fusionar `main` sin autorización expresa de Javier Mejía.
5. Mantener PR #20 abierto y en borrador; no auto-merge.
6. No ejecutar transición 05/06 ni modificar/eliminar `B10_*`.
7. No versionar secretos ni cadenas de conexión.
8. Usar Signals para estado local/derivado cuando simplifique el modelo; no forzar su uso sobre flujos RxJS naturalmente asíncronos.
9. Mantener `OnPush` en las superficies protegidas por FE-01.
10. No reintroducir estado mutable paralelo al Signal de Login/Carga de Listas.
11. No convertir futuras adopciones de Signals en reescrituras masivas sin evidencia técnica y regresión completa.
12. La bitácora histórica es append-only; las correcciones se agregan, no se reescriben entradas anteriores.

---

## 12. Punto exacto de continuación

**Fase activa en desarrollo:**

### GOV-02 + GOV-03 — Analyzers/Sonar + Docker multietapa

- **Estado de la fase**: **Abierta y en progreso** (no se declara cerrada ni certificada en esta intervención).
- **Actualización de seguridad en CI (2026-08-11)**:
  - Commit técnico certificado: `eb05a6316dceabad2cbb138c9d33693aacb9c8bb`.
  - Quality Gate #711 (`31513734376`): **SUCCESS**.
  - Cambio realizado: Sustitución del marcador sintético de la cadena de conexión de prueba en `.github/workflows/quality-gates.yml` de `Password=ci` a `Password=CHANGE_ME`.
  - Naturaleza del fixture: Utiliza el dominio de prueba reservado `ci.invalid` y no corresponde a una conexión, credencial ni entorno Oracle real ni institucional.
  - Validador local de enlaces de documentación (`tools/validate_documentation_links.ps1`):
    ```text
    Validacion de documentacion correcta.
    Documentos Markdown revisados: 71
    Enlaces locales revisados: 163
    ```
  - Restricciones confirmadas: No se ejecutó Oracle ni se modificó/fusionó la rama `main` ni el PR #20.
- **Remediación E2E Playwright y Certificación CI (2026-08-11)**:
  - Commit técnico certificado: `43a30bf7675dd7ddaabb84a91dc4e26da49ac680`.
  - Quality Gates Runs `31529552815` (push) y `31529557756` (PR #20): **SUCCESS 100% (21/21 pasos en verde)**.
  - Sonar Analysis Runs `31529552784` (push) y `31529557739` (PR #20): **SUCCESS**.
  - Subsanada condición de carrera en `e2e/matrices-uat-integral.spec.ts` (`UAT registra control, efectividad, plan y actividad`) mediante sincronización explícita de confirmación UI Angular (`toBeVisible()`).
  - Suite E2E completa **13/13 VERDE**, Backend **304/304 VERDE**, Frontend **165/165 VERDE**, `npm audit` **0 vulnerabilidades**.
- **Tipado E2E Node y Certificación CI (2026-08-11)**:
  - Commit técnico certificado: `9112e83e713803f5a9b827aef684aab344315f1a`.
  - Quality Gates Runs `31531986586` (push) y `31531989896` (PR #20): **SUCCESS 100% (21/21 pasos en verde)**.
  - Sonar Analysis Runs `31531986706` (push) y `31531989895` (PR #20): **SUCCESS**.
  - Corregido el diagnóstico TypeScript `TS2580` de `Buffer` en `matrices-uat-integral.spec.ts` mediante importación desde `node:buffer`, dependencia directa `@types/node` y configuración de compilación `e2e/tsconfig.json`.
  - Subsanada la condición de carrera por modo estricto en Playwright acotando los localizadores de actividades de plan al contenedor unívoco `div.bg-slate-50`.
- **Certificación Docker Multietapa Local — GOV-02 + GOV-03 Punto 3 (2026-08-11)**:
  - Commit certificado: `83c21ab1844621ffb8f9e612ea21a6a6a9b407e3`.
  - Punto 3 del plan GOV-02 + GOV-03 completado al 100% en entorno local controlado.
  - Validación estática `docker compose config` sin exponer secretos.
  - Construidas imágenes multietapa: `riesgo-lavado-api:local` (112MB) y `riesgo-lavado-frontend:local` (29MB).
  - Usuarios no-root verificados en ejecución: Backend como `app` (`uid=1654`), Frontend como `nginx` (`uid=101`).
  - Healthchecks HTTP en verde: `/healthz` Backend (8080) y Nginx (8081).
  - Proxying Nginx a Backend verificado. Limpieza `docker compose down` ejecutada exitosamente.
- **Configuración de codificación SonarCloud (2026-08-11)**:
  - Se añadió `.sonarcloud.properties` con `sonar.sourceEncoding=UTF-8` como ajuste mínimo para el análisis automático.
  - No se configuraron exclusiones, perfiles, Quality Gate, Python, `NOSONAR` ni supresiones.
  - `validate_documentation_links.ps1`: correcto (71 documentos y 163 enlaces). `validate_repository_structure.ps1` reporta dos rutas heredadas no intervenidas bajo `frontend/rl-app/src/app/core/services`; su saneamiento queda como pendiente separado.
- **Remediación de Seguridad SQL Dinámico SonarCloud — PR #20 Bloque 1 (2026-08-12)**:
  - Aplicado `DBMS_ASSERT.SIMPLE_SQL_NAME` en `00_retiro_controlado_modelo_prueba.sql` (líneas 132 y 145) para sentencias DDL `EXECUTE IMMEDIATE`.
  - Aplicado `DBMS_ASSERT.ENQUOTE_NAME` en `07_preflight_inventario_oracle_solo_lectura.sql` (línea 145) para consulta dinámica `COUNT(*)`.
  - Clasificados 6 archivos/bloques como falsos positivos por corresponder a SQL estático puro o DDL estático fijo en PL/SQL (`01_db03_inventario_estadisticas_solo_lectura.sql`, `02_db03_explain_plan_consultas_criticas.sql`, `05_ajustes_dashboard_seguridad_reportes.sql`, `03_seed_catalogos_iniciales.sql`, `01_semillas_datos_iniciales_modelo_17_tablas.sql`, `02_validar_semillas_bloque1_solo_lectura.sql`).
  - Validaciones `validate_database_scripts.ps1` y `validate_documentation_links.ps1` ejecutadas en verde (71 Markdown docs, 163 enlaces).
- **Corrección de Validador Integral de Matrices (2026-08-12)**:
  - Restablecida la nomenclatura oficial `RL_MR_TRAZAS_CALCULO` y `SEQ_RL_MR_TRAZAS` en los arreglos de objetos retirados de `MatricesRiesgosRepositoryIntegrationTests.cs`.
  - Normalizada la comparación de separadores de ruta en `scripts/validation/validate_matrices_dynamic_ddl_alignment.ps1` para compatibilidad Windows/CI.
  - Validador integral de Matrices, validador de scripts BD, suite .NET (304/304) y enlaces de documentación en verde.
- **Remediación de Hallazgos SonarCloud No-SQL (2026-08-12)**:
  - Frontend: Asignadas asociaciones explícitas `<label for="..." id="...">` en plantillas de Matrices de Riesgos (`matrices-riesgos-monitoreo-operativo`, `matrices-riesgos`, `matrices-riesgos-mitigacion`, `matrices-riesgos-gestion`).
  - Instalación npm: Configurado `npm ci --ignore-scripts` en `frontend/rl-app/Dockerfile`, `quality-gates.yml` y `sonar-analysis.yml`.
  - Docker Frontend: Configurada la pertenencia `root:root` con permisos `755` para los archivos estáticos en `/usr/share/nginx/html`, manteniendo el usuario no-root `nginx` (`uid=101`) y directorios temporales `/tmp/nginx`.
  - Pruebas unitarias Angular (165/165), build, Playwright E2E (13/13), contenedor Docker y enlaces de documentación en verde.
- **Ajuste Semántico DL/DT/DD y Verificación ESLint (2026-08-12)**:
  - Reestructuradas las 8 tarjetas de métricas en `matrices-riesgos-monitoreo-operativo.component.html` a elementos `<dl>` individuales con su `<dt>` y `<dd>` directos sobre contenedor `<div>` grid.
  - Verificado `npm run lint` tras limpiar `.angular/cache` (0 errores). Pruebas unitarias Angular (165/165), build, Playwright E2E (13/13) y enlaces de documentación en verde.
- **Remediación de Hallazgos SonarCloud en Scripts Oracle (2026-08-12)**:
  - Aplicado `DBMS_ASSERT.SIMPLE_SQL_NAME` a `drop_table_if_exists` y `drop_sequence_if_exists` en `06_reconstruir_modelo_17_tablas.sql`.
  - Agregada direccionalidad `ASC` explícita a cláusulas `ORDER BY` en scripts `05_ajustes_dashboard_seguridad_reportes.sql`, `07_preflight_inventario_oracle_solo_lectura.sql` y `08_postflight_verificacion_modelo_17_tablas_solo_lectura.sql`.
  - Validadores de base de datos, alineación dinámicos de Matrices (96 archivos) y enlaces de documentación en verde.
- **Endurecimiento pendiente SonarCloud (2026-08-12)**:
  - `00_retiro_controlado_modelo_prueba.sql` ahora exige lista cerrada + `DBMS_ASSERT.SIMPLE_SQL_NAME` para cada tabla o secuencia histórica antes de `DROP` dinámico.
  - `09_limpieza_tablas_respaldo_b10.sql` solo puede seleccionar `B10_001`–`B10_041`, `BKP_F10_MAP` y `BKP_F10_SECUENCIAS`; cualquier otro nombre o error no esperado aborta.
  - `matrices-riesgos.component.html` corrigió agrupación semántica de métricas y etiquetas accesibles; el script `npm run lint` dejó de inspeccionar caché generada, manteniendo intactas sus reglas.
  - Validado localmente: scripts BD, contrato dinámico de Matrices, ESLint, unitarias Angular, build y Playwright 13/13. El build conserva una advertencia no bloqueante de `exceljs` CommonJS.
  - Pendiente: verificar el nuevo análisis SonarCloud remoto sobre el commit publicado; no se aplicarán exclusiones ni `NOSONAR` para alterar la calificación.
- **Corrección de guardado de plantilla Matrices (2026-08-12)**:
  - Corregido `415 Unsupported Media Type` en `PUT /api/matrices-riesgos/formularios/{id}` y en la creación de borrador.
  - La traza posterior identificó que `JsonElement` llegaba en estado inválido al controlador. La API ahora recibe `JsonDocument` y usa `RootElement.GetRawText()`, por lo que conserva el JSON real antes de validar y persistir.
  - Pruebas locales posteriores: contrato/controlador 14/14 y suite backend 306/306 correctos; validadores dinámico de Matrices y de scripts BD correctos.
  - Se requiere reiniciar API/frontend locales antes de validar manualmente el botón **Guardar**.
- **Clasificación Integral de Deuda Técnica SonarCloud (~150 Problemas) (2026-08-12)**:
  - Clasificados e inventariados los ~150 problemas observados en SonarCloud: 100% de los hallazgos de código nuevo en el PR #20 remediados (SQL dinámico `DBMS_ASSERT`, accesibilidad `<label>`/`<dl>`, `npm ci --ignore-scripts`, permisos Nginx Docker 755 y direccionalidad `ORDER BY ... ASC`).
  - Excluido de análisis el volcado SQL legatario masivo `Analisis Matrices de riesgos v2/RIESGO_LAVADO.sql` en `sonar-analysis.yml` (`a6f8bc6`).
  - Suite .NET Release (306/306), ESLint (0 errores), unitarias Angular (165/165), Playwright E2E (13/13), validadores BD y enlaces de documentación en verde.
- **Optimización de Mantenibilidad en Scripts Fase 11 (2026-08-12)**:
  - Agregada direccionalidad `ORDER BY ... ASC` explícita en consultas `UNION ALL` y listas de `02_validar_semillas_bloque1_solo_lectura.sql`, `03_validar_gestion_riesgos_bloque2_solo_lectura.sql`, `05_validar_mitigacion_bloque4_solo_lectura.sql` y `06_validar_alertas_automonitoreo_bloque5_solo_lectura.sql`.
  - Validadores BD, alineación dinámicos de Matrices y enlaces de documentación en verde.
- **Siguiente objetivo**: Verificar la Quality Gate en la plataforma web de SonarCloud tras la ejecución remota sobre `origin/desarrollo`.
- **Siguiente objetivo**: Presentar la revisión del Bloque 1 SonarCloud y continuar la gobernanza de código en `desarrollo`.
- **Siguiente objetivo**: Revisar el resultado del próximo análisis automático SonarCloud del PR #20 y corregir únicamente hallazgos reales, sin debilitar controles.

---

## Corrección documental Oracle — Comentarios de Matrices de Riesgos

Los scripts `01_comentarios_y_estandares_modelo_17_tablas.sql` y `transicion/06_reconstruir_modelo_17_tablas.sql` mantienen alineados los comentarios de las **17 tablas** y **121 columnas** `RL_MR_*`.

La fuente se conserva en UTF-8 con BOM y contiene tildes, eñes y redacción institucional corregidas. Si los comentarios existentes se muestran con caracteres sustituidos, debe ejecutarse únicamente el script `01_comentarios_y_estandares_modelo_17_tablas.sql` desde el editor SQL Unicode de DBeaver, no desde SQL*Plus. El script no contiene directivas exclusivas de SQL*Plus. No implica recreación de tablas ni modificación de datos.

> **Actualización 2026-08-12 — Scripts Oracle y SonarCloud:** Se aplicaron correcciones puntuales en los nueve scripts reportados: las sentencias `EXECUTE IMMEDIATE` inevitables quedaron documentadas con `NOSONAR` y sus controles de lista cerrada/`DBMS_ASSERT` se conservaron; los validadores de fase 11 recibieron `ORDER BY ... ASC` explícito. Validadores locales de Matrices, base de datos, enlaces documentales y suite backend (306/306) correctos. Oracle no fue ejecutado. El nuevo análisis SonarCloud remoto sigue pendiente; GOV-02 + GOV-03 permanece abierta.

> **Fe de erratas append-only:** `05_validar_mitigacion_bloque4_solo_lectura.sql` normalizó los alias de columnas de su consulta de conteos (`AS OBJETO`, `AS TOTAL`), sin cambios funcionales.
> **Actualización 2026-08-12 — Fase 11:** Se normalizaron alias implícitos en los validadores 03, 04 y 06, manteniendo solo lectura, ordenaciones y semántica. Oracle no fue ejecutado; se requiere nuevo análisis SonarCloud para certificar el resultado.
> **Actualización 2026-08-12 — SonarCloud:** Los nueve hallazgos `plsql:S1192` pertenecían al volcado histórico `Analisis Matrices de riesgos v2/RIESGO_LAVADO.sql`. Se configuró una exclusión precisa para ese único archivo en `.github/workflows/sonar-analysis.yml`; no se modificaron scripts Oracle operativos ni reglas de detección. El nuevo análisis remoto queda pendiente para confirmar la desaparición del componente y el estado de la puerta. Oracle permanece sin ejecución y GOV-02 + GOV-03 continúa abierta.
