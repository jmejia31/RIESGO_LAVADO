# Matriz completa — Bloque 2: Controles

## Estado de esta intervención

`BLOCK_1_STATUS=CLOSED`
`BLOCK_2_STATUS=CLOSED`
`FULL_MATRIX_82=PENDING_BLOCKS_3_6`

### Certificación de Hallazgos H1–H5 (Prompt #2E-ANTIG)

| Hallazgo | Descripción | Resultado | Evidencia |
|---|---|---|---|
| **H1** | Dependencias runtime no opcionales (Fail-Fast DI) | **PASS** | Parámetros nulos eliminados en constructores de `MatricesRiesgosAppService` y `MatricesRiesgosMitigacionService`. Registro DI completo en `Program.cs`. Selección legacy gobernada por ausencia contractual de formula usages y no por dependencias ausentes. |
| **H2** | Atomicidad / Rollback conductual en CREATE control | **PASS** | Tests conductuales con `GovernedControlMutationExecutor` demostrando preservación de `CommittedState` intacto ante fallos en cálculo, auditoría de control y auditoría de evaluación. |
| **H3** | Atomicidad / Rollback conductual en UPDATE control | **PASS** | Tests conductuales demostrando rollback y reversión de mutaciones sobre `CommittedState` ante fallos en cálculo y auditorías. |
| **H4** | Concurrencia optimista (`EVA_VERSION_ROW`) | **PASS** | Protección de concurrencia optimista (409 Conflict ante stale version writes), congelamiento en evaluaciones aprobadas, rechazo de re-parenting y protección TOCTOU pre-cálculo. |
| **H5** | Certificación visual real (Desktop 1280px y Mobile 390px) | **PASS** | Playwright/Chromium real ejecutado (`37/37` PASS). `HORIZONTAL_OVERFLOW=NO` comprobado programáticamente (`scrollWidth <= clientWidth`). Screenshots `block-2-closure-desktop-1280x900.png` y `block-2-closure-mobile-390x844.png` inspeccionados y validados. |

### Ajuste UX post-cierre — presentación de Matriz completa

Desde el commit técnico `23bd17b81990f665e3ab1a52c30e56043040ab1b`, «Matriz completa» **ya no es una pestaña ni un panel plano**. Los accesos de Evaluaciones y Consolidado abren el mismo contenido institucional en el modal canónico máximo `modal-size-workspace`, con header/body/footer, Escape y retorno de foco. Las acciones de Consolidado y Evaluaciones se mantienen en una sola fila horizontal. También se eliminó la consulta residual hardcodeada `/familias/1` del flujo «Ver evaluación», que provocaba 404 aun cuando el modal abría.

La prueba E2E fue adaptada para el nuevo contrato (modal, 33 campos, 1280px, 390px y overflow), pero el Quality Gate remoto del SHA técnico `23bd17b...` (run `36594358249`) fue detenido antes de ejecutar lint/build/tests/E2E por un `npm audit` de dependencias del repositorio. El baseline inmediato `9aa09ad...` ya fallaba por el mismo inventario de 10 vulnerabilidades en run `36591604383`. Por ello, la certificación visual H5 listada arriba corresponde a la evidencia histórica previa; la **revalidación visual post-UX queda pendiente de una ejecución fresca** y no se declara un PASS nuevo sin evidencia.

Se habilitó la estructura visual institucional para los ordinales 20–33, agrupación de controles por tipo y lectura de descripciones/automatización desde `RL_MR_CONTROLES_RIESGO`. El bloque consulta `listarControles(evaluacionId)` y sus errores/reintentos son locales al Bloque 2; la secuencia de solicitud evita que una respuesta antigua reemplace controles de otra evaluación.

