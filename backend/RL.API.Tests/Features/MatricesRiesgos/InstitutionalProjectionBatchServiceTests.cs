using Microsoft.Extensions.Logging.Abstractions;
using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Persistence;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class InstitutionalProjectionBatchServiceTests
{
    [Fact]
    public async Task GetManyAsync_UsesOneBatchReadAndMatchesSingleCanonicalMapperForAll82Fields()
    {
        InstitutionalProjectionSnapshot[] snapshots =
        [
            CreateSnapshot(101, 501, "Valor de riesgo uno"),
            CreateSnapshot(102, 502, "Valor de riesgo dos"),
            CreateSnapshot(103, 503, "Valor de riesgo tres")
        ];
        var repository = new RecordingProjectionRepository(snapshots);
        var mapper = new InstitutionalProjectionMapper();
        var service = new InstitutionalProjectionService(repository, mapper, NullLogger<InstitutionalProjectionService>.Instance);

        var result = await service.GetManyAsync(snapshots.Select(snapshot => snapshot.Evaluation.EvaId).ToArray(), 7, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(1, repository.ReadManyCalls);
        Assert.Equal(0, repository.ReadSingleCalls);
        Assert.Equal(3, result.Data!.Count);
        foreach (InstitutionalProjectionSnapshot snapshot in snapshots)
        {
            InstitutionalEvaluationProjectionDto batched = result.Data[snapshot.Evaluation.EvaId];
            InstitutionalEvaluationProjectionDto single = mapper.Map(new(
                snapshot.Evaluation, snapshot.Risk, snapshot.Controls, snapshot.Mitigation, snapshot.Monitoring, snapshot.AllRisks));
            Assert.Equal(snapshot.Risk.RieId, batched.RiskId);
            Assert.Equal(single.RiskId, batched.RiskId);
            Assert.Equal(82, batched.Fields.Count);
            Assert.Equal(single.Fields.Select(field => field.FieldNumber), batched.Fields.Select(field => field.FieldNumber));
            Assert.Equal(single.Fields.Select(field => field.FieldLabel), batched.Fields.Select(field => field.FieldLabel));
            Assert.Equal(single.Fields.Select(field => field.Value), batched.Fields.Select(field => field.Value));
            Assert.Equal(single.Fields.Select(field => field.State), batched.Fields.Select(field => field.State));
        }
    }

    private static InstitutionalProjectionSnapshot CreateSnapshot(long evaluationId, long riskId, string value)
    {
        InstitutionalProjectionSource source = MatricesRiesgos.InstitutionalProjectionMapperTests.CreateSource();
        string json = source.Evaluation.EvaDataJson.Replace("Secci\u00f3n de Cumplimiento", value, StringComparison.Ordinal);
        var evaluation = new EvaluacionRiesgoDto
        {
            EvaId=evaluationId, EvaRiesgoId=riskId, EvaVersionId=source.Evaluation.EvaVersionId,
            EvaEstado="APROBADA", EvaDataJson=json, EvaDataCalcJson=source.Evaluation.EvaDataCalcJson,
            EvaVri=source.Evaluation.EvaVri, EvaVrr=source.Evaluation.EvaVrr
        };
        var risk = new RiesgoDto { RieId=riskId, RieCodigo="ROP-CUMP-59", RieNombre=value, RieDescripcion="Descripción " + value };
        return new(evaluation, risk, [risk], Array.Empty<ControlRiesgoDto>(), new(), new(), 9);
    }

    private sealed class RecordingProjectionRepository(IReadOnlyList<InstitutionalProjectionSnapshot> snapshots)
        : IInstitutionalProjectionReadRepository
    {
        public int ReadManyCalls { get; private set; }
        public int ReadSingleCalls { get; private set; }

        public Task<InstitutionalProjectionSnapshot?> ReadAsync(long evaluationId, long userId, CancellationToken cancellationToken)
        {
            ReadSingleCalls++;
            return Task.FromResult(snapshots.SingleOrDefault(snapshot => snapshot.Evaluation.EvaId == evaluationId));
        }

        public Task<IReadOnlyDictionary<long, InstitutionalProjectionSnapshot>> ReadManyAsync(
            IReadOnlyCollection<long> evaluationIds, long userId, CancellationToken cancellationToken)
        {
            ReadManyCalls++;
            IReadOnlyDictionary<long, InstitutionalProjectionSnapshot> result = snapshots
                .Where(snapshot => evaluationIds.Contains(snapshot.Evaluation.EvaId))
                .ToDictionary(snapshot => snapshot.Evaluation.EvaId);
            return Task.FromResult(result);
        }
    }
}
