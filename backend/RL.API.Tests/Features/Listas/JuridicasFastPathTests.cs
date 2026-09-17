using System.Reflection;
using RL.API.Features.Listas.Contracts;
using RL.API.Features.Listas.Persistence;
using Xunit;

namespace RL.API.Tests.Features.Listas;

public sealed class JuridicasFastPathTests
{
    [Fact]
    public void PrimeraPaginaConCuatroRegistros_OrdenaYConstruyeTotalesSinMetadataOracle()
    {
        var input = new List<CoincidenciaJuridicaDto>
        {
            new() { Nombre = "Zulu", NumeroPatrono = "3", EsManual = false, TieneMotivo = false },
            new() { Nombre = "Alfa", NumeroPatrono = "2", EsManual = false, TieneMotivo = true },
            new() { Nombre = "Alfa", NumeroPatrono = "1", EsManual = true, TieneMotivo = true },
            new() { Nombre = "Bravo", NumeroPatrono = "4", EsManual = false, TieneMotivo = false }
        };

        var result = CrearRespuesta(input, 10);

        Assert.Equal(new[] { "Alfa:1", "Alfa:2", "Bravo:4", "Zulu:3" }, result.Items.Select(item => $"{item.Nombre}:{item.NumeroPatrono}"));
        Assert.Equal(4, result.TotalRegistros);
        Assert.Equal(1, result.TotalPaginas);
        Assert.Equal(1, result.Totales.Manuales);
        Assert.Equal(1, result.Totales.ConMotivo);
        Assert.Equal(2, result.Totales.Pendientes);
        Assert.Equal(0, result.Totales.CerradosPasivos);
    }

    [Theory]
    [InlineData(1, 4, 10, true)]
    [InlineData(1, 11, 10, false)]
    [InlineData(2, 4, 10, false)]
    public void FastPathSoloSeAutorizaParaPrimeraPaginaQueCabeCompleta(int page, int rows, int pageSize, bool expected)
    {
        var method = typeof(ListasRepository).GetMethod("PuedeUsarFastPathPrimeraPagina", BindingFlags.NonPublic | BindingFlags.Static)!;

        var actual = Assert.IsType<bool>(method.Invoke(null, new object[] { page, rows, pageSize }));

        Assert.Equal(expected, actual);
    }

    private static MonitoreoPaginadoDto<CoincidenciaJuridicaDto> CrearRespuesta(List<CoincidenciaJuridicaDto> items, int pageSize)
    {
        var method = typeof(ListasRepository).GetMethod("CrearRespuestaJuridicaFastPath", BindingFlags.NonPublic | BindingFlags.Static)!;
        return Assert.IsType<MonitoreoPaginadoDto<CoincidenciaJuridicaDto>>(method.Invoke(null, new object[] { items, pageSize }));
    }
}
