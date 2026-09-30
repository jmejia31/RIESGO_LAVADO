# Matriz completa — Bloque 5 (campos 50–69)

## Alcance y contrato

El Bloque 5 presenta los veinte campos institucionales de cálculos auxiliares y verificaciones. Los campos 50–69 son de solo lectura y su valor proviene exclusivamente de `evaDataCalcJson` recibido del servidor. El navegador no ejecuta F15–F34 para esta proyección ni usa respuestas crudas como fallback. Si el valor calculado falta, se presenta el placeholder institucional `—`; un dato ausente nunca se interpreta como una verificación aprobada.

El bloque está siempre disponible en Matriz completa y comienza colapsado. El control accesible **Mostrar detalle** expande los campos y cambia a **Ocultar detalle**; al abrir una nueva instancia de Matriz completa vuelve al estado inicial. El resumen visible es **20 campos · Calculados automáticamente**. Bloque 6 permanece pendiente.

| Campo | Etiqueta literal | Fórmula | Código institucional | Celda fuente | TargetField |
|---:|---|---:|---|---|---|
| 50 | Frecuencia Residual (AUX) | 15 | `F15_FRECUENCIA_RESIDUAL_AUX` | `Matriz Consolidada!AX2` | `frecuencia_residual_aux` |
| 51 | Impacto Residual (AUX) | 16 | `F16_IMPACTO_RESIDUAL_AUX` | `Matriz Consolidada!AY2` | `impacto_residual_aux` |
| 52 | Suma Residual redondeada (AUX) | 17 | `F17_SUMA_RESIDUAL_REDONDEADA_AUX` | `Matriz Consolidada!AZ2` | `suma_residual_redondeada_aux` |
| 53 | F_base (AUX) | 18 | `F18_F_BASE_AUX` | `Matriz Consolidada!BA2` | `f_base` |
| 54 | I_base (AUX) | 19 | `F19_I_BASE_AUX` | `Matriz Consolidada!BB2` | `i_base` |
| 55 | Tope F (AUX) | 20 | `F20_TOPE_F_AUX` | `Matriz Consolidada!BC2` | `tope_f` |
| 56 | Tope I (AUX) | 21 | `F21_TOPE_I_AUX` | `Matriz Consolidada!BD2` | `tope_i` |
| 57 | Capacidad F (AUX) | 22 | `F22_CAPACIDAD_F_AUX` | `Matriz Consolidada!BE2` | `capacidad_f_aux` |
| 58 | Capacidad I (AUX) | 23 | `F23_CAPACIDAD_I_AUX` | `Matriz Consolidada!BF2` | `capacidad_i_aux` |
| 59 | Resto (AUX) | 24 | `F24_RESTO_AUX` | `Matriz Consolidada!BG2` | `resto_aux` |
| 60 | Prefiere I (AUX) | 25 | `F25_PREFIERE_I_AUX` | `Matriz Consolidada!BH2` | `prefiere_i_aux` |
| 61 | Inc_I (AUX) | 26 | `F26_INCREMENTO_I_AUX` | `Matriz Consolidada!BI2` | `incremento_i_aux` |
| 62 | Inc_F (AUX) | 27 | `F27_INCREMENTO_F_AUX` | `Matriz Consolidada!BJ2` | `incremento_f_aux` |
| 63 | Valor del Riesgo Residual (AUX) | 28 | `F28_VALOR_RIESGO_RESIDUAL_AUX` | `Matriz Consolidada!BK2` | `valor_riesgo_residual_aux` |
| 64 | Verificación | 29 | `F29_VERIFICACION_RIESGO_RESIDUAL` | `Matriz Consolidada!BL2` | `verificacion` |
| 65 | VRR 2 | 30 | `F30_VRR_2` | `Matriz Consolidada!BM2` | `vrr_2` |
| 66 | Verificar VRR 2 | 31 | `F31_VERIFICAR_VRR_2` | `Matriz Consolidada!BN2` | `verificar_vrr_2` |
| 67 | Verificar Frec | 32 | `F32_VERIFICAR_FRECUENCIA` | `Matriz Consolidada!BO2` | `verificar_frecuencia` |
| 68 | Verificar Impact | 33 | `F33_VERIFICAR_IMPACTO` | `Matriz Consolidada!BP2` | `verificar_impacto` |
| 69 | VRI-VRR | 34 | `F34_DIFERENCIA_VRI_VRR` | `Matriz Consolidada!BQ2` | `diferencia_vri_vrr` |

Fórmula, target y celda fuente canónicos provienen de [`InstitutionalFormulaDataset.cs`](../../backend/RL.API/Features/MatricesRiesgos/Domain/InstitutionalFormulaDataset.cs); las etiquetas y columnas matriciales se resuelven desde `MATRIX_FIELDS`. No se mantiene un motor ni un catálogo de fórmulas paralelo.

