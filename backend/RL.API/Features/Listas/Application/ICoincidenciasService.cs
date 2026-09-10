using RL.API.Features.Listas.Contracts;
using RL.API.Shared.Results;

namespace RL.API.Features.Listas.Application;

public interface ICoincidenciasService
{
    Task<List<CoincidenciaPatronoResumenDto>> ObtenerResumenPatronoAsync();
    Task<PaginadoDto<CoincidenciaPatronoResumenDto>> ObtenerResumenPatronoPaginadoAsync(ConsultaCoincidenciasPaginadaDto consulta);
    Task<ServiceResult<List<CoincidenciaPatronoDetalleDto>>> ObtenerDetallePatronoAsync(string? fecha);
    Task<ServiceResult<PaginadoDto<CoincidenciaPatronoDetalleDto>>> ObtenerDetallePatronoPaginadoAsync(ConsultaCoincidenciasPaginadaDto consulta);
    Task<List<CoincidenciaPatronoResumenDto>> ObtenerResumenEmpleadoAsync();
    Task<PaginadoDto<CoincidenciaPatronoResumenDto>> ObtenerResumenEmpleadoPaginadoAsync(ConsultaCoincidenciasPaginadaDto consulta);
    Task<ServiceResult<List<CoincidenciaPatronoDetalleDto>>> ObtenerDetalleEmpleadoAsync(string? fecha);
    Task<ServiceResult<PaginadoDto<CoincidenciaPatronoDetalleDto>>> ObtenerDetalleEmpleadoPaginadoAsync(ConsultaCoincidenciasPaginadaDto consulta);
    Task<ServiceResult> CalificarAsync(long id, int tipoCalificacionId, long usuarioId, bool esEmpleado);
    Task<ServiceResult<string>> ObtenerResumenMatchListaAsync(long dataId, string? nombre);
}
