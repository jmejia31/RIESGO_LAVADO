# Bloque 9 — Mitigación y monitoreo

**Estado:** `BLOCK9_STATUS=CLOSED`.
**CI final:** Quality Gates `38625179428`, SHA `ea3631bd33af480363cc576efdf037f97f809ebf`, `completed/success`.
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
- `npm test -- --watch=false`: PASS, 858/858, incluyendo pruebas del componente institucional compartido.
- `npm run lint`, `npm run build`, `npm audit --audit-level=moderate`: PASS; 0 vulnerabilidades.
- Validadores 82 campos, catálogos, UI Block 7/8 y contrato Block 9: PASS; proyección 82/82, catálogo 14/91, drift 0.
- `bash -n tools/run_block7_oracle_e2e.sh` y `git diff --check`: PASS.

## Certificación requerida en CI

El Quality Gates reutiliza el único Oracle XE desechable existente. El host de prueba exige entorno `E2E`, opt-in explícito y que `DatabaseEnvironmentGuard` acepte `LOCAL_XE`. En ese host, el test browser real cubre planes y actividades, F41/F43, alertas/duplicados, automonitoreos, los cuatro escenarios de capacidades, CRUD de seguimiento por tipo, carga/vínculo/orden/desvínculo/protección de evidencia, auditoría, F70–F82, Angular y viewport desktop/móvil. Las rutas críticas se dejan pasar a la API real.

Quality Gates confirmó Oracle XE y browser real en la misma ejecución: `BLOCK7_ORACLE_TESTS_EXECUTED=1`, `BLOCK7_ORACLE_WRITE_INTEGRATION=PASS`, `BLOCK7_REAL_BROWSER_TESTS_EXECUTED=1`, `BLOCK7_BROWSER_API_ORACLE_E2E=PASS`, `BLOCK9_CRITICAL_ROUTES_MOCKED=0`, `BLOCK9_MITIGATION_CRUD=PASS`, `BLOCK9_MONITORING_CRUD=PASS`, `BLOCK9_EVIDENCE=PASS` y `BLOCK9_RBAC=PASS`. La integración Block6 pasó 3/3 y Block8 1/1. Los resultados anteriores marcados pendientes en esta bitácora son históricos y quedaron superados por este SHA.

**Seguridad:** no se ejecutó DML/DDL en producción; no se cambió el esquema; la autenticación de prueba sólo existe en el host E2E protegido por flags y el guard de base de datos. `BLOCK8_STATUS=CLOSED`; el bloque siguiente permanece sin iniciar.

## Estándar transversal de carga de archivos

El selector de archivos usado en Monitoreo de Listas se extrajo como `InstitutionalFileUploadComponent`, y Listas, Matrices de Riesgos y Configuración ya consumen la misma interfaz. El componente es sólo selección/presentación: el consumidor mantiene upload, asociación, persistencia, autorización y auditoría. Hay cinco instancias en cuatro templates; no quedan inputs file crudos fuera del componente. Las políticas configurables mantienen las restricciones reales de cada endpoint. El estándar y su inventario se describen en [Estándar institucional de carga de archivos](../0.0%20Documentación/ESTANDAR_INSTITUCIONAL_CARGA_ARCHIVOS.md).

La certificación transversal forma parte de esta misma ejecución del Bloque 9. `SHARED_FILE_UPLOAD_COMPONENT=PASS`, `MONITOREO_LISTAS_USES_SHARED_FILE_UPLOAD=PASS`, `MATRICES_USES_SHARED_FILE_UPLOAD=PASS`, `ALL_USER_FILE_UPLOAD_SURFACES_AUDITED=PASS` (5 superficies en 4 templates), `RAW_FILE_INPUTS_OUTSIDE_SHARED_COMPONENT=0`, `CANONICAL_DROPZONE_TEXT_IMPLEMENTATIONS=1`, `DRAG_DROP=PASS`, `CLICK_TO_SELECT=PASS`, `FILE_UPLOAD_KEYBOARD=PASS` y `FILE_UPLOAD_ACCESSIBILITY=PASS`. Monitoreo de Listas pasó su E2E de selección, retiro y drag/drop en Chromium; el E2E Matrices→API→Oracle también pasó en Quality Gates y dejó la captura revisada `institutional-file-upload-matrices.png`.

El uploader compartido no añade llamadas HTTP ni cambia las políticas backend. El validador se ejecuta en Quality Gates para impedir inputs de archivo crudos fuera del componente compartido.

## Tema dinámico y estándar global de modales

La ampliación visual del Bloque 9 reutiliza el estándar modal que ya existía; no crea un segundo sistema ni cambia el backend. `colorPrimario` y `colorSecundario` de `ConfiguracionService` son la fuente única. Carga y guardado llaman al mismo método de tema; la raíz del documento recibe los dos tokens RGB, los once tonos `ihss-50…950`, foregrounds contrastados, superficies de modal y controles de cierre. Un modal abierto se actualiza al cambiar esas propiedades.

