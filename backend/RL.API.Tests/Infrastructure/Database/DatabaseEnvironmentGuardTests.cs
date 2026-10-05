using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using RL.API.Infrastructure.Database;
using Xunit;

namespace RL.API.Tests.Infrastructure.Database;

public class DatabaseEnvironmentGuardTests
{
    // 1. localhost + XE => PASS
    [Fact]
    public void Case01_Localhost_XE_Passes()
    {
        var config = new ConfigurationBuilder().Build();
        var connStr = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=XE)))";
        var result = DatabaseEnvironmentGuard.ValidateAndResolveDevelopmentConnection(connStr, config);
        Assert.Equal(connStr, result);
    }

    // 2. 127.0.0.1 + XE => PASS
    [Fact]
    public void Case02_127001_XE_Passes()
    {
        var config = new ConfigurationBuilder().Build();
        var connStr = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=127.0.0.1)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=XE)))";
        var result = DatabaseEnvironmentGuard.ValidateAndResolveDevelopmentConnection(connStr, config);
        Assert.Equal(connStr, result);
    }

    // 3. host remoto => REFUSED
    [Fact]
    public void Case03_RemoteHost_Refused()
    {
        var config = new ConfigurationBuilder().Build();
        var connStr = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=db-server.corp.ihss.hn)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=XE)))";
        var ex = Assert.Throws<InvalidOperationException>(() =>
            DatabaseEnvironmentGuard.ValidateAndResolveDevelopmentConnection(connStr, config));
        Assert.Contains("STARTUP_REFUSED", ex.Message);
    }

    // 4. IP privada remota => REFUSED
    [Fact]
    public void Case04_PrivateRemoteIP_Refused()
    {
        var config = new ConfigurationBuilder().Build();
        var connStr = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=192.168.1.100)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=XE)))";
        var ex = Assert.Throws<InvalidOperationException>(() =>
            DatabaseEnvironmentGuard.ValidateAndResolveDevelopmentConnection(connStr, config));
        Assert.Contains("STARTUP_REFUSED", ex.Message);
    }

    // 5. IP pública => REFUSED
    [Fact]
    public void Case05_PublicIP_Refused()
    {
        var config = new ConfigurationBuilder().Build();
        var connStr = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=200.50.10.5)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=XE)))";
        var ex = Assert.Throws<InvalidOperationException>(() =>
            DatabaseEnvironmentGuard.ValidateAndResolveDevelopmentConnection(connStr, config));
        Assert.Contains("STARTUP_REFUSED", ex.Message);
    }

    // 6. Cadena vacía => REFUSED
    [Fact]
    public void Case06_EmptyString_Refused()
    {
        var config = new ConfigurationBuilder().Build();
        var ex = Assert.Throws<InvalidOperationException>(() =>
            DatabaseEnvironmentGuard.ValidateAndResolveDevelopmentConnection("", config));
        Assert.Contains("STARTUP_REFUSED", ex.Message);
    }

    // 7. ConnectionString ausente (whitespace / null) => REFUSED
    [Fact]
    public void Case07_NullOrWhitespace_Refused()
    {
        var config = new ConfigurationBuilder().Build();
        var ex = Assert.Throws<InvalidOperationException>(() =>
            DatabaseEnvironmentGuard.ValidateAndResolveDevelopmentConnection("   ", config));
        Assert.Contains("STARTUP_REFUSED", ex.Message);
    }

    // 8. Malformed connection string => REFUSED
    [Fact]
    public void Case08_Malformed_Refused()
    {
        var config = new ConfigurationBuilder().Build();
        var ex1 = Assert.Throws<InvalidOperationException>(() =>
            DatabaseEnvironmentGuard.ValidateAndResolveDevelopmentConnection("User Id=test;Password=secret;", config));
        Assert.Contains("STARTUP_REFUSED", ex1.Message);

        var ex2 = Assert.Throws<InvalidOperationException>(() =>
            DatabaseEnvironmentGuard.ValidateAndResolveDevelopmentConnection("HOST=localhost;", config));
        Assert.Contains("STARTUP_REFUSED", ex2.Message);
    }

    // 9. Service distinto no autorizado => REFUSED
    [Fact]
    public void Case09_UnauthorizedService_Refused()
    {
        var config = new ConfigurationBuilder().Build();
        var connStr = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=127.0.0.1)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=ORCL)))";
        var ex = Assert.Throws<InvalidOperationException>(() =>
            DatabaseEnvironmentGuard.ValidateAndResolveDevelopmentConnection(connStr, config));
        Assert.Contains("STARTUP_REFUSED", ex.Message);
    }

    // 10. Target productivo histórico => REFUSED
    [Fact]
    public void Case10_HistoricalProductionTarget_Refused()
    {
        var config = new ConfigurationBuilder().Build();
        var connStr = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=10.1.19.112)(PORT=1521))(CONNECT_DATA=(SERVER=dedicated)(SERVICE_NAME=hpprod1)))";
        var ex = Assert.Throws<InvalidOperationException>(() =>
            DatabaseEnvironmentGuard.ValidateAndResolveDevelopmentConnection(connStr, config));
        Assert.Contains("STARTUP_REFUSED", ex.Message);
    }

    [Fact]
    public void Resolves_Password_From_Configuration_When_Missing()
    {
        var memConfig = new Dictionary<string, string?>
        {
            { "RL_ORACLE_PASSWORD", "LocalDevSecuredSecret123*" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(memConfig).Build();
        var connStr = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=XE)));User Id=RIESGO_LAVADO;";
        var result = DatabaseEnvironmentGuard.ValidateAndResolveDevelopmentConnection(connStr, config);
        Assert.Contains("Password=LocalDevSecuredSecret123*;", result);
    }
}
