[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$validator=Join-Path $PSScriptRoot 'Test-CodexQueue.ps1'
$invoke=Join-Path $PSScriptRoot 'Invoke-CodexQueue.ps1'
$tmp=Join-Path ([IO.Path]::GetTempPath()) ('rl-codex-queue-'+[guid]::NewGuid().ToString('N')); New-Item -ItemType Directory -Path $tmp|Out-Null
try {
  $q=Join-Path $tmp 'queue.json'; Copy-Item (Join-Path $root 'docs\automatizacion\TAREAS_CODEX.json') $q
  & $validator -QueuePath $q; if($LASTEXITCODE){throw 'empty queue validation failed'}
  $out=& $invoke -RepoRoot $root -QueuePath $q -DryRun; if(($out -join "`n") -notmatch 'READY_TASKS=0'){throw 'empty queue did not exit idle'}
  $ready=@{schema_version=1;repository='jmejia31/RIESGO_LAVADO';branch='desarrollo';source_of_truth='github';queue_mode='DRY_RUN';tasks=@(@{id='SIM-001';title='simulated';status='READY';priority='P2';dependencies=@();owner='CHAT';risk='LOW';branch='desarrollo';allowed_paths=@('docs/automatizacion');acceptance=@('dry run');verification=@('validator');authorization=@{approved=$true;approved_by='jmejia31';context='Issue 24 simulation'}})}|ConvertTo-Json -Depth 10
  $ready|Set-Content -LiteralPath $q -Encoding utf8
  & $validator -QueuePath $q -Quiet; if($LASTEXITCODE){throw 'ready queue validation failed'}
  $out=& $invoke -RepoRoot $root -QueuePath $q -DryRun; if(($out -join "`n") -notmatch 'DRY_RUN=PASS'){throw 'ready dry run failed'}
  Write-Output 'EMPTY_QUEUE=PASS';Write-Output 'READY_TASK_DRY_RUN=PASS';Write-Output 'CODEX_REAL_INVOKED=NO';Write-Output 'COMMIT_CREATED=NO';Write-Output 'PRODUCTION_ORACLE_TOUCHED=NO';Write-Output 'SIMULATED_SCENARIOS=2';exit 0
} finally {Remove-Item -LiteralPath $tmp -Recurse -Force -ErrorAction SilentlyContinue}
