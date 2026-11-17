using RL.API.Features.Listas.Contracts;
using Xunit;

namespace RL.API.Tests.Features.Listas;

public sealed class IdentidadMonitoreoPolicyTests
{
    [Theory]
    [InlineData("N/A")]
    [InlineData("N/A(3)")]
    [InlineData("NULL")]
    [InlineData("sin identificación")]
    public void MarcadoresNoIdentificacion_NoSeConsideranIdentidadVerificada(string documento)
    {
        Assert.True(IdentidadMonitoreoPolicy.EsMarcadorNoIdentificacion(documento));
        Assert.Equal(EstadoIdentidadMonitoreo.Ambiguous, IdentidadMonitoreoPolicy.EstadoParaPresentacion(documento));
    }

    [Theory]
    [InlineData(" 0801-1234 ", "0801-1234")]
    [InlineData(" 0801 1234 ", "0801 1234")]
    public void NormalizarDocumento_ConservaElIdentificadorYEliminaSoloEspaciosExternos(string documento, string esperado)
    {
        Assert.Equal(esperado, IdentidadMonitoreoPolicy.NormalizarDocumento(documento));
    }

    [Fact]
    public void DocumentoVacio_SeMarcaComoInvalido()
    {
        Assert.Equal(EstadoIdentidadMonitoreo.Invalid, IdentidadMonitoreoPolicy.EstadoParaPresentacion("   "));
    }
}
