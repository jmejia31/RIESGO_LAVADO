---
name: riesgo-lavado-backend-aspnet
description: Implementa y revisa el backend ASP.NET Core de RIESGO_LAVADO en backend/RL.API y backend/RL.API.Tests. Usar para APIs, servicios, autenticación, autorización, Oracle, validaciones, contratos REST, seguridad y pruebas C#.
---

# Backend ASP.NET Core — RIESGO_LAVADO

## Objetivo

Realizar cambios backend seguros, compatibles y cubiertos por pruebas sin alterar contratos institucionales accidentalmente.

## Alcance base

- API: `backend/RL.API`.
- Pruebas: `backend/RL.API.Tests`.
- La versión real del runtime se obtiene del `.csproj`; no usar versiones históricas por memoria.
- Oracle es persistencia institucional: activar `riesgo-lavado-oracle-database` cuando el cambio toque SQL, modelos persistentes o migraciones.

## Flujo obligatorio

1. Recuperar contexto mediante `AGENTS.md`, bitácora, estado colaborativo y documentación del módulo.
2. Consultar CodexGraph para símbolos, dependencias, rutas e impacto cuando esté disponible.
3. Inspeccionar controlador, servicio, DTO/modelo, persistencia y pruebas afectados.
4. Mantener separación de responsabilidades; evitar lógica de negocio duplicada en controladores.
5. Preservar códigos HTTP, shapes JSON, nombres contractuales y reglas de autorización salvo cambio aprobado.
6. Mantener validación de entrada y manejo de errores explícito; no silenciar excepciones.
7. No introducir bypass de RBAC, JWT, auditoría o validaciones para hacer pasar una prueba.
8. Añadir regresión para cada corrección funcional o de seguridad.

## Seguridad

- No registrar tokens, passwords, connection strings ni datos sensibles.
- No debilitar autenticación/autorización para pruebas.
- Consultas Oracle parametrizadas; evitar concatenación de entrada del usuario.
- Conservar auditoría y trazabilidad en operaciones sensibles.

## Validación mínima según alcance

```powershell
dotnet restore RIESGO_LAVADO.sln --configfile NuGet.Config
dotnet build RIESGO_LAVADO.sln --no-restore
dotnet test RIESGO_LAVADO.sln --configuration Release --no-restore
```

Reportar conteos reales y cualquier integración institucional no reproducible localmente.
