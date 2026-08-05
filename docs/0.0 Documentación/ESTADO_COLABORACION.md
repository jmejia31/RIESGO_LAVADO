# Estado de colaboración y punto de continuidad

> Actualización 2026-08-05: completada la Fase 4 de retiro definitivo de adaptadores y contratos heredados. El módulo conserva exclusivamente `VincularEvidenciaAsync` y `RL_MR_EVIDENCIAS_VINCULOS`; fueron retirados el adaptador de aprobación, el DTO temporal, el helper de tablas puente y `PermisoFormularioDto`. El Quality Gate institucional aprobó validador, Release, pruebas Backend, Frontend, cobertura, build Angular y E2E. No se ejecutaron Oracle, el script `05` ni el script `06`. `main` permanece intacta y el PR #20 continúa abierto y en borrador.

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

- **Intervención**: Fase 4 — Retiro definitivo de adaptadores y contratos heredados
- **Fecha**: 2026-08-05
- **Rama**: `desarrollo`
- **Estado**: completada y validada sin Oracle
- **Documento**: [`FASE_4_RETIRO_ADAPTADORES_CONTRATOS_HEREDADOS_MODELO_17_TABLAS_2026-08-05.md`](../3.%20Módulo%20Matrices%20de%20Riesgos/FASE_4_RETIRO_ADAPTADORES_CONTRATOS_HEREDADOS_MODELO_17_TABLAS_2026-08-05.md)

Resultados verificados:

- se eliminó `VincularEvidenciaAprobacionAsync`;
- se eliminó `AsociarEvidenciaAprobacionDto`;
- se eliminó `EjecutarVinculoEvidenciaAsync`;
- se retiró la referencia activa a `RL_MR_EVI_APROBACION`;
- se eliminó `PermisoFormularioDto.cs`;
- se retiró la construcción dinámica de nombres de tablas y columnas puente;
- `IMatricesRiesgosRepository` y su implementación conservan únicamente `VincularEvidenciaAsync`;
- el vínculo funcional utiliza exclusivamente `RL_MR_EVIDENCIAS_VINCULOS`;
- `TipoEntidadEvidencia` conserva siete destinos cerrados;
- el validador impide reintroducir adaptadores, DTO, permisos granulares y tablas puente específicas;
- se agregaron pruebas de contrato para el vínculo genérico y la ausencia de tipos retirados;
- el Quality Gate 31048708788 terminó en `success` sobre `9096e5f56dbc66d879043e1b3b66bca0c75898ed`;
- el PR #20 permanece abierto y en borrador;
- continúan bloqueados Oracle y los scripts `05` y `06`.

Commits principales:

```text
47e880b1e17205ae2f00864e32426142e0b1eb22
refactor(matrices): retirar adaptadores y contratos heredados [phase4-done]

9096e5f56dbc66d879043e1b3b66bca0c75898ed
docs(matrices): registrar implementacion de fase 4

e4604f72edfd441712c9aed75e7dc403bee72056
docs(matrices): cerrar fase 4 de contratos heredados
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
| **Fase 4** | Retiro de adaptadores y contratos heredados | **Completada** | Adaptador, DTO temporal, helper dinámico, tablas puente activas y permisos granulares retirados; Quality Gates correctos. |
| **Fase 5** | Validador exclusivo de 17 tablas y secuencias | **Siguiente fase** | Exigir inventario exacto y fallar ante ausencias, objetos adicionales o reintroducción heredada. |
| **Fase 6** | Pruebas automatizadas no Oracle | **Pendiente** | Consolidar cobertura Backend, Angular y E2E del corte definitivo. |
| **Fase 7** | Suite Oracle del modelo reducido | **Pendiente** | Validar 17 tablas, 17 secuencias, índices, restricciones, ciclo completo, commit y rollback. |
| **Fase 8** | Quality Gates completos sin Oracle | **Pendiente después del corte** | Reejecutar todas las puertas sobre el corte completo antes de autorizar Oracle. |
| **Fases 9–11** | Preparación, ejecución y certificación Oracle | **Bloqueadas** | Requieren respaldo, autorización y cierre correcto de Fases 1–8. |
| **Fase 12** | Documentación y cierre técnico | **Pendiente** | No declarar terminado hasta certificar Oracle y funcionamiento integral. |

---

## 4. Bloqueantes técnicos vigentes

1. El validador aún debe comparar el inventario exacto de las 17 tablas y 17 secuencias contra el DDL objetivo.
2. La prueba Oracle todavía no certifica las 17 tablas, 17 secuencias, índices, restricciones ni el ciclo completo evaluación–proyección–flujo–evidencia–auditoría.
3. El `INSERT` de riesgo de la suite Oracle debe alinearse con todas las columnas obligatorias del DDL reducido.
4. La certificación física Oracle continúa pendiente y el script `06` permanece bloqueado.

Quedaron resueltos:

- nombres físicos JSON incompatibles, Fase 1;
- trazas locales de cálculo, Fase 2;
- auditoría local, Fase 3;
- adaptadores, contratos y tablas puente activas heredadas, Fase 4.

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

La siguiente intervención es la **Fase 5 — validador exclusivo del inventario exacto de 17 tablas y 17 secuencias**:

1. declarar una lista cerrada con las 17 tablas aprobadas;
2. extraer del script `06` únicamente las sentencias activas `CREATE TABLE RL_MR_*`;
3. exigir que el conjunto del DDL coincida exactamente con las 17 tablas;
4. fallar cuando falte una tabla o aparezca una tabla número 18;
5. declarar una lista cerrada con las 17 secuencias aprobadas;
6. exigir coincidencia exacta entre el DDL y las 17 secuencias;
7. prohibir secuencias y tablas retiradas;
8. comprobar que los nombres heredados aparezcan solamente en la sección autorizada de retiro del script de transición;
9. agregar pruebas del propio validador o fixtures controlados para demostrar que detecta ausencias y objetos adicionales;
10. ejecutar compilación Release y Quality Gates sin conectarse a Oracle.
