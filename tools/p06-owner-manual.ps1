[CmdletBinding()]
param([string]$RepoRoot = '')

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($RepoRoot)) { $RepoRoot = Split-Path -Parent $PSScriptRoot }
$runner = Join-Path $RepoRoot 'tools\p06-qa-runner.ps1'
$manifest = Get-Content -Raw -LiteralPath (Join-Path $RepoRoot 'tools\qa\P06_QA_PACKAGE_MANIFEST.json') | ConvertFrom-Json
$packageDigest = $manifest.packageSha256.ToLowerInvariant()

function Stop-Owner([string]$Reason) { Write-Output "P06_OWNER_STATUS=BLOCKED"; Write-Output "P06_OWNER_BLOCK_REASON=$Reason"; exit 3 }
function Assert-LocalHttp([string]$Uri) {
    try { $response = Invoke-WebRequest -UseBasicParsing -Uri $Uri -MaximumRedirection 0 -TimeoutSec 5; if ($response.StatusCode -ne 200) { Stop-Owner "LOCAL_HTTP_STATUS_$Uri" } } catch { Stop-Owner "LOCAL_HTTP_UNAVAILABLE_$Uri" }
}
function Get-SecretMap {
    $lines = @(& dotnet user-secrets list --project (Join-Path $RepoRoot 'backend\RL.API\RL.API.csproj') 2>$null)
    if ($LASTEXITCODE -ne 0) { Stop-Owner 'BACKEND_USER_SECRETS_UNAVAILABLE' }
    $map = @{}
    foreach ($line in $lines) { if ($line -match '^\s*(?<key>[^=]+?)\s*=\s*(?<value>.*)$') { $map[$Matches.key.Trim()] = $Matches.value } }
    return $map
}
function Get-ConnectionPart([string]$Connection, [string]$Key) {
    $match = [Regex]::Match($Connection, "(?i)(?:^|;)\s*$Key\s*=\s*([^;]+)")
    if (-not $match.Success) { Stop-Owner "BACKEND_CONNECTION_MISSING_$Key" }
    return $match.Groups[1].Value.Trim()
}
function Invoke-OracleIdentity([string]$SqlPlusPath, [string]$UserName, [string]$Password, [string]$DataSource) {
    $psi = [Diagnostics.ProcessStartInfo]::new(); $psi.FileName = $SqlPlusPath; $psi.Arguments = '-L /nolog'; $psi.UseShellExecute = $false; $psi.CreateNoWindow = $true; $psi.RedirectStandardInput = $true; $psi.RedirectStandardOutput = $true; $psi.RedirectStandardError = $true
    $process = [Diagnostics.Process]::new(); $process.StartInfo = $psi
    if (-not $process.Start()) { Stop-Owner 'ORACLE_SQLPLUS_START_FAILED' }
    $process.StandardInput.WriteLine("connect $UserName/$Password@$DataSource")
    $process.StandardInput.WriteLine('set heading off feedback off pagesize 0 verify off echo off trimspool on')
    $process.StandardInput.WriteLine('whenever sqlerror exit 1')
    $process.StandardInput.WriteLine("select trim(sys_context('USERENV','CURRENT_SCHEMA')) || '|' || trim(sys_context('USERENV','SERVICE_NAME')) || '|' || trim(sys_context('USERENV','DB_UNIQUE_NAME')) || '|' || case when exists (select 1 from session_privs where privilege like '%ANY%' or privilege in ('INSERT','UPDATE','DELETE','DROP ANY TABLE','ALTER ANY TABLE','CREATE ANY TABLE')) then 'NO' else 'YES' end from dual;")
    $process.StandardInput.WriteLine('exit'); $process.StandardInput.Close()
    $stdout = $process.StandardOutput.ReadToEnd(); $stderr = $process.StandardError.ReadToEnd(); $process.WaitForExit()
    if ($process.ExitCode -ne 0 -or $stderr -match '(?i)ORA-|SP2-') { Stop-Owner 'ORACLE_READONLY_IDENTITY_QUERY_FAILED' }
    $line = ($stdout -split "`r?`n" | Where-Object { $_ -match '^\s*[^|]+\|[^|]+\|[^|]+\|[^|]+\s*$' } | Select-Object -Last 1).Trim()
    if ($line -notmatch '^(?<schema>[^|]+)\|(?<service>[^|]+)\|(?<db>[^|]+)\|(?<readonly>[^|]+)$') { Stop-Owner 'ORACLE_IDENTITY_OUTPUT_UNPARSEABLE' }
    $identity = @{ Schema=$Matches.schema.Trim(); Service=$Matches.service.Trim(); DbUniqueName=$Matches.db.Trim(); ReadOnly=$Matches.readonly.Trim() }
    if ($identity.Schema -ine 'RIESGO_LAVADO' -or $identity.Service -ine 'hpprod1' -or $identity.DbUniqueName -ine 'hpprod1' -or $identity.ReadOnly -ne 'YES') { Stop-Owner 'ORACLE_IDENTITY_TARGET_OR_PRIVILEGE_MISMATCH' }
    return $identity
}

