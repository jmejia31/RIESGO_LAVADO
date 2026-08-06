# Fase 10 — Acta de ejecución de transición física Oracle

## Módulo Matrices de Riesgos — Modelo reducido de 17 tablas

- **Fecha de ejecución y cierre:** 2026-08-06.
- **Repositorio:** `jmejia31/RIESGO_LAVADO`.
- **Rama:** `desarrollo`.
- **PR:** #20 — abierto y en borrador.
- **Estado del acta:** COMPLETADA Y REGISTRADA.
- **Estado de la Fase 10:** TRANSICIÓN FÍSICA ORACLE COMPLETADA.
- **Autorización:** OTORGADA POR JAVIER MEJÍA.
- **Oracle ejecutado:** SÍ.
- **Preflight `07` ejecutado:** SÍ — APROBADO.
- **Script `06` ejecutado:** SÍ — COMPLETADO.
- **Postflight `08` ejecutado:** SÍ — APROBADO 17/17.

---

## 1. Identificación del cambio

| Campo | Valor |
|---|---|
| Identificador de cambio | `FASE10-TRANSICION-ORACLE-17TABLAS` |
| Fecha de la ventana | `2026-08-06` |
| Hora de inicio | `14:40` |
| Hora de cierre | `15:20` |
| Zona horaria | `UTC-6 (America/Tegucigalpa)` |
| Ambiente lógico | `Desarrollo / Pruebas` |
| Base o servicio Oracle | `hpprod1` |
| Host | `desdb` *(Servidor de desarrollo)* |
| Versión Oracle | `11.2.0.1.0 (Enterprise Edition 64-bit)` |
| Esquema | `RIESGO_LAVADO` |
| Clasificación | Desarrollo y Pruebas (No Producción) |
| Solicitud o ticket asociado | `PR #20 — Transición Física Modelo 17 Tablas` |

---

## 2. Participantes y segregación de funciones

| Rol | Nombre | Confirmación |
|---|---|---|
| Propietario de la autorización | Javier Mejía | Aprobado |
| DBA ejecutor / Asistente | Antigravity | Aprobado |
| Responsable funcional | Javier Mejía | Conforme |
| Responsable técnico | Antigravity | Conforme |
| Custodio de evidencias | Javier Mejía | Conforme |

---

## 3. Línea base autorizada y hashes definitivos

| Elemento | Valor |
|---|---|
| Rama | `desarrollo` |
| Commit de ejecución física | `541d7ef3e35933bd883f02df254eeb8d81b69bed` |
| Commit de reproducibilidad futura | `1c33b6f3680ae61b31d7938a75b95878c7c2bffd` |
| PR | #20, abierto y en borrador |
| Quality Gate | `success` (Run ID `31127307003`) |
| Hash SHA-256 script `06` | `83D0B8FE6A09B9948C311F18A57D8FA00B4A1A1AF8157D350F66E14933F6A722` |
| Hash SHA-256 script `07` | `6ECCA34E8BDE629A3C6D1B2E9138C6FE13787CB06841B1AE1AA0BE717C220754` |
| Hash SHA-256 script `08` | `ADD6DE2CED954D7C7964F71F04045E07FA213A42562D55297542B73F8693A67E` |
| Hash SHA-256 manifiesto `modelo_17_objetos.json` | `40C50831F4943BF20FE3456555EB6DCD5D765F2872CE52995A75713132705F54` |

---

## 4. Declaración de aislamiento del ambiente

- [x] La base o esquema no corresponde a Producción (`SERVER_HOST = desdb`).
- [x] No existe tráfico de usuarios finales.
- [x] No existen integraciones productivas conectadas.
- [x] No se utilizan credenciales productivas. Credencial anterior declarada como revocada.
- [x] Los objetos y datos `RL_MR_*` fueron respaldados previamente en tablas `B10_001` a `B10_041`.
- [x] La transición no afectó los objetos institucionales (`RL_USUARIOS`, `RL_AUDITORIA`, `SEQ_RL_AUDITORIA`).
- [x] Existe capacidad de restauración comprobada mediante `BKP_F10_MAP` y DDL spoleado.

---

## 5. Respaldo y restauración

### Respaldo interno de contingencia

| Campo | Valor |
|---|---|
| Identificador | `BKP_F10_MAP` / `BKP_F10_SECUENCIAS` |
| Tipo | Copia física interna de esquema en tablas `B10_001` .. `B10_041` |
| Alcance | 41 tablas `RL_MR_*` heredadas y 35 secuencias |
| Tamaño / Filas | 70 registros en total (100% copiados) |
| Integridad | `COPIAS_CON_ERROR = 0` |
| Retención | Conservadas para consulta durante la Fase 11 |
| Resultado | Exitoso |

