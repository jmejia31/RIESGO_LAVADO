using Oracle.ManagedDataAccess.Client;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Infrastructure.Database;

namespace RL.API.Features.MatricesRiesgos.Persistence;

/// <summary>
/// Fachada transitoria de persistencia durante la Fase 1.2.
/// Delega el CRUD dinámico al repositorio principal y mantiene operativo el flujo
/// de evidencias mientras se termina de separar la persistencia por responsabilidades.
/// </summary>
public sealed class MatricesRiesgosRepositoryFacade : IMatricesRiesgosRepository
{
    private readonly OracleDbContext _db;
    private readonly MatricesRiesgosRepository _inner;

    public MatricesRiesgosRepositoryFacade(OracleDbContext db, MatricesRiesgosRepository inner)
    {
        _db = db;
        _inner = inner;
    }

    public Task<VersionFormularioDto?> ObtenerVersionVigenteFormularioAsync(string familiaCodigo) =>
        _inner.ObtenerVersionVigenteFormularioAsync(familiaCodigo);

    public Task<VersionFormularioDto?> ObtenerVersionFormularioAsync(long versionId) =>
        _inner.ObtenerVersionFormularioAsync(versionId);

    public Task<long> CrearBorradorFormularioAsync(long familiaId, string codigoFormulario, string jsonConfig, long usuarioId) =>
        _inner.CrearBorradorFormularioAsync(familiaId, codigoFormulario, jsonConfig, usuarioId);

    public Task<long> ClonarVersionFormularioAsync(long versionOrigenId, long usuarioId) =>
        _inner.ClonarVersionFormularioAsync(versionOrigenId, usuarioId);

    public Task<bool> ActualizarBorradorFormularioAsync(long versionId, string jsonConfig, string hash, long usuarioId) =>
        _inner.ActualizarBorradorFormularioAsync(versionId, jsonConfig, hash, usuarioId);

    public Task<bool> PublicarVersionFormularioAsync(long versionId, string hash, long usuarioId) =>
        _inner.PublicarVersionFormularioAsync(versionId, hash, usuarioId);

    public Task<bool> CambiarEstadoVigenciaFormularioAsync(long versionId, bool vigente, long usuarioId) =>
        _inner.CambiarEstadoVigenciaFormularioAsync(versionId, vigente, usuarioId);

    public Task<List<VersionFormularioDto>> ListarHistorialVersionesFormularioAsync(string familiaCodigo) =>
        _inner.ListarHistorialVersionesFormularioAsync(familiaCodigo);

    public Task<EvaluacionRiesgoDto?> ObtenerEvaluacionAsync(long evaId) =>
        _inner.ObtenerEvaluacionAsync(evaId);

    public Task<List<EvaluacionRiesgoDto>> ListarEvaluacionesPaginadasAsync(ConsultaEvaluacionPaginadaDto filtro) =>
        _inner.ListarEvaluacionesPaginadasAsync(filtro);

    public Task<long> CrearEvaluacionAsync(EvaluacionRiesgoDto dto, long usuarioId, string? ip) =>
        _inner.CrearEvaluacionAsync(dto, usuarioId, ip);

    public Task<bool> ActualizarEvaluacionAsync(EvaluacionRiesgoDto dto, long usuarioId, string? ip) =>
        _inner.ActualizarEvaluacionAsync(dto, usuarioId, ip);

    public Task<bool> TransicionarEstadoEvaluacionAsync(long evaId, string nuevoEstado, string? motivo, long usuarioId, string? ip) =>
        _inner.TransicionarEstadoEvaluacionAsync(evaId, nuevoEstado, motivo, usuarioId, ip);

    public Task<List<RevisionEvaluacionDto>> ObtenerRevisionesEvaluacionAsync(long evaId) =>
        _inner.ObtenerRevisionesEvaluacionAsync(evaId);

    public Task<long> RegistrarEvidenciaFisicaAsync(EvidenciaRegistroDto dto, long usuarioId) =>
        _inner.RegistrarEvidenciaFisicaAsync(dto, usuarioId);

    public Task<EvidenciaDto?> ObtenerEvidenciaFisicaAsync(long evidenciaId) =>
        _inner.ObtenerEvidenciaFisicaAsync(evidenciaId);

