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
    Task<MitigacionBloque4Dto?> ObtenerBloque4Async(long evaluacionId);
    Task<long> CrearPlanAsync(PlanMitigacionGuardarDto dto, long usuarioId, string? ip);
    Task<bool> ActualizarPlanAsync(long planId, PlanMitigacionGuardarDto dto, long usuarioId, string? ip);
    Task<IReadOnlyList<ActividadPlanDto>> ListarActividadesAsync(long planId);
    Task<long> CrearActividadAsync(ActividadPlanGuardarDto dto, long usuarioId, string? ip);
    Task<bool> ActualizarActividadAsync(long actividadId, ActividadPlanGuardarDto dto, long usuarioId, string? ip);
}

public sealed class MatricesRiesgosMitigacionRepository : IMatricesRiesgosMitigacionRepository, IGovernedControlMutationStore
{
    private const string Modulo = "MatricesRiesgos";
    private readonly OracleDbContext _db;
    private readonly IAuditoriaRepository _auditoria;
    private readonly GovernedControlMutationExecutor _governedMutations = new();

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
        => await _governedMutations.CreateAsync(this, dto, expectedEvaVersionRow, calculatedJson, usuarioId, ip);

    public async Task<bool> ActualizarControlGobernadoAtomicoAsync(long controlId, ControlRiesgoGuardarDto dto, int expectedEvaVersionRow, string calculatedJson, long usuarioId, string? ip)
        => await _governedMutations.UpdateAsync(this, controlId, dto, expectedEvaVersionRow, calculatedJson, usuarioId, ip);

    async Task<IGovernedControlMutationSession> IGovernedControlMutationStore.BeginAsync() =>
        await OracleGovernedControlMutationSession.CreateAsync(_db, _auditoria);

    private sealed class OracleGovernedControlMutationSession : IGovernedControlMutationSession
    {
        private readonly OracleConnection _connection;
        private readonly OracleTransaction _transaction;
        private readonly IAuditoriaRepository _auditoria;

        private OracleGovernedControlMutationSession(OracleConnection connection, OracleTransaction transaction, IAuditoriaRepository auditoria)
        {
            _connection = connection;
            _transaction = transaction;
            _auditoria = auditoria;
        }

        public static async Task<OracleGovernedControlMutationSession> CreateAsync(OracleDbContext database, IAuditoriaRepository auditoria)
        {
            OracleConnection connection = database.CreateConnection();
            try
            {
                await connection.OpenAsync();
                return new OracleGovernedControlMutationSession(connection, connection.BeginTransaction(), auditoria);
            }
            catch
            {
                await connection.DisposeAsync();
                throw;
            }
        }

        public Task LockDraftEvaluationAsync(long evaluationId, int expectedVersionRow) =>
            LockGovernedDraftEvaluationAsync(_connection, _transaction, evaluationId, expectedVersionRow);

        public async Task<long> InsertControlAsync(ControlRiesgoGuardarDto control)
        {
            long id = await SiguienteAsync(_connection, _transaction, "SEQ_RL_MR_CONTROLES");
            const string sql = @"INSERT INTO RL_MR_CONTROLES_RIESGO
                (CON_ID,CON_EVALUACION_ID,CON_TIPO,CON_DESCRIPCION,CON_AUTOMATIZACION,CON_ESTADO)
                VALUES(:id,:evaluationId,:type,:description,:automation,:state)";
            await using OracleCommand command = Comando(sql, _connection, _transaction);
            command.Parameters.Add(new OracleParameter("id", id));
            AddControlParameters(command, control);
            await command.ExecuteNonQueryAsync();
            return id;
        }

        public async Task<long?> LockControlParentAsync(long controlId)
        {
            const string sql = "SELECT CON_EVALUACION_ID FROM RL_MR_CONTROLES_RIESGO WHERE CON_ID=:id FOR UPDATE";
            await using OracleCommand command = Comando(sql, _connection, _transaction);
            command.Parameters.Add(new OracleParameter("id", controlId));
            object? value = await command.ExecuteScalarAsync();
            return value is null || value == DBNull.Value ? null : Convert.ToInt64(value, CultureInfo.InvariantCulture);
        }

