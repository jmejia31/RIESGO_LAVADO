param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [switch]$PassThru
)

$ErrorActionPreference = 'Stop'
$databaseRoot = [System.IO.Path]::GetFullPath((Join-Path $RepositoryRoot 'database'))
$databasePrefix = $databaseRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
$errors = [System.Collections.Generic.List[string]]::new()

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
    '40_rollback_descripciones_riesgos.sql'
)
$riskNameBackup = 'RL_MR_RIES_NOM_BKP_20260924'
$riskDescriptionBackup = 'RL_MR_RIES_DESC_BKP_20260924'
$legacyRiskNameBackup = 'RL_MR_RIESGOS_NOMBRES_BKP_20260923'
$legacyRiskDescriptionBackup = 'RL_MR_RIESGOS_DESC_BKP_20260923'

foreach ($identifier in @($riskNameBackup, $riskDescriptionBackup)) {
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
    $observedDescriptionRepairs = @(
        @{ Bad = 'Pol\00BFticamente'; Good = 'Pol\00EDticamente' },
        @{ Bad = 'actualizaci\00BFn'; Good = 'actualizaci\00F3n' },
        @{ Bad = 'tecnol\00BFgicas'; Good = 'tecnol\00F3gicas' },
        @{ Bad = 'dise\00BFo'; Good = 'dise\00F1o' },
        @{ Bad = 'auditor\00BFa'; Good = 'auditor\00EDa' },
        @{ Bad = 'corrupci\00BFn'; Good = 'corrupci\00F3n' },
        @{ Bad = 'p\00BFrdida'; Good = 'p\00E9rdida' },
        @{ Bad = 'c\00BFnyuge'; Good = 'c\00F3nyuge' },
        @{ Bad = 'uni\00BFn'; Good = 'uni\00F3n' },
        @{ Bad = 'inter\00BFs'; Good = 'inter\00E9s' },
        @{ Bad = 'da\00BFo'; Good = 'da\00F1o' },
        @{ Bad = '\00BFtica'; Good = '\00E9tica' },
        @{ Bad = '\00BFreas'; Good = '\00E1reas' },
        @{ Bad = 'm\00BFdicos'; Good = 'm\00E9dicos' },
        @{ Bad = 'n\00BFmina'; Good = 'n\00F3mina' },
        @{ Bad = 'se\00BFalamientos'; Good = 'se\00F1alamientos' }
    )
    foreach ($repair in $observedDescriptionRepairs) {
        $badPattern = "UNISTR\('$([regex]::Escape($repair.Bad))'\)"
        $goodPattern = "UNISTR\('$([regex]::Escape($repair.Good))'\)"
        if ($descriptionRepairSql -notmatch $badPattern -or $descriptionRepairSql -notmatch $goodPattern) {
            $errors.Add("38 no contiene el reemplazo contextual observado $($repair.Bad) -> $($repair.Good).")
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
Write-Host "Scripts activos de raiz: $($activeRootScripts.Count)"
Write-Host "Scripts alcanzables desde actualizacion segura: $($safeClosure.Count)"
Write-Host 'Matrices de Riesgos: fuera de maestros, punto de entrada bloqueado y transicion 06 manual.'
exit 0
