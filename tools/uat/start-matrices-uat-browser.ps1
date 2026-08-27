[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$profileDir = Join-Path $env:TEMP 'RIESGO_LAVADO_UAT\playwright-profile-final-d1-2'
$endpointFile = Join-Path $env:TEMP 'RIESGO_LAVADO_UAT\cdp-endpoint.txt'
$frontendUrl = 'http://localhost:4200/login'
$activePortFile = Join-Path $profileDir 'DevToolsActivePort'

function Get-UatProcesses {
    Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -in @('chrome.exe', 'msedge.exe') -and $_.CommandLine -like '*playwright-profile-final-d1-2*' }
}

function Get-PlaywrightChromiumPath {
    $playwrightPackage = Join-Path $repoRoot 'frontend\rl-app\node_modules\playwright'
    if (-not (Test-Path -LiteralPath $playwrightPackage -PathType Container)) {
        throw 'CAUSE=PLAYWRIGHT_PACKAGE_MISSING'
    }
    $nodeScript = 'const { chromium } = require(process.argv[1]); process.stdout.write(chromium.executablePath());'
    $path = (& node -e $nodeScript $playwrightPackage).Trim()
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($path) -or -not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw 'CAUSE=CHROMIUM_EXECUTABLE_MISSING'
    }
    return $path
}

function Write-Failure {
    param([string]$Cause, [System.Diagnostics.Process]$Process, [string]$Port = '')
    $alive = if ($null -ne $Process -and -not $Process.HasExited) { 'YES' } else { 'NO' }
    $active = if (Test-Path -LiteralPath $activePortFile -PathType Leaf) { 'FOUND' } else { 'MISSING' }
    $listener = 'NO'
    if ($Port -match '^\d+$') {
        $listener = if (@(Get-NetTCPConnection -LocalAddress 127.0.0.1 -LocalPort ([int]$Port) -ErrorAction SilentlyContinue).Count -gt 0) { 'YES' } else { 'NO' }
    }
    $exitCode = if ($null -ne $Process -and $Process.HasExited) { $Process.ExitCode } else { 'N/A' }
    Write-Host "PROCESS_ALIVE=$alive"
    Write-Host "DEVTOOLS_ACTIVE_PORT=$active"
    Write-Host "PORT_LISTENER=$listener"
    Write-Host "PROFILE_LOCK=$(if (@(Get-UatProcesses).Count -gt 0) { 'YES' } else { 'NO' })"
    Write-Host "CHROMIUM_EXIT_CODE=$exitCode"
    Write-Host "CAUSE=$Cause"
    exit 1
}

if (-not (Test-Path -LiteralPath $profileDir -PathType Container)) {
    New-Item -ItemType Directory -Path $profileDir -Force | Out-Null
}

$existing = @(Get-UatProcesses)
if ($existing.Count -gt 0) {
    Write-Host 'PROCESS_ALIVE=YES'
    Write-Host 'DEVTOOLS_ACTIVE_PORT=FOUND_OR_PROFILE_BUSY'
    Write-Host 'PORT_LISTENER=UNKNOWN'
    Write-Host 'PROFILE_LOCK=YES'
    Write-Host 'CHROMIUM_EXIT_CODE=N/A'
    Write-Host 'CAUSE=PROFILE_ALREADY_IN_USE; close only the validated UAT profile tree and retry'
    exit 1
}

if (Test-Path -LiteralPath $activePortFile -PathType Leaf) {
    Remove-Item -LiteralPath $activePortFile -Force
}

$chromiumPath = Get-PlaywrightChromiumPath
$arguments = @(
    '--remote-debugging-port=0',
    '--remote-debugging-address=127.0.0.1',
    "--user-data-dir=$profileDir",
    '--start-maximized',
    '--no-first-run',
    '--no-default-browser-check',
    $frontendUrl
)
$process = Start-Process -FilePath $chromiumPath -ArgumentList $arguments -PassThru

$deadline = (Get-Date).AddSeconds(20)
$port = ''
while ((Get-Date) -lt $deadline) {
    if ($process.HasExited) {
        Write-Failure -Cause 'CHROMIUM_EXITED_BEFORE_DEVTOOLS_ACTIVE_PORT' -Process $process
    }
    if (Test-Path -LiteralPath $activePortFile -PathType Leaf) {
        $activeLines = @(Get-Content -LiteralPath $activePortFile -TotalCount 2)
        if ($activeLines.Count -ge 2 -and $activeLines[0] -match '^\d+$' -and -not [string]::IsNullOrWhiteSpace($activeLines[1])) {
            $port = $activeLines[0].Trim()
            break
        }
    }
    Start-Sleep -Milliseconds 250
}

if ([string]::IsNullOrWhiteSpace($port)) {
    Write-Failure -Cause 'DEVTOOLS_ACTIVE_PORT_MISSING' -Process $process
}

$endpoint = "http://127.0.0.1:$port"
try {
    $versionResponse = Invoke-WebRequest -UseBasicParsing -Uri "$endpoint/json/version" -TimeoutSec 3
    $version = $versionResponse.Content | ConvertFrom-Json
    if ($versionResponse.StatusCode -ne 200 -or [string]::IsNullOrWhiteSpace($version.Browser)) {
        Write-Failure -Cause 'CDP_JSON_VERSION_INVALID' -Process $process -Port $port
    }
}
catch {
    Write-Failure -Cause 'CDP_JSON_VERSION_UNREACHABLE' -Process $process -Port $port
}

Set-Content -LiteralPath $endpointFile -Value $endpoint -Encoding ascii
Write-Host 'UAT_BROWSER_VISIBLE_READY=YES'
Write-Host "UAT_BROWSER_PID=$($process.Id)"
Write-Host "UAT_CDP_PORT=$port"
Write-Host "UAT_CDP_ENDPOINT=$endpoint"
