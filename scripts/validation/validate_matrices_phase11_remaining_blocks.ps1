$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '../..')
$errors = New-Object System.Collections.Generic.List[string]

$requiredFiles = @(
    'database/19_matrices_riesgos/fase11/03_validar_gestion_riesgos_bloque2_solo_lectura.sql',
    'database/19_matrices_riesgos/fase11/04_validar_flujos_bloque3_solo_lectura.sql',
    'database/19_matrices_riesgos/fase11/05_validar_mitigacion_bloque4_solo_lectura.sql',
    'database/19_matrices_riesgos/fase11/06_validar_alertas_automonitoreo_bloque5_solo_lectura.sql',
    'database/19_matrices_riesgos/fase11/07_validar_auditoria_transacciones_bloque6.sql',
    'backend/RL.API/Features/MatricesRiesgos/Persistence/MatricesRiesgosGestionRepository.cs',
    'backend/RL.API/Features/MatricesRiesgos/Persistence/MatricesRiesgosMitigacionRepository.cs',
    'backend/RL.API/Features/MatricesRiesgos/Persistence/MatricesRiesgosMonitoreoRepository.cs',
    'backend/RL.API/Features/MatricesRiesgos/Application/MatricesRiesgosReportExportService.cs',
    'backend/RL.API/Features/MatricesRiesgos/MatricesRiesgosGestionController.cs',
    'backend/RL.API/Features/MatricesRiesgos/MatricesRiesgosMitigacionController.cs',
    'backend/RL.API/Features/MatricesRiesgos/MatricesRiesgosMonitoreoController.cs',
    'backend/RL.API/Features/MatricesRiesgos/MatricesRiesgosReportesController.cs',
    'frontend/rl-app/src/app/features/admin/matrices-riesgos/models/matrices-riesgos-fase11.models.ts'
)

foreach ($relative in $requiredFiles) {
    if (-not (Test-Path -LiteralPath (Join-Path $root $relative) -PathType Leaf)) {
        $errors.Add("Falta el archivo obligatorio: $relative")
    }
}

function Read-Utf8([string]$relative) {
    return [System.IO.File]::ReadAllText((Join-Path $root $relative), [System.Text.UTF8Encoding]::new($false))
}

$readOnlyScripts = @(
    'database/19_matrices_riesgos/fase11/03_validar_gestion_riesgos_bloque2_solo_lectura.sql',
    'database/19_matrices_riesgos/fase11/04_validar_flujos_bloque3_solo_lectura.sql',
    'database/19_matrices_riesgos/fase11/05_validar_mitigacion_bloque4_solo_lectura.sql',
    'database/19_matrices_riesgos/fase11/06_validar_alertas_automonitoreo_bloque5_solo_lectura.sql'
)

foreach ($relative in $readOnlyScripts) {
    if (-not (Test-Path (Join-Path $root $relative))) { continue }
    $content = Read-Utf8 $relative
    foreach ($pattern in @(
        '(?im)^\s*INSERT\s+',
        '(?im)^\s*UPDATE\s+',
        '(?im)^\s*DELETE\s+',
        '(?im)^\s*MERGE\s+',
        '(?im)^\s*DROP\s+',
        '(?im)^\s*TRUNCATE\s+',
        '(?im)^\s*COMMIT\s*;'
    )) {
        if ([regex]::IsMatch($content, $pattern)) {
            $errors.Add("El validador de solo lectura contiene DML/DDL: $relative / $pattern")
        }
    }
}

$rollbackRelative = 'database/19_matrices_riesgos/fase11/07_validar_auditoria_transacciones_bloque6.sql'
if (Test-Path (Join-Path $root $rollbackRelative)) {
    $rollback = Read-Utf8 $rollbackRelative
    foreach ($token in @(
        'F11_B6_ROLLBACK_TEST',
        'SEQ_RL_MR_RIESGOS.NEXTVAL',
        'SEQ_RL_AUDITORIA.NEXTVAL',
        'ROLLBACK;',
        'PRUEBA ROLLBACK DATO + AUDITORIA: CORRECTA',
        'VALIDACION FASE 11 BLOQUE 6: CORRECTA'
    )) {
        if (-not $rollback.Contains($token)) { $errors.Add("Bloque 6 no contiene el control requerido: $token") }
    }
    foreach ($pattern in @('(?im)^\s*DROP\s+', '(?im)^\s*TRUNCATE\s+', '(?im)^\s*DELETE\s+', '(?im)^\s*COMMIT\s*;')) {
        if ([regex]::IsMatch($rollback, $pattern)) {
            $errors.Add("Bloque 6 contiene una operación prohibida: $pattern")
        }
    }
}

