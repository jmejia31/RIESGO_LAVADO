using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Shared.Results;

namespace RL.API.Features.MatricesRiesgos.Application;

public interface IMatricesRiesgosMonitoreoService
{
    Task<ServiceResult<IReadOnlyList<SenalAlertaDto>>> ListarAlertasAsync(long evaluacionId);
    Task<ServiceResult<long>> CrearAlertaAsync(SenalAlertaGuardarDto dto, long usuarioId, string? ip);
    Task<ServiceResult> CambiarEstadoAlertaAsync(long alertaId, SenalAlertaEstadoDto dto, long usuarioId, string? ip);
    Task<ServiceResult<IReadOnlyList<AutomonitoreoDto>>> ListarAutomonitoreoAsync(long evaluacionId);
    Task<ServiceResult<long>> RegistrarAutomonitoreoAsync(AutomonitoreoGuardarDto dto, long usuarioId, string? ip);
    Task<ServiceResult<ResumenMatricesOperativoDto>> ObtenerResumenOperativoAsync();
}

public sealed class MatricesRiesgosMonitoreoService : IMatricesRiesgosMonitoreoService
{
    private static readonly HashSet<string> EstadosAlerta = new(StringComparer.OrdinalIgnoreCase) { "ACTIVO", "INACTIVO" };
    private readonly IMatricesRiesgosMonitoreoRepository _repo;

    public MatricesRiesgosMonitoreoService(IMatricesRiesgosMonitoreoRepository repo) => _repo = repo;

    public async Task<ServiceResult<IReadOnlyList<SenalAlertaDto>>> ListarAlertasAsync(long evaluacionId) =>
        evaluacionId <= 0
            ? ServiceResult<IReadOnlyList<SenalAlertaDto>>.BadRequest("La evaluación es obligatoria.")
            : ServiceResult<IReadOnlyList<SenalAlertaDto>>.Ok(await _repo.ListarAlertasAsync(evaluacionId));

    public async Task<ServiceResult<long>> CrearAlertaAsync(SenalAlertaGuardarDto dto, long usuarioId, string? ip)
    {
        string? error = ValidarAlerta(dto);
        if (error is not null) return ServiceResult<long>.BadRequest(error);
        try { return ServiceResult<long>.Ok(await _repo.CrearAlertaAsync(dto, usuarioId, ip), "Señal de alerta creada correctamente."); }
        catch (InvalidOperationException ex) { return ServiceResult<long>.BadRequest(ex.Message); }
    }

    public async Task<ServiceResult> CambiarEstadoAlertaAsync(long alertaId, SenalAlertaEstadoDto dto, long usuarioId, string? ip)
    {
        if (alertaId <= 0) return ServiceResult.BadRequest("El ID de la alerta es obligatorio.");
        string estado = dto.AleEstado?.Trim() ?? string.Empty;
        if (!EstadosAlerta.Contains(estado)) return ServiceResult.BadRequest("El estado de alerta debe ser ACTIVO o INACTIVO.");
        bool actualizado = await _repo.CambiarEstadoAlertaAsync(alertaId, estado, usuarioId, ip);
        return actualizado ? ServiceResult.Ok("Estado de alerta actualizado.") : ServiceResult.NotFound("La alerta no existe.");
    }

    public async Task<ServiceResult<IReadOnlyList<AutomonitoreoDto>>> ListarAutomonitoreoAsync(long evaluacionId) =>
        evaluacionId <= 0
            ? ServiceResult<IReadOnlyList<AutomonitoreoDto>>.BadRequest("La evaluación es obligatoria.")
            : ServiceResult<IReadOnlyList<AutomonitoreoDto>>.Ok(await _repo.ListarAutomonitoreoAsync(evaluacionId));

    public async Task<ServiceResult<long>> RegistrarAutomonitoreoAsync(AutomonitoreoGuardarDto dto, long usuarioId, string? ip)
    {
        string? error = ValidarAutomonitoreo(dto);
        if (error is not null) return ServiceResult<long>.BadRequest(error);
        try { return ServiceResult<long>.Ok(await _repo.RegistrarAutomonitoreoAsync(dto, usuarioId, ip), "Automonitoreo registrado correctamente."); }
        catch (InvalidOperationException ex) { return ServiceResult<long>.BadRequest(ex.Message); }
    }

    public async Task<ServiceResult<ResumenMatricesOperativoDto>> ObtenerResumenOperativoAsync() =>
        ServiceResult<ResumenMatricesOperativoDto>.Ok(await _repo.ObtenerResumenOperativoAsync());

    private static string? ValidarAlerta(SenalAlertaGuardarDto dto)
    {
        if (dto.AleEvaluacionId <= 0) return "La evaluación es obligatoria.";
        string codigo = dto.AleCodigo?.Trim() ?? string.Empty;
        string indicador = dto.AleIndicador?.Trim() ?? string.Empty;
        string estado = dto.AleEstado?.Trim() ?? string.Empty;
        if (codigo.Length is < 1 or > 50) return "El código de alerta debe contener entre 1 y 50 caracteres.";
        if (indicador.Length is < 1 or > 150) return "El indicador debe contener entre 1 y 150 caracteres.";
        if (!EstadosAlerta.Contains(estado)) return "El estado de alerta debe ser ACTIVO o INACTIVO.";
        return null;
    }

    private static string? ValidarAutomonitoreo(AutomonitoreoGuardarDto dto)
    {
        if (dto.MonEvaluacionId <= 0) return "La evaluación es obligatoria.";
        if (string.IsNullOrWhiteSpace(dto.MonEstadoRiesgo) || dto.MonEstadoRiesgo.Trim().Length > 30) return "El estado de riesgo es obligatorio y no puede exceder 30 caracteres.";
        if (string.IsNullOrWhiteSpace(dto.MonEstadoContr) || dto.MonEstadoContr.Trim().Length > 30) return "El estado de controles es obligatorio y no puede exceder 30 caracteres.";
        if (string.IsNullOrWhiteSpace(dto.MonResultado) || dto.MonResultado.Trim().Length > 1000) return "El resultado es obligatorio y no puede exceder 1000 caracteres.";
        return null;
    }
}
