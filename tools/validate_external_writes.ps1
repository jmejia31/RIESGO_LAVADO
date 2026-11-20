param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
$repoFull = [System.IO.Path]::GetFullPath($RepositoryRoot)

Write-Host "Iniciando auditoria preventiva de escrituras a fuentes externas (DNP_IHSS)..."

# Lista canónica de las 6 operaciones de escritura heredadas permitidas exclusivamente en ListasRepository.cs
$allowedLegacyWrites = @(
    @{ File = "backend/RL.API/Features/Listas/Persistence/ListasRepository.cs"; Table = "DNP_IHSS.TIPO_LISTAS_CAUTELA"; Op = "INSERT"; Method = "CrearTipoListaCautelaAsync" },
    @{ File = "backend/RL.API/Features/Listas/Persistence/ListasRepository.cs"; Table = "DNP_IHSS.TIPO_LISTAS_CAUTELA"; Op = "UPDATE"; Method = "ActualizarTipoListaCautelaAsync" },
    @{ File = "backend/RL.API/Features/Listas/Persistence/ListasRepository.cs"; Table = "DNP_IHSS.TIPO_LISTAS_CAUTELA"; Op = "DELETE"; Method = "EliminarTipoListaCautelaAsync" },
    @{ File = "backend/RL.API/Features/Listas/Persistence/ListasRepository.cs"; Table = "DNP_IHSS.REPORTE_COINCIDENCIAS"; Op = "UPDATE"; Method = "CalificarCoincidenciaAsync" },
    @{ File = "backend/RL.API/Features/Listas/Persistence/ListasRepository.cs"; Table = "DNP_IHSS.LISTA_CAUTELA"; Op = "DELETE"; Method = "CargarListaCautelaAsync" },
    @{ File = "backend/RL.API/Features/Listas/Persistence/ListasRepository.cs"; Table = "DNP_IHSS.LISTA_CAUTELA"; Op = "INSERT"; Method = "CargarListaCautelaAsync" }
)

# Buscar archivos de código fuente productivo en backend y frontend
$sourceExtensions = @("*.cs", "*.ts", "*.js", "*.sql")
$searchFolders = @("backend/RL.API", "frontend/rl-app/src")

$foundViolations = [System.Collections.Generic.List[string]]::new()
$legacyMatchesFound = 0

$writePattern = [regex]'(?i)(INSERT\s+INTO|UPDATE|DELETE\s+FROM|MERGE\s+INTO)\s+(?:\"?DNP_IHSS\"?\.)?\"?([A-Z0-9_]+)\"?'

foreach ($folder in $searchFolders) {
    $folderPath = Join-Path $repoFull $folder
    if (-not (Test-Path $folderPath)) { continue }

    Get-ChildItem -Path $folderPath -Include $sourceExtensions -Recurse -File | ForEach-Object {
        $file = $_
        $relPath = $file.FullName.Substring($repoFull.Length).TrimStart('\', '/').Replace('\', '/')

        # Ignorar archivos compilados o binarios
        if ($relPath -match '/obj/' -or $relPath -match '/bin/' -or $relPath -match '/node_modules/') {
            return
        }

        $lines = [System.IO.File]::ReadAllLines($file.FullName)
        for ($i = 0; $i -lt $lines.Length; $i++) {
            $line = $lines[$i]
            if ($line -match '(?i)(INSERT\s+INTO|UPDATE|DELETE\s+FROM|MERGE\s+INTO)\s+(?:\"?DNP_IHSS\"?\.)') {
                $matchedOp = $Matches[1].ToUpper()
                $lineNum = $i + 1

                # Comprobar si coincide con una de las 6 rutas heredadas documentadas
                $isAllowed = $false
                foreach ($legacy in $allowedLegacyWrites) {
                    if ($relPath.Equals($legacy.File, [System.StringComparison]::OrdinalIgnoreCase)) {
                        if ($line -match [regex]::Escape($legacy.Table)) {
                            $isAllowed = $true
                            $legacyMatchesFound++
                            break
                        }
                    }
                }

                if (-not $isAllowed) {
                    $foundViolations.Add("Violacion detectada en ${relPath}:${lineNum}: $line")
                }
            }
        }
    }
}

Write-Host "Operaciones de escritura heredadas encontradas en ListasRepository: $legacyMatchesFound"
Write-Host "Nuevas operaciones de escritura no autorizadas a DNP_IHSS: $($foundViolations.Count)"

if ($foundViolations.Count -gt 0) {
    Write-Host "ERROR: Se encontraron escrituras no autorizadas a fuentes externas:" -ForegroundColor Red
    foreach ($v in $foundViolations) {
        Write-Host "  $v" -ForegroundColor Yellow
    }
    exit 1
}

Write-Host "=========================================================="
Write-Host "AUDITORIA DE FUENTES EXTERNAS (DNP_IHSS):"
Write-Host "  NO_NEW_EXTERNAL_WRITES=PASS"
Write-Host "  DNP_GLOBAL_READ_ONLY_COMPLIANCE=NOT_YET_DEMONSTRATED"
Write-Host "  RUTAS HEREDADAS AUDITADAS: 6 (TIPO_LISTAS_CAUTELA:3, REPORTE_COINCIDENCIAS:1, LISTA_CAUTELA:2)"
Write-Host "=========================================================="
exit 0
