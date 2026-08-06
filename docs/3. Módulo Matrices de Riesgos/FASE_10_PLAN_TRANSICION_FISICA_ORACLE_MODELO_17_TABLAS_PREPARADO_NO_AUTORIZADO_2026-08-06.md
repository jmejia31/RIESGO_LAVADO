# Fase 10 — Plan operativo de transición física Oracle

## Módulo Matrices de Riesgos — Modelo reducido de 17 tablas

- **Fecha de preparación:** 2026-08-06.
- **Repositorio:** `jmejia31/RIESGO_LAVADO`.
- **Rama técnica:** `desarrollo`.
- **PR:** #20, abierto y en borrador.
- **Modelo objetivo:** 17 tablas y 17 secuencias `RL_MR_*`.
- **Estado:** DOCUMENTO PREOPERATIVO — TRANSICIÓN FÍSICA EJECUTADA Y CERTIFICADA (Remitirse a FASE_10_ACTA_EJECUCION_TRANSICION_ORACLE_MODELO_17_TABLAS_FINAL_2026-08-06.md).
- **Commit de ejecución:** `541d7ef3e35933bd883f02df254eeb8d81b69bed`.
- **Commit de reproducibilidad:** `1c33b6f3680ae61b31d7938a75b95878c7c2bffd`.
- **Autorización de ejecución:** OTORGADA POR JAVIER MEJÍA.
- **Oracle ejecutado:** SÍ.
- **Preflight `07` ejecutado:** SÍ — APROBADO.
- **Script `05` ejecutado:** NO.
- **Script `06` ejecutado:** SÍ — COMPLETADO.
- **Postflight `08` ejecutado:** SÍ — APROBADO 17/17.

---

## 1. Propósito

Este documento controla la Fase 10 y separa expresamente:

1. la preparación técnica verificable, que puede desarrollarse sin conexión a Oracle;
2. la transición física, que solo puede ejecutarse en una base Oracle exclusiva de pruebas, con respaldo restaurable, participantes designados y autorización expresa separada.

La preparación técnica no instala objetos, no elimina objetos, no modifica datos y no equivale a autorización.

---

## 2. Alcance técnico completado durante la preparación

Se prepararon los siguientes componentes:

1. Preflight Oracle de solo lectura:

```text
database/19_matrices_riesgos/transicion/07_preflight_inventario_oracle_solo_lectura.sql
```

2. Script manual de transición física:

```text
database/19_matrices_riesgos/transicion/06_reconstruir_modelo_17_tablas.sql
```

3. Postflight Oracle de solo lectura:

```text
database/19_matrices_riesgos/transicion/08_postflight_verificacion_modelo_17_tablas_solo_lectura.sql
```

4. Manifiesto exacto del modelo:

```text
database/19_matrices_riesgos/transicion/modelo_17_objetos.json
```

5. Preparador local de hashes y evidencias:

```text
scripts/operations/prepare_matrices_phase10_evidence.ps1
```

6. Acta operativa pendiente de diligenciamiento:

```text
docs/3. Módulo Matrices de Riesgos/FASE_10_ACTA_EJECUCION_TRANSICION_ORACLE_MODELO_17_TABLAS_PENDIENTE_2026-08-06.md
```

7. Validador automático del paquete:

```text
scripts/validation/validate_matrices_phase10_transition_package.ps1
```

El preparador de evidencias no conecta a Oracle ni ejecuta SQL*Plus. Solo verifica rama, commit, árbol limpio y hashes SHA-256 de los archivos autorizables.

---

## 3. Condiciones de entrada obligatorias

La transición física no podrá iniciar hasta que todas las condiciones siguientes estén cumplidas y respaldadas por evidencia:

| N.º | Condición | Estado actual |
|---:|---|---|
| 1 | Base Oracle exclusiva de pruebas identificada | Pendiente |
| 2 | Confirmación escrita de que no es Producción | Pendiente |
| 3 | Confirmación de ausencia de datos productivos | Pendiente |
| 4 | Esquema confirmado como `RIESGO_LAVADO` | Pendiente |
| 5 | DBA ejecutor designado | Pendiente |
| 6 | DBA revisor designado | Pendiente |
| 7 | Responsable funcional designado | Pendiente |
| 8 | Custodio de evidencias designado | Pendiente |
| 9 | Respaldo completo y verificable | Pendiente |
| 10 | Prueba de restauración aprobada | Pendiente |
| 11 | Ventana de cambio aprobada | Pendiente |
| 12 | Plan de contingencia aceptado | Pendiente |
| 13 | Conexión suministrada por secreto o variable de entorno | Pendiente |
| 14 | Preflight `07` ejecutado y revisado | Pendiente |
| 15 | Decisión escrita sobre objetos y datos existentes | Pendiente |
| 16 | Commit exacto de ejecución definido | Pendiente |
| 17 | Quality Gate del commit autorizado en `success` | Pendiente |
| 18 | Hashes SHA-256 de scripts `06`, `07` y `08` registrados | Pendiente |
| 19 | Formato de autorización diligenciado y firmado | Pendiente |
| 20 | Autorización expresa de Javier Mejía | Pendiente |

