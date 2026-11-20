param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [switch]$SelfTest
)

$ErrorActionPreference = 'Stop'

# Definición canónica de las 6 operaciones de escritura heredadas permitidas exclusivamente en ListasRepository.cs
$allowedLegacyWrites = @(
    @{ Id = "DNP-01"; File = "backend/RL.API/Features/Listas/Persistence/ListasRepository.cs"; Table = "DNP_IHSS.TIPO_LISTAS_CAUTELA"; Op = "INSERT"; Method = "CrearTipoListaCautelaAsync" },
    @{ Id = "DNP-02"; File = "backend/RL.API/Features/Listas/Persistence/ListasRepository.cs"; Table = "DNP_IHSS.TIPO_LISTAS_CAUTELA"; Op = "UPDATE"; Method = "ActualizarTipoListaCautelaAsync" },
    @{ Id = "DNP-03"; File = "backend/RL.API/Features/Listas/Persistence/ListasRepository.cs"; Table = "DNP_IHSS.TIPO_LISTAS_CAUTELA"; Op = "DELETE"; Method = "EliminarTipoListaCautelaAsync" },
    @{ Id = "DNP-04"; File = "backend/RL.API/Features/Listas/Persistence/ListasRepository.cs"; Table = "DNP_IHSS.REPORTE_COINCIDENCIAS"; Op = "UPDATE"; Method = "CalificarCoincidenciaAsync" },
    @{ Id = "DNP-05"; File = "backend/RL.API/Features/Listas/Persistence/ListasRepository.cs"; Table = "DNP_IHSS.LISTA_CAUTELA"; Op = "DELETE"; Method = "CargarListaCautelaAsync" },
    @{ Id = "DNP-06"; File = "backend/RL.API/Features/Listas/Persistence/ListasRepository.cs"; Table = "DNP_IHSS.LISTA_CAUTELA"; Op = "INSERT"; Method = "CargarListaCautelaAsync" }
)

# Regex universal de operaciones de escritura sobre DNP_IHSS (incluye multilínea, comentarios intercalados y mayúsculas/minúsculas)
# Operaciones vigiladas: INSERT, UPDATE, DELETE, MERGE, TRUNCATE, ALTER, DROP, CREATE
$writeOperationsRegex = '(?is)\b(INSERT\s+(?:/\*.*?\*/\s+)*INTO|UPDATE|DELETE\s+(?:/\*.*?\*/\s+)*FROM|MERGE\s+(?:/\*.*?\*/\s+)*INTO|TRUNCATE\s+(?:/\*.*?\*/\s+)*TABLE|ALTER\s+(?:/\*.*?\*/\s+)*TABLE|DROP\s+(?:/\*.*?\*/\s+)*TABLE|CREATE\s+(?:/\*.*?\*/\s+)*TABLE)\s+(?:/\*.*?\*/\s+)*(?:\"?DNP_IHSS\"?\.)\"?([A-Z0-9_]+)\"?'

function Remove-CodeComments {
    param([string]$text)
    # Remover comentarios de bloque /* ... */ sin romper saltos de línea para preservar numeración
    $noBlockComments = [regex]::Replace($text, '/\*.*?\*/', { param($m) [regex]::Replace($m.Value, '[^\r\n]', ' ') }, [System.Text.RegularExpressions.RegexOptions]::Singleline)
    # Remover comentarios de una sola línea // y --
    $noComments = [regex]::Replace($noBlockComments, '(?m)(//|--).*?$', '')
    return $noComments
}

function Find-ExternalWritesInText {
    param(
        [string]$text,
        [string]$relativeFilePath
    )

    $cleanText = Remove-CodeComments -text $text
    $matches = [regex]::Matches($cleanText, $writeOperationsRegex)
    $results = [System.Collections.Generic.List[PSObject]]::new()

    foreach ($m in $matches) {
        $rawOp = $m.Groups[1].Value -replace '\s+', ' '
        $op = ($rawOp.Trim().ToUpperInvariant() -split ' ')[0]
        $table = "DNP_IHSS." + $m.Groups[2].Value.ToUpperInvariant()

        # Calcular línea aproximada
        $prefix = $cleanText.Substring(0, $m.Index)
        $lineNum = ([regex]::Matches($prefix, '\r?\n')).Count + 1

        $results.Add([PSCustomObject]@{
            File = $relativeFilePath
            Line = $lineNum
            Operation = $op
            FullOperation = $rawOp.Trim().ToUpperInvariant()
            Table = $table
            RawMatch = $m.Value
        })
    }

    return $results
}

