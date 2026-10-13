using Oracle.ManagedDataAccess.Client;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Infrastructure.Database;

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
}

/// <summary>Loads all data for the canonical read model from one Oracle read-only transaction.</summary>
public sealed class InstitutionalProjectionReadRepository(OracleDbContext database) : IInstitutionalProjectionReadRepository
{
    public async Task<InstitutionalProjectionSnapshot?> ReadAsync(long evaluationId, long userId, CancellationToken cancellationToken)
    {
        await using OracleConnection connection = database.CreateConnection();
        // Oracle 11g XE can have an older timezone file than the ODP.NET client.
        // ODP.NET retries with a numeric offset only when that server rejects the client region.
        connection.UseHourOffsetForUnsupportedTimezone = true;
        await connection.OpenAsync(cancellationToken);
        // Oracle XE 11g did not preserve one snapshot across statements with READ ONLY in the
        // concurrent characterization. SERIALIZABLE provides transaction-level read consistency;
        // this repository issues SELECT statements only and always rolls the transaction back.
        await ExecuteAsync(connection, "SET TRANSACTION ISOLATION LEVEL SERIALIZABLE", cancellationToken);
        int queryCount = 0;
        try
        {
            EvaluacionRiesgoDto? evaluation = await ReadEvaluationAsync(connection, evaluationId, cancellationToken);
            queryCount++;
            if (evaluation is null) return null;

            RiesgoDto? risk = await ReadRiskAsync(connection, evaluation.EvaRiesgoId, cancellationToken);
            queryCount++;
            if (risk is null) return null;
            IReadOnlyList<RiesgoDto> allRisks = await ReadAllRisksAsync(connection, cancellationToken);
            queryCount++;
            IReadOnlyList<ControlRiesgoDto> controls = await ReadControlsAsync(connection, evaluationId, cancellationToken);
            queryCount++;
            MitigacionBloque4Dto mitigation = await ReadPlansAsync(connection, evaluationId, cancellationToken);
            queryCount++;
            MatrizBloque6Dto monitoring = await ReadMonitoringAsync(connection, evaluationId, userId, cancellationToken);
            queryCount += 4;
            return new(evaluation, risk, allRisks, controls, mitigation, monitoring, queryCount);
        }
        finally
        {
            // Oracle SET TRANSACTION READ ONLY is ended explicitly even when a query or cancellation fails.
            await ExecuteAsync(connection, "ROLLBACK", CancellationToken.None);
        }
    }

