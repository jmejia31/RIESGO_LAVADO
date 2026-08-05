# Fase 1 — Alineación del contrato físico y columnas JSON

## Módulo Matrices de Riesgos — Modelo reducido de 17 tablas

- **Fecha:** 2026-08-05.
- **Rama de trabajo:** `desarrollo`.
- **Rama estable:** `main` — no modificada.
- **Estado:** completada y validada sin ejecución Oracle.

---

## 1. Objetivo

Alinear las consultas, inserciones y actualizaciones del backend con los nombres físicos definidos por el DDL objetivo de 17 tablas para `RL_MR_EVALUACIONES_RIESGO`.

La incompatibilidad corregida era:

```text
EVA_DATA_JSON       → EVA_DATOS_JSON
EVA_DATA_CALC_JSON  → EVA_CALCULOS_JSON
```

Esta fase no modifica los nombres públicos de las propiedades DTO `EvaDataJson` y `EvaDataCalcJson`, con el propósito de evitar una ruptura innecesaria de los contratos HTTP y del frontend. El cambio se limita al mapeo físico Oracle.

---

## 2. Cambios implementados

### 2.1 Repositorio de Matrices de Riesgos

Archivo:

```text
backend/RL.API/Features/MatricesRiesgos/Persistence/MatricesRiesgosRepository.cs
```

Se corrigieron todos los accesos activos a la tabla `RL_MR_EVALUACIONES_RIESGO`:

- consulta individual de evaluación;
- listado paginado;
- creación de evaluación;
- lectura previa para concurrencia optimista;
- actualización de respuestas y cálculos.

A partir de esta fase, el repositorio utiliza exclusivamente:

```text
EVA_DATOS_JSON
EVA_CALCULOS_JSON
```

### 2.2 Validador técnico

Archivo:

```text
scripts/validation/validate_matrices_dynamic_ddl_alignment.ps1
```

El validador ahora:

- exige la existencia del script manual `06_reconstruir_modelo_17_tablas.sql`;
- rechaza `EVA_DATA_JSON` y `EVA_DATA_CALC_JSON` en el repositorio activo;
- exige `EVA_DATOS_JSON` y `EVA_CALCULOS_JSON`;
- comprueba que el script `06` contenga los nombres aprobados;
- comprueba que el script `06` no conserve los nombres retirados.

---

## 3. Commits publicados

```text
f7da91d901b1f8baf2defc4a91cd14b233d681ff
refactor(matrices): alinear columnas JSON con modelo de 17 tablas

46f6f06871583ec04e57c087885f718977b22f42
test(matrices): validar columnas JSON del modelo reducido
```

---

## 4. Verificación ejecutada

Quality Gate de GitHub Actions:

```text
Run: 31041633294
Resultado: success
Commit validado: 46f6f06871583ec04e57c087885f718977b22f42
```

Etapas aprobadas:

- restauración del backend;
- instalación de dependencias frontend;
- validador de alineación dinámica;
- compilación de la solución en Release;
- instalación de Playwright Chromium;
- puertas de calidad completas del repositorio.

También se verificó directamente en el archivo publicado:

- cero referencias a `EVA_DATA_JSON`;
- cero referencias a `EVA_DATA_CALC_JSON`;
- presencia de los nombres físicos nuevos en consulta, inserción y actualización.

---

## 5. Elementos no modificados

Esta fase no adelantó trabajos pertenecientes a fases posteriores. Permanecen pendientes:

- retiro de `RL_MR_TRAZAS_CALCULO`;
- retiro de `InsertarTrazaCalculoAsync`;
- migración de `RL_MR_AUDITORIA` hacia `RL_AUDITORIA`;
- retiro de adaptadores y DTOs heredados;
- certificación Oracle de las 17 tablas.

---

## 6. Restricciones vigentes

- `main` permanece intacta;
- el PR #20 permanece abierto y en borrador;
- Oracle no fue ejecutado;
- el script `05` no fue ejecutado;
- el script `06` no fue ejecutado;
- no se ejecutaron eliminaciones físicas;
- no se publicaron credenciales.

---

## 7. Criterios de cierre

| Criterio | Resultado |
|---|---|
| Consultas usan `EVA_DATOS_JSON` y `EVA_CALCULOS_JSON` | Cumplido |
| Inserción usa los nombres físicos aprobados | Cumplido |
| Actualización usa los nombres físicos aprobados | Cumplido |
| Nombres anteriores ausentes del repositorio activo | Cumplido |
| Validador protege la alineación repositorio–script `06` | Cumplido |
| Compilación Release | Correcta |
| Quality Gates | Correctos |
| Oracle sin ejecutar | Cumplido |
| `main` intacta | Cumplido |

---

## 8. Resultado y siguiente fase

La **Fase 1 queda completada** en código y validada sin Oracle.

El siguiente trabajo autorizado es la **Fase 2 — retiro definitivo de trazas de cálculo**, que deberá:

1. eliminar las llamadas a `InsertarTrazaCalculoAsync`;
2. eliminar el método y cualquier referencia a `RL_MR_TRAZAS_CALCULO` y `SEQ_RL_MR_TRAZAS`;
3. retirar la exigencia de `TRA_REGLA_ID` del validador;
4. conservar código, versión y algoritmo de la regla dentro de `EVA_CALCULOS_JSON`;
5. actualizar pruebas y Quality Gates;
6. mantener Oracle y el script `06` bloqueados.
