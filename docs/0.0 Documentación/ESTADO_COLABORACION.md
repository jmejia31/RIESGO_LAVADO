# Estado de colaboración y punto de continuidad

> Actualización 2026-08-04: La Fase 1.3 está **implementada y certificada técnicamente por CI**, pendiente de aprobación funcional de Javier Mejía. La Fase 1.2 permanece **abierta y pendiente de pruebas Oracle controladas de transacción y rollback**. La **Fase 1 completa no está certificada**. PR #20 se mantiene en borrador; no se autoriza ninguna nueva modificación o fusión en `main`.

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

- **Intervención**: Corrección del validador local y conciliación de evidencia CI de la Fase 1.3
- **Fecha**: 2026-08-04 (Hora local)
- **Autor**: Codex
- **Rama**: `desarrollo`
- **Estado**: Se corrigió el falso positivo que examinaba `appsettings.json` aun estando ignorado por Git, sin excluir archivos rastreados. La ejecución CI `30855978597` aprobó build, suites, cobertura y E2E. La Fase 1.3 queda **certificada técnicamente en CI y pendiente de aprobación funcional**; la Fase 1.2 sigue **abierta** por Oracle; la Fase 1 global sigue **no certificada**.

---

## 3. Estado de fases del Módulo Matrices de Riesgos

| Fase | Descripción | Estado Real | Detalle / Pendiente |
|---|---|---|---|
| **Fase 0** | Reconciliación de Estructuras y Eliminación de Código Heredado | **Completada** | Código libre de tablas antiguas y `EVA_ESTADO`. |
| **Fase 1.1** | Infraestructura Oracle Segura (Script 05) | **Implementada en código** | Script 05 idempotente; **bloqueado de ejecución en Oracle**. |
| **Fase 1.2** | Alineación DDL y Atomicidad de Transacciones | **Abierta (Pendiente)** | Pendiente de pruebas Oracle controladas de commit/rollback (`RL_MR_EVI_APROBACION`). |
| **Fase 1.3** | Contratos Neutros, DTOs Dinámicos y Retiro Heredado | **Certificada técnicamente en CI** | Consolidado tipado, metodología dinámica/versionada y frontend adaptado; pendiente de aprobación funcional de Javier Mejía. |
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

1. **Separación de certificaciones**: La Fase 1.3 cuenta con certificación técnica CI, pero no debe declararse funcionalmente aprobada por Javier ni debe confundirse con el cierre de la Fase 1 global.
2. **Fase 1.2 abierta**: Mantener el seguimiento de pruebas Oracle de commit conjunto y rollback forzado en `RL_MR_EVI_APROBACION`.
3. **Oracle / Script 05**: No ejecutar en base de datos sin autorización explícita.
4. **PR #20**: Mantener en estado borrador (*draft*); no realizar merge ni modificar `main`.
5. **Rama `main`**: Permanece intacta y protegida.

---

## 6. Punto exacto de continuación

1. Mantener PR #20 en borrador y preservar `main` intacta.
2. Obtener la aprobación funcional de Javier Mejía para la Fase 1.3, sin fusionar el PR #20.
3. Planificar y autorizar por separado la ejecución controlada de pruebas transaccionales Oracle (commit/rollback) para poder cerrar la Fase 1.2.
4. Mantener el script `05` bloqueado hasta contar con autorización expresa posterior.
