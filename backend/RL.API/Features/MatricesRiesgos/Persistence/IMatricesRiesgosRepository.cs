using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RL.API.Features.MatricesRiesgos.Contracts;

namespace RL.API.Features.MatricesRiesgos.Persistence;

public enum ResultadoEliminacionEvidencia
{
    Exito,
    NoExiste,
    TieneVinculos,
    FalloDisco,
    FalloCommit
}

public interface IMatricesRiesgosRepository
{
    Task<VersionFormularioDto?> ObtenerVersionVigenteFormularioAsync(string familiaCodigo);
    Task<VersionFormularioDto?> ObtenerVersionFormularioAsync(long versionId);
    Task<long> CrearBorradorFormularioAsync(long familiaId, string codigoFormulario, string jsonConfig, long usuarioId);
    Task<long> ClonarVersionFormularioAsync(long versionOrigenId, long usuarioId);
    Task<bool> ActualizarBorradorFormularioAsync(long versionId, string jsonConfig, string hash, long usuarioId);
    Task<bool> PublicarVersionFormularioAsync(long versionId, string hash, long usuarioId);
    Task<bool> CambiarEstadoVigenciaFormularioAsync(long versionId, bool vigente, long usuarioId);
    Task<bool> EliminarVersionFormularioAsync(long versionId);
    Task<List<VersionFormularioDto>> ListarHistorialVersionesFormularioAsync(string familiaCodigo);

    Task<List<FamiliaFormularioDto>> ListarFamiliasFormularioAsync();
    Task<FamiliaFormularioDto?> ObtenerFamiliaFormularioPorIdAsync(long famId);
    Task<FamiliaFormularioDto?> ObtenerFamiliaFormularioPorCodigoAsync(string famCodigo);
    Task<long> CrearFamiliaFormularioAsync(string famCodigo, string famNombre, string? famDescripcion, bool famActivo);
    Task<bool> ActualizarFamiliaFormularioAsync(long famId, string famNombre, string? famDescripcion, bool famActivo);
    Task<bool> DesactivarFamiliaFormularioAtomicoAsync(long famId);

    Task<EvaluacionRiesgoDto?> ObtenerEvaluacionAsync(long evaId);
    Task<List<EvaluacionRiesgoDto>> ListarEvaluacionesPaginadasAsync(ConsultaEvaluacionPaginadaDto filtro);
    Task<long> CrearEvaluacionAsync(EvaluacionRiesgoDto dto, long usuarioId, string? ip);
    Task<bool> ActualizarEvaluacionAsync(EvaluacionRiesgoDto dto, long usuarioId, string? ip);
    Task<bool> TransicionarEstadoEvaluacionAsync(long evaId, string nuevoEstado, string? motivo, long usuarioId, string? ip);
    Task<List<FlujoEvaluacionDto>> ObtenerFlujosEvaluacionAsync(long evaId);

    Task<long> RegistrarEvidenciaFisicaAsync(EvidenciaRegistroDto dto, long usuarioId);
    Task<EvidenciaDto?> ObtenerEvidenciaFisicaAsync(long evidenciaId);
    Task<bool> VincularEvidenciaAsync(VincularEvidenciaDto dto, long usuarioId, string? ip);
    Task<ResultadoEliminacionEvidencia> EliminarEvidenciaSeguraAsync(long evidenciaId, Func<Task<bool>> eliminarArchivoFisico, long usuarioId, string? ip);

    Task<IReadOnlyList<RiesgoReporteFilaDto>> ObtenerConsolidadoTipadoAsync();
    Task<MetodologiaFormularioDto?> ObtenerMetodologiaDinamicaVigenteAsync();
}
