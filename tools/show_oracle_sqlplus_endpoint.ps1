[CmdletBinding()]
param(
    [string]$SettingsPath
)

$ErrorActionPreference = 'Stop'
$SettingsPath = if ([string]::IsNullOrWhiteSpace($SettingsPath)) {
    Join-Path $PSScriptRoot '..\backend\RL.API\appsettings.json'
} else { $SettingsPath }
$settings = Get-Content -LiteralPath (Resolve-Path -LiteralPath $SettingsPath) -Raw | ConvertFrom-Json
$apiProjectPath = Join-Path $PSScriptRoot '..\backend\RL.API\RL.API.csproj'
$apiProject = [xml](Get-Content -Raw -LiteralPath $apiProjectPath)
$userSecretsId = [string]$apiProject.Project.PropertyGroup.UserSecretsId
$userSecretsPath = Join-Path (Join-Path $env:APPDATA 'Microsoft\UserSecrets') (Join-Path $userSecretsId 'secrets.json')
$userSecrets = if (Test-Path -LiteralPath $userSecretsPath) { Get-Content -Raw -LiteralPath $userSecretsPath | ConvertFrom-Json } else { $null }
$profile = if (![string]::IsNullOrWhiteSpace($env:Database__Profile)) { $env:Database__Profile } elseif (![string]::IsNullOrWhiteSpace($userSecrets.Database.Profile)) { [string]$userSecrets.Database.Profile } else { [string]$settings.Database.Profile }
$connectionName = switch ($profile.Trim().ToUpperInvariant()) {
    'PRODUCTION' { 'OracleDB_Production' }
    'LOCAL_XE' { 'OracleDB_LocalXE' }
    default { throw 'Database:Profile debe ser PRODUCTION o LOCAL_XE; no se infiere ni se usa un valor por defecto.' }
}
$environmentConnectionName = "ConnectionStrings__$connectionName"
$connection = if (![string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($environmentConnectionName))) {
    [Environment]::GetEnvironmentVariable($environmentConnectionName)
} elseif (![string]::IsNullOrWhiteSpace($userSecrets.ConnectionStrings.$connectionName)) {
    [string]$userSecrets.ConnectionStrings.$connectionName
} else {
    [string]$settings.ConnectionStrings.$connectionName
}
$placeholder = [string]::IsNullOrWhiteSpace($connection) -or $connection -match '(?i)(<set-through|YOUR_|CHANGE_ME|REPLACE_)'
if ($placeholder) { throw "No existe una cadena utilizable para ConnectionStrings:$connectionName." }
$hostMatch = [regex]::Match($connection, '(?i)HOST\s*=\s*([^\)]+)')
$portMatch = [regex]::Match($connection, '(?i)PORT\s*=\s*(\d+)')
$serviceMatch = [regex]::Match($connection, '(?i)SERVICE_NAME\s*=\s*([^\)]+)')
$userMatch = [regex]::Match($connection, '(?i)(?:^|;)\s*USER\s*ID\s*=\s*([^;]+)')
if (!$hostMatch.Success -or !$portMatch.Success -or !$serviceMatch.Success -or !$userMatch.Success) {
    throw "No fue posible validar la cadena de ConnectionStrings:$connectionName."
}

Write-Output "DATABASE_PROFILE=$($profile.Trim().ToUpperInvariant())"
Write-Output 'ORACLE_HOST=REDACTED'
Write-Output 'ORACLE_PORT=REDACTED'
Write-Output 'ORACLE_SERVICE_NAME=REDACTED'
Write-Output 'ORACLE_USER=REDACTED'
Write-Output 'ORACLE_PASSWORD=NO_IMPRIMIDA; SQL*Plus la solicitará de forma interactiva.'
