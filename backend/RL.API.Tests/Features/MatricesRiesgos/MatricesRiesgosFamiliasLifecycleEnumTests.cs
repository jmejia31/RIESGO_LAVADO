using RL.API.Features.MatricesRiesgos.Persistence;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatricesRiesgosFamiliasLifecycleEnumTests
{
    [Fact]
    public void CambioEstado_ExponeResultadosFailClosed()
    {
        string[] values = Enum.GetNames<ResultadoCambioEstadoFamiliaFormulario>();

        Assert.Contains(nameof(ResultadoCambioEstadoFamiliaFormulario.Exito), values);
        Assert.Contains(nameof(ResultadoCambioEstadoFamiliaFormulario.NoExiste), values);
        Assert.Contains(nameof(ResultadoCambioEstadoFamiliaFormulario.YaEstabaEnEstado), values);
        Assert.Contains(nameof(ResultadoCambioEstadoFamiliaFormulario.TieneVersionVigente), values);
    }

    [Fact]
    public void Eliminacion_ExponeBloqueoPorVersiones()
    {
        string[] values = Enum.GetNames<ResultadoEliminacionFamiliaFormulario>();

        Assert.Contains(nameof(ResultadoEliminacionFamiliaFormulario.Exito), values);
        Assert.Contains(nameof(ResultadoEliminacionFamiliaFormulario.NoExiste), values);
        Assert.Contains(nameof(ResultadoEliminacionFamiliaFormulario.TieneVersiones), values);
    }
}