    public Task<bool> VincularEvidenciaRiesgoAsync(AsociarEvidenciaRiesgoDto dto, long usuarioId, string? ip) =>
        VincularAsync(
            "RL_MR_EVI_RIESGO",
            "EVR_RIESGO_ID",
            "EVR_EVIDENCIA_ID",
            dto.EvrRiesgoId,
            dto.EvrEvidenciaId,
            "SELECT EVA_ID FROM (SELECT EVA_ID FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_RIESGO_ID = :entidadId AND EVA_ACTIVO = 1 ORDER BY EVA_FECHA_REGISTRO DESC, EVA_ID DESC) WHERE ROWNUM = 1",
            usuarioId,
            ip);

    public Task<bool> VincularEvidenciaEvaluacionAsync(AsociarEvidenciaEvaluacionDto dto, long usuarioId, string? ip) =>
        VincularAsync(
            "RL_MR_EVI_EVALUACION",
            "EVE_EVALUACION_ID",
            "EVE_EVIDENCIA_ID",
            dto.EveEvaluacionId,
            dto.EveEvidenciaId,
            "SELECT EVA_ID FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_ID = :entidadId",
            usuarioId,
            ip);

    public Task<bool> VincularEvidenciaControlAsync(AsociarEvidenciaControlDto dto, long usuarioId, string? ip) =>
        VincularAsync(
            "RL_MR_EVI_CONTROL",
            "EVC_CONTROL_ID",
            "EVC_EVIDENCIA_ID",
            dto.EvcControlId,
            dto.EvcEvidenciaId,
            "SELECT CON_EVALUACION_ID FROM RL_MR_CONTROLES_RIESGO WHERE CON_ID = :entidadId",
            usuarioId,
            ip);

    public Task<bool> VincularEvidenciaPlanAsync(AsociarEvidenciaPlanDto dto, long usuarioId, string? ip) =>
        VincularAsync(
            "RL_MR_EVI_PLAN",
            "EVP_PLAN_ID",
            "EVP_EVIDENCIA_ID",
            dto.EvpPlanId,
            dto.EvpEvidenciaId,
            "SELECT PLA_EVALUACION_ID FROM RL_MR_PLANES WHERE PLA_ID = :entidadId",
            usuarioId,
            ip);

    public Task<bool> VincularEvidenciaActividadAsync(AsociarEvidenciaActividadDto dto, long usuarioId, string? ip) =>
        VincularAsync(
            "RL_MR_EVI_ACTIVIDAD",
            "EVA_ACTIVIDAD_ID",
            "EVA_EVIDENCIA_ID",
            dto.EvaActividadId,
            dto.EvaEvidenciaId,
            "SELECT p.PLA_EVALUACION_ID FROM RL_MR_ACTIVIDADES a JOIN RL_MR_PLANES p ON p.PLA_ID = a.ACT_PLAN_ID WHERE a.ACT_ID = :entidadId",
            usuarioId,
            ip);

    public Task<bool> VincularEvidenciaAlertaAsync(AsociarEvidenciaAlertaDto dto, long usuarioId, string? ip) =>
        VincularAsync(
            "RL_MR_EVI_ALERTA",
            "EVA_ALERTA_ID",
            "EVA_EVIDENCIA_ID",
            dto.EvaAlertaId,
            dto.EvaEvidenciaId,
            "SELECT ALE_EVALUACION_ID FROM RL_MR_SENALES_ALERTA WHERE ALE_ID = :entidadId",
            usuarioId,
            ip);

    public Task<bool> VincularEvidenciaAutomonitoreoAsync(AsociarEvidenciaAutomonitoreoDto dto, long usuarioId, string? ip) =>
        VincularAsync(
            "RL_MR_EVI_AUTOMONITOREO",
            "EVM_MONITOREO_ID",
            "EVM_EVIDENCIA_ID",
            dto.EvmMonitoreoId,
            dto.EvmEvidenciaId,
            "SELECT MON_EVALUACION_ID FROM RL_MR_AUTOMONITOREO WHERE MON_ID = :entidadId",
            usuarioId,
            ip);

