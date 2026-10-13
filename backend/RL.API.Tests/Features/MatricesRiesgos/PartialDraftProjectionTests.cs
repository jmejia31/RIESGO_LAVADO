using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Persistence;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class PartialDraftProjectionTests
{
    [Fact]
    public void PartialDraftWithoutCanonicalRiskValuesDoesNotCreateSummaryProjection()
    {
        var draft = new EvaluacionRiesgoDto
        {
            EvaDataJson = "{\"area_principal\":\"Cumplimiento\"}",
            EvaDataCalcJson = "{}"
        };

        Assert.False(MatricesRiesgosRepository.TieneCalculosSuficientesParaProyeccion(draft));
    }

    [Fact]
    public void DraftWithOnlyOneRiskValueDoesNotCreateSummaryProjection()
    {
        var draft = new EvaluacionRiesgoDto
        {
            EvaDataJson = "{}",
            EvaDataCalcJson = "{\"vri\":3}"
        };

        Assert.False(MatricesRiesgosRepository.TieneCalculosSuficientesParaProyeccion(draft));
    }

    [Fact]
    public void DraftWithBothCanonicalRiskValuesCanCreateSummaryProjection()
    {
        var draft = new EvaluacionRiesgoDto
        {
            EvaDataJson = "{}",
            EvaDataCalcJson = "{\"vri\":3,\"vrr\":1}"
        };

        Assert.True(MatricesRiesgosRepository.TieneCalculosSuficientesParaProyeccion(draft));
    }
}