        public async Task<bool> UpdateControlAsync(long controlId, ControlRiesgoGuardarDto control)
        {
            const string sql = @"UPDATE RL_MR_CONTROLES_RIESGO
                SET CON_TIPO=:type,CON_DESCRIPCION=:description,CON_AUTOMATIZACION=:automation,CON_ESTADO=:state
                WHERE CON_ID=:id AND CON_EVALUACION_ID=:evaluationId";
            await using OracleCommand command = Comando(sql, _connection, _transaction);
            command.Parameters.Add(new OracleParameter("id", controlId));
            AddControlParameters(command, control);
            return await command.ExecuteNonQueryAsync() == 1;
        }

        public Task UpdateCalculationAsync(long evaluationId, int expectedVersionRow, string calculatedJson) =>
            UpdateGovernedCalculationAsync(_connection, _transaction, evaluationId, expectedVersionRow, calculatedJson);

        public Task AuditControlAsync(long controlId, string action, ControlRiesgoGuardarDto control, long usuarioId, string? ip) =>
            _auditoria.RegistrarAsync(_connection, _transaction, "RL_MR_CONTROLES_RIESGO", controlId.ToString(), action,
                null, JsonSerializer.Serialize(control), usuarioId, null, ip, Modulo);

        public Task AuditEvaluationAsync(long evaluationId, string calculatedJson, long usuarioId, string? ip) =>
            _auditoria.RegistrarAsync(_connection, _transaction, "RL_MR_EVALUACIONES_RIESGO", evaluationId.ToString(), "UPDATE",
                null, JsonSerializer.Serialize(new { calculatedJson }), usuarioId, null, ip, Modulo);

        public Task CommitAsync() => _transaction.CommitAsync();
        public Task RollbackAsync() => _transaction.RollbackAsync();

