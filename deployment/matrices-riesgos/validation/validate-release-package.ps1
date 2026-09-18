param([string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path)

$ErrorActionPreference = 'Stop'
$errors = [System.Collections.Generic.List[string]]::new()
$packageRoot = Join-Path $RepositoryRoot 'deployment/matrices-riesgos'

function Require-File([string]$relative) {
  if (-not (Test-Path -LiteralPath (Join-Path $RepositoryRoot $relative) -PathType Leaf)) { $errors.Add("MISSING_FILE=$relative") }
}

$manifestPath = Join-Path $packageRoot 'release-manifest.json'
try {
  $manifest = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json
} catch { $errors.Add('MANIFEST_INVALID_JSON') }

@(
  'release-manifest.json',
  'oracle/01_preflight_release.sql',
  'oracle/02_install_guarded.sql',
  'oracle/03_upgrade_guarded.sql',
  'oracle/04_postflight_release.sql',
  'oracle/05_rollback_v2_draft_guarded.sql',
  'smoke/smoke-tests.ps1',
  'validation/validate-config-drift.ps1',
  'validation/validate-release-package.ps1',
  'New-ReleasePackage.ps1'
) | ForEach-Object { Require-File "deployment/matrices-riesgos/$_" }

if ($manifest) {
  foreach ($property in @('module','releaseVersion','gitSha','databaseVersion','backendArtifact','frontendArtifact','requiredScripts','preflight','install','upgrade','seed','migration','postflight','rollback','backup','restore','healthcheck','smokeTests','knownRestrictions')) {
    if ($null -eq $manifest.$property) { $errors.Add("MANIFEST_MISSING=$property") }
  }
  if ($manifest.databaseVersion.v1.status -ne 'PUBLISHED' -or $manifest.databaseVersion.v1.vigente -ne 1) { $errors.Add('V1_NOT_PROTECTED') }
  if ($manifest.databaseVersion.v2.status -ne 'DRAFT' -or $manifest.databaseVersion.v2.vigente -ne 0) { $errors.Add('V2_NOT_DRAFT') }
}

$dockerFiles = @('backend/RL.API/Dockerfile','frontend/rl-app/Dockerfile','compose.yml')
foreach ($file in $dockerFiles) {
  $path = Join-Path $RepositoryRoot $file
  $content = Get-Content -Raw -LiteralPath $path
  $content = $content -replace '\$\{[^}]+\}', '${SAFE_PLACEHOLDER}'
  if ($content -match '(?i)(password|secret|token)\s*[:=]\s*[^$\{\r\n]+') { $errors.Add("POSSIBLE_HARDCODED_SECRET=$file") }
}
$trackedPackageText = Get-ChildItem -LiteralPath $packageRoot -Recurse -File | Get-Content -Raw -ErrorAction SilentlyContinue
if ($trackedPackageText -match '(?i)(password|secret|token)\s*[:=]\s*[A-Za-z0-9+/=_\-]{12,}') { $errors.Add('PACKAGE_SECRET_PATTERN') }

if ($errors.Count -gt 0) {
  $errors | ForEach-Object { Write-Error $_ }
  Write-Output 'RELEASE_PACKAGE_VALIDATOR=FAIL'
  exit 1
}
Write-Output 'RELEASE_PACKAGE_VALIDATOR=PASS'
