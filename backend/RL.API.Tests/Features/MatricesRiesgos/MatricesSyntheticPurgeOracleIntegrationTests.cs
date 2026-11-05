using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Oracle.ManagedDataAccess.Types;
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

        // The established Oracle integration suites use synchronous Open() for
        // disposable XE bootstrap; asynchronous acquisition timed out in CI.
        OracleConnection.ClearAllPools();
        await using OracleConnection identity = await OpenDisposableConnectionAsync();
        await ValidateDisposableIdentityAsync(identity);
        Snapshot protectedBefore = await ReadProtectedSnapshotAsync(identity);

        string suffix = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)[..12].ToUpperInvariant();
        string codePrefix = Prefix + suffix;
        long familyId = 0;
        long versionA = 0;
        long versionB = 0;
        long recreatedFamilyId = 0;
        SyntheticGraph? graph = null;
        try
        {
            (familyId, versionA, versionB) = await SeedSyntheticDraftFamilyAsync(codePrefix);
            Assert.Equal(2, await CountAsync("RL_MR_VERSIONES_FORMULARIO", "VER_FAMILIA_ID", familyId));
            // Exercise a committed operational chain, not only formulario metadata.
            graph = await SeedSyntheticOperationalGraphAsync(codePrefix, versionA);
            await AssertGraphRowsAsync(graph, expectedCount: 1);
            await AssertOperationalRollbackAsync(graph);
            await DeleteOwnSyntheticGraphAsync(graph, requireRows: true);
            await AssertGraphRowsAsync(graph, expectedCount: 0);
            await AssertRollbackOfVersionDeletionAsync(versionA);

            var db = new OracleDbContext(CreateUnpooledConnectionString());
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
            Console.WriteLine("MATRICES_SYNTHETIC_OPERATIONAL_CHAIN_11_TABLES=PASS");
            Console.WriteLine("PROTECTED_PRESENT_NON_MR_TABLES_HASHED=" + protectedBefore.TableCount.ToString(CultureInfo.InvariantCulture));
            Console.WriteLine("PROTECTED_PRESENT_NON_MR_ROWS_HASHED=" + protectedBefore.RowCount.ToString(CultureInfo.InvariantCulture));
            Console.WriteLine("PROTECTED_PRESENT_NON_MR_VALUES_SHA256=PASS");
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
            if (graph is not null)
                await DeleteOwnSyntheticGraphAsync(graph, requireRows: false);
            await CleanupOwnFixtureAsync(familyId, versionA, versionB, recreatedFamilyId, codePrefix);
        }

        // Emit the real primary key/code only after fixture cleanup succeeded.
        // The Playwright suite checks the actual post-purge API and Angular state.
        Assert.True(familyId > 0);
        Assert.Equal(0, await CountAsync("RL_MR_FAMILIAS_FORMULARIO", "FAM_ID", familyId));
        Assert.Equal(0, await CountByCodeAsync("RL_MR_FAMILIAS_FORMULARIO", "FAM_CODIGO", codePrefix));
        Console.WriteLine("GATE02_PURGED_FAMILY_CODE=" + codePrefix);
        Console.WriteLine("GATE02_PURGED_FAMILY_ID=" + familyId.ToString(CultureInfo.InvariantCulture));
    }

    // The 11 tables below are present in the existing disposable browser fixture.
    // No institutional connection, DDL, unrestricted deletion or shared-table
    // mutation is used by this graph exercise.
    private sealed record SyntheticGraph(
        long RiskId, long EvaluationId, long ProjectionId, long FlowId,
        long ControlId, long PlanId, long ActivityId, long AlertId,
        long MonitoringId, long EvidenceId, long EvidenceLinkId);

    private static (string Table, string Column, long Id)[] GraphRowsInDeleteOrder(SyntheticGraph graph) =>
    [
        ("RL_MR_EVIDENCIAS_VINCULOS", "EVV_ID", graph.EvidenceLinkId),
        ("RL_MR_EVIDENCIAS", "EVI_ID", graph.EvidenceId),
        ("RL_MR_ACTIVIDADES", "ACT_ID", graph.ActivityId),
        ("RL_MR_PLANES", "PLA_ID", graph.PlanId),
        ("RL_MR_SENALES_ALERTA", "ALE_ID", graph.AlertId),
        ("RL_MR_AUTOMONITOREO", "MON_ID", graph.MonitoringId),
        ("RL_MR_CONTROLES_RIESGO", "CON_ID", graph.ControlId),
        ("RL_MR_FLUJOS_EVALUACION", "FLU_ID", graph.FlowId),
        ("RL_MR_PROYECCIONES_EVALUACION", "PROY_ID", graph.ProjectionId),
        ("RL_MR_EVALUACIONES_RIESGO", "EVA_ID", graph.EvaluationId),
        ("RL_MR_RIESGOS", "RIE_ID", graph.RiskId)
    ];

    private async Task<SyntheticGraph> SeedSyntheticOperationalGraphAsync(string codePrefix, long versionId)
    {
        await using OracleConnection connection = await OpenDisposableConnectionAsync();
        await using OracleTransaction transaction = (OracleTransaction)await connection.BeginTransactionAsync();
        try
        {
            // The browser fixture already owns every sequence below.
            // The risk ID uses another NEXTVAL from the evaluation sequence, which
            // is safe because these tables have independent primary keys.
            var graph = new SyntheticGraph(
                await NextSequenceAsync(connection, transaction, "SEQ_RL_MR_EVALUACIONES"),
                await NextSequenceAsync(connection, transaction, "SEQ_RL_MR_EVALUACIONES"),
                await NextSequenceAsync(connection, transaction, "SEQ_RL_MR_PROYECCIONES"),
                await NextSequenceAsync(connection, transaction, "SEQ_RL_MR_FLUJOS"),
                await NextSequenceAsync(connection, transaction, "SEQ_RL_MR_CONTROLES"),
                await NextSequenceAsync(connection, transaction, "SEQ_RL_MR_PLANES"),
                await NextSequenceAsync(connection, transaction, "SEQ_RL_MR_ACTIVIDADES"),
                await NextSequenceAsync(connection, transaction, "SEQ_RL_MR_SENALES"),
                await NextSequenceAsync(connection, transaction, "SEQ_RL_MR_AUTOMONITOREO"),
                await NextSequenceAsync(connection, transaction, "SEQ_RL_MR_EVIDENCIAS"),
                await NextSequenceAsync(connection, transaction, "SEQ_RL_MR_EVI_VINCULOS"));
            await ExecuteAsync(connection, transaction,
                "INSERT INTO RL_MR_RIESGOS (RIE_ID,RIE_CODIGO,RIE_NOMBRE,RIE_ACTIVO) VALUES (:id,:code,'Synthetic operational graph',1)",
                ("id", graph.RiskId), ("code", codePrefix));
            await ExecuteAsync(connection, transaction,
                "INSERT INTO RL_MR_EVALUACIONES_RIESGO (EVA_ID,EVA_RIESGO_ID,EVA_VERSION_ID,EVA_DATOS_JSON,EVA_CALCULOS_JSON,EVA_FECHA_REGISTRO,EVA_VERSION_ROW,EVA_ACTIVO) VALUES (:id,:risk,:version,'{}','{}',SYSDATE,1,1)",
                ("id", graph.EvaluationId), ("risk", graph.RiskId), ("version", versionId));
            await ExecuteAsync(connection, transaction,
                "INSERT INTO RL_MR_PROYECCIONES_EVALUACION (PROY_ID,PROY_EVALUACION_ID,PROY_CODIGO_RIESGO) VALUES (:id,:evaluation,:code)",
                ("id", graph.ProjectionId), ("evaluation", graph.EvaluationId), ("code", codePrefix));
            await ExecuteAsync(connection, transaction,
                "INSERT INTO RL_MR_FLUJOS_EVALUACION (FLU_ID,FLU_EVALUACION_ID,FLU_ESTADO) VALUES (:id,:evaluation,'DRAFT')",
                ("id", graph.FlowId), ("evaluation", graph.EvaluationId));
            await ExecuteAsync(connection, transaction,
                "INSERT INTO RL_MR_CONTROLES_RIESGO (CON_ID,CON_EVALUACION_ID,CON_TIPO,CON_DESCRIPCION,CON_ESTADO) VALUES (:id,:evaluation,'PREVENTIVO','Synthetic control','ACTIVO')",
                ("id", graph.ControlId), ("evaluation", graph.EvaluationId));
            await ExecuteAsync(connection, transaction,
                "INSERT INTO RL_MR_PLANES (PLA_ID,PLA_EVALUACION_ID,PLA_DESCRIPCION,PLA_ESTADO) VALUES (:id,:evaluation,'Synthetic plan','ACTIVO')",
                ("id", graph.PlanId), ("evaluation", graph.EvaluationId));
            await ExecuteAsync(connection, transaction,
                "INSERT INTO RL_MR_ACTIVIDADES (ACT_ID,ACT_PLAN_ID,ACT_DESCRIPCION,ACT_ESTADO) VALUES (:id,:plan,'Synthetic activity','ACTIVO')",
                ("id", graph.ActivityId), ("plan", graph.PlanId));
            await ExecuteAsync(connection, transaction,
                "INSERT INTO RL_MR_SENALES_ALERTA (ALE_ID,ALE_EVALUACION_ID,ALE_CODIGO,ALE_ESTADO) VALUES (:id,:evaluation,:code,'ACTIVA')",
                ("id", graph.AlertId), ("evaluation", graph.EvaluationId), ("code", codePrefix));
            await ExecuteAsync(connection, transaction,
                "INSERT INTO RL_MR_AUTOMONITOREO (MON_ID,MON_EVALUACION_ID,MON_ESTADO_RIESGO,MON_ESTADO_CONTR) VALUES (:id,:evaluation,'ACTIVO','ACTIVO')",
                ("id", graph.MonitoringId), ("evaluation", graph.EvaluationId));
            await ExecuteAsync(connection, transaction,
                "INSERT INTO RL_MR_EVIDENCIAS (EVI_ID,EVI_NOMBRE_ARCHIVO,EVI_EXTENSION,EVI_TAMANO,EVI_HASH,EVI_RUTA,EVI_USR_CREACION) VALUES (:id,:name,'txt',5,:hash,:path,1)",
                ("id", graph.EvidenceId), ("name", codePrefix + ".txt"),
                ("hash", codePrefix.PadRight(64, 'A')), ("path", "synthetic/" + codePrefix));
            await ExecuteAsync(connection, transaction,
                "INSERT INTO RL_MR_EVIDENCIAS_VINCULOS (EVV_ID,EVV_EVIDENCIA_ID,EVV_TIPO_ENTIDAD,EVV_ENTIDAD_ID) VALUES (:id,:evidence,'EVALUACION',:evaluation)",
                ("id", graph.EvidenceLinkId), ("evidence", graph.EvidenceId), ("evaluation", graph.EvaluationId));
            await transaction.CommitAsync();
            return graph;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task AssertGraphRowsAsync(SyntheticGraph graph, long expectedCount)
    {
        foreach ((string table, string column, long id) in GraphRowsInDeleteOrder(graph))
            Assert.Equal(expectedCount, await CountAsync(table, column, id));
    }

    private async Task AssertOperationalRollbackAsync(SyntheticGraph graph)
    {
        await using OracleConnection connection = await OpenDisposableConnectionAsync();
        await using OracleTransaction transaction = (OracleTransaction)await connection.BeginTransactionAsync();
        bool injected = false;
        try
        {
            await DeleteGraphRowAsync(connection, transaction, "RL_MR_EVIDENCIAS_VINCULOS", "EVV_ID", graph.EvidenceLinkId, true);
            await DeleteGraphRowAsync(connection, transaction, "RL_MR_EVIDENCIAS", "EVI_ID", graph.EvidenceId, true);
            injected = true;
            throw new InvalidOperationException("SYNTHETIC_PURGE_INJECTED_FAILURE");
        }
        catch (InvalidOperationException ex) when (injected && ex.Message == "SYNTHETIC_PURGE_INJECTED_FAILURE")
        {
            await transaction.RollbackAsync();
        }
        Assert.True(injected);
        await AssertGraphRowsAsync(graph, expectedCount: 1);
    }

    private async Task DeleteOwnSyntheticGraphAsync(SyntheticGraph graph, bool requireRows)
    {
        await using OracleConnection connection = await OpenDisposableConnectionAsync();
        await using OracleTransaction transaction = (OracleTransaction)await connection.BeginTransactionAsync();
        try
        {
            foreach ((string table, string column, long id) in GraphRowsInDeleteOrder(graph))
                await DeleteGraphRowAsync(connection, transaction, table, column, id, requireRows);
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static async Task DeleteGraphRowAsync(OracleConnection connection, OracleTransaction transaction,
        string table, string column, long id, bool requireOne)
    {
        // Table and column are static test-only entries from GraphRowsInDeleteOrder,
        // never from user input. Every DELETE is parameterized by a generated ID.
        await using var command = new OracleCommand("DELETE FROM " + table + " WHERE " + column + "=:id", connection)
        {
            BindByName = true, Transaction = transaction
        };
        command.Parameters.Add(new OracleParameter("id", id));
        int affected = await command.ExecuteNonQueryAsync();
        if (requireOne) Assert.Equal(1, affected);
        else Assert.InRange(affected, 0, 1);
    }

    private void RequireDisposableXe()
    {
        Assert.True(
            string.Equals(Environment.GetEnvironmentVariable(RequiredOptIn), "true", StringComparison.OrdinalIgnoreCase),
            $"{RequiredOptIn}=true is required; the synthetic purge contract must not run implicitly.");
        Assert.False(string.IsNullOrWhiteSpace(_connectionString),
            $"{ConnectionVariable} must resolve a disposable LOCAL_XE connection.");
        if (!string.IsNullOrWhiteSpace(_connectionString))
        {
            string source = new OracleConnectionStringBuilder(_connectionString).DataSource;
            Assert.True(
                (source.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase)
                    || source.Contains("localhost", StringComparison.OrdinalIgnoreCase))
                && (source.Contains("SERVICE_NAME=XE", StringComparison.OrdinalIgnoreCase)
                    || source.Contains("/XE", StringComparison.OrdinalIgnoreCase)),
                "Synthetic purge requires the disposable loopback Oracle XE service.");
        }
    }

    private string CreateUnpooledConnectionString()
    {
        var builder = new OracleConnectionStringBuilder(_connectionString!)
        {
            Pooling = false,
            ConnectionTimeout = 30
        };
        return builder.ConnectionString;
    }

    private OracleConnection CreateConnection() => new(CreateUnpooledConnectionString())
    {
        UseHourOffsetForUnsupportedTimezone = true
    };

    private async Task<OracleConnection> OpenDisposableConnectionAsync()
    {
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            OracleConnection connection = CreateConnection();
            try
            {
                // ODP.NET async acquisition timed out before any SQL in CI;
                // existing Oracle integration bootstraps use synchronous Open().
                connection.Open();
                return connection;
            }
            catch (OracleException ex) when (
                attempt < 3 && (ex.Number is 50000 or 604 or 1033 or 1034 or 1089 or 1090 or 12514 or 12528 or 12541))
            {
                connection.Dispose();
                OracleConnection.ClearAllPools();
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }

        throw new InvalidOperationException("Disposable Oracle XE could not be opened after bounded attempts.");
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
        await using OracleConnection connection = await OpenDisposableConnectionAsync();
        await using OracleTransaction transaction = (OracleTransaction)await connection.BeginTransactionAsync();
        try
        {
            long familyId = await NextSequenceAsync(connection, transaction, "SEQ_RL_MR_FAMILIAS");
            long versionA = await NextSequenceAsync(connection, transaction, "SEQ_RL_MR_VERSIONES");
            long versionB = await NextSequenceAsync(connection, transaction, "SEQ_RL_MR_VERSIONES");
            await ExecuteAsync(connection, transaction,
                "INSERT INTO RL_MR_FAMILIAS_FORMULARIO (FAM_ID,FAM_CODIGO,FAM_NOMBRE,FAM_DESCRIPCION,FAM_ACTIVO,FAM_ARCHIVADA,FAM_PREDETERMINADA,FAM_FECHA_CREACION) VALUES (:id,:code,:name,:description,1,0,0,SYSDATE)",
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

    private async Task AssertRollbackOfVersionDeletionAsync(long versionId)
    {
        await using OracleConnection connection = await OpenDisposableConnectionAsync();
        await using OracleTransaction transaction = (OracleTransaction)await connection.BeginTransactionAsync();
        await using (var command = new OracleCommand(
            "DELETE FROM RL_MR_VERSIONES_FORMULARIO WHERE VER_ID=:id", connection)
            { BindByName = true, Transaction = transaction })
        {
            command.Parameters.Add(new OracleParameter("id", versionId));
            Assert.Equal(1, await command.ExecuteNonQueryAsync());
        }
        await transaction.RollbackAsync();
        Assert.Equal(1, await CountAsync("RL_MR_VERSIONES_FORMULARIO", "VER_ID", versionId));
    }

    private async Task AssertRollbackDoesNotLeaveFamilyAsync(string code)
    {
        await using OracleConnection connection = await OpenDisposableConnectionAsync();
        await using OracleTransaction transaction = (OracleTransaction)await connection.BeginTransactionAsync();
        await ExecuteAsync(connection, transaction,
            "INSERT INTO RL_MR_FAMILIAS_FORMULARIO (FAM_ID,FAM_CODIGO,FAM_NOMBRE,FAM_ACTIVO,FAM_ARCHIVADA,FAM_PREDETERMINADA,FAM_FECHA_CREACION) VALUES (SEQ_RL_MR_FAMILIAS.NEXTVAL,:code,'Rollback fixture',1,0,0,SYSDATE)",
            ("code", code));
        await transaction.RollbackAsync();
    }

    private async Task CleanupOwnFixtureAsync(long familyId, long versionA, long versionB, long recreatedFamilyId, string codePrefix)
    {
        await using OracleConnection connection = await OpenDisposableConnectionAsync();
        await ExecuteAsync(connection, "DELETE FROM RL_MR_FORMULA_USOS WHERE FUS_VERSION_FORMULARIO_ID IN (:a,:b)", ("a", versionA), ("b", versionB));
        await ExecuteAsync(connection, "DELETE FROM RL_MR_VERSIONES_FORMULARIO WHERE VER_ID IN (:a,:b)", ("a", versionA), ("b", versionB));
        await ExecuteAsync(connection, "DELETE FROM RL_MR_FAMILIAS_FORMULARIO WHERE FAM_ID IN (:family,:recreated) OR FAM_CODIGO LIKE :prefix", ("family", familyId), ("recreated", recreatedFamilyId), ("prefix", codePrefix + "%"));
        await ExecuteAsync(connection, "COMMIT");
    }

    private async Task<Snapshot> ReadProtectedSnapshotAsync(OracleConnection connection)
    {
        // Hash every row, ID, scalar value and CLOB in every non-Matrices RL_ table
        // actually present in the disposable XE fixture. A table absent from XE
        // cannot be claimed as coverage of its corresponding institutional module.
        var tables = new List<string>();
        await using (var command = new OracleCommand(
            "SELECT TABLE_NAME FROM USER_TABLES WHERE SUBSTR(TABLE_NAME,1,3)='RL_' AND SUBSTR(TABLE_NAME,1,6)<>'RL_MR_' ORDER BY TABLE_NAME", connection))
        await using (OracleDataReader reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync()) tables.Add(reader.GetString(0));
        }

        Assert.Contains("RL_USUARIOS", tables);
        Assert.Contains("RL_USUARIO_CAPACIDADES", tables);
        Assert.Contains("RL_AUDITORIA", tables);

        var digestInput = new StringBuilder();
        long totalRows = 0;
        foreach (string table in tables)
        {
            Assert.Matches("^[A-Z][A-Z0-9_]*$", table);
            string condition = table == "RL_AUDITORIA"
                ? " WHERE SUBSTR(NVL(t.AUD_TABLA,'-'),1,6)<>'RL_MR_'"
                : string.Empty;
            // ROWID makes deletion/reinsertion of otherwise identical values visible.
            // The filtered shared audit still retains Matrices' legitimate new rows.
            string sql = "SELECT ROWIDTOCHAR(t.ROWID), t.* FROM " + table
                + " t" + condition + " ORDER BY t.ROWID";
            await using var command = new OracleCommand(sql, connection);
            await using OracleDataReader reader = await command.ExecuteReaderAsync();
            var rowData = new StringBuilder();
            for (int column = 0; column < reader.FieldCount; column++)
                AppendFingerprintCell(rowData, reader.GetName(column));
            long count = 0;
            while (await reader.ReadAsync())
            {
                count++;
                for (int column = 0; column < reader.FieldCount; column++)
                {
                    if (await reader.IsDBNullAsync(column))
                    {
                        AppendFingerprintCell(rowData, "<NULL>");
                        continue;
                    }

                    string type = reader.GetDataTypeName(column);
                    string value;
                    if (string.Equals(type, "CLOB", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(type, "NCLOB", StringComparison.OrdinalIgnoreCase))
                    {
                        using OracleClob clob = reader.GetOracleClob(column);
                        value = clob.Value;
                    }
                    else if (string.Equals(type, "BLOB", StringComparison.OrdinalIgnoreCase))
                    {
                        using OracleBlob blob = reader.GetOracleBlob(column);
                        value = Convert.ToBase64String(blob.Value);
                    }
                    else
                    {
                        object raw = reader.GetValue(column);
                        value = raw switch
                        {
                            DateTime date => date.ToString("O", CultureInfo.InvariantCulture),
                            byte[] bytes => Convert.ToBase64String(bytes),
                            _ => Convert.ToString(raw, CultureInfo.InvariantCulture) ?? string.Empty
                        };
                    }
                    AppendFingerprintCell(rowData, value);
                }
            }
            totalRows += count;
            digestInput.Append(table).Append('|')
                .Append(count.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rowData.ToString()))))
                .Append('\n');
        }
        return new Snapshot(
            tables.Count,
            totalRows,
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(digestInput.ToString()))));
    }

    private static void AppendFingerprintCell(StringBuilder builder, string value)
    {
        builder.Append(value.Length.ToString(CultureInfo.InvariantCulture))
            .Append(':').Append(value);
    }

    private async Task<long> CountAsync(string table, string column, long value)
    {
        await using OracleConnection connection = await OpenDisposableConnectionAsync();
        return await ScalarLongAsync(connection, $"SELECT COUNT(*) FROM {table} WHERE {column}=:value", ("value", value));
    }

    private async Task<long> CountByCodeAsync(string table, string column, string value)
    {
        await using OracleConnection connection = await OpenDisposableConnectionAsync();
        return await ScalarLongAsync(connection, $"SELECT COUNT(*) FROM {table} WHERE {column}=:value", ("value", value));
    }

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

    private static async Task<long> ScalarLongAsync(OracleConnection connection, string sql, params (string Name, object Value)[] parameters)
    {
        Assert.Equal(System.Data.ConnectionState.Open, connection.State);
        return Convert.ToInt64(await ScalarAsync(connection, sql, parameters), CultureInfo.InvariantCulture);
    }

    private sealed record Snapshot(int TableCount, long RowCount, string Digest);
}
