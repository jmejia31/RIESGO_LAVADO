# Estado de colaboración y punto de continuidad

> Actualización 2026-08-06: completada la Fase 7 del Módulo Matrices de Riesgos. La suite Oracle del modelo reducido quedó fortalecida y alineada con las 17 tablas: inventario físico, secuencias, índices, restricciones, ciclo completo, commit y rollback. El validador dinámico es ahora realmente vinculante en CI. El Quality Gate definitivo aprobó 222 pruebas Backend, 123 Frontend y 8 E2E, con Release en 0 advertencias y 0 errores. Oracle y los scripts `05` y `06` no fueron ejecutados. `main` permanece intacta y el PR #20 continúa abierto y en borrador.

Documento vivo. Debe actualizarse al finalizar cada intervención.

---

## 1. Línea base vigente

- **Repositorio:** `jmejia31/RIESGO_LAVADO`
- **Rama obligatoria:** `desarrollo`
- **Rama estable:** `main` — no modificar ni integrar sin autorización expresa de Javier Mejía
- **Ramas remotas permitidas:** únicamente `main` y `desarrollo`
- **PR de revisión:** #20, abierto, borrador y sin autorización de fusión
- **Arquitectura:** monolito modular con Angular, ASP.NET Core y Oracle 11g

---

## 2. Última intervención

- **Intervención:** Fase 7 — Fortalecimiento de la suite Oracle del modelo reducido
- **Fecha:** 2026-08-06
- **Rama:** `desarrollo`
- **Estado:** completada y validada en código, sin Oracle
- **Documento:** [`FASE_7_SUITE_ORACLE_MODELO_17_TABLAS_2026-08-06.md`](../3.%20Módulo%20Matrices%20de%20Riesgos/FASE_7_SUITE_ORACLE_MODELO_17_TABLAS_2026-08-06.md)

Resultados principales:

- se corrigió el `INSERT` de riesgos para incluir `RIE_NOMBRE` y `RIE_USR_CREACION`;
- la suite declara exactamente 17 tablas y 17 secuencias activas;
- se comprueba la ausencia de 18 tablas y 3 secuencias heredadas;
- se validan 16 índices funcionales y restricciones principales;
- se preparó el ciclo familia–versión–riesgo–evaluación–proyección–flujo–evidencia–vínculo–auditoría;
- se preparó commit del ciclo completo;
- se preparó rollback del ciclo base;
- se preparó rollback conjunto de vínculo y auditoría;
- la ejecución permanece protegida por `RL_ORACLE_INTEGRATION_REQUIRED=true`;
- la conexión solo puede proceder de variables de entorno o User Secrets;
- se valida `CURRENT_SCHEMA = RIESGO_LAVADO`;
- no existe ejecución automática de DDL o scripts;
- se agregaron pruebas de contrato no Oracle para la suite de certificación;
- Backend pasó de 216 a 222 pruebas;
- Frontend mantiene 123 pruebas;
- E2E mantiene 8 recorridos;
- Release terminó con 0 advertencias y 0 errores;
- el Quality Gate 31110675047 terminó en `success` sobre `3660033014014de01ff2c0f8852423c833bbfd03`;
- Oracle y los scripts `05` y `06` permanecen bloqueados.

Commits principales:

```text
c7ec0fef5cb9907f96c7a74e59f9d3ea74ede771
test(matrices): fortalecer certificacion Oracle del modelo 17

0c515fea338a0f106d3186606428ada6deaccf1f
ci(matrices): hacer vinculante el validador dinamico

75e70ced4c7b474ba8c4f89bf5c5ae705629511b
test(matrices): alinear validador con certificacion Oracle fase 7

8d09af9fee3b28e2dea2c2149821686d79f09638
fix(matrices): normalizar salida exitosa del validador

d17bc66c471c79ff8a0e5381957fa10492043de1
docs(matrices): cerrar fase 7 de suite Oracle
```

---

## 3. Estado de fases del Módulo Matrices de Riesgos

| Fase | Descripción | Estado real |
|---|---|---|
| **Fase 0-R** | Aprobación funcional del modelo reducido | **Aprobada** |
| **Fase 0-C** | Congelamiento técnico y línea base | **Completada** |
| **Fase 1** | Alineación de columnas JSON | **Completada** |
| **Fase 2** | Retiro de trazas de cálculo | **Completada** |
| **Fase 3** | Auditoría institucional | **Completada** |
| **Fase 4** | Retiro de adaptadores y contratos heredados | **Completada** |
| **Fase 5** | Inventario exacto de 17 tablas y 17 secuencias | **Completada** |
| **Fase 6** | Pruebas automatizadas no Oracle | **Completada** |
| **Fase 7** | Suite Oracle del modelo reducido | **Completada en código; certificación física pendiente** |
| **Fase 8** | Revisión final no Oracle previa a autorización | **Siguiente fase** |
| **Fases 9–11** | Ambiente, ejecución y certificación Oracle | **Bloqueadas** |
| **Fase 12** | Documentación y cierre técnico | **Pendiente** |

---

## 4. Bloqueantes vigentes

1. El modelo de 17 tablas todavía no ha sido instalado ni certificado físicamente en Oracle.
2. El script `06` es destructivo y requiere base exclusiva, respaldo validado y autorización expresa.
3. La suite Oracle está preparada, pero no se ha ejecutado con `RL_ORACLE_INTEGRATION_REQUIRED=true`.
4. Falta la revisión final de preparación de Fase 8 antes de solicitar autorización para el ambiente físico.
5. `npm ci` continúa informando 13 vulnerabilidades globales: 6 moderadas, 6 altas y 1 crítica; requieren una intervención separada y no deben corregirse con `--force` sin análisis.

Resuelto en Fases 1–7:

- nombres físicos JSON;
- trazas locales de cálculo;
- auditoría local;
- contratos y tablas puente heredadas en código activo;
- inventario exacto del DDL;
- cobertura funcional no Oracle;
- preparación integral de la certificación Oracle;
- validador vinculante en CI.

---

## 5. Directrices activas

1. Trabajar únicamente en `desarrollo`.
2. No modificar ni fusionar `main`.
3. Mantener el PR #20 abierto y en borrador.
4. No habilitar auto-merge.
5. No ejecutar Oracle.
6. No ejecutar los scripts `05` o `06`.
7. No retirar tablas físicamente ni ejecutar `DROP TABLE`.
8. No versionar credenciales o cadenas de conexión.
9. No declarar certificado el modelo antes de ejecutar pruebas Oracle reales.
10. Mantener commits identificables, validación y documentación por fase.

---

## 6. Punto exacto de continuación

La siguiente intervención es la **Fase 8 — Quality Gates finales y revisión de preparación previa a Oracle**:

1. verificar que las Fases 1–7 estén representadas en código y documentación;
2. ejecutar nuevamente los dos validadores del modelo reducido;
3. revisar el script `06` sin ejecutarlo;
4. confirmar que el script no esté integrado al instalador automático;
5. revisar seguridad de variables, secretos y bloqueos Oracle;
6. comprobar que la suite Oracle permanezca deshabilitada por defecto;
7. ejecutar Release, Backend, Frontend, cobertura, build Angular y E2E;
8. comprobar ausencia de archivos auxiliares temporales;
9. verificar `main`, ramas remotas y PR #20;
10. producir un dictamen de preparación indicando expresamente qué está listo y qué continúa bloqueado;
11. no preparar ni ejecutar el ambiente Oracle hasta una autorización posterior y separada.
