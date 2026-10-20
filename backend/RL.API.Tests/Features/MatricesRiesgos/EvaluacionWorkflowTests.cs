using RL.API.Features.MatricesRiesgos.Domain;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class EvaluacionWorkflowTests
{
    public static IEnumerable<object[]> TransicionesValidas =>
        from actual in EvaluacionWorkflow.ObtenerCatalogoEstados().Select(estado => estado.Codigo)
        from destino in EvaluacionWorkflow.ObtenerCatalogoEstados().Select(estado => estado.Codigo)
        where !actual.Equals(destino, StringComparison.OrdinalIgnoreCase)
        select new object[] { actual, destino };

    public static IEnumerable<object[]> EstadosCanonicos =>
        EvaluacionWorkflow.ObtenerCatalogoEstados().Select(estado => new object[] { estado.Codigo });

    [Fact]
    public void ExponeUnCatalogoCanonicoConSeisEstadosUnicosYEtiquetasInstitucionales()
    {
        var catalogo = EvaluacionWorkflow.ObtenerCatalogoEstados();

        Assert.Equal(6, catalogo.Count);
        Assert.Equal(6, catalogo.Select(estado => estado.Codigo).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(new[] { "APROBADA", "BORRADOR", "CERRADA", "EN_REVISION", "OBSERVADA", "RECHAZADA" },
            catalogo.Select(estado => estado.Codigo));
        Assert.Equal(new[] { "Aprobada", "Borrador", "Cerrada", "En revisión", "Observada", "Rechazada" },
            catalogo.Select(estado => estado.Etiqueta));
    }

    [Theory]
    [MemberData(nameof(TransicionesValidas))]
    public void PermiteLosTreintaCambiosDirectosEntreEstadosDistintos(string actual, string destino) =>
        Assert.True(EvaluacionWorkflow.EsTransicionPermitida(actual, destino));

    [Theory]
    [MemberData(nameof(EstadosCanonicos))]
    public void RechazaLaAutotransicion(string estado) =>
        Assert.False(EvaluacionWorkflow.EsTransicionPermitida(estado, estado));

    [Theory]
    [MemberData(nameof(EstadosCanonicos))]
    public void OfreceLosOtrosCincoEstadosComoDestino(string actual)
    {
        var destinos = EvaluacionWorkflow.ObtenerDestinosPermitidos(actual);

        Assert.Equal(5, destinos.Count);
        Assert.DoesNotContain(destinos, destino => destino.Equals(actual, StringComparison.OrdinalIgnoreCase));
        Assert.All(destinos, destino => Assert.Contains(destino, EvaluacionWorkflow.ObtenerCatalogoEstados().Select(estado => estado.Codigo)));
    }

    [Theory]
    [InlineData("DESCONOCIDO", "BORRADOR")]
    [InlineData("BORRADOR", "DESCONOCIDO")]
    [InlineData("", "APROBADA")]
    public void RechazaCodigosFueraDelCatalogo(string actual, string destino) =>
        Assert.False(EvaluacionWorkflow.EsTransicionPermitida(actual, destino));

    [Theory]
    [InlineData("DESCONOCIDO")]
    [InlineData("")]
    public void UnCodigoDesconocidoNoTieneDestinos(string actual) =>
        Assert.Empty(EvaluacionWorkflow.ObtenerDestinosPermitidos(actual));
}
