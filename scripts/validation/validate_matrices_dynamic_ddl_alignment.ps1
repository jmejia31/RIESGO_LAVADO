$ErrorActionPreference = 'Stop'

$repositoryRoot = Resolve-Path (Join-Path $PSScriptRoot '../..')
$script05 = Join-Path $repositoryRoot 'database/19_matrices_riesgos/instalacion/05_ajustes_dashboard_seguridad_reportes.sql'
$script06 = Join-Path $repositoryRoot 'database/19_matrices_riesgos/transicion/06_reconstruir_modelo_17_tablas.sql'
$workflowTemporal = Join-Path $repositoryRoot '.github/workflows/agent-fix-matrices-phase1.yml'
$repositoryFacade = Join-Path $repositoryRoot 'backend/RL.API/Features/MatricesRiesgos/Persistence/MatricesRiesgosRepositoryFacade.cs'
$legacyDtos = Join-Path $repositoryRoot 'backend/RL.API/Features/MatricesRiesgos/Contracts/Matrices/MatrizRiesgoDtos.cs'
$legacyReportDtos = Join-Path $repositoryRoot 'backend/RL.API/Features/MatricesRiesgos/Contracts/Reporteria/ReporteriaDtos.cs'
$legacyRenderer = Join-Path $repositoryRoot 'backend/RL.API/Features/MatricesRiesgos/Application/MatricesRiesgosReportRenderer.cs'
$repositoryFile = Join-Path $repositoryRoot 'backend/RL.API/Features/MatricesRiesgos/Persistence/MatricesRiesgosRepository.cs'
$repositoryContract = Join-Path $repositoryRoot 'backend/RL.API/Features/MatricesRiesgos/Persistence/IMatricesRiesgosRepository.cs'
$appServiceContract = Join-Path $repositoryRoot 'backend/RL.API/Features/MatricesRiesgos/Application/IMatricesRiesgosAppService.cs'
$controllerFile = Join-Path $repositoryRoot 'backend/RL.API/Features/MatricesRiesgos/MatricesRiesgosController.cs'
$angularModels = Join-Path $repositoryRoot 'frontend/rl-app/src/app/features/admin/matrices-riesgos/models/matrices-riesgos.models.ts'
$programFile = Join-Path $repositoryRoot 'backend/RL.API/Program.cs'
$oracleIntegrationTest = Join-Path $repositoryRoot 'backend/RL.API.Tests/Features/MatricesRiesgos/MatricesRiesgosRepositoryIntegrationTests.cs'

$moduleScanRoots = @(
    (Join-Path $repositoryRoot 'backend/RL.API/Features/MatricesRiesgos'),
    (Join-Path $repositoryRoot 'backend/RL.API.Tests/Features/MatricesRiesgos'),
    (Join-Path $repositoryRoot 'frontend/rl-app/src/app/features/admin/matrices-riesgos'),
    (Join-Path $repositoryRoot 'database/19_matrices_riesgos')
)

$traceScanRoots = @(
    (Join-Path $repositoryRoot 'backend/RL.API/Features/MatricesRiesgos'),
    (Join-Path $repositoryRoot 'backend/RL.API.Tests/Features/MatricesRiesgos'),
    (Join-Path $repositoryRoot 'frontend/rl-app/src/app/features/admin/matrices-riesgos')
)

$securityScanRoots = @(
    (Join-Path $repositoryRoot 'backend'),
    (Join-Path $repositoryRoot 'frontend'),
    (Join-Path $repositoryRoot 'scripts'),
    (Join-Path $repositoryRoot '.github')
)

$moduleExtensions = @('.cs', '.ts', '.html', '.sql', '.json')
$securityExtensions = @('.cs', '.json', '.config', '.xml', '.runsettings', '.ps1', '.yml', '.yaml', '.env', '.txt')
$excludedDirectoryNames = @('bin', 'obj', 'node_modules', 'dist', 'coverage', 'Historico', 'retiro_controlado', 'transicion', '.git')
$errors = New-Object System.Collections.Generic.List[string]

