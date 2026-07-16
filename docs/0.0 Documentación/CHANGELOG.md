# Historial de cambios

Todos los cambios notables se documentan en este archivo. El proyecto sigue categorías inspiradas en Keep a Changelog.

## [No publicado]

### Cierre formal de la Fase 10 de Matrices de Riesgos - 2026-07-16

- Se cubrieron reactivacion de planes y vista previa documental; las suites alcanzaron 77/77 Backend, 123/123 Frontend y 6/6 E2E.
- Se agrego un E2E autenticado y autorizado del modulo 10 con identidad efimera y API simulada, sin credenciales ni escrituras Oracle.
- La cobertura quedo en 15.35%/16.36% Backend y 31.07%/26.97%/29.94%/31.26% Frontend, elevando los pisos anti-regresion.
- GitHub Actions ejecuta restauracion, dependencias, navegador y la puerta integral en cada cambio de `main` y pull request.
- Se incorporaron matriz de trazabilidad, politica de retencion, indice de evidencias y un script DBA reproducible de solo lectura.
- El documento oficial pasa a version 1.2, estado aprobada y cerrada, conforme a la autorizacion expresa del responsable del proyecto.

### Estandarización integral de módulos - 2026-07-16

- Backend quedó organizado por módulos con `Application`, `Contracts` y `Persistence`, y capas opcionales únicamente cuando contienen lógica real.
- Seguridad, Oracle e identificadores compartidos se ubicaron en `Core/Security`, `Infrastructure/Database` y `Shared/Identifiers`.
- Las páginas Angular se consolidaron bajo `pages`; las cinco capacidades de Listas quedaron dentro de un único módulo Frontend.
- Se retiraron carpetas heredadas y vacías, se preservaron los directorios de ejecución con datos locales y se reforzó el validador estructural.
- Las rutas HTTP, nombres públicos, contratos API y scripts Oracle permanecieron sin cambios.

### Reorganización de Contracts de Matrices de Riesgos - 2026-07-16

- Los 30 DTO del módulo se conservaron sin cambios de nombre, propiedades o namespace público.
- El archivo monolítico `MatricesRiesgosDto.cs` se separó en contratos de matrices, planes de acción, evidencias y reportería.
- El validador estructural ahora exige las seis ubicaciones nuevas para impedir regresiones de organización.

### Fortalecimiento de la Fase 10 de Matrices de Riesgos - 2026-07-16

- Se ocultaron las rutas físicas de evidencias en JSON y se limitaron las descargas al almacenamiento protegido.
- Se aplicó validación de firma real, MIME, extensión, tamaño, nombre y longitudes Oracle antes de almacenar archivos o planes.
- Las cargas incompletas eliminan archivos parciales y conservan el archivo cuando la metadata ya fue confirmada en Oracle.
- Se eliminaron responsables personales precargados de los formularios de controles y planes.
- Se añadieron 15 pruebas Backend y 16 Frontend; las suites alcanzaron 71/71, 115/115 y 5/5 E2E.
- La cobertura subió a 15.31%/16.10% Backend y 29.66%/26.05%/28.35%/29.79% Frontend, elevando todos los pisos.
- Estos pendientes quedaron resueltos en el cierre formal: evidencia API/auditoria, validacion DBA reproducible, navegacion autenticada, trazabilidad documental y aprobacion registrada.

### Fase 21 de auditoría integral y cierre - 2026-07-15

- Se confirmó `main` como única rama local/remota real, sincronizada antes de iniciar el cierre.
- Se auditaron estructura, archivos rastreados, artefactos, secretos, documentación, SQL e integridad Git.
- `appsettings.Development.json` se retiró del seguimiento y se añadió a `.gitignore`, conservando intacta la copia local.
- NuGet y npm reportaron cero vulnerabilidades conocidas sin modificar versiones de dependencias.
- Se aprobaron 56/56 pruebas Backend, 99/99 pruebas Frontend, 5/5 E2E y el build productivo de 374.97 KB.
- Se preservaron los documentos Word, incluido el entregable de usuario, y se cerró el plan sin fases de reorganización pendientes.

### Fase 20 de calidad Backend - 2026-07-15

