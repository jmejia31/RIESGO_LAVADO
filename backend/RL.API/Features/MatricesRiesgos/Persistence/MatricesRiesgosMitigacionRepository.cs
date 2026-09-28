using System.Text.Json;
using System.Data;
using System.Globalization;
using Oracle.ManagedDataAccess.Client;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;
using RL.API.Infrastructure.Database;

namespace RL.API.Features.MatricesRiesgos.Persistence;

public interface IMatricesRiesgosMitigacionRepository
{
    Task<IReadOnlyList<ControlRiesgoDto>> ListarControlesAsync(long evaluacionId);
    Task<ControlRiesgoDto?> ObtenerControlAsync(long controlId);
    Task<long> CrearControlAsync(ControlRiesgoGuardarDto dto, long usuarioId, string? ip);
    Task<bool> ActualizarControlAsync(long controlId, ControlRiesgoGuardarDto dto, long usuarioId, string? ip);
    Task<long> CrearControlGobernadoAtomicoAsync(ControlRiesgoGuardarDto dto, int expectedEvaVersionRow, string calculatedJson, long usuarioId, string? ip);
    Task<bool> ActualizarControlGobernadoAtomicoAsync(long controlId, ControlRiesgoGuardarDto dto, int expectedEvaVersionRow, string calculatedJson, long usuarioId, string? ip);
    Task<IReadOnlyList<EvaluacionControlDto>> ListarEvaluacionesControlAsync(long controlId);
    Task<long> RegistrarEvaluacionControlAsync(long controlId, EvaluacionControlGuardarDto dto, long usuarioId, string? ip);
    Task<IReadOnlyList<PlanMitigacionDto>> ListarPlanesAsync(long evaluacionId);
    Task<long> CrearPlanAsync(PlanMitigacionGuardarDto dto, long usuarioId, string? ip);
    Task<bool> ActualizarPlanAsync(long planId, PlanMitigacionGuardarDto dto, long usuarioId, string? ip);
    Task<IReadOnlyList<ActividadPlanDto>> ListarActividadesAsync(long planId);
    Task<long> CrearActividadAsync(ActividadPlanGuardarDto dto, long usuarioId, string? ip);
    Task<bool> ActualizarActividadAsync(long actividadId, ActividadPlanGuardarDto dto, long usuarioId, string? ip);
}

public sealed class MatricesRiesgosMitigacionRepository : IMatricesRiesgosMitigacionRepository
{
    private const string Modulo = "MatricesRiesgos";
    private readonly OracleDbContext _db;
    private readonly IAuditoriaRepository _auditoria;

