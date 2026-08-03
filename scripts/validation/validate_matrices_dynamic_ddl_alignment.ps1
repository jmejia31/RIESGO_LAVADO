$ErrorActionPreference = 'Stop'

$repositoryRoot = Resolve-Path (Join-Path $PSScriptRoot '../..')
$script05 = Join-Path $repositoryRoot 'database/19_matrices_riesgos/instalacion/05_ajustes_dashboard_seguridad_reportes.sql'
$workflowTemporal = Join-Path $repositoryRoot '.github/workflows/agent-fix-matrices-phase1.yml'
$repositoryFacade = Join-Path $repositoryRoot 'backend/RL.API/Features/MatricesRiesgos/Persistence/MatricesRiesgosRepositoryFacade.cs'
$repositoryFile = Join-Path $repositoryRoot 'backend/RL.API/Features/MatricesRiesgos/Persistence/MatricesRiesgosRepository.cs'
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

    $segments = $File.FullName -split '[\\/]'
    foreach ($segment in $segments) {
        if ($excludedDirectoryNames -contains $segment) {
            return $true
        }
    }

    return $false
}

function Get-SourceFiles {
    param(
        [string[]]$Roots,
        [string[]]$Extensions
    )

    $result = New-Object System.Collections.Generic.List[System.IO.FileInfo]
    foreach ($root in $Roots) {
        if (-not (Test-Path -LiteralPath $root)) {
            $errors.Add("No se encontró una raíz obligatoria: $root")
            continue
        }

        Get-ChildItem -LiteralPath $root -Recurse -File | Where-Object {
            $Extensions -contains $_.Extension.ToLowerInvariant() -and
            -not (Test-IsExcludedPath -File $_)
        } | ForEach-Object { $result.Add($_) }
    }

    return $result
}

if (-not (Test-Path -LiteralPath $script05)) {
    $errors.Add("No se encontró el script Oracle 05: $script05")
}

if (-not (Test-Path -LiteralPath $repositoryFile)) {
    $errors.Add("No se encontró el repositorio principal de Matrices: $repositoryFile")
}

if (-not (Test-Path -LiteralPath $programFile)) {
    $errors.Add("No se encontró Program.cs: $programFile")
}

if (Test-Path -LiteralPath $workflowTemporal) {
    $errors.Add('El workflow temporal agent-fix-matrices-phase1.yml no debe permanecer publicado.')
}

if (Test-Path -LiteralPath $repositoryFacade) {
    $errors.Add('MatricesRiesgosRepositoryFacade.cs no debe existir; las vinculaciones deben ser operativas en el repositorio registrado.')
}

$forbiddenTokens = [ordered]@{
    'FLU_ESTADO_NUEVO' = 'La tabla definitiva solo contiene FLU_ESTADO.'
    'FLU_ESTADO_ANTERIOR' = 'La tabla definitiva solo contiene FLU_ESTADO.'
    'EVA_ESTADO' = 'El estado actual procede del último flujo; no existe EVA_ESTADO.'
    'EVA_VRI' = 'VRI se persiste en datos calculados, proyección y trazas; no existe EVA_VRI.'
    'EVA_ETP' = 'No existe EVA_ETP en RL_MR_EVALUACIONES_RIESGO.'
    'EVA_VRR' = 'VRR se persiste en datos calculados, proyección y trazas; no existe EVA_VRR.'
    'EVA_FECHA_EVAL' = 'La columna física es EVA_FECHA_REGISTRO.'
    'EVA_USR_EVAL' = 'La columna física es EVA_USR_REGISTRO.'
    'PROY_ETP' = 'No existe PROY_ETP en RL_MR_PROYECCIONES_EVALUACION.'
    'RL_MR_MODELOS' = 'Tabla retirada del modelo dinámico definitivo.'
    'RL_MR_FACTORES' = 'Tabla retirada del modelo dinámico definitivo.'
    'RL_MR_VARIABLES' = 'Tabla retirada del modelo dinámico definitivo.'
    'RL_MR_ESCALAS' = 'Tabla retirada del modelo dinámico definitivo.'
    'RL_MR_CRITERIOS' = 'Tabla retirada del modelo dinámico definitivo.'
    'DeterminarClasificacionResidual' = 'La clasificación no puede permanecer rígida en C#.'
    'RegistrarAuditoriaAsync' = 'El contrato institucional vigente expone RegistrarAsync.'
}

$moduleFiles = Get-SourceFiles -Roots $moduleScanRoots -Extensions $moduleExtensions
foreach ($file in $moduleFiles) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    foreach ($entry in $forbiddenTokens.GetEnumerator()) {
        if ($content.Contains($entry.Key)) {
            $relativePath = [IO.Path]::GetRelativePath($repositoryRoot, $file.FullName)
            $matches = Select-String -LiteralPath $file.FullName -SimpleMatch $entry.Key
            foreach ($match in $matches) {
                $errors.Add("$relativePath:$($match.LineNumber): identificador incompatible '$($entry.Key)'. $($entry.Value)")
            }
        }
    }
}

