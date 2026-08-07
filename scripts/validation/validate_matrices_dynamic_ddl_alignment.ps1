$ErrorActionPreference = 'Stop'

$repositoryRoot = Resolve-Path (Join-Path $PSScriptRoot '../..')
$errors = [System.Collections.Generic.List[string]]::new()

function Add-Error([string]$Message) {
    $errors.Add($Message)
}

function Read-RepoFile([string]$RelativePath) {
    $path = Join-Path $repositoryRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        Add-Error "No se encontró el archivo obligatorio: $RelativePath"
        return ''
    }
    return [System.IO.File]::ReadAllText($path)
}

function Assert-Contains([string]$Content, [string]$Token, [string]$Message) {
    if (-not $Content.Contains($Token)) { Add-Error $Message }
}

function Assert-NotContains([string]$Content, [string]$Token, [string]$Message) {
    if ($Content.Contains($Token)) { Add-Error $Message }
}

function Get-SourceFiles([string[]]$Roots, [string[]]$Extensions) {
    $excluded = @('bin','obj','node_modules','dist','coverage','Historico','retiro_controlado','transicion','.git')
    foreach ($relativeRoot in $Roots) {
        $root = Join-Path $repositoryRoot $relativeRoot
        if (-not (Test-Path -LiteralPath $root)) {
            Add-Error "No se encontró una raíz obligatoria: $relativeRoot"
            continue
        }
        Get-ChildItem -LiteralPath $root -Recurse -File | Where-Object {
            $Extensions -contains $_.Extension.ToLowerInvariant() -and
            -not (@($_.FullName -split '[\\/]') | Where-Object { $excluded -contains $_ })
        }
    }
}

$repositoryRelative = 'backend/RL.API/Features/MatricesRiesgos/Persistence/MatricesRiesgosRepository.cs'
$repositoryContractRelative = 'backend/RL.API/Features/MatricesRiesgos/Persistence/IMatricesRiesgosRepository.cs'
$appServiceContractRelative = 'backend/RL.API/Features/MatricesRiesgos/Application/IMatricesRiesgosAppService.cs'
$controllerRelative = 'backend/RL.API/Features/MatricesRiesgos/MatricesRiesgosController.cs'
$angularModelsRelative = 'frontend/rl-app/src/app/features/admin/matrices-riesgos/models/matrices-riesgos.models.ts'
$programRelative = 'backend/RL.API/Program.cs'
$oracleIntegrationRelative = 'backend/RL.API.Tests/Features/MatricesRiesgos/MatricesRiesgosRepositoryIntegrationTests.cs'
$script05Relative = 'database/19_matrices_riesgos/instalacion/05_ajustes_dashboard_seguridad_reportes.sql'
$script06Relative = 'database/19_matrices_riesgos/transicion/06_reconstruir_modelo_17_tablas.sql'

$repository = Read-RepoFile $repositoryRelative
$repositoryContract = Read-RepoFile $repositoryContractRelative
$appServiceContract = Read-RepoFile $appServiceContractRelative
$controller = Read-RepoFile $controllerRelative
$angularModels = Read-RepoFile $angularModelsRelative
$program = Read-RepoFile $programRelative
$oracleIntegration = Read-RepoFile $oracleIntegrationRelative
$script05 = Read-RepoFile $script05Relative
$script06 = Read-RepoFile $script06Relative

foreach ($relative in @(
    '.github/workflows/agent-fix-matrices-phase1.yml',
    'backend/RL.API/Features/MatricesRiesgos/Persistence/MatricesRiesgosRepositoryFacade.cs',
    'backend/RL.API/Features/MatricesRiesgos/Contracts/Matrices/MatrizRiesgoDtos.cs',
    'backend/RL.API/Features/MatricesRiesgos/Contracts/Reporteria/ReporteriaDtos.cs',
    'backend/RL.API/Features/MatricesRiesgos/Application/MatricesRiesgosReportRenderer.cs',
    'backend/RL.API/Features/MatricesRiesgos/Contracts/Configuracion/PermisoFormularioDto.cs'
)) {
    if (Test-Path -LiteralPath (Join-Path $repositoryRoot $relative)) {
        Add-Error "No debe permanecer el archivo retirado: $relative"
    }
}

