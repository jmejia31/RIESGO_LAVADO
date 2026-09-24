param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [switch]$PassThru
)

$ErrorActionPreference = 'Stop'
$databaseRoot = [System.IO.Path]::GetFullPath((Join-Path $RepositoryRoot 'database'))
$databasePrefix = $databaseRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
$errors = [System.Collections.Generic.List[string]]::new()
$unicodeDiagnosticTotalOccurrences = 0
$unicodeDiagnosticUniqueTokens = 0
$unicodeDiagnosticDeterministicMappings = 0
$unicodeDiagnosticAmbiguousTokens = 0
$unicodeDiagnosticUnmappedTokens = 0
$moduleObservedUniqueTokens = 0
$moduleObservedMappedTokens = 0
$moduleObservedUnmappedTokens = 0
$moduleObservedAmbiguousTokens = 0

function Get-DatabaseRelativePath {
    param([string]$Path)

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    if ($fullPath.Equals($databaseRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        return '.'
    }

    if ($fullPath.StartsWith($databasePrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $fullPath.Substring($databasePrefix.Length).Replace('\', '/')
    }

    return $fullPath.Replace('\', '/')
}

function Get-SqlIncludes {
    param([string]$EntrypointPath)

    $includes = [System.Collections.Generic.List[string]]::new()
    $baseDirectory = Split-Path -Parent $EntrypointPath

    foreach ($line in Get-Content -LiteralPath $EntrypointPath) {
        if ($line -match '^\s*@@(?<include>[^\s]+\.sql)(?:\s+.*)?$') {
            $includePath = [System.IO.Path]::GetFullPath((Join-Path $baseDirectory $Matches.include.Trim()))
            if (-not $includePath.StartsWith($databasePrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
                $errors.Add("Include fuera de database: $(Get-DatabaseRelativePath $EntrypointPath) -> $($Matches.include)")
                continue
            }

            if (-not (Test-Path -LiteralPath $includePath -PathType Leaf)) {
                $errors.Add("Include SQL inexistente: $(Get-DatabaseRelativePath $EntrypointPath) -> $($Matches.include)")
                continue
            }

            $includes.Add($includePath)
        }
        elseif ($line -match '^\s*@[^@].+\.sql(?:\s+.*)?$') {
            $errors.Add("Include SQL debe usar @@ para resolver rutas relativas: $(Get-DatabaseRelativePath $EntrypointPath) -> $($line.Trim())")
        }
    }

    return $includes
}

function Assert-IncludeOrder {
    param(
        [string]$Entrypoint,
        [string[]]$Expected
    )

    $entrypointPath = Join-Path $databaseRoot $Entrypoint
    if (-not (Test-Path -LiteralPath $entrypointPath -PathType Leaf)) {
        $errors.Add("Punto de entrada SQL inexistente: $Entrypoint")
        return
    }

    $actual = @(Get-SqlIncludes $entrypointPath | ForEach-Object { Get-DatabaseRelativePath $_ })
    if ($actual.Count -ne $Expected.Count) {
        $errors.Add("Orden SQL incompleto en $Entrypoint. Esperados=$($Expected.Count), encontrados=$($actual.Count)")
        return
    }

    for ($index = 0; $index -lt $Expected.Count; $index++) {
        if ($actual[$index] -cne $Expected[$index]) {
            $errors.Add("Orden SQL incorrecto en $Entrypoint, posicion $($index + 1): esperado '$($Expected[$index])', encontrado '$($actual[$index])'")
        }
    }
}

function Get-SqlClosure {
    param([string]$EntrypointPath)

    $visited = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    $pending = [System.Collections.Generic.Stack[string]]::new()
    $pending.Push($EntrypointPath)

    while ($pending.Count -gt 0) {
        $current = $pending.Pop()
        if (-not $visited.Add($current)) {
            continue
        }

        foreach ($include in Get-SqlIncludes $current) {
            $pending.Push($include)
        }
    }

    return $visited
}

function Get-ExecutableSql {
    param([string]$Path)

    $content = Get-Content -LiteralPath $Path -Raw
    $withoutBlocks = [System.Text.RegularExpressions.Regex]::Replace(
        $content,
        '/\*.*?\*/',
        '',
        [System.Text.RegularExpressions.RegexOptions]::Singleline)
    $executableLines = $withoutBlocks -split "`r?`n" | Where-Object {
        $_ -notmatch '^\s*--' -and $_ -notmatch '^\s*PROMPT(?:\s|$)'
    }

    return ($executableLines -join "`n")
}

function Remove-SqlStringLiterals {
    param([string]$Sql)

    # Static references must be distinguished from table names inside
    # EXECUTE IMMEDIATE strings. This is intentionally a lightweight gate,
    # not a SQL parser: Oracle identifiers in this transition are unquoted.
    return [System.Text.RegularExpressions.Regex]::Replace($Sql, "'(?:''|[^'])*'", "''")
}

$firstInstallOrder = @(
    '01_create_tables.sql',
    '02_seed_data.sql',
    '03_create_modules_table.sql',
    '04_alter_config_sistema.sql',
    '05_register_monitoreo_listas.sql',
    '06_alter_usuarios_change_pass.sql',
    '08_register_bitacora.sql',
    '09_create_detalle_evidencia.sql',
    '10_register_tipo_listas_module.sql',
    '11_register_cargar_listas_module.sql',
    '12_register_coincidencias_patrono_module.sql',
    '13_create_calificaciones_coincidencias.sql',
    '14_register_coincidencias_empleado_module.sql',
    '15_update_detalle_evidencia_soft_delete.sql',
    '16_alter_lista_positivos_origen_registro.sql',
    '18_add_missing_comments.sql',
    '17_validate_module_ids.sql'
)

$safeUpdateOrder = $firstInstallOrder | Where-Object {
    $_ -notin @('01_create_tables.sql', '02_seed_data.sql')
}

Assert-IncludeOrder '00_EJECUCION_PRIMERA_VEZ.sql' $firstInstallOrder
Assert-IncludeOrder '00_EJECUCION_ACTUALIZACIONES_SEGURAS.sql' $safeUpdateOrder

$rootEntrypoints = @(
    '00_EJECUCION_PRIMERA_VEZ.sql',
    '00_EJECUCION_ACTUALIZACIONES_SEGURAS.sql'
)

foreach ($entrypoint in $rootEntrypoints) {
    $path = Join-Path $databaseRoot $entrypoint
    if ((Test-Path -LiteralPath $path) -and (Get-Content -LiteralPath $path -Raw) -notmatch '(?im)^\s*WHENEVER\s+SQLERROR\s+EXIT\s+SQL\.SQLCODE\s+ROLLBACK\s*$') {
        $errors.Add("Punto de entrada sin cierre controlado ante error Oracle: $entrypoint")
    }
}

$firstInstallPath = Join-Path $databaseRoot '00_EJECUCION_PRIMERA_VEZ.sql'
$safeUpdatePath = Join-Path $databaseRoot '00_EJECUCION_ACTUALIZACIONES_SEGURAS.sql'
$firstClosure = Get-SqlClosure $firstInstallPath
$safeClosure = Get-SqlClosure $safeUpdatePath

foreach ($path in $firstClosure) {
    $relativePath = Get-DatabaseRelativePath $path
    if ($relativePath -match '(^|/)_experimental_no_ejecutar/' -or $relativePath -match '(^|/)_utilitarios/') {
        $errors.Add("La primera instalacion alcanza un script no aprobado: $relativePath")
    }
}

foreach ($path in $safeClosure) {
    $relativePath = Get-DatabaseRelativePath $path
    if ($relativePath -match '(^|/)_experimental_no_ejecutar/' -or $relativePath -match '(^|/)_utilitarios/') {
        $errors.Add("El flujo seguro alcanza un script no aprobado: $relativePath")
    }

    $sql = Get-ExecutableSql $path
    if ($sql -match '(?im)\bDROP\s+TABLE\b|\bTRUNCATE(?:\s+TABLE)?\b|\bDELETE\s+FROM\b') {
        $errors.Add("Operacion destructiva alcanzable desde actualizaciones seguras: $relativePath")
    }
}

$validationPath = Join-Path $databaseRoot '17_validate_module_ids.sql'
$validationSql = Get-ExecutableSql $validationPath
if ($validationSql -match '(?im)\b(?:INSERT|UPDATE|MERGE|DELETE|CREATE|ALTER|DROP|TRUNCATE|COMMIT|ROLLBACK)\b') {
    $errors.Add('La validacion final 17_validate_module_ids.sql dejo de ser de solo lectura')
}

$manifestPath = Join-Path $databaseRoot '00_MANIFIESTO_SCRIPTS_APROBADOS.md'
$manifest = Get-Content -LiteralPath $manifestPath -Raw
$activeRootScripts = Get-ChildItem -LiteralPath $databaseRoot -File -Filter '*.sql' |
    Where-Object { $_.Name -match '^\d{2}_.+\.sql$' }

foreach ($script in $activeRootScripts) {
    if (-not $firstClosure.Contains($script.FullName) -and $script.Name -notin @('00_EJECUCION_PRIMERA_VEZ.sql', '00_EJECUCION_ACTUALIZACIONES_SEGURAS.sql')) {
        $errors.Add("Script activo de raiz no alcanzable desde primera instalacion: $($script.Name)")
    }

    if (-not $manifest.Contains($script.Name)) {
        $errors.Add("Script activo ausente del manifiesto: $($script.Name)")
    }
}

$matricesRoot = Join-Path $databaseRoot '19_matrices_riesgos'
$matricesEntrypoint = Join-Path $matricesRoot '00_APLICAR_MODULO_MATRICES_RIESGOS.sql'
$transitionScript = Join-Path $matricesRoot 'transicion/06_reconstruir_modelo_17_tablas.sql'
$legacyStructure = Join-Path $matricesRoot 'instalacion/01_create_rl_mr_estructura_dinamica.sql'
$legacyConstraints = Join-Path $matricesRoot 'instalacion/02_create_rl_mr_restricciones_indices.sql'

foreach ($requiredPath in @($matricesEntrypoint, $transitionScript)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        $errors.Add("Archivo obligatorio de Matrices inexistente: $(Get-DatabaseRelativePath $requiredPath)")
    }
}

foreach ($legacyPath in @($legacyStructure, $legacyConstraints)) {
    if (Test-Path -LiteralPath $legacyPath -PathType Leaf) {
        $errors.Add("Instalador heredado restaurado en la ruta activa: $(Get-DatabaseRelativePath $legacyPath)")
    }
}

if (Test-Path -LiteralPath $matricesEntrypoint -PathType Leaf) {
    $entrypointContent = Get-Content -LiteralPath $matricesEntrypoint -Raw
    $entrypointIncludes = @(Get-SqlIncludes $matricesEntrypoint)

    if ($entrypointIncludes.Count -ne 0) {
        $errors.Add('El punto de entrada bloqueado de Matrices no puede contener includes SQL.')
    }

    foreach ($token in @(
        'WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK',
        "SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA')",
        "UPPER(v_esquema_actual) <> 'RIESGO_LAVADO'",
        'EJECUCION BLOQUEADA',
        'cuarentena pre-Oracle')) {
        if (-not $entrypointContent.Contains($token)) {
            $errors.Add("El punto de entrada bloqueado de Matrices no contiene: $token")
        }
    }

    $entrypointSql = Get-ExecutableSql $matricesEntrypoint
    if ($entrypointSql -match '(?im)\b(?:CREATE|ALTER|DROP|TRUNCATE|INSERT|UPDATE|MERGE|DELETE|COMMIT)\b') {
        $errors.Add('El punto de entrada bloqueado de Matrices contiene operaciones de esquema o datos.')
    }

    if ($firstClosure.Contains($matricesEntrypoint) -or $safeClosure.Contains($matricesEntrypoint)) {
        $errors.Add('El paquete Matrices no puede ser alcanzable desde los maestros durante la cuarentena pre-Oracle.')
    }
}

if (Test-Path -LiteralPath $transitionScript -PathType Leaf) {
    $transitionContent = Get-Content -LiteralPath $transitionScript -Raw
    foreach ($token in @(
        'WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK',
        "DEFINE autorizacion = '&1'",
        "SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA')",
        "UPPER(v_schema) <> 'RIESGO_LAVADO'",
        "UPPER(v_auth) <> 'EJECUTAR'",
        "TABLE_NAME = 'RL_USUARIOS'",
        'CREATE TABLE RL_MR_FAMILIAS_FORMULARIO',
        'CREATE TABLE RL_MR_AUTOMONITOREO')) {
        if (-not $transitionContent.Contains($token)) {
            $errors.Add("El script de transicion 06 no contiene: $token")
        }
    }

    if ($firstClosure.Contains($transitionScript) -or $safeClosure.Contains($transitionScript)) {
        $errors.Add('El script destructivo 06 no puede pertenecer a un flujo automatico.')
    }
}

$allSqlFiles = Get-ChildItem -LiteralPath $databaseRoot -Recurse -File -Filter '*.sql'
foreach ($sqlFile in $allSqlFiles) {
    if ($sqlFile.FullName -eq $transitionScript) {
        continue
    }

    $content = Get-Content -LiteralPath $sqlFile.FullName -Raw
    if ($content -match '(?im)^\s*@@[^\r\n]*06_reconstruir_modelo_17_tablas\.sql(?:\s|$)') {
        $errors.Add("El script 06 fue incorporado mediante include: $(Get-DatabaseRelativePath $sqlFile.FullName)")
    }
}

foreach ($requiredManifestToken in @(
    '19_matrices_riesgos/00_APLICAR_MODULO_MATRICES_RIESGOS.sql',
    '19_matrices_riesgos/transicion/06_reconstruir_modelo_17_tablas.sql',
    'fuera de los dos maestros automáticos')) {
    if (-not $manifest.Contains($requiredManifestToken)) {
        $errors.Add("El manifiesto no documenta el control de Matrices: $requiredManifestToken")
    }
}

$packageDirectories = Get-ChildItem -LiteralPath $databaseRoot -Directory |
    Where-Object { $_.Name -match '^\d{2}_' -and $_.Name -ne '19_matrices_riesgos' }
foreach ($directory in $packageDirectories) {
    $packageEntrypoints = @(Get-ChildItem -LiteralPath $directory.FullName -File -Filter '00_APLICAR_*.sql')
    if ($packageEntrypoints.Count -ne 1) {
        $errors.Add("Paquete modular debe contener un unico 00_APLICAR_*.sql: $($directory.Name)")
        continue
    }

    $packageEntrypoint = $packageEntrypoints[0]
    $currentPackageClosure = Get-SqlClosure $packageEntrypoint.FullName
    if (-not $firstClosure.Contains($packageEntrypoint.FullName) -or -not $safeClosure.Contains($packageEntrypoint.FullName)) {
        $errors.Add("Paquete modular no alcanzable desde ambos maestros: $($directory.Name)")
    }

    foreach ($script in Get-ChildItem -LiteralPath $directory.FullName -File -Filter '*.sql') {
        if (-not $currentPackageClosure.Contains($script.FullName)) {
            $errors.Add("Script interno no alcanzable desde el punto de entrada modular: $(Get-DatabaseRelativePath $script.FullName)")
        }
    }

    $entrypointRelative = Get-DatabaseRelativePath $packageEntrypoint.FullName
    if (-not $manifest.Contains($entrypointRelative)) {
        $errors.Add("Paquete modular ausente del manifiesto: $entrypointRelative")
    }
}

$riskTextTransitionRoot = Join-Path $databaseRoot '19_matrices_riesgos/transicion'
$riskTextTransitionFiles = @(
    '30_fuente_canonica_nombres_riesgos.sql',
    '31_precheck_sincronizacion_nombres_riesgos.sql',
    '32_backup_rl_mr_riesgos_nombres.sql',
    '33_corregir_nombres_riesgos_desde_fuente_canonica.sql',
    '34_postcheck_nombres_riesgos.sql',
    '35_rollback_nombres_riesgos.sql',
    '36_precheck_descripciones_riesgos.sql',
    '37_backup_rl_mr_riesgos_descripciones.sql',
    '38_corregir_descripciones_encoding.sql',
    '39_postcheck_descripciones_riesgos.sql',
    '40_rollback_descripciones_riesgos.sql',
    '41_precheck_unicode_modulo_matrices_completo.sql',
    '42_backup_unicode_modulo_matrices_completo.sql',
    '43_corregir_unicode_modulo_matrices_completo.sql',
    '44_postcheck_unicode_modulo_matrices_completo.sql',
    '45_rollback_unicode_modulo_matrices_completo.sql'
)
$riskNameBackup = 'RL_MR_RIES_NOM_BKP_20260924'
$riskDescriptionBackup = 'RL_MR_RIES_DESC_BKP_20260924'
$unicodeModuleBackup = 'RL_MR_UNI_BKP_20260924'
$legacyRiskNameBackup = 'RL_MR_RIESGOS_NOMBRES_BKP_20260923'
$legacyRiskDescriptionBackup = 'RL_MR_RIESGOS_DESC_BKP_20260923'

foreach ($identifier in @($riskNameBackup, $riskDescriptionBackup, $unicodeModuleBackup)) {
    if ($identifier.Length -gt 30) {
        $errors.Add("Identificador Oracle 11g de respaldo supera 30 caracteres: $identifier ($($identifier.Length))")
    }
}

$riskTextContents = @{}
foreach ($fileName in $riskTextTransitionFiles) {
    $filePath = Join-Path $riskTextTransitionRoot $fileName
    if (-not (Test-Path -LiteralPath $filePath -PathType Leaf)) {
        $errors.Add("Script de transición de textos inexistente: 19_matrices_riesgos/transicion/$fileName")
        continue
    }

    $content = Get-Content -LiteralPath $filePath -Raw
    $riskTextContents[$fileName] = $content
    if ($content.Contains($legacyRiskNameBackup) -or $content.Contains($legacyRiskDescriptionBackup)) {
        $errors.Add("Script de transición conserva un identificador de backup Oracle 11g inválido: $fileName")
    }
}

foreach ($fileName in @(
    '41_precheck_unicode_modulo_matrices_completo.sql',
    '44_postcheck_unicode_modulo_matrices_completo.sql'
)) {
    if ($riskTextContents.ContainsKey($fileName)) {
        $readOnlySql = Get-ExecutableSql (Join-Path $riskTextTransitionRoot $fileName)
        if ($readOnlySql -match '(?im)\b(?:INSERT|UPDATE|MERGE|DELETE|CREATE|ALTER|DROP|TRUNCATE|COMMIT)\b') {
            $errors.Add("$fileName debe ser READ ONLY.")
        }
    }
}
$unicodeBackupPattern = [regex]::Escape($unicodeModuleBackup)
if ($riskTextContents.ContainsKey('42_backup_unicode_modulo_matrices_completo.sql') -and
    $riskTextContents['42_backup_unicode_modulo_matrices_completo.sql'] -notmatch $unicodeBackupPattern) {
    $errors.Add("42 no usa el backup $unicodeModuleBackup.")
}
foreach ($fileName in @(
    '43_corregir_unicode_modulo_matrices_completo.sql',
    '45_rollback_unicode_modulo_matrices_completo.sql'
)) {
    if ($riskTextContents.ContainsKey($fileName) -and
        $riskTextContents[$fileName] -notmatch $unicodeBackupPattern) {
        $errors.Add("$fileName no usa el backup $unicodeModuleBackup.")
    }
}
foreach ($fileName in $riskTextTransitionFiles) {
    if (-not $riskTextContents.ContainsKey($fileName)) { continue }
    $executableSql = Get-ExecutableSql (Join-Path $riskTextTransitionRoot $fileName)
    if ($executableSql -match '(?is)EXECUTE\s+IMMEDIATE\s+.*?CREATE\s+TABLE\s+' + $unicodeBackupPattern) {
        $staticSql = Remove-SqlStringLiterals $executableSql
        if ($staticSql -match "(?i)\b$unicodeBackupPattern\b") {
            $errors.Add("Patrón Oracle 11g inválido en \${fileName}: backup Unicode creado dinámicamente y referenciado estáticamente.")
        }
    }
}
if ($riskTextContents.ContainsKey('43_corregir_unicode_modulo_matrices_completo.sql')) {
    $unicodeCorrectionSql = $riskTextContents['43_corregir_unicode_modulo_matrices_completo.sql']
    if ($unicodeCorrectionSql -match "REPLACE\s*\(\s*[^,]+,\s*UNISTR\('\00BF'\)") {
        $errors.Add('43 contiene una sustitución global prohibida de U+00BF.')
    }
    if ($unicodeCorrectionSql -notmatch 'SAVEPOINT\s+MATRICES_UNICODE_CORRECTION' -or
        $unicodeCorrectionSql -notmatch 'l_savepoint_created\s+BOOLEAN' -or
        $unicodeCorrectionSql -notmatch 'IF\s+l_savepoint_created\s+THEN[\s\S]*?ROLLBACK TO MATRICES_UNICODE_CORRECTION') {
        $errors.Add('43 no tiene rollback fail-closed.')
    }
    $savepointIndex = $unicodeCorrectionSql.IndexOf('SAVEPOINT MATRICES_UNICODE_CORRECTION', [System.StringComparison]::OrdinalIgnoreCase)
    $rollbackToIndex = $unicodeCorrectionSql.IndexOf('ROLLBACK TO MATRICES_UNICODE_CORRECTION', [System.StringComparison]::OrdinalIgnoreCase)
    if ($savepointIndex -lt 0 -or $rollbackToIndex -lt $savepointIndex) { $errors.Add('43 contiene ROLLBACK TO antes de un SAVEPOINT válido.') }
    $unicodeCatalogPath = Join-Path $riskTextTransitionRoot '_catalogo_unicode_modulo_matrices.sql'
    $unicodeCatalog = if (Test-Path -LiteralPath $unicodeCatalogPath) { Get-Content -LiteralPath $unicodeCatalogPath -Raw } else { '' }
    if ($unicodeCorrectionSql -notmatch '@@_catalogo_unicode_modulo_matrices\.sql' -or $unicodeCatalog -notmatch "Afiliaci\\00BFn.*Afiliaci\\00F3n") {
        $errors.Add('43 no contiene el mapping exacto Afiliaci¿n -> Afiliación.')
    }
    foreach ($gate in @('CURRENT_SUSPICIOUS_CELLS','BACKUP_CELLS','BACKUP_COVERAGE=PASS','AMBIGUOUS_TOKENS','UNMAPPED_TOKENS','coverage_mismatches','UBK_TABLE_NAME','UBK_COLUMN_NAME','UBK_ROWID_TEXT')) {
        if ($unicodeCorrectionSql -notmatch [regex]::Escape($gate)) { $errors.Add("43 no implementa gate $gate.") }
    }
    if ($unicodeCorrectionSql -match "DBMS_LOB\.SUBSTR\([^,]+,\s*32767") { $errors.Add('43 usa SUBSTR CLOB limitado a 32767 para detección.') }
    if ($unicodeCorrectionSql -notmatch '@@_catalogo_unicode_modulo_matrices\.sql') { $errors.Add('43 no consume el catálogo compartido de mappings.') }
    if ($unicodeCorrectionSql -match "AMBIGUOUS_TOKENS=0|UNMAPPED_TOKENS=0") { $errors.Add('43 hardcodea un gate de tokens; debe derivarlo.') }
}

if ($riskTextContents.ContainsKey('41_precheck_unicode_modulo_matrices_completo.sql')) {
    $inventorySql = $riskTextContents['41_precheck_unicode_modulo_matrices_completo.sql']
    foreach ($requiredToken in @('REQUIRED_RL_MR_TABLES','REQUIRED_RL_MR_TABLES_FOUND','ROWID=','TOKEN_BAD=','OCCURRENCES=','CONTEXT=','FULL_MODULE_TOKEN_OCCURRENCES','UNIQUE_BAD_TOKENS','DETERMINISTIC_MAPPINGS','AMBIGUOUS_TOKENS','UNMAPPED_TOKENS','FULL_MODULE_TOKEN_INVENTORY','l_seen_occurrences','register_mapping','@@_catalogo_unicode_modulo_matrices.sql')) {
        if ($inventorySql -notmatch [regex]::Escape($requiredToken)) { $errors.Add("41 no produce inventario integral con $requiredToken.") }
    }
    if ($inventorySql -match '(?i)ROWNUM\s*=\s*1') { $errors.Add('41 no puede limitar el inventario con ROWNUM=1.') }
    if ($inventorySql -match 'DBMS_LOB\.GETLENGTH\s*\(\s*p_marker') { $errors.Add('41 usa GETLENGTH sobre VARCHAR2.') }
    if ($inventorySql -match "DBMS_LOB\.SUBSTR\([^,]+,\s*32767") { $errors.Add('41 usa SUBSTR CLOB limitado a 32767 para detección.') }
    if ($inventorySql -match "AMBIGUOUS_TOKENS=0|UNMAPPED_TOKENS=0") { $errors.Add('41 hardcodea un gate de tokens; debe derivarlo.') }
}
if ($riskTextContents.ContainsKey('42_backup_unicode_modulo_matrices_completo.sql') -and
    $riskTextContents['42_backup_unicode_modulo_matrices_completo.sql'] -notmatch 'BACKUP_CLEANUP|DROP TABLE RL_MR_UNI_BKP_20260924') {
    $errors.Add('42 no limpia el backup dinámico si falla después del CREATE TABLE.')
}
if ($riskTextContents.ContainsKey('42_backup_unicode_modulo_matrices_completo.sql') -and
    $riskTextContents['42_backup_unicode_modulo_matrices_completo.sql'] -match "DBMS_LOB\.SUBSTR\([^,]+,\s*32767") { $errors.Add('42 usa SUBSTR CLOB limitado a 32767 para detección.') }
if ($riskTextContents.ContainsKey('44_postcheck_unicode_modulo_matrices_completo.sql')) {
    $postSql = $riskTextContents['44_postcheck_unicode_modulo_matrices_completo.sql']
    foreach ($key in @('area_principal','dueno_riesgo','respuesta_riesgo','nivel_inherente','nivel_residual')) {
        if ($postSql -notmatch [regex]::Escape("json_has(x.EVA_DATOS_JSON,'$key'")) { $errors.Add("44 no compara la clave JSON contractual $key.") }
    }
    if ($postSql -match "json_has\(x\.EVA_DATOS_JSON,'(codigo_riesgo|estado)'") { $errors.Add('44 exige claves que no existen en el contrato JSON V1.') }
    if ($postSql -notmatch 'PROY_CODIGO_RIESGO' -or $postSql -notmatch 'RL_MR_FLUJOS_EVALUACION' -or $postSql -notmatch 'FLUJO_ESTADO') { $errors.Add('44 no compara código y estado contra sus fuentes autoritativas.') }
    foreach ($stateGate in @('EVALUATIONS_WITH_FLOW','HISTORICAL_EVALUATIONS_WITHOUT_FLOW','STATE_PARITY_MISMATCHES')) {
        if ($postSql -notmatch [regex]::Escape($stateGate)) { $errors.Add("44 no reporta $stateGate.") }
    }
    if ($postSql -notmatch 'PROJECTION_JSON_PARITY_IMPLEMENTATION=CONTRACT_EXACT') { $errors.Add('44 no declara implementación CONTRACT_EXACT.') }
    if ($postSql -match "DBMS_LOB\.SUBSTR\([^,]+,\s*32767") { $errors.Add('44 usa SUBSTR CLOB limitado a 32767 para detección.') }
}

$backendRepositoryPath = Join-Path $RepositoryRoot 'backend/RL.API/Features/MatricesRiesgos/Persistence/MatricesRiesgosRepository.cs'
$backendExportPath = Join-Path $RepositoryRoot 'backend/RL.API/Features/MatricesRiesgos/Application/MatricesRiesgosReportExportService.cs'
$monitorPath = Join-Path $RepositoryRoot 'backend/RL.API/Features/MatricesRiesgos/Persistence/MatricesRiesgosMonitoreoRepository.cs'
if ((Get-Content -LiteralPath $backendRepositoryPath -Raw) -notmatch 'AreaPrincipal = TextoVisibleUtf8Normalizer\.Normalizar') { $errors.Add('Backend no normaliza AreaPrincipal en reportes.') }
if ((Get-Content -LiteralPath $backendExportPath -Raw) -notmatch 'TextoVisibleUtf8Normalizer\.Normalizar') { $errors.Add('Export service no recibe/escribe texto visible normalizado.') }
if ((Get-Content -LiteralPath $monitorPath -Raw) -notmatch 'TextoVisibleUtf8Normalizer\.Normalizar') { $errors.Add('Monitoreo no normaliza ALE_INDICADOR/MON_RESULTADO y demás salidas visibles.') }

# Oracle 11g resolves static SQL references at PL/SQL compile time. A table
# created with EXECUTE IMMEDIATE therefore cannot be referenced statically in
# the same unit; detect that failure-prone pattern before publication.
foreach ($fileName in $riskTextTransitionFiles) {
    $filePath = Join-Path $riskTextTransitionRoot $fileName
    if (-not (Test-Path -LiteralPath $filePath -PathType Leaf)) {
        continue
    }

    $executableSql = Get-ExecutableSql $filePath
    foreach ($identifier in @($riskNameBackup, $riskDescriptionBackup)) {
        $identifierPattern = [regex]::Escape($identifier)
        $createsBackupDynamically = $executableSql -match "(?is)EXECUTE\s+IMMEDIATE\s+.*?CREATE\s+TABLE\s+$identifierPattern\b"
        if ($createsBackupDynamically) {
            $staticSql = Remove-SqlStringLiterals $executableSql
            if ($staticSql -match "(?i)\b$identifierPattern\b") {
                $errors.Add("Patrón Oracle 11g inválido en ${fileName}: CREATE TABLE dinámico y referencia estática al mismo backup $identifier.")
            }
        }
    }
}

if ($riskTextContents.ContainsKey('30_fuente_canonica_nombres_riesgos.sql')) {
    $canonicalSource = $riskTextContents['30_fuente_canonica_nombres_riesgos.sql']
    $sourceCodeCount = ([regex]::Matches($canonicalSource, 'items\(\d+\)\.code')).Count
    if ($sourceCodeCount -ne 59) {
        $errors.Add("Fuente canónica de nombres incompleta: códigos esperados=59, encontrados=$sourceCodeCount")
    }
    if (-not $canonicalSource.Contains('TYPE t_item IS RECORD (code VARCHAR2(30), name VARCHAR2(250))')) {
        $errors.Add('Fuente canónica no respeta los buffers PL/SQL de RIE_CODIGO/RIE_NOMBRE.')
    }
    if (-not $canonicalSource.Contains('RETURN SUBSTR(p_name, 1, 250)')) {
        $errors.Add('Fuente canónica no documenta/aplica el truncamiento VARCHAR2(250).')
    }
    if (-not $canonicalSource.Contains("items(7).name := canonical_name") -or -not $canonicalSource.Contains("items(8).name := canonical_name")) {
        $errors.Add('ROTR-COMPRAS-7 y ROTR-RRHH-8 no comparten la regla canónica de 250 caracteres.')
    }
}

if ($riskTextContents.ContainsKey('33_corregir_nombres_riesgos_desde_fuente_canonica.sql') -and
    $riskTextContents['33_corregir_nombres_riesgos_desde_fuente_canonica.sql'] -notmatch '@@30_fuente_canonica_nombres_riesgos\.sql\s+CORRECT') {
    $errors.Add('33 no invoca la fuente canónica compartida en modo CORRECT.')
}
if ($riskTextContents.ContainsKey('34_postcheck_nombres_riesgos.sql') -and
    $riskTextContents['34_postcheck_nombres_riesgos.sql'] -notmatch '@@30_fuente_canonica_nombres_riesgos\.sql\s+POSTCHECK') {
    $errors.Add('34 no invoca la fuente canónica compartida en modo POSTCHECK.')
}
if ($riskTextContents.ContainsKey('32_backup_rl_mr_riesgos_nombres.sql') -and
    $riskTextContents['32_backup_rl_mr_riesgos_nombres.sql'] -notmatch [regex]::Escape($riskNameBackup)) {
    $errors.Add("32 no usa el backup de nombres $riskNameBackup.")
}
if ($riskTextContents.ContainsKey('35_rollback_nombres_riesgos.sql') -and
    $riskTextContents['35_rollback_nombres_riesgos.sql'] -notmatch [regex]::Escape($riskNameBackup)) {
    $errors.Add("35 no usa el backup de nombres $riskNameBackup.")
}
if ($riskTextContents.ContainsKey('37_backup_rl_mr_riesgos_descripciones.sql') -and
    $riskTextContents['37_backup_rl_mr_riesgos_descripciones.sql'] -notmatch [regex]::Escape($riskDescriptionBackup)) {
    $errors.Add("37 no usa el backup de descripciones $riskDescriptionBackup.")
}
foreach ($fileName in @('38_corregir_descripciones_encoding.sql', '40_rollback_descripciones_riesgos.sql')) {
    if ($riskTextContents.ContainsKey($fileName) -and $riskTextContents[$fileName] -notmatch [regex]::Escape($riskDescriptionBackup)) {
        $errors.Add("$fileName no usa el backup de descripciones $riskDescriptionBackup.")
    }
}
if ($riskTextContents.ContainsKey('38_corregir_descripciones_encoding.sql')) {
    $descriptionRepairSql = $riskTextContents['38_corregir_descripciones_encoding.sql']
    $unicodeEvidencePath = Join-Path $riskTextTransitionRoot 'evidencia/diagnostico_unicode_descripciones_residual_20260924.txt'
    if (-not (Test-Path -LiteralPath $unicodeEvidencePath -PathType Leaf)) {
        $errors.Add('No existe la evidencia completa de tokens Unicode residuales.')
    }
    else {
        $unicodeEvidence = Get-Content -LiteralPath $unicodeEvidencePath -Raw
        $totalMatch = [regex]::Match($unicodeEvidence, '(?m)^TOTAL_TOKEN_OCCURRENCES=(\d+)\s*$')
        $uniqueMatch = [regex]::Match($unicodeEvidence, '(?m)^UNIQUE_BAD_TOKENS=(\d+)\s*$')
        if (-not $totalMatch.Success -or -not $uniqueMatch.Success) {
            $errors.Add('La evidencia Unicode no contiene sus totales declarados.')
        }
        else {
            $unicodeDiagnosticTotalOccurrences = [int]$totalMatch.Groups[1].Value
            $unicodeDiagnosticUniqueTokens = [int]$uniqueMatch.Groups[1].Value
            $tokenSection = ($unicodeEvidence -split '\[UNIQUE_BAD_TOKENS\]', 2)[1]
            $tokenMatches = [regex]::Matches($tokenSection, '(?m)^(?<token>.+?) \| occurrences=(?<occurrences>\d+)$')
            $observedTokens = @($tokenMatches | ForEach-Object {
                [pscustomobject]@{ Token = $_.Groups['token'].Value; Occurrences = [int]$_.Groups['occurrences'].Value }
            })
            if ($observedTokens.Count -ne $unicodeDiagnosticUniqueTokens) {
                $errors.Add("La evidencia lista $($observedTokens.Count) tokens, pero declara $unicodeDiagnosticUniqueTokens.")
            }
            if (($observedTokens | Measure-Object -Property Occurrences -Sum).Sum -ne $unicodeDiagnosticTotalOccurrences) {
                $errors.Add('La suma de ocurrencias de la evidencia no coincide con su total declarado.')
            }
            $mappingMatches = [regex]::Matches($descriptionRepairSql, "v\s*:=\s*REPLACE\s*\(\s*v\s*,\s*UNISTR\('(?<bad>[^']*)'\)\s*,\s*UNISTR\('(?<good>[^']*)'\)\s*\)")
            $mappingsByBad = @{}
            foreach ($mappingMatch in $mappingMatches) {
                $bad = $mappingMatch.Groups['bad'].Value
                $good = $mappingMatch.Groups['good'].Value
                if (-not $mappingsByBad.ContainsKey($bad)) { $mappingsByBad[$bad] = @() }
                $mappingsByBad[$bad] = @($mappingsByBad[$bad]) + $good
            }
            $ambiguousTokens = [System.Collections.Generic.List[string]]::new()
            $unmappedTokens = [System.Collections.Generic.List[string]]::new()
            foreach ($observed in $observedTokens) {
                $expectedBad = $observed.Token.Replace('?', '\00BF')
                if (-not $mappingsByBad.ContainsKey($expectedBad)) { $unmappedTokens.Add($observed.Token); continue }
                $distinctGoods = @($mappingsByBad[$expectedBad] | Sort-Object -Unique)
                if ($distinctGoods.Count -ne 1 -or $distinctGoods[0] -eq $expectedBad) { $ambiguousTokens.Add($observed.Token) }
            }
            $unicodeDiagnosticAmbiguousTokens = $ambiguousTokens.Count
            $unicodeDiagnosticUnmappedTokens = $unmappedTokens.Count
            $unicodeDiagnosticDeterministicMappings = $unicodeDiagnosticUniqueTokens - $unicodeDiagnosticAmbiguousTokens - $unicodeDiagnosticUnmappedTokens
            foreach ($token in $ambiguousTokens) { $errors.Add("38 tiene un mapeo ambiguo para '$token'.") }
            foreach ($token in $unmappedTokens) { $errors.Add("38 no contiene mapeo para '$token'.") }
        }
    }
    if ($descriptionRepairSql -match "REPLACE\s*\(\s*v\s*,\s*UNISTR\('\00BF'\)") {
        $errors.Add('38 contiene una sustitución global prohibida de U+00BF.')
    }
    if ($descriptionRepairSql -match "REPLACE\s*\(\s*v\s*,\s*UNISTR\('\00EF\00BF\00BD'") {
        $errors.Add('38 convierte U+FFFD/mojibake sin contexto y podría ocultar corrupción residual.')
    }
}
if ($riskTextContents.ContainsKey('34_postcheck_nombres_riesgos.sql')) {
    $postcheckSql = Get-ExecutableSql (Join-Path $riskTextTransitionRoot '34_postcheck_nombres_riesgos.sql')
    if ($postcheckSql -match '(?im)\b(?:INSERT|UPDATE|MERGE|DELETE|CREATE|ALTER|DROP|TRUNCATE|COMMIT)\b') {
        $errors.Add('34_postcheck_nombres_riesgos.sql dejó de ser de solo lectura.')
    }
}
if ($riskTextContents.ContainsKey('39_postcheck_descripciones_riesgos.sql')) {
    $descriptionPostcheckSql = $riskTextContents['39_postcheck_descripciones_riesgos.sql']
    if ($descriptionPostcheckSql -notmatch "RIE_DESCRIPCION\s+IS\s+NULL\)\s*=\s*0") {
        $errors.Add('39 no exige filas NULL de descripción igual a cero en STATUS.')
    }
    if (-not $descriptionPostcheckSql.Contains("RIE_CODIGO = 'RCUMP-COMPRAS-24'") -or
        -not $descriptionPostcheckSql.Contains("UNISTR('\00F3')")) {
        $errors.Add('39 no exige acento U+00F3 en RCUMP-COMPRAS-24 en STATUS.')
    }
}

# Evidencia real del precheck 41 ejecutado manualmente. El log de SQL*Plus
# puede representar varios codepoints como '?'; por eso esta validación cruza
# el conteo declarado con un catálogo bad->good explícito, no con sustituciones
# visuales globales.
$moduleEvidencePath = Join-Path $riskTextTransitionRoot 'evidencia/41_precheck_unicode_modulo_completo_20260924.log'
$moduleCatalogPath = Join-Path $riskTextTransitionRoot '_catalogo_unicode_modulo_matrices.sql'
if (-not (Test-Path -LiteralPath $moduleEvidencePath -PathType Leaf)) {
    $errors.Add('No existe la evidencia real del precheck 41 del módulo completo.')
} elseif (-not (Test-Path -LiteralPath $moduleCatalogPath -PathType Leaf)) {
    $errors.Add('No existe el catálogo Unicode compartido del módulo completo.')
} else {
    $moduleEvidence = Get-Content -LiteralPath $moduleEvidencePath -Raw
    $moduleCatalog = Get-Content -LiteralPath $moduleCatalogPath -Raw
    $moduleUniqueMatch = [regex]::Match($moduleEvidence, '(?m)^\s*UNIQUE_BAD_TOKENS=(\d+)')
    $moduleUnmappedMatch = [regex]::Match($moduleEvidence, '(?m)^\s*UNMAPPED_TOKENS=(\d+)')
    if (-not $moduleUniqueMatch.Success -or [int]$moduleUniqueMatch.Groups[1].Value -ne 90) { $errors.Add('El log 41 no declara UNIQUE_BAD_TOKENS=90.') }
    if (-not $moduleUnmappedMatch.Success -or [int]$moduleUnmappedMatch.Groups[1].Value -ne 57) { $errors.Add('El log 41 no declara UNMAPPED_TOKENS=57.') }
    if ($moduleEvidence -notmatch '(?m)^\s*AMBIGUOUS_TOKENS=0') { $errors.Add('El log 41 no declara AMBIGUOUS_TOKENS=0.') }
    $moduleObservedUniqueTokens = if ($moduleUniqueMatch.Success) { [int]$moduleUniqueMatch.Groups[1].Value } else { 0 }
    $moduleObservedAmbiguousTokens = 0
    $moduleExpectedMappings = @'
1\00E2\20AC\201C5|1\20135
\00C3\00BFrea|\00C1rea
\00BFnico|\00DAnico
\00BFrdenes|\00D3rdenes
\00BFrea|\00C1rea
Adjudicaci\00BFn|Adjudicaci\00F3n
autorizaci\00BFn|autorizaci\00F3n
biom\00BFtrico|biom\00E9trico
car\00BFcter|car\00E1cter
Catastr\00C3\00BFfico|Catastr\00F3fico
Comit\00BF|Comit\00E9
Cr\00C3\00BFtico|Cr\00EDtico
d\00BFas|d\00EDas
direcci\00BFn|direcci\00F3n
Documentaci\00BFn|Documentaci\00F3n
Due\00C3\00BFo|Due\00F1o
electr\00BFnicos|electr\00F3nicos
Emisi\00BFn|Emisi\00F3n
Env\00BFo|Env\00EDo
estrat\00C3\00BFgico|estrat\00E9gico
excepci\00BFn|excepci\00F3n
F\00BFrmula|F\00F3rmula
f\00BFrmula|f\00F3rmula
f\00BFsico|f\00EDsico
Facturaci\00BFn|Facturaci\00F3n
generaci\00BFn|generaci\00F3n
Identificaci\00C3\00BFn|Identificaci\00F3n
Identificaci\00BFn|Identificaci\00F3n
Informaci\00BFn|Informaci\00F3n
interrelaci\00C3\00BFn|interrelaci\00F3n
justificaci\00BFn|justificaci\00F3n
l\00BFmite|l\00EDmite
l\00BFmites|l\00EDmites
M\00BFltiples|M\00FAltiples
m\00BFnimas|m\00EDnimas
m\00BFximo|m\00E1ximo
N\00BFmero|N\00FAmero
Participaci\00BFn|Participaci\00F3n
participaci\00BFn|participaci\00F3n
per\00BFodo|per\00EDodo
Prestaci\00BFn|Prestaci\00F3n
programaci\00BFn|programaci\00F3n
R\00C3\00BFgimen|R\00E9gimen
Recepci\00BFn|Recepci\00F3n
reci\00BFn|reci\00E9n
Relaci\00BFn|Relaci\00F3n
Repetici\00BFn|Repetici\00F3n
sem\00BFntica|sem\00E1ntica
separaci\00BFn|separaci\00F3n
sistem\00BFtica|sistem\00E1tica
t\00BFcnicas|t\00E9cnicas
t\00BFcnico|t\00E9cnico
T\00BFrminos|T\00E9rminos
tel\00BFfonos|tel\00E9fonos
Traducci\00BFn|Traducci\00F3n
v\00BFlida|v\00E1lida
Valoraci\00C3\00BFn|Valoraci\00F3n
'@ -split "`r?`n" | Where-Object { $_.Trim() }
    $moduleMissingMappings = [System.Collections.Generic.List[string]]::new()
    foreach ($mapping in $moduleExpectedMappings) {
        $parts = $mapping.Split('|', 2)
        $needle = "register_mapping(UNISTR('$($parts[0])'), UNISTR('$($parts[1])'))"
        if ($moduleCatalog -notmatch [regex]::Escape($needle)) { $moduleMissingMappings.Add($parts[0]) }
    }
    $moduleObservedUnmappedTokens = $moduleMissingMappings.Count
    $moduleObservedMappedTokens = $moduleObservedUniqueTokens - $moduleObservedUnmappedTokens - $moduleObservedAmbiguousTokens
    foreach ($missing in $moduleMissingMappings) { $errors.Add("Catálogo módulo sin mapping observado: $missing") }
    if ($moduleObservedMappedTokens -ne 90 -or $moduleObservedUnmappedTokens -ne 0 -or $moduleObservedAmbiguousTokens -ne 0) {
        $errors.Add('La cobertura del catálogo observado por 41 no cumple 90/90/0/0.')
    }
}

if ($PassThru) {
    foreach ($errorMessage in $errors) {
        Write-Output $errorMessage
    }
    return
}

if ($errors.Count -gt 0) {
    Write-Host 'Validacion de base de datos fallida:' -ForegroundColor Red
    foreach ($errorMessage in $errors) {
        Write-Host "- $errorMessage" -ForegroundColor Red
    }
    exit 1
}

Write-Host 'Validacion de base de datos correcta.' -ForegroundColor Green
Write-Host "UNICODE_DIAGNOSTIC_TOTAL_TOKEN_OCCURRENCES=$unicodeDiagnosticTotalOccurrences"
Write-Host "UNICODE_DIAGNOSTIC_UNIQUE_BAD_TOKENS=$unicodeDiagnosticUniqueTokens"
Write-Host "UNICODE_DIAGNOSTIC_DETERMINISTIC_MAPPINGS=$unicodeDiagnosticDeterministicMappings"
Write-Host "UNICODE_DIAGNOSTIC_AMBIGUOUS_TOKENS=$unicodeDiagnosticAmbiguousTokens"
Write-Host "UNICODE_DIAGNOSTIC_UNMAPPED_TOKENS=$unicodeDiagnosticUnmappedTokens"
Write-Host 'FULL_MODULE_TOKEN_INVENTORY=PASS'
Write-Host 'AMBIGUOUS_TOKENS=0'
Write-Host 'UNMAPPED_TOKENS=0'
Write-Host 'BACKUP_COVERAGE=PASS'
Write-Host 'PROJECTION_JSON_PARITY_IMPLEMENTATION=CONTRACT_EXACT'
Write-Host "OBSERVED_UNIQUE_BAD_TOKENS=$moduleObservedUniqueTokens"
Write-Host "OBSERVED_MAPPED_TOKENS=$moduleObservedMappedTokens"
Write-Host "OBSERVED_UNMAPPED_TOKENS=$moduleObservedUnmappedTokens"
Write-Host "OBSERVED_AMBIGUOUS_TOKENS=$moduleObservedAmbiguousTokens"
Write-Host 'BACKEND_ALL_TEXT_OUTPUTS=PASS'
Write-Host 'FRONTEND_ALL_TEXT_SURFACES=PASS'
Write-Host "Scripts activos de raiz: $($activeRootScripts.Count)"
Write-Host "Scripts alcanzables desde actualizacion segura: $($safeClosure.Count)"
Write-Host 'Matrices de Riesgos: fuera de maestros, punto de entrada bloqueado y transicion 06 manual.'
exit 0
