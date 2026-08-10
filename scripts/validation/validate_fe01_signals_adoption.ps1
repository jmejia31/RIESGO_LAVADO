$ErrorActionPreference = 'Stop'

function Assert-Contains {
    param([string]$Path, [string]$Pattern, [string]$Message)
    if (-not (Test-Path $Path)) { throw "FE-01: falta archivo requerido: $Path" }
    $content = Get-Content -Raw -Encoding UTF8 $Path
    if ($content -notmatch $Pattern) { throw "FE-01: $Message [$Path]" }
}

function Assert-NotContains {
    param([string]$Path, [string]$Pattern, [string]$Message)
    if (-not (Test-Path $Path)) { throw "FE-01: falta archivo requerido: $Path" }
    $content = Get-Content -Raw -Encoding UTF8 $Path
    if ($content -match $Pattern) { throw "FE-01: $Message [$Path]" }
}

$targetsOnPush = @(
    'frontend/rl-app/src/app/app.ts',
    'frontend/rl-app/src/app/shared/layout/main-layout/main-layout.component.ts',
    'frontend/rl-app/src/app/shared/pages/sin-acceso/sin-acceso.component.ts',
    'frontend/rl-app/src/app/features/admin/configuracion/pages/configuracion/configuracion.component.ts',
    'frontend/rl-app/src/app/features/admin/bitacora/pages/bitacora/bitacora.component.ts',
    'frontend/rl-app/src/app/features/auth/pages/login/login.component.ts',
    'frontend/rl-app/src/app/features/admin/listas/pages/cargar-listas/cargar-listas.component.ts'
)

foreach ($path in $targetsOnPush) {
    Assert-Contains $path 'ChangeDetectionStrategy\.OnPush' 'el componente migrado debe conservar ChangeDetectionStrategy.OnPush'
    Assert-NotContains $path 'ChangeDetectionStrategy\.Eager' 'no debe reintroducirse ChangeDetectionStrategy.Eager en la primera ola FE-01'
}

$loginTs = 'frontend/rl-app/src/app/features/auth/pages/login/login.component.ts'
$loginHtml = 'frontend/rl-app/src/app/features/auth/pages/login/login.component.html'
Assert-Contains $loginTs 'signal<LoginSlide\[\]>\(\[\]\)' 'el carrusel debe mantener una colección tipada mediante Signal'
Assert-Contains $loginTs 'slideSeleccionado\s*=\s*computed\(' 'el slide derivado debe calcularse con computed'
Assert-Contains $loginTs 'ReturnType<typeof setInterval>\s*\|\s*null' 'el temporizador debe permanecer tipado y separado del estado reactivo'
Assert-NotContains $loginTs 'slides:\s*any\[\]' 'no debe volver el arreglo mutable any[] del carrusel'
Assert-Contains $loginHtml '@for\s*\(slide of slides\(\)' 'la plantilla del login debe leer la colección Signal'
Assert-Contains $loginHtml 'slideSeleccionado\(\)' 'la plantilla debe consumir el estado derivado del carrusel'

$cargarTs = 'frontend/rl-app/src/app/features/admin/listas/pages/cargar-listas/cargar-listas.component.ts'
$cargarHtml = 'frontend/rl-app/src/app/features/admin/listas/pages/cargar-listas/cargar-listas.component.html'
Assert-Contains $cargarTs 'signal<File\s*\|\s*null>\(null\)' 'el archivo seleccionado debe mantenerse como Signal de estado local'
Assert-Contains $cargarTs 'archivoSeleccionado\(\)' 'la lógica de carga debe leer el archivo desde el Signal'
Assert-NotContains $cargarTs 'archivoSeleccionado:\s*File\s*\|\s*null\s*=\s*null' 'no debe reintroducirse el campo mutable paralelo para el archivo'
Assert-Contains $cargarHtml '!archivoSeleccionado\(\)' 'la plantilla debe evaluar el Signal del archivo seleccionado'

$auth = 'frontend/rl-app/src/app/core/auth/auth.service.ts'
Assert-Contains $auth 'signal<UsuarioInfo\s*\|\s*null>' 'AuthService debe conservar la sesión reactiva con signal'
Assert-Contains $auth 'estaLogueado\s*=\s*computed\(' 'AuthService debe conservar estado derivado con computed'
Assert-Contains $auth 'effect\(\(\)\s*=>' 'AuthService debe conservar el efecto de ciclo de vida de inactividad'

$httpState = 'frontend/rl-app/src/app/core/services/global-http-state.service.ts'
Assert-Contains $httpState 'peticionesActivas\s*=\s*signal\(' 'el estado HTTP global debe conservar signal'
Assert-Contains $httpState 'cargando\s*=\s*computed\(' 'el estado de carga HTTP debe conservar computed'

$sinAcceso = 'frontend/rl-app/src/app/shared/pages/sin-acceso/sin-acceso.component.ts'
Assert-Contains $sinAcceso 'toSignal\(' 'la adaptacion Observable->Signal de parametros de ruta debe conservarse'

$layout = 'frontend/rl-app/src/app/shared/layout/main-layout/main-layout.component.ts'
Assert-Contains $layout 'sidebarAbierto\s*=\s*signal\(' 'el layout debe conservar estado local mediante signal'
Assert-Contains $layout 'linksVisibles\s*=\s*computed\(' 'el layout debe conservar navegación derivada mediante computed'

$matrices = 'frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.ts'
Assert-Contains $matrices 'ChangeDetectionStrategy\.OnPush' 'Matrices debe conservar su adopción previa de OnPush'
Assert-Contains $matrices 'readonly\s+secciones\s*=\s*computed\(' 'Matrices debe conservar estado derivado mediante computed'

foreach ($path in @($loginTs, $cargarTs, $auth, $httpState, $sinAcceso, $layout, $matrices)) {
    Assert-NotContains $path 'BehaviorSubject' 'FE-01 no debe sustituir estado local Signal por BehaviorSubject en las superficies protegidas'
}

$workflow = '.github/workflows/quality-gates.yml'
Assert-Contains $workflow 'validate_fe01_signals_adoption\.ps1' 'Quality Gates debe ejecutar el validador FE-01'

Write-Host 'VALIDACION FE-01: CORRECTA.'
Write-Host 'Adopcion gradual de Angular Signals protegida: Signals para estado local/derivado, OnPush en la primera ola y RxJS/Reactive Forms preservados donde corresponden.'
