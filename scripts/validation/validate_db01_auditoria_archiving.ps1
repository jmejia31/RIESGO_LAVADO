$ErrorActionPreference = 'Stop'

$repositoryRoot = Resolve-Path (Join-Path $PSScriptRoot '../..')
$archiveRoot = Join-Path $repositoryRoot 'database/auditoria/archivado'
$readmePath = Join-Path $archiveRoot 'README.md'
$diagnosticPath = Join-Path $archiveRoot '01_db01_diagnostico_rl_auditoria_solo_lectura.sql'
$dossierPath = Join-Path $repositoryRoot 'docs/4. Base de Datos/DB_01_POLITICA_ARCHIVADO_RL_AUDITORIA_2026-08-10.md'
$ddlPath = Join-Path $repositoryRoot 'database/01_create_tables.sql'
$repositoryPath = Join-Path $repositoryRoot 'backend/RL.API/Features/Auditoria/Persistence/AuditoriaRepository.cs'
$workflowPath = Join-Path $repositoryRoot '.github/workflows/quality-gates.yml'
$errors = [System.Collections.Generic.List[string]]::new()

function Assert-FileExists {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        $errors.Add("DB-01: archivo obligatorio inexistente: $Path")
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

foreach ($path in @($readmePath, $diagnosticPath, $dossierPath, $ddlPath, $repositoryPath, $workflowPath)) {
    Assert-FileExists $path
}

Assert-ContainsTokens $dossierPath @(
    '# DB-01 — Política de archivado de `RL_AUDITORIA`',
    '**Retención institucional aprobada:** **NO DEFINIDA**',
    '**Borrado automático:** **PROHIBIDO**',
    '**NO DELETE AUTOMÁTICO**',
    '`COPY_ONLY`',
    '`LEGAL_HOLD`',
    'Mientras el plazo de retención y la fecha de corte no sean aprobados por Cumplimiento/Legal, ningún registro se considera elegible para purga.',
    'la fuente se conserva',
    'DB-01 no crea DDL',
    'No se presupone que Oracle Partitioning esté licenciado/disponible',
    'no se haya movido ni eliminado un solo registro'
) 'Expediente DB-01'

Assert-ContainsTokens $readmePath @(
    'Retención institucional aprobada: **NO DEFINIDA**',
    'Borrado automático: **PROHIBIDO**',
    '`COPY_ONLY`',
    '`LEGAL_HOLD`',
    'DB-01 no autoriza eliminación automática ni manual de la fuente.'
) 'README DB-01'

Assert-ContainsTokens $diagnosticPath @(
    "SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA')",
    'RL_AUDITORIA',
    'MIN(AUD_FECHA)',
    'MAX(AUD_FECHA)',
    "TRUNC(AUD_FECHA, 'MM')",
    'AUD_ACCION',
    'AUD_MODULO',
    'AUD_TABLA',
    'DBMS_LOB.GETLENGTH(AUD_DATOS_ANT)',
    'DBMS_LOB.GETLENGTH(AUD_DATOS_NVO)'
) 'Diagnóstico DB-01'

if (Test-Path -LiteralPath $diagnosticPath -PathType Leaf) {
    $sql = Get-Content -LiteralPath $diagnosticPath -Raw
    $withoutComments = ($sql -split "`r?`n" | Where-Object { $_ -notmatch '^\s*--' -and $_ -notmatch '^\s*PROMPT(?:\s|$)' }) -join "`n"

    if ($withoutComments -match '(?im)^\s*(?:INSERT|UPDATE|DELETE|MERGE|CREATE|ALTER|DROP|TRUNCATE|COMMIT|BEGIN|DECLARE|EXEC(?:UTE)?)\b') {
        $errors.Add('DB-01: el diagnóstico dejó de ser exclusivamente SELECT/solo lectura.')
    }
    if ($withoutComments -match '(?i)DBMS_SCHEDULER|DBMS_JOB') {
        $errors.Add('DB-01: el diagnóstico no puede crear ni invocar automatización Oracle.')
    }

    # Se permite únicamente calcular longitud agregada de los CLOB; nunca proyectar su contenido.
    if ($withoutComments -match '(?im)^\s*SELECT\s+(?:AUD_USR_EMAIL|AUD_IP|AUD_DATOS_ANT|AUD_DATOS_NVO)\b' -or
        $withoutComments -match '(?im),\s*(?:AUD_USR_EMAIL|AUD_IP|AUD_DATOS_ANT|AUD_DATOS_NVO)\b\s*(?:,|FROM|AS)') {
        $errors.Add('DB-01: el diagnóstico agregado no debe proyectar correo, IP ni contenido CLOB.')
    }
}

Assert-ContainsTokens $ddlPath @(
    'CREATE TABLE RL_AUDITORIA',
    'AUD_ID          NUMBER(20)',
    'AUD_DATOS_ANT   CLOB',
    'AUD_DATOS_NVO   CLOB',
    'AUD_FECHA       DATE',
    'SEQ_RL_AUDITORIA',
    'IDX_RL_AUD_TABLA',
    'IDX_RL_AUD_USR'
) 'Contrato físico RL_AUDITORIA'

Assert-ContainsTokens $repositoryPath @(
    'INSERT INTO RL_AUDITORIA',
    'SEQ_RL_AUDITORIA.NEXTVAL',
    'SELECT COUNT(*) FROM RL_AUDITORIA',
    'ORDER BY AUD_FECHA DESC, AUD_ID DESC'
) 'Contrato Backend Auditoría'

Assert-ContainsTokens $workflowPath @(
    './scripts/validation/validate_db01_auditoria_archiving.ps1'
) 'Quality Gate DB-01'

$packageSqlFiles = Get-ChildItem -LiteralPath $archiveRoot -Filter '*.sql' -File -ErrorAction SilentlyContinue
foreach ($file in $packageSqlFiles) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    $withoutComments = ($content -split "`r?`n" | Where-Object { $_ -notmatch '^\s*--' -and $_ -notmatch '^\s*PROMPT(?:\s|$)' }) -join "`n"
    if ($withoutComments -match '(?im)^\s*(?:INSERT|UPDATE|DELETE|MERGE|CREATE|ALTER|DROP|TRUNCATE|COMMIT|BEGIN|DECLARE|EXEC(?:UTE)?)\b') {
        $errors.Add("DB-01: SQL no permitido en paquete de política: $($file.Name)")
    }
    if ($withoutComments -match '(?i)DBMS_SCHEDULER|DBMS_JOB') {
        $errors.Add("DB-01: automatización Oracle no permitida en paquete: $($file.Name)")
    }
    if ($content -match '(?i)05_reconstruir|06_reconstruir|B10_') {
        $errors.Add("DB-01: el paquete no debe alcanzar transición ni respaldos B10: $($file.Name)")
    }
}

$secretPattern = '(?is)(Data\s+Source|Server)\s*=.+?(User\s+Id|UserId|Uid)\s*=.+?(Password|Pwd)\s*=\s*(?!\s*(?:CHANGE_ME|REPLACE_ME|\$\{|<|__|PENDIENTE|$))'
foreach ($path in @($readmePath, $diagnosticPath, $dossierPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { continue }
    $content = Get-Content -LiteralPath $path -Raw
    if ($content -match $secretPattern) {
        $errors.Add("DB-01: posible credencial/cadena de conexión versionada: $path")
    }
}

if ($errors.Count -gt 0) {
    Write-Host "VALIDACION DB-01: FALLO ($($errors.Count) hallazgos)." -ForegroundColor Red
    foreach ($item in $errors) { Write-Host "- $item" -ForegroundColor Red }
    exit 1
}

Write-Host 'VALIDACION DB-01: CORRECTA.' -ForegroundColor Green
Write-Host 'Política COPY_ONLY, sin borrado automático, DDL/DML físico ni scheduler.' -ForegroundColor Green
Write-Host 'Retención permanece NO DEFINIDA hasta aprobación institucional.' -ForegroundColor Yellow
exit 0
