[CmdletBinding()]
param([switch]$Clear)
$runtime=Join-Path $env:LOCALAPPDATA 'RiesgoLavado\CodexQueue'; New-Item -ItemType Directory -Force -Path $runtime|Out-Null
$flag=Join-Path $runtime 'PAUSE'; if($Clear){Remove-Item -LiteralPath $flag -Force -ErrorAction SilentlyContinue;Write-Output 'PAUSE=CLEARED'}else{Set-Content -LiteralPath $flag -Value 'PAUSE' -NoNewline;Write-Output 'PAUSE=ACTIVE'}
