using System;
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
