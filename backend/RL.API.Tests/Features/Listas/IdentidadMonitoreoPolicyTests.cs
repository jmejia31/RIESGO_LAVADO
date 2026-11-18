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

    [Fact]
    public void DocumentoNoVacioSinCardinalidad_NoSeMarcaComoVerificado()
    {
        Assert.Equal(
            EstadoIdentidadMonitoreo.UnverifiedAssociation,
            IdentidadMonitoreoPolicy.EstadoParaPresentacion("0801-1234"));
    }

    [Theory]
    [InlineData("08011999123456", false, "VERIFIED")]
    [InlineData("08011999123456", true, "UNVERIFIED_ASSOCIATION")]
    [InlineData("N/A(3)", false, "AMBIGUOUS")]
    [InlineData("N/A", true, "AMBIGUOUS")]
    [InlineData("", false, "INVALID")]
    [InlineData(" ", false, "INVALID")]
    public void Juridicas_ExponenIdentidadOperableSoloConFuenteInstitucional(
        string numeroPatrono, bool esManual, string esperado)
    {
        var coincidencia = new CoincidenciaJuridicaDto
        {
            NumeroPatrono = numeroPatrono,
            EsManual = esManual
        };
        Assert.Equal(esperado, coincidencia.EstadoIdentidad);
        Assert.Equal(esperado, coincidencia.EstadoAsociacion);

        var json = System.Text.Json.JsonSerializer.Serialize(
            coincidencia, new System.Text.Json.JsonSerializerOptions
            { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
        Assert.Contains($"\"estadoIdentidad\":\"{esperado}\"", json, StringComparison.Ordinal);
        Assert.Contains($"\"estadoAsociacion\":\"{esperado}\"", json, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(1, "VERIFIED")]
    [InlineData(2, "AMBIGUOUS")]
    [InlineData(0, "UNVERIFIED_ASSOCIATION")]
    public void Asociacion_DniDeFuenteRepetidoNoHabilitaMotivosPorDocumento(int coincidenciasFuente, string esperado)
    {
        Assert.Equal(EstadoIdentidadMonitoreo.Verified,
            IdentidadMonitoreoPolicy.EstadoParaPresentacion("0801-1234", 1, 1));
        Assert.Equal(esperado,
            IdentidadMonitoreoPolicy.EstadoAsociacionParaPresentacion("0801-1234", 1, 1, coincidenciasFuente));
    }

    [Theory]
    [InlineData(1, 1, "VERIFIED")]
    [InlineData(2, 1, "VERIFIED")]
    [InlineData(2, 2, "AMBIGUOUS")]
    [InlineData(1, 0, "UNVERIFIED_ASSOCIATION")]
    [InlineData(0, 0, "UNVERIFIED_ASSOCIATION")]
    public void EstadoParaPresentacion_UsaCardinalidadDeFuente(int referencias, int sujetos, string esperado)
    {
        Assert.Equal(esperado, IdentidadMonitoreoPolicy.EstadoParaPresentacion("0801-1234", referencias, sujetos));
    }
}
