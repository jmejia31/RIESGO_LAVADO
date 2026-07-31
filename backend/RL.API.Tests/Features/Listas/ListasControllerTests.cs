#pragma warning disable CA1416
using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RL.API.Features.Listas;
using RL.API.Features.Listas.Application;
using RL.API.Features.Listas.Contracts;
using RL.API.Shared.Results;
using RL.API.Tests.Support;
using Xunit;

namespace RL.API.Tests.Features.Listas;

public sealed class ListasControllerTests
{
    [Fact]
    public void ObtenerPoliticaEvidencias_RetornaOk()
    {
        var controller = CrearController(out _, out var evidenciasStub, out _);
        
        var politica = new EvidenciaPoliticaDto(10, 10485760, new string[] { ".pdf" }, "PDF");
        evidenciasStub.On(nameof(IEvidenciasService.ObtenerPolitica), _ => politica);

        var result = controller.ObtenerPoliticaEvidencias();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ObtenerJuridicas_RetornaOk()
    {
        var controller = CrearController(out var listasStub, out _, out _);
        
        listasStub.On(nameof(IListasService.ObtenerJuridicasAsync), _ => Task.FromResult(new List<CoincidenciaJuridicaDto>()));

        var result = await controller.ObtenerJuridicas();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ObtenerNaturales_RetornaOk()
    {
        var controller = CrearController(out var listasStub, out _, out _);
        
        listasStub.On(nameof(IListasService.ObtenerNaturalesAsync), _ => Task.FromResult(new List<CoincidenciaNaturalDto>()));

        var result = await controller.ObtenerNaturales();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ObtenerEmpleados_RetornaOk()
    {
        var controller = CrearController(out var listasStub, out _, out _);
        
        listasStub.On(nameof(IListasService.ObtenerEmpleadosAsync), _ => Task.FromResult(new List<CoincidenciaEmpleadoDto>()));

        var result = await controller.ObtenerEmpleados();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ObtenerTiposDocumento_RetornaOk()
    {
        var controller = CrearController(out var listasStub, out _, out _);
        
        listasStub.On(nameof(IListasService.ObtenerTiposDocumentoAsync), _ => Task.FromResult(new List<TipoDocumentoDto>()));

        var result = await controller.ObtenerTiposDocumento();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ObtenerTiposListasCautela_RetornaOk()
    {
        var controller = CrearController(out var listasStub, out _, out _);
        
        listasStub.On(nameof(IListasService.ObtenerTiposListasCautelaAsync), _ => Task.FromResult(new List<TipoListaCautelaDto>()));

        var result = await controller.ObtenerTiposListasCautela();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ObtenerDetalleNatural_RetornaOk()
    {
        var controller = CrearController(out var listasStub, out _, out _);
        listasStub.On(nameof(IListasService.ObtenerDetalleNaturalAsync), _ => Task.FromResult(new List<DetalleCoincidenciaNaturalDto>()));

        var result = await controller.ObtenerDetalleNatural("0801");

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ObtenerDetalleEmpleado_RetornaOk()
    {
        var controller = CrearController(out var listasStub, out _, out _);
        listasStub.On(nameof(IListasService.ObtenerDetalleEmpleadoAsync), _ => Task.FromResult(new List<DetalleCoincidenciaEmpleadoDto>()));

        var result = await controller.ObtenerDetalleEmpleado("0801");

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ObtenerResumenListas_RetornaOk()
    {
        var controller = CrearController(out var listasStub, out _, out _);
        listasStub.On(nameof(IListasService.ObtenerResumenListasAsync), _ => Task.FromResult(new List<ResumenListaDto>()));

        var result = await controller.ObtenerResumenListas();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task CrearTipoListaCautela_RetornaOk()
    {
        var controller = CrearController(out var listasStub, out _, out _);
        var createdDto = new TipoListaCautelaCreadaDto(1, "Lista Creada", "CSV", 1);
        listasStub.On(nameof(IListasService.CrearTipoListaCautelaAsync), _ => Task.FromResult(ServiceResult<TipoListaCautelaCreadaDto>.Ok(createdDto)));

        var result = await controller.CrearTipoListaCautela(new TipoListaCautelaDto());

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ActualizarTipoListaCautela_RetornaOk()
    {
        var controller = CrearController(out var listasStub, out _, out _);
        listasStub.On(nameof(IListasService.ActualizarTipoListaCautelaAsync), _ => Task.FromResult(ServiceResult.Ok("Actualizado")));

        var result = await controller.ActualizarTipoListaCautela(1, new TipoListaCautelaDto());

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task EliminarTipoListaCautela_RetornaOk()
    {
        var controller = CrearController(out var listasStub, out _, out _);
        listasStub.On(nameof(IListasService.EliminarTipoListaCautelaAsync), _ => Task.FromResult(ServiceResult.Ok("Eliminado")));

        var result = await controller.EliminarTipoListaCautela(1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task UploadCautela_RetornaOk()
    {
        var controller = CrearController(out var listasStub, out _, out _);
        listasStub.On(nameof(IListasService.ProcesarCargaCautelaAsync), _ => Task.FromResult(ServiceResult.Ok("Cargado")));

        var fileStub = new FormFile(Stream.Null, 0, 100, "archivo", "test.csv");
        var request = new ListasController.UploadCautelaRequest
        {
            Archivo = fileStub,
            TipoListaCautelaId = 1
        };
        var result = await controller.UploadCautela(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }



    private static ListasController CrearController(
        out InterfaceStub listasStub,
        out InterfaceStub evidenciasStub,
        out InterfaceStub coincidenciasStub)
    {
        var listas = InterfaceStub.Create<IListasService>(out listasStub);
        var evidencias = InterfaceStub.Create<IEvidenciasService>(out evidenciasStub);
        var coincidencias = InterfaceStub.Create<ICoincidenciasService>(out coincidenciasStub);

        var controller = new ListasController(listas, evidencias, coincidencias);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "99"),
            new(ClaimTypes.Email, "test@ihss.hn"),
            new(ClaimTypes.Role, "ADMIN")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        
        var httpContext = new DefaultHttpContext { User = principal };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return controller;
    }
}
