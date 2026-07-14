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
        if ($line -match '^\s*@@(?<include>.+\.sql)\s*$') {
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
        elseif ($line -match '^\s*@[^@].+\.sql\s*$') {
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
    $withoutBlocks = [System.Text.RegularExpressions.Regex]::Replace($content, '/\*.*?\*/', '', [System.Text.RegularExpressions.RegexOptions]::Singleline)
    $executableLines = $withoutBlocks -split "`r?`n" | Where-Object {
        $_ -notmatch '^\s*--' -and $_ -notmatch '^\s*PROMPT(?:\s|$)'
    }

    return ($executableLines -join "`n")
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
    '19_matrices_riesgos/00_APLICAR_MODULO_MATRICES_RIESGOS.sql',
    '17_validate_module_ids.sql'
)

$safeUpdateOrder = $firstInstallOrder | Where-Object { $_ -notin @('01_create_tables.sql', '02_seed_data.sql') }
$matricesOrder = @(
    '19_matrices_riesgos/01_create_rl_mr_estructura.sql',
    '19_matrices_riesgos/02_register_modulo_matrices_riesgos.sql',
    '19_matrices_riesgos/03_seed_metodologia_matrices_riesgos.sql',
    '19_matrices_riesgos/04_fix_encoding_textos_oracle.sql',
    '19_matrices_riesgos/05_align_estado_en_evaluacion.sql'
)

Assert-IncludeOrder '00_EJECUCION_PRIMERA_VEZ.sql' $firstInstallOrder
Assert-IncludeOrder '00_EJECUCION_ACTUALIZACIONES_SEGURAS.sql' $safeUpdateOrder
Assert-IncludeOrder '19_matrices_riesgos/00_APLICAR_MODULO_MATRICES_RIESGOS.sql' $matricesOrder

$entrypoints = @(
    '00_EJECUCION_PRIMERA_VEZ.sql',
    '00_EJECUCION_ACTUALIZACIONES_SEGURAS.sql',
    '19_matrices_riesgos/00_APLICAR_MODULO_MATRICES_RIESGOS.sql'
)

foreach ($entrypoint in $entrypoints) {
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

$activeRootScripts = Get-ChildItem -LiteralPath $databaseRoot -File -Filter '*.sql' |
    Where-Object { $_.Name -match '^\d{2}_.+\.sql$' }
$manifestPath = Join-Path $databaseRoot '00_MANIFIESTO_SCRIPTS_APROBADOS.md'
$manifest = Get-Content -LiteralPath $manifestPath -Raw

foreach ($script in $activeRootScripts) {
    if (-not $firstClosure.Contains($script.FullName) -and $script.Name -notin @('00_EJECUCION_PRIMERA_VEZ.sql', '00_EJECUCION_ACTUALIZACIONES_SEGURAS.sql')) {
        $errors.Add("Script activo de raiz no alcanzable desde primera instalacion: $($script.Name)")
    }

    if (-not $manifest.Contains($script.Name)) {
        $errors.Add("Script activo ausente del manifiesto: $($script.Name)")
    }
}

$packageDirectories = Get-ChildItem -LiteralPath $databaseRoot -Directory |
    Where-Object { $_.Name -match '^\d{2}_' }
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
Write-Host "Paquetes modulares: $($packageDirectories.Count)"
Write-Host "Scripts alcanzables desde actualizacion segura: $($safeClosure.Count)"
