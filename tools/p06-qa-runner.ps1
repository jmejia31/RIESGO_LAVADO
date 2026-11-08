[CmdletBinding()]
param(
    [ValidateSet('PREFLIGHT', 'CERTIFICATION')]
    [string]$Mode = 'PREFLIGHT',
    [string]$RepoRoot = '',
    [switch]$ValidateOnly
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = Split-Path -Parent $PSScriptRoot
}
$frontend = Join-Path $RepoRoot 'frontend\rl-app'
$manifestPath = Join-Path $RepoRoot 'tools\qa\P06_QA_PACKAGE_MANIFEST.json'
$manifest = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json

function Stop-P06([string]$Reason, [int]$Code = 3) {
    Write-Output "P06_STATUS=BLOCKED"
    Write-Output "P06_BLOCK_REASON=$Reason"
    exit $Code
}

function Get-ManifestDigest($ManifestObject) {
    $rows = foreach ($file in ($ManifestObject.files | Sort-Object path)) {
        "$($file.path)=$((Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $RepoRoot $file.path)).Hash.ToLowerInvariant())"
    }
    $payload = (($rows -join "`n") + "`n")
    $sha = [Security.Cryptography.SHA256]::Create()
    return ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($payload)))).Replace('-', '').ToLowerInvariant()
}

function Stop-Contract([string]$Reason) {
    Stop-P06 $Reason
}

function Assert-ExternalAttestation([string]$EnvironmentName, [hashtable]$ExpectedMarkers, [string]$PackageDigest) {
    $pathValue = [Environment]::GetEnvironmentVariable($EnvironmentName)
    if ([string]::IsNullOrWhiteSpace($pathValue) -or -not [IO.Path]::IsPathRooted($pathValue)) { Stop-P06 "MISSING_$EnvironmentName" }
    $fullPath = [IO.Path]::GetFullPath($pathValue)
    $repoFullPath = [IO.Path]::GetFullPath($RepoRoot).TrimEnd('\') + '\'
    if ($fullPath.StartsWith($repoFullPath, [StringComparison]::OrdinalIgnoreCase)) { Stop-P06 "ATTESTATION_INSIDE_REPO_$EnvironmentName" }
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) { Stop-P06 "ATTESTATION_FILE_MISSING_$EnvironmentName" }
    $hashName = $EnvironmentName -replace '_PATH$', '_SHA256'
    $expectedHash = [Environment]::GetEnvironmentVariable($hashName)
    if ([string]::IsNullOrWhiteSpace($expectedHash)) { Stop-P06 "MISSING_$hashName" }
    $actualHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $fullPath).Hash.ToLowerInvariant()
    if ($actualHash -ne $expectedHash.ToLowerInvariant()) { Stop-P06 "ATTESTATION_HASH_MISMATCH_$EnvironmentName" }

    $lines = @(Get-Content -LiteralPath $fullPath)
    $values = @{}
    foreach ($line in $lines) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        if ($line -notmatch '^[A-Z][A-Z0-9_]*=[^=\r\n]+$') { Stop-Contract "ATTESTATION_MALFORMED_$EnvironmentName" }
        $key, $value = $line -split '=', 2
        if ($values.ContainsKey($key)) { Stop-Contract "ATTESTATION_DUPLICATE_$EnvironmentName" }
        if ($key -match '(?i)password|token|cookie|authorization|connectionstring|secret' -or $value -match '(?i)password|token|cookie|authorization|connectionstring|secret') { Stop-P06 "ATTESTATION_SENSITIVE_$EnvironmentName" }
        $values[$key] = $value
    }
    $requiredFields = @('CONTROL', 'RESPONSIBLE', 'EVALUATED_AT', 'EVIDENCE_ID', 'PACKAGE_SHA256', 'ENVIRONMENT', 'RELEASE', 'RESULT', 'VALID_UNTIL', 'INDEPENDENT_METHOD') + @($ExpectedMarkers.Keys)
    foreach ($key in $requiredFields) {
        if (-not $values.ContainsKey($key)) { Stop-Contract "ATTESTATION_FIELD_MISSING_$EnvironmentName" }
    }
    foreach ($key in $values.Keys) {
        if ($requiredFields -notcontains $key) { Stop-Contract "ATTESTATION_UNKNOWN_FIELD_$EnvironmentName" }
    }
    if ($values['PACKAGE_SHA256'].ToLowerInvariant() -ne $PackageDigest) { Stop-Contract "ATTESTATION_PACKAGE_MISMATCH_$EnvironmentName" }
    if ($values['RESULT'] -ne 'PASS') { Stop-Contract "ATTESTATION_RESULT_NOT_PASS_$EnvironmentName" }
    if ($values['EVALUATED_AT'] -notmatch '^\d{4}-\d{2}-\d{2}$' -or $values['VALID_UNTIL'] -notmatch '^\d{4}-\d{2}-\d{2}$') { Stop-Contract "ATTESTATION_DATE_INVALID_$EnvironmentName" }
    foreach ($key in $ExpectedMarkers.Keys) {
        if ($values[$key] -ne $ExpectedMarkers[$key]) { Stop-Contract "ATTESTATION_VALUE_INVALID_${EnvironmentName}_$key" }
    }
    return $values
}

