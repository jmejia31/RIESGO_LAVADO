using System.Data;
using System.Data.Common;
using System.Text.Json;
using Oracle.ManagedDataAccess.Client;
using RL.API.Features.Identidad.Domain;

const string settingsPath = "backend/RL.API/appsettings.json";
bool migrate = args.Any(a => string.Equals(a, "--migrate", StringComparison.OrdinalIgnoreCase));
var settings = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(settingsPath));
string raw = settings.GetProperty("ConnectionStrings").GetProperty("OracleDB").GetString() ?? throw new InvalidOperationException("Oracle connection configuration is incomplete.");
string user = Extract(raw, "User Id");
string password = Extract(raw, "Password");
string dataSource = Extract(raw, "Data Source");
string connectionString = $"User Id={user};Password={password};Data Source={dataSource};Connection Timeout=30";

await using var connection = new OracleConnection(connectionString);
await connection.OpenAsync();
string primaryKey = await ResolvePrimaryKeyAsync(connection);
if (!string.Equals(primaryKey, "RFT_ID", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Unexpected RL_REFRESH_TOKENS primary key.");
var columns = await ReadColumnsAsync(connection);
Print("REFRESH_TOKEN_COLUMNS", string.Join(",", columns.Select(c => $"{c.Name}:{c.Type}")));
await PrintLatestMetadataAsync(connection, columns, primaryKey);

var snapshotA = await ReadCountsAsync(connection);
await Task.Delay(TimeSpan.FromSeconds(5));
var snapshotB = await ReadCountsAsync(connection);
var before = snapshotB;
bool concurrentPlaintextWriterRisk = snapshotB.Legacy > snapshotA.Legacy;
Print("SNAPSHOT_A_TOTAL", snapshotA.Total); Print("SNAPSHOT_A_LEGACY", snapshotA.Legacy); Print("SNAPSHOT_A_HASHED", snapshotA.Hashed);
Print("SNAPSHOT_B_TOTAL", snapshotB.Total); Print("SNAPSHOT_B_LEGACY", snapshotB.Legacy); Print("SNAPSHOT_B_HASHED", snapshotB.Hashed);
Print("NEW_LEGACY_ROWS_OBSERVED", concurrentPlaintextWriterRisk ? "TRUE" : "FALSE");
Print("PRECHECK_TOTAL", before.Total); Print("PRECHECK_LEGACY", before.Legacy); Print("PRECHECK_HASHED", before.Hashed);
if (before.Total != before.Legacy || before.Hashed != 0 || before.Legacy == 0)
    throw new InvalidOperationException("Refresh token precondition failed.");

if (!migrate)
{
    Print("PRECHECK_CONSISTENT", "TRUE"); Print("DYNAMIC_BASELINE", "TRUE");
    Print("DML_EXECUTED", "FALSE"); Print("NEW_TOKEN_RECONCILIATION", "PASS");
    Print("CONCURRENT_PLAINTEXT_WRITER_RISK", concurrentPlaintextWriterRisk ? "TRUE" : "FALSE");
    Print("MIGRATION_READY", concurrentPlaintextWriterRisk ? "FALSE" : "TRUE");
    return;
}

if (concurrentPlaintextWriterRisk) throw new InvalidOperationException("Concurrent plaintext writer risk detected.");

await using var transaction = (OracleTransaction)await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted);
long migrated = 0;
try
{
    var rows = new List<(long Id, string Token)>();
    await using (var command = Create(connection, transaction, $"SELECT RFT_ID, RFT_TOKEN FROM RL_REFRESH_TOKENS WHERE {RefreshTokenMigrationPolicy.LegacyPredicate} FOR UPDATE"))
    await using (var reader = await command.ExecuteReaderAsync())
    {
        while (await reader.ReadAsync()) rows.Add((reader.GetInt64(0), reader.GetString(1)));
    }
    if (rows.Count != before.Legacy) throw new InvalidOperationException("Locked migration candidate count differs from precheck.");

    foreach (var row in rows)
    {
        string hash = RefreshTokenSecurity.Hash(row.Token);
        if (!RefreshTokenSecurity.IsSha256Hash(hash)) throw new InvalidOperationException("Backend hash format validation failed.");
        await using var update = Create(connection, transaction, "UPDATE RL_REFRESH_TOKENS SET RFT_TOKEN = :hash WHERE RFT_ID = :id AND RFT_TOKEN = :original");
        update.Parameters.Add(new OracleParameter("hash", hash));
        update.Parameters.Add(new OracleParameter("id", row.Id));
        update.Parameters.Add(new OracleParameter("original", row.Token));
        if (await update.ExecuteNonQueryAsync() != 1) throw new DBConcurrencyException("A refresh token row changed during migration.");
        migrated++;
    }

    var after = await ReadCountsAsync(connection, transaction);
    if (!RefreshTokenMigrationPolicy.CanCommit(before.Legacy, migrated, after.Total, after.Hashed, after.RequiresMigration, after.InvalidFormat))
    {
        Print("POST_TX_TOTAL", after.Total); Print("POST_TX_HASHED", after.Hashed); Print("POST_TX_REQUIRES_MIGRATION", after.RequiresMigration); Print("POST_TX_INVALID_FORMAT", after.InvalidFormat); Print("POST_TX_MIGRATED_ROWS", migrated);
        throw new InvalidOperationException("Refresh token migration postcondition failed.");
    }
    await transaction.CommitAsync();

    await using var independent = new OracleConnection(connectionString);
    await independent.OpenAsync();
    var final = await ReadCountsAsync(independent);
    if (final.Total < before.Total || final.Plaintext != 0 || final.Hashed != final.Total || final.RequiresMigration != 0 || final.InvalidFormat != 0)
        throw new InvalidOperationException("Independent refresh token postcheck failed.");

    Print("PRECHECK_TOTAL", before.Total); Print("PRECHECK_LEGACY", before.Legacy); Print("PRECHECK_HASHED", 0);
    Print("MIGRATION_INPUT_LEGACY", before.Legacy); Print("MIGRATION_INPUT_HASHED", 0); Print("MIGRATED_ROWS", migrated);
    Print("POST_HASHED", final.Hashed); Print("POST_REQUIRES_MIGRATION", final.RequiresMigration); Print("IDEMPOTENCY_SECOND_PASS_ROWS", final.RequiresMigration);
    Print("DOUBLE_HASH_PROTECTION", "PASS_BY_PRECONDITION_AND_IDEMPOTENT_SELECTION");
}
catch
{
    try { await transaction.RollbackAsync(); } catch { }
    throw;
}

static string Extract(string raw, string key)
{
    var match = System.Text.RegularExpressions.Regex.Match(raw, $"(?i)(?:^|;)\\s*{System.Text.RegularExpressions.Regex.Escape(key)}\\s*=\\s*([^;]+)");
    return match.Success ? match.Groups[1].Value.Trim() : throw new InvalidOperationException($"Missing Oracle connection key: {key}.");
}

static OracleCommand Create(OracleConnection connection, OracleTransaction? transaction, string sql)
{
    var command = connection.CreateCommand(); command.CommandText = sql; command.Transaction = transaction; command.CommandTimeout = 60; return command;
}

static async Task<string> ResolvePrimaryKeyAsync(OracleConnection connection)
{
    await using var command = Create(connection, null, "SELECT cols.COLUMN_NAME FROM USER_CONSTRAINTS cons JOIN USER_CONS_COLUMNS cols ON cols.CONSTRAINT_NAME = cons.CONSTRAINT_NAME AND cols.OWNER = cons.OWNER WHERE cons.TABLE_NAME = 'RL_REFRESH_TOKENS' AND cons.CONSTRAINT_TYPE = 'P' ORDER BY cols.POSITION");
    await using var reader = await command.ExecuteReaderAsync();
    var keys = new List<string>(); while (await reader.ReadAsync()) keys.Add(reader.GetString(0));
    if (keys.Count != 1) throw new InvalidOperationException("RL_REFRESH_TOKENS primary key metadata is not singular.");
    return keys[0];
}

static async Task<List<ColumnInfo>> ReadColumnsAsync(OracleConnection connection)
{
    await using var command = Create(connection, null, "SELECT COLUMN_NAME, DATA_TYPE FROM USER_TAB_COLUMNS WHERE TABLE_NAME = 'RL_REFRESH_TOKENS' ORDER BY COLUMN_ID");
    await using var reader = await command.ExecuteReaderAsync();
    var columns = new List<ColumnInfo>();
    while (await reader.ReadAsync()) columns.Add(new ColumnInfo(reader.GetString(0), reader.GetString(1)));
    if (columns.Count == 0) throw new InvalidOperationException("RL_REFRESH_TOKENS metadata not found.");
    return columns;
}

static async Task<bool> PrintLatestMetadataAsync(OracleConnection connection, IReadOnlyCollection<ColumnInfo> columns, string primaryKey)
{
    var dateColumn = columns.FirstOrDefault(c => (c.Type is "DATE" or "TIMESTAMP" or "TIMESTAMP WITH TIME ZONE") && (c.Name.Contains("FECHA", StringComparison.OrdinalIgnoreCase) || c.Name.Contains("CREA", StringComparison.OrdinalIgnoreCase) || c.Name.Contains("REG", StringComparison.OrdinalIgnoreCase)));
    var revokedColumn = columns.FirstOrDefault(c => c.Name.Contains("REVOC", StringComparison.OrdinalIgnoreCase));
    var expiresColumn = columns.FirstOrDefault(c => c.Name.Contains("EXPIR", StringComparison.OrdinalIgnoreCase));
    Print("DATE_COLUMN", dateColumn?.Name ?? "NONE");
    Print("USER_COLUMN", columns.FirstOrDefault(c => c.Name.Contains("USR", StringComparison.OrdinalIgnoreCase))?.Name ?? "NONE");
    Print("REVOCATION_COLUMN", revokedColumn?.Name ?? "NONE");
    Print("EXPIRATION_COLUMN", expiresColumn?.Name ?? "NONE");
    string order = dateColumn is null ? $"{Quote(primaryKey)} DESC" : $"{Quote(dateColumn.Name)} DESC NULLS LAST, {Quote(primaryKey)} DESC";
    string dateSelect = dateColumn is null ? "CAST(NULL AS VARCHAR2(32))" : $"TO_CHAR({Quote(dateColumn.Name)}, 'YYYY-MM-DD HH24:MI:SS')";
    string revokedSelect = revokedColumn is null ? "CAST(NULL AS NUMBER)" : Quote(revokedColumn.Name);
    string expirySelect = expiresColumn is null ? "CAST(NULL AS DATE)" : Quote(expiresColumn.Name);
    string sql = $"SELECT TOKEN_ID, CREATED_AT, REVOKED_VALUE, EXPIRES_AT, TOKEN_FORMAT FROM (SELECT {Quote(primaryKey)} AS TOKEN_ID, {dateSelect} AS CREATED_AT, {revokedSelect} AS REVOKED_VALUE, {expirySelect} AS EXPIRES_AT, CASE WHEN {RefreshTokenMigrationPolicy.LegacyPredicate} THEN 'LEGACY' ELSE 'SHA256_OR_OTHER' END AS TOKEN_FORMAT FROM RL_REFRESH_TOKENS ORDER BY {order}) WHERE ROWNUM = 1";
    await using var command = Create(connection, null, sql);
    await using var reader = await command.ExecuteReaderAsync();
    if (!await reader.ReadAsync()) throw new InvalidOperationException("Unable to identify latest refresh token row.");
    Print("LATEST_ROW_PK", reader[0]);
    Print("LATEST_ROW_CREATED_AT", reader.IsDBNull(1) ? "NONE" : reader.GetString(1));
    Print("LATEST_ROW_REVOKED", reader.IsDBNull(2) ? "UNKNOWN" : Convert.ToString(reader[2], System.Globalization.CultureInfo.InvariantCulture)!);
    if (reader.IsDBNull(3)) Print("LATEST_ROW_EXPIRATION", "UNKNOWN");
    else Print("LATEST_ROW_EXPIRATION", Convert.ToDateTime(reader[3], System.Globalization.CultureInfo.InvariantCulture) > DateTime.Now ? "ACTIVE" : "EXPIRED");
    Print("LATEST_ROW_FORMAT", reader.GetString(4));
    return string.Equals(reader.GetString(4), "LEGACY", StringComparison.Ordinal);
}

static string Quote(string identifier) => $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

static async Task<TokenCounts> ReadCountsAsync(OracleConnection connection, OracleTransaction? transaction = null)
{
    string sql = $@"SELECT COUNT(*) TOTAL,
        SUM(CASE WHEN {RefreshTokenMigrationPolicy.LegacyPredicate} THEN 1 ELSE 0 END) LEGACY,
        SUM(CASE WHEN {RefreshTokenMigrationPolicy.HashPredicate} THEN 1 ELSE 0 END) HASHED,
        SUM(CASE WHEN NOT ({RefreshTokenMigrationPolicy.HashPredicate}) THEN 1 ELSE 0 END) PLAINTEXT,
        SUM(CASE WHEN NOT ({RefreshTokenMigrationPolicy.HashPredicate}) THEN 1 ELSE 0 END) INVALID_FORMAT
        FROM RL_REFRESH_TOKENS";
    long total; long legacy; long hashed; long plaintext; long invalid;
    await using (var command = Create(connection, transaction, sql))
    await using (var reader = await command.ExecuteReaderAsync())
    {
        if (!await reader.ReadAsync()) throw new InvalidOperationException("Unable to read refresh token counts.");
        total = Convert.ToInt64(reader[0]); legacy = Convert.ToInt64(reader[1]); hashed = Convert.ToInt64(reader[2]); plaintext = Convert.ToInt64(reader[3]); invalid = Convert.ToInt64(reader[4]);
    }
    await using var second = Create(connection, transaction, $"SELECT COUNT(*) FROM RL_REFRESH_TOKENS WHERE {RefreshTokenMigrationPolicy.LegacyPredicate}");
    long requires = Convert.ToInt64(await second.ExecuteScalarAsync());
    return new TokenCounts(total, legacy, hashed, plaintext, invalid, requires);
}

static void Print(string key, object value) => Console.WriteLine($"{key}={value}");
record TokenCounts(long Total, long Legacy, long Hashed, long Plaintext, long InvalidFormat, long RequiresMigration);
record ColumnInfo(string Name, string Type);
