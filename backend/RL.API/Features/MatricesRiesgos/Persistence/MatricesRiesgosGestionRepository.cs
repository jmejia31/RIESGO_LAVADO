using System.Text.Json;
using Oracle.ManagedDataAccess.Client;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Infrastructure.Database;

namespace RL.API.Features.MatricesRiesgos.Persistence;

public interface IMatricesRiesgosGestionRepository
{
    Task<IReadOnlyList<RiesgoDto>> ListarRiesgosAsync(bool incluirInactivos);
    Task<RiesgoDto?> ObtenerRiesgoAsync(long riesgoId);
    Task<long> CrearRiesgoAsync(RiesgoGuardarDto dto, long usuarioId, string? ip);
    Task<bool> ActualizarRiesgoAsync(long riesgoId, RiesgoGuardarDto dto, long usuarioId, string? ip);
}

public sealed class MatricesRiesgosGestionRepository : IMatricesRiesgosGestionRepository
{
    private const string ModuloAuditoria = "MatricesRiesgos";
    private readonly OracleDbContext _db;
    private readonly IAuditoriaRepository _auditoria;

    public MatricesRiesgosGestionRepository(OracleDbContext db, IAuditoriaRepository auditoria)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _auditoria = auditoria ?? throw new ArgumentNullException(nameof(auditoria));
    }

    public async Task<IReadOnlyList<RiesgoDto>> ListarRiesgosAsync(bool incluirInactivos)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        string sql = @"
            SELECT RIE_ID, RIE_CODIGO, RIE_NOMBRE, RIE_DESCRIPCION,
                   RIE_ACTIVO, RIE_USR_CREACION, RIE_FECHA_CREACION
              FROM RL_MR_RIESGOS" +
            (incluirInactivos ? string.Empty : " WHERE RIE_ACTIVO = 1") +
            " ORDER BY RIE_CODIGO";

        await using var cmd = new OracleCommand(sql, conn) { BindByName = true };
        var lista = new List<RiesgoDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) lista.Add(Mapear(reader));
        return lista;
    }

    public async Task<RiesgoDto?> ObtenerRiesgoAsync(long riesgoId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        const string sql = @"
            SELECT RIE_ID, RIE_CODIGO, RIE_NOMBRE, RIE_DESCRIPCION,
                   RIE_ACTIVO, RIE_USR_CREACION, RIE_FECHA_CREACION
              FROM RL_MR_RIESGOS
             WHERE RIE_ID = :riesgoId";
        await using var cmd = new OracleCommand(sql, conn) { BindByName = true };
        cmd.Parameters.Add(new OracleParameter("riesgoId", riesgoId));
        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Mapear(reader) : null;
    }

    public async Task<long> CrearRiesgoAsync(RiesgoGuardarDto dto, long usuarioId, string? ip)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var trans = conn.BeginTransaction();
        try
        {
            await ExigirUsuarioActivoAsync(conn, trans, usuarioId);
            string codigo = NormalizarCodigo(dto.RieCodigo);
            await ExigirCodigoDisponibleAsync(conn, trans, codigo, null);
            long id = Convert.ToInt64(await EjecutarEscalarAsync(conn, trans, "SELECT SEQ_RL_MR_RIESGOS.NEXTVAL FROM DUAL"));

            const string sql = @"
                INSERT INTO RL_MR_RIESGOS (
                    RIE_ID, RIE_CODIGO, RIE_NOMBRE, RIE_DESCRIPCION,
                    RIE_ACTIVO, RIE_USR_CREACION, RIE_FECHA_CREACION
                ) VALUES (
                    :id, :codigo, :nombre, :descripcion, :activo, :usuarioId, SYSDATE
                )";
            await using var cmd = new OracleCommand(sql, conn) { BindByName = true, Transaction = trans };
            cmd.Parameters.Add(new OracleParameter("id", id));
            cmd.Parameters.Add(new OracleParameter("codigo", codigo));
            cmd.Parameters.Add(new OracleParameter("nombre", dto.RieNombre.Trim()));
            cmd.Parameters.Add(new OracleParameter("descripcion", (object?)dto.RieDescripcion?.Trim() ?? DBNull.Value));
            cmd.Parameters.Add(new OracleParameter("activo", dto.RieActivo ? 1 : 0));
            cmd.Parameters.Add(new OracleParameter("usuarioId", usuarioId));
            await cmd.ExecuteNonQueryAsync();

            await _auditoria.RegistrarAsync(conn, trans, "RL_MR_RIESGOS", id.ToString(), "INSERT",
                null, JsonSerializer.Serialize(new { Codigo = codigo, dto.RieNombre, dto.RieDescripcion, dto.RieActivo }),
                usuarioId, null, ip, ModuloAuditoria);
            await trans.CommitAsync();
            return id;
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> ActualizarRiesgoAsync(long riesgoId, RiesgoGuardarDto dto, long usuarioId, string? ip)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var trans = conn.BeginTransaction();
        try
        {
            await ExigirUsuarioActivoAsync(conn, trans, usuarioId);
            const string sqlAnterior = @"
                SELECT RIE_CODIGO, RIE_NOMBRE, RIE_DESCRIPCION, RIE_ACTIVO
                  FROM RL_MR_RIESGOS
                 WHERE RIE_ID = :id
                 FOR UPDATE";
            string? codigoAnterior = null;
            string? nombreAnterior = null;
            string? descripcionAnterior = null;
            bool activoAnterior = false;
            await using (var cmdAnterior = new OracleCommand(sqlAnterior, conn) { BindByName = true, Transaction = trans })
            {
                cmdAnterior.Parameters.Add(new OracleParameter("id", riesgoId));
                await using var reader = await cmdAnterior.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    await trans.RollbackAsync();
                    return false;
                }
                codigoAnterior = reader.GetString(0);
                nombreAnterior = reader.GetString(1);
                descripcionAnterior = reader.IsDBNull(2) ? null : reader.GetString(2);
                activoAnterior = reader.GetInt32(3) == 1;
            }

            string codigo = NormalizarCodigo(dto.RieCodigo);
            await ExigirCodigoDisponibleAsync(conn, trans, codigo, riesgoId);
            const string sql = @"
                UPDATE RL_MR_RIESGOS
                   SET RIE_CODIGO = :codigo,
                       RIE_NOMBRE = :nombre,
                       RIE_DESCRIPCION = :descripcion,
                       RIE_ACTIVO = :activo
                 WHERE RIE_ID = :id";
            await using var cmd = new OracleCommand(sql, conn) { BindByName = true, Transaction = trans };
            cmd.Parameters.Add(new OracleParameter("codigo", codigo));
            cmd.Parameters.Add(new OracleParameter("nombre", dto.RieNombre.Trim()));
            cmd.Parameters.Add(new OracleParameter("descripcion", (object?)dto.RieDescripcion?.Trim() ?? DBNull.Value));
            cmd.Parameters.Add(new OracleParameter("activo", dto.RieActivo ? 1 : 0));
            cmd.Parameters.Add(new OracleParameter("id", riesgoId));
            await cmd.ExecuteNonQueryAsync();

            await _auditoria.RegistrarAsync(conn, trans, "RL_MR_RIESGOS", riesgoId.ToString(), "UPDATE",
                JsonSerializer.Serialize(new { Codigo = codigoAnterior, Nombre = nombreAnterior, Descripcion = descripcionAnterior, Activo = activoAnterior }),
                JsonSerializer.Serialize(new { Codigo = codigo, dto.RieNombre, dto.RieDescripcion, dto.RieActivo }),
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

    private static string NormalizarCodigo(string codigo) => codigo.Trim().ToUpperInvariant();

    private static async Task ExigirUsuarioActivoAsync(OracleConnection conn, OracleTransaction trans, long usuarioId)
    {
        const string sql = "SELECT COUNT(*) FROM RL_USUARIOS WHERE USR_ID = :id AND USR_ACTIVO = 1";
        await using var cmd = new OracleCommand(sql, conn) { BindByName = true, Transaction = trans };
        cmd.Parameters.Add(new OracleParameter("id", usuarioId));
        if (Convert.ToInt32(await cmd.ExecuteScalarAsync()) != 1)
            throw new InvalidOperationException("El usuario institucional no existe o está inactivo.");
    }

    private static async Task ExigirCodigoDisponibleAsync(OracleConnection conn, OracleTransaction trans, string codigo, long? excluirId)
    {
        string sql = "SELECT COUNT(*) FROM RL_MR_RIESGOS WHERE RIE_CODIGO = :codigo";
        if (excluirId.HasValue) sql += " AND RIE_ID <> :id";
        await using var cmd = new OracleCommand(sql, conn) { BindByName = true, Transaction = trans };
        cmd.Parameters.Add(new OracleParameter("codigo", codigo));
        if (excluirId.HasValue) cmd.Parameters.Add(new OracleParameter("id", excluirId.Value));
        if (Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0)
            throw new InvalidOperationException($"Ya existe un riesgo con código '{codigo}'.");
    }

    private static async Task<object> EjecutarEscalarAsync(OracleConnection conn, OracleTransaction trans, string sql)
    {
        await using var cmd = new OracleCommand(sql, conn) { BindByName = true, Transaction = trans };
        return await cmd.ExecuteScalarAsync() ?? throw new InvalidOperationException("Oracle no devolvió un valor esperado.");
    }

    private static RiesgoDto Mapear(OracleDataReader reader) => new()
    {
        RieId = reader.GetInt64(0),
        RieCodigo = reader.GetString(1),
        RieNombre = reader.GetString(2),
        RieDescripcion = reader.IsDBNull(3) ? null : reader.GetString(3),
        RieActivo = reader.GetInt32(4) == 1,
        RieUsrCreacion = reader.GetInt64(5),
        RieFechaCreacion = reader.GetDateTime(6)
    };
}