La ausencia de una sola condición mantiene la ejecución física bloqueada.

---

## 4. Participantes mínimos

| Rol | Responsabilidad durante la ventana |
|---|---|
| Javier Mejía | Autorizar expresamente el inicio o cancelar la ejecución |
| DBA ejecutor | Conectar, respaldar, ejecutar comandos y conservar logs |
| DBA revisor | Verificar esquema, comandos, resultados y detenciones |
| Responsable funcional | Confirmar disposición de datos y resultado esperado |
| Responsable técnico | Verificar commit, hashes, validadores y suite |
| Custodio de evidencias | Resguardar actas, salidas y pruebas sin secretos |

Una misma persona no debería aprobar, ejecutar y certificar en solitario una operación destructiva.

---

## 5. Preparación de evidencias antes de la ventana

Desde una copia local limpia de `desarrollo` deberá ejecutarse:

```powershell
./scripts/operations/prepare_matrices_phase10_evidence.ps1
```

El script:

- exige la rama `desarrollo`;
- exige árbol de trabajo limpio;
- registra el commit exacto;
- calcula SHA-256 de `06`, `07`, `08`, el manifiesto y los documentos de control;
- genera `fase10_manifest.json` y `fase10_resumen.txt` en una carpeta temporal;
- declara expresamente que Oracle y la transición no fueron ejecutados;
- no lee ni imprime una cadena de conexión.

Los hashes definitivos deben calcularse nuevamente dentro de la ventana sobre el commit autorizado.

---

## 6. Secuencia operativa prevista

### Etapa A — Congelamiento y comprobaciones

1. Confirmar fecha, hora de inicio, hora máxima de cierre y zona horaria.
2. Confirmar participantes y canales de comunicación.
3. Congelar cambios sobre `desarrollo` durante la ventana.
4. Registrar el commit exacto autorizado.
5. Verificar que `main` continúe intacta.
6. Confirmar que el PR #20 permanezca abierto y en borrador.
7. Confirmar Quality Gate en `success`.
8. Calcular y comparar hashes SHA-256.
9. Confirmar respaldo y restauración.
10. Preparar carpeta segura de evidencias.

### Etapa B — Identificación y preflight `07`

1. Conectarse mediante el mecanismo seguro del DBA.
2. Confirmar la identidad de la base y el host.
3. Confirmar `CURRENT_SCHEMA = RIESGO_LAVADO`.
4. Ejecutar únicamente:

```text
database/19_matrices_riesgos/transicion/07_preflight_inventario_oracle_solo_lectura.sql
```

5. Conservar la salida completa sin secretos.
6. Revisar tablas, secuencias, conteos, objetos inválidos y restricciones deshabilitadas.
7. Detenerse si existe cualquier discrepancia o dato sin disposición autorizada.

### Etapa C — Confirmación final de autorización

1. Presentar el resultado del preflight.
2. Confirmar la decisión sobre datos `RL_MR_*` existentes.
3. Confirmar respaldo y restauración.
4. Completar el formato separado de autorización.
5. Completar el acta de Fase 10 hasta la sección previa al DDL.
6. Recibir autorización expresa de Javier Mejía y conformidad de los responsables.
7. Registrar la hora exacta de autorización.

### Etapa D — Transición física mediante script `06`

Solo después de la autorización:

1. Ejecutar manualmente:

```text
@database/19_matrices_riesgos/transicion/06_reconstruir_modelo_17_tablas.sql EJECUTAR
```

2. Conservar toda la salida de SQL*Plus.
3. Detenerse ante el primer error no contemplado.
4. No improvisar comandos correctivos.
5. Registrar el último objeto procesado.
6. No ejecutar semillas, script `05` ni ajustes adicionales fuera del plan autorizado.

### Etapa E — Postflight `08`

Después de una transición sin errores deberá ejecutarse:

```text
database/19_matrices_riesgos/transicion/08_postflight_verificacion_modelo_17_tablas_solo_lectura.sql
```

El postflight exige:

- exactamente 17 tablas `RL_MR_*`;
- exactamente 17 secuencias `SEQ_RL_MR_*`;
- cero tablas o secuencias faltantes;
- cero objetos inesperados;
- cero objetos heredados retirados;
- claves primarias habilitadas en todas las tablas objetivo;
- cero restricciones inactivas;
- cero objetos inválidos `RL_MR_*`;
- permanencia de `RL_USUARIOS`, `RL_AUDITORIA` y `SEQ_RL_AUDITORIA`.

