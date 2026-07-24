using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;

namespace RL.API.Features.MatricesRiesgos.Persistence;

public interface IMatricesRiesgosRepository
{
    Task<MetodologiaCalculoDto?> ObtenerMetodologiaVigenteAsync();
    Task<MatrizRiesgoDetalleDto?> ObtenerMatrizAsync(long matrizId);
    Task<List<MatrizRiesgoResumenDto>> ListarMatricesAsync(MatrizRiesgoFiltroDto filtro);
    Task<MatricesRiesgoDashboardDto> ObtenerDashboardAsync(MatrizRiesgoReporteFiltroDto filtro);
    Task<MatricesRiesgoReporteDto> ObtenerReporteAsync(MatrizRiesgoReporteFiltroDto filtro);
    Task RegistrarExportacionReporteAsync(MatrizRiesgoReporteFiltroDto filtro, string formato, long usuarioId, string? usuarioEmail, string? ip);
    Task<long> CrearMatrizAsync(MatrizRiesgoCrearRequestDto dto, long usuarioId, string? usuarioEmail, string? ip);
    Task<bool> ActualizarMatrizAsync(long matrizId, MatrizRiesgoCrearRequestDto dto, long usuarioId, string? usuarioEmail, string? ip);
    Task<MatrizCalculoRequestDto?> PrepararSolicitudCalculoAsync(long matrizId, string tipoCalculo, string? motivoCalculo, bool esRecalculo);
    Task PersistirResultadoCalculoAsync(long matrizId, MatrizCalculoResultadoDto resultado, string? motivoCalculo, bool esRecalculo, long usuarioId, string? usuarioEmail, string? ip);
    Task<bool> CambiarEstadoAsync(long matrizId, string estado, string motivo, long usuarioId, string? usuarioEmail, string? ip);
    Task<bool> EliminarMatrizAsync(long matrizId, string motivo, long usuarioId, string? usuarioEmail, string? ip);
    Task<List<MatrizRiesgoHistorialDto>> ObtenerHistorialAsync(long matrizId);
    Task<List<MatrizRiesgoPlanAccionDto>> ListarPlanesAsync(long matrizId);
    Task<long> CrearPlanAsync(long matrizId, MatrizRiesgoPlanAccionRequestDto dto, long usuarioId, string? usuarioEmail, string? ip);
    Task<bool> ActualizarPlanAsync(long matrizId, long planId, MatrizRiesgoPlanAccionRequestDto dto, long usuarioId, string? usuarioEmail, string? ip);
    Task<bool> CambiarEstadoPlanAsync(long matrizId, long planId, string estado, string motivo, long usuarioId, string? usuarioEmail, string? ip);
    Task<bool> InactivarPlanAsync(long matrizId, long planId, string motivo, long usuarioId, string? usuarioEmail, string? ip);
    Task<bool> ReactivarPlanAsync(long matrizId, long planId, string motivo, long usuarioId, string? usuarioEmail, string? ip);
    Task<bool> TienePlanTratadoParaCierreAsync(long matrizId);
    Task<List<MatrizRiesgoEvidenciaDto>> ListarEvidenciasAsync(long matrizId);
    Task<long> RegistrarEvidenciaAsync(MatrizRiesgoEvidenciaRegistroDto dto, long usuarioId, string? usuarioEmail, string? ip);
    Task<MatrizRiesgoEvidenciaDto?> ObtenerEvidenciaAsync(long matrizId, long evidenciaId);
    Task RegistrarDescargaEvidenciaAsync(long matrizId, long evidenciaId, long usuarioId, string? usuarioEmail, string? ip);
    Task<bool> InactivarEvidenciaAsync(long matrizId, long evidenciaId, string motivo, long usuarioId, string? usuarioEmail, string? ip);
    Task<List<MatrizRiesgoCriterioDto>> ListarCriteriosAsync(bool incluirInactivos);
    Task<long> CrearCriterioAsync(MatrizRiesgoCriterioRequestDto dto, long usuarioId, string? usuarioEmail, string? ip);
    Task<bool> ActualizarCriterioAsync(long criterioId, MatrizRiesgoCriterioRequestDto dto, long usuarioId, string? usuarioEmail, string? ip);
    Task<bool> InactivarCriterioAsync(long criterioId, string motivo, long usuarioId, string? usuarioEmail, string? ip);
    Task<bool> ReactivarCriterioAsync(long criterioId, string motivo, long usuarioId, string? usuarioEmail, string? ip);
    Task<bool> CriterioTieneUsoHistoricoAsync(long criterioId);
    Task<bool> EliminarCriterioAsync(long criterioId, string motivo, long usuarioId, string? usuarioEmail, string? ip);
}
