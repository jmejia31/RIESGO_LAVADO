using RL.API.Features.MatricesRiesgos.Domain;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class TextoVisibleUtf8NormalizerTests
{
    [Theory]
    [InlineData("informaci\u00BFn", "información")]
    [InlineData("evaluaci\u00BFn", "evaluación")]
    [InlineData("gesti\u00BFn", "gestión")]
    [InlineData("informaci\uFFFDn", "información")]
    [InlineData("Due\u00EF\u00BFo", "Dueño")]
    [InlineData("Due\u00C3\u00B1o", "Dueño")]
    [InlineData("Informaci\u00C3\u00B3n t\u00C3\u00A9cnica", "Información técnica")]
    public void Normalizar_ReparaCorrupcionConocida(string valor, string esperado) =>
        Assert.Equal(esperado, TextoVisibleUtf8Normalizer.Normalizar(valor));

    [Fact]
    public void Normalizar_PreservaCaracteresEspañolesYPreguntasValidas()
    {
        Assert.Equal("¿Qué información necesita?", TextoVisibleUtf8Normalizer.Normalizar("¿Qué información necesita?"));
        Assert.Equal("Dueño", TextoVisibleUtf8Normalizer.Normalizar("Dueño"));
        Assert.Equal("áéíóú ÁÉÍÓÚ ñÑ üÜ ¡! ¿?", TextoVisibleUtf8Normalizer.Normalizar("áéíóú ÁÉÍÓÚ ñÑ üÜ ¡! ¿?"));
    }

    [Theory]
    [InlineData("U+FFFD: Evaluaci\uFFFDn")]
    [InlineData("informaci\u00BFn")]
    [InlineData("Due\u00C3\u00B1o")]
    [InlineData("Due\u00EF\u00BFo")]
    public void ContieneMojibake_DetectaCorrupcion(string valor) =>
        Assert.True(TextoVisibleUtf8Normalizer.ContieneMojibake(valor));

    [Theory]
    [InlineData("información")]
    [InlineData("¿Qué información necesita?")]
    [InlineData("Dueño")]
    public void ContieneMojibake_NoMarcaTextoSano(string valor) =>
        Assert.False(TextoVisibleUtf8Normalizer.ContieneMojibake(valor));
}
