$ErrorActionPreference = 'Stop'

$repositoryRoot = Resolve-Path (Join-Path $PSScriptRoot '../..')
$repositoryFile = Join-Path $repositoryRoot 'backend/RL.API/Features/MatricesRiesgos/Persistence/MatricesRiesgosRepository.cs'
$script05 = Join-Path $repositoryRoot 'database/19_matrices_riesgos/instalacion/05_ajustes_dashboard_seguridad_reportes.sql'

if (-not (Test-Path -LiteralPath $repositoryFile)) {
    throw "No se encontró el repositorio de Matrices de Riesgos: $repositoryFile"
}

if (-not (Test-Path -LiteralPath $script05)) {
    throw "No se encontró el script Oracle 05: $script05"
}

$repositoryContent = Get-Content -LiteralPath $repositoryFile -Raw
$scriptContent = Get-Content -LiteralPath $script05 -Raw

$forbiddenRepositoryTokens = @(
    'FLU_ESTADO_NUEVO',
    'FLU_ESTADO_ANTERIOR',
    'PROY_ETP',
    'DeterminarClasificacionResidual'
)

$errors = New-Object System.Collections.Generic.List[string]

foreach ($token in $forbiddenRepositoryTokens) {
    if ($repositoryContent.Contains($token)) {
        $errors.Add("Identificador incompatible con el DDL definitivo presente en MatricesRiesgosRepository.cs: $token")
    }
}

$requiredScriptTokens = @(
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

if ($errors.Count -gt 0) {
    foreach ($errorItem in $errors) {
        Write-Error $errorItem
    }
    exit 1
}

Write-Host 'Validación de alineación básica con el DDL dinámico: CORRECTA.'
