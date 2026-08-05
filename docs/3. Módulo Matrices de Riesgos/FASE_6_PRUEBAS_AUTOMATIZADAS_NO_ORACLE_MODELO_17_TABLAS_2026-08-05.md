# Fase 6 — Consolidación de pruebas automatizadas no Oracle

## Módulo Matrices de Riesgos — Modelo reducido de 17 tablas

- **Fecha:** 2026-08-05.
- **Rama de trabajo:** `desarrollo`.
- **Rama estable:** `main` — no modificada.
- **Estado:** completada y validada sin conexión Oracle.

---

## 1. Objetivo

Consolidar la cobertura automatizada del corte definitivo al modelo reducido antes de preparar la certificación física Oracle, cubriendo especialmente los contratos que fueron modificados en las Fases 1 a 5:

- evaluaciones dinámicas;
- historial de flujos;
- vínculo genérico de evidencias;
- lista cerrada de siete entidades destino;
- compensación de archivos cuando falla el vínculo;
- contratos Angular y recorridos E2E;
- inventario exacto de 17 tablas y 17 secuencias.

Esta fase no autoriza ni ejecuta Oracle, el script `05` o el script `06`.

---

## 2. Línea base antes de la intervención

| Suite | Resultado inicial |
|---|---:|
| Backend | 198 pruebas aprobadas |
| Frontend | 115 pruebas aprobadas |
| E2E | 7 recorridos aprobados |
| Cobertura Backend — líneas | 16.45 % |
| Cobertura Backend — ramas | 16.80 % |
| Cobertura Frontend — sentencias | 33.30 % |
| Cobertura Frontend — ramas | 31.06 % |
| Cobertura Frontend — funciones | 30.17 % |
| Cobertura Frontend — líneas | 32.65 % |

El diagnóstico identificó tres brechas principales:

1. la lista cerrada de destinos de evidencia estaba validada como contrato, pero no se comprobaba cada consulta SQL cerrada ni todos los errores del AppService;
2. Angular tenía cobertura insuficiente sobre flujos, transición, JSON inválido y compensación de evidencia huérfana;
3. el E2E todavía simulaba la ruta retirada de revisiones y no recorría el historial canónico de flujos.

---

## 3. Pruebas Backend incorporadas

Archivo creado:

```text
backend/RL.API.Tests/Features/MatricesRiesgos/
MatricesRiesgosEvidenceNonOracleTests.cs
```

### 3.1 Resolución cerrada de entidades

Se verifican individualmente los siete destinos aprobados:

| Tipo | Tabla esperada | Identificador esperado |
|---|---|---|
| `Riesgo` | `RL_MR_RIESGOS` | `RIE_ID` |
| `Evaluacion` | `RL_MR_EVALUACIONES_RIESGO` | `EVA_ID` |
| `Control` | `RL_MR_CONTROLES_RIESGO` | `CON_ID` |
| `Plan` | `RL_MR_PLANES` | `PLA_ID` |
| `Actividad` | `RL_MR_ACTIVIDADES` | `ACT_ID` |
| `Alerta` | `RL_MR_SENALES_ALERTA` | `ALE_ID` |
| `Automonitoreo` | `RL_MR_AUTOMONITOREO` | `MON_ID` |

Cada consulta debe:

- usar una tabla conocida y codificada en servidor;
- usar una columna conocida;
- filtrar mediante el parámetro `:id`;
- rechazar cualquier valor fuera del enum permitido.

### 3.2 Comportamiento del AppService

Se añadieron casos para comprobar:

- delegación de los siete destinos al único método `VincularEvidenciaAsync`;
- propagación correcta de DTO, usuario e IP;
- respuesta HTTP 400 cuando el repositorio no crea el vínculo;
- respuesta HTTP 404 cuando no existe la evidencia o la entidad destino;
- respuesta HTTP 400 cuando el tipo de entidad no está permitido.

Resultado: **18 pruebas Backend adicionales**.

---

## 4. Pruebas Angular incorporadas

Archivo creado:

```text
frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/
matrices-riesgos/matrices-riesgos.component.workflow.spec.ts
```

Se incorporaron ocho casos:

1. cargar el historial de flujos al editar una evaluación;
2. descartar respuestas JSON que no sean un objeto;
3. ejecutar una transición, limpiar el motivo y recargar evaluación e historial;
4. mostrar el mensaje funcional cuando la transición falla;
5. cargar y vincular una evidencia mediante el contrato único;
6. eliminar la evidencia huérfana cuando falla el vínculo;
7. vaciar el historial cuando la consulta de flujos falla;
8. rechazar una definición técnica con JSON inválido sin llamar al Backend.

