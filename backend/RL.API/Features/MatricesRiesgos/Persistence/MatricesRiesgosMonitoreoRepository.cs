using System.Text.Json;
using Oracle.ManagedDataAccess.Client;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;
using RL.API.Infrastructure.Database;

namespace RL.API.Features.MatricesRiesgos.Persistence;

public interface IMatricesRiesgosMonitoreoRepository
{
    Task<IReadOnlyList<SenalAlertaDto>> ListarAlertasAsync(long evaluacionId);
    Task<long> CrearAlertaAsync(SenalAlertaGuardarDto dto, long usuarioId, string? ip);
    Task<bool> CambiarEstadoAlertaAsync(long alertaId, string estado, long usuarioId, string? ip);
    Task<IReadOnlyList<AutomonitoreoDto>> ListarAutomonitoreoAsync(long evaluacionId);
    Task<MatrizBloque6Dto> ObtenerBloque6Async(long evaluacionId, long usuarioId);
    Task<bool> ActualizarObservacionAsync(long evaluacionId, bool esArea, string? texto, long usuarioId, string? ip);
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
                AleIndicador = TextoVisibleUtf8Normalizer.Normalizar(reader.GetString(3)),
                AleEstado = TextoVisibleUtf8Normalizer.Normalizar(reader.GetString(4)),
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
                   MON_RESULTADO, MON_USR_ID, MON_FECHA,
                   MON_OBSERVACIONES_AREA, MON_OBSERVACIONES_UGR
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
                MonEstadoRiesgo = TextoVisibleUtf8Normalizer.Normalizar(reader.GetString(2)),
                MonEstadoContr = TextoVisibleUtf8Normalizer.Normalizar(reader.GetString(3)),
                MonResultado = TextoVisibleUtf8Normalizer.Normalizar(reader.GetString(4)),
                MonUsrId = reader.GetInt64(5), MonFecha = reader.GetDateTime(6),
                MonObservacionesArea = reader.IsDBNull(7) ? null : TextoVisibleUtf8Normalizer.Normalizar(reader.GetString(7)),
                MonObservacionesUgr = reader.IsDBNull(8) ? null : TextoVisibleUtf8Normalizer.Normalizar(reader.GetString(8))
            });
        }
        return lista;
    }

    public async Task<MatrizBloque6Dto> ObtenerBloque6Async(long evaluacionId, long usuarioId)
    {
        var alertas = await ListarAlertasAsync(evaluacionId);
        var automonitoreos = await ListarAutomonitoreoAsync(evaluacionId);
        var ultimo = automonitoreos.FirstOrDefault();
        var controles = new Dictionary<long, (string tipo, string descripcion, string? estado, decimal? efectividad, List<EvidenciaMatrizDto> evidencias)>();
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        const string sql = @"
            SELECT c.CON_ID, c.CON_TIPO, c.CON_DESCRIPCION, c.CON_ESTADO_MONITOREO,
                   c.CON_EFECTIVIDAD_MONITOREO, e.EVI_ID, e.EVI_NOMBRE_ARCHIVO
              FROM RL_MR_CONTROLES_RIESGO c
              LEFT JOIN RL_MR_EVIDENCIAS_VINCULOS v
                ON v.EVV_TIPO_ENTIDAD = 'CONTROL' AND v.EVV_ENTIDAD_ID = c.CON_ID
              LEFT JOIN RL_MR_EVIDENCIAS e ON e.EVI_ID = v.EVV_EVIDENCIA_ID
             WHERE c.CON_EVALUACION_ID = :evaluacionId
             ORDER BY c.CON_TIPO, c.CON_ID, v.EVV_ID";
        await using (var cmd = Comando(sql, conn))
        {
            cmd.Parameters.Add(new OracleParameter("evaluacionId", evaluacionId));
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                long id = reader.GetInt64(0);
                if (!controles.TryGetValue(id, out var control))
                {
                    control = (TextoVisibleUtf8Normalizer.Normalizar(reader.GetString(1)),
                        TextoVisibleUtf8Normalizer.Normalizar(reader.GetString(2)),
                        reader.IsDBNull(3) ? null : TextoVisibleUtf8Normalizer.Normalizar(reader.GetString(3)),
                        reader.IsDBNull(4) ? null : reader.GetDecimal(4), new List<EvidenciaMatrizDto>());
                    controles.Add(id, control);
                }
                if (!reader.IsDBNull(5)) control.evidencias.Add(new EvidenciaMatrizDto(reader.GetInt64(5), TextoVisibleUtf8Normalizer.Normalizar(reader.GetString(6))));
            }
        }

        var capacidades = new HashSet<string>(StringComparer.Ordinal);
        await using (var cmd = Comando(@"SELECT UCP_CAPACIDAD FROM RL_USUARIO_CAPACIDADES WHERE UCP_USR_ID=:usuarioId AND UCP_ACTIVO=1", conn))
        {
            cmd.Parameters.Add(new OracleParameter("usuarioId", usuarioId));
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync()) capacidades.Add(reader.GetString(0));
        }
        return new MatrizBloque6Dto
        {
            SenalesAlerta = alertas,
            EstadoRiesgo = ultimo?.MonEstadoRiesgo,
            ObservacionesArea = ultimo?.MonObservacionesArea,
            ObservacionesUgr = ultimo?.MonObservacionesUgr,
            PuedeEditarObservacionesArea = capacidades.Contains("MATRICES_RIESGO_OBSERVACIONES_AREA_EDITAR"),
            PuedeEditarObservacionesUgr = capacidades.Contains("MATRICES_RIESGO_OBSERVACIONES_UGR_EDITAR"),
            Controles = controles.Select(x => new ControlMonitoreoMatrizDto
            {
                ControlId = x.Key, Tipo = x.Value.tipo, Descripcion = x.Value.descripcion,
                EstadoMonitoreo = x.Value.estado, EfectividadMonitoreo = x.Value.efectividad,
                Evidencias = x.Value.evidencias.DistinctBy(evidencia => evidencia.Id).ToArray()
            }).ToArray()
        };
    }

    public async Task<bool> ActualizarObservacionAsync(long evaluacionId, bool esArea, string? texto, long usuarioId, string? ip)
    {
        string columna = esArea ? "MON_OBSERVACIONES_AREA" : "MON_OBSERVACIONES_UGR";
        string capacidad = esArea ? "MATRICES_RIESGO_OBSERVACIONES_AREA_EDITAR" : "MATRICES_RIESGO_OBSERVACIONES_UGR_EDITAR";
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var tx = conn.BeginTransaction();
        try
        {
            await using (var permiso = Comando(@"SELECT COUNT(*) FROM RL_USUARIO_CAPACIDADES WHERE UCP_USR_ID=:usuarioId AND UCP_CAPACIDAD=:capacidad AND UCP_ACTIVO=1", conn, tx))
            {
                permiso.Parameters.Add(new OracleParameter("usuarioId", usuarioId));
                permiso.Parameters.Add(new OracleParameter("capacidad", capacidad));
                if (Convert.ToInt32(await permiso.ExecuteScalarAsync()) != 1) throw new UnauthorizedAccessException("El usuario no posee la capacidad requerida.");
            }

            long monitoreoId;
            string? anterior;
            await using (var anteriorCmd = Comando($@"SELECT MON_ID, {columna} FROM RL_MR_AUTOMONITOREO
                WHERE MON_ID=(SELECT MON_ID FROM (SELECT MON_ID FROM RL_MR_AUTOMONITOREO
                    WHERE MON_EVALUACION_ID=:evaluacionId ORDER BY MON_FECHA DESC, MON_ID DESC) WHERE ROWNUM=1) FOR UPDATE", conn, tx))
            {
                anteriorCmd.Parameters.Add(new OracleParameter("evaluacionId", evaluacionId));
                await using var reader = await anteriorCmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) { await tx.RollbackAsync(); return false; }
                monitoreoId = reader.GetInt64(0);
                anterior = reader.IsDBNull(1) ? null : reader.GetString(1);
            }
            await using (var update = Comando($"UPDATE RL_MR_AUTOMONITOREO SET {columna}=:texto WHERE MON_ID=:id", conn, tx))
            {
                update.Parameters.Add(new OracleParameter("texto", string.IsNullOrWhiteSpace(texto) ? DBNull.Value : texto.Trim()));
                update.Parameters.Add(new OracleParameter("id", monitoreoId));
                await update.ExecuteNonQueryAsync();
            }
            await _auditoria.RegistrarAsync(conn, tx, "RL_MR_AUTOMONITOREO", monitoreoId.ToString(), "UPDATE",
                JsonSerializer.Serialize(new { Campo = columna, Observacion = anterior }), JsonSerializer.Serialize(new { Campo = columna, Observacion = texto?.Trim() }),
                usuarioId, null, ip, Modulo);
            await tx.CommitAsync();
            return true;
        }
        catch { await tx.RollbackAsync(); throw; }
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
              (SELECT COUNT(*) FROM RL_MR_PLANES WHERE UPPER(PLA_ESTADO) IN ('PENDIENTE','EN_PROCESO','VENCIDO')) PLANES_ABIERTOS,
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
