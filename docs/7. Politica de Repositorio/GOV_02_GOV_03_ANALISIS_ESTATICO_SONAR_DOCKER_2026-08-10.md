# GOV-02 + GOV-03 — Analisis estatico / Sonar y Docker multietapa

Fecha: 2026-08-10
Rama: `desarrollo`
Estado: IMPLEMENTADO; certificacion CI final pendiente del HEAD de cierre.

## 1. Objetivo

Cerrar el ultimo bloque del Plan de Mejoras Integrales sin modificar reglas de negocio, Oracle ni Produccion:

- GOV-02: establecer analisis estatico reproducible para Backend y Frontend, con una adopcion incremental que no convierta deuda heredada en una refactorizacion masiva, y dejar SonarQube Cloud preparado sin versionar secretos ni inventar identificadores externos.
- GOV-03: contenerizar Backend y Frontend mediante builds multietapa, runtimes minimos y ejecucion no-root.

## 2. GOV-02 — Analizadores locales y linea base incremental

### Backend .NET

`Directory.Build.props` aplica a la solucion:

- `AnalysisLevel=10-recommended`, fijando el conjunto recomendado correspondiente a .NET 10;
- analizadores habilitados durante build e IDE;
- warnings propios del compilador bloqueantes mediante `TreatWarningsAsErrors=true`;
- diagnostics CA heredados visibles pero no promovidos masivamente a error mediante `CodeAnalysisTreatWarningsAsErrors=false`;
- deuda IDE de estilo no se incorpora como bloqueo masivo en esta primera ola (`EnforceCodeStyleInBuild=false`).

El primer ensayo estricto de GOV-02 ejecutado con `-warnaserror` en Quality Gates #694 evidencio 184 diagnostics CA/IDE ya presentes en la base de codigo. Entre ellos existen reglas de mantenimiento, rendimiento, estilo y seguridad como CA2100 en construccion de comandos SQL internos. Convertir las 184 observaciones en errores dentro de esta fase ampliaria el alcance hacia una refactorizacion transversal no autorizada y elevaria el riesgo funcional.

Por ello, GOV-02 adopta el patron de linea base incremental recomendado para un sistema existente:

1. los analizadores se ejecutan en CI y sus hallazgos permanecen visibles;
2. los warnings del compilador C# continuan siendo bloqueantes;
3. no se agregan `NoWarn` globales ni supresiones masivas para ocultar deuda;
4. el frontend mantiene ESLint como gate bloqueante;
5. SonarQube Cloud queda preparado para gobernar deuda nueva mediante su Quality Gate cuando las credenciales/variables institucionales sean configuradas;
6. los diagnostics heredados deben abordarse en bloques tecnicos separados, priorizando seguridad/correctitud antes que estilo o micro-optimizaciones.

Esta separacion sigue la semantica oficial de MSBuild: `CodeAnalysisTreatWarningsAsErrors=false` mantiene visibles los warnings CA aun cuando no se desea convertir la deuda de analisis existente en un fallo de compilacion.

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

La corrida `31426469159` confirmo el comportamiento seguro sin configuracion: el workflow completo termino correctamente, pero scanner, build analizado, coberturas y Quality Gate remoto fueron omitidos porque las tres configuraciones externas no estaban presentes. Por tanto, la integracion Sonar queda preparada, pero **no se afirma certificacion Sonar Cloud remota** hasta que esos valores sean provistos y exista una corrida real exitosa del Quality Gate externo.

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
- analizadores .NET ejecutados con warnings del compilador como errores y deuda CA heredada visible;
- ESLint bloqueante;
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
