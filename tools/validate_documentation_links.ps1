param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [switch]$PassThru
)

$ErrorActionPreference = 'Stop'
$errors = [System.Collections.Generic.List[string]]::new()
$checkedLinks = 0

$trackedMarkdown = @(
    git -c safe.directory=C:/RIESGO_LAVADO -c core.quotepath=false ls-files -- '*.md'
)
if ($LASTEXITCODE -ne 0) {
    $errors.Add('No fue posible obtener los documentos Markdown rastreados por Git')
}

foreach ($relativeDocumentPath in $trackedMarkdown) {
    $documentPath = Join-Path $RepositoryRoot $relativeDocumentPath
    if (-not (Test-Path -LiteralPath $documentPath -PathType Leaf)) {
        $errors.Add("Documento rastreado inexistente: $relativeDocumentPath")
        continue
    }

    $content = Get-Content -LiteralPath $documentPath -Raw -Encoding UTF8
    $matches = [System.Text.RegularExpressions.Regex]::Matches(
        $content,
        '!?\[[^\]]*\]\((?<target>[^\)]+)\)'
    )

    foreach ($match in $matches) {
        $target = $match.Groups['target'].Value.Trim()
        if ($target.StartsWith('<') -and $target.EndsWith('>')) {
            $target = $target.Substring(1, $target.Length - 2)
        }

        if ($target -match '^(?i:https?|mailto|file):' -or $target.StartsWith('#')) {
            continue
        }

        $targetWithoutAnchor = ($target -split '#', 2)[0]
        $targetWithoutQuery = ($targetWithoutAnchor -split '\?', 2)[0]
        if ([string]::IsNullOrWhiteSpace($targetWithoutQuery)) {
            continue
        }

        try {
            $decodedTarget = [System.Uri]::UnescapeDataString($targetWithoutQuery)
        }
        catch {
            $lineNumber = ($content.Substring(0, $match.Index) -split "`r?`n").Count
            $errors.Add("Enlace local con codificacion invalida: ${relativeDocumentPath}:$lineNumber -> $target")
            continue
        }

        $checkedLinks++
        $resolvedPath = if ([System.IO.Path]::IsPathRooted($decodedTarget)) {
            [System.IO.Path]::GetFullPath($decodedTarget)
        }
        else {
            [System.IO.Path]::GetFullPath((Join-Path (Split-Path -Parent $documentPath) $decodedTarget))
        }

        if (-not (Test-Path -LiteralPath $resolvedPath)) {
            $lineNumber = ($content.Substring(0, $match.Index) -split "`r?`n").Count
            $errors.Add("Enlace local roto: ${relativeDocumentPath}:$lineNumber -> $target")
        }
    }
}

if ($PassThru) {
    foreach ($errorMessage in $errors) {
        Write-Output $errorMessage
    }
    return
}

if ($errors.Count -gt 0) {
    Write-Host 'Validacion de documentacion fallida:' -ForegroundColor Red
    foreach ($errorMessage in $errors) {
        Write-Host "- $errorMessage" -ForegroundColor Red
    }
    exit 1
}

Write-Host 'Validacion de documentacion correcta.' -ForegroundColor Green
Write-Host "Documentos Markdown revisados: $($trackedMarkdown.Count)"
Write-Host "Enlaces locales revisados: $checkedLinks"
