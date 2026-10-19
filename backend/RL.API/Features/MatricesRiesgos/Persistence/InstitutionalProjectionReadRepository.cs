using Oracle.ManagedDataAccess.Client;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Infrastructure.Database;
using System.Data;

namespace RL.API.Features.MatricesRiesgos.Persistence;

public sealed record InstitutionalProjectionSnapshot(
    EvaluacionRiesgoDto Evaluation,
    RiesgoDto Risk,
    IReadOnlyList<RiesgoDto> AllRisks,
    IReadOnlyList<ControlRiesgoDto> Controls,
    MitigacionBloque4Dto Mitigation,
    MatrizBloque6Dto Monitoring,
    int QueryCount);

public interface IInstitutionalProjectionReadRepository
{
    Task<InstitutionalProjectionSnapshot?> ReadAsync(long evaluationId, long userId, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<long, InstitutionalProjectionSnapshot>> ReadManyAsync(
        IReadOnlyCollection<long> evaluationIds, long userId, CancellationToken cancellationToken);
}

/// <summary>Loads all data for the canonical read model from one Oracle read-only transaction.</summary>
public sealed class InstitutionalProjectionReadRepository(OracleDbContext database) : IInstitutionalProjectionReadRepository
{
    private const int BatchSize = 500;

    /// <summary>
    /// Loads selected evaluations with set-based reads. Evaluation IDs are supplied by the
    /// caller's already-filtered consolidated result; this method never selects/reselects them.
    /// Every source query is bounded by 500 IDs, avoiding Oracle's IN-list limit and per-risk I/O.
    /// </summary>
    public async Task<IReadOnlyDictionary<long, InstitutionalProjectionSnapshot>> ReadManyAsync(
        IReadOnlyCollection<long> evaluationIds, long userId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(evaluationIds);
        if (userId <= 0 || evaluationIds.Any(id => id <= 0))
            throw new ArgumentOutOfRangeException(nameof(evaluationIds), "Los IDs de evaluación y usuario deben ser positivos.");

        long[] ids = evaluationIds.Distinct().Order().ToArray();
        if (ids.Length == 0) return new Dictionary<long, InstitutionalProjectionSnapshot>();

        await using OracleConnection connection = database.CreateConnection();
        connection.UseHourOffsetForUnsupportedTimezone = true;
        await connection.OpenAsync(cancellationToken);
        await using OracleTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            IReadOnlyList<RiesgoDto> allRisks = await ReadAllRisksAsync(connection, transaction, cancellationToken);
            var capabilities = await ReadCapabilitiesAsync(connection, transaction, userId, cancellationToken);
            var result = new Dictionary<long, InstitutionalProjectionSnapshot>(ids.Length);
            int batchCount = (ids.Length + BatchSize - 1) / BatchSize;
            int totalQueryCount = checked(batchCount * 7 + 2);

            foreach (long[] batch in ids.Chunk(BatchSize))
            {
                cancellationToken.ThrowIfCancellationRequested();
                InClause evaluationIn = BuildInClause(batch, "eval");
                var evaluations = await ReadEvaluationsBatchAsync(connection, transaction, evaluationIn, cancellationToken);
                var riskIds = evaluations.Values.Select(item => item.EvaRiesgoId).Distinct().ToArray();
                var risks = await ReadRisksBatchAsync(connection, transaction, riskIds, cancellationToken);
                var controls = await ReadControlsBatchAsync(connection, transaction, evaluationIn, cancellationToken);
                var mitigation = await ReadPlansBatchAsync(connection, transaction, evaluationIn, cancellationToken);
                var monitoring = await ReadMonitoringBatchAsync(connection, transaction, evaluationIn, capabilities, cancellationToken);

                foreach ((long evaluationId, EvaluacionRiesgoDto evaluation) in evaluations)
                {
                    if (!risks.TryGetValue(evaluation.EvaRiesgoId, out RiesgoDto? risk)) continue;
                    result[evaluationId] = new(
                        evaluation,
                        risk,
                        allRisks,
                        controls.GetValueOrDefault(evaluationId) ?? Array.Empty<ControlRiesgoDto>(),
                        mitigation.GetValueOrDefault(evaluationId) ?? new MitigacionBloque4Dto(),
                        monitoring.GetValueOrDefault(evaluationId) ?? EmptyMonitoring(capabilities),
                        totalQueryCount);
                }
            }

            return result;
        }
        finally
        {
            await transaction.RollbackAsync(CancellationToken.None);
        }
    }

