using RL.API.Features.MatricesRiesgos.Domain;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class EvaluacionWorkflowTests
{
    [Theory]
    [InlineData("BORRADOR", "EN_REVISION")]
    [InlineData("EN_REVISION", "APROBADA")]
    [InlineData("EN_REVISION", "OBSERVADA")]
    [InlineData("OBSERVADA", "BORRADOR")]
    [InlineData("APROBADA", "CERRADA")]
    public void PermiteTransicionesInstitucionales(string actual, string destino) =>
        Assert.True(EvaluacionWorkflow.EsTransicionPermitida(actual, destino));

    [Theory]
    [InlineData("BORRADOR", "APROBADA")]
    [InlineData("CERRADA", "BORRADOR")]
    [InlineData("", "APROBADA")]
    public void RechazaTransicionesNoDefinidas(string actual, string destino) =>
        Assert.False(EvaluacionWorkflow.EsTransicionPermitida(actual, destino));
}