function Test-IsExcludedPath {
    param([System.IO.FileInfo]$File)
    foreach ($segment in ($File.FullName -split '[\\/]')) {
        if ($excludedDirectoryNames -contains $segment) { return $true }
    }
    return $false
}

function Get-SourceFiles {
    param([string[]]$Roots, [string[]]$Extensions)
    $result = New-Object System.Collections.Generic.List[System.IO.FileInfo]
    foreach ($root in $Roots) {
        if (-not (Test-Path -LiteralPath $root)) {
            $errors.Add("No se encontró una raíz obligatoria: $root")
            continue
        }
        Get-ChildItem -LiteralPath $root -Recurse -File | Where-Object {
            $Extensions -contains $_.Extension.ToLowerInvariant() -and -not (Test-IsExcludedPath -File $_)
        } | ForEach-Object { $result.Add($_) }
    }
    return $result
}

function Get-RelativeRepositoryPath {
    param([string]$Path)
    $root = ([string]$repositoryRoot).TrimEnd('\', '/')
    return $Path.Substring($root.Length).TrimStart('\', '/')
}

function Test-IsIgnoredLocalFile {
    param([System.IO.FileInfo]$File)
    $relativePath = Get-RelativeRepositoryPath -Path $File.FullName
    & git -C ([string]$repositoryRoot) check-ignore --quiet -- $relativePath 2>$null
    return $LASTEXITCODE -eq 0
}

foreach ($requiredFile in @(
    $script05,
    $script06,
    $repositoryFile,
    $repositoryContract,
    $appServiceContract,
    $controllerFile,
    $angularModels,
    $programFile,
    $oracleIntegrationTest)) {
    if (-not (Test-Path -LiteralPath $requiredFile)) {
        $errors.Add("No se encontró un archivo obligatorio: $requiredFile")
    }
}

foreach ($forbiddenFile in @($workflowTemporal, $repositoryFacade, $legacyDtos, $legacyReportDtos, $legacyRenderer)) {
    if (Test-Path -LiteralPath $forbiddenFile) {
        $errors.Add("No debe permanecer el archivo retirado: $(Get-RelativeRepositoryPath -Path $forbiddenFile)")
    }
}

$forbiddenTokens = @(
    [pscustomobject]@{ Token = 'FLU_ESTADO_NUEVO'; Message = 'La tabla definitiva solo contiene FLU_ESTADO.' },
    [pscustomobject]@{ Token = 'FLU_ESTADO_ANTERIOR'; Message = 'La tabla definitiva solo contiene FLU_ESTADO.' },
    [pscustomobject]@{ Token = 'EVA_ESTADO'; Message = 'El estado procede del último flujo.' },
    [pscustomobject]@{ Token = 'EVA_VRI'; Message = 'VRI no es una columna de evaluaciones.' },
    [pscustomobject]@{ Token = 'EVA_ETP'; Message = 'ETP no es una columna de evaluaciones.' },
    [pscustomobject]@{ Token = 'EVA_VRR'; Message = 'VRR no es una columna de evaluaciones.' },
    [pscustomobject]@{ Token = 'EVA_FECHA_EVAL'; Message = 'La columna física es EVA_FECHA_REGISTRO.' },
    [pscustomobject]@{ Token = 'EVA_USR_EVAL'; Message = 'La columna física es EVA_USR_REGISTRO.' },
    [pscustomobject]@{ Token = 'PROY_ETP'; Message = 'La proyección definitiva no contiene ETP.' },
    [pscustomobject]@{ Token = 'RL_MR_MODELOS'; Message = 'Tabla retirada del modelo dinámico.' },
    [pscustomobject]@{ Token = 'RL_MR_FACTORES'; Message = 'Tabla retirada del modelo dinámico.' },
    [pscustomobject]@{ Token = 'RL_MR_VARIABLES'; Message = 'Tabla retirada del modelo dinámico.' },
    [pscustomobject]@{ Token = 'RL_MR_ESCALAS'; Message = 'Tabla retirada del modelo dinámico.' },
    [pscustomobject]@{ Token = 'RL_MR_CRITERIOS'; Message = 'Tabla retirada del modelo dinámico.' },
    [pscustomobject]@{ Token = 'ModeloId'; Message = 'Contrato del modelo heredado.' },
    [pscustomobject]@{ Token = 'ModeloVersion'; Message = 'Contrato del modelo heredado.' },
    [pscustomobject]@{ Token = 'FactorInstitucionalDto'; Message = 'Contrato de factores heredado.' },
    [pscustomobject]@{ Token = 'VariableMetodologiaRespuestaDto'; Message = 'Contrato de variables heredado.' },
    [pscustomobject]@{ Token = 'MatrizRiesgoResumenDto'; Message = 'Contrato de matriz basada en sujeto retirado.' },
    [pscustomobject]@{ Token = 'MatrizRiesgoDetalleDto'; Message = 'Contrato de matriz basada en sujeto retirado.' },
    [pscustomobject]@{ Token = 'MatrizRiesgoVariableDetalleDto'; Message = 'Contrato de variables retirado.' },
    [pscustomobject]@{ Token = 'PorFactor'; Message = 'Agrupación del modelo heredado.' },
    [pscustomobject]@{ Token = 'factorId'; Message = 'Identificador de factor retirado del contrato funcional.' },
    [pscustomobject]@{ Token = 'variableId'; Message = 'Identificador de variable retirado del contrato funcional.' },
    [pscustomobject]@{ Token = 'FactorId'; Message = 'Identificador de factor retirado del contrato funcional.' },
    [pscustomobject]@{ Token = 'VariableId'; Message = 'Identificador de variable retirado del contrato funcional.' },
    [pscustomobject]@{ Token = 'List<Dictionary<string, object>>'; Message = 'Los reportes deben usar DTOs tipados.' },
    [pscustomobject]@{ Token = 'DeterminarClasificacionResidual'; Message = 'La clasificación no puede ser rígida en código.' },
    [pscustomobject]@{ Token = 'RegistrarAuditoriaAsync'; Message = 'El contrato institucional expone RegistrarAsync.' }
)

$moduleFiles = Get-SourceFiles -Roots $moduleScanRoots -Extensions $moduleExtensions
foreach ($file in $moduleFiles) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    foreach ($entry in $forbiddenTokens) {
        if ($content.Contains($entry.Token)) {
            $relativePath = Get-RelativeRepositoryPath -Path $file.FullName
            foreach ($match in (Select-String -LiteralPath $file.FullName -SimpleMatch $entry.Token)) {
                $errors.Add("${relativePath}:$($match.LineNumber): identificador incompatible '$($entry.Token)'. $($entry.Message)")
            }
        }
    }
}

$traceForbiddenTokens = @(
    [pscustomobject]@{ Token = 'InsertarTrazaCalculoAsync'; Message = 'El modelo reducido no escribe trazas locales de cálculo.' },
    [pscustomobject]@{ Token = 'RL_MR_TRAZAS_CALCULO'; Message = 'La tabla de trazas fue retirada del modelo objetivo.' },
    [pscustomobject]@{ Token = 'SEQ_RL_MR_TRAZAS'; Message = 'La secuencia de trazas fue retirada del modelo objetivo.' },
    [pscustomobject]@{ Token = 'TRA_REGLA_ID'; Message = 'La regla utilizada se conserva dentro de EVA_CALCULOS_JSON.' }
)

$traceFiles = @(
    Get-SourceFiles -Roots $traceScanRoots -Extensions $moduleExtensions | Where-Object {
        $_.FullName -ne $oracleIntegrationTest
    }
)
foreach ($file in $traceFiles) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    foreach ($entry in $traceForbiddenTokens) {
        if ($content.Contains($entry.Token)) {
            $relativePath = Get-RelativeRepositoryPath -Path $file.FullName
            foreach ($match in (Select-String -LiteralPath $file.FullName -SimpleMatch $entry.Token)) {
                $errors.Add("${relativePath}:$($match.LineNumber): traza incompatible '$($entry.Token)'. $($entry.Message)")
            }
        }
    }
}

