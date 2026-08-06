# Fase 7 — Fortalecimiento de la suite Oracle del modelo reducido

## Módulo Matrices de Riesgos — Modelo de 17 tablas

- **Fecha:** 2026-08-06.
- **Rama de trabajo:** `desarrollo`.
- **Rama estable:** `main` — no modificada.
- **Estado:** completada y validada en código, sin ejecución Oracle.
- **Certificación física Oracle:** pendiente.

---

## 1. Objetivo

Preparar una suite de certificación Oracle completa y segura para el modelo reducido de 17 tablas, sin abrir conexiones durante la ejecución ordinaria, sin ejecutar DDL y sin ejecutar los scripts `05` o `06`.

La certificación física continuará bloqueada hasta disponer de base Oracle exclusiva de pruebas, respaldo validado, credenciales seguras y autorización expresa.

---

## 2. Defectos corregidos

La suite anterior presentaba dos incompatibilidades principales:

1. el `INSERT` de `RL_MR_RIESGOS` omitía `RIE_NOMBRE` y `RIE_USR_CREACION`, columnas obligatorias del DDL reducido;
2. la prevalidación comprobaba únicamente cuatro tablas y no certificaba el modelo completo.

La nueva suite alinea el riesgo con:

```text
RIE_ID
RIE_CODIGO
RIE_NOMBRE
RIE_DESCRIPCION
RIE_ACTIVO
RIE_USR_CREACION
```

Los campos de fecha utilizan los valores por defecto definidos por Oracle.

---

## 3. Contrato físico preparado

La suite declara y compara exactamente:

- 17 tablas activas `RL_MR_*`;
- 17 secuencias activas `SEQ_RL_MR_*`;
- 18 tablas heredadas que deben estar ausentes;
- 3 secuencias heredadas que deben estar ausentes;
- 16 índices funcionales principales;
- claves primarias de las 17 tablas;
- claves foráneas, restricciones únicas y restricciones `CHECK` críticas.

También exige la presencia de:

```text
RL_USUARIOS
RL_AUDITORIA
SEQ_RL_AUDITORIA
```

La validación sobre `USER_TABLES` y `USER_SEQUENCES` exige conjuntos exactos para los objetos específicos del módulo.

---

## 4. Ciclo funcional completo preparado

La suite utiliza registros aislados con prefijo `TMR17_` para recorrer:

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

Las evaluaciones utilizan `EVA_DATOS_JSON` y `EVA_CALCULOS_JSON`. Los cálculos conservan código, versión y algoritmo de la regla.

---

## 5. Escenarios transaccionales preparados

La suite contiene cuatro escenarios Oracle:

1. **Contrato físico:** inventario, índices, restricciones y ausencias heredadas.
2. **Commit del ciclo completo:** confirma los registros base, el vínculo y la auditoría.
3. **Rollback del ciclo base:** comprueba que familia, versión, riesgo, evaluación, proyección, flujo y evidencia no persistan.
4. **Rollback de vínculo y auditoría:** provoca un fallo después de insertar auditoría y comprueba que no persista ninguno de los dos registros.

La limpieza opera en orden inverso de dependencias y no modifica datos institucionales existentes.

---

## 6. Bloqueo y seguridad

La suite permanece deshabilitada por defecto y solo intenta conectarse cuando:

```text
RL_ORACLE_INTEGRATION_REQUIRED=true
```

La conexión debe proceder de `ConnectionStrings__OracleDB` o User Secrets. Antes de ejecutar se exige:

```text
CURRENT_SCHEMA = RIESGO_LAVADO
```

La suite no ejecuta `CREATE`, `DROP`, scripts SQL ni migraciones automáticas.

---

## 7. Pruebas no Oracle agregadas

Se incorporó:

```text
MatricesRiesgosOracleCertificationContractTests.cs
```

Estas pruebas comprueban que:

- existan exactamente 17 tablas y 17 secuencias declaradas;
- no existan duplicados;
- los objetos activos y retirados no se mezclen;
- estén presentes los escenarios de contrato físico, commit, rollback y auditoría;
- los índices y restricciones principales estén definidos sin duplicados.

La suite Backend pasó de 216 a **222 pruebas aprobadas**.

---

## 8. Corrección del validador y CI

Durante la fase se detectaron tres debilidades:

1. el validador confundía los inventarios de ausencia con referencias SQL activas;
2. el workflow terminaba la validación con un `exit 0` externo que podía ocultar fallos;
3. PowerShell heredaba el código del último `git check-ignore` incluso después de una validación correcta.

El resultado definitivo es:

- la suite Oracle queda excluida únicamente del escaneo textual genérico de trazas;
- los objetos retirados pueden aparecer como inventario de ausencia;
- cualquier `INSERT`, `UPDATE`, `DELETE`, `MERGE` o `FROM` contra objetos retirados produce fallo;
- `TRA_REGLA_ID` continúa prohibido;
- el workflow respeta el resultado real del validador;
- el validador ejecuta `exit 1` con hallazgos y `exit 0` solo después de una revisión limpia.

---

## 9. Commits principales

```text
c7ec0fef5cb9907f96c7a74e59f9d3ea74ede771
test(matrices): fortalecer certificacion Oracle del modelo 17

0c515fea338a0f106d3186606428ada6deaccf1f
ci(matrices): hacer vinculante el validador dinamico

75e70ced4c7b474ba8c4f89bf5c5ae705629511b
test(matrices): alinear validador con certificacion Oracle fase 7

8d09af9fee3b28e2dea2c2149821686d79f09638
fix(matrices): normalizar salida exitosa del validador

3660033014014de01ff2c0f8852423c833bbfd03
docs(matrices): registrar normalizacion del validador fase 7
```

Los archivos auxiliares usados para aplicar las correcciones fueron eliminados y no permanecen en la rama.

---

## 10. Validación final

Quality Gate definitivo:

```text
Run: 31110675047
Commit validado: 3660033014014de01ff2c0f8852423c833bbfd03
Resultado: success
```

Resultados:

| Validación | Resultado |
|---|---:|
| Validador dinámico vinculante | Correcto |
| Inventario exacto | 17 tablas y 17 secuencias |
| Pruebas negativas del inventario | 9 correctas |
| Build Release | 0 advertencias, 0 errores |
| Backend | 222 aprobadas |
| Frontend | 123 aprobadas |
| E2E | 8 aprobadas |
| Cobertura Backend — líneas | 16.72 % |
| Cobertura Backend — ramas | 17.18 % |
| Cobertura Frontend — sentencias | 34.41 % |
| Cobertura Frontend — ramas | 31.52 % |
| Cobertura Frontend — funciones | 31.69 % |
| Cobertura Frontend — líneas | 33.87 % |

---

## 11. Restricciones verificadas

- `main` no fue modificada;
- el PR #20 permanece abierto y en borrador;
- Oracle no fue ejecutado;
- el script `05` no fue ejecutado;
- el script `06` no fue ejecutado;
- no se ejecutaron objetos físicos ni migraciones;
- no se publicaron credenciales.

---

## 12. Cierre y siguiente fase

La **Fase 7 queda completada en código y validada sin Oracle**.

Esto no constituye certificación física Oracle. La siguiente intervención es la **Fase 8 — Quality Gates finales y revisión de preparación previa a Oracle**, que deberá comprobar el estado íntegro de las Fases 1–7, revisar scripts, pruebas, seguridad, documentación y bloqueos antes de solicitar autorización para preparar el ambiente físico.