El header y footer institucionales heredan un gradiente dinámico. El generador oscurece cada extremo del gradiente hasta alcanzar contraste de texto WCAG AA ≥4.5:1; el body, backdrop y sombras permanecen neutros. El color primario o secundario inválido no rompe CSS y conserva el último tema válido. Botones de marca, foco, selección y acentos usan la paleta dinámica; los azules de mensajes informativos siguen siendo semánticos.

La auditoría encontró 22 instancias modales. Se migraron una vez los dos shells con headers fuera de la clase canónica; los demás conservaron su template. Todos los 22 quedan cubiertos por el tema global, sin color especial para Matriz completa. No se alteraron tamaños, scroll, eventos, formularios ni flujos.

La configuración y los estilos institucionales están descritos en [Estándar de tema dinámico y modales](../0.0%20Documentación/ESTANDAR_TEMA_DINAMICO_Y_MODALES.md). Los validadores `validate_institutional_modal_standard.js` y `validate_dynamic_system_theme.js` se ejecutan en Quality Gates. La auditoría global no encontró colores de marca azules fijos; permanecen tres bindings azules para el estado semántico `info`.

Verificación local de esta enmienda: frontend 867/867; lint, build, `npm audit` (0 vulnerabilidades), validadores modal/tema y `git diff --check` PASS. E2E Chromium: modal shell 8/8, flujo Matriz completa 1/1, uploader Monitoreo de Listas 1/1 y prueba visual azul/rojo 2/2. Las capturas y auditorías externas se guardan junto con los artefactos del Bloque 9 y se sellan en `SHA256SUMS.json`. Quality Gates remoto del SHA que contiene esta documentación se verifica antes del handoff.

La auditoría final también revisó templates Angular inline en TypeScript y colores asignados a SweetAlert. Se eliminaron los colores fijos hallados en la capa visual: 52 botones de confirmación ahora consumen `--ihss-primary-color`; los defaults de Configuración están centralizados; los estados pendientes de selector/foco usan tokens `ihss-*`. Los tres azules informativos semánticos permanecen clasificados como `info`. Los validadores modal/tema, contrato 82/82, catálogos, UI Block7/Block8/Block9, estructura, scripts DB y enlaces documentales pasaron en esta intervención. Frontend local: lint, build, audit sin vulnerabilidades y 867/867 unitarias; Playwright local focalizado: modal shell 8/8, Matriz completa azul/rojo 1/1 y uploader Monitoreo de Listas 1/1. El resultado remoto del SHA publicado se añadirá al registro después de CI.

Quality Gates `38636545241` ejecutó el SHA `7180b0950142542356ba15a2d2d7de2b2ef529ae` y terminó `completed/success`. CI verificó backend 965/965, frontend 867/867, Block7 Oracle/browser 1/1, Block9 Oracle mitigación/monitoreo/RBAC/evidencias y browser real PASS (1 prueba de cada tipo), rutas críticas mocked=0, Block6 Oracle 3/3, Block8 Oracle 1/1, contenedores y publicación de capturas. El commit documental que registra este run tendrá su Quality Gates propio antes del handoff.

### Corrección de navegación en el browser real

En CI `38613567583`, los gates de build y validación pasaron, pero el test de carga desde Matrices buscó el uploader después de abrir `Seguimiento de evaluación`, acción que corresponde al workflow transaccional. Se corrigió la ruta de prueba para abrir el borrador con `Editar evaluación` y navegar al Bloque 6 antes de seleccionar evidencia. La repetición Oracle/browser es obligatoria; el estado permanece pendiente hasta el CI completo exitoso.

El run `38614577297` pasó la carga de evidencia desde esa ruta y progresó al conflicto concurrente. El test sólo falló al hacer un segundo cierre del modal después de que un guardado exitoso ya lo había cerrado. El cierre del E2E ahora es condicional si el diálogo permanece visible. Hace falta confirmar la repetición íntegra en CI.

En el run `38615607010`, el flujo superó carga y concurrencia, y detectó que el test buscaba las observaciones en el modal de workflow `Seguimiento de evaluación`. Se corrigió para leerlas desde el Bloque 6 del editor y exigir el GET real del snapshot antes de validar controles, evidencias y Área/UGR.

El run `38616285559` avanzó hasta las aserciones de Área/UGR y señaló una ambigüedad de selector (`getByLabel` encontraba el textarea y el botón Guardar). La prueba ahora selecciona expresamente el rol `textbox`; falta confirmar el siguiente CI completo.

El Quality Gates `38616972937` terminó `completed/success` en el SHA `6b0d23f4f7d1503a8fe891eea23964e14142371b`: Block7 Oracle write 1/1, browser real 1/1, Block9 mitigación/monitoreo/RBAC/evidencias Oracle y browser PASS sin rutas críticas mockeadas, backend 965/965, frontend 858/858, Playwright 49/49, Block6 Oracle 3/3 y Block8 Oracle 1/1. El artefacto de CI omitió la captura nombrada del uploader Matrix; el E2E ahora la escribe en la raíz de `test-results`, y se ejecutará otra vez antes de fijar el cierre definitivo.

