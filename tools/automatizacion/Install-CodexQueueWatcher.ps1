[CmdletBinding()]
param([switch]$Apply,[switch]$Controlled,[string]$OwnerAuthorization='',[int]$Minutes=5)
$ErrorActionPreference='Stop'
$repo=(Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$taskName='RiesgoLavado-CodexQueue'
$script=Join-Path $PSScriptRoot 'Invoke-CodexQueue.ps1'
$mode='DRY_RUN'
$executorSwitch='-DryRun'
if($Controlled) {
    if(-not $Apply) {
        Write-Output 'CONTROLLED_ACTIVATION=PREVIEW_ONLY'
        Write-Output 'WATCHER_INSTALLED=NO'
        Write-Output 'DRY_RUN=PASS'
        exit 0
    }
    if($OwnerAuthorization -ne 'jmejia31:ISSUE24-CONTROLLED-ACCEPTANCE') {
        throw 'Explicit owner authorization required for controlled watcher activation'
    }
    $mode='CONTROLLED'
    $executorSwitch='-AllowExecution'
}
if($Minutes -lt 1 -or $Minutes -gt 60) { throw 'Minutes must be 1..60' }
$quote=[char]34
$action="powershell.exe -NoProfile -File $quote$script$quote -RepoRoot $quote$repo$quote $executorSwitch"
if(-not $Apply) {
    Write-Output 'WATCHER_INSTALLED=NO'
    Write-Output 'DRY_RUN=PASS'
    Write-Output "SCHEDULE_PREVIEW=$taskName every $Minutes minutes as current user mode=$mode"
    exit 0
}

if($Controlled) {
    if([IO.Path]::GetFileName($repo.TrimEnd('\')) -ne 'RIESGO_LAVADO_CODEX_WORKER') {
        throw 'ISOLATED_WORKER_REQUIRED'
    }
    $branch=(& git -C $repo branch --show-current).Trim()
    if($LASTEXITCODE -ne 0 -or $branch -ne 'desarrollo') { throw 'DEVELOPMENT_BRANCH_REQUIRED' }
    $dirty=@(& git -C $repo status --porcelain --untracked-files=all)
    if($LASTEXITCODE -ne 0 -or $dirty.Count -ne 0) { throw 'WORKTREE_NOT_CLEAN' }

    & git -C $repo fetch origin desarrollo --quiet
    if($LASTEXITCODE -ne 0) { throw 'GITHUB_FETCH_FAILED' }
    & git -C $repo pull --ff-only origin desarrollo --quiet
    if($LASTEXITCODE -ne 0) { throw 'GITHUB_FAST_FORWARD_REQUIRED' }
    $localHead=(& git -C $repo rev-parse HEAD).Trim()
    $remoteHead=(& git -C $repo rev-parse origin/desarrollo).Trim()
    if($localHead -ne $remoteHead) { throw 'REMOTE_HEAD_DIVERGED' }
    $dirty=@(& git -C $repo status --porcelain --untracked-files=all)
    if($LASTEXITCODE -ne 0 -or $dirty.Count -ne 0) { throw 'WORKTREE_CHANGED_AFTER_PULL' }

    $queue=Get-Content -Raw -LiteralPath (Join-Path $repo 'docs\automatizacion\TAREAS_CODEX.json') | ConvertFrom-Json
    $state=Get-Content -Raw -LiteralPath (Join-Path $repo 'docs\automatizacion\ESTADO_TAREAS.json') | ConvertFrom-Json
    if($queue.queue_mode -ne 'CONTROLLED') { throw 'CANONICAL_CONTROLLED_QUEUE_REQUIRED' }
    if($state.controls.auto_approve -ne $false -or $state.controls.dry_run_default -ne $true -or
       $state.controls.pause -ne $false -or $state.controls.stop -ne $false -or
       [int]$state.controls.max_parallel -ne 1 -or [int]$state.controls.max_attempts -gt 2) {
        throw 'UNSAFE_PERSISTED_CONTROLS'
    }
    $runtime=Join-Path $env:LOCALAPPDATA 'RiesgoLavado\CodexQueue'
    if((Test-Path (Join-Path $runtime 'PAUSE')) -or (Test-Path (Join-Path $runtime 'STOP'))) {
        throw 'LOCAL_PAUSE_OR_STOP_ACTIVE'
    }
    & powershell.exe -NoProfile -File (Join-Path $PSScriptRoot 'Test-CodexQueue.ps1') -Quiet
    if($LASTEXITCODE -ne 0) { throw 'QUEUE_SCHEMA_INVALID' }
    $check=@(& powershell.exe -NoProfile -File $script -RepoRoot $repo -DryRun)
    if($LASTEXITCODE -ne 0 -or ($check -join ' ') -notmatch 'LOCAL_QUEUE_MATCHES_GITHUB=YES' -or
        ($check -join ' ') -notmatch 'CODEX_INVOKED=NO') {
        throw 'GITHUB_SOURCE_PREFLIGHT_FAILED'
    }
    if(-not (Get-Command codex.exe -ErrorAction SilentlyContinue) -or
        -not (Get-Command rg -ErrorAction SilentlyContinue)) { throw 'CODEX_OR_SCANNER_UNAVAILABLE' }
    $help=& codex exec --help 2>&1 | Out-String
    if($LASTEXITCODE -ne 0 -or $help -notmatch '--sandbox') { throw 'CODEX_SANDBOX_UNAVAILABLE' }
    Write-Output 'AUTONOMOUS_PREFLIGHT=PASS'
    Write-Output "WORKER_SHA=$localHead"
    Write-Output 'SCOPE=LOW_RISK_OWNER_APPROVED_EXACT_FILES_ONLY'
}
$existing=Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
if($existing -and $Controlled) {
    $argsText=($existing.Actions | ForEach-Object { $_.Arguments }) -join ' '
    if($argsText -notmatch '-DryRun' -or $argsText -match '-AllowExecution' -or
       $argsText -notmatch 'RIESGO_LAVADO_CODEX_WORKER') {
        throw 'PREEXISTING_WATCHER_NOT_SAFE_FOR_REPLACEMENT'
    }
}
$replaced=$false
try {
    if($existing) { Disable-ScheduledTask -TaskName $taskName -ErrorAction Stop | Out-Null }
    & schtasks.exe /Create /TN $taskName /TR $action /SC MINUTE /MO $Minutes /F /RU $env:USERNAME
    if($LASTEXITCODE -ne 0) { throw "TASK_SCHEDULER_CREATE_FAILED:$LASTEXITCODE" }
    $installed=Get-ScheduledTask -TaskName $taskName -ErrorAction Stop
    $argsText=($installed.Actions | ForEach-Object { $_.Arguments }) -join ' '
    if($installed.State -ne 'Ready' -or
       $argsText -notmatch [regex]::Escape($executorSwitch) -or
       $argsText -notmatch 'RIESGO_LAVADO_CODEX_WORKER') {
        throw 'WATCHER_INSTALL_POSTCONDITION_FAILED'
    }
    if($Controlled -and $argsText -match '-DryRun') { throw 'CONTROLLED_WATCHER_ARGUMENTS_INVALID' }
    if(-not $Controlled -and $argsText -match '-AllowExecution') { throw 'DRYRUN_WATCHER_ARGUMENTS_INVALID' }
    $replaced=$true
    Write-Output 'WATCHER_INSTALLED=YES'
    Write-Output 'WATCHER_ENABLED=YES'
    Write-Output "WATCHER_MODE=$mode"
    Write-Output 'SANDBOX_MODE=workspace-write'
    Write-Output 'QUEUE_EMPTY_EXIT=ENFORCED'
    if($Controlled) { Write-Output 'AUTONOMOUS_OWNER_APPROVED_LOW_RISK_ONLY=YES' }
} finally {
    if(-not $replaced) {
        try { Disable-ScheduledTask -TaskName $taskName -ErrorAction Stop | Out-Null } catch {}
        Write-Output 'WATCHER_FAIL_CLOSED=DISABLED'
    }
}
