# Fase 7 — Fortalecimiento de la suite Oracle del modelo reducido

## Módulo Matrices de Riesgos — Modelo de 17 tablas

- **Fecha:** 2026-08-06.
- **Rama de trabajo:** `desarrollo`.
- **Rama estable:** `main` — no modificada.
- **Estado:** implementación completada en código; validación institucional en ejecución.
- **Oracle:** no ejecutado.

---

## 1. Objetivo

Preparar una suite de certificación Oracle completa y segura para el modelo reducido de 17 tablas, sin abrir conexiones durante la ejecución ordinaria, sin ejecutar DDL y sin ejecutar los scripts `05` o `06`.

La certificación física continuará bloqueada hasta disponer de:

- base Oracle exclusiva de pruebas;
- respaldo validado;
- cadena de conexión suministrada mediante variable de entorno o User Secrets;
- autorización expresa de ejecución.

---

## 2. Defectos corregidos

La suite anterior presentaba dos incompatibilidades principales:

1. el `INSERT` de `RL_MR_RIESGOS` omitía `RIE_NOMBRE` y `RIE_USR_CREACION`, columnas obligatorias del DDL reducido;
2. la prevalidación comprobaba únicamente `RL_MR_RIESGOS`, `RL_MR_EVIDENCIAS`, `RL_MR_EVIDENCIAS_VINCULOS` y `RL_AUDITORIA`.

La nueva suite alinea el riesgo con:

```text
RIE_ID
RIE_CODIGO
RIE_NOMBRE
RIE_DESCRIPCION
RIE_ACTIVO
RIE_USR_CREACION
```

Los campos de fecha continúan utilizando los valores por defecto definidos por Oracle.

---

## 3. Inventario físico preparado

La suite declara y compara exactamente:

- 17 tablas activas `RL_MR_*`;
- 17 secuencias activas `SEQ_RL_MR_*`;
- 18 tablas heredadas que deben estar ausentes;
- 3 secuencias heredadas que deben estar ausentes;
- 16 índices funcionales principales;
- claves primarias de las 17 tablas;
- claves foráneas, restricciones únicas y `CHECK` críticas.

También exige la presencia de los objetos institucionales reutilizados:

```text
RL_USUARIOS
RL_AUDITORIA
SEQ_RL_AUDITORIA
```

La consulta a `USER_TABLES` y `USER_SEQUENCES` debe devolver conjuntos exactos para los objetos específicos de Matrices de Riesgos.

---

## 4. Ciclo funcional completo preparado

La suite crea registros aislados con prefijo `TMR17_` para recorrer:

```text
Familia de formulario
  → versión publicada y vigente
  → riesgo
  → evaluación dinámica
  → proyección tipada
  → flujo inicial
  → evidencia
  → vínculo genérico con la evaluación
  → auditoría institucional
```

Los datos dinámicos utilizan:

```text
EVA_DATOS_JSON
EVA_CALCULOS_JSON
```

Los cálculos conservan código, versión y algoritmo de regla dentro de `EVA_CALCULOS_JSON`.

---

## 5. Escenarios transaccionales

Se prepararon cuatro escenarios Oracle:

1. **Contrato físico**: tablas, secuencias, índices, restricciones y ausencias heredadas.
2. **Commit del ciclo completo**: confirma todos los registros base, el vínculo y la auditoría.
3. **Rollback del ciclo base**: inserta familia, versión, riesgo, evaluación, proyección, flujo y evidencia en una transacción y comprueba que ninguno persista después del rollback.
4. **Rollback de vínculo y auditoría**: provoca un fallo controlado después de insertar la auditoría y comprueba que no persistan ni vínculo ni auditoría.

La limpieza elimina los registros en orden inverso de dependencias y no altera datos institucionales existentes.

---

## 6. Bloqueo y seguridad

La suite permanece deshabilitada por defecto. Solo intenta conectarse cuando:

```text
RL_ORACLE_INTEGRATION_REQUIRED=true
```

La cadena debe proceder de:

```text
ConnectionStrings__OracleDB
```

O de User Secrets. No existen credenciales codificadas en el repositorio.

Antes de ejecutar cualquier escenario se valida que:

```text
CURRENT_SCHEMA = RIESGO_LAVADO
```

La suite no contiene ejecución de `CREATE`, `DROP`, scripts SQL ni migraciones automáticas.

---

## 7. Pruebas no Oracle agregadas

Se incorporó:

```text
MatricesRiesgosOracleCertificationContractTests.cs
```

Estas pruebas se ejecutan siempre y comprueban que:

- el inventario declare exactamente 17 tablas y 17 secuencias;
- no haya duplicados;
- objetos activos y retirados no se mezclen;
- estén declarados los escenarios de contrato físico, commit, rollback y auditoría;
- índices y restricciones principales estén definidos sin duplicados.

---

## 8. Validador y Quality Gate

Durante la intervención se detectaron y corrigieron dos debilidades de calidad:

1. el validador trataba las listas de objetos cuya ausencia se certifica como si fueran referencias SQL activas;
2. el workflow terminaba la etapa del validador con `exit 0`, ocultando su código de fallo.

El validador ahora:

- excluye la suite Oracle únicamente del escaneo textual genérico de trazas;
- exige el inventario, los campos obligatorios y los cuatro escenarios nuevos;
- permite mencionar objetos retirados solo como inventario de ausencia;
- rechaza `INSERT`, `UPDATE`, `DELETE`, `MERGE` o `FROM` contra objetos retirados;
- continúa prohibiendo `TRA_REGLA_ID`.

El Quality Gate ya no fuerza una salida exitosa: cualquier hallazgo del validador bloqueará realmente la ejecución.

---

## 9. Commits principales

```text
c7ec0fef5cb9907f96c7a74e59f9d3ea74ede771
test(matrices): fortalecer certificacion Oracle del modelo 17

0c515fea338a0f106d3186606428ada6deaccf1f
ci(matrices): hacer vinculante el validador dinamico

75e70ced4c7b474ba8c4f89bf5c5ae705629511b
test(matrices): alinear validador con certificacion Oracle fase 7
```

Los archivos auxiliares de aplicación fueron eliminados automáticamente y no permanecen en la rama.

---

## 10. Restricciones vigentes

- `main` permanece intacta;
- el PR #20 permanece abierto y en borrador;
- Oracle no fue ejecutado;
- el script `05` no fue ejecutado;
- el script `06` no fue ejecutado;
- no se ejecutaron `CREATE`, `DROP` ni migraciones;
- no se publicaron secretos.

---

## 11. Criterio pendiente de cierre

La Fase 7 se declarará completada en código cuando el Quality Gate institucional apruebe, con el validador ahora vinculante:

- alineación dinámica;
- inventario exacto de 17 tablas y 17 secuencias;
- compilación Release;
- pruebas Backend;
- pruebas Frontend y cobertura;
- build Angular;
- E2E.

Esto no equivaldrá a certificación física Oracle. Esa certificación continuará pendiente hasta la fase expresamente autorizada.
