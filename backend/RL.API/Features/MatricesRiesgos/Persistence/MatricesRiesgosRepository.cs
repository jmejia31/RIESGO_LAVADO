using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Infrastructure.Database;

namespace RL.API.Features.MatricesRiesgos.Persistence;

public sealed class MatricesRiesgosRepository : IMatricesRiesgosRepository
{
    private const string ModuloAuditoria = "MatricesRiesgos";

    private static readonly HashSet<string> EstadosEvaluacionPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "BORRADOR",
        "EN_REVISION",
        "OBSERVADA",
        "APROBADA",
        "RECHAZADA",
        "CERRADA"
    };

    private readonly OracleDbContext _db;
    private readonly IAuditoriaRepository? _auditoriaRepository;

    public MatricesRiesgosRepository(OracleDbContext db)
        : this(db, null)
    {
    }

    public MatricesRiesgosRepository(OracleDbContext db, IAuditoriaRepository? auditoriaRepository)
    {
        _db = db;
        _auditoriaRepository = auditoriaRepository;
    }

    // ============================================================
    // 1. GESTIÓN DEL CICLO DE VIDA DEL FORMULARIO Y VERSIONES
    // ============================================================

    public async Task<VersionFormularioDto?> ObtenerVersionVigenteFormularioAsync(string familiaCodigo)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT v.VER_ID,
                   v.VER_FAMILIA_ID,
                   v.VER_CODIGO,
                   v.VER_VERSION,
                   v.VER_JSON,
                   v.VER_HASH,
                   v.VER_ESTADO,
                   v.VER_VIGENTE,
                   v.VER_FECHA_INICIO,
                   v.VER_FECHA_FIN,
                   v.VER_FECHA_CREACION,
                   v.VER_USR_CREACION
              FROM RL_MR_VERSIONES_FORMULARIO v
              JOIN RL_MR_FAMILIAS_FORMULARIO f
                ON f.FAM_ID = v.VER_FAMILIA_ID
             WHERE f.FAM_CODIGO = :familiaCodigo
               AND v.VER_ESTADO = 'PUBLISHED'
               AND v.VER_VIGENTE = 1";

        await using var cmd = CrearComando(sql, conn);
        cmd.Parameters.Add(new OracleParameter("familiaCodigo", familiaCodigo));

        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapearVersionFormulario(reader) : null;
    }

    public async Task<VersionFormularioDto?> ObtenerVersionFormularioAsync(long versionId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT VER_ID,
                   VER_FAMILIA_ID,
                   VER_CODIGO,
                   VER_VERSION,
                   VER_JSON,
                   VER_HASH,
                   VER_ESTADO,
                   VER_VIGENTE,
                   VER_FECHA_INICIO,
                   VER_FECHA_FIN,
                   VER_FECHA_CREACION,
                   VER_USR_CREACION
              FROM RL_MR_VERSIONES_FORMULARIO
             WHERE VER_ID = :versionId";

        await using var cmd = CrearComando(sql, conn);
        cmd.Parameters.Add(new OracleParameter("versionId", versionId));

        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapearVersionFormulario(reader) : null;
    }

    public async Task<long> CrearBorradorFormularioAsync(
        long familiaId,
        string codigoFormulario,
        string jsonConfig,
        long usuarioId)
    {
        ValidarJson(jsonConfig, nameof(jsonConfig));

        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var trans = conn.BeginTransaction();

        try
        {
            const string sqlMax = @"
                SELECT NVL(MAX(VER_VERSION), 0) + 1
                  FROM RL_MR_VERSIONES_FORMULARIO
                 WHERE VER_FAMILIA_ID = :familiaId";

            await using var cmdMax = CrearComando(sqlMax, conn, trans);
            cmdMax.Parameters.Add(new OracleParameter("familiaId", familiaId));
            int siguienteVersion = Convert.ToInt32(await cmdMax.ExecuteScalarAsync());

            long nuevoId = await ObtenerSiguienteSecuenciaAsync(conn, trans, "SEQ_RL_MR_VERSIONES");

            const string sqlInsert = @"
                INSERT INTO RL_MR_VERSIONES_FORMULARIO (
                    VER_ID,
                    VER_FAMILIA_ID,
                    VER_CODIGO,
                    VER_VERSION,
                    VER_JSON,
                    VER_HASH,
                    VER_ESTADO,
                    VER_VIGENTE,
                    VER_USR_CREACION
                ) VALUES (
                    :verId,
                    :familiaId,
                    :codigoFormulario,
                    :version,
                    :jsonConfig,
                    :hash,
                    'DRAFT',
                    0,
                    :usuarioId
                )";

            await using var cmdInsert = CrearComando(sqlInsert, conn, trans);
            cmdInsert.Parameters.Add(new OracleParameter("verId", nuevoId));
            cmdInsert.Parameters.Add(new OracleParameter("familiaId", familiaId));
            cmdInsert.Parameters.Add(new OracleParameter("codigoFormulario", codigoFormulario));
            cmdInsert.Parameters.Add(new OracleParameter("version", siguienteVersion));
            cmdInsert.Parameters.Add(new OracleParameter("jsonConfig", OracleDbType.Clob) { Value = jsonConfig });
            cmdInsert.Parameters.Add(new OracleParameter("hash", CalcularHash(jsonConfig)));
            cmdInsert.Parameters.Add(new OracleParameter("usuarioId", usuarioId));
            await cmdInsert.ExecuteNonQueryAsync();

            await trans.CommitAsync();
            return nuevoId;
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }

    public async Task<long> ClonarVersionFormularioAsync(long versionOrigenId, long usuarioId)
    {
        var origen = await ObtenerVersionFormularioAsync(versionOrigenId)
            ?? throw new KeyNotFoundException($"No se encontró la versión origen ID {versionOrigenId}.");

        return await CrearBorradorFormularioAsync(
            origen.VerFamiliaId,
            origen.VerCodigo,
            origen.VerJson,
            usuarioId);
    }

    public async Task<bool> ActualizarBorradorFormularioAsync(
        long versionId,
        string jsonConfig,
        string hash,
        long usuarioId)
    {
        ValidarJson(jsonConfig, nameof(jsonConfig));

        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            UPDATE RL_MR_VERSIONES_FORMULARIO
               SET VER_JSON = :jsonConfig,
                   VER_HASH = :hash
             WHERE VER_ID = :versionId
               AND VER_ESTADO = 'DRAFT'";

        await using var cmd = CrearComando(sql, conn);
        cmd.Parameters.Add(new OracleParameter("jsonConfig", OracleDbType.Clob) { Value = jsonConfig });
        cmd.Parameters.Add(new OracleParameter("hash", hash));
        cmd.Parameters.Add(new OracleParameter("versionId", versionId));
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> PublicarVersionFormularioAsync(long versionId, string hash, long usuarioId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var trans = conn.BeginTransaction();

        try
        {
            const string sqlSelect = @"
                SELECT VER_FAMILIA_ID
                  FROM RL_MR_VERSIONES_FORMULARIO
                 WHERE VER_ID = :versionId
                   AND VER_ESTADO IN ('APPROVED', 'DRAFT')
                 FOR UPDATE";

            await using var cmdSelect = CrearComando(sqlSelect, conn, trans);
            cmdSelect.Parameters.Add(new OracleParameter("versionId", versionId));
            object? familiaIdObj = await cmdSelect.ExecuteScalarAsync();
            if (familiaIdObj is null)
            {
                await trans.RollbackAsync();
                return false;
            }

            long familiaId = Convert.ToInt64(familiaIdObj);

            const string sqlApagar = @"
                UPDATE RL_MR_VERSIONES_FORMULARIO
                   SET VER_VIGENTE = 0,
                       VER_FECHA_FIN = SYSDATE
                 WHERE VER_FAMILIA_ID = :familiaId
                   AND VER_VIGENTE = 1
                   AND VER_ID <> :versionId";

            await using var cmdApagar = CrearComando(sqlApagar, conn, trans);
            cmdApagar.Parameters.Add(new OracleParameter("familiaId", familiaId));
            cmdApagar.Parameters.Add(new OracleParameter("versionId", versionId));
            await cmdApagar.ExecuteNonQueryAsync();

            const string sqlPublicar = @"
                UPDATE RL_MR_VERSIONES_FORMULARIO
                   SET VER_ESTADO = 'PUBLISHED',
                       VER_VIGENTE = 1,
                       VER_HASH = :hash,
                       VER_FECHA_INICIO = SYSDATE,
                       VER_FECHA_FIN = NULL
                 WHERE VER_ID = :versionId";

            await using var cmdPublicar = CrearComando(sqlPublicar, conn, trans);
            cmdPublicar.Parameters.Add(new OracleParameter("hash", hash));
            cmdPublicar.Parameters.Add(new OracleParameter("versionId", versionId));

            if (await cmdPublicar.ExecuteNonQueryAsync() != 1)
            {
                await trans.RollbackAsync();
                return false;
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

    public async Task<bool> CambiarEstadoVigenciaFormularioAsync(long versionId, bool vigente, long usuarioId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            UPDATE RL_MR_VERSIONES_FORMULARIO
               SET VER_VIGENTE = :vigente,
                   VER_FECHA_INICIO = CASE
                       WHEN :vigente = 1 THEN NVL(VER_FECHA_INICIO, SYSDATE)
                       ELSE VER_FECHA_INICIO
                   END,
                   VER_FECHA_FIN = CASE
                       WHEN :vigente = 0 THEN SYSDATE
                       ELSE NULL
                   END
             WHERE VER_ID = :versionId
               AND VER_ESTADO = 'PUBLISHED'";

        await using var cmd = CrearComando(sql, conn);
        cmd.Parameters.Add(new OracleParameter("vigente", vigente ? 1 : 0));
        cmd.Parameters.Add(new OracleParameter("versionId", versionId));
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<List<VersionFormularioDto>> ListarHistorialVersionesFormularioAsync(string familiaCodigo)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT v.VER_ID,
                   v.VER_FAMILIA_ID,
                   v.VER_CODIGO,
                   v.VER_VERSION,
                   v.VER_JSON,
                   v.VER_HASH,
                   v.VER_ESTADO,
                   v.VER_VIGENTE,
                   v.VER_FECHA_INICIO,
                   v.VER_FECHA_FIN,
                   v.VER_FECHA_CREACION,
                   v.VER_USR_CREACION
              FROM RL_MR_VERSIONES_FORMULARIO v
              JOIN RL_MR_FAMILIAS_FORMULARIO f
                ON f.FAM_ID = v.VER_FAMILIA_ID
             WHERE f.FAM_CODIGO = :familiaCodigo
             ORDER BY v.VER_VERSION DESC";

        await using var cmd = CrearComando(sql, conn);
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
    // 2. GESTIÓN DE EVALUACIONES, PROYECCIONES, FLUJOS Y TRAZAS
    // ============================================================

    public async Task<EvaluacionRiesgoDto?> ObtenerEvaluacionAsync(long evaId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT e.EVA_ID,
                   e.EVA_RIESGO_ID,
                   e.EVA_VERSION_ID,
                   NVL(f.FLU_ESTADO, 'BORRADOR'),
                   e.EVA_DATA_JSON,
                   e.EVA_DATA_CALC_JSON,
                   p.PROY_VRI,
                   CAST(NULL AS NUMBER),
                   p.PROY_VRR,
                   e.EVA_FECHA_REGISTRO,
                   e.EVA_USR_REGISTRO,
                   e.EVA_VERSION_ROW,
                   e.EVA_ACTIVO
              FROM RL_MR_EVALUACIONES_RIESGO e
              LEFT JOIN RL_MR_PROYECCIONES_EVALUACION p
                ON p.PROY_EVALUACION_ID = e.EVA_ID
              LEFT JOIN (
                    SELECT FLU_EVALUACION_ID, FLU_ESTADO
                      FROM (
                            SELECT FLU_EVALUACION_ID,
                                   FLU_ESTADO,
                                   ROW_NUMBER() OVER (
                                       PARTITION BY FLU_EVALUACION_ID
                                       ORDER BY FLU_FECHA DESC, FLU_ID DESC
                                   ) RN
                              FROM RL_MR_FLUJOS_EVALUACION
                           )
                     WHERE RN = 1
              ) f
                ON f.FLU_EVALUACION_ID = e.EVA_ID
             WHERE e.EVA_ID = :evaId";

        await using var cmd = CrearComando(sql, conn);
        cmd.Parameters.Add(new OracleParameter("evaId", evaId));

        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapearEvaluacion(reader) : null;
    }

    public async Task<List<EvaluacionRiesgoDto>> ListarEvaluacionesPaginadasAsync(
        ConsultaEvaluacionPaginadaDto filtro)
    {
        if (filtro.Pagina < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(filtro.Pagina));
        }

        if (filtro.RegistrosPorPagina < 1 || filtro.RegistrosPorPagina > 200)
        {
            throw new ArgumentOutOfRangeException(nameof(filtro.RegistrosPorPagina));
        }

        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        var baseSql = new StringBuilder(@"
            SELECT e.EVA_ID,
                   e.EVA_RIESGO_ID,
                   e.EVA_VERSION_ID,
                   NVL(f.FLU_ESTADO, 'BORRADOR'),
                   e.EVA_DATA_JSON,
                   e.EVA_DATA_CALC_JSON,
                   p.PROY_VRI,
                   CAST(NULL AS NUMBER),
                   p.PROY_VRR,
                   e.EVA_FECHA_REGISTRO,
                   e.EVA_USR_REGISTRO,
                   e.EVA_VERSION_ROW,
                   e.EVA_ACTIVO
              FROM RL_MR_EVALUACIONES_RIESGO e
              LEFT JOIN RL_MR_PROYECCIONES_EVALUACION p
                ON p.PROY_EVALUACION_ID = e.EVA_ID
              LEFT JOIN (
                    SELECT FLU_EVALUACION_ID, FLU_ESTADO
                      FROM (
                            SELECT FLU_EVALUACION_ID,
                                   FLU_ESTADO,
                                   ROW_NUMBER() OVER (
                                       PARTITION BY FLU_EVALUACION_ID
                                       ORDER BY FLU_FECHA DESC, FLU_ID DESC
                                   ) RN
                              FROM RL_MR_FLUJOS_EVALUACION
                           )
                     WHERE RN = 1
              ) f
                ON f.FLU_EVALUACION_ID = e.EVA_ID
             WHERE e.EVA_ACTIVO = 1");

        var parameters = new List<OracleParameter>();

        if (filtro.RiesgoId.HasValue)
        {
            baseSql.Append(" AND e.EVA_RIESGO_ID = :riesgoId");
            parameters.Add(new OracleParameter("riesgoId", filtro.RiesgoId.Value));
        }

        if (!string.IsNullOrWhiteSpace(filtro.Estado))
        {
            baseSql.Append(" AND f.FLU_ESTADO = :estado");
            parameters.Add(new OracleParameter("estado", filtro.Estado.Trim().ToUpperInvariant()));
        }

        if (!string.IsNullOrWhiteSpace(filtro.Area))
        {
            baseSql.Append(" AND p.PROY_AREA_PRINCIPAL = :area");
            parameters.Add(new OracleParameter("area", filtro.Area.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(filtro.NivelResidual))
        {
            baseSql.Append(" AND p.PROY_NIVEL_RESIDUAL = :nivelResidual");
            parameters.Add(new OracleParameter("nivelResidual", filtro.NivelResidual.Trim()));
        }

        int offset = (filtro.Pagina - 1) * filtro.RegistrosPorPagina;
        string queryPaginada = $@"
            SELECT *
              FROM (
                    SELECT q.*, ROWNUM NUMERO_FILA
                      FROM (
                            {baseSql}
                            ORDER BY e.EVA_FECHA_REGISTRO DESC, e.EVA_ID DESC
                           ) q
                     WHERE ROWNUM <= :filaFinal
                   )
             WHERE NUMERO_FILA > :filaInicial";

        await using var cmd = CrearComando(queryPaginada, conn);
        foreach (var parameter in parameters)
        {
            cmd.Parameters.Add(parameter);
        }

        cmd.Parameters.Add(new OracleParameter("filaFinal", offset + filtro.RegistrosPorPagina));
        cmd.Parameters.Add(new OracleParameter("filaInicial", offset));

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
        ValidarDatosEvaluacion(dto);

        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var trans = conn.BeginTransaction();

        try
        {
            long reglaId = await ResolverReglaVersionFormularioAsync(
                conn,
                trans,
                dto.EvaVersionId,
                exigirVigente: true);

            long nuevoEvaId = await ObtenerSiguienteSecuenciaAsync(
                conn,
                trans,
                "SEQ_RL_MR_EVALUACIONES");

            var proyeccion = ConstruirProyeccion(dto);
            string codigoRiesgo = await ObtenerCodigoRiesgoAsync(
                conn,
                trans,
                dto.EvaRiesgoId);

            const string sqlInsert = @"
                INSERT INTO RL_MR_EVALUACIONES_RIESGO (
                    EVA_ID,
                    EVA_RIESGO_ID,
                    EVA_VERSION_ID,
                    EVA_DATA_JSON,
                    EVA_DATA_CALC_JSON,
                    EVA_FECHA_REGISTRO,
                    EVA_USR_REGISTRO,
                    EVA_VERSION_ROW,
                    EVA_ACTIVO
                ) VALUES (
                    :evaId,
                    :riesgoId,
                    :versionId,
                    :dataJson,
                    :dataCalcJson,
                    SYSDATE,
                    :usuarioId,
                    1,
                    1
                )";

            await using var cmdInsert = CrearComando(sqlInsert, conn, trans);
            cmdInsert.Parameters.Add(new OracleParameter("evaId", nuevoEvaId));
            cmdInsert.Parameters.Add(new OracleParameter("riesgoId", dto.EvaRiesgoId));
            cmdInsert.Parameters.Add(new OracleParameter("versionId", dto.EvaVersionId));
            cmdInsert.Parameters.Add(new OracleParameter("dataJson", OracleDbType.Clob) { Value = dto.EvaDataJson });
            cmdInsert.Parameters.Add(new OracleParameter("dataCalcJson", OracleDbType.Clob) { Value = dto.EvaDataCalcJson });
            cmdInsert.Parameters.Add(new OracleParameter("usuarioId", usuarioId));
            await cmdInsert.ExecuteNonQueryAsync();

            await InsertarProyeccionAsync(
                conn,
                trans,
                nuevoEvaId,
                codigoRiesgo,
                proyeccion,
                "BORRADOR");

            await InsertarFlujoAsync(
                conn,
                trans,
                nuevoEvaId,
                "BORRADOR",
                "Creación inicial",
                usuarioId);

            await InsertarTrazaCalculoAsync(
                conn,
                trans,
                nuevoEvaId,
                reglaId,
                dto.EvaDataJson,
                dto.EvaDataCalcJson,
                usuarioId);

            await InsertarAuditoriaCampoAsync(
                conn,
                trans,
                nuevoEvaId,
                "__EVALUACION__",
                null,
                dto.EvaDataJson,
                usuarioId,
                ip);

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
        ValidarDatosEvaluacion(dto);

        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var trans = conn.BeginTransaction();

        try
        {
            const string sqlSelect = @"
                SELECT EVA_DATA_JSON,
                       EVA_VERSION_ROW,
                       EVA_VERSION_ID
                  FROM RL_MR_EVALUACIONES_RIESGO
                 WHERE EVA_ID = :evaId
                   AND EVA_ACTIVO = 1
                 FOR UPDATE";

            await using var cmdSelect = CrearComando(sqlSelect, conn, trans);
            cmdSelect.Parameters.Add(new OracleParameter("evaId", dto.EvaId));

            string jsonAnterior;
            int versionRowActual;
            long versionFormularioId;

            await using (var reader = await cmdSelect.ExecuteReaderAsync())
            {
                if (!await reader.ReadAsync())
                {
                    await trans.RollbackAsync();
                    return false;
                }

                jsonAnterior = reader.GetString(0);
                versionRowActual = reader.GetInt32(1);
                versionFormularioId = reader.GetInt64(2);
            }

            if (versionRowActual != dto.EvaVersionRow)
            {
                throw new DBConcurrencyException(
                    $"Conflicto de modificación concurrente en la evaluación {dto.EvaId}.");
            }

            long reglaId = await ResolverReglaVersionFormularioAsync(
                conn,
                trans,
                versionFormularioId,
                exigirVigente: false);

            const string sqlRevision = @"
                INSERT INTO RL_MR_REVISIONES_EVALUACION (
                    REV_ID,
                    REV_EVALUACION_ID,
                    REV_DATOS_JSON,
                    REV_FECHA,
                    REV_USR_ID
                ) VALUES (
                    SEQ_RL_MR_REVISIONES.NEXTVAL,
                    :evaId,
                    :jsonAnterior,
                    SYSDATE,
                    :usuarioId
                )";

            await using var cmdRevision = CrearComando(sqlRevision, conn, trans);
            cmdRevision.Parameters.Add(new OracleParameter("evaId", dto.EvaId));
            cmdRevision.Parameters.Add(new OracleParameter("jsonAnterior", OracleDbType.Clob) { Value = jsonAnterior });
            cmdRevision.Parameters.Add(new OracleParameter("usuarioId", usuarioId));
            await cmdRevision.ExecuteNonQueryAsync();

            const string sqlUpdate = @"
                UPDATE RL_MR_EVALUACIONES_RIESGO
                   SET EVA_DATA_JSON = :dataJson,
                       EVA_DATA_CALC_JSON = :dataCalcJson,
                       EVA_VERSION_ROW = :nuevaVersionRow
                 WHERE EVA_ID = :evaId
                   AND EVA_VERSION_ROW = :versionRow
                   AND EVA_ACTIVO = 1";

            await using var cmdUpdate = CrearComando(sqlUpdate, conn, trans);
            cmdUpdate.Parameters.Add(new OracleParameter("dataJson", OracleDbType.Clob) { Value = dto.EvaDataJson });
            cmdUpdate.Parameters.Add(new OracleParameter("dataCalcJson", OracleDbType.Clob) { Value = dto.EvaDataCalcJson });
            cmdUpdate.Parameters.Add(new OracleParameter("nuevaVersionRow", versionRowActual + 1));
            cmdUpdate.Parameters.Add(new OracleParameter("evaId", dto.EvaId));
            cmdUpdate.Parameters.Add(new OracleParameter("versionRow", versionRowActual));

            if (await cmdUpdate.ExecuteNonQueryAsync() != 1)
            {
                throw new DBConcurrencyException(
                    $"No se pudo actualizar la evaluación {dto.EvaId} por un conflicto de concurrencia.");
            }

            var proyeccion = ConstruirProyeccion(dto);
            int proyeccionesActualizadas = await ActualizarProyeccionAsync(
                conn,
                trans,
                dto.EvaId,
                proyeccion);

            if (proyeccionesActualizadas != 1)
            {
                throw new InvalidOperationException(
                    $"La evaluación {dto.EvaId} debe tener exactamente una proyección; se actualizaron {proyeccionesActualizadas}.");
            }

            await InsertarTrazaCalculoAsync(
                conn,
                trans,
                dto.EvaId,
                reglaId,
                dto.EvaDataJson,
                dto.EvaDataCalcJson,
                usuarioId);

            await InsertarAuditoriaCampoAsync(
                conn,
                trans,
                dto.EvaId,
                "__EVALUACION__",
                jsonAnterior,
                dto.EvaDataJson,
                usuarioId,
                ip);

            await trans.CommitAsync();
            return true;
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> TransicionarEstadoEvaluacionAsync(
        long evaId,
        string nuevoEstado,
        string? motivo,
        long usuarioId,
        string? ip)
    {
        string estadoNormalizado = nuevoEstado.Trim().ToUpperInvariant();
        if (!EstadosEvaluacionPermitidos.Contains(estadoNormalizado))
        {
            throw new ArgumentException(
                $"Estado de evaluación no permitido: {nuevoEstado}.",
                nameof(nuevoEstado));
        }

        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var trans = conn.BeginTransaction();

        try
        {
            const string sqlLock = @"
                SELECT EVA_ID
                  FROM RL_MR_EVALUACIONES_RIESGO
                 WHERE EVA_ID = :evaId
                   AND EVA_ACTIVO = 1
                 FOR UPDATE";

            await using var cmdLock = CrearComando(sqlLock, conn, trans);
            cmdLock.Parameters.Add(new OracleParameter("evaId", evaId));
            if (await cmdLock.ExecuteScalarAsync() is null)
            {
                await trans.RollbackAsync();
                return false;
            }

            string estadoAnterior = await ObtenerEstadoActualAsync(conn, trans, evaId);

            const string sqlActualizarProyeccion = @"
                UPDATE RL_MR_PROYECCIONES_EVALUACION
                   SET PROY_ESTADO_EVALUACION = :nuevoEstado
                 WHERE PROY_EVALUACION_ID = :evaId";

            await using var cmdProyeccion = CrearComando(sqlActualizarProyeccion, conn, trans);
            cmdProyeccion.Parameters.Add(new OracleParameter("nuevoEstado", estadoNormalizado));
            cmdProyeccion.Parameters.Add(new OracleParameter("evaId", evaId));

            if (await cmdProyeccion.ExecuteNonQueryAsync() != 1)
            {
                throw new InvalidOperationException(
                    $"No se encontró una proyección única para la evaluación {evaId}.");
            }

            await InsertarFlujoAsync(
                conn,
                trans,
                evaId,
                estadoNormalizado,
                motivo,
                usuarioId);

            await InsertarAuditoriaCampoAsync(
                conn,
                trans,
                evaId,
                "estado",
                estadoAnterior,
                estadoNormalizado,
                usuarioId,
                ip);

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
            SELECT REV_ID,
                   REV_EVALUACION_ID,
                   REV_DATOS_JSON,
                   REV_FECHA,
                   REV_USR_ID
              FROM RL_MR_REVISIONES_EVALUACION
             WHERE REV_EVALUACION_ID = :evaId
             ORDER BY REV_FECHA DESC, REV_ID DESC";

        await using var cmd = CrearComando(sql, conn);
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
    // 3. EVIDENCIAS Y VINCULACIONES
    // ============================================================

    public async Task<long> RegistrarEvidenciaFisicaAsync(EvidenciaRegistroDto dto, long usuarioId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        long nuevoId = await ObtenerSiguienteSecuenciaAsync(
            conn,
            transaction: null,
            "SEQ_RL_MR_EVIDENCIAS");

        const string sql = @"
            INSERT INTO RL_MR_EVIDENCIAS (
                EVI_ID,
                EVI_NOMBRE_ARCHIVO,
                EVI_EXTENSION,
                EVI_TAMANO,
                EVI_HASH,
                EVI_RUTA,
                EVI_USR_CREACION
            ) VALUES (
                :evidenciaId,
                :nombre,
                :extension,
                :tamano,
                :hash,
                :ruta,
                :usuarioId
            )";

        await using var cmd = CrearComando(sql, conn);
        cmd.Parameters.Add(new OracleParameter("evidenciaId", nuevoId));
        cmd.Parameters.Add(new OracleParameter("nombre", dto.EviNombreArchivo));
        cmd.Parameters.Add(new OracleParameter("extension", dto.EviExtension));
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
            SELECT EVI_ID,
                   EVI_NOMBRE_ARCHIVO,
                   EVI_EXTENSION,
                   EVI_TAMANO,
                   EVI_HASH,
                   EVI_RUTA,
                   EVI_USR_CREACION,
                   EVI_FECHA_CREACION
              FROM RL_MR_EVIDENCIAS
             WHERE EVI_ID = :evidenciaId";

        await using var cmd = CrearComando(sql, conn);
        cmd.Parameters.Add(new OracleParameter("evidenciaId", evidenciaId));

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

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

    public Task<bool> VincularEvidenciaRiesgoAsync(
        AsociarEvidenciaRiesgoDto dto,
        long usuarioId,
        string? ip) =>
        EjecutarVinculoEvidenciaAsync(
            "RL_MR_EVI_RIESGO",
            "EVR_RIESGO_ID",
            "EVR_EVIDENCIA_ID",
            dto.EvrRiesgoId,
            dto.EvrEvidenciaId,
            @"SELECT EVA_ID
                FROM (
                      SELECT EVA_ID
                        FROM RL_MR_EVALUACIONES_RIESGO
                       WHERE EVA_RIESGO_ID = :entidadId
                         AND EVA_ACTIVO = 1
                       ORDER BY EVA_FECHA_REGISTRO DESC, EVA_ID DESC
                     )
               WHERE ROWNUM = 1",
            usuarioId,
            ip);

    public Task<bool> VincularEvidenciaEvaluacionAsync(
        AsociarEvidenciaEvaluacionDto dto,
        long usuarioId,
        string? ip) =>
        EjecutarVinculoEvidenciaAsync(
            "RL_MR_EVI_EVALUACION",
            "EVE_EVALUACION_ID",
            "EVE_EVIDENCIA_ID",
            dto.EveEvaluacionId,
            dto.EveEvidenciaId,
            "SELECT EVA_ID FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_ID = :entidadId",
            usuarioId,
            ip);

    public Task<bool> VincularEvidenciaControlAsync(
        AsociarEvidenciaControlDto dto,
        long usuarioId,
        string? ip) =>
        EjecutarVinculoEvidenciaAsync(
            "RL_MR_EVI_CONTROL",
            "EVC_CONTROL_ID",
            "EVC_EVIDENCIA_ID",
            dto.EvcControlId,
            dto.EvcEvidenciaId,
            "SELECT CON_EVALUACION_ID FROM RL_MR_CONTROLES_RIESGO WHERE CON_ID = :entidadId",
            usuarioId,
            ip);

    public Task<bool> VincularEvidenciaPlanAsync(
        AsociarEvidenciaPlanDto dto,
        long usuarioId,
        string? ip) =>
        EjecutarVinculoEvidenciaAsync(
            "RL_MR_EVI_PLAN",
            "EVP_PLAN_ID",
            "EVP_EVIDENCIA_ID",
            dto.EvpPlanId,
            dto.EvpEvidenciaId,
            "SELECT PLA_EVALUACION_ID FROM RL_MR_PLANES WHERE PLA_ID = :entidadId",
            usuarioId,
            ip);

    public Task<bool> VincularEvidenciaActividadAsync(
        AsociarEvidenciaActividadDto dto,
        long usuarioId,
        string? ip) =>
        EjecutarVinculoEvidenciaAsync(
            "RL_MR_EVI_ACTIVIDAD",
            "EVA_ACTIVIDAD_ID",
            "EVA_EVIDENCIA_ID",
            dto.EvaActividadId,
            dto.EvaEvidenciaId,
            @"SELECT p.PLA_EVALUACION_ID
                FROM RL_MR_ACTIVIDADES a
                JOIN RL_MR_PLANES p
                  ON p.PLA_ID = a.ACT_PLAN_ID
               WHERE a.ACT_ID = :entidadId",
            usuarioId,
            ip);

    public Task<bool> VincularEvidenciaAlertaAsync(
        AsociarEvidenciaAlertaDto dto,
        long usuarioId,
        string? ip) =>
        EjecutarVinculoEvidenciaAsync(
            "RL_MR_EVI_ALERTA",
            "EVA_ALERTA_ID",
            "EVA_EVIDENCIA_ID",
            dto.EvaAlertaId,
            dto.EvaEvidenciaId,
            "SELECT ALE_EVALUACION_ID FROM RL_MR_SENALES_ALERTA WHERE ALE_ID = :entidadId",
            usuarioId,
            ip);

    public Task<bool> VincularEvidenciaAutomonitoreoAsync(
        AsociarEvidenciaAutomonitoreoDto dto,
        long usuarioId,
        string? ip) =>
        EjecutarVinculoEvidenciaAsync(
            "RL_MR_EVI_AUTOMONITOREO",
            "EVM_MONITOREO_ID",
            "EVM_EVIDENCIA_ID",
            dto.EvmMonitoreoId,
            dto.EvmEvidenciaId,
            "SELECT MON_EVALUACION_ID FROM RL_MR_AUTOMONITOREO WHERE MON_ID = :entidadId",
            usuarioId,
            ip);

    public Task<bool> VincularEvidenciaRevisionAsync(
        AsociarEvidenciaRevisionDto dto,
        long usuarioId,
        string? ip) =>
        EjecutarVinculoEvidenciaAsync(
            "RL_MR_EVI_REVISION",
            "EVV_REVISION_ID",
            "EVV_EVIDENCIA_ID",
            dto.EvvRevisionId,
            dto.EvvEvidenciaId,
            "SELECT REV_EVALUACION_ID FROM RL_MR_REVISIONES_EVALUACION WHERE REV_ID = :entidadId",
            usuarioId,
            ip);

    public Task<bool> VincularEvidenciaAprobacionAsync(
        AsociarEvidenciaAprobacionDto dto,
        long usuarioId,
        string? ip) =>
        EjecutarVinculoEvidenciaAsync(
            "RL_MR_EVI_APROBACION",
            "EVAP_APROBACION_ID",
            "EVAP_EVIDENCIA_ID",
            dto.EvapAprobacionId,
            dto.EvapEvidenciaId,
            sqlResolverEvaluacion: null,
            usuarioId,
            ip);

    public async Task<ResultadoEliminacionEvidencia> EliminarEvidenciaSeguraAsync(
        long evidenciaId,
        Func<Task<bool>> eliminarArchivoFisico,
        long usuarioId,
        string? ip)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var trans = conn.BeginTransaction();

        try
        {
            const string sqlLock = @"
                SELECT EVI_ID
                  FROM RL_MR_EVIDENCIAS
                 WHERE EVI_ID = :evidenciaId
                 FOR UPDATE";

            await using var cmdLock = CrearComando(sqlLock, conn, trans);
            cmdLock.Parameters.Add(new OracleParameter("evidenciaId", evidenciaId));
            if (await cmdLock.ExecuteScalarAsync() is null)
            {
                await trans.RollbackAsync();
                return ResultadoEliminacionEvidencia.NoExiste;
            }

            const string sqlVinculos = @"
                SELECT (SELECT COUNT(*) FROM RL_MR_EVI_RIESGO WHERE EVR_EVIDENCIA_ID = :evidenciaId)
                     + (SELECT COUNT(*) FROM RL_MR_EVI_EVALUACION WHERE EVE_EVIDENCIA_ID = :evidenciaId)
                     + (SELECT COUNT(*) FROM RL_MR_EVI_CONTROL WHERE EVC_EVIDENCIA_ID = :evidenciaId)
                     + (SELECT COUNT(*) FROM RL_MR_EVI_PLAN WHERE EVP_EVIDENCIA_ID = :evidenciaId)
                     + (SELECT COUNT(*) FROM RL_MR_EVI_ACTIVIDAD WHERE EVA_EVIDENCIA_ID = :evidenciaId)
                     + (SELECT COUNT(*) FROM RL_MR_EVI_ALERTA WHERE EVA_EVIDENCIA_ID = :evidenciaId)
                     + (SELECT COUNT(*) FROM RL_MR_EVI_AUTOMONITOREO WHERE EVM_EVIDENCIA_ID = :evidenciaId)
                     + (SELECT COUNT(*) FROM RL_MR_EVI_REVISION WHERE EVV_EVIDENCIA_ID = :evidenciaId)
                     + (SELECT COUNT(*) FROM RL_MR_EVI_APROBACION WHERE EVAP_EVIDENCIA_ID = :evidenciaId)
                  FROM DUAL";

            await using var cmdVinculos = CrearComando(sqlVinculos, conn, trans);
            cmdVinculos.Parameters.Add(new OracleParameter("evidenciaId", evidenciaId));
            if (Convert.ToInt32(await cmdVinculos.ExecuteScalarAsync()) > 0)
            {
                await trans.RollbackAsync();
                return ResultadoEliminacionEvidencia.TieneVinculos;
            }

            const string sqlDelete = @"
                DELETE FROM RL_MR_EVIDENCIAS
                 WHERE EVI_ID = :evidenciaId";

            await using var cmdDelete = CrearComando(sqlDelete, conn, trans);
            cmdDelete.Parameters.Add(new OracleParameter("evidenciaId", evidenciaId));
            await cmdDelete.ExecuteNonQueryAsync();

            bool archivoEliminado;
            try
            {
                archivoEliminado = await eliminarArchivoFisico();
            }
            catch
            {
                archivoEliminado = false;
            }

            if (!archivoEliminado)
            {
                await trans.RollbackAsync();
                return ResultadoEliminacionEvidencia.FalloDisco;
            }

            try
            {
                await trans.CommitAsync();
                return ResultadoEliminacionEvidencia.Exito;
            }
            catch
            {
                return ResultadoEliminacionEvidencia.FalloCommit;
            }
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }

    // ============================================================
    // 4. REPORTE CONSOLIDADO TRANSITORIO
    // ============================================================

    public async Task<List<Dictionary<string, object>>> ObtenerConsolidadoMatricesAsync()
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT PROY_EVALUACION_ID,
                   PROY_CODIGO_RIESGO,
                   PROY_ESTADO_EVALUACION,
                   PROY_VRI,
                   PROY_VRR,
                   PROY_NIVEL_INHERENTE,
                   PROY_NIVEL_RESIDUAL,
                   PROY_RESPUESTA_RIESGO,
                   PROY_AREA_PRINCIPAL,
                   PROY_DUENO_RIESGO,
                   PROY_FECHA_EVAL
              FROM RL_MR_PROYECCIONES_EVALUACION
             ORDER BY PROY_FECHA_EVAL DESC, PROY_EVALUACION_ID DESC";

        await using var cmd = CrearComando(sql, conn);
        var lista = new List<Dictionary<string, object>>();
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            lista.Add(new Dictionary<string, object>
            {
                ["EvaluacionId"] = reader.GetInt64(0),
                ["CodigoRiesgo"] = reader.GetString(1),
                ["Estado"] = reader.GetString(2),
                ["Vri"] = reader.GetInt32(3),
                ["Vrr"] = reader.GetInt32(4),
                ["NivelInherente"] = reader.GetString(5),
                ["NivelResidual"] = reader.GetString(6),
                ["RespuestaRiesgo"] = reader.GetString(7),
                ["Area"] = reader.GetString(8),
                ["Dueno"] = reader.GetString(9),
                ["Fecha"] = reader.GetDateTime(10)
            });
        }

        return lista;
    }

    // ============================================================
    // 5. METODOLOGÍA VIGENTE — SE RECONSTRUIRÁ EN FASE 1.4
    // ============================================================

    public Task<MetodologiaMatricesDto?> ObtenerMetodologiaVigenteAsync()
    {
        return Task.FromResult<MetodologiaMatricesDto?>(new MetodologiaMatricesDto
        {
            Version = "PENDIENTE_MODELO_DINAMICO",
            PesoTotalEsperado = 0m,
            PuntajeMinimo = 1m,
            PuntajeMaximo = 9m,
            MitigacionMaximaPct = 0m,
            DecimalesCalculo = 4,
            DecimalesVisualizacion = 2,
            MitigacionesPermitidas = new List<decimal>()
        });
    }

    // ============================================================
    // AUXILIARES PRIVADOS
    // ============================================================

    private async Task<bool> EjecutarVinculoEvidenciaAsync(
        string tablaPuente,
        string columnaEntidad,
        string columnaEvidencia,
        long entidadId,
        long evidenciaId,
        string? sqlResolverEvaluacion,
        long usuarioId,
        string? ip)
    {
        if (sqlResolverEvaluacion is null && _auditoriaRepository is null)
        {
            throw new InvalidOperationException(
                "La vinculación de evidencias de aprobación requiere la auditoría transversal institucional.");
        }

        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var trans = conn.BeginTransaction();

        long? evaluacionId = null;

        try
        {
            const string sqlExisteEvidencia = @"
                SELECT EVI_ID
                  FROM RL_MR_EVIDENCIAS
                 WHERE EVI_ID = :evidenciaId";

            await using var cmdExisteEvidencia = CrearComando(sqlExisteEvidencia, conn, trans);
            cmdExisteEvidencia.Parameters.Add(new OracleParameter("evidenciaId", evidenciaId));
            if (await cmdExisteEvidencia.ExecuteScalarAsync() is null)
            {
                throw new KeyNotFoundException($"No se encontró la evidencia {evidenciaId}.");
            }

            if (!string.IsNullOrWhiteSpace(sqlResolverEvaluacion))
            {
                await using var cmdResolver = CrearComando(sqlResolverEvaluacion, conn, trans);
                cmdResolver.Parameters.Add(new OracleParameter("entidadId", entidadId));
                object? resultado = await cmdResolver.ExecuteScalarAsync();
                if (resultado is null)
                {
                    throw new InvalidOperationException(
                        $"No fue posible resolver la evaluación relacionada con {tablaPuente} y entidad {entidadId}.");
                }

                evaluacionId = Convert.ToInt64(resultado);
            }

            string sqlInsert = $@"
                INSERT INTO {tablaPuente} (
                    {columnaEntidad},
                    {columnaEvidencia}
                ) VALUES (
                    :entidadId,
                    :evidenciaId
                )";

            await using var cmdInsert = CrearComando(sqlInsert, conn, trans);
            cmdInsert.Parameters.Add(new OracleParameter("entidadId", entidadId));
            cmdInsert.Parameters.Add(new OracleParameter("evidenciaId", evidenciaId));
            await cmdInsert.ExecuteNonQueryAsync();

            if (evaluacionId.HasValue)
            {
                await InsertarAuditoriaCampoAsync(
                    conn,
                    trans,
                    evaluacionId.Value,
                    "evidencia",
                    null,
                    $"{tablaPuente}:{entidadId}:{evidenciaId}",
                    usuarioId,
                    ip);
            }

            if (_auditoriaRepository is not null)
            {
                string datosNuevos = JsonSerializer.Serialize(new
                {
                    tablaPuente,
                    entidadId,
                    evidenciaId,
                    evaluacionId
                });

                await _auditoriaRepository.RegistrarAsync(
                    conn,
                    trans,
                    tablaPuente,
                    $"{entidadId}:{evidenciaId}",
                    "VINCULAR_EVIDENCIA",
                    null,
                    datosNuevos,
                    usuarioId,
                    null,
                    ip,
                    ModuloAuditoria);
            }
            else if (string.Equals(tablaPuente, "RL_MR_EVI_APROBACION", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "La vinculación de evidencia a aprobación exige auditoría transversal transaccional.");
            }

            await trans.CommitAsync();
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }

        return true;
    }

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
            EvaDataCalcJson = reader.GetString(5),
            EvaVri = reader.IsDBNull(6) ? null : reader.GetInt32(6),
            EvaEtp = reader.IsDBNull(7) ? null : reader.GetDecimal(7),
            EvaVrr = reader.IsDBNull(8) ? null : reader.GetInt32(8),
            EvaFechaEval = reader.GetDateTime(9),
            EvaUsrEval = reader.GetInt64(10),
            EvaVersionRow = reader.GetInt32(11),
            EvaActivo = reader.GetInt32(12) == 1
        };
    }

    private static void ValidarDatosEvaluacion(EvaluacionRiesgoDto dto)
    {
        if (dto.EvaRiesgoId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dto.EvaRiesgoId));
        }

        if (dto.EvaVersionId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dto.EvaVersionId));
        }

        if (string.IsNullOrWhiteSpace(dto.EvaDataJson))
        {
            throw new ArgumentException(
                "Las respuestas dinámicas son obligatorias.",
                nameof(dto.EvaDataJson));
        }

        if (string.IsNullOrWhiteSpace(dto.EvaDataCalcJson))
        {
            throw new ArgumentException(
                "Los resultados calculados son obligatorios.",
                nameof(dto.EvaDataCalcJson));
        }

        ValidarJson(dto.EvaDataJson, nameof(dto.EvaDataJson));
        ValidarJson(dto.EvaDataCalcJson, nameof(dto.EvaDataCalcJson));
    }

    private static ProyeccionEvaluacion ConstruirProyeccion(EvaluacionRiesgoDto dto)
    {
        var respuestas = MapearDiccionario(dto.EvaDataJson);
        var calculados = MapearDiccionario(dto.EvaDataCalcJson);

        int vri = dto.EvaVri ?? ObtenerEntero(calculados, "vri")
            ?? throw new InvalidOperationException("No se encontró VRI en el resultado de cálculo.");

        int vrr = dto.EvaVrr ?? ObtenerEntero(calculados, "vrr")
            ?? throw new InvalidOperationException("No se encontró VRR en el resultado de cálculo.");

        if (vri is < 1 or > 9 || vrr is < 1 or > 9)
        {
            throw new InvalidOperationException(
                "VRI y VRR deben estar dentro del dominio institucional 1–9.");
        }

        return new ProyeccionEvaluacion(
            ObtenerTextoRequerido(respuestas, calculados, "area_principal"),
            vri,
            vrr,
            ObtenerTextoRequerido(respuestas, calculados, "nivel_inherente"),
            ObtenerTextoRequerido(respuestas, calculados, "nivel_residual"),
            ObtenerTextoRequerido(respuestas, calculados, "respuesta_riesgo"),
            ObtenerTextoRequerido(respuestas, calculados, "dueno_riesgo"));
    }

    private static Dictionary<string, JsonElement> MapearDiccionario(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)
                ?? new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("El contenido dinámico no es JSON válido.", nameof(json), ex);
        }
    }

    private static string ObtenerTextoRequerido(
        IReadOnlyDictionary<string, JsonElement> respuestas,
        IReadOnlyDictionary<string, JsonElement> calculados,
        string clave)
    {
        if (TryObtenerTexto(calculados, clave, out string? calculado))
        {
            return calculado!;
        }

        if (TryObtenerTexto(respuestas, clave, out string? respuesta))
        {
            return respuesta!;
        }

        throw new InvalidOperationException(
            $"La proyección requiere el campo dinámico '{clave}'.");
    }

    private static bool TryObtenerTexto(
        IReadOnlyDictionary<string, JsonElement> origen,
        string clave,
        out string? valor)
    {
        valor = null;
        if (!origen.TryGetValue(clave, out JsonElement elemento))
        {
            return false;
        }

        valor = elemento.ValueKind switch
        {
            JsonValueKind.String => elemento.GetString(),
            JsonValueKind.Number => elemento.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => elemento.GetRawText()
        };

        return !string.IsNullOrWhiteSpace(valor);
    }

    private static int? ObtenerEntero(
        IReadOnlyDictionary<string, JsonElement> origen,
        string clave)
    {
        if (!origen.TryGetValue(clave, out JsonElement elemento))
        {
            return null;
        }

        if (elemento.ValueKind == JsonValueKind.Number && elemento.TryGetInt32(out int numero))
        {
            return numero;
        }

        return elemento.ValueKind == JsonValueKind.String
            && int.TryParse(elemento.GetString(), out numero)
                ? numero
                : null;
    }

    private static void ValidarJson(string contenido, string nombreParametro)
    {
        try
        {
            using JsonDocument _ = JsonDocument.Parse(contenido);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("El contenido JSON no es válido.", nombreParametro, ex);
        }
    }

    private static string CalcularHash(string contenido)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        return Convert.ToHexString(
            sha.ComputeHash(Encoding.UTF8.GetBytes(contenido)))
            .ToLowerInvariant();
    }

    private static OracleCommand CrearComando(
        string sql,
        OracleConnection conn,
        OracleTransaction? transaction = null)
    {
        var command = new OracleCommand(sql, conn)
        {
            BindByName = true
        };

        if (transaction is not null)
        {
            command.Transaction = transaction;
        }

        return command;
    }

    private static async Task<long> ObtenerSiguienteSecuenciaAsync(
        OracleConnection conn,
        OracleTransaction? transaction,
        string secuencia)
    {
        string sql = $"SELECT {secuencia}.NEXTVAL FROM DUAL";
        await using var cmd = CrearComando(sql, conn, transaction);
        return Convert.ToInt64(await cmd.ExecuteScalarAsync());
    }

    private static async Task<string> ObtenerCodigoRiesgoAsync(
        OracleConnection conn,
        OracleTransaction transaction,
        long riesgoId)
    {
        const string sql = @"
            SELECT RIE_CODIGO
              FROM RL_MR_RIESGOS
             WHERE RIE_ID = :riesgoId
               AND RIE_ACTIVO = 1";

        await using var cmd = CrearComando(sql, conn, transaction);
        cmd.Parameters.Add(new OracleParameter("riesgoId", riesgoId));

        return (await cmd.ExecuteScalarAsync())?.ToString()
            ?? throw new KeyNotFoundException($"No se encontró el riesgo activo {riesgoId}.");
    }

    private static async Task InsertarProyeccionAsync(
        OracleConnection conn,
        OracleTransaction transaction,
        long evaluacionId,
        string codigoRiesgo,
        ProyeccionEvaluacion proyeccion,
        string estado)
    {
        const string sql = @"
            INSERT INTO RL_MR_PROYECCIONES_EVALUACION (
                PROY_ID,
                PROY_EVALUACION_ID,
                PROY_CODIGO_RIESGO,
                PROY_AREA_PRINCIPAL,
                PROY_VRI,
                PROY_VRR,
                PROY_NIVEL_INHERENTE,
                PROY_NIVEL_RESIDUAL,
                PROY_RESPUESTA_RIESGO,
                PROY_ESTADO_EVALUACION,
                PROY_DUENO_RIESGO,
                PROY_FECHA_EVAL
            ) VALUES (
                SEQ_RL_MR_PROYECCIONES.NEXTVAL,
                :evaluacionId,
                :codigoRiesgo,
                :area,
                :vri,
                :vrr,
                :nivelInherente,
                :nivelResidual,
                :respuesta,
                :estado,
                :dueno,
                SYSDATE
            )";

        await using var cmd = CrearComando(sql, conn, transaction);
        cmd.Parameters.Add(new OracleParameter("evaluacionId", evaluacionId));
        cmd.Parameters.Add(new OracleParameter("codigoRiesgo", codigoRiesgo));
        cmd.Parameters.Add(new OracleParameter("area", proyeccion.Area));
        cmd.Parameters.Add(new OracleParameter("vri", proyeccion.Vri));
        cmd.Parameters.Add(new OracleParameter("vrr", proyeccion.Vrr));
        cmd.Parameters.Add(new OracleParameter("nivelInherente", proyeccion.NivelInherente));
        cmd.Parameters.Add(new OracleParameter("nivelResidual", proyeccion.NivelResidual));
        cmd.Parameters.Add(new OracleParameter("respuesta", proyeccion.Respuesta));
        cmd.Parameters.Add(new OracleParameter("estado", estado));
        cmd.Parameters.Add(new OracleParameter("dueno", proyeccion.Dueno));
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<int> ActualizarProyeccionAsync(
        OracleConnection conn,
        OracleTransaction transaction,
        long evaluacionId,
        ProyeccionEvaluacion proyeccion)
    {
        const string sql = @"
            UPDATE RL_MR_PROYECCIONES_EVALUACION
               SET PROY_AREA_PRINCIPAL = :area,
                   PROY_VRI = :vri,
                   PROY_VRR = :vrr,
                   PROY_NIVEL_INHERENTE = :nivelInherente,
                   PROY_NIVEL_RESIDUAL = :nivelResidual,
                   PROY_RESPUESTA_RIESGO = :respuesta,
                   PROY_DUENO_RIESGO = :dueno,
                   PROY_FECHA_EVAL = SYSDATE
             WHERE PROY_EVALUACION_ID = :evaluacionId";

        await using var cmd = CrearComando(sql, conn, transaction);
        cmd.Parameters.Add(new OracleParameter("area", proyeccion.Area));
        cmd.Parameters.Add(new OracleParameter("vri", proyeccion.Vri));
        cmd.Parameters.Add(new OracleParameter("vrr", proyeccion.Vrr));
        cmd.Parameters.Add(new OracleParameter("nivelInherente", proyeccion.NivelInherente));
        cmd.Parameters.Add(new OracleParameter("nivelResidual", proyeccion.NivelResidual));
        cmd.Parameters.Add(new OracleParameter("respuesta", proyeccion.Respuesta));
        cmd.Parameters.Add(new OracleParameter("dueno", proyeccion.Dueno));
        cmd.Parameters.Add(new OracleParameter("evaluacionId", evaluacionId));
        return await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<string> ObtenerEstadoActualAsync(
        OracleConnection conn,
        OracleTransaction transaction,
        long evaluacionId)
    {
        const string sql = @"
            SELECT FLU_ESTADO
              FROM (
                    SELECT FLU_ESTADO
                      FROM RL_MR_FLUJOS_EVALUACION
                     WHERE FLU_EVALUACION_ID = :evaluacionId
                     ORDER BY FLU_FECHA DESC, FLU_ID DESC
                   )
             WHERE ROWNUM = 1";

        await using var cmd = CrearComando(sql, conn, transaction);
        cmd.Parameters.Add(new OracleParameter("evaluacionId", evaluacionId));
        return (await cmd.ExecuteScalarAsync())?.ToString() ?? "BORRADOR";
    }

    private static async Task InsertarFlujoAsync(
        OracleConnection conn,
        OracleTransaction transaction,
        long evaluacionId,
        string estado,
        string? motivo,
        long usuarioId)
    {
        const string sql = @"
            INSERT INTO RL_MR_FLUJOS_EVALUACION (
                FLU_ID,
                FLU_EVALUACION_ID,
                FLU_ESTADO,
                FLU_MOTIVO,
                FLU_USR_ID,
                FLU_FECHA
            ) VALUES (
                SEQ_RL_MR_FLUJOS.NEXTVAL,
                :evaluacionId,
                :estado,
                :motivo,
                :usuarioId,
                SYSDATE
            )";

        await using var cmd = CrearComando(sql, conn, transaction);
        cmd.Parameters.Add(new OracleParameter("evaluacionId", evaluacionId));
        cmd.Parameters.Add(new OracleParameter("estado", estado));
        cmd.Parameters.Add(new OracleParameter("motivo", motivo ?? (object)DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("usuarioId", usuarioId));
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<long> ResolverReglaVersionFormularioAsync(
        OracleConnection conn,
        OracleTransaction transaction,
        long versionFormularioId,
        bool exigirVigente)
    {
        string sqlVersion = @"
            SELECT VER_JSON
              FROM RL_MR_VERSIONES_FORMULARIO
             WHERE VER_ID = :versionId
               AND VER_ESTADO = 'PUBLISHED'";

        if (exigirVigente)
        {
            sqlVersion += " AND VER_VIGENTE = 1";
        }

        await using var cmdVersion = CrearComando(sqlVersion, conn, transaction);
        cmdVersion.Parameters.Add(new OracleParameter("versionId", versionFormularioId));
        object? jsonObj = await cmdVersion.ExecuteScalarAsync();
        if (jsonObj is null)
        {
            throw new InvalidOperationException(
                $"La versión {versionFormularioId} no está publicada"
                + (exigirVigente ? " y vigente." : "."));
        }

        var referencia = ExtraerReferenciaRegla(jsonObj.ToString()!);

        const string sqlRegla = @"
            SELECT REG_ID
              FROM RL_MR_REGLAS_CALCULO
             WHERE REG_CODIGO = :codigo
               AND REG_VERSION = :version
               AND REG_ACTIVA = 1";

        await using var cmdRegla = CrearComando(sqlRegla, conn, transaction);
        cmdRegla.Parameters.Add(new OracleParameter("codigo", referencia.Codigo));
        cmdRegla.Parameters.Add(new OracleParameter("version", referencia.Version));
        object? reglaObj = await cmdRegla.ExecuteScalarAsync();

        return reglaObj is not null
            ? Convert.ToInt64(reglaObj)
            : throw new InvalidOperationException(
                $"La regla {referencia.Codigo} versión {referencia.Version} declarada por el formulario no existe o está inactiva.");
    }

    private static ReferenciaRegla ExtraerReferenciaRegla(string versionJson)
    {
        using JsonDocument document = JsonDocument.Parse(versionJson);
        JsonElement root = document.RootElement;

        foreach (string propertyName in new[] { "reglaCalculo", "regla_calculo" })
        {
            if (TryGetPropertyIgnoreCase(root, propertyName, out JsonElement regla)
                && TryLeerReferenciaRegla(regla, out ReferenciaRegla? referencia))
            {
                return referencia!;
            }
        }

        foreach (string propertyName in new[] { "reglas", "reglasCalculo", "reglas_calculo" })
        {
            if (!TryGetPropertyIgnoreCase(root, propertyName, out JsonElement reglas))
            {
                continue;
            }

            if (reglas.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement regla in reglas.EnumerateArray())
                {
                    if (TryLeerReferenciaRegla(regla, out ReferenciaRegla? referencia))
                    {
                        return referencia!;
                    }
                }
            }
            else if (reglas.ValueKind == JsonValueKind.Object)
            {
                if (TryLeerReferenciaRegla(reglas, out ReferenciaRegla? referenciaDirecta))
                {
                    return referenciaDirecta!;
                }

                foreach (JsonProperty property in reglas.EnumerateObject())
                {
                    if (TryLeerReferenciaRegla(property.Value, out ReferenciaRegla? referencia))
                    {
                        return referencia!;
                    }
                }
            }
        }

        throw new InvalidOperationException(
            "La versión publicada del formulario no declara código y versión de la regla de cálculo.");
    }

    private static bool TryLeerReferenciaRegla(
        JsonElement element,
        out ReferenciaRegla? referencia)
    {
        referencia = null;
        if (element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!TryGetPropertyIgnoreCase(element, "codigo", out JsonElement codigoElement)
            || !TryGetPropertyIgnoreCase(element, "version", out JsonElement versionElement))
        {
            return false;
        }

        string? codigo = codigoElement.GetString();
        string? version = versionElement.GetString();
        if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(version))
        {
            return false;
        }

        referencia = new ReferenciaRegla(codigo.Trim(), version.Trim());
        return true;
    }

    private static bool TryGetPropertyIgnoreCase(
        JsonElement element,
        string propertyName,
        out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (string.Equals(
                    property.Name,
                    propertyName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private static async Task InsertarTrazaCalculoAsync(
        OracleConnection conn,
        OracleTransaction transaction,
        long evaluacionId,
        long reglaId,
        string entradasJson,
        string resultadosJson,
        long usuarioId)
    {
        const string sql = @"
            INSERT INTO RL_MR_TRAZAS_CALCULO (
                TRA_ID,
                TRA_EVALUACION_ID,
                TRA_REGLA_ID,
                TRA_ENTRADAS_JSON,
                TRA_RESULTADOS_JSON,
                TRA_FECHA,
                TRA_USR_ID
            ) VALUES (
                SEQ_RL_MR_TRAZAS.NEXTVAL,
                :evaluacionId,
                :reglaId,
                :entradasJson,
                :resultadosJson,
                SYSDATE,
                :usuarioId
            )";

        await using var cmd = CrearComando(sql, conn, transaction);
        cmd.Parameters.Add(new OracleParameter("evaluacionId", evaluacionId));
        cmd.Parameters.Add(new OracleParameter("reglaId", reglaId));
        cmd.Parameters.Add(new OracleParameter("entradasJson", OracleDbType.Clob) { Value = entradasJson });
        cmd.Parameters.Add(new OracleParameter("resultadosJson", OracleDbType.Clob) { Value = resultadosJson });
        cmd.Parameters.Add(new OracleParameter("usuarioId", usuarioId));
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task InsertarAuditoriaCampoAsync(
        OracleConnection conn,
        OracleTransaction transaction,
        long evaluacionId,
        string campoClave,
        string? valorAnterior,
        string? valorNuevo,
        long usuarioId,
        string? ip)
    {
        const string sql = @"
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
                :campoClave,
                :valorAnterior,
                :valorNuevo,
                :ip,
                :usuarioId,
                SYSDATE
            )";

        await using var cmd = CrearComando(sql, conn, transaction);
        cmd.Parameters.Add(new OracleParameter("evaluacionId", evaluacionId));
        cmd.Parameters.Add(new OracleParameter("campoClave", campoClave));
        cmd.Parameters.Add(new OracleParameter("valorAnterior", OracleDbType.Clob)
        {
            Value = valorAnterior ?? (object)DBNull.Value
        });
        cmd.Parameters.Add(new OracleParameter("valorNuevo", OracleDbType.Clob)
        {
            Value = valorNuevo ?? (object)DBNull.Value
        });
        cmd.Parameters.Add(new OracleParameter("ip", ip ?? (object)DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("usuarioId", usuarioId));
        await cmd.ExecuteNonQueryAsync();
    }

    private sealed record ProyeccionEvaluacion(
        string Area,
        int Vri,
        int Vrr,
        string NivelInherente,
        string NivelResidual,
        string Respuesta,
        string Dueno);

    private sealed record ReferenciaRegla(string Codigo, string Version);
}
