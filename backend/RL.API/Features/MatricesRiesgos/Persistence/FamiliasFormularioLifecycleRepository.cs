using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Oracle.ManagedDataAccess.Client;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Infrastructure.Database;
using RL.API.Infrastructure.Http;

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

            if (familia.Archivada)
            {
                await transaction.RollbackAsync();
                return ResultadoCambioEstadoFamiliaFormulario.EstaArchivada;
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

            if (familia.Predeterminada)
            {
                await transaction.RollbackAsync();
                return ResultadoCambioEstadoFamiliaFormulario.EsPredeterminada;
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

    public async Task<ResultadoArchivadoFamiliaFormulario> ArchivarFamiliaFormularioAtomicoAsync(long famId, string motivo)
    {
        await using OracleConnection conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var transactionBase = await conn.BeginTransactionAsync();
        var transaction = (OracleTransaction)transactionBase;
        try
        {
            FamiliaBloqueada? familia = await ObtenerFamiliaBloqueadaAsync(conn, transaction, famId);
            if (familia is null) { await transaction.RollbackAsync(); return ResultadoArchivadoFamiliaFormulario.NoExiste; }
            if (familia.Archivada) { await transaction.RollbackAsync(); return ResultadoArchivadoFamiliaFormulario.YaArchivada; }
            if (familia.Activa) { await transaction.RollbackAsync(); return ResultadoArchivadoFamiliaFormulario.FamiliaActiva; }
            if (familia.Predeterminada) { await transaction.RollbackAsync(); return ResultadoArchivadoFamiliaFormulario.EsPredeterminada; }

            const string sqlVigente = @"
                SELECT COUNT(*) FROM RL_MR_VERSIONES_FORMULARIO
                 WHERE VER_FAMILIA_ID = :famId AND VER_ESTADO = 'PUBLISHED' AND VER_VIGENTE = 1";
            await using (var cmdVigente = CrearComando(sqlVigente, conn, transaction))
            {
                cmdVigente.Parameters.Add(new OracleParameter("famId", famId));
                if (Convert.ToInt32(await cmdVigente.ExecuteScalarAsync()) > 0)
                { await transaction.RollbackAsync(); return ResultadoArchivadoFamiliaFormulario.TieneVersionVigente; }
            }

            const string sql = @"
                UPDATE RL_MR_FAMILIAS_FORMULARIO
                   SET FAM_ARCHIVADA = 1, FAM_ACTIVO = 0, FAM_FECHA_ARCHIVADO = SYSDATE,
                       FAM_USR_ARCHIVADO = :usuarioId, FAM_MOTIVO_ARCHIVADO = :motivo
                 WHERE FAM_ID = :famId AND FAM_ARCHIVADA = 0 AND FAM_ACTIVO = 0";
            await using var cmd = CrearComando(sql, conn, transaction);
            cmd.Parameters.Add(new OracleParameter("usuarioId", ObtenerContextoAuditoria().UsuarioId ?? (object)DBNull.Value));
            cmd.Parameters.Add(new OracleParameter("motivo", motivo));
            cmd.Parameters.Add(new OracleParameter("famId", famId));
            if (await cmd.ExecuteNonQueryAsync() != 1) { await transaction.RollbackAsync(); return ResultadoArchivadoFamiliaFormulario.YaArchivada; }

            (long? usuarioId, string? ip) = ObtenerContextoAuditoria();
            await _auditoria.RegistrarAsync(conn, transaction, TablaFamilias, familia.Codigo, "ARCHIVE",
                JsonSerializer.Serialize(new { familia.Codigo, familia.Activa, familia.Archivada }),
                JsonSerializer.Serialize(new { familia.Codigo, FamActivo = false, FamArchivada = true, Motivo = motivo }),
                usuarioId, null, ip, ModuloAuditoria);
            await transaction.CommitAsync();
            return ResultadoArchivadoFamiliaFormulario.Exito;
        }
        catch { await transaction.RollbackAsync(); throw; }
    }

    public async Task<ResultadoRestauracionFamiliaFormulario> RestaurarFamiliaFormularioAtomicoAsync(long famId)
    {
        await using OracleConnection conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var transactionBase = await conn.BeginTransactionAsync();
        var transaction = (OracleTransaction)transactionBase;
        try
        {
            FamiliaBloqueada? familia = await ObtenerFamiliaBloqueadaAsync(conn, transaction, famId);
            if (familia is null) { await transaction.RollbackAsync(); return ResultadoRestauracionFamiliaFormulario.NoExiste; }
            if (!familia.Archivada) { await transaction.RollbackAsync(); return ResultadoRestauracionFamiliaFormulario.NoEstaArchivada; }
            const string sql = @"
                UPDATE RL_MR_FAMILIAS_FORMULARIO
                   SET FAM_ARCHIVADA = 0, FAM_ACTIVO = 0
                 WHERE FAM_ID = :famId AND FAM_ARCHIVADA = 1";
            await using var cmd = CrearComando(sql, conn, transaction);
            cmd.Parameters.Add(new OracleParameter("famId", famId));
            if (await cmd.ExecuteNonQueryAsync() != 1) { await transaction.RollbackAsync(); return ResultadoRestauracionFamiliaFormulario.NoEstaArchivada; }
            (long? usuarioId, string? ip) = ObtenerContextoAuditoria();
            await _auditoria.RegistrarAsync(conn, transaction, TablaFamilias, familia.Codigo, "RESTORE",
                JsonSerializer.Serialize(new { familia.Codigo, FamActivo = false, FamArchivada = true }),
                JsonSerializer.Serialize(new { familia.Codigo, FamActivo = false, FamArchivada = false }),
                usuarioId, null, ip, ModuloAuditoria);
            await transaction.CommitAsync();
            return ResultadoRestauracionFamiliaFormulario.Exito;
        }
        catch { await transaction.RollbackAsync(); throw; }
    }

    public async Task<FamiliaFormularioPurgaPrecheckDto?> ObtenerPurgaPrecheckAsync(long famId)
    {
        await using OracleConnection conn = _db.CreateConnection();
        await conn.OpenAsync();
        const string sql = @"
            SELECT f.FAM_ID, f.FAM_CODIGO, f.FAM_ACTIVO, f.FAM_ARCHIVADA, f.FAM_PREDETERMINADA,
                   (SELECT COUNT(*) FROM RL_MR_VERSIONES_FORMULARIO v WHERE v.VER_FAMILIA_ID = f.FAM_ID),
                   (SELECT COUNT(*) FROM RL_MR_VERSIONES_FORMULARIO v WHERE v.VER_FAMILIA_ID = f.FAM_ID AND v.VER_ESTADO = 'PUBLISHED'),
                   (SELECT COUNT(*) FROM RL_MR_VERSIONES_FORMULARIO v WHERE v.VER_FAMILIA_ID = f.FAM_ID AND v.VER_ESTADO = 'PUBLISHED' AND v.VER_VIGENTE = 1),
                   (SELECT COUNT(*) FROM RL_MR_EVALUACIONES_RIESGO e JOIN RL_MR_VERSIONES_FORMULARIO v ON v.VER_ID = e.EVA_VERSION_ID WHERE v.VER_FAMILIA_ID = f.FAM_ID),
                   (SELECT COUNT(*) FROM RL_MR_FORMULA_USOS u JOIN RL_MR_VERSIONES_FORMULARIO v ON v.VER_ID = u.FUS_VERSION_FORMULARIO_ID WHERE v.VER_FAMILIA_ID = f.FAM_ID),
                   (SELECT COUNT(*) FROM RL_MR_AUDITORIA a WHERE a.AUD_TABLA = 'RL_MR_FAMILIAS_FORMULARIO' AND (a.AUD_REGISTRO_ID = TO_CHAR(f.FAM_ID) OR a.AUD_REGISTRO_ID = f.FAM_CODIGO))
              FROM RL_MR_FAMILIAS_FORMULARIO f WHERE f.FAM_ID = :famId";
        await using var cmd = CrearComando(sql, conn, null);
        cmd.Parameters.Add(new OracleParameter("famId", famId));
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        int versiones = Convert.ToInt32(reader.GetValue(5));
        int publicadas = Convert.ToInt32(reader.GetValue(6));
        int vigentes = Convert.ToInt32(reader.GetValue(7));
        int evaluaciones = Convert.ToInt32(reader.GetValue(8));
        int usosFormula = Convert.ToInt32(reader.GetValue(9));
        int auditoria = Convert.ToInt32(reader.GetValue(10));
        var motivos = new List<string>();
        if (reader.GetInt32(4) == 1) motivos.Add("La familia es predeterminada.");
        if (reader.GetInt32(2) == 1) motivos.Add("La familia permanece activa.");
        if (vigentes > 0) motivos.Add("Existen versiones publicadas vigentes.");
        if (evaluaciones > 0) motivos.Add("Existen evaluaciones históricas vinculadas.");
        if (usosFormula > 0) motivos.Add("Existen usos de fórmulas vinculados a versiones.");
        if (auditoria > 0) motivos.Add("Existe auditoría institucional protegida.");
        motivos.Add("La procedencia de dato de prueba no se demuestra por el código y requiere evidencia institucional.");
        motivos.Add("La purga física permanece bloqueada por política de integridad/retención; el archivado es la alternativa segura.");
        return new FamiliaFormularioPurgaPrecheckDto
        {
            FamiliaId = reader.GetInt64(0), FamiliaCodigo = reader.GetString(1), EsDatoDePrueba = false,
            FamiliaActiva = reader.GetInt32(2) == 1, FamiliaArchivada = reader.GetInt32(3) == 1, FamiliaPredeterminada = reader.GetInt32(4) == 1,
            TotalVersiones = versiones, VersionesPublicadas = publicadas, VersionesPublicadasVigentes = vigentes,
            TotalEvaluaciones = evaluaciones, TotalUsosFormula = usosFormula, TotalReferenciasAuditoria = auditoria,
            DependenciasCompletamenteAuditadas = false, PurgaElegible = false, MotivosBloqueo = motivos
        };
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
                       FAM_ACTIVO,
                       FAM_PREDETERMINADA,
                       FAM_ARCHIVADA
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
            reader.GetInt32(3) == 1,
            reader.GetInt32(4) == 1,
            reader.GetInt32(5) == 1);
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
        string? ip = httpContext?.GetClientIp();
        return (usuarioId, ip);
    }

    private static OracleCommand CrearComando(
        string sql,
        OracleConnection conn,
        OracleTransaction? transaction)
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
        bool Activa,
        bool Predeterminada,
        bool Archivada);
}
