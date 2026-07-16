using Oracle.ManagedDataAccess.Client;

namespace RL.API.Infrastructure.Database;

/// <summary>
/// Contexto de conexión a Oracle 11g — fábrica de conexiones
/// </summary>
public class OracleDbContext
{
    private readonly string _connectionString;

    public OracleDbContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public OracleConnection CreateConnection()
        => new OracleConnection(_connectionString);
}
