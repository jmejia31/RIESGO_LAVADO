using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Shared.Results;

namespace RL.API.Features.MatricesRiesgos.Application;

public interface IMatricesRiesgosMitigacionService
{
    Task<ServiceResult<IReadOnlyList<ControlRiesgoDto>>> ListarControlesAsync(long evaluacionId);
    Task<ServiceResult<long>> CrearControlAsync(ControlRiesgoGuardarDto dto, long usuarioId, string? ip);
    Task<ServiceResult> ActualizarControlAsync(long controlId, ControlRiesgoGuardarDto dto, long usuarioId, string? ip);
    Task<ServiceResult<IReadOnlyList<EvaluacionControlDto>>> ListarEvaluacionesControlAsync(long controlId);
    Task<ServiceResult<long>> RegistrarEvaluacionControlAsync(long controlId, EvaluacionControlGuardarDto dto, long usuarioId, string? ip);
    Task<ServiceResult<IReadOnlyList<PlanMitigacionDto>>> ListarPlanesAsync(long evaluacionId);
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

    private readonly IMatricesRiesgosMitigacionRepository _repo;

    public MatricesRiesgosMitigacionService(IMatricesRiesgosMitigacionRepository repo) => _repo = repo;

    public async Task<ServiceResult<IReadOnlyList<ControlRiesgoDto>>> ListarControlesAsync(long evaluacionId) =>
        evaluacionId <= 0
            ? ServiceResult<IReadOnlyList<ControlRiesgoDto>>.BadRequest("La evaluación es obligatoria.")
            : ServiceResult<IReadOnlyList<ControlRiesgoDto>>.Ok(await _repo.ListarControlesAsync(evaluacionId));

    public async Task<ServiceResult<long>> CrearControlAsync(ControlRiesgoGuardarDto dto, long usuarioId, string? ip)
    {
        string? error = ValidarControl(dto);
        if (error is not null) return ServiceResult<long>.BadRequest(error);
        try { return ServiceResult<long>.Ok(await _repo.CrearControlAsync(dto, usuarioId, ip), "Control creado correctamente."); }
        catch (InvalidOperationException ex) { return ServiceResult<long>.BadRequest(ex.Message); }
    }

    public async Task<ServiceResult> ActualizarControlAsync(long controlId, ControlRiesgoGuardarDto dto, long usuarioId, string? ip)
    {
        if (controlId <= 0) return ServiceResult.BadRequest("El ID del control es obligatorio.");
        string? error = ValidarControl(dto);
        if (error is not null) return ServiceResult.BadRequest(error);
        try
        {
            return await _repo.ActualizarControlAsync(controlId, dto, usuarioId, ip)
                ? ServiceResult.Ok("Control actualizado correctamente.")
                : ServiceResult.NotFound("El control no existe.");
        }
        catch (InvalidOperationException ex) { return ServiceResult.BadRequest(ex.Message); }
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
        try { return ServiceResult<long>.Ok(await _repo.RegistrarEvaluacionControlAsync(controlId, dto, usuarioId, ip), "Evaluación de control registrada."); }
        catch (InvalidOperationException ex) { return ServiceResult<long>.BadRequest(ex.Message); }
    }

    public async Task<ServiceResult<IReadOnlyList<PlanMitigacionDto>>> ListarPlanesAsync(long evaluacionId) =>
        evaluacionId <= 0
            ? ServiceResult<IReadOnlyList<PlanMitigacionDto>>.BadRequest("La evaluación es obligatoria.")
            : ServiceResult<IReadOnlyList<PlanMitigacionDto>>.Ok(await _repo.ListarPlanesAsync(evaluacionId));

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
        if (string.IsNullOrWhiteSpace(dto.ConEstado) || dto.ConEstado.Trim().Length > 20) return "El estado del control es obligatorio y no puede exceder 20 caracteres.";
        return null;
    }

    private static string? ValidarPlan(PlanMitigacionGuardarDto dto)
    {
        if (dto.PlaEvaluacionId <= 0) return "La evaluación es obligatoria.";
        if (string.IsNullOrWhiteSpace(dto.PlaDescripcion) || dto.PlaDescripcion.Trim().Length > 500) return "La descripción del plan es obligatoria y no puede exceder 500 caracteres.";
        if (dto.PlaAvance is < 0 or > 100) return "El avance del plan debe estar entre 0 y 100.";
        if (dto.PlaPresupuesto < 0) return "El presupuesto no puede ser negativo.";
        if (dto.PlaFechaFin < dto.PlaFechaInicio) return "La fecha final no puede ser anterior a la fecha inicial.";
        if (string.IsNullOrWhiteSpace(dto.PlaEstado) || dto.PlaEstado.Trim().Length > 30) return "El estado del plan es obligatorio y no puede exceder 30 caracteres.";
        return null;
    }

    private static string? ValidarActividad(ActividadPlanGuardarDto dto)
    {
        if (dto.ActPlanId <= 0) return "El plan es obligatorio.";
        if (string.IsNullOrWhiteSpace(dto.ActDescripcion) || dto.ActDescripcion.Trim().Length > 500) return "La descripción de la actividad es obligatoria y no puede exceder 500 caracteres.";
        if (string.IsNullOrWhiteSpace(dto.ActResponsable) || dto.ActResponsable.Trim().Length > 150) return "El responsable es obligatorio y no puede exceder 150 caracteres.";
        if (dto.ActAvance is < 0 or > 100) return "El avance de la actividad debe estar entre 0 y 100.";
        if (dto.ActFechaFin < dto.ActFechaInicio) return "La fecha final no puede ser anterior a la fecha inicial.";
        if (string.IsNullOrWhiteSpace(dto.ActEstado) || dto.ActEstado.Trim().Length > 30) return "El estado de la actividad es obligatorio y no puede exceder 30 caracteres.";
        return null;
    }
}