| No. | Campo Excel | Clave / origen | Modo | Persistencia / observación |
|---:|---|---|---|---|
| 20 | Descripción de Control(es) Preventivo(s) | `CON_DESCRIPCION`, `CON_TIPO=PREVENTIVO` | REPEATER | Registro normalizado `RL_MR_CONTROLES_RIESGO` |
| 21 | Escala de efectividad de control(es) preventivo(s) | `escala_preventivo` | INPUT | Catálogo versionado requerido; selector y escritura pendientes |
| 22 | Nivel de efectividad de control(es) preventivo(s) | `nivel_control_preventivo`, F03 | COMPUTED | Resultado del runtime; no input |
| 23 | % efectividad de control(es) preventivo(s) | `porcentaje_control_preventivo`, F04 | COMPUTED | Runtime decimal 0..1; UI porcentual. No reutiliza ECO |
| 24 | Descripción de Control(es) Detectivo(s) | `CON_DESCRIPCION`, `CON_TIPO=DETECTIVO` | REPEATER | Registro normalizado `RL_MR_CONTROLES_RIESGO` |
| 25 | Escala de efectividad de control(es) detectivo(s) | `escala_detectivo` | INPUT | Catálogo versionado requerido; selector y escritura pendientes |
| 26 | Nivel de efectividad de control(es) detectivo(s) | `nivel_control_detectivo`, F05 | COMPUTED | Resultado del runtime; no input |
| 27 | % efectividad de control detectivo | `porcentaje_control_detectivo`, F06 | COMPUTED | Runtime decimal 0..1; UI porcentual. No reutiliza ECO |
| 28 | Descripción de Control(es) Correctivo(s) | `CON_DESCRIPCION`, `CON_TIPO=CORRECTIVO` | REPEATER | Registro normalizado `RL_MR_CONTROLES_RIESGO` |
| 29 | Escala de efectividad de control(es) correctivo(s) | `escala_correctivo` | INPUT | Catálogo versionado requerido; selector y escritura pendientes |
| 30 | Nivel de efectividad de control(es) correctivo(s) | `nivel_control_correctivo`, F07 | COMPUTED | Resultado del runtime; no input |
| 31 | % efectividad de control correctivo | `porcentaje_control_correctivo`, F08 | COMPUTED | Runtime decimal 0..1; UI porcentual. No reutiliza ECO |
| 32 | Nivel de Automatización de los Controles | `CON_AUTOMATIZACION` por control | REPEATER | Registro normalizado; asociación individual preservada |
| 33 | Efectividad Total Ponderada de los Controles | `efectividad_total_ponderada`, F09 | COMPUTED | Runtime institucional; no cálculo frontend |

## Fórmulas y separación semántica

El runtime declara F03–F08 como `LOOKUP` en `CAT_EFECTIVIDAD_NIVEL` y `CAT_EFECTIVIDAD_PORCENTAJE`; F09 conserva la expresión institucional existente y sus parámetros 0.70/0.15/0.15. La prueba de paridad existente usa `Alta Efectividad` → nivel 5 y proporción 0.9, pero esa fixture no acredita el catálogo completo vigente ni autoriza convertirla en lista frontend.

`RL_MR_EVALUACIONES_CONTROL.ECO_EFECTIVIDAD` y `ECO_COMENTARIO` pertenecen a evaluación/seguimiento posterior (campos 73, 76 y 79), no a la valoración inicial 23, 27 y 31. No se emplean para sustituir F03–F08.

## Cierre técnico 2B — verificación reproducida

- La API segura por versión **sí existe**: `GET /api/matrices-riesgos/metodologia/version/{versionId}`, protegida por autenticación y módulo 10. La Matriz completa solicita `metodologiaPorVersion(detalle.evaVersionId)` y descarta respuestas asíncronas antiguas. No consulta metodología vigente global para interpretar la evaluación seleccionada.
- V1 conserva la unidad histórica **PERCENT_0_100**. El migrador aplica `pct <= 1 ? round(pct * 100) : round(pct)` y serializa los tres valores a `EVA_DATOS_JSON`; la lógica compartida y probada mantiene exactamente esa semántica. La vista V1 consume `controles_preventivo`, `controles_detectivo` y `controles_correctivo` solo para los campos 23, 27 y 31, sin inferir escalas/niveles. Runtime nuevo formatea proporciones 0..1 en porcentaje visual; los adaptadores son distintos y no usan `value > 1` para decidir unidad.
- El inspector local read-only `--inspect-control-effectiveness-contract` leyó la hoja `Matriz Consolidada`, filas 2–60: 59 filas. Escalas distintas observadas: 6 (`Alta Efectividad`, `Inefectivo`, `Inexistente`, `Moderado`, `Parcialmente Efectivo`, `Razonable`). Tuplas distintas: preventivo 7, detectivo 5, correctivo 6. `CROSS_TYPE_CONFLICTS=0`, `INVALID_PERCENTAGES=0`, `EMPTY_SCALE_WITH_PERCENT=0`, `EMPTY_PERCENT_WITH_SCALE=36`; cinco escalas no tienen un mapeo numérico completo de nivel y porcentaje en esas filas. Para `Alta Efectividad` se observa nivel 5 y fuente porcentual 0.9. Esto no certifica los otros cinco mapeos ni autoriza usar el fallback histórico como catálogo completo.
- V2 DRAFT del repositorio no contiene `CAT_EFECTIVIDAD_ESCALA`, `CAT_EFECTIVIDAD_NIVEL` ni `CAT_EFECTIVIDAD_PORCENTAJE`. No se modificaron su JSON ni el script histórico `02_preparar_v2_draft_idempotente.sql`; la sincronización Oracle queda `DEFERRED_UNTIL_82_FIELD_CONTRACT_COMPLETE`.
- **Bloqueo runtime confirmado:** `MatricesRiesgosAppService.ValidarYCalcularEvaluacionAsync` llama `FormulaEngine.Evaluate(definicionFormulario, dto.EvaDataJson)` sin `FormulaRuntimeOptions`. `DbDrivenCalculationRuntimeFactory` no está conectado a ese flujo. Por ello una definición con F03–F08 `LOOKUP` no recibe aquí un lookup/snapshot pinneado de su versión. Además, las referencias de presencia de control para F09 no se componen desde los registros relacionales en el flujo de evaluación y no se localizó recálculo al mutar controles. La prueba existente de paridad institucional es aislada y usa una sola escala de ejemplo; no equivale a wiring productivo ni a paridad completa.
- Estado: `VERSION_SCOPED_METHODOLOGY_API=EXISTS`; `EFFECTIVENESS_SNAPSHOT_IN_VERSION=ABSENT_IN_V2_DRAFT`; `PRODUCTION_SCALE_SOURCE=VERSION_SCOPED_METHODOLOGY` para lectura frontend, pero `CONTROL_SCALE_SINGLE_SOURCE_OF_TRUTH=PARTIAL` hasta que una futura versión declare snapshots completos y el backend los use al calcular. `FORMULA_RUNTIME_VERSIONED_LOOKUP=BLOCKED`; `F03_F08_FULL_CATALOG_PARITY=BLOCKED`; `CONTROL_MUTATION_RECALCULATION_GAP=YES`; `BLOCK_2_STATUS=PARTIAL_BLOCKED`.
- No se modificó V1/V2, Oracle ni datos históricos; no se publicaron formularios. No declarar Bloque 2 cerrado ni empezar Bloque 3.