- Se cubrieron reglas de aplicación de Listas para positivos, seguimientos, exportaciones auditadas y procesamiento de cargas.
- Se probaron creación, actualización, cálculo, estados, exportaciones y criterios de Matrices de Riesgos.
- Los repositorios, la auditoría y el motor de cálculo se sustituyeron por interfaces simuladas sin paquetes nuevos ni conexiones externas.
- La suite Backend aumentó de 30 a 56 pruebas en nueve archivos y mantuvo 99/99 pruebas Frontend y 5/5 recorridos E2E.
- La cobertura Backend subió a 13.79% de líneas y 13.10% de ramas.
- Se elevaron los pisos anti-regresión a 13.7% y 13.0% sin modificar código productivo ni datos Oracle.

### Fase 19 de calidad Frontend - 2026-07-15

- Se cubrieron exportaciones de lista principal y ficha de patrono condicionadas a auditoría obligatoria.
- Se verificó que los fallos de auditoría cancelen el archivo y recuperen el estado de la pantalla.
- Se probaron creación, edición, actualización y error de criterios de matrices.
- La suite Frontend aumentó de 91 a 99 pruebas en trece archivos y conservó 5/5 recorridos E2E.
- La cobertura Frontend subió a 28.31% de sentencias, 24.80% de ramas, 26.59% de funciones y 28.30% de líneas.
- Se elevaron los pisos anti-regresión sin escribir archivos ni registrar auditorías reales.

### Fase 18 de calidad Frontend - 2026-07-15

- Se cubrieron visualización, descarga por enlace y error de evidencias mediante APIs de navegador simuladas.
- Se probaron exportaciones EXCEL/PDF de matrices sin generar archivos reales.
- Se caracterizó la carga, reconstrucción, actualización y recálculo de matrices en modo edición.
- La suite Frontend aumentó de 82 a 91 pruebas en trece archivos y conservó 5/5 recorridos E2E.
- La cobertura Frontend subió a 23.74% de sentencias, 18.12% de ramas, 20.74% de funciones y 23.61% de líneas.
- Se elevaron los pisos anti-regresión sin modificar código productivo ni abrir archivos o ventanas reales.

### Fase 17 de calidad Frontend - 2026-07-15

- Se cubrió la política documental para tamaño máximo, extensiones autorizadas y archivos válidos.
- Se probaron cancelación, éxito y error de eliminaciones lógicas simuladas de evidencias y seguimientos.
- Se caracterizaron motivos únicos, cambios de estado, eliminación de matrices y mantenimiento de criterios.
- La suite Frontend aumentó de 70 a 82 pruebas en trece archivos y conservó 5/5 recorridos E2E.
- La cobertura Frontend subió a 22.27% de sentencias, 16.69% de ramas, 19.04% de funciones y 22.15% de líneas.
- Se elevaron los pisos anti-regresión sin modificar código productivo ni eliminar datos reales.

### Fase 16 de calidad Frontend - 2026-07-15

- Se cubrieron validaciones previas y escrituras simuladas de registros manuales y seguimientos de Listas.
- Se probaron bloqueos por matrices incompletas o duplicadas, creación con cálculo automático y errores controlados.
- Se caracterizaron campos obligatorios de criterios y el motivo requerido para recalcular matrices.
- La suite Frontend aumentó de 60 a 70 pruebas en trece archivos y conservó 5/5 recorridos E2E.
- La cobertura Frontend subió a 19.28% de sentencias, 14.49% de ramas, 15.21% de funciones y 18.91% de líneas.
- Se elevaron los pisos anti-regresión sin modificar código productivo ni acceder a datos reales.

### Fase 15 de calidad Frontend - 2026-07-15

- Se probaron la coordinación y recuperación ante errores de `MonitoreoListasComponent` y `MatricesRiesgosComponent`.
- Se cubrieron cargas por tipo, filtros, catálogos, política documental, seguimientos, reportes, selección e historial.
- Se verificó la propagación de errores HTTP desde los servicios sin realizar solicitudes reales.
- La suite Frontend aumentó de 46 a 60 pruebas en trece archivos y conservó 5/5 recorridos E2E.
- La cobertura Frontend subió a 13.89% de sentencias, 10.41% de ramas, 10.74% de funciones y 13.17% de líneas.
- Se elevaron los pisos anti-regresión y el validador exige las dos nuevas suites de componentes.

### Fase 14 de calidad Frontend - 2026-07-15

- Se cubrió el interceptor de confirmación para aceptación, cancelación, exclusiones y limpieza de cabeceras internas.
- Se probaron los contratos HTTP principales de `ListasService` y `MatricesRiesgosService` sin acceder a la API real.
- La suite Frontend aumentó de 31 a 46 pruebas en once archivos y conservó 5/5 recorridos E2E.
- La cobertura Frontend subió a 8.27% de sentencias, 8.92% de ramas, 7.35% de funciones y 7.23% de líneas.
- Se elevaron los pisos anti-regresión y el validador exige las tres nuevas suites.

