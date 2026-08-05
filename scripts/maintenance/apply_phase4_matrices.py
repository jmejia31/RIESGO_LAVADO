from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]
REPOSITORY_PATH = ROOT / "backend/RL.API/Features/MatricesRiesgos/Persistence/MatricesRiesgosRepository.cs"
EVIDENCE_DTOS_PATH = ROOT / "backend/RL.API/Features/MatricesRiesgos/Contracts/Evidencias/EvidenciaDtos.cs"
PERMISSION_PATH = ROOT / "backend/RL.API/Features/MatricesRiesgos/Contracts/Configuracion/PermisoFormularioDto.cs"
VALIDATOR_PATH = ROOT / "scripts/validation/validate_matrices_dynamic_ddl_alignment.ps1"
TEST_PATH = ROOT / "backend/RL.API.Tests/Features/MatricesRiesgos/MatricesRiesgosEvidenceContractTests.cs"
WORKFLOW_PATH = ROOT / ".github/workflows/phase4-remove-legacy.yml"
THIS_SCRIPT = Path(__file__).resolve()


def replace_once(content: str, pattern: str, replacement: str, description: str) -> str:
    updated, count = re.subn(pattern, replacement, content, count=1, flags=re.S)
    if count != 1:
        raise RuntimeError(f"No se modificó exactamente una vez: {description}. Coincidencias: {count}.")
    return updated


repository = REPOSITORY_PATH.read_text(encoding="utf-8")
repository = replace_once(
    repository,
    r"\n    // Compatibilidad de prueba Oracle: no existe endpoint ni contrato de aplicación para esta tabla retirada\.\n"
    r"    public Task<bool> VincularEvidenciaAprobacionAsync\(.*?\n"
    r"    public async Task<ResultadoEliminacionEvidencia>",
    "\n    public async Task<ResultadoEliminacionEvidencia>",
    "adaptador temporal de aprobación",
)
repository = replace_once(
    repository,
    r"\n    private async Task<bool> EjecutarVinculoEvidenciaAsync\(.*?\n"
    r"    private static MetodologiaFormularioDto ConstruirMetodologiaDinamica",
    "\n    private static MetodologiaFormularioDto ConstruirMetodologiaDinamica",
    "helper dinámico de tablas puente",
)

for token in (
    "VincularEvidenciaAprobacionAsync",
    "AsociarEvidenciaAprobacionDto",
    "EjecutarVinculoEvidenciaAsync",
    "RL_MR_EVI_APROBACION",
    "tablaPuente",
    "columnaEntidad",
    "columnaEvidencia",
):
    if token in repository:
        raise RuntimeError(f"El repositorio conserva el residuo heredado: {token}.")

for token in (
    "public async Task<bool> VincularEvidenciaAsync",
    "INSERT INTO RL_MR_EVIDENCIAS_VINCULOS",
    "ObtenerConsultaEntidadEvidencia",
    "SEQ_RL_MR_EVI_VINCULOS",
):
    if token not in repository:
        raise RuntimeError(f"El repositorio perdió el contrato vigente: {token}.")

REPOSITORY_PATH.write_text(repository, encoding="utf-8")

evidence_dtos = EVIDENCE_DTOS_PATH.read_text(encoding="utf-8")
evidence_dtos = replace_once(
    evidence_dtos,
    r"\n/// <summary>\n/// Contrato temporal exclusivo de la prueba Oracle pendiente\. No se expone por API\.\n/// </summary>\n"
    r"public sealed class AsociarEvidenciaAprobacionDto\n\{.*?\n\}\n?$",
    "\n",
    "DTO temporal AsociarEvidenciaAprobacionDto",
)
if "AsociarEvidenciaAprobacionDto" in evidence_dtos:
    raise RuntimeError("No se retiró completamente AsociarEvidenciaAprobacionDto.")
EVIDENCE_DTOS_PATH.write_text(evidence_dtos, encoding="utf-8")

if not PERMISSION_PATH.exists():
    raise RuntimeError("No se encontró PermisoFormularioDto.cs para su retiro controlado.")
PERMISSION_PATH.unlink()

TEST_PATH.write_text(
    '''using System;
using System.Linq;
using System.Reflection;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Persistence;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatricesRiesgosEvidenceContractTests
{
    [Fact]
    public void RepositorioYContrato_ExponenSoloVinculoGenerico()
    {
        string[] contrato = typeof(IMatricesRiesgosRepository)
            .GetMethods()
            .Where(m => m.Name.Contains("VincularEvidencia", StringComparison.Ordinal))
            .Select(m => m.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        string[] implementacion = typeof(MatricesRiesgosRepository)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.Name.Contains("VincularEvidencia", StringComparison.Ordinal))
            .Select(m => m.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(new[] { nameof(IMatricesRiesgosRepository.VincularEvidenciaAsync) }, contrato);
        Assert.Equal(new[] { nameof(MatricesRiesgosRepository.VincularEvidenciaAsync) }, implementacion);
    }

    [Fact]
    public void Ensamblado_NoContieneContratosTemporalesRetirados()
    {
        Assembly assembly = typeof(VincularEvidenciaDto).Assembly;

        Assert.Null(assembly.GetType(
            "RL.API.Features.MatricesRiesgos.Contracts.AsociarEvidenciaAprobacionDto"));
        Assert.Null(assembly.GetType(
            "RL.API.Features.MatricesRiesgos.Contracts.PermisoFormularioDto"));
    }

    [Fact]
    public void TipoEntidadEvidencia_ConservaListaCerradaDeSieteDestinos()
    {
        string[] esperados =
        {
            "Riesgo",
            "Evaluacion",
            "Control",
            "Plan",
            "Actividad",
            "Alerta",
            "Automonitoreo"
        };

        Assert.Equal(esperados, Enum.GetNames<TipoEntidadEvidencia>());
    }
}
''',
    encoding="utf-8",
)

validator = VALIDATOR_PATH.read_text(encoding="utf-8")
marker = "$securityFiles = @("
if marker not in validator:
    raise RuntimeError("No se encontró el punto de extensión del validador.")
if "$phase4ForbiddenTokens" in validator:
    raise RuntimeError("El validador ya contiene el bloque de Fase 4; se evita duplicarlo.")

phase4_block = r'''
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

'''
validator = validator.replace(marker, phase4_block + marker, 1)
VALIDATOR_PATH.write_text(validator, encoding="utf-8")

for temporary_path in (WORKFLOW_PATH, THIS_SCRIPT):
    if temporary_path.exists():
        temporary_path.unlink()
