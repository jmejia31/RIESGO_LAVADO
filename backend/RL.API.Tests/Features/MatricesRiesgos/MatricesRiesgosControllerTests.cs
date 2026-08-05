#pragma warning disable CA1416
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RL.API.Features.MatricesRiesgos;
using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Shared.Results;
using RL.API.Tests.Support;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatricesRiesgosControllerTests
{
    [Fact]
    public async Task ObtenerVersionVigente_Ok_RetornaContratoVersionado()
    {
        MatricesRiesgosController controller = CrearController(out InterfaceStub service);
        service.On(nameof(IMatricesRiesgosAppService.ObtenerVersionVigenteFormularioAsync), _ =>
            Task.FromResult(ServiceResult<VersionFormularioDto>.Ok(new VersionFormularioDto
            {
                VerId = 10,
                VerCodigo = "FORM_A",
                VerVersion = 2
            })));

        IActionResult result = await controller.ObtenerVersionVigenteFormulario("FORM_A");

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CrearEvaluacion_Ok_RetornaIdentificador()
    {
        MatricesRiesgosController controller = CrearController(out InterfaceStub service);
        service.On(nameof(IMatricesRiesgosAppService.CrearEvaluacionAsync), _ =>
            Task.FromResult(ServiceResult<long>.Ok(55)));

        IActionResult result = await controller.CrearEvaluacion(new EvaluacionRiesgoDto());

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(55L, ok.Value!.GetType().GetProperty("datos")!.GetValue(ok.Value));
    }

    [Fact]
    public async Task ActualizarEvaluacion_IdsDiferentes_RetornaBadRequest()
    {
        MatricesRiesgosController controller = CrearController(out _);

        IActionResult result = await controller.ActualizarEvaluacion(1, new EvaluacionRiesgoDto { EvaId = 2 });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ListarEvaluaciones_UsaContratoDinamico()
    {
        MatricesRiesgosController controller = CrearController(out InterfaceStub service);
        service.On(nameof(IMatricesRiesgosAppService.ListarEvaluacionesPaginadasAsync), _ =>
            Task.FromResult(ServiceResult<List<EvaluacionRiesgoDto>>.Ok(new List<EvaluacionRiesgoDto>())));

        IActionResult result = await controller.ListarEvaluacionesPaginadas(new ConsultaEvaluacionPaginadaDto());

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ObtenerConsolidado_RetornaFilasTipadas()
    {
        MatricesRiesgosController controller = CrearController(out InterfaceStub service);
        IReadOnlyList<RiesgoReporteFilaDto> filas = new List<RiesgoReporteFilaDto>
        {
            new() { RiesgoId = 1, EvaluacionId = 2, CodigoRiesgo = "R-001" }
        };
        service.On(nameof(IMatricesRiesgosAppService.ObtenerConsolidadoTipadoAsync), _ =>
            Task.FromResult(ServiceResult<IReadOnlyList<RiesgoReporteFilaDto>>.Ok(filas)));

        IActionResult result = await controller.ObtenerConsolidado();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ObtenerMetodologia_RetornaVersionSeccionesCatalogosYReglas()
    {
        MatricesRiesgosController controller = CrearController(out InterfaceStub service);
        var metodologia = new MetodologiaFormularioDto
        {
            VersionFormularioId = 10,
            Codigo = "FORM_A",
            Version = 2,
            Secciones = new[] { new SeccionFormularioDto { Clave = "s1" } },
            Catalogos = new[] { new CatalogoMatricesDto { Codigo = "CAT_A" } },
            Reglas = new[] { new ReglaCalculoMatricesDto { Codigo = "CALCULO_VRI_VRR", Version = "1.0" } }
        };
        service.On(nameof(IMatricesRiesgosAppService.ObtenerMetodologiaDinamicaVigenteAsync), _ =>
            Task.FromResult(ServiceResult<MetodologiaFormularioDto>.Ok(metodologia)));

        IActionResult result = await controller.ObtenerMetodologiaVigente();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ServicioRetornaNotFound_ControladorConservaStatusCode()
    {
        MatricesRiesgosController controller = CrearController(out InterfaceStub service);
        service.On(nameof(IMatricesRiesgosAppService.ObtenerMetodologiaDinamicaVigenteAsync), _ =>
            Task.FromResult(ServiceResult<MetodologiaFormularioDto>.NotFound("No existe")));

        IActionResult result = await controller.ObtenerMetodologiaVigente();

        ObjectResult response = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, response.StatusCode);
    }

    private static MatricesRiesgosController CrearController(out InterfaceStub serviceStub)
    {
        IMatricesRiesgosAppService service = InterfaceStub.Create<IMatricesRiesgosAppService>(out serviceStub);
        ILogger<MatricesRiesgosController> logger = InterfaceStub.Create<ILogger<MatricesRiesgosController>>(out InterfaceStub loggerStub);
        loggerStub.On("Log", _ => null);

        var controller = new MatricesRiesgosController(service, logger);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "99"),
            new(ClaimTypes.Email, "test@ihss.hn"),
            new(ClaimTypes.Role, "ADMIN")
        };
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
        };
        context.Request.Headers["X-Forwarded-For"] = "127.0.0.1";
        controller.ControllerContext = new ControllerContext { HttpContext = context };
        return controller;
    }
}
