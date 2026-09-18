# Manual operativo — Matrices de Riesgos

## Inicio, parada y reinicio

Con Docker Compose, validar variables secretas fuera del repositorio y ejecutar `docker compose config` antes de `docker compose up -d`. Verificar primero backend, luego frontend. Para reiniciar, usar `docker compose restart backend frontend`; para detener, `docker compose stop`. No borrar volúmenes de evidencias.

## Health y logs

- Backend liveness: `GET /healthz` debe responder 200 sin validar Oracle.
- Backend readiness: `GET /readyz` debe responder 200 solo cuando Oracle está disponible; 503 significa no listo.
- Frontend: `GET /healthz` debe responder 200.
- Revisar stdout del contenedor y `/app/logs`; nunca copiar secretos a tickets.

## Operación diaria

Ejecutar el smoke test de `deployment/matrices-riesgos/smoke/smoke-tests.ps1` después de cada despliegue. Vigilar 5xx, latencia, readiness, fallos Oracle, espacio de evidencias y exportaciones fallidas. Mantener una copia del manifest y checksums junto con el release.

## Backup y restore

Antes de cambio: registrar SHA, versión Oracle, estado V1/V2, conteos y ruta de evidencias. El DBA ejecuta Data Pump o el mecanismo institucional en un objetivo aislado. Restaurar en orden: schema/objetos, datos, evidencias físicas, configuración no secreta, validadores y smoke. No restaurar destructivamente `hpprod1` durante una prueba.

## Rollback y contingencia

Para fallo de código, redeploy de la imagen anterior y ejecutar postflight. Para configuración, restaurar plantilla aprobada e inyectar secretos nuevamente. Para fallo Oracle, detener tráfico de escritura, conservar logs y escalar al DBA. Para V2 inválida, no publicar; ejecutar solo el rollback lógico guardado si cumple sus precondiciones.

## Escalamiento

Escalar a soporte con timestamp, SHA, endpoint, status HTTP, correlation id y mensaje sanitizado. Escalar a DBA los ORA- de conectividad, objetos inválidos, constraints y restore. Escalar a Seguridad los 401/403 inesperados, IDOR o exposición de secretos.
