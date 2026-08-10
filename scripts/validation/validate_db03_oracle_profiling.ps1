$ErrorActionPreference = 'Stop'

$repositoryRoot = Resolve-Path (Join-Path $PSScriptRoot '../..')
$performanceRoot = Join-Path $repositoryRoot 'database/19_matrices_riesgos/performance'
$masterPath = Join-Path $performanceRoot '00_db03_ejecutar_profiling_autorizado.sql'
$inventoryPath = Join-Path $performanceRoot '01_db03_inventario_estadisticas_solo_lectura.sql'
$explainPath = Join-Path $performanceRoot '02_db03_explain_plan_consultas_criticas.sql'
$readmePath = Join-Path $performanceRoot 'README.md'
$dossierPath = Join-Path $repositoryRoot 'docs/4. Base de Datos/DB_03_PROFILING_ORACLE_EXPLAIN_PLAN_2026-08-10.md'
$workflowPath = Join-Path $repositoryRoot '.github/workflows/quality-gates.yml'
$errors = [System.Collections.Generic.List[string]]::new()

function Assert-FileExists {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        $errors.Add("DB-03: archivo obligatorio inexistente: $Path")
    }
}

function Assert-ContainsTokens {
    param([string]$Path, [string[]]$Tokens, [string]$Context)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return }
    $content = Get-Content -LiteralPath $Path -Raw
    foreach ($token in $Tokens) {
        if (-not $content.Contains($token)) {
            $errors.Add("$Context no contiene el control obligatorio: $token")
        }
    }
}

function Get-ExecutableSql {
    param([string]$Path)
    $content = Get-Content -LiteralPath $Path -Raw
    $withoutBlocks = [System.Text.RegularExpressions.Regex]::Replace(
        $content,
        '/\*.*?\*/',
        '',
        [System.Text.RegularExpressions.RegexOptions]::Singleline)
    $lines = $withoutBlocks -split "`r?`n" | Where-Object {
        $_ -notmatch '^\s*--' -and $_ -notmatch '^\s*PROMPT(?:\s|$)'
    }
    return ($lines -join "`n")
}

foreach ($path in @($masterPath, $inventoryPath, $explainPath, $readmePath, $dossierPath, $workflowPath)) {
    Assert-FileExists $path
}

Assert-ContainsTokens $masterPath @(
    "DEFINE autorizacion = '&1'",
    "UPPER(v_schema) <> 'RIESGO_LAVADO'",
    "UPPER(v_auth) <> 'EJECUTAR_DB03'",
    '@@01_db03_inventario_estadisticas_solo_lectura.sql',
    '@@02_db03_explain_plan_consultas_criticas.sql',
    'WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK'
) 'Punto de entrada DB-03'

if (Test-Path -LiteralPath $masterPath -PathType Leaf) {
    $master = Get-Content -LiteralPath $masterPath -Raw
    if ($master -match '(?i)06_reconstruir_modelo_17_tablas|09_limpieza_tablas_respaldo_b10') {
        $errors.Add('DB-03 no puede incluir scripts de transición o limpieza B10.')
    }
    if ($master -match '(?im)^\s*@@.*(?:transicion|retiro_controlado)') {
        $errors.Add('DB-03 contiene un include hacia un flujo Oracle fuera de performance.')
    }
}

if (Test-Path -LiteralPath $inventoryPath -PathType Leaf) {
    $inventorySql = Get-ExecutableSql $inventoryPath
    if ($inventorySql -match '(?im)\b(?:EXPLAIN\s+PLAN|CREATE|ALTER|DROP|TRUNCATE|INSERT|UPDATE|MERGE|DELETE|COMMIT|ROLLBACK)\b') {
        $errors.Add('El inventario DB-03 dejó de ser exclusivamente de solo lectura.')
    }

    Assert-ContainsTokens $inventoryPath @(
        "SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA')",
        'USER_TAB_STATISTICS',
        'USER_INDEXES',
        'USER_IND_COLUMNS',
        'USER_TAB_COL_STATISTICS',
        "'RL_MR_EVALUACIONES_RIESGO'",
        "'RL_MR_PROYECCIONES_EVALUACION'",
        "'RL_MR_FLUJOS_EVALUACION'",
        "'RL_AUDITORIA'"
    ) 'Inventario DB-03'
}