if (Test-Path -LiteralPath $repositoryFile) {
    $content = Get-Content -LiteralPath $repositoryFile -Raw

    foreach ($token in @('EVA_DATA_JSON', 'EVA_DATA_CALC_JSON')) {
        if ($content.Contains($token)) {
            $errors.Add("MatricesRiesgosRepository.cs conserva la columna física retirada '$token'.")
        }
    }

    foreach ($token in @(
        'InsertarAuditoriaCampoAsync',
        'INSERT INTO RL_MR_AUDITORIA',
        'SEQ_RL_MR_AUDITORIA',
        'IAuditoriaRepository? _auditoriaRepository',
        'this(db, null)')) {
        if ($content.Contains($token)) {
            $errors.Add("MatricesRiesgosRepository.cs conserva auditoría local o inyección opcional retirada: '$token'.")
        }
    }

    $required = [ordered]@{
        'command.Transaction = transaction' = 'Los comandos deben propagar OracleTransaction.'
        'FLU_ESTADO' = 'El estado debe proceder de flujos.'
        "VER_ESTADO = 'PUBLISHED'" = 'La metodología y reglas deben usar versiones publicadas.'
        'REG_CODIGO = :codigo' = 'La regla debe resolverse por código.'
        'REG_VERSION = :version' = 'La regla debe resolverse por versión.'
        'REG_ALGORITMO_ID' = 'El algoritmo debe resolverse desde el catálogo institucional de reglas.'
        'EVA_DATOS_JSON' = 'Las respuestas deben usar el nombre físico aprobado por el DDL reducido.'
        'EVA_CALCULOS_JSON' = 'Los cálculos deben usar el nombre físico aprobado por el DDL reducido.'
        'IncorporarMetadatosRegla' = 'El servidor debe incorporar metadatos de la regla al resultado calculado.'
        'reglaCodigo' = 'El resultado calculado debe conservar el código de regla.'
        'reglaVersion' = 'El resultado calculado debe conservar la versión de regla.'
        'algoritmoId' = 'El resultado calculado debe conservar el identificador de algoritmo.'
        'Task<IReadOnlyList<RiesgoReporteFilaDto>> ObtenerConsolidadoTipadoAsync' = 'El consolidado debe ser tipado.'
        'Task<MetodologiaFormularioDto?> ObtenerMetodologiaDinamicaVigenteAsync' = 'La metodología debe usar contrato neutro.'
        'VersionFormularioId = versionId' = 'La metodología debe conservar la versión del formulario.'
        'Secciones = secciones' = 'La metodología debe conservar secciones y campos.'
        'Catalogos = catalogos' = 'La metodología debe conservar catálogos.'
        'Reglas = reglas' = 'La metodología debe conservar reglas.'
        'VincularEvidenciaAsync' = 'La vinculación funcional debe usar el contrato genérico.'
        'RL_MR_EVIDENCIAS_VINCULOS' = 'La vinculación funcional debe usar la tabla genérica vigente.'
            'private readonly IAuditoriaRepository _auditoriaRepository;' = 'La auditoría institucional debe ser obligatoria.'
            'IAuditoriaRepository auditoriaRepository' = 'El repositorio debe exigir auditoría institucional por constructor.'
            '"CREAR_EVALUACION"' = 'La creación debe registrarse en RL_AUDITORIA.'
            '"ACTUALIZAR_EVALUACION"' = 'La actualización debe registrarse en RL_AUDITORIA.'
            '"TRANSICION_ESTADO"' = 'La transición debe registrarse en RL_AUDITORIA.'
    }
    foreach ($entry in $required.GetEnumerator()) {
        if (-not $content.Contains($entry.Key)) {
            $errors.Add("MatricesRiesgosRepository.cs no contiene '$($entry.Key)'. $($entry.Value)")
        }
    }
    if ($content.Contains('NotSupportedException')) {
        $errors.Add('MatricesRiesgosRepository.cs contiene NotSupportedException.')
    }
}

