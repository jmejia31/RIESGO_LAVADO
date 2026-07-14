# Historial de cambios

Todos los cambios notables se documentan en este archivo. El proyecto sigue categorías inspiradas en Keep a Changelog.

## [No publicado]

### Fase 7 de reorganización - 2026-07-14

- Se completó la limpieza y validación integral del repositorio sobre `main`.
- Se actualizó el informe final con las suites Backend/Frontend y los riesgos residuales conocidos.
- Se añadió validación automática de enlaces Markdown locales y se incorporó al control estructural.
- Se actualizó el README del frontend para reflejar Angular CLI 22 y el flujo real de trabajo.
- Se verificó que Git no rastree artefactos, configuración local ni patrones de secretos de alta confianza.

### Fase 6 de reorganización - 2026-07-14

- Se preservaron en la raíz los scripts Oracle históricos para mantener compatibilidad con instalaciones existentes.
- Se documentó `19_matrices_riesgos` como paquete modular con punto de entrada y orden interno únicos.
- Se añadió un validador del grafo de includes, orden de maestros, cobertura del manifiesto y cierre de solo lectura.
- Se blindó estáticamente el flujo de actualización segura contra operaciones destructivas alcanzables de forma recursiva.
- Se validaron en Oracle, sin escrituras, los módulos activos y las rutas reservadas del `2` al `10`.

### Fase 5 de reorganización - 2026-07-14

- Se migraron Listas y Matrices de Riesgos a módulos verticales bajo `Features`.
- Se separaron contratos, interfaces, aplicación, dominio de cálculo y persistencia Oracle.
- Se conservaron endpoints, autorizaciones, SQL y reglas funcionales existentes.
- Se actualizaron las pruebas del motor de cálculo y se añadieron pruebas de límites HTTP.
- Se amplió el validador para impedir el regreso de ambos módulos a carpetas heredadas por tipo.

### Fase 4 de reorganización - 2026-07-14

- Se migró Catálogos a un módulo vertical con `Application`, `Domain` y `Persistence`.
- Se conservaron las rutas, autorizaciones y respuestas públicas del controlador.
- Se añadieron pruebas del servicio y de los contratos públicos de roles y módulos.
- Se amplió el validador para impedir que Catálogos regrese a las carpetas heredadas por tipo.
- Se incorporó la actualización proporcionada por el usuario al documento funcional de gestión de riesgos.

### Fase 3 de reorganización - 2026-07-14

- Se extrajeron a HTML las plantillas inline de monitoreo de listas, configuración y bitácora.
- Se creó un componente presentacional para la tabla de matrices filtradas.
- Se migraron las pantallas a carga diferida con `loadComponent`, preservando URLs y guards.
- El paquete inicial de producción se redujo de aproximadamente `1.25 MB` a `375 KB`.
- Se añadieron pruebas y validaciones estructurales para carga diferida y plantillas externas.

### Fase 2 de reorganización - 2026-07-14

- Se agruparon autenticación, configuración y catálogos globales por responsabilidad dentro de `core`.
- Se movieron los servicios de Active Directory y auditoría a las funcionalidades de usuarios y bitácora.
- Se crearon límites `data-access` y `models` para listas y matrices de riesgos.
- Se actualizaron los imports sin cambiar rutas Angular, endpoints ni contratos HTTP.
- Se amplió el validador para impedir el regreso de archivos a `core/services` y `core/models`.

### Fase 1 de reorganización - 2026-07-14

- Se aprobó la arquitectura híbrida por funcionalidad y responsabilidad para backend, frontend y Oracle.
- Se documentaron rutas objetivo, dependencias permitidas, convenciones de archivos y criterios para código compartido.
- Se definió el plan de migración en fases con controles y reversión por commit.
- Se añadió `tools/validate_repository_structure.ps1` para detectar rutas obligatorias ausentes, artefactos rastreados e includes SQL rotos.

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
