$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = Resolve-Path (Join-Path $PSScriptRoot '../..')
$transitionRoot = Join-Path $repositoryRoot 'database/19_matrices_riesgos/transicion'
$transitionPath = Join-Path $transitionRoot '06_reconstruir_modelo_17_tablas.sql'
$preflightPath = Join-Path $transitionRoot '07_preflight_inventario_oracle_solo_lectura.sql'
$postflightPath = Join-Path $transitionRoot '08_postflight_verificacion_modelo_17_tablas_solo_lectura.sql'
$modelPath = Join-Path $transitionRoot 'modelo_17_objetos.json'
$readmePath = Join-Path $transitionRoot 'README.md'
$moduleDocsDir = (Get-ChildItem (Join-Path $repositoryRoot 'docs') -Directory | Where-Object { $_.Name -like '3.*' } | Select-Object -First 1).FullName
$planPath = Join-Path $moduleDocsDir 'FASE_10_PLAN_TRANSICION_FISICA_ORACLE_MODELO_17_TABLAS_PREPARADO_NO_AUTORIZADO_2026-08-06.md'
$authorizationPath = Join-Path $moduleDocsDir 'FASE_9_FORMATO_AUTORIZACION_EJECUCION_ORACLE_FASE_10_2026-08-06.md'
$recordPath = Join-Path $moduleDocsDir 'FASE_10_ACTA_EJECUCION_TRANSICION_ORACLE_MODELO_17_TABLAS_PENDIENTE_2026-08-06.md'
$preparationPath = Join-Path $repositoryRoot 'scripts/operations/prepare_matrices_phase10_evidence.ps1'
$workflowPath = Join-Path $repositoryRoot '.github/workflows/quality-gates.yml'
$errors = [System.Collections.Generic.List[string]]::new()

function Assert-FileExists {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        $errors.Add("Archivo obligatorio de Fase 10 inexistente: $Path")
    }
}