### Fase 13 de calidad Frontend - 2026-07-15

- Se agregaron pruebas unitarias para los guards de autenticación, roles y módulos.
- Se cubrió el interceptor JWT al adjuntar tokens, renovar sesiones y manejar respuestas `401`/`403`.
- Se probaron `AuthService`, `ConfiguracionService` y `AuditoriaService` con `HttpTestingController`.
- La suite Frontend aumentó de 6 a 31 pruebas en ocho archivos.
- La cobertura Frontend subió a 5.85% de sentencias, 6.79% de ramas, 4.47% de funciones y 4.91% de líneas.
- Se elevaron los pisos anti-regresión y el validador exige las cinco nuevas suites.

### Fase 12 de cierre arquitectónico - 2026-07-15

- Se encapsuló en `UsuarioRepository` la consulta Oracle que localiza refresh tokens vigentes.
- Se agregaron pruebas de rotación, token inexistente y revocación para usuarios inactivos.
- Se movió `ServiceResult` a `Shared/Results` y se actualizaron Listas y Matrices de Riesgos.
- Se vaciaron las carpetas heredadas `Controllers`, `DTOs`, `Models`, `Repositories` y `Services`.
- El validador estructural ahora impide genéricamente reintroducir archivos en esas carpetas.
- La suite Backend alcanzó 30/30 pruebas y 7.11% de líneas y 6.78% de ramas.

### Fase 11 de reorganización - 2026-07-15

- Se migraron Auth y Usuarios al módulo vertical `Features/Identidad`.
- Se separaron aplicación, contratos, dominio, persistencia e integraciones de Active Directory y correo SMTP.
- Se preservaron rutas REST, permisos, claims y vigencias JWT, BCrypt, bloqueo por intentos, recuperación de contraseña y SQL Oracle.
- Se agregaron cuatro pruebas de caracterización con emisión real de JWT y validación del control de intentos.
- La suite Backend alcanzó 27/27 pruebas y la cobertura subió a 6.93% de líneas y 6.41% de ramas.
- El validador estructural exige el nuevo contexto y rechaza las nueve ubicaciones heredadas.

### Fase 10 de reorganización - 2026-07-15

- Se migró `Auditoria` a un módulo vertical con `Application`, `Contracts` y `Persistence`.
- Se preservaron ruta REST, módulos autorizados, filtros, paginación, SQL Oracle, resolución de IP y respuesta segura con `traceId`.
- Se trasladó la clasificación y registro de exportaciones desde el controlador a `AuditoriaService`.
- Se mantuvo `IAuditoriaRepository` como puerto transversal para los módulos que producen eventos auditables.
- Se agregaron ocho casos de caracterización; la suite Backend alcanzó 23/23 pruebas aprobadas.
- La cobertura Backend subió a 4.62% de líneas y 4.36% de ramas, elevando sus pisos anti-regresión.

### Fase 9 de reorganización - 2026-07-15

- Se migró `Configuracion` a un módulo vertical con `Application`, `Contracts` y `Persistence`.
- Se preservaron rutas REST, autorización por rol y módulo 3, respuestas públicas, SQL Oracle y reglas de carga de imágenes.
- Se trasladó la coordinación de auditoría desde el controlador a `ConfiguracionService` sin cambiar sus datos funcionales.
- Se agregaron cinco pruebas de caracterización; la suite Backend alcanzó 15/15 casos aprobados.
- La cobertura Backend subió a 3.88% de líneas y 3.21% de ramas, elevando sus pisos anti-regresión.
- Se amplió el validador estructural para exigir el nuevo módulo y rechazar las ubicaciones heredadas.

### Fase 8 de calidad - 2026-07-14

- Se añadió cobertura Backend con Coverlet y Frontend con V8 sobre el código completo de ambos componentes.
- Se definieron pisos iniciales anti-regresión sin presentar la cobertura baja actual como objetivo suficiente.
- Se incorporó Playwright 1.61.1 con cinco escenarios E2E no destructivos de login y enrutamiento.
- Se creó un ejecutor E2E que inicia y detiene Angular de forma controlada en Windows.
- Se añadió `tools/run_quality_gates.ps1` para ejecutar pruebas, cobertura, umbrales y E2E en una sola puerta.
- La auditoría npm posterior a la instalación informó cero vulnerabilidades.

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
