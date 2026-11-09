param(
    [switch]$Quiet
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = Resolve-Path (Join-Path $PSScriptRoot '../..')
$validator = Join-Path $repositoryRoot 'scripts/validation/validate_matrices_preoracle_readiness.ps1'
$fixtureRoot = Join-Path ([IO.Path]::GetTempPath()) ('riesgo-lavado-p07-preoracle-' + [guid]::NewGuid().ToString('N'))

$fixtureFiles = @(
    'database/19_matrices_riesgos/transicion/06_reconstruir_modelo_17_tablas.sql',
    'database/19_matrices_riesgos/transicion/15_ddl_configuracion_calculo_312.sql',
    'database/19_matrices_riesgos/00_APLICAR_MODULO_MATRICES_RIESGOS.sql',
    'database/00_EJECUCION_PRIMERA_VEZ.sql',
    'database/00_EJECUCION_ACTUALIZACIONES_SEGURAS.sql',
    'tools/validate_database_scripts.ps1',
    'scripts/validation/validate_matrices_dynamic_ddl_alignment.ps1',
    'scripts/validation/validate_matrices_17_object_inventory.ps1',
    'scripts/validation/test_matrices_17_object_inventory.ps1',
    'backend/RL.API.Tests/Features/MatricesRiesgos/MatricesRiesgosRepositoryIntegrationTests.cs',
    'backend/RL.API.Tests/Features/MatricesRiesgos/MatricesRiesgosOracleCertificationContractTests.cs',
    'backend/RL.API.Tests/Support/OracleTestDatabaseProfile.cs',
    'deployment/matrices-riesgos/oracle/01_preflight_release.sql',
    '.github/workflows/quality-gates.yml'
)

function Copy-Fixture {
    param([string]$Destination)

    New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    foreach ($relativePath in $fixtureFiles) {
        $sourcePath = Join-Path $repositoryRoot $relativePath
        $destinationPath = Join-Path $Destination $relativePath
        $destinationDirectory = Split-Path -Parent $destinationPath
        New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
        Copy-Item -LiteralPath $sourcePath -Destination $destinationPath -Force
    }
}

function Read-Fixture {
    param([string]$Root, [string]$RelativePath)
    return [IO.File]::ReadAllText((Join-Path $Root $RelativePath))
}

function Write-Fixture {
    param([string]$Root, [string]$RelativePath, [string]$Content)
    [IO.File]::WriteAllText((Join-Path $Root $RelativePath), $Content, [Text.UTF8Encoding]::new($false))
}

function Invoke-FixtureValidator {
    param([string]$Root)

    $output = @(& powershell -NoProfile -ExecutionPolicy Bypass -File $validator -RepositoryRoot $Root 2>&1 | ForEach-Object { [string]$_ })
    [PSCustomObject]@{
        ExitCode = $LASTEXITCODE
        Output = ($output -join [Environment]::NewLine)
    }
}

function Assert-Result {
    param(
        [string]$Name,
        [int]$ExpectedExitCode,
        [object]$Result
    )

    if ($Result.ExitCode -ne $ExpectedExitCode) {
        throw "$Name esperaba exit code $ExpectedExitCode y obtuvo $($Result.ExitCode)."
    }

    if ($Result.Output -match 'TEST_ONLY_SECRET|Password=|Pwd=') {
        throw "$Name expuso un valor sensible o una contraseña en la salida."
    }
}

function Invoke-Case {
    param(
        [string]$Name,
        [scriptblock]$Mutate,
        [int]$ExpectedExitCode
    )

    $caseRoot = Join-Path $fixtureRoot $Name
    Copy-Fixture -Destination $caseRoot
    & $Mutate $caseRoot
    Assert-Result -Name $Name -ExpectedExitCode $ExpectedExitCode -Result (Invoke-FixtureValidator -Root $caseRoot)
}

try {
    New-Item -ItemType Directory -Path $fixtureRoot -Force | Out-Null

    $canonicalRoot = Join-Path $fixtureRoot 'canonical'
    Copy-Fixture -Destination $canonicalRoot
    Assert-Result -Name 'canonical' -ExpectedExitCode 0 -Result (Invoke-FixtureValidator -Root $canonicalRoot)
    Write-Host 'P07_PREORACLE_CASE_01_CANONICAL=PASS'

    Invoke-Case -Name 'missing-profile-opt-in' -ExpectedExitCode 1 -Mutate {
        param($root)
        $path = 'backend/RL.API.Tests/Features/MatricesRiesgos/MatricesRiesgosRepositoryIntegrationTests.cs'
        $content = Read-Fixture $root $path
        Write-Fixture $root $path ($content.Replace('RL_ORACLE_INTEGRATION_REQUIRED', 'REMOVED_IN_FIXTURE'))
    }
    Write-Host 'P07_PREORACLE_CASE_02_EXPLICIT_OPT_IN_REMOVED=FAIL_CLOSED'

    Invoke-Case -Name 'generic-connection-fallback' -ExpectedExitCode 1 -Mutate {
        param($root)
        $path = 'backend/RL.API.Tests/Features/MatricesRiesgos/MatricesRiesgosRepositoryIntegrationTests.cs'
        $content = Read-Fixture $root $path
        $content = $content.Replace('OracleTestDatabaseProfile.TryResolveConfiguredLocalXe(configuration, out string? localXe)', 'configuration.GetConnectionString("OracleDB")')
        Write-Fixture $root $path $content
    }
    Write-Host 'P07_PREORACLE_CASE_03_GENERIC_FALLBACK=FAIL_CLOSED'

    Invoke-Case -Name 'helper-generic-fallback' -ExpectedExitCode 1 -Mutate {
        param($root)
        $path = 'backend/RL.API.Tests/Support/OracleTestDatabaseProfile.cs'
        $content = Read-Fixture $root $path
        Write-Fixture $root $path ($content + [Environment]::NewLine + 'var unsafeFallback = configuration.GetConnectionString("OracleDB");')
    }
    Write-Host 'P07_PREORACLE_CASE_04_HELPER_FALLBACK=FAIL_CLOSED'

    Invoke-Case -Name 'destructive-master-include' -ExpectedExitCode 1 -Mutate {
        param($root)
        $path = 'database/00_EJECUCION_PRIMERA_VEZ.sql'
        $content = Read-Fixture $root $path
        Write-Fixture $root $path ($content + [Environment]::NewLine + '@@19_matrices_riesgos/transicion/06_reconstruir_modelo_17_tablas.sql')
    }
    Write-Host 'P07_PREORACLE_CASE_05_DESTRUCTIVE_INCLUDE=FAIL_CLOSED'

    Invoke-Case -Name 'encoded-secret' -ExpectedExitCode 1 -Mutate {
        param($root)
        $path = 'backend/RL.API.Tests/Features/MatricesRiesgos/MatricesRiesgosRepositoryIntegrationTests.cs'
        $content = Read-Fixture $root $path
        $fixtureSecret = 'Data Source=' + 'TEST_ONLY;User Id=fixture;Pass' + 'word=TEST_ONLY_SECRET'
        Write-Fixture $root $path ($content + [Environment]::NewLine + '// ' + $fixtureSecret)
    }
    Write-Host 'P07_PREORACLE_CASE_06_SECRET=FAIL_CLOSED'

    Invoke-Case -Name 'altered-operational-model' -ExpectedExitCode 1 -Mutate {
        param($root)
        $path = 'database/19_matrices_riesgos/transicion/06_reconstruir_modelo_17_tablas.sql'
        $content = Read-Fixture $root $path
        Write-Fixture $root $path ($content.Replace('CREATE TABLE RL_MR_AUTOMONITOREO', 'CREATE TABLE REMOVED_AUTOMONITOREO'))
    }
    Write-Host 'P07_PREORACLE_CASE_07_MODEL_ALTERED=FAIL_CLOSED'

    Invoke-Case -Name 'calculation-object-removed' -ExpectedExitCode 1 -Mutate {
        param($root)
        $path = 'database/19_matrices_riesgos/transicion/15_ddl_configuracion_calculo_312.sql'
        $content = Read-Fixture $root $path
        Write-Fixture $root $path ($content.Replace('RL_MR_FUNCIONES', 'RL_MR_FUNCIONES_REMOVED'))
    }
    Write-Host 'P07_PREORACLE_CASE_08_CALCULATION_OBJECT_REMOVED=FAIL_CLOSED'

    Write-Host 'P07_PREORACLE_NEGATIVE_REGRESSIONS=PASS'
}
finally {
    if (Test-Path -LiteralPath $fixtureRoot) {
        Remove-Item -LiteralPath $fixtureRoot -Recurse -Force
    }
}
