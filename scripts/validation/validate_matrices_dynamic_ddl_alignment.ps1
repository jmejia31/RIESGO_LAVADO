$ErrorActionPreference = 'Stop'

$repositoryRoot = Resolve-Path (Join-Path $PSScriptRoot '../..')
$script05 = Join-Path $repositoryRoot 'database/19_matrices_riesgos/instalacion/05_ajustes_dashboard_seguridad_reportes.sql'
$workflowTemporal = Join-Path $repositoryRoot '.github/workflows/agent-fix-matrices-phase1.yml'
$repositoryFacade = Join-Path $repositoryRoot 'backend/RL.API/Features/MatricesRiesgos/Persistence/MatricesRiesgosRepositoryFacade.cs'
$legacyDtos = Join-Path $repositoryRoot 'backend/RL.API/Features/MatricesRiesgos/Contracts/Matrices/MatrizRiesgoDtos.cs'
$legacyReportDtos = Join-Path $repositoryRoot 'backend/RL.API/Features/MatricesRiesgos/Contracts/Reporteria/ReporteriaDtos.cs'
$legacyRenderer = Join-Path $repositoryRoot 'backend/RL.API/Features/MatricesRiesgos/Application/MatricesRiesgosReportRenderer.cs'
$repositoryFile = Join-Path $repositoryRoot 'backend/RL.API/Features/MatricesRiesgos/Persistence/MatricesRiesgosRepository.cs'
$repositoryContract = Join-Path $repositoryRoot 'backend/RL.API/Features/MatricesRiesgos/Persistence/IMatricesRiesgosRepository.cs'
$appServiceContract = Join-Path $repositoryRoot 'backend/RL.API/Features/MatricesRiesgos/Application/IMatricesRiesgosAppService.cs'
$controllerFile = Join-Path $repositoryRoot 'backend/RL.API/Features/MatricesRiesgos/MatricesRiesgosController.cs'
$angularModels = Join-Path $repositoryRoot 'frontend/rl-app/src/app/features/admin/matrices-riesgos/models/matrices-riesgos.models.ts'
$programFile = Join-Path $repositoryRoot 'backend/RL.API/Program.cs'

$moduleScanRoots = @(
    (Join-Path $repositoryRoot 'backend/RL.API/Features/MatricesRiesgos'),
    (Join-Path $repositoryRoot 'backend/RL.API.Tests/Features/MatricesRiesgos'),
    (Join-Path $repositoryRoot 'frontend/rl-app/src/app/features/admin/matrices-riesgos'),
    (Join-Path $repositoryRoot 'database/19_matrices_riesgos')
)

$securityScanRoots = @(
    (Join-Path $repositoryRoot 'backend'),
    (Join-Path $repositoryRoot 'frontend'),
    (Join-Path $repositoryRoot 'scripts'),
    (Join-Path $repositoryRoot '.github')
)

$moduleExtensions = @('.cs', '.ts', '.html', '.sql', '.json')
$securityExtensions = @('.cs', '.json', '.config', '.xml', '.runsettings', '.ps1', '.yml', '.yaml', '.env', '.txt')
$excludedDirectoryNames = @('bin', 'obj', 'node_modules', 'dist', 'coverage', 'Historico', 'retiro_controlado', '.git')
$errors = New-Object System.Collections.Generic.List[string]

function Test-IsExcludedPath {
    param([System.IO.FileInfo]$File)
    foreach ($segment in ($File.FullName -split '[\\/]')) {
        if ($excludedDirectoryNames -contains $segment) { return $true }
    }
    return $false
}

function Get-SourceFiles {
    param([string[]]$Roots, [string[]]$Extensions)
    $result = New-Object System.Collections.Generic.List[System.IO.FileInfo]
    foreach ($root in $Roots) {
        if (-not (Test-Path -LiteralPath $root)) {
            $errors.Add("No se encontró una raíz obligatoria: $root")
            continue
        }
        Get-ChildItem -LiteralPath $root -Recurse -File | Where-Object {
            $Extensions -contains $_.Extension.ToLowerInvariant() -and -not (Test-IsExcludedPath -File $_)
        } | ForEach-Object { $result.Add($_) }
    }
    return $result
}