function Assert-ContainsTokens {
    param(
        [string]$Path,
        [string[]]$Tokens,
        [string]$Context
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return
    }

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

$requiredFiles = @(
    $transitionPath,
    $preflightPath,
    $postflightPath,
    $modelPath,
    $readmePath,
    $planPath,
    $authorizationPath,
    $recordPath,
    $preparationPath,
    $workflowPath
)

foreach ($requiredFile in $requiredFiles) {
    Assert-FileExists $requiredFile
}

Assert-ContainsTokens $transitionPath @(
    'Uso: SOLO manual, con respaldo validado y autorizacion expresa.',
    "DEFINE autorizacion = '&1'",
    "UPPER(v_auth) <> 'EJECUTAR'",
    "UPPER(v_schema) <> 'RIESGO_LAVADO'",
    'WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK'
) 'Script destructivo 06'

Assert-ContainsTokens $preflightPath @(
    'PREFLIGHT ORACLE DE MATRICES DE RIESGOS - SOLO LECTURA',
    "UPPER(v_schema_actual) <> 'RIESGO_LAVADO'",
    "TABLE_NAME = 'RL_USUARIOS'",
    "TABLE_NAME = 'RL_AUDITORIA'",
    "SEQUENCE_NAME = 'SEQ_RL_AUDITORIA'",
    'CONTEO REAL DE REGISTROS EN TABLAS RL_MR_*',
    'Este archivo NO autoriza ni ejecuta el script 06.'
) 'Preflight 07'

Assert-ContainsTokens $postflightPath @(
    'POSTFLIGHT ORACLE - MODELO REDUCIDO DE 17 TABLAS',
    'Verificacion de solo lectura. No ejecuta DDL ni DML.',
    "UPPER(v_schema_actual) <> 'RIESGO_LAVADO'",
    "TABLE_NAME = 'RL_USUARIOS'",
    "TABLE_NAME = 'RL_AUDITORIA'",
    "SEQUENCE_NAME = 'SEQ_RL_AUDITORIA'",
    'Tablas RL_MR_*             : ',
    'Secuencias SEQ_RL_MR_*     : ',
    'POSTFLIGHT CORRECTO: inventario fisico 17/17 sin objetos heredados detectados.',
    'No declarar certificacion funcional; corresponde a la Fase 11.',
    'Este archivo no ejecuta ni autoriza el script 06.'
) 'Postflight 08'

foreach ($readOnlyPath in @($preflightPath, $postflightPath)) {
    if (-not (Test-Path -LiteralPath $readOnlyPath -PathType Leaf)) {
        continue
    }

    $executableSql = Get-ExecutableSql $readOnlyPath
    $rawContent = Get-Content -LiteralPath $readOnlyPath -Raw

    if ($executableSql -match '(?im)\b(?:CREATE|ALTER|DROP|TRUNCATE|INSERT|UPDATE|MERGE|DELETE|COMMIT)\b') {
        $errors.Add("El archivo de solo lectura contiene DDL o DML: $readOnlyPath")
    }

    if ($rawContent -match '(?im)^\s*@@') {
        $errors.Add("El archivo de solo lectura no puede incluir otros scripts: $readOnlyPath")
    }

    if ($rawContent -match '(?im)^\s*@[^@].*06_reconstruir_modelo_17_tablas\.sql') {
        $errors.Add("El archivo de solo lectura intenta ejecutar el script 06: $readOnlyPath")
    }
}

if (Test-Path -LiteralPath $modelPath -PathType Leaf) {
    try {
        $model = Get-Content -LiteralPath $modelPath -Raw | ConvertFrom-Json

        if (@($model.tables).Count -ne 17) {
            $errors.Add("El manifiesto debe contener exactamente 17 tablas; detectadas: $(@($model.tables).Count).")
        }

        if (@($model.sequences).Count -ne 17) {
            $errors.Add("El manifiesto debe contener exactamente 17 secuencias; detectadas: $(@($model.sequences).Count).")
        }

        if (@($model.tables | Select-Object -Unique).Count -ne 17) {
            $errors.Add('El manifiesto contiene tablas duplicadas.')
        }

        if (@($model.sequences | Select-Object -Unique).Count -ne 17) {
            $errors.Add('El manifiesto contiene secuencias duplicadas.')
        }

        $transitionContent = if (Test-Path -LiteralPath $transitionPath) { Get-Content -LiteralPath $transitionPath -Raw } else { '' }
        $postflightContent = if (Test-Path -LiteralPath $postflightPath) { Get-Content -LiteralPath $postflightPath -Raw } else { '' }

        foreach ($table in @($model.tables)) {
            if (-not $transitionContent.Contains($table)) {
                $errors.Add("El script 06 no contiene la tabla objetivo: $table")
            }
            if (-not $postflightContent.Contains($table)) {
                $errors.Add("El postflight 08 no verifica la tabla objetivo: $table")
            }
        }

        foreach ($sequence in @($model.sequences)) {
            if (-not $transitionContent.Contains($sequence)) {
                $errors.Add("El script 06 no contiene la secuencia objetivo: $sequence")
            }
            if (-not $postflightContent.Contains($sequence)) {
                $errors.Add("El postflight 08 no verifica la secuencia objetivo: $sequence")
            }
        }

        foreach ($retiredTable in @($model.retired_tables)) {
            if (-not $transitionContent.Contains($retiredTable)) {
                $errors.Add("El script 06 no retira la tabla heredada: $retiredTable")
            }
            if (-not $postflightContent.Contains($retiredTable)) {
                $errors.Add("El postflight 08 no controla la tabla retirada: $retiredTable")
            }
        }

        foreach ($retiredSequence in @($model.retired_sequences)) {
            if (-not $postflightContent.Contains($retiredSequence)) {
                $errors.Add("El postflight 08 no controla la secuencia retirada: $retiredSequence")
            }
        }
    }
    catch {
        $errors.Add("No se pudo interpretar el manifiesto JSON del modelo: $($_.Exception.Message)")
    }
}

Assert-ContainsTokens $preparationPath @(
    "'desarrollo'",
    "'status', '--porcelain'",
    "'rev-parse', 'HEAD'",
    'Get-FileHash -LiteralPath $absolutePath -Algorithm SHA256',
    'authorized       = $false',
    'oracleExecuted   = $false',
    'Este manifiesto no conecta a Oracle.',
    'Autorización de Fase 10 permanece NO OTORGADA.'
) 'Preparador de evidencias Fase 10'

if (Test-Path -LiteralPath $preparationPath -PathType Leaf) {
    $preparationContent = Get-Content -LiteralPath $preparationPath -Raw
    if ($preparationContent -match '(?im)\bsqlplus\b|Invoke-Sqlcmd|OracleConnection|ConnectionStrings__OracleDB') {
        $errors.Add('El preparador de evidencias no puede conectarse ni ejecutar Oracle.')
    }
}

Assert-ContainsTokens $planPath @(
    '# Fase 10 — Plan operativo de transición física Oracle',
    '**Estado:** PREPARACIÓN TÉCNICA COMPLETADA Y CERTIFICADA',
    '**Autorización de ejecución:** NO OTORGADA.',
    '08_postflight_verificacion_modelo_17_tablas_solo_lectura.sql',
    'prepare_matrices_phase10_evidence.ps1',
    'FASE_10_ACTA_EJECUCION_TRANSICION_ORACLE_MODELO_17_TABLAS_PENDIENTE_2026-08-06.md',
    'FASE 10 — PREPARACION TECNICA: COMPLETADA Y CERTIFICADA',
    'SCRIPT 06: NO EJECUTADO',
    '13 vulnerabilidades',
    '1 crítica'
) 'Plan operativo de Fase 10'

Assert-ContainsTokens $recordPath @(
    '# Fase 10 — Acta de ejecución de transición física Oracle',
    '**Estado del acta:** PENDIENTE DE DILIGENCIAMIENTO.',
    '**Autorización:** NO OTORGADA.',
    '## 5. Respaldo y restauración',
    '## 6. Preflight `07` de solo lectura',
    '## 7. Autorización final antes del DDL',
    '## 8. Ejecución del script `06`',
    '## 9. Postflight `08` de solo lectura',
    'DECISION: NO OTORGADA',
    'ORACLE EJECUTADO: NO',
    'SCRIPT 06 EJECUTADO: NO',
    'POSTFLIGHT 08 EJECUTADO: NO',
    'FASE 10: NO COMPLETADA',
    '13 vulnerabilidades',
    '1 crítica'
) 'Acta operativa de Fase 10'

Assert-ContainsTokens $authorizationPath @(
    '**Estado actual:** **NO OTORGADA**.',
    'DECISION: NO OTORGADA',
    'AUTORIZACION FASE 10: NO OTORGADA',
    'ORACLE EJECUTADO: NO',
    'SCRIPT 06 EJECUTADO: NO'
) 'Formato separado de autorización'

if (Test-Path -LiteralPath $authorizationPath -PathType Leaf) {
    $authorizationContent = Get-Content -LiteralPath $authorizationPath -Raw
    if ($authorizationContent -match '(?im)^\s*DECISION:\s*OTORGADA\s*$') {
        $errors.Add('La autorización de Fase 10 aparece otorgada sin evidencia externa verificable.')
    }
}

Assert-ContainsTokens $readmePath @(
    '07_preflight_inventario_oracle_solo_lectura.sql',
    '06_reconstruir_modelo_17_tablas.sql',
    '08_postflight_verificacion_modelo_17_tablas_solo_lectura.sql',
    'prepare_matrices_phase10_evidence.ps1',
    'No existe autorización implícita'
) 'README de transición'

Assert-ContainsTokens $workflowPath @(
    './scripts/validation/validate_matrices_phase10_transition_package.ps1'
) 'Quality Gate'

foreach ($file in @($planPath, $authorizationPath, $recordPath, $preparationPath, $preflightPath, $postflightPath)) {
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) {
        continue
    }

    $content = Get-Content -LiteralPath $file -Raw
    $secretPattern = '(?is)(Data\s+Source|Server)\s*=.+?(User\s+Id|UserId|Uid)\s*=.+?(Password|Pwd)\s*=\s*(?!\s*(?:CHANGE_ME|REPLACE_ME|\$\{|<|__|PENDIENTE|$))'
    if ($content -match $secretPattern) {
        $errors.Add("Posible credencial Oracle codificada en: $file")
    }
}

if ($errors.Count -gt 0) {
    Write-Host "Validación del paquete operativo Fase 10: FALLO ($($errors.Count) hallazgos)." -ForegroundColor Red
    foreach ($item in $errors) {
        Write-Host "- $item" -ForegroundColor Red
    }
    exit 1
}

Write-Host 'Validación del paquete operativo Fase 10: CORRECTA.' -ForegroundColor Green
Write-Host 'Preflight 07, script manual 06, postflight 08, manifiesto y acta permanecen controlados.' -ForegroundColor Green
Write-Host 'Oracle no fue conectado ni ejecutado. Autorización permanece NO OTORGADA.' -ForegroundColor Yellow
exit 0
