using System.Reflection;
using RL.API.Features.Auditoria.Persistence;
using Xunit;

namespace RL.API.Tests.Features.Auditoria;

public sealed class AuditoriaRepositoryOrderingTests
{
    [Theory]
    [InlineData("fecha", "desc", "ORDER BY AUD_FECHA DESC, AUD_ID DESC")]
    [InlineData("usuario", "asc", "ORDER BY NVL((SELECT USR_NOMBRE || ' ' || USR_APELLIDO FROM RL_USUARIOS WHERE USR_ID = AUD_USR_ID), AUD_USR_EMAIL) ASC, AUD_ID ASC")]
    [InlineData("tabla", "asc", "ORDER BY AUD_TABLA ASC, AUD_ID ASC")]
    [InlineData("id", "desc", "ORDER BY AUD_ID DESC")]
    public void ConstruyeOrdenDesdeListaBlanca(string? orden, string? direccion, string esperado)
    {
        var metodo = typeof(AuditoriaRepository).GetMethod("ConstruirOrdenBitacora", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(metodo);

        var resultado = Assert.IsType<string>(metodo!.Invoke(null, new object?[] { orden, direccion }));

        Assert.Equal(esperado, resultado);
    }

    [Fact]
    public void RechazaEntradaDeOrdenYDireccionFueraDeLaListaBlanca()
    {
        var metodo = typeof(AuditoriaRepository).GetMethod("ConstruirOrdenBitacora", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(metodo);

        var resultado = Assert.IsType<string>(metodo!.Invoke(null, new object?[] { "AUD_ID DESC; DROP TABLE RL_AUDITORIA", "DESC; DROP TABLE RL_AUDITORIA" }));

        Assert.Equal("ORDER BY AUD_FECHA ASC, AUD_ID ASC", resultado);
    }
}
