using RL.API.Features.Listas.Contracts;
using RL.API.Services;

namespace RL.API.Features.Listas.Application;

public interface ICoincidenciasService
{
    Task<List<CoincidenciaPatronoResumenDto>> ObtenerResumenPatronoAsync();
    Task<ServiceResult<List<CoincidenciaPatronoDetalleDto>>> ObtenerDetallePatronoAsync(string? fecha);
    Task<List<CoincidenciaPatronoResumenDto>> ObtenerResumenEmpleadoAsync();
    Task<ServiceResult<List<CoincidenciaPatronoDetalleDto>>> ObtenerDetalleEmpleadoAsync(string? fecha);
    Task<ServiceResult> CalificarAsync(long id, int tipoCalificacionId, long usuarioId, bool esEmpleado);
    Task<ServiceResult<string>> ObtenerResumenMatchListaAsync(long dataId, string? nombre);
}
