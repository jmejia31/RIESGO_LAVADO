using RL.API.Infrastructure.Files;
using Xunit;

namespace RL.API.Tests.Infrastructure;

public sealed class DurableFileOperationJournalTests
{
    [Fact]
    public async Task ProcessTerminationReleasesLeaseAndAllowsRestartRecovery()
    {
        string root = CreateTemporaryRoot();
        try
        {
            string targetName = "crash.bin";
            string target = Path.Combine(root, targetName);
            await File.WriteAllTextAsync(target, "survives-process-termination");
            await DurableFileOperationCrashRecoveryOracleTests.RunCrashProbeAsync(root, "MatrixEvidenceDelete", targetName, targetName);

            var journal = new DurableFileOperationJournal(root);
            Assert.Equal(1, await journal.RecoverPendingAsync((_, _) => Task.FromResult(true), Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance));
            Assert.Equal("survives-process-termination", await File.ReadAllTextAsync(target));
            Assert.Equal(0, await journal.RecoverPendingAsync((_, _) => Task.FromResult(true), Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ProcessTerminationDuringReconciliationCanBeRetriedIdempotently()
    {
        string root = CreateTemporaryRoot();
        try
        {
            const string targetName = "reconcile.bin";
            await DurableFileOperationCrashRecoveryOracleTests.RunCrashProbeAsync(
                root,
                "MatrixEvidenceUpload",
                targetName,
                targetName,
                afterReconcile: true);

            var journal = new DurableFileOperationJournal(root);
            Assert.Equal(1, await journal.RecoverPendingAsync((_, _) => Task.FromResult(true), Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance));
            Assert.Equal("upload-after-crash", await File.ReadAllTextAsync(Path.Combine(root, targetName)));
            Assert.Equal(0, await journal.RecoverPendingAsync((_, _) => Task.FromResult(true), Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task UploadRecoveryPromotesPendingFileWhenDatabaseReferenceExistsAndIsIdempotent()
    {
        string root = CreateTemporaryRoot();
        try
        {
            var journal = new DurableFileOperationJournal(root);
            var operation = await journal.BeginAsync(
                DurableFileOperationType.MatrixEvidenceUpload,
                "evidence.bin",
                "App_Data/Evidencias/evidence.bin");
            await File.WriteAllTextAsync(operation.PendingPath, "durable-payload");
            string journalFile = operation.JournalFile;
            string target = operation.TargetPath;
            string pending = operation.PendingPath;
            await operation.DisposeAsync();

            Assert.Equal(1, await journal.RecoverPendingAsync((_, _) => Task.FromResult(true), Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance));
            Assert.Equal("durable-payload", await File.ReadAllTextAsync(target));
            Assert.False(File.Exists(journalFile));
            Assert.False(File.Exists(pending));
            Assert.Equal(0, await journal.RecoverPendingAsync((_, _) => Task.FromResult(true), Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task DeleteRecoveryRestoresFileWhenDatabaseReferenceStillExists()
    {
        string root = CreateTemporaryRoot();
        try
        {
            string target = Path.Combine(root, "evidence.bin");
            await File.WriteAllTextAsync(target, "keep-this-file");
            var journal = new DurableFileOperationJournal(root);
            var operation = await journal.BeginAsync(
                DurableFileOperationType.MatrixEvidenceDelete,
                "evidence.bin",
                "App_Data/Evidencias/evidence.bin",
                evidenceId: 51,
                hadPhysicalFile: true);
            File.Move(target, operation.PendingPath);
            string pending = operation.PendingPath;
            await operation.DisposeAsync();

            Assert.Equal(1, await journal.RecoverPendingAsync((_, _) => Task.FromResult(true), Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance));
            Assert.Equal("keep-this-file", await File.ReadAllTextAsync(target));
            Assert.False(File.Exists(pending));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task UploadRecoveryRemovesPendingFileWhenDatabaseReferenceIsAbsent()
    {
        string root = CreateTemporaryRoot();
        try
        {
            var journal = new DurableFileOperationJournal(root);
            var operation = await journal.BeginAsync(
                DurableFileOperationType.ListEvidenceUpload,
                "evidence.bin",
                "evidence.bin");
            await File.WriteAllTextAsync(operation.PendingPath, "orphan-payload");
            string pending = operation.PendingPath;
            string target = operation.TargetPath;
            await operation.DisposeAsync();

            Assert.Equal(1, await journal.RecoverPendingAsync((_, _) => Task.FromResult(false), Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance));
            Assert.False(File.Exists(pending));
            Assert.False(File.Exists(target));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RecoveryRemovesAnInterruptedJournalTempWithoutTouchingUnrelatedFiles()
    {
        string root = CreateTemporaryRoot();
        try
        {
            string journalDirectory = Path.Combine(root, ".operations");
            Directory.CreateDirectory(journalDirectory);
            string temporaryJournal = Path.Combine(journalDirectory, "interrupted.json.tmp");
            string unrelated = Path.Combine(root, "unrelated.bin");
            await File.WriteAllTextAsync(temporaryJournal, "partial");
            await File.WriteAllTextAsync(unrelated, "preserve");

            var journal = new DurableFileOperationJournal(root);
            Assert.Equal(0, await journal.RecoverPendingAsync((_, _) => Task.FromResult(false), Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance));
            Assert.False(File.Exists(temporaryJournal));
            Assert.Equal("preserve", await File.ReadAllTextAsync(unrelated));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTemporaryRoot()
    {
        string root = Path.Combine(Path.GetTempPath(), "rl-durable-file-journal-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
