using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;
using RL.API.Services;

namespace RL.API.Features.MatricesRiesgos.Application;

public interface IMatricesRiesgosAppService
{
    Task<ServiceResult<MetodologiaCalculoDto>> ObtenerMetodologiaVigenteAsync();
    Task<ServiceResult<MatricesRiesgoDashboardDto>> ObtenerDashboardAsync();
    Task<ServiceResult<MatricesRiesgoReporteDto>> ObtenerReporteAsync(MatrizRiesgoReporteFiltroDto filtro);
    Task<ServiceResult<MatrizRiesgoExportacionDto>> ExportarReporteAsync(MatrizRiesgoReporteFiltroDto filtro, string formato, long usuarioId, string? usuarioEmail, string? ip);
    Task<ServiceResult<List<MatrizRiesgoResumenDto>>> ListarAsync(MatrizRiesgoFiltroDto filtro);
    Task<ServiceResult<MatrizRiesgoDetalleDto>> ObtenerAsync(long matrizId);
    Task<ServiceResult<MatrizRiesgoDetalleDto>> CrearAsync(MatrizRiesgoCrearRequestDto dto, long usuarioId, string? usuarioEmail, string? ip);
    Task<ServiceResult<MatrizRiesgoDetalleDto>> ActualizarAsync(long matrizId, MatrizRiesgoCrearRequestDto dto, long usuarioId, string? usuarioEmail, string? ip);
    Task<ServiceResult<MatrizCalculoResultadoDto>> CalcularAsync(long matrizId, MatrizRiesgoCalcularRequestDto dto, bool esRecalculo, long usuarioId, string? usuarioEmail, string? ip);
    Task<ServiceResult> CambiarEstadoAsync(long matrizId, MatrizRiesgoCambiarEstadoRequestDto dto, long usuarioId, string? usuarioEmail, string? ip);
    Task<ServiceResult> EliminarMatrizAsync(long matrizId, MatrizRiesgoInactivarRequestDto dto, long usuarioId, string? usuarioEmail, string? ip);
    Task<ServiceResult<List<MatrizRiesgoHistorialDto>>> ObtenerHistorialAsync(long matrizId);
    Task<ServiceResult<List<MatrizRiesgoCriterioDto>>> ListarCriteriosAsync(bool incluirInactivos);
    Task<ServiceResult<MatrizRiesgoCriterioDto>> CrearCriterioAsync(MatrizRiesgoCriterioRequestDto dto, long usuarioId, string? usuarioEmail, string? ip);
    Task<ServiceResult<MatrizRiesgoCriterioDto>> ActualizarCriterioAsync(long criterioId, MatrizRiesgoCriterioRequestDto dto, long usuarioId, string? usuarioEmail, string? ip);
    Task<ServiceResult> InactivarCriterioAsync(long criterioId, MatrizRiesgoInactivarRequestDto dto, long usuarioId, string? usuarioEmail, string? ip);
    Task<ServiceResult> EliminarCriterioAsync(long criterioId, MatrizRiesgoInactivarRequestDto dto, long usuarioId, string? usuarioEmail, string? ip);
}
