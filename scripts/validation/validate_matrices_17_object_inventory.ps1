param(
    [string]$DdlPath = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot '../..')) 'database/19_matrices_riesgos/transicion/06_reconstruir_modelo_17_tablas.sql'),
    [string]$ManifestPath = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot '../..')) 'database/19_matrices_riesgos/transicion/modelo_17_objetos.json'),
    [switch]$Quiet
)

$ErrorActionPreference = 'Stop'
$validationErrors = [System.Collections.Generic.List[string]]::new()

function Add-ValidationError {
    param([string]$Message)
    $validationErrors.Add($Message)
}

function Get-NormalizedArray {
    param([object[]]$Values)
    return @($Values | ForEach-Object { ([string]$_).Trim().ToUpperInvariant() })
}

function Get-Duplicates {
    param([string[]]$Values)
    return @($Values | Group-Object | Where-Object { $_.Count -gt 1 } | ForEach-Object { $_.Name } | Sort-Object)
}

if (-not (Test-Path -LiteralPath $DdlPath -PathType Leaf)) {
    throw "No se encontró el DDL del modelo reducido: $DdlPath"
}
if (-not (Test-Path -LiteralPath $ManifestPath -PathType Leaf)) {
    throw "No se encontró el manifiesto del modelo reducido: $ManifestPath"
}

$manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
$expectedTables = Get-NormalizedArray -Values @($manifest.tables)
$expectedSequences = Get-NormalizedArray -Values @($manifest.sequences)
$retiredTables = Get-NormalizedArray -Values @($manifest.retired_tables)
$retiredSequences = Get-NormalizedArray -Values @($manifest.retired_sequences)

if ($expectedTables.Count -ne 17) {
    Add-ValidationError "El manifiesto debe declarar exactamente 17 tablas; declara $($expectedTables.Count)."
}
if ($expectedSequences.Count -ne 17) {
    Add-ValidationError "El manifiesto debe declarar exactamente 17 secuencias; declara $($expectedSequences.Count)."
}

$tableManifestDuplicates = Get-Duplicates -Values $expectedTables
$sequenceManifestDuplicates = Get-Duplicates -Values $expectedSequences
if ($tableManifestDuplicates.Count -gt 0) {
    Add-ValidationError "El manifiesto contiene tablas duplicadas: $($tableManifestDuplicates -join ', ')."
}
if ($sequenceManifestDuplicates.Count -gt 0) {
    Add-ValidationError "El manifiesto contiene secuencias duplicadas: $($sequenceManifestDuplicates -join ', ')."
}

foreach ($table in $expectedTables) {
    if ($table -notmatch '^RL_MR_[A-Z0-9_]+$') {
        Add-ValidationError "Nombre de tabla inválido en el manifiesto: $table."
    }
}
foreach ($sequence in $expectedSequences) {
    if ($sequence -notmatch '^SEQ_RL_MR_[A-Z0-9_]+$') {
        Add-ValidationError "Nombre de secuencia inválido en el manifiesto: $sequence."
    }
}

$ddlRaw = Get-Content -LiteralPath $DdlPath -Raw
$ddlWithoutBlockComments = [regex]::Replace($ddlRaw, '(?s)/\*.*?\*/', ' ')
$ddlForCreates = [regex]::Replace($ddlWithoutBlockComments, '(?m)--[^\r\n]*$', '')

$activeTables = @(
    [regex]::Matches($ddlForCreates, '(?im)^\s*CREATE\s+TABLE\s+(RL_MR_[A-Z0-9_]+)\b') |
        ForEach-Object { $_.Groups[1].Value.ToUpperInvariant() }
)
$activeSequences = @(
    [regex]::Matches($ddlForCreates, '(?im)^\s*CREATE\s+SEQUENCE\s+(SEQ_RL_MR_[A-Z0-9_]+)\b') |
        ForEach-Object { $_.Groups[1].Value.ToUpperInvariant() }
)

$tableCreateDuplicates = Get-Duplicates -Values $activeTables
$sequenceCreateDuplicates = Get-Duplicates -Values $activeSequences
if ($tableCreateDuplicates.Count -gt 0) {
    Add-ValidationError "CREATE TABLE duplicado: $($tableCreateDuplicates -join ', ')."
}
if ($sequenceCreateDuplicates.Count -gt 0) {
    Add-ValidationError "CREATE SEQUENCE duplicado: $($sequenceCreateDuplicates -join ', ')."
}

