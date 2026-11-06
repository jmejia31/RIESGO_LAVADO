# Plan de escenarios QA para Matrices de Riesgos

Estado: `IMPLEMENTED_PENDING_REVIEW`

Este documento disena escenarios. No ejecuta navegador, API, Oracle, credenciales ni mutaciones.

## Precondiciones comunes

Antes de cualquier prueba futura se debe verificar SHA/rama, identidad exacta del ambiente y base, login y permisos efectivos, runner aprobado, `QA_SECRET_ACCESS_GATE`, `QA_DB_TARGET_VERIFIED`, fixture sintetico con prefijo, IDs, dependencias, auditoria, limpieza selectiva y autorizacion humana. Ante ambiguedad: STOP.

## Escenarios de solo lectura: modulos 2-9

| Grupo | Accion propuesta | Evidencia | Estado |
|---|---|---|---|
| Navegacion | Abrir rutas visibles para QA | Ruta, status y permisos sanitizados | `PENDING` |
| Listados | Consultar filtros, paginacion y detalle | Conteos agregados y contrato sin PII | `PENDING` |
| Autorizacion | Intentar ruta sin permiso efectivo | HTTP esperado sin tokens | `PENDING` |
| Auditoria | Consultar si el rol lo permite | Accion, fecha y resultado sanitizados | `PENDING` |
| Configuracion/usuarios | Inspeccion de contrato, no edicion | Permiso observado sin credenciales | `PENDING` |

Para estos modulos se prohiben creacion, edicion, eliminacion, carga de archivos y cambios de roles o configuracion.

## Escenarios sinteticos: modulo 10

| ID | Escenario | Fixture | Criterio | Limpieza |
|---|---|---|---|---|
| M10-01 | Crear familia aislada | Prefijo y dependencia conocida | ID y readback propios | ID propio |
| M10-02 | Crear dos versiones DRAFT | Familia M10-01 | Estado, contador y asociacion | Versiones propias |
| M10-03 | Editar borrador autorizado | Version propia | PUT, readback y auditoria | Conservar hasta cierre |
| M10-04 | Rechazar version protegida | Fixture sintetica | HTTP de contrato sin cambio | No borrar ajenos |
| M10-05 | Publicar version sintetica | Dependencias y auditoria | Vigencia y lectura posterior | Solo con autorizacion |
| M10-06 | Eliminar DRAFT elegible | Version sin dependencias | DELETE, GET 404 y restantes intactas | ID propio |
| M10-07 | Concurrencia y rollback | Dos sesiones sinteticas | Conflicto controlado e integridad | Recuperacion documentada |

Todos son disenos pendientes. Ningun ID, dato, HTTP, auditoria o limpieza se considera ejecutado por este documento.

## Evidencia y stop conditions

Registrar solo SHA, run ID, escenario, IDs sinteticos, status agregado, auditoria y before/after minimo. No capturar cookies, tokens, contrasenas, PII ni dumps Oracle. Detener ante destino no identificado, permiso inesperado, dato no sintetico, error de auditoria, readback inconsistente o acceso fuera de Matrices.
