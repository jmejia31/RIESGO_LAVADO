# Manual técnico final — Matrices de Riesgos

## Arquitectura

El módulo usa Angular (`frontend/rl-app`), ASP.NET Core (`backend/RL.API`) y Oracle (`RIESGO_LAVADO`). La API calcula y valida en servidor; Angular no es autoridad de scoring, permisos ni estados. El renderer consume el JSON versionado y `ReportPreview` es compartido por reportes y Excel.

## Gobierno del ciclo de vida H01-H14

La transición de familias y formularios se gobierna en servidor y Oracle. H01 protege la vigencia histórica y bloquea familias archivadas; H05 registra cada transición de versión en la transacción de negocio; H06/H07 serializan el bloqueo y la relectura de familia para evitar que una carrera de archivado elimine o actualice un borrador. H02/H09 exponen el historial de una familia mediante una consulta específica autorizada para módulo 10, con resolución canónica por ID y paginación aislada.

El frontend centraliza `familiaArchivada`/`puedeGestionarFamilia` en los modales de detalle y edición. Una familia archivada es visible como `ARCHIVADA`, permanece consultable y no ofrece mutaciones administrativas. La eliminación ordinaria usa iconografía `delete`, no `versions`, y el backend rechaza familias archivadas dentro de su transacción (H10/H11). Los KPI de familias provienen del consolidado servidor y los filtros archivado/estado no se mezclan con el total institucional (H12).

Antes de devolver vigencia a una versión histórica se validan familia, hash de definición, contrato JSON y runtime versionado; una discrepancia bloquea la operación sin modificar el JSON histórico (H13). La migración de ciclo de vida se mantiene preparada para Oracle XE descartable y no se ha ejecutado contra Oracle productivo (H14).

## Oracle

El modelo operativo tiene 17 tablas: familias, versiones, catálogos, elementos, reglas, riesgos, evaluaciones, proyecciones, flujos, controles, evaluaciones de controles, planes, actividades, evidencias, vínculos de evidencias, señales y automonitoreo. El motor añade ocho tablas de fórmulas/versiones/funciones/parámetros. Las secuencias y restricciones están versionadas en `database/19_matrices_riesgos`.

Familia institucional: ID 22, `MATRIZ_RIESGOS_LAFT`. V1: ID 61, publicada y vigente. V2: ID 63, borrador y no vigente. Los 59 históricos permanecen vinculados a V1.

## Contratos y cálculo

`VER_JSON` define campos dinámicos; `EVA_DATOS_JSON` conserva respuestas; `EVA_CALCULOS_JSON` conserva resultados server-side. Controles, planes, actividades, evidencias, alertas y automonitoreo son entidades normalizadas. La regla activa es `CALCULO_VRI_VRR` 1.0 con algoritmo `MATRICES_VRI_ADITIVO_1_9`.

## Backend y API

El backend se publica con `dotnet publish --configuration Release` o `backend/RL.API/Dockerfile`, target `net10.0`. La configuración se inyecta por variables de ambiente. Health es `/healthz`; readiness es `/readyz` y valida Oracle. El puerto de contenedor es 8080. Las operaciones transaccionales comparten conexión/transacción cuando la operación requiere auditoría.

## Frontend

Se construye con Node 24.18.0 y `npm ci`, luego `npm run build -- --configuration production`. Nginx sirve estáticos, expone `/healthz`, hace fallback SPA y proxy de `/api/` y `/hubs/` al backend. El environment productivo usa rutas relativas; no contiene secretos.

## Seguridad y auditoría

Autenticación, autorización de módulo/rol/estado y validación de payload ocurren en backend. Se rechazan IDOR, mutaciones de versión publicada, JSON inválido, archivos inválidos y manipulación de resultados calculados. Las acciones críticas registran auditoría institucional. No se registran contraseñas, tokens ni documentos completos.

## Deployment, continuidad y troubleshooting

El manifest y scripts están en [`deployment/matrices-riesgos`](../../deployment/matrices-riesgos/release-manifest.json). Preflight/postflight son read-only. El rollback de código es redeploy de la imagen anterior; el rollback Oracle es lógico. El script de retiro de V2 solo acepta una V2 DRAFT, vigente 0, hash esperado y sin evaluaciones vinculadas. Nunca elimina V1, riesgos históricos o evaluaciones V1.

Backup/restore, monitoreo y contingencia se detallan en la [guía DBA](GUIA_DBA_MATRICES_RIESGOS.md) y el [manual operativo](MANUAL_OPERATIVO_MATRICES_RIESGOS.md).

## Deuda explícita

RTO/RPO no están definidos institucionalmente; restore Data Pump requiere un objetivo aislado y privilegios DBA. Permanecen documentados Jurídicas production-like, VER_ID 27/28, RBAC granular global y los dos placeholders de prueba.

## H08 ampliado y límites de aceptación

La certificación automatizada H08 usa solamente Oracle XE descartable, una familia sintética y el host E2E explícitamente habilitado. El test exige gates independientes H08-A a H08-E, verifica la ruta Angular real y compara identificadores, hash y JSON persistidos individualmente. La aceptación visual y la capacitación institucional permanecen `PENDING_USER_EXECUTION`; su procedimiento y matriz de evidencia están en [`GUIA_VALIDACION_MANUAL_H08.md`](GUIA_VALIDACION_MANUAL_H08.md).
