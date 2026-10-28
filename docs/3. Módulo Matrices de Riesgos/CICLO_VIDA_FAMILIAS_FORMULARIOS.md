# Ciclo de vida de familias de formularios

Una familia activa puede recibir nuevas versiones y participar en nuevas evaluaciones cuando tiene una versión publicada y vigente. Una familia inactiva queda fuera de nuevas operaciones, pero conserva su historial.

## Archivado

El archivado es reversible y no elimina filas. Solo procede para una familia inactiva, no predeterminada y sin versión publicada vigente. Las versiones, evaluaciones, cálculos, evidencias y auditoría permanecen intactos. El caso `PRUEBA_FORMULARIO` debe resolverse por esta vía cuando conserva una versión publicada no vigente.

El endpoint administrativo es `PUT /api/matrices-riesgos/familias/{id}/archivar` y exige rol administrador, justificación, bloqueo de fila, validación transaccional y auditoría.

## Restauración

`PUT /api/matrices-riesgos/familias/{id}/restaurar` devuelve la familia a `INACTIVA`. No publica, activa ni convierte en predeterminada ninguna versión.

## Depuración extraordinaria

El precheck `GET /api/matrices-riesgos/familias/{id}/purga-precheck` es de solo lectura y muestra dependencias conocidas. La depuración física permanece bloqueada por defecto: el código de familia no prueba procedencia de datos de prueba, las referencias de auditoría son historia protegida y el mapa completo de retención/dependencias debe aprobarse institucionalmente. No se ejecutan borrados productivos ni se ofrece una cascada destructiva.

El filtro operativo excluye familias archivadas por defecto; el filtro `ARCHIVADAS` permite consultarlas a administradores.

## Contrato consolidado H01-H14

Los estados de familia son excluyentes y se muestran como `ACTIVA`, `INACTIVA` o `ARCHIVADA`. `ARCHIVADA` es un estado reversible de solo lectura: permite consultar definiciones, versiones e historial autorizado, pero bloquea edición, creación, clonación, publicación, vigencia, eliminación de borradores y activación directa. La restauración siempre devuelve la familia a `INACTIVA`; la activación posterior es una operación separada.

Las mutaciones de versiones publicadas y su vigencia bloquean la versión y la familia dentro de la misma transacción. Las publicaciones, activaciones, desactivaciones, actualizaciones y eliminaciones de borradores registran auditoría transaccional; si la auditoría falla, la mutación se revierte. Las carreras de archivado contra edición/eliminación de borradores se resuelven leyendo nuevamente el estado de la familia después del bloqueo.

El historial de una familia se consulta mediante el endpoint específico `GET /api/auditoria/familia?familiaId=...`, autorizado para el módulo 10. El backend resuelve el código canónico y no acepta códigos alternativos arbitrarios para ampliar el alcance de la consulta. La paginación conserva orden descendente por fecha e ID y el frontend no re-filtra por una identidad aportada por el cliente.

La eliminación ordinaria está bloqueada para familias archivadas aun cuando no tengan versiones. Las versiones históricas no se reactivan sin validar familia activa/no archivada, hash persistido, contrato de definición y dependencias del runtime. Ninguna de estas reglas autoriza cambios Oracle productivos: las pruebas de integración usan exclusivamente Oracle XE descartable.

## Certificación H08 ampliada

El runner H08 ampliado ejecuta en secuencia las interacciones Angular de desactivación, archivado, filtro de archivadas, restauración y activación; después valida vigencia, evaluaciones antes/después de restaurar, snapshots individuales de versiones/evaluaciones/evidencias y auditoría de ambos ciclos. La evidencia técnica automatizada se separa de la aceptación manual descrita en [`GUIA_VALIDACION_MANUAL_H08.md`](GUIA_VALIDACION_MANUAL_H08.md).
