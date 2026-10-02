# Cat?logos institucionales de Matrices de Riesgos

## Certificaci?n del Bloque 3

**Estado t?cnico: CERRADO.** El contrato can?nico contiene 14 cat?logos y 91 ?tems. Se validaron los consumidores backend, frontend, importaci?n y exportaci?n con el manifiesto versionado y pruebas. No se ejecut? DML, DDL, seed ni procedimiento en producci?n. No se inicia Bloque 4.

## Fuentes y m?todo

- Workbook institucional: `Matrices de Riesgos.xlsx`, SHA-256 `5c3fc00864947afe1e34d3d6ffdfc6da008eaa3c8f1c6c764161014d5ef9a385`.
- Hojas inspeccionadas: `Matriz Consolidada`, `Listas`, `Otras Tablas`, `Listas Automonitoreo` (oculta) e `Instructivo`. Se inspeccionaron tablas, f?rmulas y validaciones. No hay named ranges definidos; Campo 39 carece de Data Validation.
- Contrato: [matriz_riesgos_catalogos_manifest.json](../../backend/RL.API/Features/MatricesRiesgos/Contracts/matriz_riesgos_catalogos_manifest.json). Contiene 14 cat?logos/91 ?tems. Backend carga el manifiesto embebido; el validator compara los datos generados del frontend con cada ?tem del manifest.
- Producci?n se consult? en transacci?n Oracle READ ONLY y termin? con ROLLBACK. Identidad: HPPROD1 / hpprod1 / hpprod1 / RIESGO_LAVADO / RIESGO_LAVADO. DML=0, DDL=0, procedimientos=0, escrituras=0.

Cat?logos congelados: `RISK_TYPE`, `RISK_RESPONSE`, `RISK_LEVEL`, `FREQUENCY`, `IMPACT`, `AREA`, `REGIME`, `CONTROL_TYPE`, `CONTROL_WEIGHT`, `CONTROL_EFFECTIVENESS`, `CONTROL_AUTOMATION`, `MONITORING_RISK_STATUS`, `MONITORING_CONTROL_STATUS`, `MONITORING_EFFECTIVENESS`.

## Nivel de riesgo

La fuente workbook `Listas!A16:B25` determina los labels exactos:

| Valor | Etiqueta |
|---:|---|
| 1 | Riesgo no significativo |
| 2 | Riesgo no significativo |
| 3 | Riesgo bajo |
| 4 | Riesgo bajo |
| 5 | Riesgo Medio |
| 6 | Riesgo Alto |
| 7 | Riesgo Alto |
| 8 | Riesgo Intolerable |
| 9 | Riesgo Intolerable |

Producci?n tiene cuatro bandas hist?ricas (`BAJO`, `MODERADO`, `ALTO`, `CRITICO`) en `MR_NIVEL_RIESGO`. Los n?meros VRI/VRR 1..9 se conservan en las proyecciones. Por eso la salida se deriva del n?mero conservado y no se altera el cat?logo ni los datos de producci?n. Backend runtime/proyecci?n, UI y PDF/XLSX usan el lookup can?nico. ROP-CUMP-59: VRI=3 ? `Riesgo bajo`; VRR=1 ? `Riesgo no significativo`.

## Respuesta al riesgo

Pol?tica de autoridad: (1) valores observados en Matriz Consolidada; (2) tabla `t_resp_riesgo` de Listas; (3) claves del seed/runtime; (4) texto narrativo del Instructivo. Campo 39 no tiene Data Validation, pero Matriz Consolidada, Listas y seeds sostienen una correspondencia inequ?voca de claves. `MITIGAR` presenta `Mitigar` y acepta alias exacto `Reducir`; `TRANSFERIR` presenta `Transferir/Compartir` y acepta alias exacto `Transferir`. `EVITAR` y `ACEPTAR` conservan su etiqueta.

`ROP-CUMP-50`, `ROP-CUMP-53`, `ROP-CUMP-54` son equivalentes por alias. `RCUMP-COMPRAS-37` tiene Excel vac?o y `MITIGAR` en la proyecci?n y JSON de la evaluaci?n 65/version 61; la clave est? activa y el audit seleccion? la evaluaci?n aprobada. Se clasifica `EXCEL_BASELINE_MISSING_BUT_DB_VALUE_VALID`, acci?n `PRESERVE_PRODUCTION`. No se modifica el registro. Resultado: 3 alias equivalentes, 0 conflictos de datos reales en Campo 39.

## ?rea, frecuencia, impacto y r?gimen

