# Estado de colaboración y punto de continuidad

> Actualización 2026-08-05: completada la Fase 2 de retiro definitivo de trazas de cálculo para el modelo de 17 tablas. El backend ya no escribe en `RL_MR_TRAZAS_CALCULO`; el código, versión y algoritmo de la regla se conservan dentro de `EVA_CALCULOS_JSON` con valores controlados por el servidor. No se ejecutaron Oracle, el script `05` ni el script `06`. `main` permanece intacta y el PR #20 continúa abierto y en borrador.

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

- **Intervención**: Fase 2 — Retiro definitivo de trazas de cálculo
- **Fecha**: 2026-08-05
- **Rama**: `desarrollo`
- **Estado**: completada y validada sin Oracle
- **Documento**: [`FASE_2_RETIRO_TRAZAS_CALCULO_MODELO_17_TABLAS_2026-08-05.md`](../3.%20Módulo%20Matrices%20de%20Riesgos/FASE_2_RETIRO_TRAZAS_CALCULO_MODELO_17_TABLAS_2026-08-05.md)

Resultados verificados:

- se eliminaron las llamadas de traza durante la creación y actualización de evaluaciones;
- se eliminó `InsertarTrazaCalculoAsync`;
- el código activo ya no depende de `RL_MR_TRAZAS_CALCULO`, `SEQ_RL_MR_TRAZAS` ni `TRA_REGLA_ID`;
- la regla se valida contra la versión publicada y `RL_MR_REGLAS_CALCULO`;
- `REG_ALGORITMO_ID` se obtiene desde el catálogo institucional;
- `reglaCodigo`, `reglaVersion` y `algoritmoId` se incorporan en `EVA_CALCULOS_JSON`;
- cualquier metadato de regla enviado por el cliente se sobrescribe con el valor institucional;
- el validador prohíbe la reintroducción de trazas en backend, pruebas y frontend;
- el Quality Gate 31043691118 terminó en `success`;
- el PR #20 permanece abierto y en borrador;
- continúan bloqueados Oracle y los scripts `05` y `06`.

Commits principales:

```text
fab207abf4eec51a9d2adf02a4906e49907d6859
refactor(matrices): retirar trazas y persistir metadatos de regla

1afa5910a3b00d2d1e5f511a7f657d32304f88cb
test(matrices): prohibir trazas y exigir metadatos de regla

1014746fe22204e7e7cf4c585e3bf9be90916e12
test(matrices): cubrir metadatos institucionales de calculo

56b1913c6abc65dc99eae73be4355a41a6170a82
test(matrices): compatibilizar prueba de metadatos de calculo

6fe8fbb809defaeec18f77fee456a5d0d9311c47
docs(matrices): cerrar fase 2 de retiro de trazas
```

---

## 3. Estado de fases del Módulo Matrices de Riesgos

| Fase | Descripción | Estado real | Detalle / pendiente |
|---|---|---|---|
| **Fase 0-R** | Aprobación funcional del modelo reducido | **Aprobada** | Modelo objetivo de 17 tablas aprobado; datos descartables confirmados como pruebas. |
| **Fase 0-C** | Congelamiento técnico y línea base del corte | **Completada** | Ramas, PR, cabeceras, Quality Gate, inventario y restricciones documentados. |
| **Fase 1** | Alineación de columnas JSON | **Completada** | Repositorio y validador usan `EVA_DATOS_JSON` y `EVA_CALCULOS_JSON`; Quality Gates correctos. |
| **Fase 2** | Retiro de trazas de cálculo | **Completada** | Trazas locales retiradas; regla, versión y algoritmo quedan dentro de `EVA_CALCULOS_JSON`; Quality Gates correctos. |
| **Fase 3** | Auditoría institucional | **Siguiente fase** | Sustituir `RL_MR_AUDITORIA` por `RL_AUDITORIA` dentro de la misma transacción. |
| **Fase 4** | Retiro de adaptadores y contratos heredados | **Pendiente** | Eliminar adaptador de aprobación, tablas puente, DTO temporal y permisos granulares huérfanos. |
| **Fase 5** | Validador exclusivo de 17 tablas | **Pendiente** | Prohibir objetos heredados y exigir inventario exacto de tablas y secuencias. |
| **Fase 6** | Pruebas automatizadas no Oracle | **Pendiente** | Actualizar Backend, Angular y E2E para el corte definitivo. |
| **Fase 7** | Suite Oracle del modelo reducido | **Pendiente** | Validar 17 tablas, 17 secuencias, índices, restricciones, commit y rollback. |
| **Fase 8** | Quality Gates completos sin Oracle | **Pendiente después del corte** | Reejecutar validador, Release, pruebas, cobertura, build Angular y E2E. |
| **Fases 9–11** | Preparación, ejecución y certificación Oracle | **Bloqueadas** | Requieren respaldo, autorización y cierre correcto de Fases 1–8. |
| **Fase 12** | Documentación y cierre técnico | **Pendiente** | No declarar terminado hasta certificar Oracle y funcionamiento integral. |

---

## 4. Bloqueantes técnicos vigentes

1. El repositorio todavía conserva escrituras relacionadas con `RL_MR_AUDITORIA` y `SEQ_RL_MR_AUDITORIA`.
2. Permanecen adaptadores internos hacia `RL_MR_EVI_APROBACION` y lógica de tablas puente heredadas.
3. Permanecen contratos temporales como `AsociarEvidenciaAprobacionDto` y `PermisoFormularioDto`.
4. La prueba Oracle todavía no certifica las 17 tablas, 17 secuencias, índices, restricciones ni el ciclo completo.

Quedaron resueltos:

- la incompatibilidad de nombres `EVA_DATA_JSON`/`EVA_DATA_CALC_JSON`, corregida en la Fase 1;
- las escrituras y dependencias de `RL_MR_TRAZAS_CALCULO`, retiradas en la Fase 2.

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

La siguiente intervención es la **Fase 3 — migración de auditoría local hacia la auditoría institucional**:

1. eliminar `InsertarAuditoriaCampoAsync`;
2. retirar todas las escrituras a `RL_MR_AUDITORIA` y `SEQ_RL_MR_AUDITORIA`;
3. utilizar `IAuditoriaRepository.RegistrarAsync` con la misma `OracleConnection` y `OracleTransaction` de la operación principal;
4. cubrir creación de evaluación, actualización, transición de estado y cualquier vínculo que todavía dependa de auditoría local;
5. registrar tabla, identificador, acción, valores anteriores y nuevos, usuario, IP y módulo institucional;
6. actualizar el validador y las pruebas automatizadas;
7. ejecutar compilación Release y Quality Gates sin conectarse a Oracle.
