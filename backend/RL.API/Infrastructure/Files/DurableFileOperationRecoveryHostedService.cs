using Microsoft.Extensions.Hosting;
using Oracle.ManagedDataAccess.Client;
using RL.API.Infrastructure.Database;

namespace RL.API.Infrastructure.Files;

public sealed class DurableFileOperationRecoveryHostedService : BackgroundService
{
    private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(30);
    private readonly OracleDbContext _database;
    private readonly ILogger<DurableFileOperationRecoveryHostedService> _logger;
    private readonly IReadOnlyList<DurableFileOperationJournal> _journals;

    public DurableFileOperationRecoveryHostedService(
        OracleDbContext database,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        ILogger<DurableFileOperationRecoveryHostedService> logger)
    {
        _database = database;
        _logger = logger;
        string matrixRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "Evidencias");
        string listasStorage = configuration["Evidencias:StoragePath"] ?? "App_Data/Evidencias";
        string listasRoot = Path.GetFullPath(Path.IsPathRooted(listasStorage)
            ? listasStorage
            : Path.Combine(environment.ContentRootPath, listasStorage));
        string configurationRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        string configurationJournal = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "Evidencias", ".configuration-image-operations");
        _journals = new[]
            {
                new DurableFileOperationJournal(matrixRoot),
                new DurableFileOperationJournal(listasRoot),
                new DurableFileOperationJournal(configurationRoot, configurationJournal)
            }
            .GroupBy(journal => journal.JournalPath, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        do
        {
            try
            {
                int recovered = 0;
                foreach (DurableFileOperationJournal journal in _journals)
                    recovered += await journal.RecoverPendingAsync(ExisteReferenciaOracleAsync, _logger, stoppingToken);
                if (recovered > 0)
                    _logger.LogInformation("Recovered {OperationCount} durable evidence file operation(s).", recovered);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError("Durable evidence recovery could not complete; pending journals will be retried. ErrorType={ErrorType}", exception.GetType().Name);
            }

            try
            {
                await Task.Delay(RetryInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        } while (!stoppingToken.IsCancellationRequested);
    }

    private async Task<bool> ExisteReferenciaOracleAsync(DurableFileOperationRecord record, CancellationToken cancellationToken)
    {
        await using OracleConnection connection = _database.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using OracleCommand command = connection.CreateCommand();
        command.BindByName = true;

        if (record.Type == DurableFileOperationType.MatrixEvidenceDelete)
        {
            command.CommandText = "SELECT COUNT(*) FROM RL_MR_EVIDENCIAS WHERE EVI_ID = :evidenceId";
            command.Parameters.Add(new OracleParameter("evidenceId", record.EvidenceId));
        }
        else if (record.Type == DurableFileOperationType.MatrixEvidenceUpload)
        {
            command.CommandText = "SELECT COUNT(*) FROM RL_MR_EVIDENCIAS WHERE EVI_RUTA = :databasePath";
            command.Parameters.Add(new OracleParameter("databasePath", record.DatabasePath));
        }
        else if (record.Type == DurableFileOperationType.ListEvidenceUpload)
        {
            command.CommandText = "SELECT COUNT(*) FROM RL_DETALLE_EVIDENCIA WHERE EVI_RUTA_ARCHIVO = :databasePath";
            command.Parameters.Add(new OracleParameter("databasePath", record.DatabasePath));
        }
        else
        {
            command.CommandText = @"SELECT COUNT(*)
                                     FROM RL_AUDITORIA
                                    WHERE AUD_TABLA = 'RL_LOGIN_SLIDES'
                                      AND AUD_REGISTRO_ID = :databasePath
                                      AND AUD_ACCION = 'INSERT'
                                      AND AUD_MODULO = 'Configuracion'";
            command.Parameters.Add(new OracleParameter("databasePath", record.DatabasePath));
        }

        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }
}
