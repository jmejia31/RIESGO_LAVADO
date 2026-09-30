using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;
using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Shared.Results;
using System.Data;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RL.API.Features.MatricesRiesgos.Application;

public interface IMatricesRiesgosMitigacionService
{
    Task<ServiceResult<IReadOnlyList<ControlRiesgoDto>>> ListarControlesAsync(long evaluacionId);
    Task<ServiceResult<long>> CrearControlAsync(ControlRiesgoGuardarDto dto, long usuarioId, string? ip);
    Task<ServiceResult> ActualizarControlAsync(long controlId, ControlRiesgoGuardarDto dto, long usuarioId, string? ip);
    Task<ServiceResult<IReadOnlyList<EvaluacionControlDto>>> ListarEvaluacionesControlAsync(long controlId);
    Task<ServiceResult<long>> RegistrarEvaluacionControlAsync(long controlId, EvaluacionControlGuardarDto dto, long usuarioId, string? ip);
    Task<ServiceResult<IReadOnlyList<PlanMitigacionDto>>> ListarPlanesAsync(long evaluacionId);
    Task<ServiceResult<MitigacionBloque4Dto>> ObtenerBloque4Async(long evaluacionId);
    Task<ServiceResult<long>> CrearPlanAsync(PlanMitigacionGuardarDto dto, long usuarioId, string? ip);
    Task<ServiceResult> ActualizarPlanAsync(long planId, PlanMitigacionGuardarDto dto, long usuarioId, string? ip);
    Task<ServiceResult<IReadOnlyList<ActividadPlanDto>>> ListarActividadesAsync(long planId);
    Task<ServiceResult<long>> CrearActividadAsync(ActividadPlanGuardarDto dto, long usuarioId, string? ip);
    Task<ServiceResult> ActualizarActividadAsync(long actividadId, ActividadPlanGuardarDto dto, long usuarioId, string? ip);
}

public sealed class MatricesRiesgosMitigacionService : IMatricesRiesgosMitigacionService
{
    private static readonly HashSet<string> TiposControl = new(StringComparer.OrdinalIgnoreCase)
    { "PREVENTIVO", "DETECTIVO", "CORRECTIVO" };

    private static readonly HashSet<string> Automatizaciones = new(StringComparer.OrdinalIgnoreCase)
    { "MANUAL", "SEMIAUTOMATICO", "AUTOMATICO" };

    private static readonly HashSet<string> EstadosControl = new(StringComparer.OrdinalIgnoreCase)
    { "ACTIVO", "INACTIVO" };

    // Contrato histórico Fase 10: PENDIENTE, EN_PROCESO, CERRADO, VENCIDO
    // e INACTIVO como estado de ciclo de vida. No se aceptan estados arbitrarios.
    private static readonly HashSet<string> EstadosPlan = new(StringComparer.OrdinalIgnoreCase)
    { "PENDIENTE", "EN_PROCESO", "CERRADO", "VENCIDO", "INACTIVO" };

    private readonly IMatricesRiesgosMitigacionRepository _repo;

    private readonly IMatricesRiesgosRepository _evaluations;
    private readonly VersionedCalculationRuntimeService _runtime;

    public MatricesRiesgosMitigacionService(
        IMatricesRiesgosMitigacionRepository repo,
        IMatricesRiesgosRepository evaluations,
        VersionedCalculationRuntimeService runtime)
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _evaluations = evaluations ?? throw new ArgumentNullException(nameof(evaluations));
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    public async Task<ServiceResult<IReadOnlyList<ControlRiesgoDto>>> ListarControlesAsync(long evaluacionId) =>
        evaluacionId <= 0
            ? ServiceResult<IReadOnlyList<ControlRiesgoDto>>.BadRequest("La evaluación es obligatoria.")
            : ServiceResult<IReadOnlyList<ControlRiesgoDto>>.Ok(await _repo.ListarControlesAsync(evaluacionId));

    public async Task<ServiceResult<long>> CrearControlAsync(ControlRiesgoGuardarDto dto, long usuarioId, string? ip)
    {
        string? error = ValidarControl(dto);
        if (error is not null) return ServiceResult<long>.BadRequest(error);
        try
        {
            long? governedId = await CrearControlGobernadoAsync(dto, usuarioId, ip);
            return ServiceResult<long>.Ok(governedId ?? await _repo.CrearControlAsync(dto, usuarioId, ip), "Control creado correctamente.");
        }
        catch (DBConcurrencyException ex) { return ServiceResult<long>.Conflict(ex.Message); }
        catch (InvalidOperationException ex) { return ServiceResult<long>.BadRequest(ex.Message); }
        catch (FormulaRuntimeException ex) { return ServiceResult<long>.BadRequest("Runtime gobernado inválido: " + ex.Code); }
        catch (KeyNotFoundException ex) { return ServiceResult<long>.NotFound(ex.Message); }
    }

