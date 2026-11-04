[CmdletBinding()]
param([string]$RepoRoot='',[string]$StatePath='',[string]$RuntimePath='')
$ErrorActionPreference='Stop'
if([string]::IsNullOrWhiteSpace($RepoRoot)){if([string]::IsNullOrWhiteSpace($PSScriptRoot)){throw 'Cannot resolve canonical repository path'};$RepoRoot=(Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path}
$queue=Get-Content -Raw (Join-Path $RepoRoot 'docs\automatizacion\TAREAS_CODEX.json')|ConvertFrom-Json
$stateFile=if([string]::IsNullOrWhiteSpace($StatePath)){Join-Path $RepoRoot 'docs\automatizacion\ESTADO_TAREAS.json'}else{$StatePath}
$state=Get-Content -Raw $stateFile|ConvertFrom-Json
$runtime=if([string]::IsNullOrWhiteSpace($RuntimePath)){Join-Path $env:LOCALAPPDATA 'RiesgoLavado\CodexQueue'}else{$RuntimePath}
$tasks=@($queue.tasks)
$ready=@($tasks|Where-Object {
    $task=$_
    $persisted=@($state.states|Where-Object{$_.task_id -eq $task.id}|Select-Object -Last 1)
    $entry=if($persisted.Count -gt 0){$persisted[0]}else{$null}
    $blocked=($entry -and $entry.status -in @('CLAIMED','RUNNING','IMPLEMENTED_PENDING_REVIEW','APPROVED','BLOCKED'))
    $exhausted=($entry -and $entry.status -eq 'FAILED' -and [int]$entry.attempt -ge 2)
    $depsOk=$true
    foreach($dep in @($task.dependencies)){
        $upstream=@($tasks|Where-Object{$_.id -eq $dep})
        if($upstream.Count -ne 1 -or $upstream[0].status -ne 'APPROVED'){$depsOk=$false}
    }
    $task.status -eq 'READY' -and $task.authorization.approved -eq $true -and
    $task.risk -eq 'LOW' -and -not $blocked -and -not $exhausted -and $depsOk
})
Write-Output "QUEUE_FILE=docs/automatizacion/TAREAS_CODEX.json"
Write-Output "QUEUE_MODE=$($queue.queue_mode)"
Write-Output "READY_TASKS=$($ready.Count)"
Write-Output "RUNNING_TASKS=$(@($state.states|Where-Object{$_.status -eq 'RUNNING'}).Count)"
Write-Output "PENDING_REVIEW_TASKS=$(@($state.states|Where-Object{$_.status -eq 'IMPLEMENTED_PENDING_REVIEW'}).Count)"
Write-Output "BLOCKED_TASKS=$(@($state.states|Where-Object{$_.status -eq 'BLOCKED'}).Count)"
Write-Output "PAUSE=$([bool](Test-Path (Join-Path $runtime 'PAUSE')))"
Write-Output "STOP=$([bool](Test-Path (Join-Path $runtime 'STOP')))"
Write-Output 'IDLE_CODEX_PROCESS=NO'
