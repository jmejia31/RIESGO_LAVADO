# Fase 10 — Acta de ejecución de transición física Oracle

## Módulo Matrices de Riesgos — Modelo reducido de 17 tablas

- **Fecha de preparación del formato:** 2026-08-06.
- **Repositorio:** `jmejia31/RIESGO_LAVADO`.
- **Rama autorizable:** `desarrollo`.
- **PR:** #20 — debe permanecer abierto y en borrador.
- **Estado del acta:** PENDIENTE DE DILIGENCIAMIENTO.
- **Estado de la Fase 10:** PREPARACIÓN TÉCNICA EN CURSO; EJECUCIÓN FÍSICA NO INICIADA.
- **Autorización:** NO OTORGADA.
- **Oracle ejecutado:** NO.
- **Preflight `07` ejecutado:** NO.
- **Script `06` ejecutado:** NO.
- **Postflight `08` ejecutado:** NO.

> Este documento es un formato de evidencia. Su existencia no autoriza la ejecución de Oracle ni del script `06`.

---

## 1. Identificación del cambio

| Campo | Valor |
|---|---|
| Identificador de cambio | `PENDIENTE` |
| Fecha de la ventana | `AAAA-MM-DD` |
| Hora de inicio | `HH:MM` |
| Hora máxima de cierre | `HH:MM` |
| Zona horaria | `PENDIENTE` |
| Ambiente lógico | `PENDIENTE_DBA` |
| Base o servicio Oracle | `PENDIENTE_DBA` |
| Host | `PENDIENTE_DBA` |
| Versión Oracle | `PENDIENTE_DBA` |
| Esquema | Debe ser `RIESGO_LAVADO` |
| Clasificación | Desarrollo, QA o certificación; nunca Producción |
| Solicitud o ticket asociado | `PENDIENTE` |

---

## 2. Participantes y segregación de funciones

| Rol | Nombre | Confirmación |
|---|---|---|
| Propietario de la autorización | Javier Mejía | Pendiente |
| DBA ejecutor | `PENDIENTE_DBA` | Pendiente |
| DBA revisor | `PENDIENTE_DBA_REVISOR` | Pendiente |
| Responsable funcional | `PENDIENTE_RESPONSABLE` | Pendiente |
| Responsable técnico | `PENDIENTE_TECNICO` | Pendiente |
| Custodio de evidencias | `PENDIENTE_CUSTODIO` | Pendiente |

---

## 3. Línea base autorizable

| Elemento | Valor |
|---|---|
| Rama | `desarrollo` |
| Commit autorizado | `PENDIENTE` |
| Commit de `main` antes de la ventana | `PENDIENTE` |
| PR | #20, abierto y en borrador |
| Quality Gate | `PENDIENTE` |
| Run de Quality Gate | `PENDIENTE` |
| Hash SHA-256 script `06` | `PENDIENTE` |
| Hash SHA-256 script `07` | `PENDIENTE` |
| Hash SHA-256 script `08` | `PENDIENTE` |
| Hash SHA-256 manifiesto `modelo_17_objetos.json` | `PENDIENTE` |
| Manifiesto de evidencia | `PENDIENTE` |

### Verificación

- [ ] El commit coincide con el aprobado.
- [ ] El árbol de trabajo está limpio.
- [ ] `main` permanece intacta.
- [ ] El PR #20 permanece abierto y en borrador.
- [ ] El Quality Gate del commit autorizado finalizó en `success`.
- [ ] Los hashes fueron calculados desde los archivos versionados.
- [ ] No se detectaron credenciales en archivos, logs o capturas.

---

## 4. Declaración de aislamiento del ambiente

El DBA y el responsable funcional deberán confirmar:

- [ ] La base o esquema no corresponde a Producción.
- [ ] No existe tráfico de usuarios finales.
- [ ] No existen integraciones productivas conectadas.
- [ ] No se utilizan credenciales productivas.
- [ ] Los objetos y datos `RL_MR_*` son de prueba o tienen disposición autorizada.
- [ ] La transición no afectará otros módulos institucionales.
- [ ] Existe capacidad real de restauración.

### Declaración escrita

