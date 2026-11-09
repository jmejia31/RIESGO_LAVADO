param(
    [string]$RepositoryRoot
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    Resolve-Path (Join-Path $PSScriptRoot '../..')
}
else {
    Resolve-Path -LiteralPath $RepositoryRoot
}
$databaseRoot = Join-Path $repositoryRoot 'database'
$matricesRoot = Join-Path $databaseRoot '19_matrices_riesgos'
$script06 = Join-Path $matricesRoot 'transicion/06_reconstruir_modelo_17_tablas.sql'
$calculationDdl = Join-Path $matricesRoot 'transicion/15_ddl_configuracion_calculo_312.sql'
$moduleEntrypoint = Join-Path $matricesRoot '00_APLICAR_MODULO_MATRICES_RIESGOS.sql'
$firstInstall = Join-Path $databaseRoot '00_EJECUCION_PRIMERA_VEZ.sql'
$safeUpdate = Join-Path $databaseRoot '00_EJECUCION_ACTUALIZACIONES_SEGURAS.sql'
$databaseValidator = Join-Path $repositoryRoot 'tools/validate_database_scripts.ps1'
$dynamicValidator = Join-Path $repositoryRoot 'scripts/validation/validate_matrices_dynamic_ddl_alignment.ps1'
$inventoryValidator = Join-Path $repositoryRoot 'scripts/validation/validate_matrices_17_object_inventory.ps1'
$inventoryTests = Join-Path $repositoryRoot 'scripts/validation/test_matrices_17_object_inventory.ps1'
$integrationTest = Join-Path $repositoryRoot 'backend/RL.API.Tests/Features/MatricesRiesgos/MatricesRiesgosRepositoryIntegrationTests.cs'
$oracleProfileHelper = Join-Path $repositoryRoot 'backend/RL.API.Tests/Support/OracleTestDatabaseProfile.cs'
$contractTest = Join-Path $repositoryRoot 'backend/RL.API.Tests/Features/MatricesRiesgos/MatricesRiesgosOracleCertificationContractTests.cs'
$deploymentPreflight = Join-Path $repositoryRoot 'deployment/matrices-riesgos/oracle/01_preflight_release.sql'
$qualityWorkflow = Join-Path $repositoryRoot '.github/workflows/quality-gates.yml'
$legacyStructure = Join-Path $matricesRoot 'instalacion/01_create_rl_mr_estructura_dinamica.sql'
$legacyConstraints = Join-Path $matricesRoot 'instalacion/02_create_rl_mr_restricciones_indices.sql'
$errors = [System.Collections.Generic.List[string]]::new()

