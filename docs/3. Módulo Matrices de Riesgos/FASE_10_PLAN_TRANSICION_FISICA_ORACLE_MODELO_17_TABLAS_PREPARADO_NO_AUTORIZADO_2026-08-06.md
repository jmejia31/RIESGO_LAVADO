# Fase 10 — Plan operativo de transición física Oracle

## Módulo Matrices de Riesgos — Modelo reducido de 17 tablas

- **Fecha de preparación:** 2026-08-06.
- **Repositorio:** `jmejia31/RIESGO_LAVADO`.
- **Rama técnica:** `desarrollo`.
- **PR:** #20, abierto y en borrador.
- **Modelo objetivo:** 17 tablas y 17 secuencias `RL_MR_*`.
- **Estado:** PREPARADA, NO INICIADA.
- **Autorización de ejecución:** NO OTORGADA.
- **Oracle ejecutado:** NO.
- **Script `05` ejecutado:** NO.
- **Script `06` ejecutado:** NO.

---

## 1. Propósito

Este documento deja preparada la ejecución controlada de la Fase 10. No constituye autorización, no habilita una conexión y no permite ejecutar DDL.

La Fase 10 tendrá como único objetivo realizar, en una base Oracle exclusiva de pruebas y dentro de una ventana aprobada, la transición física hacia el modelo reducido de 17 tablas y 17 secuencias, conservando evidencia suficiente para la certificación de la Fase 11.

---

## 2. Condiciones de entrada obligatorias

La Fase 10 no podrá iniciar hasta que todas las condiciones siguientes estén marcadas como cumplidas y respaldadas por evidencia:

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
| 18 | Hashes SHA-256 de scripts `06` y `07` registrados | Pendiente |
| 19 | Formato de autorización diligenciado y firmado | Pendiente |
| 20 | Autorización expresa de Javier Mejía | Pendiente |

La ausencia de una sola condición mantiene la fase bloqueada.

---

## 3. Artefactos autorizables

### Preflight de solo lectura

```text
database/19_matrices_riesgos/transicion/07_preflight_inventario_oracle_solo_lectura.sql
```

### Transición física manual

```text
database/19_matrices_riesgos/transicion/06_reconstruir_modelo_17_tablas.sql
```

### Suite de certificación

```text
backend/RL.API.Tests/Features/MatricesRiesgos/MatricesRiesgosRepositoryIntegrationTests.cs
```

### Expediente y autorización

```text
docs/3. Módulo Matrices de Riesgos/FASE_9_EXPEDIENTE_AUTORIZACION_ORACLE_MODELO_17_TABLAS_2026-08-06.md
docs/3. Módulo Matrices de Riesgos/FASE_9_FORMATO_AUTORIZACION_EJECUCION_ORACLE_FASE_10_2026-08-06.md
```

No se permite ejecutar copias locales diferentes de los archivos versionados en el commit autorizado.

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

No se recomienda que una sola persona autorice, ejecute y certifique una operación destructiva.

---

## 5. Preparación de la ventana

Antes de conectarse:

1. Confirmar fecha, hora de inicio, hora máxima de cierre y zona horaria.
2. Confirmar participantes y canales de comunicación.
3. Congelar cambios sobre `desarrollo` durante la ventana.
4. Registrar el commit exacto autorizado.
5. Verificar que `main` continúe intacta.
6. Confirmar que el PR #20 permanezca abierto y en borrador.
7. Ejecutar Quality Gates no Oracle sobre el commit autorizado.
8. Calcular hashes SHA-256 de los scripts `06` y `07`.
9. Confirmar respaldo y restauración.
10. Preparar una carpeta segura para logs y evidencias.
11. Cargar la conexión únicamente mediante el mecanismo seguro aprobado.
12. Confirmar que no se impriman contraseñas ni cadenas completas.

---

## 6. Secuencia operativa prevista

### Etapa A — Identificación y preflight

1. Abrir SQL*Plus o herramienta aprobada sin incluir credenciales en el comando documentado.
2. Conectarse mediante el mecanismo seguro del DBA.
3. Ejecutar consultas de identidad del ambiente.
4. Confirmar `CURRENT_SCHEMA = RIESGO_LAVADO`.
5. Ejecutar únicamente el script `07` de solo lectura.
6. Guardar la salida completa sin secretos.
7. Revisar tablas, secuencias, conteos, objetos inválidos y restricciones deshabilitadas.
8. Comparar el inventario con el expediente.
9. Detenerse si existe cualquier discrepancia no resuelta.

### Etapa B — Confirmación final de autorización

1. Presentar el resultado del preflight.
2. Confirmar la decisión sobre datos `RL_MR_*` existentes.
3. Confirmar respaldo y restauración.
4. Completar el formato de autorización.
5. Recibir autorización expresa separada.
6. Registrar hora exacta de autorización.

### Etapa C — Transición física

Solo después de la autorización:

