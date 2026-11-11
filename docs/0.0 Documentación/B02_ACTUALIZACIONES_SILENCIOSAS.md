# B02 - Actualizaciones silenciosas y estabilidad visual

## Alcance

Esta norma separa el conteo tecnico de solicitudes HTTP de la presentacion visual.
Las consultas ordinarias usan `SILENT_REFRESH`; las operaciones que requieren
feedback global deben declarar `USER_ACTION` o `CRITICAL_OPERATION` mediante
`HTTP_LOADING_INTENT`. La carga inicial y los refrescos ordinarios pertenecen a
la vista afectada y no al encabezado global.

## Contrato de estados por vista

- `UNINITIALIZED`: la vista aun no tiene una respuesta valida.
- `INITIAL_LOADING`: se reserva la superficie inicial sin datos falsos.
- `READY`: la respuesta valida se muestra normalmente.
- `REFRESHING`: la tabla y sus controles permanecen montados mientras llega la nueva respuesta.
- `EMPTY`: la respuesta valida confirma cero registros.
- `ERROR`: el error se muestra; no se convierte en una lista vacia silenciosa.

Durante `REFRESHING` se conservan filtros, paginacion, foco, modal y geometria.
Las respuestas antiguas no se aplican cuando la secuencia de solicitud ya no es
la vigente. Los datos previos se conservan solo como contexto visible y siempre
acompanados del error cuando la consulta nueva falla.

## Consumidores auditados

| Modulo | Estado B02 | Motivo |
|---|---|---|
| Matrices de Riesgos - evaluaciones | MODIFIED | tabla estable durante filtros, paginacion y refresh |
| Matrices de Riesgos - consolidado | MODIFIED | tabla estable durante filtros, orden y refresh |
| Matrices de Riesgos - plantillas | MODIFIED | superficie no se desmonta durante refresh de familias |
| Monitoreo de Listas | MODIFIED | cambio de categoria y filtros conservan la tabla |
| Usuarios | MODIFIED | selector y tabla permanecen montados |
| Tipo Listas | MODIFIED | tabla permanece montada durante actualizacion |
| Coincidencias Empleado | NO_CHANGE_REQUIRED | el indicador ya esta dentro del cuerpo de la tabla |
| Coincidencias Patrono | NO_CHANGE_REQUIRED | el indicador ya esta dentro del cuerpo de la tabla |
| Cargar Listas | NO_CHANGE_REQUIRED | estados de importacion son operaciones propias |
| Configuracion del Sistema | NO_CHANGE_REQUIRED | carga local del formulario y feedback de mutacion |
| Bitacora del Sistema | NO_CHANGE_REQUIRED | consumidor local sin reemplazo global adicional |
| Form Builder | NO_CHANGE_REQUIRED | solo se audito carga; diseno y formulas quedan fuera de B02 |
| Main Layout | MODIFIED | no muestra spinner por GET ordinario ni aplica aria-busy global |

## Seguridad y limites

No se modificaron autenticacion, refresh tokens, antiforgery, logout, timeout,
autorizacion, backend, Oracle, migraciones, auditoria, P06, P07, main,
produccion ni watcher. Los errores HTTP y los mensajes de confirmacion se
conservan. GitHub Actions no se usa como verificador de esta fase.

## Evidencia local

La evidencia debe registrar conteos reales de la ejecucion final. Las pruebas
heredadas de B01 se mantienen como referencia y no se presentan como una nueva
ejecucion. La aceptacion del propietario de B01 queda registrada en Issue #34.
