$ErrorActionPreference = 'Stop'

$requiredFiles = @(
    'frontend/rl-app/src/index.html',
    'frontend/rl-app/src/styles.css',
    'frontend/rl-app/src/app/shared/layout/main-layout/main-layout.component.html',
    'frontend/rl-app/src/app/shared/layout/main-layout/main-layout.component.ts',
    'frontend/rl-app/src/app/shared/components/skeleton-loader/skeleton-loader.component.ts',
    'frontend/rl-app/src/app/shared/components/skeleton-loader/skeleton-loader.component.spec.ts'
)

foreach ($file in $requiredFiles) {
    if (-not (Test-Path $file)) {
        throw "FE-03/FE-04: falta artefacto requerido: $file"
    }
}

$index = Get-Content 'frontend/rl-app/src/index.html' -Raw
$styles = Get-Content 'frontend/rl-app/src/styles.css' -Raw
$layout = Get-Content 'frontend/rl-app/src/app/shared/layout/main-layout/main-layout.component.html' -Raw
$layoutTs = Get-Content 'frontend/rl-app/src/app/shared/layout/main-layout/main-layout.component.ts' -Raw
$skeleton = Get-Content 'frontend/rl-app/src/app/shared/components/skeleton-loader/skeleton-loader.component.ts' -Raw

$checks = @(
    @{ Name = 'Idioma principal es-HN'; Ok = $index -match '<html\s+lang="es-HN"' },
    @{ Name = 'Skip link'; Ok = $layout -match 'class="skip-link"' -and $layout -match 'href="#contenido-principal"' },
    @{ Name = 'Landmark main identificable'; Ok = $layout -match '<main\s+id="contenido-principal"' -and $layout -match 'tabindex="-1"' },
    @{ Name = 'Estado aria-busy global'; Ok = $layout -match '\[attr\.aria-busy\]="globalState\.cargando\(\)"' },
    @{ Name = 'Control sidebar relacionado'; Ok = $layout -match 'aria-controls="navegacion-principal"' -and $layout -match '\[attr\.aria-expanded\]="sidebarAbierto\(\)"' },
    @{ Name = 'Ruta activa anunciable'; Ok = $layout -match 'ariaCurrentWhenActive="page"' },
    @{ Name = 'Estado de carga live'; Ok = $layout -match 'role="status"' -and $layout -match 'aria-live="polite"' },
    @{ Name = 'Skeleton transversal'; Ok = $layout -match '<app-skeleton-loader' -and $layoutTs -match 'SkeletonLoaderComponent' },
    @{ Name = 'Skeleton accesible'; Ok = $skeleton -match 'role="status"' -and $skeleton -match 'aria-busy="true"' -and $skeleton -match 'aria-hidden="true"' },
    @{ Name = 'Foco visible'; Ok = $styles -match ':focus-visible' },
    @{ Name = 'Reduccion de movimiento'; Ok = $styles -match 'prefers-reduced-motion:\s*reduce' },
    @{ Name = 'Animacion skeleton controlada'; Ok = $styles -match '\.skeleton-block' -and $styles -match '@keyframes skeleton-pulse' }
)

$failed = $checks | Where-Object { -not $_.Ok }
if ($failed) {
    $names = ($failed | ForEach-Object { $_.Name }) -join ', '
    throw "FE-03/FE-04: controles faltantes: $names"
}

$positiveTabindexPattern = 'tabindex\s*=\s*["''](?:[1-9][0-9]*)["'']'
foreach ($file in @(
    'frontend/rl-app/src/app/shared/layout/main-layout/main-layout.component.html',
    'frontend/rl-app/src/app/shared/components/skeleton-loader/skeleton-loader.component.ts'
)) {
    $content = Get-Content $file -Raw
    if ($content -match $positiveTabindexPattern) {
        throw "FE-03: se detectó tabindex positivo en $file"
    }
}

Write-Host 'VALIDACION FE-03/FE-04: CORRECTA.'
Write-Host 'Semántica, foco, estados WAI-ARIA, reducción de movimiento y skeleton transversal protegidos.'
