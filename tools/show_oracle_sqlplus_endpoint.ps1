[CmdletBinding()]
param(
    [string]$SettingsPath
)

$ErrorActionPreference = 'Stop'
$SettingsPath = if ([string]::IsNullOrWhiteSpace($SettingsPath)) {
    Join-Path $PSScriptRoot '..\backend\RL.API\appsettings.json'
} else { $SettingsPath }
$settings = Get-Content -LiteralPath (Resolve-Path -LiteralPath $SettingsPath) -Raw | ConvertFrom-Json
$connection = [string]$settings.ConnectionStrings.OracleDB
$hostMatch = [regex]::Match($connection, '(?i)HOST\s*=\s*([^\)]+)')
$portMatch = [regex]::Match($connection, '(?i)PORT\s*=\s*(\d+)')
$serviceMatch = [regex]::Match($connection, '(?i)SERVICE_NAME\s*=\s*([^\)]+)')
$userMatch = [regex]::Match($connection, '(?i)(?:^|;)\s*USER\s*ID\s*=\s*([^;]+)')
if (!$hostMatch.Success -or !$portMatch.Success -or !$serviceMatch.Success -or !$userMatch.Success) {
    throw 'No fue posible extraer HOST, PORT, SERVICE_NAME y USER ID de ConnectionStrings:OracleDB.'
}

Write-Output "ORACLE_HOST=$($hostMatch.Groups[1].Value.Trim())"
Write-Output "ORACLE_PORT=$($portMatch.Groups[1].Value.Trim())"
Write-Output "ORACLE_SERVICE_NAME=$($serviceMatch.Groups[1].Value.Trim())"
Write-Output "ORACLE_USER=$($userMatch.Groups[1].Value.Trim())"
Write-Output 'ORACLE_PASSWORD=NO_IMPRIMIDA; SQL*Plus la solicitará de forma interactiva.'
