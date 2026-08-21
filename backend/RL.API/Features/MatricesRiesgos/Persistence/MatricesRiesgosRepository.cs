using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;
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
    private readonly IAuditoriaRepository _auditoriaRepository;

    public MatricesRiesgosRepository(
        OracleDbContext db,
        IAuditoriaRepository auditoriaRepository)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _auditoriaRepository = auditoriaRepository
            ?? throw new ArgumentNullException(nameof(auditoriaRepository));
    }

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
        VersionFormularioDto origen = await ObtenerVersionFormularioAsync(versionOrigenId)
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
               AND VER_VIGENTE = 0
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

            // C02: Serialización estricta por familia bloqueando la fila de la familia
            const string sqlLockFam = @"
                SELECT FAM_ID
                  FROM RL_MR_FAMILIAS_FORMULARIO
                 WHERE FAM_ID = :familiaId
                 FOR UPDATE";

            await using (var cmdLockFam = CrearComando(sqlLockFam, conn, trans))
            {
                cmdLockFam.Parameters.Add(new OracleParameter("familiaId", familiaId));
                await cmdLockFam.ExecuteScalarAsync();
            }

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
        await using var trans = conn.BeginTransaction();

        try
        {
            if (vigente)
            {
                const string sqlSelectFam = @"
                    SELECT VER_FAMILIA_ID
                      FROM RL_MR_VERSIONES_FORMULARIO
                     WHERE VER_ID = :versionId
                     FOR UPDATE";

                await using var cmdFam = CrearComando(sqlSelectFam, conn, trans);
                cmdFam.Parameters.Add(new OracleParameter("versionId", versionId));
                object? famIdObj = await cmdFam.ExecuteScalarAsync();
                if (famIdObj is null)
                {
                    await trans.RollbackAsync();
                    return false;
                }

                long familiaId = Convert.ToInt64(famIdObj);

                // C02: Serialización estricta por familia bloqueando la fila de la familia
                const string sqlLockFam = @"
                    SELECT FAM_ID
                      FROM RL_MR_FAMILIAS_FORMULARIO
                     WHERE FAM_ID = :familiaId
                     FOR UPDATE";

                await using (var cmdLockFam = CrearComando(sqlLockFam, conn, trans))
                {
                    cmdLockFam.Parameters.Add(new OracleParameter("familiaId", familiaId));
                    await cmdLockFam.ExecuteScalarAsync();
                }

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
            }

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

            await using var cmd = CrearComando(sql, conn, trans);
            cmd.Parameters.Add(new OracleParameter("vigente", vigente ? 1 : 0));
            cmd.Parameters.Add(new OracleParameter("versionId", versionId));
            bool exito = await cmd.ExecuteNonQueryAsync() > 0;

            if (exito)
            {
                await trans.CommitAsync();
                return true;
            }

            await trans.RollbackAsync();
            return false;
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> EliminarVersionFormularioAsync(long versionId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        // C01: Verificar que sea un borrador DRAFT y no sea la versión activa
        const string sqlCheck = @"
            SELECT VER_VIGENTE, VER_ESTADO
              FROM RL_MR_VERSIONES_FORMULARIO
             WHERE VER_ID = :versionId";

        await using var cmdCheck = CrearComando(sqlCheck, conn);
        cmdCheck.Parameters.Add(new OracleParameter("versionId", versionId));
        await using var reader = await cmdCheck.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return false;
        }

        int vigente = reader.GetInt32(0);
        string estado = reader.GetString(1);
        if (vigente == 1 || !estado.Equals("DRAFT", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // C01: Eliminar únicamente versión DRAFT e inactiva (defensa en profundidad)
        const string sqlDelete = @"
            DELETE FROM RL_MR_VERSIONES_FORMULARIO
             WHERE VER_ID = :versionId
               AND VER_VIGENTE = 0
               AND VER_ESTADO = 'DRAFT'";

        await using var cmdDelete = CrearComando(sqlDelete, conn);
        cmdDelete.Parameters.Add(new OracleParameter("versionId", versionId));
        return await cmdDelete.ExecuteNonQueryAsync() > 0;
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

    public async Task<List<FamiliaFormularioDto>> ListarFamiliasFormularioAsync()
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT f.FAM_ID,
                   f.FAM_CODIGO,
                   f.FAM_NOMBRE,
                   f.FAM_DESCRIPCION,
                   f.FAM_ACTIVO,
                   f.FAM_FECHA_CREACION,
                   (SELECT COUNT(*) FROM RL_MR_VERSIONES_FORMULARIO v WHERE v.VER_FAMILIA_ID = f.FAM_ID) AS TOTAL_VERSIONES,
                   (SELECT COUNT(*) FROM RL_MR_VERSIONES_FORMULARIO v WHERE v.VER_FAMILIA_ID = f.FAM_ID AND v.VER_VIGENTE = 1) AS TIENE_VIGENTE
              FROM RL_MR_FAMILIAS_FORMULARIO f
             ORDER BY f.FAM_CODIGO ASC";

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        var lista = new List<FamiliaFormularioDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(new FamiliaFormularioDto
            {
                FamId = reader.GetInt64(0),
                FamCodigo = reader.GetString(1),
                FamNombre = reader.GetString(2),
                FamDescripcion = reader.IsDBNull(3) ? null : reader.GetString(3),
                FamActivo = reader.GetInt32(4) == 1,
                FamFechaCreacion = reader.GetDateTime(5),
                TotalVersiones = Convert.ToInt32(reader.GetValue(6)),
                TieneVersionVigente = Convert.ToInt32(reader.GetValue(7)) > 0
            });
        }

        return lista;
    }

    public async Task<FamiliaFormularioDto?> ObtenerFamiliaFormularioPorIdAsync(long famId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT f.FAM_ID,
                   f.FAM_CODIGO,
                   f.FAM_NOMBRE,
                   f.FAM_DESCRIPCION,
                   f.FAM_ACTIVO,
                   f.FAM_FECHA_CREACION,
                   (SELECT COUNT(*) FROM RL_MR_VERSIONES_FORMULARIO v WHERE v.VER_FAMILIA_ID = f.FAM_ID) AS TOTAL_VERSIONES,
                   (SELECT COUNT(*) FROM RL_MR_VERSIONES_FORMULARIO v WHERE v.VER_FAMILIA_ID = f.FAM_ID AND v.VER_VIGENTE = 1) AS TIENE_VIGENTE
              FROM RL_MR_FAMILIAS_FORMULARIO f
             WHERE f.FAM_ID = :famId";

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new OracleParameter("famId", famId));

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new FamiliaFormularioDto
            {
                FamId = reader.GetInt64(0),
                FamCodigo = reader.GetString(1),
                FamNombre = reader.GetString(2),
                FamDescripcion = reader.IsDBNull(3) ? null : reader.GetString(3),
                FamActivo = reader.GetInt32(4) == 1,
                FamFechaCreacion = reader.GetDateTime(5),
                TotalVersiones = Convert.ToInt32(reader.GetValue(6)),
                TieneVersionVigente = Convert.ToInt32(reader.GetValue(7)) > 0
            };
        }

        return null;
    }

    public async Task<FamiliaFormularioDto?> ObtenerFamiliaFormularioPorCodigoAsync(string famCodigo)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT f.FAM_ID,
                   f.FAM_CODIGO,
                   f.FAM_NOMBRE,
                   f.FAM_DESCRIPCION,
                   f.FAM_ACTIVO,
                   f.FAM_FECHA_CREACION,
                   (SELECT COUNT(*) FROM RL_MR_VERSIONES_FORMULARIO v WHERE v.VER_FAMILIA_ID = f.FAM_ID) AS TOTAL_VERSIONES,
                   (SELECT COUNT(*) FROM RL_MR_VERSIONES_FORMULARIO v WHERE v.VER_FAMILIA_ID = f.FAM_ID AND v.VER_VIGENTE = 1) AS TIENE_VIGENTE
              FROM RL_MR_FAMILIAS_FORMULARIO f
             WHERE UPPER(f.FAM_CODIGO) = :famCodigo";

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new OracleParameter("famCodigo", (famCodigo ?? string.Empty).Trim().ToUpperInvariant()));

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new FamiliaFormularioDto
            {
                FamId = reader.GetInt64(0),
                FamCodigo = reader.GetString(1),
                FamNombre = reader.GetString(2),
                FamDescripcion = reader.IsDBNull(3) ? null : reader.GetString(3),
                FamActivo = reader.GetInt32(4) == 1,
                FamFechaCreacion = reader.GetDateTime(5),
                TotalVersiones = Convert.ToInt32(reader.GetValue(6)),
                TieneVersionVigente = Convert.ToInt32(reader.GetValue(7)) > 0
            };
        }

        return null;
    }

    public async Task<long> CrearFamiliaFormularioAsync(string famCodigo, string famNombre, string? famDescripcion, bool famActivo)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        const string sqlSeq = "SELECT SEQ_RL_MR_FAMILIAS.NEXTVAL FROM DUAL";
        await using var cmdSeq = conn.CreateCommand();
        cmdSeq.CommandText = sqlSeq;
        long newId = Convert.ToInt64(await cmdSeq.ExecuteScalarAsync());

        const string sqlInsert = @"
            INSERT INTO RL_MR_FAMILIAS_FORMULARIO (
                FAM_ID, FAM_CODIGO, FAM_NOMBRE, FAM_DESCRIPCION, FAM_ACTIVO, FAM_FECHA_CREACION
            ) VALUES (
                :famId, :famCodigo, :famNombre, :famDescripcion, :famActivo, SYSDATE
            )";

        await using var cmdInsert = conn.CreateCommand();
        cmdInsert.CommandText = sqlInsert;
        cmdInsert.Parameters.Add(new OracleParameter("famId", newId));
        cmdInsert.Parameters.Add(new OracleParameter("famCodigo", (famCodigo ?? string.Empty).Trim().ToUpperInvariant()));
        cmdInsert.Parameters.Add(new OracleParameter("famNombre", (famNombre ?? string.Empty).Trim()));
        cmdInsert.Parameters.Add(new OracleParameter("famDescripcion", (object?)famDescripcion?.Trim() ?? DBNull.Value));
        cmdInsert.Parameters.Add(new OracleParameter("famActivo", famActivo ? 1 : 0));

        await cmdInsert.ExecuteNonQueryAsync();
        return newId;
    }

    public async Task<bool> ActualizarFamiliaFormularioAsync(long famId, string famNombre, string? famDescripcion, bool famActivo)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            UPDATE RL_MR_FAMILIAS_FORMULARIO
               SET FAM_NOMBRE = :famNombre,
                   FAM_DESCRIPCION = :famDescripcion,
                   FAM_ACTIVO = :famActivo
             WHERE FAM_ID = :famId";

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new OracleParameter("famNombre", (famNombre ?? string.Empty).Trim()));
        cmd.Parameters.Add(new OracleParameter("famDescripcion", (object?)famDescripcion?.Trim() ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("famActivo", famActivo ? 1 : 0));
        cmd.Parameters.Add(new OracleParameter("famId", famId));

        int rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    public async Task<bool> DesactivarFamiliaFormularioAtomicoAsync(long famId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();

        try
        {
            const string sqlCheckVigente = @"
                SELECT COUNT(*)
                  FROM RL_MR_VERSIONES_FORMULARIO
                 WHERE VER_FAMILIA_ID = :famId
                   AND VER_VIGENTE = 1";

            await using (var cmdCheck = conn.CreateCommand())
            {
                cmdCheck.Transaction = (OracleTransaction)tx;
                cmdCheck.CommandText = sqlCheckVigente;
                cmdCheck.Parameters.Add(new OracleParameter("famId", famId));

                int vigentes = Convert.ToInt32(await cmdCheck.ExecuteScalarAsync());
                if (vigentes > 0)
                {
                    await tx.RollbackAsync();
                    return false;
                }
            }

            const string sqlUpdate = @"
                UPDATE RL_MR_FAMILIAS_FORMULARIO
                   SET FAM_ACTIVO = 0
                 WHERE FAM_ID = :famId";

            int rows;
            await using (var cmdUpdate = conn.CreateCommand())
            {
                cmdUpdate.Transaction = (OracleTransaction)tx;
                cmdUpdate.CommandText = sqlUpdate;
                cmdUpdate.Parameters.Add(new OracleParameter("famId", famId));

                rows = await cmdUpdate.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
            return rows > 0;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<EvaluacionRiesgoDto?> ObtenerEvaluacionAsync(long evaId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT e.EVA_ID,
                   e.EVA_RIESGO_ID,
                   e.EVA_VERSION_ID,
                   NVL(f.FLU_ESTADO, 'BORRADOR'),
                   e.EVA_DATOS_JSON,
                   e.EVA_CALCULOS_JSON,
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
    }    public async Task<EvaluacionesPaginadasDto> ListarEvaluacionesPaginadasAsync(
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

        var whereSql = new StringBuilder(@"
             WHERE e.EVA_ACTIVO = 1");

        var parameters = new List<OracleParameter>();
        if (filtro.RiesgoId.HasValue)
        {
            whereSql.Append(" AND e.EVA_RIESGO_ID = :riesgoId");
            parameters.Add(new OracleParameter("riesgoId", filtro.RiesgoId.Value));
        }
        if (!string.IsNullOrWhiteSpace(filtro.Estado))
        {
            whereSql.Append(" AND NVL(f.FLU_ESTADO, 'BORRADOR') = :estado");
            parameters.Add(new OracleParameter("estado", filtro.Estado.Trim().ToUpperInvariant()));
        }
        if (!string.IsNullOrWhiteSpace(filtro.Area))
        {
            whereSql.Append(" AND p.PROY_AREA_PRINCIPAL = :area");
            parameters.Add(new OracleParameter("area", filtro.Area.Trim()));
        }
        if (!string.IsNullOrWhiteSpace(filtro.NivelResidual))
        {
            whereSql.Append(" AND p.PROY_NIVEL_RESIDUAL = :nivelResidual");
            parameters.Add(new OracleParameter("nivelResidual", filtro.NivelResidual.Trim()));
        }
        if (!string.IsNullOrWhiteSpace(filtro.Buscar))
        {
            string busqueda = $"%{filtro.Buscar.Trim().ToUpperInvariant()}%";
            whereSql.Append(@" AND (
                UPPER(r.RIE_CODIGO) LIKE :buscar
                OR UPPER(r.RIE_NOMBRE) LIKE :buscar
                OR UPPER(v.VER_CODIGO) LIKE :buscar
                OR TO_CHAR(e.EVA_ID) LIKE :buscar
            )");
            parameters.Add(new OracleParameter("buscar", busqueda));
        }

        const string joinsSql = @"
              FROM RL_MR_EVALUACIONES_RIESGO e
              JOIN RL_MR_RIESGOS r
                ON r.RIE_ID = e.EVA_RIESGO_ID
              JOIN RL_MR_VERSIONES_FORMULARIO v
                ON v.VER_ID = e.EVA_VERSION_ID
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
                ON f.FLU_EVALUACION_ID = e.EVA_ID";

        string countQuery = $@"
            SELECT COUNT(*)
            {joinsSql}
            {whereSql}";

        await using var cmdCount = CrearComando(countQuery, conn);
        foreach (OracleParameter parameter in parameters)
        {
            cmdCount.Parameters.Add(new OracleParameter(parameter.ParameterName, parameter.Value));
        }
        int totalRegistros = Convert.ToInt32(await cmdCount.ExecuteScalarAsync());

        int paginaEfectiva = PaginacionEvaluacionesHelper.CalcularPaginaEfectiva(
            totalRegistros,
            filtro.RegistrosPorPagina,
            filtro.Pagina);

        int offset = (paginaEfectiva - 1) * filtro.RegistrosPorPagina;
        string selectDataSql = $@"
            SELECT e.EVA_ID,
                   e.EVA_RIESGO_ID,
                   r.RIE_CODIGO,
                   r.RIE_NOMBRE,
                   e.EVA_VERSION_ID,
                   v.VER_CODIGO,
                   v.VER_VERSION,
                   NVL(f.FLU_ESTADO, 'BORRADOR') AS ESTADO,
                   p.PROY_VRI,
                   p.PROY_VRR,
                   p.PROY_NIVEL_RESIDUAL,
                   e.EVA_FECHA_REGISTRO
            {joinsSql}
            {whereSql}";

        string paginatedQuery = $@"
            SELECT *
              FROM (
                    SELECT q.*, ROWNUM NUMERO_FILA
                      FROM (
                            {selectDataSql}
                            ORDER BY e.EVA_FECHA_REGISTRO DESC, e.EVA_ID DESC
                           ) q
                     WHERE ROWNUM <= :filaFinal
                   )
             WHERE NUMERO_FILA > :filaInicial";

        await using var cmd = CrearComando(paginatedQuery, conn);
        foreach (OracleParameter parameter in parameters)
        {
            cmd.Parameters.Add(new OracleParameter(parameter.ParameterName, parameter.Value));
        }
        cmd.Parameters.Add(new OracleParameter("filaFinal", offset + filtro.RegistrosPorPagina));
        cmd.Parameters.Add(new OracleParameter("filaInicial", offset));

        var lista = new List<EvaluacionRiesgoResumenDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(new EvaluacionRiesgoResumenDto
            {
                EvaId = reader.GetInt64(0),
                EvaRiesgoId = reader.GetInt64(1),
                RiesgoCodigo = reader.GetString(2),
                RiesgoNombre = reader.GetString(3),
                EvaVersionId = reader.GetInt64(4),
                VersionCodigo = reader.GetString(5),
                VersionNumero = reader.GetInt32(6),
                Estado = reader.GetString(7),
                Vri = reader.IsDBNull(8) ? null : Convert.ToInt32(reader.GetValue(8)),
                Vrr = reader.IsDBNull(9) ? null : Convert.ToInt32(reader.GetValue(9)),
                NivelResidual = reader.IsDBNull(10) ? null : reader.GetString(10),
                FechaEval = reader.GetDateTime(11)
            });
        }

        return new EvaluacionesPaginadasDto
        {
            Items = lista,
            Pagina = paginaEfectiva,
            RegistrosPorPagina = filtro.RegistrosPorPagina,
            TotalRegistros = totalRegistros
        };
    }

    public async Task<long> CrearEvaluacionAsync(EvaluacionRiesgoDto dto, long usuarioId, string? ip)
    {
        ValidarDatosEvaluacion(dto);

        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var trans = conn.BeginTransaction();

        try
        {
            ReglaCalculoResuelta regla = await ResolverReglaVersionFormularioAsync(
                conn,
                trans,
                dto.EvaVersionId,
                exigirVigente: true);
            string calculosJson = IncorporarMetadatosRegla(
                dto.EvaDataCalcJson,
                regla.Codigo,
                regla.Version,
                regla.AlgoritmoId);
            long evaluacionId = await ObtenerSiguienteSecuenciaAsync(conn, trans, "SEQ_RL_MR_EVALUACIONES");
            ProyeccionEvaluacion proyeccion = ConstruirProyeccion(dto);
            string codigoRiesgo = await ObtenerCodigoRiesgoAsync(conn, trans, dto.EvaRiesgoId);

            const string sqlInsert = @"
                INSERT INTO RL_MR_EVALUACIONES_RIESGO (
                    EVA_ID,
                    EVA_RIESGO_ID,
                    EVA_VERSION_ID,
                    EVA_DATOS_JSON,
                    EVA_CALCULOS_JSON,
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
            cmdInsert.Parameters.Add(new OracleParameter("evaId", evaluacionId));
            cmdInsert.Parameters.Add(new OracleParameter("riesgoId", dto.EvaRiesgoId));
            cmdInsert.Parameters.Add(new OracleParameter("versionId", dto.EvaVersionId));
            cmdInsert.Parameters.Add(new OracleParameter("dataJson", OracleDbType.Clob) { Value = dto.EvaDataJson });
            cmdInsert.Parameters.Add(new OracleParameter("dataCalcJson", OracleDbType.Clob) { Value = calculosJson });
            cmdInsert.Parameters.Add(new OracleParameter("usuarioId", usuarioId));
            await cmdInsert.ExecuteNonQueryAsync();

            await InsertarProyeccionAsync(conn, trans, evaluacionId, codigoRiesgo, proyeccion, "BORRADOR");
            await InsertarFlujoAsync(conn, trans, evaluacionId, "BORRADOR", "Creación inicial", usuarioId);
            await _auditoriaRepository.RegistrarAsync(
        conn,
        trans,
        "RL_MR_EVALUACIONES_RIESGO",
        evaluacionId.ToString(),
        "INSERT",
        null,
        JsonSerializer.Serialize(new
        {
            dto.EvaRiesgoId,
            dto.EvaVersionId,
            Datos = dto.EvaDataJson,
            Calculos = calculosJson
        }),
        usuarioId,
        null,
        ip,
        ModuloAuditoria);

            await trans.CommitAsync();
            return evaluacionId;
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
                SELECT e.EVA_DATOS_JSON,
                       e.EVA_VERSION_ROW,
                       e.EVA_VERSION_ID,
                       NVL(f.FLU_ESTADO, 'BORRADOR') AS ESTADO
                  FROM RL_MR_EVALUACIONES_RIESGO e
                  LEFT JOIN (
                        SELECT FLU_EVALUACION_ID, FLU_ESTADO
                          FROM (
                                SELECT FLU_EVALUACION_ID,
                                       FLU_ESTADO,
                                       ROW_NUMBER() OVER (PARTITION BY FLU_EVALUACION_ID ORDER BY FLU_ID DESC) rn
                                  FROM RL_MR_FLUJO_EVALUACION
                               )
                         WHERE rn = 1
                      ) f ON f.FLU_EVALUACION_ID = e.EVA_ID
                 WHERE e.EVA_ID = :evaId
                   AND e.EVA_ACTIVO = 1
                 FOR UPDATE OF e.EVA_ID";

            await using var cmdSelect = CrearComando(sqlSelect, conn, trans);
            cmdSelect.Parameters.Add(new OracleParameter("evaId", dto.EvaId));

            string jsonAnterior;
            int versionRowActual;
            long versionFormularioId;
            string estadoPersistido;
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
                estadoPersistido = reader.GetString(3);
            }

            if (!string.Equals(estadoPersistido, "BORRADOR", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"No se permite modificar una evaluación en estado '{estadoPersistido}'. Solo se permite editar evaluaciones en estado BORRADOR.");
            }

            if (versionRowActual != dto.EvaVersionRow)
            {
                throw new DBConcurrencyException($"Conflicto de modificación concurrente en la evaluación {dto.EvaId}.");
            }

            ReglaCalculoResuelta regla = await ResolverReglaVersionFormularioAsync(
                conn,
                trans,
                versionFormularioId,
                exigirVigente: false);
            string calculosJson = IncorporarMetadatosRegla(
                dto.EvaDataCalcJson,
                regla.Codigo,
                regla.Version,
                regla.AlgoritmoId);

            const string sqlUpdate = @"
                UPDATE RL_MR_EVALUACIONES_RIESGO
                   SET EVA_DATOS_JSON = :dataJson,
                       EVA_CALCULOS_JSON = :dataCalcJson,
                       EVA_VERSION_ROW = :nuevaVersionRow
                 WHERE EVA_ID = :evaId
                   AND EVA_VERSION_ROW = :versionRow
                   AND EVA_ACTIVO = 1";

            await using var cmdUpdate = CrearComando(sqlUpdate, conn, trans);
            cmdUpdate.Parameters.Add(new OracleParameter("dataJson", OracleDbType.Clob) { Value = dto.EvaDataJson });
            cmdUpdate.Parameters.Add(new OracleParameter("dataCalcJson", OracleDbType.Clob) { Value = calculosJson });
            cmdUpdate.Parameters.Add(new OracleParameter("nuevaVersionRow", versionRowActual + 1));
            cmdUpdate.Parameters.Add(new OracleParameter("evaId", dto.EvaId));
            cmdUpdate.Parameters.Add(new OracleParameter("versionRow", versionRowActual));
            if (await cmdUpdate.ExecuteNonQueryAsync() != 1)
            {
                throw new DBConcurrencyException($"No se pudo actualizar la evaluación {dto.EvaId} por un conflicto de concurrencia.");
            }

            ProyeccionEvaluacion proyeccion = ConstruirProyeccion(dto);
            int actualizadas = await ActualizarProyeccionAsync(conn, trans, dto.EvaId, proyeccion);
            if (actualizadas != 1)
            {
                throw new InvalidOperationException($"La evaluación {dto.EvaId} debe tener exactamente una proyección; se actualizaron {actualizadas}.");
            }

            await _auditoriaRepository.RegistrarAsync(
        conn,
        trans,
        "RL_MR_EVALUACIONES_RIESGO",
        dto.EvaId.ToString(),
        "UPDATE",
        JsonSerializer.Serialize(new
        {
            Datos = jsonAnterior,
            VersionRow = versionRowActual
        }),
        JsonSerializer.Serialize(new
        {
            Datos = dto.EvaDataJson,
            Calculos = calculosJson,
            VersionRow = versionRowActual + 1
        }),
        usuarioId,
        null,
        ip,
        ModuloAuditoria);

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
        string estado = nuevoEstado.Trim().ToUpperInvariant();
        if (!EstadosEvaluacionPermitidos.Contains(estado))
        {
            throw new ArgumentException($"Estado de evaluación no permitido: {nuevoEstado}.", nameof(nuevoEstado));
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

            string anterior = await ObtenerEstadoActualAsync(conn, trans, evaId);

            const string sqlProyeccion = @"
                UPDATE RL_MR_PROYECCIONES_EVALUACION
                   SET PROY_ESTADO_EVALUACION = :estado
                 WHERE PROY_EVALUACION_ID = :evaId";

            await using var cmdProyeccion = CrearComando(sqlProyeccion, conn, trans);
            cmdProyeccion.Parameters.Add(new OracleParameter("estado", estado));
            cmdProyeccion.Parameters.Add(new OracleParameter("evaId", evaId));
            if (await cmdProyeccion.ExecuteNonQueryAsync() != 1)
            {
                throw new InvalidOperationException($"No se encontró una proyección única para la evaluación {evaId}.");
            }

            await InsertarFlujoAsync(conn, trans, evaId, estado, motivo, usuarioId);
            await _auditoriaRepository.RegistrarAsync(
        conn,
        trans,
        "RL_MR_EVALUACIONES_RIESGO",
        evaId.ToString(),
        "UPDATE",
        JsonSerializer.Serialize(new { Estado = anterior }),
        JsonSerializer.Serialize(new { Estado = estado, Motivo = motivo }),
        usuarioId,
        null,
        ip,
        ModuloAuditoria);
            await trans.CommitAsync();
            return true;
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }

    public async Task<List<FlujoEvaluacionDto>> ObtenerFlujosEvaluacionAsync(long evaId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        const string sql = @"SELECT FLU_ID, FLU_EVALUACION_ID, FLU_ESTADO, FLU_MOTIVO, FLU_USR_ID, FLU_FECHA
                               FROM RL_MR_FLUJOS_EVALUACION WHERE FLU_EVALUACION_ID = :evaId
                              ORDER BY FLU_FECHA DESC, FLU_ID DESC";
        await using var cmd = CrearComando(sql, conn);
        cmd.Parameters.Add(new OracleParameter("evaId", evaId));
        var lista = new List<FlujoEvaluacionDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(new FlujoEvaluacionDto { FluId = reader.GetInt64(0), FluEvaluacionId = reader.GetInt64(1), FluEstado = reader.GetString(2), FluMotivo = reader.IsDBNull(3) ? null : reader.GetString(3), FluUsrId = reader.GetInt64(4), FluFecha = reader.GetDateTime(5) });
        }
        return lista;
    }

    public async Task<long> RegistrarEvidenciaFisicaAsync(EvidenciaRegistroDto dto, long usuarioId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        long evidenciaId = await ObtenerSiguienteSecuenciaAsync(conn, transaction: null, "SEQ_RL_MR_EVIDENCIAS");

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
        cmd.Parameters.Add(new OracleParameter("evidenciaId", evidenciaId));
        cmd.Parameters.Add(new OracleParameter("nombre", dto.EviNombreArchivo));
        cmd.Parameters.Add(new OracleParameter("extension", dto.EviExtension));
        cmd.Parameters.Add(new OracleParameter("tamano", dto.EviTamano));
        cmd.Parameters.Add(new OracleParameter("hash", dto.EviHash));
        cmd.Parameters.Add(new OracleParameter("ruta", dto.EviRuta));
        cmd.Parameters.Add(new OracleParameter("usuarioId", usuarioId));
        await cmd.ExecuteNonQueryAsync();
        return evidenciaId;
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

    public async Task<bool> VincularEvidenciaAsync(VincularEvidenciaDto dto, long usuarioId, string? ip)
    {
        string sqlEntidad = ObtenerConsultaEntidadEvidencia(dto.TipoEntidad);
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var trans = conn.BeginTransaction();

        try
        {
            await using (var cmdEvidencia = CrearComando("SELECT EVI_ID FROM RL_MR_EVIDENCIAS WHERE EVI_ID = :id", conn, trans))
            {
                cmdEvidencia.Parameters.Add(new OracleParameter("id", dto.EvidenciaId));
                if (await cmdEvidencia.ExecuteScalarAsync() is null)
                    throw new KeyNotFoundException($"No se encontró la evidencia {dto.EvidenciaId}.");
            }

            await using (var cmdEntidad = CrearComando(sqlEntidad, conn, trans))
            {
                cmdEntidad.Parameters.Add(new OracleParameter("id", dto.EntidadId));
                if (await cmdEntidad.ExecuteScalarAsync() is null)
                    throw new KeyNotFoundException($"No se encontró la entidad {dto.TipoEntidad} con ID {dto.EntidadId}.");
            }

            long vinculoId = await ObtenerSiguienteSecuenciaAsync(conn, trans, "SEQ_RL_MR_EVI_VINCULOS");
            const string sqlInsert = @"
                INSERT INTO RL_MR_EVIDENCIAS_VINCULOS (
                    EVV_ID, EVV_EVIDENCIA_ID, EVV_TIPO_ENTIDAD, EVV_ENTIDAD_ID, EVV_USR_CREACION
                ) VALUES (:id, :evidenciaId, :tipo, :entidadId, :usuarioId)";
            await using (var cmdInsert = CrearComando(sqlInsert, conn, trans))
            {
                cmdInsert.Parameters.Add(new OracleParameter("id", vinculoId));
                cmdInsert.Parameters.Add(new OracleParameter("evidenciaId", dto.EvidenciaId));
                cmdInsert.Parameters.Add(new OracleParameter("tipo", dto.TipoEntidad.ToString().ToUpperInvariant()));
                cmdInsert.Parameters.Add(new OracleParameter("entidadId", dto.EntidadId));
                cmdInsert.Parameters.Add(new OracleParameter("usuarioId", usuarioId));
                await cmdInsert.ExecuteNonQueryAsync();
            }

            await _auditoriaRepository.RegistrarAsync(
                conn, trans, "RL_MR_EVIDENCIAS_VINCULOS", vinculoId.ToString(), "INSERT",
                null, JsonSerializer.Serialize(new { dto.EvidenciaId, TipoEntidad = dto.TipoEntidad.ToString(), dto.EntidadId }),
                usuarioId, null, ip, ModuloAuditoria);
            await trans.CommitAsync();
            return true;
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }

    private static string ObtenerConsultaEntidadEvidencia(TipoEntidadEvidencia tipo) => tipo switch
    {
        TipoEntidadEvidencia.Riesgo => "SELECT RIE_ID FROM RL_MR_RIESGOS WHERE RIE_ID = :id",
        TipoEntidadEvidencia.Evaluacion => "SELECT EVA_ID FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_ID = :id",
        TipoEntidadEvidencia.Control => "SELECT CON_ID FROM RL_MR_CONTROLES_RIESGO WHERE CON_ID = :id",
        TipoEntidadEvidencia.Plan => "SELECT PLA_ID FROM RL_MR_PLANES WHERE PLA_ID = :id",
        TipoEntidadEvidencia.Actividad => "SELECT ACT_ID FROM RL_MR_ACTIVIDADES WHERE ACT_ID = :id",
        TipoEntidadEvidencia.Alerta => "SELECT ALE_ID FROM RL_MR_SENALES_ALERTA WHERE ALE_ID = :id",
        TipoEntidadEvidencia.Automonitoreo => "SELECT MON_ID FROM RL_MR_AUTOMONITOREO WHERE MON_ID = :id",
        _ => throw new InvalidOperationException("El tipo de entidad de evidencia no está permitido.")
    };

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
                SELECT COUNT(*)
                  FROM RL_MR_EVIDENCIAS_VINCULOS
                 WHERE EVV_EVIDENCIA_ID = :evidenciaId";

            await using var cmdVinculos = CrearComando(sqlVinculos, conn, trans);
            cmdVinculos.Parameters.Add(new OracleParameter("evidenciaId", evidenciaId));
            if (Convert.ToInt32(await cmdVinculos.ExecuteScalarAsync()) > 0)
            {
                await trans.RollbackAsync();
                return ResultadoEliminacionEvidencia.TieneVinculos;
            }

            const string sqlDelete = "DELETE FROM RL_MR_EVIDENCIAS WHERE EVI_ID = :evidenciaId";
            await using var cmdDelete = CrearComando(sqlDelete, conn, trans);
            cmdDelete.Parameters.Add(new OracleParameter("evidenciaId", evidenciaId));
            await cmdDelete.ExecuteNonQueryAsync();

            bool eliminado;
            try
            {
                eliminado = await eliminarArchivoFisico();
            }
            catch
            {
                eliminado = false;
            }

            if (!eliminado)
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

    public async Task<IReadOnlyList<RiesgoReporteFilaDto>> ObtenerConsolidadoTipadoAsync()
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT e.EVA_RIESGO_ID,
                   p.PROY_EVALUACION_ID,
                   e.EVA_VERSION_ID,
                   p.PROY_CODIGO_RIESGO,
                   p.PROY_AREA_PRINCIPAL,
                   p.PROY_DUENO_RIESGO,
                   p.PROY_VRI,
                   p.PROY_NIVEL_INHERENTE,
                   p.PROY_VRR,
                   p.PROY_NIVEL_RESIDUAL,
                   p.PROY_RESPUESTA_RIESGO,
                   p.PROY_ESTADO_EVALUACION,
                   p.PROY_FECHA_EVAL
              FROM RL_MR_PROYECCIONES_EVALUACION p
              JOIN RL_MR_EVALUACIONES_RIESGO e
                ON e.EVA_ID = p.PROY_EVALUACION_ID
             WHERE e.EVA_ACTIVO = 1
             ORDER BY p.PROY_FECHA_EVAL DESC, p.PROY_EVALUACION_ID DESC";

        await using var cmd = CrearComando(sql, conn);
        var lista = new List<RiesgoReporteFilaDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(new RiesgoReporteFilaDto
            {
                RiesgoId = reader.GetInt64(0),
                EvaluacionId = reader.GetInt64(1),
                VersionFormularioId = reader.GetInt64(2),
                CodigoRiesgo = reader.GetString(3),
                AreaPrincipal = reader.GetString(4),
                DuenoRiesgo = reader.GetString(5),
                Vri = reader.GetInt32(6),
                NivelInherente = reader.GetString(7),
                Vrr = reader.GetInt32(8),
                NivelResidual = reader.GetString(9),
                RespuestaRiesgo = reader.GetString(10),
                EstadoEvaluacion = reader.GetString(11),
                FechaEvaluacion = reader.GetDateTime(12)
            });
        }
        return lista;
    }

    public async Task<MetodologiaFormularioDto?> ObtenerMetodologiaDinamicaVigenteAsync()
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT VER_ID, VER_CODIGO, VER_VERSION, VER_JSON
              FROM (
                    SELECT VER_ID, VER_CODIGO, VER_VERSION, VER_JSON
                      FROM RL_MR_VERSIONES_FORMULARIO
                     WHERE VER_ESTADO = 'PUBLISHED'
                       AND VER_VIGENTE = 1
                     ORDER BY VER_FECHA_INICIO DESC, VER_ID DESC
                   )
             WHERE ROWNUM = 1";

        await using var cmd = CrearComando(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        long versionId = reader.GetInt64(0);
        string codigo = reader.GetString(1);
        int version = reader.GetInt32(2);
        string definicion = reader.GetString(3);
        return ConstruirMetodologiaDinamica(versionId, codigo, version, definicion);
    }

    public async Task<MetodologiaFormularioDto?> ObtenerMetodologiaDinamicaPorVersionAsync(long versionId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT VER_ID, VER_CODIGO, VER_VERSION, VER_JSON
              FROM RL_MR_VERSIONES_FORMULARIO
             WHERE VER_ID = :versionId";

        await using var cmd = CrearComando(sql, conn);
        cmd.Parameters.Add(new OracleParameter("versionId", versionId));
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        long id = reader.GetInt64(0);
        string codigo = reader.GetString(1);
        int version = reader.GetInt32(2);
        string definicion = reader.GetString(3);
        return ConstruirMetodologiaDinamica(id, codigo, version, definicion);
    }

    private static MetodologiaFormularioDto ConstruirMetodologiaDinamica(
        long versionId,
        string codigo,
        int version,
        string definicion)
    {
        using JsonDocument document = JsonDocument.Parse(definicion);
        JsonElement root = document.RootElement;
        var secciones = new List<SeccionFormularioDto>();
        var catalogos = new List<CatalogoMatricesDto>();
        var reglas = new List<ReglaCalculoMatricesDto>();

        if (TryGetPropertyIgnoreCase(root, "secciones", out JsonElement seccionesElement)
            && seccionesElement.ValueKind == JsonValueKind.Array)
        {
            int indiceSeccion = 0;
            foreach (JsonElement seccionElement in seccionesElement.EnumerateArray())
            {
                indiceSeccion++;
                var campos = new List<CampoFormularioDto>();
                if (TryGetPropertyIgnoreCase(seccionElement, "campos", out JsonElement camposElement)
                    && camposElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement campoElement in camposElement.EnumerateArray())
                    {
                        campos.Add(new CampoFormularioDto
                        {
                            CampoCanonicoId = LeerLongNullable(campoElement, "campoCanonicoId"),
                            Clave = LeerTexto(campoElement, "clave", LeerTexto(campoElement, "id", string.Empty)),
                            Etiqueta = LeerTexto(campoElement, "etiqueta", string.Empty),
                            Tipo = LeerTexto(campoElement, "tipo", "texto"),
                            CodigoCatalogo = LeerTextoNullable(campoElement, "codigoCatalogo")
                                ?? LeerTextoNullable(campoElement, "catalogoCodigo"),
                            Obligatorio = LeerBooleano(campoElement, "obligatorio", false),
                            SoloLectura = LeerBooleano(campoElement, "soloLectura", false)
                        });
                    }
                }

                secciones.Add(new SeccionFormularioDto
                {
                    Clave = LeerTexto(seccionElement, "clave", LeerTexto(seccionElement, "id", $"seccion_{indiceSeccion}")),
                    Titulo = LeerTexto(seccionElement, "titulo", $"Sección {indiceSeccion}"),
                    Orden = LeerEntero(seccionElement, "orden", indiceSeccion),
                    Campos = campos
                });
            }
        }

        if (TryGetPropertyIgnoreCase(root, "catalogos", out JsonElement catalogosElement)
            && catalogosElement.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement catalogoElement in catalogosElement.EnumerateArray())
            {
                var elementos = new List<ElementoCatalogoMatricesDto>();
                if (TryGetPropertyIgnoreCase(catalogoElement, "elementos", out JsonElement elementosElement)
                    && elementosElement.ValueKind == JsonValueKind.Array)
                {
                    int orden = 0;
                    foreach (JsonElement elemento in elementosElement.EnumerateArray())
                    {
                        orden++;
                        elementos.Add(new ElementoCatalogoMatricesDto
                        {
                            Codigo = LeerTexto(elemento, "codigo", string.Empty),
                            Valor = LeerTexto(elemento, "valor", LeerTexto(elemento, "nombre", string.Empty)),
                            Orden = LeerEntero(elemento, "orden", orden)
                        });
                    }
                }

                catalogos.Add(new CatalogoMatricesDto
                {
                    Codigo = LeerTexto(catalogoElement, "codigo", string.Empty),
                    Nombre = LeerTexto(catalogoElement, "nombre", string.Empty),
                    Elementos = elementos
                });
            }
        }

        foreach (string nombrePropiedad in new[] { "reglas", "reglasCalculo", "reglas_calculo" })
        {
            if (!TryGetPropertyIgnoreCase(root, nombrePropiedad, out JsonElement reglasElement))
            {
                continue;
            }

            IEnumerable<JsonElement> elementosRegla = reglasElement.ValueKind == JsonValueKind.Array
                ? reglasElement.EnumerateArray()
                : reglasElement.ValueKind == JsonValueKind.Object
                    ? new[] { reglasElement }
                    : Array.Empty<JsonElement>();

            foreach (JsonElement reglaElement in elementosRegla)
            {
                string reglaCodigo = LeerTexto(reglaElement, "codigo", string.Empty);
                string reglaVersion = LeerTexto(reglaElement, "version", string.Empty);
                if (string.IsNullOrWhiteSpace(reglaCodigo) || string.IsNullOrWhiteSpace(reglaVersion))
                {
                    continue;
                }

                JsonElement? parametros = TryGetPropertyIgnoreCase(reglaElement, "parametros", out JsonElement parametrosElement)
                    ? parametrosElement.Clone()
                    : null;

                reglas.Add(new ReglaCalculoMatricesDto
                {
                    Codigo = reglaCodigo,
                    Version = reglaVersion,
                    AlgoritmoId = LeerTexto(reglaElement, "algoritmoId", string.Empty),
                    Parametros = parametros
                });
            }
            break;
        }

        return new MetodologiaFormularioDto
        {
            VersionFormularioId = versionId,
            Codigo = codigo,
            Version = version,
            Secciones = secciones,
            Catalogos = catalogos,
            Reglas = reglas
        };
    }

    private static VersionFormularioDto MapearVersionFormulario(OracleDataReader reader) => new()
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

    private static EvaluacionRiesgoDto MapearEvaluacion(OracleDataReader reader) => new()
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

    private static void ValidarDatosEvaluacion(EvaluacionRiesgoDto dto)
    {
        if (dto.EvaRiesgoId <= 0) throw new ArgumentOutOfRangeException(nameof(dto.EvaRiesgoId));
        if (dto.EvaVersionId <= 0) throw new ArgumentOutOfRangeException(nameof(dto.EvaVersionId));
        if (string.IsNullOrWhiteSpace(dto.EvaDataJson)) throw new ArgumentException("Las respuestas dinámicas son obligatorias.", nameof(dto.EvaDataJson));
        if (string.IsNullOrWhiteSpace(dto.EvaDataCalcJson)) throw new ArgumentException("Los resultados calculados son obligatorios.", nameof(dto.EvaDataCalcJson));
        ValidarJson(dto.EvaDataJson, nameof(dto.EvaDataJson));
        ValidarJson(dto.EvaDataCalcJson, nameof(dto.EvaDataCalcJson));
    }

    private static ProyeccionEvaluacion ConstruirProyeccion(EvaluacionRiesgoDto dto)
    {
        Dictionary<string, JsonElement> respuestas = MapearDiccionario(dto.EvaDataJson);
        Dictionary<string, JsonElement> calculados = MapearDiccionario(dto.EvaDataCalcJson);
        int vri = dto.EvaVri ?? ObtenerEntero(calculados, "vri")
            ?? throw new InvalidOperationException("No se encontró VRI en el resultado de cálculo.");
        int vrr = dto.EvaVrr ?? ObtenerEntero(calculados, "vrr")
            ?? throw new InvalidOperationException("No se encontró VRR en el resultado de cálculo.");
        if (vri is < 1 or > 9 || vrr is < 1 or > 9)
        {
            throw new InvalidOperationException("VRI y VRR deben estar dentro del dominio institucional 1–9.");
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

    private static string IncorporarMetadatosRegla(
        string calculosJson,
        string reglaCodigo,
        string reglaVersion,
        string algoritmoId)
    {
        JsonNode? raiz;
        try
        {
            raiz = JsonNode.Parse(calculosJson);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Los resultados calculados no contienen JSON válido.", nameof(calculosJson), ex);
        }

        if (raiz is not JsonObject calculos)
        {
            throw new ArgumentException("Los resultados calculados deben ser un objeto JSON.", nameof(calculosJson));
        }

        // Estos metadatos proceden de la versión publicada y del catálogo de reglas.
        // Se sobrescribe cualquier valor remitido por el cliente para impedir suplantación.
        calculos["reglaCodigo"] = reglaCodigo;
        calculos["reglaVersion"] = reglaVersion;
        calculos["algoritmoId"] = algoritmoId;

        return calculos.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    private static string ObtenerTextoRequerido(
        IReadOnlyDictionary<string, JsonElement> respuestas,
        IReadOnlyDictionary<string, JsonElement> calculados,
        string clave)
    {
        if (TryObtenerTexto(calculados, clave, out string? calculado)) return calculado!;
        if (TryObtenerTexto(respuestas, clave, out string? respuesta)) return respuesta!;
        throw new InvalidOperationException($"La proyección requiere el campo dinámico '{clave}'.");
    }

    private static bool TryObtenerTexto(IReadOnlyDictionary<string, JsonElement> origen, string clave, out string? valor)
    {
        valor = null;
        if (!origen.TryGetValue(clave, out JsonElement elemento)) return false;
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

    private static int? ObtenerEntero(IReadOnlyDictionary<string, JsonElement> origen, string clave)
    {
        if (!origen.TryGetValue(clave, out JsonElement elemento)) return null;
        if (elemento.ValueKind == JsonValueKind.Number && elemento.TryGetInt32(out int numero)) return numero;
        return elemento.ValueKind == JsonValueKind.String && int.TryParse(elemento.GetString(), out numero) ? numero : null;
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
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(contenido))).ToLowerInvariant();
    }

    private static OracleCommand CrearComando(string sql, OracleConnection conn, OracleTransaction? transaction = null)
    {
        var command = new OracleCommand(sql, conn) { BindByName = true };
        if (transaction is not null) command.Transaction = transaction;
        return command;
    }

    private static async Task<long> ObtenerSiguienteSecuenciaAsync(
        OracleConnection conn,
        OracleTransaction? transaction,
        string secuencia)
    {
        await using var cmd = CrearComando($"SELECT {secuencia}.NEXTVAL FROM DUAL", conn, transaction);
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

    private static async Task<ReglaCalculoResuelta> ResolverReglaVersionFormularioAsync(
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
        if (exigirVigente) sqlVersion += " AND VER_VIGENTE = 1";

        await using var cmdVersion = CrearComando(sqlVersion, conn, transaction);
        cmdVersion.Parameters.Add(new OracleParameter("versionId", versionFormularioId));
        object? jsonObj = await cmdVersion.ExecuteScalarAsync();
        if (jsonObj is null)
        {
            throw new InvalidOperationException($"La versión {versionFormularioId} no está publicada" + (exigirVigente ? " y vigente." : "."));
        }

        ReferenciaReglaDeclarada referencia = ExtraerReferenciaRegla(jsonObj.ToString()!);
        const string sqlRegla = @"
            SELECT REG_ALGORITMO_ID
              FROM RL_MR_REGLAS_CALCULO
             WHERE REG_CODIGO = :codigo
               AND REG_VERSION = :version
               AND REG_ACTIVA = 1";
        await using var cmdRegla = CrearComando(sqlRegla, conn, transaction);
        cmdRegla.Parameters.Add(new OracleParameter("codigo", referencia.Codigo));
        cmdRegla.Parameters.Add(new OracleParameter("version", referencia.Version));
        object? algoritmoObj = await cmdRegla.ExecuteScalarAsync();
        if (algoritmoObj is null || string.IsNullOrWhiteSpace(algoritmoObj.ToString()))
        {
            throw new InvalidOperationException($"La regla {referencia.Codigo} versión {referencia.Version} declarada por el formulario no existe o está inactiva.");
        }

        return new ReglaCalculoResuelta(
            referencia.Codigo,
            referencia.Version,
            algoritmoObj.ToString()!);
    }

    private static ReferenciaReglaDeclarada ExtraerReferenciaRegla(string versionJson)
    {
        using JsonDocument document = JsonDocument.Parse(versionJson);
        JsonElement root = document.RootElement;

        foreach (string propertyName in new[] { "reglaCalculo", "regla_calculo" })
        {
            if (TryGetPropertyIgnoreCase(root, propertyName, out JsonElement regla)
                && TryLeerReferenciaRegla(regla, out ReferenciaReglaDeclarada? referencia))
            {
                return referencia!;
            }
        }

        foreach (string propertyName in new[] { "reglas", "reglasCalculo", "reglas_calculo" })
        {
            if (!TryGetPropertyIgnoreCase(root, propertyName, out JsonElement reglas)) continue;
            if (reglas.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement regla in reglas.EnumerateArray())
                {
                    if (TryLeerReferenciaRegla(regla, out ReferenciaReglaDeclarada? referencia)) return referencia!;
                }
            }
            else if (reglas.ValueKind == JsonValueKind.Object)
            {
                if (TryLeerReferenciaRegla(reglas, out ReferenciaReglaDeclarada? directa)) return directa!;
                foreach (JsonProperty property in reglas.EnumerateObject())
                {
                    if (TryLeerReferenciaRegla(property.Value, out ReferenciaReglaDeclarada? referencia)) return referencia!;
                }
            }
        }

        throw new InvalidOperationException("La versión publicada del formulario no declara código y versión de la regla de cálculo.");
    }

    private static bool TryLeerReferenciaRegla(JsonElement element, out ReferenciaReglaDeclarada? referencia)
    {
        referencia = null;
        if (element.ValueKind != JsonValueKind.Object) return false;
        if (!TryGetPropertyIgnoreCase(element, "codigo", out JsonElement codigoElement)
            || !TryGetPropertyIgnoreCase(element, "version", out JsonElement versionElement))
        {
            return false;
        }
        string? codigo = codigoElement.GetString();
        string? version = versionElement.GetString();
        if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(version)) return false;
        referencia = new ReferenciaReglaDeclarada(codigo.Trim(), version.Trim());
        return true;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }
        value = default;
        return false;
    }

    private static string LeerTexto(JsonElement element, string propiedad, string predeterminado)
    {
        return TryGetPropertyIgnoreCase(element, propiedad, out JsonElement value)
            && value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? predeterminado
                : predeterminado;
    }

    private static string? LeerTextoNullable(JsonElement element, string propiedad)
    {
        return TryGetPropertyIgnoreCase(element, propiedad, out JsonElement value)
            && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
    }

    private static int LeerEntero(JsonElement element, string propiedad, int predeterminado)
    {
        return TryGetPropertyIgnoreCase(element, propiedad, out JsonElement value)
            && value.ValueKind == JsonValueKind.Number
            && value.TryGetInt32(out int numero)
                ? numero
                : predeterminado;
    }

    private static long? LeerLongNullable(JsonElement element, string propiedad)
    {
        return TryGetPropertyIgnoreCase(element, propiedad, out JsonElement value)
            && value.ValueKind == JsonValueKind.Number
            && value.TryGetInt64(out long numero)
                ? numero
                : null;
    }

    private static bool LeerBooleano(JsonElement element, string propiedad, bool predeterminado)
    {
        return TryGetPropertyIgnoreCase(element, propiedad, out JsonElement value)
            ? value.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => predeterminado
            }
            : predeterminado;
    }

    private sealed record ProyeccionEvaluacion(
        string Area,
        int Vri,
        int Vrr,
        string NivelInherente,
        string NivelResidual,
        string Respuesta,
        string Dueno);

    private sealed record ReferenciaReglaDeclarada(string Codigo, string Version);

    private sealed record ReglaCalculoResuelta(string Codigo, string Version, string AlgoritmoId);
}
