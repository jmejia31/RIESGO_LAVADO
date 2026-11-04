[CmdletBinding()]
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,
    [string]$QueuePath = '',
    [switch]$DryRun = $true,
    [switch]$AllowExecution,
    [switch]$SkipRemoteSync
)
$ErrorActionPreference = 'Stop'
if ($AllowExecution) { $DryRun = $false }
if ([string]::IsNullOrWhiteSpace($QueuePath)) { $QueuePath = Join-Path $RepoRoot 'docs\automatizacion\TAREAS_CODEX.json' }
$queueRelative = 'docs/automatizacion/TAREAS_CODEX.json'
$validator = Join-Path $PSScriptRoot 'Test-CodexQueue.ps1'
$runtime = Join-Path $env:LOCALAPPDATA 'RiesgoLavado\CodexQueue'
New-Item -ItemType Directory -Force -Path $runtime | Out-Null

function Stop-Safely([string]$Reason) { Write-Output "EXECUTION_BLOCKED=$Reason"; Write-Output 'CODEX_INVOKED=NO'; exit 3 }
function Get-Sha256([string]$Path) { (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash.ToLowerInvariant() }
function Save-State([string]$TaskId,[string]$Status,[int]$Attempt=1) {
    if (-not $isRepoQueue) { return }
    $statePath=Join-Path $RepoRoot 'docs\automatizacion\ESTADO_TAREAS.json'
    $state=Get-Content -Raw $statePath|ConvertFrom-Json
    $now=(Get-Date).ToUniversalTime().ToString('o')
    $entry=@{task_id=$TaskId;status=$Status;attempt=$Attempt;updated_at=$now;executor='COD';review_required=$true}
    $existing=@($state.states|Where-Object{$_.task_id -ne $TaskId})
    $state.states=@($existing+$entry)
    $state|ConvertTo-Json -Depth 10|Set-Content -LiteralPath $statePath -Encoding utf8
    & git -C $RepoRoot diff --check; if($LASTEXITCODE){Stop-Safely 'STATE_DIFF_CHECK_FAILED'}
    & git -C $RepoRoot add -- 'docs/automatizacion/ESTADO_TAREAS.json'
    & git -C $RepoRoot commit -m "chore(automatizacion): estado $TaskId $Status" --quiet
    if($LASTEXITCODE){Stop-Safely 'STATE_COMMIT_FAILED'}
    & git -C $RepoRoot push origin desarrollo --quiet
    if($LASTEXITCODE){Stop-Safely 'STATE_PUSH_FAILED'}
}
function Invoke-AllowedVerifications($Task) {
    $commands=@($Task.verification_commands)
    if($commands.Count -eq 0){Stop-Safely 'VERIFICATION_COMMANDS_MISSING'}
    foreach($command in $commands){
        switch([string]$command){
            'git diff --check' { & git -C $RepoRoot diff --check; if($LASTEXITCODE){Stop-Safely 'GATE_DIFF_CHECK_FAILED'} }
            'queue-schema' { & $validator -QueuePath $QueuePath -Quiet; if($LASTEXITCODE){Stop-Safely 'GATE_QUEUE_SCHEMA_FAILED'} }
            'secret-scan' { $hit=rg -n --glob '*.md' --glob '*.json' --glob '*.txt' --glob '!tools/automatizacion/*.ps1' '(?i)(password\s*=|connectionstring|authorization:\s*bearer|set-cookie|api[_-]?key\s*=|BEGIN (RSA|OPENSSH|EC) PRIVATE KEY)' $RepoRoot 2>$null; if($hit){Stop-Safely 'GATE_SECRET_SCAN_FAILED'} }
            default { Stop-Safely "UNAPPROVED_VERIFICATION_COMMAND:$command" }
        }
    }
    Write-Output 'VERIFICATION_GATES=PASS'
}

$isRepoQueue = [IO.Path]::GetFullPath($QueuePath) -eq [IO.Path]::GetFullPath((Join-Path $RepoRoot $queueRelative.Replace('/','\')))
if ($isRepoQueue -and -not $SkipRemoteSync) {
    & git -C $RepoRoot fetch origin desarrollo --quiet
    if ($LASTEXITCODE -ne 0) { Stop-Safely 'GITHUB_FETCH_FAILED' }
    $dirty = (& git -C $RepoRoot status --porcelain --untracked-files=no | Out-String).Trim()
    if ($dirty) { Stop-Safely 'TRACKED_WORKTREE_DIRTY' }
    $head = (& git -C $RepoRoot rev-parse HEAD).Trim()
    $remote = (& git -C $RepoRoot rev-parse origin/desarrollo).Trim()
    if ($head -ne $remote) {
        & git -C $RepoRoot pull --ff-only origin desarrollo --quiet
        if ($LASTEXITCODE -ne 0) { Stop-Safely 'REMOTE_HEAD_DIVERGED_OR_PULL_FAILED' }
        $head = (& git -C $RepoRoot rev-parse HEAD).Trim()
        $remote = (& git -C $RepoRoot rev-parse origin/desarrollo).Trim()
    }
    if ($head -ne $remote) { Stop-Safely 'REMOTE_HEAD_NOT_CONFIRMED' }
    $remoteQueue = Join-Path $runtime 'remote-queue.json'
    & git -C $RepoRoot show "origin/desarrollo:$queueRelative" | Set-Content -LiteralPath $remoteQueue -Encoding utf8
    if ($LASTEXITCODE -ne 0) { Stop-Safely 'REMOTE_QUEUE_NOT_FOUND' }
    if ((Get-Sha256 $QueuePath) -ne (Get-Sha256 $remoteQueue)) { Stop-Safely 'LOCAL_QUEUE_DIFFERS_FROM_GITHUB' }
    Write-Output "REMOTE_HEAD=$remote"
    Write-Output "QUEUE_SHA256=$(Get-Sha256 $QueuePath)"
} else { Write-Output 'QUEUE_SOURCE=FIXTURE_OR_SYNC_SKIPPED' }

& $validator -QueuePath $QueuePath -Quiet
if ($LASTEXITCODE -ne 0) { Stop-Safely 'QUEUE_SCHEMA_INVALID' }
$pause = Join-Path $runtime 'PAUSE'; $stop = Join-Path $runtime 'STOP'
if (Test-Path $stop) { Write-Output 'STOP=ACTIVE'; Write-Output 'CODEX_INVOKED=NO'; exit 0 }
if (Test-Path $pause) { Write-Output 'PAUSE=ACTIVE'; Write-Output 'CODEX_INVOKED=NO'; exit 0 }
$mutex = New-Object Threading.Mutex($false, 'Local\RiesgoLavado-CodexQueue')
if (-not $mutex.WaitOne(0)) { Write-Output 'LOCK=BUSY'; Write-Output 'CODEX_INVOKED=NO'; exit 0 }
try {
    $queue = Get-Content -Raw -LiteralPath $QueuePath | ConvertFrom-Json
    $tasks = @($queue.tasks)
    $ready = @($tasks | Where-Object { $_.status -eq 'READY' -and $_.authorization.approved -eq $true })
    $eligible = @($ready | Where-Object {
        $depsOk = $true
        foreach($dep in @($_.dependencies)) { $d=$tasks | Where-Object {$_.id -eq $dep}; if($d.status -ne 'APPROVED') {$depsOk=$false} }
        $depsOk -and $_.risk -notin @('HIGH','CRITICAL')
    })
    $highRisk = @($ready | Where-Object {$_.risk -in @('HIGH','CRITICAL')})
    if ($highRisk.Count -gt 0) { Write-Output "HIGH_RISK_BLOCKED=$($highRisk.Count)" }
    if ($eligible.Count -eq 0) { Write-Output "READY_TASKS=$($ready.Count)"; Write-Output 'IDLE_CODEX_PROCESS=NO'; Write-Output 'CODEX_INVOKED=NO'; exit 0 }
    $task = $eligible | Sort-Object priority,id | Select-Object -First 1
    Write-Output "TASK_SELECTED=$($task.id)"
    if ($DryRun -or -not $AllowExecution) { Write-Output 'DRY_RUN=PASS'; Write-Output 'CODEX_INVOKED=NO'; Write-Output 'COMMIT_CREATED=NO'; exit 0 }
    if ($queue.queue_mode -ne 'CONTROLLED') { Stop-Safely 'CONTROLLED_MODE_REQUIRED' }
    $help = & codex exec --help 2>&1 | Out-String
    if ($help -notmatch '--sandbox') { Stop-Safely 'CODEX_SANDBOX_UNAVAILABLE' }
    if ($help -match 'dangerously-bypass|--yolo') { Stop-Safely 'UNSAFE_CODEX_CAPABILITY_DETECTED' }
    $prompt = "Execute only task $($task.id). Read repository instructions. Allowed paths: $($task.allowed_paths -join ', '). Do not use YOLO, bypass approvals, Oracle, production, main, migration 54, secrets, or unrelated files. Do not self-approve. Return evidence and leave review state pending."
    Save-State $task.id 'CLAIMED'
    Save-State $task.id 'RUNNING'
    $proc = Start-Process -FilePath 'codex' -ArgumentList @('exec','--sandbox','workspace-write','-C',$RepoRoot,$prompt) -Wait -PassThru -NoNewWindow
    Write-Output "CODEX_EXIT_CODE=$($proc.ExitCode)"
    if ($proc.ExitCode -ne 0) { Stop-Safely 'CODEX_FAILED' }
    Invoke-AllowedVerifications $task
    & git -C $RepoRoot diff --check; if($LASTEXITCODE){Stop-Safely 'DIFF_CHECK_FAILED'}
    $statusLines=@(& git -C $RepoRoot status --porcelain --untracked-files=all)
    $changed=@(); foreach($line in $statusLines){$path=($line -replace '^..\s+','').Trim('"'); if($path){$changed+=$path}}
    foreach($path in $changed){$allowed=$false;foreach($pattern in @($task.allowed_paths)){if($path -like ($pattern.TrimEnd('/')+'*')){$allowed=$true}};if(-not $allowed){Stop-Safely "UNAUTHORIZED_CHANGED_PATH:$path"}}
    if($changed.Count -eq 0){Stop-Safely 'NO_CHANGES_TO_PUBLISH'}
    & git -C $RepoRoot add -- $changed
    & git -C $RepoRoot commit -m "feat(automatizacion): tarea $($task.id)" --quiet
    if($LASTEXITCODE){Stop-Safely 'TASK_COMMIT_FAILED'}
    & git -C $RepoRoot push origin desarrollo --quiet
    if($LASTEXITCODE){Stop-Safely 'TASK_PUSH_FAILED'}
    Save-State $task.id 'IMPLEMENTED_PENDING_REVIEW'
    Write-Output 'RESULT_STATE=IMPLEMENTED_PENDING_REVIEW'
    Write-Output 'AUTO_APPROVE=NO'
    Write-Output 'PUBLISH=PASS'
} finally { $mutex.ReleaseMutex(); $mutex.Dispose() }
