param(
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = Resolve-Path (Join-Path $PSScriptRoot '../..')
$timestamp = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ')

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path ([System.IO.Path]::GetTempPath()) "RIESGO_LAVADO-Fase10-$timestamp"
}

function Invoke-GitText {
    param([string[]]$Arguments)

    $output = & git -C $repositoryRoot @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Git falló: git $($Arguments -join ' ')`n$output"
    }

    return (($output | Out-String).Trim())
}

$moduleDocsRel = (Get-ChildItem (Join-Path $repositoryRoot 'docs') -Directory | Where-Object { $_.Name -like '3.*' } | Select-Object -First 1).Name

$requiredFiles = [ordered]@{
    TransitionScript = 'database/19_matrices_riesgos/transicion/06_reconstruir_modelo_17_tablas.sql'
    PreflightScript  = 'database/19_matrices_riesgos/transicion/07_preflight_inventario_oracle_solo_lectura.sql'
    PostflightScript = 'database/19_matrices_riesgos/transicion/08_postflight_verificacion_modelo_17_tablas_solo_lectura.sql'
    ObjectManifest   = 'database/19_matrices_riesgos/transicion/modelo_17_objetos.json'
    Phase10Plan      = "docs/$moduleDocsRel/FASE_10_PLAN_TRANSICION_FISICA_ORACLE_MODELO_17_TABLAS_PREPARADO_NO_AUTORIZADO_2026-08-06.md"
    Authorization    = "docs/$moduleDocsRel/FASE_9_FORMATO_AUTORIZACION_EJECUCION_ORACLE_FASE_10_2026-08-06.md"
    ExecutionRecord  = "docs/$moduleDocsRel/FASE_10_ACTA_EJECUCION_TRANSICION_ORACLE_MODELO_17_TABLAS_PENDIENTE_2026-08-06.md"
}

foreach ($entry in $requiredFiles.GetEnumerator()) {
    $absolutePath = Join-Path $repositoryRoot $entry.Value
    if (-not (Test-Path -LiteralPath $absolutePath -PathType Leaf)) {
        throw "Falta el archivo obligatorio '$($entry.Key)': $($entry.Value)"
    }
}

$branch = Invoke-GitText @('rev-parse', '--abbrev-ref', 'HEAD')
$commit = Invoke-GitText @('rev-parse', 'HEAD')
$workingTree = Invoke-GitText @('status', '--porcelain')

if ($branch -ne 'desarrollo') {
    throw "Preparación bloqueada: la rama activa es '$branch' y debe ser 'desarrollo'."
}

if (-not [string]::IsNullOrWhiteSpace($workingTree)) {
    throw "Preparación bloqueada: el árbol de trabajo contiene cambios sin confirmar.`n$workingTree"
}

$hashes = foreach ($entry in $requiredFiles.GetEnumerator()) {
    $absolutePath = Join-Path $repositoryRoot $entry.Value
    $hash = Get-FileHash -LiteralPath $absolutePath -Algorithm SHA256

    [ordered]@{
        logicalName = $entry.Key
        path        = $entry.Value.Replace('\', '/')
        sha256      = $hash.Hash.ToLowerInvariant()
        bytes       = (Get-Item -LiteralPath $absolutePath).Length
    }
}

$manifestPath = Join-Path $OutputDirectory 'fase10_manifest.json'
$summaryPath = Join-Path $OutputDirectory 'fase10_resumen.txt'
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

$manifest = [ordered]@{
    generatedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
    repository     = 'jmejia31/RIESGO_LAVADO'
    branch         = $branch
    commit         = $commit
    workingTree    = 'clean'
    phase          = 10
    physicalExecution = [ordered]@{
        authorized       = $false
        oracleExecuted   = $false
        preflightExecuted = $false
        transitionExecuted = $false
        postflightExecuted = $false
    }
    safeguards = @(
        'Este manifiesto no conecta a Oracle.',
        'Este manifiesto no ejecuta SQL*Plus.',
        'Este manifiesto no autoriza el script 06.',
        'Las credenciales no deben almacenarse en el repositorio ni en este manifiesto.'
    )
    files = $hashes
}

$manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $manifestPath -Encoding utf8

@(
    'FASE 10 - MANIFIESTO DE PREPARACION',
    "Generado UTC : $($manifest.generatedAtUtc)",
    "Repositorio  : $($manifest.repository)",
    "Rama         : $branch",
    "Commit       : $commit",
    'Arbol        : limpio',
    'Autorizacion : NO OTORGADA',
    'Oracle       : NO EJECUTADO',
    '',
    'ARCHIVOS Y SHA-256',
    ($hashes | ForEach-Object { "- $($_.path) = $($_.sha256)" }),
    '',
    'Este paquete no contiene ni debe contener credenciales Oracle.'
) | Set-Content -LiteralPath $summaryPath -Encoding utf8

Write-Host 'Preparación de evidencias Fase 10: CORRECTA.' -ForegroundColor Green
Write-Host "Rama: $branch" -ForegroundColor Green
Write-Host "Commit: $commit" -ForegroundColor Green
Write-Host "Manifiesto: $manifestPath" -ForegroundColor Green
Write-Host "Resumen: $summaryPath" -ForegroundColor Green
Write-Host 'Oracle no fue conectado ni ejecutado.' -ForegroundColor Yellow
Write-Host 'Autorización de Fase 10 permanece NO OTORGADA.' -ForegroundColor Yellow