    public MatricesRiesgosMitigacionRepository(OracleDbContext db, IAuditoriaRepository auditoria)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _auditoria = auditoria ?? throw new ArgumentNullException(nameof(auditoria));
    }

    public async Task<IReadOnlyList<ControlRiesgoDto>> ListarControlesAsync(long evaluacionId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        const string sql = @"
            SELECT CON_ID, CON_EVALUACION_ID, CON_TIPO, CON_DESCRIPCION, CON_AUTOMATIZACION, CON_ESTADO
              FROM RL_MR_CONTROLES_RIESGO
             WHERE CON_EVALUACION_ID = :evaluacionId
             ORDER BY CON_ID";
        await using var cmd = Comando(sql, conn);
        cmd.Parameters.Add(new OracleParameter("evaluacionId", evaluacionId));
        var lista = new List<ControlRiesgoDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(new ControlRiesgoDto
            {
                ConId = reader.GetInt64(0),
                ConEvaluacionId = reader.GetInt64(1),
                ConTipo = reader.GetString(2),
                ConDescripcion = TextoVisibleUtf8Normalizer.Normalizar(reader.GetString(3)),
                ConAutomatizacion = reader.GetString(4),
                ConEstado = reader.GetString(5)
            });
        }
        return lista;
    }

    public async Task<ControlRiesgoDto?> ObtenerControlAsync(long controlId)
    {
        await using var connection = _db.CreateConnection();
        await connection.OpenAsync();
        const string sql = @"SELECT CON_ID,CON_EVALUACION_ID,CON_TIPO,CON_DESCRIPCION,CON_AUTOMATIZACION,CON_ESTADO
                               FROM RL_MR_CONTROLES_RIESGO WHERE CON_ID=:id";
        await using var command = Comando(sql, connection);
        command.Parameters.Add(new OracleParameter("id", controlId));
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return new ControlRiesgoDto
        {
            ConId=reader.GetInt64(0), ConEvaluacionId=reader.GetInt64(1), ConTipo=reader.GetString(2),
            ConDescripcion=TextoVisibleUtf8Normalizer.Normalizar(reader.GetString(3)), ConAutomatizacion=reader.GetString(4), ConEstado=reader.GetString(5)
        };
    }

    public async Task<long> CrearControlAsync(ControlRiesgoGuardarDto dto, long usuarioId, string? ip)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var tx = conn.BeginTransaction();
        try
        {
            await ExigirEvaluacionAsync(conn, tx, dto.ConEvaluacionId);
            long id = await SiguienteAsync(conn, tx, "SEQ_RL_MR_CONTROLES");
            const string sql = @"
                INSERT INTO RL_MR_CONTROLES_RIESGO
                    (CON_ID, CON_EVALUACION_ID, CON_TIPO, CON_DESCRIPCION, CON_AUTOMATIZACION, CON_ESTADO)
                VALUES (:id, :evaluacionId, :tipo, :descripcion, :automatizacion, :estado)";
            await using var cmd = Comando(sql, conn, tx);
            cmd.Parameters.Add(new OracleParameter("id", id));
            cmd.Parameters.Add(new OracleParameter("evaluacionId", dto.ConEvaluacionId));
            cmd.Parameters.Add(new OracleParameter("tipo", dto.ConTipo.Trim().ToUpperInvariant()));
            cmd.Parameters.Add(new OracleParameter("descripcion", dto.ConDescripcion.Trim()));
            cmd.Parameters.Add(new OracleParameter("automatizacion", dto.ConAutomatizacion.Trim().ToUpperInvariant()));
            cmd.Parameters.Add(new OracleParameter("estado", dto.ConEstado.Trim().ToUpperInvariant()));
            await cmd.ExecuteNonQueryAsync();
            await AuditarAsync(conn, tx, "RL_MR_CONTROLES_RIESGO", id, "INSERT", dto, usuarioId, ip);
            await tx.CommitAsync();
            return id;
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    public async Task<bool> ActualizarControlAsync(long controlId, ControlRiesgoGuardarDto dto, long usuarioId, string? ip)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var tx = conn.BeginTransaction();
        try
        {
            await ExigirEvaluacionAsync(conn, tx, dto.ConEvaluacionId);
            const string sql = @"
                UPDATE RL_MR_CONTROLES_RIESGO
                   SET CON_EVALUACION_ID = :evaluacionId,
                       CON_TIPO = :tipo,
                       CON_DESCRIPCION = :descripcion,
                       CON_AUTOMATIZACION = :automatizacion,
                       CON_ESTADO = :estado
                 WHERE CON_ID = :id";
            await using var cmd = Comando(sql, conn, tx);
            cmd.Parameters.Add(new OracleParameter("evaluacionId", dto.ConEvaluacionId));
            cmd.Parameters.Add(new OracleParameter("tipo", dto.ConTipo.Trim().ToUpperInvariant()));
            cmd.Parameters.Add(new OracleParameter("descripcion", dto.ConDescripcion.Trim()));
            cmd.Parameters.Add(new OracleParameter("automatizacion", dto.ConAutomatizacion.Trim().ToUpperInvariant()));
            cmd.Parameters.Add(new OracleParameter("estado", dto.ConEstado.Trim().ToUpperInvariant()));
            cmd.Parameters.Add(new OracleParameter("id", controlId));
            if (await cmd.ExecuteNonQueryAsync() != 1) { await tx.RollbackAsync(); return false; }
            await AuditarAsync(conn, tx, "RL_MR_CONTROLES_RIESGO", controlId, "UPDATE", dto, usuarioId, ip);
            await tx.CommitAsync();
            return true;
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    public async Task<long> CrearControlGobernadoAtomicoAsync(ControlRiesgoGuardarDto dto, int expectedEvaVersionRow, string calculatedJson, long usuarioId, string? ip)
    {
        await using var connection = _db.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = connection.BeginTransaction();
        try
        {
            await LockGovernedDraftEvaluationAsync(connection, transaction, dto.ConEvaluacionId, expectedEvaVersionRow);
            long id = await SiguienteAsync(connection, transaction, "SEQ_RL_MR_CONTROLES");
            const string insert = @"INSERT INTO RL_MR_CONTROLES_RIESGO
                (CON_ID,CON_EVALUACION_ID,CON_TIPO,CON_DESCRIPCION,CON_AUTOMATIZACION,CON_ESTADO)
                VALUES(:id,:evaluationId,:type,:description,:automation,:state)";
            await using (var command = Comando(insert, connection, transaction))
            {
                command.Parameters.Add(new OracleParameter("id", id));
                AddControlParameters(command, dto);
                await command.ExecuteNonQueryAsync();
            }
            await UpdateGovernedCalculationAsync(connection, transaction, dto.ConEvaluacionId, expectedEvaVersionRow, calculatedJson);
            await AuditarAsync(connection, transaction, "RL_MR_CONTROLES_RIESGO", id, "INSERT", dto, usuarioId, ip);
            await AuditarAsync(connection, transaction, "RL_MR_EVALUACIONES_RIESGO", dto.ConEvaluacionId, "UPDATE", new { calculatedJson }, usuarioId, ip);
            await transaction.CommitAsync();
            return id;
        }
        catch { await transaction.RollbackAsync(); throw; }
    }

    public async Task<bool> ActualizarControlGobernadoAtomicoAsync(long controlId, ControlRiesgoGuardarDto dto, int expectedEvaVersionRow, string calculatedJson, long usuarioId, string? ip)
    {
        await using var connection = _db.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = connection.BeginTransaction();
        try
        {
            await LockGovernedDraftEvaluationAsync(connection, transaction, dto.ConEvaluacionId, expectedEvaVersionRow);
            const string selectControl = "SELECT CON_EVALUACION_ID FROM RL_MR_CONTROLES_RIESGO WHERE CON_ID=:id FOR UPDATE";
            long existingEvaluationId;
            await using (var read = Comando(selectControl, connection, transaction))
            {
                read.Parameters.Add(new OracleParameter("id", controlId));
                object? value = await read.ExecuteScalarAsync();
                if (value is null || value == DBNull.Value) { await transaction.RollbackAsync(); return false; }
                existingEvaluationId = Convert.ToInt64(value, CultureInfo.InvariantCulture);
            }
            if (existingEvaluationId != dto.ConEvaluacionId)
                throw new InvalidOperationException("No se permite cambiar el padre de un control gobernado.");
            const string update = @"UPDATE RL_MR_CONTROLES_RIESGO
                SET CON_TIPO=:type,CON_DESCRIPCION=:description,CON_AUTOMATIZACION=:automation,CON_ESTADO=:state
                WHERE CON_ID=:id AND CON_EVALUACION_ID=:evaluationId";
            await using (var command = Comando(update, connection, transaction))
            {
                command.Parameters.Add(new OracleParameter("id", controlId));
                AddControlParameters(command, dto);
                if (await command.ExecuteNonQueryAsync() != 1) { await transaction.RollbackAsync(); return false; }
            }
            await UpdateGovernedCalculationAsync(connection, transaction, dto.ConEvaluacionId, expectedEvaVersionRow, calculatedJson);
            await AuditarAsync(connection, transaction, "RL_MR_CONTROLES_RIESGO", controlId, "UPDATE", dto, usuarioId, ip);
            await AuditarAsync(connection, transaction, "RL_MR_EVALUACIONES_RIESGO", dto.ConEvaluacionId, "UPDATE", new { calculatedJson }, usuarioId, ip);
            await transaction.CommitAsync();
            return true;
        }
        catch { await transaction.RollbackAsync(); throw; }
    }

    private static async Task LockGovernedDraftEvaluationAsync(OracleConnection connection, OracleTransaction transaction, long evaluationId, int expectedVersionRow)
    {
        const string sql = "SELECT EVA_VERSION_ROW FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_ID=:id AND EVA_ACTIVO=1 FOR UPDATE";
        await using var command = Comando(sql, connection, transaction);
        command.Parameters.Add(new OracleParameter("id", evaluationId));
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) throw new KeyNotFoundException("La evaluación no existe.");
        if (reader.GetInt32(0) != expectedVersionRow) throw new DBConcurrencyException("La evaluación cambió durante la mutación del control. Recargue e intente nuevamente.");
        await reader.DisposeAsync();
        string state = await ObtenerEstadoActualAsync(connection, transaction, evaluationId);
        if (!state.Equals("BORRADOR", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Los controles de una evaluación gobernada solo se pueden modificar en estado BORRADOR.");
    }

    private static async Task<string> ObtenerEstadoActualAsync(OracleConnection connection, OracleTransaction transaction, long evaluationId)
    {
        const string sql = @"SELECT FLU_ESTADO FROM (
                                  SELECT FLU_ESTADO FROM RL_MR_FLUJOS_EVALUACION
                                   WHERE FLU_EVALUACION_ID=:id ORDER BY FLU_FECHA DESC,FLU_ID DESC
                              ) WHERE ROWNUM=1";
        await using var command = Comando(sql, connection, transaction);
        command.Parameters.Add(new OracleParameter("id", evaluationId));
        object? result = await command.ExecuteScalarAsync();
        return result is null || result == DBNull.Value ? "BORRADOR" : Convert.ToString(result, CultureInfo.InvariantCulture) ?? "BORRADOR";
    }

    private static async Task UpdateGovernedCalculationAsync(OracleConnection connection, OracleTransaction transaction, long evaluationId, int expectedVersionRow, string calculatedJson)
    {
        const string sql = @"UPDATE RL_MR_EVALUACIONES_RIESGO
                                SET EVA_CALCULOS_JSON=:calculation,EVA_VERSION_ROW=EVA_VERSION_ROW+1
                              WHERE EVA_ID=:id AND EVA_ACTIVO=1 AND EVA_VERSION_ROW=:expected";
        await using var command = Comando(sql, connection, transaction);
        command.Parameters.Add(new OracleParameter("calculation", OracleDbType.Clob) { Value = calculatedJson });
        command.Parameters.Add(new OracleParameter("id", evaluationId));
        command.Parameters.Add(new OracleParameter("expected", expectedVersionRow));
        if (await command.ExecuteNonQueryAsync() != 1) throw new DBConcurrencyException("No fue posible actualizar el cálculo por concurrencia o cambio de estado.");
    }

    private static void AddControlParameters(OracleCommand command, ControlRiesgoGuardarDto dto)
    {
        command.Parameters.Add(new OracleParameter("evaluationId", dto.ConEvaluacionId));
        command.Parameters.Add(new OracleParameter("type", dto.ConTipo.Trim().ToUpperInvariant()));
        command.Parameters.Add(new OracleParameter("description", dto.ConDescripcion.Trim()));
        command.Parameters.Add(new OracleParameter("automation", dto.ConAutomatizacion.Trim().ToUpperInvariant()));
        command.Parameters.Add(new OracleParameter("state", dto.ConEstado.Trim().ToUpperInvariant()));
    }

    public async Task<IReadOnlyList<EvaluacionControlDto>> ListarEvaluacionesControlAsync(long controlId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        const string sql = @"
            SELECT ECO_ID, ECO_CONTROL_ID, ECO_EFECTIVIDAD, ECO_COMENTARIO
              FROM RL_MR_EVALUACIONES_CONTROL
             WHERE ECO_CONTROL_ID = :controlId
             ORDER BY ECO_ID DESC";
        await using var cmd = Comando(sql, conn);
        cmd.Parameters.Add(new OracleParameter("controlId", controlId));
        var lista = new List<EvaluacionControlDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(new EvaluacionControlDto
            {
                EcoId = reader.GetInt64(0),
                EcoControlId = reader.GetInt64(1),
                EcoEfectividad = reader.GetDecimal(2),
                EcoComentario = reader.IsDBNull(3) ? null : TextoVisibleUtf8Normalizer.Normalizar(reader.GetString(3))
            });
        }
        return lista;
    }

    public async Task<long> RegistrarEvaluacionControlAsync(long controlId, EvaluacionControlGuardarDto dto, long usuarioId, string? ip)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var tx = conn.BeginTransaction();
        try
        {
            await ExigirExisteAsync(conn, tx, "RL_MR_CONTROLES_RIESGO", "CON_ID", controlId, "El control no existe.");
            long id = await SiguienteAsync(conn, tx, "SEQ_RL_MR_EVAL_CONTROLES");
            const string sql = @"
                INSERT INTO RL_MR_EVALUACIONES_CONTROL (ECO_ID, ECO_CONTROL_ID, ECO_EFECTIVIDAD, ECO_COMENTARIO)
                VALUES (:id, :controlId, :efectividad, :comentario)";
            await using var cmd = Comando(sql, conn, tx);
            cmd.Parameters.Add(new OracleParameter("id", id));
            cmd.Parameters.Add(new OracleParameter("controlId", controlId));
            cmd.Parameters.Add(new OracleParameter("efectividad", dto.EcoEfectividad));
            cmd.Parameters.Add(new OracleParameter("comentario", (object?)dto.EcoComentario?.Trim() ?? DBNull.Value));
            await cmd.ExecuteNonQueryAsync();
            await AuditarAsync(conn, tx, "RL_MR_EVALUACIONES_CONTROL", id, "INSERT", new { ControlId = controlId, dto.EcoEfectividad, dto.EcoComentario }, usuarioId, ip);
            await tx.CommitAsync();
            return id;
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    public async Task<IReadOnlyList<PlanMitigacionDto>> ListarPlanesAsync(long evaluacionId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        const string sql = @"
            SELECT PLA_ID, PLA_EVALUACION_ID, PLA_DESCRIPCION, PLA_AVANCE, PLA_PRESUPUESTO,
                   PLA_FECHA_INICIO, PLA_FECHA_FIN, PLA_ESTADO
              FROM RL_MR_PLANES
             WHERE PLA_EVALUACION_ID = :evaluacionId
             ORDER BY PLA_ID DESC";
        await using var cmd = Comando(sql, conn);
        cmd.Parameters.Add(new OracleParameter("evaluacionId", evaluacionId));
        var lista = new List<PlanMitigacionDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(new PlanMitigacionDto
            {
                PlaId = reader.GetInt64(0), PlaEvaluacionId = reader.GetInt64(1), PlaDescripcion = TextoVisibleUtf8Normalizer.Normalizar(reader.GetString(2)),
                PlaAvance = reader.GetDecimal(3), PlaPresupuesto = reader.GetDecimal(4), PlaFechaInicio = reader.GetDateTime(5),
                PlaFechaFin = reader.GetDateTime(6), PlaEstado = reader.GetString(7)
            });
        }
        return lista;
    }

    public async Task<long> CrearPlanAsync(PlanMitigacionGuardarDto dto, long usuarioId, string? ip)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var tx = conn.BeginTransaction();
        try
        {
            await ExigirEvaluacionAsync(conn, tx, dto.PlaEvaluacionId);
            long id = await SiguienteAsync(conn, tx, "SEQ_RL_MR_PLANES");
            const string sql = @"
                INSERT INTO RL_MR_PLANES
                    (PLA_ID, PLA_EVALUACION_ID, PLA_DESCRIPCION, PLA_AVANCE, PLA_PRESUPUESTO, PLA_FECHA_INICIO, PLA_FECHA_FIN, PLA_ESTADO)
                VALUES (:id, :evaluacionId, :descripcion, :avance, :presupuesto, :inicio, :fin, :estado)";
            await using var cmd = Comando(sql, conn, tx);
            AgregarParametrosPlan(cmd, id, dto);
            await cmd.ExecuteNonQueryAsync();
            await AuditarAsync(conn, tx, "RL_MR_PLANES", id, "INSERT", dto, usuarioId, ip);
            await tx.CommitAsync();
            return id;
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    public async Task<bool> ActualizarPlanAsync(long planId, PlanMitigacionGuardarDto dto, long usuarioId, string? ip)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var tx = conn.BeginTransaction();
        try
        {
            await ExigirEvaluacionAsync(conn, tx, dto.PlaEvaluacionId);
            const string sql = @"
                UPDATE RL_MR_PLANES
                   SET PLA_EVALUACION_ID = :evaluacionId, PLA_DESCRIPCION = :descripcion,
                       PLA_AVANCE = :avance, PLA_PRESUPUESTO = :presupuesto,
                       PLA_FECHA_INICIO = :inicio, PLA_FECHA_FIN = :fin, PLA_ESTADO = :estado
                 WHERE PLA_ID = :id";
            await using var cmd = Comando(sql, conn, tx);
            AgregarParametrosPlan(cmd, planId, dto);
            if (await cmd.ExecuteNonQueryAsync() != 1) { await tx.RollbackAsync(); return false; }
            await AuditarAsync(conn, tx, "RL_MR_PLANES", planId, "UPDATE", dto, usuarioId, ip);
            await tx.CommitAsync();
            return true;
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    public async Task<IReadOnlyList<ActividadPlanDto>> ListarActividadesAsync(long planId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        const string sql = @"
            SELECT ACT_ID, ACT_PLAN_ID, ACT_DESCRIPCION, ACT_RESPONSABLE, ACT_AVANCE, ACT_FECHA_INICIO, ACT_FECHA_FIN, ACT_ESTADO
              FROM RL_MR_ACTIVIDADES
             WHERE ACT_PLAN_ID = :planId
             ORDER BY ACT_ID";
        await using var cmd = Comando(sql, conn);
        cmd.Parameters.Add(new OracleParameter("planId", planId));
        var lista = new List<ActividadPlanDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(new ActividadPlanDto
            {
                ActId = reader.GetInt64(0), ActPlanId = reader.GetInt64(1), ActDescripcion = TextoVisibleUtf8Normalizer.Normalizar(reader.GetString(2)),
                ActResponsable = TextoVisibleUtf8Normalizer.Normalizar(reader.GetString(3)), ActAvance = reader.GetDecimal(4), ActFechaInicio = reader.GetDateTime(5),
                ActFechaFin = reader.GetDateTime(6), ActEstado = reader.GetString(7)
            });
        }
        return lista;
    }

    public async Task<long> CrearActividadAsync(ActividadPlanGuardarDto dto, long usuarioId, string? ip)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var tx = conn.BeginTransaction();
        try
        {
            await ExigirExisteAsync(conn, tx, "RL_MR_PLANES", "PLA_ID", dto.ActPlanId, "El plan no existe.");
            long id = await SiguienteAsync(conn, tx, "SEQ_RL_MR_ACTIVIDADES");
            const string sql = @"
                INSERT INTO RL_MR_ACTIVIDADES
                    (ACT_ID, ACT_PLAN_ID, ACT_DESCRIPCION, ACT_RESPONSABLE, ACT_AVANCE, ACT_FECHA_INICIO, ACT_FECHA_FIN, ACT_ESTADO)
                VALUES (:id, :planId, :descripcion, :responsable, :avance, :inicio, :fin, :estado)";
            await using var cmd = Comando(sql, conn, tx);
            AgregarParametrosActividad(cmd, id, dto);
            await cmd.ExecuteNonQueryAsync();
            await AuditarAsync(conn, tx, "RL_MR_ACTIVIDADES", id, "INSERT", dto, usuarioId, ip);
            await tx.CommitAsync();
            return id;
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    public async Task<bool> ActualizarActividadAsync(long actividadId, ActividadPlanGuardarDto dto, long usuarioId, string? ip)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var tx = conn.BeginTransaction();
        try
        {
            await ExigirExisteAsync(conn, tx, "RL_MR_PLANES", "PLA_ID", dto.ActPlanId, "El plan no existe.");
            const string sql = @"
                UPDATE RL_MR_ACTIVIDADES
                   SET ACT_PLAN_ID = :planId, ACT_DESCRIPCION = :descripcion, ACT_RESPONSABLE = :responsable,
                       ACT_AVANCE = :avance, ACT_FECHA_INICIO = :inicio, ACT_FECHA_FIN = :fin, ACT_ESTADO = :estado
                 WHERE ACT_ID = :id";
            await using var cmd = Comando(sql, conn, tx);
            AgregarParametrosActividad(cmd, actividadId, dto);
            if (await cmd.ExecuteNonQueryAsync() != 1) { await tx.RollbackAsync(); return false; }
            await AuditarAsync(conn, tx, "RL_MR_ACTIVIDADES", actividadId, "UPDATE", dto, usuarioId, ip);
            await tx.CommitAsync();
            return true;
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    private static void AgregarParametrosPlan(OracleCommand cmd, long id, PlanMitigacionGuardarDto dto)
    {
        cmd.Parameters.Add(new OracleParameter("evaluacionId", dto.PlaEvaluacionId));
        cmd.Parameters.Add(new OracleParameter("descripcion", dto.PlaDescripcion.Trim()));
        cmd.Parameters.Add(new OracleParameter("avance", dto.PlaAvance));
        cmd.Parameters.Add(new OracleParameter("presupuesto", dto.PlaPresupuesto));
        cmd.Parameters.Add(new OracleParameter("inicio", dto.PlaFechaInicio));
        cmd.Parameters.Add(new OracleParameter("fin", dto.PlaFechaFin));
        cmd.Parameters.Add(new OracleParameter("estado", dto.PlaEstado.Trim().ToUpperInvariant()));
        cmd.Parameters.Add(new OracleParameter("id", id));
    }

    private static void AgregarParametrosActividad(OracleCommand cmd, long id, ActividadPlanGuardarDto dto)
    {
        cmd.Parameters.Add(new OracleParameter("planId", dto.ActPlanId));
        cmd.Parameters.Add(new OracleParameter("descripcion", dto.ActDescripcion.Trim()));
        cmd.Parameters.Add(new OracleParameter("responsable", dto.ActResponsable.Trim()));
        cmd.Parameters.Add(new OracleParameter("avance", dto.ActAvance));
        cmd.Parameters.Add(new OracleParameter("inicio", dto.ActFechaInicio));
        cmd.Parameters.Add(new OracleParameter("fin", dto.ActFechaFin));
        cmd.Parameters.Add(new OracleParameter("estado", dto.ActEstado.Trim().ToUpperInvariant()));
        cmd.Parameters.Add(new OracleParameter("id", id));
    }

    private async Task AuditarAsync(OracleConnection conn, OracleTransaction tx, string tabla, long id, string accion, object datos, long usuarioId, string? ip) =>
        await _auditoria.RegistrarAsync(conn, tx, tabla, id.ToString(), accion, null, JsonSerializer.Serialize(datos), usuarioId, null, ip, Modulo);

    private static async Task ExigirEvaluacionAsync(OracleConnection conn, OracleTransaction tx, long evaluacionId) =>
        await ExigirExisteAsync(conn, tx, "RL_MR_EVALUACIONES_RIESGO", "EVA_ID", evaluacionId, "La evaluación no existe.");

    private static async Task ExigirExisteAsync(OracleConnection conn, OracleTransaction tx, string tabla, string columna, long id, string mensaje)
    {
        string sql = $"SELECT COUNT(*) FROM {tabla} WHERE {columna} = :id";
        await using var cmd = Comando(sql, conn, tx);
        cmd.Parameters.Add(new OracleParameter("id", id));
        if (Convert.ToInt32(await cmd.ExecuteScalarAsync()) != 1) throw new InvalidOperationException(mensaje);
    }

    private static async Task<long> SiguienteAsync(OracleConnection conn, OracleTransaction tx, string secuencia)
    {
        await using var cmd = Comando($"SELECT {secuencia}.NEXTVAL FROM DUAL", conn, tx);
        return Convert.ToInt64(await cmd.ExecuteScalarAsync());
    }

    private static OracleCommand Comando(string sql, OracleConnection conn, OracleTransaction? tx = null) =>
        new(sql, conn) { BindByName = true, Transaction = tx };
}
