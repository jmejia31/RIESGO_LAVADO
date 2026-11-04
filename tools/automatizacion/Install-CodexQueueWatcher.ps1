[CmdletBinding()]
param([switch]$Apply,[switch]$Controlled,[string]$OwnerAuthorization='',[int]$Minutes=5)
$ErrorActionPreference='Stop'
$repo=(Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$taskName='RiesgoLavado-CodexQueue'
$script=Join-Path $PSScriptRoot 'Invoke-CodexQueue.ps1'
$mode='DRY_RUN'; $switch='-DryRun'
if($Controlled){
  if(-not $Apply){Write-Output 'CONTROLLED_ACTIVATION=PREVIEW_ONLY';Write-Output 'WATCHER_INSTALLED=NO';Write-Output 'DRY_RUN=PASS';exit 0}
  if($OwnerAuthorization -ne 'jmejia31:ISSUE24-CONTROLLED-ACCEPTANCE'){throw 'Explicit owner authorization required for controlled watcher activation'}
  $mode='CONTROLLED'; $switch='-AllowExecution'
}
$action="powershell.exe -NoProfile -File `"$script`" -RepoRoot `"$repo`" $switch"
if(-not $Apply){Write-Output 'WATCHER_INSTALLED=NO';Write-Output 'DRY_RUN=PASS';Write-Output "SCHEDULE_PREVIEW=$taskName every $Minutes minutes as current user mode=$mode";exit 0}
if($Minutes -lt 1 -or $Minutes -gt 60){throw 'Minutes must be 1..60'}
& schtasks.exe /Create /TN $taskName /TR $action /SC MINUTE /MO $Minutes /F /RU $env:USERNAME
if($LASTEXITCODE -ne 0){throw "Task Scheduler creation failed: $LASTEXITCODE"}
Write-Output 'WATCHER_INSTALLED=YES';Write-Output 'WATCHER_ENABLED=YES';Write-Output "WATCHER_MODE=$mode";Write-Output 'SANDBOX_MODE=workspace-write';Write-Output 'QUEUE_EMPTY_EXIT=ENFORCED'
