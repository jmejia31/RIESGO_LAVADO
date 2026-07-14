# Historial de cambios

Todos los cambios notables se documentan en este archivo. El proyecto sigue categorías inspiradas en Keep a Changelog.

## [No publicado]

### Cierre técnico previo a reorganización - 2026-07-14

- Se centralizó el paquete Oracle activo de matrices bajo `database/19_matrices_riesgos`.
- Los maestros incluyen comentarios, matrices y validación final de módulos 2 a 10.
- Las respuestas HTTP 500 dejaron de exponer mensajes internos y ahora incluyen `traceId`.
- Se consolidó la documentación técnica bajo `docs/0.0 Documentación`, manteniendo el README principal en la raíz.
- Se añadió una base de pruebas unitarias para el motor de cálculo de matrices.

### Añadido

- Documentación raíz de arquitectura, API, base de datos, despliegue, seguridad y contribución.
- Informe reproducible de limpieza integral.

### Cambiado

- Exclusiones Git ampliadas para artefactos, caches, logs y configuración local sensible.

### Eliminado

- Scaffold Angular sin referencias de `coincidencias-empleado`; se conserva la implementación funcional `.component.*`.

## [0.1.0] - 2026-06-18

### Añadido

- Línea base del sistema y módulos funcionales documentados en el historial Git y `docs`.
