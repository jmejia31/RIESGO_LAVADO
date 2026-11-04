[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$validator=Join-Path $PSScriptRoot 'Test-CodexQueue.ps1'
$invoke=Join-Path $PSScriptRoot 'Invoke-CodexQueue.ps1'
$pauseScript=Join-Path $PSScriptRoot 'Pause-CodexQueue.ps1'
$stopScript=Join-Path $PSScriptRoot 'Stop-CodexQueue.ps1'
$tmp=Join-Path ([IO.Path]::GetTempPath()) ('rl-codex-queue-'+[guid]::NewGuid().ToString('N')); New-Item -ItemType Directory -Path $tmp|Out-Null
function Write-Queue($tasks,$mode='DRY_RUN') { $o=@{schema_version=1;repository='jmejia31/RIESGO_LAVADO';branch='desarrollo';source_of_truth='github';queue_mode=$mode;tasks=@($tasks)}|ConvertTo-Json -Depth 12; $o|Set-Content -LiteralPath (Join-Path $tmp 'queue.json') -Encoding utf8; Join-Path $tmp 'queue.json' }
function Assert-ValidationFails($path) { $o=[IO.Path]::GetTempFileName(); $e=[IO.Path]::GetTempFileName(); try { $p=Start-Process powershell.exe -ArgumentList @('-NoProfile','-File',$validator,'-QueuePath',$path,'-Quiet') -Wait -PassThru -WindowStyle Hidden -RedirectStandardOutput $o -RedirectStandardError $e; if($p.ExitCode -eq 0){throw 'expected validation failure'} } finally {Remove-Item $o,$e -Force -ErrorAction SilentlyContinue} }
function Task($id='SIM-001',$risk='LOW',$approved=$true,$paths=@('docs/automatizacion'),$deps=@()) { @{id=$id;title='inert simulation';status='READY';priority='P2';dependencies=$deps;owner='CHAT';risk=$risk;branch='desarrollo';allowed_paths=$paths;acceptance=@('dry run only');verification=@('queue validation');verification_commands=@('queue-schema','git diff --check','secret-scan');authorization=@{approved=$approved;approved_by=($(if($approved){'jmejia31'}else{''}));context='Issue 24 independent simulation'}} }
try {
  $empty=Write-Queue @(); & $validator -QueuePath $empty -Quiet; if($LASTEXITCODE){throw 'empty queue validation failed'}; $out=& $invoke -RepoRoot $root -QueuePath $empty -DryRun -SkipRemoteSync; if(($out -join "`n") -notmatch 'READY_TASKS=0'){throw 'empty queue did not exit idle'}; Write-Output 'EMPTY_QUEUE=PASS'
  $ready=Write-Queue @(Task); & $validator -QueuePath $ready -Quiet; if($LASTEXITCODE){throw 'ready queue validation failed'}; $out=& $invoke -RepoRoot $root -QueuePath $ready -DryRun -SkipRemoteSync; if(($out -join "`n") -notmatch 'DRY_RUN=PASS'){throw 'ready dry run failed'}; Write-Output 'READY_TASK_DRY_RUN=PASS'
  $unauth=Write-Queue @(Task 'SIM-UNAUTH' 'LOW' $false); $out=& $invoke -RepoRoot $root -QueuePath $unauth -DryRun -SkipRemoteSync; if(($out -join "`n") -notmatch 'READY_TASKS=0'){throw 'unauthorized task was eligible'}; Write-Output 'APPROVAL_GUARD=PASS'
  $high=Write-Queue @(Task 'SIM-HIGH' 'HIGH' $true); $out=& $invoke -RepoRoot $root -QueuePath $high -DryRun -SkipRemoteSync; if(($out -join "`n") -notmatch 'HIGH_RISK_BLOCKED=1'){throw 'high risk task was not blocked'}; Write-Output 'HIGH_RISK_GUARD=PASS'
  Assert-ValidationFails (Write-Queue @(Task 'SIM-DEP' 'LOW' $true @('docs/automatizacion') @('MISSING'))); Write-Output 'DEPENDENCY_GUARD=PASS'
  Assert-ValidationFails (Write-Queue @(Task 'SIM-PATH' 'LOW' $true @('main'))); Write-Output 'PROTECTED_PATH_GUARD=PASS'
  & $pauseScript; $out=& $invoke -RepoRoot $root -DryRun -SkipRemoteSync; if(($out -join "`n") -notmatch 'PAUSE=ACTIVE'){throw 'pause failed'}; & $pauseScript -Clear; Write-Output 'PAUSE_GUARD=PASS'
  & $stopScript; $out=& $invoke -RepoRoot $root -DryRun -SkipRemoteSync; if(($out -join "`n") -notmatch 'STOP=ACTIVE'){throw 'stop failed'}; & $stopScript -Clear; Write-Output 'STOP_GUARD=PASS'
  $mutex=New-Object Threading.Mutex($false,'Local\RiesgoLavado-CodexQueue'); $mutex.WaitOne()|Out-Null; try { $out=& powershell.exe -NoProfile -File $invoke -RepoRoot $root -DryRun -SkipRemoteSync 2>&1; if(($out -join "`n") -notmatch 'LOCK=BUSY'){throw 'lock was not enforced'} } finally {$mutex.ReleaseMutex();$mutex.Dispose()}; Write-Output 'LOCAL_LOCK_GUARD=PASS'
  $out=& $invoke -RepoRoot $root -DryRun; if(($out -join "`n") -notmatch 'REMOTE_HEAD=|TRACKED_WORKTREE_DIRTY'){throw 'remote HEAD guard not observed'}; Write-Output 'GITHUB_SOURCE_GUARD=PASS'
  Write-Output 'CODEX_REAL_INVOKED=NO';Write-Output 'COMMIT_CREATED=NO';Write-Output 'PRODUCTION_ORACLE_TOUCHED=NO';Write-Output 'SIMULATED_SCENARIOS=10';exit 0
} finally {& $pauseScript -Clear 2>$null; & $stopScript -Clear 2>$null; Remove-Item -LiteralPath $tmp -Recurse -Force -ErrorAction SilentlyContinue}
