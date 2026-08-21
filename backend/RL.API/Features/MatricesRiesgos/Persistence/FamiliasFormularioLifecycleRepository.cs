using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Oracle.ManagedDataAccess.Client;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Infrastructure.Database;

namespace RL.API.Features.MatricesRiesgos.Persistence;

public sealed class FamiliasFormularioLifecycleRepository : IFamiliasFormularioLifecycleRepository
{
    private const string TablaFamilias = "RL_MR_FAMILIAS_FORMULARIO";
    private const string ModuloAuditoria = "MatricesRiesgos";

    private readonly OracleDbContext _db;
    private readonly IAuditoriaRepository _auditoria;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public FamiliasFormularioLifecycleRepository(
        OracleDbContext db,
        IAuditoriaRepository auditoria,
        IHttpContextAccessor httpContextAccessor)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _auditoria = auditoria ?? throw new ArgumentNullException(nameof(auditoria));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public async Task<ResultadoCambioEstadoFamiliaFormulario> ActivarFamiliaFormularioAtomicoAsync(long famId)
    {
        await using OracleConnection conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var transactionBase = await conn.BeginTransactionAsync();
        var transaction = (OracleTransaction)transactionBase;

        try
        {
            FamiliaBloqueada? familia = await ObtenerFamiliaBloqueadaAsync(conn, transaction, famId);
            if (familia is null)
            {
                await transaction.RollbackAsync();
                return ResultadoCambioEstadoFamiliaFormulario.NoExiste;
            }

            if (familia.Activa)
            {
                await transaction.RollbackAsync();
                return ResultadoCambioEstadoFamiliaFormulario.YaEstabaEnEstado;
            }

            const string sql = @"
                UPDATE RL_MR_FAMILIAS_FORMULARIO
                   SET FAM_ACTIVO = 1
                 WHERE FAM_ID = :famId
                   AND FAM_ACTIVO = 0";

            await using var cmd = CrearComando(sql, conn, transaction);
            cmd.Parameters.Add(new OracleParameter("famId", famId));
            if (await cmd.ExecuteNonQueryAsync() != 1)
            {
                await transaction.RollbackAsync();
                return ResultadoCambioEstadoFamiliaFormulario.NoExiste;
            }

            await RegistrarCambioEstadoAsync(conn, transaction, familia, activoNuevo: true);
            await transaction.CommitAsync();
            return ResultadoCambioEstadoFamiliaFormulario.Exito;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<ResultadoCambioEstadoFamiliaFormulario> DesactivarFamiliaFormularioAtomicoAsync(long famId)
    {
        await using OracleConnection conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var transactionBase = await conn.BeginTransactionAsync();
        var transaction = (OracleTransaction)transactionBase;

        try
        {
            FamiliaBloqueada? familia = await ObtenerFamiliaBloqueadaAsync(conn, transaction, famId);
            if (familia is null)
            {
                await transaction.RollbackAsync();
                return ResultadoCambioEstadoFamiliaFormulario.NoExiste;
            }

            if (!familia.Activa)
            {
                await transaction.RollbackAsync();
                return ResultadoCambioEstadoFamiliaFormulario.YaEstabaEnEstado;
            }

            const string sqlVigente = @"
                SELECT COUNT(*)
                  FROM RL_MR_VERSIONES_FORMULARIO
                 WHERE VER_FAMILIA_ID = :famId
                   AND VER_ESTADO = 'PUBLISHED'
                   AND VER_VIGENTE = 1";

            await using (var cmdVigente = CrearComando(sqlVigente, conn, transaction))
            {
                cmdVigente.Parameters.Add(new OracleParameter("famId", famId));
                int vigentes = Convert.ToInt32(await cmdVigente.ExecuteScalarAsync());
                if (vigentes > 0)
                {
                    await transaction.RollbackAsync();
                    return ResultadoCambioEstadoFamiliaFormulario.TieneVersionVigente;
                }
            }

            const string sql = @"
                UPDATE RL_MR_FAMILIAS_FORMULARIO
                   SET FAM_ACTIVO = 0
                 WHERE FAM_ID = :famId
                   AND FAM_ACTIVO = 1";

            await using var cmd = CrearComando(sql, conn, transaction);
            cmd.Parameters.Add(new OracleParameter("famId", famId));
            if (await cmd.ExecuteNonQueryAsync() != 1)
            {
                await transaction.RollbackAsync();
                return ResultadoCambioEstadoFamiliaFormulario.NoExiste;
            }

            await RegistrarCambioEstadoAsync(conn, transaction, familia, activoNuevo: false);
            await transaction.CommitAsync();
            return ResultadoCambioEstadoFamiliaFormulario.Exito;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<ResultadoEliminacionFamiliaFormulario> EliminarFamiliaFormularioSeguraAsync(long famId)
    {
        await using OracleConnection conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var transactionBase = await conn.BeginTransactionAsync();
        var transaction = (OracleTransaction)transactionBase;

        try
        {
            FamiliaBloqueada? familia = await ObtenerFamiliaBloqueadaAsync(conn, transaction, famId);
            if (familia is null)
            {
                await transaction.RollbackAsync();
                return ResultadoEliminacionFamiliaFormulario.NoExiste;
            }

            const string sqlVersiones = @"
                SELECT COUNT(*)
                  FROM RL_MR_VERSIONES_FORMULARIO
                 WHERE VER_FAMILIA_ID = :famId";

            await using (var cmdVersiones = CrearComando(sqlVersiones, conn, transaction))
            {
                cmdVersiones.Parameters.Add(new OracleParameter("famId", famId));
                if (Convert.ToInt32(await cmdVersiones.ExecuteScalarAsync()) > 0)
                {
                    await transaction.RollbackAsync();
                    return ResultadoEliminacionFamiliaFormulario.TieneVersiones;
                }
            }

            const string sqlDelete = @"
                DELETE FROM RL_MR_FAMILIAS_FORMULARIO f
                 WHERE f.FAM_ID = :famId
                   AND NOT EXISTS (
                       SELECT 1
                         FROM RL_MR_VERSIONES_FORMULARIO v
                        WHERE v.VER_FAMILIA_ID = f.FAM_ID
                   )";

            await using var cmdDelete = CrearComando(sqlDelete, conn, transaction);
            cmdDelete.Parameters.Add(new OracleParameter("famId", famId));
            if (await cmdDelete.ExecuteNonQueryAsync() != 1)
            {
                await transaction.RollbackAsync();
                return ResultadoEliminacionFamiliaFormulario.TieneVersiones;
            }

            (long? usuarioId, string? ip) = ObtenerContextoAuditoria();
            await _auditoria.RegistrarAsync(
                conn,
                transaction,
                TablaFamilias,
                famId.ToString(),
                "DELETE",
                JsonSerializer.Serialize(new
                {
                    familia.Codigo,
                    familia.Nombre,
                    familia.Descripcion,
                    Activa = familia.Activa
                }),
                null,
                usuarioId,
                null,
                ip,
                ModuloAuditoria);

            await transaction.CommitAsync();
            return ResultadoEliminacionFamiliaFormulario.Exito;
        }
        catch (OracleException ex) when (ex.Number == 2292)
        {
            await transaction.RollbackAsync();
            return ResultadoEliminacionFamiliaFormulario.TieneVersiones;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<FamiliaBloqueada?> ObtenerFamiliaBloqueadaAsync(
        OracleConnection conn,
        OracleTransaction transaction,
        long famId)
    {
        const string sql = @"
            SELECT FAM_CODIGO,
                   FAM_NOMBRE,
                   FAM_DESCRIPCION,
                   FAM_ACTIVO
              FROM RL_MR_FAMILIAS_FORMULARIO
             WHERE FAM_ID = :famId
             FOR UPDATE";

        await using var cmd = CrearComando(sql, conn, transaction);
        cmd.Parameters.Add(new OracleParameter("famId", famId));
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new FamiliaBloqueada(
            reader.GetString(0),
            reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetString(2),
            reader.GetInt32(3) == 1);
    }

    private async Task RegistrarCambioEstadoAsync(
        OracleConnection conn,
        OracleTransaction transaction,
        FamiliaBloqueada familia,
        bool activoNuevo)
    {
        (long? usuarioId, string? ip) = ObtenerContextoAuditoria();
        await _auditoria.RegistrarAsync(
            conn,
            transaction,
            TablaFamilias,
            familia.Codigo,
            "UPDATE",
            JsonSerializer.Serialize(new { familia.Codigo, FamActivo = familia.Activa }),
            JsonSerializer.Serialize(new { familia.Codigo, FamActivo = activoNuevo }),
            usuarioId,
            null,
            ip,
            ModuloAuditoria);
    }

    private (long? UsuarioId, string? Ip) ObtenerContextoAuditoria()
    {
        HttpContext? httpContext = _httpContextAccessor.HttpContext;
        string? id = httpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        long? usuarioId = long.TryParse(id, out long parsed) ? parsed : null;
        string? ip = httpContext?.Connection.RemoteIpAddress?.ToString();
        return (usuarioId, ip);
    }

    private static OracleCommand CrearComando(
        string sql,
        OracleConnection conn,
        OracleTransaction transaction)
    {
        return new OracleCommand(sql, conn)
        {
            BindByName = true,
            Transaction = transaction
        };
    }

    private sealed record FamiliaBloqueada(
        string Codigo,
        string Nombre,
        string? Descripcion,
        bool Activa);
}