Las 59 filas muestran 20 ?reas. La ?nica relaci?n observada de ?rea ? ?rea Consolidada es `Secci?n de Cumplimiento ? Secci?n de Cumplimiento` (11 filas); hay 0 relaciones ambiguas y 19 celdas vac?as. Campo 04 es opcional seg?n el manifest: los vac?os son `NO_SOURCE_VALUE`; no se inventaron mappings.

Workbook define frecuencia e impacto como dominios num?ricos 1..5 sin labels descriptivos. Las etiquetas adicionales de DB se clasifican `DISPLAY_METADATA_ONLY`; no cambian la clave num?rica y no se usan como etiquetas institucionales del workbook.

R?gimen tiene siete claves workbook: `IVM`, `RP`, `EM`, `IVM-RP`, `IVM-EM`, `RP-EM`, `Todos`. Se almacena como string `EVA_DATOS_JSON.regimen_afectado`; la instant?nea de proyecciones aprobadas no mostr? valores actuales contradictorios. No se crean opciones productivas.

## Controles, automatizaci?n y monitoreo

- Tipos: PREVENTIVO/Preventivo, DETECTIVO/Detectivo, CORRECTIVO/Correctivo.
- Pesos de `Otras Tablas`: Preventivo 0.70, Detectivo 0.15, Correctivo 0.15; suma 1.00.
- Escala ?nica (`Listas!t_efectividad`): Inexistente 0/0%; Inefectivo 1/0%; Razonable 2/30%; Parcialmente Efectivo 3/50%; Moderado 4/85%; Alta Efectividad 5/90%. `Es inefectivo` es alias exacto de `Inefectivo`. Inexistente describe ausencia de controles; no crea un control f?sico.
- Automatizaci?n: Automatizado, Semiautomatizado, Manual. `Semi-Automatizado` es alias expl?cito de `Semiautomatizado`. Se conservan claves t?cnicas `AUTOMATICO`, `SEMIAUTOMATICO`, `MANUAL`.
- Monitoreo de riesgo: Vigente, Mitigado, Nuevo. Monitoreo de controles: Se mantiene, No se mantiene, Requiere actualizaci?n. Efectividad: Inexistente, Inefectivo, Razonable, Parcialmente Efectivo, Moderado, Alta Efectividad. Las pantallas usan selects del manifest y backend rechaza valores desconocidos. `Vigente` no es default; controles vac?os representan ciclo pendiente. Porcentaje en blanco significa sin evaluaci?n.

## Paridad de consumidores

Backend valida claves/aliases exactos, normaliza respuestas y automatizaci?n, proyecta estados de monitoreo can?nicos y devuelve exports PDF/XLSX con label de riesgo derivado de VRI/VRR y respuesta por clave. Frontend consume datos generados desde el manifest, muestra labels can?nicos, usa claves persistentes y opciones cerradas para control/monitoreo. Importaci?n futura acepta s?lo claves o aliases registrados; unknown ? `FAIL_CLOSED`. No hay fuzzy matching ni normalizaci?n general de may?sculas, acentos o puntuaci?n.

El validator determinista est? en [validate_matrices_catalogs.js](../../tools/validate_matrices_catalogs.js). Las pruebas cubren niveles 1..9, ROP-CUMP-59, aliases, opciones y unknown fail-closed.

## Producci?n y Bloque 2 handoff

Las cuatro bandas se registran como `LEGACY_COARSE_BAND`; no requieren migraci?n para la paridad de salida, porque VRI/VRR num?rico se conserva y los consumidores proyectan el label can?nico. No se prepar? ni ejecut? DML. `BASELINE_IMPORT_CANDIDATES=398` sigue congelado; `DATA_CONFLICTS=87` no se altera; `OPERATIONAL_VALUES_TO_PRESERVE=4` quedan protegidos.

Artefactos forenses locales sanitizados: `%TEMP%\RIESGO_LAVADO_BLOCK3_CATALOG_AUDIT_20261002_1548_785327\`; hashes en `SHA256SUMS.json`. No se versionan dumps productivos.

## Verificaciones

- Contrato workbook: 82/82 y SHA esperado.
- Cat?logos: 14/14, 91 ?tems; unknown/unresolved=0.
- Backend focal: 20/20; build API PASS.
- Frontend TypeScript, lint y Angular build PASS; suite integral final: 84 archivos/845 pruebas PASS. Tres expectativas hist?ricas de labels fueron actualizadas a las etiquetas can?nicas.
- Validadores estructura, scripts de base de datos y enlaces de documentaci?n: PASS.
- Producci?n: DML=0, DDL=0, procedimientos=0, escrituras=0.
