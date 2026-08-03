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
    // ============================================================
    // 1. GESTIÓN DEL CICLO DE VIDA DEL FORMULARIO Y VERSIONES
    // ============================================================
    Task<VersionFormularioDto?> ObtenerVersionVigenteFormularioAsync(string familiaCodigo);
    Task<VersionFormularioDto?> ObtenerVersionFormularioAsync(long versionId);
    Task<long> CrearBorradorFormularioAsync(long familiaId, string codigoFormulario, string jsonConfig, long usuarioId);
    Task<long> ClonarVersionFormularioAsync(long versionOrigenId, long usuarioId);
    Task<bool> ActualizarBorradorFormularioAsync(long versionId, string jsonConfig, string hash, long usuarioId);
    Task<bool> PublicarVersionFormularioAsync(long versionId, string hash, long usuarioId);
    Task<bool> CambiarEstadoVigenciaFormularioAsync(long versionId, bool vigente, long usuarioId);
    Task<List<VersionFormularioDto>> ListarHistorialVersionesFormularioAsync(string familiaCodigo);

    // ============================================================
    // 2. GESTIÓN DE EVALUACIONES E HISTORIAL DE CAMBIOS
    // ============================================================
    Task<EvaluacionRiesgoDto?> ObtenerEvaluacionAsync(long evaId);
    Task<List<EvaluacionRiesgoDto>> ListarEvaluacionesPaginadasAsync(ConsultaEvaluacionPaginadaDto filtro);
    Task<long> CrearEvaluacionAsync(EvaluacionRiesgoDto dto, long usuarioId, string? ip);
    Task<bool> ActualizarEvaluacionAsync(EvaluacionRiesgoDto dto, long usuarioId, string? ip);
    Task<bool> TransicionarEstadoEvaluacionAsync(long evaId, string nuevoEstado, string? motivo, long usuarioId, string? ip);
    Task<List<RevisionEvaluacionDto>> ObtenerRevisionesEvaluacionAsync(long evaId);

    // ============================================================
    // 3. ARCHIVO FÍSICO CENTRAL DE EVIDENCIAS Y SUS VINCULACIONES
    // ============================================================
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

    // ============================================================
    // 4. MIGRACIÓN CONTROLADA DE REPORTES A DTOs TIPADOS
    // ============================================================
    [Obsolete("Contrato transitorio de Fase 1.3. Migrar consumidores a ObtenerConsolidadoTipadoAsync y eliminar antes del cierre.")]
    Task<List<Dictionary<string, object>>> ObtenerConsolidadoMatricesAsync();

    async Task<IReadOnlyList<RiesgoReporteFilaDto>> ObtenerConsolidadoTipadoAsync()
    {
#pragma warning disable CS0618
        List<Dictionary<string, object>> filas = await ObtenerConsolidadoMatricesAsync();
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

    // ============================================================
    // 5. MIGRACIÓN CONTROLADA DE METODOLOGÍA A CONTRATO DINÁMICO
    // ============================================================
    [Obsolete("Contrato transitorio de Fase 1.3. Migrar consumidores a ObtenerMetodologiaDinamicaVigenteAsync y eliminar antes del cierre.")]
    Task<MetodologiaMatricesDto?> ObtenerMetodologiaVigenteAsync();

    async Task<MetodologiaFormularioDto?> ObtenerMetodologiaDinamicaVigenteAsync()
    {
#pragma warning disable CS0618
        MetodologiaMatricesDto? legado = await ObtenerMetodologiaVigenteAsync();
#pragma warning restore CS0618
        if (legado is null)
        {
            return null;
        }

        return new MetodologiaFormularioDto
        {
            Codigo = legado.Version,
            Version = 0,
            Secciones = Array.Empty<SeccionFormularioDto>(),
            Catalogos = Array.Empty<CatalogoMatricesDto>(),
            Reglas = Array.Empty<ReglaCalculoMatricesDto>()
        };
    }
}
