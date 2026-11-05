# Pure helper: send the canonical prompt as strict UTF-8 bytes without BOM.
# Windows PowerShell 5.1 / .NET Framework may default Process.StandardInput
# to a legacy console code page, which corrupts accented JSON task text.
function Write-CodexUtf8Stdin {
    param(
        [Parameter(Mandatory=$true)][System.Diagnostics.Process]$Process,
        [Parameter(Mandatory=$true)][string]$Prompt
    )

    $utf8 = [System.Text.UTF8Encoding]::new($false, $true)
    $bytes = $utf8.GetBytes($Prompt)
    $stream = $Process.StandardInput.BaseStream
    $stream.Write($bytes, 0, $bytes.Length)
    $stream.Flush()
    $stream.Close()
}
