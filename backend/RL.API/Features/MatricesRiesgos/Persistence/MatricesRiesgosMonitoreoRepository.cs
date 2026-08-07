using System.Text.Json;
using Oracle.ManagedDataAccess.Client;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Infrastructure.Database;

namespace RL.API.Features.MatricesRiesgos.Persistence;

public interface IMatricesRiesgosMonitoreoRepository
{
    Task<IReadOnlyList<SenalAlertaDto>> ListarAlertasAsync(long evaluacionId);
    Task<long> CrearAlertaAsync(SenalAlertaGuardarDto dto, long usuarioId, string? ip);
    Task<bool> CambiarEstadoAlertaAsync(long alertaId, string estado, long usuarioId, string? ip);
    Task<IReadOnlyList<AutomonitoreoDto>> ListarAutomonitoreoAsync(long evaluacionId);
    Task<long> RegistrarAutomonitoreoAsync(AutomonitoreoGuardarDto dto, long usuarioId, string? ip);
    Task<ResumenMatricesOperativoDto> ObtenerResumenOperativoAsync();
}

public sealed class MatricesRiesgosMonitoreoRepository : IMatricesRiesgosMonitoreoRepository
{
    private const string Modulo = "MatricesRiesgos";
    private readonly OracleDbContext _db;
    private readonly IAuditoriaRepository _auditoria;

