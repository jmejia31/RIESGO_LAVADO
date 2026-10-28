# Guía de validación manual H08

Esta guía cubre la aceptación funcional manual posterior a la certificación automatizada H08. La automatización usa una familia sintética y Oracle XE descartable; esta guía debe ejecutarse en un ambiente no productivo autorizado.

Estado inicial de esta guía: `PENDING_USER_EXECUTION`. Codex no marca como ejecutada ninguna actividad que deba realizar el usuario.

## Acceso y nomenclatura real

1. Ingrese al sistema con un usuario que tenga el rol `ADMINISTRADOR`.
2. Abra el módulo `Matrices de Riesgos`.
3. Use la pestaña `Plantillas` para administrar familias y versiones.
4. Use la pestaña `Evaluaciones` para registrar evaluaciones.
5. Las acciones de familia disponibles son `Desactivar familia`, `Activar familia`, `Archivar familia` y `Restaurar familia`. Las justificaciones se capturan en los modales institucionales correspondientes.

## Ejercicios

### 1. Crear una familia

En `Matrices de Riesgos > Plantillas`, seleccione `Crear una nueva familia`. Complete `Código`, `Nombre` y, si corresponde, `Descripción`; guarde con `Guardar cambios de la familia`. Busque el código en la tabla y confirme que aparece con estado `ACTIVA` o `INACTIVA` según el flujo elegido.

### 2. Crear y revisar una versión

Abra `Ver detalle` de la familia y seleccione `Crear una nueva versión en esta familia`. En el `Form Builder`, agregue al menos un campo representativo, configure su etiqueta, tipo y validación, guarde la definición y revise la vista previa. No utilice una definición vacía como evidencia de un formulario funcional.

### 3. Publicar y controlar vigencia

Desde `Detalle de Familia`, use `Publicar versión`. Confirme el mensaje institucional y verifique `PUBLICADA` y `VIGENTE`. Después use `Desactivar versión`, confirme el motivo si se solicita y verifique `PUBLICADA` y `NO VIGENTE`. No edite una versión histórica publicada.

### 4. Registrar la primera evaluación

En `Evaluaciones`, seleccione `Crear nueva evaluación`, elija la familia y el `Riesgo Institucional a Evaluar`, complete los campos del formulario y pulse `Guardar borrador`. Confirme que la evaluación aparece en la tabla con la versión utilizada.

### 5. Desactivar y archivar

Retire primero la vigencia de la versión cuando el sistema lo requiera. En `Plantillas`, seleccione `Desactivar familia`, confirme la operación y luego `Archivar familia`. Escriba el motivo en `Justificación del archivado` y seleccione `Confirmar archivado`. Verifique que desaparece del filtro `No archivadas`.

### 6. Restaurar

Cambie el filtro `Archivo` a `Archivadas`, seleccione `Restaurar familia`, confirme el mensaje y escriba el motivo en `Justificación de la restauración`. Seleccione `Confirmar restauración`. Verifique que la familia vuelve como `INACTIVA`, sin que se publiquen o activen versiones automáticamente.

### 7. Activar nuevamente

Seleccione `Activar familia` y confirme. Abra `Ver detalle`, revise las versiones históricas, seleccione `Activar versión` sobre una versión `PUBLICADA` válida y verifique `VIGENTE`. Confirme que una familia archivada nunca permite activar una versión.

### 8. Registrar una segunda evaluación

En `Evaluaciones`, seleccione nuevamente la familia y el riesgo. Confirme que aparece la versión vigente, complete datos diferentes de la primera evaluación y use `Guardar borrador`. Verifique que el identificador de evaluación es distinto y que la referencia de versión es válida.

### 9. Revisar auditoría

Abra `Ver detalle` y seleccione `Ver historial completo`. Identifique creación, publicación, desactivación, archivado, restauración y activación. Compruebe usuario, fecha, orden y motivo. Si una transición no aparece, detenga la aceptación y regístrela como `MANUAL_VALIDATION_REQUIRED`.

## Evidencia manual

| STEP_ID | ACTION | EXPECTED_RESULT | ACTUAL_RESULT | SCREENSHOT_REFERENCE | USER_VALIDATION | STATUS |
|---|---|---|---|---|---|---|
| H08-M01 | Crear familia | Familia visible en `Plantillas` | Pendiente | Pendiente | Pendiente | `PENDING_USER_EXECUTION` |
| H08-M02 | Crear versión y configurar campos | Definición guardada y revisada | Pendiente | Pendiente | Pendiente | `PENDING_USER_EXECUTION` |
| H08-M03 | Publicar y retirar vigencia | Estados publicados verificables | Pendiente | Pendiente | Pendiente | `PENDING_USER_EXECUTION` |
| H08-M04 | Crear primera evaluación | Evaluación persistida con versión válida | Pendiente | Pendiente | Pendiente | `PENDING_USER_EXECUTION` |
| H08-M05 | Desactivar y archivar | Familia archivada con motivo | Pendiente | Pendiente | Pendiente | `PENDING_USER_EXECUTION` |
| H08-M06 | Restaurar y activar | Familia activa; historial conservado | Pendiente | Pendiente | Pendiente | `PENDING_USER_EXECUTION` |
| H08-M07 | Crear segunda evaluación | Segundo ID y referencia válidos | Pendiente | Pendiente | Pendiente | `PENDING_USER_EXECUTION` |
| H08-M08 | Consultar auditoría | Eventos, usuarios, fechas y motivos visibles | Pendiente | Pendiente | Pendiente | `PENDING_USER_EXECUTION` |

## Preparación para construir manualmente la Matriz de Riesgos LA/FT de 82 campos

Esta capacitación se reserva para un ambiente no productivo autorizado y no forma parte del fixture H08. El administrador debe crear una familia de capacitación, crear una versión, abrir el `Form Builder`, diseñar secciones, agregar los 82 campos, configurar catálogos y validaciones, incorporar únicamente cálculos aprobados, verificar campo por campo, publicar y registrar evaluaciones. Después debe practicar actualización permitida, controles, mitigación, seguimiento, consolidados, auditoría y exportaciones, verificando la paridad institucional entre PDF y Excel. No se deben alterar los 82 campos ni las 34 fórmulas institucionales durante la certificación H08.

## Clasificación de resultados

- `PASS`: el usuario ejecutó el paso y conservó evidencia.
- `PENDING_USER_EXECUTION`: aún no ejecutado por el usuario.
- `MANUAL_VALIDATION_REQUIRED`: la automatización no cubre una observación visual o funcional específica.
- `BLOCKED`: el ambiente, permisos o contrato impiden ejecutar el paso; registrar la causa exacta.
