# Matriz completa — Bloque 2: Controles

## Estado de esta intervención

`BLOCK_1_STATUS=CLOSED`
`BLOCK_2_IMPLEMENTED=PARTIAL_BLOCKED`
`FULL_MATRIX_82=PENDING_BLOCKS_3_6`

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

## Dependencias fail-closed

- **Escalas:** el runtime ejecuta LOOKUP sobre snapshots fijados por versión. La API de catálogos existente consulta el catálogo maestro mutable y no demuestra que exponga el mismo snapshot pinneado usado por LOOKUP. El contrato vigente no ofrece una lectura segura de esas entradas por versión. No se inventó un selector ni se actualizaron JSON V1/V2.
- **Unidad histórica V1:** los fixtures automatizados del repositorio incluyen valores como `0.25` y `70` para las mismas claves heredadas. No son evidencia de los 59 registros históricos reales ni permiten confirmar si la unidad es 0..1 o 0..100. Por ello la vista no convierte ni presenta esos valores como porcentaje; no se infieren escalas.
- **Persistencia/cálculo:** V1/V2 no declaran las claves de escala y resultado del nuevo contrato. No se cambió el DRAFT. Persistir F03–F09 en una versión futura requiere agregar y aprobar el contrato versionado/snapshot correspondiente; no se ejecutó Oracle.
- **Administración:** Mitigación → Controles se conserva como administrador operacional. La vista institucional de este bloque es de consulta; no duplica CRUD. Los valores de escala no son editables hasta que el selector versionado exista.

## Verificación

La intervención local debe registrar conteos reales de frontend/backend, lint/build/E2E y validadores en BITACORA_COLABORACION.md. `BLOCK_2_IMPLEMENTED=PARTIAL_BLOCKED` y los gates que dependen de escala/unidad permanecen pendientes; no declarar cierre de Bloque 2.
