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

    [Fact]
    public async Task RegistrarSeguimiento_FalloAlmacenamiento_CompensacionExitosa_AfirmaReversion()
    {
        var service = CrearServicio(out var repo);
        repo.On(nameof(IListasRepository.ContarPositivosActivosAsync), _ => Task.FromResult(1));
        repo.On(nameof(IListasRepository.ObtenerPositivoPorDocumentoAsync), _ =>
            Task.FromResult<ExistingPositivoDto?>(new ExistingPositivoDto { PositivoId = 100, TipoPositivoId = 2 }));
        repo.On(nameof(IListasRepository.RegistrarSeguimientoAsync), _ => Task.FromResult(1001L));
        repo.On(nameof(IListasRepository.GuardarEvidenciaMetaAsync), _ =>
            throw new InvalidOperationException("Fallo forzado al persistir evidencia"));
        repo.On(nameof(IListasRepository.EliminarSeguimientoLogicoAsync), _ => Task.FromResult(true));

        var pdfBytes = "%PDF-1.4\n%%EOF"u8.ToArray();
        var file = new Microsoft.AspNetCore.Http.FormFile(new MemoryStream(pdfBytes), 0, pdfBytes.Length, "archivo", "evidencia.pdf")
        {
            Headers = new Microsoft.AspNetCore.Http.HeaderDictionary { ["Content-Type"] = "application/pdf" }
        };

        var result = await service.RegistrarSeguimientoAsync("0801199012345", "Nota con fallo", [file], 1, 2);

        Assert.False(result.Success);
        Assert.Contains("la operación fue revertida", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Single(repo.CallsTo(nameof(IListasRepository.EliminarSeguimientoLogicoAsync)));
    }

    [Fact]
    public async Task RegistrarSeguimiento_FalloAlmacenamiento_FalloCompensacion_NoAfirmaReversionNoDemostrada()
    {
        var service = CrearServicio(out var repo);
        repo.On(nameof(IListasRepository.ContarPositivosActivosAsync), _ => Task.FromResult(1));
        repo.On(nameof(IListasRepository.ObtenerPositivoPorDocumentoAsync), _ =>
            Task.FromResult<ExistingPositivoDto?>(new ExistingPositivoDto { PositivoId = 100, TipoPositivoId = 2 }));
        repo.On(nameof(IListasRepository.RegistrarSeguimientoAsync), _ => Task.FromResult(1001L));
        repo.On(nameof(IListasRepository.GuardarEvidenciaMetaAsync), _ =>
            throw new InvalidOperationException("Fallo forzado al persistir evidencia"));
        // La compensación devuelve false (no se pudo inactivar en base de datos)
        repo.On(nameof(IListasRepository.EliminarSeguimientoLogicoAsync), _ => Task.FromResult(false));

        var pdfBytes = "%PDF-1.4\n%%EOF"u8.ToArray();
        var file = new Microsoft.AspNetCore.Http.FormFile(new MemoryStream(pdfBytes), 0, pdfBytes.Length, "archivo", "evidencia.pdf")
        {
            Headers = new Microsoft.AspNetCore.Http.HeaderDictionary { ["Content-Type"] = "application/pdf" }
        };

        var result = await service.RegistrarSeguimientoAsync("0801199012345", "Nota con fallo compensacion", [file], 1, 2);

        Assert.False(result.Success);
        // Debe indicar que la reversión no pudo confirmarse y NO afirmar que fue revertida
        Assert.DoesNotContain("la operación fue revertida", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no pudo confirmarse", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegistrarSeguimiento_FalloAlmacenamiento_ExcepcionCompensacion_NoAfirmaReversionNoDemostrada()
    {
        var service = CrearServicio(out var repo);
        repo.On(nameof(IListasRepository.ContarPositivosActivosAsync), _ => Task.FromResult(1));
        repo.On(nameof(IListasRepository.ObtenerPositivoPorDocumentoAsync), _ =>
            Task.FromResult<ExistingPositivoDto?>(new ExistingPositivoDto { PositivoId = 100, TipoPositivoId = 2 }));
        repo.On(nameof(IListasRepository.RegistrarSeguimientoAsync), _ => Task.FromResult(1001L));
        repo.On(nameof(IListasRepository.GuardarEvidenciaMetaAsync), _ =>
            throw new InvalidOperationException("Fallo forzado al persistir evidencia"));
        // La compensación arroja excepción
        repo.On(nameof(IListasRepository.EliminarSeguimientoLogicoAsync), _ =>
            throw new InvalidOperationException("DB inaccesible durante compensación"));

        var pdfBytes = "%PDF-1.4\n%%EOF"u8.ToArray();
        var file = new Microsoft.AspNetCore.Http.FormFile(new MemoryStream(pdfBytes), 0, pdfBytes.Length, "archivo", "evidencia.pdf")
        {
            Headers = new Microsoft.AspNetCore.Http.HeaderDictionary { ["Content-Type"] = "application/pdf" }
        };

        var result = await service.RegistrarSeguimientoAsync("0801199012345", "Nota con excepcion compensacion", [file], 1, 2);

        Assert.False(result.Success);
        Assert.DoesNotContain("la operación fue revertida", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no pudo confirmarse", result.Message, StringComparison.OrdinalIgnoreCase);
    }
}
