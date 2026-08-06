# Formato de autorización separada — Fase 10 Oracle

## Transición física del Módulo Matrices de Riesgos

> Este documento es un formato de control. Su existencia en el repositorio **no constituye autorización**.

- **Estado actual:** **NO OTORGADA**.
- **Fase solicitada:** Fase 10 — Transición física controlada.
- **Script solicitado:** `database/19_matrices_riesgos/transicion/06_reconstruir_modelo_17_tablas.sql`.
- **Parámetro previsto:** `EJECUTAR`.
- **Ambiente permitido:** base Oracle exclusiva de pruebas; nunca Producción.
- **Modelo esperado:** 17 tablas y 17 secuencias `RL_MR_*`.

---

## 1. Identificación del cambio

| Campo | Valor |
|---|---|
| Número de solicitud o ticket | `PENDIENTE` |
| Fecha solicitada | `AAAA-MM-DD` |
| Ventana | `AAAA-MM-DD HH:MM–HH:MM` |
| Instancia o servicio | `PENDIENTE_DBA` |
| Esquema | `RIESGO_LAVADO` |
| Clasificación del ambiente | `PENDIENTE_DBA` |
| Commit exacto de `desarrollo` | `PENDIENTE` |
| Quality Gate | `PENDIENTE` |
| Hash SHA-256 del script `06` | `PENDIENTE` |
| Hash SHA-256 del preflight `07` | `PENDIENTE` |

---

## 2. Prerrequisitos

- [ ] Ambiente exclusivo identificado.
- [ ] Declaración escrita de no Producción.
- [ ] Sin tráfico de usuarios finales.
- [ ] Sin integraciones productivas.
- [ ] Respaldo completo exitoso.
- [ ] Restauración validada.
- [ ] Preflight `07` ejecutado y revisado.
- [ ] Datos `RL_MR_*` existentes conciliados.
- [ ] Commit y hashes verificados.
- [ ] Quality Gate aprobado.
- [ ] DBA ejecutor designado.
- [ ] DBA revisor designado.
- [ ] Responsable funcional designado.
- [ ] Custodio de evidencias designado.
- [ ] Plan de contingencia aceptado.
- [ ] Método seguro de conexión preparado.
- [ ] Ausencia de secretos en archivos y evidencias.

**Resultado de prerrequisitos:** PENDIENTE.

---

## 3. Declaración del DBA

Yo, `NOMBRE_DBA_PENDIENTE`, declaro que:

1. verifiqué la identidad de la instancia y del esquema;
2. confirmé que el ambiente no es Producción;
3. confirmé que existe respaldo restaurable;
4. revisé el carácter destructivo del script `06`;
5. comprendo que el DDL Oracle puede confirmar cambios implícitamente;
6. ejecutaré únicamente el archivo y commit autorizados;
7. detendré el proceso ante el primer error;
8. conservaré evidencia sin credenciales.

- **Nombre:** `PENDIENTE`.
- **Cargo:** `PENDIENTE`.
- **Fecha y hora:** `PENDIENTE`.
- **Firma o aprobación verificable:** `PENDIENTE`.

---

## 4. Declaración del responsable funcional

Yo, `NOMBRE_RESPONSABLE_PENDIENTE`, confirmo que:

1. los datos `RL_MR_*` existentes fueron revisados;
2. su disposición fue definida por escrito;
3. la transición puede realizarse en el ambiente identificado;
4. el resultado esperado es el modelo reducido de 17 tablas.

- **Nombre:** `PENDIENTE`.
- **Cargo:** `PENDIENTE`.
- **Fecha y hora:** `PENDIENTE`.
- **Firma o aprobación verificable:** `PENDIENTE`.

---

## 5. Autorización de Javier Mejía

La ejecución de la Fase 10 solo queda autorizada cuando esta sección contiene una aprobación expresa, fechada y verificable.

```text
DECISION: NO OTORGADA
ALCANCE AUTORIZADO: NINGUNO
FECHA: PENDIENTE
COMMIT AUTORIZADO: PENDIENTE
AMBIENTE AUTORIZADO: PENDIENTE
VENTANA AUTORIZADA: PENDIENTE
OBSERVACIONES: PENDIENTE
```

- **Nombre:** Javier Mejía.
- **Aprobación verificable:** PENDIENTE.

Una autorización verbal, implícita, inferida de una conversación anterior o basada únicamente en la existencia de este archivo no es válida.

---

## 6. Condiciones de ejecución una vez autorizada

- Ejecutar exclusivamente en el ambiente indicado.
- Confirmar `CURRENT_SCHEMA = RIESGO_LAVADO`.
- Ejecutar primero el preflight `07` de solo lectura.
- Comparar los hashes contra los autorizados.
- Utilizar el parámetro literal `EJECUTAR` únicamente durante la ventana.
- No ejecutar `05` ni semillas adicionales salvo autorización específica.
- No modificar el script durante la ventana.
- No improvisar comandos ante errores.
- Conservar el log completo.
- Pasar a la Fase 11 para certificación antes de declarar éxito.

---

## 7. Revocación y vencimiento

La autorización se considera revocada cuando:

- cambia el commit;
- cambia el hash de un script;
- cambia la instancia, esquema o ventana;
- el respaldo deja de estar disponible;
- aparece un hallazgo de seguridad o integridad;
- el Quality Gate deja de aprobar;
- se detectan datos no conciliados;
- un responsable retira su conformidad.

Cualquier modificación exige una nueva autorización.

---

## 8. Estado final del formato

```text
AUTORIZACION FASE 10: NO OTORGADA
ORACLE EJECUTADO: NO
SCRIPT 06 EJECUTADO: NO
```

Este formato debe permanecer en estado `NO OTORGADA` hasta que todos los prerrequisitos estén sustentados con evidencia real.
