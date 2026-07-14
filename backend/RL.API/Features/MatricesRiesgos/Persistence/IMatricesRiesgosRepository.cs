using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;

namespace RL.API.Features.MatricesRiesgos.Persistence;

public interface IMatricesRiesgosRepository
{
    Task<MetodologiaCalculoDto?> ObtenerMetodologiaVigenteAsync();
    Task<MatrizRiesgoDetalleDto?> ObtenerMatrizAsync(long matrizId);
    Task<List<MatrizRiesgoResumenDto>> ListarMatricesAsync(MatrizRiesgoFiltroDto filtro);
    Task<MatricesRiesgoDashboardDto> ObtenerDashboardAsync();
    Task<MatricesRiesgoReporteDto> ObtenerReporteAsync(MatrizRiesgoReporteFiltroDto filtro);
    Task RegistrarExportacionReporteAsync(MatrizRiesgoReporteFiltroDto filtro, string formato, long usuarioId, string? usuarioEmail, string? ip);
    Task<long> CrearMatrizAsync(MatrizRiesgoCrearRequestDto dto, long usuarioId, string? usuarioEmail, string? ip);
    Task<bool> ActualizarMatrizAsync(long matrizId, MatrizRiesgoCrearRequestDto dto, long usuarioId, string? usuarioEmail, string? ip);
    Task<MatrizCalculoRequestDto?> PrepararSolicitudCalculoAsync(long matrizId, string tipoCalculo, string? motivoCalculo, bool esRecalculo);
    Task PersistirResultadoCalculoAsync(long matrizId, MatrizCalculoResultadoDto resultado, string? motivoCalculo, bool esRecalculo, long usuarioId, string? usuarioEmail, string? ip);
    Task<bool> CambiarEstadoAsync(long matrizId, string estado, string motivo, long usuarioId, string? usuarioEmail, string? ip);
    Task<bool> EliminarMatrizAsync(long matrizId, string motivo, long usuarioId, string? usuarioEmail, string? ip);
    Task<List<MatrizRiesgoHistorialDto>> ObtenerHistorialAsync(long matrizId);
    Task<List<MatrizRiesgoCriterioDto>> ListarCriteriosAsync(bool incluirInactivos);
    Task<long> CrearCriterioAsync(MatrizRiesgoCriterioRequestDto dto, long usuarioId, string? usuarioEmail, string? ip);
    Task<bool> ActualizarCriterioAsync(long criterioId, MatrizRiesgoCriterioRequestDto dto, long usuarioId, string? usuarioEmail, string? ip);
    Task<bool> InactivarCriterioAsync(long criterioId, string motivo, long usuarioId, string? usuarioEmail, string? ip);
    Task<bool> EliminarCriterioAsync(long criterioId, string motivo, long usuarioId, string? usuarioEmail, string? ip);
}
