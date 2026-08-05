# Estado de colaboración y punto de continuidad

> Actualización 2026-08-05: completada la Fase 5 del Módulo Matrices de Riesgos. El DDL de transición queda protegido por un manifiesto cerrado y un validador obligatorio que exige exactamente 17 tablas y 17 secuencias, rechaza faltantes, elementos adicionales, duplicados y objetos heredados activos. La suite negativa de nueve casos y el Quality Gate completo terminaron correctamente. No se ejecutaron Oracle, el script `05` ni el script `06`. `main` permanece intacta y el PR #20 continúa abierto y en borrador.

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

- **Intervención**: Fase 5 — Validador exclusivo del inventario exacto
- **Fecha**: 2026-08-05
- **Rama**: `desarrollo`
- **Estado**: completada y validada sin Oracle
- **Documento**: [`FASE_5_VALIDADOR_INVENTARIO_EXACTO_17_TABLAS_17_SECUENCIAS_2026-08-05.md`](../3.%20Módulo%20Matrices%20de%20Riesgos/FASE_5_VALIDADOR_INVENTARIO_EXACTO_17_TABLAS_17_SECUENCIAS_2026-08-05.md)

Resultados verificados:

- se creó `modelo_17_objetos.json` como fuente única de verdad;
- el manifiesto declara exactamente 17 tablas activas;
- el manifiesto declara exactamente 17 secuencias activas;
- se creó `validate_matrices_17_object_inventory.ps1`;
- el validador extrae solamente sentencias `CREATE` activas, excluyendo comentarios;
- se comparan conjuntos exactos y se detectan faltantes, adicionales y duplicados;
- la sección de retiro debe contener las 17 tablas vigentes y las 18 heredadas declaradas;
- las secuencias se retiran genéricamente mediante `SEQ_RL_MR_%`;
- objetos heredados fuera del retiro controlado producen fallo;
- se creó `test_matrices_17_object_inventory.ps1` con nueve fixtures;
- la etapa se integró como obligatoria en `.github/workflows/quality-gates.yml`;
- el script `06` no fue modificado ni ejecutado;
- el Quality Gate 31050344761 terminó en `success` sobre `4140734b2b46ebadcd603de0be97221becc93e7b`;
- el PR #20 permanece abierto y en borrador;
- continúan bloqueados Oracle y los scripts `05` y `06`.

Commits principales:

```text
370cad4a3f0023361556e6d800eb6904840fc1de
feat(matrices): validar inventario exacto de 17 objetos

4140734b2b46ebadcd603de0be97221becc93e7b
fix(matrices): normalizar salida de pruebas del inventario

1e5744028ccd50df628fb9b4d3d8b7d655e6a5c5
docs(matrices): cerrar fase 5 de inventario exacto
```

---

## 3. Estado de fases del Módulo Matrices de Riesgos

| Fase | Descripción | Estado real | Detalle / pendiente |
|---|---|---|---|
| **Fase 0-R** | Aprobación funcional del modelo reducido | **Aprobada** | Modelo objetivo de 17 tablas aprobado; datos descartables confirmados como pruebas. |
| **Fase 0-C** | Congelamiento técnico y línea base del corte | **Completada** | Ramas, PR, cabeceras, Quality Gate, inventario y restricciones documentados. |
| **Fase 1** | Alineación de columnas JSON | **Completada** | Repositorio y validador usan `EVA_DATOS_JSON` y `EVA_CALCULOS_JSON`. |
| **Fase 2** | Retiro de trazas de cálculo | **Completada** | Trazas locales retiradas; metadatos de reglas conservados en JSON. |
| **Fase 3** | Auditoría institucional | **Completada** | Operaciones críticas utilizan `RL_AUDITORIA` en la misma transacción. |
| **Fase 4** | Retiro de adaptadores y contratos heredados | **Completada** | Adaptador, DTO temporal, helper dinámico, tablas puente activas y permisos granulares retirados. |
| **Fase 5** | Inventario exacto de 17 tablas y 17 secuencias | **Completada** | Manifiesto, validador, nueve pruebas negativas y CI obligatoria aprobados. |
| **Fase 6** | Pruebas automatizadas no Oracle | **Siguiente fase** | Consolidar cobertura funcional Backend, Angular y E2E del corte definitivo. |
| **Fase 7** | Suite Oracle del modelo reducido | **Pendiente** | Validar inventario físico, índices, restricciones, transacciones, commit y rollback. |
| **Fase 8** | Quality Gates completos sin Oracle | **Pendiente después del corte** | Reejecutar todas las puertas antes de autorizar Oracle. |
| **Fases 9–11** | Preparación, ejecución y certificación Oracle | **Bloqueadas** | Requieren respaldo, autorización y cierre correcto de Fases 1–8. |
| **Fase 12** | Documentación y cierre técnico | **Pendiente** | No declarar terminado hasta certificar Oracle y funcionamiento integral. |

---

## 4. Bloqueantes técnicos vigentes

1. Falta consolidar las pruebas no Oracle del comportamiento funcional completo del corte definitivo.
2. La suite Oracle aún no certifica las 17 tablas, 17 secuencias, índices y restricciones en el esquema real.
3. El `INSERT` de riesgo de la suite Oracle debe alinearse con todas las columnas obligatorias del DDL reducido.
4. Falta probar el ciclo completo evaluación–proyección–flujo–evidencia–auditoría con commit y rollback reales.
5. El script `06` continúa bloqueado hasta completar las fases previas y obtener respaldo y autorización expresa.

Quedaron resueltos:

- nombres físicos JSON incompatibles, Fase 1;
- trazas locales de cálculo, Fase 2;
- auditoría local, Fase 3;
- adaptadores, contratos y tablas puente activas heredadas, Fase 4;
- ausencia de control exacto del inventario DDL, Fase 5.

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

La siguiente intervención es la **Fase 6 — consolidación de pruebas automatizadas no Oracle**:

1. inventariar las pruebas Backend, Angular y E2E existentes del módulo;
2. identificar operaciones del modelo reducido sin cobertura directa;
3. agregar pruebas de contratos, validaciones, transacciones simuladas y errores controlados;
4. cubrir creación y actualización de evaluaciones con metadatos de regla;
5. cubrir transición de estados y proyección tipada;
6. cubrir vínculo genérico de evidencias para los siete destinos;
7. cubrir rechazo de tipos o entidades inválidas;
8. cubrir metodología dinámica y consolidado tipado;
9. reforzar pruebas Angular del diseñador, captura dinámica, reportería y evidencias;
10. reforzar E2E de los recorridos críticos sin conectarse a Oracle;
11. revisar cobertura y ajustar umbrales únicamente con evidencia;
12. ejecutar compilación Release y Quality Gates completos.