1. Ejecutar el script `06` manualmente con el parámetro literal `EJECUTAR`.
2. Conservar toda la salida de SQL*Plus.
3. Detenerse ante el primer error no contemplado.
4. No improvisar correcciones en la consola.
5. Registrar el último objeto procesado.
6. No ejecutar semillas, scripts `05` ni ajustes adicionales fuera del plan autorizado.

### Etapa D — Verificación inmediata

1. Confirmar exactamente 17 tablas activas.
2. Confirmar exactamente 17 secuencias activas.
3. Confirmar claves primarias, foráneas, únicas y `CHECK` principales.
4. Confirmar índices requeridos.
5. Confirmar ausencia de tablas y secuencias heredadas.
6. Confirmar objetos institucionales reutilizados.
7. Revisar objetos inválidos y restricciones deshabilitadas.
8. Registrar inventario posterior.

### Etapa E — Handoff a Fase 11

1. No declarar el módulo certificado todavía.
2. Habilitar la suite Oracle mediante `RL_ORACLE_INTEGRATION_REQUIRED=true`.
3. Proporcionar `ConnectionStrings__OracleDB` por un secreto temporal.
4. Ejecutar la certificación física y funcional.
5. Conservar evidencia de `COMMIT`, `ROLLBACK`, flujo, proyección, vínculo y auditoría.
6. Trasladar los resultados a la Fase 11.

---

## 7. Criterios de detención inmediata

La ejecución debe detenerse cuando ocurra cualquiera de estas condiciones:

- el esquema no es `RIESGO_LAVADO`;
- el ambiente corresponde o está conectado a Producción;
- falta el respaldo o la prueba de restauración;
- el preflight contiene resultados no conciliados;
- aparecen datos cuya disposición no fue autorizada;
- faltan `RL_USUARIOS`, `RL_AUDITORIA` o `SEQ_RL_AUDITORIA`;
- el hash de un script no coincide;
- el commit no coincide con el autorizado;
- el Quality Gate no está en `success`;
- falta un participante obligatorio;
- se produce un error Oracle no contemplado;
- la salida revela una credencial;
- el inventario posterior no contiene exactamente 17 tablas y 17 secuencias;
- una restricción o índice crítico queda ausente;
- existe duda sobre integridad, trazabilidad o alcance.

---

## 8. Contingencia y restauración

Oracle realiza commits implícitos en operaciones DDL. Por ello, `ROLLBACK` no revierte necesariamente objetos ya eliminados o creados.

Ante un fallo:

1. Detener la ejecución.
2. Preservar el log y el código Oracle del error.
3. No ejecutar comandos manuales no aprobados.
4. Comparar el inventario posterior con el previo.
5. Bloquear el uso funcional del módulo.
6. Determinar si el estado puede completarse de manera controlada o requiere restauración.
7. Restaurar cuando exista modelo parcial, objetos críticos ausentes o duda de integridad.
8. Ejecutar nuevamente el preflight después de restaurar.
9. Emitir acta de incidente antes de cualquier reintento.
10. Exigir una nueva autorización para repetir la transición.

---

## 9. Evidencias que deberán producirse

- identificación del ambiente;
- declaración de no Producción;
- responsables designados;
- respaldo exitoso;
- prueba de restauración;
- commit y hashes autorizados;
- Quality Gate aprobado;
- salida del preflight `07`;
- inventario y conteos previos;
- decisión sobre datos existentes;
- autorización firmada;
- hora de inicio y cierre;
- log completo del script `06`;
- inventario posterior;
- restricciones e índices posteriores;
- resultado de la suite Oracle;
- acta de éxito o incidente;
- confirmación de eliminación de secretos temporales.

---

## 10. Criterios de salida de Fase 10

La Fase 10 podrá cerrarse únicamente cuando:

1. el script autorizado haya finalizado sin errores, o se haya restaurado formalmente el estado previo;
2. existan exactamente 17 tablas y 17 secuencias objetivo;
3. los objetos heredados estén ausentes;
4. las restricciones e índices críticos estén activos;
5. los objetos institucionales permanezcan íntegros;
6. el inventario posterior esté documentado;
7. los logs y evidencias estén custodiados;
8. los secretos temporales hayan sido retirados;
9. exista un acta de ejecución;
10. la Fase 11 reciba formalmente el ambiente para certificación.

El cierre de Fase 10 no equivale a certificación funcional; esa certificación pertenece a la Fase 11.

---

## 11. Estado actual

```text
FASE 9: COMPLETADA
FASE 10: PREPARADA, NO INICIADA
AUTORIZACION: NO OTORGADA
AMBIENTE ORACLE: PENDIENTE
RESPALDO Y RESTAURACION: PENDIENTES
PREFLIGHT 07: NO EJECUTADO
SCRIPT 05: NO EJECUTADO
SCRIPT 06: NO EJECUTADO
MAIN: INTACTA
PR 20: ABIERTO Y EN BORRADOR
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
