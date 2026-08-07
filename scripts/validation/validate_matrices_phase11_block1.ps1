$ErrorActionPreference = 'Stop'

$repositoryRoot = Resolve-Path (Join-Path $PSScriptRoot '../..')
$phase11Root = Join-Path $repositoryRoot 'database/19_matrices_riesgos/fase11'
$jsonPath = Join-Path $phase11Root 'formulario_matriz_riesgos_laft_v1.json'
$seedPath = Join-Path $phase11Root '01_semillas_datos_iniciales_modelo_17_tablas.sql'
$validationPath = Join-Path $phase11Root '02_validar_semillas_bloque1_solo_lectura.sql'
$errors = New-Object System.Collections.Generic.List[string]

foreach ($path in @($jsonPath, $seedPath, $validationPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        $errors.Add("No se encontró el artefacto obligatorio: $path")
    }
}

if ($errors.Count -eq 0) {
    $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
    $jsonRaw = [System.IO.File]::ReadAllText($jsonPath, $utf8NoBom)
    $seedContent = [System.IO.File]::ReadAllText($seedPath, $utf8NoBom)
    $validationContent = [System.IO.File]::ReadAllText($validationPath, $utf8NoBom)

    try {
        if ($PSVersionTable.PSVersion.Major -ge 6) {
            $definition = $jsonRaw | ConvertFrom-Json -Depth 100
        } else {
            $definition = $jsonRaw | ConvertFrom-Json
        }
    }
    catch {
        $errors.Add("La definición JSON oficial no es válida: $($_.Exception.Message)")
        $definition = $null
    }

    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        $hashBytes = $sha256.ComputeHash($utf8NoBom.GetBytes($jsonRaw))
        $jsonHash = ([System.BitConverter]::ToString($hashBytes)).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $sha256.Dispose()
    }

    if ($jsonHash.Length -ne 64) {
        $errors.Add('El hash SHA-256 calculado no tiene 64 caracteres.')
    }

    if (-not $seedContent.Contains($jsonHash)) {
        $errors.Add("El script de semillas no contiene el hash real del JSON: $jsonHash")
    }

    if (-not $validationContent.Contains($jsonHash)) {
        $errors.Add("El script de validación no contiene el hash real del JSON: $jsonHash")
    }

    $jsonMatch = [System.Text.RegularExpressions.Regex]::Match(
        $seedContent,
        "v_json\s+CLOB\s*:=\s*q'~(?<json>.*?)~';",
        [System.Text.RegularExpressions.RegexOptions]::Singleline
    )
    if (-not $jsonMatch.Success) {
        $errors.Add('El script de semillas no contiene la definición JSON en v_json.')
    }
    elseif ($jsonMatch.Groups['json'].Value -cne $jsonRaw) {
        $errors.Add('El JSON embebido en el script no coincide byte a byte con la definición oficial.')
    }

    if ($null -ne $definition) {
        if ($definition.codigoFormulario -ne 'MATRIZ_RIESGOS_LAFT') {
            $errors.Add('El código oficial del formulario debe ser MATRIZ_RIESGOS_LAFT.')
        }

        $fields = @($definition.secciones | ForEach-Object { @($_.campos) })
        $fieldIds = @($fields | ForEach-Object { [string]$_.id })
        $fieldKeys = @($fields | ForEach-Object { [string]$_.clave })
        $requiredFields = @(
            'area_principal',
            'dueno_riesgo',
            'frecuencia_inherente',
            'impacto_inherente',
            'nivel_inherente',
            'controles_preventivo',
            'controles_detectivo',
            'controles_correctivo',
            'frecuencia_residual',
            'impacto_residual',
            'nivel_residual',
            'respuesta_riesgo'
        )

        foreach ($field in $requiredFields) {
            if ($fieldIds -notcontains $field) {
                $errors.Add("Falta el campo dinámico obligatorio: $field")
            }
            if ($fieldKeys -notcontains $field) {
                $errors.Add("Falta la clave frontend obligatoria: $field")
            }
        }

        if (@($fieldIds | Group-Object | Where-Object Count -gt 1).Count -gt 0) {
            $errors.Add('La definición contiene identificadores de campo duplicados.')
        }

        foreach ($field in $fields) {
            if ([string]$field.id -cne [string]$field.clave) {
                $errors.Add("El campo '$($field.id)' debe mantener id y clave idénticos.")
            }
        }

        $catalogs = @($definition.catalogos)
        $requiredCatalogs = @{
            'MR_FRECUENCIA_1_5' = 5
            'MR_IMPACTO_1_5' = 5
            'MR_NIVEL_RIESGO' = 4
            'MR_RESPUESTA_RIESGO' = 4
        }
        foreach ($entry in $requiredCatalogs.GetEnumerator()) {
            $catalog = @($catalogs | Where-Object codigo -eq $entry.Key)
            if ($catalog.Count -ne 1) {
                $errors.Add("Debe existir exactamente un catálogo '$($entry.Key)'.")
                continue
            }
            if (@($catalog[0].elementos).Count -ne $entry.Value) {
                $errors.Add("El catálogo '$($entry.Key)' debe contener $($entry.Value) elementos.")
            }
        }

        foreach ($field in @($fields | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_.codigoCatalogo) })) {
            if (@($catalogs | Where-Object codigo -eq $field.codigoCatalogo).Count -ne 1) {
                $errors.Add("El campo '$($field.id)' referencia un catálogo inexistente: $($field.codigoCatalogo)")
            }
        }

        $rules = @($definition.reglas)
        $officialRule = @(
            $rules | Where-Object {
                $_.codigo -eq 'CALCULO_VRI_VRR' -and
                $_.version -eq '1.0' -and
                $_.algoritmoId -eq 'MATRICES_VRI_ADITIVO_1_9'
            }
        )
        if ($officialRule.Count -ne 1) {
            $errors.Add('Debe existir exactamente una regla CALCULO_VRI_VRR v1.0 con el algoritmo oficial.')
        }
    }

    $requiredSeedTokens = @(
        "CURRENT_SCHEMA",
        "USR_ACTIVO = 1",
        "SEQ_RL_MR_FAMILIAS.NEXTVAL",
        "SEQ_RL_MR_VERSIONES.NEXTVAL",
        "SEQ_RL_MR_CATALOGOS.NEXTVAL",
        "SEQ_RL_MR_ELEMENTOS.NEXTVAL",
        "SEQ_RL_MR_REGLAS.NEXTVAL",
        "MERGE INTO RL_MR_FAMILIAS_FORMULARIO",
        "MERGE INTO RL_MR_CATALOGOS",
        "MERGE INTO RL_MR_ELEMENTOS_CATALOGO",
        "MERGE INTO RL_MR_REGLAS_CALCULO",
        "DBMS_LOB.COMPARE",
        "COMMIT;",
        "ROLLBACK;",
        "SEMILLAS FASE 11 BLOQUE 1: APLICADAS Y VALIDADAS"
    )
    foreach ($token in $requiredSeedTokens) {
        if (-not $seedContent.Contains($token)) {
            $errors.Add("El script de semillas no contiene el control obligatorio: $token")
        }
    }

    foreach ($forbiddenPattern in @(
        '(?im)^\s*DROP\s+',
        '(?im)^\s*TRUNCATE\s+',
        '(?im)^\s*DELETE\s+FROM\s+',
        'B10_',
        '05_ajustes_dashboard_seguridad_reportes',
        '06_reconstruir_modelo_17_tablas'
    )) {
        if ([System.Text.RegularExpressions.Regex]::IsMatch($seedContent, $forbiddenPattern)) {
            $errors.Add("El script de semillas contiene una operación o referencia prohibida: $forbiddenPattern")
        }
    }

    foreach ($token in @(
        'VALIDACION FASE 11 BLOQUE 1: CORRECTA',
        'MATRIZ_RIESGOS_LAFT',
        'CALCULO_VRI_VRR',
        'MATRICES_VRI_ADITIVO_1_9',
        'USER_OBJECTS',
        'USER_CONSTRAINTS'
    )) {
        if (-not $validationContent.Contains($token)) {
            $errors.Add("El script de validación no contiene el control obligatorio: $token")
        }
    }
}

if ($errors.Count -gt 0) {
    Write-Host 'VALIDACION FASE 11 BLOQUE 1: INCORRECTA' -ForegroundColor Red
    $errors | ForEach-Object { Write-Host " - $_" -ForegroundColor Red }
    exit 1
}

Write-Host 'VALIDACION FASE 11 BLOQUE 1: CORRECTA' -ForegroundColor Green