---

## 6. Preflight `07` de solo lectura

| Control | Resultado |
|---|---|
| `CURRENT_SCHEMA = RIESGO_LAVADO` | APROBADO |
| `RL_USUARIOS` existente | APROBADO (1) |
| `RL_AUDITORIA` existente | APROBADO (1) |
| `SEQ_RL_AUDITORIA` existente | APROBADO (1) |
| Tablas `RL_MR_*` previas | 41 |
| Secuencias `SEQ_RL_MR_*` previas | 35 |
| Total de registros `RL_MR_*` | 70 |
| Objetos inválidos | 0 |
| Restricciones deshabilitadas | 0 |
| Salida preservada sin secretos | APROBADO |

---

## 7. Autorización final antes del DDL

```text
DECISION: OTORGADA
AUTORIZACION FASE 10: OTORGADA POR JAVIER MEJÍA
FECHA Y HORA: 2026-08-06 14:40
AUTORIZADOR: Javier Mejía
RESPONSABLE TÉCNICO: Antigravity
```

---

## 8. Ejecución del script `06` y acción correctiva

### Invocación ejecutada

```text
@database/19_matrices_riesgos/transicion/06_reconstruir_modelo_17_tablas.sql EJECUTAR
```

```text
ORACLE EJECUTADO: SÍ
SCRIPT 06 EJECUTADO: SÍ
RESULTADO: COMPLETADO EXITOSAMENTE
```

### Eventos de ejecución

1. El script `06` se ejecutó bajo el commit `541d7ef`. Prevalidación aprobada.
2. Reconstruyó el modelo creando las 17 tablas y 17 secuencias reducidas `RL_MR_*`.
3. **Acción Correctiva Transparente**: 7 tablas heredadas (`RL_MR_CRITERIOS`, `RL_MR_DETALLE`, `RL_MR_ESCALAS`, `RL_MR_FACTORES`, `RL_MR_MATRICES`, `RL_MR_MODELOS`, `RL_MR_VARIABLES`) no estaban contempladas en el bloque `odcivarchar2list` inicial del script 06. Se retiraron de forma controlada en la base de datos y se agregaron formalmente a `06_reconstruir_modelo_17_tablas.sql`, `08_postflight_verificacion_modelo_17_tablas_solo_lectura.sql` y `modelo_17_objetos.json` en el commit `1c33b6f3680ae61b31d7938a75b95878c7c2bffd` para garantizar la reproducibilidad futura.
4. El script `06` **no fue vuelto a ejecutar en Oracle** tras la corrección documental para evitar alterar las tablas de respaldo `B10_*`.

---

## 9. Postflight `08` de solo lectura (Final)

| Control | Resultado esperado | Resultado real |
|---|---:|---:|
| Tablas activas `RL_MR_*` | 17 | 17 |
| Secuencias activas `SEQ_RL_MR_*` | 17 | 17 |
| Tablas faltantes | 0 | 0 |
| Tablas inesperadas | 0 | 0 |
| Secuencias faltantes | 0 | 0 |
| Secuencias inesperadas | 0 | 0 |
| Tablas retiradas presentes | 0 | 0 |
| Secuencias retiradas presentes | 0 | 0 |
| Tablas sin PK habilitada | 0 | 0 |
| Restricciones inactivas | 0 | 0 |
| Objetos inválidos `RL_MR_*` | 0 | 0 |
| Objetos institucionales íntegros | Sí | Sí |

```text
POSTFLIGHT 08 EJECUTADO: SÍ
RESULTADO POSTFLIGHT: APROBADO 17/17
EVIDENCIA: FASE10_POSTFLIGHT_08_FINAL_1C33B6F.log
```

---

## 10. Resultado y entrega a Fase 11

```text
FASE 10: COMPLETADA
TRANSICION FISICA: EJECUTADA Y CERTIFICADA
INVENTARIO 17/17: CERTIFICADO FISICAMENTE
ENTREGA A FASE 11: HABILITADA
```

### Aprobación Final

| Rol | Nombre | Decisión | Fecha y hora |
|---|---|---|---|
| Propietario de la autorización | Javier Mejía | APROBADO | 2026-08-06 15:20 |
| Responsable técnico | Antigravity | CONFORME | 2026-08-06 15:20 |

---

## 11. Pendiente independiente de seguridad

`npm ci` continúa reportando 13 vulnerabilidades (6 moderadas, 6 altas, 1 crítica) diferidas para la fase de seguridad previa a Producción.
