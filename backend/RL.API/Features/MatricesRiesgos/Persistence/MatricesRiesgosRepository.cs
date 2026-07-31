using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Infrastructure.Database;

namespace RL.API.Features.MatricesRiesgos.Persistence;

public sealed class MatricesRiesgosRepository : IMatricesRiesgosRepository
{
    private const string ModuloAuditoria = "MatricesRiesgos";
    private readonly OracleDbContext _db;

    public MatricesRiesgosRepository(OracleDbContext db)
    {
        _db = db;
    }

    // ============================================================
    // 1. GESTIÓN DEL CICLO DE VIDA DEL FORMULARIO Y VERSIONES
    // ============================================================

    public async Task<VersionFormularioDto?> ObtenerVersionVigenteFormularioAsync(string familiaCodigo)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT v.VER_ID, v.VER_FAMILIA_ID, v.VER_CODIGO, v.VER_VERSION, v.VER_JSON, 
                   v.VER_HASH, v.VER_ESTADO, v.VER_VIGENTE, v.VER_FECHA_INICIO, v.VER_FECHA_FIN, 
                   v.VER_FECHA_CREACION, v.VER_USR_CREACION
              FROM RL_MR_VERSIONES_FORMULARIO v
              JOIN RL_MR_FAMILIAS_FORMULARIO f ON v.VER_FAMILIA_ID = f.FAM_ID
             WHERE f.FAM_CODIGO = :familiaCodigo 
               AND v.VER_VIGENTE = 1";

