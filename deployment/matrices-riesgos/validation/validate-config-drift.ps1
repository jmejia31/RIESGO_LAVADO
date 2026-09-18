param(
  [ValidateSet('LOCAL','DEVELOPMENT','QA','PRODUCTION')][string]$Environment = 'LOCAL',
  [string]$ApiBaseUrl = '',
  [string]$OracleAlias = '',
  [string]$EvidencePath = '',
  [string]$LogLevel = ''
)

$ErrorActionPreference = 'Stop'
$errors = [System.Collections.Generic.List[string]]::new()
$expectedApi = if ($Environment -eq 'PRODUCTION') { '/api' } elseif ($Environment -eq 'LOCAL') { 'http://localhost:5043/api' } else { '<environment-specific-secret-free-value>' }
if ($ApiBaseUrl -and $Environment -eq 'PRODUCTION' -and $ApiBaseUrl -ne '/api') { $errors.Add('API_URL_DRIFT') }
if ($OracleAlias -match '(?i)(password|pwd|secret|user id|uid)') { $errors.Add('ORACLE_ALIAS_CONTAINS_SECRET') }
if ($EvidencePath -and [System.IO.Path]::IsPathRooted($EvidencePath) -eq $false) { $errors.Add('EVIDENCE_PATH_NOT_ABSOLUTE') }
if ($LogLevel -match '(?i)^debug$' -and $Environment -eq 'PRODUCTION') { $errors.Add('PRODUCTION_DEBUG_LOG_LEVEL') }
Write-Output "ENVIRONMENT=$Environment"
Write-Output "EXPECTED_API_BASE=$expectedApi"
if ($errors.Count -gt 0) { $errors | ForEach-Object { Write-Error $_ }; Write-Output 'CONFIG_DRIFT_CHECK=FAIL'; exit 1 }
Write-Output 'CONFIG_DRIFT_CHECK=PASS'