if (Test-Path -LiteralPath $script06) {
    $content = Get-Content -LiteralPath $script06 -Raw
    foreach ($token in @('EVA_DATOS_JSON', 'EVA_CALCULOS_JSON')) {
        if (-not $content.Contains($token)) {
            $errors.Add("El script 06 no contiene la columna física obligatoria '$token'.")
        }
    }
    foreach ($token in @('EVA_DATA_JSON', 'EVA_DATA_CALC_JSON')) {
        if ($content.Contains($token)) {
            $errors.Add("El script 06 conserva la columna física retirada '$token'.")
        }
    }
    foreach ($pattern in @(
        'CREATE TABLE\s+RL_MR_AUDITORIA',
        'CREATE SEQUENCE\s+SEQ_RL_MR_AUDITORIA')) {
        if ($content -match $pattern) {
            $errors.Add("El script 06 vuelve a crear un objeto de auditoría local retirado: $pattern")
        }
    }
    if (-not $content.Contains("'RL_MR_AUDITORIA'")) {
        $errors.Add('El script 06 debe conservar el retiro controlado de RL_MR_AUDITORIA heredada.')
    }

    if ($content -match '(?im)^\s*CREATE\s+TABLE\s+RL_MR_TRAZAS_CALCULO\b') {
        $errors.Add('El script 06 no puede crear RL_MR_TRAZAS_CALCULO.')
    }
    if ($content -match '(?im)^\s*CREATE\s+SEQUENCE\s+SEQ_RL_MR_TRAZAS\b') {
        $errors.Add('El script 06 no puede crear SEQ_RL_MR_TRAZAS.')
    }
    if (-not $content.Contains("'RL_MR_TRAZAS_CALCULO'")) {
        $errors.Add('El script 06 debe retirar la tabla heredada RL_MR_TRAZAS_CALCULO durante la reconstrucción controlada.')
    }
}

