$ErrorActionPreference = 'Stop'

$repositoryRoot = Resolve-Path (Join-Path $PSScriptRoot '../..')
$preflightPath = Join-Path $repositoryRoot 'database/19_matrices_riesgos/transicion/07_preflight_inventario_oracle_solo_lectura.sql'
$transitionPath = Join-Path $repositoryRoot 'database/19_matrices_riesgos/transicion/06_reconstruir_modelo_17_tablas.sql'
$dossierPath = Join-Path $repositoryRoot 'docs/3. Módulo Matrices de Riesgos/FASE_9_EXPEDIENTE_AUTORIZACION_ORACLE_MODELO_17_TABLAS_2026-08-06.md'
$authorizationPath = Join-Path $repositoryRoot 'docs/3. Módulo Matrices de Riesgos/FASE_9_FORMATO_AUTORIZACION_EJECUCION_ORACLE_FASE_10_2026-08-06.md'
$workflowPath = Join-Path $repositoryRoot '.github/workflows/quality-gates.yml'
$errors = [System.Collections.Generic.List[string]]::new()

function Assert-FileExists {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        $errors.Add("Archivo obligatorio de Fase 9 inexistente: $Path")
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

foreach ($requiredFile in @(
    $preflightPath,
    $transitionPath,
    $dossierPath,
    $authorizationPath,
    $workflowPath)) {
    Assert-FileExists $requiredFile
}

Assert-ContainsTokens $preflightPath @(
    'PREFLIGHT ORACLE DE MATRICES DE RIESGOS - SOLO LECTURA',
    "SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA')",
    "SYS_CONTEXT('USERENV', 'SESSION_USER')",
    "SYS_CONTEXT('USERENV', 'DB_NAME')",
    "SYS_CONTEXT('USERENV', 'SERVER_HOST')",
    "UPPER(v_schema_actual) <> 'RIESGO_LAVADO'",
    "TABLE_NAME = 'RL_USUARIOS'",
    "TABLE_NAME = 'RL_AUDITORIA'",
    "SEQUENCE_NAME = 'SEQ_RL_AUDITORIA'",
    "TABLE_NAME LIKE 'RL\_MR\_%'",
    "SEQUENCE_NAME LIKE 'SEQ\_RL\_MR\_%'",
    'CONTEO REAL DE REGISTROS EN TABLAS RL_MR_*',
    'OBJETOS INVALIDOS DEL ESQUEMA',
    'RESTRICCIONES DESHABILITADAS EN OBJETOS RL_MR_*',
    'Este resultado no certifica el modelo de 17 tablas.'
) 'Preflight Oracle de solo lectura'

if (Test-Path -LiteralPath $preflightPath -PathType Leaf) {
    $preflightContent = Get-Content -LiteralPath $preflightPath -Raw
    $preflightSql = Get-ExecutableSql $preflightPath

    if ($preflightSql -match '(?im)\b(?:CREATE|ALTER|DROP|TRUNCATE|INSERT|UPDATE|MERGE|DELETE|COMMIT)\b') {
        $errors.Add('El preflight de Fase 9 dejó de ser de solo lectura.')
    }

    if ($preflightContent -match '(?im)^\s*@@') {
        $errors.Add('El preflight de Fase 9 no puede incluir otros scripts SQL.')
    }

    if ($preflightContent -match '(?im)^\s*@[^@].*06_reconstruir_modelo_17_tablas\.sql') {
        $errors.Add('El preflight de Fase 9 intenta ejecutar el script destructivo 06.')
    }

    if ($preflightContent -match '(?is)(Data\s+Source|Server)\s*=.+?(User\s+Id|UserId|Uid)\s*=.+?(Password|Pwd)\s*=') {
        $errors.Add('El preflight de Fase 9 contiene una cadena de conexión codificada.')
    }
}

Assert-ContainsTokens $transitionPath @(
    'Uso: SOLO manual, con respaldo validado y autorizacion expresa.',
    "DEFINE autorizacion = '&1'",
    "UPPER(v_auth) <> 'EJECUTAR'",
    "UPPER(v_schema) <> 'RIESGO_LAVADO'"
) 'Script destructivo 06'

Assert-ContainsTokens $dossierPath @(
    '# Fase 9 — Expediente de preparación y autorización Oracle',
    '**Autorización para la Fase 10:** **NO OTORGADA**.',
    '## 3. Identificación obligatoria del ambiente Oracle',
    '## 4. Confirmación de aislamiento y ausencia de datos productivos',
    '## 5. Responsables y segregación de funciones',
    '## 7. Respaldo y restauración',
    '## 8. Inventario físico previo de solo lectura',
    '## 9. Permisos mínimos',
    '## 10. Método seguro para la conexión Oracle',
    '## 11. Plan previsto para la Fase 10',
    '## 12. Plan de contingencia',
    '## 13. Evidencias obligatorias',
    '## 14. Criterios de entrada para la Fase 10',
    '## 16. Dictamen de la Fase 9',
    'AMBIENTE ORACLE EXCLUSIVO: PENDIENTE DE IDENTIFICACION Y EVIDENCIA',
    'AUTORIZACION FASE 10: NO OTORGADA',
    'SCRIPT 06: NO EJECUTADO',
    '13 vulnerabilidades',
    '6 moderadas',
    '6 altas',
    '1 crítica'
) 'Expediente de Fase 9'

Assert-ContainsTokens $authorizationPath @(
    '# Formato de autorización separada — Fase 10 Oracle',
    '**Estado actual:** **NO OTORGADA**.',
    '## 2. Prerrequisitos',
    '## 3. Declaración del DBA',
    '## 4. Declaración del responsable funcional',
    '## 5. Autorización de Javier Mejía',
    'DECISION: NO OTORGADA',
    'AUTORIZACION FASE 10: NO OTORGADA',
    'ORACLE EJECUTADO: NO',
    'SCRIPT 06 EJECUTADO: NO'
) 'Formato de autorización de Fase 10'

if (Test-Path -LiteralPath $authorizationPath -PathType Leaf) {
    $authorizationContent = Get-Content -LiteralPath $authorizationPath -Raw

    if ($authorizationContent -match '(?im)^\s*DECISION:\s*OTORGADA\s*$') {
        $errors.Add('El formato de autorización de Fase 10 aparece aprobado sin evidencia externa.')
    }

    foreach ($placeholder in @(
        'PENDIENTE_DBA',
        'PENDIENTE',
        '**Aprobación verificable:** PENDIENTE.')) {
        if (-not $authorizationContent.Contains($placeholder)) {
            $errors.Add("El formato de autorización perdió el marcador obligatorio: $placeholder")
        }
    }
}

Assert-ContainsTokens $workflowPath @(
    './scripts/validation/validate_matrices_phase9_oracle_dossier.ps1'
) 'Quality Gate'

foreach ($documentationFile in @($dossierPath, $authorizationPath)) {
    if (-not (Test-Path -LiteralPath $documentationFile -PathType Leaf)) {
        continue
    }

    $content = Get-Content -LiteralPath $documentationFile -Raw
    $secretPattern = '(?is)(Data\s+Source|Server)\s*=.+?(User\s+Id|UserId|Uid)\s*=.+?(Password|Pwd)\s*=\s*(?!\s*(?:CHANGE_ME|REPLACE_ME|\$\{|<|__|PENDIENTE|$))'
    if ($content -match $secretPattern) {
        $errors.Add("Posible credencial Oracle codificada en: $documentationFile")
    }
}

if ($errors.Count -gt 0) {
    Write-Host "Validación del expediente Oracle Fase 9: FALLO ($($errors.Count) hallazgos)." -ForegroundColor Red
    foreach ($item in $errors) {
        Write-Host "- $item" -ForegroundColor Red
    }
    exit 1
}

Write-Host 'Validación del expediente Oracle Fase 9: CORRECTA.' -ForegroundColor Green
Write-Host 'Preflight de solo lectura, expediente y autorización separada permanecen preparados y no ejecutados.' -ForegroundColor Green
exit 0
