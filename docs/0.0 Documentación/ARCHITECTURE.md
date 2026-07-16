# Arquitectura

> La estructura actual se migrará gradualmente hacia la arquitectura híbrida definida en [ESTRUCTURA_OBJETIVO.md](ESTRUCTURA_OBJETIVO.md). El orden y los controles de esa migración están en [PLAN_REORGANIZACION.md](PLAN_REORGANIZACION.md).

## Contexto

El sistema implementa una arquitectura web de tres capas desplegables: SPA Angular, API REST ASP.NET Core y Oracle. La API es el único componente que accede a la base de datos y a integraciones institucionales.

## Frontend

`frontend/rl-app/src/app` se organiza en:

- `core`: modelos, servicios HTTP, guards e interceptores de autenticación y confirmación.
- `features`: módulos por capacidad funcional; cada pantalla enrutada vive bajo `pages`.
- `shared`: layout y páginas transversales.
- `app.routes.ts`: rutas y permisos por identificador de módulo.

Los módulos 2 a 10 corresponden a usuarios, configuración, monitoreo, bitácora, tipos y carga de listas, coincidencias y matrices. El token JWT se adjunta mediante interceptor; los guards validan sesión, rol y módulo.

La estructura actual separa `core`, `features` y `shared`. `core` conserva autenticación y configuración global, mientras usuarios, bitácora, listas y matrices de riesgos son propietarios de su acceso HTTP y sus modelos. Desde la fase 3, las pantallas se cargan bajo demanda, las plantillas grandes están separadas de la lógica TypeScript y matrices de riesgos comenzó su división en componentes presentacionales.

## Backend

`backend/RL.API` mantiene estas responsabilidades:

- `Features/<Modulo>`: controlador y capas `Application`, `Contracts` y `Persistence`; `Domain` e `Integrations` se agregan solo cuando existe una responsabilidad real.
- `Core/Security`: autorización por módulo y marcadores de operaciones auditables.
- `Infrastructure/Database`: creación de conexiones Oracle.
- `Middleware`: manejo uniforme de errores HTTP.
- `Shared/Identifiers` y `Shared/Results`: utilidades transversales con consumidores reales.

`Program.cs` compone dependencias, JWT, CORS, Swagger, Serilog y el pipeline HTTP. Los contratos públicos no deben cambiarse sin versionado y coordinación con el frontend.

`Features/Auditoria`, `Features/Catalogos`, `Features/Configuracion`, `Features/Identidad`, `Features/Listas` y `Features/MatricesRiesgos` funcionan como módulos verticales: sus controladores dependen de `Application`, los casos de uso dependen de abstracciones y `Persistence` contiene Oracle. Auditoría es una capacidad transversal: su implementación pertenece al módulo y otros módulos dependen únicamente de `IAuditoriaRepository` para registrar eventos. Identidad agrupa Auth y Usuarios; Active Directory y SMTP aparecen bajo `Integrations`, separados de contratos, dominio y persistencia. La localización y vigencia de refresh tokens pertenecen a `Persistence`; `Application` solo coordina su rotación. En Matrices de Riesgos, `Domain` conserva el motor de cálculo puro. Los resultados técnicos reutilizables viven en `Shared/Results`. La API continúa siendo un único desplegable.

Las carpetas heredadas por tipo `Controllers`, `DTOs`, `Models`, `Repositories`, `Services`, `Security` y `Helpers` fueron retiradas y su reaparición queda bloqueada por el validador estructural. El código nuevo debe pertenecer a un módulo funcional o a una responsabilidad transversal explícita bajo `Core`, `Infrastructure`, `Middleware` o `Shared`.

## Datos e integraciones

Oracle conserva usuarios, permisos, configuración, auditoría, listas, evidencias y matrices. Los scripts históricos permanecen en la raíz de `database` por compatibilidad y los módulos nuevos se incorporan como paquetes numerados con un único `00_APLICAR_*.sql`. Los dos maestros resuelven dependencias mediante `@@`, terminan en una validación de solo lectura y su grafo se comprueba con `tools/validate_database_scripts.ps1`. Active Directory autentica dominios configurados; SMTP soporta recuperación/notificaciones. Los archivos cargados se almacenan fuera de Git bajo rutas configurables.

## Decisiones de seguridad

- Configuración real en `appsettings.json` local ignorado; el repositorio incluye solo una plantilla.
- JWT valida emisor, audiencia, firma y expiración sin tolerancia de reloj.
- CORS se limita a orígenes configurados.
- Endpoints sensibles requieren JWT, rol o módulo.
- Logs y cargas no son artefactos versionables.
