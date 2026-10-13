using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Shared.Results;

namespace RL.API.Features.MatricesRiesgos.Application;

public interface IInstitutionalProjectionService
{
    Task<ServiceResult<InstitutionalEvaluationProjectionDto>> GetAsync(long evaluationId, long userId, CancellationToken cancellationToken);
}

/// <summary>
/// Temporary composition boundary over the established bounded read services.
/// Each source query remains parameterized by its repository; the source services
/// currently open independent Oracle connections, so this does not claim a single SCN snapshot.
/// </summary>
public sealed class InstitutionalProjectionService : IInstitutionalProjectionService
{
    private readonly IMatricesRiesgosAppService _evaluations;
    private readonly IMatricesRiesgosGestionService _risks;
    private readonly IMatricesRiesgosMitigacionService _mitigation;
    private readonly IMatricesRiesgosMonitoreoService _monitoring;
    private readonly InstitutionalProjectionMapper _mapper;
    private readonly ILogger<InstitutionalProjectionService> _logger;

    public InstitutionalProjectionService(
        IMatricesRiesgosAppService evaluations,
        IMatricesRiesgosGestionService risks,
        IMatricesRiesgosMitigacionService mitigation,
        IMatricesRiesgosMonitoreoService monitoring,
        InstitutionalProjectionMapper mapper,
        ILogger<InstitutionalProjectionService> logger)
    {
        _evaluations = evaluations;
        _risks = risks;
        _mitigation = mitigation;
        _monitoring = monitoring;
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
            cancellationToken.ThrowIfCancellationRequested();
            ServiceResult<EvaluacionRiesgoDto> evaluationResult = await _evaluations.ObtenerEvaluacionAsync(evaluationId);
            if (!evaluationResult.Success || evaluationResult.Data is null)
                return ServiceResult<InstitutionalEvaluationProjectionDto>.NotFound("No se encontró la evaluación solicitada.");
            EvaluacionRiesgoDto evaluation = evaluationResult.Data;

            cancellationToken.ThrowIfCancellationRequested();
            ServiceResult<RiesgoDto> riskResult = await _risks.ObtenerRiesgoAsync(evaluation.EvaRiesgoId);
            if (!riskResult.Success || riskResult.Data is null)
                return ServiceResult<InstitutionalEvaluationProjectionDto>.NotFound("No se encontró el riesgo maestro de la evaluación.");

            cancellationToken.ThrowIfCancellationRequested();
            ServiceResult<IReadOnlyList<ControlRiesgoDto>> controlsResult = await _mitigation.ListarControlesAsync(evaluationId);
            cancellationToken.ThrowIfCancellationRequested();
            ServiceResult<MitigacionBloque4Dto> mitigationResult = await _mitigation.ObtenerBloque4Async(evaluationId);
            cancellationToken.ThrowIfCancellationRequested();
            ServiceResult<MatrizBloque6Dto> monitoringResult = await _monitoring.ObtenerBloque6Async(evaluationId, userId);

            if (!controlsResult.Success || controlsResult.Data is null
                || !mitigationResult.Success || mitigationResult.Data is null
                || !monitoringResult.Success || monitoringResult.Data is null)
                return ServiceResult<InstitutionalEvaluationProjectionDto>.Conflict("No fue posible leer todas las fuentes requeridas para la proyección institucional.");

            InstitutionalEvaluationProjectionDto projection = _mapper.Map(new(
                evaluation, riskResult.Data, controlsResult.Data, mitigationResult.Data, monitoringResult.Data));
            timer.Stop();
            _logger.LogInformation(
                "Matriz institucional proyectada. EvaluationId={EvaluationId}, RiskCode={RiskCode}, FieldCount={FieldCount}, ReadServiceCount={ReadServiceCount}, DurationMs={DurationMs}",
                evaluationId, riskResult.Data.RieCodigo, projection.Fields.Count, 5, timer.ElapsedMilliseconds);
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
    }
}
