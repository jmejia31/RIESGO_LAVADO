$ErrorActionPreference = 'Stop'

function Assert-Contains {
    param([string]$Path, [string]$Pattern, [string]$Message)
    if (-not (Test-Path $Path)) { throw "GOV-02/GOV-03: falta archivo requerido: $Path" }
    $content = Get-Content -Raw -Encoding UTF8 $Path
    if ($content -notmatch $Pattern) { throw "GOV-02/GOV-03: $Message [$Path]" }
}

function Assert-NotContains {
    param([string]$Path, [string]$Pattern, [string]$Message)
    if (-not (Test-Path $Path)) { throw "GOV-02/GOV-03: falta archivo requerido: $Path" }
    $content = Get-Content -Raw -Encoding UTF8 $Path
    if ($content -match $Pattern) { throw "GOV-02/GOV-03: $Message [$Path]" }
}

$props = 'Directory.Build.props'
Assert-Contains $props '<AnalysisLevel>10-recommended</AnalysisLevel>' 'los analizadores .NET deben fijar el conjunto recomendado de .NET 10'
Assert-Contains $props '<RunAnalyzersDuringBuild>true</RunAnalyzersDuringBuild>' 'los analizadores deben ejecutarse durante build'
Assert-Contains $props '<TreatWarningsAsErrors>true</TreatWarningsAsErrors>' 'los warnings del compilador deben ser bloqueantes'
Assert-Contains $props '<CodeAnalysisTreatWarningsAsErrors>false</CodeAnalysisTreatWarningsAsErrors>' 'la deuda CA heredada debe permanecer visible sin bloquear la adopcion inicial'
Assert-Contains $props '<EnforceCodeStyleInBuild>false</EnforceCodeStyleInBuild>' 'la primera ola no debe convertir deuda IDE de estilo heredada en bloqueo masivo'

$eslint = 'frontend/rl-app/eslint.config.mjs'
Assert-Contains $eslint "'no-debugger': 'error'" 'ESLint debe bloquear debugger'
Assert-Contains $eslint "'no-eval': 'error'" 'ESLint debe bloquear eval'
Assert-Contains $eslint "'no-implied-eval': 'error'" 'ESLint debe bloquear evaluacion implicita'
Assert-Contains $eslint "'no-new-func': 'error'" 'ESLint debe bloquear Function dinamico'
Assert-Contains $eslint "'eqeqeq': \['error', 'always'\]" 'ESLint debe exigir igualdad estricta'

$angular = 'frontend/rl-app/angular.json'
Assert-Contains $angular '"fileReplacements"' 'production debe reemplazar environment.ts'
Assert-Contains $angular '"with": "src/environments/environment.prod.ts"' 'production debe usar environment.prod.ts'
Assert-Contains $angular '"lcov"' 'la cobertura frontend debe producir LCOV para Sonar'

$backendDocker = 'backend/RL.API/Dockerfile'
Assert-Contains $backendDocker 'FROM mcr\.microsoft\.com/dotnet/sdk:10\.0 AS restore' 'backend debe tener stage SDK separado'
Assert-Contains $backendDocker 'FROM mcr\.microsoft\.com/dotnet/aspnet:10\.0 AS runtime' 'backend debe usar runtime ASP.NET separado'
Assert-Contains $backendDocker 'USER app' 'backend debe ejecutar como usuario no root app'
Assert-Contains $backendDocker 'HEALTHCHECK' 'backend debe declarar healthcheck'
Assert-Contains $backendDocker '/healthz' 'healthcheck backend debe usar liveness y no readiness Oracle'
Assert-NotContains $backendDocker '(?i)(password|secretkey)\s*=' 'Dockerfile backend no debe contener secretos hardcodeados'

