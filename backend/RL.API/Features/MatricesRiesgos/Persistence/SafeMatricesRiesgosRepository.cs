using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Oracle.ManagedDataAccess.Client;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Infrastructure.Database;

namespace RL.API.Features.MatricesRiesgos.Persistence;

/// <summary>
/// Decorador de persistencia para endurecer las mutaciones de familias sin alterar
/// el repositorio consolidado de Matrices. Las lecturas y el resto de operaciones
/// se delegan íntegramente al repositorio existente.
/// </summary>
public sealed class SafeMatricesRiesgosRepository : IMatricesRiesgosRepository
{
    private const string TablaFamilias = "RL_MR_FAMILIAS_FORMULARIO";
    private const string ModuloAuditoria = "MatricesRiesgos";

    private readonly MatricesRiesgosRepository _inner;
    private readonly OracleDbContext _db;
    private readonly IAuditoriaRepository _auditoria;
    private readonly IFamiliasFormularioLifecycleRepository _familiasLifecycle;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SafeMatricesRiesgosRepository(
        MatricesRiesgosRepository inner,
        OracleDbContext db,
        IAuditoriaRepository auditoria,
        IFamiliasFormularioLifecycleRepository familiasLifecycle,
        IHttpContextAccessor httpContextAccessor)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _auditoria = auditoria ?? throw new ArgumentNullException(nameof(auditoria));
        _familiasLifecycle = familiasLifecycle ?? throw new ArgumentNullException(nameof(familiasLifecycle));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public Task<VersionFormularioDto?> ObtenerVersionVigenteFormularioAsync(string familiaCodigo) =>
        _inner.ObtenerVersionVigenteFormularioAsync(familiaCodigo);

    public Task<VersionFormularioDto?> ObtenerVersionFormularioAsync(long versionId) =>
        _inner.ObtenerVersionFormularioAsync(versionId);

    public Task<long> CrearBorradorFormularioAsync(long familiaId, string codigoFormulario, string jsonConfig, long usuarioId) =>
        _inner.CrearBorradorFormularioAsync(familiaId, codigoFormulario, jsonConfig, usuarioId);

    public Task<long> ClonarVersionFormularioAsync(long versionOrigenId, long usuarioId) =>
        _inner.ClonarVersionFormularioAsync(versionOrigenId, usuarioId);

    public Task<bool> ActualizarBorradorFormularioAsync(long versionId, string jsonConfig, string hash, long usuarioId) =>
        _inner.ActualizarBorradorFormularioAsync(versionId, jsonConfig, hash, usuarioId);

    public Task<bool> PublicarVersionFormularioAsync(long versionId, string hash, long usuarioId) =>
        _inner.PublicarVersionFormularioAsync(versionId, hash, usuarioId);

    public Task<bool> CambiarEstadoVigenciaFormularioAsync(long versionId, bool vigente, long usuarioId) =>
        _inner.CambiarEstadoVigenciaFormularioAsync(versionId, vigente, usuarioId);

    public Task<bool> EliminarVersionFormularioAsync(long versionId) =>
        _inner.EliminarVersionFormularioAsync(versionId);

    public Task<List<VersionFormularioDto>> ListarHistorialVersionesFormularioAsync(string familiaCodigo) =>
        _inner.ListarHistorialVersionesFormularioAsync(familiaCodigo);

    public Task<List<FamiliaFormularioDto>> ListarFamiliasFormularioAsync() =>
        _inner.ListarFamiliasFormularioAsync();

    public Task<FamiliaFormularioDto?> ObtenerFamiliaFormularioPorIdAsync(long famId) =>
        _inner.ObtenerFamiliaFormularioPorIdAsync(famId);

    public Task<FamiliaFormularioDto?> ObtenerFamiliaFormularioPorCodigoAsync(string famCodigo) =>
        _inner.ObtenerFamiliaFormularioPorCodigoAsync(famCodigo);

