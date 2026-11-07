[CmdletBinding()]
param(
    [ValidateSet('PREFLIGHT', 'CERTIFICATION')]
    [string]$Mode = 'PREFLIGHT',
    [string]$RepoRoot = ''
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

function Assert-ExternalAttestation([string]$EnvironmentName, [string[]]$RequiredMarkers) {
    $pathValue = [Environment]::GetEnvironmentVariable($EnvironmentName)
    if ([string]::IsNullOrWhiteSpace($pathValue) -or -not [IO.Path]::IsPathRooted($pathValue)) { Stop-P06 "MISSING_$EnvironmentName" }
    $fullPath = [IO.Path]::GetFullPath($pathValue)
    $repoFullPath = [IO.Path]::GetFullPath($RepoRoot).TrimEnd('\') + '\'
    if ($fullPath.StartsWith($repoFullPath, [StringComparison]::OrdinalIgnoreCase)) { Stop-P06 "ATTESTATION_INSIDE_REPO_$EnvironmentName" }
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) { Stop-P06 "ATTESTATION_FILE_MISSING_$EnvironmentName" }
    $text = Get-Content -Raw -LiteralPath $fullPath
    if ($text -match '(?i)password|token|cookie|authorization|connectionstring|secret') { Stop-P06 "ATTESTATION_SENSITIVE_$EnvironmentName" }
    foreach ($marker in $RequiredMarkers) {
        if ($text -notmatch "(?m)^$([Regex]::Escape($marker))=YES\s*$") { Stop-P06 "ATTESTATION_MARKER_MISSING_$EnvironmentName" }
    }
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

    $required = @('P06_QA_TRUSTED_CONTEXT', 'P06_QA_PACKAGE_SHA256', 'P06_QA_DB_TARGET_VERIFIED', 'P06_QA_WEB_ORIGIN_VERIFIED', 'P06_QA_EXPECTED_ROLE', 'P06_QA_EXPECTED_MODULE_IDS_JSON', 'RL_QA_WEB_USERNAME', 'RL_QA_WEB_PASSWORD', 'P06_QA_ISOLATION_ATTESTATION_PATH', 'P06_QA_DB_ATTESTATION_PATH', 'P06_QA_WEB_ATTESTATION_PATH')
    foreach ($name in $required) {
        if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($name))) { Stop-P06 "MISSING_$name" }
    }
    if ([Environment]::GetEnvironmentVariable('P06_QA_TRUSTED_CONTEXT') -ne 'YES') { Stop-P06 'TRUSTED_CONTEXT_NOT_ATTESTED' }
    if ([Environment]::GetEnvironmentVariable('P06_QA_DB_TARGET_VERIFIED') -ne 'YES') { Stop-P06 'ORACLE_TARGET_NOT_ATTESTED' }
    if ([Environment]::GetEnvironmentVariable('P06_QA_WEB_ORIGIN_VERIFIED') -ne 'YES') { Stop-P06 'WEB_ORIGIN_NOT_ATTESTED' }
    if ([Environment]::GetEnvironmentVariable('P06_QA_PACKAGE_SHA256').ToLowerInvariant() -ne $digest) { Stop-P06 'APPROVED_PACKAGE_HASH_MISMATCH' }
    Assert-ExternalAttestation 'P06_QA_ISOLATION_ATTESTATION_PATH' @('TRUSTED_RUNNER_ISOLATION', 'CODEX_ACCESS', 'WATCHER_ENABLED')
    Assert-ExternalAttestation 'P06_QA_DB_ATTESTATION_PATH' @('BACKEND_ORACLE_CONNECTION', 'ORACLE_SCHEMA_MATCH', 'ORACLE_SERVICE_MATCH', 'ORACLE_DB_UNIQUE_NAME_MATCH')
    Assert-ExternalAttestation 'P06_QA_WEB_ATTESTATION_PATH' @('QA_WEB_ORIGIN_VERIFIED', 'QA_WEB_RELEASE_VERIFIED', 'QA_TLS_CONTRACT_VERIFIED')
    $watcher = Get-ScheduledTask -TaskName 'RiesgoLavado-CodexQueue' -ErrorAction SilentlyContinue
    if ($watcher -and $watcher.Settings.Enabled) { Stop-P06 'WATCHER_ENABLED' }

    $runOutput = @(& npm exec --no -- playwright test --config=playwright.qa.config.ts --reporter=line 2>&1)
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