    private static async Task<EvaluacionRiesgoDto?> ReadEvaluationAsync(OracleConnection connection, long id, CancellationToken token)
    {
        const string sql = @"SELECT e.EVA_ID,e.EVA_RIESGO_ID,e.EVA_VERSION_ID,NVL(f.FLU_ESTADO,'BORRADOR'),
e.EVA_DATOS_JSON,e.EVA_CALCULOS_JSON,p.PROY_VRI,CAST(NULL AS NUMBER),p.PROY_VRR,e.EVA_FECHA_REGISTRO,
e.EVA_USR_REGISTRO,e.EVA_VERSION_ROW,e.EVA_ACTIVO
FROM RL_MR_EVALUACIONES_RIESGO e
LEFT JOIN RL_MR_PROYECCIONES_EVALUACION p ON p.PROY_EVALUACION_ID=e.EVA_ID
LEFT JOIN (SELECT FLU_EVALUACION_ID,FLU_ESTADO FROM (SELECT FLU_EVALUACION_ID,FLU_ESTADO,
ROW_NUMBER() OVER(PARTITION BY FLU_EVALUACION_ID ORDER BY FLU_FECHA DESC,FLU_ID DESC) RN FROM RL_MR_FLUJOS_EVALUACION) WHERE RN=1) f
ON f.FLU_EVALUACION_ID=e.EVA_ID WHERE e.EVA_ID=:id";
        await using OracleCommand command = CreateCommand(connection, sql, ("id", id));
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

    private static async Task<RiesgoDto?> ReadRiskAsync(OracleConnection connection, long id, CancellationToken token)
    {
        await using OracleCommand command = CreateCommand(connection,
            "SELECT RIE_ID,RIE_CODIGO,RIE_NOMBRE,RIE_DESCRIPCION,RIE_ACTIVO,RIE_USR_CREACION,RIE_FECHA_CREACION FROM RL_MR_RIESGOS WHERE RIE_ID=:id", ("id", id));
        await using OracleDataReader reader = await command.ExecuteReaderAsync(token);
        return await reader.ReadAsync(token) ? MapRisk(reader) : null;
    }

    private static async Task<IReadOnlyList<RiesgoDto>> ReadAllRisksAsync(OracleConnection connection, CancellationToken token)
    {
        const string sql = "SELECT RIE_ID,RIE_CODIGO,RIE_NOMBRE,RIE_DESCRIPCION,RIE_ACTIVO,RIE_USR_CREACION,RIE_FECHA_CREACION FROM RL_MR_RIESGOS ORDER BY RIE_ID";
        await using OracleCommand command = CreateCommand(connection, sql);
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

    private static async Task<IReadOnlyList<ControlRiesgoDto>> ReadControlsAsync(OracleConnection connection, long evaluationId, CancellationToken token)
    {
        const string sql = "SELECT CON_ID,CON_EVALUACION_ID,CON_TIPO,CON_DESCRIPCION,CON_AUTOMATIZACION,CON_ESTADO,CON_ESTADO_MONITOREO,CON_EFECTIVIDAD_MONITOREO FROM RL_MR_CONTROLES_RIESGO WHERE CON_EVALUACION_ID=:id ORDER BY CON_ID ASC";
        await using OracleCommand command = CreateCommand(connection, sql, ("id", evaluationId));
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

    private static async Task<MitigacionBloque4Dto> ReadPlansAsync(OracleConnection connection, long evaluationId, CancellationToken token)
    {
        const string sql = @"SELECT p.PLA_ID,p.PLA_EVALUACION_ID,p.PLA_DESCRIPCION,p.PLA_AVANCE,p.PLA_PRESUPUESTO,
p.PLA_FECHA_INICIO,p.PLA_FECHA_FIN,p.PLA_ESTADO,p.PLA_MONITOREO_SEGUIMIENTO,p.PLA_RESPONSABLES,p.PLA_RECURSOS,
a.ACT_ID,a.ACT_PLAN_ID,a.ACT_DESCRIPCION,a.ACT_RESPONSABLE,a.ACT_AVANCE,a.ACT_FECHA_INICIO,a.ACT_FECHA_FIN,a.ACT_ESTADO
FROM RL_MR_PLANES p LEFT JOIN RL_MR_ACTIVIDADES a ON a.ACT_PLAN_ID=p.PLA_ID
WHERE p.PLA_EVALUACION_ID=:id ORDER BY p.PLA_ID ASC,a.ACT_ID ASC";
        await using OracleCommand command = CreateCommand(connection, sql, ("id", evaluationId));
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

    private static async Task<MatrizBloque6Dto> ReadMonitoringAsync(OracleConnection connection, long evaluationId, long userId, CancellationToken token)
    {
        var alerts = new List<SenalAlertaDto>();
        await using (OracleCommand command = CreateCommand(connection, "SELECT ALE_ID,ALE_EVALUACION_ID,ALE_CODIGO,ALE_INDICADOR,ALE_ESTADO,ALE_FECHA_DISPARO FROM RL_MR_SENALES_ALERTA WHERE ALE_EVALUACION_ID=:id ORDER BY ALE_ID", ("id", evaluationId)))
        await using (OracleDataReader reader = await command.ExecuteReaderAsync(token))
            while (await reader.ReadAsync(token)) alerts.Add(new() { AleId=reader.GetInt64(0), AleEvaluacionId=reader.GetInt64(1), AleCodigo=reader.GetString(2), AleIndicador=reader.GetString(3), AleEstado=reader.GetString(4), AleFechaDisparo=reader.IsDBNull(5)?null:reader.GetDateTime(5) });

        string? state = null, area = null, ugr = null;
        await using (OracleCommand command = CreateCommand(connection, "SELECT MON_ESTADO_RIESGO,MON_OBSERVACIONES_AREA,MON_OBSERVACIONES_UGR FROM (SELECT MON_ESTADO_RIESGO,MON_OBSERVACIONES_AREA,MON_OBSERVACIONES_UGR FROM RL_MR_AUTOMONITOREO WHERE MON_EVALUACION_ID=:id ORDER BY MON_FECHA DESC,MON_ID DESC) WHERE ROWNUM=1", ("id", evaluationId)))
        await using (OracleDataReader reader = await command.ExecuteReaderAsync(token))
            if (await reader.ReadAsync(token)) { state=NullableText(reader,0); area=NullableText(reader,1); ugr=NullableText(reader,2); }

        var controls = new Dictionary<long, ControlMonitoreoMatrizDto>();
        await using (OracleCommand command = CreateCommand(connection, @"SELECT c.CON_ID,c.CON_TIPO,c.CON_DESCRIPCION,c.CON_ESTADO_MONITOREO,c.CON_EFECTIVIDAD_MONITOREO,e.EVI_ID,e.EVI_NOMBRE_ARCHIVO,e.EVI_FECHA_CREACION
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
        await using (OracleCommand command = CreateCommand(connection,"SELECT UCP_CAPACIDAD FROM RL_USUARIO_CAPACIDADES WHERE UCP_USR_ID=:userId AND UCP_ACTIVO=1",("userId",userId)))
        await using (OracleDataReader reader = await command.ExecuteReaderAsync(token))
            while (await reader.ReadAsync(token)) capabilities.Add(reader.GetString(0));
        return new MatrizBloque6Dto { SenalesAlerta=alerts, EstadoRiesgo=state, ObservacionesArea=area, ObservacionesUgr=ugr,
            PuedeEditarObservacionesArea=capabilities.Contains("MATRICES_RIESGO_OBSERVACIONES_AREA_EDITAR"), PuedeEditarObservacionesUgr=capabilities.Contains("MATRICES_RIESGO_OBSERVACIONES_UGR_EDITAR"), Controles=controls.Values.ToArray() };
    }

    private static OracleCommand CreateCommand(OracleConnection connection, string sql, params (string Name, object Value)[] parameters)
    {
        var command = new OracleCommand(sql, connection) { BindByName=true };
        foreach ((string name, object value) in parameters) command.Parameters.Add(new OracleParameter(name,value));
        return command;
    }

    private static async Task ExecuteAsync(OracleConnection connection, string sql, CancellationToken token)
    {
        await using OracleCommand command = new(sql, connection) { BindByName=true };
        await command.ExecuteNonQueryAsync(token);
    }

    private static string? NullableText(OracleDataReader reader, int index) => reader.IsDBNull(index) ? null : reader.GetString(index);
}