        public async ValueTask DisposeAsync()
        {
            await _transaction.DisposeAsync();
            await _connection.DisposeAsync();
        }
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
                   PLA_FECHA_INICIO, PLA_FECHA_FIN, PLA_ESTADO,
                   PLA_MONITOREO_SEGUIMIENTO, PLA_RESPONSABLES, PLA_RECURSOS
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
                PlaFechaFin = reader.GetDateTime(6), PlaEstado = reader.GetString(7),
                PlaMonitoreoSeguimiento = TextoNullable(reader, 8), PlaResponsables = TextoNullable(reader, 9), PlaRecursos = TextoNullable(reader, 10)
            });
        }
        return lista;
    }

    public async Task<MitigacionBloque4Dto?> ObtenerBloque4Async(long evaluacionId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        const string sql = @"
            SELECT e.EVA_ID,
                   p.PLA_ID, p.PLA_EVALUACION_ID, p.PLA_DESCRIPCION, p.PLA_AVANCE, p.PLA_PRESUPUESTO,
                   p.PLA_FECHA_INICIO, p.PLA_FECHA_FIN, p.PLA_ESTADO,
                   p.PLA_MONITOREO_SEGUIMIENTO, p.PLA_RESPONSABLES, p.PLA_RECURSOS,
                   a.ACT_ID, a.ACT_PLAN_ID, a.ACT_DESCRIPCION, a.ACT_RESPONSABLE, a.ACT_AVANCE,
                   a.ACT_FECHA_INICIO, a.ACT_FECHA_FIN, a.ACT_ESTADO
              FROM RL_MR_EVALUACIONES_RIESGO e
              LEFT JOIN RL_MR_PLANES p ON p.PLA_EVALUACION_ID = e.EVA_ID
              LEFT JOIN RL_MR_ACTIVIDADES a ON a.ACT_PLAN_ID = p.PLA_ID
             WHERE e.EVA_ID = :evaluacionId
             ORDER BY p.PLA_ID DESC, a.ACT_ID";
        await using var cmd = Comando(sql, conn);
        cmd.Parameters.Add(new OracleParameter("evaluacionId", evaluacionId));

        var planes = new List<PlanMitigacionDto>();
        var porId = new Dictionary<long, PlanMitigacionDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        do
        {
            if (reader.IsDBNull(1)) continue;
            long planId = reader.GetInt64(1);
            if (!porId.TryGetValue(planId, out var plan))
            {
                plan = new PlanMitigacionDto
                {
                    PlaId = planId,
                    PlaEvaluacionId = reader.GetInt64(2),
                    PlaDescripcion = TextoVisibleUtf8Normalizer.Normalizar(reader.GetString(3)),
                    PlaAvance = reader.GetDecimal(4),
                    PlaPresupuesto = reader.GetDecimal(5),
                    PlaFechaInicio = reader.GetDateTime(6),
                    PlaFechaFin = reader.GetDateTime(7),
                    PlaEstado = reader.GetString(8),
                    PlaMonitoreoSeguimiento = TextoNullable(reader, 9),
                    PlaResponsables = TextoNullable(reader, 10),
                    PlaRecursos = TextoNullable(reader, 11)
                };
                porId.Add(planId, plan);
                planes.Add(plan);
            }

            if (!reader.IsDBNull(12))
            {
                var actividades = plan.Actividades as List<ActividadPlanDto>;
                if (actividades is null)
                {
                    actividades = [];
                    plan.Actividades = actividades;
                }
                actividades.Add(new ActividadPlanDto
                {
                    ActId = reader.GetInt64(12),
                    ActPlanId = reader.GetInt64(13),
                    ActDescripcion = TextoVisibleUtf8Normalizer.Normalizar(reader.GetString(14)),
                    ActResponsable = TextoVisibleUtf8Normalizer.Normalizar(reader.GetString(15)),
                    ActAvance = reader.GetDecimal(16),
                    ActFechaInicio = reader.GetDateTime(17),
                    ActFechaFin = reader.GetDateTime(18),
                    ActEstado = reader.GetString(19)
                });
                plan.CantidadActividades++;
            }
        } while (await reader.ReadAsync());

        return new MitigacionBloque4Dto { CantidadAcciones = planes.Count, Planes = planes };
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
                    (PLA_ID, PLA_EVALUACION_ID, PLA_DESCRIPCION, PLA_AVANCE, PLA_PRESUPUESTO, PLA_FECHA_INICIO, PLA_FECHA_FIN, PLA_ESTADO,
                     PLA_MONITOREO_SEGUIMIENTO, PLA_RESPONSABLES, PLA_RECURSOS)
                VALUES (:id, :evaluacionId, :descripcion, :avance, :presupuesto, :inicio, :fin, :estado,
                        :monitoreo, :responsables, :recursos)";
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
            var anterior = await ObtenerPlanParaAuditoriaAsync(conn, tx, planId);
            if (anterior is null) { await tx.RollbackAsync(); return false; }
            if (anterior.PlaEvaluacionId != dto.PlaEvaluacionId) throw new InvalidOperationException("No se permite cambiar la evaluación padre de un plan.");
            const string sql = @"
                UPDATE RL_MR_PLANES
                   SET PLA_DESCRIPCION = :descripcion, PLA_AVANCE = :avance, PLA_PRESUPUESTO = :presupuesto,
                       PLA_FECHA_INICIO = :inicio, PLA_FECHA_FIN = :fin, PLA_ESTADO = :estado,
                       PLA_MONITOREO_SEGUIMIENTO = :monitoreo, PLA_RESPONSABLES = :responsables, PLA_RECURSOS = :recursos
                 WHERE PLA_ID = :id AND PLA_EVALUACION_ID = :evaluacionId";
            await using var cmd = Comando(sql, conn, tx);
            AgregarParametrosPlan(cmd, planId, dto);
            if (await cmd.ExecuteNonQueryAsync() != 1) { await tx.RollbackAsync(); return false; }
            await AuditarAntesDespuesAsync(conn, tx, "RL_MR_PLANES", planId, anterior, dto, usuarioId, ip);
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
            long? planActual = await ObtenerPlanPadreActividadAsync(conn, tx, actividadId);
            if (planActual is null) { await tx.RollbackAsync(); return false; }
            if (planActual.Value != dto.ActPlanId) throw new InvalidOperationException("No se permite cambiar el plan padre de una actividad.");
            const string sql = @"
                UPDATE RL_MR_ACTIVIDADES
                   SET ACT_DESCRIPCION = :descripcion, ACT_RESPONSABLE = :responsable,
                       ACT_AVANCE = :avance, ACT_FECHA_INICIO = :inicio, ACT_FECHA_FIN = :fin, ACT_ESTADO = :estado
                 WHERE ACT_ID = :id AND ACT_PLAN_ID = :planId";
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
        cmd.Parameters.Add(new OracleParameter("monitoreo", (object?)NormalizarTextoPlan(dto.PlaMonitoreoSeguimiento) ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("responsables", (object?)NormalizarTextoPlan(dto.PlaResponsables) ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("recursos", (object?)NormalizarTextoPlan(dto.PlaRecursos) ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("id", id));
    }

    private static string? NormalizarTextoPlan(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : TextoVisibleUtf8Normalizer.Normalizar(valor.Trim());

    private static string? TextoNullable(OracleDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : TextoVisibleUtf8Normalizer.Normalizar(reader.GetString(ordinal));

    private static async Task<PlanMitigacionDto?> ObtenerPlanParaAuditoriaAsync(OracleConnection conn, OracleTransaction tx, long planId)
    {
        const string sql = @"SELECT PLA_ID, PLA_EVALUACION_ID, PLA_DESCRIPCION, PLA_AVANCE, PLA_PRESUPUESTO,
                                    PLA_FECHA_INICIO, PLA_FECHA_FIN, PLA_ESTADO,
                                    PLA_MONITOREO_SEGUIMIENTO, PLA_RESPONSABLES, PLA_RECURSOS
                               FROM RL_MR_PLANES WHERE PLA_ID = :id FOR UPDATE";
        await using var cmd = Comando(sql, conn, tx);
        cmd.Parameters.Add(new OracleParameter("id", planId));
        await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow);
        if (!await reader.ReadAsync()) return null;
        return new PlanMitigacionDto
        {
            PlaId = reader.GetInt64(0), PlaEvaluacionId = reader.GetInt64(1),
            PlaDescripcion = TextoVisibleUtf8Normalizer.Normalizar(reader.GetString(2)),
            PlaAvance = reader.GetDecimal(3), PlaPresupuesto = reader.GetDecimal(4),
            PlaFechaInicio = reader.GetDateTime(5), PlaFechaFin = reader.GetDateTime(6), PlaEstado = reader.GetString(7),
            PlaMonitoreoSeguimiento = TextoNullable(reader, 8), PlaResponsables = TextoNullable(reader, 9), PlaRecursos = TextoNullable(reader, 10)
        };
    }

    private static async Task<long?> ObtenerPlanPadreActividadAsync(OracleConnection conn, OracleTransaction tx, long actividadId)
    {
        const string sql = "SELECT ACT_PLAN_ID FROM RL_MR_ACTIVIDADES WHERE ACT_ID = :id FOR UPDATE";
        await using var cmd = Comando(sql, conn, tx);
        cmd.Parameters.Add(new OracleParameter("id", actividadId));
        object? value = await cmd.ExecuteScalarAsync();
        return value is null or DBNull ? null : Convert.ToInt64(value);
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

    private async Task AuditarAntesDespuesAsync(OracleConnection conn, OracleTransaction tx, string tabla, long id, object anteriores, object nuevos, long usuarioId, string? ip) =>
        await _auditoria.RegistrarAsync(conn, tx, tabla, id.ToString(), "UPDATE", JsonSerializer.Serialize(anteriores), JsonSerializer.Serialize(nuevos), usuarioId, null, ip, Modulo);

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
