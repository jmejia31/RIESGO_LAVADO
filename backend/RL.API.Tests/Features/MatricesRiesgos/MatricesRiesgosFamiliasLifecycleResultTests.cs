using RL.API.Features.MatricesRiesgos.Persistence;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatricesRiesgosFamiliasLifecycleResultTests
{
    [Theory]
    [InlineData(ResultadoEliminacionFamiliaFormulario.Exito)]
    [InlineData(ResultadoEliminacionFamiliaFormulario.NoExiste)]
    [InlineData(ResultadoEliminacionFamiliaFormulario.TieneVersiones)]
    public void Eliminacion_ResultadosSonDeterministas(ResultadoEliminacionFamiliaFormulario resultado)
    {
        Assert.True(Enum.IsDefined(resultado));
    }

    [Theory]
    [InlineData(ResultadoCambioEstadoFamiliaFormulario.Exito)]
    [InlineData(ResultadoCambioEstadoFamiliaFormulario.NoExiste)]
    [InlineData(ResultadoCambioEstadoFamiliaFormulario.YaEstabaEnEstado)]
    [InlineData(ResultadoCambioEstadoFamiliaFormulario.TieneVersionVigente)]
    public void CambioEstado_ResultadosSonDeterministas(ResultadoCambioEstadoFamiliaFormulario resultado)
    {
        Assert.True(Enum.IsDefined(resultado));
    }
}
