param([switch]$Quiet)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Resolve-Path (Join-Path $PSScriptRoot '../..')
$validatorPath = Join-Path $PSScriptRoot 'validate_matrices_17_object_inventory.ps1'
$ddlPath = Join-Path $repositoryRoot 'database/19_matrices_riesgos/transicion/06_reconstruir_modelo_17_tablas.sql'
$manifestPath = Join-Path $repositoryRoot 'database/19_matrices_riesgos/transicion/modelo_17_objetos.json'
$pwshPath = (Get-Command pwsh -ErrorAction Stop).Source
$failures = [System.Collections.Generic.List[string]]::new()
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("matrices-17-inventory-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempRoot | Out-Null

function Invoke-InventoryFixture {
    param(
        [string]$Name,
        [string]$DdlContent,
        [bool]$ShouldPass,
        [string]$ExpectedMessage = '',
        [string]$FixtureManifestPath = $manifestPath
    )

    $fixturePath = Join-Path $tempRoot ($Name + '.sql')
    [System.IO.File]::WriteAllText($fixturePath, $DdlContent, [System.Text.UTF8Encoding]::new($false))
    $output = & $pwshPath -NoProfile -File $validatorPath -DdlPath $fixturePath -ManifestPath $FixtureManifestPath -Quiet 2>&1 | Out-String
    $exitCode = $LASTEXITCODE

    if ($ShouldPass -and $exitCode -ne 0) {
        $failures.Add("$Name debía aprobar y falló. Salida: $output")
        return
    }
    if (-not $ShouldPass -and $exitCode -eq 0) {
        $failures.Add("$Name debía fallar y aprobó.")
        return
    }
    if (-not $ShouldPass -and -not [string]::IsNullOrWhiteSpace($ExpectedMessage) -and $output -notmatch [regex]::Escape($ExpectedMessage)) {
        $failures.Add("$Name falló, pero no informó '$ExpectedMessage'. Salida: $output")
    }
}

try {
    $sourceDdl = Get-Content -LiteralPath $ddlPath -Raw

    Invoke-InventoryFixture -Name 'baseline' -DdlContent $sourceDdl -ShouldPass $true

    $missingTable = $sourceDdl -replace '(?im)^\s*CREATE\s+TABLE\s+RL_MR_AUTOMONITOREO\b', '-- CREATE TABLE RL_MR_AUTOMONITOREO'
    Invoke-InventoryFixture -Name 'missing-table' -DdlContent $missingTable -ShouldPass $false -ExpectedMessage 'Faltan tablas activas: RL_MR_AUTOMONITOREO'

    $extraTable = $sourceDdl + "`nCREATE TABLE RL_MR_TABLA_18 (ID NUMBER);`n"
    Invoke-InventoryFixture -Name 'extra-table' -DdlContent $extraTable -ShouldPass $false -ExpectedMessage 'Sobran tablas activas: RL_MR_TABLA_18'

    $duplicateTable = $sourceDdl + "`nCREATE TABLE RL_MR_RIESGOS (ID NUMBER);`n"
    Invoke-InventoryFixture -Name 'duplicate-table' -DdlContent $duplicateTable -ShouldPass $false -ExpectedMessage 'CREATE TABLE duplicado: RL_MR_RIESGOS'

    $missingSequence = $sourceDdl -replace '(?im)^\s*CREATE\s+SEQUENCE\s+SEQ_RL_MR_AUTOMONITOREO\b', '-- CREATE SEQUENCE SEQ_RL_MR_AUTOMONITOREO'
    Invoke-InventoryFixture -Name 'missing-sequence' -DdlContent $missingSequence -ShouldPass $false -ExpectedMessage 'Faltan secuencias activas: SEQ_RL_MR_AUTOMONITOREO'

    $extraSequence = $sourceDdl + "`nCREATE SEQUENCE SEQ_RL_MR_EXTRA START WITH 1 NOCACHE;`n"
    Invoke-InventoryFixture -Name 'extra-sequence' -DdlContent $extraSequence -ShouldPass $false -ExpectedMessage 'Sobran secuencias activas: SEQ_RL_MR_EXTRA'

    $legacyCreate = $sourceDdl + "`nCREATE TABLE RL_MR_AUDITORIA (ID NUMBER);`n"
    Invoke-InventoryFixture -Name 'legacy-create' -DdlContent $legacyCreate -ShouldPass $false -ExpectedMessage 'RL_MR_AUDITORIA'

    $legacyOutsideRetirement = $sourceDdl + "`n-- referencia heredada no autorizada: RL_MR_TRAZAS_CALCULO`n"
    Invoke-InventoryFixture -Name 'legacy-outside-retirement' -DdlContent $legacyOutsideRetirement -ShouldPass $false -ExpectedMessage 'Objeto heredado fuera de la sección autorizada de retiro: RL_MR_TRAZAS_CALCULO'

    $badManifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    $badManifest.tables = @($badManifest.tables | Where-Object { $_ -ne 'RL_MR_AUTOMONITOREO' })
    $badManifestPath = Join-Path $tempRoot 'manifest-16-tables.json'
    [System.IO.File]::WriteAllText(
        $badManifestPath,
        ($badManifest | ConvertTo-Json -Depth 10),
        [System.Text.UTF8Encoding]::new($false))
    Invoke-InventoryFixture -Name 'manifest-16-tables' -DdlContent $sourceDdl -ShouldPass $false -ExpectedMessage 'El manifiesto debe declarar exactamente 17 tablas' -FixtureManifestPath $badManifestPath
}
finally {
    if (Test-Path -LiteralPath $tempRoot) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}

if ($failures.Count -gt 0) {
    Write-Host "Pruebas del inventario exacto: FALLO ($($failures.Count) casos)." -ForegroundColor Red
    foreach ($failure in $failures) {
        Write-Host "- $failure" -ForegroundColor Red
    }
    exit 1
}

if (-not $Quiet) {
    Write-Host 'Pruebas del inventario exacto: CORRECTAS (9 casos).' -ForegroundColor Green
}

exit 0
