using RL.API.Features.Listas.Contracts;
using RL.API.Features.Listas.Persistence;
using RL.API.Shared.Results;
using System.Globalization;

namespace RL.API.Features.Listas.Application;

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
        // Proceso de consulta de detalle: normaliza la fecha antes de consultar Oracle
        // para mantener consistencia entre filtros de patronos y empleados.
        var fechaNormalizada = NormalizarFecha(fecha);
        if (fechaNormalizada == null)
            return ServiceResult<List<CoincidenciaPatronoDetalleDto>>.BadRequest("El parámetro fecha es obligatorio y debe tener formato YYYY-MM-DD.");

        var result = await _repo.ObtenerDetalleCoincidenciasPatronoAsync(fechaNormalizada);
        return ServiceResult<List<CoincidenciaPatronoDetalleDto>>.Ok(result);
    }

    public Task<List<CoincidenciaPatronoResumenDto>> ObtenerResumenEmpleadoAsync() => _repo.ObtenerResumenCoincidenciasEmpleadoAsync();

    public async Task<ServiceResult<List<CoincidenciaPatronoDetalleDto>>> ObtenerDetalleEmpleadoAsync(string? fecha)
    {
        var fechaNormalizada = NormalizarFecha(fecha);
        if (fechaNormalizada == null)
            return ServiceResult<List<CoincidenciaPatronoDetalleDto>>.BadRequest("El parámetro fecha es obligatorio y debe tener formato YYYY-MM-DD.");

        var result = await _repo.ObtenerDetalleCoincidenciasEmpleadoAsync(fechaNormalizada);
        return ServiceResult<List<CoincidenciaPatronoDetalleDto>>.Ok(result);
    }

    public async Task<ServiceResult> CalificarAsync(long id, int tipoCalificacionId, long usuarioId, bool esEmpleado)
    {
        // Proceso de calificación: valida la decisión permitida y delega la persistencia
        // auditada al repositorio correspondiente para patrono o empleado.
        if (id <= 0)
            return ServiceResult.BadRequest("El identificador de coincidencia es obligatorio.");

        if (tipoCalificacionId is not (1 or 2))
            return ServiceResult.BadRequest("Solo se permite calificar como Positivo o Falso Positivo.");

        var ok = await _repo.CalificarCoincidenciaAsync(id, tipoCalificacionId, usuarioId, esEmpleado);
        return ok
            ? ServiceResult.Ok("Coincidencia calificada exitosamente.")
            : ServiceResult.NotFound("No se encontró el registro de coincidencia especificado para este módulo.");
    }

    public async Task<ServiceResult<string>> ObtenerResumenMatchListaAsync(long dataId, string? nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return ServiceResult<string>.BadRequest("El parámetro nombre es requerido.");

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
