[CmdletBinding()]
param(
    [string]$QueuePath = (Join-Path $PSScriptRoot '..\..\docs\automatizacion\TAREAS_CODEX.json'),
    [switch]$Quiet
)
$ErrorActionPreference = 'Stop'

function Fail([string]$Message) { throw "QUEUE_VALIDATION=FAIL: $Message" }
if (-not (Test-Path -LiteralPath $QueuePath -PathType Leaf)) { Fail "queue not found: $QueuePath" }
try { $queue = Get-Content -Raw -LiteralPath $QueuePath | ConvertFrom-Json } catch { Fail "invalid JSON: $($_.Exception.Message)" }
if ($queue.schema_version -ne 1) { Fail 'schema_version must be 1' }
if ($queue.repository -ne 'jmejia31/RIESGO_LAVADO') { Fail 'repository mismatch' }
if ($queue.branch -ne 'desarrollo') { Fail 'branch must be desarrollo' }
if ($queue.source_of_truth -ne 'github') { Fail 'source_of_truth must be github' }
if (@('DRY_RUN','CONTROLLED') -notcontains [string]$queue.queue_mode) { Fail 'queue_mode invalid' }
$tasks = @($queue.tasks)
$ids = @{}
$allowed = @('PENDING_AUTHORIZATION','READY','CLAIMED','RUNNING','IMPLEMENTED_PENDING_REVIEW','CHANGES_REQUESTED','APPROVED','BLOCKED','FAILED','CANCELLED')
$protected = @('main','production','PRUEBA_FORMULARIO','migrations','migration 54','oracle institucional')
foreach ($task in $tasks) {
    if ([string]::IsNullOrWhiteSpace($task.id)) { Fail 'task id missing' }
    if ($ids.ContainsKey($task.id)) { Fail "duplicate task id: $($task.id)" }
    $ids[$task.id] = $true
    if ($allowed -notcontains [string]$task.status) { Fail "invalid status for $($task.id)" }
    if ($task.branch -ne 'desarrollo') { Fail "task branch must be desarrollo: $($task.id)" }
    if (-not $task.allowed_paths -or -not $task.acceptance -or -not $task.verification) { Fail "task contract incomplete: $($task.id)" }
    if ($task.authorization.approved -eq $true -and [string]::IsNullOrWhiteSpace($task.authorization.approved_by)) { Fail "approved task has no approver: $($task.id)" }
    foreach ($path in @($task.allowed_paths)) {
        if ([IO.Path]::IsPathRooted([string]$path) -or ([string]$path -match '(^|[\\/])\.\.([\\/]|$)')) { Fail "unsafe allowed path: $path" }
        foreach ($word in $protected) { if ([string]$path -match [regex]::Escape($word)) { Fail "protected path: $path" } }
    }
}
foreach ($task in $tasks) { foreach ($dep in @($task.dependencies)) { if (-not $ids.ContainsKey($dep)) { Fail "missing dependency $dep for $($task.id)" }; if ($dep -eq $task.id) { Fail "self dependency $dep" } } }
$ready = @($tasks | Where-Object { $_.status -eq 'READY' -and $_.authorization.approved -eq $true })
if (-not $Quiet) {
    Write-Output "QUEUE_SCHEMA_VALIDATED=PASS"
    Write-Output "TASKS=$($tasks.Count)"
    Write-Output "READY_TASKS=$($ready.Count)"
    Write-Output "UNAUTHORIZED_READY_TASKS=$(@($tasks | Where-Object {$_.status -eq 'READY' -and $_.authorization.approved -ne $true}).Count)"
}
exit 0
