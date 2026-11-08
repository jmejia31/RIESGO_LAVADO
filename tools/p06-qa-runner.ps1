[CmdletBinding()]
param(
    [ValidateSet('PREFLIGHT', 'CERTIFICATION', 'LOCAL_OWNER_MANUAL')]
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

function Assert-ManualOwnerDbIdentity([string]$PackageDigest) {
    $pathValue = [Environment]::GetEnvironmentVariable('P06_QA_MANUAL_DB_IDENTITY_PATH')
    if ([string]::IsNullOrWhiteSpace($pathValue) -or -not [IO.Path]::IsPathRooted($pathValue)) { Stop-P06 'MISSING_P06_QA_MANUAL_DB_IDENTITY_PATH' }
    $fullPath = [IO.Path]::GetFullPath($pathValue)
    $repoFullPath = [IO.Path]::GetFullPath($RepoRoot).TrimEnd('\') + '\'
    if ($fullPath.StartsWith($repoFullPath, [StringComparison]::OrdinalIgnoreCase)) { Stop-P06 'MANUAL_DB_IDENTITY_INSIDE_REPO' }
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) { Stop-P06 'MANUAL_DB_IDENTITY_FILE_MISSING' }
    $values = @{}
    foreach ($line in @(Get-Content -LiteralPath $fullPath)) {
        if ([string]::IsNullOrWhiteSpace($line) -or $line -notmatch '^[A-Z][A-Z0-9_]*=[^=\r\n]+$') { Stop-P06 'MANUAL_DB_IDENTITY_MALFORMED' }
        $key, $value = $line -split '=', 2
        if ($values.ContainsKey($key) -or $value -match '(?i)password|token|secret|connectionstring') { Stop-P06 'MANUAL_DB_IDENTITY_UNSAFE' }
        $values[$key] = $value
    }
    $expected = @{ SOURCE = 'BACKEND_EFFECTIVE_CONNECTION'; QUERY_EXECUTED = 'YES'; QUERY_OPERATION = 'SELECT_ONLY'; PRIVILEGE_CHECK = 'EFFECTIVE_NO_DML_DDL_EXECUTE'; BACKEND_RUNTIME_CORRESPONDENCE = 'YES'; API_READONLY_ENDPOINT = 'PASS'; CONFIG_SOURCE = 'BACKEND_PROCESS_CONFIGURATION'; SCHEMA = 'RIESGO_LAVADO'; SERVICE = 'hpprod1'; DB_UNIQUE_NAME = 'hpprod1'; READ_ONLY = 'YES'; PACKAGE_SHA256 = $PackageDigest }
    foreach ($key in $expected.Keys) { if ($values[$key] -ne $expected[$key]) { Stop-P06 "MANUAL_DB_IDENTITY_INVALID_$key" } }
    Write-Output 'P06_MANUAL_DB_IDENTITY=PASS'
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
    $requiredFields = @('CONTROL', 'RESPONSIBLE', 'EVALUATED_AT', 'EVIDENCE_ID', 'PACKAGE_SHA256', 'ENVIRONMENT', 'RELEASE', 'RESULT', 'VALID_UNTIL', 'INDEPENDENT_METHOD', 'ISSUER', 'APPROVER', 'APPROVAL_ID', 'APPROVED_AT', 'INDEPENDENT_REVIEW', 'SIGNATURE_ALGORITHM') + @($ExpectedMarkers.Keys)
    foreach ($key in $requiredFields) {
        if (-not $values.ContainsKey($key)) { Stop-Contract "ATTESTATION_FIELD_MISSING_$EnvironmentName" }
    }
    foreach ($key in $values.Keys) {
        if ($requiredFields -notcontains $key) { Stop-Contract "ATTESTATION_UNKNOWN_FIELD_$EnvironmentName" }
    }
    if ($values['PACKAGE_SHA256'].ToLowerInvariant() -ne $PackageDigest) { Stop-Contract "ATTESTATION_PACKAGE_MISMATCH_$EnvironmentName" }
    if ($values['RESULT'] -ne 'PASS') { Stop-Contract "ATTESTATION_RESULT_NOT_PASS_$EnvironmentName" }
    if ($values['INDEPENDENT_REVIEW'] -ne 'YES' -or $values['APPROVER'] -match '(?i)codex|agent') { Stop-Contract "ATTESTATION_APPROVAL_INVALID_$EnvironmentName" }
    if ($values['EVALUATED_AT'] -notmatch '^\d{4}-\d{2}-\d{2}$' -or $values['VALID_UNTIL'] -notmatch '^\d{4}-\d{2}-\d{2}$' -or $values['APPROVED_AT'] -notmatch '^\d{4}-\d{2}-\d{2}$') { Stop-Contract "ATTESTATION_DATE_INVALID_$EnvironmentName" }
    $today = [DateTime]::UtcNow.Date
    $evaluatedAt = [DateTime]::ParseExact($values['EVALUATED_AT'], 'yyyy-MM-dd', [Globalization.CultureInfo]::InvariantCulture)
    $validUntil = [DateTime]::ParseExact($values['VALID_UNTIL'], 'yyyy-MM-dd', [Globalization.CultureInfo]::InvariantCulture)
    if ($evaluatedAt.Date -gt $today -or $validUntil.Date -lt $today -or $validUntil.Date -lt $evaluatedAt.Date) { Stop-Contract "ATTESTATION_EXPIRED_$EnvironmentName" }
    foreach ($key in $ExpectedMarkers.Keys) {
        if ($values[$key] -ne $ExpectedMarkers[$key]) { Stop-Contract "ATTESTATION_VALUE_INVALID_${EnvironmentName}_$key" }
    }
    $signatureName = $EnvironmentName -replace '_PATH$', '_SIGNATURE_PATH'
    $signatureValue = [Environment]::GetEnvironmentVariable($signatureName)
    if ([string]::IsNullOrWhiteSpace($signatureValue) -or -not [IO.Path]::IsPathRooted($signatureValue)) { Stop-P06 "MISSING_$signatureName" }
    $signaturePath = [IO.Path]::GetFullPath($signatureValue)
    if ($signaturePath.StartsWith($repoFullPath, [StringComparison]::OrdinalIgnoreCase)) { Stop-P06 "ATTESTATION_SIGNATURE_INSIDE_REPO_$EnvironmentName" }
    if (-not (Test-Path -LiteralPath $signaturePath -PathType Leaf)) { Stop-P06 "ATTESTATION_SIGNATURE_FILE_MISSING_$EnvironmentName" }
    $trustedThumbprint = [Environment]::GetEnvironmentVariable('P06_QA_ATTESTATION_SIGNER_THUMBPRINT')
    if ([string]::IsNullOrWhiteSpace($trustedThumbprint)) { Stop-P06 'MISSING_P06_QA_ATTESTATION_SIGNER_THUMBPRINT' }
    try {
        Add-Type -AssemblyName System.Security.Cryptography.Pkcs -ErrorAction Stop
        $contentInfo = [System.Security.Cryptography.Pkcs.ContentInfo]::new([IO.File]::ReadAllBytes($fullPath))
        $cms = [System.Security.Cryptography.Pkcs.SignedCms]::new($contentInfo, $true)
        $cms.Decode([IO.File]::ReadAllBytes($signaturePath))
        $cms.CheckSignature($false)
        if ($cms.SignerInfos.Count -ne 1 -or $null -eq $cms.SignerInfos[0].Certificate) { Stop-Contract "ATTESTATION_SIGNER_INVALID_$EnvironmentName" }
        $signerCertificate = $cms.SignerInfos[0].Certificate
        $now = [DateTime]::UtcNow
        if ($signerCertificate.Thumbprint -ne $trustedThumbprint -or $now -lt $signerCertificate.NotBefore.ToUniversalTime() -or $now -gt $signerCertificate.NotAfter.ToUniversalTime()) { Stop-Contract "ATTESTATION_SIGNER_INVALID_$EnvironmentName" }
        if ($values['SIGNATURE_ALGORITHM'] -ne 'CMS-SHA256') { Stop-Contract "ATTESTATION_SIGNATURE_ALGORITHM_INVALID_$EnvironmentName" }
    } catch {
        Stop-Contract "ATTESTATION_SIGNATURE_INVALID_$EnvironmentName"
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

    if ($Mode -eq 'LOCAL_OWNER_MANUAL') {
        $manualRequired = @('P06_QA_MANUAL_OWNER_APPROVAL', 'P06_QA_EXPECTED_ROLE', 'P06_QA_EXPECTED_MODULE_IDS_JSON', 'RL_QA_WEB_USERNAME', 'RL_QA_WEB_PASSWORD', 'P06_QA_MANUAL_DB_IDENTITY_PATH')
        foreach ($name in $manualRequired) { if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($name))) { Stop-P06 "MISSING_$name" } }
        if ([Environment]::GetEnvironmentVariable('P06_QA_MANUAL_OWNER_APPROVAL') -ne 'YES') { Stop-P06 'MANUAL_OWNER_APPROVAL_REQUIRED' }
        if ([Environment]::GetEnvironmentVariable('P06_QA_WEB_ORIGIN') -ne 'http://localhost:4200' -or [Environment]::GetEnvironmentVariable('P06_QA_API_ORIGIN') -ne 'http://localhost:5043') { Stop-P06 'LOCAL_OWNER_ORIGIN_MISMATCH' }
        foreach ($uri in @('http://localhost:4200/', 'http://localhost:5043/healthz')) {
            try { $response = Invoke-WebRequest -UseBasicParsing -Uri $uri -MaximumRedirection 0 -TimeoutSec 5; if ($response.StatusCode -ne 200) { Stop-P06 'LOCAL_OWNER_ORIGIN_UNAVAILABLE' } } catch { Stop-P06 'LOCAL_OWNER_ORIGIN_UNAVAILABLE' }
        }
        [Environment]::SetEnvironmentVariable('P06_QA_WEB_ORIGIN_VERIFIED', 'YES', 'Process')
        Write-Output 'P06_LOCAL_WEB_ORIGIN_VERIFIED=PASS'
        Assert-ManualOwnerDbIdentity $digest
        $watcher = Get-ScheduledTask -TaskName 'RiesgoLavado-CodexQueue' -ErrorAction SilentlyContinue
        if ($watcher -and $watcher.Settings.Enabled) { Stop-P06 'WATCHER_ENABLED' }
        if ($ValidateOnly) { Write-Output 'P06_LOCAL_OWNER_MANUAL_PREFLIGHT=PASS'; Write-Output 'P06_REAL_CERTIFICATION=NOT_EXECUTED'; exit 0 }
    }
    else {
    $required = @('P06_QA_TRUSTED_CONTEXT', 'P06_QA_PACKAGE_SHA256', 'P06_QA_DB_TARGET_VERIFIED', 'P06_QA_WEB_ORIGIN_VERIFIED', 'P06_QA_EXPECTED_ROLE', 'P06_QA_EXPECTED_MODULE_IDS_JSON', 'P06_QA_WEB_ORIGIN', 'P06_QA_API_ORIGIN', 'P06_QA_RELEASE', 'RL_QA_WEB_USERNAME', 'RL_QA_WEB_PASSWORD', 'P06_QA_ISOLATION_ATTESTATION_PATH', 'P06_QA_DB_ATTESTATION_PATH', 'P06_QA_WEB_ATTESTATION_PATH', 'P06_QA_ISOLATION_ATTESTATION_SHA256', 'P06_QA_DB_ATTESTATION_SHA256', 'P06_QA_WEB_ATTESTATION_SHA256', 'P06_QA_ISOLATION_ATTESTATION_SIGNATURE_PATH', 'P06_QA_DB_ATTESTATION_SIGNATURE_PATH', 'P06_QA_WEB_ATTESTATION_SIGNATURE_PATH', 'P06_QA_ATTESTATION_SIGNER_THUMBPRINT')
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