$moduleFiles = @(Get-SourceFiles @(
    'backend/RL.API/Features/MatricesRiesgos',
    'backend/RL.API.Tests/Features/MatricesRiesgos',
    'frontend/rl-app/src/app/features/admin/matrices-riesgos',
    'database/19_matrices_riesgos'
) @('.cs','.ts','.html','.sql','.json'))

$legacyTokens = @(
    'FLU_ESTADO_NUEVO','FLU_ESTADO_ANTERIOR','EVA_ESTADO','EVA_VRI','EVA_ETP','EVA_VRR',
    'EVA_FECHA_EVAL','EVA_USR_EVAL','PROY_ETP','RL_MR_MODELOS','RL_MR_FACTORES','RL_MR_VARIABLES',
    'RL_MR_ESCALAS','RL_MR_CRITERIOS','ModeloId','ModeloVersion','FactorInstitucionalDto',
    'VariableMetodologiaRespuestaDto','MatrizRiesgoResumenDto','MatrizRiesgoDetalleDto',
    'MatrizRiesgoVariableDetalleDto','List<Dictionary<string, object>>','DeterminarClasificacionResidual',
    'RegistrarAuditoriaAsync','InsertarTrazaCalculoAsync','RL_MR_TRAZAS_CALCULO','SEQ_RL_MR_TRAZAS','TRA_REGLA_ID',
    'VincularEvidenciaAprobacionAsync','AsociarEvidenciaAprobacionDto','EjecutarVinculoEvidenciaAsync',
    'RL_MR_EVI_RIESGO','RL_MR_EVI_EVALUACION','RL_MR_EVI_CONTROL','RL_MR_EVI_PLAN',
    'RL_MR_EVI_ACTIVIDAD','RL_MR_EVI_ALERTA','RL_MR_EVI_AUTOMONITOREO','RL_MR_EVI_REVISION',
    'RL_MR_EVI_APROBACION','PermisoFormularioDto','tablaPuente','columnaEntidad','columnaEvidencia'
)