if (Test-Path -LiteralPath $repositoryFile) {
    $repositoryContent = Get-Content -LiteralPath $repositoryFile -Raw

    $requiredRepositoryPatterns = [ordered]@{
        'command.Transaction = transaction' = 'Los comandos transaccionales deben recibir explícitamente OracleTransaction.'
        'OracleTransaction transaction' = 'Los auxiliares transaccionales deben exigir OracleTransaction.'
        'FLU_ESTADO' = 'El estado debe leerse y escribirse mediante FLU_ESTADO.'
        "VER_ESTADO = 'PUBLISHED'" = 'La regla debe resolverse desde la versión publicada del formulario.'
        'REG_CODIGO = :codigo' = 'La regla debe resolverse por su código declarado en la versión.'
        'REG_VERSION = :version' = 'La regla debe resolverse por su versión declarada en el formulario.'
        'TRA_REGLA_ID' = 'La traza debe persistir la regla exacta aplicada.'
        'VincularEvidenciaRiesgoAsync' = 'Debe existir la vinculación de evidencia a riesgo.'
        'VincularEvidenciaEvaluacionAsync' = 'Debe existir la vinculación de evidencia a evaluación.'
        'VincularEvidenciaControlAsync' = 'Debe existir la vinculación de evidencia a control.'
        'VincularEvidenciaPlanAsync' = 'Debe existir la vinculación de evidencia a plan.'
        'VincularEvidenciaActividadAsync' = 'Debe existir la vinculación de evidencia a actividad.'
        'VincularEvidenciaAlertaAsync' = 'Debe existir la vinculación de evidencia a alerta.'
        'VincularEvidenciaAutomonitoreoAsync' = 'Debe existir la vinculación de evidencia a automonitoreo.'
        'VincularEvidenciaRevisionAsync' = 'Debe existir la vinculación de evidencia a revisión.'
        'VincularEvidenciaAprobacionAsync' = 'Debe existir la vinculación de evidencia a aprobación.'
    }

    foreach ($entry in $requiredRepositoryPatterns.GetEnumerator()) {
        if (-not $repositoryContent.Contains($entry.Key)) {
            $errors.Add("MatricesRiesgosRepository.cs no contiene '$($entry.Key)'. $($entry.Value)")
        }
    }

    if ($repositoryContent.Contains('NotSupportedException')) {
        $errors.Add('MatricesRiesgosRepository.cs contiene NotSupportedException; ninguna vinculación de evidencias puede quedar deshabilitada.')
    }

    if ($repositoryContent -match "REG_ACTIVA\s*=\s*1\s*ORDER\s+BY\s+REG_ID") {
        $errors.Add('La regla de cálculo se selecciona globalmente por último REG_ID activo; debe vincularse a código y versión del formulario.')
    }
}

if (Test-Path -LiteralPath $programFile) {
    $programContent = Get-Content -LiteralPath $programFile -Raw
    if (-not $programContent.Contains('AddScoped<IMatricesRiesgosRepository, MatricesRiesgosRepository>()')) {
        $errors.Add('Program.cs no registra directamente MatricesRiesgosRepository como IMatricesRiesgosRepository.')
    }

    if ($programContent.Contains('MatricesRiesgosRepositoryFacade')) {
        $errors.Add('Program.cs todavía referencia la fachada transitoria de Matrices.')
    }
}

# Escaneo preventivo de secretos. No muestra el valor detectado.
$securityFiles = Get-SourceFiles -Roots $securityScanRoots -Extensions $securityExtensions
$connectionStringPattern = '(?is)(Data\s+Source|Server)\s*=.+?(User\s+Id|UserId|Uid)\s*=.+?(Password|Pwd)\s*='
$jsonOraclePasswordPattern = '(?is)"(?:OracleDB|ConnectionStrings?)"\s*:\s*"[^"\r\n]*(?:Password|Pwd)\s*='
$standalonePasswordPattern = '(?im)^\s*(?:Password|Pwd)\s*=\s*(?!\s*(?:CHANGE_ME|REPLACE_ME|\$\{|<|__|$)).+'

foreach ($file in $securityFiles) {
    if ($file.Name -match '(?i)\.example$|example\.|sample\.') {
        continue
    }

    $content = Get-Content -LiteralPath $file.FullName -Raw
    $containsSecret =
        $content -match $connectionStringPattern -or
        $content -match $jsonOraclePasswordPattern -or
        $content -match $standalonePasswordPattern

    if ($containsSecret) {
        $relativePath = [IO.Path]::GetRelativePath($repositoryRoot, $file.FullName)
        $errors.Add("$relativePath: posible credencial o cadena Oracle codificada. Mover a variables de entorno, User Secrets o configuración local ignorada.")
    }
}

if (Test-Path -LiteralPath $script05) {
    $scriptContent = Get-Content -LiteralPath $script05 -Raw
    $requiredScriptTokens = @(
        "WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK",
        "DEFINE autorizacion = '&1'",
        "SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA')",
        "UPPER(TRIM(v_auth)) <> 'EJECUTAR'",
        'UQ_RL_MR_PROY_EVA',
        'IX_RL_MR_PROY_DASHBOARD'
    )

    foreach ($token in $requiredScriptTokens) {
        if (-not $scriptContent.Contains($token)) {
            $errors.Add("El script 05 no contiene la protección o estructura obligatoria: $token")
        }
    }

    if ($scriptContent -match '(?ms)BEGIN\s+PROMPT') {
        $errors.Add('El script 05 contiene PROMPT dentro de un bloque PL/SQL.')
    }
}

if ($errors.Count -gt 0) {
    Write-Host "Validación integral de Matrices: FALLÓ ($($errors.Count) hallazgos)." -ForegroundColor Red
    foreach ($errorItem in $errors) {
        Write-Error $errorItem
    }
    exit 1
}

Write-Host "Validación integral de Matrices contra DDL, transacciones y seguridad: CORRECTA." -ForegroundColor Green
Write-Host "Archivos del módulo revisados: $($moduleFiles.Count). Archivos de seguridad revisados: $($securityFiles.Count)." -ForegroundColor Green