function Assert-FileExists {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        $errors.Add("Archivo obligatorio inexistente: $Path")
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

foreach ($requiredFile in @(
    $script06,
    $calculationDdl,
    $moduleEntrypoint,
    $firstInstall,
    $safeUpdate,
    $databaseValidator,
    $dynamicValidator,
    $inventoryValidator,
    $inventoryTests,
    $integrationTest,
    $oracleProfileHelper,
    $contractTest,
    $deploymentPreflight,
    $qualityWorkflow)) {
    Assert-FileExists $requiredFile
}

foreach ($legacyFile in @($legacyStructure, $legacyConstraints)) {
    if (Test-Path -LiteralPath $legacyFile -PathType Leaf) {
        $errors.Add("Permanece un instalador heredado de 34 tablas: $legacyFile")
    }
}

Assert-ContainsTokens $script06 @(
    'Uso: SOLO manual, con respaldo validado y autorizacion expresa.',
    'No esta incluido en 00_APLICAR_MODULO_MATRICES_RIESGOS.sql.',
    'WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK',
    "DEFINE autorizacion = '&1'",
    "SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA')",
    "UPPER(v_schema) <> 'RIESGO_LAVADO'",
    "UPPER(v_auth) <> 'EJECUTAR'",
    "TABLE_NAME = 'RL_USUARIOS'",
    'DROP TABLE ',
    'CREATE SEQUENCE SEQ_RL_MR_FAMILIAS',
    'CREATE SEQUENCE SEQ_RL_MR_AUTOMONITOREO',
    'CREATE TABLE RL_MR_FAMILIAS_FORMULARIO',
    'CREATE TABLE RL_MR_AUTOMONITOREO',
    'EVA_DATOS_JSON',
    'EVA_CALCULOS_JSON',
    'RL_MR_EVIDENCIAS_VINCULOS') 'Script de transición 06'

if (Test-Path -LiteralPath $script06 -PathType Leaf) {
    $script06Content = Get-Content -LiteralPath $script06 -Raw
    $createdTables = [regex]::Matches($script06Content, '(?im)^\s*CREATE\s+TABLE\s+(RL_MR_[A-Z0-9_]+)\b') |
        ForEach-Object { $_.Groups[1].Value } |
        Sort-Object -Unique
    $createdSequences = [regex]::Matches($script06Content, '(?im)^\s*CREATE\s+SEQUENCE\s+(SEQ_RL_MR_[A-Z0-9_]+)\b') |
        ForEach-Object { $_.Groups[1].Value } |
        Sort-Object -Unique

    if ($createdTables.Count -ne 17) {
        $errors.Add("El script 06 crea $($createdTables.Count) tablas únicas; se esperaban 17.")
    }
    if ($createdSequences.Count -ne 17) {
        $errors.Add("El script 06 crea $($createdSequences.Count) secuencias únicas; se esperaban 17.")
    }

    foreach ($forbiddenObject in @(
        'RL_MR_CAMPOS_FORMULARIO',
        'RL_MR_APROBACIONES_FORMULARIO',
        'RL_MR_PERMISOS_FORMULARIO',
        'RL_MR_RELACIONES_RIESGO',
        'RL_MR_REVISIONES_EVALUACION',
        'RL_MR_TRAZAS_CALCULO',
        'RL_MR_AUDITORIA',
        'RL_MR_EVI_RIESGO',
        'RL_MR_EVI_EVALUACION',
        'RL_MR_EVI_CONTROL',
        'RL_MR_EVI_PLAN',
        'RL_MR_EVI_ACTIVIDAD',
        'RL_MR_EVI_ALERTA',
        'RL_MR_EVI_AUTOMONITOREO',
        'RL_MR_EVI_REVISION',
        'RL_MR_EVI_APROBACION')) {
        if ($createdTables -contains $forbiddenObject) {
            $errors.Add("El script 06 vuelve a crear la tabla heredada: $forbiddenObject")
        }
    }
}

Assert-ContainsTokens $moduleEntrypoint @(
    'Estado: BLOQUEADO DURANTE PREPARACION Y CERTIFICACION ORACLE.',
    'WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK',
    "SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA')",
    "UPPER(v_esquema_actual) <> 'RIESGO_LAVADO'",
    'EJECUCION BLOQUEADA',
    'cuarentena pre-Oracle') 'Punto de entrada de Matrices'

if (Test-Path -LiteralPath $moduleEntrypoint -PathType Leaf) {
    $entrypointContent = Get-Content -LiteralPath $moduleEntrypoint -Raw
    if ($entrypointContent -match '(?im)^\s*@@') {
        $errors.Add('El punto de entrada bloqueado de Matrices contiene includes SQL.')
    }
    if ($entrypointContent -match '(?im)^\s*(?:CREATE|ALTER|DROP|TRUNCATE|INSERT|UPDATE|MERGE|DELETE|COMMIT)\b') {
        $errors.Add('El punto de entrada bloqueado de Matrices contiene DDL o DML ejecutable.')
    }
}

foreach ($master in @($firstInstall, $safeUpdate)) {
    if (-not (Test-Path -LiteralPath $master -PathType Leaf)) {
        continue
    }

    $masterContent = Get-Content -LiteralPath $master -Raw
    if ($masterContent.Contains('19_matrices_riesgos/00_APLICAR_MODULO_MATRICES_RIESGOS.sql')) {
        $errors.Add("El paquete Matrices sigue integrado al maestro automático: $master")
    }
    if ($masterContent.Contains('06_reconstruir_modelo_17_tablas.sql')) {
        $errors.Add("El script 06 sigue integrado al maestro automático: $master")
    }
}

$allSqlFiles = Get-ChildItem -LiteralPath $databaseRoot -Recurse -File -Filter '*.sql'
foreach ($sqlFile in $allSqlFiles) {
    if ($sqlFile.FullName -eq $script06) {
        continue
    }

    $content = Get-Content -LiteralPath $sqlFile.FullName -Raw
    if ($content -match '(?im)^\s*@@[^\r\n]*06_reconstruir_modelo_17_tablas\.sql(?:\s|$)') {
        $errors.Add("El script 06 fue incorporado mediante include: $($sqlFile.FullName)")
    }
}

Assert-ContainsTokens $integrationTest @(
    'RL_ORACLE_INTEGRATION_REQUIRED',
    '.AddEnvironmentVariables()',
    '.AddUserSecrets<MatricesRiesgosRepositoryIntegrationTests>(optional: true)',
    'OracleTestDatabaseProfile.TryResolveConfiguredLocalXe(configuration, out string? localXe)',
    'if (!_integrationRequired)',
    'Este resultado no certifica físicamente el modelo de 17 tablas.',
    'string.Equals(esquema, "RIESGO_LAVADO"',
    'ValidarContratoFisicoAsync(conn)',
    'TablasModelo17',
    'SecuenciasModelo17',
    'TablasRetiradas',
    'SecuenciasRetiradas') 'Suite Oracle'

if (Test-Path -LiteralPath $integrationTest -PathType Leaf) {
    Assert-ContainsTokens $oracleProfileHelper @(
        'DatabaseProfileResolver.ProfileConfigurationKey',
        'DatabaseProfileResolver.LocalXeProfileName',
        'configuration.GetConnectionString("OracleDB_LocalXE")',
        'DatabaseProfileResolver.Resolve(selectedLocalXe)',
        'ConnectionStrings:OracleDB_LocalXE'
    ) 'Resolver del perfil Oracle de pruebas'

    $integrationContent = Get-Content -LiteralPath $integrationTest -Raw
    if ($integrationContent -match '(?i)GetConnectionString\(\s*"OracleDB"\s*\)' -or
        $integrationContent -match '(?i)ConnectionStrings:OracleDB"') {
        $errors.Add('La suite Oracle contiene un fallback generico OracleDB; debe usar un perfil LOCAL_XE explicito.')
    }
    foreach ($forbiddenSql in @(
        'CREATE TABLE ',
        'CREATE SEQUENCE ',
        'ALTER TABLE ',
        'DROP TABLE ',
        'DROP SEQUENCE ',
        'TRUNCATE TABLE ')) {
        if ($integrationContent.Contains($forbiddenSql)) {
            $errors.Add("La suite Oracle contiene DDL prohibido: $forbiddenSql")
        }
    }

    $connectionLiteralPattern = '(?is)(Data\s+Source|Server)\s*=.+?(User\s+Id|UserId|Uid)\s*=.+?(Password|Pwd)\s*='
    if ($integrationContent -match $connectionLiteralPattern) {
        $errors.Add('La suite Oracle contiene una cadena de conexión codificada.')
    }
}

if (Test-Path -LiteralPath $oracleProfileHelper -PathType Leaf) {
    $oracleProfileContent = Get-Content -LiteralPath $oracleProfileHelper -Raw
    if ($oracleProfileContent -match '(?i)GetConnectionString\(\s*"OracleDB"\s*\)' -or
        $oracleProfileContent -match '(?i)ConnectionStrings:OracleDB"') {
        $errors.Add('El helper Oracle contiene un fallback generico OracleDB.')
    }
}

$calculationTables = @(
    'RL_MR_FORMULAS',
    'RL_MR_FORMULA_USOS',
    'RL_MR_FORMULA_VERSIONES',
    'RL_MR_FUNCION_ARGUMENTOS',
    'RL_MR_FUNCIONES',
    'RL_MR_FUNCION_VERSIONES',
    'RL_MR_PARAMETROS_CALCULO',
    'RL_MR_PARAMETRO_VERSIONES'
)

$calculationSequences = @(
    'SEQ_RL_MR_FORMULAS',
    'SEQ_RL_MR_FORMULA_USOS',
    'SEQ_RL_MR_FORMULA_VERSIONES',
    'SEQ_RL_MR_FUNCION_ARGUMENTOS',
    'SEQ_RL_MR_FUNCIONES',
    'SEQ_RL_MR_FUNCION_VERSIONES',
    'SEQ_RL_MR_PARAMETROS',
    'SEQ_RL_MR_PARAMETRO_VERSIONES'
)

if (Test-Path -LiteralPath $calculationDdl -PathType Leaf) {
    $calculationContent = Get-Content -LiteralPath $calculationDdl -Raw
    $deploymentContent = Get-Content -LiteralPath $deploymentPreflight -Raw

    foreach ($table in $calculationTables) {
        if ($calculationContent -notmatch "(?im)create_table_if_absent\('$([regex]::Escape($table))'") {
            $errors.Add("El DDL de configuracion de calculo no declara la tabla: $table")
        }
        if ($deploymentContent -notmatch [regex]::Escape($table)) {
            $errors.Add("El contrato de deployment no enumera la tabla de calculo: $table")
        }
    }

    foreach ($sequence in $calculationSequences) {
        if ($calculationContent -notmatch "(?im)create_sequence_if_absent\('$([regex]::Escape($sequence))'") {
            $errors.Add("El DDL de configuracion de calculo no declara la secuencia: $sequence")
        }
    }

    if ($calculationTables.Count -ne 8 -or $calculationSequences.Count -ne 8) {
        $errors.Add('El inventario estatico de configuracion de calculo no contiene exactamente 8 tablas y 8 secuencias.')
    }
}

Assert-ContainsTokens $contractTest @(
    'InventarioOracle_DeclaraExactamenteDiecisieteTablasYSecuencias',
    'InventarioOracle_NoMezclaObjetosActivosYRetirados',
    'SuiteOracle_ConservaEscenariosFisicoCommitRollbackYAuditoria',
    'SuiteOracle_DeclaraIndicesYRestriccionesPrincipalesSinDuplicados') 'Pruebas de contrato Oracle no conectadas'

Assert-ContainsTokens $qualityWorkflow @(
    './tools/validate_database_scripts.ps1',
    './scripts/validation/validate_matrices_preoracle_readiness.ps1',
    './scripts/validation/validate_matrices_dynamic_ddl_alignment.ps1',
    './scripts/validation/validate_matrices_17_object_inventory.ps1',
    './scripts/validation/test_matrices_17_object_inventory.ps1') 'Quality Gate'

$securityRoots = @(
    (Join-Path $repositoryRoot 'backend'),
    (Join-Path $repositoryRoot 'scripts'),
    (Join-Path $repositoryRoot '.github'),
    (Join-Path $repositoryRoot 'database'))
$securityExtensions = @('.cs', '.json', '.config', '.xml', '.runsettings', '.ps1', '.yml', '.yaml', '.env', '.txt', '.sql')
$excludedSegments = @('bin', 'obj', 'node_modules', 'dist', 'coverage', '.git', 'Historico', 'retiro_controlado')
$secretPattern = '(?im)(Data\s+Source|Server)\s*=[^\r\n]*(User\s+Id|UserId|Uid)\s*=[^\r\n]*(Password|Pwd)\s*=\s*(?!\s*(?:CHANGE_ME|REPLACE_ME|\$\{|<|__|$))'

foreach ($root in $securityRoots) {
    if (-not (Test-Path -LiteralPath $root)) {
        continue
    }

    foreach ($file in Get-ChildItem -LiteralPath $root -Recurse -File) {
        if ($securityExtensions -notcontains $file.Extension.ToLowerInvariant()) {
            continue
        }
        if (($file.FullName -split '[\\/]') | Where-Object { $excludedSegments -contains $_ }) {
            continue
        }
        if ($file.Name -match '(?i)\.example$|example\.|sample\.') {
            continue
        }

        $content = Get-Content -LiteralPath $file.FullName -Raw
        if ($content -match $secretPattern) {
            $errors.Add("Posible credencial Oracle codificada: $($file.FullName)")
        }
    }
}

foreach ($temporaryWorkflow in @(
    '.github/workflows/phase7-fix-validator.yml',
    '.github/workflows/phase7-normalize-validator-exit.yml')) {
    $temporaryPath = Join-Path $repositoryRoot $temporaryWorkflow
    if (Test-Path -LiteralPath $temporaryPath -PathType Leaf) {
        $errors.Add("Permanece un workflow auxiliar temporal: $temporaryWorkflow")
    }
}

if ($errors.Count -gt 0) {
    Write-Host "Preparación pre-Oracle de Matrices: FALLO ($($errors.Count) hallazgos)." -ForegroundColor Red
    foreach ($item in $errors) {
        Write-Host "- $item" -ForegroundColor Red
    }
    exit 1
}

Write-Host 'Preparación pre-Oracle de Matrices: CORRECTA.' -ForegroundColor Green
Write-Host 'Modelo objetivo: 17 tablas y 17 secuencias; script 06 manual y aislado; suite Oracle bloqueada por entorno.' -ForegroundColor Green
exit 0
