[CmdletBinding()]
param([string]$RepoRoot = '')

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($RepoRoot)) { $RepoRoot = Split-Path -Parent $PSScriptRoot }
$runner = Join-Path $RepoRoot 'tools\p06-qa-runner.ps1'
$identityPath = Read-Host 'Ruta absoluta del archivo externo con identidad Oracle ya comprobada desde la conexion efectiva del backend'
$approval = Read-Host 'Escriba P06-OWNER-APPROVED para continuar'
if ($approval -ne 'P06-OWNER-APPROVED') { throw 'Aprobacion manual no confirmada.' }
$role = Read-Host 'Rol esperado exacto'
$modules = Read-Host 'IDs de modulos esperados en JSON, por ejemplo [1,2]'
$credential = Get-Credential -Message 'Credencial QA del propietario; no se imprime ni se guarda'
$old = @{}
try {
    $names = @('P06_QA_MODE','P06_QA_MANUAL_OWNER_APPROVAL','P06_QA_EXPECTED_ROLE','P06_QA_EXPECTED_MODULE_IDS_JSON','P06_QA_WEB_ORIGIN','P06_QA_API_ORIGIN','P06_QA_MANUAL_DB_IDENTITY_PATH','RL_QA_WEB_USERNAME','RL_QA_WEB_PASSWORD')
    foreach ($name in $names) { $old[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
    [Environment]::SetEnvironmentVariable('P06_QA_MODE','LOCAL_OWNER_MANUAL','Process')
    [Environment]::SetEnvironmentVariable('P06_QA_MANUAL_OWNER_APPROVAL','YES','Process')
    [Environment]::SetEnvironmentVariable('P06_QA_EXPECTED_ROLE',$role,'Process')
    [Environment]::SetEnvironmentVariable('P06_QA_EXPECTED_MODULE_IDS_JSON',$modules,'Process')
    [Environment]::SetEnvironmentVariable('P06_QA_WEB_ORIGIN','http://localhost:4200','Process')
    [Environment]::SetEnvironmentVariable('P06_QA_API_ORIGIN','http://localhost:5043','Process')
    [Environment]::SetEnvironmentVariable('P06_QA_MANUAL_DB_IDENTITY_PATH',$identityPath,'Process')
    [Environment]::SetEnvironmentVariable('RL_QA_WEB_USERNAME',$credential.UserName,'Process')
    [Environment]::SetEnvironmentVariable('RL_QA_WEB_PASSWORD',$credential.GetNetworkCredential().Password,'Process')
    & powershell -NoProfile -File $runner -Mode LOCAL_OWNER_MANUAL -RepoRoot $RepoRoot
    exit $LASTEXITCODE
}
finally {
    foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name, $old[$name], 'Process') }
    $credential = $null
}