    public async Task<ServiceResult> ActualizarControlAsync(long controlId, ControlRiesgoGuardarDto dto, long usuarioId, string? ip)
    {
        if (controlId <= 0) return ServiceResult.BadRequest("El ID del control es obligatorio.");
        string? error = ValidarControl(dto);
        if (error is not null) return ServiceResult.BadRequest(error);
        try
        {
            bool? governedUpdate = await ActualizarControlGobernadoAsync(controlId, dto, usuarioId, ip);
            if (governedUpdate.HasValue)
                return governedUpdate.Value ? ServiceResult.Ok("Control y cálculo actualizados correctamente.") : ServiceResult.NotFound("El control no existe.");
            return await _repo.ActualizarControlAsync(controlId, dto, usuarioId, ip)
                ? ServiceResult.Ok("Control actualizado correctamente.")
                : ServiceResult.NotFound("El control no existe.");
        }
        catch (DBConcurrencyException ex) { return ServiceResult.Conflict(ex.Message); }
        catch (InvalidOperationException ex) { return ServiceResult.BadRequest(ex.Message); }
        catch (FormulaRuntimeException ex) { return ServiceResult.BadRequest("Runtime gobernado inválido: " + ex.Code); }
        catch (KeyNotFoundException ex) { return ServiceResult.NotFound(ex.Message); }
    }

    private async Task<long?> CrearControlGobernadoAsync(ControlRiesgoGuardarDto requested, long userId, string? ip)
    {
        EvaluacionRiesgoDto? evaluation = await _evaluations.ObtenerEvaluacionAsync(requested.ConEvaluacionId);
        if (evaluation is null) throw new InvalidOperationException("La evaluación asociada al control no existe.");
        IReadOnlyList<ControlRiesgoDto> current = await _repo.ListarControlesAsync(requested.ConEvaluacionId);
        List<ControlRiesgoDto> hypothetical = current.Append(ToControl(requested, 0)).ToList();
        string definition = await GetDefinitionAsync(evaluation.EvaVersionId);
        GovernedCalculationResult result = await _runtime.CalculateAsync(evaluation.EvaVersionId, definition, evaluation.EvaDataJson, Presence(hypothetical));
        if (!result.IsGoverned) return null;
        EnsureCalculationSuccess(result);
        return await _repo.CrearControlGobernadoAtomicoAsync(requested, evaluation.EvaVersionRow,
            MergeCalculatedJson(evaluation.EvaDataCalcJson, result.Evaluation!.Values), userId, ip);
    }

    private async Task<bool?> ActualizarControlGobernadoAsync(long controlId, ControlRiesgoGuardarDto requested, long userId, string? ip)
    {
        EvaluacionRiesgoDto? evaluation = await _evaluations.ObtenerEvaluacionAsync(requested.ConEvaluacionId);
        if (evaluation is null) throw new InvalidOperationException("La evaluación asociada al control no existe.");

        string targetDefinition = await GetDefinitionAsync(evaluation.EvaVersionId);
        GovernedCalculationResult targetCalc = await _runtime.CalculateAsync(evaluation.EvaVersionId, targetDefinition, evaluation.EvaDataJson);

        ControlRiesgoDto? existing = await _repo.ObtenerControlAsync(controlId);
        bool existingIsGoverned = false;
        if (existing is not null && existing.ConEvaluacionId != requested.ConEvaluacionId)
        {
            EvaluacionRiesgoDto? oldEvaluation = await _evaluations.ObtenerEvaluacionAsync(existing.ConEvaluacionId);
            if (oldEvaluation is not null)
            {
                string oldDefinition = await GetDefinitionAsync(oldEvaluation.EvaVersionId);
                existingIsGoverned = (await _runtime.CalculateAsync(oldEvaluation.EvaVersionId, oldDefinition, oldEvaluation.EvaDataJson)).IsGoverned;
            }
        }

        if (!targetCalc.IsGoverned && !existingIsGoverned)
        {
            return null;
        }

        if (existing is null) return false;
        if (existing.ConEvaluacionId != requested.ConEvaluacionId)
            throw new InvalidOperationException("No se permite cambiar el padre de un control gobernado.");

        IReadOnlyList<ControlRiesgoDto> current = await _repo.ListarControlesAsync(requested.ConEvaluacionId);
        if (!current.Any(control => control.ConId == controlId)) return false;
        List<ControlRiesgoDto> hypothetical = current.Select(control => control.ConId == controlId ? ToControl(requested, controlId) : control).ToList();
        GovernedCalculationResult result = await _runtime.CalculateAsync(evaluation.EvaVersionId, targetDefinition, evaluation.EvaDataJson, Presence(hypothetical));
        if (!result.IsGoverned) return null;
        EnsureCalculationSuccess(result);
        return await _repo.ActualizarControlGobernadoAtomicoAsync(controlId, requested, evaluation.EvaVersionRow,
            MergeCalculatedJson(evaluation.EvaDataCalcJson, result.Evaluation!.Values), userId, ip);
    }