    private static async Task<Dictionary<long, EvaluacionRiesgoDto>> ReadEvaluationsBatchAsync(
        OracleConnection connection, OracleTransaction transaction, InClause inClause, CancellationToken token)
    {
        string sql = $@"SELECT e.EVA_ID,e.EVA_RIESGO_ID,e.EVA_VERSION_ID,NVL(f.FLU_ESTADO,'BORRADOR'),
e.EVA_DATOS_JSON,e.EVA_CALCULOS_JSON,p.PROY_VRI,CAST(NULL AS NUMBER),p.PROY_VRR,e.EVA_FECHA_REGISTRO,
e.EVA_USR_REGISTRO,e.EVA_VERSION_ROW,e.EVA_ACTIVO
FROM RL_MR_EVALUACIONES_RIESGO e
LEFT JOIN RL_MR_PROYECCIONES_EVALUACION p ON p.PROY_EVALUACION_ID=e.EVA_ID
LEFT JOIN (SELECT FLU_EVALUACION_ID,FLU_ESTADO FROM (SELECT FLU_EVALUACION_ID,FLU_ESTADO,
ROW_NUMBER() OVER(PARTITION BY FLU_EVALUACION_ID ORDER BY FLU_FECHA DESC,FLU_ID DESC) RN FROM RL_MR_FLUJOS_EVALUACION) WHERE RN=1) f
ON f.FLU_EVALUACION_ID=e.EVA_ID WHERE e.EVA_ID IN ({inClause.Sql})";
        var rows = new Dictionary<long, EvaluacionRiesgoDto>();
        await using OracleCommand command = CreateCommand(connection, transaction, sql, inClause.Parameters);
        await using OracleDataReader reader = await command.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token))
        {
            var evaluation = new EvaluacionRiesgoDto
            {
                EvaId = reader.GetInt64(0), EvaRiesgoId = reader.GetInt64(1), EvaVersionId = reader.GetInt64(2),
                EvaEstado = reader.GetString(3), EvaDataJson = reader.GetString(4), EvaDataCalcJson = reader.GetString(5),
                EvaVri = reader.IsDBNull(6) ? null : Convert.ToInt32(reader.GetDecimal(6)), EvaEtp = reader.IsDBNull(7) ? null : reader.GetDecimal(7),
                EvaVrr = reader.IsDBNull(8) ? null : Convert.ToInt32(reader.GetDecimal(8)), EvaFechaEval = reader.GetDateTime(9),
                EvaUsrEval = reader.GetInt64(10), EvaVersionRow = Convert.ToInt32(reader.GetDecimal(11)), EvaActivo = reader.GetInt32(12) == 1
            };
            rows.Add(evaluation.EvaId, evaluation);
        }
        return rows;
    }

    private static async Task<Dictionary<long, RiesgoDto>> ReadRisksBatchAsync(
        OracleConnection connection, OracleTransaction transaction, IReadOnlyCollection<long> ids, CancellationToken token)
    {
        if (ids.Count == 0) return new();
        InClause inClause = BuildInClause(ids.ToArray(), "risk");
        string sql = $"SELECT RIE_ID,RIE_CODIGO,RIE_NOMBRE,RIE_DESCRIPCION,RIE_ACTIVO,RIE_USR_CREACION,RIE_FECHA_CREACION FROM RL_MR_RIESGOS WHERE RIE_ID IN ({inClause.Sql})";
        var rows = new Dictionary<long, RiesgoDto>();
        await using OracleCommand command = CreateCommand(connection, transaction, sql, inClause.Parameters);
        await using OracleDataReader reader = await command.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token))
        {
            RiesgoDto risk = MapRisk(reader);
            rows.Add(risk.RieId, risk);
        }
        return rows;
    }

    private static async Task<Dictionary<long, IReadOnlyList<ControlRiesgoDto>>> ReadControlsBatchAsync(
        OracleConnection connection, OracleTransaction transaction, InClause ids, CancellationToken token)
    {
        string sql = $"SELECT CON_ID,CON_EVALUACION_ID,CON_TIPO,CON_DESCRIPCION,CON_AUTOMATIZACION,CON_ESTADO,CON_ESTADO_MONITOREO,CON_EFECTIVIDAD_MONITOREO FROM RL_MR_CONTROLES_RIESGO WHERE CON_EVALUACION_ID IN ({ids.Sql}) ORDER BY CON_EVALUACION_ID,CON_ID";
        var grouped = new Dictionary<long, List<ControlRiesgoDto>>();
        await using OracleCommand command = CreateCommand(connection, transaction, sql, ids.Parameters);
        await using OracleDataReader reader = await command.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token))
        {
            var row = new ControlRiesgoDto
            {
                ConId=reader.GetInt64(0), ConEvaluacionId=reader.GetInt64(1), ConTipo=reader.GetString(2),
                ConDescripcion=reader.GetString(3), ConAutomatizacion=reader.GetString(4), ConEstado=reader.GetString(5),
                ConEstadoMonitoreo=NullableText(reader,6), ConEfectividadMonitoreo=reader.IsDBNull(7)?null:reader.GetDecimal(7)
            };
            if (!grouped.TryGetValue(row.ConEvaluacionId, out List<ControlRiesgoDto>? list)) grouped[row.ConEvaluacionId] = list = new();
            list.Add(row);
        }
        return grouped.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<ControlRiesgoDto>)pair.Value);
    }

    private static async Task<Dictionary<long, MitigacionBloque4Dto>> ReadPlansBatchAsync(
        OracleConnection connection, OracleTransaction transaction, InClause ids, CancellationToken token)
    {
        string sql = $@"SELECT p.PLA_ID,p.PLA_EVALUACION_ID,p.PLA_DESCRIPCION,p.PLA_AVANCE,p.PLA_PRESUPUESTO,
p.PLA_FECHA_INICIO,p.PLA_FECHA_FIN,p.PLA_ESTADO,p.PLA_MONITOREO_SEGUIMIENTO,p.PLA_RESPONSABLES,p.PLA_RECURSOS,
a.ACT_ID,a.ACT_PLAN_ID,a.ACT_DESCRIPCION,a.ACT_RESPONSABLE,a.ACT_AVANCE,a.ACT_FECHA_INICIO,a.ACT_FECHA_FIN,a.ACT_ESTADO
FROM RL_MR_PLANES p LEFT JOIN RL_MR_ACTIVIDADES a ON a.ACT_PLAN_ID=p.PLA_ID
WHERE p.PLA_EVALUACION_ID IN ({ids.Sql}) ORDER BY p.PLA_EVALUACION_ID,p.PLA_ID,a.ACT_ID";
        var byEvaluation = new Dictionary<long, List<PlanMitigacionDto>>();
        var byPlan = new Dictionary<long, PlanMitigacionDto>();
        await using OracleCommand command = CreateCommand(connection, transaction, sql, ids.Parameters);
        await using OracleDataReader reader = await command.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token))
        {
            long planId = reader.GetInt64(0);
            if (!byPlan.TryGetValue(planId, out PlanMitigacionDto? plan))
            {
                plan = new PlanMitigacionDto { PlaId=planId, PlaEvaluacionId=reader.GetInt64(1), PlaDescripcion=reader.GetString(2),
                    PlaAvance=reader.GetDecimal(3), PlaPresupuesto=reader.GetDecimal(4), PlaFechaInicio=reader.GetDateTime(5),
                    PlaFechaFin=reader.GetDateTime(6), PlaEstado=reader.GetString(7), PlaMonitoreoSeguimiento=NullableText(reader,8),
                    PlaResponsables=NullableText(reader,9), PlaRecursos=NullableText(reader,10), Actividades=new List<ActividadPlanDto>() };
                byPlan.Add(planId, plan);
                if (!byEvaluation.TryGetValue(plan.PlaEvaluacionId, out List<PlanMitigacionDto>? list)) byEvaluation[plan.PlaEvaluacionId] = list = new();
                list.Add(plan);
            }
            if (!reader.IsDBNull(11))
            {
                ((List<ActividadPlanDto>)plan.Actividades).Add(new() { ActId=reader.GetInt64(11), ActPlanId=reader.GetInt64(12), ActDescripcion=reader.GetString(13),
                    ActResponsable=reader.GetString(14), ActAvance=reader.GetDecimal(15), ActFechaInicio=reader.GetDateTime(16), ActFechaFin=reader.GetDateTime(17), ActEstado=reader.GetString(18) });
                plan.CantidadActividades++;
            }
        }
        return byEvaluation.ToDictionary(pair => pair.Key, pair => new MitigacionBloque4Dto { CantidadAcciones=pair.Value.Count, Planes=pair.Value });
    }

    private static async Task<HashSet<string>> ReadCapabilitiesAsync(OracleConnection connection, OracleTransaction transaction, long userId, CancellationToken token)
    {
        var capabilities = new HashSet<string>(StringComparer.Ordinal);
        await using OracleCommand command = CreateCommand(connection, transaction, "SELECT UCP_CAPACIDAD FROM RL_USUARIO_CAPACIDADES WHERE UCP_USR_ID=:userId AND UCP_ACTIVO=1", ("userId", userId));
        await using OracleDataReader reader = await command.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token)) capabilities.Add(reader.GetString(0));
        return capabilities;
    }

    private static MatrizBloque6Dto EmptyMonitoring(HashSet<string> capabilities) => new()
    {
        SenalesAlerta=Array.Empty<SenalAlertaDto>(), Controles=Array.Empty<ControlMonitoreoMatrizDto>(),
        PuedeEditarObservacionesArea=capabilities.Contains("MATRICES_RIESGO_OBSERVACIONES_AREA_EDITAR"),
        PuedeEditarObservacionesUgr=capabilities.Contains("MATRICES_RIESGO_OBSERVACIONES_UGR_EDITAR")
    };

    private static async Task<Dictionary<long, MatrizBloque6Dto>> ReadMonitoringBatchAsync(
        OracleConnection connection, OracleTransaction transaction, InClause ids, HashSet<string> capabilities, CancellationToken token)
    {
        var alerts = new Dictionary<long, List<SenalAlertaDto>>();
        await using (OracleCommand command = CreateCommand(connection, transaction,
            $"SELECT ALE_ID,ALE_EVALUACION_ID,ALE_CODIGO,ALE_INDICADOR,ALE_ESTADO,ALE_FECHA_DISPARO FROM RL_MR_SENALES_ALERTA WHERE ALE_EVALUACION_ID IN ({ids.Sql}) ORDER BY ALE_EVALUACION_ID,ALE_ID", ids.Parameters))
        await using (OracleDataReader reader = await command.ExecuteReaderAsync(token))
            while (await reader.ReadAsync(token))
            {
                var alert = new SenalAlertaDto { AleId=reader.GetInt64(0), AleEvaluacionId=reader.GetInt64(1), AleCodigo=reader.GetString(2),
                    AleIndicador=reader.GetString(3), AleEstado=reader.GetString(4), AleFechaDisparo=reader.IsDBNull(5)?null:reader.GetDateTime(5) };
                if (!alerts.TryGetValue(alert.AleEvaluacionId, out List<SenalAlertaDto>? list)) alerts[alert.AleEvaluacionId] = list = new();
                list.Add(alert);
            }

        var monitoringState = new Dictionary<long, (string? State, string? Area, string? Ugr)>();
        string stateSql = $@"SELECT MON_EVALUACION_ID,MON_ESTADO_RIESGO,MON_OBSERVACIONES_AREA,MON_OBSERVACIONES_UGR FROM (
SELECT MON_EVALUACION_ID,MON_ESTADO_RIESGO,MON_OBSERVACIONES_AREA,MON_OBSERVACIONES_UGR,
ROW_NUMBER() OVER(PARTITION BY MON_EVALUACION_ID ORDER BY MON_FECHA DESC,MON_ID DESC) RN
FROM RL_MR_AUTOMONITOREO WHERE MON_EVALUACION_ID IN ({ids.Sql})) WHERE RN=1";
        await using (OracleCommand command = CreateCommand(connection, transaction, stateSql, ids.Parameters))
        await using (OracleDataReader reader = await command.ExecuteReaderAsync(token))
            while (await reader.ReadAsync(token)) monitoringState[reader.GetInt64(0)] = (NullableText(reader,1), NullableText(reader,2), NullableText(reader,3));

        var controlsByEvaluation = new Dictionary<long, Dictionary<long, ControlMonitoreoMatrizDto>>();
        string controlsSql = $@"SELECT c.CON_EVALUACION_ID,c.CON_ID,c.CON_TIPO,c.CON_DESCRIPCION,c.CON_ESTADO_MONITOREO,c.CON_EFECTIVIDAD_MONITOREO,e.EVI_ID,e.EVI_NOMBRE_ARCHIVO,e.EVI_FECHA_CREACION