if (Test-Path -LiteralPath $repositoryContract) {
    $content = Get-Content -LiteralPath $repositoryContract -Raw
    if (-not $content.Contains('Task<bool> VincularEvidenciaAsync(VincularEvidenciaDto dto')) {
        $errors.Add('IMatricesRiesgosRepository no expone el vínculo genérico de evidencias.')
    }
    if (-not $content.Contains('Task<IReadOnlyList<RiesgoReporteFilaDto>> ObtenerConsolidadoTipadoAsync()')) {
        $errors.Add('IMatricesRiesgosRepository no expone el consolidado tipado.')
    }
    if (-not $content.Contains('Task<MetodologiaFormularioDto?> ObtenerMetodologiaDinamicaVigenteAsync()')) {
        $errors.Add('IMatricesRiesgosRepository no expone metodología neutra.')
    }
}

if (Test-Path -LiteralPath $appServiceContract) {
    $content = Get-Content -LiteralPath $appServiceContract -Raw
    if (-not $content.Contains('ServiceResult<IReadOnlyList<RiesgoReporteFilaDto>>')) {
        $errors.Add('IMatricesRiesgosAppService no expone filas tipadas.')
    }
    if (-not $content.Contains('ServiceResult<MetodologiaFormularioDto>')) {
        $errors.Add('IMatricesRiesgosAppService no expone metodología neutra.')
    }
}

if (Test-Path -LiteralPath $controllerFile) {
    $content = Get-Content -LiteralPath $controllerFile -Raw
    if (-not $content.Contains('ObtenerConsolidadoTipadoAsync')) {
        $errors.Add('El endpoint consolidado no consume el contrato tipado.')
    }
    if (-not $content.Contains('ObtenerMetodologiaDinamicaVigenteAsync')) {
        $errors.Add('El endpoint de metodología no consume el contrato neutro.')
    }
}

