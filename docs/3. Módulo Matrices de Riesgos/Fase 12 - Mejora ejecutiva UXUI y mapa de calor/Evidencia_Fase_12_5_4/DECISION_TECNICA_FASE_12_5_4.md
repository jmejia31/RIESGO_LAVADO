# Decisión técnica — Fase 12.5.4

## Endpoint público de recálculo

El inventario del repositorio no identificó consumidores funcionales del endpoint `/{id}/recalcular`. Las únicas referencias activas correspondían a la declaración del controlador, un método no utilizado del servicio Angular y su prueba aislada. Por ello se retira la superficie pública separada, manteniendo el cálculo automático posterior a crear o editar mediante `/{id}/calcular`.

La lógica interna de cálculo, persistencia, versionado de resultados y auditoría no se modifica.

## Codificación

La única cadena dañada localizada en código activo se conserva como patrón de compatibilidad mediante escapes Unicode seguros. Los archivos de evidencia histórica no se reescriben para preservar su integridad y trazabilidad.

## UX y accesibilidad

Se incorporan estados de exportación diferenciados, prevención de solicitudes duplicadas, mensajes de bloqueo contextual, regiones de estado accesibles, etiquetas de filtros, navegación identificada y semántica de diálogo.