    public MatricesRiesgosMonitoreoRepository(OracleDbContext db, IAuditoriaRepository auditoria)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _auditoria = auditoria ?? throw new ArgumentNullException(nameof(auditoria));
    }

    public async Task<IReadOnlyList<SenalAlertaDto>> ListarAlertasAsync(long evaluacionId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        const string sql = @"
            SELECT ALE_ID, ALE_EVALUACION_ID, ALE_CODIGO, ALE_INDICADOR, ALE_ESTADO, ALE_FECHA_DISPARO
              FROM RL_MR_SENALES_ALERTA
             WHERE ALE_EVALUACION_ID = :evaluacionId
             ORDER BY NVL(ALE_FECHA_DISPARO, DATE '1900-01-01') DESC, ALE_ID DESC";
        await using var cmd = Comando(sql, conn);
        cmd.Parameters.Add(new OracleParameter("evaluacionId", evaluacionId));
        var lista = new List<SenalAlertaDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(new SenalAlertaDto
            {
                AleId = reader.GetInt64(0),
                AleEvaluacionId = reader.GetInt64(1),
                AleCodigo = reader.GetString(2),
                AleIndicador = reader.GetString(3),
                AleEstado = reader.GetString(4),
                AleFechaDisparo = reader.IsDBNull(5) ? null : reader.GetDateTime(5)
            });
        }
        return lista;
    }

    public async Task<long> CrearAlertaAsync(SenalAlertaGuardarDto dto, long usuarioId, string? ip)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var tx = conn.BeginTransaction();
        try
        {
            await ExigirEvaluacionAsync(conn, tx, dto.AleEvaluacionId);
            string codigo = dto.AleCodigo.Trim().ToUpperInvariant();
            await using (var dup = Comando(@"
                SELECT COUNT(*) FROM RL_MR_SENALES_ALERTA
                 WHERE ALE_EVALUACION_ID = :evaluacionId AND ALE_CODIGO = :codigo", conn, tx))
            {
                dup.Parameters.Add(new OracleParameter("evaluacionId", dto.AleEvaluacionId));
                dup.Parameters.Add(new OracleParameter("codigo", codigo));
                if (Convert.ToInt32(await dup.ExecuteScalarAsync()) > 0)
                    throw new InvalidOperationException($"Ya existe la señal '{codigo}' para la evaluación.");
            }

            long id = await SiguienteAsync(conn, tx, "SEQ_RL_MR_SENALES");
            string estado = dto.AleEstado.Trim().ToUpperInvariant();
            const string sql = @"
                INSERT INTO RL_MR_SENALES_ALERTA
                    (ALE_ID, ALE_EVALUACION_ID, ALE_CODIGO, ALE_INDICADOR, ALE_ESTADO, ALE_FECHA_DISPARO)
                VALUES (:id, :evaluacionId, :codigo, :indicador, :estado,
                        CASE WHEN :estadoFecha = 'ACTIVO' THEN SYSDATE ELSE NULL END)";
            await using var cmd = Comando(sql, conn, tx);
            cmd.Parameters.Add(new OracleParameter("id", id));
            cmd.Parameters.Add(new OracleParameter("evaluacionId", dto.AleEvaluacionId));
            cmd.Parameters.Add(new OracleParameter("codigo", codigo));
            cmd.Parameters.Add(new OracleParameter("indicador", dto.AleIndicador.Trim()));
            cmd.Parameters.Add(new OracleParameter("estado", estado));
            cmd.Parameters.Add(new OracleParameter("estadoFecha", estado));
            await cmd.ExecuteNonQueryAsync();

            await _auditoria.RegistrarAsync(conn, tx, "RL_MR_SENALES_ALERTA", id.ToString(), "INSERT",
                null, JsonSerializer.Serialize(new { dto.AleEvaluacionId, Codigo = codigo, dto.AleIndicador, Estado = estado }),
                usuarioId, null, ip, Modulo);
            await tx.CommitAsync();
            return id;
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    public async Task<bool> CambiarEstadoAlertaAsync(long alertaId, string estado, long usuarioId, string? ip)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var tx = conn.BeginTransaction();
        try
        {
            string? anterior;
            await using (var cmdAnterior = Comando("SELECT ALE_ESTADO FROM RL_MR_SENALES_ALERTA WHERE ALE_ID = :id FOR UPDATE", conn, tx))
            {
                cmdAnterior.Parameters.Add(new OracleParameter("id", alertaId));
                anterior = (await cmdAnterior.ExecuteScalarAsync())?.ToString();
            }
            if (anterior is null) { await tx.RollbackAsync(); return false; }

            string nuevo = estado.Trim().ToUpperInvariant();
            const string sql = @"
                UPDATE RL_MR_SENALES_ALERTA
                   SET ALE_ESTADO = :estado,
                       ALE_FECHA_DISPARO = CASE
                           WHEN :estadoFecha = 'ACTIVO' THEN NVL(ALE_FECHA_DISPARO, SYSDATE)
                           ELSE ALE_FECHA_DISPARO
                       END
                 WHERE ALE_ID = :id";
            await using var cmd = Comando(sql, conn, tx);
            cmd.Parameters.Add(new OracleParameter("estado", nuevo));
            cmd.Parameters.Add(new OracleParameter("estadoFecha", nuevo));
            cmd.Parameters.Add(new OracleParameter("id", alertaId));
            await cmd.ExecuteNonQueryAsync();

            await _auditoria.RegistrarAsync(conn, tx, "RL_MR_SENALES_ALERTA", alertaId.ToString(), "UPDATE",
                JsonSerializer.Serialize(new { Estado = anterior }), JsonSerializer.Serialize(new { Estado = nuevo }),
                usuarioId, null, ip, Modulo);
            await tx.CommitAsync();
            return true;
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    public async Task<IReadOnlyList<AutomonitoreoDto>> ListarAutomonitoreoAsync(long evaluacionId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        const string sql = @"
            SELECT MON_ID, MON_EVALUACION_ID, MON_ESTADO_RIESGO, MON_ESTADO_CONTR,
                   MON_RESULTADO, MON_USR_ID, MON_FECHA
              FROM RL_MR_AUTOMONITOREO
             WHERE MON_EVALUACION_ID = :evaluacionId
             ORDER BY MON_FECHA DESC, MON_ID DESC";
        await using var cmd = Comando(sql, conn);
        cmd.Parameters.Add(new OracleParameter("evaluacionId", evaluacionId));
        var lista = new List<AutomonitoreoDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(new AutomonitoreoDto
            {
                MonId = reader.GetInt64(0), MonEvaluacionId = reader.GetInt64(1),
                MonEstadoRiesgo = reader.GetString(2), MonEstadoContr = reader.GetString(3),
                MonResultado = reader.GetString(4), MonUsrId = reader.GetInt64(5), MonFecha = reader.GetDateTime(6)
            });
        }
        return lista;
    }

    public async Task<long> RegistrarAutomonitoreoAsync(AutomonitoreoGuardarDto dto, long usuarioId, string? ip)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var tx = conn.BeginTransaction();
        try
        {
            await ExigirEvaluacionAsync(conn, tx, dto.MonEvaluacionId);
            await ExigirUsuarioAsync(conn, tx, usuarioId);
            long id = await SiguienteAsync(conn, tx, "SEQ_RL_MR_AUTOMONITOREO");
            const string sql = @"
                INSERT INTO RL_MR_AUTOMONITOREO
                    (MON_ID, MON_EVALUACION_ID, MON_ESTADO_RIESGO, MON_ESTADO_CONTR, MON_RESULTADO, MON_USR_ID, MON_FECHA)
                VALUES (:id, :evaluacionId, :estadoRiesgo, :estadoControl, :resultado, :usuarioId, SYSDATE)";
            await using var cmd = Comando(sql, conn, tx);
            cmd.Parameters.Add(new OracleParameter("id", id));
            cmd.Parameters.Add(new OracleParameter("evaluacionId", dto.MonEvaluacionId));
            cmd.Parameters.Add(new OracleParameter("estadoRiesgo", dto.MonEstadoRiesgo.Trim().ToUpperInvariant()));
            cmd.Parameters.Add(new OracleParameter("estadoControl", dto.MonEstadoContr.Trim().ToUpperInvariant()));
            cmd.Parameters.Add(new OracleParameter("resultado", dto.MonResultado.Trim()));
            cmd.Parameters.Add(new OracleParameter("usuarioId", usuarioId));
            await cmd.ExecuteNonQueryAsync();

            await _auditoria.RegistrarAsync(conn, tx, "RL_MR_AUTOMONITOREO", id.ToString(), "INSERT",
                null, JsonSerializer.Serialize(new { dto.MonEvaluacionId, dto.MonEstadoRiesgo, dto.MonEstadoContr, dto.MonResultado }),
                usuarioId, null, ip, Modulo);
            await tx.CommitAsync();
            return id;
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    public async Task<ResumenMatricesOperativoDto> ObtenerResumenOperativoAsync()
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        const string sql = @"
            SELECT
              (SELECT COUNT(*) FROM RL_MR_RIESGOS WHERE RIE_ACTIVO = 1) RIESGOS_ACTIVOS,
              (SELECT COUNT(*) FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_ACTIVO = 1) EVALUACIONES_ACTIVAS,
              (SELECT COUNT(*) FROM RL_MR_PROYECCIONES_EVALUACION WHERE PROY_ESTADO_EVALUACION = 'APROBADA') EVALUACIONES_APROBADAS,
              (SELECT COUNT(*) FROM RL_MR_PROYECCIONES_EVALUACION WHERE UPPER(PROY_NIVEL_RESIDUAL) IN ('ALTO','CRITICO')) ALTO_CRITICO,
              (SELECT COUNT(*) FROM RL_MR_SENALES_ALERTA WHERE ALE_ESTADO = 'ACTIVO') ALERTAS_ACTIVAS,
              (SELECT COUNT(*) FROM RL_MR_PLANES WHERE UPPER(PLA_ESTADO) NOT IN ('CERRADO','COMPLETADO','FINALIZADO')) PLANES_ABIERTOS,
              (SELECT COUNT(*) FROM RL_MR_ACTIVIDADES
                WHERE ACT_FECHA_FIN < TRUNC(SYSDATE) AND UPPER(ACT_ESTADO) NOT IN ('CERRADA','COMPLETADA','FINALIZADA')) ACTIVIDADES_VENCIDAS,
              (SELECT COUNT(*) FROM RL_MR_AUTOMONITOREO WHERE MON_FECHA >= SYSDATE - 30) MONITOREOS_30
            FROM DUAL";
        await using var cmd = Comando(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) throw new InvalidOperationException("Oracle no devolvió el resumen operativo.");
        return new ResumenMatricesOperativoDto
        {
            FechaGeneracion = DateTime.Now,
            RiesgosActivos = reader.GetInt32(0),
            EvaluacionesActivas = reader.GetInt32(1),
            EvaluacionesAprobadas = reader.GetInt32(2),
            RiesgosAltoCritico = reader.GetInt32(3),
            AlertasActivas = reader.GetInt32(4),
            PlanesAbiertos = reader.GetInt32(5),
            ActividadesVencidas = reader.GetInt32(6),
            AutomonitoreosUltimos30Dias = reader.GetInt32(7)
        };
    }

    private static async Task ExigirEvaluacionAsync(OracleConnection conn, OracleTransaction tx, long id)
    {
        await using var cmd = Comando("SELECT COUNT(*) FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_ID = :id AND EVA_ACTIVO = 1", conn, tx);
        cmd.Parameters.Add(new OracleParameter("id", id));
        if (Convert.ToInt32(await cmd.ExecuteScalarAsync()) != 1) throw new InvalidOperationException("La evaluación activa no existe.");
    }

    private static async Task ExigirUsuarioAsync(OracleConnection conn, OracleTransaction tx, long id)
    {
        await using var cmd = Comando("SELECT COUNT(*) FROM RL_USUARIOS WHERE USR_ID = :id AND USR_ACTIVO = 1", conn, tx);
        cmd.Parameters.Add(new OracleParameter("id", id));
        if (Convert.ToInt32(await cmd.ExecuteScalarAsync()) != 1) throw new InvalidOperationException("El usuario institucional no existe o está inactivo.");
    }

    private static async Task<long> SiguienteAsync(OracleConnection conn, OracleTransaction tx, string secuencia)
    {
        await using var cmd = Comando($"SELECT {secuencia}.NEXTVAL FROM DUAL", conn, tx);
        return Convert.ToInt64(await cmd.ExecuteScalarAsync());
    }

    private static OracleCommand Comando(string sql, OracleConnection conn, OracleTransaction? tx = null) =>
        new(sql, conn) { BindByName = true, Transaction = tx };
}
