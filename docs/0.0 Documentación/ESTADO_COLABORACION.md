# Estado de colaboración y punto de continuidad

> Actualización 2026-08-03: Corrección documental. La Fase 1.3 está **implementada en código y pendiente de certificación**. La Fase 1.2 permanece **abierta y pendiente de pruebas Oracle controladas de transacción y rollback**. La **Fase 1 completa no está certificada**. PR #20 se mantiene en borrador; `main` permanece intacta.

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

- **Intervención**: Corrección documental de estado de Fase 1.3 y Fase 1 / Verificación de validadores estáticos
- **Fecha**: 2026-08-03 (Hora local)
- **Autor**: Antigravity
- **Rama**: `desarrollo`
- **Estado**: Corrección de bitácora y estado de colaboración para retirar declaraciones prematuras de "cierre" o "certificación". Se confirmaron como aprobados los 4 validadores estáticos locales. La Fase 1.3 queda marcada como **implementada en código y pendiente de certificación**; la Fase 1.2 como **abierta**; la Fase 1 como **no certificada**.

---

## 3. Estado de fases del Módulo Matrices de Riesgos

| Fase | Descripción | Estado Real | Detalle / Pendiente |
|---|---|---|---|
| **Fase 0** | Reconciliación de Estructuras y Eliminación de Código Heredado | **Completada** | Código libre de tablas antiguas y `EVA_ESTADO`. |
| **Fase 1.1** | Infraestructura Oracle Segura (Script 05) | **Implementada en código** | Script 05 idempotente; **bloqueado de ejecución en Oracle**. |
| **Fase 1.2** | Alineación DDL y Atomicidad de Transacciones | **Abierta (Pendiente)** | Pendiente de pruebas Oracle controladas de commit/rollback (`RL_MR_EVI_APROBACION`). |
| **Fase 1.3** | Contratos Neutros, DTOs Dinámicos y Retiro Heredado | **Implementada en código** | Consolidado tipado, metodología dinámico/versionada y frontend adaptado; **pendiente de certificación CI y Quality Gates**. |
| **Fase 1.4 – 1.9** | Metodología, Consultas, Endpoints, Accesibilidad, HTTP y Certificación | **Pendientes de certificación** | Avances técnicos integrados; certificación global pendiente de pruebas ejecutable y Oracle. |
| **Fase 1 Global** | Plan de Implementación por Fases | **No Certificada (En progreso)** | PR #20 en borrador, `main` intacta, sin fusiones. |

---

## 4. Estado de validadores y pruebas

| Verificación | Estado | Detalle |
|---|---|---|
| `validate_matrices_dynamic_ddl_alignment.ps1` | **Aprobado** (Estático) | 46 archivos de módulo y 115 de seguridad validados sin hallazgos. |
| `validate_documentation_links.ps1` | **Aprobado** (Estático) | 42 documentos Markdown y 145 enlaces locales verificados. |
| `validate_database_scripts.ps1` | **Aprobado** (Estático) | 19 scripts activos raíz, 1 paquete modular, 23 alcanzables. |
| `validate_repository_structure.ps1` | **Aprobado** (Estático) | 118 rutas obligatorias y 471 archivos rastreados validados. |
| **Compilación Release y Cobertura Backend** | **Pendiente de ejecución CI** | Requiere SDK .NET 10 en entorno CI. |
| **Pruebas Angular y E2E Frontend** | **Pendiente de ejecución CI** | Requiere Node 22+/24 en entorno CI. |
| **Pruebas Oracle de Transacción / Rollback** | **Pendiente** | Entorno Oracle controlado pendiente de ejecución. |

---

## 5. Directrices y restricciones activas

1. **Retiro de afirmaciones de cierre**: No declarar "cerrada", "certificada" ni "100% aprobada" la Fase 1.3 ni la Fase 1 global mientras sigan pendientes los Quality Gates reproducidos y las pruebas Oracle.
2. **Fase 1.2 abierta**: Mantener el seguimiento de pruebas Oracle de commit conjunto y rollback forzado en `RL_MR_EVI_APROBACION`.
3. **Oracle / Script 05**: No ejecutar en base de datos sin autorización explícita.
4. **PR #20**: Mantener en estado borrador (*draft*); no realizar merge ni modificar `main`.
5. **Rama `main`**: Permanece intacta y protegida.

---

## 6. Punto exacto de continuación

1. Mantener PR #20 en borrador y preservar `main` intacta.
2. Ejecutar y registrar las pruebas de Quality Gates completas en el pipeline CI (compilación Release, suites de pruebas unitarias backend/frontend con cobertura, pruebas E2E).
3. Planificar la ejecución controlada de pruebas transaccionales Oracle (commit/rollback) para poder cerrar la Fase 1.2.
4. Proceder con la certificación formal de la Fase 1.3 tras verificar la salida de CI y Quality Gates.
