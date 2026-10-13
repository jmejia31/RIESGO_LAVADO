using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Shared.Results;

namespace RL.API.Features.MatricesRiesgos.Application;

public interface IInstitutionalProjectionService
{
    Task<ServiceResult<InstitutionalEvaluationProjectionDto>> GetAsync(long evaluationId, long userId, CancellationToken cancellationToken);
}

/// <summary>Composes the canonical read model from a single Oracle read-only snapshot.</summary>
public sealed class InstitutionalProjectionService : IInstitutionalProjectionService
{
    private readonly IInstitutionalProjectionReadRepository _repository;
    private readonly InstitutionalProjectionMapper _mapper;
    private readonly ILogger<InstitutionalProjectionService> _logger;

    public InstitutionalProjectionService(
        IInstitutionalProjectionReadRepository repository,
        InstitutionalProjectionMapper mapper,
        ILogger<InstitutionalProjectionService> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ServiceResult<InstitutionalEvaluationProjectionDto>> GetAsync(
        long evaluationId,
        long userId,
        CancellationToken cancellationToken)
    {
        if (evaluationId <= 0 || userId <= 0)
            return ServiceResult<InstitutionalEvaluationProjectionDto>.BadRequest("La evaluación y el usuario deben ser válidos.");

        Stopwatch timer = Stopwatch.StartNew();
        try
        {
            InstitutionalProjectionSnapshot? snapshot = await _repository.ReadAsync(evaluationId, userId, cancellationToken);
            if (snapshot is null)
                return ServiceResult<InstitutionalEvaluationProjectionDto>.NotFound("No se encontró la evaluación solicitada o su riesgo maestro.");
            InstitutionalEvaluationProjectionDto projection = _mapper.Map(new(
                snapshot.Evaluation, snapshot.Risk, snapshot.Controls, snapshot.Mitigation, snapshot.Monitoring, snapshot.AllRisks));
            timer.Stop();
            _logger.LogInformation(
                "Matriz institucional proyectada. EvaluationId={EvaluationId}, RiskCode={RiskCode}, FieldCount={FieldCount}, ReadQueryCount={ReadQueryCount}, DurationMs={DurationMs}",
                evaluationId, snapshot.Risk.RieCodigo, projection.Fields.Count, snapshot.QueryCount, timer.ElapsedMilliseconds);
            return ServiceResult<InstitutionalEvaluationProjectionDto>.Ok(projection);
        }
        catch (InstitutionalProjectionException exception)
        {
            timer.Stop();
            _logger.LogWarning(exception, "Datos incompatibles para proyección institucional. EvaluationId={EvaluationId}, DurationMs={DurationMs}", evaluationId, timer.ElapsedMilliseconds);
            return ServiceResult<InstitutionalEvaluationProjectionDto>.Conflict("La evaluación contiene datos incompatibles con el contrato institucional.");
        }
        catch (System.Text.Json.JsonException exception)
        {
            timer.Stop();
            _logger.LogWarning(exception, "JSON de matriz inválido. EvaluationId={EvaluationId}, DurationMs={DurationMs}", evaluationId, timer.ElapsedMilliseconds);
            return ServiceResult<InstitutionalEvaluationProjectionDto>.Conflict("Los datos JSON de la evaluación no son válidos para la proyección institucional.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Oracle.ManagedDataAccess.Client.OracleException exception)
        {
            timer.Stop();
            _logger.LogError(exception, "Fallo de persistencia al leer proyección institucional. EvaluationId={EvaluationId}, DurationMs={DurationMs}", evaluationId, timer.ElapsedMilliseconds);
            return new(false, null, "No fue posible leer la proyección institucional en este momento.", 503);
        }
    }
}