## Trazabilidad F01–F34 en Configuración de cálculo

La lectura de fórmulas agrega metadata derivada del dataset canónico: número institucional, target, celda fuente y columna Excel. La UI relaciona `TargetField` con exactamente un `MATRIX_FIELDS.key` y valida que la columna Excel coincida. El detalle muestra etiqueta del campo, columna (por ejemplo, **Columna L**) y número institucional. Esta información es de solo lectura y no forma parte de los DTO de escritura ni se persiste en la base de datos. Fórmulas personalizadas no reciben referencias institucionales inventadas.

| Fórmula | Código | Campo matriz | Ordinal matriz | Excel | TargetField |
|---:|---|---|---:|---|---|
| 01 | `F01_VALOR_RIESGO_INHERENTE` | Valor del Riesgo Inherente | 12 | L | `valor_riesgo_inherente` |
| 02 | `F02_NIVEL_RIESGO_INHERENTE` | Nivel de Riesgo Inherente | 13 | M | `nivel_riesgo_inherente` |
| 03 | `F03_NIVEL_CONTROL_PREVENTIVO` | Nivel de efectividad de control(es) preventivo(s) | 22 | V | `nivel_control_preventivo` |
| 04 | `F04_PORCENTAJE_CONTROL_PREVENTIVO` | % efectividad de control(es) preventivo(s) | 23 | W | `porcentaje_control_preventivo` |
| 05 | `F05_NIVEL_CONTROL_DETECTIVO` | Nivel de efectividad de control(es) detectivo(s) | 26 | Z | `nivel_control_detectivo` |
| 06 | `F06_PORCENTAJE_CONTROL_DETECTIVO` | % efectividad de control detectivo | 27 | AA | `porcentaje_control_detectivo` |
| 07 | `F07_NIVEL_CONTROL_CORRECTIVO` | Nivel de efectividad de control(es) correctivo(s) | 30 | AD | `nivel_control_correctivo` |
| 08 | `F08_PORCENTAJE_CONTROL_CORRECTIVO` | % efectividad de control correctivo | 31 | AE | `porcentaje_control_correctivo` |
| 09 | `F09_EFECTIVIDAD_TOTAL_PONDERADA` | Efectividad Total Ponderada de los Controles | 33 | AG | `efectividad_total_ponderada` |
| 10 | `F10_RIESGO_RESIDUAL_DESCRIPCION` | Riesgo Residual | 34 | AH | `riesgo_residual_descripcion` |
| 11 | `F11_FRECUENCIA_RESIDUAL` | Frecuencia Residual | 35 | AI | `frecuencia_residual` |
| 12 | `F12_IMPACTO_RESIDUAL` | Impacto Residual | 36 | AJ | `impacto_residual` |
| 13 | `F13_VALOR_RIESGO_RESIDUAL` | Valor del Riesgo Residual | 37 | AK | `valor_riesgo_residual` |
| 14 | `F14_NIVEL_RIESGO_RESIDUAL` | Nivel del Riesgo Residual | 38 | AL | `nivel_riesgo_residual` |
| 15 | `F15_FRECUENCIA_RESIDUAL_AUX` | Frecuencia Residual (AUX) | 50 | AX | `frecuencia_residual_aux` |
| 16 | `F16_IMPACTO_RESIDUAL_AUX` | Impacto Residual (AUX) | 51 | AY | `impacto_residual_aux` |
| 17 | `F17_SUMA_RESIDUAL_REDONDEADA_AUX` | Suma Residual redondeada (AUX) | 52 | AZ | `suma_residual_redondeada_aux` |
| 18 | `F18_F_BASE_AUX` | F_base (AUX) | 53 | BA | `f_base` |
| 19 | `F19_I_BASE_AUX` | I_base (AUX) | 54 | BB | `i_base` |
| 20 | `F20_TOPE_F_AUX` | Tope F (AUX) | 55 | BC | `tope_f` |
| 21 | `F21_TOPE_I_AUX` | Tope I (AUX) | 56 | BD | `tope_i` |
| 22 | `F22_CAPACIDAD_F_AUX` | Capacidad F (AUX) | 57 | BE | `capacidad_f_aux` |
| 23 | `F23_CAPACIDAD_I_AUX` | Capacidad I (AUX) | 58 | BF | `capacidad_i_aux` |
| 24 | `F24_RESTO_AUX` | Resto (AUX) | 59 | BG | `resto_aux` |
| 25 | `F25_PREFIERE_I_AUX` | Prefiere I (AUX) | 60 | BH | `prefiere_i_aux` |
| 26 | `F26_INCREMENTO_I_AUX` | Inc_I (AUX) | 61 | BI | `incremento_i_aux` |
| 27 | `F27_INCREMENTO_F_AUX` | Inc_F (AUX) | 62 | BJ | `incremento_f_aux` |
| 28 | `F28_VALOR_RIESGO_RESIDUAL_AUX` | Valor del Riesgo Residual (AUX) | 63 | BK | `valor_riesgo_residual_aux` |
| 29 | `F29_VERIFICACION_RIESGO_RESIDUAL` | Verificación | 64 | BL | `verificacion` |
| 30 | `F30_VRR_2` | VRR 2 | 65 | BM | `vrr_2` |
| 31 | `F31_VERIFICAR_VRR_2` | Verificar VRR 2 | 66 | BN | `verificar_vrr_2` |
| 32 | `F32_VERIFICAR_FRECUENCIA` | Verificar Frec | 67 | BO | `verificar_frecuencia` |
| 33 | `F33_VERIFICAR_IMPACTO` | Verificar Impact | 68 | BP | `verificar_impacto` |
| 34 | `F34_DIFERENCIA_VRI_VRR` | VRI-VRR | 69 | BQ | `diferencia_vri_vrr` |

