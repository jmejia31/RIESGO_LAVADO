# Estado de colaboración y punto de continuidad

> Actualización 2026-08-05: completada la Fase 1 de alineación del contrato físico JSON para el modelo de 17 tablas. El repositorio usa `EVA_DATOS_JSON` y `EVA_CALCULOS_JSON`; el validador protege la compatibilidad con el script `06`. No se ejecutaron Oracle, el script `05` ni el script `06`. `main` permanece intacta y el PR #20 continúa abierto y en borrador.

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

- **Intervención**: Fase 1 — Alineación del contrato físico y columnas JSON
- **Fecha**: 2026-08-05
- **Rama**: `desarrollo`
- **Estado**: completada y validada sin Oracle
- **Documento**: [`FASE_1_ALINEACION_COLUMNAS_JSON_MODELO_17_TABLAS_2026-08-05.md`](../3.%20Módulo%20Matrices%20de%20Riesgos/FASE_1_ALINEACION_COLUMNAS_JSON_MODELO_17_TABLAS_2026-08-05.md)

Resultados verificados:

- el repositorio utiliza exclusivamente `EVA_DATOS_JSON` y `EVA_CALCULOS_JSON` para la persistencia física de evaluaciones;
- se corrigieron consultas, listado paginado, inserción, lectura de concurrencia y actualización;
- el validador rechaza los nombres físicos retirados y exige los nombres aprobados;
- el validador comprueba la alineación con `06_reconstruir_modelo_17_tablas.sql`;
- el Quality Gate 31041633294 terminó en `success`;
- el PR #20 permanece abierto y en borrador;
- continúan bloqueados Oracle y los scripts `05` y `06`.

Commits principales:

```text
f7da91d901b1f8baf2defc4a91cd14b233d681ff
refactor(matrices): alinear columnas JSON con modelo de 17 tablas

46f6f06871583ec04e57c087885f718977b22f42
test(matrices): validar columnas JSON del modelo reducido

2ad71dca3e35b6c2f552fe81476c1a8b8d4c5f08
docs(matrices): cerrar fase 1 de alineacion JSON
```

---

## 3. Estado de fases del Módulo Matrices de Riesgos

| Fase | Descripción | Estado real | Detalle / pendiente |
|---|---|---|---|
| **Fase 0-R** | Aprobación funcional del modelo reducido | **Aprobada** | Modelo objetivo de 17 tablas aprobado; datos descartables confirmados como pruebas. |
| **Fase 0-C** | Congelamiento técnico y línea base del corte | **Completada** | Ramas, PR, cabeceras, Quality Gate, inventario y restricciones documentados. |
| **Fase 1** | Alineación de columnas JSON | **Completada** | Repositorio y validador usan `EVA_DATOS_JSON` y `EVA_CALCULOS_JSON`; Quality Gates correctos. |
| **Fase 2** | Retiro de trazas de cálculo | **Siguiente fase** | Eliminar `RL_MR_TRAZAS_CALCULO`, secuencia, método y llamadas; conservar metadatos de regla en cálulos JSON. |
| **Fase 3** | Auditoría institucional | **Pendiente** | Sustituir `RL_MR_AUDITORIA` por `RL_AUDITORIA` dentro de la misma transacción. |
| **Fase 4** | Retiro de adaptadores y contratos heredados | **Pendiente** | Eliminar adaptador de aprobación, tablas puente, DTO temporal y permisos granulares huérfanos. |
| **Fase 5** | Validador exclusivo de 17 tablas | **Pendiente** | Prohibir objetos heredados y exigir inventario exacto de tablas y secuencias. |
| **Fase 6** | Pruebas automatizadas no Oracle | **Pendiente** | Actualizar Backend, Angular y E2E para el corte definitivo. |
| **Fase 7** | Suite Oracle del modelo reducido | **Pendiente** | Validar 17 tablas, 17 secuencias, índices, restricciones, commit y rollback. |
| **Fase 8** | Quality Gates completos sin Oracle | **Pendiente después del corte** | Reejecutar validador, Release, pruebas, cobertura, build Angular y E2E. |
| **Fases 9–11** | Preparación, ejecución y certificación Oracle | **Bloqueadas** | Requieren respaldo, autorización y cierre correcto de Fases 1–8. |
| **Fase 12** | Documentación y cierre técnico | **Pendiente** | No declarar terminado hasta certificar Oracle y funcionamiento integral. |

---

## 4. Bloqueantes técnicos vigentes

1. El repositorio todavía conserva escrituras y llamadas relacionadas con `RL_MR_TRAZAS_CALCULO`.
2. El repositorio todavía conserva escrituras relacionadas con `RL_MR_AUDITORIA` y `SEQ_RL_MR_AUDITORIA`.
3. Permanecen adaptadores internos hacia `RL_MR_EVI_APROBACION` y lógica de tablas puente heredadas.
4. Permanecen contratos temporales como `AsociarEvidenciaAprobacionDto` y `PermisoFormularioDto`.
5. La prueba Oracle todavía no certifica las 17 tablas, 17 secuencias, índices, restricciones ni el ciclo completo.

La incompatibilidad de nombres `EVA_DATA_JSON`/`EVA_DATA_CALC_JSON` quedó resuelta en la Fase 1.

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

La siguiente intervención es la **Fase 2 — retiro definitivo de trazas de cálculo**:

1. eliminar las llamadas a `InsertarTrazaCalculoAsync` en creación y actualización;
2. eliminar el método `InsertarTrazaCalculoAsync`;
3. retirar referencias a `RL_MR_TRAZAS_CALCULO`, `SEQ_RL_MR_TRAZAS` y `TRA_REGLA_ID`;
4. conservar en `EVA_CALCULOS_JSON` el código, versión y algoritmo de la regla utilizada;
5. actualizar el validador y las pruebas;
6. ejecutar compilación Release y Quality Gates sin conectarse a Oracle.