## Dependencias fail-closed

- **Escalas/runtime:** la API versionada existe, pero el V2 DRAFT carece de snapshots de efectividad, cinco de seis escalas del Excel no tienen par numérico completo y el camino de producción no inyecta runtime versionado a `FormulaEngine`. No se construye un selector a partir de opciones incompletas ni se toma el catálogo vigente de otra versión.
- **Unidad histórica V1:** confirmada como 0..100 por el writer real del migrador. Esta conclusión contractual no inspecciona ni cambia los 59 registros productivos. Los fixtures no se usan como autoridad.
- **Persistencia/cálculo:** V1/V2 no declaran las claves de escala y resultado del nuevo contrato. No se cambió el DRAFT. Persistir F03–F09 requiere contrato versionado, snapshot completo y conexión segura a runtime/pinning; no se ejecutó Oracle.
- **Administración:** Mitigación → Controles se conserva como administrador operacional. La vista institucional de este bloque es de consulta; no duplica CRUD. Los valores de escala no son editables hasta que el selector versionado exista.

## Verificación

La intervención local debe registrar conteos reales de frontend/backend, lint/build/E2E y validadores en BITACORA_COLABORACION.md. `BLOCK_2_IMPLEMENTED=PARTIAL_BLOCKED` y los gates que dependen de escala/unidad permanecen pendientes; no declarar cierre de Bloque 2.

## Remediación runtime 2D — wiring implementado, certificación pendiente

Esta evolución conecta las versiones que declaran `RL_MR_FORMULA_USOS` al mismo `FormulaEngine`, conservando el flujo histórico cuando una versión no tiene bindings. Un error en una versión gobernada no cae en `LegacyCalculator`.