    private async Task<string> GetDefinitionAsync(long versionId)
    {
        // Las versiones de evaluación son inmutables; la definición se recupera por el ID ligado a la evaluación.
        VersionFormularioDto? version = await _evaluations.ObtenerVersionFormularioAsync(versionId);
        return version is null || string.IsNullOrWhiteSpace(version.VerJson)
            ? throw new InvalidOperationException("No se encontró la definición de la versión asociada para recalcular controles.")
            : version.VerJson;
    }


    private static IReadOnlyDictionary<string, bool> Presence(IEnumerable<ControlRiesgoDto> controls) => new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
    {
        [InstitutionalCalculationContextKeys.PreventiveControl] = controls.Any(control => control.ConTipo.Equals("PREVENTIVO", StringComparison.OrdinalIgnoreCase)),
        [InstitutionalCalculationContextKeys.DetectiveControl] = controls.Any(control => control.ConTipo.Equals("DETECTIVO", StringComparison.OrdinalIgnoreCase)),
        [InstitutionalCalculationContextKeys.CorrectiveControl] = controls.Any(control => control.ConTipo.Equals("CORRECTIVO", StringComparison.OrdinalIgnoreCase))
    };

    private static ControlRiesgoDto ToControl(ControlRiesgoGuardarDto source, long id) => new()
    {
        ConId=id, ConEvaluacionId=source.ConEvaluacionId, ConTipo=source.ConTipo, ConDescripcion=source.ConDescripcion,
        ConAutomatizacion=source.ConAutomatizacion, ConEstado=source.ConEstado
        ,ConEstadoMonitoreo=source.ConEstadoMonitoreo, ConEfectividadMonitoreo=source.ConEfectividadMonitoreo
    };

    private static void EnsureCalculationSuccess(GovernedCalculationResult result)
    {
        if (result.Evaluation is null || !result.Evaluation.Success)
            throw new InvalidOperationException("No fue posible recalcular la evaluación gobernada: " + string.Join("; ", result.Evaluation?.Errors.Select(error => error.Code.ToString()) ?? Array.Empty<string>()));
    }