if (Test-Path -LiteralPath $explainPath -PathType Leaf) {
    $explainContent = Get-Content -LiteralPath $explainPath -Raw
    $explainSql = Get-ExecutableSql $explainPath

    Assert-ContainsTokens $explainPath @(
        'EXPLAIN PLAN SET STATEMENT_ID',
        "DBMS_XPLAN.DISPLAY('PLAN_TABLE'",
        'ROLLBACK;',
        'DB03_Q01', 'DB03_Q02', 'DB03_Q03', 'DB03_Q04', 'DB03_Q05', 'DB03_Q06',
        'DB03_Q07', 'DB03_Q08', 'DB03_Q09', 'DB03_Q10', 'DB03_Q11',
        'ROW_NUMBER() OVER',
        'RL_MR_EVALUACIONES_RIESGO',
        'RL_MR_PROYECCIONES_EVALUACION',
        'RL_MR_FLUJOS_EVALUACION',
        'RL_MR_SENALES_ALERTA',
        'RL_MR_AUTOMONITOREO',
        'RL_AUDITORIA'
    ) 'EXPLAIN PLAN DB-03'

    if ($explainSql -match '(?im)\bCREATE\s+(?:UNIQUE\s+)?INDEX\b|\bALTER\s+TABLE\b|\bDROP\b|\bTRUNCATE\b|\bCOMMIT\b') {
        $errors.Add('El paquete EXPLAIN DB-03 contiene DDL o COMMIT no permitido.')
    }
    if ($explainSql -match '(?im)\b(?:INSERT\s+INTO|UPDATE|DELETE\s+FROM|MERGE\s+INTO)\s+RL_') {
        $errors.Add('El paquete EXPLAIN DB-03 contiene DML directo sobre una tabla RL_*.')
    }
    if ($explainContent -match '(?i)06_reconstruir_modelo_17_tablas|09_limpieza_tablas_respaldo_b10') {
        $errors.Add('El paquete EXPLAIN DB-03 referencia un script destructivo/de limpieza.')
    }

    $explainCount = [regex]::Matches($explainContent, '(?im)^\s*EXPLAIN\s+PLAN\s+SET\s+STATEMENT_ID').Count
    $displayCount = [regex]::Matches($explainContent, "DBMS_XPLAN\.DISPLAY\('PLAN_TABLE'").Count
    if ($explainCount -ne 11) {
        $errors.Add("DB-03 debe perfilar exactamente 11 consultas críticas; encontradas: $explainCount")
    }
    if ($displayCount -ne 11) {
        $errors.Add("DB-03 debe mostrar exactamente 11 planes; encontrados: $displayCount")
    }
}

Assert-ContainsTokens $readmePath @(
    'EJECUTAR_DB03',
    'No contiene `CREATE INDEX`',
    'PLAN_TABLE',
    'TABLE ACCESS FULL',
    "LOWER(...) LIKE '%texto%'"
) 'README DB-03'

Assert-ContainsTokens $dossierPath @(
    '# DB-03 — Profiling Oracle / `EXPLAIN PLAN`',
    '**Ejecución física Oracle:** **PENDIENTE**',
    '**DDL de índices:** **NO EJECUTADO**',
    'DB03_Q01', 'DB03_Q02', 'DB03_Q03', 'DB03_Q04', 'DB03_Q05', 'DB03_Q06',
    'DB03_Q07', 'DB03_Q08', 'DB03_Q09', 'DB03_Q10', 'DB03_Q11',
    'IDX_RL_MR_VER_VIG',
    'IDX_RL_MR_FLU_EVA_FEC',
    'IDX_RL_MR_PROY_BUSQ',
    'No se aprueba ningún índice nuevo sin evidencia física'
) 'Expediente DB-03'

Assert-ContainsTokens $workflowPath @(
    './scripts/validation/validate_db03_oracle_profiling.ps1'
) 'Quality Gate DB-03'

$secretPattern = '(?is)(Data\s+Source|Server)\s*=.+?(User\s+Id|UserId|Uid)\s*=.+?(Password|Pwd)\s*=\s*(?!\s*(?:CHANGE_ME|REPLACE_ME|\$\{|<|__|PENDIENTE|$))'
foreach ($path in @($masterPath, $inventoryPath, $explainPath, $readmePath, $dossierPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { continue }
    $content = Get-Content -LiteralPath $path -Raw
    if ($content -match $secretPattern) {
        $errors.Add("DB-03 detectó una posible cadena de conexión/credencial versionada: $path")
    }
}

if ($errors.Count -gt 0) {
    Write-Host "VALIDACION DB-03: FALLO ($($errors.Count) hallazgos)." -ForegroundColor Red
    foreach ($item in $errors) { Write-Host "- $item" -ForegroundColor Red }
    exit 1
}

Write-Host 'VALIDACION DB-03: CORRECTA.' -ForegroundColor Green
Write-Host 'Paquete de profiling/EXPLAIN PLAN preparado y protegido contra DDL/DML de negocio.' -ForegroundColor Green
Write-Host 'CI no ejecuta Oracle real ni genera planes físicos; esa evidencia requiere ambiente Oracle autorizado.' -ForegroundColor Yellow
exit 0