foreach ($file in $manifest.files) {
    $path = Join-Path $RepoRoot $file.path
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { Stop-P06 'PACKAGE_FILE_MISSING' }
    $actual = (Get-FileHash -Algorithm SHA256 -LiteralPath $path).Hash.ToLowerInvariant()
    if ($actual -ne $file.sha256.ToLowerInvariant()) { Stop-P06 'PACKAGE_HASH_MISMATCH' }
}
$digest = Get-ManifestDigest $manifest
if ($digest -ne $manifest.packageSha256.ToLowerInvariant()) { Stop-P06 'PACKAGE_MANIFEST_MISMATCH' }
Write-Output 'P06_PACKAGE_INTEGRITY=PASS'
Write-Output "P06_PACKAGE_SHA256=$digest"

Push-Location $frontend
try {
    if (-not (Test-Path -LiteralPath 'node_modules\@playwright\test\package.json' -PathType Leaf)) { Stop-P06 'PLAYWRIGHT_NOT_INSTALLED' }
    $discovery = @(& npm exec --no -- playwright test --list --pass-with-no-tests --config=playwright.qa.config.ts 2>&1)
    if ($LASTEXITCODE -ne 0) { Stop-P06 'PLAYWRIGHT_DISCOVERY_FAILED' }
    $discoveryText = $discovery -join "`n"
    if ($discoveryText -notmatch 'Total:\s+6 tests in 1 file') { Stop-P06 'MANDATORY_SCENARIOS_NOT_DISCOVERED' }
    Write-Output 'P06_PLAYWRIGHT_DISCOVERY=6'

    if ($Mode -eq 'PREFLIGHT') {
        Write-Output 'P06_PREFLIGHT=PASS'
        Write-Output 'P06_REAL_CERTIFICATION=NOT_EXECUTED'
        exit 0
    }

    $required = @('P06_QA_TRUSTED_CONTEXT', 'P06_QA_PACKAGE_SHA256', 'P06_QA_DB_TARGET_VERIFIED', 'P06_QA_WEB_ORIGIN_VERIFIED', 'P06_QA_EXPECTED_ROLE', 'P06_QA_EXPECTED_MODULE_IDS_JSON', 'P06_QA_WEB_ORIGIN', 'P06_QA_API_ORIGIN', 'P06_QA_RELEASE', 'RL_QA_WEB_USERNAME', 'RL_QA_WEB_PASSWORD', 'P06_QA_ISOLATION_ATTESTATION_PATH', 'P06_QA_DB_ATTESTATION_PATH', 'P06_QA_WEB_ATTESTATION_PATH', 'P06_QA_ISOLATION_ATTESTATION_SHA256', 'P06_QA_DB_ATTESTATION_SHA256', 'P06_QA_WEB_ATTESTATION_SHA256')
    foreach ($name in $required) {
        if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($name))) { Stop-P06 "MISSING_$name" }
    }
    if ([Environment]::GetEnvironmentVariable('P06_QA_TRUSTED_CONTEXT') -ne 'YES') { Stop-P06 'TRUSTED_CONTEXT_NOT_ATTESTED' }
    if ([Environment]::GetEnvironmentVariable('P06_QA_DB_TARGET_VERIFIED') -ne 'YES') { Stop-P06 'ORACLE_TARGET_NOT_ATTESTED' }
    if ([Environment]::GetEnvironmentVariable('P06_QA_WEB_ORIGIN_VERIFIED') -ne 'YES') { Stop-P06 'WEB_ORIGIN_NOT_ATTESTED' }
    if ([Environment]::GetEnvironmentVariable('P06_QA_PACKAGE_SHA256').ToLowerInvariant() -ne $digest) { Stop-P06 'APPROVED_PACKAGE_HASH_MISMATCH' }
    $webOrigin = [Environment]::GetEnvironmentVariable('P06_QA_WEB_ORIGIN')
    $apiOrigin = [Environment]::GetEnvironmentVariable('P06_QA_API_ORIGIN')
    foreach ($origin in @($webOrigin, $apiOrigin)) {
        $uri = [Uri]$origin
        if ($uri.AbsolutePath -ne '/' -or $uri.Query -or $uri.Fragment -or $uri.UserInfo) { Stop-P06 'ORIGIN_FORMAT_INVALID' }
        if ($uri.Scheme -ne 'https') {
            if ($uri.Host -notin @('localhost', '127.0.0.1', '::1') -or [Environment]::GetEnvironmentVariable('P06_QA_LOCALHOST_EXCEPTION') -ne 'YES') { Stop-P06 'TLS_ORIGIN_REQUIRED' }
        }
    }
    $isolation = Assert-ExternalAttestation 'P06_QA_ISOLATION_ATTESTATION_PATH' @{ TRUSTED_RUNNER_ISOLATION = 'YES'; CODEX_ACCESS = 'NO'; WATCHER_ENABLED = 'NO' } $digest
    $db = Assert-ExternalAttestation 'P06_QA_DB_ATTESTATION_PATH' @{ BACKEND_ORACLE_CONNECTION = 'YES'; ORACLE_SCHEMA_MATCH = 'YES'; ORACLE_SERVICE_MATCH = 'YES'; ORACLE_DB_UNIQUE_NAME_MATCH = 'YES' } $digest
    $webExpected = @{ QA_WEB_ORIGIN_VERIFIED = 'YES'; QA_API_ORIGIN_VERIFIED = 'YES'; QA_WEB_RELEASE_VERIFIED = 'YES'; QA_TLS_CONTRACT_VERIFIED = 'YES'; QA_WEB_ORIGIN = $webOrigin; QA_API_ORIGIN = $apiOrigin }
    if ($webOrigin -match '^http://') { $webExpected['QA_LOCALHOST_EXCEPTION_APPROVED'] = 'YES' }
    $web = Assert-ExternalAttestation 'P06_QA_WEB_ATTESTATION_PATH' $webExpected $digest
    if ($web['QA_WEB_ORIGIN'] -ne $webOrigin -or $web['QA_API_ORIGIN'] -ne $apiOrigin -or $web['RELEASE'] -ne [Environment]::GetEnvironmentVariable('P06_QA_RELEASE')) { Stop-P06 'APPROVED_DESTINATION_MISMATCH' }
    if ($webOrigin -match '^http://' -and $web['QA_LOCALHOST_EXCEPTION_APPROVED'] -ne 'YES') { Stop-P06 'LOCALHOST_EXCEPTION_NOT_ATTESTED' }
    $watcher = Get-ScheduledTask -TaskName 'RiesgoLavado-CodexQueue' -ErrorAction SilentlyContinue
    if ($watcher -and $watcher.Settings.Enabled) { Stop-P06 'WATCHER_ENABLED' }

    if ($ValidateOnly) {
        Write-Output 'P06_CERTIFICATION_PRECONDITIONS=PASS'
        Write-Output 'P06_REAL_CERTIFICATION=NOT_EXECUTED'
        exit 0
    }

    $previousMode = [Environment]::GetEnvironmentVariable('P06_QA_MODE', 'Process')
    [Environment]::SetEnvironmentVariable('P06_QA_MODE', $Mode, 'Process')
    $runOutput = @(& npm exec --no -- playwright test --config=playwright.qa.config.ts --reporter=line 2>&1)
    [Environment]::SetEnvironmentVariable('P06_QA_MODE', $previousMode, 'Process')
    $runExit = $LASTEXITCODE
    $runText = $runOutput -join "`n"
    if ($runExit -ne 0) { Write-Output 'P06_REAL_CERTIFICATION=FAIL'; exit 1 }
    if ($runText -match '\bskipped\b' -or $runText -notmatch '6 passed') { Stop-P06 'CERTIFICATION_INCOMPLETE' }
    Write-Output 'P06_REAL_CERTIFICATION=PASS'
    exit 0
}
finally {
    Pop-Location
}
