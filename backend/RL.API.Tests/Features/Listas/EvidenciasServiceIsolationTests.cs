using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using RL.API.Features.Listas.Application;
using RL.API.Features.Listas.Contracts;
using RL.API.Features.Listas.Persistence;
using RL.API.Tests.Support;
using Xunit;

namespace RL.API.Tests.Features.Listas;

public sealed class EvidenciasServiceIsolationTests
{
    private static EvidenciasService CrearServicio(out InterfaceStub repoStub)
    {
        var repo = InterfaceStub.Create<IListasRepository>(out repoStub);
        var env = InterfaceStub.Create<IWebHostEnvironment>(out _);
        var config = new ConfigurationBuilder().Build();
        return new EvidenciasService(repo, config, env);
    }

    [Fact]
    public async Task RegistrarSeguimiento_AmbiguedadSinTipo_LanzaAmbiguousIdentityException()
    {
        var service = CrearServicio(out var repo);
        repo.On(nameof(IListasRepository.ContarPositivosActivosAsync), args =>
        {
            var tipo = (int)args[1]!;
            return Task.FromResult(tipo == 0 ? 2 : 1);
        });

        await Assert.ThrowsAsync<AmbiguousIdentityException>(async () =>
        {
            await service.RegistrarSeguimientoAsync("0801199012345", "Nota de seguimiento", null, 1);
        });
    }

    [Fact]
    public async Task RegistrarSeguimiento_ConTipoPositivoId_AislaPositivoYGuardaSeguimiento()
    {
        var service = CrearServicio(out var repo);
        repo.On(nameof(IListasRepository.ContarPositivosActivosAsync), _ => Task.FromResult(1));
        repo.On(nameof(IListasRepository.ObtenerPositivoPorDocumentoAsync), args =>
        {
            var tipo = (int?)args[1];
            return Task.FromResult<ExistingPositivoDto?>(new ExistingPositivoDto
            {
                PositivoId = tipo == 2 ? 100 : 200,
                TipoPositivoId = tipo ?? 2
            });
        });
        repo.On(nameof(IListasRepository.RegistrarSeguimientoAsync), args =>
        {
            var posId = (long)args[0]!;
            return Task.FromResult(posId == 100 ? 1001L : 2001L);
        });

        // Registrar seguimiento para Natural (tipoPositivoId = 2)
        var resultNatural = await service.RegistrarSeguimientoAsync("0801199012345", "Nota Natural", null, 1, 2);
        Assert.True(resultNatural.Success);

        // Registrar seguimiento para Empleado (tipoPositivoId = 3)
        var resultEmpleado = await service.RegistrarSeguimientoAsync("0801199012345", "Nota Empleado", null, 1, 3);
        Assert.True(resultEmpleado.Success);

        var callsRegistrar = repo.CallsTo(nameof(IListasRepository.RegistrarSeguimientoAsync));
        Assert.Equal(2, callsRegistrar.Count);
        Assert.Equal(100L, callsRegistrar[0].Arguments[0]);
        Assert.Equal(200L, callsRegistrar[1].Arguments[0]);
    }

    [Fact]
    public async Task RegistrarSeguimiento_NaturalSinPositivo_NoCaeEnEmpleado()
    {
        var service = CrearServicio(out var repo);
        repo.On(nameof(IListasRepository.ContarPositivosActivosAsync), _ => Task.FromResult(0));
        repo.On(nameof(IListasRepository.ObtenerPositivoPorDocumentoAsync), args =>
        {
            var tipo = (int?)args[1];
            if (tipo == 2) return Task.FromResult<ExistingPositivoDto?>(null);
            return Task.FromResult<ExistingPositivoDto?>(new ExistingPositivoDto { PositivoId = 200, TipoPositivoId = 3 });
        });

        // Al intentar registrar seguimiento para persona natural (tipo = 2), no debe asociarlo al empleado 200
        var result = await service.RegistrarSeguimientoAsync("0801199012345", "Nota para natural", null, 1, 2);
        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Empty(repo.CallsTo(nameof(IListasRepository.RegistrarSeguimientoAsync)));
    }
}
