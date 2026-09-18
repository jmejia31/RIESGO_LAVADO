param(
  [string]$RepositoryRoot = '',
  [Parameter(Mandatory=$true)][string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
  $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..')).Path
}
$root = (Resolve-Path $RepositoryRoot).Path
$output = [System.IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $output) { Remove-Item -LiteralPath $output -Recurse -Force }
New-Item -ItemType Directory -Path $output | Out-Null

$files = @(
  'deployment/matrices-riesgos/release-manifest.json',
  'deployment/matrices-riesgos/New-ReleasePackage.ps1',
  'deployment/matrices-riesgos/oracle',
  'deployment/matrices-riesgos/smoke',
  'deployment/matrices-riesgos/validation'
)
foreach ($relative in $files) {
  $source = Join-Path $root $relative
  if (-not (Test-Path -LiteralPath $source)) { throw "Falta artefacto requerido: $relative" }
  $destination = Join-Path $output $relative
  if ((Get-Item -LiteralPath $source).PSIsContainer) { Copy-Item -LiteralPath $source -Destination (Split-Path $destination) -Recurse -Force }
  else { New-Item -ItemType Directory -Path (Split-Path $destination) -Force | Out-Null; Copy-Item -LiteralPath $source -Destination $destination -Force }
}

$docRoot = Get-ChildItem -LiteralPath (Join-Path $root 'docs') -Directory |
  Where-Object { $_.Name -like '3.*Matrices*' } | Select-Object -First 1
if (-not $docRoot) { throw 'No se encontró el directorio documental del módulo Matrices de Riesgos.' }
$docNames = @(
  'FASE_7_INVENTARIO_RELEASE.md',
  'FASE_7_DESPLIEGUE_DOCUMENTACION_CAPACITACION_CIERRE.md',
  'MANUAL_TECNICO_MATRICES_RIESGOS_FINAL.md',
  'MANUAL_FUNCIONAL_MATRICES_RIESGOS_FINAL.md',
  'MANUAL_OPERATIVO_MATRICES_RIESGOS.md',
  'GUIA_DBA_MATRICES_RIESGOS.md',
  'GUIA_SOPORTE_MATRICES_RIESGOS.md',
  'PLAN_CAPACITACION_MATRICES_RIESGOS.md',
  'MATERIAL_CAPACITACION_MATRICES_RIESGOS.md',
  'CHECKLIST_CAPACITACION_MATRICES_RIESGOS.md',
  'PLANTILLA_ASISTENCIA_CAPACITACION.md',
  'PLANTILLA_EVALUACION_CAPACITACION.md',
  'CONFIGURACION_AMBIENTES_MATRICES_RIESGOS.md',
  'MONITOREO_CONTINGENCIA_FASE7.md',
  'BACKUP_RESTORE_FASE7.md',
  'RELEASE_NOTES_MATRICES_RIESGOS_95_98.md'
)
$docDestinationRoot = Join-Path $output ('docs\' + $docRoot.Name)
foreach ($name in $docNames) {
  $source = Join-Path $docRoot.FullName $name
  if (-not (Test-Path -LiteralPath $source)) { throw "Falta documento requerido: $name" }
  New-Item -ItemType Directory -Path $docDestinationRoot -Force | Out-Null
  Copy-Item -LiteralPath $source -Destination (Join-Path $docDestinationRoot $name) -Force
}

Get-ChildItem -LiteralPath $output -Recurse -File | ForEach-Object {
  if ($_.Name -in @('SHA256SUMS','release-package.zip')) { return }
  $hash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
  "$hash  $($_.FullName.Substring($output.Length + 1))"
} | Sort-Object | Set-Content -LiteralPath (Join-Path $output 'SHA256SUMS') -Encoding UTF8
Write-Output "PACKAGE_PATH=$output"
Write-Output "PACKAGE_SHA256SUMS=$(Join-Path $output 'SHA256SUMS')"
