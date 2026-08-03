$ErrorActionPreference = 'Stop'

$repositoryRoot = Resolve-Path (Join-Path $PSScriptRoot '../..')
$script05 = Join-Path $repositoryRoot 'database/19_matrices_riesgos/instalacion/05_ajustes_dashboard_seguridad_reportes.sql'
$workflowTemporal = Join-Path $repositoryRoot '.github/workflows/agent-fix-matrices-phase1.yml'

$scanRoots = @(
    (Join-Path $repositoryRoot 'backend/RL.API/Features/MatricesRiesgos'),
    (Join-Path $repositoryRoot 'backend/RL.API.Tests/Features/MatricesRiesgos'),
    (Join-Path $repositoryRoot 'frontend/rl-app/src/app/features/admin/matrices-riesgos'),
    (Join-Path $repositoryRoot 'database/19_matrices_riesgos')
)

$extensions = @('.cs', '.ts', '.html', '.sql', '.json')
$excludedDirectoryNames = @('bin', 'obj', 'node_modules', 'dist', 'coverage', 'Historico')
$errors = New-Object System.Collections.Generic.List[string]

if (-not (Test-Path -LiteralPath $script05)) {
    $errors.Add("No se encontró el script Oracle 05: $script05")
}

if (Test-Path -LiteralPath $workflowTemporal) {
    $errors.Add('El workflow temporal agent-fix-matrices-phase1.yml no debe permanecer publicado.')
}

$forbiddenTokens = [ordered]@{
    'FLU_ESTADO_NUEVO' = 'La tabla definitiva solo contiene FLU_ESTADO.'
    'FLU_ESTADO_ANTERIOR' = 'La tabla definitiva solo contiene FLU_ESTADO.'
    'EVA_ESTADO' = 'El estado actual procede del último flujo; no existe EVA_ESTADO.'
    'EVA_VRI' = 'VRI se persiste en datos calculados, proyección y trazas; no existe EVA_VRI.'
    'EVA_ETP' = 'No existe EVA_ETP en RL_MR_EVALUACIONES_RIESGO.'
    'EVA_VRR' = 'VRR se persiste en datos calculados, proyección y trazas; no existe EVA_VRR.'
    'EVA_FECHA_EVAL' = 'La columna física es EVA_FECHA_REGISTRO.'
    'EVA_USR_EVAL' = 'La columna física es EVA_USR_REGISTRO.'
    'PROY_ETP' = 'No existe PROY_ETP en RL_MR_PROYECCIONES_EVALUACION.'
    'RL_MR_MODELOS' = 'Tabla retirada del modelo dinámico definitivo.'
    'RL_MR_FACTORES' = 'Tabla retirada del modelo dinámico definitivo.'
    'RL_MR_VARIABLES' = 'Tabla retirada del modelo dinámico definitivo.'
    'RL_MR_ESCALAS' = 'Tabla retirada del modelo dinámico definitivo.'
    'RL_MR_CRITERIOS' = 'Tabla retirada del modelo dinámico definitivo.'
    'DeterminarClasificacionResidual' = 'La clasificación no puede permanecer rígida en C#.'
    'RegistrarAuditoriaAsync' = 'El contrato institucional vigente expone RegistrarAsync.'
}

$sourceFiles = New-Object System.Collections.Generic.List[System.IO.FileInfo]
foreach ($root in $scanRoots) {
    if (-not (Test-Path -LiteralPath $root)) {
        $errors.Add("No se encontró una raíz obligatoria del módulo: $root")
        continue
    }

    Get-ChildItem -LiteralPath $root -Recurse -File | Where-Object {
        $extensions -contains $_.Extension.ToLowerInvariant() -and
        -not ($_.FullName -split [IO.Path]::DirectorySeparatorChar | Where-Object { $excludedDirectoryNames -contains $_ })
    } | ForEach-Object { $sourceFiles.Add($_) }
}

foreach ($file in $sourceFiles) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    foreach ($entry in $forbiddenTokens.GetEnumerator()) {
        if ($content.Contains($entry.Key)) {
            $relativePath = [IO.Path]::GetRelativePath($repositoryRoot, $file.FullName)
            $matches = Select-String -LiteralPath $file.FullName -SimpleMatch $entry.Key
            foreach ($match in $matches) {
                $errors.Add("$relativePath:$($match.LineNumber): identificador incompatible '$($entry.Key)'. $($entry.Value)")
            }
        }
    }
}

if (Test-Path -LiteralPath $script05) {
    $scriptContent = Get-Content -LiteralPath $script05 -Raw
    $requiredScriptTokens = @(
        "WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK",
        "DEFINE autorizacion = '&1'",
        "SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA')",
        "UPPER(TRIM(v_auth)) <> 'EJECUTAR'",
        'UQ_RL_MR_PROY_EVA',
        'IX_RL_MR_PROY_DASHBOARD'
    )

    foreach ($token in $requiredScriptTokens) {
        if (-not $scriptContent.Contains($token)) {
            $errors.Add("El script 05 no contiene la protección o estructura obligatoria: $token")
        }
    }

    if ($scriptContent -match '(?ms)BEGIN\s+PROMPT') {
        $errors.Add('El script 05 contiene PROMPT dentro de un bloque PL/SQL.')
    }
}

if ($errors.Count -gt 0) {
    Write-Host "Validación de alineación dinámica: FALLÓ ($($errors.Count) hallazgos)." -ForegroundColor Red
    foreach ($errorItem in $errors) {
        Write-Error $errorItem
    }
    exit 1
}

Write-Host "Validación integral del módulo Matrices contra el DDL dinámico: CORRECTA. Archivos revisados: $($sourceFiles.Count)." -ForegroundColor Green
