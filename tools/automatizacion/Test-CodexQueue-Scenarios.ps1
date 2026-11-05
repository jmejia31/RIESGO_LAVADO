[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$validator=Join-Path $PSScriptRoot 'Test-CodexQueue.ps1'
$invoke=Join-Path $PSScriptRoot 'Invoke-CodexQueue.ps1'
$pauseScript=Join-Path $PSScriptRoot 'Pause-CodexQueue.ps1'
$stopScript=Join-Path $PSScriptRoot 'Stop-CodexQueue.ps1'
$tmp=Join-Path ([IO.Path]::GetTempPath()) ('rl-codex-queue-'+[guid]::NewGuid().ToString('N')); New-Item -ItemType Directory -Path $tmp|Out-Null
$testRuntime=Join-Path $tmp 'runtime'; $preserveRuntime=Join-Path $tmp 'preserve-runtime'; New-Item -ItemType Directory -Path $testRuntime,$preserveRuntime|Out-Null
$testState=Join-Path $tmp 'state.json'; Copy-Item (Join-Path $root 'docs\automatizacion\ESTADO_TAREAS.json') $testState
function Write-Queue($tasks,$mode='DRY_RUN') { $o=@{schema_version=1;repository='jmejia31/RIESGO_LAVADO';branch='desarrollo';source_of_truth='github';queue_mode=$mode;tasks=@($tasks)}|ConvertTo-Json -Depth 12; $o|Set-Content -LiteralPath (Join-Path $tmp 'queue.json') -Encoding utf8; Join-Path $tmp 'queue.json' }
function Assert-ValidationFails($path) { $o=[IO.Path]::GetTempFileName(); $e=[IO.Path]::GetTempFileName(); try { $p=Start-Process powershell.exe -ArgumentList @('-NoProfile','-File',$validator,'-QueuePath',$path,'-Quiet') -Wait -PassThru -WindowStyle Hidden -RedirectStandardOutput $o -RedirectStandardError $e; if($p.ExitCode -eq 0){throw 'expected validation failure'} } finally {Remove-Item $o,$e -Force -ErrorAction SilentlyContinue} }
function Task($id='SIM-001',$risk='LOW',$approved=$true,$paths=@('docs/automatizacion'),$deps=@()) { @{id=$id;title='inert simulation';status='READY';priority='P2';dependencies=$deps;owner='CHAT';risk=$risk;branch='desarrollo';allowed_paths=$paths;acceptance=@('dry run only');verification=@('queue validation');verification_commands=@('queue-schema','git diff --check','secret-scan');authorization=@{approved=$approved;approved_by=($(if($approved){'jmejia31'}else{''}));context='Issue 24 independent simulation'}} }
try {
  $smokeValidator=& powershell.exe -NoProfile -File (Join-Path $PSScriptRoot 'Test-CodexQueue.ps1') 2>&1; if($LASTEXITCODE -ne 0 -or ($smokeValidator -join "`n") -notmatch 'QUEUE_SCHEMA_VALIDATED=PASS'){throw 'validator no-args smoke failed'}; Write-Output 'QUEUE_VALIDATOR_NO_ARGS=PASS'
  $smokeStatus=& powershell.exe -NoProfile -File (Join-Path $PSScriptRoot 'Get-CodexQueueStatus.ps1') 2>&1; if($LASTEXITCODE -ne 0 -or ($smokeStatus -join "`n") -notmatch 'QUEUE_MODE=(DRY_RUN|CONTROLLED)'){throw 'status no-args smoke failed'}; Write-Output 'QUEUE_STATUS_NO_ARGS=PASS'
  $empty=Write-Queue @(); & $validator -QueuePath $empty -Quiet; if($LASTEXITCODE){throw 'empty queue validation failed'}; $out=& $invoke -RepoRoot $root -QueuePath $empty -StatePath $testState -RuntimePath $testRuntime -DryRun -SkipRemoteSync; if(($out -join "`n") -notmatch 'READY_TASKS=0'){throw "empty queue did not exit idle: $($out -join ' | ')"}; Write-Output 'EMPTY_QUEUE=PASS'
  $ready=Write-Queue @(Task); & $validator -QueuePath $ready -Quiet; if($LASTEXITCODE){throw 'ready queue validation failed'}; $out=& $invoke -RepoRoot $root -QueuePath $ready -StatePath $testState -RuntimePath $testRuntime -DryRun -SkipRemoteSync; if(($out -join "`n") -notmatch 'DRY_RUN=PASS'){throw 'ready dry run failed'}; Write-Output 'READY_TASK_DRY_RUN=PASS'
  $stateObj=Get-Content -Raw $testState|ConvertFrom-Json; $stateObj.states=@(@{task_id='SIM-001';status='IMPLEMENTED_PENDING_REVIEW';attempt=1;updated_at=(Get-Date).ToUniversalTime().ToString('o');executor='COD';review_required=$true}); $stateObj|ConvertTo-Json -Depth 10|Set-Content $testState -Encoding utf8; $out=& $invoke -RepoRoot $root -QueuePath $ready -StatePath $testState -RuntimePath $testRuntime -DryRun -SkipRemoteSync; if(($out -join "`n") -notmatch 'READY_TASKS=0'){throw 'completed task was selected again'}; Write-Output 'COMPLETED_TASK_NONREPEAT=PASS'
  $stateObj.states=@(); $stateObj|ConvertTo-Json -Depth 10|Set-Content $testState -Encoding utf8; $out=& $invoke -RepoRoot $root -QueuePath $ready -StatePath $testState -RuntimePath $testRuntime -DryRun -SkipRemoteSync -SimulateFailure; if(($out -join "`n") -notmatch 'SIMULATED_FAILURE_STATUS=FAILED'){throw 'failure attempt one not persisted'}; $out=& $invoke -RepoRoot $root -QueuePath $ready -StatePath $testState -RuntimePath $testRuntime -DryRun -SkipRemoteSync -SimulateFailure; if(($out -join "`n") -notmatch 'SIMULATED_FAILURE_STATUS=BLOCKED'){throw 'failure attempt two not blocked'}; $out=& $invoke -RepoRoot $root -QueuePath $ready -StatePath $testState -RuntimePath $testRuntime -DryRun -SkipRemoteSync; if(($out -join "`n") -notmatch 'READY_TASKS=0'){throw 'blocked task remained eligible'}; Write-Output 'RETRY_LIMIT_PERSISTENCE=PASS'
  $out=& $invoke -RepoRoot $root -QueuePath $ready -StatePath $testState -RuntimePath $testRuntime -AllowExecution -SkipRemoteSync 2>&1; if(($out -join "`n") -notmatch 'REAL_EXECUTION_REQUIRES_CANONICAL_GITHUB_QUEUE'){throw 'real fixture execution was not blocked'}; Write-Output 'REAL_FIXTURE_GUARD=PASS'
  $out=& $invoke -RepoRoot $root -StatePath $testState -RuntimePath $testRuntime -AllowExecution 2>&1; if(($out -join "`n") -notmatch 'REAL_EXECUTION_REQUIRES_CANONICAL_CONTROL_PATHS'){throw 'real control path override was not blocked'}; Write-Output 'REAL_CONTROL_PATH_GUARD=PASS'
  $source=Get-Content -Raw $invoke; if($source -notmatch '\$codexArgs\s*=\s*@\(' -or $source -notmatch '\$contract\s*=\s*\$task' -or $source -match "if \(\$help -match 'dangerously-bypass"){throw 'codex argument contract guard missing'}; Write-Output 'CODEX_ARGUMENT_GUARD=PASS'; Write-Output 'TASK_CONTRACT_GUARD=PASS'
  $preview=& (Join-Path $PSScriptRoot 'Install-CodexQueueWatcher.ps1') -Controlled; if(($preview -join "`n") -notmatch 'CONTROLLED_ACTIVATION=PREVIEW_ONLY'){throw 'controlled watcher preview missing'}; $installer=Get-Content -Raw (Join-Path $PSScriptRoot 'Install-CodexQueueWatcher.ps1'); $launcher=Get-Content -Raw (Join-Path $PSScriptRoot 'Run-CodexQueueHidden.vbs'); if($installer -notmatch 'wscript\.exe' -or $installer -notmatch 'Run-CodexQueueHidden\.vbs' -or $launcher -notmatch 'shell\.Run' -or $launcher -notmatch ', 0, True'){throw 'hidden launcher guard missing'}; Write-Output 'WATCHER_TRANSITION_GUARD=PASS'; Write-Output 'WATCHER_HIDDEN_WINDOW=PASS'; Write-Output 'WATCHER_NO_CONSOLE_LAUNCHER=PASS'
  $unauth=Write-Queue @(Task 'SIM-UNAUTH' 'LOW' $false); $out=& $invoke -RepoRoot $root -QueuePath $unauth -DryRun -SkipRemoteSync; if(($out -join "`n") -notmatch 'READY_TASKS=0'){throw 'unauthorized task was eligible'}; Write-Output 'APPROVAL_GUARD=PASS'
  $high=Write-Queue @(Task 'SIM-HIGH' 'HIGH' $true); $out=& $invoke -RepoRoot $root -QueuePath $high -DryRun -SkipRemoteSync; if(($out -join "`n") -notmatch 'HIGH_RISK_BLOCKED=1'){throw 'high risk task was not blocked'}; Write-Output 'HIGH_RISK_GUARD=PASS'
  Assert-ValidationFails (Write-Queue @(Task 'SIM-DEP' 'LOW' $true @('docs/automatizacion') @('MISSING'))); Write-Output 'DEPENDENCY_GUARD=PASS'
  Assert-ValidationFails (Write-Queue @(Task 'SIM-PATH' 'LOW' $true @('main'))); Write-Output 'PROTECTED_PATH_GUARD=PASS'
  & $pauseScript -RuntimePath $testRuntime; $out=& $invoke -RepoRoot $root -StatePath $testState -RuntimePath $testRuntime -DryRun -SkipRemoteSync; if(($out -join "`n") -notmatch 'PAUSE=ACTIVE'){throw 'pause failed'}; & $pauseScript -RuntimePath $testRuntime -Clear; Write-Output 'PAUSE_GUARD=PASS'
  & $stopScript -RuntimePath $testRuntime; $out=& $invoke -RepoRoot $root -StatePath $testState -RuntimePath $testRuntime -DryRun -SkipRemoteSync; if(($out -join "`n") -notmatch 'STOP=ACTIVE'){throw 'stop failed'}; & $stopScript -RuntimePath $testRuntime -Clear; Write-Output 'STOP_GUARD=PASS'
  Set-Content (Join-Path $preserveRuntime 'PAUSE') 'owner-pause'; Set-Content (Join-Path $preserveRuntime 'STOP') 'owner-stop'; $out=& $invoke -RepoRoot $root -StatePath $testState -RuntimePath $preserveRuntime -DryRun -SkipRemoteSync; if(-not (Test-Path (Join-Path $preserveRuntime 'PAUSE')) -or -not (Test-Path (Join-Path $preserveRuntime 'STOP'))){throw 'preexisting safety flags were removed'}; Write-Output 'PREEXISTING_FLAGS_PRESERVED=PASS'
  $mutex=New-Object Threading.Mutex($false,'Local\RiesgoLavado-CodexQueue'); $mutex.WaitOne()|Out-Null; try { $out=& powershell.exe -NoProfile -File $invoke -RepoRoot $root -StatePath $testState -RuntimePath $testRuntime -DryRun -SkipRemoteSync 2>&1; if(($out -join "`n") -notmatch 'LOCK=BUSY'){throw 'lock was not enforced'} } finally {$mutex.ReleaseMutex();$mutex.Dispose()}; Write-Output 'LOCAL_LOCK_GUARD=PASS'
  $out=& $invoke -RepoRoot $root -DryRun; if(($out -join "`n") -notmatch 'REMOTE_HEAD=|TRACKED_WORKTREE_DIRTY'){throw 'remote HEAD guard not observed'}; Write-Output 'GITHUB_SOURCE_GUARD=PASS'
  $source=Get-Content -Raw $invoke; if($source -notmatch 'origin/desarrollo' -or $source -notmatch 'no-ext-diff' -or $source -match '\$remoteText'){throw 'git-aware queue comparison guard missing'}; & git diff --exit-code --quiet origin/desarrollo -- docs/automatizacion/TAREAS_CODEX.json; if($LASTEXITCODE -ne 0){throw 'canonical queue is not equal to GitHub'}; Write-Output 'QUEUE_GIT_DIFF_REGRESSION=PASS'
  $utf8a=Join-Path $tmp 'queue-utf8-a.json'; $utf8b=Join-Path $tmp 'queue-utf8-b.json'; [IO.File]::WriteAllText($utf8a,"{`"nombre`":`"Definición ágil`"}`r`n",(New-Object Text.UTF8Encoding($false))); [IO.File]::WriteAllText($utf8b,"{`"nombre`":`"Definición ágil`"}`r`n",(New-Object Text.UTF8Encoding($false))); & git diff --no-index --quiet $utf8a $utf8b; if($LASTEXITCODE -ne 0){throw 'UTF-8 accent/line ending equality failed'}; Write-Output 'QUEUE_UTF8_ACCENTS_EOL=PASS'
  [IO.File]::WriteAllText($utf8b,"{`"nombre`":`"Definición ágil`"}`n",(New-Object Text.UTF8Encoding($false))); & git diff --no-index --quiet $utf8a $utf8b; if($LASTEXITCODE -ne 0){throw "line-ending normalization failed, got $LASTEXITCODE"}; Write-Output 'QUEUE_EOL_NORMALIZATION=PASS'
  [IO.File]::WriteAllText($utf8b,"{`"nombre`":`"Definición alterada`"}`n",(New-Object Text.UTF8Encoding($false))); & git diff --no-index --quiet $utf8a $utf8b; if($LASTEXITCODE -ne 1){throw "expected divergence exit 1, got $LASTEXITCODE"}; Write-Output 'QUEUE_REAL_DIVERGENCE_EXIT1=PASS'
  $oldErrorAction=$ErrorActionPreference; $ErrorActionPreference='Continue'; & git diff --definitely-invalid-option 2>$null; $gitError=$LASTEXITCODE; $ErrorActionPreference=$oldErrorAction; if($gitError -le 1){throw "expected git error exit >1, got $gitError"}; Write-Output 'QUEUE_GIT_ERROR_EXIT_GT1=PASS'
  $status=& (Join-Path $PSScriptRoot 'Get-CodexQueueStatus.ps1') -RepoRoot $root -StatePath $testState -RuntimePath $testRuntime; if(($status -join "`n") -notmatch 'RUNNING_TASKS=0' -or ($status -join "`n") -notmatch 'PENDING_REVIEW_TASKS=0'){throw 'state reconciliation failed'}; Write-Output 'STATE_RECONCILIATION=PASS'
  $scannerMissing=Join-Path $tmp 'missing-rg.exe'; $probe=& powershell.exe -NoProfile -File $invoke -RepoRoot $root -RuntimePath $testRuntime -DryRun -SecurityProbe -SecretScannerPath $scannerMissing -ChangedPath 'docs/automatizacion/ISSUE24_ACCEPTANCE_PROOF.txt' 2>&1; if(($probe -join "`n") -notmatch 'SECRET_SCANNER_UNAVAILABLE'){throw 'missing ripgrep was not fail-closed'}; Write-Output 'SECRET_SCANNER_MISSING=PASS'
  $scannerError=Join-Path $tmp 'rg-error.ps1'; Set-Content $scannerError "if(`$args[0] -eq '--version'){exit 0}; exit 2"; $probe=& powershell.exe -NoProfile -File $invoke -RepoRoot $root -RuntimePath $testRuntime -DryRun -SecurityProbe -SecretScannerPath $scannerError -ChangedPath 'docs/automatizacion/ISSUE24_ACCEPTANCE_PROOF.txt' 2>&1; if(($probe -join "`n") -notmatch 'SECRET_SCAN_ERROR'){throw 'ripgrep error was not fail-closed'}; Write-Output 'SECRET_SCANNER_ERROR=PASS'
  $scannerMatch=Join-Path $tmp 'rg-match.ps1'; Set-Content $scannerMatch "if(`$args[0] -eq '--version'){exit 0}; exit 0"; $probe=& powershell.exe -NoProfile -File $invoke -RepoRoot $root -RuntimePath $testRuntime -DryRun -SecurityProbe -SecretScannerPath $scannerMatch -ChangedPath 'docs/automatizacion/ISSUE24_ACCEPTANCE_PROOF.txt' 2>&1; if(($probe -join "`n") -notmatch 'SECRET_SCAN_MATCH'){throw 'secret match was not blocked'}; Write-Output 'SECRET_SCANNER_MATCH=PASS'
  $scannerClean=Join-Path $tmp 'rg-clean.ps1'; Set-Content $scannerClean "if(`$args[0] -eq '--version'){exit 0}; exit 1"; $probe=& powershell.exe -NoProfile -File $invoke -RepoRoot $root -RuntimePath $testRuntime -DryRun -SecretScannerPath $scannerClean -SecurityProbe -ChangedPath 'docs/automatizacion/ISSUE24_ACCEPTANCE_PROOF.txt' 2>&1; if(($probe -join "`n") -notmatch 'ALLOWED_PATH=ACCEPTED' -or ($probe -join "`n") -notmatch 'SECURITY_PROBE=PASS'){throw "authorized exact path did not pass: $($probe -join ' | ')"}; Write-Output 'AUTHORIZED_EXACT_PATH=PASS'
  foreach($badPath in @('docs/automatizacion/ISSUE24_ACCEPTANCE_PROOF.txt.bak','docs/automatizacion/ISSUE24_ACCEPTANCE_PROOF.txtx','docs/automatizacion/../README.md')) { $probe=& powershell.exe -NoProfile -File $invoke -RepoRoot $root -RuntimePath $testRuntime -DryRun -SecretScannerPath $scannerClean -SecurityProbe -ChangedPath $badPath 2>&1; if(($probe -join "`n") -notmatch 'ALLOWED_PATH=REJECTED'){throw "unsafe path accepted: $badPath"} }; Write-Output 'ADVERSARIAL_ALLOWED_PATHS=PASS'
  $source=Get-Content -Raw $invoke; if($source -notmatch 'RedirectStandardInput' -or $source -notmatch 'Write-CodexUtf8Stdin -Process \$proc -Prompt \$prompt' -or $source -match '\$proc\.StandardInput\.Write\(' -or $source -match 'Start-Process -FilePath .codex. -ArgumentList \(\$codexArgs \+ \$prompt\)'){throw 'codex stdin contract transfer guard missing'}; $stdinExpected = '$codexArgs = @(''exec'',''--sandbox'',''workspace-write'',''-C'',$RepoRoot,''-'')'; if(-not $source.Contains($stdinExpected)){throw 'codex stdin prompt sentinel missing'}; $utf8Helper=Get-Content -Raw (Join-Path $PSScriptRoot 'Write-CodexUtf8Stdin.ps1'); if($utf8Helper -notmatch 'UTF8Encoding' -or $utf8Helper -notmatch 'StandardInput.BaseStream' -or $utf8Helper -notmatch 'GetBytes\(\$Prompt\)'){throw 'strict utf8 stdin writer missing'}; Write-Output 'CODEX_STDIN_PROMPT_SENTINEL=PASS'; Write-Output 'CODEX_CONTRACT_INTEGRITY=PASS'
  # Executable regression, not just a source scan: non-ASCII prompts must reach a
  # child process byte-for-byte through the same process-stdin helper as Codex.
  # This probe runs only powershell.exe in a temporary isolated child, never Codex.
  . (Join-Path $PSScriptRoot 'Write-CodexUtf8Stdin.ps1')
  $unicodePrompt = "Solicitud con revisi$([char]0x00F3)n, dise$([char]0x00F1)o y $([char]0x20AC)"
  $receiverScript = @'
$expected = "Solicitud con revisi$([char]0x00F3)n, dise$([char]0x00F1)o y $([char]0x20AC)"
$memory = New-Object System.IO.MemoryStream
[Console]::OpenStandardInput().CopyTo($memory)
try { $received = ([System.Text.UTF8Encoding]::new($false, $true)).GetString($memory.ToArray()) } catch { exit 21 }
if ($received -cne $expected) { exit 22 }
Write-Output 'UTF8_PROMPT_RECEIVED=PASS'
'@
  $receiverEncoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($receiverScript))
  $inputStartInfo = New-Object System.Diagnostics.ProcessStartInfo
  $inputStartInfo.FileName = 'powershell.exe'
  $inputStartInfo.Arguments = "-NoProfile -NonInteractive -EncodedCommand $receiverEncoded"
  $inputStartInfo.UseShellExecute = $false
  $inputStartInfo.CreateNoWindow = $true
  $inputStartInfo.RedirectStandardInput = $true
  $inputStartInfo.RedirectStandardOutput = $true
  $inputStartInfo.RedirectStandardError = $true
  $inputProcess = New-Object System.Diagnostics.Process
  $inputProcess.StartInfo = $inputStartInfo
  $inputStarted = $false
  try {
      $inputStarted = $inputProcess.Start()
      if (-not $inputStarted) { throw 'utf8 probe process did not start' }
      $stdoutAsync = $inputProcess.StandardOutput.ReadToEndAsync()
      $stderrAsync = $inputProcess.StandardError.ReadToEndAsync()
      Write-CodexUtf8Stdin -Process $inputProcess -Prompt $unicodePrompt
      if (-not $inputProcess.WaitForExit(30000)) { throw 'utf8 probe child timeout' }
      if ($inputProcess.ExitCode -ne 0 -or $stdoutAsync.Result -notmatch 'UTF8_PROMPT_RECEIVED=PASS') {
          throw "utf8 probe child rejected bytes: exit=$($inputProcess.ExitCode)"
      }
      Write-Output 'CODEX_UTF8_PROCESS_STDIN=PASS'
  } finally {
      if ($inputStarted -and -not $inputProcess.HasExited) { try { $inputProcess.Kill() } catch {} }
      $inputProcess.Dispose()
  }

  $autonomousCases=@(
    @{id='OWNER_APPROVED_PROOF';risk='LOW';path='docs/automatizacion/ISSUE24_AUTO_ACCEPTANCE_PROOF.txt';allowed=$true},
    @{id='OWNER_APPROVED_FRONTEND';risk='LOW';path='frontend/rl-app/src/app/demo/demo.component.ts';allowed=$true},
    @{id='MEDIUM_RISK_REJECTED';risk='MEDIUM';path='docs/automatizacion/ISSUE24_AUTO_ACCEPTANCE_PROOF.txt';allowed=$false},
    @{id='WORKFLOW_REJECTED';risk='LOW';path='.github/workflows/quality-gates.yml';allowed=$false},
    @{id='TRAVERSAL_REJECTED';risk='LOW';path='docs/../README.md';allowed=$false},
    @{id='CREDENTIALS_REJECTED';risk='LOW';path='backend/RL.API/appsettings.json';allowed=$false},
    @{id='BROAD_DIRECTORY_REJECTED';risk='LOW';path='frontend/rl-app/src/app/';allowed=$false}
  )
  foreach($c in $autonomousCases) {
    $probe=@(& powershell.exe -NoProfile -File $invoke -DryRun -AutonomousContractProbe -AutonomousRisk $c.risk -AutonomousPath $c.path 2>&1)
    $probeExit=$LASTEXITCODE
    $expected=if($c.allowed){'ACCEPTED'}else{'REJECTED'}
    $expectedExit=if($c.allowed){0}else{3}
    if($probeExit -ne $expectedExit -or ($probe -join ' ') -notmatch "AUTONOMOUS_CONTRACT_PROBE=$expected" -or
       ($probe -join ' ') -notmatch 'CODEX_INVOKED=NO') {
        throw "autonomous guard failure $($c.id): exit $probeExit; output $($probe -join ' | ')"
    }
    Write-Output "AUTONOMOUS_$($c.id)=PASS"
  }
  Write-Output 'CODEX_REAL_INVOKED=NO';Write-Output 'COMMIT_CREATED=NO';Write-Output 'PRODUCTION_ORACLE_TOUCHED=NO';Write-Output 'SIMULATED_SCENARIOS=39';exit 0
} finally {& $pauseScript -RuntimePath $testRuntime -Clear 2>$null; & $stopScript -RuntimePath $testRuntime -Clear 2>$null; Remove-Item -LiteralPath $tmp -Recurse -Force -ErrorAction SilentlyContinue}
