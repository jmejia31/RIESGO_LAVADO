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

function Assert-NonEmptyDirectory {
    param([string]$RelativePath)

    $absolutePath = Join-Path $RepositoryRoot $RelativePath
    if (-not (Test-Path -LiteralPath $absolutePath -PathType Container)) {
        $errors.Add("Falta la carpeta obligatoria: $RelativePath")
        return
    }

    if (-not (Get-ChildItem -LiteralPath $absolutePath -Recurse -File | Select-Object -First 1)) {
        $errors.Add("Carpeta obligatoria sin archivos: $RelativePath")
    }
}

$requiredPaths = @(
    'README.md',
    'RIESGO_LAVADO.sln',
    'backend/RL.API/RL.API.csproj',
    'backend/RL.API.Tests/RL.API.Tests.csproj',
    'backend/RL.API/Core/Security/AuditRequiredAttribute.cs',
    'backend/RL.API/Core/Security/ModuloAuthorizeAttribute.cs',
    'backend/RL.API/Infrastructure/Database/OracleDbContext.cs',
    'backend/RL.API/Shared/Identifiers/HashIdHelper.cs',
    'backend/RL.API/Features/Catalogos/CatalogosController.cs',
    'backend/RL.API/Features/Catalogos/Application/ICatalogoService.cs',
    'backend/RL.API/Features/Catalogos/Application/CatalogoService.cs',
    'backend/RL.API/Features/Catalogos/Contracts/Modulo.cs',
    'backend/RL.API/Features/Catalogos/Persistence/ICatalogoRepository.cs',
    'backend/RL.API/Features/Catalogos/Persistence/CatalogoRepository.cs',
    'backend/RL.API.Tests/Features/Catalogos/CatalogosModuleTests.cs',
    'backend/RL.API/Features/Auditoria/AuditoriaController.cs',
    'backend/RL.API/Features/Auditoria/Application/IAuditoriaService.cs',
    'backend/RL.API/Features/Auditoria/Application/AuditoriaService.cs',
    'backend/RL.API/Features/Auditoria/Contracts/AuditoriaDto.cs',
    'backend/RL.API/Features/Auditoria/Persistence/IAuditoriaRepository.cs',
    'backend/RL.API/Features/Auditoria/Persistence/AuditoriaRepository.cs',
    'backend/RL.API.Tests/Features/Auditoria/AuditoriaModuleCharacterizationTests.cs',
    'backend/RL.API/Features/Configuracion/ConfiguracionController.cs',
    'backend/RL.API/Features/Configuracion/Application/IConfiguracionService.cs',
    'backend/RL.API/Features/Configuracion/Application/ConfiguracionService.cs',
    'backend/RL.API/Features/Configuracion/Contracts/ConfigSistema.cs',
    'backend/RL.API/Features/Configuracion/Contracts/LoginSlide.cs',
    'backend/RL.API/Features/Configuracion/Persistence/IConfiguracionRepository.cs',
    'backend/RL.API/Features/Configuracion/Persistence/ConfiguracionRepository.cs',
    'backend/RL.API.Tests/Features/Configuracion/ConfiguracionModuleCharacterizationTests.cs',
    'backend/RL.API/Features/Identidad/AuthController.cs',
    'backend/RL.API/Features/Identidad/Application/IAuthService.cs',
    'backend/RL.API/Features/Identidad/Application/AuthService.cs',
    'backend/RL.API/Features/Identidad/Contracts/AuthDTOs.cs',
    'backend/RL.API/Features/Identidad/Contracts/EstadoUsuarioDto.cs',
    'backend/RL.API/Features/Identidad/Domain/Usuario.cs',
    'backend/RL.API/Features/Identidad/Domain/Rol.cs',
    'backend/RL.API/Features/Identidad/Persistence/IUsuarioRepository.cs',
    'backend/RL.API/Features/Identidad/Persistence/UsuarioRepository.cs',
    'backend/RL.API/Features/Identidad/Integrations/ActiveDirectory/IActivoDirectorioService.cs',
    'backend/RL.API/Features/Identidad/Integrations/ActiveDirectory/ActiveDirectorioService.cs',
    'backend/RL.API/Features/Identidad/Integrations/ActiveDirectory/ActiveDirectoryContracts.cs',
    'backend/RL.API/Features/Identidad/Integrations/Email/IEmailService.cs',
    'backend/RL.API/Features/Identidad/Integrations/Email/EmailService.cs',
    'backend/RL.API/Features/Identidad/Integrations/Email/SmtpSettings.cs',
    'backend/RL.API.Tests/Features/Identidad/IdentidadModuleCharacterizationTests.cs',
    'backend/RL.API/Features/Listas/ListasController.cs',
    'backend/RL.API/Features/Listas/Application/IListasService.cs',
    'backend/RL.API/Features/Listas/Application/ListasService.cs',
    'backend/RL.API/Features/Listas/Persistence/IListasRepository.cs',
    'backend/RL.API/Features/Listas/Persistence/ListasRepository.cs',
    'backend/RL.API/Features/MatricesRiesgos/MatricesRiesgosController.cs',
    'backend/RL.API/Features/MatricesRiesgos/Application/IMatricesRiesgosAppService.cs',
    'backend/RL.API/Features/MatricesRiesgos/Application/MatricesRiesgosAppService.cs',
    'backend/RL.API/Features/MatricesRiesgos/Contracts/Matrices/MatrizRiesgoDtos.cs',
    'backend/RL.API/Features/MatricesRiesgos/Contracts/Matrices/MatrizRiesgoRequestDtos.cs',
    'backend/RL.API/Features/MatricesRiesgos/Contracts/Matrices/MatrizRiesgoCriterioDtos.cs',
    'backend/RL.API/Features/MatricesRiesgos/Contracts/PlanesAccion/PlanAccionDtos.cs',
    'backend/RL.API/Features/MatricesRiesgos/Contracts/Evidencias/EvidenciaDtos.cs',
    'backend/RL.API/Features/MatricesRiesgos/Contracts/Reporteria/ReporteriaDtos.cs',
    'backend/RL.API/Features/MatricesRiesgos/Domain/IMatricesRiesgoService.cs',
    'backend/RL.API/Features/MatricesRiesgos/Domain/MatricesRiesgoService.cs',
    'backend/RL.API/Features/MatricesRiesgos/Persistence/IMatricesRiesgosRepository.cs',
    'backend/RL.API/Features/MatricesRiesgos/Persistence/MatricesRiesgosRepository.cs',
    'backend/RL.API.Tests/Features/ModuleBoundariesTests.cs',
    'backend/RL.API.Tests/Features/MatricesRiesgos/MatricesRiesgoServiceTests.cs',
    'backend/RL.API.Tests/Features/Listas/ListasApplicationTests.cs',
    'backend/RL.API.Tests/Features/MatricesRiesgos/MatricesRiesgosApplicationTests.cs',
    'backend/RL.API.Tests/Support/InterfaceStub.cs',
    'backend/RL.API/Shared/Results/ServiceResult.cs',
    'frontend/rl-app/package.json',
    'frontend/rl-app/src/app/core/auth/auth.service.ts',
    'frontend/rl-app/src/app/core/auth/auth.service.spec.ts',
    'frontend/rl-app/src/app/core/configuration/configuracion.service.ts',
    'frontend/rl-app/src/app/core/configuration/configuracion.service.spec.ts',
    'frontend/rl-app/src/app/core/guards/guards.spec.ts',
    'frontend/rl-app/src/app/core/interceptors/auth.interceptor.spec.ts',
    'frontend/rl-app/src/app/core/interceptors/confirmacion-cambios.interceptor.spec.ts',
    'frontend/rl-app/src/app/features/admin/listas/data-access/listas.service.ts',
    'frontend/rl-app/src/app/features/admin/listas/data-access/listas.service.spec.ts',
    'frontend/rl-app/src/app/features/admin/listas/models/listas.models.ts',
    'frontend/rl-app/src/app/features/admin/matrices-riesgos/data-access/matrices-riesgos.service.ts',
    'frontend/rl-app/src/app/features/admin/matrices-riesgos/data-access/matrices-riesgos.service.spec.ts',
    'frontend/rl-app/src/app/features/admin/matrices-riesgos/models/matrices-riesgos.models.ts',
    'frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.ts',
    'frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.html',
    'frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.spec.ts',
    'frontend/rl-app/src/app/features/admin/bitacora/pages/bitacora/bitacora.component.ts',
    'frontend/rl-app/src/app/features/admin/bitacora/pages/bitacora/bitacora.component.html',
    'frontend/rl-app/src/app/features/admin/bitacora/data-access/auditoria.service.spec.ts',
    'frontend/rl-app/src/app/features/admin/configuracion/pages/configuracion/configuracion.component.ts',
    'frontend/rl-app/src/app/features/admin/configuracion/pages/configuracion/configuracion.component.html',
    'frontend/rl-app/src/app/features/admin/usuarios/pages/usuarios/usuarios.component.ts',
    'frontend/rl-app/src/app/features/admin/usuarios/pages/usuarios/usuarios.component.html',
    'frontend/rl-app/src/app/features/admin/listas/pages/monitoreo-listas/monitoreo-listas.component.ts',
    'frontend/rl-app/src/app/features/admin/listas/pages/monitoreo-listas/monitoreo-listas.component.html',
    'frontend/rl-app/src/app/features/admin/listas/pages/monitoreo-listas/monitoreo-listas.component.spec.ts',
    'frontend/rl-app/src/app/features/admin/listas/pages/tipo-listas/tipo-listas.component.ts',
    'frontend/rl-app/src/app/features/admin/listas/pages/cargar-listas/cargar-listas.component.ts',
    'frontend/rl-app/src/app/features/admin/listas/pages/coincidencias-patrono/coincidencias-patrono.component.ts',
    'frontend/rl-app/src/app/features/admin/listas/pages/coincidencias-empleado/coincidencias-empleado.component.ts',
    'frontend/rl-app/src/app/features/auth/pages/login/login.component.ts',
    'frontend/rl-app/src/app/features/auth/pages/login/login.component.html',
    'frontend/rl-app/src/app/features/admin/matrices-riesgos/components/matrices-reporte-tabla/matrices-reporte-tabla.component.ts',
    'database/00_EJECUCION_PRIMERA_VEZ.sql',
    'database/00_EJECUCION_ACTUALIZACIONES_SEGURAS.sql',
    'database/00_MANIFIESTO_SCRIPTS_APROBADOS.md',
    'database/19_matrices_riesgos/00_APLICAR_MODULO_MATRICES_RIESGOS.sql',
    'database/19_matrices_riesgos/README.md',
    'tools/validate_database_scripts.ps1',
    'tools/validate_documentation_links.ps1',
    'tools/run_quality_gates.ps1',
    'backend/RL.API.Tests/coverage.runsettings',
    'frontend/rl-app/playwright.config.ts',
    'frontend/rl-app/e2e/login-and-routing.spec.ts',
    "docs/$documentationDirectoryName/ARCHITECTURE.md",
    "docs/$documentationDirectoryName/ESTRUCTURA_OBJETIVO.md",
    "docs/$documentationDirectoryName/PLAN_REORGANIZACION.md",
    "docs/$documentationDirectoryName/QUALITY.md"
)

