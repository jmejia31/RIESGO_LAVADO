using Xunit;

namespace RL.API.Tests.Infrastructure.Health;

public sealed class P06OracleIdentityControllerContractTests
{
    [Fact]
    public void P06Probe_IsLocalOnly_ExplicitlyDisabledAndSelectOnly()
    {
        var source = ReadSource("backend/RL.API/Infrastructure/Health/P06OracleIdentityController.cs");

        Assert.Contains("P06:LocalIdentityProbeEnabled", source, StringComparison.Ordinal);
        Assert.Contains("IPAddress.IsLoopback", source, StringComparison.Ordinal);
        Assert.Contains("/__p06/oracle-identity", source, StringComparison.Ordinal);
        Assert.Contains("SELECT", source, StringComparison.Ordinal);
        Assert.Contains("SYS_CONTEXT('USERENV', 'SESSION_USER')", source, StringComparison.Ordinal);
        Assert.Contains("SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA')", source, StringComparison.Ordinal);
        Assert.Contains("SYS_CONTEXT('USERENV', 'SERVICE_NAME')", source, StringComparison.Ordinal);
        Assert.Contains("SYS_CONTEXT('USERENV', 'DB_UNIQUE_NAME')", source, StringComparison.Ordinal);
        Assert.Contains("SYS_CONTEXT('USERENV', 'DB_NAME')", source, StringComparison.Ordinal);
        Assert.Contains("SYS_CONTEXT('USERENV', 'INSTANCE_NAME')", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ConnectionString", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Password", source, StringComparison.Ordinal);
        Assert.DoesNotContain("INSERT INTO", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE ", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE FROM", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP ", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void P06Probe_ReturnsRuntimeProcessIdentityAndResolvedProfile()
    {
        var source = ReadSource("backend/RL.API/Infrastructure/Health/P06OracleIdentityController.cs");
        var program = ReadSource("backend/RL.API/Program.cs");

        Assert.Contains("Environment.ProcessId", source, StringComparison.Ordinal);
        Assert.Contains("_resolvedConnection.ProfileName", source, StringComparison.Ordinal);
        Assert.Contains("builder.Services.AddSingleton(databaseConnection)", program, StringComparison.Ordinal);
    }

    private static string ReadSource(string relativePath)
    {
        var root = FindRepositoryRoot();
        return File.ReadAllText(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
