# Estado de colaboración y punto de continuidad

> Actualización 2026-08-05: completada la Fase 6 del Módulo Matrices de Riesgos. Se consolidaron las pruebas automatizadas no Oracle del vínculo genérico de evidencias, sus siete destinos cerrados, el historial de flujos, las transiciones, la compensación de evidencias huérfanas y el recorrido E2E canónico. El resultado final es 216 pruebas Backend, 123 Frontend y 8 E2E, con compilación Release en 0 advertencias y 0 errores. No se ejecutaron Oracle, el script `05` ni el script `06`. `main` permanece intacta y el PR #20 continúa abierto y en borrador.

Documento vivo. Debe actualizarse al finalizar cada intervención junto con `BITACORA_COLABORACION.md` cuando corresponda.

---

## 1. Línea base vigente

- **Repositorio**: `jmejia31/RIESGO_LAVADO`
- **Rama de trabajo obligatoria**: `desarrollo`
- **Rama estable**: `main` — no modificar ni integrar sin autorización expresa de Javier Mejía
- **Ramas remotas permitidas**: únicamente `main` y `desarrollo`
- **Aprobador final**: Javier Mejía (`jmejia31`)
- **PR de revisión**: #20, abierto, borrador y sin autorización de fusión
- **Arquitectura**: monolito modular con Angular, ASP.NET Core y Oracle 11g

---

## 2. Última intervención

- **Intervención**: Fase 6 — Consolidación de pruebas automatizadas no Oracle
- **Fecha**: 2026-08-05
- **Rama**: `desarrollo`
- **Estado**: completada y validada sin Oracle
- **Documento**: [`FASE_6_PRUEBAS_AUTOMATIZADAS_NO_ORACLE_MODELO_17_TABLAS_2026-08-05.md`](../3.%20Módulo%20Matrices%20de%20Riesgos/FASE_6_PRUEBAS_AUTOMATIZADAS_NO_ORACLE_MODELO_17_TABLAS_2026-08-05.md)

Resultados verificados:

- se añadió cobertura Backend para las siete entidades permitidas del vínculo genérico;
- cada destino se resuelve mediante tabla, columna y parámetro `:id` cerrados en servidor;
- se comprueba el rechazo de tipos fuera del enum permitido;
- el AppService prueba respuestas 400 y 404 para errores controlados;
- Angular cubre carga de flujos, transición correcta y fallida, JSON inválido y compensación de evidencia huérfana;
- el E2E dejó de simular la ruta retirada de revisiones;
- el E2E consume `/evaluaciones/{id}/flujos` y prueba `/transiciones`;
- Backend pasó de 198 a 216 pruebas;
- Frontend pasó de 115 a 123 pruebas;
- E2E pasó de 7 a 8 recorridos;
- cobertura Backend: líneas 16.72 %, ramas 17.18 %;
- cobertura Frontend: sentencias 34.41 %, ramas 31.52 %, funciones 31.69 %, líneas 33.87 %;
- compilación Release: 0 advertencias y 0 errores;
- el Quality Gate 31053808302 terminó en `success` sobre `b4937e42f1515203310a75cb2ca0f138d643e0c4`;
- el PR #20 permanece abierto y en borrador;
- continúan bloqueados Oracle y los scripts `05` y `06`.

Commits principales:

```text
7b751e0825af82ea7991d848b0fc0979c9290de2
test(matrices): cubrir destinos y errores del vínculo genérico

ddf4bae226b38cc176f65e1f6a4b4ac0ef3a4e7c
test(matrices): cubrir flujos y compensación de evidencias Angular

f49049559d6245e7c253454f497d78bbbc7e9be9
test(matrices): cubrir historial de flujos y transición E2E

b4937e42f1515203310a75cb2ca0f138d643e0c4
test(matrices): eliminar advertencia en teoría de evidencias

a11df1bbb925000100e4c905a016a1d597d32430
docs(matrices): cerrar fase 6 de pruebas no Oracle
```

---

## 3. Estado de fases del Módulo Matrices de Riesgos

