#pragma warning disable CA1416
using System;
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
    public async Task ObtenerVersionVigenteFormulario_Ok_RetornaDatos()
    {
        var controller = CrearController(out var serviceStub, out _);
        var expected = new VersionFormularioDto { VerId = 1, VerCodigo = "FORM_A" };
        serviceStub.On(nameof(IMatricesRiesgosAppService.ObtenerVersionVigenteFormularioAsync), _ => Task.FromResult(ServiceResult<VersionFormularioDto>.Ok(expected)));

        var result = await controller.ObtenerVersionVigenteFormulario("FORM_A");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);
    }

    [Fact]
    public async Task ObtenerVersionVigenteFormulario_Fallo_RetornaStatusCodeCorrespondiente()
    {
        var controller = CrearController(out var serviceStub, out _);
        serviceStub.On(nameof(IMatricesRiesgosAppService.ObtenerVersionVigenteFormularioAsync), _ => Task.FromResult(ServiceResult<VersionFormularioDto>.NotFound("No encontrado")));

        var result = await controller.ObtenerVersionVigenteFormulario("FORM_A");

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, statusResult.StatusCode);
    }

    [Fact]
    public async Task CrearBorradorFormulario_AdminRole_RetornaOk()
    {
        var controller = CrearController(out var serviceStub, out _);
        serviceStub.On(nameof(IMatricesRiesgosAppService.CrearBorradorFormularioAsync), _ => Task.FromResult(ServiceResult<long>.Ok(10L)));

        var result = await controller.CrearBorradorFormulario(1, "FORM_A", "{}");

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ClonarVersionFormulario_RetornaOk()
    {
        var controller = CrearController(out var serviceStub, out _);
        serviceStub.On(nameof(IMatricesRiesgosAppService.ClonarVersionFormularioAsync), _ => Task.FromResult(ServiceResult<long>.Ok(11L)));

        var result = await controller.ClonarVersionFormulario(1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ActualizarBorradorFormulario_RetornaOk()
    {
        var controller = CrearController(out var serviceStub, out _);
        serviceStub.On(nameof(IMatricesRiesgosAppService.ActualizarBorradorFormularioAsync), _ => Task.FromResult(ServiceResult.Ok("Actualizado")));

        var result = await controller.ActualizarBorradorFormulario(1, "{}");

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task PublicarVersionFormulario_RetornaOk()
    {
        var controller = CrearController(out var serviceStub, out _);
        serviceStub.On(nameof(IMatricesRiesgosAppService.PublicarVersionFormularioAsync), _ => Task.FromResult(ServiceResult.Ok("Publicado")));

        var result = await controller.PublicarVersionFormulario(1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task CambiarEstadoVigenciaFormulario_RetornaOk()
    {
        var controller = CrearController(out var serviceStub, out _);
        serviceStub.On(nameof(IMatricesRiesgosAppService.CambiarEstadoVigenciaFormularioAsync), _ => Task.FromResult(ServiceResult.Ok("Cambiado")));

        var result = await controller.CambiarEstadoVigenciaFormulario(1, true);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ListarHistorialVersionesFormulario_RetornaOk()
    {
        var controller = CrearController(out var serviceStub, out _);
        serviceStub.On(nameof(IMatricesRiesgosAppService.ListarHistorialVersionesFormularioAsync), _ => Task.FromResult(ServiceResult<List<VersionFormularioDto>>.Ok(new List<VersionFormularioDto>())));

        var result = await controller.ListarHistorialVersionesFormulario("FORM_A");

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ObtenerEvaluacion_RetornaOk()
    {
        var controller = CrearController(out var serviceStub, out _);
        var expected = new EvaluacionRiesgoDto { EvaId = 1 };
        serviceStub.On(nameof(IMatricesRiesgosAppService.ObtenerEvaluacionAsync), _ => Task.FromResult(ServiceResult<EvaluacionRiesgoDto>.Ok(expected)));

        var result = await controller.ObtenerEvaluacion(1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ListarEvaluacionesPaginadas_RetornaOk()
    {
        var controller = CrearController(out var serviceStub, out _);
        serviceStub.On(nameof(IMatricesRiesgosAppService.ListarEvaluacionesPaginadasAsync), _ => Task.FromResult(ServiceResult<List<EvaluacionRiesgoDto>>.Ok(new List<EvaluacionRiesgoDto>())));

        var result = await controller.ListarEvaluacionesPaginadas(new ConsultaEvaluacionPaginadaDto());

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task CrearEvaluacion_RetornaOk()
    {
        var controller = CrearController(out var serviceStub, out _);
        serviceStub.On(nameof(IMatricesRiesgosAppService.CrearEvaluacionAsync), _ => Task.FromResult(ServiceResult<long>.Ok(100L)));

        var result = await controller.CrearEvaluacion(new EvaluacionRiesgoDto());

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ActualizarEvaluacion_IdsNoCoinciden_RetornaBadRequest()
    {
        var controller = CrearController(out _, out _);
        var result = await controller.ActualizarEvaluacion(1, new EvaluacionRiesgoDto { EvaId = 2 });

        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badResult.Value);
    }

    [Fact]
    public async Task ActualizarEvaluacion_IdsCoinciden_RetornaOk()
    {
        var controller = CrearController(out var serviceStub, out _);
        serviceStub.On(nameof(IMatricesRiesgosAppService.ActualizarEvaluacionAsync), _ => Task.FromResult(ServiceResult.Ok("Actualizado")));

        var result = await controller.ActualizarEvaluacion(1, new EvaluacionRiesgoDto { EvaId = 1 });

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task TransicionarEstadoEvaluacion_RetornaOk()
    {
        var controller = CrearController(out var serviceStub, out _);
        serviceStub.On(nameof(IMatricesRiesgosAppService.TransicionarEstadoEvaluacionAsync), _ => Task.FromResult(ServiceResult.Ok("Transicionado")));

        var result = await controller.TransicionarEstadoEvaluacion(1, "EN_REVISION", "Aprobar");

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ObtenerRevisionesEvaluacion_RetornaOk()
    {
        var controller = CrearController(out var serviceStub, out _);
        serviceStub.On(nameof(IMatricesRiesgosAppService.ObtenerRevisionesEvaluacionAsync), _ => Task.FromResult(ServiceResult<List<RevisionEvaluacionDto>>.Ok(new List<RevisionEvaluacionDto>())));

        var result = await controller.ObtenerRevisionesEvaluacion(1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ObtenerConsolidado_RetornaOk()
    {
        var controller = CrearController(out var serviceStub, out _);
        serviceStub.On(nameof(IMatricesRiesgosAppService.ObtenerConsolidadoMatricesAsync), _ => Task.FromResult(ServiceResult<List<Dictionary<string, object>>>.Ok(new List<Dictionary<string, object>>())));

        var result = await controller.ObtenerConsolidado();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task VincularEvidencias_RetornanOk()
    {
        var controller = CrearController(out var serviceStub, out _);
        serviceStub.On(nameof(IMatricesRiesgosAppService.VincularEvidenciaRiesgoAsync), _ => Task.FromResult(ServiceResult.Ok("Vinculado")));
        serviceStub.On(nameof(IMatricesRiesgosAppService.VincularEvidenciaEvaluacionAsync), _ => Task.FromResult(ServiceResult.Ok("Vinculado")));
        serviceStub.On(nameof(IMatricesRiesgosAppService.VincularEvidenciaControlAsync), _ => Task.FromResult(ServiceResult.Ok("Vinculado")));
        serviceStub.On(nameof(IMatricesRiesgosAppService.VincularEvidenciaPlanAsync), _ => Task.FromResult(ServiceResult.Ok("Vinculado")));
        serviceStub.On(nameof(IMatricesRiesgosAppService.VincularEvidenciaActividadAsync), _ => Task.FromResult(ServiceResult.Ok("Vinculado")));
        serviceStub.On(nameof(IMatricesRiesgosAppService.VincularEvidenciaAlertaAsync), _ => Task.FromResult(ServiceResult.Ok("Vinculado")));
        serviceStub.On(nameof(IMatricesRiesgosAppService.VincularEvidenciaAutomonitoreoAsync), _ => Task.FromResult(ServiceResult.Ok("Vinculado")));
        serviceStub.On(nameof(IMatricesRiesgosAppService.VincularEvidenciaRevisionAsync), _ => Task.FromResult(ServiceResult.Ok("Vinculado")));
        serviceStub.On(nameof(IMatricesRiesgosAppService.VincularEvidenciaAprobacionAsync), _ => Task.FromResult(ServiceResult.Ok("Vinculado")));

        Assert.IsType<OkObjectResult>(await controller.VincularEvidenciaRiesgo(new AsociarEvidenciaRiesgoDto()));
        Assert.IsType<OkObjectResult>(await controller.VincularEvidenciaEvaluacion(new AsociarEvidenciaEvaluacionDto()));
        Assert.IsType<OkObjectResult>(await controller.VincularEvidenciaControl(new AsociarEvidenciaControlDto()));
        Assert.IsType<OkObjectResult>(await controller.VincularEvidenciaPlan(new AsociarEvidenciaPlanDto()));
        Assert.IsType<OkObjectResult>(await controller.VincularEvidenciaActividad(new AsociarEvidenciaActividadDto()));
        Assert.IsType<OkObjectResult>(await controller.VincularEvidenciaAlerta(new AsociarEvidenciaAlertaDto()));
        Assert.IsType<OkObjectResult>(await controller.VincularEvidenciaAutomonitoreo(new AsociarEvidenciaAutomonitoreoDto()));
        Assert.IsType<OkObjectResult>(await controller.VincularEvidenciaRevision(new AsociarEvidenciaRevisionDto()));
        Assert.IsType<OkObjectResult>(await controller.VincularEvidenciaAprobacion(new AsociarEvidenciaAprobacionDto()));
    }

    [Fact]
    public async Task ObtenerVersionVigenteFormulario_Exception_Retorna500()
    {
        var controller = CrearController(out var serviceStub, out _);
        serviceStub.On(nameof(IMatricesRiesgosAppService.ObtenerVersionVigenteFormularioAsync), _ => {
            throw new Exception("Excepción simulada");
        });

        var result = await controller.ObtenerVersionVigenteFormulario("FORM_A");

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task CrearBorradorFormulario_Exception_Retorna500()
    {
        var controller = CrearController(out var serviceStub, out _);
        serviceStub.On(nameof(IMatricesRiesgosAppService.CrearBorradorFormularioAsync), _ => {
            throw new Exception("Excepción simulada");
        });

        var result = await controller.CrearBorradorFormulario(1, "FORM_A", "{}");

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task ClonarVersionFormulario_Exception_Retorna500()
    {
        var controller = CrearController(out var serviceStub, out _);
        serviceStub.On(nameof(IMatricesRiesgosAppService.ClonarVersionFormularioAsync), _ => {
            throw new Exception("Excepción simulada");
        });

        var result = await controller.ClonarVersionFormulario(1);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task ActualizarBorradorFormulario_Exception_Retorna500()
    {
        var controller = CrearController(out var serviceStub, out _);
        serviceStub.On(nameof(IMatricesRiesgosAppService.ActualizarBorradorFormularioAsync), _ => {
            throw new Exception("Excepción simulada");
        });

        var result = await controller.ActualizarBorradorFormulario(1, "{}");

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task PublicarVersionFormulario_Exception_Retorna500()
    {
        var controller = CrearController(out var serviceStub, out _);
        serviceStub.On(nameof(IMatricesRiesgosAppService.PublicarVersionFormularioAsync), _ => {
            throw new Exception("Excepción simulada");
        });

        var result = await controller.PublicarVersionFormulario(1);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task CambiarEstadoVigenciaFormulario_Exception_Retorna500()
    {
        var controller = CrearController(out var serviceStub, out _);
        serviceStub.On(nameof(IMatricesRiesgosAppService.CambiarEstadoVigenciaFormularioAsync), _ => {
            throw new Exception("Excepción simulada");
        });

        var result = await controller.CambiarEstadoVigenciaFormulario(1, true);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task ListarHistorialVersionesFormulario_Exception_Retorna500()
    {
        var controller = CrearController(out var serviceStub, out _);
        serviceStub.On(nameof(IMatricesRiesgosAppService.ListarHistorialVersionesFormularioAsync), _ => {
            throw new Exception("Excepción simulada");
        });

        var result = await controller.ListarHistorialVersionesFormulario("FORM_A");

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task CrearEvaluacion_Exception_Retorna500()
    {
        var controller = CrearController(out var serviceStub, out _);
        serviceStub.On(nameof(IMatricesRiesgosAppService.CrearEvaluacionAsync), _ => {
            throw new Exception("Excepción simulada");
        });

        var result = await controller.CrearEvaluacion(new EvaluacionRiesgoDto());

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task ActualizarEvaluacion_Exception_Retorna500()
    {
        var controller = CrearController(out var serviceStub, out _);
        serviceStub.On(nameof(IMatricesRiesgosAppService.ActualizarEvaluacionAsync), _ => {
            throw new Exception("Excepción simulada");
        });

        var result = await controller.ActualizarEvaluacion(1, new EvaluacionRiesgoDto { EvaId = 1 });

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task TransicionarEstadoEvaluacion_Exception_Retorna500()
    {
        var controller = CrearController(out var serviceStub, out _);
        serviceStub.On(nameof(IMatricesRiesgosAppService.TransicionarEstadoEvaluacionAsync), _ => {
            throw new Exception("Excepción simulada");
        });

        var result = await controller.TransicionarEstadoEvaluacion(1, "EN_REVISION", "Aprobar");

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task CrearBorradorFormulario_Ok_RetornaLong()
    {
        var controller = CrearController(out var serviceStub, out _);
        serviceStub.On(nameof(IMatricesRiesgosAppService.CrearBorradorFormularioAsync), _ => Task.FromResult(ServiceResult<long>.Ok(123)));

        var result = await controller.CrearBorradorFormulario(1, "FORM_A", "{}");

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        var successProp = okResult.Value.GetType().GetProperty("success");
        Assert.True((bool)successProp!.GetValue(okResult.Value)!);
        var datosProp = okResult.Value.GetType().GetProperty("datos");
        Assert.Equal(123L, (long)datosProp!.GetValue(okResult.Value)!);
    }

    [Fact]
    public async Task ClonarVersionFormulario_Ok_RetornaLong()
    {
        var controller = CrearController(out var serviceStub, out _);
        serviceStub.On(nameof(IMatricesRiesgosAppService.ClonarVersionFormularioAsync), _ => Task.FromResult(ServiceResult<long>.Ok(456)));

        var result = await controller.ClonarVersionFormulario(1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        var successProp = okResult.Value.GetType().GetProperty("success");
        Assert.True((bool)successProp!.GetValue(okResult.Value)!);
        var datosProp = okResult.Value.GetType().GetProperty("datos");
        Assert.Equal(456L, (long)datosProp!.GetValue(okResult.Value)!);
    }

    [Fact]
    public async Task ActualizarBorradorFormulario_Ok_RetornaOk()
    {
        var controller = CrearController(out var serviceStub, out _);
        serviceStub.On(nameof(IMatricesRiesgosAppService.ActualizarBorradorFormularioAsync), _ => Task.FromResult(ServiceResult.Ok("Actualizado")));

        var result = await controller.ActualizarBorradorFormulario(1, "{}");

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        var successProp = okResult.Value.GetType().GetProperty("success");
        Assert.True((bool)successProp!.GetValue(okResult.Value)!);
    }

    [Fact]
    public async Task PublicarVersionFormulario_Ok_RetornaOk()
    {
        var controller = CrearController(out var serviceStub, out _);
        serviceStub.On(nameof(IMatricesRiesgosAppService.PublicarVersionFormularioAsync), _ => Task.FromResult(ServiceResult.Ok("Publicado")));

        var result = await controller.PublicarVersionFormulario(1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        var successProp = okResult.Value.GetType().GetProperty("success");
        Assert.True((bool)successProp!.GetValue(okResult.Value)!);
    }

    [Fact]
    public async Task CambiarEstadoVigenciaFormulario_Ok_RetornaOk()
    {
        var controller = CrearController(out var serviceStub, out _);
        serviceStub.On(nameof(IMatricesRiesgosAppService.CambiarEstadoVigenciaFormularioAsync), _ => Task.FromResult(ServiceResult.Ok("Vigencia cambiada")));

        var result = await controller.CambiarEstadoVigenciaFormulario(1, true);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        var successProp = okResult.Value.GetType().GetProperty("success");
        Assert.True((bool)successProp!.GetValue(okResult.Value)!);
    }

    [Fact]
    public async Task ListarHistorialVersionesFormulario_Ok_RetornaLista()
    {
        var controller = CrearController(out var serviceStub, out _);
        serviceStub.On(nameof(IMatricesRiesgosAppService.ListarHistorialVersionesFormularioAsync), _ => Task.FromResult(ServiceResult<List<VersionFormularioDto>>.Ok(new List<VersionFormularioDto>())));

        var result = await controller.ListarHistorialVersionesFormulario("FORM_A");

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        var successProp = okResult.Value.GetType().GetProperty("success");
        Assert.True((bool)successProp!.GetValue(okResult.Value)!);
    }

    [Fact]
    public async Task EliminarEvidencia_RetornaOk()
    {
        var controller = CrearController(out var serviceStub, out _);
        serviceStub.On(nameof(IMatricesRiesgosAppService.EliminarEvidenciaAsync), _ => Task.FromResult(ServiceResult.Ok("Eliminado")));

        var result = await controller.EliminarEvidencia(123L);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        var successProp = okResult.Value.GetType().GetProperty("success");
        Assert.True((bool)successProp!.GetValue(okResult.Value)!);
    }
    private static MatricesRiesgosController CrearController(out InterfaceStub serviceStub, out InterfaceStub loggerStub)
    {
        var service = InterfaceStub.Create<IMatricesRiesgosAppService>(out serviceStub);
        var logger = InterfaceStub.Create<ILogger<MatricesRiesgosController>>(out loggerStub);
        
        loggerStub.On("Log", args => null);

        var controller = new MatricesRiesgosController(service, logger);
        
        // Mocking ClaimsPrincipal
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "99"),
            new(ClaimTypes.Email, "test@ihss.hn"),
            new(ClaimTypes.Role, "ADMIN")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        
        var httpContext = new DefaultHttpContext { User = principal };
        httpContext.Request.Headers["X-Forwarded-For"] = "127.0.0.1";
        
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return controller;
    }
}