foreach ($file in $moduleFiles) {
    $content = [System.IO.File]::ReadAllText($file.FullName)
    foreach ($token in $legacyTokens) {
        if ($content.Contains($token)) {
            $relative = $file.FullName.Substring(([string]$repositoryRoot).Length).TrimStart('\','/')
            Add-Error "${relative} reintroduce el contrato heredado '$token'."
        }
    }
}

foreach ($token in @('EVA_DATA_JSON','EVA_DATA_CALC_JSON')) {
    Assert-NotContains $repository $token "MatricesRiesgosRepository.cs conserva la columna física retirada '$token'."
}
foreach ($token in @(
    'InsertarAuditoriaCampoAsync','INSERT INTO RL_MR_AUDITORIA','SEQ_RL_MR_AUDITORIA',
    'IAuditoriaRepository? _auditoriaRepository','this(db, null)'
)) {
    Assert-NotContains $repository $token "MatricesRiesgosRepository.cs conserva auditoría local/opcional retirada: '$token'."
}

$requiredRepositoryTokens = [ordered]@{
    'command.Transaction = transaction' = 'Los comandos deben propagar OracleTransaction.'
    'FLU_ESTADO' = 'El estado debe proceder del historial de flujos.'
    "VER_ESTADO = 'PUBLISHED'" = 'La metodología debe usar versiones publicadas.'
    'REG_CODIGO = :codigo' = 'La regla debe resolverse por código.'
    'REG_VERSION = :version' = 'La regla debe resolverse por versión.'
    'REG_ALGORITMO_ID' = 'La regla debe resolver el algoritmo institucional.'
    'EVA_DATOS_JSON' = 'Las respuestas deben usar la columna física aprobada.'
    'EVA_CALCULOS_JSON' = 'Los cálculos deben usar la columna física aprobada.'
    'IncorporarMetadatosRegla' = 'El servidor debe incorporar metadatos de cálculo.'
    'reglaCodigo' = 'El resultado debe conservar el código de regla.'
    'reglaVersion' = 'El resultado debe conservar la versión de regla.'
    'algoritmoId' = 'El resultado debe conservar el algoritmo.'
    'Task<IReadOnlyList<RiesgoReporteFilaDto>> ObtenerConsolidadoTipadoAsync' = 'El consolidado debe ser tipado.'
    'Task<MetodologiaFormularioDto?> ObtenerMetodologiaDinamicaVigenteAsync' = 'La metodología debe usar contrato neutro.'
    'VersionFormularioId = versionId' = 'La metodología debe conservar la versión.'
    'Secciones = secciones' = 'La metodología debe conservar secciones.'
    'Catalogos = catalogos' = 'La metodología debe conservar catálogos.'
    'Reglas = reglas' = 'La metodología debe conservar reglas.'
    'VincularEvidenciaAsync' = 'Debe conservarse la vinculación genérica de evidencias.'
    'RL_MR_EVIDENCIAS_VINCULOS' = 'Debe utilizarse la tabla genérica de vínculos.'
    'private readonly IAuditoriaRepository _auditoriaRepository;' = 'La auditoría institucional debe ser obligatoria.'
    'IAuditoriaRepository auditoriaRepository' = 'El constructor debe exigir auditoría institucional.'
}
foreach ($entry in $requiredRepositoryTokens.GetEnumerator()) {
    Assert-Contains $repository $entry.Key "MatricesRiesgosRepository.cs no contiene '$($entry.Key)'. $($entry.Value)"
}
Assert-NotContains $repository 'NotSupportedException' 'MatricesRiesgosRepository.cs contiene NotSupportedException.'

$auditPhysicalContract = Read-RepoFile 'database/01_create_tables.sql'
Assert-Contains $auditPhysicalContract "AUD_ACCION IN ('INSERT','UPDATE','DELETE','LOGIN','LOGOUT','EXPORT')" 'El DDL institucional no contiene el dominio esperado de AUD_ACCION.'

$createEvalPattern = '(?s)public\s+async\s+Task<long>\s+CrearEvaluacionAsync.*?_auditoriaRepository\.RegistrarAsync\(.*?"RL_MR_EVALUACIONES_RIESGO".*?"INSERT".*?dto\.EvaRiesgoId.*?dto\.EvaVersionId.*?Datos\s*=.*?Calculos\s*='
if ($repository -notmatch $createEvalPattern) {
    Add-Error 'CrearEvaluacionAsync no audita como INSERT con contexto funcional de riesgo, versión, datos y cálculos.'
}

$updateEvalPattern = '(?s)public\s+async\s+Task<bool>\s+ActualizarEvaluacionAsync.*?_auditoriaRepository\.RegistrarAsync\(.*?"RL_MR_EVALUACIONES_RIESGO".*?"UPDATE".*?Datos\s*=\s*jsonAnterior.*?VersionRow\s*=\s*versionRowActual.*?Datos\s*=\s*dto\.EvaDataJson.*?Calculos\s*=\s*calculosJson'
if ($repository -notmatch $updateEvalPattern) {
    Add-Error 'ActualizarEvaluacionAsync no audita como UPDATE preservando datos/versiones anterior y nueva.'
}

$transitionPattern = '(?s)public\s+async\s+Task<bool>\s+TransicionarEstadoEvaluacionAsync.*?InsertarFlujoAsync\(.*?_auditoriaRepository\.RegistrarAsync\(.*?"RL_MR_EVALUACIONES_RIESGO".*?"UPDATE".*?Estado\s*=\s*anterior.*?Estado\s*=\s*estado.*?Motivo\s*=\s*motivo'
if ($repository -notmatch $transitionPattern) {
    Add-Error 'TransicionarEstadoEvaluacionAsync no conserva flujo + auditoría UPDATE con estado anterior, nuevo y motivo.'
}

foreach ($forbiddenAuditAction in @('"CREAR_EVALUACION"','"ACTUALIZAR_EVALUACION"','"TRANSICION_ESTADO"')) {
    if ($repository.Contains($forbiddenAuditAction)) {
        Add-Error "AUD_ACCION no puede volver a usar $forbiddenAuditAction; Oracle exige el dominio físico institucional."
    }
}

foreach ($token in @('EVA_DATOS_JSON','EVA_CALCULOS_JSON')) {
    Assert-Contains $script06 $token "El script 06 no contiene la columna física obligatoria '$token'."
}
foreach ($token in @('EVA_DATA_JSON','EVA_DATA_CALC_JSON')) {
    Assert-NotContains $script06 $token "El script 06 conserva la columna física retirada '$token'."
}
foreach ($pattern in @('CREATE TABLE\s+RL_MR_AUDITORIA','CREATE SEQUENCE\s+SEQ_RL_MR_AUDITORIA')) {
    if ($script06 -match $pattern) { Add-Error "El script 06 vuelve a crear auditoría local: $pattern" }
}
Assert-Contains $script06 "'RL_MR_AUDITORIA'" 'El script 06 debe conservar el retiro controlado de RL_MR_AUDITORIA heredada.'
if ($script06 -match '(?im)^\s*CREATE\s+TABLE\s+RL_MR_TRAZAS_CALCULO\b') { Add-Error 'El script 06 no puede crear RL_MR_TRAZAS_CALCULO.' }
if ($script06 -match '(?im)^\s*CREATE\s+SEQUENCE\s+SEQ_RL_MR_TRAZAS\b') { Add-Error 'El script 06 no puede crear SEQ_RL_MR_TRAZAS.' }
Assert-Contains $script06 "'RL_MR_TRAZAS_CALCULO'" 'El script 06 debe retirar RL_MR_TRAZAS_CALCULO durante la transición controlada.'

foreach ($token in @(
    'Task<bool> VincularEvidenciaAsync(VincularEvidenciaDto dto',
    'Task<IReadOnlyList<RiesgoReporteFilaDto>> ObtenerConsolidadoTipadoAsync()',
    'Task<MetodologiaFormularioDto?> ObtenerMetodologiaDinamicaVigenteAsync()'
)) {
    Assert-Contains $repositoryContract $token "IMatricesRiesgosRepository no contiene '$token'."
}
foreach ($token in @('ServiceResult<IReadOnlyList<RiesgoReporteFilaDto>>','ServiceResult<MetodologiaFormularioDto>')) {
    Assert-Contains $appServiceContract $token "IMatricesRiesgosAppService no contiene '$token'."
}
foreach ($token in @('ObtenerConsolidadoTipadoAsync','ObtenerMetodologiaDinamicaVigenteAsync')) {
    Assert-Contains $controller $token "MatricesRiesgosController no contiene '$token'."
}
foreach ($token in @('MetodologiaFormulario','SeccionFormulario','CampoFormulario','CatalogoMatrices','ReglaCalculoMatrices','RiesgoReporteFila')) {
    Assert-Contains $angularModels $token "Los modelos Angular no contienen '$token'."
}
Assert-Contains $program 'AddScoped<IMatricesRiesgosRepository, MatricesRiesgosRepository>()' 'Program.cs no registra MatricesRiesgosRepository.'
Assert-NotContains $program 'MatricesRiesgosRepositoryFacade' 'Program.cs referencia la fachada retirada.'

foreach ($token in @(
    'RL_ORACLE_INTEGRATION_REQUIRED','TipoEntidadEvidencia.Evaluacion','RL_MR_EVIDENCIAS_VINCULOS',
    'RL_AUDITORIA','SEQ_RL_AUDITORIA','AuditoriaFallaDespuesDeInsertar','TablasModelo17','SecuenciasModelo17',
    'IndicesPrincipales','RestriccionesPrincipales','RIE_NOMBRE','RIE_USR_CREACION',
    'EsquemaModelo17_InventarioIndicesRestriccionesYAusencias_CumplenContrato',
    'CicloCompleto_Commit_PersisteFamiliaVersionRiesgoEvaluacionProyeccionFlujoEvidenciaVinculoYAuditoria',
    'CicloCompleto_Rollback_NoPersisteRegistrosBase'
)) {
    Assert-Contains $oracleIntegration $token "La suite Oracle no contiene el control '$token'."
}

$securityFiles = @(Get-SourceFiles @('backend','frontend','scripts','.github') @('.cs','.json','.config','.xml','.runsettings','.ps1','.yml','.yaml','.env','.txt'))
$connectionStringPattern = '(?is)(Data\s+Source|Server)\s*=.+?(User\s+Id|UserId|Uid)\s*=.+?(Password|Pwd)\s*=\s*(?!\s*(?:CHANGE_ME|REPLACE_ME|\$\{|<|__|$))'
$jsonOraclePasswordPattern = '(?is)"(?:OracleDB|ConnectionStrings?)"\s*:\s*"[^"\r\n]*(?:Password|Pwd)\s*=\s*(?!\s*(?:CHANGE_ME|REPLACE_ME|\$\{|<|__|$))'
$standalonePasswordPattern = '(?im)^\s*(?:Password|Pwd)\s*=\s*(?!\s*(?:CHANGE_ME|REPLACE_ME|\$\{|<|__|$)).+'
foreach ($file in $securityFiles) {
    if ($file.Name -match '(?i)\.example$|example\.|sample\.') { continue }
    $relative = $file.FullName.Substring(([string]$repositoryRoot).Length).TrimStart('\','/')
    & git -C ([string]$repositoryRoot) check-ignore --quiet -- $relative 2>$null
    if ($LASTEXITCODE -eq 0) { continue }
    $content = [System.IO.File]::ReadAllText($file.FullName)
    $containsSecret = $content -match $connectionStringPattern -or $content -match $jsonOraclePasswordPattern
    if ($file.Extension -ne '.cs') { $containsSecret = $containsSecret -or ($content -match $standalonePasswordPattern) }
    if ($containsSecret) { Add-Error "${relative}: posible credencial Oracle codificada." }
}

foreach ($token in @(
    'WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK',
    "DEFINE autorizacion = '&1'",
    "SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA')",
    "UPPER(TRIM(v_auth)) <> 'EJECUTAR'",
    'UQ_RL_MR_PROY_EVA',
    'IX_RL_MR_PROY_DASHBOARD'
)) {
    Assert-Contains $script05 $token "El script 05 no contiene el control '$token'."
}
if ($script05 -match '(?ms)BEGIN\s+PROMPT') { Add-Error 'El script 05 contiene PROMPT dentro de PL/SQL.' }

if ($errors.Count -gt 0) {
    Write-Host "Validacion integral de Matrices: FALLO ($($errors.Count) hallazgos)." -ForegroundColor Red
    foreach ($item in $errors) { Write-Host "- $item" -ForegroundColor Red }
    exit 1
}

Write-Host 'Validacion integral de Matrices contra DDL, contratos neutros, auditoria fisica Oracle 11g, transacciones y seguridad: CORRECTA.' -ForegroundColor Green
Write-Host "Archivos del modulo revisados: $($moduleFiles.Count). Archivos de seguridad revisados: $($securityFiles.Count)." -ForegroundColor Green
exit 0