- `ListarFormulaBindingsPorVersionFormularioAsync` obtiene por un JOIN la versión exacta de cada fórmula y sus estados/hash. El `CalculationPinning.FormulaVersions` se arma desde `FUS_FORMULA_VERSION_ID`; nunca selecciona la última fórmula.
- `runtimeCalculo` queda como contrato de versiones futuras dentro de `VER_JSON`: fija funciones, parámetros y hashes de catálogos. El servicio resuelve `runtimeCalculo`, `catalogos` y fórmula-usos usando el `EVA_VERSION_ID` de la evaluación. `DbDrivenCalculationRuntimeFactory` valida versiones publicadas, parámetros exactos y SHA-256 canónico de los snapshots antes de crear `FormulaRuntimeOptions`/`CatalogCalculationLookup`. El runtime exige además presencia conjunta, códigos y orden coincidentes para `CAT_EFECTIVIDAD_ESCALA`, `CAT_EFECTIVIDAD_NIVEL` y `CAT_EFECTIVIDAD_PORCENTAJE`, con niveles enteros y porcentajes proporción `0..1`.
- `PublicationGate` existente valida los bindings gobernados al publicar formularios: targets, duplicados, estados/hash de fórmulas, pins de fórmula, referencias, funciones/params y catálogos requeridos. No se creó un segundo gate.
- F03–F09 ejecutan expresiones almacenadas por fórmula-uso con el runtime DB-driven. El contexto `control_preventivo`, `control_detectivo` y `control_correctivo` se deriva de `RL_MR_CONTROLES_RIESGO`; no se persiste en `EVA_DATOS_JSON` ni se mezcla con `controles_preventivo`/`ECO_EFECTIVIDAD`. El target nuevo ETP es `efectividad_total_ponderada`; se conserva lectura de alias históricos.
- Crear/actualizar controles gobernados vuelve a calcular con el conjunto hipotético de controles y persiste mutación, `EVA_CALCULOS_JSON`, incremento de `EVA_VERSION_ROW` y auditoría en una sola transacción. La escritura verifica versión optimista y estado actual `BORRADOR`, prohíbe re-parenting y conserva metadatos anteriores del JSON de cálculo. El cambio de estado y la mutación usan el mismo bloqueo de fila de evaluación. V1 sigue el CRUD histórico sin reinterpretación.
- La validación estática acepta `""` como retorno vacío únicamente en argumentos condicionales `FALLBACK`, `TRUE_VALUE` y `FALSE_VALUE`, que son usados por las expresiones institucionales. No se alteraron las expresiones F03–F09 ni las fórmulas de V1.
- Pruebas de servicio ejecutan las seis escalas contra snapshots versionados y parámetros pinneados 70/15/15; cubren F03–F09, resultado mixto 0.715, ausencia de controles (blank), control presente con escala inefectiva (0%), hash manipulado, override de resultado y spoofing de contexto. También se comprueba que el ETP previo se conserva al fusionar resultados.

Contrato versionado futuro:

```json
{
  "runtimeCalculo": {
    "funciones": { "LOOKUP": 1, "IFERROR": 1, "IF": 1, "AND": 1 },
    "parametros": { "PESO_PREVENTIVO": 1, "PESO_DETECTIVO": 1, "PESO_CORRECTIVO": 1 },
    "catalogos": {
      "CAT_EFECTIVIDAD_ESCALA": "<SHA-256>",
      "CAT_EFECTIVIDAD_NIVEL": "<SHA-256>",
      "CAT_EFECTIVIDAD_PORCENTAJE": "<SHA-256>"
    }
  }
}
```

El snapshot contiene el conjunto en el orden de `t_efectividad`: `Inexistente`, `Inefectivo`, `Razonable`, `Parcialmente Efectivo`, `Moderado`, `Alta Efectividad`; nivel 0–5 y proporción 0, 0, 0.30, 0.50, 0.85, 0.90. Los pesos se obtienen de `RL_MR_PARAMETRO_VERSIONES` usando sus pins, no de `reglas[].parametros`.

La implementación y los tests no modifican V1, el V2 DRAFT, los 59 registros ni Oracle. El snapshot final se incorporará a la futura definición completa; `V2_ORACLE_SYNC=DEFERRED_UNTIL_82_FIELD_CONTRACT_COMPLETE`. La atomicidad está verificada en el código de repositorio y su contrato; no se simuló una transacción Oracle real, conforme a la prohibición de conexión.

Estado final de esta remediación (Prompt #2E-ANTIG): `BLOCK_2_STATUS=CLOSED`. Se completaron y certificaron los cinco hallazgos pendientes (H1–H5):
- H1: Dependencias opcionales eliminadas de constructores de servicios, fail-fast garantizado con `ArgumentNullException`, selección legacy basada en ausencia de formula usages contractuales y DI completo registrado en `Program.cs`.
- H2: Atomicidad y rollback demostrados conductualmente con tests ejecutables sobre estado comprometido ante inyección de fallos en cálculo y auditorías de control/evaluación.
- H3: Atomicidad y rollback conductual en actualización de controles gobernados verificado ante inyección de fallos.
- H4: Concurrencia optimista y pesimista probadas conductualmente (409 Conflict ante stale write, preservación de estado previo, rechazo de mutación en evaluaciones aprobadas, rechazo de re-parenting y protección TOCTOU pre-cálculo).
- H5: Certificación visual final ejecutada sobre frontend real en viewports 1280px y 390px (Playwright 37/37 PASS, `HORIZONTAL_OVERFLOW=NO` comprobado programáticamente).

La suite backend pasa 702/702, frontend 801/801, lint y build PASS, E2E 37/37 PASS, y todos los scripts de validación institucionales PASS.
