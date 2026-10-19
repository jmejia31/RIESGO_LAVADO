using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using RL.API.Infrastructure.Database;
using Xunit;

namespace RL.API.Tests.Infrastructure.Database;

public class DatabaseEnvironmentGuardTests
{
    private static readonly string FakeCredential = string.Concat("local", "-test-only", "-value");
    private static readonly string LocalXeConnection = CreateConnection("(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=127.0.0.1)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=XE)))");
    private static readonly string ProductionConnection = CreateConnection("(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=db-production.example.invalid)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=APPDB)))");

    private static string CreateConnection(string dataSource) => new OracleConnectionStringBuilder
    {
        DataSource = dataSource,
        UserID = "test",
        Password = FakeCredential
    }.ConnectionString;

    private static IConfiguration Configuration(string? profile, string? production = null, string? localXe = null, string? generic = null)
    {
        var settings = new Dictionary<string, string?>();
        if (profile is not null)
            settings[DatabaseProfileResolver.ProfileConfigurationKey] = profile;
        if (production is not null)
            settings["ConnectionStrings:OracleDB_Production"] = production;
        if (localXe is not null)
            settings["ConnectionStrings:OracleDB_LocalXE"] = localXe;
        if (generic is not null)
            settings["ConnectionStrings:OracleDB"] = generic;

        return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
    }

    [Fact]
    public void MissingProfileFailsClosedEvenWhenGenericConnectionExists()
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            DatabaseProfileResolver.Resolve(Configuration(null, generic: LocalXeConnection)));

        Assert.Contains("Database:Profile", error.Message);
        Assert.DoesNotContain(FakeCredential, error.Message);
    }

    [Fact]
    public void UnknownProfileFailsClosed()
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            DatabaseProfileResolver.Resolve(Configuration("STAGING", ProductionConnection, LocalXeConnection)));

        Assert.Contains("no es válido", error.Message);
    }

    [Fact]
    public void LegacyProfileAliasIsNotAccepted()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DATABASE_PROFILE"] = "LOCAL_XE",
            ["ConnectionStrings:OracleDB_LocalXE"] = LocalXeConnection
        }).Build();

        Assert.Throws<InvalidOperationException>(() => DatabaseProfileResolver.Resolve(configuration));
    }

    [Fact]
    public void ProductionProfileSelectsOnlyProductionConnection()
    {
        var resolved = DatabaseProfileResolver.Resolve(Configuration("PRODUCTION", ProductionConnection, LocalXeConnection));

        Assert.Equal(DatabaseProfile.Production, resolved.Profile);
        Assert.Contains("APPDB", resolved.ConnectionString);
        Assert.DoesNotContain("SERVICE_NAME=XE", resolved.ConnectionString);
    }

    [Fact]
    public void LocalXeProfileSelectsOnlyLocalXeConnection()
    {
        var resolved = DatabaseProfileResolver.Resolve(Configuration("LOCAL_XE", ProductionConnection, LocalXeConnection));

        Assert.Equal(DatabaseProfile.LocalXe, resolved.Profile);
        Assert.Contains("SERVICE_NAME=XE", resolved.ConnectionString);
        Assert.DoesNotContain("db-production", resolved.ConnectionString);
    }

    [Theory]
    [InlineData("PRODUCTION")]
    [InlineData("LOCAL_XE")]
    public void MissingProfileSpecificConnectionNeverFallsBackToGeneric(string profile)
    {
        var generic = profile == "LOCAL_XE" ? LocalXeConnection : ProductionConnection;
        var error = Assert.Throws<InvalidOperationException>(() =>
            DatabaseProfileResolver.Resolve(Configuration(profile, generic: generic)));

        Assert.Contains("ConnectionStrings:OracleDB_", error.Message);
    }

    [Fact]
    public void ProductionCannotSelectLocalXeEvenWhenExplicitlyConfigured()
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            DatabaseProfileResolver.Resolve(Configuration("PRODUCTION", LocalXeConnection, LocalXeConnection)));

        Assert.Contains("no puede apuntar", error.Message);
    }

    [Fact]
    public void LocalXeRejectsRemoteDestinationAndNeverFallsBackToProduction()
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            DatabaseProfileResolver.Resolve(Configuration("LOCAL_XE", ProductionConnection, ProductionConnection)));

        Assert.Contains("LOCAL_XE acepta únicamente", error.Message);
    }

    [Fact]
    public void MissingProductionConfigurationFailsWithoutUsingLocalXe()
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            DatabaseProfileResolver.Resolve(Configuration("PRODUCTION", localXe: LocalXeConnection)));

        Assert.Contains("ConnectionStrings:OracleDB_Production", error.Message);
    }

    [Fact]
    public void MissingLocalXeConfigurationFailsWithoutUsingProduction()
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            DatabaseProfileResolver.Resolve(Configuration("LOCAL_XE", production: ProductionConnection)));

        Assert.Contains("ConnectionStrings:OracleDB_LocalXE", error.Message);
    }

    [Fact]
    public void MissingCredentialsFailWithoutLeakingConfigurationValues()
    {
        const string missingPassword = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=db-production.example.invalid)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=APPDB)));User Id=test;";
        var error = Assert.Throws<InvalidOperationException>(() =>
            DatabaseProfileResolver.Resolve(Configuration("PRODUCTION", production: missingPassword)));

        Assert.Contains("usuario y contraseña", error.Message);
        Assert.DoesNotContain(FakeCredential, error.Message);
        Assert.DoesNotContain("db-production", error.Message);
    }

    [Fact]
    public void ExampleCredentialMarkerFailsClosed()
    {
        var example = CreateConnection("(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=db-production.example.invalid)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=APPDB)))");
        var marked = new OracleConnectionStringBuilder(example) { Password = "<set-through-approved-secret-source>" }.ConnectionString;
        var error = Assert.Throws<InvalidOperationException>(() =>
            DatabaseProfileResolver.Resolve(Configuration("PRODUCTION", production: marked)));

        Assert.Contains("requiere destino, usuario y contraseña configurados", error.Message);
    }

    [Fact]
    public void LegacyGuardDelegatesToTheExplicitResolverAndIgnoresGenericConnection()
    {
        var configuration = Configuration("LOCAL_XE", localXe: LocalXeConnection, generic: ProductionConnection);

        var resolved = DatabaseEnvironmentGuard.ValidateAndResolveDevelopmentConnection(ProductionConnection, configuration);

        Assert.Contains("SERVICE_NAME=XE", resolved);
        Assert.DoesNotContain("APPDB", resolved);
    }
}
