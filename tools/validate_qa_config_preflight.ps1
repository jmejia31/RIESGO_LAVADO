param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
$workflowPath = Join-Path $RepositoryRoot '.github/workflows/qa-config-preflight.yml'
if (-not (Test-Path -LiteralPath $workflowPath -PathType Leaf)) {
    throw "QA preflight workflow not found: $workflowPath"
}

$workflow = Get-Content -LiteralPath $workflowPath -Raw -Encoding UTF8
$required = @(
    'workflow_dispatch:',
    'permissions:',
    'contents: read',
    'timeout-minutes:',
    'cancel-in-progress: false',
    'QA_INSTITUTIONAL_EXECUTION: NOT_EXECUTED',
    'QA_AUTONOMOUS_MUTATIONS: DISABLED',
    'Test-CodexQueue.ps1',
    'validate_repository_structure.ps1',
    'validate_database_scripts.ps1',
    'validate_documentation_links.ps1',
    'npm run lint',
    'npm run build',
    'git diff --check'
)
foreach ($marker in $required) {
    if ($workflow.IndexOf($marker, [System.StringComparison]::Ordinal) -lt 0) {
        throw "QA preflight is missing required fail-closed marker: $marker"
    }
}

if ($workflow -match '(?im)^\s*(push|pull_request|schedule):') {
    throw 'QA preflight must remain manual-only; an automatic trigger was found.'
}
if ($workflow -match '(?i)\bsecrets\.|ConnectionStrings__Oracle|ORACLE_PASSWORD|sqlplus|ALTER\s+SYSTEM|DROP\s+TABLE|TRUNCATE\s+TABLE') {
    throw 'QA preflight contains an institutional credential, Oracle connection, or destructive SQL reference.'
}
if ($workflow -notmatch '(?im)^\s*permissions:\s*\r?\n\s+contents:\s+read\s*$') {
    throw 'QA preflight permissions are not restricted to contents: read.'
}
if ($workflow -notmatch '(?im)^\s*timeout-minutes:\s*[1-9][0-9]*\s*$') {
    throw 'QA preflight must define a positive job timeout.'
}

Write-Output 'QA_WORKFLOW_STATIC_CONTRACT=PASS'
Write-Output 'QA_WORKFLOW_MANUAL_ONLY=PASS'
Write-Output 'QA_WORKFLOW_FAIL_CLOSED=PASS'
Write-Output 'QA_WORKFLOW_NO_INSTITUTIONAL_CONNECTION=PASS'
