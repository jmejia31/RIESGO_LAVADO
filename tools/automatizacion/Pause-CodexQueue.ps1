[CmdletBinding()]
param([switch]$Clear,[string]$RuntimePath='')
$runtime=if([string]::IsNullOrWhiteSpace($RuntimePath)){Join-Path $env:LOCALAPPDATA 'RiesgoLavado\CodexQueue'}else{$RuntimePath}; New-Item -ItemType Directory -Force -Path $runtime|Out-Null
$flag=Join-Path $runtime 'PAUSE'; if($Clear){Remove-Item -LiteralPath $flag -Force -ErrorAction SilentlyContinue;Write-Output 'PAUSE=CLEARED'}else{Set-Content -LiteralPath $flag -Value 'PAUSE' -NoNewline;Write-Output 'PAUSE=ACTIVE'}