Las pruebas contractuales cruzan los 34 targets contra `MATRIX_FIELDS`, exigen una sola coincidencia, comparan etiquetas/ordinales/columnas y cubren los extremos F01 y F34. No se modificaron expresiones, tipos, targets, códigos, hashes, versiones ni DML institucional de F01–F34.

## Estados de verificación

- Campos 64 (`verificacion`) y 66 (`verificar_vrr_2`): el valor numérico exactamente igual a cero se presenta como **PASS**; cualquier valor numérico distinto de cero como **Revisar**. No se aplica tolerancia. Ausente, nulo, vacío o no numérico se muestra como `—` y **Sin dato**, nunca como PASS.
- Campos 67–69 (`verificar_frecuencia`, `verificar_impacto`, `diferencia_vri_vrr`): valores mayores o iguales a cero se muestran como **Esperado**; valores negativos como **Revisar**. Los valores originales se conservan y no se fuerza cero ni se aplica valor absoluto.
- Los números se leen de la respuesta calculada del servidor; el frontend no recalcula, cambia signo ni redondea para decidir el estado.

## Persistencia, alcance y certificación

No se requiere cambio de esquema. No se modificaron scripts SQL, fórmula DML ni datos de fórmula. No se intentó conectar a Oracle institucional ni ejecutar DML/DDL. No se tocaron `main`, producción ni los campos 70–82. La matriz completa conserva 69 campos implementados y Bloque 6 como único bloque pendiente.

## Evidencia de certificación

- `BASE_SHA=5f607c079b2f75b4552553b33a35f365c490af54`.
- `IMPLEMENTATION_SHA=22ca4f0f24f6b04ebfa366b2bab7532bcee639ce` (`feat(matrices): implement block 5 and formula traceability`).
- Quality Gate remoto de implementación: `RUN_ID=36735618174`, `RUN_NUMBER=1680`, `HEAD_SHA=22ca4f0f24f6b04ebfa366b2bab7532bcee639ce`, `STATUS=completed`, `CONCLUSION=success`.
- Evidencia remota: backend 750/750, 0 fallidos, 0 omitidos; frontend 83/83 archivos, 828/828; E2E 44/44, sin fallos ni reintentos; `npm audit=0`.
- Cobertura remota: backend 37.07% líneas, 40.35% ramas; frontend 62.56% sentencias, 56.05% ramas, 58.96% funciones, 63.49% líneas.
- Quality gates locales reproducidos en una copia temporal limpia: backend 750/750; frontend 83/83 y 828/828; E2E 44/44; build de producción, lint, auditoría y `tools/run_quality_gates.ps1` PASS. Los validadores de repositorio, base de datos, enlaces y UTF-8/mojibake también pasaron.
- `git diff --check=PASS`; instalación limpia (`npm ci`) y auditoría sin vulnerabilidades en la copia temporal. En el checkout compartido `npm ci` encontró `EPERM` al reemplazar esbuild, bloqueado por un `ng serve -o` preexistente; no se detuvo ese proceso. `main`, producción, Bloque 6 y Oracle institucional no fueron tocados.
- `SCHEMA_CHANGE_REQUIRED=NO`; los únicos cambios de dependencia son actualizaciones compatibles de pins/lockfile para restablecer `npm audit=0` sin `--force`.

La bitácora y el estado colaborativo de esta intervención están en [`BITACORA_COLABORACION.md`](../../BITACORA_COLABORACION.md) y [`ESTADO_COLABORACION.md`](../0.0%20Documentación/ESTADO_COLABORACION.md). El Quality Gate del commit documental final se registra en el reporte de cierre para conservar la comprobación sobre su SHA exacto.