function Get-RelativeRepositoryPath {
    param([string]$Path)
    $root = ([string]$repositoryRoot).TrimEnd('\', '/')
    return $Path.Substring($root.Length).TrimStart('\', '/')
}

foreach ($requiredFile in @($script05, $repositoryFile, $repositoryContract, $appServiceContract, $controllerFile, $angularModels, $programFile)) {
    if (-not (Test-Path -LiteralPath $requiredFile)) {
        $errors.Add("No se encontró un archivo obligatorio: $requiredFile")
    }
}

foreach ($forbiddenFile in @($workflowTemporal, $repositoryFacade, $legacyDtos, $legacyReportDtos, $legacyRenderer)) {
    if (Test-Path -LiteralPath $forbiddenFile) {
        $errors.Add("No debe permanecer el archivo retirado: $(Get-RelativeRepositoryPath -Path $forbiddenFile)")
    }
}

$forbiddenTokens = @(
    [pscustomobject]@{ Token = 'FLU_ESTADO_NUEVO'; Message = 'La tabla definitiva solo contiene FLU_ESTADO.' },
    [pscustomobject]@{ Token = 'FLU_ESTADO_ANTERIOR'; Message = 'La tabla definitiva solo contiene FLU_ESTADO.' },
    [pscustomobject]@{ Token = 'EVA_ESTADO'; Message = 'El estado procede del último flujo.' },
    [pscustomobject]@{ Token = 'EVA_VRI'; Message = 'VRI no es una columna de evaluaciones.' },
    [pscustomobject]@{ Token = 'EVA_ETP'; Message = 'ETP no es una columna de evaluaciones.' },
    [pscustomobject]@{ Token = 'EVA_VRR'; Message = 'VRR no es una columna de evaluaciones.' },
    [pscustomobject]@{ Token = 'EVA_FECHA_EVAL'; Message = 'La columna física es EVA_FECHA_REGISTRO.' },
    [pscustomobject]@{ Token = 'EVA_USR_EVAL'; Message = 'La columna física es EVA_USR_REGISTRO.' },
    [pscustomobject]@{ Token = 'PROY_ETP'; Message = 'La proyección definitiva no contiene ETP.' },
    [pscustomobject]@{ Token = 'RL_MR_MODELOS'; Message = 'Tabla retirada del modelo dinámico.' },
    [pscustomobject]@{ Token = 'RL_MR_FACTORES'; Message = 'Tabla retirada del modelo dinámico.' },
    [pscustomobject]@{ Token = 'RL_MR_VARIABLES'; Message = 'Tabla retirada del modelo dinámico.' },
    [pscustomobject]@{ Token = 'RL_MR_ESCALAS'; Message = 'Tabla retirada del modelo dinámico.' },
    [pscustomobject]@{ Token = 'RL_MR_CRITERIOS'; Message = 'Tabla retirada del modelo dinámico.' },
    [pscustomobject]@{ Token = 'ModeloId'; Message = 'Contrato del modelo heredado.' },
    [pscustomobject]@{ Token = 'ModeloVersion'; Message = 'Contrato del modelo heredado.' },
    [pscustomobject]@{ Token = 'FactorInstitucionalDto'; Message = 'Contrato de factores heredado.' },
    [pscustomobject]@{ Token = 'VariableMetodologiaRespuestaDto'; Message = 'Contrato de variables heredado.' },
    [pscustomobject]@{ Token = 'MatrizRiesgoResumenDto'; Message = 'Contrato de matriz basada en sujeto retirado.' },
    [pscustomobject]@{ Token = 'MatrizRiesgoDetalleDto'; Message = 'Contrato de matriz basada en sujeto retirado.' },
    [pscustomobject]@{ Token = 'MatrizRiesgoVariableDetalleDto'; Message = 'Contrato de variables retirado.' },
    [pscustomobject]@{ Token = 'PorFactor'; Message = 'Agrupación del modelo heredado.' },
    [pscustomobject]@{ Token = 'factorId'; Message = 'Identificador de factor retirado del contrato funcional.' },
    [pscustomobject]@{ Token = 'variableId'; Message = 'Identificador de variable retirado del contrato funcional.' },
    [pscustomobject]@{ Token = 'FactorId'; Message = 'Identificador de factor retirado del contrato funcional.' },
    [pscustomobject]@{ Token = 'VariableId'; Message = 'Identificador de variable retirado del contrato funcional.' },
    [pscustomobject]@{ Token = 'List<Dictionary<string, object>>'; Message = 'Los reportes deben usar DTOs tipados.' },
    [pscustomobject]@{ Token = 'DeterminarClasificacionResidual'; Message = 'La clasificación no puede ser rígida en código.' },
    [pscustomobject]@{ Token = 'RegistrarAuditoriaAsync'; Message = 'El contrato institucional expone RegistrarAsync.' }
)

$moduleFiles = Get-SourceFiles -Roots $moduleScanRoots -Extensions $moduleExtensions
foreach ($file in $moduleFiles) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    foreach ($entry in $forbiddenTokens) {
        if ($content.Contains($entry.Token)) {
            $relativePath = Get-RelativeRepositoryPath -Path $file.FullName
            foreach ($match in (Select-String -LiteralPath $file.FullName -SimpleMatch $entry.Token)) {
                $errors.Add("${relativePath}:$($match.LineNumber): identificador incompatible '$($entry.Token)'. $($entry.Message)")
            }
        }
    }
}

if (Test-Path -LiteralPath $repositoryFile) {
    $content = Get-Content -LiteralPath $repositoryFile -Raw
    $required = [ordered]@{
        'command.Transaction = transaction' = 'Los comandos deben propagar OracleTransaction.'
        'FLU_ESTADO' = 'El estado debe proceder de flujos.'
        "VER_ESTADO = 'PUBLISHED'" = 'La metodología y reglas deben usar versiones publicadas.'
        'REG_CODIGO = :codigo' = 'La regla debe resolverse por código.'
        'REG_VERSION = :version' = 'La regla debe resolverse por versión.'
        'TRA_REGLA_ID' = 'La traza debe guardar la regla exacta.'
        'Task<IReadOnlyList<RiesgoReporteFilaDto>> ObtenerConsolidadoTipadoAsync' = 'El consolidado debe ser tipado.'
        'Task<MetodologiaFormularioDto?> ObtenerMetodologiaDinamicaVigenteAsync' = 'La metodología debe usar contrato neutro.'
        'VersionFormularioId = versionId' = 'La metodología debe conservar la versión del formulario.'
        'Secciones = secciones' = 'La metodología debe conservar secciones y campos.'
        'Catalogos = catalogos' = 'La metodología debe conservar catálogos.'
        'Reglas = reglas' = 'La metodología debe conservar reglas.'
        'VincularEvidenciaAprobacionAsync' = 'Debe mantenerse la vinculación a aprobación.'
    }
    foreach ($entry in $required.GetEnumerator()) {
        if (-not $content.Contains($entry.Key)) {
            $errors.Add("MatricesRiesgosRepository.cs no contiene '$($entry.Key)'. $($entry.Value)")
        }
    }
    if ($content.Contains('NotSupportedException')) {
        $errors.Add('MatricesRiesgosRepository.cs contiene NotSupportedException.')
    }
}

if (Test-Path -LiteralPath $repositoryContract) {
    $content = Get-Content -LiteralPath $repositoryContract -Raw
    if (-not $content.Contains('Task<IReadOnlyList<RiesgoReporteFilaDto>> ObtenerConsolidadoTipadoAsync()')) {
        $errors.Add('IMatricesRiesgosRepository no expone el consolidado tipado.')
    }
    if (-not $content.Contains('Task<MetodologiaFormularioDto?> ObtenerMetodologiaDinamicaVigenteAsync()')) {
        $errors.Add('IMatricesRiesgosRepository no expone metodología neutra.')
    }
}

if (Test-Path -LiteralPath $appServiceContract) {
    $content = Get-Content -LiteralPath $appServiceContract -Raw
    if (-not $content.Contains('ServiceResult<IReadOnlyList<RiesgoReporteFilaDto>>')) {
        $errors.Add('IMatricesRiesgosAppService no expone filas tipadas.')
    }
    if (-not $content.Contains('ServiceResult<MetodologiaFormularioDto>')) {
        $errors.Add('IMatricesRiesgosAppService no expone metodología neutra.')
    }
}

if (Test-Path -LiteralPath $controllerFile) {
    $content = Get-Content -LiteralPath $controllerFile -Raw
    if (-not $content.Contains('ObtenerConsolidadoTipadoAsync')) {
        $errors.Add('El endpoint consolidado no consume el contrato tipado.')
    }
    if (-not $content.Contains('ObtenerMetodologiaDinamicaVigenteAsync')) {
        $errors.Add('El endpoint de metodología no consume el contrato neutro.')
    }
}

if (Test-Path -LiteralPath $angularModels) {
    $content = Get-Content -LiteralPath $angularModels -Raw
    foreach ($token in @('MetodologiaFormulario', 'SeccionFormulario', 'CampoFormulario', 'CatalogoMatrices', 'ReglaCalculoMatrices', 'RiesgoReporteFila')) {
        if (-not $content.Contains($token)) {
            $errors.Add("Los modelos Angular no contienen el contrato neutro '$token'.")
        }
    }
}

if (Test-Path -LiteralPath $programFile) {
    $content = Get-Content -LiteralPath $programFile -Raw
    if (-not $content.Contains('AddScoped<IMatricesRiesgosRepository, MatricesRiesgosRepository>()')) {
        $errors.Add('Program.cs no registra directamente MatricesRiesgosRepository.')
    }
    if ($content.Contains('MatricesRiesgosRepositoryFacade')) {
        $errors.Add('Program.cs referencia la fachada retirada.')
    }
}

$securityFiles = Get-SourceFiles -Roots $securityScanRoots -Extensions $securityExtensions
$connectionStringPattern = '(?is)(Data\s+Source|Server)\s*=.+?(User\s+Id|UserId|Uid)\s*=.+?(Password|Pwd)\s*='
$jsonOraclePasswordPattern = '(?is)"(?:OracleDB|ConnectionStrings?)"\s*:\s*"[^"\r\n]*(?:Password|Pwd)\s*='
$standalonePasswordPattern = '(?im)^\s*(?:Password|Pwd)\s*=\s*(?!\s*(?:CHANGE_ME|REPLACE_ME|\$\{|<|__|$)).+'
foreach ($file in $securityFiles) {
    if ($file.Name -match '(?i)\.example$|example\.|sample\.') { continue }
    $content = Get-Content -LiteralPath $file.FullName -Raw
    $containsSecret = $content -match $connectionStringPattern -or $content -match $jsonOraclePasswordPattern
    if ($file.Extension -ne '.cs') { $containsSecret = $containsSecret -or ($content -match $standalonePasswordPattern) }
    if ($containsSecret) {
        $errors.Add("$(Get-RelativeRepositoryPath -Path $file.FullName): posible credencial Oracle codificada.")
    }
}

if (Test-Path -LiteralPath $script05) {
    $content = Get-Content -LiteralPath $script05 -Raw
    foreach ($token in @(
        'WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK',
        "DEFINE autorizacion = '&1'",
        "SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA')",
        "UPPER(TRIM(v_auth)) <> 'EJECUTAR'",
        'UQ_RL_MR_PROY_EVA',
        'IX_RL_MR_PROY_DASHBOARD')) {
        if (-not $content.Contains($token)) {
            $errors.Add("El script 05 no contiene: $token")
        }
    }
    if ($content -match '(?ms)BEGIN\s+PROMPT') {
        $errors.Add('El script 05 contiene PROMPT dentro de PL/SQL.')
    }
}

if ($errors.Count -gt 0) {
    Write-Host "Validacion integral de Matrices: FALLO ($($errors.Count) hallazgos)." -ForegroundColor Red
    foreach ($item in $errors) {
        Write-Host "- $item" -ForegroundColor Red
    }
    exit 1
}

Write-Host 'Validacion integral de Matrices contra DDL, contratos neutros, transacciones y seguridad: CORRECTA.' -ForegroundColor Green
Write-Host "Archivos del modulo revisados: $($moduleFiles.Count). Archivos de seguridad revisados: $($securityFiles.Count)." -ForegroundColor Green
