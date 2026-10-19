using System.Diagnostics;
using Oracle.ManagedDataAccess.Client;
using RL.API.Infrastructure.Database;
using RL.API.Infrastructure.Files;
using Xunit;

namespace RL.API.Tests.Infrastructure;

public sealed class DurableFileOperationCrashRecoveryOracleTests
{
    internal const string ProbeRootVariable = "RL_P1_CRASH_PROBE_ROOT";
    internal const string ProbeTypeVariable = "RL_P1_CRASH_PROBE_TYPE";
    internal const string ProbeTargetVariable = "RL_P1_CRASH_PROBE_TARGET";
    internal const string ProbeDatabasePathVariable = "RL_P1_CRASH_PROBE_DATABASE_PATH";
    internal const string ProbeAfterReconcileVariable = "RL_P1_CRASH_PROBE_AFTER_RECONCILE";

    [Fact]
    [Trait("Category", "Block12OracleIntegration")]
    public async Task ChildProcessCrashesAreReconciledAgainstDisposableOracleState()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("RL_BLOCK12_FILE_RECOVERY_REQUIRED"), "true", StringComparison.OrdinalIgnoreCase))
            return;

        string connectionString = Support.OracleTestDatabaseProfile.RequireLocalXeConnection(null);
        var database = new OracleDbContext(connectionString);
        string table = "P1FR_" + Guid.NewGuid().ToString("N")[..20].ToUpperInvariant();
        string root = Path.Combine(Path.GetTempPath(), "rl-p1-crash-recovery-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        await ExecuteAsync(database, $"CREATE TABLE {table} (REF_PATH VARCHAR2(512) PRIMARY KEY)");

        try
        {
            string rollbackPath = "rollback-" + Guid.NewGuid().ToString("N") + ".bin";
            string rollbackTarget = Path.Combine(root, rollbackPath);
            await File.WriteAllTextAsync(rollbackTarget, "restore-after-crash");
            await InsertReferenceAsync(database, table, rollbackPath);
            await RunCrashProbeAsync(root, "MatrixEvidenceDelete", rollbackPath, rollbackPath);

            var journal = new DurableFileOperationJournal(root);
            Assert.Equal(1, await journal.RecoverPendingAsync(
                (record, token) => OracleReferenceExistsAsync(database, table, record.DatabasePath, token),
                Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance));
            Assert.Equal("restore-after-crash", await File.ReadAllTextAsync(rollbackTarget));
            Assert.Equal(0, await journal.RecoverPendingAsync(
                (record, token) => OracleReferenceExistsAsync(database, table, record.DatabasePath, token),
                Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance));
            await DeleteReferenceAsync(database, table, rollbackPath);

            string committedDeletePath = "committed-" + Guid.NewGuid().ToString("N") + ".bin";
            string committedDeleteTarget = Path.Combine(root, committedDeletePath);
            await File.WriteAllTextAsync(committedDeleteTarget, "delete-after-crash");
            await RunCrashProbeAsync(root, "MatrixEvidenceDelete", committedDeletePath, committedDeletePath);
            Assert.NotEmpty(Directory.GetFiles(root, committedDeletePath + ".pending-delete-*"));
            Assert.Equal(1, await journal.RecoverPendingAsync(
                (record, token) => OracleReferenceExistsAsync(database, table, record.DatabasePath, token),
                Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance));
            Assert.False(File.Exists(committedDeleteTarget));
            Assert.Empty(Directory.GetFiles(root, committedDeletePath + ".pending-delete-*"));

            string uploadPath = "upload-" + Guid.NewGuid().ToString("N") + ".bin";
            await InsertReferenceAsync(database, table, uploadPath);
            await RunCrashProbeAsync(root, "MatrixEvidenceUpload", uploadPath, uploadPath);
            Assert.Equal(1, await journal.RecoverPendingAsync(
                (record, token) => OracleReferenceExistsAsync(database, table, record.DatabasePath, token),
                Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance));
            Assert.Equal("upload-after-crash", await File.ReadAllTextAsync(Path.Combine(root, uploadPath)));
            await DeleteReferenceAsync(database, table, uploadPath);
        }
        finally
        {
            await ExecuteAsync(database, $"DROP TABLE {table} PURGE");
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task CrashProbeTerminatesWithoutReleasingItsJournal()
    {
        string? root = Environment.GetEnvironmentVariable(ProbeRootVariable);
        if (string.IsNullOrWhiteSpace(root)) return;

        string typeText = Environment.GetEnvironmentVariable(ProbeTypeVariable) ?? string.Empty;
        if (!Enum.TryParse(typeText, out DurableFileOperationType type))
            throw new InvalidOperationException("The crash probe operation type is invalid.");
        string target = Environment.GetEnvironmentVariable(ProbeTargetVariable) ?? string.Empty;
        string databasePath = Environment.GetEnvironmentVariable(ProbeDatabasePathVariable) ?? string.Empty;
        var journal = new DurableFileOperationJournal(root);
        await using var operation = await journal.BeginAsync(type, target, databasePath, evidenceId: 987654);

        if (type == DurableFileOperationType.MatrixEvidenceDelete)
            File.Move(operation.TargetPath, operation.PendingPath);
        else
            await File.WriteAllTextAsync(operation.PendingPath, "upload-after-crash");

        if (string.Equals(Environment.GetEnvironmentVariable(ProbeAfterReconcileVariable), "true", StringComparison.OrdinalIgnoreCase))
            File.Move(operation.PendingPath, operation.TargetPath);

        Environment.Exit(73);
    }

    internal static async Task RunCrashProbeAsync(string root, string type, string target, string databasePath, bool afterReconcile = false)
    {
        string repositoryRoot = FindRepositoryRoot();
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = repositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        start.ArgumentList.Add("test");
        start.ArgumentList.Add(Path.Combine(repositoryRoot, "backend", "RL.API.Tests", "RL.API.Tests.csproj"));
        start.ArgumentList.Add("--configuration");
        start.ArgumentList.Add("Release");
        start.ArgumentList.Add("--no-build");
        start.ArgumentList.Add("--no-restore");
        start.ArgumentList.Add("--filter");
        start.ArgumentList.Add("FullyQualifiedName~CrashProbeTerminatesWithoutReleasingItsJournal");
        start.Environment[ProbeRootVariable] = root;
        start.Environment[ProbeTypeVariable] = type;
        start.Environment[ProbeTargetVariable] = target;
        start.Environment[ProbeDatabasePathVariable] = databasePath;
        start.Environment[ProbeAfterReconcileVariable] = afterReconcile ? "true" : "false";

        using Process child = Process.Start(start) ?? throw new InvalidOperationException("The controlled crash probe could not be started.");
        Task<string> stdout = child.StandardOutput.ReadToEndAsync();
        Task<string> stderr = child.StandardError.ReadToEndAsync();
        await child.WaitForExitAsync().WaitAsync(TimeSpan.FromMinutes(2));
        _ = await stdout;
        _ = await stderr;
        Assert.NotEqual(0, child.ExitCode);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RIESGO_LAVADO.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("The repository root could not be located for the crash probe.");
    }

    private static async Task<bool> OracleReferenceExistsAsync(OracleDbContext database, string table, string path, CancellationToken token)
    {
        await using OracleConnection connection = database.CreateConnection();
        await connection.OpenAsync(token);
        await using OracleCommand command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = $"SELECT COUNT(*) FROM {table} WHERE REF_PATH = :path";
        command.Parameters.Add(new OracleParameter("path", path));
        return Convert.ToInt32(await command.ExecuteScalarAsync(token)) > 0;
    }

    private static async Task InsertReferenceAsync(OracleDbContext database, string table, string path)
    {
        await using OracleConnection connection = database.CreateConnection();
        await connection.OpenAsync();
        await using OracleCommand command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = $"INSERT INTO {table} (REF_PATH) VALUES (:path)";
        command.Parameters.Add(new OracleParameter("path", path));
        await command.ExecuteNonQueryAsync();
    }

    private static async Task DeleteReferenceAsync(OracleDbContext database, string table, string path)
    {
        await using OracleConnection connection = database.CreateConnection();
        await connection.OpenAsync();
        await using OracleCommand command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = $"DELETE FROM {table} WHERE REF_PATH = :path";
        command.Parameters.Add(new OracleParameter("path", path));
        await command.ExecuteNonQueryAsync();
    }

    private static async Task ExecuteAsync(OracleDbContext database, string sql)
    {
        await using OracleConnection connection = database.CreateConnection();
        await connection.OpenAsync();
        await using OracleCommand command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}
