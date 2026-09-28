# Matriz institucional completa — Bloque 1

## Estado de implementación

```text
MATRIX_82_ALIGNMENT_STARTED=YES
BLOCK_1_IMPLEMENTED=YES
FIELDS_01_19_VISIBLE=19/19
ORDER_01_19=PASS
EXACT_EXCEL_LABELS_01_19=PASS
F01=PASS
F02=PASS
GTIC_FIELDS_PRESERVED=YES
GTIC_ACTIVATION_CRITERION=MISSING_CONTROLLED_SOURCE
BLOCKS_2_6_IMPLEMENTED=NO
FULL_MATRIX_82=PENDING_BLOCKS_2_6
FORM_VERSION_PUBLICATION=NOT_EXECUTED
DRAFT_STRATEGY=NO_ORACLE_DRAFT_CHANGE_REQUIRED
```

La pestaña «Matriz completa» es una vista de consulta independiente de los flujos existentes de crear, editar, ver, seguimiento, consolidado y plantillas. Las definiciones de los 82 ordinales viven en `frontend/rl-app/src/app/features/admin/matrices-riesgos/models/matriz-institucional.contract.ts`; solo los campos 01–19 tienen comportamiento implementado. La metadata de bloques 2–6 reserva posiciones y etiquetas para evolución posterior, sin habilitar esos campos.

Los ordinales representan posición de presentación y nunca identifican una evaluación o riesgo. Para evaluaciones históricas V1, las propiedades no declaradas se muestran como «No disponible en esta versión», sin backfill ni valores supuestos. Los campos 17–19 permanecen visibles; no existe una fuente estructurada controlada que active su aplicabilidad GTIC, por lo que no se ocultan ni se autocompletan con «No aplica».

## Trazabilidad — Bloque 1, campos 01–19

| # | Campo Excel (etiqueta visible exacta) | Clave técnica | Origen | Modo | Persistencia |
|---:|---|---|---|---|---|
| 01 | No. | — | Posición del listado actual | PRESENTATION ONLY | No se persiste; no es `EVA_ID` ni `RIE_ID` |
| 02 | Código de Riesgo | — | Riesgo relacionado (`RIE_CODIGO`) | MASTER | Maestro de riesgos; no se duplica en JSON |
| 03 | Área | `area_principal` | Respuesta JSON / proyección `PROY_AREA_PRINCIPAL` | INPUT | `EVA_DATOS_JSON` versionado |
| 04 | Área Consolidada | `area_consolidada` | Respuesta JSON si la versión la declara | INPUT | `EVA_DATOS_JSON`; ausente en histórico V1 |
| 05 | Tipo de Riesgo | `tipo_riesgo` | Respuesta JSON si la versión la declara | INPUT | `EVA_DATOS_JSON`; no inferido en V1 |
| 06 | Procedimiento | `procedimiento` | Respuesta JSON si la versión la declara | INPUT | `EVA_DATOS_JSON` versionado |
| 07 | Objetivo(s) Estratégico(s) | `objetivos_estrategicos` | Respuesta JSON si la versión la declara | INPUT | `EVA_DATOS_JSON` versionado |
| 08 | Riesgo Inherente | — | Riesgo relacionado (`RIE_NOMBRE`) | MASTER | Maestro de riesgos; no se duplica en JSON |
| 09 | Evaluación | — | Riesgo relacionado (`RIE_DESCRIPCION`) | MASTER | Maestro de riesgos; no se duplica en JSON |
| 10 | Frecuencia | `frecuencia_inherente` | Respuesta JSON / catálogo `MR_FRECUENCIA_1_5` | INPUT | `EVA_DATOS_JSON` versionado |
| 11 | Impacto | `impacto_inherente` | Respuesta JSON / catálogo `MR_IMPACTO_1_5` | INPUT | `EVA_DATOS_JSON` versionado |
| 12 | Valor del Riesgo Inherente | — | Cálculo oficial `F01_VALOR_RIESGO_INHERENTE` / `EVA_VRI` | COMPUTED | Cálculo y proyección existentes; solo lectura |
| 13 | Nivel de Riesgo Inherente | — | Resultado `F02_NIVEL_RIESGO_INHERENTE` del runtime/catálogo | COMPUTED | JSON de cálculos/proyección existentes; solo lectura |
| 14 | Responsable o dueño del riesgo | `dueno_riesgo` | Respuesta JSON / proyección `PROY_DUENO_RIESGO` | INPUT | `EVA_DATOS_JSON` versionado |
| 15 | Régimen afectado | `regimen_afectado` | Respuesta JSON si la versión lo declara | INPUT | `EVA_DATOS_JSON`; ausente en histórico V1 |
| 16 | Transversalidad o Interrelación con otros Riesgos | `transversalidad` | Respuesta JSON si la versión lo declara | INPUT | `EVA_DATOS_JSON`; ausente en histórico V1 |
| 17 | Amenazas (Solo para riesgos de GTIC) | `amenazas_gtic` | Respuesta JSON si la versión lo declara | INPUT | `EVA_DATOS_JSON`; sin criterio de aplicabilidad controlado |
| 18 | Vulnerabilidades (Solo para riesgos de GTIC) | `vulnerabilidades_gtic` | Respuesta JSON si la versión lo declara | INPUT | `EVA_DATOS_JSON`; sin criterio de aplicabilidad controlado |
| 19 | Activos de Información (Solo para riesgos de GTIC) | `activos_informacion_gtic` | Respuesta JSON si la versión lo declara | INPUT | `EVA_DATOS_JSON`; sin criterio de aplicabilidad controlado |

Los campos 12 y 13 se presentan desde resultados del servidor/runtime. El frontend no implementa fórmulas alternativas. El contrato V1 y los datos históricos permanecen inmutables. El V2 DRAFT no se modificó: esta entrega es de consulta, conserva los flujos de edición existentes y no publica ni activa una versión de formulario. Las futuras escrituras de claves nuevas requieren una versión que las declare.

## Evidencia de esta intervención

- Frontend unit tests: 796/796 PASS; lint PASS; build PASS.
- Backend tests: 657/657 PASS (sin conexión Oracle institucional).
- E2E completo: 37/37 PASS, incluida apertura desde Evaluaciones, orden y etiquetas 01–19, solo lectura 12–13, campos 17–19, estado histórico sin dato y retorno de navegación.
- Validadores de scripts de base de datos, encoding, estructura y enlaces: PASS.
- `git diff --check`: PASS (volver a ejecutar tras el handoff final).
- `PRODUCTION_DEPLOYED=FALSE`, `INSTITUTIONAL_TRAINING_EXECUTED=FALSE`; Issue #22 permanece abierto.
- No se ejecutaron scripts Oracle ni se preparó sincronización DRAFT: `ORACLE_DDL_REQUIRED=NO`, `ORACLE_DML_SCRIPT_PREPARED=NO`.

## Continuidad

Siguiente alcance autorizado por el plan: Bloque 2 — Controles, campos 20–33. No iniciar ese bloque como parte de esta intervención.
