[CmdletBinding()]
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,
    [string]$QueuePath = '',
    [switch]$DryRun = $true,
    [switch]$AllowExecution,
    [switch]$Once
)
$ErrorActionPreference = 'Stop'
if ($AllowExecution) { $DryRun = $false }
if ([string]::IsNullOrWhiteSpace($QueuePath)) { $QueuePath = Join-Path $RepoRoot 'docs\automatizacion\TAREAS_CODEX.json' }
$validator = Join-Path $PSScriptRoot 'Test-CodexQueue.ps1'
$runtime = Join-Path $env:LOCALAPPDATA 'RiesgoLavado\CodexQueue'
New-Item -ItemType Directory -Force -Path $runtime | Out-Null
& $validator -QueuePath $queuePath -Quiet
if ($LASTEXITCODE -ne 0) { exit 2 }
$pause = Join-Path $runtime 'PAUSE'
$stop = Join-Path $runtime 'STOP'
if (Test-Path $stop) { Write-Output 'STOP=ACTIVE'; Write-Output 'CODEX_INVOKED=NO'; exit 0 }
if (Test-Path $pause) { Write-Output 'PAUSE=ACTIVE'; Write-Output 'CODEX_INVOKED=NO'; exit 0 }
$mutex = New-Object Threading.Mutex($false, 'Local\RiesgoLavado-CodexQueue')
if (-not $mutex.WaitOne(0)) { Write-Output 'LOCK=BUSY'; Write-Output 'CODEX_INVOKED=NO'; exit 0 }
try {
    $queue = Get-Content -Raw -LiteralPath $queuePath | ConvertFrom-Json
    $tasks = @($queue.tasks)
    $ready = @($tasks | Where-Object { $_.status -eq 'READY' -and $_.authorization.approved -eq $true })
    $eligible = @($ready | Where-Object {
        $depsOk = $true
        foreach($dep in @($_.dependencies)) { $d=$tasks | Where-Object {$_.id -eq $dep}; if($d.status -ne 'APPROVED') {$depsOk=$false} }
        $depsOk
    })
    if ($eligible.Count -eq 0) { Write-Output 'READY_TASKS=0'; Write-Output 'IDLE_CODEX_PROCESS=NO'; Write-Output 'CODEX_INVOKED=NO'; exit 0 }
    $task = $eligible | Sort-Object priority,id | Select-Object -First 1
    Write-Output "TASK_SELECTED=$($task.id)"
    if ($DryRun -or -not $AllowExecution) { Write-Output 'DRY_RUN=PASS'; Write-Output 'CODEX_INVOKED=NO'; Write-Output 'COMMIT_CREATED=NO'; exit 0 }
    if ($queue.queue_mode -ne 'CONTROLLED') { throw 'controlled queue mode required for execution' }
    $help = & codex exec --help 2>&1 | Out-String
    if ($help -notmatch '--sandbox') { throw 'codex exec lacks sandbox option' }
    $prompt = "Execute only task $($task.id). Read the repository instructions. Allowed paths: $($task.allowed_paths -join ', '). Do not use YOLO, bypass approvals, Oracle, production, main, migration 54, secrets, or unrelated files. Do not self-approve. Return evidence and leave review state pending."
    $proc = Start-Process -FilePath 'codex' -ArgumentList @('exec','--sandbox','workspace-write','-C',$RepoRoot,$prompt) -Wait -PassThru -NoNewWindow
    Write-Output "CODEX_EXIT_CODE=$($proc.ExitCode)"
    if ($proc.ExitCode -ne 0) { throw 'Codex execution failed; task remains for bounded retry/block handling' }
    Write-Output 'RESULT_STATE=IMPLEMENTED_PENDING_REVIEW'
    Write-Output 'AUTO_APPROVE=NO'
} finally { $mutex.ReleaseMutex(); $mutex.Dispose() }
