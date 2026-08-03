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
    Task<List<VersionFormularioDto>> ListarHistorialVersionesFormularioAsync(string familiaCodigo);

    Task<EvaluacionRiesgoDto?> ObtenerEvaluacionAsync(long evaId);
    Task<List<EvaluacionRiesgoDto>> ListarEvaluacionesPaginadasAsync(ConsultaEvaluacionPaginadaDto filtro);
    Task<long> CrearEvaluacionAsync(EvaluacionRiesgoDto dto, long usuarioId, string? ip);
    Task<bool> ActualizarEvaluacionAsync(EvaluacionRiesgoDto dto, long usuarioId, string? ip);
    Task<bool> TransicionarEstadoEvaluacionAsync(long evaId, string nuevoEstado, string? motivo, long usuarioId, string? ip);
    Task<List<RevisionEvaluacionDto>> ObtenerRevisionesEvaluacionAsync(long evaId);

    Task<long> RegistrarEvidenciaFisicaAsync(EvidenciaRegistroDto dto, long usuarioId);
    Task<EvidenciaDto?> ObtenerEvidenciaFisicaAsync(long evidenciaId);
    Task<bool> VincularEvidenciaRiesgoAsync(AsociarEvidenciaRiesgoDto dto, long usuarioId, string? ip);
    Task<bool> VincularEvidenciaEvaluacionAsync(AsociarEvidenciaEvaluacionDto dto, long usuarioId, string? ip);
    Task<bool> VincularEvidenciaControlAsync(AsociarEvidenciaControlDto dto, long usuarioId, string? ip);
    Task<bool> VincularEvidenciaPlanAsync(AsociarEvidenciaPlanDto dto, long usuarioId, string? ip);
    Task<bool> VincularEvidenciaActividadAsync(AsociarEvidenciaActividadDto dto, long usuarioId, string? ip);
    Task<bool> VincularEvidenciaAlertaAsync(AsociarEvidenciaAlertaDto dto, long usuarioId, string? ip);
    Task<bool> VincularEvidenciaAutomonitoreoAsync(AsociarEvidenciaAutomonitoreoDto dto, long usuarioId, string? ip);
    Task<bool> VincularEvidenciaRevisionAsync(AsociarEvidenciaRevisionDto dto, long usuarioId, string? ip);
    Task<bool> VincularEvidenciaAprobacionAsync(AsociarEvidenciaAprobacionDto dto, long usuarioId, string? ip);
    Task<ResultadoEliminacionEvidencia> EliminarEvidenciaSeguraAsync(long evidenciaId, Func<Task<bool>> eliminarArchivoFisico, long usuarioId, string? ip);

    async Task<IReadOnlyList<RiesgoReporteFilaDto>> ObtenerConsolidadoTipadoAsync()
    {
        if (this is not MatricesRiesgosRepository repositorio)
        {
            throw new InvalidOperationException("La implementación debe proporcionar el consolidado tipado.");
        }

#pragma warning disable CS0618
        List<Dictionary<string, object>> filas = await repositorio.ObtenerConsolidadoMatricesAsync();
#pragma warning restore CS0618
        var resultado = new List<RiesgoReporteFilaDto>(filas.Count);
        foreach (Dictionary<string, object> fila in filas)
        {
            resultado.Add(new RiesgoReporteFilaDto
            {
                EvaluacionId = Convert.ToInt64(fila["EvaluacionId"]),
                CodigoRiesgo = Convert.ToString(fila["CodigoRiesgo"]) ?? string.Empty,
                EstadoEvaluacion = Convert.ToString(fila["Estado"]) ?? string.Empty,
                Vri = Convert.ToInt32(fila["Vri"]),
                Vrr = Convert.ToInt32(fila["Vrr"]),
                NivelInherente = Convert.ToString(fila["NivelInherente"]) ?? string.Empty,
                NivelResidual = Convert.ToString(fila["NivelResidual"]) ?? string.Empty,
                RespuestaRiesgo = Convert.ToString(fila["RespuestaRiesgo"]) ?? string.Empty,
                AreaPrincipal = Convert.ToString(fila["Area"]) ?? string.Empty,
                DuenoRiesgo = Convert.ToString(fila["Dueno"]) ?? string.Empty,
                FechaEvaluacion = Convert.ToDateTime(fila["Fecha"])
            });
        }
        return resultado;
    }

    async Task<MetodologiaFormularioDto?> ObtenerMetodologiaDinamicaVigenteAsync()
    {
        if (this is not MatricesRiesgosRepository repositorio)
        {
            throw new InvalidOperationException("La implementación debe proporcionar la metodología dinámica.");
        }

#pragma warning disable CS0618
        MetodologiaMatricesDto? anterior = await repositorio.ObtenerMetodologiaVigenteAsync();
#pragma warning restore CS0618
        return anterior is null
            ? null
            : new MetodologiaFormularioDto
            {
                Codigo = anterior.Version,
                Version = 0,
                Secciones = Array.Empty<SeccionFormularioDto>(),
                Catalogos = Array.Empty<CatalogoMatricesDto>(),
                Reglas = Array.Empty<ReglaCalculoMatricesDto>()
            };
    }
}