$phase11Sql = Get-ChildItem -LiteralPath (Join-Path $root 'database/19_matrices_riesgos/fase11') -Filter '*.sql' -File
foreach ($file in $phase11Sql) {
    $content = [System.IO.File]::ReadAllText($file.FullName)
    if ($content -match 'B10_') { $errors.Add("Un script de Fase 11 referencia respaldos B10_*: $($file.Name)") }
    if ($content -match '06_reconstruir_modelo_17_tablas' -or $content -match '05_ajustes_dashboard_seguridad_reportes') {
        $errors.Add("Un script de Fase 11 referencia un script prohibido de transición: $($file.Name)")
    }
}

$program = Read-Utf8 'backend/RL.API/Program.cs'
foreach ($token in @(
    'IMatricesRiesgosGestionRepository, MatricesRiesgosGestionRepository',
    'IMatricesRiesgosMitigacionRepository, MatricesRiesgosMitigacionRepository',
    'IMatricesRiesgosMonitoreoRepository, MatricesRiesgosMonitoreoRepository',
    'IMatricesRiesgosGestionService, MatricesRiesgosGestionService',
    'IMatricesRiesgosMitigacionService, MatricesRiesgosMitigacionService',
    'IMatricesRiesgosMonitoreoService, MatricesRiesgosMonitoreoService',
    'IMatricesRiesgosReportExportService, MatricesRiesgosReportExportService'
)) {
    if (-not $program.Contains($token)) { $errors.Add("Program.cs no registra: $token") }
}

$gestionRepo = Read-Utf8 'backend/RL.API/Features/MatricesRiesgos/Persistence/MatricesRiesgosGestionRepository.cs'
$mitigacionRepo = Read-Utf8 'backend/RL.API/Features/MatricesRiesgos/Persistence/MatricesRiesgosMitigacionRepository.cs'
$monitoreoRepo = Read-Utf8 'backend/RL.API/Features/MatricesRiesgos/Persistence/MatricesRiesgosMonitoreoRepository.cs'
foreach ($pair in @(
    @{ Name = 'Gestión'; Content = $gestionRepo },
    @{ Name = 'Mitigación'; Content = $mitigacionRepo },
    @{ Name = 'Monitoreo'; Content = $monitoreoRepo }
)) {
    if (-not $pair.Content.Contains('BeginTransaction')) { $errors.Add("$($pair.Name) no utiliza transacciones Oracle.") }
    if (-not $pair.Content.Contains('_auditoria.RegistrarAsync(conn')) { $errors.Add("$($pair.Name) no registra auditoría en la transacción compartida.") }
    foreach ($forbidden in @('DROP TABLE', 'TRUNCATE TABLE')) {
        if ($pair.Content.Contains($forbidden)) { $errors.Add("$($pair.Name) contiene operación destructiva: $forbidden") }
    }
}

$export = Read-Utf8 'backend/RL.API/Features/MatricesRiesgos/Application/MatricesRiesgosReportExportService.cs'
foreach ($token in @(
    'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    'application/pdf',
    'ZipArchive',
    '%PDF-1.4'
)) {
    if (-not $export.Contains($token)) { $errors.Add("El exportador no contiene el control/formato esperado: $token") }
}

if ($errors.Count -gt 0) {
    Write-Host 'VALIDACION FASE 11 BLOQUES 2-6: INCORRECTA' -ForegroundColor Red
    $errors | ForEach-Object { Write-Host " - $_" -ForegroundColor Red }
    exit 1
}

Write-Host 'VALIDACION FASE 11 BLOQUES 2-6: CORRECTA' -ForegroundColor Green
