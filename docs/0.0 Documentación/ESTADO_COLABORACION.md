# Estado de colaboración y punto de continuidad

> Actualización 2026-08-05: completada la Fase 3 de migración de auditoría local hacia la auditoría institucional. El backend ya no escribe en `RL_MR_AUDITORIA` ni utiliza `SEQ_RL_MR_AUDITORIA`; creación, actualización, transición y vínculos se registran mediante `IAuditoriaRepository.RegistrarAsync` en `RL_AUDITORIA`, compartiendo la misma conexión y transacción Oracle. No se ejecutaron Oracle, el script `05` ni el script `06`. `main` permanece intacta y el PR #20 continúa abierto y en borrador.

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

- **Intervención**: Fase 3 — Migración de auditoría local hacia auditoría institucional
- **Fecha**: 2026-08-05
- **Rama**: `desarrollo`
- **Estado**: completada y validada sin Oracle
- **Documento**: [`FASE_3_MIGRACION_AUDITORIA_INSTITUCIONAL_MODELO_17_TABLAS_2026-08-05.md`](../3.%20Módulo%20Matrices%20de%20Riesgos/FASE_3_MIGRACION_AUDITORIA_INSTITUCIONAL_MODELO_17_TABLAS_2026-08-05.md)

Resultados verificados:

- se eliminó `InsertarAuditoriaCampoAsync`;
- el repositorio ya no escribe en `RL_MR_AUDITORIA` ni utiliza `SEQ_RL_MR_AUDITORIA`;
- `IAuditoriaRepository` es una dependencia obligatoria de `MatricesRiesgosRepository`;
- la creación de evaluaciones registra `CREAR_EVALUACION` en la misma transacción;
- la actualización registra `ACTUALIZAR_EVALUACION` en la misma transacción;
- las transiciones registran `TRANSICION_ESTADO` en la misma transacción;
- la vinculación genérica y el adaptador temporal utilizan únicamente auditoría institucional;
- el validador impide reintroducir auditoría local o inyección opcional;
- el script `06` conserva únicamente el retiro controlado de la tabla local heredada;
- se agregaron pruebas del constructor obligatorio, ausencia del método local y contrato transaccional compartido;
- el Quality Gate 31045641517 terminó en `success`;
- el PR #20 permanece abierto y en borrador;
- continúan bloqueados Oracle y los scripts `05` y `06`.

Commits principales:

```text
973150d82d8cc71e3f6c65d4e68fa29aa9150355
refactor(matrices): migrar auditoria local a institucional [phase3-done]

490259bf06d14df5988b4064ed253c5258ed2a58
test(matrices): validar contrato transaccional de auditoria institucional

3d5722657d76d170c2abb9fda5c3214b0a7c665c
docs(matrices): cerrar fase 3 de auditoria institucional
```

---

## 3. Estado de fases del Módulo Matrices de Riesgos

| Fase | Descripción | Estado real | Detalle / pendiente |
|---|---|---|---|
| **Fase 0-R** | Aprobación funcional del modelo reducido | **Aprobada** | Modelo objetivo de 17 tablas aprobado; datos descartables confirmados como pruebas. |
| **Fase 0-C** | Congelamiento técnico y línea base del corte | **Completada** | Ramas, PR, cabeceras, Quality Gate, inventario y restricciones documentados. |
| **Fase 1** | Alineación de columnas JSON | **Completada** | Repositorio y validador usan `EVA_DATOS_JSON` y `EVA_CALCULOS_JSON`; Quality Gates correctos. |
| **Fase 2** | Retiro de trazas de cálculo | **Completada** | Trazas locales retiradas; regla, versión y algoritmo quedan dentro de `EVA_CALCULOS_JSON`; Quality Gates correctos. |
| **Fase 3** | Auditoría institucional | **Completada** | Auditoría local retirada; operaciones críticas utilizan `RL_AUDITORIA` dentro de la misma transacción; Quality Gates correctos. |
| **Fase 4** | Retiro de adaptadores y contratos heredados | **Siguiente fase** | Eliminar adaptador de aprobación, lógica de tablas puente, DTO temporal y permisos granulares huérfanos. |
| **Fase 5** | Validador exclusivo de 17 tablas | **Pendiente** | Prohibir objetos heredados y exigir inventario exacto de tablas y secuencias. |
| **Fase 6** | Pruebas automatizadas no Oracle | **Pendiente** | Actualizar Backend, Angular y E2E para el corte definitivo. |
| **Fase 7** | Suite Oracle del modelo reducido | **Pendiente** | Validar 17 tablas, 17 secuencias, índices, restricciones, commit y rollback. |
| **Fase 8** | Quality Gates completos sin Oracle | **Pendiente después del corte** | Reejecutar validador, Release, pruebas, cobertura, build Angular y E2E. |
| **Fases 9–11** | Preparación, ejecución y certificación Oracle | **Bloqueadas** | Requieren respaldo, autorización y cierre correcto de Fases 1–8. |
| **Fase 12** | Documentación y cierre técnico | **Pendiente** | No declarar terminado hasta certificar Oracle y funcionamiento integral. |

---

## 4. Bloqueantes técnicos vigentes

1. Permanecen el adaptador interno hacia `RL_MR_EVI_APROBACION` y la lógica de construcción dinámica para tablas puente heredadas.
2. Permanecen contratos temporales como `AsociarEvidenciaAprobacionDto` y `PermisoFormularioDto`.
3. El validador todavía debe evolucionar para exigir el inventario exacto de 17 tablas y 17 secuencias.
4. La prueba Oracle todavía no certifica las 17 tablas, 17 secuencias, índices, restricciones ni el ciclo completo.

Quedaron resueltos:

- la incompatibilidad de nombres `EVA_DATA_JSON`/`EVA_DATA_CALC_JSON`, corregida en la Fase 1;
- las escrituras y dependencias de `RL_MR_TRAZAS_CALCULO`, retiradas en la Fase 2;
- las escrituras y dependencias de `RL_MR_AUDITORIA`, retiradas en la Fase 3.

Los bloqueantes restantes impiden ejecutar el script `06`.

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
9. No declarar certificado el modelo reducido antes de las pruebas Oracle.
10. Cada fase debe cerrar con commit identificable, validación y documentación de resultado.

---

## 6. Punto exacto de continuación

La siguiente intervención es la **Fase 4 — retiro definitivo de adaptadores y contratos heredados**:

1. eliminar `VincularEvidenciaAprobacionAsync` del repositorio y su interfaz;
2. eliminar `AsociarEvidenciaAprobacionDto`;
3. eliminar `EjecutarVinculoEvidenciaAsync` y cualquier construcción dinámica de nombres de tablas o columnas puente;
4. retirar toda referencia activa a `RL_MR_EVI_APROBACION` y a las tablas `RL_MR_EVI_*`;
5. eliminar `PermisoFormularioDto` y cualquier consumidor residual;
6. mantener exclusivamente `VincularEvidenciaAsync` y `RL_MR_EVIDENCIAS_VINCULOS`;
7. actualizar validador, pruebas backend y pruebas Oracle preparatorias;
8. ejecutar compilación Release y Quality Gates sin conectarse a Oracle.