    public Task<bool> VincularEvidenciaRevisionAsync(AsociarEvidenciaRevisionDto dto, long usuarioId, string? ip) =>
        VincularAsync(
            "RL_MR_EVI_REVISION",
            "EVV_REVISION_ID",
            "EVV_EVIDENCIA_ID",
            dto.EvvRevisionId,
            dto.EvvEvidenciaId,
            "SELECT REV_EVALUACION_ID FROM RL_MR_REVISIONES_EVALUACION WHERE REV_ID = :entidadId",
            usuarioId,
            ip);

    public Task<bool> VincularEvidenciaAprobacionAsync(AsociarEvidenciaAprobacionDto dto, long usuarioId, string? ip) =>
        VincularAsync(
            "RL_MR_EVI_APROBACION",
            "EVAP_APROBACION_ID",
            "EVAP_EVIDENCIA_ID",
            dto.EvapAprobacionId,
            dto.EvapEvidenciaId,
            null,
            usuarioId,
            ip);

    public Task<ResultadoEliminacionEvidencia> EliminarEvidenciaSeguraAsync(
        long evidenciaId,
        Func<Task<bool>> eliminarArchivoFisico,
        long usuarioId,
        string? ip) =>
        _inner.EliminarEvidenciaSeguraAsync(evidenciaId, eliminarArchivoFisico, usuarioId, ip);

    public Task<List<Dictionary<string, object>>> ObtenerConsolidadoMatricesAsync() =>
        _inner.ObtenerConsolidadoMatricesAsync();

    public Task<MetodologiaMatricesDto?> ObtenerMetodologiaVigenteAsync() =>
        _inner.ObtenerMetodologiaVigenteAsync();

    private async Task<bool> VincularAsync(
        string tablaPuente,
        string columnaEntidad,
        string columnaEvidencia,
        long entidadId,
        long evidenciaId,
        string? sqlResolverEvaluacion,
        long usuarioId,
        string? ip)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var trans = conn.BeginTransaction();

        try
        {
            long? evaluacionId = null;
            if (!string.IsNullOrWhiteSpace(sqlResolverEvaluacion))
            {
                await using var cmdResolver = new OracleCommand(sqlResolverEvaluacion, conn);
                cmdResolver.Parameters.Add(new OracleParameter("entidadId", entidadId));
                object? resultado = await cmdResolver.ExecuteScalarAsync();
                if (resultado is not null)
                {
                    evaluacionId = Convert.ToInt64(resultado);
                }
            }

            string sqlInsert = $"INSERT INTO {tablaPuente} ({columnaEntidad}, {columnaEvidencia}) VALUES (:entidadId, :evidenciaId)";
            await using var cmdInsert = new OracleCommand(sqlInsert, conn);
            cmdInsert.Parameters.Add(new OracleParameter("entidadId", entidadId));
            cmdInsert.Parameters.Add(new OracleParameter("evidenciaId", evidenciaId));
            await cmdInsert.ExecuteNonQueryAsync();

            if (evaluacionId.HasValue)
            {
                const string sqlAuditoria = @"
                    INSERT INTO RL_MR_AUDITORIA (
                        AUD_ID,
                        AUD_EVALUACION_ID,
                        AUD_CAMPO_CLAVE,
                        AUD_VALOR_ANT,
                        AUD_VALOR_NVO,
                        AUD_IP,
                        AUD_USR_ID,
                        AUD_FECHA
                    ) VALUES (
                        SEQ_RL_MR_AUDITORIA.NEXTVAL,
                        :evaluacionId,
                        'evidencia',
                        NULL,
                        :valorNuevo,
                        :ip,
                        :usuarioId,
                        SYSDATE
                    )";

                await using var cmdAuditoria = new OracleCommand(sqlAuditoria, conn);
                cmdAuditoria.Parameters.Add(new OracleParameter("evaluacionId", evaluacionId.Value));
                cmdAuditoria.Parameters.Add(new OracleParameter("valorNuevo", OracleDbType.Clob)
                {
                    Value = $"{tablaPuente}:{entidadId}:{evidenciaId}"
                });
                cmdAuditoria.Parameters.Add(new OracleParameter("ip", ip ?? (object)DBNull.Value));
                cmdAuditoria.Parameters.Add(new OracleParameter("usuarioId", usuarioId));
                await cmdAuditoria.ExecuteNonQueryAsync();
            }

            await trans.CommitAsync();
            return true;
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }
}
