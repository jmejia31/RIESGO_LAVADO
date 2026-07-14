# Arquitectura

## Contexto

El sistema implementa una arquitectura web de tres capas desplegables: SPA Angular, API REST ASP.NET Core y Oracle. La API es el único componente que accede a la base de datos y a integraciones institucionales.

## Frontend

`frontend/rl-app/src/app` se organiza en:

- `core`: modelos, servicios HTTP, guards e interceptores de autenticación y confirmación.
- `features`: pantallas por capacidad funcional.
- `shared`: layout y páginas transversales.
- `app.routes.ts`: rutas y permisos por identificador de módulo.

Los módulos 2 a 10 corresponden a usuarios, configuración, monitoreo, bitácora, tipos y carga de listas, coincidencias y matrices. El token JWT se adjunta mediante interceptor; los guards validan sesión, rol y módulo.

## Backend

`backend/RL.API` mantiene estas responsabilidades:

- `Controllers`: contratos HTTP, autorización y códigos de respuesta.
- `Services`: reglas funcionales, validaciones y coordinación.
- `Repositories`: consultas y comandos Oracle.
- `DTOs` y `Models`: contratos de transporte y representación.
- `Security` y `Middleware`: autorización por módulo, marcadores de operaciones auditables y manejo uniforme de errores. La auditoría se persiste explícitamente desde servicios o repositorios dentro de la transacción funcional.
- `Infrastructure/OracleDbContext.cs`: creación de conexiones Oracle.

`Program.cs` compone dependencias, JWT, CORS, Swagger, Serilog y el pipeline HTTP. Los contratos públicos no deben cambiarse sin versionado y coordinación con el frontend.

## Datos e integraciones

Oracle conserva usuarios, permisos, configuración, auditoría, listas, evidencias y matrices. Active Directory autentica dominios configurados; SMTP soporta recuperación/notificaciones. Los archivos cargados se almacenan fuera de Git bajo rutas configurables.

## Decisiones de seguridad

- Configuración real en `appsettings.json` local ignorado; el repositorio incluye solo una plantilla.
- JWT valida emisor, audiencia, firma y expiración sin tolerancia de reloj.
- CORS se limita a orígenes configurados.
- Endpoints sensibles requieren JWT, rol o módulo.
- Logs y cargas no son artefactos versionables.
