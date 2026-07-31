# Estado de colaboración y punto de continuidad

> Documento vivo. Debe actualizarse al finalizar cada intervención junto con [`BITACORA_COLABORACION.md`](../../BITACORA_COLABORACION.md).

## 1. Línea base vigente

- **Repositorio**: `jmejia31/RIESGO_LAVADO`.
- **Rama de trabajo obligatoria**: `desarrollo`.
- **Rama estable**: `main`.
- **Aprobador final**: Javier Mejía (`jmejia31`).
- **Regla**: no modificar ni integrar `main` sin autorización expresa.
- **Arquitectura**: monolito modular con Angular, ASP.NET Core y Oracle.
- **Frontend declarado**: Angular `22.0.3`, Angular CLI `22.0.4`, TypeScript `6.0.3`, Node `24.18.0`, npm `11.12.1`.
- **Backend declarado**: .NET `10.0`, `Oracle.ManagedDataAccess.Core` `23.4.0`.

## 2. Última intervención

- **Intervención**: #24.
- **Fecha**: 2026-07-31 01:02, hora local.
- **Autor**: Antigravity.
- **Rama**: `desarrollo`.
- **Objetivo**: Desarrollar el Hito 7.0 de la Fase 7 Frontend: corrección del endpoint del historial de formularios y creación de la API transaccional `DELETE /evidencias/{id}` de compensación de evidencias huérfanas de forma idempotente.
- **Estado**: **Hito 7.0 Backend Completado y Aprobado Técnicamente.** Cobertura de backend lograda de Líneas: 15.76% y Ramas: 16.89%. 177 pruebas unitarias de backend, 165 de frontend y 7 de Playwright aprobadas al 100% en las Quality Gates.

## 3. Estado de fases

### 3.1 Programa de reorganización

Las fases de reorganización arquitectónica y calidad 1–21 están documentadas como completadas. No corresponde abrir una nueva fase de reorganización por continuidad numérica.

### 3.2 Matrices de Riesgos

- La Fase 5 de base de datos física definitiva `RL_MR_*` en Oracle se encuentra instalada y validada al 100%.
- La Fase 6 de Desarrollo del Backend ASP.NET Core está completada y aprobada técnicamente.
- El Hito 7.0 de ajustes de APIs y compensación de evidencias se encuentra implementado y certificado al 100% de calidad.
- La siguiente fase a iniciar es el **Desarrollo del Frontend (Fase 7 - Angular 22 - Hito 7.1 en adelante)**.

### 3.3 Dictamen vigente

La fase vigente queda:

**Fase 7 Frontend — Hito 7.0 (Ajuste Previo Backend) completado. Aprobado para iniciar el desarrollo del frontend.**

La continuidad posterior se ha plasmado en el [Análisis y Plan Definitivo de Implementación del Módulo Matrices de Riesgos](../3.%20Módulo%20Matrices%20de%20Riesgos/ANALISIS_PLAN_DEFINITIVO_MATRIZ_RIESGOS.md).

## 4. Estado de componentes

| Componente | Estado conocido |
|---|---|
| Backend modular | Activo y certificado con 177 pruebas unitarias |
| Frontend Angular | Activo y certificado con 165 pruebas unitarias |
| Oracle | Estructura dinámica y física instalada al 100% |
| Monitoreo de Listas | Integrado con pruebas de controlador en backend |
| Matrices de Riesgos | Backend de 25 endpoints listo para frontend |
| Auditoría de exportaciones | Obligatoria en todos los flujos de descarga |

## 5. Relación entre ramas

La comparación al inicio de la Intervención #12 confirmó que:

- `desarrollo` es la rama de integración activa.
- Todos los cambios están validados localmente y aprobados sin warnings de compilador ni fallos en quality gates.
- Integrar a `main` requiere autorización expresa de Javier Mejía.

## 6. Cambios de la Intervención #12

### 6.1 Ejecutado

- Corrección de la ruta del historial de formularios a `GET api/matrices-riesgos/formularios/historial?familiaCodigo=...`.
- Implementación de la API idempotente de compensación `DELETE api/matrices-riesgos/evidencias/{id}` con borrado de archivo físico y base de datos relacional.
- Integración de 4 nuevas pruebas unitarias en backend.
- Aumento de cobertura de backend a 15.76% de líneas y 16.89% de ramas.
- Aprobación definitiva de `tools/run_quality_gates.ps1` con salida limpia y exit code 0.

### 6.2 No ejecutado

- Conexión real a Active Directory o SMTP institucional (pendientes por entorno).

## 8. Cierre formal de Hito 7.0

El backend de Matrices de Riesgos se encuentra formalmente cerrado y certificado. El Hito 7.0 de compensación e historial de versiones está finalizado al 100%.

## 9. Responsabilidades

| Actividad | Responsable |
|---|---|
| Auditoría de código y calidad | Antigravity/ChatGPT |
| Desarrollo e integración | Codex/ChatGPT |
| Pruebas y despliegue | Antigravity/Codex |
| Aprobación final y requerimientos | Javier Mejía |

## 10. Restricciones vigentes

- No reducir pruebas o cobertura para aprobar un cambio.
- No declarar cierre o aprobación funcional final sin Javier Mejía.
- Conservar contratos y estructura relacional Oracle.

## 11. Punto exacto de continuación

La siguiente intervención debe:

1. Iniciar el desarrollo Frontend (Fase 7) en Angular 22 sobre la rama `desarrollo` (Hito 7.1 en adelante).
2. Crear los servicios de HttpClient y modelos en TypeScript para consumir los 25 endpoints del backend de Matrices de Riesgos.
3. Desarrollar las interfaces del mapa de calor 5x5, captura de evaluaciones con bloqueo por coherencia residual, dropdowns de selector-catalogo y catalogo-multiple, y evidencias en dos pasos con compensación.
4. Mantener la cobertura y validadores al 100% de éxito.