if (Test-Path -LiteralPath $angularModels) {
    $content = Get-Content -LiteralPath $angularModels -Raw
    foreach ($token in @('MetodologiaFormulario', 'SeccionFormulario', 'CampoFormulario', 'CatalogoMatrices', 'ReglaCalculoMatrices', 'RiesgoReporteFila')) {
        if (-not $content.Contains($token)) {
            $errors.Add("Los modelos Angular no contienen el contrato neutro '$token'.")
        }
    }
}

if (Test-Path -LiteralPath $oracleIntegrationTest) {
    $content = Get-Content -LiteralPath $oracleIntegrationTest -Raw
    foreach ($token in @(
        'RL_ORACLE_INTEGRATION_REQUIRED',
        'TipoEntidadEvidencia.Evaluacion',
        'RL_MR_EVIDENCIAS_VINCULOS',
        'RL_AUDITORIA',
        'SEQ_RL_AUDITORIA',
        'AuditoriaFallaDespuesDeInsertar',
        'TablasModelo17',
        'SecuenciasModelo17',
        'IndicesPrincipales',
        'RestriccionesPrincipales',
        'RIE_NOMBRE',
        'RIE_USR_CREACION',
        'EsquemaModelo17_InventarioIndicesRestriccionesYAusencias_CumplenContrato',
        'CicloCompleto_Commit_PersisteFamiliaVersionRiesgoEvaluacionProyeccionFlujoEvidenciaVinculoYAuditoria',
        'CicloCompleto_Rollback_NoPersisteRegistrosBase')) {
        if (-not $content.Contains($token)) {
            $errors.Add("La suite Oracle del modelo reducido no contiene el control obligatorio '$token'.")
        }
    }

    $retiredOracleObjects = @(
        'RL_MR_EVI_APROBACION',
        'RL_MR_EVI_REVISION',
        'RL_MR_EVI_AUTOMONITOREO',
        'RL_MR_EVI_ALERTA',
        'RL_MR_EVI_ACTIVIDAD',
        'RL_MR_EVI_PLAN',
        'RL_MR_EVI_CONTROL',
        'RL_MR_EVI_EVALUACION',
        'RL_MR_EVI_RIESGO',
        'RL_MR_DETALLES_IMPORTACION',
        'RL_MR_LOTES_IMPORTACION',
        'RL_MR_TRAZAS_CALCULO',
        'RL_MR_AUDITORIA',
        'RL_MR_PERMISOS_FORMULARIO',
        'RL_MR_APROBACIONES_FORMULARIO',
        'RL_MR_CAMPOS_FORMULARIO',
        'RL_MR_RELACIONES_RIESGO',
        'RL_MR_REVISIONES_EVALUACION',
        'SEQ_RL_MR_AUDITORIA',
        'SEQ_RL_MR_TRAZAS',
        'SEQ_RL_MR_REVISIONES')

    foreach ($token in $retiredOracleObjects) {
        $escaped = [regex]::Escape($token)
        $activeSqlPattern = "(?im)\b(?:INSERT\s+INTO|UPDATE|DELETE\s+FROM|MERGE\s+INTO|FROM)\s+$escaped\b"
        if ($content -match $activeSqlPattern) {
            $errors.Add("La suite Oracle ejecuta SQL activo contra el objeto heredado '$token'.")
        }
    }

    if ($content.Contains('TRA_REGLA_ID')) {
        $errors.Add("La suite Oracle reintroduce la columna heredada 'TRA_REGLA_ID'.")
    }
}

if (Test-Path -LiteralPath $programFile) {
    $content = Get-Content -LiteralPath $programFile -Raw
    if (-not $content.Contains('AddScoped<IMatricesRiesgosRepository, MatricesRiesgosRepository>()')) {
        $errors.Add('Program.cs no registra directamente MatricesRiesgosRepository.')
    }
    if ($content.Contains('MatricesRiesgosRepositoryFacade')) {
        $errors.Add('Program.cs referencia la fachada retirada.')
    }
}


