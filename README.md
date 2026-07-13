# Sistema de Gestión de Riesgo de Lavado de Activos

Aplicación institucional para administrar usuarios, configuración, listas de cautela, coincidencias, evidencias, auditoría y matrices de riesgos. El repositorio reúne una SPA Angular, una API ASP.NET Core y scripts Oracle controlados.

## Arquitectura y tecnologías

- `frontend/rl-app`: Angular 22, TypeScript 6, RxJS, Tailwind CSS, jsPDF y XLSX.
- `backend/RL.API`: ASP.NET Core 10, controladores REST, JWT, Oracle Managed Data Access, Serilog y Swagger.
- `database`: scripts Oracle SQL/PLSQL de instalación, actualización, validación y respaldo.
- `docs`: documentación funcional, técnica, evidencias y entregables históricos.

El navegador consume `/api`; la API aplica autenticación/autorización y delega negocio a servicios y persistencia a repositorios Oracle. Consulte [ARCHITECTURE.md](ARCHITECTURE.md).

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
- Orden, dependencias y precauciones: [DATABASE.md](DATABASE.md) y `database/00_MANIFIESTO_SCRIPTS_APROBADOS.md`.

Nunca ejecute el flujo de primera instalación sobre datos productivos. Todo cambio requiere respaldo, prueba previa y aprobación DBA.

## Compilación y pruebas

```powershell
dotnet build RIESGO_LAVADO.sln --no-restore
dotnet test RIESGO_LAVADO.sln --no-build --no-restore
cd frontend/rl-app
npm run build
npm test -- --watch=false
```

Actualmente la solución no contiene un proyecto .NET de pruebas; `dotnet test` valida la solución pero no descubre casos. Angular contiene pruebas Vitest.

## Despliegue y seguridad

Consulte [DEPLOYMENT.md](DEPLOYMENT.md) antes de publicar y [SECURITY.md](SECURITY.md) para el manejo de secretos, archivos cargados, JWT y reporte de vulnerabilidades. No versionar compilaciones, logs, datos de ejecución, evidencias cargadas ni configuración local.

## Documentación

- [ARCHITECTURE.md](ARCHITECTURE.md): componentes, límites y flujos.
- [API.md](API.md): superficies REST y autenticación.
- [DATABASE.md](DATABASE.md): ejecución Oracle y reversión.
- [CONTRIBUTING.md](CONTRIBUTING.md): flujo de contribución.
- [CHANGELOG.md](CHANGELOG.md): cambios relevantes.
- [CLEANUP_REPORT.md](CLEANUP_REPORT.md): evidencia de la limpieza integral.
- `docs/`: documentación modular, funcional e histórica.