Este resultado valida el inventario físico inmediato, pero no sustituye la certificación funcional de Fase 11.

### Etapa F — Handoff a Fase 11

1. Cerrar el acta de ejecución o incidente.
2. Retirar secretos temporales.
3. Conservar hashes, preflight, log del script `06` y postflight.
4. No declarar todavía certificación funcional.
5. Entregar el ambiente a la Fase 11 para pruebas Oracle reales.

---

## 7. Criterios de detención inmediata

La ejecución debe detenerse cuando ocurra cualquiera de estas condiciones:

- el esquema no es `RIESGO_LAVADO`;
- el ambiente corresponde o está conectado a Producción;
- falta el respaldo o la prueba de restauración;
- el preflight contiene resultados no conciliados;
- aparecen datos cuya disposición no fue autorizada;
- faltan `RL_USUARIOS`, `RL_AUDITORIA` o `SEQ_RL_AUDITORIA`;
- un hash no coincide;
- el commit no coincide con el autorizado;
- el Quality Gate no está en `success`;
- falta un participante obligatorio;
- se produce un error Oracle no contemplado;
- la salida revela una credencial;
- el postflight no confirma exactamente 17 tablas y 17 secuencias;
- una restricción o índice crítico queda ausente;
- existe duda sobre integridad, trazabilidad o alcance.

---

## 8. Contingencia y restauración

Oracle realiza commits implícitos en operaciones DDL. Por ello, `ROLLBACK` no revierte necesariamente objetos ya eliminados o creados.

Ante un fallo:

1. Detener la ejecución.
2. Preservar el log y el código Oracle del error.
3. No ejecutar comandos manuales no aprobados.
4. Registrar el último objeto procesado.
5. Comparar el inventario posterior con el previo.
6. Bloquear el uso funcional del módulo.
7. Determinar si procede completar controladamente o restaurar.
8. Restaurar cuando exista modelo parcial, objetos críticos ausentes o duda de integridad.
9. Ejecutar nuevamente el preflight después de restaurar.
10. Emitir acta de incidente y exigir nueva autorización antes de reintentar.

---

## 9. Evidencias obligatorias

- identificación del ambiente;
- declaración de no Producción;
- responsables designados;
- respaldo exitoso;
- prueba de restauración;
- commit y hashes autorizados;
- Quality Gate aprobado;
- manifiesto generado por `prepare_matrices_phase10_evidence.ps1`;
- salida del preflight `07`;
- inventario y conteos previos;
- decisión sobre datos existentes;
- autorización firmada;
- log completo del script `06`;
- salida del postflight `08`;
- inventario posterior;
- restricciones e índices posteriores;
- acta de éxito o incidente;
- confirmación de eliminación de secretos temporales.

---

## 10. Criterios de salida de Fase 10

La Fase 10 podrá cerrarse únicamente cuando:

1. el script autorizado haya finalizado sin errores, o se haya restaurado formalmente el estado previo;
2. el postflight confirme exactamente 17 tablas y 17 secuencias;
3. los objetos heredados estén ausentes;
4. las restricciones e índices críticos estén activos;
5. los objetos institucionales permanezcan íntegros;
6. el inventario posterior esté documentado;
7. los logs y evidencias estén custodiados;
8. los secretos temporales hayan sido retirados;
9. exista un acta de ejecución o incidente completa;
10. la Fase 11 reciba formalmente el ambiente para certificación.

El cierre de Fase 10 no equivale a certificación funcional.

---

## 11. Estado actual

```text
FASE 9: COMPLETADA
FASE 10 — PREPARACIÓN TÉCNICA: COMPLETADA
FASE 10 — TRANSICIÓN FÍSICA ORACLE: COMPLETADA
PREFLIGHT 07: APROBADO
SCRIPT 05: NO EJECUTADO
SCRIPT 06: EJECUTADO
POSTFLIGHT 08: APROBADO 17/17
RESPALDO B10_*: CONSERVADO PARA FASE 11
FASE 11: HABILITADA
MAIN: INTACTA
PR #20: ABIERTO Y EN BORRADOR
```

---

## 12. Pendiente independiente de seguridad

`npm ci` continúa reportando:

```text
13 vulnerabilidades
6 moderadas
6 altas
1 crítica
```

No se aplicó `npm audit fix --force`, porque podría introducir cambios incompatibles. Este pendiente requiere una fase de seguridad separada antes de Producción y debe recordarse al final de cada fase hasta su resolución formal.
