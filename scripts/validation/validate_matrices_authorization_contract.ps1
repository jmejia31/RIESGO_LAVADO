$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '../..')
$errors = [System.Collections.Generic.List[string]]::new()

function Read-Required([string]$relative) {
    $path = Join-Path $root $relative
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        $errors.Add("Falta archivo obligatorio: $relative")
        return ''
    }
    return [System.IO.File]::ReadAllText($path)
}

function Require([string]$content, [string]$token, [string]$message) {
    if (-not $content.Contains($token)) { $errors.Add($message) }
}

function Forbid([string]$content, [string]$token, [string]$message) {
    if ($content.Contains($token)) { $errors.Add($message) }
}

$seed = Read-Required 'database/02_seed_data.sql'
$roles = Read-Required 'backend/RL.API/Core/Security/SystemRoles.cs'
$authService = Read-Required 'backend/RL.API/Features/Identidad/Application/AuthService.cs'
$moduleAuthorize = Read-Required 'backend/RL.API/Core/Security/ModuloAuthorizeAttribute.cs'
$controller = Read-Required 'backend/RL.API/Features/MatricesRiesgos/MatricesRiesgosController.cs'
$gestion = Read-Required 'backend/RL.API/Features/MatricesRiesgos/MatricesRiesgosGestionController.cs'
$mitigacion = Read-Required 'backend/RL.API/Features/MatricesRiesgos/MatricesRiesgosMitigacionController.cs'
$monitoreo = Read-Required 'backend/RL.API/Features/MatricesRiesgos/MatricesRiesgosMonitoreoController.cs'
$reportes = Read-Required 'backend/RL.API/Features/MatricesRiesgos/MatricesRiesgosReportesController.cs'
$tests = Read-Required 'backend/RL.API.Tests/Features/MatricesRiesgos/MatricesRiesgosAuthorizationContractTests.cs'

foreach ($role in @('ADMINISTRADOR','SUPERVISOR','ANALISTA')) {
    Require $seed "'$role'" "database/02_seed_data.sql no contiene el rol institucional '$role'."
    Require $roles "= `"$role`";" "SystemRoles.cs no contiene el nombre canónico '$role'."
}

Require $authService 'new Claim(ClaimTypes.Role, usuario.Rol.RolNombre)' 'AuthService no emite el rol institucional real en ClaimTypes.Role.'
Require $authService 'new Claim("modulos", string.Join(",", usuario.ModulosIds ?? new List<int>()))' 'AuthService no emite el claim modulos desde los módulos reales del usuario.'

Require $moduleAuthorize 'StatusCodes.Status403Forbidden' 'ModuloAuthorize no conserva respuesta 403.'
Require $moduleAuthorize 'new UnauthorizedObjectResult' 'ModuloAuthorize no conserva respuesta 401 para no autenticados.'
Require $moduleAuthorize 'FindFirst("modulos")' 'ModuloAuthorize no valida el claim modulos.'

foreach ($entry in @(
    @{ Name = 'MatricesRiesgosController'; Content = $controller },
    @{ Name = 'MatricesRiesgosGestionController'; Content = $gestion },
    @{ Name = 'MatricesRiesgosMitigacionController'; Content = $mitigacion },
    @{ Name = 'MatricesRiesgosMonitoreoController'; Content = $monitoreo },
    @{ Name = 'MatricesRiesgosReportesController'; Content = $reportes }
)) {
    Require $entry.Content '[Authorize]' "$($entry.Name) perdió [Authorize]."
    if ($entry.Content -notmatch '\[ModuloAuthorize\((?:10|ModuloId)\)\]') { $errors.Add("$($entry.Name) no conserva autorización del módulo 10.") }
    Forbid $entry.Content '[AllowAnonymous]' "$($entry.Name) no puede introducir [AllowAnonymous]."
}

$adminAttribute = '[Authorize(Roles = SystemRoles.Administrador)]'
$adminAttributeCount = ([regex]::Matches($controller, [regex]::Escape($adminAttribute))).Count
# El controlador protege cinco operaciones del ciclo de versiones, además de
# eliminación y tres operaciones CRUD de familias. Todas son mutaciones
# administrativas válidas y deben conservar el rol canónico.
if ($adminAttributeCount -ne 9) { $errors.Add("MatricesRiesgosController debe contener exactamente 9 protecciones administrativas; actual=$adminAttributeCount.") }

foreach ($method in @('CrearBorradorFormulario','ClonarVersionFormulario','ActualizarBorradorFormulario','PublicarVersionFormulario','CambiarEstadoVigenciaFormulario')) {
    Require $controller $method "Falta la operación administrativa $method."
}
foreach ($legacy in @('ADMIN, DBA, RIESGOS_ADMIN','RIESGOS_ADMIN')) {
    Forbid $controller $legacy "MatricesRiesgosController conserva rol heredado/inexistente '$legacy'."
}

foreach ($token in @(
    'ModuloMatrices_NoAutenticado_Devuelve401',
    'ModuloMatrices_AutenticadoSinClaimModulos_Devuelve403',
    'ModuloMatrices_AutenticadoSinModulo10_Devuelve403',
    'ModuloMatrices_AdministradorConModulo10_PasaFiltroDeModulo',
    'OperacionesAdministrativasFormulario_ExigenAdministradorCanonico',
    'RolesAliasesHeredados_NoFormanParteDelContratoDePlantillas'
)) { Require $tests $token "La suite de autorización no contiene '$token'." }

if ($errors.Count -gt 0) {
    Write-Host "VALIDACION AUTORIZACION MATRICES: INCORRECTA ($($errors.Count) hallazgos)" -ForegroundColor Red
    foreach ($error in $errors) { Write-Host " - $error" -ForegroundColor Red }
    exit 1
}

Write-Host 'VALIDACION AUTORIZACION MATRICES: CORRECTA' -ForegroundColor Green
Write-Host 'Contrato: no autenticado=401; autenticado sin modulo=403; ADMINISTRADOR + modulo 10 habilita operaciones administrativas de Plantillas.' -ForegroundColor Green