foreach ($path in $requiredPaths) {
    Assert-RepositoryPath $path
}

$backendModules = @('Auditoria', 'Catalogos', 'Configuracion', 'Identidad', 'Listas', 'MatricesRiesgos')
foreach ($module in $backendModules) {
    foreach ($layer in @('Application', 'Contracts', 'Persistence')) {
        Assert-NonEmptyDirectory "backend/RL.API/Features/$module/$layer"
    }
}

$frontendModules = @(
    'frontend/rl-app/src/app/features/auth',
    'frontend/rl-app/src/app/features/admin/bitacora',
    'frontend/rl-app/src/app/features/admin/configuracion',
    'frontend/rl-app/src/app/features/admin/listas',
    'frontend/rl-app/src/app/features/admin/matrices-riesgos',
    'frontend/rl-app/src/app/features/admin/usuarios'
)
foreach ($module in $frontendModules) {
    Assert-NonEmptyDirectory "$module/pages"
    $modulePath = Join-Path $RepositoryRoot $module
    foreach ($rootFile in Get-ChildItem -LiteralPath $modulePath -File) {
        $relativeFile = $rootFile.FullName.Substring($RepositoryRoot.Length).TrimStart('\', '/').Replace('\', '/')
        $errors.Add("Archivo frontend suelto en raiz de modulo: $relativeFile")
    }
}

$sourceRoots = @(
    'backend/RL.API/Core',
    'backend/RL.API/Infrastructure',
    'backend/RL.API/Features',
    'backend/RL.API/Shared',
    'frontend/rl-app/src/app/core',
    'frontend/rl-app/src/app/features',
    'frontend/rl-app/src/app/shared'
)
foreach ($sourceRoot in $sourceRoots) {
    $sourceRootPath = Join-Path $RepositoryRoot $sourceRoot
    foreach ($directory in Get-ChildItem -LiteralPath $sourceRootPath -Directory -Recurse) {
        if (-not (Get-ChildItem -LiteralPath $directory.FullName -Force | Select-Object -First 1)) {
            $relativeDirectory = $directory.FullName.Substring($RepositoryRoot.Length).TrimStart('\', '/').Replace('\', '/')
            $errors.Add("Carpeta fuente vacia: $relativeDirectory")
        }
    }
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
    'frontend/rl-app/src/app/features/admin/bitacora/pages/bitacora/bitacora.component.ts',
    'frontend/rl-app/src/app/features/admin/configuracion/pages/configuracion/configuracion.component.ts',
    'frontend/rl-app/src/app/features/admin/listas/pages/monitoreo-listas/monitoreo-listas.component.ts'
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

$legacyFrontendPatterns = @(
    '^frontend/rl-app/src/app/core/(models|services)/',
    '^frontend/rl-app/src/app/features/auth/login/',
    '^frontend/rl-app/src/app/features/admin/(cargar-listas|coincidencias-empleado|coincidencias-patrono|monitoreo-listas|tipo-listas)/',
    '^frontend/rl-app/src/app/features/admin/(bitacora|configuracion|usuarios|matrices-riesgos)/[^/]+$'
)
foreach ($file in $trackedFiles) {
    foreach ($legacyFrontendPattern in $legacyFrontendPatterns) {
        if ($file -match $legacyFrontendPattern) {
            if (Test-Path -LiteralPath (Join-Path $RepositoryRoot $file) -PathType Leaf) {
                $errors.Add("Archivo frontend en carpeta heredada: $file")
            }
            break
        }
    }
}

$legacyFrontendDirectories = @(
    'frontend/rl-app/src/app/core/models',
    'frontend/rl-app/src/app/core/services',
    'frontend/rl-app/src/app/features/auth/login',
    'frontend/rl-app/src/app/features/admin/cargar-listas',
    'frontend/rl-app/src/app/features/admin/coincidencias-empleado',
    'frontend/rl-app/src/app/features/admin/coincidencias-patrono',
    'frontend/rl-app/src/app/features/admin/monitoreo-listas',
    'frontend/rl-app/src/app/features/admin/tipo-listas'
)
foreach ($legacyDirectory in $legacyFrontendDirectories) {
    if (Test-Path -LiteralPath (Join-Path $RepositoryRoot $legacyDirectory) -PathType Container) {
        $errors.Add("Carpeta frontend heredada: $legacyDirectory")
    }
}

$legacyBackendPaths = @(
    'backend/RL.API/Controllers/AuthController.cs',
    'backend/RL.API/Controllers/AuditoriaController.cs',
    'backend/RL.API/Controllers/CatalogosController.cs',
    'backend/RL.API/Controllers/ConfiguracionController.cs',
    'backend/RL.API/Controllers/ListasController.cs',
    'backend/RL.API/Controllers/MatricesRiesgosController.cs',
    'backend/RL.API/Services/CatalogoService.cs',
    'backend/RL.API/Services/AuthService.cs',
    'backend/RL.API/Services/ActiveDirectorioService.cs',
    'backend/RL.API/Services/EmailService.cs',
    'backend/RL.API/Services/ServiceResult.cs',
    'backend/RL.API/Services/ListasService.cs',
    'backend/RL.API/Services/CoincidenciasService.cs',
    'backend/RL.API/Services/EvidenciasService.cs',
    'backend/RL.API/Services/MatricesRiesgosAppService.cs',
    'backend/RL.API/Services/MatricesRiesgoService.cs',
    'backend/RL.API/Repositories/CatalogoRepository.cs',
    'backend/RL.API/Repositories/UsuarioRepository.cs',
    'backend/RL.API/Repositories/AuditoriaRepository.cs',
    'backend/RL.API/Repositories/ConfiguracionRepository.cs',
    'backend/RL.API/Repositories/ListasRepository.cs',
    'backend/RL.API/Repositories/MatricesRiesgosRepository.cs',
    'backend/RL.API/Models/Modulo.cs',
    'backend/RL.API/Models/Usuario.cs',
    'backend/RL.API/Models/Rol.cs',
    'backend/RL.API/Models/SmtpSettings.cs',
    'backend/RL.API/Models/ConfigSistema.cs',
    'backend/RL.API/Models/LoginSlide.cs',
    'backend/RL.API/Models/ListasModels.cs',
    'backend/RL.API/DTOs/MatricesRiesgosDto.cs',
    'backend/RL.API/DTOs/MatricesRiesgoCalculoDto.cs',
    'backend/RL.API/DTOs/AuditoriaDto.cs',
    'backend/RL.API/DTOs/AuthDTOs.cs'
    'backend/RL.API/Infrastructure/OracleDbContext.cs'
)
foreach ($legacyPath in $legacyBackendPaths) {
    if (Test-Path -LiteralPath (Join-Path $RepositoryRoot $legacyPath)) {
        $errors.Add("Archivo backend en ubicacion heredada: $legacyPath")
    }
}

$legacyBackendDirectories = @(
    'backend/RL.API/Controllers',
    'backend/RL.API/DTOs',
    'backend/RL.API/Models',
    'backend/RL.API/Repositories',
    'backend/RL.API/Services',
    'backend/RL.API/Security',
    'backend/RL.API/Helpers'
)
foreach ($legacyDirectory in $legacyBackendDirectories) {
    $legacyDirectoryPath = Join-Path $RepositoryRoot $legacyDirectory
    if (Test-Path -LiteralPath $legacyDirectoryPath -PathType Container) {
        $errors.Add("Carpeta backend heredada por tipo: $legacyDirectory")
    }
}

$rootMarkdown = Get-ChildItem -LiteralPath $RepositoryRoot -File -Filter '*.md' |
    Where-Object { $_.Name -notin @('README.md', 'AGENTS.md', 'BITACORA_COLABORACION.md') }
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

$databaseValidator = Join-Path $RepositoryRoot 'tools/validate_database_scripts.ps1'
if (Test-Path -LiteralPath $databaseValidator) {
    $databaseErrors = @(& $databaseValidator -RepositoryRoot $RepositoryRoot -PassThru)
    foreach ($databaseError in $databaseErrors) {
        $errors.Add("Base de datos: $databaseError")
    }
}

$documentationValidator = Join-Path $RepositoryRoot 'tools/validate_documentation_links.ps1'
if (Test-Path -LiteralPath $documentationValidator) {
    $documentationErrors = @(& $documentationValidator -RepositoryRoot $RepositoryRoot -PassThru)
    foreach ($documentationError in $documentationErrors) {
        $errors.Add("Documentacion: $documentationError")
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
