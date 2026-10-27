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
