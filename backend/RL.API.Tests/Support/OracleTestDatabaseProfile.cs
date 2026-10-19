using Microsoft.Extensions.Configuration;
using RL.API.Infrastructure.Database;

namespace RL.API.Tests.Support;

internal sealed class OracleTestDatabaseProfile
{
    public static bool TryResolveConfiguredLocalXe(IConfiguration configuration, out string? connectionString)
    {
        var profile = configuration[DatabaseProfileResolver.ProfileConfigurationKey];
        if (string.IsNullOrWhiteSpace(profile))
        {
            connectionString = null;
            return false;
        }

        if (!string.Equals(profile.Trim(), DatabaseProfileResolver.LocalXeProfileName, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Oracle integration tests require Database:Profile=LOCAL_XE.");

        connectionString = DatabaseProfileResolver.Resolve(configuration).ConnectionString;
        return true;
    }

    public static string RequireLocalXeConnection(string? connectionString)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddUserSecrets<OracleTestDatabaseProfile>(optional: true)
            .AddUserSecrets<OracleDbContext>(optional: true)
            .AddEnvironmentVariables()
            .Build();
        var profile = configuration[DatabaseProfileResolver.ProfileConfigurationKey];
        if (!string.Equals(profile?.Trim(), DatabaseProfileResolver.LocalXeProfileName, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Oracle integration tests require the explicit Database:Profile=LOCAL_XE setting.");

        connectionString ??= configuration.GetConnectionString("OracleDB_LocalXE");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("The disposable Oracle XE connection is not configured.");

        IConfiguration selectedLocalXe = new ConfigurationBuilder()
            .AddConfiguration(configuration)
            .AddInMemoryCollection(new Dictionary<string, string?>
        {
            [DatabaseProfileResolver.ProfileConfigurationKey] = DatabaseProfileResolver.LocalXeProfileName,
            ["ConnectionStrings:OracleDB_LocalXE"] = connectionString
        }).Build();

        return DatabaseProfileResolver.Resolve(selectedLocalXe).ConnectionString;
    }
}