| Fase | Descripción | Estado real | Detalle / pendiente |
|---|---|---|---|
| **Fase 0-R** | Aprobación funcional del modelo reducido | **Aprobada** | Modelo objetivo de 17 tablas aprobado; datos descartables confirmados como pruebas. |
| **Fase 0-C** | Congelamiento técnico y línea base del corte | **Completada** | Ramas, PR, Quality Gate, inventario y restricciones documentados. |
| **Fase 1** | Alineación de columnas JSON | **Completada** | Repositorio y validador usan `EVA_DATOS_JSON` y `EVA_CALCULOS_JSON`. |
| **Fase 2** | Retiro de trazas de cálculo | **Completada** | Trazas locales retiradas; metadatos de reglas conservados en JSON. |
| **Fase 3** | Auditoría institucional | **Completada** | Operaciones críticas utilizan `RL_AUDITORIA` en la misma transacción. |
| **Fase 4** | Retiro de adaptadores y contratos heredados | **Completada** | Adaptadores, DTO temporal, helper dinámico, tablas puente activas y permisos granulares retirados. |
| **Fase 5** | Inventario exacto de 17 tablas y 17 secuencias | **Completada** | Manifiesto, validador, nueve pruebas negativas y CI obligatoria aprobados. |
| **Fase 6** | Pruebas automatizadas no Oracle | **Completada** | 216 Backend, 123 Frontend y 8 E2E; cobertura incrementada; Release sin advertencias. |
| **Fase 7** | Fortalecimiento de la suite Oracle reducida | **Siguiente fase** | Preparar inventario físico, restricciones y ciclo completo, sin conectarse a Oracle. |
| **Fase 8** | Quality Gates completos previos a Oracle | **Pendiente** | Reejecutar todas las puertas sobre la suite Oracle preparada antes de solicitar autorización. |
| **Fases 9–11** | Preparación, ejecución y certificación Oracle | **Bloqueadas** | Requieren base exclusiva, respaldo, autorización y cierre correcto de Fases 1–8. |
| **Fase 12** | Documentación y cierre técnico | **Pendiente** | No declarar terminado hasta certificar Oracle y funcionamiento integral. |

---

## 4. Bloqueantes técnicos vigentes

1. La suite Oracle todavía no certifica las 17 tablas, 17 secuencias, índices y restricciones en un esquema real.
2. El `INSERT` de riesgo de la suite Oracle debe alinearse con `RIE_NOMBRE`, `RIE_USR_CREACION` y las demás columnas obligatorias.
3. Falta preparar el ciclo familia–versión–riesgo–evaluación–proyección–flujo–evidencia–vínculo–auditoría.
4. Faltan escenarios Oracle explícitos de commit y rollback para el modelo reducido.
5. El script `06` continúa bloqueado hasta completar las fases previas y obtener respaldo y autorización expresa.
6. `npm ci` informa 13 vulnerabilidades globales de dependencias; requieren una intervención separada de seguridad y no deben corregirse con `--force` sin análisis de impacto.

Quedaron resueltos:

- nombres físicos JSON incompatibles, Fase 1;
- trazas locales de cálculo, Fase 2;
- auditoría local, Fase 3;
- adaptadores, contratos y tablas puente activas heredadas, Fase 4;
- control exacto del inventario DDL, Fase 5;
- brechas no Oracle del vínculo genérico, flujos y E2E, Fase 6.

---

## 5. Directrices y restricciones activas

1. Trabajar únicamente en `desarrollo`.
2. No modificar ni fusionar `main`.
3. Mantener el PR #20 abierto y en borrador.
4. No habilitar auto-merge.
5. No ejecutar Oracle.
6. No ejecutar los scripts `05` o `06`.
7. No retirar tablas físicamente ni ejecutar `DROP TABLE`.
8. No versionar credenciales o cadenas de conexión.
9. No declarar certificado el modelo reducido antes de las pruebas Oracle reales.
10. Cada fase debe cerrar con commit identificable, validación y documentación de resultado.

---

## 6. Punto exacto de continuación

La siguiente intervención es la **Fase 7 — fortalecimiento de la suite Oracle del modelo reducido**, todavía sin conectarse a Oracle:

1. revisar completamente `MatricesRiesgosRepositoryIntegrationTests.cs`;
2. alinear todos los `INSERT` con las columnas obligatorias del script `06`;
3. declarar el inventario de 17 tablas y 17 secuencias esperado por la prueba;
4. preparar validación de índices, claves primarias, foráneas, únicas y `CHECK` principales;
5. preparar el ciclo funcional completo desde familia hasta auditoría institucional;
6. preparar pruebas de vínculo para los siete tipos permitidos;
7. preparar commit conjunto de operación funcional y auditoría;
8. preparar rollback cuando falle la auditoría o el vínculo;
9. comprobar ausencia física de revisiones, trazas, auditoría local y tablas `RL_MR_EVI_*`;
10. mantener `RL_ORACLE_INTEGRATION_REQUIRED` como bloqueo obligatorio;
11. ejecutar únicamente compilación y Quality Gates no Oracle;
12. no ejecutar la suite Oracle, el script `05` o el script `06` hasta autorización expresa.
