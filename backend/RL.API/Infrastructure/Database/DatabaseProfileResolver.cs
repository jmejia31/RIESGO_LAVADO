using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;

namespace RL.API.Infrastructure.Database;

public enum DatabaseProfile
{
    Production,
    LocalXe
}

public sealed class ResolvedDatabaseConnection
{
    internal ResolvedDatabaseConnection(DatabaseProfile profile, string connectionString)
    {
        Profile = profile;
        ConnectionString = connectionString;
    }

    public DatabaseProfile Profile { get; }

    public string ProfileName => Profile switch
    {
        DatabaseProfile.Production => DatabaseProfileResolver.ProductionProfileName,
        DatabaseProfile.LocalXe => DatabaseProfileResolver.LocalXeProfileName,
        _ => throw new InvalidOperationException("Perfil Oracle no compatible.")
    };

    public string ConnectionString { get; }
}

/// <summary>Resuelve el único perfil y la única fuente de conexión Oracle permitidos.</summary>
public static class DatabaseProfileResolver
{
    public const string ProfileConfigurationKey = "Database:Profile";
    public const string ProductionProfileName = "PRODUCTION";
    public const string LocalXeProfileName = "LOCAL_XE";

    private static readonly HashSet<string> LocalHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "localhost", "127.0.0.1", "::1"
    };

    public static ResolvedDatabaseConnection Resolve(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var configuredProfile = configuration[ProfileConfigurationKey];
        if (string.IsNullOrWhiteSpace(configuredProfile))
        {
            throw new InvalidOperationException(
                "STARTUP_REFUSED: Database:Profile no está configurado. Seleccione explícitamente PRODUCTION o LOCAL_XE.");
        }

        var profile = configuredProfile.Trim().ToUpperInvariant() switch
        {
            ProductionProfileName => DatabaseProfile.Production,
            LocalXeProfileName => DatabaseProfile.LocalXe,
            _ => throw new InvalidOperationException(
                "STARTUP_REFUSED: Database:Profile no es válido. Los únicos perfiles permitidos son PRODUCTION y LOCAL_XE.")
        };

        var connectionName = profile switch
        {
            DatabaseProfile.Production => "OracleDB_Production",
            DatabaseProfile.LocalXe => "OracleDB_LocalXE",
            _ => throw new InvalidOperationException("Perfil Oracle no compatible.")
        };

        var connectionString = configuration.GetConnectionString(connectionName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"STARTUP_REFUSED: ConnectionStrings:{connectionName} no está configurada para el perfil seleccionado.");
        }

        OracleConnectionStringBuilder connection;
        try
        {
            connection = new OracleConnectionStringBuilder(connectionString);
        }
        catch (ArgumentException)
        {
            throw new InvalidOperationException(
                $"STARTUP_REFUSED: ConnectionStrings:{connectionName} tiene un formato inválido.");
        }

        if (string.IsNullOrWhiteSpace(connection.DataSource) ||
            string.IsNullOrWhiteSpace(connection.UserID) ||
            string.IsNullOrWhiteSpace(connection.Password) ||
            IsPlaceholder(connection.DataSource) || IsPlaceholder(connection.UserID) || IsPlaceholder(connection.Password))
        {
            throw new InvalidOperationException(
                $"STARTUP_REFUSED: ConnectionStrings:{connectionName} requiere destino, usuario y contraseña configurados.");
        }

        var hostMatch = Regex.Match(connection.DataSource, @"(?i)HOST\s*=\s*([^\)\s;]+)");
        var serviceMatch = Regex.Match(connection.DataSource, @"(?i)SERVICE_NAME\s*=\s*([^\)\s;]+)");
        var host = hostMatch.Success ? hostMatch.Groups[1].Value.Trim() : string.Empty;
        var service = serviceMatch.Success ? serviceMatch.Groups[1].Value.Trim() : string.Empty;

        if (profile == DatabaseProfile.LocalXe &&
            (!LocalHosts.Contains(host) || !string.Equals(service, "XE", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                "STARTUP_REFUSED: LOCAL_XE acepta únicamente localhost/127.0.0.1 y el servicio XE.");
        }

        if (profile == DatabaseProfile.Production &&
            ((host.Length > 0 && LocalHosts.Contains(host)) || string.Equals(service, "XE", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                "STARTUP_REFUSED: PRODUCTION no puede apuntar al destino local Oracle XE.");
        }

        return new ResolvedDatabaseConnection(profile, connection.ConnectionString);
    }

    private static bool IsPlaceholder(string value) =>
        value.StartsWith('<') ||
        value.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("CHANGE_ME", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("REPLACE_", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("SET_", StringComparison.OrdinalIgnoreCase);
}
