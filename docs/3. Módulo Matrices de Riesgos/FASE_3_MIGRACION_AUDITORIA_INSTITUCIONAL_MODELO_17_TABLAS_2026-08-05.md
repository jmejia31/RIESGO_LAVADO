# Fase 3 — Migración de auditoría local hacia auditoría institucional

## Módulo Matrices de Riesgos — Modelo reducido de 17 tablas

- **Fecha:** 2026-08-05.
- **Rama de trabajo:** `desarrollo`.
- **Rama estable:** `main` — no modificada.
- **Estado:** completada y validada sin ejecución Oracle.

---

## 1. Objetivo

Eliminar del backend de Matrices de Riesgos toda escritura operativa hacia la auditoría local retirada y utilizar exclusivamente la auditoría institucional `RL_AUDITORIA`, compartiendo la misma conexión y transacción Oracle de la operación funcional.

Los objetos retirados del código activo son:

```text
RL_MR_AUDITORIA
SEQ_RL_MR_AUDITORIA
InsertarAuditoriaCampoAsync
```

La persistencia de auditoría queda delegada a:

```text
IAuditoriaRepository.RegistrarAsync
RL_AUDITORIA
SEQ_RL_AUDITORIA
```

---

## 2. Cambios implementados

### 2.1 Inyección obligatoria de auditoría institucional

Archivo:

```text
backend/RL.API/Features/MatricesRiesgos/Persistence/MatricesRiesgosRepository.cs
```

`MatricesRiesgosRepository` ahora expone un único constructor público que exige:

```text
OracleDbContext
IAuditoriaRepository
```

Se eliminó el constructor que permitía crear el repositorio sin auditoría y se retiró la nulabilidad de `_auditoriaRepository`.

La configuración de inyección de dependencias ya registraba `IAuditoriaRepository` y `IMatricesRiesgosRepository`, por lo que el contenedor resuelve la dependencia institucional de forma directa.

### 2.2 Creación de evaluación

La creación de una evaluación registra en la misma transacción:

- evaluación dinámica;
- proyección para consolidado;
- flujo inicial `BORRADOR`;
- auditoría institucional con acción `CREAR_EVALUACION`.

La auditoría usa:

```text
AUD_TABLA       = RL_MR_EVALUACIONES_RIESGO
AUD_REGISTRO_ID = identificador de la evaluación
AUD_ACCION      = CREAR_EVALUACION
AUD_DATOS_NVO   = riesgo, versión, datos y cálculos persistidos
AUD_USR_ID      = usuario ejecutor
AUD_IP          = IP recibida por la operación
AUD_MODULO      = MatricesRiesgos
```

Si la auditoría institucional falla, la creación completa se revierte mediante la misma `OracleTransaction`.

### 2.3 Actualización de evaluación

La actualización registra:

```text
AUD_ACCION = ACTUALIZAR_EVALUACION
```

Los datos anteriores incluyen las respuestas previas y la versión de concurrencia. Los datos nuevos incluyen las respuestas, los cálculos con metadatos institucionales y la nueva versión de fila.

La evaluación, su proyección y la auditoría se confirman o revierten de manera conjunta.

### 2.4 Transición de estado

Cada transición registra:

```text
AUD_ACCION = TRANSICION_ESTADO
```

Se conservan:

- estado anterior;
- estado nuevo;
- motivo;
- usuario;
- IP;
- módulo.

La actualización de la proyección, el nuevo flujo y la auditoría institucional utilizan la misma conexión y transacción.

### 2.5 Vinculación de evidencias

El vínculo genérico `RL_MR_EVIDENCIAS_VINCULOS` ya utilizaba auditoría institucional transaccional. En esta fase:

- se eliminó la condición que permitía auditoría opcional;
- se hizo obligatoria la auditoría institucional;
- se retiró la escritura local residual del adaptador heredado;
- el adaptador temporal, mientras permanezca hasta la Fase 4, registra solamente mediante `IAuditoriaRepository`.

### 2.6 Eliminación de la auditoría local

Se eliminó físicamente del repositorio el método:

```text
InsertarAuditoriaCampoAsync
```

El código activo ya no contiene inserciones hacia:

```text
RL_MR_AUDITORIA
SEQ_RL_MR_AUDITORIA
```

El script `06` conserva únicamente el retiro controlado de la tabla heredada y no vuelve a crearla.

---

## 3. Validador fortalecido

Archivo:

```text
scripts/validation/validate_matrices_dynamic_ddl_alignment.ps1
```

El validador ahora rechaza en el repositorio activo:

```text
InsertarAuditoriaCampoAsync
INSERT INTO RL_MR_AUDITORIA
SEQ_RL_MR_AUDITORIA
IAuditoriaRepository? _auditoriaRepository
this(db, null)
```