Resultado: **8 pruebas Frontend adicionales**.

---

## 5. Recorrido E2E incorporado

Archivo actualizado:

```text
frontend/rl-app/e2e/login-and-routing.spec.ts
```

Cambios:

- se retiró la simulación obsoleta de `/evaluaciones/{id}/revisiones`;
- se incorporó `GET /evaluaciones/{id}/flujos`;
- se incorporó `POST /evaluaciones/{id}/transiciones`;
- se añadió un recorrido que abre una evaluación, visualiza `Captura inicial`, selecciona `EN_REVISION`, envía el motivo `Captura completada` y verifica el mensaje de éxito.

Resultado: **1 recorrido E2E adicional**.

---

## 6. Resultado final de pruebas y cobertura

Quality Gate de implementación final:

```text
Run: 31053808302
Commit: b4937e42f1515203310a75cb2ca0f138d643e0c4
Resultado: success
```

| Suite | Resultado final | Variación |
|---|---:|---:|
| Backend | 216 aprobadas | +18 |
| Frontend | 123 aprobadas en 20 archivos | +8 |
| E2E | 8 aprobadas | +1 |
| Cobertura Backend — líneas | 16.72 % | +0.27 pp |
| Cobertura Backend — ramas | 17.18 % | +0.38 pp |
| Cobertura Frontend — sentencias | 34.41 % | +1.11 pp |
| Cobertura Frontend — ramas | 31.52 % | +0.46 pp |
| Cobertura Frontend — funciones | 31.69 % | +1.52 pp |
| Cobertura Frontend — líneas | 33.87 % | +1.22 pp |

Compilación Release:

```text
Build succeeded.
0 Warning(s)
0 Error(s)
```

También aprobaron:

- validador de alineación dinámica;
- inventario exacto de 17 tablas y 17 secuencias;
- nueve pruebas negativas del inventario;
- instalación de Playwright;
- build Angular;
- todas las puertas de cobertura configuradas.

---

## 7. Commits de la fase

```text
7b751e0825af82ea7991d848b0fc0979c9290de2
test(matrices): cubrir destinos y errores del vínculo genérico

ddf4bae226b38cc176f65e1f6a4b4ac0ef3a4e7c
test(matrices): cubrir flujos y compensación de evidencias Angular

f49049559d6245e7c253454f497d78bbbc7e9be9
test(matrices): cubrir historial de flujos y transición E2E

b4937e42f1515203310a75cb2ca0f138d643e0c4
test(matrices): eliminar advertencia en teoría de evidencias
```

---

## 8. Observación de dependencias fuera del alcance

Durante `npm ci`, la auditoría de npm informó 13 vulnerabilidades en el árbol global de dependencias: 6 moderadas, 6 altas y 1 crítica. Este resultado ya existía fuera del objetivo funcional de la Fase 6 y no impidió los Quality Gates actuales.

No se ejecutó `npm audit fix --force`, porque podría introducir actualizaciones incompatibles y requiere una intervención separada de seguridad de dependencias, con análisis de impacto y pruebas completas.

---

## 9. Restricciones verificadas

- `main` no fue modificada;
- el PR #20 debe permanecer abierto y en borrador;
- Oracle no fue ejecutado;
- el script `05` no fue ejecutado;
- el script `06` no fue ejecutado;
- no se eliminaron objetos físicos;
- no se versionaron secretos o cadenas de conexión.

---

## 10. Cierre y siguiente fase

La **Fase 6 queda completada** con cobertura funcional adicional y Quality Gates correctos.

La siguiente intervención es la **Fase 7 — fortalecimiento de la suite Oracle del modelo reducido**, exclusivamente en código y sin ejecutar Oracle. Deberá:

1. alinear todos los `INSERT` con las columnas obligatorias del DDL;
2. comprobar el inventario de 17 tablas y 17 secuencias;
3. validar índices y restricciones principales;
4. preparar el ciclo familia–versión–riesgo–evaluación–proyección–flujo–evidencia–vínculo–auditoría;
5. preparar escenarios de commit y rollback;
6. comprobar la ausencia de revisiones, trazas, auditoría local y tablas `RL_MR_EVI_*`;
7. mantener la ejecución protegida por `RL_ORACLE_INTEGRATION_REQUIRED`;
8. no conectarse a Oracle hasta contar con base exclusiva, respaldo y autorización expresa.
