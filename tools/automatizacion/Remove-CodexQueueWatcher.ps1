[CmdletBinding()]
param([switch]$Apply)
$taskName='RiesgoLavado-CodexQueue'
if(-not $Apply){Write-Output 'WATCHER_REMOVED=NO';Write-Output 'DRY_RUN=PASS';exit 0}
& schtasks.exe /Delete /TN $taskName /F
if($LASTEXITCODE -ne 0){throw "Task Scheduler removal failed: $LASTEXITCODE"}
Write-Output 'WATCHER_REMOVED=YES'
