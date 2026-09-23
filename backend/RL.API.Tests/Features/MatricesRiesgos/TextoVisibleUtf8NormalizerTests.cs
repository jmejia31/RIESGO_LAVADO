using RL.API.Features.MatricesRiesgos.Domain;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class TextoVisibleUtf8NormalizerTests
{
    [Fact]
    public void Normalizar_ReparaMojibakeComunYPatronesDeRiesgos()
    {
        const string bad = "\u00EF\u00BF\u00BD";

        Assert.Equal(
            "Registro de proveedores con información inconsistente",
            TextoVisibleUtf8Normalizer.Normalizar($"Registro de proveedores con informaci{bad}n inconsistente"));

        Assert.Equal(
            "Empresas que no están inscritas",
            TextoVisibleUtf8Normalizer.Normalizar($"Empresas que no est{bad}n inscritas"));

        Assert.Equal(
            "evaluación técnica, económica",
            TextoVisibleUtf8Normalizer.Normalizar($"evaluaci{bad}n t{bad}cnica, econ{bad}mica"));

        Assert.Equal(
            "Definición sin garantías adecuadas de ejecución",
            TextoVisibleUtf8Normalizer.Normalizar($"Definici{bad}n sin garant{bad}as adecuadas de ejecuci{bad}n"));

        Assert.Equal(
            "términos de referencia de una licitación pública",
            TextoVisibleUtf8Normalizer.Normalizar($"t{bad}rminos de referencia de una licitaci{bad}n p{bad}blica"));
    }

    [Fact]
    public void Normalizar_ReparaSecuenciasUtf8InterpretadasComoLatin1()
    {
        Assert.Equal(
            "Información técnica",
            TextoVisibleUtf8Normalizer.Normalizar("Informaci\u00C3\u00B3n t\u00C3\u00A9cnica"));
        Assert.Equal(
            "Dueño",
            TextoVisibleUtf8Normalizer.Normalizar("Due\u00C3\u00B1o"));
    }

    [Fact]
    public void ContieneMojibake_DetectaMarcadoresYNoMarcaTextoSano()
    {
        Assert.False(TextoVisibleUtf8Normalizer.ContieneMojibake("Evaluación técnica"));
        Assert.True(TextoVisibleUtf8Normalizer.ContieneMojibake("Evaluaci\u00EF\u00BF\u00BDn"));
        Assert.True(TextoVisibleUtf8Normalizer.ContieneMojibake("Evaluaci\uFFFDn"));
        Assert.True(TextoVisibleUtf8Normalizer.ContieneMojibake("Evaluaci\u00C3\u00B3n"));
    }
}