FROM RL_MR_CONTROLES_RIESGO c LEFT JOIN RL_MR_EVIDENCIAS_VINCULOS v ON v.EVV_TIPO_ENTIDAD='CONTROL' AND v.EVV_ENTIDAD_ID=c.CON_ID
LEFT JOIN RL_MR_EVIDENCIAS e ON e.EVI_ID=v.EVV_EVIDENCIA_ID WHERE c.CON_EVALUACION_ID IN ({ids.Sql})
ORDER BY c.CON_EVALUACION_ID,c.CON_ID,e.EVI_FECHA_CREACION,e.EVI_ID";
        await using (OracleCommand command = CreateCommand(connection, transaction, controlsSql, ids.Parameters))
        await using (OracleDataReader reader = await command.ExecuteReaderAsync(token))
            while (await reader.ReadAsync(token))
            {
                long evaluationId = reader.GetInt64(0), controlId = reader.GetInt64(1);
                if (!controlsByEvaluation.TryGetValue(evaluationId, out Dictionary<long, ControlMonitoreoMatrizDto>? controls))
                    controlsByEvaluation[evaluationId] = controls = new();
                if (!controls.TryGetValue(controlId, out ControlMonitoreoMatrizDto? control))
                {
                    control = new ControlMonitoreoMatrizDto { ControlId=controlId, Tipo=reader.GetString(2), Descripcion=reader.GetString(3),
                        EstadoMonitoreo=NullableText(reader,4), EfectividadMonitoreo=reader.IsDBNull(5)?null:reader.GetDecimal(5), Evidencias=new List<EvidenciaMatrizDto>() };
                    controls.Add(controlId, control);
                }
                if (!reader.IsDBNull(6)) ((List<EvidenciaMatrizDto>)control.Evidencias).Add(new(reader.GetInt64(6), reader.GetString(7), reader.GetDateTime(8)));
            }

        var result = new Dictionary<long, MatrizBloque6Dto>();
        foreach (long evaluationId in ids.Values)
        {
            var current = monitoringState.GetValueOrDefault(evaluationId);
            result[evaluationId] = new MatrizBloque6Dto
            {
                SenalesAlerta=alerts.TryGetValue(evaluationId, out List<SenalAlertaDto>? evaluationAlerts)
                    ? evaluationAlerts : Array.Empty<SenalAlertaDto>(),
                EstadoRiesgo=current.State, ObservacionesArea=current.Area, ObservacionesUgr=current.Ugr,
                PuedeEditarObservacionesArea=capabilities.Contains("MATRICES_RIESGO_OBSERVACIONES_AREA_EDITAR"),
                PuedeEditarObservacionesUgr=capabilities.Contains("MATRICES_RIESGO_OBSERVACIONES_UGR_EDITAR"),
                Controles=controlsByEvaluation.TryGetValue(evaluationId, out Dictionary<long, ControlMonitoreoMatrizDto>? controls)
                    ? controls.Values.OrderBy(control => control.ControlId).ToArray() : Array.Empty<ControlMonitoreoMatrizDto>()
            };
        }
        return result;
    }

    private static InClause BuildInClause(IReadOnlyList<long> values, string prefix)
    {
        var parameters = values.Select((value, index) => ($"{prefix}{index}", (object)value)).ToArray();
        return new(string.Join(",", parameters.Select(parameter => $":{parameter.Item1}")), parameters) { Values = values.ToArray() };
    }

    private sealed record InClause(string Sql, (string Name, object Value)[] Parameters)
    {
        public IReadOnlyList<long> Values { get; init; } = Array.Empty<long>();
    }
    public async Task<InstitutionalProjectionSnapshot?> ReadAsync(long evaluationId, long userId, CancellationToken cancellationToken)
    {
        await using OracleConnection connection = database.CreateConnection();
        // Oracle 11g XE can have an older timezone file than the ODP.NET client.
        // ODP.NET retries with a numeric offset only when that server rejects the client region.
        connection.UseHourOffsetForUnsupportedTimezone = true;
        await connection.OpenAsync(cancellationToken);
        // All query commands explicitly join this serializable transaction. The repository issues
        // SELECT statements only and rolls back, providing one transaction-level Oracle snapshot.
        await using OracleTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        int queryCount = 0;
        try
        {
            EvaluacionRiesgoDto? evaluation = await ReadEvaluationAsync(connection, transaction, evaluationId, cancellationToken);
            queryCount++;
            if (evaluation is null) return null;

            RiesgoDto? risk = await ReadRiskAsync(connection, transaction, evaluation.EvaRiesgoId, cancellationToken);
            queryCount++;
            if (risk is null) return null;
            IReadOnlyList<RiesgoDto> allRisks = await ReadAllRisksAsync(connection, transaction, cancellationToken);
            queryCount++;
            IReadOnlyList<ControlRiesgoDto> controls = await ReadControlsAsync(connection, transaction, evaluationId, cancellationToken);
            queryCount++;
            MitigacionBloque4Dto mitigation = await ReadPlansAsync(connection, transaction, evaluationId, cancellationToken);
            queryCount++;
            MatrizBloque6Dto monitoring = await ReadMonitoringAsync(connection, transaction, evaluationId, userId, cancellationToken);
            queryCount += 4;
            return new(evaluation, risk, allRisks, controls, mitigation, monitoring, queryCount);
        }
        finally
        {
            // Oracle SET TRANSACTION READ ONLY is ended explicitly even when a query or cancellation fails.
            await transaction.RollbackAsync(CancellationToken.None);
        }
    }

    private static async Task<EvaluacionRiesgoDto?> ReadEvaluationAsync(OracleConnection connection, OracleTransaction transaction, long id, CancellationToken token)
    {
        const string sql = @"SELECT e.EVA_ID,e.EVA_RIESGO_ID,e.EVA_VERSION_ID,NVL(f.FLU_ESTADO,'BORRADOR'),
e.EVA_DATOS_JSON,e.EVA_CALCULOS_JSON,p.PROY_VRI,CAST(NULL AS NUMBER),p.PROY_VRR,e.EVA_FECHA_REGISTRO,
e.EVA_USR_REGISTRO,e.EVA_VERSION_ROW,e.EVA_ACTIVO
FROM RL_MR_EVALUACIONES_RIESGO e
LEFT JOIN RL_MR_PROYECCIONES_EVALUACION p ON p.PROY_EVALUACION_ID=e.EVA_ID
LEFT JOIN (SELECT FLU_EVALUACION_ID,FLU_ESTADO FROM (SELECT FLU_EVALUACION_ID,FLU_ESTADO,
ROW_NUMBER() OVER(PARTITION BY FLU_EVALUACION_ID ORDER BY FLU_FECHA DESC,FLU_ID DESC) RN FROM RL_MR_FLUJOS_EVALUACION) WHERE RN=1) f
ON f.FLU_EVALUACION_ID=e.EVA_ID WHERE e.EVA_ID=:id";
        await using OracleCommand command = CreateCommand(connection, transaction, sql, ("id", id));
        await using OracleDataReader reader = await command.ExecuteReaderAsync(token);
        if (!await reader.ReadAsync(token)) return null;
        return new EvaluacionRiesgoDto
        {
            EvaId = reader.GetInt64(0), EvaRiesgoId = reader.GetInt64(1), EvaVersionId = reader.GetInt64(2),
            EvaEstado = reader.GetString(3), EvaDataJson = reader.GetString(4), EvaDataCalcJson = reader.GetString(5),
            EvaVri = reader.IsDBNull(6) ? null : Convert.ToInt32(reader.GetDecimal(6)), EvaEtp = reader.IsDBNull(7) ? null : reader.GetDecimal(7),
            EvaVrr = reader.IsDBNull(8) ? null : Convert.ToInt32(reader.GetDecimal(8)), EvaFechaEval = reader.GetDateTime(9),
            EvaUsrEval = reader.GetInt64(10), EvaVersionRow = Convert.ToInt32(reader.GetDecimal(11)), EvaActivo = reader.GetInt32(12) == 1
        };
    }

    private static async Task<RiesgoDto?> ReadRiskAsync(OracleConnection connection, OracleTransaction transaction, long id, CancellationToken token)
    {
        await using OracleCommand command = CreateCommand(connection, transaction,
            "SELECT RIE_ID,RIE_CODIGO,RIE_NOMBRE,RIE_DESCRIPCION,RIE_ACTIVO,RIE_USR_CREACION,RIE_FECHA_CREACION FROM RL_MR_RIESGOS WHERE RIE_ID=:id", ("id", id));
        await using OracleDataReader reader = await command.ExecuteReaderAsync(token);
        return await reader.ReadAsync(token) ? MapRisk(reader) : null;
    }

    private static async Task<IReadOnlyList<RiesgoDto>> ReadAllRisksAsync(OracleConnection connection, OracleTransaction transaction, CancellationToken token)
    {
        const string sql = "SELECT RIE_ID,RIE_CODIGO,RIE_NOMBRE,RIE_DESCRIPCION,RIE_ACTIVO,RIE_USR_CREACION,RIE_FECHA_CREACION FROM RL_MR_RIESGOS ORDER BY RIE_ID";
        await using OracleCommand command = CreateCommand(connection, transaction, sql);
        await using OracleDataReader reader = await command.ExecuteReaderAsync(token);
        var rows = new List<RiesgoDto>();
        while (await reader.ReadAsync(token)) rows.Add(MapRisk(reader));
        return rows;
    }

    private static RiesgoDto MapRisk(OracleDataReader reader) => new()
    {
        RieId = reader.GetInt64(0), RieCodigo = reader.GetString(1), RieNombre = reader.GetString(2),
        RieDescripcion = reader.IsDBNull(3) ? null : reader.GetString(3), RieActivo = reader.GetInt32(4) == 1,
        RieUsrCreacion = reader.GetInt64(5), RieFechaCreacion = reader.GetDateTime(6)
    };

    private static async Task<IReadOnlyList<ControlRiesgoDto>> ReadControlsAsync(OracleConnection connection, OracleTransaction transaction, long evaluationId, CancellationToken token)
    {
        const string sql = "SELECT CON_ID,CON_EVALUACION_ID,CON_TIPO,CON_DESCRIPCION,CON_AUTOMATIZACION,CON_ESTADO,CON_ESTADO_MONITOREO,CON_EFECTIVIDAD_MONITOREO FROM RL_MR_CONTROLES_RIESGO WHERE CON_EVALUACION_ID=:id ORDER BY CON_ID ASC";
        await using OracleCommand command = CreateCommand(connection, transaction, sql, ("id", evaluationId));
        await using OracleDataReader reader = await command.ExecuteReaderAsync(token);
        var rows = new List<ControlRiesgoDto>();
        while (await reader.ReadAsync(token)) rows.Add(new()
        {
            ConId = reader.GetInt64(0), ConEvaluacionId = reader.GetInt64(1), ConTipo = reader.GetString(2),
            ConDescripcion = reader.GetString(3), ConAutomatizacion = reader.GetString(4), ConEstado = reader.GetString(5),
            ConEstadoMonitoreo = reader.IsDBNull(6) ? null : reader.GetString(6), ConEfectividadMonitoreo = reader.IsDBNull(7) ? null : reader.GetDecimal(7)
        });
        return rows;
    }

    private static async Task<MitigacionBloque4Dto> ReadPlansAsync(OracleConnection connection, OracleTransaction transaction, long evaluationId, CancellationToken token)
    {
        const string sql = @"SELECT p.PLA_ID,p.PLA_EVALUACION_ID,p.PLA_DESCRIPCION,p.PLA_AVANCE,p.PLA_PRESUPUESTO,
p.PLA_FECHA_INICIO,p.PLA_FECHA_FIN,p.PLA_ESTADO,p.PLA_MONITOREO_SEGUIMIENTO,p.PLA_RESPONSABLES,p.PLA_RECURSOS,
a.ACT_ID,a.ACT_PLAN_ID,a.ACT_DESCRIPCION,a.ACT_RESPONSABLE,a.ACT_AVANCE,a.ACT_FECHA_INICIO,a.ACT_FECHA_FIN,a.ACT_ESTADO
FROM RL_MR_PLANES p LEFT JOIN RL_MR_ACTIVIDADES a ON a.ACT_PLAN_ID=p.PLA_ID
WHERE p.PLA_EVALUACION_ID=:id ORDER BY p.PLA_ID ASC,a.ACT_ID ASC";
        await using OracleCommand command = CreateCommand(connection, transaction, sql, ("id", evaluationId));
        await using OracleDataReader reader = await command.ExecuteReaderAsync(token);
        var plans = new List<PlanMitigacionDto>();
        var byId = new Dictionary<long, PlanMitigacionDto>();
        while (await reader.ReadAsync(token))
        {
            long id = reader.GetInt64(0);
            if (!byId.TryGetValue(id, out PlanMitigacionDto? plan))
            {
                plan = new PlanMitigacionDto { PlaId=id, PlaEvaluacionId=reader.GetInt64(1), PlaDescripcion=reader.GetString(2), PlaAvance=reader.GetDecimal(3),
                    PlaPresupuesto=reader.GetDecimal(4), PlaFechaInicio=reader.GetDateTime(5), PlaFechaFin=reader.GetDateTime(6), PlaEstado=reader.GetString(7),
                    PlaMonitoreoSeguimiento=NullableText(reader,8), PlaResponsables=NullableText(reader,9), PlaRecursos=NullableText(reader,10), Actividades=new List<ActividadPlanDto>() };
                plans.Add(plan); byId.Add(id,plan);
            }
            if (!reader.IsDBNull(11))
            {
                ((List<ActividadPlanDto>)plan.Actividades).Add(new() { ActId=reader.GetInt64(11), ActPlanId=reader.GetInt64(12), ActDescripcion=reader.GetString(13),
                    ActResponsable=reader.GetString(14), ActAvance=reader.GetDecimal(15), ActFechaInicio=reader.GetDateTime(16), ActFechaFin=reader.GetDateTime(17), ActEstado=reader.GetString(18) });
                plan.CantidadActividades++;
            }
        }
        return new() { CantidadAcciones=plans.Count, Planes=plans };
    }

    private static async Task<MatrizBloque6Dto> ReadMonitoringAsync(OracleConnection connection, OracleTransaction transaction, long evaluationId, long userId, CancellationToken token)
    {
        var alerts = new List<SenalAlertaDto>();
        await using (OracleCommand command = CreateCommand(connection, transaction, "SELECT ALE_ID,ALE_EVALUACION_ID,ALE_CODIGO,ALE_INDICADOR,ALE_ESTADO,ALE_FECHA_DISPARO FROM RL_MR_SENALES_ALERTA WHERE ALE_EVALUACION_ID=:id ORDER BY ALE_ID", ("id", evaluationId)))
        await using (OracleDataReader reader = await command.ExecuteReaderAsync(token))
            while (await reader.ReadAsync(token)) alerts.Add(new() { AleId=reader.GetInt64(0), AleEvaluacionId=reader.GetInt64(1), AleCodigo=reader.GetString(2), AleIndicador=reader.GetString(3), AleEstado=reader.GetString(4), AleFechaDisparo=reader.IsDBNull(5)?null:reader.GetDateTime(5) });

        string? state = null, area = null, ugr = null;
        await using (OracleCommand command = CreateCommand(connection, transaction, "SELECT MON_ESTADO_RIESGO,MON_OBSERVACIONES_AREA,MON_OBSERVACIONES_UGR FROM (SELECT MON_ESTADO_RIESGO,MON_OBSERVACIONES_AREA,MON_OBSERVACIONES_UGR FROM RL_MR_AUTOMONITOREO WHERE MON_EVALUACION_ID=:id ORDER BY MON_FECHA DESC,MON_ID DESC) WHERE ROWNUM=1", ("id", evaluationId)))
        await using (OracleDataReader reader = await command.ExecuteReaderAsync(token))
            if (await reader.ReadAsync(token)) { state=NullableText(reader,0); area=NullableText(reader,1); ugr=NullableText(reader,2); }

        var controls = new Dictionary<long, ControlMonitoreoMatrizDto>();
        await using (OracleCommand command = CreateCommand(connection, transaction, @"SELECT c.CON_ID,c.CON_TIPO,c.CON_DESCRIPCION,c.CON_ESTADO_MONITOREO,c.CON_EFECTIVIDAD_MONITOREO,e.EVI_ID,e.EVI_NOMBRE_ARCHIVO,e.EVI_FECHA_CREACION
FROM RL_MR_CONTROLES_RIESGO c LEFT JOIN RL_MR_EVIDENCIAS_VINCULOS v ON v.EVV_TIPO_ENTIDAD='CONTROL' AND v.EVV_ENTIDAD_ID=c.CON_ID
LEFT JOIN RL_MR_EVIDENCIAS e ON e.EVI_ID=v.EVV_EVIDENCIA_ID WHERE c.CON_EVALUACION_ID=:id
ORDER BY c.CON_ID ASC,e.EVI_FECHA_CREACION ASC,e.EVI_ID ASC", ("id", evaluationId)))
        await using (OracleDataReader reader = await command.ExecuteReaderAsync(token))
        {
            while (await reader.ReadAsync(token))
            {
                long controlId=reader.GetInt64(0);
                if (!controls.TryGetValue(controlId,out ControlMonitoreoMatrizDto? control))
                {
                    control=new() { ControlId=controlId,Tipo=reader.GetString(1),Descripcion=reader.GetString(2),EstadoMonitoreo=NullableText(reader,3),EfectividadMonitoreo=reader.IsDBNull(4)?null:reader.GetDecimal(4),Evidencias=new List<EvidenciaMatrizDto>() };
                    controls.Add(controlId,control);
                }
                if (!reader.IsDBNull(5)) ((List<EvidenciaMatrizDto>)control.Evidencias).Add(new(reader.GetInt64(5),reader.GetString(6),reader.GetDateTime(7)));
            }
        }

        var capabilities = new HashSet<string>(StringComparer.Ordinal);
        await using (OracleCommand command = CreateCommand(connection, transaction,"SELECT UCP_CAPACIDAD FROM RL_USUARIO_CAPACIDADES WHERE UCP_USR_ID=:userId AND UCP_ACTIVO=1",("userId",userId)))
        await using (OracleDataReader reader = await command.ExecuteReaderAsync(token))
            while (await reader.ReadAsync(token)) capabilities.Add(reader.GetString(0));
        return new MatrizBloque6Dto { SenalesAlerta=alerts, EstadoRiesgo=state, ObservacionesArea=area, ObservacionesUgr=ugr,
            PuedeEditarObservacionesArea=capabilities.Contains("MATRICES_RIESGO_OBSERVACIONES_AREA_EDITAR"), PuedeEditarObservacionesUgr=capabilities.Contains("MATRICES_RIESGO_OBSERVACIONES_UGR_EDITAR"), Controles=controls.Values.ToArray() };
    }

    private static OracleCommand CreateCommand(OracleConnection connection, OracleTransaction transaction, string sql, params (string Name, object Value)[] parameters)
    {
        var command = new OracleCommand(sql, connection) { BindByName=true, Transaction=transaction };
        foreach ((string name, object value) in parameters) command.Parameters.Add(new OracleParameter(name,value));
        return command;
    }

    private static string? NullableText(OracleDataReader reader, int index) => reader.IsDBNull(index) ? null : reader.GetString(index);
}