```text
PENDIENTE DE DECLARACION DEL DBA Y RESPONSABLE FUNCIONAL.
```

---

## 5. Respaldo y restauración

### Respaldo previo

| Campo | Valor |
|---|---|
| Identificador | `PENDIENTE_DBA` |
| Tipo | `PENDIENTE_DBA` |
| Alcance | Esquema completo o base completa |
| Inicio | `PENDIENTE_DBA` |
| Finalización | `PENDIENTE_DBA` |
| Tamaño | `PENDIENTE_DBA` |
| Integridad o hash | `PENDIENTE_DBA` |
| Retención | `PENDIENTE_DBA` |
| Resultado | Pendiente |

### Prueba de restauración

| Campo | Valor |
|---|---|
| Identificador | `PENDIENTE_DBA` |
| Destino controlado | `PENDIENTE_DBA` |
| Fecha | `PENDIENTE_DBA` |
| Tiempo de recuperación | `PENDIENTE_DBA` |
| Validaciones posteriores | `PENDIENTE_DBA` |
| Resultado | Pendiente |

- [ ] Respaldo finalizado exitosamente.
- [ ] Integridad del respaldo verificada.
- [ ] Restauración probada y documentada.
- [ ] Procedimiento de recuperación disponible durante la ventana.

---

## 6. Preflight `07` de solo lectura

### Archivo

```text
database/19_matrices_riesgos/transicion/07_preflight_inventario_oracle_solo_lectura.sql
```

| Control | Resultado |
|---|---|
| `CURRENT_SCHEMA = RIESGO_LAVADO` | Pendiente |
| `RL_USUARIOS` existente | Pendiente |
| `RL_AUDITORIA` existente | Pendiente |
| `SEQ_RL_AUDITORIA` existente | Pendiente |
| Tablas `RL_MR_*` previas | Pendiente |
| Secuencias `SEQ_RL_MR_*` previas | Pendiente |
| Total de registros `RL_MR_*` | Pendiente |
| Objetos inválidos | Pendiente |
| Restricciones deshabilitadas | Pendiente |
| Salida preservada sin secretos | Pendiente |

### Decisión sobre objetos y datos existentes

```text
PENDIENTE DE DECISION ESCRITA.
```

**Ruta o identificador de evidencia:** `PENDIENTE`.

---

## 7. Autorización final antes del DDL

La ejecución del script `06` solo podrá comenzar cuando todos los controles anteriores hayan sido aprobados.

```text
DECISION: NO OTORGADA
AUTORIZACION FASE 10: NO OTORGADA
FECHA Y HORA: PENDIENTE
AUTORIZADOR: Javier Mejía
DBA CONFORME: PENDIENTE
RESPONSABLE FUNCIONAL CONFORME: PENDIENTE
```

- [ ] Autorización expresa de Javier Mejía.
- [ ] Conformidad del DBA ejecutor.
- [ ] Conformidad del DBA revisor.
- [ ] Conformidad del responsable funcional.
- [ ] Hora exacta de autorización registrada.

---

## 8. Ejecución del script `06`

### Archivo

```text
database/19_matrices_riesgos/transicion/06_reconstruir_modelo_17_tablas.sql
```

### Invocación autorizable

```text
@database/19_matrices_riesgos/transicion/06_reconstruir_modelo_17_tablas.sql EJECUTAR
```

La línea anterior documenta la forma futura de invocación; no demuestra ni autoriza su ejecución.

| Evento | Fecha y hora | Responsable | Resultado |
|---|---|---|---|
| Inicio de ejecución | Pendiente | Pendiente | Pendiente |
| Prevalidación interna | Pendiente | Pendiente | Pendiente |
| Retiro de objetos heredados | Pendiente | Pendiente | Pendiente |
| Creación de secuencias | Pendiente | Pendiente | Pendiente |
| Creación de tablas | Pendiente | Pendiente | Pendiente |
| Creación de restricciones e índices | Pendiente | Pendiente | Pendiente |
| Finalización | Pendiente | Pendiente | Pendiente |

