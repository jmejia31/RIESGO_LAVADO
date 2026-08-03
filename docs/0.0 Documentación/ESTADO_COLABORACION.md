# Estado de colaboracion y punto de continuidad

Documento vivo. Actualizar al finalizar cada intervencion junto con BITACORA_COLABORACION.md.

## 1. Linea base vigente

- Repositorio: jmejia31/RIESGO_LAVADO
- Rama de trabajo obligatoria: desarrollo
- Rama estable: main
- Aprobador final: Javier Mejia (jmejia31)
- Arquitectura: monolito modular con Angular, ASP.NET Core y Oracle
- Frontend: Angular 22.0.3, CLI 22.0.4, TypeScript 6.0.3, Node 24.18.0
- Backend: .NET 10.0, Oracle.ManagedDataAccess.Core 23.4.0

## 2. Ultima intervencion

- Intervencion: Finalización de Fase 0: Reconciliación de Estructuras y Eliminación de Código Heredado
- Fecha: 2026-08-03 08:18 hora local (UTC-6)
- Autor: Antigravity
- Rama: desarrollo
- Commit HEAD: `191c8ee` publicado en origin/desarrollo
- Estado: Punto de entrada oficial Oracle unificado. Archivos SQL antiguos del modelo heredado eliminados del repositorio. Referencias a EVA_ESTADO removidas en todo el backend y consultas refactorizadas para usar flujos de estado. Compilación y 181 pruebas del backend aprobadas.

## 3. Estado de fases

### 3.1 Reorganizacion
Las fases 1-21 estan completadas. No corresponde abrir nueva fase de reorganizacion.

### 3.2 Matrices de Riesgos

- Fase 5: Oracle RL_MR_* instalada y validada con semilla de metodologia vigente
- Fase 6: Backend ASP.NET Core completado y certificado
- Hito 7.0: DELETE /evidencias/{id} con compensacion transaccional - certificado
- Hito 7.1: 25 endpoints TypeScript - implementado y testeado
- Hito 7.2: Dashboard 5x5 interactivo - implementado
- Hito 7.3: Renderizado dinamico, coherencia VRR, evidencias 2 pasos - implementado
- Hito 7.4: Plantillas, ciclo de vida y maquetador visual interactivo completo (CRUD sin codigo) - implementado
- Hito 7.5: Quality Gates locales - aprobadas
- Ajustes de Diseño, Seguridad y Reportes en Oracle: Plan técnico consolidado aprobado por los socios.
- Fase 0 Reconciliación: Completado (Código libre de tablas antiguas y EVA_ESTADO, migración 05 creada).

### 3.3 Dictamen vigente
Fase 0 de reconciliación completada con éxito. Listo para revisión de los socios antes de proceder con el dashboard, reportes y frontend.

## 4. Estado de componentes

| Componente | Estado |
|---|---|
| Backend modular | 181 pruebas unitarias correctas |
| Frontend Angular | 183 pruebas unitarias correctas |
| E2E Playwright | 7 pruebas correctas |
| Oracle | Estructura instalada y sembrada al 100% con punto de entrada dinámico unificado |
| Matrices de Riesgos Frontend | Completo: Dashboard 5x5, captura, plantillas y maquetador visual de esquemas |

## 5. Relacion entre ramas

- desarrollo es la rama activa. HEAD = `191c8ee` sincronizado con origin/desarrollo.
- Integrar a main requiere autorizacion expresa de Javier Mejia.

## 6. Quality Gates

| Metrica | Valor | Estado |
|---|---|---|
| Backend lineas | 16.02% | OK (>=15.30%) |
| Backend ramas | 16.43% | OK (>=16.30%) |
| Frontend sentencias | 40.20% | OK |
| Frontend lineas | 40.40% | OK |

## 7. Restricciones vigentes

- No reducir pruebas o cobertura para aprobar cambios.
- No declarar cierre o aprobacion final sin Javier Mejia.
- Conservar contratos y estructura relacional Oracle.
- Validacion de sintaxis JSON es client-side; backend debe rechazar esquemas invalidos semanticamente.
- Pruebas de integracion Oracle pendientes antes de declarar el modulo listo para produccion.

## 8. Responsabilidades

| Actividad | Responsable |
|---|---|
| Auditoria de codigo y calidad | Antigravity / ChatGPT |
| Desarrollo e integracion | Codex / ChatGPT |
| Pruebas y despliegue | Antigravity / Codex |
| Aprobacion final y requerimientos | Javier Mejia |

## 9. Punto exacto de continuacion

1. Ejecución de la **Fase 1: Implementación de Consultas Relacionales en Oracle 11g** (reconstrucción de metodología vigente dinámica, proyecciones optimizadas y queries de agregación y paginación en base de datos).
2. Revisión de los socios sobre la Fase 0 completada.
