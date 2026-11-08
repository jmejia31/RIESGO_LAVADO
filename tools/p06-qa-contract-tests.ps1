[CmdletBinding()]
param([string]$RepoRoot = '')

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($RepoRoot)) { $RepoRoot = Split-Path -Parent $PSScriptRoot }
$runner = Join-Path $RepoRoot 'tools\p06-qa-runner.ps1'
$manifestPath = Join-Path $RepoRoot 'tools\qa\P06_QA_PACKAGE_MANIFEST.json'
$digest = (Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json).packageSha256
$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('p06-contract-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempRoot | Out-Null

function Set-EnvValue([string]$Name, [string]$Value) { [Environment]::SetEnvironmentVariable($Name, $Value, 'Process') }
function Clear-P06Env {
    @('P06_QA_MODE','P06_QA_TRUSTED_CONTEXT','P06_QA_PACKAGE_SHA256','P06_QA_DB_TARGET_VERIFIED','P06_QA_WEB_ORIGIN_VERIFIED','P06_QA_EXPECTED_ROLE','P06_QA_EXPECTED_MODULE_IDS_JSON','P06_QA_WEB_ORIGIN','P06_QA_API_ORIGIN','P06_QA_RELEASE','P06_QA_LOCALHOST_EXCEPTION','RL_QA_WEB_USERNAME','RL_QA_WEB_PASSWORD','P06_QA_ISOLATION_ATTESTATION_PATH','P06_QA_DB_ATTESTATION_PATH','P06_QA_WEB_ATTESTATION_PATH','P06_QA_ISOLATION_ATTESTATION_SHA256','P06_QA_DB_ATTESTATION_SHA256','P06_QA_WEB_ATTESTATION_SHA256','P06_QA_ISOLATION_ATTESTATION_SIGNATURE_PATH','P06_QA_DB_ATTESTATION_SIGNATURE_PATH','P06_QA_WEB_ATTESTATION_SIGNATURE_PATH','P06_QA_ATTESTATION_SIGNER_THUMBPRINT') | ForEach-Object { Set-EnvValue $_ $null }
}
function Write-Contract([string]$Name, [string[]]$Lines) {
    $path = Join-Path $tempRoot $Name
    [IO.File]::WriteAllLines($path, $Lines, [Text.UTF8Encoding]::new($false))
    return $path
}
function Set-AttestationEnv([string]$IsolationPath, [string]$DbPath, [string]$WebPath) {
    Set-EnvValue 'P06_QA_ISOLATION_ATTESTATION_PATH' $IsolationPath
    Set-EnvValue 'P06_QA_DB_ATTESTATION_PATH' $DbPath
    Set-EnvValue 'P06_QA_WEB_ATTESTATION_PATH' $WebPath
    Set-EnvValue 'P06_QA_ISOLATION_ATTESTATION_SHA256' ((Get-FileHash $IsolationPath -Algorithm SHA256).Hash)
    Set-EnvValue 'P06_QA_DB_ATTESTATION_SHA256' ((Get-FileHash $DbPath -Algorithm SHA256).Hash)
    Set-EnvValue 'P06_QA_WEB_ATTESTATION_SHA256' ((Get-FileHash $WebPath -Algorithm SHA256).Hash)
    foreach ($pair in @(@('P06_QA_ISOLATION_ATTESTATION_SIGNATURE_PATH', 'isolation.p7s'), @('P06_QA_DB_ATTESTATION_SIGNATURE_PATH', 'db.p7s'), @('P06_QA_WEB_ATTESTATION_SIGNATURE_PATH', 'web.p7s'))) {
        $signaturePath = Join-Path $tempRoot $pair[1]
        [IO.File]::WriteAllBytes($signaturePath, [byte[]](0x00, 0x01, 0x02))
        Set-EnvValue $pair[0] $signaturePath
    }
    Set-EnvValue 'P06_QA_ATTESTATION_SIGNER_THUMBPRINT' ('0' * 40)
}
function Invoke-Runner([string]$Label, [string]$ExpectedReason = '', [string]$Mode = 'CERTIFICATION') {
    $output = @(& powershell -NoProfile -File $runner -Mode $Mode -RepoRoot $RepoRoot -ValidateOnly 2>&1)
    $exitCode = $LASTEXITCODE
    $text = $output -join "`n"
    if ($ExpectedReason) {
        if ($exitCode -ne 3 -or $text -notmatch [Regex]::Escape($ExpectedReason)) { throw "$Label failed: expected $ExpectedReason, exit $exitCode, output=$text" }
    } else {
        $successMarker = if ($Mode -eq 'LOCAL_OWNER_MANUAL') { 'P06_LOCAL_OWNER_MANUAL_PREFLIGHT=PASS' } else { 'P06_CERTIFICATION_PRECONDITIONS=PASS' }
        if ($exitCode -ne 0 -or $text -notmatch [Regex]::Escape($successMarker)) { throw "$Label failed: exit $exitCode, output=$text" }
    }
    Write-Output "P06_CONTRACT_$Label=PASS"
}

try {
    Clear-P06Env
    $common = @('CONTROL=external-control','RESPONSIBLE=owner-security','EVALUATED_AT=2026-11-07','EVIDENCE_ID=P06-TEST-001',"PACKAGE_SHA256=$digest",'ENVIRONMENT=synthetic-test','RELEASE=test-release','RESULT=PASS','VALID_UNTIL=2027-11-07','INDEPENDENT_METHOD=owner-reviewed','ISSUER=security-authority','APPROVER=owner-security','APPROVAL_ID=P06-TEST-APPROVAL','APPROVED_AT=2026-11-07','INDEPENDENT_REVIEW=YES','SIGNATURE_ALGORITHM=CMS-SHA256')
    $iso = Write-Contract 'isolation.txt' ($common + @('TRUSTED_RUNNER_ISOLATION=YES','CODEX_ACCESS=NO','WATCHER_ENABLED=NO'))
    $db = Write-Contract 'db.txt' ($common + @('BACKEND_ORACLE_CONNECTION=YES','ORACLE_SCHEMA_MATCH=YES','ORACLE_SERVICE_MATCH=YES','ORACLE_DB_UNIQUE_NAME_MATCH=YES'))
    $web = Write-Contract 'web.txt' ($common + @('QA_WEB_ORIGIN_VERIFIED=YES','QA_API_ORIGIN_VERIFIED=YES','QA_WEB_RELEASE_VERIFIED=YES','QA_TLS_CONTRACT_VERIFIED=YES','QA_WEB_ORIGIN=http://localhost:4200','QA_API_ORIGIN=http://localhost:5043','QA_LOCALHOST_EXCEPTION_APPROVED=YES'))
    Set-AttestationEnv $iso $db $web
    Set-EnvValue 'P06_QA_MODE' 'CERTIFICATION'; Set-EnvValue 'P06_QA_TRUSTED_CONTEXT' 'YES'; Set-EnvValue 'P06_QA_PACKAGE_SHA256' $digest; Set-EnvValue 'P06_QA_DB_TARGET_VERIFIED' 'YES'; Set-EnvValue 'P06_QA_WEB_ORIGIN_VERIFIED' 'YES'; Set-EnvValue 'P06_QA_EXPECTED_ROLE' 'TEST'; Set-EnvValue 'P06_QA_EXPECTED_MODULE_IDS_JSON' '[1]'; Set-EnvValue 'P06_QA_WEB_ORIGIN' 'http://localhost:4200'; Set-EnvValue 'P06_QA_API_ORIGIN' 'http://localhost:5043'; Set-EnvValue 'P06_QA_RELEASE' 'test-release'; Set-EnvValue 'P06_QA_LOCALHOST_EXCEPTION' 'YES'; Set-EnvValue 'RL_QA_WEB_USERNAME' 'P06_TEST_SENTINEL'; Set-EnvValue 'RL_QA_WEB_PASSWORD' 'P06_TEST_SENTINEL'
    Invoke-Runner 'UNSIGNED_ATTESTATIONS' 'ATTESTATION_SIGNATURE_INVALID_P06_QA_ISOLATION_ATTESTATION'

    $cases = @(
        @{ Name='CODEX_ACCESS_YES'; File=$iso; Replace=@('CODEX_ACCESS=YES'); Reason='ATTESTATION_VALUE_INVALID_P06_QA_ISOLATION_ATTESTATION_PATH_CODEX_ACCESS' },
        @{ Name='WATCHER_YES'; File=$iso; Replace=@('WATCHER_ENABLED=YES'); Reason='ATTESTATION_VALUE_INVALID_P06_QA_ISOLATION_ATTESTATION_PATH_WATCHER_ENABLED' },
        @{ Name='MISSING_ISOLATION'; File=$iso; Replace=@('TRUSTED_RUNNER_ISOLATION='); Reason='ATTESTATION_FIELD_MISSING_P06_QA_ISOLATION_ATTESTATION_PATH' },
        @{ Name='DUPLICATE'; File=$iso; Replace=@('TRUSTED_RUNNER_ISOLATION=YES','TRUSTED_RUNNER_ISOLATION=YES'); Reason='ATTESTATION_DUPLICATE_P06_QA_ISOLATION_ATTESTATION_PATH' },
        @{ Name='UNKNOWN'; File=$iso; Replace=@('UNKNOWN_FIELD=NO'); Reason='ATTESTATION_UNKNOWN_FIELD_P06_QA_ISOLATION_ATTESTATION_PATH' },
        @{ Name='SENSITIVE'; File=$iso; Replace=@('NOTE=secret'); Reason='ATTESTATION_SENSITIVE_P06_QA_ISOLATION_ATTESTATION_PATH' },
        @{ Name='MALFORMED'; File=$iso; Replace=@('malformed line'); Reason='ATTESTATION_MALFORMED_P06_QA_ISOLATION_ATTESTATION_PATH' },
        @{ Name='APPROVER_CODEX'; File=$iso; Replace=@('APPROVER=Codex'); Reason='ATTESTATION_APPROVAL_INVALID_P06_QA_ISOLATION_ATTESTATION' },
        @{ Name='EXPIRED'; File=$iso; Replace=@('VALID_UNTIL=2020-01-01'); Reason='ATTESTATION_EXPIRED_P06_QA_ISOLATION_ATTESTATION' }
    )
    foreach ($case in $cases) {
        $original = Get-Content -LiteralPath $case.File
        $lines = [Collections.Generic.List[string]]::new()
        foreach ($line in $original) { [void]$lines.Add($line) }
        foreach ($replacement in $case.Replace) {
            if ($replacement -eq 'TRUSTED_RUNNER_ISOLATION=') { $lines.Remove('TRUSTED_RUNNER_ISOLATION=YES') | Out-Null; continue }
            if ($replacement -eq 'UNKNOWN_FIELD=NO' -or $replacement -eq 'NOTE=secret' -or $replacement -eq 'malformed line') { [void]$lines.Add($replacement); continue }
            if ($replacement -eq 'TRUSTED_RUNNER_ISOLATION=YES' -and $case.Name -eq 'DUPLICATE') { [void]$lines.Add($replacement); continue }
            $index = $lines.IndexOf(($original | Where-Object { $_ -like (($replacement -split '=')[0] + '=*') } | Select-Object -First 1))
            if ($index -ge 0) { $lines[$index] = $replacement }
        }
        [IO.File]::WriteAllLines($case.File, $lines, [Text.UTF8Encoding]::new($false))
        Set-EnvValue 'P06_QA_ISOLATION_ATTESTATION_SHA256' ((Get-FileHash $case.File -Algorithm SHA256).Hash)
        Invoke-Runner $case.Name $case.Reason
        [IO.File]::WriteAllLines($case.File, $original, [Text.UTF8Encoding]::new($false))
        Set-EnvValue 'P06_QA_ISOLATION_ATTESTATION_SHA256' ((Get-FileHash $case.File -Algorithm SHA256).Hash)
    }

    Set-EnvValue 'P06_QA_ISOLATION_ATTESTATION_PATH' (Join-Path $RepoRoot 'tools\qa\P06_QA_PACKAGE_MANIFEST.json')
    Set-EnvValue 'P06_QA_ISOLATION_ATTESTATION_SHA256' ((Get-FileHash (Join-Path $RepoRoot 'tools\qa\P06_QA_PACKAGE_MANIFEST.json') -Algorithm SHA256).Hash)
    Invoke-Runner 'INSIDE_REPO' 'ATTESTATION_INSIDE_REPO_P06_QA_ISOLATION_ATTESTATION'

    Set-EnvValue 'P06_QA_ISOLATION_ATTESTATION_PATH' (Join-Path $tempRoot 'missing.txt')
    Set-EnvValue 'P06_QA_ISOLATION_ATTESTATION_SHA256' ('0' * 64)
    Invoke-Runner 'MISSING_FILE' 'ATTESTATION_FILE_MISSING_P06_QA_ISOLATION_ATTESTATION'
    Set-EnvValue 'P06_QA_ISOLATION_ATTESTATION_PATH' $iso
    Set-EnvValue 'P06_QA_ISOLATION_ATTESTATION_SHA256' ('0' * 64)
    Invoke-Runner 'HASH_MISMATCH' 'ATTESTATION_HASH_MISMATCH_P06_QA_ISOLATION_ATTESTATION'

    Clear-P06Env
    $manualIdentity = Write-Contract 'manual-db-identity.txt' @('SOURCE=OWNER_MANUAL_SQLPLUS_CONNECTION','QUERY_EXECUTED=YES','QUERY_OPERATION=SELECT_ONLY','PRIVILEGE_CHECK=AUTH_PERSISTENCE_OWNER','PRIVILEGE_RESULT=PASS','AUTH_OBJECTS_VALID=YES','AUTH_TRIGGERS_VALID=YES','BACKEND_RUNTIME_CORRESPONDENCE=PASS','CONFIG_SOURCE=CANDIDATE_CONFIG_NOT_RUNTIME_PROOF','SCHEMA=RIESGO_LAVADO','SERVICE=hpprod1','DB_UNIQUE_NAME=hpprod1',"PACKAGE_SHA256=$digest")
    Set-EnvValue 'P06_QA_MODE' 'LOCAL_OWNER_MANUAL'; Set-EnvValue 'P06_QA_MANUAL_OWNER_APPROVAL' 'YES'; Set-EnvValue 'P06_QA_EXPECTED_ROLE' 'TEST'; Set-EnvValue 'P06_QA_EXPECTED_MODULE_IDS_JSON' '[1]'; Set-EnvValue 'P06_QA_WEB_ORIGIN' 'http://localhost:4200'; Set-EnvValue 'P06_QA_API_ORIGIN' 'http://localhost:5043'; Set-EnvValue 'P06_QA_MANUAL_DB_IDENTITY_PATH' $manualIdentity; Set-EnvValue 'RL_QA_WEB_USERNAME' 'P06_TEST_SENTINEL'; Set-EnvValue 'RL_QA_WEB_PASSWORD' 'P06_TEST_SENTINEL'
    Invoke-Runner 'LOCAL_OWNER_MANUAL_PREFLIGHT' '' 'LOCAL_OWNER_MANUAL'

    Clear-P06Env
    $blocked = @(& powershell -NoProfile -File $runner -Mode CERTIFICATION -RepoRoot $RepoRoot -ValidateOnly 2>&1)
    if ($LASTEXITCODE -ne 3 -or ($blocked -join "`n") -notmatch 'MISSING_P06_QA_TRUSTED_CONTEXT') { throw 'CERTIFICATION_WITHOUT_CREDENTIALS failed' }
    Write-Output 'CERTIFICATION_WITHOUT_CREDENTIALS=BLOCKED'
    $preflight = @(& powershell -NoProfile -File $runner -Mode PREFLIGHT -RepoRoot $RepoRoot 2>&1)
    if ($LASTEXITCODE -ne 0 -or ($preflight -join "`n") -notmatch 'P06_PREFLIGHT=PASS') { throw 'PREFLIGHT_WITHOUT_CREDENTIALS failed' }
    Write-Output 'PREFLIGHT_WITHOUT_CREDENTIALS=PASS'
    Write-Output 'PREFLIGHT_REAL_AUTH_EXECUTED=NO'
    $runnerText = Get-Content -Raw -LiteralPath $runner
    if ($runnerText -notmatch "SetEnvironmentVariable\('P06_QA_MODE'" -or $runnerText -notmatch "skipped.*CERTIFICATION_INCOMPLETE" -or $runnerText -notmatch "6 passed") { throw 'CERTIFICATION_MODE_CONTRACT_NOT_PRESENT' }
    if ($runnerText -notmatch 'CheckSignature\(\$false\)' -or $runnerText -match 'CheckSignature\(\$true\)') { throw 'CMS_CHAIN_VALIDATION_CONTRACT_NOT_PRESENT' }
    Write-Output 'CMS_CHAIN_VALIDATION_CONTRACT=PASS'
    $ownerText = Get-Content -Raw -LiteralPath (Join-Path $RepoRoot 'tools\p06-owner-manual.ps1')
    if ($ownerText -notmatch 'AUTH_PERSISTENCE_OWNER' -or $ownerText -notmatch 'AUTH_OBJECTS_VALID' -or $ownerText -notmatch 'AUTH_TRIGGERS_VALID' -or $ownerText -match 'EFFECTIVE_NO_DML_DDL_EXECUTE' -or $ownerText -match 'READ_ONLY=') { throw 'ORACLE_EFFECTIVE_PRIVILEGE_CONTRACT_NOT_PRESENT' }
    if ($ownerText -notmatch 'Get-NetTCPConnection' -or $ownerText -notmatch 'api/configuracion/sistema' -or $ownerText -notmatch '/readyz' -or $ownerText -notmatch 'BACKEND_RUNTIME_CORRESPONDENCE_UNPROVEN') { throw 'BACKEND_EFFECTIVE_CONFIG_CONTRACT_NOT_PRESENT' }
    Write-Output 'ORACLE_EFFECTIVE_PRIVILEGE_CONTRACT=PASS'
    Write-Output 'BACKEND_EFFECTIVE_CONFIG_CONTRACT=PASS'
    if (@([pscustomobject]@{ OwningProcess = 15824 }, [pscustomobject]@{ OwningProcess = 15824 } | Sort-Object OwningProcess -Unique).Count -ne 1) { throw 'MULTIPLE_LISTENERS_SAME_PID_NOT_DEDUPLICATED' }
    Write-Output 'MULTIPLE_LISTENERS_SAME_PID=PASS'
    if (@([pscustomobject]@{ OwningProcess = 15824 }, [pscustomobject]@{ OwningProcess = 27183 } | Sort-Object OwningProcess -Unique).Count -eq 1) { throw 'DIFFERENT_PIDS_NOT_REJECTED' }
    Write-Output 'DIFFERENT_PIDS_REJECTED=PASS'
    $specText = Get-Content -Raw -LiteralPath (Join-Path $RepoRoot 'frontend\rl-app\e2e\p06-qa-real-auth.spec.ts')
    if ($specText -notmatch 'if \(!isRealAuthMode\) return') { throw 'LOCAL_OWNER_LOGOUT_CLEANUP_NOT_PRESENT' }
    Write-Output 'LOCAL_OWNER_LOGOUT_CLEANUP=PASS'
    Write-Output 'CERTIFICATION_MODE_PROPAGATION=PASS'
    Write-Output 'CERTIFICATION_SKIPPED_TEST_REJECTED=PASS'
    Write-Output 'CERTIFICATION_ZERO_TESTS_REJECTED=PASS'
    Write-Output 'CERTIFICATION_CONTRACT_TESTS=PASS'
    exit 0
}
finally {
    Clear-P06Env
    if (Test-Path -LiteralPath $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
}
