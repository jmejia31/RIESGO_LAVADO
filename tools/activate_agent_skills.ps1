[CmdletBinding()]
param(
    [switch]$SkipPull
)

$ErrorActionPreference = 'Stop'

function Invoke-Checked {
    param(
        [Parameter(Mandatory = $true)][string]$Command,
        [Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments
    )

    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Fallo: $Command $($Arguments -join ' ') (exit $LASTEXITCODE)"
    }
}

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    throw 'Git no esta disponible en PATH.'
}

$repoRoot = (& git rev-parse --show-toplevel 2>$null).Trim()
if (-not $repoRoot) {
    throw 'Ejecuta este script dentro del checkout de RIESGO_LAVADO.'
}

Push-Location $repoRoot
try {
    if (-not (Test-Path -LiteralPath '.agents/skills' -PathType Container)) {
        throw 'No existe .agents/skills. Actualiza origin/desarrollo antes de continuar.'
    }

    $originUrl = (& git remote get-url origin).Trim()
    if ($originUrl -notmatch 'jmejia31[/:]RIESGO_LAVADO(?:\.git)?$') {
        throw "El remote origin no apunta al repositorio oficial jmejia31/RIESGO_LAVADO: $originUrl"
    }

    $branch = (& git branch --show-current).Trim()
    if ($branch -ne 'desarrollo') {
        throw "Rama actual: '$branch'. Cambia a 'desarrollo' antes de activar el entorno compartido."
    }

    Invoke-Checked git fetch origin desarrollo

    $ahead = [int]((& git rev-list --count 'origin/desarrollo..HEAD').Trim())
    $behind = [int]((& git rev-list --count 'HEAD..origin/desarrollo').Trim())
    $dirty = @(& git status --porcelain)

    if (-not $SkipPull -and $behind -gt 0) {
        if ($dirty.Count -gt 0 -or $ahead -gt 0) {
            throw "El checkout no puede actualizarse de forma segura: ahead=$ahead behind=$behind dirty=$($dirty.Count). Conserva el trabajo existente y sincroniza manualmente antes de continuar."
        }
        Invoke-Checked git pull --ff-only origin desarrollo
        $ahead = [int]((& git rev-list --count 'origin/desarrollo..HEAD').Trim())
        $behind = [int]((& git rev-list --count 'HEAD..origin/desarrollo').Trim())
    }

    Invoke-Checked git config core.hooksPath .githooks

    $python = Get-Command python -ErrorAction SilentlyContinue
    if ($python) {
        Invoke-Checked python tools/validate_agent_skills.py
    }
    elseif (Get-Command py -ErrorAction SilentlyContinue) {
        Invoke-Checked py -3 tools/validate_agent_skills.py
    }
    else {
        throw 'Python 3 no esta disponible. Es requerido para validar Agent Skills.'
    }

    $skillCount = @(
        Get-ChildItem -LiteralPath '.agents/skills' -Directory |
            Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'SKILL.md') -PathType Leaf }
    ).Count

    $hooksPath = (& git config --get core.hooksPath).Trim()
    $head = (& git rev-parse HEAD).Trim()

    Write-Host ''
    Write-Host '=== RIESGO_LAVADO Agent Skills ACTIVADO ===' -ForegroundColor Green
    Write-Host "Repositorio : $repoRoot"
    Write-Host "Rama        : $branch"
    Write-Host "HEAD        : $head"
    Write-Host "Ahead/Behind: $ahead/$behind"
    Write-Host "Skills      : $skillCount"
    Write-Host "Git hooks   : $hooksPath"
    Write-Host ''
    Write-Host 'Siguiente paso: cierra/reabre la sesion del agente y abre/inicia Codex o Antigravity desde la raiz del repositorio.' -ForegroundColor Cyan
    Write-Host 'AGENTS.md y .agents/skills/ son la fuente operativa compartida.' -ForegroundColor Cyan
}
finally {
    Pop-Location
}
