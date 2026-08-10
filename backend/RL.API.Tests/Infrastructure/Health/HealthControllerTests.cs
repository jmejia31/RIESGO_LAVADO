using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RL.API.Infrastructure.Health;
using Xunit;

namespace RL.API.Tests.Infrastructure.Health;

public sealed class HealthControllerTests
{
    [Fact]
    public void Liveness_Retorna200Healthy_SinConsultarReadiness()
    {
        var probe = new StubReadinessProbe(false);
        var controller = new HealthController(probe);

        var action = controller.Liveness();

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
        var body = Assert.IsType<HealthProbeResponse>(ok.Value);
        Assert.Equal("Healthy", body.Status);
        Assert.Equal(0, probe.Calls);
    }

    [Fact]
    public async Task Readiness_CuandoOracleDisponible_Retorna200Healthy()
    {
        var probe = new StubReadinessProbe(true);
        var controller = new HealthController(probe);

        var action = await controller.Readiness(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
        var body = Assert.IsType<HealthProbeResponse>(ok.Value);
        Assert.Equal("Healthy", body.Status);
        Assert.Equal(1, probe.Calls);
    }

    [Fact]
    public async Task Readiness_CuandoOracleNoDisponible_Retorna503SinDetalleTecnico()
    {
        var probe = new StubReadinessProbe(false);
        var controller = new HealthController(probe);

        var action = await controller.Readiness(CancellationToken.None);

        var unavailable = Assert.IsType<ObjectResult>(action.Result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, unavailable.StatusCode);
        var body = Assert.IsType<HealthProbeResponse>(unavailable.Value);
        Assert.Equal("Unhealthy", body.Status);
        Assert.Equal(1, probe.Calls);
    }

    [Fact]
    public void ContratoHttp_UsaRutasRaizYPermiteMonitoreoAnonimo()
    {
        var controllerType = typeof(HealthController);
        Assert.NotEmpty(controllerType.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true));

        var liveness = controllerType.GetMethod(nameof(HealthController.Liveness));
        var readiness = controllerType.GetMethod(nameof(HealthController.Readiness));
        Assert.NotNull(liveness);
        Assert.NotNull(readiness);

        var healthz = Assert.Single(liveness!.GetCustomAttributes(typeof(HttpGetAttribute), inherit: false).Cast<HttpGetAttribute>());
        var readyz = Assert.Single(readiness!.GetCustomAttributes(typeof(HttpGetAttribute), inherit: false).Cast<HttpGetAttribute>());

        Assert.Equal("/healthz", healthz.Template);
        Assert.Equal("/readyz", readyz.Template);
    }

    [Theory]
    [InlineData(-5, HealthProbeOptions.MinOracleTimeoutSeconds)]
    [InlineData(0, HealthProbeOptions.MinOracleTimeoutSeconds)]
    [InlineData(3, 3)]
    [InlineData(99, HealthProbeOptions.MaxOracleTimeoutSeconds)]
    public void HealthProbeOptions_AcotaTimeoutOracle(int configured, int expected)
    {
        var options = new HealthProbeOptions { OracleTimeoutSeconds = configured };
        Assert.Equal(expected, options.GetEffectiveOracleTimeoutSeconds());
    }

    private sealed class StubReadinessProbe(bool isReady) : ISystemReadinessProbe
    {
        public int Calls { get; private set; }

        public Task<bool> IsReadyAsync(CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(isReady);
        }
    }
}
