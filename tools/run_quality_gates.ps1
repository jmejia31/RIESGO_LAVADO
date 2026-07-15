param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [switch]$SkipE2E
)

$ErrorActionPreference = 'Stop'
$backendThresholds = @{
    Lines = 7.1
    Branches = 6.7
}
$frontendThresholds = @{
    Statements = 28.3
    Branches = 24.8
    Functions = 26.5
    Lines = 28.3
}
$failures = [System.Collections.Generic.List[string]]::new()

function Assert-Minimum {
    param(
        [string]$Name,
        [double]$Actual,
        [double]$Minimum
    )

    if ($Actual + 0.0001 -lt $Minimum) {
        $failures.Add("$Name bajo el minimo: actual=$($Actual.ToString('F2'))%, minimo=$($Minimum.ToString('F2'))%")
    }
}

$backendProject = Join-Path $RepositoryRoot 'backend/RL.API.Tests/RL.API.Tests.csproj'
$backendSettings = Join-Path $RepositoryRoot 'backend/RL.API.Tests/coverage.runsettings'
$backendResults = Join-Path $RepositoryRoot 'backend/RL.API.Tests/TestResults/quality-gates'
$frontendRoot = Join-Path $RepositoryRoot 'frontend/rl-app'

Write-Host '=== Cobertura Backend ===' -ForegroundColor Cyan
& dotnet test $backendProject `
    --configuration Release `
    --no-restore `
    --settings $backendSettings `
    --collect 'XPlat Code Coverage' `
    --results-directory $backendResults
if ($LASTEXITCODE -ne 0) {
    throw "Las pruebas o la recoleccion de cobertura Backend fallaron con codigo $LASTEXITCODE"
}

$backendCoverageFile = Get-ChildItem -LiteralPath $backendResults -Recurse -File -Filter 'coverage.cobertura.xml' |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1
if (-not $backendCoverageFile) {
    throw 'No se genero coverage.cobertura.xml para el Backend'
}

[xml]$backendCoverage = Get-Content -LiteralPath $backendCoverageFile.FullName -Raw
$backendRoot = $backendCoverage.coverage
$backendLinePct = 100.0 * [double]$backendRoot.'lines-covered' / [double]$backendRoot.'lines-valid'
$backendBranchPct = 100.0 * [double]$backendRoot.'branches-covered' / [double]$backendRoot.'branches-valid'

Assert-Minimum 'Backend lineas' $backendLinePct $backendThresholds.Lines
Assert-Minimum 'Backend ramas' $backendBranchPct $backendThresholds.Branches

Write-Host '=== Cobertura Frontend ===' -ForegroundColor Cyan
Push-Location $frontendRoot
try {
    & npm.cmd run test:coverage
    if ($LASTEXITCODE -ne 0) {
        throw "Las pruebas o la recoleccion de cobertura Frontend fallaron con codigo $LASTEXITCODE"
    }

    $frontendCoveragePath = Join-Path $frontendRoot 'coverage/rl-app/coverage-summary.json'
    if (-not (Test-Path -LiteralPath $frontendCoveragePath -PathType Leaf)) {
        throw 'No se genero coverage/rl-app/coverage-summary.json para el Frontend'
    }

    $frontendCoverage = Get-Content -LiteralPath $frontendCoveragePath -Raw | ConvertFrom-Json
    $frontendStatementPct = [double]$frontendCoverage.total.statements.pct
    $frontendBranchPct = [double]$frontendCoverage.total.branches.pct
    $frontendFunctionPct = [double]$frontendCoverage.total.functions.pct
    $frontendLinePct = [double]$frontendCoverage.total.lines.pct

    Assert-Minimum 'Frontend sentencias' $frontendStatementPct $frontendThresholds.Statements
    Assert-Minimum 'Frontend ramas' $frontendBranchPct $frontendThresholds.Branches
    Assert-Minimum 'Frontend funciones' $frontendFunctionPct $frontendThresholds.Functions
    Assert-Minimum 'Frontend lineas' $frontendLinePct $frontendThresholds.Lines

    if (-not $SkipE2E) {
        Write-Host '=== Pruebas E2E ===' -ForegroundColor Cyan
        & npm.cmd run e2e
        if ($LASTEXITCODE -ne 0) {
            throw "Las pruebas E2E fallaron con codigo $LASTEXITCODE"
        }
    }
}
finally {
    Pop-Location
}

Write-Host '=== Resumen de cobertura ===' -ForegroundColor Cyan
Write-Host ('Backend  lineas={0:F2}% ramas={1:F2}%' -f $backendLinePct, $backendBranchPct)
Write-Host ('Frontend sentencias={0:F2}% ramas={1:F2}% funciones={2:F2}% lineas={3:F2}%' -f $frontendStatementPct, $frontendBranchPct, $frontendFunctionPct, $frontendLinePct)

if ($failures.Count -gt 0) {
    Write-Host 'Puertas de calidad fallidas:' -ForegroundColor Red
    foreach ($failure in $failures) {
        Write-Host "- $failure" -ForegroundColor Red
    }
    exit 1
}

Write-Host 'Puertas de calidad correctas.' -ForegroundColor Green