El run `38618732332` confirmó success pero no retuvo ese archivo: la ejecución E2E general limpia `test-results` después de la suite Oracle. El test y `quality-gates.yml` pasaron a escribir/subir la captura desde `RUNNER_TEMP/block9-browser-evidence`. El run final `38625179428` incluyó la captura; inspección visual confirmó el control, el archivo vinculado y la dropzone institucional.

## Cierre verificado

- Mitigación: planes y actividades reales Oracle; 2 planes y 5 actividades proyectan `F41=2` y `F43=5`; `F40–F49=10/10`; conteos derivados sin autoridad cliente.
- Monitoreo: `F70–F82=13/13`, alertas, automonitoreo append-only, cuatro controles por tipo y evidencias con orden canónico, vínculo/desvínculo y protección de borrado enlazado.
- Acceso: módulo 10 requerido; escenarios Área-only, UGR-only, ambos y ninguno; escritura cruzada rechazada con HTTP 403. Observaciones Área/UGR conservan columnas y capacidades independientes.
- Calidad: backend `965/965`, frontend `858/858`, Playwright `49 passed`, Block6 Oracle `3/3`, Block8 Oracle `1/1`; contrato 82/82, auditoría Block9 `23/23`, checks `12/12`. Quality Gates final `38625179428` terminó `success`.
- Estándar de archivos: `InstitutionalFileUploadComponent` único, cinco superficies migradas, cero inputs file fuera del shared; Monitoreo de Listas es la referencia visual y Matrices también consume el mismo componente. La guía transversal está en [Estándar institucional de carga de archivos](../0.0%20Documentación/ESTANDAR_INSTITUCIONAL_CARGA_ARCHIVOS.md).
- Seguridad: producción no fue consultada ni modificada; DML=0, DDL=0, apply=NO, esquema sin cambios y bypass productivo=NO. `BLOCK1_STATUS`–`BLOCK9_STATUS=CLOSED`; `NEXT_BLOCK_STATUS=NOT_STARTED`.

## Refinamiento premium del tema modal — ejecución consolidada

La enmienda mantiene un solo shell institucional. `colorPrimario` genera el gradiente principal monocromático en tonos oscuro/medio/claro; `colorSecundario` genera su familia propia y sólo aporta highlight/foco cuando la transición es armónica. Los selectores del shell consumen tokens centrales; los valores de reserva viven en `:root`. No hay gradientes ni colores de marca por modal.

El header combina highlight radial, gradiente multicapa y geometría CSS con `pointer-events:none`. El footer usa una gradación más calmada de la familia primaria. El cuerpo sigue neutro; cierre, foco, acciones de marca e iconos heredan tokens. Las acciones interactivas de pie son icon-only con icono canónico, `aria-label` y `title`/tooltip; no presentan etiqueta visible permanente. La información de estado sigue visible. La geometría, scroll, formularios y handlers se mantienen.

Inventario actual: 22 modales, 22 con shell canónico, cero outliers y cero overrides locales. Playwright verificó cambio en vivo azul→rojo→claro→verde en el mismo modal, consistencia de geometría y 390×844 sin overflow. Crear Usuario comprobó etiqueta Guardar, estado disabled diferenciable y SVG del botón con 16 px, visible y opacidad propia 1. Matriz completa pasó el cambio azul/rojo. Contraste calculado con luminancia relativa: título, subtítulo y acciones ≥4.5:1; control de cierre ≥3:1 en los cuatro temas.

Validadores en Quality Gates: `validate_institutional_modal_standard.js`, `validate_dynamic_system_theme.js`, `validate_premium_modal_design.js` y `validate_modal_icon_contrast.js`. En esta corrida: shell 22/22, 286 usos del icono canónico auditados y cero cierres icon-only de pie.

Artefactos fuera de Git: `%USERPROFILE%\Documents\RIESGO_LAVADO\UI_MODAL_PREMIUM_STANDARD\20261014_173649\`. Incluye inventario global, auditoría de tema/gradiente/contraste/responsive, capturas azul/roja/clara y hashes. Verificación local de esta corrida: backend 972/972, frontend 877/877, lint/build/audit PASS, Playwright 53 passed y 1 Oracle XE omitido localmente. Quality Gates `38660281773` terminó `completed/success` para `e2f6c2ed49460bb9a508159ffab4de47d75af818`; Block7 Oracle/browser=1/1, mitigación/monitoreo/RBAC/evidencia Oracle PASS, rutas críticas mocked=0, Oracle XE Block6/Block8 PASS, contenedores PASS.

La suite funcional completa sigue certificada por el checkpoint CI anterior; consolidado mantiene todos los estados, los errores editoriales no contaminan la página y la edición histórica conserva claves legacy. No se reabren los Bloques 1–9 ni se inicia el siguiente bloque.

El commit documental final `208b8e2ca010ae9704b87b8cf99893e283b8c156` también pasó Quality Gates en el run `38661183895` (`completed/success`). La ejecución final repitió los validadores, E2E, Oracle XE de Bloques 6/8, pasos Oracle/browser de Bloques 7/9 y builds de contenedores.

Seguridad de esta corrida: producción no se consultó ni modificó; DML=0, DDL=0, apply=NO, sin cambio de esquema ni bypass de autenticación.