    internal static string MergeCalculatedJson(string? currentJson, IReadOnlyDictionary<string, object?> calculatedValues)
    {
        JsonObject root;
        try
        {
            root = string.IsNullOrWhiteSpace(currentJson) ? new JsonObject() : JsonNode.Parse(currentJson) as JsonObject
                ?? throw new InvalidOperationException("EVA_CALCULOS_JSON debe ser un objeto JSON.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("No se puede actualizar un cálculo gobernado con EVA_CALCULOS_JSON inválido.", exception);
        }

        foreach ((string key, object? value) in calculatedValues)
        {
            string? existingKey = root.Select(property => property.Key)
                .FirstOrDefault(existing => existing.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (existingKey is not null && !existingKey.Equals(key, StringComparison.Ordinal)) root.Remove(existingKey);
            root[key] = JsonSerializer.SerializeToNode(value);
        }
        return root.ToJsonString();
    }

    public async Task<ServiceResult<IReadOnlyList<EvaluacionControlDto>>> ListarEvaluacionesControlAsync(long controlId) =>
        controlId <= 0
            ? ServiceResult<IReadOnlyList<EvaluacionControlDto>>.BadRequest("El control es obligatorio.")
            : ServiceResult<IReadOnlyList<EvaluacionControlDto>>.Ok(await _repo.ListarEvaluacionesControlAsync(controlId));

    public async Task<ServiceResult<long>> RegistrarEvaluacionControlAsync(long controlId, EvaluacionControlGuardarDto dto, long usuarioId, string? ip)
    {
        if (controlId <= 0) return ServiceResult<long>.BadRequest("El control es obligatorio.");
        if (dto.EcoEfectividad is < 0 or > 100) return ServiceResult<long>.BadRequest("La efectividad debe estar entre 0 y 100.");
        if ((dto.EcoComentario?.Length ?? 0) > 500) return ServiceResult<long>.BadRequest("El comentario no puede exceder 500 caracteres.");
        if (TextoVisibleUtf8Normalizer.ContieneMojibake(dto.EcoComentario)) return ServiceResult<long>.BadRequest("El comentario contiene caracteres de codificación inválidos.");
        try { return ServiceResult<long>.Ok(await _repo.RegistrarEvaluacionControlAsync(controlId, dto, usuarioId, ip), "Evaluación de control registrada."); }
        catch (InvalidOperationException ex) { return ServiceResult<long>.BadRequest(ex.Message); }
    }

    public async Task<ServiceResult<IReadOnlyList<PlanMitigacionDto>>> ListarPlanesAsync(long evaluacionId) =>
        evaluacionId <= 0
            ? ServiceResult<IReadOnlyList<PlanMitigacionDto>>.BadRequest("La evaluación es obligatoria.")
            : ServiceResult<IReadOnlyList<PlanMitigacionDto>>.Ok(await _repo.ListarPlanesAsync(evaluacionId));

    public async Task<ServiceResult<MitigacionBloque4Dto>> ObtenerBloque4Async(long evaluacionId)
    {
        if (evaluacionId <= 0) return ServiceResult<MitigacionBloque4Dto>.BadRequest("La evaluación es obligatoria.");
        var bloque = await _repo.ObtenerBloque4Async(evaluacionId);
        return bloque is null
            ? ServiceResult<MitigacionBloque4Dto>.NotFound("La evaluación no existe.")
            : ServiceResult<MitigacionBloque4Dto>.Ok(bloque);
    }

    public async Task<ServiceResult<long>> CrearPlanAsync(PlanMitigacionGuardarDto dto, long usuarioId, string? ip)
    {
        string? error = ValidarPlan(dto);
        if (error is not null) return ServiceResult<long>.BadRequest(error);
        try { return ServiceResult<long>.Ok(await _repo.CrearPlanAsync(dto, usuarioId, ip), "Plan creado correctamente."); }
        catch (InvalidOperationException ex) { return ServiceResult<long>.BadRequest(ex.Message); }
    }

    public async Task<ServiceResult> ActualizarPlanAsync(long planId, PlanMitigacionGuardarDto dto, long usuarioId, string? ip)
    {
        if (planId <= 0) return ServiceResult.BadRequest("El ID del plan es obligatorio.");
        string? error = ValidarPlan(dto);
        if (error is not null) return ServiceResult.BadRequest(error);
        try
        {
            return await _repo.ActualizarPlanAsync(planId, dto, usuarioId, ip)
                ? ServiceResult.Ok("Plan actualizado correctamente.")
                : ServiceResult.NotFound("El plan no existe.");
        }
        catch (InvalidOperationException ex) { return ServiceResult.BadRequest(ex.Message); }
    }

    public async Task<ServiceResult<IReadOnlyList<ActividadPlanDto>>> ListarActividadesAsync(long planId) =>
        planId <= 0
            ? ServiceResult<IReadOnlyList<ActividadPlanDto>>.BadRequest("El plan es obligatorio.")
            : ServiceResult<IReadOnlyList<ActividadPlanDto>>.Ok(await _repo.ListarActividadesAsync(planId));

    public async Task<ServiceResult<long>> CrearActividadAsync(ActividadPlanGuardarDto dto, long usuarioId, string? ip)
    {
        string? error = ValidarActividad(dto);
        if (error is not null) return ServiceResult<long>.BadRequest(error);
        try { return ServiceResult<long>.Ok(await _repo.CrearActividadAsync(dto, usuarioId, ip), "Actividad creada correctamente."); }
        catch (InvalidOperationException ex) { return ServiceResult<long>.BadRequest(ex.Message); }
    }

    public async Task<ServiceResult> ActualizarActividadAsync(long actividadId, ActividadPlanGuardarDto dto, long usuarioId, string? ip)
    {
        if (actividadId <= 0) return ServiceResult.BadRequest("El ID de la actividad es obligatorio.");
        string? error = ValidarActividad(dto);
        if (error is not null) return ServiceResult.BadRequest(error);
        try
        {
            return await _repo.ActualizarActividadAsync(actividadId, dto, usuarioId, ip)
                ? ServiceResult.Ok("Actividad actualizada correctamente.")
                : ServiceResult.NotFound("La actividad no existe.");
        }
        catch (InvalidOperationException ex) { return ServiceResult.BadRequest(ex.Message); }
    }

    private static string? ValidarControl(ControlRiesgoGuardarDto dto)
    {
        if (dto.ConEvaluacionId <= 0) return "La evaluación es obligatoria.";
        if (!TiposControl.Contains(dto.ConTipo?.Trim() ?? string.Empty)) return "Tipo de control inválido.";
        if (!Automatizaciones.Contains(dto.ConAutomatizacion?.Trim() ?? string.Empty)) return "Automatización de control inválida.";
        if (string.IsNullOrWhiteSpace(dto.ConDescripcion) || dto.ConDescripcion.Trim().Length > 500) return "La descripción del control es obligatoria y no puede exceder 500 caracteres.";
        if (TextoVisibleUtf8Normalizer.ContieneMojibake(dto.ConDescripcion)) return "La descripción del control contiene caracteres de codificación inválidos.";
        if (!EstadosControl.Contains(dto.ConEstado?.Trim() ?? string.Empty)) return "El estado del control debe ser ACTIVO o INACTIVO.";
        if (!string.IsNullOrWhiteSpace(dto.ConEstadoMonitoreo) && (dto.ConEstadoMonitoreo.Trim().Length > 30 || TextoVisibleUtf8Normalizer.ContieneMojibake(dto.ConEstadoMonitoreo))) return "El estado de monitoreo debe contener hasta 30 caracteres válidos.";
        if (dto.ConEfectividadMonitoreo is < 0 or > 100) return "La efectividad de monitoreo debe estar entre 0 y 100.";
        return null;
    }

    private static string? ValidarPlan(PlanMitigacionGuardarDto dto)
    {
        if (dto.PlaEvaluacionId <= 0) return "La evaluación es obligatoria.";
        if (string.IsNullOrWhiteSpace(dto.PlaDescripcion) || dto.PlaDescripcion.Trim().Length > 500) return "La descripción del plan es obligatoria y no puede exceder 500 caracteres.";
        if (TextoVisibleUtf8Normalizer.ContieneMojibake(dto.PlaDescripcion)) return "La descripción del plan contiene caracteres de codificación inválidos.";
        if (!TextoPlanOpcionalValido(dto.PlaMonitoreoSeguimiento)
            || !TextoPlanOpcionalValido(dto.PlaResponsables)
            || !TextoPlanOpcionalValido(dto.PlaRecursos)) return "Los datos del plan exceden 1000 caracteres o contienen codificación inválida.";
        if (dto.PlaAvance is < 0 or > 100) return "El avance del plan debe estar entre 0 y 100.";
        if (dto.PlaPresupuesto < 0) return "El presupuesto no puede ser negativo.";
        if (dto.PlaFechaFin < dto.PlaFechaInicio) return "La fecha final no puede ser anterior a la fecha inicial.";
        if (!EstadosPlan.Contains(dto.PlaEstado?.Trim() ?? string.Empty)) return "El estado del plan debe ser PENDIENTE, EN_PROCESO, CERRADO, VENCIDO o INACTIVO.";
        return null;
    }

    private static bool TextoPlanOpcionalValido(string? valor) =>
        string.IsNullOrWhiteSpace(valor)
        || (valor.Trim().Length <= 1000 && !TextoVisibleUtf8Normalizer.ContieneMojibake(valor));

    private static string? ValidarActividad(ActividadPlanGuardarDto dto)
    {
        if (dto.ActPlanId <= 0) return "El plan es obligatorio.";
        if (string.IsNullOrWhiteSpace(dto.ActDescripcion) || dto.ActDescripcion.Trim().Length > 500) return "La descripción de la actividad es obligatoria y no puede exceder 500 caracteres.";
        if (TextoVisibleUtf8Normalizer.ContieneMojibake(dto.ActDescripcion) || TextoVisibleUtf8Normalizer.ContieneMojibake(dto.ActResponsable)) return "La actividad contiene caracteres de codificación inválidos.";
        if (string.IsNullOrWhiteSpace(dto.ActResponsable) || dto.ActResponsable.Trim().Length > 150) return "El responsable es obligatorio y no puede exceder 150 caracteres.";
        if (dto.ActAvance is < 0 or > 100) return "El avance de la actividad debe estar entre 0 y 100.";
        if (dto.ActFechaFin < dto.ActFechaInicio) return "La fecha final no puede ser anterior a la fecha inicial.";
        if (string.IsNullOrWhiteSpace(dto.ActEstado) || dto.ActEstado.Trim().Length > 30) return "El estado de la actividad es obligatorio y no puede exceder 30 caracteres.";
        return null;
    }
}