```text
ORACLE EJECUTADO: NO
SCRIPT 06 EJECUTADO: NO
RESULTADO: PENDIENTE
ULTIMO OBJETO PROCESADO: PENDIENTE
CODIGO ORACLE: PENDIENTE
EVIDENCIA DEL LOG: PENDIENTE
```

---

## 9. Postflight `08` de solo lectura

### Archivo

```text
database/19_matrices_riesgos/transicion/08_postflight_verificacion_modelo_17_tablas_solo_lectura.sql
```

| Control | Resultado esperado | Resultado real |
|---|---:|---:|
| Tablas activas `RL_MR_*` | 17 | Pendiente |
| Secuencias activas `SEQ_RL_MR_*` | 17 | Pendiente |
| Tablas faltantes | 0 | Pendiente |
| Tablas inesperadas | 0 | Pendiente |
| Secuencias faltantes | 0 | Pendiente |
| Secuencias inesperadas | 0 | Pendiente |
| Tablas retiradas presentes | 0 | Pendiente |
| Secuencias retiradas presentes | 0 | Pendiente |
| Tablas sin PK habilitada | 0 | Pendiente |
| Restricciones inactivas | 0 | Pendiente |
| Objetos inválidos `RL_MR_*` | 0 | Pendiente |
| Objetos institucionales íntegros | Sí | Pendiente |

```text
POSTFLIGHT 08 EJECUTADO: NO
RESULTADO POSTFLIGHT: PENDIENTE
EVIDENCIA: PENDIENTE
```

---

## 10. Incidentes y contingencia

### ¿Ocurrió un incidente?

```text
NO DETERMINADO; EJECUCION NO INICIADA.
```

| Campo | Valor |
|---|---|
| Fecha y hora | `PENDIENTE` |
| Código Oracle | `PENDIENTE` |
| Último objeto procesado | `PENDIENTE` |
| Estado físico detectado | `PENDIENTE` |
| Decisión | Completar controladamente, restaurar o cancelar |
| Responsable de decisión | `PENDIENTE` |
| Restauración ejecutada | `PENDIENTE` |
| Resultado de restauración | `PENDIENTE` |
| Nuevo preflight ejecutado | `PENDIENTE` |
| Acta de incidente | `PENDIENTE` |

Ante cualquier error no contemplado se prohíbe improvisar comandos correctivos.

---

## 11. Retiro de secretos temporales

- [ ] `ConnectionStrings__OracleDB` retirada del entorno temporal.
- [ ] `RL_ORACLE_INTEGRATION_REQUIRED` retirada o restablecida.
- [ ] No quedaron cadenas de conexión en archivos, historial, logs o capturas.
- [ ] La credencial fue rotada si existió una exposición accidental.
- [ ] El custodio confirmó la sanitización de evidencias.

---

## 12. Resultado y entrega a Fase 11

```text
FASE 10: NO COMPLETADA
TRANSICION FISICA: NO EJECUTADA
INVENTARIO 17/17: NO CERTIFICADO FISICAMENTE
ENTREGA A FASE 11: NO REALIZADA
```

La Fase 10 podrá cerrarse únicamente con ejecución exitosa o restauración formal, inventario posterior documentado y entrega controlada a la Fase 11.

### Firmas o aprobaciones

| Rol | Nombre | Decisión | Fecha y hora |
|---|---|---|---|
| Javier Mejía | Javier Mejía | Pendiente | Pendiente |
| DBA ejecutor | `PENDIENTE_DBA` | Pendiente | Pendiente |
| DBA revisor | `PENDIENTE_DBA_REVISOR` | Pendiente | Pendiente |
| Responsable funcional | `PENDIENTE_RESPONSABLE` | Pendiente | Pendiente |
| Responsable técnico | `PENDIENTE_TECNICO` | Pendiente | Pendiente |
| Custodio de evidencias | `PENDIENTE_CUSTODIO` | Pendiente | Pendiente |

---

## 13. Pendiente independiente de seguridad

`npm ci` continúa reportando:

```text
13 vulnerabilidades
6 moderadas
6 altas
1 crítica
```

No se aplicó `npm audit fix --force`, porque podría introducir cambios incompatibles. Este pendiente requiere una fase de seguridad separada antes de Producción y debe recordarse al final de cada fase hasta su resolución formal.
