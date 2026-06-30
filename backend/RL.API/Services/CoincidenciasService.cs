using RL.API.DTOs;
using RL.API.Repositories;
using System.Globalization;

namespace RL.API.Services;

public interface ICoincidenciasService
{
    Task<List<CoincidenciaPatronoResumenDto>> ObtenerResumenPatronoAsync();
    Task<ServiceResult<List<CoincidenciaPatronoDetalleDto>>> ObtenerDetallePatronoAsync(string? fecha);
    Task<List<CoincidenciaPatronoResumenDto>> ObtenerResumenEmpleadoAsync();
    Task<ServiceResult<List<CoincidenciaPatronoDetalleDto>>> ObtenerDetalleEmpleadoAsync(string? fecha);
    Task<ServiceResult> CalificarAsync(long id, int tipoCalificacionId, long usuarioId, bool esEmpleado);
    Task<ServiceResult<string>> ObtenerResumenMatchListaAsync(long dataId, string? nombre);
}

public sealed class CoincidenciasService : ICoincidenciasService
{
    private readonly IListasRepository _repo;

    public CoincidenciasService(IListasRepository repo)
    {
        _repo = repo;
    }

    public Task<List<CoincidenciaPatronoResumenDto>> ObtenerResumenPatronoAsync() => _repo.ObtenerResumenCoincidenciasPatronoAsync();

    public async Task<ServiceResult<List<CoincidenciaPatronoDetalleDto>>> ObtenerDetallePatronoAsync(string? fecha)
    {
        var fechaNormalizada = NormalizarFecha(fecha);
        if (fechaNormalizada == null)
            return ServiceResult<List<CoincidenciaPatronoDetalleDto>>.BadRequest("El parametro fecha es obligatorio y debe tener formato YYYY-MM-DD.");

        var result = await _repo.ObtenerDetalleCoincidenciasPatronoAsync(fechaNormalizada);
        return ServiceResult<List<CoincidenciaPatronoDetalleDto>>.Ok(result);
    }

    public Task<List<CoincidenciaPatronoResumenDto>> ObtenerResumenEmpleadoAsync() => _repo.ObtenerResumenCoincidenciasEmpleadoAsync();

    public async Task<ServiceResult<List<CoincidenciaPatronoDetalleDto>>> ObtenerDetalleEmpleadoAsync(string? fecha)
    {
        var fechaNormalizada = NormalizarFecha(fecha);
        if (fechaNormalizada == null)
            return ServiceResult<List<CoincidenciaPatronoDetalleDto>>.BadRequest("El parametro fecha es obligatorio y debe tener formato YYYY-MM-DD.");

        var result = await _repo.ObtenerDetalleCoincidenciasEmpleadoAsync(fechaNormalizada);
        return ServiceResult<List<CoincidenciaPatronoDetalleDto>>.Ok(result);
    }

    public async Task<ServiceResult> CalificarAsync(long id, int tipoCalificacionId, long usuarioId, bool esEmpleado)
    {
        if (id <= 0)
            return ServiceResult.BadRequest("El identificador de coincidencia es obligatorio.");

        if (tipoCalificacionId is not (1 or 2))
            return ServiceResult.BadRequest("Solo se permite calificar como Positivo o Falso Positivo.");

        var ok = await _repo.CalificarCoincidenciaAsync(id, tipoCalificacionId, usuarioId, esEmpleado);
        return ok
            ? ServiceResult.Ok("Coincidencia calificada exitosamente.")
            : ServiceResult.NotFound("No se encontro el registro de coincidencia especificado para este modulo.");
    }

    public async Task<ServiceResult<string>> ObtenerResumenMatchListaAsync(long dataId, string? nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return ServiceResult<string>.BadRequest("El parametro nombre es requerido.");

        var detail = await _repo.ObtenerResumenMatchListaAsync(dataId, nombre);
        return ServiceResult<string>.Ok(detail);
    }

    private static string? NormalizarFecha(string? fecha)
    {
        if (string.IsNullOrWhiteSpace(fecha))
            return null;

        return DateTime.TryParseExact(
            fecha.Trim(),
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
            ? parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : null;
    }
}
