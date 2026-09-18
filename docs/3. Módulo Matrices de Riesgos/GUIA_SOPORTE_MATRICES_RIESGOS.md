# Guía de soporte — Matrices de Riesgos

| Síntoma | Diagnóstico | Acción |
|---|---|---|
| Login no disponible | revisar frontend health, API health y autenticación | ejecutar smoke; escalar si health falla |
| 403 | rol/módulo/estado no autorizado | verificar permiso, no modificar token |
| 500 | correlation id y logs backend | revisar excepción sanitizada y Oracle readiness |
| Oracle no listo | `/readyz`, alias y logs de conexión | validar alias/credencial por canal seguro; escalar DBA |
| Formulario vacío | familia/version endpoint y V1 | ejecutar preflight; no editar V1 manualmente |
| Evaluación rechazada | respuesta inválida o estado no permitido | revisar 422/409 y contrato JSON |
| Control/plan/actividad no guarda | validación o transacción | revisar payload y auditoría; no crear datos incompletos |
| Evidencia falla | extensión/tamaño/ruta/permisos | revisar política y almacenamiento; nunca relajar límites |
| Export falla | filtros, preview, logs | regenerar preview; verificar espacio y timeout |
| Histórico incorrecto | binding de versión | query de EVA_VERSION_ID; debe permanecer V1 |
| V2 visible como vigente | drift o publicación indebida | bloquear escrituras, ejecutar preflight y escalar |
| Health 200/readiness 503 | API viva pero Oracle no disponible | tratar como no listo, no ocultar dependencia |

Para cada incidente registrar SHA, ambiente, hora, endpoint, status, correlation id, pasos y evidencia sin PII ni secretos.