$phase4EvidenceDtos = Join-Path $repositoryRoot 'backend/RL.API/Features/MatricesRiesgos/Contracts/Evidencias/EvidenciaDtos.cs'
$phase4PermissionContract = Join-Path $repositoryRoot 'backend/RL.API/Features/MatricesRiesgos/Contracts/Configuracion/PermisoFormularioDto.cs'
if (Test-Path -LiteralPath $phase4PermissionContract) {
    $errors.Add('No debe permanecer PermisoFormularioDto.cs en el modelo reducido.')
}
if (Test-Path -LiteralPath $phase4EvidenceDtos) {
    $content = Get-Content -LiteralPath $phase4EvidenceDtos -Raw
    if ($content.Contains('AsociarEvidenciaAprobacionDto')) {
        $errors.Add('EvidenciaDtos.cs conserva el DTO temporal de aprobación.')
    }
}

$phase4ScanRoots = @(
    (Join-Path $repositoryRoot 'backend/RL.API/Features/MatricesRiesgos'),
    (Join-Path $repositoryRoot 'frontend/rl-app/src/app/features/admin/matrices-riesgos')
)
$phase4ForbiddenTokens = @(
    [pscustomobject]@{ Token = 'VincularEvidenciaAprobacionAsync'; Message = 'El adaptador de aprobación fue retirado.' },
    [pscustomobject]@{ Token = 'AsociarEvidenciaAprobacionDto'; Message = 'El DTO temporal de aprobación fue retirado.' },
    [pscustomobject]@{ Token = 'EjecutarVinculoEvidenciaAsync'; Message = 'No se permite un helper dinámico hacia tablas puente.' },
    [pscustomobject]@{ Token = 'RL_MR_EVI_RIESGO'; Message = 'La tabla puente específica fue retirada.' },
    [pscustomobject]@{ Token = 'RL_MR_EVI_EVALUACION'; Message = 'La tabla puente específica fue retirada.' },
    [pscustomobject]@{ Token = 'RL_MR_EVI_CONTROL'; Message = 'La tabla puente específica fue retirada.' },
    [pscustomobject]@{ Token = 'RL_MR_EVI_PLAN'; Message = 'La tabla puente específica fue retirada.' },
    [pscustomobject]@{ Token = 'RL_MR_EVI_ACTIVIDAD'; Message = 'La tabla puente específica fue retirada.' },
    [pscustomobject]@{ Token = 'RL_MR_EVI_ALERTA'; Message = 'La tabla puente específica fue retirada.' },
    [pscustomobject]@{ Token = 'RL_MR_EVI_AUTOMONITOREO'; Message = 'La tabla puente específica fue retirada.' },
    [pscustomobject]@{ Token = 'RL_MR_EVI_REVISION'; Message = 'La tabla puente específica fue retirada.' },
    [pscustomobject]@{ Token = 'RL_MR_EVI_APROBACION'; Message = 'La tabla puente específica fue retirada.' },
    [pscustomobject]@{ Token = 'PermisoFormularioDto'; Message = 'Los permisos granulares del formulario fueron retirados.' },
    [pscustomobject]@{ Token = 'tablaPuente'; Message = 'No se permite construir destinos SQL dinámicos para evidencias.' },
    [pscustomobject]@{ Token = 'columnaEntidad'; Message = 'No se permite construir columnas dinámicas para tablas puente.' },
    [pscustomobject]@{ Token = 'columnaEvidencia'; Message = 'No se permite construir columnas dinámicas para tablas puente.' }
)

$phase4Files = Get-SourceFiles -Roots $phase4ScanRoots -Extensions $moduleExtensions
foreach ($file in $phase4Files) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    foreach ($entry in $phase4ForbiddenTokens) {
        if ($content.Contains($entry.Token)) {
            $relativePath = Get-RelativeRepositoryPath -Path $file.FullName
            foreach ($match in (Select-String -LiteralPath $file.FullName -SimpleMatch $entry.Token)) {
                $errors.Add("${relativePath}:$($match.LineNumber): contrato heredado '$($entry.Token)'. $($entry.Message)")
            }
        }
    }
}