# All checks below run before any web credential prompt.
$preflight = @(& powershell -NoProfile -File $runner -Mode PREFLIGHT -RepoRoot $RepoRoot 2>&1)
if ($LASTEXITCODE -ne 0) { Stop-Owner 'PACKAGE_PREFLIGHT_FAILED' }
$watcher = Get-ScheduledTask -TaskName 'RiesgoLavado-CodexQueue' -ErrorAction SilentlyContinue
if ($watcher -and $watcher.Settings.Enabled) { Stop-Owner 'WATCHER_ENABLED' }
Assert-LocalHttp 'http://localhost:4200/'
Assert-LocalHttp 'http://localhost:5043/healthz'
$backendProcess = @(Get-CimInstance Win32_Process | Where-Object { $_.CommandLine -match '(?i)RL.API.dll|dotnet.*run' })
if ($backendProcess.Count -eq 0) { Stop-Owner 'BACKEND_PROCESS_NOT_FOUND' }
$sqlplus = Get-Command sqlplus -ErrorAction SilentlyContinue
if (-not $sqlplus) { Stop-Owner 'SQLPLUS_NOT_AVAILABLE' }
$settings = Get-SecretMap
if ($settings['Database:Profile'] -ne 'PRODUCTION') { Stop-Owner 'BACKEND_PROFILE_NOT_PRODUCTION' }
$connection = $settings['ConnectionStrings:OracleDB_Production']
if ([string]::IsNullOrWhiteSpace($connection)) { Stop-Owner 'BACKEND_EFFECTIVE_CONNECTION_NOT_AVAILABLE' }
$oracleUser = Get-ConnectionPart $connection 'User Id'; $oraclePassword = Get-ConnectionPart $connection 'Password'; $dataSource = Get-ConnectionPart $connection 'Data Source'
$identity = Invoke-OracleIdentity $sqlplus.Source $oracleUser $oraclePassword $dataSource
$identityPath = Join-Path ([IO.Path]::GetTempPath()) ('P06-OracleIdentity-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ') + '.txt')
[IO.File]::WriteAllLines($identityPath, @('SOURCE=BACKEND_EFFECTIVE_CONNECTION','QUERY_EXECUTED=YES','SCHEMA=' + $identity.Schema,'SERVICE=' + $identity.Service,'DB_UNIQUE_NAME=' + $identity.DbUniqueName,'READ_ONLY=' + $identity.ReadOnly,'PACKAGE_SHA256=' + $packageDigest), [Text.UTF8Encoding]::new($false))
Write-Output 'P06_OWNER_ORACLE_IDENTITY=PASS'
Write-Output 'P06_OWNER_ORACLE_EVIDENCE=GENERATED_EXTERNAL_SANITIZED'

$approval = Read-Host 'Escriba P06-OWNER-APPROVED para continuar'
if ($approval -ne 'P06-OWNER-APPROVED') { Stop-Owner 'MANUAL_OWNER_APPROVAL_REQUIRED' }
$role = Read-Host 'Rol esperado exacto'; $modules = Read-Host 'IDs de modulos esperados en JSON, por ejemplo [1,2]'
$credential = Get-Credential -Message 'Credencial QA web; no se imprime ni se guarda'
$names = @('P06_QA_MODE','P06_QA_MANUAL_OWNER_APPROVAL','P06_QA_EXPECTED_ROLE','P06_QA_EXPECTED_MODULE_IDS_JSON','P06_QA_WEB_ORIGIN','P06_QA_API_ORIGIN','P06_QA_MANUAL_DB_IDENTITY_PATH','RL_QA_WEB_USERNAME','RL_QA_WEB_PASSWORD'); $old = @{}
try {
    foreach ($name in $names) { $old[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
    [Environment]::SetEnvironmentVariable('P06_QA_MODE','LOCAL_OWNER_MANUAL','Process'); [Environment]::SetEnvironmentVariable('P06_QA_MANUAL_OWNER_APPROVAL','YES','Process'); [Environment]::SetEnvironmentVariable('P06_QA_EXPECTED_ROLE',$role,'Process'); [Environment]::SetEnvironmentVariable('P06_QA_EXPECTED_MODULE_IDS_JSON',$modules,'Process'); [Environment]::SetEnvironmentVariable('P06_QA_WEB_ORIGIN','http://localhost:4200','Process'); [Environment]::SetEnvironmentVariable('P06_QA_API_ORIGIN','http://localhost:5043','Process'); [Environment]::SetEnvironmentVariable('P06_QA_MANUAL_DB_IDENTITY_PATH',$identityPath,'Process'); [Environment]::SetEnvironmentVariable('RL_QA_WEB_USERNAME',$credential.UserName,'Process'); [Environment]::SetEnvironmentVariable('RL_QA_WEB_PASSWORD',$credential.GetNetworkCredential().Password,'Process')
    & powershell -NoProfile -File $runner -Mode LOCAL_OWNER_MANUAL -RepoRoot $RepoRoot
    exit $LASTEXITCODE
}
finally { foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name, $old[$name], 'Process') }; $credential = $null; $oraclePassword = $null }
