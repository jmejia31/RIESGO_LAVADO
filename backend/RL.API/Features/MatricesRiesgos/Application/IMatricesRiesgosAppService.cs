using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Shared.Results;

namespace RL.API.Features.MatricesRiesgos.Application;

public interface IMatricesRiesgosAppService
{
    Task<ServiceResult<VersionFormularioDto>> ObtenerVersionVigenteFormularioAsync(string? familiaCodigo);
    Task<ServiceResult<VersionFormularioDto>> ObtenerVersionFormularioAsync(long versionId);
    Task<ServiceResult<long>> CrearBorradorFormularioAsync(long familiaId, string codigoFormulario, string jsonConfig, long usuarioId);
    Task<ServiceResult<long>> ClonarVersionFormularioAsync(long versionOrigenId, long usuarioId);
    Task<ServiceResult> ActualizarBorradorFormularioAsync(long versionId, string jsonConfig, long usuarioId);
    Task<ServiceResult> PublicarVersionFormularioAsync(long versionId, long usuarioId);
    Task<ServiceResult> CambiarEstadoVigenciaFormularioAsync(long versionId, bool vigente, long usuarioId);
    Task<ServiceResult> EliminarVersionFormularioAsync(long versionId);
    Task<ServiceResult<List<VersionFormularioDto>>> ListarHistorialVersionesFormularioAsync(string familiaCodigo);

    Task<ServiceResult<FamiliasFormularioPaginadasDto>> ListarFamiliasFormularioPaginadasAsync(ConsultaFamiliasFormularioPaginadaDto filtro);
    Task<ServiceResult<FamiliaFormularioDto>> ObtenerFamiliaFormularioPorIdAsync(long famId);
    Task<ServiceResult<FamiliaPredeterminadaDto>> ObtenerFamiliaPredeterminadaAsync();
    Task<ServiceResult> EstablecerFamiliaPredeterminadaAsync(long famId, long usuarioId, string? ip);
    Task<ServiceResult<long>> CrearFamiliaFormularioAsync(CrearFamiliaFormularioDto dto);
    Task<ServiceResult> ActualizarFamiliaFormularioAsync(long famId, ActualizarFamiliaFormularioDto dto);
    Task<ServiceResult> DesactivarFamiliaFormularioAsync(long famId);

    Task<ServiceResult<EvaluacionRiesgoDto>> ObtenerEvaluacionAsync(long evaId);
    Task<ServiceResult<EvaluacionesPaginadasDto>> ListarEvaluacionesPaginadasAsync(ConsultaEvaluacionPaginadaDto filtro);
    Task<ServiceResult<long>> CrearEvaluacionAsync(EvaluacionRiesgoDto dto, long usuarioId, string? ip);
    Task<ServiceResult> ActualizarEvaluacionAsync(EvaluacionRiesgoDto dto, long usuarioId, string? ip);
    Task<ServiceResult> TransicionarEstadoEvaluacionAsync(long evaId, string nuevoEstado, string? motivo, long usuarioId, string? ip);
    Task<ServiceResult<List<FlujoEvaluacionDto>>> ObtenerFlujosEvaluacionAsync(long evaId);

    Task<ServiceResult<EvidenciaDto>> CargarArchivoEvidenciaFisicaAsync(IFormFile archivo, long usuarioId);
    Task<ServiceResult<EvidenciaDto>> ObtenerEvidenciaFisicaAsync(long evidenciaId);
    Task<ServiceResult> VincularEvidenciaAsync(VincularEvidenciaDto dto, long usuarioId, string? ip);
    Task<ServiceResult> EliminarEvidenciaAsync(long evidenciaId, long usuarioId, string? ip);

    Task<ServiceResult<ReporteMatricesPaginadoDto>> ObtenerConsolidadoPaginadoAsync(FiltroReporteMatricesDto filtro);
    Task<ServiceResult<IReadOnlyList<RiesgoReporteFilaDto>>> ObtenerConsolidadoParaExportacionAsync(FiltroReporteMatricesDto filtro);
    Task<ServiceResult<MetodologiaFormularioDto>> ObtenerMetodologiaDinamicaVigenteAsync();
    Task<ServiceResult<MetodologiaFormularioDto>> ObtenerMetodologiaDinamicaPorVersionAsync(long versionId);
}
