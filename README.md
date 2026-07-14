# Sistema de Gestión de Riesgo de Lavado de Activos

Aplicación institucional para administrar usuarios, configuración, listas de cautela, coincidencias, evidencias, auditoría y matrices de riesgos. El repositorio reúne una SPA Angular, una API ASP.NET Core y scripts Oracle controlados.

## Arquitectura y tecnologías

- `frontend/rl-app`: Angular 22, TypeScript 6, RxJS, Tailwind CSS, jsPDF y XLSX.
- `backend/RL.API`: ASP.NET Core 10, controladores REST, JWT, Oracle Managed Data Access, Serilog y Swagger.
- `database`: scripts Oracle SQL/PLSQL de instalación, actualización, validación y respaldo.
- `docs`: documentación funcional, técnica, evidencias y entregables históricos.

El navegador consume `/api`; la API aplica autenticación/autorización y delega negocio a servicios y persistencia a repositorios Oracle. Consulte la [arquitectura](docs/0.0%20Documentación/ARCHITECTURE.md).

## Requisitos previos

- Node.js `24.18.0` y npm `11.12.1` (versiones declaradas por el frontend).
- .NET SDK `10.0.x`.
- Oracle y SQL*Plus compatibles con los scripts del proyecto.
- Credenciales institucionales y acceso de red entregados por canales seguros.

## Configuración

1. Copie `backend/RL.API/appsettings.example.json` como `backend/RL.API/appsettings.json`.
2. Reemplace únicamente los marcadores locales de Oracle, JWT, Active Directory y SMTP. `appsettings.json` está ignorado y nunca debe confirmarse en Git.
3. Para desarrollo, el frontend usa `http://localhost:5043/api`; producción usa `/api` mediante `src/environments/environment.prod.ts`.

No se requieren archivos `.env` actualmente. Si se incorporan, versionar solo `.env.example` sin secretos.

## Instalación y ejecución

Backend:

```powershell
dotnet restore RIESGO_LAVADO.sln --configfile NuGet.Config
dotnet run --project backend/RL.API/RL.API.csproj
```

Frontend, en otra terminal:

```powershell
cd frontend/rl-app
npm ci
npm start
```

La interfaz queda normalmente en `http://localhost:4200`; Swagger está disponible en `/swagger` cuando la API corre en ambiente Development.

## Base de datos

- Instalación nueva aprobada: `database/00_EJECUCION_PRIMERA_VEZ.sql`.
- Actualización de un esquema existente: `database/00_EJECUCION_ACTUALIZACIONES_SEGURAS.sql`.
- Orden, dependencias y precauciones: [guía de base de datos](docs/0.0%20Documentación/DATABASE.md) y `database/00_MANIFIESTO_SCRIPTS_APROBADOS.md`.

Nunca ejecute el flujo de primera instalación sobre datos productivos. Todo cambio requiere respaldo, prueba previa y aprobación DBA.

## Compilación y pruebas

```powershell
dotnet build RIESGO_LAVADO.sln --no-restore
dotnet test RIESGO_LAVADO.sln --no-build --no-restore
cd frontend/rl-app
npm run build
npm test -- --watch=false
```

La solución incluye pruebas unitarias del motor de matrices en `backend/RL.API.Tests`; Angular contiene pruebas Vitest. Toda modificación funcional debe mantener ambas suites aprobadas.

## Despliegue y seguridad

Consulte [despliegue](docs/0.0%20Documentación/DEPLOYMENT.md) antes de publicar y [seguridad](docs/0.0%20Documentación/SECURITY.md) para el manejo de secretos, archivos cargados, JWT y reporte de vulnerabilidades. No versionar compilaciones, logs, datos de ejecución, evidencias cargadas ni configuración local.

## Documentación

- [Arquitectura](docs/0.0%20Documentación/ARCHITECTURE.md): componentes, límites y flujos.
- [Estructura objetivo](docs/0.0%20Documentación/ESTRUCTURA_OBJETIVO.md): organización híbrida por funcionalidad y responsabilidad.
- [Plan de reorganización](docs/0.0%20Documentación/PLAN_REORGANIZACION.md): fases, orden, controles y reversión.
- [API](docs/0.0%20Documentación/API.md): superficies REST y autenticación.
- [Base de datos](docs/0.0%20Documentación/DATABASE.md): ejecución Oracle y reversión.
- [Contribución](docs/0.0%20Documentación/CONTRIBUTING.md): flujo de contribución.
- [Cambios](docs/0.0%20Documentación/CHANGELOG.md): cambios relevantes.
- [Informe de limpieza](docs/0.0%20Documentación/CLEANUP_REPORT.md): evidencia de la limpieza integral.
- `docs/`: documentación modular, funcional e histórica.
