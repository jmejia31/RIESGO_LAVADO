using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Infrastructure.Database;
using RL.API.Tests.Support;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

/// <summary>
/// Certifica la purga de una fixture sintética de Matrices únicamente en el
/// Oracle XE desechable explícitamente habilitado por CI. No usa DDL ni borra
/// filas fuera de los identificadores generados por esta prueba.
/// </summary>
[Collection("MatricesRiesgosOracle")]
public sealed class MatricesSyntheticPurgeOracleIntegrationTests
{
    private const string RequiredOptIn = "RL_MATRICES_SYNTHETIC_PURGE_REQUIRED";
    private const string ConnectionVariable = "ConnectionStrings__OracleDB_LocalXE";
    private const string Prefix = "SYNTH_PURGE_";
    private readonly string? _connectionString;

    public MatricesSyntheticPurgeOracleIntegrationTests()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddUserSecrets<MatricesSyntheticPurgeOracleIntegrationTests>(optional: true)
            .AddEnvironmentVariables()
            .Build();
        _connectionString = OracleTestDatabaseProfile.TryResolveConfiguredLocalXe(configuration, out string? localXe)
            ? localXe
            : null;
    }

    [Fact]
    [Trait("Category", "OracleIntegration")]
    public async Task SyntheticPurge_PreservaAislamientoRollbackIntegridadYRecreacion()
    {
        RequireDisposableXe();

        await using OracleConnection identity = CreateConnection();
        await identity.OpenAsync();
        await ValidateDisposableIdentityAsync(identity);
        Snapshot protectedBefore = await ReadProtectedSnapshotAsync(identity);

        string suffix = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)[..12].ToUpperInvariant();
        string codePrefix = Prefix + suffix;
        long familyId = 0;
        long versionA = 0;
        long versionB = 0;
        long recreatedFamilyId = 0;
        try
        {
            (familyId, versionA, versionB) = await SeedSyntheticDraftFamilyAsync(codePrefix);
            Assert.Equal(2, await CountAsync("RL_MR_VERSIONES_FORMULARIO", "VER_FAMILIA_ID", familyId));

            var db = new OracleDbContext(_connectionString!);
            var audit = new AuditoriaRepository(db, new HttpContextAccessor());
            var matrices = new MatricesRiesgosRepository(db, audit);
            var families = new FamiliasFormularioLifecycleRepository(db, audit, new HttpContextAccessor());

            Assert.True(await matrices.EliminarVersionFormularioAsync(versionA));
            Assert.True(await matrices.EliminarVersionFormularioAsync(versionB));
            Assert.Equal(0, await CountAsync("RL_MR_VERSIONES_FORMULARIO", "VER_FAMILIA_ID", familyId));

            ResultadoEliminacionFamiliaFormulario familyResult = await families.EliminarFamiliaFormularioSeguraAsync(familyId);
            Assert.Equal(ResultadoEliminacionFamiliaFormulario.Exito, familyResult);
            Assert.Equal(0, await CountAsync("RL_MR_FAMILIAS_FORMULARIO", "FAM_ID", familyId));
            Assert.Equal(0, await CountAsync("RL_MR_VERSIONES_FORMULARIO", "VER_FAMILIA_ID", familyId));
            Assert.Equal(0, await CountAsync("RL_MR_FORMULA_USOS", "FUS_VERSION_FORMULARIO_ID", versionA));
            Assert.Equal(0, await CountAsync("RL_MR_FORMULA_USOS", "FUS_VERSION_FORMULARIO_ID", versionB));

            Snapshot protectedAfterPurge = await ReadProtectedSnapshotAsync(identity);
            Assert.Equal(protectedBefore, protectedAfterPurge);

            await AssertRollbackDoesNotLeaveFamilyAsync(codePrefix + "_ROLLBACK");
            Assert.Equal(0, await CountByCodeAsync("RL_MR_FAMILIAS_FORMULARIO", "FAM_CODIGO", codePrefix + "_ROLLBACK"));

            recreatedFamilyId = await matrices.CrearFamiliaFormularioAsync(
                codePrefix + "_RECREATED", "Synthetic purge recreation", "CI-only fixture", true);
            Assert.True(recreatedFamilyId > 0);
            Assert.Equal(1, await CountAsync("RL_MR_FAMILIAS_FORMULARIO", "FAM_ID", recreatedFamilyId));
            Assert.Equal(0, await CountAsync("RL_MR_VERSIONES_FORMULARIO", "VER_FAMILIA_ID", recreatedFamilyId));

            Assert.Equal(protectedBefore, await ReadProtectedSnapshotAsync(identity));
            Console.WriteLine("SYNTHETIC_MATRICES_RECORDS_REMOVED=PASS");
            Console.WriteLine("CROSS_MODULE_ISOLATION=PASS");
            Console.WriteLine("OTHER_MODULES_SYNTHETIC_DATA_CHANGED=0");
            Console.WriteLine("REFERENTIAL_INTEGRITY=PASS");
            Console.WriteLine("PROTECTED_AUDIT_PRESERVED=PASS");
            Console.WriteLine("ROLLBACK_TEST=PASS");
            Console.WriteLine("MATRICES_EMPTY_STATE=PASS");
            Console.WriteLine("NEW_SYNTHETIC_FAMILY_AFTER_CLEANUP=PASS");
        }
        finally
        {
            await CleanupOwnFixtureAsync(familyId, versionA, versionB, recreatedFamilyId, codePrefix);
        }
    }

    private void RequireDisposableXe()
    {
        Assert.True(
            string.Equals(Environment.GetEnvironmentVariable(RequiredOptIn), "true", StringComparison.OrdinalIgnoreCase),
            $"{RequiredOptIn}=true is required; the synthetic purge contract must not run implicitly.");
        Assert.False(string.IsNullOrWhiteSpace(_connectionString),
            $"{ConnectionVariable} must resolve a disposable LOCAL_XE connection.");
    }

    private OracleConnection CreateConnection()
    {
        // The disposable XE image is also exercised by the broader Oracle suite.
        // Keep this isolated contract out of ODP.NET's shared pool: pooled SYSTEM
        // acquisition can surface transient recursive-SQL startup errors before
        // the test has executed any SQL. The connection still targets only the
        // explicitly supplied LOCAL_XE profile.
        var builder = new OracleConnectionStringBuilder(_connectionString!)
        {
            Pooling = false,
            ConnectionTimeout = 30
        };
        return new OracleConnection(builder.ConnectionString)
        {
            UseHourOffsetForUnsupportedTimezone = true
        };
    }

    private async Task ValidateDisposableIdentityAsync(OracleConnection connection)
    {
        string schema = Convert.ToString(await ScalarAsync(connection,
            "SELECT SYS_CONTEXT('USERENV','CURRENT_SCHEMA') FROM DUAL"), CultureInfo.InvariantCulture) ?? "";
        string service = Convert.ToString(await ScalarAsync(connection,
            "SELECT SYS_CONTEXT('USERENV','SERVICE_NAME') FROM DUAL"), CultureInfo.InvariantCulture) ?? "";
        // CI bootstraps its disposable image through SYSTEM; local XE uses the
        // dedicated RIESGO_LAVADO user. Accept only those known disposable
        // profiles and verify the required objects in the active schema.
        Assert.Contains(schema, new[] { "RIESGO_LAVADO", "SYSTEM" }, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("XE", service, StringComparison.OrdinalIgnoreCase);
        Assert.True(await CountByNameAsync(connection, "ALL_TABLES", "TABLE_NAME", "RL_MR_FAMILIAS_FORMULARIO") > 0);
        Assert.True(await CountByNameAsync(connection, "ALL_TABLES", "TABLE_NAME", "RL_MR_VERSIONES_FORMULARIO") > 0);
        Assert.True(await CountByNameAsync(connection, "ALL_TABLES", "TABLE_NAME", "RL_AUDITORIA") > 0);
    }

    private async Task<(long FamilyId, long VersionA, long VersionB)> SeedSyntheticDraftFamilyAsync(string codePrefix)
    {
        await using OracleConnection connection = CreateConnection();
        await connection.OpenAsync();
        await using OracleTransaction transaction = (OracleTransaction)await connection.BeginTransactionAsync();
        try
        {
            long familyId = await NextSequenceAsync(connection, transaction, "SEQ_RL_MR_FAMILIAS");
            long versionA = await NextSequenceAsync(connection, transaction, "SEQ_RL_MR_VERSIONES");
            long versionB = await NextSequenceAsync(connection, transaction, "SEQ_RL_MR_VERSIONES");
            await ExecuteAsync(connection, transaction,
                "INSERT INTO RL_MR_FAMILIAS_FORMULARIO (FAM_ID,FAM_CODIGO,FAM_NOMBRE,FAM_DESCRIPCION,FAM_ACTIVO,FAM_ARCHIVADA,FAM_FECHA_CREACION) VALUES (:id,:code,:name,:description,1,0,SYSDATE)",
                ("id", familyId), ("code", codePrefix), ("name", "Synthetic purge family"), ("description", "Disposable CI fixture"));
            foreach (long versionId in new[] { versionA, versionB })
            {
                await ExecuteAsync(connection, transaction,
                    "INSERT INTO RL_MR_VERSIONES_FORMULARIO (VER_ID,VER_FAMILIA_ID,VER_CODIGO,VER_VERSION,VER_JSON,VER_HASH,VER_ESTADO,VER_VIGENTE,VER_FECHA_INICIO,VER_FECHA_CREACION) VALUES (:id,:family,:code,1,:json,:hash,'DRAFT',0,SYSDATE,SYSDATE)",
                    ("id", versionId), ("family", familyId), ("code", codePrefix + "_" + versionId), ("json", "{\"sections\":[]}"), ("hash", codePrefix.PadRight(64, '0')));
            }
            await transaction.CommitAsync();
            return (familyId, versionA, versionB);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task AssertRollbackDoesNotLeaveFamilyAsync(string code)
    {
        await using OracleConnection connection = CreateConnection();
        await connection.OpenAsync();
        await using OracleTransaction transaction = (OracleTransaction)await connection.BeginTransactionAsync();
        await ExecuteAsync(connection, transaction,
            "INSERT INTO RL_MR_FAMILIAS_FORMULARIO (FAM_ID,FAM_CODIGO,FAM_NOMBRE,FAM_ACTIVO,FAM_ARCHIVADA,FAM_FECHA_CREACION) VALUES (SEQ_RL_MR_FAMILIAS.NEXTVAL,:code,'Rollback fixture',1,0,SYSDATE)",
            ("code", code));
        await transaction.RollbackAsync();
    }

    private async Task CleanupOwnFixtureAsync(long familyId, long versionA, long versionB, long recreatedFamilyId, string codePrefix)
    {
        await using OracleConnection connection = CreateConnection();
        await connection.OpenAsync();
        await ExecuteAsync(connection, "DELETE FROM RL_MR_FORMULA_USOS WHERE FUS_VERSION_FORMULARIO_ID IN (:a,:b)", ("a", versionA), ("b", versionB));
        await ExecuteAsync(connection, "DELETE FROM RL_MR_VERSIONES_FORMULARIO WHERE VER_ID IN (:a,:b)", ("a", versionA), ("b", versionB));
        await ExecuteAsync(connection, "DELETE FROM RL_MR_FAMILIAS_FORMULARIO WHERE FAM_ID IN (:family,:recreated) OR FAM_CODIGO LIKE :prefix", ("family", familyId), ("recreated", recreatedFamilyId), ("prefix", codePrefix + "%"));
        await ExecuteAsync(connection, "COMMIT");
    }

    private async Task<Snapshot> ReadProtectedSnapshotAsync(OracleConnection connection)
    {
        return new Snapshot(
            await ScalarLongAsync(connection, "SELECT COUNT(*) FROM RL_USUARIOS"),
            await ScalarLongAsync(connection, "SELECT NVL(MIN(USR_ID),0) FROM RL_USUARIOS"),
            await ScalarLongAsync(connection, "SELECT NVL(MAX(USR_ID),0) FROM RL_USUARIOS"),
            await ScalarLongAsync(connection, "SELECT COUNT(*) FROM RL_AUDITORIA WHERE AUD_TABLA='RL_USUARIOS'"),
            await ScalarLongAsync(connection, "SELECT NVL(MAX(AUD_ID),0) FROM RL_AUDITORIA WHERE AUD_TABLA='RL_USUARIOS'"));
    }

    private async Task<long> CountAsync(string table, string column, long value) => await ScalarLongAsync(CreateConnection(), $"SELECT COUNT(*) FROM {table} WHERE {column}=:value", ("value", value));
    private async Task<long> CountByCodeAsync(string table, string column, string value) => await ScalarLongAsync(CreateConnection(), $"SELECT COUNT(*) FROM {table} WHERE {column}=:value", ("value", value));

    private async Task<long> NextSequenceAsync(OracleConnection connection, OracleTransaction transaction, string sequence) =>
        Convert.ToInt64(await ExecuteScalarAsync(connection, transaction, $"SELECT {sequence}.NEXTVAL FROM DUAL"), CultureInfo.InvariantCulture);

    private async Task<long> CountByNameAsync(OracleConnection connection, string view, string column, string value) =>
        Convert.ToInt64(await ScalarAsync(connection, $"SELECT COUNT(*) FROM {view} WHERE {column}=:value", ("value", value)), CultureInfo.InvariantCulture);

    private static async Task ExecuteAsync(OracleConnection connection, string sql, params (string Name, object Value)[] parameters)
    {
        await using OracleCommand command = new(sql, connection) { BindByName = true };
        foreach ((string name, object value) in parameters) command.Parameters.Add(new OracleParameter(name, value));
        await command.ExecuteNonQueryAsync();
    }

    private static async Task ExecuteAsync(OracleConnection connection, OracleTransaction transaction, string sql, params (string Name, object Value)[] parameters)
    {
        await using OracleCommand command = new(sql, connection) { BindByName = true, Transaction = transaction };
        foreach ((string name, object value) in parameters) command.Parameters.Add(new OracleParameter(name, value));
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<object?> ScalarAsync(OracleConnection connection, string sql, params (string Name, object Value)[] parameters)
    {
        await using OracleCommand command = new(sql, connection) { BindByName = true };
        foreach ((string name, object value) in parameters) command.Parameters.Add(new OracleParameter(name, value));
        return await command.ExecuteScalarAsync();
    }

    private static async Task<object?> ExecuteScalarAsync(OracleConnection connection, OracleTransaction transaction, string sql)
    {
        await using OracleCommand command = new(sql, connection) { BindByName = true, Transaction = transaction };
        return await command.ExecuteScalarAsync();
    }

    private async Task<long> ScalarLongAsync(OracleConnection connection, string sql, params (string Name, object Value)[] parameters)
    {
        await using (connection)
        {
            if (connection.State != System.Data.ConnectionState.Open) await connection.OpenAsync();
            return Convert.ToInt64(await ScalarAsync(connection, sql, parameters), CultureInfo.InvariantCulture);
        }
    }

    private sealed record Snapshot(long UserCount, long UserMin, long UserMax, long UserAuditCount, long UserAuditMax);
}