También exige:

```text
private readonly IAuditoriaRepository _auditoriaRepository;
IAuditoriaRepository auditoriaRepository
CREAR_EVALUACION
ACTUALIZAR_EVALUACION
TRANSICION_ESTADO
```

Para el script `06` verifica que:

- no cree `RL_MR_AUDITORIA`;
- no cree `SEQ_RL_MR_AUDITORIA`;
- conserve el retiro manual de la tabla heredada durante la reconstrucción autorizada.

---

## 4. Pruebas automatizadas

Se agregó:

```text
backend/RL.API.Tests/Features/MatricesRiesgos/
MatricesRiesgosInstitutionalAuditTests.cs
```

Las pruebas verifican que:

1. `MatricesRiesgosRepository` exponga un único constructor;
2. el constructor exija `OracleDbContext` e `IAuditoriaRepository`;
3. no exista `InsertarAuditoriaCampoAsync`;
4. `IAuditoriaRepository` exponga la operación con `OracleConnection` y `OracleTransaction` compartidas.

La prueba Oracle existente mantiene la comprobación de commit y rollback conjunto para el vínculo genérico y `RL_AUDITORIA`. La ampliación del ciclo completo evaluación–proyección–flujo pertenece a la Fase 7.

---

## 5. Commits publicados

```text
973150d82d8cc71e3f6c65d4e68fa29aa9150355
refactor(matrices): migrar auditoria local a institucional [phase3-done]

490259bf06d14df5988b4064ed253c5258ed2a58
test(matrices): validar contrato transaccional de auditoria institucional
```

El flujo auxiliar utilizado para aplicar el corte se eliminó automáticamente dentro del commit de migración y no permanece en el repositorio.

---

## 6. Verificación ejecutada

Quality Gate de GitHub Actions:

```text
Run: 31045641517
Resultado: success
Commit validado: 490259bf06d14df5988b4064ed253c5258ed2a58
```

Etapas aprobadas:

- restauración del backend;
- instalación de dependencias frontend;
- validador de alineación dinámica;
- compilación de la solución en Release;
- instalación de Playwright Chromium;
- pruebas backend;
- pruebas frontend y cobertura;
- build Angular;
- pruebas E2E;
- puertas completas del repositorio.

---

## 7. Elementos no modificados

Esta fase no adelantó el retiro físico de contratos pertenecientes a la siguiente fase. Permanecen temporalmente:

- `VincularEvidenciaAprobacionAsync`;
- `AsociarEvidenciaAprobacionDto`;
- `EjecutarVinculoEvidenciaAsync` para la tabla puente heredada;
- `PermisoFormularioDto`.

También continúa pendiente la certificación física Oracle de las 17 tablas.

---

## 8. Restricciones vigentes

- `main` permanece intacta;
- el PR #20 permanece abierto y en borrador;
- Oracle no fue ejecutado;
- el script `05` no fue ejecutado;
- el script `06` no fue ejecutado;
- no se ejecutaron eliminaciones físicas;
- no se publicaron credenciales.

---

## 9. Criterios de cierre

| Criterio | Resultado |
|---|---|
| Método local de auditoría eliminado | Cumplido |
| Escrituras en `RL_MR_AUDITORIA` retiradas | Cumplido |
| Secuencia local retirada del código activo | Cumplido |
| Auditoría institucional obligatoria por constructor | Cumplido |
| Creación auditada en la misma transacción | Cumplido |
| Actualización auditada en la misma transacción | Cumplido |
| Transición auditada en la misma transacción | Cumplido |
| Vínculos con auditoría institucional obligatoria | Cumplido |
| Validador actualizado | Cumplido |
| Pruebas automatizadas incorporadas | Cumplido |
| Compilación Release | Correcta |
| Quality Gates | Correctos |
| Oracle sin ejecutar | Cumplido |
| `main` intacta | Cumplido |

---

## 10. Resultado y siguiente fase

La **Fase 3 queda completada** en código y validada sin Oracle.

El siguiente trabajo autorizado es la **Fase 4 — retiro definitivo de adaptadores y contratos heredados**, que deberá:

1. eliminar `VincularEvidenciaAprobacionAsync`;
2. eliminar `AsociarEvidenciaAprobacionDto`;
3. eliminar `EjecutarVinculoEvidenciaAsync` y la construcción dinámica de tablas puente;
4. eliminar toda referencia activa a `RL_MR_EVI_APROBACION` y a las tablas `RL_MR_EVI_*`;
5. eliminar `PermisoFormularioDto`;
6. mantener exclusivamente `VincularEvidenciaAsync` y `RL_MR_EVIDENCIAS_VINCULOS`;
7. actualizar pruebas y validador;
8. mantener Oracle y el script `06` bloqueados.
