param(
  [string]$BackendUrl = 'http://localhost:5043',
  [string]$FrontendUrl = 'http://localhost:4200',
  [switch]$SkipFrontend
)

$ErrorActionPreference = 'Stop'
function Test-Http200([string]$Name, [string]$Url) {
  $response = Invoke-WebRequest -UseBasicParsing -Uri $Url -Method Get -TimeoutSec 15
  if ($response.StatusCode -ne 200) { throw "$Name status=$($response.StatusCode)" }
  Write-Output "$Name=PASS;STATUS=$($response.StatusCode)"
}

Test-Http200 'BACKEND_HEALTHZ' "$BackendUrl/healthz"
Test-Http200 'BACKEND_READYZ' "$BackendUrl/readyz"
if (-not $SkipFrontend) { Test-Http200 'FRONTEND_HEALTHZ' "$FrontendUrl/healthz" }
Write-Output 'SMOKE_TESTS=PASS'
