param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'

$extensions = @(
    '.cs', '.ts', '.html', '.css', '.scss', '.json', '.sql', '.ps1',
    '.yml', '.yaml', '.txt', '.xml', '.csproj', '.sln', '.props', '.targets'
)

$scanRoots = @(
    'frontend/rl-app/src',
    'frontend/rl-app/e2e',
    'backend/RL.API',
    'backend/RL.API.Tests',
    'database',
    'tools',
    'scripts',
    '.github'
)

$excludedDirectories = @(
    '.git', 'node_modules', 'bin', 'obj', 'coverage', 'TestResults', '.angular', '.sonarqube'
)

$patterns = [ordered]@{
    'U+FFFD replacement character' = [string][char]0xFFFD
    'UTF-8 replacement decoded as Windows-1252' = ([string][char]0x00EF) + [char]0x00BF + [char]0x00BD
    'UTF-8 lead byte C3 decoded as text' = [string][char]0x00C3
    'UTF-8 lead byte C2 decoded as text' = [string][char]0x00C2
    'smart punctuation mojibake prefix' = ([string][char]0x00E2) + [char]0x20AC
    'emoji mojibake prefix' = ([string][char]0x00F0) + [char]0x0178
}

$findings = [System.Collections.Generic.List[object]]::new()
$files = [System.Collections.Generic.List[System.IO.FileInfo]]::new()

foreach ($relativeRoot in $scanRoots) {
    $root = Join-Path $RepositoryRoot $relativeRoot
    if (-not (Test-Path -LiteralPath $root)) {
        continue
    }

    Get-ChildItem -LiteralPath $root -Recurse -File |
        Where-Object {
            $extensions -contains $_.Extension.ToLowerInvariant() -and
            -not ($_.FullName.Split([IO.Path]::DirectorySeparatorChar) | Where-Object { $excludedDirectories -contains $_ })
        } |
        ForEach-Object { $files.Add($_) }
}

foreach ($file in $files) {
    $lineNumber = 0
    foreach ($line in [IO.File]::ReadLines($file.FullName)) {
        $lineNumber++
        foreach ($entry in $patterns.GetEnumerator()) {
            if ($line.IndexOf([string]$entry.Value, [StringComparison]::Ordinal) -ge 0) {
                $relative = [IO.Path]::GetRelativePath($RepositoryRoot, $file.FullName).Replace('\', '/')
                $findings.Add([pscustomobject]@{
                    File = $relative
                    Line = $lineNumber
                    Pattern = $entry.Key
                    Text = $line.Trim()
                })
                break
            }
        }
    }
}

if ($findings.Count -gt 0) {
    Write-Host "TEXT_ENCODING_INTEGRITY=FAIL" -ForegroundColor Red
    Write-Host "MOJIBAKE_FINDINGS=$($findings.Count)" -ForegroundColor Red
    foreach ($finding in $findings) {
        Write-Host ("{0}:{1}: {2} :: {3}" -f $finding.File, $finding.Line, $finding.Pattern, $finding.Text) -ForegroundColor Red
    }
    exit 1
}

Write-Host "TEXT_ENCODING_INTEGRITY=PASS" -ForegroundColor Green
Write-Host "MOJIBAKE_FINDINGS=0" -ForegroundColor Green
