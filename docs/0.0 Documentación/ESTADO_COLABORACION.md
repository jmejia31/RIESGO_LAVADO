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

- Intervencion: Resolucion Brecha de Metodologia y puerto 5043 (Hito 7.1+)
- Fecha: 2026-07-31 10:35 hora local (UTC-6)
- Autor: Antigravity
- Rama: desarrollo
- Commit HEAD: `3773852` publicado en origin/desarrollo
- Estado: Metodologia vigente implementada en backend. Frontend funcional al 100%. Quality Gates aprobadas.

## 3. Estado de fases

### 3.1 Reorganizacion
Las fases 1-21 estan completadas. No corresponde abrir nueva fase de reorganizacion.

### 3.2 Matrices de Riesgos

- Fase 5: Oracle RL_MR_* instalada y validada
- Fase 6: Backend ASP.NET Core completado y certificado
- Hito 7.0: DELETE /evidencias/{id} con compensacion transaccional - certificado
- Hito 7.1: 25 endpoints TypeScript - implementado y testeado
- Hito 7.2: Dashboard 5x5 interactivo - implementado
- Hito 7.3: Renderizado dinamico, coherencia VRR, evidencias 2 pasos - implementado
- Hito 7.4: Plantillas, ciclo de vida, modal Editor JSON - implementado
- Hito 7.5: Quality Gates locales - aprobadas

### 3.3 Dictamen vigente
Fase 7 completada localmente. Pendiente validacion institucional Javier Mejia.

## 4. Estado de componentes

| Componente | Estado |
|---|---|
| Backend modular | 181 pruebas unitarias correctas |
| Frontend Angular | 183 pruebas unitarias correctas |
| E2E Playwright | 7 pruebas correctas |
| Oracle | Estructura instalada al 100% |
| Matrices de Riesgos Frontend | Completo: Dashboard 5x5, captura, plantillas |

## 5. Relacion entre ramas

- desarrollo es la rama activa. HEAD = `3773852` sincronizado con origin/desarrollo.
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

1. Prueba integracion Oracle real DELETE /evidencias/{id}: bloqueo FOR UPDATE, ciclo archivo+Oracle, ERROR_COMPENSACION_EVIDENCIA.
2. Validacion institucional Javier Mejia sobre modulo completo.
3. Definir Fase 8 (roles, despliegue, AD/SMTP) o pase a produccion.