$frontendDocker = 'frontend/rl-app/Dockerfile'
Assert-Contains $frontendDocker 'FROM node:24\.18\.0-alpine3\.24 AS build' 'frontend debe compilar en stage Node separado'
Assert-Contains $frontendDocker 'FROM nginx:1\.31\.3-alpine AS runtime' 'frontend debe usar runtime Nginx separado'
Assert-Contains $frontendDocker 'USER nginx' 'frontend debe ejecutar como usuario no root nginx'
Assert-Contains $frontendDocker 'HEALTHCHECK' 'frontend debe declarar healthcheck'

$nginx = 'frontend/rl-app/nginx.conf'
Assert-Contains $nginx 'listen 8080;' 'Nginx no root debe escuchar en puerto no privilegiado'
Assert-Contains $nginx 'proxy_pass http://backend:8080;' 'frontend debe enrutar API al servicio backend interno'
Assert-Contains $nginx 'try_files \$uri \$uri/ /index\.html;' 'SPA debe conservar fallback de rutas Angular'

$compose = 'compose.yml'
Assert-Contains $compose 'RL_ORACLE_CONNECTION_STRING:\?Defina RL_ORACLE_CONNECTION_STRING' 'compose debe exigir Oracle por variable externa'
Assert-Contains $compose 'RL_JWT_SECRET:\?Defina RL_JWT_SECRET' 'compose debe exigir JWT por variable externa'
Assert-NotContains $compose '(?im)^\s*ConnectionStrings__OracleDB:(?![ \t]*\$\{)' 'Oracle debe provenir de interpolacion de entorno y no de un valor literal'
Assert-NotContains $compose '(?im)^\s*Jwt__SecretKey:(?![ \t]*\$\{)' 'JWT secret debe provenir de interpolacion de entorno y no de un valor literal'

$sonar = '.github/workflows/sonar-analysis.yml'
Assert-Contains $sonar 'dotnet-sonarscanner --version 11\.2\.0' 'SonarScanner for .NET debe estar versionado'
Assert-Contains $sonar 'secrets\.SONAR_TOKEN' 'Sonar debe consumir token desde GitHub Secrets'
Assert-Contains $sonar 'vars\.SONAR_PROJECT_KEY' 'Sonar project key debe venir de variable de repositorio'
Assert-Contains $sonar 'vars\.SONAR_ORGANIZATION' 'Sonar organization debe venir de variable de repositorio'
Assert-Contains $sonar 'sonar\.qualitygate\.wait=true' 'Sonar debe esperar el Quality Gate remoto'
Assert-NotContains $sonar '(?i)SONAR_TOKEN:\s*[A-Za-z0-9_\-]{20,}' 'no debe existir token Sonar literal'

$quality = '.github/workflows/quality-gates.yml'
Assert-Contains $quality 'validate_gov02_gov03_quality_container\.ps1' 'Quality Gates debe validar GOV-02/GOV-03'
Assert-Contains $quality 'npm run lint' 'Quality Gates debe ejecutar ESLint'
Assert-Contains $quality 'RunAnalyzers=true' 'Quality Gates debe forzar analizadores .NET'
Assert-Contains $quality 'TreatWarningsAsErrors=true' 'Quality Gates debe bloquear warnings del compilador'
Assert-Contains $quality 'CodeAnalysisTreatWarningsAsErrors=false' 'Quality Gates debe conservar la linea base incremental para diagnostics CA heredados'
Assert-NotContains $quality '(?i)-warnaserror' 'Quality Gates no debe promover indiscriminadamente toda la deuda analyzer heredada'
Assert-Contains $quality 'docker build.*backend/RL\.API/Dockerfile' 'Quality Gates debe construir imagen backend'
Assert-Contains $quality 'docker build.*frontend/rl-app/Dockerfile' 'Quality Gates debe construir imagen frontend'

Write-Host 'VALIDACION GOV-02/GOV-03: CORRECTA.'
Write-Host 'Analisis estatico incremental, Sonar preparado y Docker multietapa no-root protegidos por contrato CI.'