        await using var cmd = new OracleCommand(sql, conn);
        cmd.Parameters.Add(new OracleParameter("familiaCodigo", familiaCodigo));

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapearVersionFormulario(reader);
        }
        return null;
    }

    public async Task<VersionFormularioDto?> ObtenerVersionFormularioAsync(long versionId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT VER_ID, VER_FAMILIA_ID, VER_CODIGO, VER_VERSION, VER_JSON, 
                   VER_HASH, VER_ESTADO, VER_VIGENTE, VER_FECHA_INICIO, VER_FECHA_FIN, 
                   VER_FECHA_CREACION, VER_USR_CREACION
              FROM RL_MR_VERSIONES_FORMULARIO
             WHERE VER_ID = :versionId";

        await using var cmd = new OracleCommand(sql, conn);
        cmd.Parameters.Add(new OracleParameter("versionId", versionId));

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapearVersionFormulario(reader);
        }
        return null;
    }

    public async Task<long> CrearBorradorFormularioAsync(long familiaId, string codigoFormulario, string jsonConfig, long usuarioId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        // Determinar siguiente número de versión
        const string sqlMax = "SELECT NVL(MAX(VER_VERSION), 0) + 1 FROM RL_MR_VERSIONES_FORMULARIO WHERE VER_FAMILIA_ID = :familiaId";
        await using var cmdMax = new OracleCommand(sqlMax, conn);
        cmdMax.Parameters.Add(new OracleParameter("familiaId", familiaId));
        int siguienteVersion = Convert.ToInt32(await cmdMax.ExecuteScalarAsync());

        // Obtener siguiente ID de secuencia
        const string sqlSeq = "SELECT SEQ_RL_MR_VERSIONES.NEXTVAL FROM DUAL";
        await using var cmdSeq = new OracleCommand(sqlSeq, conn);
        long nuevoId = Convert.ToInt64(await cmdSeq.ExecuteScalarAsync());

        const string sqlInsert = @"
            INSERT INTO RL_MR_VERSIONES_FORMULARIO (
                VER_ID, VER_FAMILIA_ID, VER_CODIGO, VER_VERSION, VER_JSON, VER_HASH, 
                VER_ESTADO, VER_VIGENTE, VER_USR_CREACION
            ) VALUES (
                :verId, :familiaId, :codigoFormulario, :version, :jsonConfig, 'DRAFT_HASH', 
                'DRAFT', 0, :usuarioId
            )";

        await using var cmd = new OracleCommand(sqlInsert, conn);
        cmd.Parameters.Add(new OracleParameter("verId", nuevoId));
        cmd.Parameters.Add(new OracleParameter("familiaId", familiaId));
        cmd.Parameters.Add(new OracleParameter("codigoFormulario", codigoFormulario));
        cmd.Parameters.Add(new OracleParameter("version", siguienteVersion));
        cmd.Parameters.Add(new OracleParameter("jsonConfig", OracleDbType.Clob) { Value = jsonConfig });
        cmd.Parameters.Add(new OracleParameter("usuarioId", usuarioId));

        await cmd.ExecuteNonQueryAsync();
        return nuevoId;
    }

    public async Task<long> ClonarVersionFormularioAsync(long versionOrigenId, long usuarioId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        // Obtener datos origen
        var origen = await ObtenerVersionFormularioAsync(versionOrigenId);
        if (origen == null)
        {
            throw new KeyNotFoundException($"No se encontró la versión origen ID {versionOrigenId}");
        }

        return await CrearBorradorFormularioAsync(origen.VerFamiliaId, origen.VerCodigo, origen.VerJson, usuarioId);
    }

    public async Task<bool> ActualizarBorradorFormularioAsync(long versionId, string jsonConfig, string hash, long usuarioId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            UPDATE RL_MR_VERSIONES_FORMULARIO
               SET VER_JSON = :jsonConfig,
                   VER_HASH = :hash
             WHERE VER_ID = :versionId 
               AND VER_ESTADO = 'DRAFT'";

        await using var cmd = new OracleCommand(sql, conn);
        cmd.Parameters.Add(new OracleParameter("jsonConfig", OracleDbType.Clob) { Value = jsonConfig });
        cmd.Parameters.Add(new OracleParameter("hash", hash));
        cmd.Parameters.Add(new OracleParameter("versionId", versionId));

        int filas = await cmd.ExecuteNonQueryAsync();
        return filas > 0;
    }

    public async Task<bool> PublicarVersionFormularioAsync(long versionId, string hash, long usuarioId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var trans = conn.BeginTransaction();

        try
        {
            // 1. Obtener la versión borrador
            const string sqlSelect = "SELECT VER_FAMILIA_ID FROM RL_MR_VERSIONES_FORMULARIO WHERE VER_ID = :versionId AND VER_ESTADO = 'DRAFT'";
            await using var cmdSelect = new OracleCommand(sqlSelect, conn);
            cmdSelect.Parameters.Add(new OracleParameter("versionId", versionId));
            var familiaIdObj = await cmdSelect.ExecuteScalarAsync();
            if (familiaIdObj == null)
            {
                await trans.RollbackAsync();
                return false;
            }
            long familiaId = Convert.ToInt64(familiaIdObj);

            // 2. Apagar la vigencia de cualquier versión anterior de la misma familia
            const string sqlApagar = "UPDATE RL_MR_VERSIONES_FORMULARIO SET VER_VIGENTE = 0, VER_FECHA_FIN = SYSDATE WHERE VER_FAMILIA_ID = :familiaId AND VER_VIGENTE = 1";
            await using var cmdApagar = new OracleCommand(sqlApagar, conn);
            cmdApagar.Parameters.Add(new OracleParameter("familiaId", familiaId));
            await cmdApagar.ExecuteNonQueryAsync();

            // 3. Publicar y activar vigencia de la nueva versión
            const string sqlPublicar = @"
                UPDATE RL_MR_VERSIONES_FORMULARIO
                   SET VER_ESTADO = 'PUBLISHED',
                       VER_VIGENTE = 1,
                       VER_HASH = :hash,
                       VER_FECHA_INICIO = SYSDATE
                 WHERE VER_ID = :versionId";
            await using var cmdPublicar = new OracleCommand(sqlPublicar, conn);
            cmdPublicar.Parameters.Add(new OracleParameter("hash", hash));
            cmdPublicar.Parameters.Add(new OracleParameter("versionId", versionId));
            await cmdPublicar.ExecuteNonQueryAsync();

            await trans.CommitAsync();
            return true;
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> CambiarEstadoVigenciaFormularioAsync(long versionId, bool vigente, long usuarioId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        const string sql = "UPDATE RL_MR_VERSIONES_FORMULARIO SET VER_VIGENTE = :vigente WHERE VER_ID = :versionId";
        await using var cmd = new OracleCommand(sql, conn);
        cmd.Parameters.Add(new OracleParameter("vigente", vigente ? 1 : 0));
        cmd.Parameters.Add(new OracleParameter("versionId", versionId));

        int filas = await cmd.ExecuteNonQueryAsync();
        return filas > 0;
    }

    public async Task<List<VersionFormularioDto>> ListarHistorialVersionesFormularioAsync(string familiaCodigo)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT v.VER_ID, v.VER_FAMILIA_ID, v.VER_CODIGO, v.VER_VERSION, v.VER_JSON, 
                   v.VER_HASH, v.VER_ESTADO, v.VER_VIGENTE, v.VER_FECHA_INICIO, v.VER_FECHA_FIN, 
                   v.VER_FECHA_CREACION, v.VER_USR_CREACION
              FROM RL_MR_VERSIONES_FORMULARIO v
              JOIN RL_MR_FAMILIAS_FORMULARIO f ON v.VER_FAMILIA_ID = f.FAM_ID
             WHERE f.FAM_CODIGO = :familiaCodigo
             ORDER BY v.VER_VERSION DESC";

        await using var cmd = new OracleCommand(sql, conn);
        cmd.Parameters.Add(new OracleParameter("familiaCodigo", familiaCodigo));

        var lista = new List<VersionFormularioDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(MapearVersionFormulario(reader));
        }
        return lista;
    }

    // ============================================================
    // 2. GESTIÓN DE EVALUACIONES E HISTORIAL DE CAMBIOS
    // ============================================================

    public async Task<EvaluacionRiesgoDto?> ObtenerEvaluacionAsync(long evaId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT EVA_ID, EVA_RIESGO_ID, EVA_VERSION_ID, EVA_ESTADO, EVA_DATA_JSON, 
                   EVA_DATA_CALC_JSON, EVA_VRI, EVA_ETP, EVA_VRR, EVA_FECHA_EVAL, 
                   EVA_USR_EVAL, EVA_VERSION_ROW, EVA_ACTIVO
              FROM RL_MR_EVALUACIONES_RIESGO
             WHERE EVA_ID = :evaId";

        await using var cmd = new OracleCommand(sql, conn);
        cmd.Parameters.Add(new OracleParameter("evaId", evaId));

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapearEvaluacion(reader);
        }
        return null;
    }

    public async Task<List<EvaluacionRiesgoDto>> ListarEvaluacionesPaginadasAsync(ConsultaEvaluacionPaginadaDto filtro)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        var sql = new System.Text.StringBuilder();
        sql.Append(@"
            SELECT e.EVA_ID, e.EVA_RIESGO_ID, e.EVA_VERSION_ID, e.EVA_ESTADO, e.EVA_DATA_JSON, 
                   e.EVA_DATA_CALC_JSON, e.EVA_VRI, e.EVA_ETP, e.EVA_VRR, e.EVA_FECHA_EVAL, 
                   e.EVA_USR_EVAL, e.EVA_VERSION_ROW, e.EVA_ACTIVO
              FROM RL_MR_EVALUACIONES_RIESGO e
              JOIN RL_MR_PROYECCIONES_EVALUACION p ON e.EVA_ID = p.PROY_EVALUACION_ID
             WHERE e.EVA_ACTIVO = 1 ");

        var parameters = new List<OracleParameter>();

        if (filtro.RiesgoId.HasValue)
        {
            sql.Append(" AND e.EVA_RIESGO_ID = :riesgoId");
            parameters.Add(new OracleParameter("riesgoId", filtro.RiesgoId.Value));
        }
        if (!string.IsNullOrWhiteSpace(filtro.Estado))
        {
            sql.Append(" AND e.EVA_ESTADO = :estado");
            parameters.Add(new OracleParameter("estado", filtro.Estado.ToUpperInvariant()));
        }
        if (!string.IsNullOrWhiteSpace(filtro.Area))
        {
            sql.Append(" AND p.PROY_AREA_PRINCIPAL = :area");
            parameters.Add(new OracleParameter("area", filtro.Area));
        }
        if (!string.IsNullOrWhiteSpace(filtro.NivelResidual))
        {
            sql.Append(" AND p.PROY_NIVEL_RESIDUAL = :nivel");
            parameters.Add(new OracleParameter("nivel", filtro.NivelResidual));
        }

        // Paginación en Oracle 11g
        int offset = (filtro.Pagina - 1) * filtro.RegistrosPorPagina;
        int limit = filtro.RegistrosPorPagina;

        string queryPaginada = $@"
            SELECT * FROM (
                SELECT a.*, ROWNUM rnum FROM (
                    {sql}
                    ORDER BY e.EVA_FECHA_EVAL DESC
                ) a WHERE ROWNUM <= :maxRow
            ) WHERE rnum > :minRow";

        await using var cmd = new OracleCommand(queryPaginada, conn);
        foreach (var p in parameters)
        {
            cmd.Parameters.Add(p);
        }
        cmd.Parameters.Add(new OracleParameter("maxRow", offset + limit));
        cmd.Parameters.Add(new OracleParameter("minRow", offset));

        var lista = new List<EvaluacionRiesgoDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(MapearEvaluacion(reader));
        }
        return lista;
    }

    public async Task<long> CrearEvaluacionAsync(EvaluacionRiesgoDto dto, long usuarioId, string? ip)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var trans = conn.BeginTransaction();

        try
        {
            // 1. Obtener siguiente ID de secuencia para Evaluación
            const string sqlSeq = "SELECT SEQ_RL_MR_EVALUACIONES.NEXTVAL FROM DUAL";
            await using var cmdSeq = new OracleCommand(sqlSeq, conn);
            long nuevoEvaId = Convert.ToInt64(await cmdSeq.ExecuteScalarAsync());

            // 2. Insertar en RL_MR_EVALUACIONES_RIESGO
            const string sqlInsert = @"
                INSERT INTO RL_MR_EVALUACIONES_RIESGO (
                    EVA_ID, EVA_RIESGO_ID, EVA_VERSION_ID, EVA_ESTADO, EVA_DATA_JSON, 
                    EVA_DATA_CALC_JSON, EVA_VRI, EVA_ETP, EVA_VRR, EVA_FECHA_EVAL, 
                    EVA_USR_EVAL, EVA_VERSION_ROW, EVA_ACTIVO
                ) VALUES (
                    :evaId, :riesgoId, :versionId, 'BORRADOR', :dataJson, 
                    :dataCalcJson, :vri, :etp, :vrr, SYSDATE, 
                    :usuarioId, 1, 1
                )";

            await using var cmdInsert = new OracleCommand(sqlInsert, conn);
            cmdInsert.Parameters.Add(new OracleParameter("evaId", nuevoEvaId));
            cmdInsert.Parameters.Add(new OracleParameter("riesgoId", dto.EvaRiesgoId));
            cmdInsert.Parameters.Add(new OracleParameter("versionId", dto.EvaVersionId));
            cmdInsert.Parameters.Add(new OracleParameter("dataJson", OracleDbType.Clob) { Value = dto.EvaDataJson });
            cmdInsert.Parameters.Add(new OracleParameter("dataCalcJson", OracleDbType.Clob) { Value = dto.EvaDataCalcJson });
            cmdInsert.Parameters.Add(new OracleParameter("vri", dto.EvaVri ?? (object)DBNull.Value));
            cmdInsert.Parameters.Add(new OracleParameter("etp", dto.EvaEtp ?? (object)DBNull.Value));
            cmdInsert.Parameters.Add(new OracleParameter("vrr", dto.EvaVrr ?? (object)DBNull.Value));
            cmdInsert.Parameters.Add(new OracleParameter("usuarioId", usuarioId));

            await cmdInsert.ExecuteNonQueryAsync();

            // Parsear campos planos de respuestas para guardarlos en la proyección relacional
            var answers = MapearDiccionario(dto.EvaDataJson);
            string area = answers.TryGetValue("area_principal", out object? aVal) ? aVal?.ToString() ?? "N/D" : "N/D";
            string due = answers.TryGetValue("dueno_riesgo", out object? dVal) ? dVal?.ToString() ?? "N/D" : "N/D";
            
            // Obtener código de riesgo permanente
            const string sqlRieCod = "SELECT RIE_CODIGO FROM RL_MR_RIESGOS WHERE RIE_ID = :riesgoId";
            await using var cmdRieCod = new OracleCommand(sqlRieCod, conn);
            cmdRieCod.Parameters.Add(new OracleParameter("riesgoId", dto.EvaRiesgoId));
            string codigoRiesgo = (await cmdRieCod.ExecuteScalarAsync())?.ToString() ?? "RIE_N/D";

            // Determinar clasificación textual
            string nivel = DeterminarClasificacionResidual(dto.EvaVrr);

            // 3. Crear Proyección Plana
            const string sqlProj = @"
                INSERT INTO RL_MR_PROYECCIONES_EVALUACION (
                    PROY_ID, PROY_EVALUACION_ID, PROY_CODIGO_RIESGO, PROY_ESTADO_EVALUACION, 
                    PROY_VRI, PROY_ETP, PROY_VRR, PROY_NIVEL_RESIDUAL, 
                    PROY_AREA_PRINCIPAL, PROY_DUENO_RIESGO, PROY_FECHA_EVAL
                ) VALUES (
                    SEQ_RL_MR_PROYECCIONES.NEXTVAL, :evaId, :codigoRiesgo, 'BORRADOR',
                    :vri, :etp, :vrr, :nivel,
                    :area, :due, SYSDATE
                )";

            await using var cmdProj = new OracleCommand(sqlProj, conn);
            cmdProj.Parameters.Add(new OracleParameter("evaId", nuevoEvaId));
            cmdProj.Parameters.Add(new OracleParameter("codigoRiesgo", codigoRiesgo));
            cmdProj.Parameters.Add(new OracleParameter("vri", dto.EvaVri ?? (object)DBNull.Value));
            cmdProj.Parameters.Add(new OracleParameter("etp", dto.EvaEtp ?? (object)DBNull.Value));
            cmdProj.Parameters.Add(new OracleParameter("vrr", dto.EvaVrr ?? (object)DBNull.Value));
            cmdProj.Parameters.Add(new OracleParameter("nivel", nivel));
            cmdProj.Parameters.Add(new OracleParameter("area", area));
            cmdProj.Parameters.Add(new OracleParameter("due", due));
            await cmdProj.ExecuteNonQueryAsync();

            // 4. Registrar Flujo de Estados
            const string sqlFlujo = @"
                INSERT INTO RL_MR_FLUJOS_EVALUACION (
                    FLU_ID, FLU_EVALUACION_ID, FLU_ESTADO_ANTERIOR, FLU_ESTADO_NUEVO, FLU_FECHA, FLU_USR_ID
                ) VALUES (
                    SEQ_RL_MR_FLUJOS.NEXTVAL, :evaId, NULL, 'BORRADOR', SYSDATE, :usuarioId
                )";
            await using var cmdFlujo = new OracleCommand(sqlFlujo, conn);
            cmdFlujo.Parameters.Add(new OracleParameter("evaId", nuevoEvaId));
            cmdFlujo.Parameters.Add(new OracleParameter("usuarioId", usuarioId));
            await cmdFlujo.ExecuteNonQueryAsync();

            // 5. Registrar Auditoría Inicial
            const string sqlAud = @"
                INSERT INTO RL_MR_AUDITORIA (
                    AUD_ID, AUD_EVALUACION_ID, AUD_ACCION, AUD_DETALLE, AUD_FECHA, AUD_USR_ID, AUD_IP
                ) VALUES (
                    SEQ_RL_MR_AUDITORIA.NEXTVAL, :evaId, 'CREAR', 'Registro de evaluación inicial', SYSDATE, :usuarioId, :ip
                )";
            await using var cmdAud = new OracleCommand(sqlAud, conn);
            cmdAud.Parameters.Add(new OracleParameter("evaId", nuevoEvaId));
            cmdAud.Parameters.Add(new OracleParameter("usuarioId", usuarioId));
            cmdAud.Parameters.Add(new OracleParameter("ip", ip ?? (object)DBNull.Value));
            await cmdAud.ExecuteNonQueryAsync();

            await trans.CommitAsync();
            return nuevoEvaId;
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> ActualizarEvaluacionAsync(EvaluacionRiesgoDto dto, long usuarioId, string? ip)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var trans = conn.BeginTransaction();

        try
        {
            // 1. Obtener la evaluación actual para resguardo e historial
            const string sqlSelect = "SELECT EVA_ESTADO, EVA_DATA_JSON, EVA_VERSION_ROW FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_ID = :evaId FOR UPDATE";
            await using var cmdSelect = new OracleCommand(sqlSelect, conn);
            cmdSelect.Parameters.Add(new OracleParameter("evaId", dto.EvaId));

            string jsonAnterior = string.Empty;
            string estadoActual = string.Empty;
            int versionRowActual = 0;

            await using (var reader = await cmdSelect.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    estadoActual = reader.GetString(0);
                    jsonAnterior = reader.GetString(1);
                    versionRowActual = reader.GetInt32(2);
                }
                else
                {
                    await trans.RollbackAsync();
                    return false;
                }
            }

            // 2. Validar concurrencia optimista
            if (versionRowActual != dto.EvaVersionRow)
            {
                await trans.RollbackAsync();
                throw new DBConcurrencyException($"Conflicto de modificación concurrente. El registro de evaluación {dto.EvaId} fue alterado por otro usuario.");
            }

            // 3. Registrar Instantánea Histórica en RL_MR_REVISIONES_EVALUACION
            const string sqlRev = @"
                INSERT INTO RL_MR_REVISIONES_EVALUACION (
                    REV_ID, REV_EVALUACION_ID, REV_DATOS_JSON, REV_FECHA, REV_USR_ID
                ) VALUES (
                    SEQ_RL_MR_REVISIONES.NEXTVAL, :evaId, :jsonAnterior, SYSDATE, :usuarioId
                )";
            await using var cmdRev = new OracleCommand(sqlRev, conn);
            cmdRev.Parameters.Add(new OracleParameter("evaId", dto.EvaId));
            cmdRev.Parameters.Add(new OracleParameter("jsonAnterior", OracleDbType.Clob) { Value = jsonAnterior });
            cmdRev.Parameters.Add(new OracleParameter("usuarioId", usuarioId));
            await cmdRev.ExecuteNonQueryAsync();

            // 4. Actualizar tabla transaccional incrementando versión de fila
            const string sqlUpdate = @"
                UPDATE RL_MR_EVALUACIONES_RIESGO
                   SET EVA_DATA_JSON = :dataJson,
                       EVA_DATA_CALC_JSON = :dataCalcJson,
                       EVA_VRI = :vri,
                       EVA_ETP = :etp,
                       EVA_VRR = :vrr,
                       EVA_FECHA_EVAL = SYSDATE,
                       EVA_USR_EVAL = :usuarioId,
                       EVA_VERSION_ROW = :nuevaVersionRow
                 WHERE EVA_ID = :evaId 
                   AND EVA_VERSION_ROW = :versionRow";

            int nuevaVersionRow = versionRowActual + 1;
            await using var cmdUpdate = new OracleCommand(sqlUpdate, conn);
            cmdUpdate.Parameters.Add(new OracleParameter("dataJson", OracleDbType.Clob) { Value = dto.EvaDataJson });
            cmdUpdate.Parameters.Add(new OracleParameter("dataCalcJson", OracleDbType.Clob) { Value = dto.EvaDataCalcJson });
            cmdUpdate.Parameters.Add(new OracleParameter("vri", dto.EvaVri ?? (object)DBNull.Value));
            cmdUpdate.Parameters.Add(new OracleParameter("etp", dto.EvaEtp ?? (object)DBNull.Value));
            cmdUpdate.Parameters.Add(new OracleParameter("vrr", dto.EvaVrr ?? (object)DBNull.Value));
            cmdUpdate.Parameters.Add(new OracleParameter("usuarioId", usuarioId));
            cmdUpdate.Parameters.Add(new OracleParameter("nuevaVersionRow", nuevaVersionRow));
            cmdUpdate.Parameters.Add(new OracleParameter("evaId", dto.EvaId));
            cmdUpdate.Parameters.Add(new OracleParameter("versionRow", versionRowActual));

            int filas = await cmdUpdate.ExecuteNonQueryAsync();
            if (filas == 0)
            {
                await trans.RollbackAsync();
                return false;
            }

            // Parsear campos planos para actualizar proyecciones
            var answers = MapearDiccionario(dto.EvaDataJson);
            string area = answers.TryGetValue("area_principal", out object? aVal) ? aVal?.ToString() ?? "N/D" : "N/D";
            string due = answers.TryGetValue("dueno_riesgo", out object? dVal) ? dVal?.ToString() ?? "N/D" : "N/D";
            string nivel = DeterminarClasificacionResidual(dto.EvaVrr);

            // 5. Actualizar Proyección Plana
            const string sqlProj = @"
                UPDATE RL_MR_PROYECCIONES_EVALUACION
                   SET PROY_VRI = :vri,
                       PROY_ETP = :etp,
                       PROY_VRR = :vrr,
                       PROY_NIVEL_RESIDUAL = :nivel,
                       PROY_AREA_PRINCIPAL = :area,
                       PROY_DUENO_RIESGO = :due,
                       PROY_FECHA_EVAL = SYSDATE
                 WHERE PROY_EVALUACION_ID = :evaId";

            await using var cmdProj = new OracleCommand(sqlProj, conn);
            cmdProj.Parameters.Add(new OracleParameter("vri", dto.EvaVri ?? (object)DBNull.Value));
            cmdProj.Parameters.Add(new OracleParameter("etp", dto.EvaEtp ?? (object)DBNull.Value));
            cmdProj.Parameters.Add(new OracleParameter("vrr", dto.EvaVrr ?? (object)DBNull.Value));
            cmdProj.Parameters.Add(new OracleParameter("nivel", nivel));
            cmdProj.Parameters.Add(new OracleParameter("area", area));
            cmdProj.Parameters.Add(new OracleParameter("due", due));
            cmdProj.Parameters.Add(new OracleParameter("evaId", dto.EvaId));
            await cmdProj.ExecuteNonQueryAsync();

            // 6. Registrar Auditoría
            const string sqlAud = @"
                INSERT INTO RL_MR_AUDITORIA (
                    AUD_ID, AUD_EVALUACION_ID, AUD_ACCION, AUD_DETALLE, AUD_FECHA, AUD_USR_ID, AUD_IP
                ) VALUES (
                    SEQ_RL_MR_AUDITORIA.NEXTVAL, :evaId, 'ACTUALIZAR', 'Modificación de respuestas del formulario', SYSDATE, :usuarioId, :ip
                )";
            await using var cmdAud = new OracleCommand(sqlAud, conn);
            cmdAud.Parameters.Add(new OracleParameter("evaId", dto.EvaId));
            cmdAud.Parameters.Add(new OracleParameter("usuarioId", usuarioId));
            cmdAud.Parameters.Add(new OracleParameter("ip", ip ?? (object)DBNull.Value));
            await cmdAud.ExecuteNonQueryAsync();

            await trans.CommitAsync();
            return true;
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> TransicionarEstadoEvaluacionAsync(long evaId, string nuevoEstado, string? motivo, long usuarioId, string? ip)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var trans = conn.BeginTransaction();

        try
        {
            // 1. Obtener estado anterior
            const string sqlSelect = "SELECT EVA_ESTADO FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_ID = :evaId FOR UPDATE";
            await using var cmdSelect = new OracleCommand(sqlSelect, conn);
            cmdSelect.Parameters.Add(new OracleParameter("evaId", evaId));
            var anteriorObj = await cmdSelect.ExecuteScalarAsync();
            if (anteriorObj == null)
            {
                await trans.RollbackAsync();
                return false;
            }
            string estadoAnterior = anteriorObj.ToString()!;

            // 2. Modificar estado en la tabla de Evaluaciones
            const string sqlUpdate = "UPDATE RL_MR_EVALUACIONES_RIESGO SET EVA_ESTADO = :nuevoEstado WHERE EVA_ID = :evaId";
            await using var cmdUpdate = new OracleCommand(sqlUpdate, conn);
            cmdUpdate.Parameters.Add(new OracleParameter("nuevoEstado", nuevoEstado.ToUpperInvariant()));
            cmdUpdate.Parameters.Add(new OracleParameter("evaId", evaId));
            await cmdUpdate.ExecuteNonQueryAsync();

            // 3. Modificar estado en la tabla de Proyecciones
            const string sqlProj = "UPDATE RL_MR_PROYECCIONES_EVALUACION SET PROY_ESTADO_EVALUACION = :nuevoEstado WHERE PROY_EVALUACION_ID = :evaId";
            await using var cmdProj = new OracleCommand(sqlProj, conn);
            cmdProj.Parameters.Add(new OracleParameter("nuevoEstado", nuevoEstado.ToUpperInvariant()));
            cmdProj.Parameters.Add(new OracleParameter("evaId", evaId));
            await cmdProj.ExecuteNonQueryAsync();

            // 4. Escribir Flujo de Estados
            const string sqlFlujo = @"
                INSERT INTO RL_MR_FLUJOS_EVALUACION (
                    FLU_ID, FLU_EVALUACION_ID, FLU_ESTADO_ANTERIOR, FLU_ESTADO_NUEVO, FLU_MOTIVO, FLU_FECHA, FLU_USR_ID
                ) VALUES (
                    SEQ_RL_MR_FLUJOS.NEXTVAL, :evaId, :anterior, :nuevo, :motivo, SYSDATE, :usuarioId
                )";
            await using var cmdFlujo = new OracleCommand(sqlFlujo, conn);
            cmdFlujo.Parameters.Add(new OracleParameter("evaId", evaId));
            cmdFlujo.Parameters.Add(new OracleParameter("anterior", estadoAnterior));
            cmdFlujo.Parameters.Add(new OracleParameter("nuevo", nuevoEstado.ToUpperInvariant()));
            cmdFlujo.Parameters.Add(new OracleParameter("motivo", motivo ?? (object)DBNull.Value));
            cmdFlujo.Parameters.Add(new OracleParameter("usuarioId", usuarioId));
            await cmdFlujo.ExecuteNonQueryAsync();

            // 5. Registrar Auditoría
            const string sqlAud = @"
                INSERT INTO RL_MR_AUDITORIA (
                    AUD_ID, AUD_EVALUACION_ID, AUD_ACCION, AUD_DETALLE, AUD_FECHA, AUD_USR_ID, AUD_IP
                ) VALUES (
                    SEQ_RL_MR_AUDITORIA.NEXTVAL, :evaId, 'TRANSICION', :detalle, SYSDATE, :usuarioId, :ip
                )";
            string detalle = $"Transición de estado: {estadoAnterior} -> {nuevoEstado}. Motivo: {motivo}";
            await using var cmdAud = new OracleCommand(sqlAud, conn);
            cmdAud.Parameters.Add(new OracleParameter("evaId", evaId));
            cmdAud.Parameters.Add(new OracleParameter("detalle", detalle));
            cmdAud.Parameters.Add(new OracleParameter("usuarioId", usuarioId));
            cmdAud.Parameters.Add(new OracleParameter("ip", ip ?? (object)DBNull.Value));
            await cmdAud.ExecuteNonQueryAsync();

            await trans.CommitAsync();
            return true;
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }

    public async Task<List<RevisionEvaluacionDto>> ObtenerRevisionesEvaluacionAsync(long evaId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT REV_ID, REV_EVALUACION_ID, REV_DATOS_JSON, REV_FECHA, REV_USR_ID
              FROM RL_MR_REVISIONES_EVALUACION
             WHERE REV_EVALUACION_ID = :evaId
             ORDER BY REV_FECHA DESC";

        await using var cmd = new OracleCommand(sql, conn);
        cmd.Parameters.Add(new OracleParameter("evaId", evaId));

        var lista = new List<RevisionEvaluacionDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(new RevisionEvaluacionDto
            {
                RevId = reader.GetInt64(0),
                RevEvaluacionId = reader.GetInt64(1),
                RevDatosJson = reader.GetString(2),
                RevFecha = reader.GetDateTime(3),
                RevUsrId = reader.GetInt64(4)
            });
        }
        return lista;
    }

    // ============================================================
    // 3. ARCHIVO FÍSICO CENTRAL DE EVIDENCIAS Y SUS VINCULACIONES
    // ============================================================

    public async Task<long> RegistrarEvidenciaFisicaAsync(EvidenciaRegistroDto dto, long usuarioId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        const string sqlSeq = "SELECT SEQ_RL_MR_EVIDENCIAS.NEXTVAL FROM DUAL";
        await using var cmdSeq = new OracleCommand(sqlSeq, conn);
        long nuevoId = Convert.ToInt64(await cmdSeq.ExecuteScalarAsync());

        const string sql = @"
            INSERT INTO RL_MR_EVIDENCIAS (
                EVI_ID, EVI_NOMBRE_ARCHIVO, EVI_EXTENSION, EVI_TAMANO, EVI_HASH, EVI_RUTA, EVI_USR_CREACION
            ) VALUES (
                :eviId, :nombre, :ext, :tamano, :hash, :ruta, :usuarioId
            )";

        await using var cmd = new OracleCommand(sql, conn);
        cmd.Parameters.Add(new OracleParameter("eviId", nuevoId));
        cmd.Parameters.Add(new OracleParameter("nombre", dto.EviNombreArchivo));
        cmd.Parameters.Add(new OracleParameter("ext", dto.EviExtension));
        cmd.Parameters.Add(new OracleParameter("tamano", dto.EviTamano));
        cmd.Parameters.Add(new OracleParameter("hash", dto.EviHash));
        cmd.Parameters.Add(new OracleParameter("ruta", dto.EviRuta));
        cmd.Parameters.Add(new OracleParameter("usuarioId", usuarioId));

        await cmd.ExecuteNonQueryAsync();
        return nuevoId;
    }

    public async Task<EvidenciaDto?> ObtenerEvidenciaFisicaAsync(long evidenciaId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT EVI_ID, EVI_NOMBRE_ARCHIVO, EVI_EXTENSION, EVI_TAMANO, EVI_HASH, EVI_RUTA, EVI_USR_CREACION, EVI_FECHA_CREACION
              FROM RL_MR_EVIDENCIAS
             WHERE EVI_ID = :evidenciaId";

        await using var cmd = new OracleCommand(sql, conn);
        cmd.Parameters.Add(new OracleParameter("evidenciaId", evidenciaId));

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new EvidenciaDto
            {
                EviId = reader.GetInt64(0),
                EviNombreArchivo = reader.GetString(1),
                EviExtension = reader.GetString(2),
                EviTamano = reader.GetInt64(3),
                EviHash = reader.GetString(4),
                EviRuta = reader.GetString(5),
                EviUsrCreacion = reader.GetInt64(6),
                EviFechaCreacion = reader.GetDateTime(7)
            };
        }
        return null;
    }

    public async Task<bool> VincularEvidenciaRiesgoAsync(AsociarEvidenciaRiesgoDto dto, long usuarioId, string? ip)
    {
        return await EjecutarVinculoEvidenciaAsync("RL_MR_EVI_RIESGO", "EVR_RIESGO_ID", "EVR_EVIDENCIA_ID", dto.EvrRiesgoId, dto.EvrEvidenciaId, null, "Riesgo", usuarioId, ip);
    }

    public async Task<bool> VincularEvidenciaEvaluacionAsync(AsociarEvidenciaEvaluacionDto dto, long usuarioId, string? ip)
    {
        return await EjecutarVinculoEvidenciaAsync("RL_MR_EVI_EVALUACION", "EVE_EVALUACION_ID", "EVE_EVIDENCIA_ID", dto.EveEvaluacionId, dto.EveEvidenciaId, dto.EveEvaluacionId, "Evaluacion", usuarioId, ip);
    }

    public async Task<bool> VincularEvidenciaControlAsync(AsociarEvidenciaControlDto dto, long usuarioId, string? ip)
    {
        return await EjecutarVinculoEvidenciaAsync("RL_MR_EVI_CONTROL", "EVC_CONTROL_ID", "EVC_EVIDENCIA_ID", dto.EvcControlId, dto.EvcEvidenciaId, null, "Control", usuarioId, ip);
    }

    public async Task<bool> VincularEvidenciaPlanAsync(AsociarEvidenciaPlanDto dto, long usuarioId, string? ip)
    {
        return await EjecutarVinculoEvidenciaAsync("RL_MR_EVI_PLAN", "EVP_PLAN_ID", "EVP_EVIDENCIA_ID", dto.EvpPlanId, dto.EvpEvidenciaId, null, "Plan", usuarioId, ip);
    }

    public async Task<bool> VincularEvidenciaActividadAsync(AsociarEvidenciaActividadDto dto, long usuarioId, string? ip)
    {
        return await EjecutarVinculoEvidenciaAsync("RL_MR_EVI_ACTIVIDAD", "EVA_ACTIVIDAD_ID", "EVA_EVIDENCIA_ID", dto.EvaActividadId, dto.EvaEvidenciaId, null, "Actividad", usuarioId, ip);
    }

    public async Task<bool> VincularEvidenciaAlertaAsync(AsociarEvidenciaAlertaDto dto, long usuarioId, string? ip)
    {
        return await EjecutarVinculoEvidenciaAsync("RL_MR_EVI_ALERTA", "EVA_ALERTA_ID", "EVA_EVIDENCIA_ID", dto.EvaAlertaId, dto.EvaEvidenciaId, null, "Alerta", usuarioId, ip);
    }

    public async Task<bool> VincularEvidenciaAutomonitoreoAsync(AsociarEvidenciaAutomonitoreoDto dto, long usuarioId, string? ip)
    {
        return await EjecutarVinculoEvidenciaAsync("RL_MR_EVI_AUTOMONITOREO", "EVM_MONITOREO_ID", "EVM_EVIDENCIA_ID", dto.EvmMonitoreoId, dto.EvmEvidenciaId, null, "Automonitoreo", usuarioId, ip);
    }

    public async Task<bool> VincularEvidenciaRevisionAsync(AsociarEvidenciaRevisionDto dto, long usuarioId, string? ip)
    {
        return await EjecutarVinculoEvidenciaAsync("RL_MR_EVI_REVISION", "EVV_REVISION_ID", "EVV_EVIDENCIA_ID", dto.EvvRevisionId, dto.EvvEvidenciaId, null, "Revision", usuarioId, ip);
    }

    public async Task<bool> VincularEvidenciaAprobacionAsync(AsociarEvidenciaAprobacionDto dto, long usuarioId, string? ip)
    {
        return await EjecutarVinculoEvidenciaAsync("RL_MR_EVI_APROBACION", "EVAP_APROBACION_ID", "EVAP_EVIDENCIA_ID", dto.EvapAprobacionId, dto.EvapEvidenciaId, null, "Aprobacion", usuarioId, ip);
    }

    public async Task<bool> EvidenciaTieneVinculosAsync(long evidenciaId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT (SELECT COUNT(*) FROM RL_MR_EVI_RIESGO WHERE EVR_EVIDENCIA_ID = :id)
                 + (SELECT COUNT(*) FROM RL_MR_EVI_EVALUACION WHERE EVE_EVIDENCIA_ID = :id)
                 + (SELECT COUNT(*) FROM RL_MR_EVI_CONTROL WHERE EVC_EVIDENCIA_ID = :id)
                 + (SELECT COUNT(*) FROM RL_MR_EVI_PLAN WHERE EVP_EVIDENCIA_ID = :id)
                 + (SELECT COUNT(*) FROM RL_MR_EVI_ACTIVIDAD WHERE EVA_EVIDENCIA_ID = :id)
                 + (SELECT COUNT(*) FROM RL_MR_EVI_ALERTA WHERE EVA_EVIDENCIA_ID = :id)
                 + (SELECT COUNT(*) FROM RL_MR_EVI_AUTOMONITOREO WHERE EVM_EVIDENCIA_ID = :id)
                 + (SELECT COUNT(*) FROM RL_MR_EVI_REVISION WHERE EVV_EVIDENCIA_ID = :id)
                 + (SELECT COUNT(*) FROM RL_MR_EVI_APROBACION WHERE EVAP_EVIDENCIA_ID = :id) AS total
              FROM DUAL";

        await using var cmd = new OracleCommand(sql, conn);
        cmd.Parameters.Add(new OracleParameter("id", evidenciaId));

        int total = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        return total > 0;
    }

    public async Task<bool> EliminarEvidenciaFisicaAsync(long evidenciaId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        const string sql = "DELETE FROM RL_MR_EVIDENCIAS WHERE EVI_ID = :id";
        await using var cmd = new OracleCommand(sql, conn);
        cmd.Parameters.Add(new OracleParameter("id", evidenciaId));

        int rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    private async Task<bool> EjecutarVinculoEvidenciaAsync(string tablaPuente, string colId, string colEvi, long entidadId, long evidenciaId, long? evaluacionId, string tipoEntidad, long usuarioId, string? ip)
    {
        // Regla obligatoria: No se permite vincular sin aud_evaluacion_id si se puede determinar
        if (evaluacionId == null)
        {
            // Bloquear si no se provee un ID de evaluación para la auditoría, de acuerdo con el plan técnico
            throw new ArgumentException("Es obligatorio proveer una evaluación relacionada para la auditoría de vinculación de evidencias.");
        }

        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var trans = conn.BeginTransaction();

        try
        {
            // 1. Insertar en tabla asociativa
            string sqlInsert = $"INSERT INTO {tablaPuente} ({colId}, {colEvi}) VALUES (:entidadId, :eviId)";
            await using var cmdInsert = new OracleCommand(sqlInsert, conn);
            cmdInsert.Parameters.Add(new OracleParameter("entidadId", entidadId));
            cmdInsert.Parameters.Add(new OracleParameter("eviId", evidenciaId));
            await cmdInsert.ExecuteNonQueryAsync();

            // 2. Registrar auditoría con link a la evaluación de forma obligatoria
            const string sqlAud = @"
                INSERT INTO RL_MR_AUDITORIA (
                    AUD_ID, AUD_EVALUACION_ID, AUD_ACCION, AUD_DETALLE, AUD_FECHA, AUD_USR_ID, AUD_IP
                ) VALUES (
                    SEQ_RL_MR_AUDITORIA.NEXTVAL, :evaId, 'VINCULAR_EVIDENCIA', :detalle, SYSDATE, :usuarioId, :ip
                )";
            string detalle = $"Asociación de archivo de evidencia ID {evidenciaId} a la entidad {tipoEntidad} ID {entidadId}";
            await using var cmdAud = new OracleCommand(sqlAud, conn);
            cmdAud.Parameters.Add(new OracleParameter("evaId", evaluacionId.Value));
            cmdAud.Parameters.Add(new OracleParameter("detalle", detalle));
            cmdAud.Parameters.Add(new OracleParameter("usuarioId", usuarioId));
            cmdAud.Parameters.Add(new OracleParameter("ip", ip ?? (object)DBNull.Value));
            await cmdAud.ExecuteNonQueryAsync();

            await trans.CommitAsync();
            return true;
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }

    // ============================================================
    // 4. REPORTES CONSOLIDADOS Y CATÁLOGOS PARAMÉTRICOS
    // ============================================================

    public async Task<List<Dictionary<string, object>>> ObtenerConsolidadoMatricesAsync()
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT PROY_EVALUACION_ID, PROY_CODIGO_RIESGO, PROY_ESTADO_EVALUACION, 
                   PROY_VRI, PROY_ETP, PROY_VRR, PROY_NIVEL_RESIDUAL, 
                   PROY_AREA_PRINCIPAL, PROY_DUENO_RIESGO, PROY_FECHA_EVAL
              FROM RL_MR_PROYECCIONES_EVALUACION
             ORDER BY PROY_FECHA_EVAL DESC";

        await using var cmd = new OracleCommand(sql, conn);
        var lista = new List<Dictionary<string, object>>();

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object>
            {
                { "EvaluacionId", reader.GetInt64(0) },
                { "CodigoRiesgo", reader.GetString(1) },
                { "Estado", reader.GetString(2) },
                { "Vri", reader.IsDBNull(3) ? 0 : reader.GetInt32(3) },
                { "Etp", reader.IsDBNull(4) ? 0m : reader.GetDecimal(4) },
                { "Vrr", reader.IsDBNull(5) ? 0 : reader.GetInt32(5) },
                { "NivelResidual", reader.IsDBNull(6) ? string.Empty : reader.GetString(6) },
                { "Area", reader.IsDBNull(7) ? string.Empty : reader.GetString(7) },
                { "Dueno", reader.IsDBNull(8) ? string.Empty : reader.GetString(8) },
                { "Fecha", reader.GetDateTime(9) }
            };
            lista.Add(row);
        }
        return lista;
    }

    // ============================================================
    // DICCIONARIOS Y AUXILIARES DE MAPEO
    // ============================================================

    private static VersionFormularioDto MapearVersionFormulario(OracleDataReader reader)
    {
        return new VersionFormularioDto
        {
            VerId = reader.GetInt64(0),
            VerFamiliaId = reader.GetInt64(1),
            VerCodigo = reader.GetString(2),
            VerVersion = reader.GetInt32(3),
            VerJson = reader.GetString(4),
            VerHash = reader.GetString(5),
            VerEstado = reader.GetString(6),
            VerVigente = reader.GetInt32(7) == 1,
            VerFechaInicio = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
            VerFechaFin = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
            VerFechaCreacion = reader.GetDateTime(10),
            VerUsrCreacion = reader.GetInt64(11)
        };
    }

    private static EvaluacionRiesgoDto MapearEvaluacion(OracleDataReader reader)
    {
        return new EvaluacionRiesgoDto
        {
            EvaId = reader.GetInt64(0),
            EvaRiesgoId = reader.GetInt64(1),
            EvaVersionId = reader.GetInt64(2),
            EvaEstado = reader.GetString(3),
            EvaDataJson = reader.GetString(4),
            EvaDataCalcJson = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
            EvaVri = reader.IsDBNull(6) ? null : reader.GetInt32(6),
            EvaEtp = reader.IsDBNull(7) ? null : reader.GetDecimal(7),
            EvaVrr = reader.IsDBNull(8) ? null : reader.GetInt32(8),
            EvaFechaEval = reader.GetDateTime(9),
            EvaUsrEval = reader.GetInt64(10),
            EvaVersionRow = reader.GetInt32(11),
            EvaActivo = reader.GetInt32(12) == 1
        };
    }

    private static Dictionary<string, object> MapearDiccionario(string json)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, object>();
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }

    private static string DeterminarClasificacionResidual(int? vrr)
    {
        if (!vrr.HasValue) return "SIN_CLASIFICAR";
        return vrr.Value switch
        {
            <= 2 => "BAJO",
            <= 4 => "MODERADO",
            <= 6 => "ALTO",
            _ => "CRÍTICO"
        };
    }
}