    public async Task<long> CrearFamiliaFormularioAsync(
        string famCodigo,
        string famNombre,
        string? famDescripcion,
        bool famActivo)
    {
        await using OracleConnection conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var transactionBase = await conn.BeginTransactionAsync();
        var transaction = (OracleTransaction)transactionBase;

        try
        {
            const string sqlSeq = "SELECT SEQ_RL_MR_FAMILIAS.NEXTVAL FROM DUAL";
            await using var cmdSeq = CrearComando(sqlSeq, conn, transaction);
            long famId = Convert.ToInt64(await cmdSeq.ExecuteScalarAsync());

            string codigo = (famCodigo ?? string.Empty).Trim().ToUpperInvariant();
            string nombre = (famNombre ?? string.Empty).Trim();
            string? descripcion = famDescripcion?.Trim();

            const string sqlInsert = @"
                INSERT INTO RL_MR_FAMILIAS_FORMULARIO (
                    FAM_ID,
                    FAM_CODIGO,
                    FAM_NOMBRE,
                    FAM_DESCRIPCION,
                    FAM_ACTIVO,
                    FAM_FECHA_CREACION
                ) VALUES (
                    :famId,
                    :famCodigo,
                    :famNombre,
                    :famDescripcion,
                    :famActivo,
                    SYSDATE
                )";

            await using var cmdInsert = CrearComando(sqlInsert, conn, transaction);
            cmdInsert.Parameters.Add(new OracleParameter("famId", famId));
            cmdInsert.Parameters.Add(new OracleParameter("famCodigo", codigo));
            cmdInsert.Parameters.Add(new OracleParameter("famNombre", nombre));
            cmdInsert.Parameters.Add(new OracleParameter("famDescripcion", (object?)descripcion ?? DBNull.Value));
            cmdInsert.Parameters.Add(new OracleParameter("famActivo", famActivo ? 1 : 0));
            await cmdInsert.ExecuteNonQueryAsync();

            (long? usuarioId, string? ip) = ObtenerContextoAuditoria();
            await _auditoria.RegistrarAsync(
                conn,
                transaction,
                TablaFamilias,
                famId.ToString(),
                "INSERT",
                null,
                JsonSerializer.Serialize(new
                {
                    FamId = famId,
                    FamCodigo = codigo,
                    FamNombre = nombre,
                    FamDescripcion = descripcion,
                    FamActivo = famActivo
                }),
                usuarioId,
                null,
                ip,
                ModuloAuditoria);

            await transaction.CommitAsync();
            return famId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> ActualizarFamiliaFormularioAsync(
        long famId,
        string famNombre,
        string? famDescripcion,
        bool famActivo)
    {
        _ = famActivo; // El estado se conserva; activar/desactivar usa operaciones dedicadas.

        await using OracleConnection conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var transactionBase = await conn.BeginTransactionAsync();
        var transaction = (OracleTransaction)transactionBase;

        try
        {
            const string sqlLock = @"
                SELECT FAM_CODIGO,
                       FAM_NOMBRE,
                       FAM_DESCRIPCION,
                       FAM_ACTIVO
                  FROM RL_MR_FAMILIAS_FORMULARIO
                 WHERE FAM_ID = :famId
                 FOR UPDATE";

            string codigo;
            string nombreAnterior;
            string? descripcionAnterior;
            bool activoActual;
            await using (var cmdLock = CrearComando(sqlLock, conn, transaction))
            {
                cmdLock.Parameters.Add(new OracleParameter("famId", famId));
                await using var reader = await cmdLock.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                codigo = reader.GetString(0);
                nombreAnterior = reader.GetString(1);
                descripcionAnterior = reader.IsDBNull(2) ? null : reader.GetString(2);
                activoActual = reader.GetInt32(3) == 1;
            }

            string nombre = (famNombre ?? string.Empty).Trim();
            string? descripcion = famDescripcion?.Trim();

            const string sqlUpdate = @"
                UPDATE RL_MR_FAMILIAS_FORMULARIO
                   SET FAM_NOMBRE = :famNombre,
                       FAM_DESCRIPCION = :famDescripcion
                 WHERE FAM_ID = :famId";

            await using var cmdUpdate = CrearComando(sqlUpdate, conn, transaction);
            cmdUpdate.Parameters.Add(new OracleParameter("famNombre", nombre));
            cmdUpdate.Parameters.Add(new OracleParameter("famDescripcion", (object?)descripcion ?? DBNull.Value));
            cmdUpdate.Parameters.Add(new OracleParameter("famId", famId));
            if (await cmdUpdate.ExecuteNonQueryAsync() != 1)
            {
                await transaction.RollbackAsync();
                return false;
            }

            (long? usuarioId, string? ip) = ObtenerContextoAuditoria();
            await _auditoria.RegistrarAsync(
                conn,
                transaction,
                TablaFamilias,
                famId.ToString(),
                "UPDATE",
                JsonSerializer.Serialize(new
                {
                    FamCodigo = codigo,
                    FamNombre = nombreAnterior,
                    FamDescripcion = descripcionAnterior,
                    FamActivo = activoActual
                }),
                JsonSerializer.Serialize(new
                {
                    FamCodigo = codigo,
                    FamNombre = nombre,
                    FamDescripcion = descripcion,
                    FamActivo = activoActual
                }),
                usuarioId,
                null,
                ip,
                ModuloAuditoria);

            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> DesactivarFamiliaFormularioAtomicoAsync(long famId)
    {
        ResultadoCambioEstadoFamiliaFormulario result =
            await _familiasLifecycle.DesactivarFamiliaFormularioAtomicoAsync(famId);
        return result is ResultadoCambioEstadoFamiliaFormulario.Exito
            or ResultadoCambioEstadoFamiliaFormulario.YaEstabaEnEstado;
    }

    public Task<EvaluacionRiesgoDto?> ObtenerEvaluacionAsync(long evaId) =>
        _inner.ObtenerEvaluacionAsync(evaId);

    public Task<EvaluacionesPaginadasDto> ListarEvaluacionesPaginadasAsync(ConsultaEvaluacionPaginadaDto filtro) =>
        _inner.ListarEvaluacionesPaginadasAsync(filtro);

    public Task<long> CrearEvaluacionAsync(EvaluacionRiesgoDto dto, long usuarioId, string? ip) =>
        _inner.CrearEvaluacionAsync(dto, usuarioId, ip);

    public Task<bool> ActualizarEvaluacionAsync(EvaluacionRiesgoDto dto, long usuarioId, string? ip) =>
        _inner.ActualizarEvaluacionAsync(dto, usuarioId, ip);

    public Task<bool> TransicionarEstadoEvaluacionAsync(long evaId, string nuevoEstado, string? motivo, long usuarioId, string? ip) =>
        _inner.TransicionarEstadoEvaluacionAsync(evaId, nuevoEstado, motivo, usuarioId, ip);

    public Task<List<FlujoEvaluacionDto>> ObtenerFlujosEvaluacionAsync(long evaId) =>
        _inner.ObtenerFlujosEvaluacionAsync(evaId);

    public Task<long> RegistrarEvidenciaFisicaAsync(EvidenciaRegistroDto dto, long usuarioId) =>
        _inner.RegistrarEvidenciaFisicaAsync(dto, usuarioId);

    public Task<EvidenciaDto?> ObtenerEvidenciaFisicaAsync(long evidenciaId) =>
        _inner.ObtenerEvidenciaFisicaAsync(evidenciaId);

    public Task<bool> VincularEvidenciaAsync(VincularEvidenciaDto dto, long usuarioId, string? ip) =>
        _inner.VincularEvidenciaAsync(dto, usuarioId, ip);

    public Task<ResultadoEliminacionEvidencia> EliminarEvidenciaSeguraAsync(
        long evidenciaId,
        Func<Task<bool>> eliminarArchivoFisico,
        long usuarioId,
        string? ip) =>
        _inner.EliminarEvidenciaSeguraAsync(evidenciaId, eliminarArchivoFisico, usuarioId, ip);

    public Task<IReadOnlyList<RiesgoReporteFilaDto>> ObtenerConsolidadoTipadoAsync() =>
        _inner.ObtenerConsolidadoTipadoAsync();

    public Task<MetodologiaFormularioDto?> ObtenerMetodologiaDinamicaVigenteAsync() =>
        _inner.ObtenerMetodologiaDinamicaVigenteAsync();

    public Task<MetodologiaFormularioDto?> ObtenerMetodologiaDinamicaPorVersionAsync(long versionId) =>
        _inner.ObtenerMetodologiaDinamicaPorVersionAsync(versionId);

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
}
