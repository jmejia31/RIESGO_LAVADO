# GOV-02 + GOV-03 — Analisis estatico / Sonar y Docker multietapa

Fecha: 2026-08-10
Rama: `desarrollo`
Estado inicial de esta entrega: IMPLEMENTADO, pendiente de certificacion CI del commit tecnico.

## 1. Objetivo

Cerrar el ultimo bloque del Plan de Mejoras Integrales sin modificar reglas de negocio, Oracle ni Produccion:

- GOV-02: establecer analisis estatico reproducible y bloqueante para Backend y Frontend, y dejar SonarQube Cloud preparado sin versionar secretos ni inventar identificadores externos.
- GOV-03: contenerizar Backend y Frontend mediante builds multietapa, runtimes minimos y ejecucion no-root.

## 2. GOV-02 — Analizadores locales bloqueantes

### Backend .NET

`Directory.Build.props` aplica a la solucion:

- `AnalysisLevel=latest-recommended`;
- `AnalysisMode=Recommended`;
- `EnforceCodeStyleInBuild=true`;
- analizadores habilitados durante build y analisis en IDE.

El workflow `quality-gates.yml` ejecuta una compilacion adicional con `-p:RunAnalyzers=true -warnaserror`. Por tanto, cualquier warning emitido por el build/analyzers en CI bloquea la entrega.

### Frontend Angular/TypeScript

Se incorpora `frontend/rl-app/eslint.config.mjs` usando la infraestructura ESLint/TypeScript ya presente en `package-lock.json`, sin introducir una dependencia nueva solo para esta fase.

El perfil base bloquea, entre otros:

- `debugger`;
- `eval`;
- evaluacion implicita;
- `new Function`;
- `with`;
- `var`;
- comparaciones no estrictas.

`npm run lint` es una puerta obligatoria del Quality Gate.

## 3. SonarQube Cloud

Se incorpora `.github/workflows/sonar-analysis.yml` con SonarScanner for .NET 11.2.0.

No se versionan ni se inventan:

- token;
- organization;
- project key.

La integracion consume exclusivamente:

- Secret `SONAR_TOKEN`;
- Variable `SONAR_PROJECT_KEY`;
- Variable `SONAR_ORGANIZATION`.

Si los tres valores no existen, el workflow informa que Sonar remoto esta pendiente de configuracion externa y omite el scanner sin exponer valores. Cuando existen, el scanner espera el Quality Gate remoto (`sonar.qualitygate.wait=true`).

El frontend genera LCOV para que Sonar pueda consumir cobertura JavaScript/TypeScript. La cobertura Backend se prepara como OpenCover en el workflow Sonar.

Nota de certificacion: los analizadores locales forman parte del Quality Gate base y son certificables sin servicios externos. La certificacion remota Sonar solo puede afirmarse despues de configurar las tres credenciales/variables institucionales y observar un Quality Gate remoto exitoso.

## 4. GOV-03 — Docker multietapa

### Backend

`backend/RL.API/Dockerfile`:

1. restaura y publica con `mcr.microsoft.com/dotnet/sdk:10.0`;
2. copia solo artefactos publicados a `mcr.microsoft.com/dotnet/aspnet:10.0`;
3. ejecuta como usuario no-root `app`;
4. expone puerto 8080;
5. conserva `/healthz` como liveness sin consultar Oracle;
6. prepara directorios escribibles para logs y evidencias;
7. deshabilita diagnosticos .NET en runtime mediante `DOTNET_EnableDiagnostics=0`.

### Frontend

`frontend/rl-app/Dockerfile`:

1. compila Angular con Node 24.18.0;
2. usa Nginx 1.31.3 Alpine como runtime separado;
3. ejecuta como usuario no-root `nginx`;
4. escucha en puerto 8080;
5. sirve la SPA con fallback a `index.html`;
6. enruta `/api/` y `/hubs/` hacia el servicio interno `backend:8080`;
7. incluye healthcheck propio sin dependencia de Oracle.

## 5. Correccion necesaria para build productivo Angular

Antes de GOV-03 existian `environment.ts` y `environment.prod.ts`, pero `angular.json` no declaraba `fileReplacements`.

Se agrega el reemplazo explicito:

`src/environments/environment.ts` -> `src/environments/environment.prod.ts`

Con ello el build productivo utiliza rutas relativas `/api` y `/hubs` en lugar de las URLs locales de desarrollo.

## 6. Compose local/controlado

`compose.yml` permite construir ambos servicios sin almacenar credenciales.

Exige desde el entorno:

- `RL_ORACLE_CONNECTION_STRING`;
- `RL_JWT_SECRET`.

Issuer, audience y origen frontend admiten valores no sensibles por defecto para uso controlado local. El archivo no autoriza ni ejecuta despliegue en Produccion.

## 7. Quality Gates

El workflow principal debe certificar en el mismo HEAD:

- validador GOV-02/GOV-03;
- analizadores .NET con warnings como errores;
- ESLint;
- controles DB/Oracle existentes;
- FE-01 y FE-03/FE-04;
- build Release;
- Backend/Frontend unitarios y cobertura;
- E2E;
- `docker compose config` con valores CI ficticios;
- build de las dos imagenes Docker;
- comprobacion de usuario final no-root.

## 8. Restricciones preservadas

- `main` no se modifica.
- PR #20 permanece abierto, draft y no fusionado.
- Oracle no se conecta ni ejecuta por esta fase.
- No se ejecutan scripts 05/06.
- No se modifican ni eliminan `B10_*`.
- Produccion permanece fuera de alcance.
- No se versionan secretos, tokens ni connection strings reales.