$activeTableSet = @($activeTables | Sort-Object -Unique)
$activeSequenceSet = @($activeSequences | Sort-Object -Unique)
$missingTables = @($expectedTables | Where-Object { $_ -notin $activeTableSet } | Sort-Object)
$extraTables = @($activeTableSet | Where-Object { $_ -notin $expectedTables } | Sort-Object)
$missingSequences = @($expectedSequences | Where-Object { $_ -notin $activeSequenceSet } | Sort-Object)
$extraSequences = @($activeSequenceSet | Where-Object { $_ -notin $expectedSequences } | Sort-Object)

if ($missingTables.Count -gt 0) {
    Add-ValidationError "Faltan tablas activas: $($missingTables -join ', ')."
}
if ($extraTables.Count -gt 0) {
    Add-ValidationError "Sobran tablas activas: $($extraTables -join ', ')."
}
if ($missingSequences.Count -gt 0) {
    Add-ValidationError "Faltan secuencias activas: $($missingSequences -join ', ')."
}
if ($extraSequences.Count -gt 0) {
    Add-ValidationError "Sobran secuencias activas: $($extraSequences -join ', ')."
}
if ($activeTableSet.Count -ne 17) {
    Add-ValidationError "El DDL debe contener exactamente 17 CREATE TABLE únicos; contiene $($activeTableSet.Count)."
}
if ($activeSequenceSet.Count -ne 17) {
    Add-ValidationError "El DDL debe contener exactamente 17 CREATE SEQUENCE únicos; contiene $($activeSequenceSet.Count)."
}

$retirementMarker = '-- Secuencias del modelo reducido.'
$retirementMarkerIndex = $ddlRaw.IndexOf($retirementMarker, [System.StringComparison]::OrdinalIgnoreCase)
if ($retirementMarkerIndex -lt 0) {
    Add-ValidationError "No se encontró el marcador que separa el retiro controlado de la creación activa: $retirementMarker"
}
else {
    $retirementSection = $ddlRaw.Substring(0, $retirementMarkerIndex)
    $activeSection = $ddlRaw.Substring($retirementMarkerIndex)

    $retirementTables = @(
        [regex]::Matches($retirementSection, "(?i)'(RL_MR_[A-Z0-9_]+)'") |
            ForEach-Object { $_.Groups[1].Value.ToUpperInvariant() }
    )
    $retirementTableDuplicates = Get-Duplicates -Values $retirementTables
    if ($retirementTableDuplicates.Count -gt 0) {
        Add-ValidationError "La lista de retiro contiene tablas duplicadas: $($retirementTableDuplicates -join ', ')."
    }

    $expectedRetirementTables = @($expectedTables + $retiredTables | Sort-Object -Unique)
    $retirementTableSet = @($retirementTables | Sort-Object -Unique)
    $missingRetirementTables = @($expectedRetirementTables | Where-Object { $_ -notin $retirementTableSet } | Sort-Object)
    $extraRetirementTables = @($retirementTableSet | Where-Object { $_ -notin $expectedRetirementTables } | Sort-Object)

    if ($missingRetirementTables.Count -gt 0) {
        Add-ValidationError "La sección de retiro no contempla estas tablas: $($missingRetirementTables -join ', ')."
    }
    if ($extraRetirementTables.Count -gt 0) {
        Add-ValidationError "La sección de retiro contiene tablas no autorizadas: $($extraRetirementTables -join ', ')."
    }

    if ($retirementSection -notmatch "(?is)USER_SEQUENCES\s+WHERE\s+SEQUENCE_NAME\s+LIKE\s+'SEQ_RL_MR_%'") {
        Add-ValidationError "La sección de retiro debe eliminar genéricamente todas las secuencias SEQ_RL_MR_%."
    }

    foreach ($legacyObject in @($retiredTables + $retiredSequences)) {
        $pattern = '(?i)\b' + [regex]::Escape($legacyObject) + '\b'
        if ([regex]::IsMatch($activeSection, $pattern)) {
            Add-ValidationError "Objeto heredado fuera de la sección autorizada de retiro: $legacyObject."
        }
    }
}

if ($validationErrors.Count -gt 0) {
    Write-Host "Inventario exacto de Matrices: FALLO ($($validationErrors.Count) hallazgos)." -ForegroundColor Red
    foreach ($item in $validationErrors) {
        Write-Host "- $item" -ForegroundColor Red
    }
    exit 1
}

if (-not $Quiet) {
    Write-Host 'Inventario exacto de Matrices: CORRECTO.' -ForegroundColor Green
    Write-Host "Tablas activas: $($activeTableSet.Count). Secuencias activas: $($activeSequenceSet.Count)." -ForegroundColor Green
}
