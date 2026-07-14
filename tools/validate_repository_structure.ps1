param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
$errors = [System.Collections.Generic.List[string]]::new()
$documentationDirectoryName = '0.0 Documentaci' + [char]0x00F3 + 'n'

function Assert-RepositoryPath {
    param([string]$RelativePath)

    $absolutePath = Join-Path $RepositoryRoot $RelativePath
    if (-not (Test-Path -LiteralPath $absolutePath)) {
        $errors.Add("Falta la ruta obligatoria: $RelativePath")
    }
}

$requiredPaths = @(
    'README.md',
    'RIESGO_LAVADO.sln',
    'backend/RL.API/RL.API.csproj',
    'backend/RL.API.Tests/RL.API.Tests.csproj',
    'frontend/rl-app/package.json',
    'frontend/rl-app/src/app/core/auth/auth.service.ts',
    'frontend/rl-app/src/app/core/configuration/configuracion.service.ts',
    'frontend/rl-app/src/app/features/admin/listas/data-access/listas.service.ts',
    'frontend/rl-app/src/app/features/admin/listas/models/listas.models.ts',
    'frontend/rl-app/src/app/features/admin/matrices-riesgos/data-access/matrices-riesgos.service.ts',
    'frontend/rl-app/src/app/features/admin/matrices-riesgos/models/matrices-riesgos.models.ts',
    'frontend/rl-app/src/app/features/admin/bitacora/bitacora.component.html',
    'frontend/rl-app/src/app/features/admin/configuracion/configuracion.component.html',
    'frontend/rl-app/src/app/features/admin/monitoreo-listas/monitoreo-listas.component.html',
    'frontend/rl-app/src/app/features/admin/matrices-riesgos/components/matrices-reporte-tabla/matrices-reporte-tabla.component.ts',
    'database/00_EJECUCION_PRIMERA_VEZ.sql',
    'database/00_EJECUCION_ACTUALIZACIONES_SEGURAS.sql',
    'database/00_MANIFIESTO_SCRIPTS_APROBADOS.md',
    'database/19_matrices_riesgos/00_APLICAR_MODULO_MATRICES_RIESGOS.sql',
    "docs/$documentationDirectoryName/ARCHITECTURE.md",
    "docs/$documentationDirectoryName/ESTRUCTURA_OBJETIVO.md",
    "docs/$documentationDirectoryName/PLAN_REORGANIZACION.md"
)

foreach ($path in $requiredPaths) {
    Assert-RepositoryPath $path
}

$trackedFiles = @(git -C $RepositoryRoot ls-files)
if ($LASTEXITCODE -ne 0) {
    throw 'No fue posible consultar los archivos rastreados por Git.'
}

$forbiddenTrackedPattern = '(^|/)(bin|obj|dist|logs|Uploads|App_Data|tmp|tmp_build)/'
foreach ($file in $trackedFiles) {
    if ($file -match $forbiddenTrackedPattern) {
        $errors.Add("Artefacto de ejecución rastreado por Git: $file")
    }
}

$largeComponents = @(
    'frontend/rl-app/src/app/features/admin/bitacora/bitacora.component.ts',
    'frontend/rl-app/src/app/features/admin/configuracion/configuracion.component.ts',
    'frontend/rl-app/src/app/features/admin/monitoreo-listas/monitoreo-listas.component.ts'
)
foreach ($component in $largeComponents) {
    $componentPath = Join-Path $RepositoryRoot $component
    if ((Test-Path -LiteralPath $componentPath) -and (Get-Content -Raw -LiteralPath $componentPath) -match 'template\s*:\s*`') {
        $errors.Add("Componente grande con plantilla inline: $component")
    }
}

$routesPath = Join-Path $RepositoryRoot 'frontend/rl-app/src/app/app.routes.ts'
if (Test-Path -LiteralPath $routesPath) {
    $routesContent = Get-Content -Raw -LiteralPath $routesPath
    $lazyRouteCount = ([regex]::Matches($routesContent, 'loadComponent\s*:')).Count
    if ($lazyRouteCount -lt 12) {
        $errors.Add("Las pantallas enrutadas deben usar carga diferida. Encontradas: $lazyRouteCount; esperadas: 12 o mas.")
    }
}

$legacyFrontendPattern = '^frontend/rl-app/src/app/core/(models|services)/'
foreach ($file in $trackedFiles) {
    if ($file -match $legacyFrontendPattern) {
        $errors.Add("Archivo frontend en carpeta generica heredada: $file")
    }
}

$rootMarkdown = Get-ChildItem -LiteralPath $RepositoryRoot -File -Filter '*.md' |
    Where-Object { $_.Name -ne 'README.md' }
foreach ($file in $rootMarkdown) {
    $errors.Add("Documento general fuera del directorio tecnico de docs: $($file.Name)")
}

$sqlEntrypoints = @(
    'database/00_EJECUCION_PRIMERA_VEZ.sql',
    'database/00_EJECUCION_ACTUALIZACIONES_SEGURAS.sql',
    'database/19_matrices_riesgos/00_APLICAR_MODULO_MATRICES_RIESGOS.sql'
)

foreach ($entrypoint in $sqlEntrypoints) {
    $entrypointPath = Join-Path $RepositoryRoot $entrypoint
    if (-not (Test-Path -LiteralPath $entrypointPath)) {
        continue
    }

    $baseDirectory = Split-Path -Parent $entrypointPath
    foreach ($line in Get-Content -LiteralPath $entrypointPath) {
        if ($line -match '^@@(.+\.sql)\s*$') {
            $includedPath = Join-Path $baseDirectory $Matches[1]
            if (-not (Test-Path -LiteralPath $includedPath)) {
                $errors.Add("Include SQL inexistente: $entrypoint -> $($Matches[1])")
            }
        }
    }
}

if ($errors.Count -gt 0) {
    Write-Host 'Validacion estructural fallida:' -ForegroundColor Red
    foreach ($errorMessage in $errors) {
        Write-Host "- $errorMessage" -ForegroundColor Red
    }
    exit 1
}

Write-Host 'Validacion estructural correcta.' -ForegroundColor Green
Write-Host "Rutas obligatorias: $($requiredPaths.Count)"
Write-Host "Archivos rastreados revisados: $($trackedFiles.Count)"
Write-Host "Maestros SQL revisados: $($sqlEntrypoints.Count)"
