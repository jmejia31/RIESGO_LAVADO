using System.Security.Cryptography;
using System.Text.Json;

namespace RL.API.Infrastructure.Files;

public enum DurableFileOperationType
{
    MatrixEvidenceUpload,
    MatrixEvidenceDelete,
    ListEvidenceUpload,
    ConfigurationImageUpload
}

public sealed record DurableFileOperationRecord(
    Guid OperationId,
    DurableFileOperationType Type,
    string TargetRelativePath,
    string PendingRelativePath,
    string DatabasePath,
    long? EvidenceId,
    bool HadPhysicalFile);

/// <summary>
/// Durable journal for application uploads that are paired with Oracle metadata or audit.
/// Entries are flushed before filesystem effects and resolved from the authoritative Oracle
/// reference after a process restart, including an ambiguous commit outcome.
/// </summary>
public sealed class DurableFileOperationJournal
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _rootPath;
    private readonly string _journalPath;

    public DurableFileOperationJournal(string rootPath, string? journalDirectory = null)
    {
        _rootPath = Path.GetFullPath(rootPath ?? throw new ArgumentNullException(nameof(rootPath)));
        _journalPath = Path.GetFullPath(journalDirectory ?? Path.Combine(_rootPath, ".operations"));
    }

    public string JournalPath => _journalPath;

    public async Task<Lease> BeginAsync(
        DurableFileOperationType type,
        string targetRelativePath,
        string databasePath,
        long? evidenceId = null,
        bool hadPhysicalFile = false,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_rootPath);
        Directory.CreateDirectory(_journalPath);

        _ = ResolveUnderRoot(targetRelativePath);
        Guid operationId = Guid.NewGuid();
        string suffix = type == DurableFileOperationType.MatrixEvidenceDelete ? "pending-delete" : "pending-upload";
        string pendingRelativePath = targetRelativePath + $".{suffix}-{operationId:N}";
        string journalFile = Path.Combine(_journalPath, operationId.ToString("N") + ".json");
        string lockFile = Path.Combine(_journalPath, operationId.ToString("N") + ".lock");
        var lockStream = new FileStream(lockFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.WriteThrough);
        var record = new DurableFileOperationRecord(
            operationId,
            type,
            NormalizeRelative(targetRelativePath),
            NormalizeRelative(pendingRelativePath),
            databasePath,
            evidenceId,
            hadPhysicalFile);

        try
        {
            await PersistRecordAsync(journalFile, record, cancellationToken);
            return new Lease(this, record, journalFile, lockStream);
        }
        catch
        {
            await lockStream.DisposeAsync();
            throw;
        }
    }

    public async Task<int> RecoverPendingAsync(
        Func<DurableFileOperationRecord, CancellationToken, Task<bool>> databaseReferenceExists,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(databaseReferenceExists);
        ArgumentNullException.ThrowIfNull(logger);
        if (!Directory.Exists(_journalPath)) return 0;

        RecoverIncompleteJournalWrites();
        int recovered = 0;
        foreach (string journalFile in Directory.EnumerateFiles(_journalPath, "*.json", SearchOption.TopDirectoryOnly).OrderBy(path => path, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            string lockFile = Path.ChangeExtension(journalFile, ".lock");
            FileStream? lockStream = null;
            try
            {
                lockStream = new FileStream(lockFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.WriteThrough);
            }
            catch (IOException)
            {
                // Another live request or instance owns this operation lease.
                continue;
            }

            await using (lockStream)
            {
                try
                {
                    DurableFileOperationRecord? record;
                    await using (var stream = new FileStream(journalFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan))
                    {
                        record = await JsonSerializer.DeserializeAsync<DurableFileOperationRecord>(stream, JsonOptions, cancellationToken);
                    }
                    if (record is null || record.OperationId.ToString("N") + ".json" != Path.GetFileName(journalFile))
                        throw new InvalidDataException("The durable file operation journal entry is invalid.");

                    bool referenceExists = await databaseReferenceExists(record, cancellationToken);
                    await ReconcileRecordAsync(record, referenceExists, cancellationToken);
                    File.Delete(journalFile);
                    recovered++;
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogError(
                        "Durable file operation {OperationId} could not be reconciled; the journal remains for retry. ErrorType={ErrorType}",
                        Path.GetFileNameWithoutExtension(journalFile),
                        exception.GetType().Name);
                }
            }

            if (!File.Exists(journalFile))
            {
                try { File.Delete(lockFile); } catch (IOException) { }
            }
        }

        return recovered;
    }

    private void RecoverIncompleteJournalWrites()
    {
        foreach (string temporaryFile in Directory.EnumerateFiles(_journalPath, "*.json.tmp", SearchOption.TopDirectoryOnly))
        {
            string name = Path.GetFileName(temporaryFile);
            string lockFile = Path.Combine(_journalPath, name[..^".json.tmp".Length] + ".lock");
            try
            {
                using var lockStream = new FileStream(lockFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.WriteThrough);
                File.Delete(temporaryFile);
            }
            catch (IOException)
            {
                // A live request may still be flushing this marker.
            }
        }
    }

    private async Task ReconcileRecordAsync(DurableFileOperationRecord record, bool databaseReferenceExists, CancellationToken cancellationToken)
    {
        string target = ResolveUnderRoot(record.TargetRelativePath);
        string pending = ResolveUnderRoot(record.PendingRelativePath);

        if (record.Type is DurableFileOperationType.MatrixEvidenceUpload or DurableFileOperationType.ListEvidenceUpload or DurableFileOperationType.ConfigurationImageUpload)
        {
            if (!databaseReferenceExists)
            {
                DeleteIfExists(pending);
                DeleteIfExists(target);
                return;
            }

            if (File.Exists(pending))
            {
                if (File.Exists(target))
                {
                    if (!await FilesHaveSameHashAsync(target, pending, cancellationToken))
                        throw new IOException("The evidence target is occupied by different content; automatic reconciliation was not performed.");
                    File.Delete(pending);
                }
                else
                {
                    File.Move(pending, target);
                }
            }
            else if (!File.Exists(target))
            {
                throw new FileNotFoundException("The Oracle evidence row exists, but neither its pending nor final file exists.");
            }

            return;
        }

        if (databaseReferenceExists)
        {
            if (File.Exists(pending))
            {
                if (File.Exists(target))
                {
                    if (!await FilesHaveSameHashAsync(target, pending, cancellationToken))
                        throw new IOException("The original evidence path is occupied by different content; automatic restoration was not performed.");
                    File.Delete(pending);
                }
                else
                {
                    File.Move(pending, target);
                }
            }
            else if (record.HadPhysicalFile && !File.Exists(target))
            {
                throw new FileNotFoundException("The Oracle evidence row exists, but its original and pending files are missing.");
            }

            return;
        }

        DeleteIfExists(pending);
        DeleteIfExists(target);
    }

    private string ResolveUnderRoot(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
            throw new InvalidDataException("A journal path must be relative to the evidence root.");

        string fullPath = Path.GetFullPath(Path.Combine(_rootPath, relativePath));
        string rootPrefix = _rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("A journal path resolves outside the evidence root.");
        return fullPath;
    }

    private static string NormalizeRelative(string value) => value.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

    private static async Task PersistRecordAsync(string journalFile, DurableFileOperationRecord record, CancellationToken cancellationToken)
    {
        string temporary = journalFile + ".tmp";
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(record, JsonOptions);
        await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.WriteThrough))
        {
            await stream.WriteAsync(bytes, cancellationToken);
            stream.Flush(flushToDisk: true);
        }

        File.Move(temporary, journalFile);
    }

    private static async Task<bool> FilesHaveSameHashAsync(string left, string right, CancellationToken cancellationToken)
    {
        await using var leftStream = new FileStream(left, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await using var rightStream = new FileStream(right, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        byte[] leftHash = await SHA256.HashDataAsync(leftStream, cancellationToken);
        byte[] rightHash = await SHA256.HashDataAsync(rightStream, cancellationToken);
        return CryptographicOperations.FixedTimeEquals(leftHash, rightHash);
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }

    private Task CompleteAsync(string journalFile)
    {
        if (File.Exists(journalFile)) File.Delete(journalFile);
        return Task.CompletedTask;
    }

    public sealed class Lease : IAsyncDisposable
    {
        private readonly DurableFileOperationJournal _owner;
        private readonly FileStream _lockStream;
        private bool _completed;

        internal Lease(DurableFileOperationJournal owner, DurableFileOperationRecord record, string journalFile, FileStream lockStream)
        {
            _owner = owner;
            Record = record;
            JournalFile = journalFile;
            _lockStream = lockStream;
            TargetPath = owner.ResolveUnderRoot(record.TargetRelativePath);
            PendingPath = owner.ResolveUnderRoot(record.PendingRelativePath);
        }

        public DurableFileOperationRecord Record { get; }
        public string JournalFile { get; }
        public string TargetPath { get; }
        public string PendingPath { get; }

        public async Task CompleteAsync()
        {
            await _owner.CompleteAsync(JournalFile);
            _completed = true;
        }

        public async ValueTask DisposeAsync()
        {
            await _lockStream.DisposeAsync();
            if (_completed)
            {
                string lockPath = Path.ChangeExtension(JournalFile, ".lock");
                try { File.Delete(lockPath); } catch (IOException) { }
            }
        }
    }
}