function Audit-Directory {
    param(
        [string]$RootPath,
        [string[]]$SearchFolders,
        [hashtable[]]$AllowedWrites
    )

    $sourceExtensions = @("*.cs", "*.ts", "*.js", "*.sql")
    $foundWrites = [System.Collections.Generic.List[PSObject]]::new()
    $violations = [System.Collections.Generic.List[PSObject]]::new()
    $matchedAllowedCount = 0

    foreach ($folder in $SearchFolders) {
        $folderPath = Join-Path $RootPath $folder
        if (-not (Test-Path $folderPath)) { continue }

        Get-ChildItem -Path $folderPath -Include $sourceExtensions -Recurse -File | ForEach-Object {
            $file = $_
            $relPath = $file.FullName.Substring($RootPath.Length).TrimStart('\', '/').Replace('\', '/')

            if ($relPath -match '/obj/' -or $relPath -match '/bin/' -or $relPath -match '/node_modules/') {
                return
            }

            $content = [System.IO.File]::ReadAllText($file.FullName)
            $writes = Find-ExternalWritesInText -text $content -relativeFilePath $relPath

            foreach ($w in $writes) {
                $foundWrites.Add($w)

                # Comprobar si coincide con una de las rutas heredadas autorizadas
                $matched = $false
                foreach ($leg in $AllowedWrites) {
                    if ($w.File.Equals($leg.File, [System.StringComparison]::OrdinalIgnoreCase) -and
                        $w.Table.Equals($leg.Table, [System.StringComparison]::OrdinalIgnoreCase) -and
                        $w.Operation.Equals($leg.Op, [System.StringComparison]::OrdinalIgnoreCase)) {
                        $matched = $true
                        break
                    }
                }

                if ($matched) {
                    $matchedAllowedCount++
                } else {
                    $violations.Add($w)
                }
            }
        }
    }

    return @{
        TotalWrites = $foundWrites.Count
        AllowedMatches = $matchedAllowedCount
        Violations = $violations
        AllWrites = $foundWrites
    }
}

# Modo Self-Test
if ($SelfTest) {
    Write-Host "Ejecutando suite de auto-pruebas sinteticas de validate_external_writes..." -ForegroundColor Cyan
    $testPassed = $true

    # 1. Prueba: sentencia SELECT pura no debe considerarse escritura
    $selectSql = "SELECT * FROM DNP_IHSS.REPORTE_COINCIDENCIAS WHERE ROWNUM = 1"
    $r1 = Find-ExternalWritesInText -text $selectSql -relativeFilePath "dummy.cs"
    if ($r1.Count -ne 0) {
        Write-Host "  FAIL: SELECT reportado falsamente como escritura" -ForegroundColor Red
        $testPassed = $false
    } else {
        Write-Host "  PASS: SELECT puro no detectado como escritura" -ForegroundColor Green
    }

    # 2. Prueba: MERGE no autorizado es detectado
    $mergeSql = "MERGE INTO DNP_IHSS.OTRA_TABLA USING DUAL ON (1=1) WHEN MATCHED THEN UPDATE SET A = 1"
    $r2 = Find-ExternalWritesInText -text $mergeSql -relativeFilePath "dummy.cs"
    if ($r2.Count -eq 0 -or $r2[0].Operation -ne "MERGE") {
        Write-Host "  FAIL: MERGE INTO no detectado" -ForegroundColor Red
        $testPassed = $false
    } else {
        Write-Host "  PASS: MERGE INTO detectado correctamente" -ForegroundColor Green
    }

    # 3. Prueba: Escritura multilínea dividida con espacios y saltos de línea
    $multilineSql = @"
        INSERT
        INTO
        DNP_IHSS.NUEVA_TABLA_PROHIBIDA (COL1) VALUES (1)
"@
    $r3 = Find-ExternalWritesInText -text $multilineSql -relativeFilePath "dummy.cs"
    if ($r3.Count -eq 0 -or $r3[0].Table -ne "DNP_IHSS.NUEVA_TABLA_PROHIBIDA") {
        Write-Host "  FAIL: Escritura multilinea no detectada" -ForegroundColor Red
        $testPassed = $false
    } else {
        Write-Host "  PASS: Escritura multilinea detectada correctamente" -ForegroundColor Green
    }

    # 4. Prueba: Comentarios no deben ocultar escrituras (comentarios intercalados)
    $commentSql = "INSERT /* comentario */ INTO DNP_IHSS.TABLA_CON_COM (COL) VALUES (1)"
    $r4 = Find-ExternalWritesInText -text $commentSql -relativeFilePath "dummy.cs"
    if ($r4.Count -eq 0) {
        Write-Host "  FAIL: Escritura con comentarios intercalados no detectada" -ForegroundColor Red
        $testPassed = $false
    } else {
        Write-Host "  PASS: Escritura con comentarios intercalados detectada" -ForegroundColor Green
    }

    # 5. Prueba: Comentario puro con texto de INSERT no debe ser falso positivo
    $pureCommentSql = "// INSERT INTO DNP_IHSS.COMENTADA (A) VALUES (1)"
    $r5 = Find-ExternalWritesInText -text $pureCommentSql -relativeFilePath "dummy.cs"
    if ($r5.Count -ne 0) {
        Write-Host "  FAIL: Comentario de linea reportado como falso positivo" -ForegroundColor Red
        $testPassed = $false
    } else {
        Write-Host "  PASS: Comentario puro no genera falso positivo" -ForegroundColor Green
    }

    # 6. Prueba: DROP TABLE o TRUNCATE TABLE detectado
    $ddlSql = "TRUNCATE TABLE DNP_IHSS.TABLA_CAUTELA"
    $r6 = Find-ExternalWritesInText -text $ddlSql -relativeFilePath "dummy.cs"
    if ($r6.Count -eq 0 -or $r6[0].Operation -ne "TRUNCATE") {
        Write-Host "  FAIL: TRUNCATE TABLE no detectado" -ForegroundColor Red
        $testPassed = $false
    } else {
        Write-Host "  PASS: TRUNCATE TABLE detectado correctamente" -ForegroundColor Green
    }

    if (-not $testPassed) {
        Write-Error "SELF_TEST_FAILED: Una o mas autopruebas del validador fallaron."
        exit 1
    }

    Write-Host "EXTERNAL_WRITES_VALIDATOR_SELF_TEST=PASS" -ForegroundColor Green
    exit 0
}

# Ejecución normal sobre el repositorio
$repoFull = [System.IO.Path]::GetFullPath($RepositoryRoot)
Write-Host "Iniciando auditoria preventiva de escrituras a fuentes externas (DNP_IHSS)..."

$audit = Audit-Directory -RootPath $repoFull -SearchFolders @("backend/RL.API", "frontend/rl-app/src") -AllowedWrites $allowedLegacyWrites

Write-Host "Operaciones de escritura heredadas encontradas en ListasRepository: $($audit.AllowedMatches)/$($allowedLegacyWrites.Count)"
Write-Host "Nuevas operaciones de escritura no autorizadas a DNP_IHSS: $($audit.Violations.Count)"

if ($audit.Violations.Count -gt 0) {
    Write-Host "ERROR: Se encontraron escrituras no autorizadas a fuentes externas:" -ForegroundColor Red
    foreach ($v in $audit.Violations) {
        Write-Host "  [VIOLACION] $($v.File):$($v.Line) -> $($v.Operation) sobre $($v.Table)" -ForegroundColor Yellow
        Write-Host "    $($v.RawMatch)" -ForegroundColor DarkGray
    }
    exit 1
}

if ($audit.AllowedMatches -ne 6) {
    Write-Host "ADVERTENCIA: Se esperaban exactamente 6 rutas heredadas auditadas, pero se encontraron $($audit.AllowedMatches)." -ForegroundColor Yellow
}

Write-Host "=========================================================="
Write-Host "AUDITORIA DE FUENTES EXTERNAS (DNP_IHSS):"
Write-Host "  NO_NEW_EXTERNAL_WRITES=PASS"
Write-Host "  DNP_GLOBAL_READ_ONLY_COMPLIANCE=NOT_YET_DEMONSTRATED"
Write-Host "  RUTAS HEREDADAS AUDITADAS: $($audit.AllowedMatches)/6 (DNP-01 a DNP-06)"
Write-Host "=========================================================="
exit 0
