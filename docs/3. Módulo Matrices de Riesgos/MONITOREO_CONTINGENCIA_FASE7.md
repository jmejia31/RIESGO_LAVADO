# Monitoreo y contingencia — Fase 7

## Monitoreo operativo

| METRIC | SOURCE | THRESHOLD_OR_BASELINE | ACTION |
|---|---|---|---|
| Disponibilidad API | `/healthz`/HTTP | 200; cualquier no-200 | revisar proceso y reiniciar |
| Readiness Oracle | `/readyz`/HTTP | 200; 503 no listo | revisar alias, sesión y DBA |
| HTTP 5xx | logs/hosting | baseline cero sostenido | capturar correlation id y escalar |
| Latencia | access logs/cliente | comparar baseline del release | investigar query/exportación |
| Reinicios | runtime | no inesperados | revisar memoria, healthcheck y logs |
| Almacenamiento evidencia | volumen y logs | espacio suficiente | liberar por política/expandir autorizado |
| Exportaciones fallidas | logs API | baseline cero | revisar preview, filtros y almacenamiento |
| Errores Oracle | logs sanitizados | baseline cero | validar readiness y DBA |

No se inventa una plataforma de métricas. La fuente real es HTTP, logs de aplicación y métricas del hosting disponible.

## Matriz de contingencia

| Escenario | DETECTION | IMPACT | IMMEDIATE_ACTION | ROLLBACK | RECOVERY | VALIDATION | ESCALATION |
|---|---|---|---|---|---|---|---|
| Oracle unavailable | readiness 503/ORA | no lecturas/escrituras | detener cambios | no DDL | restaurar conectividad | `/readyz`, preflight | DBA |
| backend down | health no responde | API no disponible | revisar proceso | imagen anterior | restart/redeploy | smoke | Operaciones |
| frontend unavailable | nginx health | UI no disponible | revisar nginx | imagen anterior | redeploy estáticos | `/healthz` | Operaciones |
| storage unavailable | upload/export error | evidencias/reportes | bloquear uploads | config/volumen anterior | recuperar volumen | upload controlado | DBA/Soporte |
| export failure | logs/preview | entrega reportes | conservar filtros | redeploy si código | repetir preview | parity | Soporte |
| partial deployment | versiones mixtas | comportamiento impredecible | detener tráfico | rollback ambos artefactos | redeploy coordinado | manifest + smoke | Release |
| invalid V2 state | preflight | riesgo de publicación | bloquear V2 | rollback lógico guardado | restaurar DRAFT | V1 hash/binding | Funcional/DBA |
| disk full | métricas/errores | fallos generales | detener generación | no borrar historia | ampliar/limpiar autorizado | health/smoke | Operaciones |
| missing secret | startup failure | proceso no inicia | no imprimir valor | restaurar secret store | inyectar por canal seguro | health/readiness | Seguridad |

## Recuperación segura

Las simulaciones no provocan caída productiva: se usa configuración inválida en proceso local/controlado, alias Oracle no resoluble en un proceso aislado, redeploy de artefacto anterior y smoke posterior. RTO/RPO: `NOT_INSTITUTIONALLY_DEFINED`.
