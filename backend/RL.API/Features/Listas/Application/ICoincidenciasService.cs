using RL.API.Features.Listas.Contracts;
using RL.API.Shared.Results;

namespace RL.API.Features.Listas.Application;

public interface ICoincidenciasService
{
    Task<PaginadoDto<CoincidenciaPatronoResumenDto>> ObtenerResumenPatronoPaginadoAsync(ConsultaCoincidenciasPaginadaDto consulta);
    Task<ServiceResult<PaginadoDto<CoincidenciaPatronoDetalleDto>>> ObtenerDetallePatronoPaginadoAsync(ConsultaCoincidenciasPaginadaDto consulta);
    Task<ServiceResult<List<CoincidenciaPatronoDetalleDto>>> ObtenerDetallePatronoParaExportarAsync(string? fecha);
    Task<PaginadoDto<CoincidenciaPatronoResumenDto>> ObtenerResumenEmpleadoPaginadoAsync(ConsultaCoincidenciasPaginadaDto consulta);
    Task<ServiceResult<PaginadoDto<CoincidenciaPatronoDetalleDto>>> ObtenerDetalleEmpleadoPaginadoAsync(ConsultaCoincidenciasPaginadaDto consulta);
    Task<ServiceResult<List<CoincidenciaPatronoDetalleDto>>> ObtenerDetalleEmpleadoParaExportarAsync(string? fecha);
    Task<ServiceResult> CalificarAsync(long id, int tipoCalificacionId, long usuarioId, bool esEmpleado);
    Task<ServiceResult<string>> ObtenerResumenMatchListaAsync(long dataId, string? nombre);
}
