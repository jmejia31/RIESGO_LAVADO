using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using RL.API.Infrastructure.RateLimiting;

namespace RL.API.Tests.Infrastructure;

public sealed class ApplicationRateLimitingTests
{
    [Theory]
    [InlineData("POST", "/api/auth/login", RateLimitPolicies.Login)]
    [InlineData("POST", "/API/AUTH/LOGIN", RateLimitPolicies.Login)]
    [InlineData("POST", "/api/auth/recuperar-password", RateLimitPolicies.PasswordRecovery)]
    [InlineData("POST", "/api/auth/refresh", RateLimitPolicies.RefreshToken)]
    [InlineData("GET", "/api/matrices-riesgos/reportes/consolidado.xlsx", RateLimitPolicies.ReportExport)]
    [InlineData("GET", "/api/matrices-riesgos/reportes/consolidado.pdf", RateLimitPolicies.ReportExport)]
    [InlineData("POST", "/api/matrices-riesgos/evidencias/cargar", RateLimitPolicies.EvidenceUpload)]
    public void ForRequest_ProtegeRutasSensiblesConPoliticaEsperada(
        string method,
        string path,
        string expectedPolicy)
    {
        var context = CreateContext(method, path, userId: "42");
        var partition = ApplicationRateLimitPartitions.ForRequest(context, new RateLimitingSettings());

        Assert.StartsWith(expectedPolicy + ":", partition.PartitionKey, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("GET", "/api/auth/login")]
    [InlineData("POST", "/api/matrices-riesgos/consolidado")]
    [InlineData("GET", "/healthz")]
    [InlineData("GET", "/readyz")]
    [InlineData("GET", "/api/auth/perfil")]
    public void ForRequest_NoLimitaRutasFueraDelAlcanceBe04(string method, string path)
    {
        var context = CreateContext(method, path, userId: "42");
        var partition = ApplicationRateLimitPartitions.ForRequest(context, new RateLimitingSettings());

        Assert.Equal(RateLimitPolicies.Unlimited, partition.PartitionKey);
    }

    [Fact]
    public void AnonymousPartitionKey_UsaRemoteIpYNoConfiaEnForwardedFor()
    {
        var context = CreateContext("POST", "/api/auth/login", ip: "10.20.30.40");
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.77";
        context.Request.Headers["X-Real-IP"] = "198.51.100.25";

        var key = ApplicationRateLimitPartitions.AnonymousPartitionKey(context);

        Assert.Equal("ip:10.20.30.40", key);
    }

    [Fact]
    public void AuthenticatedPartitionKey_PriorizaIdentificadorDelUsuario()
    {
        var context = CreateContext("GET", "/api/matrices-riesgos/reportes/consolidado.pdf", userId: " 901 ");

        var key = ApplicationRateLimitPartitions.AuthenticatedPartitionKey(context);

        Assert.Equal("user:901", key);
    }

    [Fact]
    public void Login_RespetaPermitLimitSinCola()
    {
        var settings = new RateLimitingSettings
        {
            LoginPermitLimit = 2,
            LoginWindowSeconds = 60
        };
        var context = CreateContext("POST", "/api/auth/login", ip: "10.0.0.8");
        using var limiter = PartitionedRateLimiter.Create<HttpContext, string>(
            request => ApplicationRateLimitPartitions.ForRequest(request, settings));

        using var first = limiter.AttemptAcquire(context);
        using var second = limiter.AttemptAcquire(context);
        using var rejected = limiter.AttemptAcquire(context);

        Assert.True(first.IsAcquired);
        Assert.True(second.IsAcquired);
        Assert.False(rejected.IsAcquired);
        Assert.True(rejected.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter));
        Assert.True(retryAfter > TimeSpan.Zero);
    }

    [Fact]
    public void ConfiguracionInvalida_SeNormalizaANivelesSeguros()
    {
        var settings = new RateLimitingSettings
        {
            LoginPermitLimit = 0,
            LoginWindowSeconds = 0
        };
        var context = CreateContext("POST", "/api/auth/login");
        using var limiter = PartitionedRateLimiter.Create<HttpContext, string>(
            request => ApplicationRateLimitPartitions.ForRequest(request, settings));

        using var first = limiter.AttemptAcquire(context);
        using var rejected = limiter.AttemptAcquire(context);

        Assert.True(first.IsAcquired);
        Assert.False(rejected.IsAcquired);
    }

    [Fact]
    public void Reportes_SeAislanPorUsuarioAutenticado()
    {
        var settings = new RateLimitingSettings
        {
            ReportExportPermitLimit = 1,
            ReportExportWindowSeconds = 60
        };
        var userA = CreateContext("GET", "/api/matrices-riesgos/reportes/consolidado.xlsx", userId: "101");
        var userB = CreateContext("GET", "/api/matrices-riesgos/reportes/consolidado.xlsx", userId: "202");
        using var limiter = PartitionedRateLimiter.Create<HttpContext, string>(
            request => ApplicationRateLimitPartitions.ForRequest(request, settings));

        using var a1 = limiter.AttemptAcquire(userA);
        using var a2 = limiter.AttemptAcquire(userA);
        using var b1 = limiter.AttemptAcquire(userB);

        Assert.True(a1.IsAcquired);
        Assert.False(a2.IsAcquired);
        Assert.True(b1.IsAcquired);
    }

    [Fact]
    public void RutaNoProtegida_PermaneceSinLimiteGlobal()
    {
        var context = CreateContext("GET", "/healthz");
        using var limiter = PartitionedRateLimiter.Create<HttpContext, string>(
            request => ApplicationRateLimitPartitions.ForRequest(request, new RateLimitingSettings()));

        for (var i = 0; i < 100; i++)
        {
            using var lease = limiter.AttemptAcquire(context);
            Assert.True(lease.IsAcquired);
        }
    }

    private static DefaultHttpContext CreateContext(
        string method,
        string path,
        string ip = "127.0.0.1",
        string? userId = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Connection.RemoteIpAddress = IPAddress.Parse(ip);

        if (userId is not null)
        {
            context.User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, userId) },
                    authenticationType: "test"));
        }

        return context;
    }
}
