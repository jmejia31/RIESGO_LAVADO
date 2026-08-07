using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Shared.Results;

namespace RL.API.Features.MatricesRiesgos.Application;

public interface IMatricesRiesgosGestionService
{
    Task<ServiceResult<IReadOnlyList<RiesgoDto>>> ListarRiesgosAsync(bool incluirInactivos);
    Task<ServiceResult<RiesgoDto>> ObtenerRiesgoAsync(long riesgoId);
    Task<ServiceResult<long>> CrearRiesgoAsync(RiesgoGuardarDto dto, long usuarioId, string? ip);
    Task<ServiceResult> ActualizarRiesgoAsync(long riesgoId, RiesgoGuardarDto dto, long usuarioId, string? ip);
}

public sealed class MatricesRiesgosGestionService : IMatricesRiesgosGestionService
{
    private readonly IMatricesRiesgosGestionRepository _repo;

    public MatricesRiesgosGestionService(IMatricesRiesgosGestionRepository repo)
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
    }

    public async Task<ServiceResult<IReadOnlyList<RiesgoDto>>> ListarRiesgosAsync(bool incluirInactivos) =>
        ServiceResult<IReadOnlyList<RiesgoDto>>.Ok(await _repo.ListarRiesgosAsync(incluirInactivos));

    public async Task<ServiceResult<RiesgoDto>> ObtenerRiesgoAsync(long riesgoId)
    {
        if (riesgoId <= 0) return ServiceResult<RiesgoDto>.BadRequest("El ID del riesgo debe ser mayor que cero.");
        RiesgoDto? riesgo = await _repo.ObtenerRiesgoAsync(riesgoId);
        return riesgo is null
            ? ServiceResult<RiesgoDto>.NotFound($"No se encontró el riesgo con ID {riesgoId}.")
            : ServiceResult<RiesgoDto>.Ok(riesgo);
    }

    public async Task<ServiceResult<long>> CrearRiesgoAsync(RiesgoGuardarDto dto, long usuarioId, string? ip)
    {
        string? error = Validar(dto);
        if (error is not null) return ServiceResult<long>.BadRequest(error);
        try
        {
            long id = await _repo.CrearRiesgoAsync(dto, usuarioId, ip);
            return ServiceResult<long>.Ok(id, "Riesgo creado correctamente.");
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult<long>.BadRequest(ex.Message);
        }
    }

    public async Task<ServiceResult> ActualizarRiesgoAsync(long riesgoId, RiesgoGuardarDto dto, long usuarioId, string? ip)
    {
        if (riesgoId <= 0) return ServiceResult.BadRequest("El ID del riesgo debe ser mayor que cero.");
        string? error = Validar(dto);
        if (error is not null) return ServiceResult.BadRequest(error);
        try
        {
            bool actualizado = await _repo.ActualizarRiesgoAsync(riesgoId, dto, usuarioId, ip);
            return actualizado
                ? ServiceResult.Ok("Riesgo actualizado correctamente.")
                : ServiceResult.NotFound($"No se encontró el riesgo con ID {riesgoId}.");
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult.BadRequest(ex.Message);
        }
    }

    private static string? Validar(RiesgoGuardarDto dto)
    {
        if (dto is null) return "Los datos del riesgo son obligatorios.";
        string codigo = dto.RieCodigo?.Trim() ?? string.Empty;
        string nombre = dto.RieNombre?.Trim() ?? string.Empty;
        string descripcion = dto.RieDescripcion?.Trim() ?? string.Empty;
        if (codigo.Length is < 1 or > 30) return "El código del riesgo debe contener entre 1 y 30 caracteres.";
        if (nombre.Length is < 1 or > 250) return "El nombre del riesgo debe contener entre 1 y 250 caracteres.";
        if (descripcion.Length > 2000) return "La descripción del riesgo no puede exceder 2000 caracteres.";
        return null;
    }
}
