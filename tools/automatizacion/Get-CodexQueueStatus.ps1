[CmdletBinding()]
param([string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,[string]$StatePath='',[string]$RuntimePath='')
$ErrorActionPreference='Stop'
$queue=Get-Content -Raw (Join-Path $RepoRoot 'docs\automatizacion\TAREAS_CODEX.json')|ConvertFrom-Json
$stateFile=if([string]::IsNullOrWhiteSpace($StatePath)){Join-Path $RepoRoot 'docs\automatizacion\ESTADO_TAREAS.json'}else{$StatePath}
$state=Get-Content -Raw $stateFile|ConvertFrom-Json
$runtime=if([string]::IsNullOrWhiteSpace($RuntimePath)){Join-Path $env:LOCALAPPDATA 'RiesgoLavado\CodexQueue'}else{$RuntimePath}
$tasks=@($queue.tasks)
$ready=@($tasks|Where-Object{$_.status -eq 'READY' -and $_.authorization.approved -eq $true})
Write-Output "QUEUE_FILE=docs/automatizacion/TAREAS_CODEX.json"
Write-Output "QUEUE_MODE=$($queue.queue_mode)"
Write-Output "READY_TASKS=$($ready.Count)"
Write-Output "RUNNING_TASKS=$(@($state.states|Where-Object{$_.status -eq 'RUNNING'}).Count)"
Write-Output "PENDING_REVIEW_TASKS=$(@($state.states|Where-Object{$_.status -eq 'IMPLEMENTED_PENDING_REVIEW'}).Count)"
Write-Output "BLOCKED_TASKS=$(@($state.states|Where-Object{$_.status -eq 'BLOCKED'}).Count)"
Write-Output "PAUSE=$([bool](Test-Path (Join-Path $runtime 'PAUSE')))"
Write-Output "STOP=$([bool](Test-Path (Join-Path $runtime 'STOP')))"
Write-Output 'IDLE_CODEX_PROCESS=NO'
