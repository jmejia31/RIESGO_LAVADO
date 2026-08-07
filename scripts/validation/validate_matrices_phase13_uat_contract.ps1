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

$principal = Read-Required 'backend/RL.API/Features/MatricesRiesgos/MatricesRiesgosController.cs'
$gestion = Read-Required 'backend/RL.API/Features/MatricesRiesgos/MatricesRiesgosGestionController.cs'
$mitigacion = Read-Required 'backend/RL.API/Features/MatricesRiesgos/MatricesRiesgosMitigacionController.cs'
$monitoreo = Read-Required 'backend/RL.API/Features/MatricesRiesgos/MatricesRiesgosMonitoreoController.cs'
$reportes = Read-Required 'backend/RL.API/Features/MatricesRiesgos/MatricesRiesgosReportesController.cs'
$frontend = Read-Required 'frontend/rl-app/src/app/features/admin/matrices-riesgos/data-access/matrices-riesgos.service.ts'
$uatTests = Read-Required 'backend/RL.API.Tests/Features/MatricesRiesgos/MatricesRiesgosPhase13UatContractTests.cs'
$authE2e = Read-Required 'frontend/rl-app/e2e/matrices-authorization.spec.ts'

foreach ($entry in @(
    @{ Name = 'MatricesRiesgosController'; Content = $principal },
    @{ Name = 'MatricesRiesgosGestionController'; Content = $gestion },
    @{ Name = 'MatricesRiesgosMitigacionController'; Content = $mitigacion },
    @{ Name = 'MatricesRiesgosMonitoreoController'; Content = $monitoreo },
    @{ Name = 'MatricesRiesgosReportesController'; Content = $reportes }
)) {
    Require $entry.Content '[Authorize]' "$($entry.Name) perdió [Authorize]."
    if ($entry.Content -notmatch '\[ModuloAuthorize\((?:10|ModuloId)\)\]') { $errors.Add("$($entry.Name) perdió ModuloAuthorize(10).") }
    Forbid $entry.Content '[AllowAnonymous]' "$($entry.Name) no puede exponer acciones anónimas."
}

foreach ($token in @(
    'formularios/borrador','formularios/{id:long}/clonar','formularios/{id:long}/publicar','formularios/{id:long}/estado',
    'formulario/version-vigente','formularios/historial','evaluaciones/{id:long}','evaluaciones','evaluaciones/{id:long}/transiciones',
    'evaluaciones/{id:long}/flujos','evidencias/vinculos','evidencias/cargar','evidencias/{id:long}','consolidado','metodologia/vigente'
)) { Require $principal $token "Falta contrato Backend crítico '$token'." }

foreach ($token in @('[HttpGet]','[HttpGet("{id:long}")]','[HttpPost]','[HttpPut("{id:long}")]')) {
    Require $gestion $token "Gestión de riesgos perdió endpoint '$token'."
}
foreach ($token in @('CrearControl','ActualizarControl','EvaluarControl','CrearPlan','ActualizarPlan','CrearActividad','ActualizarActividad')) {
    Require $mitigacion $token "Mitigación perdió operación '$token'."
}
foreach ($token in @('CrearAlerta','CambiarEstadoAlerta','RegistrarAutomonitoreo','ObtenerResumen')) {
    Require $monitoreo $token "Monitoreo perdió operación '$token'."
}
foreach ($token in @('consolidado.xlsx','consolidado.pdf','Descarga de reporte consolidado Excel','Descarga de reporte consolidado PDF')) {
    Require $reportes $token "Reportería perdió contrato/auditoría '$token'."
}

foreach ($method in @(
    'metodologiaVigente','obtenerConsolidado','obtenerVersionVigenteFormulario','listarHistorialVersionesFormulario',
    'crearBorradorFormulario','clonarVersionFormulario','actualizarBorradorFormulario','publicarVersionFormulario','cambiarVigenciaFormulario',
    'listarRiesgos','obtenerRiesgo','crearRiesgo','actualizarRiesgo','listarEvaluaciones','obtenerEvaluacion','crearEvaluacion',
    'actualizarEvaluacion','transicionarEvaluacion','obtenerFlujos','listarControles','crearControl','actualizarControl','evaluarControl',
    'listarPlanes','crearPlan','actualizarPlan','listarActividades','crearActividad','actualizarActividad','listarAlertas','crearAlerta',
    'cambiarEstadoAlerta','listarAutomonitoreo','registrarAutomonitoreo','obtenerResumenOperativo','descargarConsolidadoExcel',
    'descargarConsolidadoPdf','cargarEvidencia','vincularEvidencia','eliminarEvidenciaHuerfana'
)) { Require $frontend "$method(" "Angular no contiene el consumo UAT '$method'." }

foreach ($legacy in @('RL_MR_REVISIONES_EVALUACION','RL_MR_TRAZAS_CALCULO','RL_MR_AUDITORIA','RL_MR_EVI_APROBACION','AsociarEvidenciaAprobacionDto')) {
    Forbid ($principal + $gestion + $mitigacion + $monitoreo + $reportes) $legacy "Los controllers reintroducen contrato heredado '$legacy'."
}

foreach ($token in @(
    'TodosLosControllers_ConservanAutenticacionYModulo10',
    'TodasLasMutaciones_ExigenAuditoria',
    'DescargasDeReportes_ExigenAuditoriaExplicita',
    'SuperficieUat_ConservaOperacionesCriticas',
    'Plantillas_SiguenRestringidasAlAdministradorInstitucional'
)) { Require $uatTests $token "Falta prueba de contrato UAT '$token'." }

foreach ($token in @('ADMINISTRADOR','403','clonar')) {
    Require $authE2e $token "El E2E de autorización no conserva evidencia '$token'."
}

if ($errors.Count -gt 0) {
    Write-Host "VALIDACION FASE 13 UAT MATRICES: INCORRECTA ($($errors.Count) hallazgos)" -ForegroundColor Red
    foreach ($error in $errors) { Write-Host " - $error" -ForegroundColor Red }
    exit 1
}

Write-Host 'VALIDACION FASE 13 UAT MATRICES: CORRECTA' -ForegroundColor Green
Write-Host 'Cobertura contractual: Plantillas, riesgos, evaluaciones, flujos, mitigación, evidencias, monitoreo, reportes, autenticación, módulo 10 y auditoría.' -ForegroundColor Green