if (Test-Path -LiteralPath $repositoryFile) {
    $content = Get-Content -LiteralPath $repositoryFile -Raw
    foreach ($token in @(
        'public async Task<bool> VincularEvidenciaAsync',
        'INSERT INTO RL_MR_EVIDENCIAS_VINCULOS',
        'ObtenerConsultaEntidadEvidencia',
        'SEQ_RL_MR_EVI_VINCULOS')) {
        if (-not $content.Contains($token)) {
            $errors.Add("MatricesRiesgosRepository.cs no conserva el vínculo genérico obligatorio '$token'.")
        }
    }
}

if (Test-Path -LiteralPath $repositoryContract) {
    $content = Get-Content -LiteralPath $repositoryContract -Raw
    if ($content.Contains('VincularEvidenciaAprobacionAsync')) {
        $errors.Add('IMatricesRiesgosRepository conserva un vínculo específico retirado.')
    }
}

$securityFiles = @(
    Get-SourceFiles -Roots $securityScanRoots -Extensions $securityExtensions | Where-Object {
        -not (Test-IsIgnoredLocalFile -File $_)
    }
)
$connectionStringPattern = '(?is)(Data\s+Source|Server)\s*=.+?(User\s+Id|UserId|Uid)\s*=.+?(Password|Pwd)\s*=\s*(?!\s*(?:CHANGE_ME|REPLACE_ME|\$\{|<|__|$))'
$jsonOraclePasswordPattern = '(?is)"(?:OracleDB|ConnectionStrings?)"\s*:\s*"[^"\r\n]*(?:Password|Pwd)\s*=\s*(?!\s*(?:CHANGE_ME|REPLACE_ME|\$\{|<|__|$))'
$standalonePasswordPattern = '(?im)^\s*(?:Password|Pwd)\s*=\s*(?!\s*(?:CHANGE_ME|REPLACE_ME|\$\{|<|__|$)).+'
foreach ($file in $securityFiles) {
    if ($file.Name -match '(?i)\.example$|example\.|sample\.') { continue }
    $content = Get-Content -LiteralPath $file.FullName -Raw
    $containsSecret = $content -match $connectionStringPattern -or $content -match $jsonOraclePasswordPattern
    if ($file.Extension -ne '.cs') { $containsSecret = $containsSecret -or ($content -match $standalonePasswordPattern) }
    if ($containsSecret) {
        $errors.Add("$(Get-RelativeRepositoryPath -Path $file.FullName): posible credencial Oracle codificada.")
    }
}

if (Test-Path -LiteralPath $script05) {
    $content = Get-Content -LiteralPath $script05 -Raw
    foreach ($token in @(
        'WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK',
        "DEFINE autorizacion = '&1'",
        "SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA')",
        "UPPER(TRIM(v_auth)) <> 'EJECUTAR'",
        'UQ_RL_MR_PROY_EVA',
        'IX_RL_MR_PROY_DASHBOARD')) {
        if (-not $content.Contains($token)) {
            $errors.Add("El script 05 no contiene: $token")
        }
    }
    if ($content -match '(?ms)BEGIN\s+PROMPT') {
        $errors.Add('El script 05 contiene PROMPT dentro de PL/SQL.')
    }
}

if ($errors.Count -gt 0) {
    Write-Host "Validacion integral de Matrices: FALLO ($($errors.Count) hallazgos)." -ForegroundColor Red
    foreach ($item in $errors) {
        Write-Host "- $item" -ForegroundColor Red
    }
    exit 1
}

Write-Host 'Validacion integral de Matrices contra DDL, contratos neutros, metadatos de cálculo, transacciones y seguridad: CORRECTA.' -ForegroundColor Green
Write-Host "Archivos del modulo revisados: $($moduleFiles.Count). Archivos sin trazas revisados: $($traceFiles.Count). Archivos de seguridad revisados: $($securityFiles.Count)." -ForegroundColor Green
exit 0
