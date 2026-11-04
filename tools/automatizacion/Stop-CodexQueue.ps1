[CmdletBinding()]
param([switch]$Clear)
$runtime=Join-Path $env:LOCALAPPDATA 'RiesgoLavado\CodexQueue'; New-Item -ItemType Directory -Force -Path $runtime|Out-Null
$flag=Join-Path $runtime 'STOP'; if($Clear){Remove-Item -LiteralPath $flag -Force -ErrorAction SilentlyContinue;Write-Output 'STOP=CLEARED'}else{Set-Content -LiteralPath $flag -Value 'STOP' -NoNewline;Write-Output 'STOP=ACTIVE'}
