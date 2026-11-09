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
function Assert-BackendRuntimeCorrespondence([string]$RepoRoot) {
    $repoPath = [IO.Path]::GetFullPath($RepoRoot).TrimEnd('\')
    $processes = @(Get-CimInstance Win32_Process | Where-Object {
        $_.CommandLine -and $_.CommandLine -match '(?i)RL.API.dll|dotnet.*run' -and $_.CommandLine -match [Regex]::Escape($repoPath)
    })
    $listeners = @(Get-NetTCPConnection -LocalPort 5043 -State Listen -ErrorAction SilentlyContinue)
    $matched = @($listeners | Where-Object { $processes.ProcessId -contains $_.OwningProcess } | Sort-Object OwningProcess -Unique)
    if ($matched.Count -ne 1) { Stop-Owner 'BACKEND_RUNTIME_CORRESPONDENCE_UNPROVEN' }
    $runtimeProcess = @($processes | Where-Object { $_.ProcessId -eq [int]$matched[0].OwningProcess })
    if ($runtimeProcess.Count -ne 1) { Stop-Owner 'BACKEND_RUNTIME_CORRESPONDENCE_UNPROVEN' }
    $api = $null
    try { $api = Invoke-WebRequest -UseBasicParsing -Uri 'http://localhost:5043/api/configuracion/sistema' -MaximumRedirection 0 -TimeoutSec 5 } catch { Stop-Owner 'BACKEND_READONLY_ENDPOINT_UNAVAILABLE' }
    if ($api.StatusCode -ne 200) { Stop-Owner 'BACKEND_READONLY_ENDPOINT_STATUS' }
    try { $ready = Invoke-WebRequest -UseBasicParsing -Uri 'http://localhost:5043/readyz' -MaximumRedirection 0 -TimeoutSec 5 } catch { Stop-Owner 'BACKEND_DATABASE_READINESS_UNAVAILABLE' }
    if ($ready.StatusCode -ne 200) { Stop-Owner 'BACKEND_DATABASE_READINESS_STATUS' }
    try { $probe = Invoke-WebRequest -UseBasicParsing -Uri 'http://localhost:5043/__p06/oracle-identity' -MaximumRedirection 0 -TimeoutSec 5 } catch { Stop-Owner 'BACKEND_ORACLE_IDENTITY_PROBE_UNAVAILABLE' }
    if ($probe.StatusCode -ne 200) { Stop-Owner 'BACKEND_ORACLE_IDENTITY_PROBE_STATUS' }
    try { $probeBody = $probe.Content | ConvertFrom-Json } catch { Stop-Owner 'BACKEND_ORACLE_IDENTITY_PROBE_INVALID' }
    if ($probeBody.processId -ne [int]$matched[0].OwningProcess) { Stop-Owner 'BACKEND_PROCESS_IDENTITY_MISMATCH' }
    if ([string]::IsNullOrWhiteSpace($probeBody.profile) -or [string]::IsNullOrWhiteSpace($probeBody.sessionUser) -or [string]::IsNullOrWhiteSpace($probeBody.currentSchema) -or [string]::IsNullOrWhiteSpace($probeBody.serviceName) -or [string]::IsNullOrWhiteSpace($probeBody.dbUniqueName) -or [string]::IsNullOrWhiteSpace($probeBody.dbName) -or [string]::IsNullOrWhiteSpace($probeBody.instanceName)) { Stop-Owner 'BACKEND_ORACLE_IDENTITY_PROBE_INCOMPLETE' }
    Write-Output 'P06_OWNER_BACKEND_RUNTIME=PASS'
    return @{ ProcessId = $runtimeProcess[0].ProcessId; Endpoint = 'PASS'; Oracle = $probeBody }
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
    # La identidad y el contrato de persistencia son comprobaciones distintas.
    # No se exige una cuenta sin DML: login/refresh/logout necesitan persistir
    # sesiones, rotaciones, revocaciones y auditoría mediante el API.
    $process.StandardInput.WriteLine("select 'IDENTITY|' || trim(sys_context('USERENV','CURRENT_SCHEMA')) || '|' || trim(sys_context('USERENV','SERVICE_NAME')) || '|' || trim(sys_context('USERENV','DB_UNIQUE_NAME')) || '|' || trim(sys_context('USERENV','DB_NAME')) || '|' || trim(sys_context('USERENV','INSTANCE_NAME')) || '|SELECT_ONLY' from dual;")
    $process.StandardInput.WriteLine("select 'PRIVILEGES|AUTH_PERSISTENCE_OWNER|' || case when (select count(*) from user_tables where table_name in ('RL_USUARIOS','RL_REFRESH_TOKENS','RL_AUDITORIA')) = 3 then 'YES' else 'NO' end || '|AUTH_OBJECTS_VALID|' || case when (select count(*) from user_objects where object_name in ('RL_USUARIOS','RL_REFRESH_TOKENS','RL_AUDITORIA') and status <> 'VALID') = 0 then 'YES' else 'NO' end || '|AUTH_TRIGGERS_VALID|' || case when (select count(*) from user_triggers where table_name in ('RL_USUARIOS','RL_REFRESH_TOKENS','RL_AUDITORIA') and status <> 'ENABLED') = 0 then 'YES' else 'NO' end from dual;")
    $process.StandardInput.WriteLine('exit'); $process.StandardInput.Close()
    $stdout = $process.StandardOutput.ReadToEnd(); $stderr = $process.StandardError.ReadToEnd(); $process.WaitForExit()
    if ($process.ExitCode -ne 0 -or $stderr -match '(?i)ORA-|SP2-') { Stop-Owner 'ORACLE_READONLY_IDENTITY_QUERY_FAILED' }
    $lines = @($stdout -split "`r?`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ -match '^(IDENTITY|PRIVILEGES)\|' })
    $identityLine = $lines | Where-Object { $_ -like 'IDENTITY|*' } | Select-Object -Last 1
    $privilegeLine = $lines | Where-Object { $_ -like 'PRIVILEGES|*' } | Select-Object -Last 1
    $identityMatch = [Regex]::Match($identityLine, '^IDENTITY\|(?<schema>[^|]+)\|(?<service>[^|]+)\|(?<db>[^|]+)\|(?<dbname>[^|]+)\|(?<instance>[^|]+)\|(?<operation>[^|]+)$')
    $privilegeMatch = [Regex]::Match($privilegeLine, '^PRIVILEGES\|AUTH_PERSISTENCE_OWNER\|(?<owner>YES|NO)\|AUTH_OBJECTS_VALID\|(?<objects>YES|NO)\|AUTH_TRIGGERS_VALID\|(?<triggers>YES|NO)$')
    if (-not $identityMatch.Success -or -not $privilegeMatch.Success) { Stop-Owner 'ORACLE_IDENTITY_OUTPUT_UNPARSEABLE' }
    $identity = @{ Schema=$identityMatch.Groups['schema'].Value.Trim(); Service=$identityMatch.Groups['service'].Value.Trim(); DbUniqueName=$identityMatch.Groups['db'].Value.Trim(); DbName=$identityMatch.Groups['dbname'].Value.Trim(); InstanceName=$identityMatch.Groups['instance'].Value.Trim(); QueryOperation=$identityMatch.Groups['operation'].Value.Trim(); PrivilegeCheck='AUTH_PERSISTENCE_OWNER'; PrivilegeResult=$privilegeMatch.Groups['owner'].Value.Trim(); ObjectsValid=$privilegeMatch.Groups['objects'].Value.Trim(); TriggersValid=$privilegeMatch.Groups['triggers'].Value.Trim() }
    if ($identity.Schema -ine 'RIESGO_LAVADO' -or $identity.Service -ine 'hpprod1' -or $identity.DbUniqueName -ine 'hpprod1' -or $identity.QueryOperation -ne 'SELECT_ONLY') { Stop-Owner 'ORACLE_IDENTITY_TARGET_MISMATCH' }
    if ($identity.PrivilegeResult -ne 'YES' -or $identity.ObjectsValid -ne 'YES' -or $identity.TriggersValid -ne 'YES') { Stop-Owner 'ORACLE_AUTH_PERSISTENCE_PRIVILEGE_MISMATCH' }
    return $identity
}

# All checks below run before any web credential prompt.
$preflight = @(& powershell -NoProfile -File $runner -Mode PREFLIGHT -RepoRoot $RepoRoot 2>&1)
if ($LASTEXITCODE -ne 0) { Stop-Owner 'PACKAGE_PREFLIGHT_FAILED' }
$watcher = Get-ScheduledTask -TaskName 'RiesgoLavado-CodexQueue' -ErrorAction SilentlyContinue
if ($watcher -and $watcher.Settings.Enabled) { Stop-Owner 'WATCHER_ENABLED' }
Assert-LocalHttp 'http://localhost:4200/'
Assert-LocalHttp 'http://localhost:5043/healthz'
$backendRuntime = Assert-BackendRuntimeCorrespondence $RepoRoot
$sqlplus = Get-Command sqlplus -ErrorAction SilentlyContinue
if (-not $sqlplus) { Stop-Owner 'SQLPLUS_NOT_AVAILABLE' }
$settings = Get-SecretMap
if ($settings['Database:Profile'] -ne 'PRODUCTION') { Stop-Owner 'BACKEND_PROFILE_NOT_PRODUCTION' }
$connection = $settings['ConnectionStrings:OracleDB_Production']
if ([string]::IsNullOrWhiteSpace($connection)) { Stop-Owner 'BACKEND_EFFECTIVE_CONNECTION_NOT_AVAILABLE' }
$oracleUser = Get-ConnectionPart $connection 'User Id'; $oraclePassword = Get-ConnectionPart $connection 'Password'; $dataSource = Get-ConnectionPart $connection 'Data Source'
$identity = Invoke-OracleIdentity $sqlplus.Source $oracleUser $oraclePassword $dataSource
if ($backendRuntime.Oracle.profile -ne 'PRODUCTION' -or $backendRuntime.Oracle.sessionUser -ine $identity.Schema -or $backendRuntime.Oracle.currentSchema -ine $identity.Schema -or $backendRuntime.Oracle.serviceName -ine $identity.Service -or $backendRuntime.Oracle.dbUniqueName -ine $identity.DbUniqueName -or $backendRuntime.Oracle.dbName -ine $identity.DbName -or $backendRuntime.Oracle.instanceName -ine $identity.InstanceName) { Stop-Owner 'BACKEND_ORACLE_IDENTITY_MISMATCH' }
$identityPath = Join-Path ([IO.Path]::GetTempPath()) ('P06-OracleIdentity-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ') + '.txt')
[IO.File]::WriteAllLines($identityPath, @('SOURCE=BACKEND_ORACLE_DB_CONTEXT_AND_SQLPLUS','QUERY_EXECUTED=YES','QUERY_OPERATION=' + $identity.QueryOperation,'PRIVILEGE_CHECK=' + $identity.PrivilegeCheck,'PRIVILEGE_RESULT=PASS','AUTH_OBJECTS_VALID=' + $identity.ObjectsValid,'AUTH_TRIGGERS_VALID=' + $identity.TriggersValid,'BACKEND_RUNTIME_ORACLE_PROOF=PASS','BACKEND_PROCESS_ID=' + $backendRuntime.ProcessId,'BACKEND_PROFILE=' + $backendRuntime.Oracle.profile,'BACKEND_SESSION_USER=' + $backendRuntime.Oracle.sessionUser,'BACKEND_CURRENT_SCHEMA=' + $backendRuntime.Oracle.currentSchema,'BACKEND_SERVICE=' + $backendRuntime.Oracle.serviceName,'BACKEND_DB_UNIQUE_NAME=' + $backendRuntime.Oracle.dbUniqueName,'BACKEND_DB_NAME=' + $backendRuntime.Oracle.dbName,'BACKEND_INSTANCE_NAME=' + $backendRuntime.Oracle.instanceName,'SCHEMA=' + $identity.Schema,'SERVICE=' + $identity.Service,'DB_UNIQUE_NAME=' + $identity.DbUniqueName,'DB_NAME=' + $identity.DbName,'INSTANCE_NAME=' + $identity.InstanceName,'PACKAGE_SHA256=' + $packageDigest), [Text.UTF8Encoding]::new($false))
Write-Output 'P06_OWNER_ORACLE_IDENTITY=PASS'
Write-Output 'P06_OWNER_ORACLE_EVIDENCE=GENERATED_EXTERNAL_SANITIZED'

# /readyz demuestra que la instancia API tiene una conexión operativa, pero no
# demuestra que esa conexión sea exactamente la identidad obtenida por SQL*Plus.
# No se solicita aprobación ni credenciales web mientras esa correspondencia no
# esté expuesta por un diagnóstico seguro del runtime efectivo.
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
