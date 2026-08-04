# Estado de colaboración y punto de continuidad

> Actualización 2026-08-04: La Fase 1.3 ha sido aprobada a nivel funcional por Javier Mejía. La Fase 1.2 permanece abierta y pendiente de la codificación y ejecución controlada de pruebas Oracle (Paso 1 del plan aprobado). PR #20 en borrador, `main` intacta.

Documento vivo. Debe actualizarse al finalizar cada intervención junto con `BITACORA_COLABORACION.md`.

---

## 1. Línea base vigente

- **Repositorio**: `jmejia31/RIESGO_LAVADO`
- **Rama de trabajo obligatoria**: `desarrollo`
- **Rama estable**: `main` — no modificar ni integrar sin autorización expresa de Javier Mejía
- **Aprobador final**: Javier Mejía (`jmejia31`)
- **Arquitectura**: monolito modular con Angular 22, ASP.NET Core 10 y Oracle 11g
- **Frontend**: Angular `22.0.3`, CLI `22.0.4`, TypeScript `6.0.3`, Node `24.18.0`
- **Backend**: .NET `10.0`, `Oracle.ManagedDataAccess.Core` `23.4.0`

---

## 2. Última intervención

- **Intervención**: Aprobación funcional de la Fase 1.3 y autorización del Paso 1 de Fase 1.2 (Pruebas Oracle)
- **Fecha**: 2026-08-04 (Hora local)
- **Autor**: Antigravity
- **Rama**: `desarrollo`
- **Estado**: Aprobación funcional de la Fase 1.3 otorgada formalmente por Javier Mejía. Se autorizó la codificación de las pruebas de integración Oracle en `backend/RL.API.Tests/Features/MatricesRiesgos/MatricesRiesgosRepositoryIntegrationTests.cs` (Paso 1 del plan). La Fase 1.2 continúa abierta; la Fase 1 global sigue no certificada.

---

## 3. Estado de fases del Módulo Matrices de Riesgos

| Fase | Descripción | Estado Real | Detalle / Pendiente |
|---|---|---|---|
| **Fase 0** | Reconciliación de Estructuras y Eliminación de Código Heredado | **Completada** | Código libre de tablas antiguas y `EVA_ESTADO`. |
| **Fase 1.1** | Infraestructura Oracle Segura (Script 05) | **Implementada en código** | Script 05 idempotente; **bloqueado de ejecución en Oracle**. |
| **Fase 1.2** | Alineación DDL y Atomicidad de Transacciones | **Abierta (Pendiente)** | Pendiente de pruebas Oracle controladas de commit/rollback (`RL_MR_EVI_APROBACION`). |
| **Fase 1.3** | Contratos Neutros, DTOs Dinámicos y Retiro Heredado | **Aprobada funcionalmente** | Aprobada por Javier Mejía el 2026-08-04. Pendiente de fusión de PR #20 (mantenido en borrador). |
| **Fase 1.4 – 1.9** | Metodología, Consultas, Endpoints, Accesibilidad, HTTP y Certificación | **Pendientes de certificación** | Avances técnicos integrados; certificación global pendiente de pruebas ejecutable y Oracle. |
| **Fase 1 Global** | Plan de Implementación por Fases | **No Certificada (En progreso)** | PR #20 en borrador, `main` intacta, sin fusiones. |

---

## 4. Estado de validadores y pruebas

| Verificación | Estado | Detalle |
|---|---|---|
| `validate_matrices_dynamic_ddl_alignment.ps1` | **Aprobado** (Estático) | 46 archivos de módulo y 114 archivos no ignorados de seguridad validados sin hallazgos. |
| `validate_documentation_links.ps1` | **Aprobado** (Estático) | 42 documentos Markdown y 145 enlaces locales verificados. |
| `validate_database_scripts.ps1` | **Aprobado** (Estático) | 19 scripts activos raíz, 1 paquete modular, 23 alcanzables. |
| `validate_repository_structure.ps1` | **Aprobado** (Estático) | 118 rutas obligatorias y 471 archivos rastreados validados. |
| **Compilación Release y Cobertura Backend** | **Aprobada en CI** | Ejecución `30855978597`: build sin errores ni advertencias; 188 pruebas; cobertura 16.22 % de líneas y 16.60 % de ramas. |
| **Pruebas Angular y E2E Frontend** | **Aprobadas en CI** | Ejecución `30855978597`: 122 pruebas frontend, build correcto y 7 E2E. |
| **Pruebas Oracle de Transacción / Rollback** | **Pendiente** | Entorno Oracle controlado pendiente de ejecución. |

---

## 5. Directrices y restricciones activas

1. **Fase 1.3 Aprobada**: La Fase 1.3 cuenta con aprobación funcional y certificación técnica en CI, pero el PR #20 debe mantenerse en borrador hasta cerrar la Fase 1.2 y la Fase 1 global.
2. **Fase 1.2 abierta**: Mantener el seguimiento de pruebas Oracle de commit conjunto y rollback forzado en `RL_MR_EVI_APROBACION`.
3. **Oracle / Script 05**: No ejecutar en base de datos sin autorización explícita.
4. **PR #20**: Mantener en estado borrador (*draft*); no realizar merge ni modificar `main`.
5. **Rama `main`**: Permanece intacta y protegida.

---

## 6. Punto exacto de continuación

1. Mantener PR #20 en borrador y preservar `main` intacta.
2. Codificar la suite de pruebas de integración Oracle controladas en `backend/RL.API.Tests/Features/MatricesRiesgos/MatricesRiesgosRepositoryIntegrationTests.cs` sin ejecutar contra Oracle de forma física.
3. Compilar en Release, correr la suite de pruebas unitarias sin Oracle (verificando la advertencia) y ejecutar validadores estáticos locales.
4. Entregar el código a Codex para revisión. La ejecución física contra Oracle y el script `05` requerirán una autorización nueva y separada.
