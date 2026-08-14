#pragma warning disable CA1416, CA1707, CA2201
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using RL.API.Features.MatricesRiesgos;
using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Shared.Results;
using RL.API.Tests.Support;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatricesRiesgosControllersContractTests
{
    private static void ConfigurarContextoHttp(ControllerBase controller, long usuarioId = 99, string? xForwardedFor = null, string? xRealIp = null, string? remoteIp = "192.168.1.50")
    {
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuarioId.ToString()),
            new(ClaimTypes.Email, "usuario.prueba@ihss.hn"),
            new(ClaimTypes.Role, "ADMINISTRADOR")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        if (!string.IsNullOrEmpty(xForwardedFor))
        {
            httpContext.Request.Headers["X-Forwarded-For"] = xForwardedFor;
        }

        if (!string.IsNullOrEmpty(xRealIp))
        {
            httpContext.Request.Headers["X-Real-IP"] = xRealIp;
        }

        if (!string.IsNullOrEmpty(remoteIp))
        {
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(remoteIp);
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    #region 1. MatricesRiesgosGestionController Tests

    [Fact]
    public async Task GestionController_Listar_RetornaOkConDatos()
    {
        IMatricesRiesgosGestionService service = InterfaceStub.Create<IMatricesRiesgosGestionService>(out InterfaceStub stub);
        var datos = new List<RiesgoDto> { new() { RieId = 1, RieCodigo = "R-01", RieNombre = "Riesgo 1" } };
        stub.On(nameof(IMatricesRiesgosGestionService.ListarRiesgosAsync), _ => Task.FromResult(ServiceResult<IReadOnlyList<RiesgoDto>>.Ok(datos)));

        var controller = new MatricesRiesgosGestionController(service);
        ConfigurarContextoHttp(controller);

        IActionResult result = await controller.Listar(true);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GestionController_Obtener_RetornaNotFound_CuandoNoExiste()
    {
        IMatricesRiesgosGestionService service = InterfaceStub.Create<IMatricesRiesgosGestionService>(out InterfaceStub stub);
        stub.On(nameof(IMatricesRiesgosGestionService.ObtenerRiesgoAsync), _ => Task.FromResult(ServiceResult<RiesgoDto>.NotFound("No encontrado")));

        var controller = new MatricesRiesgosGestionController(service);
        ConfigurarContextoHttp(controller);

        IActionResult result = await controller.Obtener(999);

        ObjectResult res = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, res.StatusCode);
    }

    [Fact]
    public async Task GestionController_Crear_ResuelveIpForwardedFor_YRetornaOk()
    {
        IMatricesRiesgosGestionService service = InterfaceStub.Create<IMatricesRiesgosGestionService>(out InterfaceStub stub);
        stub.On(nameof(IMatricesRiesgosGestionService.CrearRiesgoAsync), args =>
        {
            Assert.Equal("200.1.1.1", args[2]); // Valida que split de X-Forwarded-For tome la primera IP
            return Task.FromResult(ServiceResult<long>.Ok(101L, "Creado"));
        });

        var controller = new MatricesRiesgosGestionController(service);
        ConfigurarContextoHttp(controller, xForwardedFor: "200.1.1.1, 10.0.0.1");

        IActionResult result = await controller.Crear(new RiesgoGuardarDto { RieCodigo = "R-101", RieNombre = "Nuevo" });

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    [Fact]
    public async Task GestionController_Crear_ResuelveIpRealIp_YRetornaOk()
    {
        IMatricesRiesgosGestionService service = InterfaceStub.Create<IMatricesRiesgosGestionService>(out InterfaceStub stub);
        stub.On(nameof(IMatricesRiesgosGestionService.CrearRiesgoAsync), args =>
        {
            Assert.Equal("190.4.5.6", args[2]);
            return Task.FromResult(ServiceResult<long>.Ok(102L, "Creado"));
        });

        var controller = new MatricesRiesgosGestionController(service);
        ConfigurarContextoHttp(controller, xRealIp: "190.4.5.6");

        IActionResult result = await controller.Crear(new RiesgoGuardarDto { RieCodigo = "R-102", RieNombre = "Nuevo 2" });

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    [Fact]
    public async Task GestionController_Actualizar_RetornaBadRequest_CuandoServicioFalla()
    {
        IMatricesRiesgosGestionService service = InterfaceStub.Create<IMatricesRiesgosGestionService>(out InterfaceStub stub);
        stub.On(nameof(IMatricesRiesgosGestionService.ActualizarRiesgoAsync), _ => Task.FromResult(ServiceResult.BadRequest("Datos invalidos")));

        var controller = new MatricesRiesgosGestionController(service);
        ConfigurarContextoHttp(controller);

        IActionResult result = await controller.Actualizar(1, new RiesgoGuardarDto { RieCodigo = "", RieNombre = "" });

        ObjectResult res = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, res.StatusCode);
    }

    [Fact]
    public async Task GestionController_Actualizar_RetornaOk_CuandoExitoso()
    {
        IMatricesRiesgosGestionService service = InterfaceStub.Create<IMatricesRiesgosGestionService>(out InterfaceStub stub);
        stub.On(nameof(IMatricesRiesgosGestionService.ActualizarRiesgoAsync), _ => Task.FromResult(ServiceResult.Ok("Actualizado")));

        var controller = new MatricesRiesgosGestionController(service);
        ConfigurarContextoHttp(controller);

        IActionResult result = await controller.Actualizar(1, new RiesgoGuardarDto { RieCodigo = "R-1", RieNombre = "Valido" });

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    #endregion

    #region 2. MatricesRiesgosMitigacionController Tests

    [Fact]
    public async Task MitigacionController_Controles_ListarYCrearYActualizar()
    {
        IMatricesRiesgosMitigacionService service = InterfaceStub.Create<IMatricesRiesgosMitigacionService>(out InterfaceStub stub);
        stub.On(nameof(IMatricesRiesgosMitigacionService.ListarControlesAsync), _ => Task.FromResult(ServiceResult<IReadOnlyList<ControlRiesgoDto>>.Ok(new List<ControlRiesgoDto>())));
        stub.On(nameof(IMatricesRiesgosMitigacionService.CrearControlAsync), _ => Task.FromResult(ServiceResult<long>.Ok(50L, "Creado")));
        stub.On(nameof(IMatricesRiesgosMitigacionService.ActualizarControlAsync), _ => Task.FromResult(ServiceResult.Ok("Actualizado")));

        var controller = new MatricesRiesgosMitigacionController(service);
        ConfigurarContextoHttp(controller, xForwardedFor: "181.1.2.3, 10.0.0.1");

        IActionResult listResult = await controller.ListarControles(10);
        IActionResult createResult = await controller.CrearControl(new ControlRiesgoGuardarDto { ConTipo = "PREVENTIVO", ConDescripcion = "Control A" });
        IActionResult updateResult = await controller.ActualizarControl(50, new ControlRiesgoGuardarDto { ConTipo = "PREVENTIVO", ConDescripcion = "Control A Mod" });

        Assert.IsType<OkObjectResult>(listResult);
        Assert.IsType<OkObjectResult>(createResult);
        Assert.IsType<OkObjectResult>(updateResult);
    }

    [Fact]
    public async Task MitigacionController_EvaluacionesControl_ListarYEvaluar()
    {
        IMatricesRiesgosMitigacionService service = InterfaceStub.Create<IMatricesRiesgosMitigacionService>(out InterfaceStub stub);
        stub.On(nameof(IMatricesRiesgosMitigacionService.ListarEvaluacionesControlAsync), _ => Task.FromResult(ServiceResult<IReadOnlyList<EvaluacionControlDto>>.Ok(new List<EvaluacionControlDto>())));
        stub.On(nameof(IMatricesRiesgosMitigacionService.RegistrarEvaluacionControlAsync), _ => Task.FromResult(ServiceResult<long>.Ok(12L, "Registrado")));

        var controller = new MatricesRiesgosMitigacionController(service);
        ConfigurarContextoHttp(controller, xRealIp: "172.16.0.4");

        IActionResult listResult = await controller.ListarEvaluacionesControl(5);
        IActionResult evalResult = await controller.EvaluarControl(5, new EvaluacionControlGuardarDto { EcoEfectividad = 85.5m, EcoComentario = "Efectivo" });

        Assert.IsType<OkObjectResult>(listResult);
        Assert.IsType<OkObjectResult>(evalResult);
    }

    [Fact]
    public async Task MitigacionController_PlanesYActividades_ListarYCrearYActualizar()
    {
        IMatricesRiesgosMitigacionService service = InterfaceStub.Create<IMatricesRiesgosMitigacionService>(out InterfaceStub stub);
        stub.On(nameof(IMatricesRiesgosMitigacionService.ListarPlanesAsync), _ => Task.FromResult(ServiceResult<IReadOnlyList<PlanMitigacionDto>>.Ok(new List<PlanMitigacionDto>())));
        stub.On(nameof(IMatricesRiesgosMitigacionService.CrearPlanAsync), _ => Task.FromResult(ServiceResult<long>.Ok(21L, "Plan creado")));
        stub.On(nameof(IMatricesRiesgosMitigacionService.ActualizarPlanAsync), _ => Task.FromResult(ServiceResult.Ok("Plan actualizado")));

        stub.On(nameof(IMatricesRiesgosMitigacionService.ListarActividadesAsync), _ => Task.FromResult(ServiceResult<IReadOnlyList<ActividadPlanDto>>.Ok(new List<ActividadPlanDto>())));
        stub.On(nameof(IMatricesRiesgosMitigacionService.CrearActividadAsync), _ => Task.FromResult(ServiceResult<long>.Ok(31L, "Actividad creada")));
        stub.On(nameof(IMatricesRiesgosMitigacionService.ActualizarActividadAsync), _ => Task.FromResult(ServiceResult.Ok("Actividad actualizada")));

        var controller = new MatricesRiesgosMitigacionController(service);
        ConfigurarContextoHttp(controller);

        IActionResult listPlanes = await controller.ListarPlanes(1);
        IActionResult createPlan = await controller.CrearPlan(new PlanMitigacionGuardarDto { PlaDescripcion = "Plan 1" });
        IActionResult updatePlan = await controller.ActualizarPlan(21, new PlanMitigacionGuardarDto { PlaDescripcion = "Plan 1 Mod" });

        IActionResult listActs = await controller.ListarActividades(21);
        IActionResult createAct = await controller.CrearActividad(new ActividadPlanGuardarDto { ActDescripcion = "Act 1" });
        IActionResult updateAct = await controller.ActualizarActividad(31, new ActividadPlanGuardarDto { ActDescripcion = "Act 1 Mod" });

        Assert.IsType<OkObjectResult>(listPlanes);
        Assert.IsType<OkObjectResult>(createPlan);
        Assert.IsType<OkObjectResult>(updatePlan);
        Assert.IsType<OkObjectResult>(listActs);
        Assert.IsType<OkObjectResult>(createAct);
        Assert.IsType<OkObjectResult>(updateAct);
    }

    [Fact]
    public async Task MitigacionController_PropagaCodigosDeError()
    {
        IMatricesRiesgosMitigacionService service = InterfaceStub.Create<IMatricesRiesgosMitigacionService>(out InterfaceStub stub);
        stub.On(nameof(IMatricesRiesgosMitigacionService.ListarControlesAsync), _ => Task.FromResult(ServiceResult<IReadOnlyList<ControlRiesgoDto>>.BadRequest("Evaluacion requerida")));
        stub.On(nameof(IMatricesRiesgosMitigacionService.ActualizarPlanAsync), _ => Task.FromResult(ServiceResult.NotFound("Plan no existe")));

        var controller = new MatricesRiesgosMitigacionController(service);
        ConfigurarContextoHttp(controller);

        IActionResult errList = await controller.ListarControles(0);
        IActionResult errPlan = await controller.ActualizarPlan(99, new PlanMitigacionGuardarDto());

        ObjectResult resList = Assert.IsType<ObjectResult>(errList);
        ObjectResult resPlan = Assert.IsType<ObjectResult>(errPlan);

        Assert.Equal(400, resList.StatusCode);
        Assert.Equal(404, resPlan.StatusCode);
    }

    #endregion

    #region 3. MatricesRiesgosMonitoreoController Tests

    [Fact]
    public async Task MonitoreoController_Alertas_ListarCrearYCambiarEstado()
    {
        IMatricesRiesgosMonitoreoService service = InterfaceStub.Create<IMatricesRiesgosMonitoreoService>(out InterfaceStub stub);
        stub.On(nameof(IMatricesRiesgosMonitoreoService.ListarAlertasAsync), _ => Task.FromResult(ServiceResult<IReadOnlyList<SenalAlertaDto>>.Ok(new List<SenalAlertaDto>())));
        stub.On(nameof(IMatricesRiesgosMonitoreoService.CrearAlertaAsync), _ => Task.FromResult(ServiceResult<long>.Ok(80L, "Alerta creada")));
        stub.On(nameof(IMatricesRiesgosMonitoreoService.CambiarEstadoAlertaAsync), _ => Task.FromResult(ServiceResult.Ok("Estado cambiado")));

        var controller = new MatricesRiesgosMonitoreoController(service);
        ConfigurarContextoHttp(controller, xForwardedFor: "190.1.2.3, 127.0.0.1");

        IActionResult listResult = await controller.ListarAlertas(2);
        IActionResult createResult = await controller.CrearAlerta(new SenalAlertaGuardarDto { AleCodigo = "ALT-01", AleIndicador = "Ind 1" });
        IActionResult stateResult = await controller.CambiarEstadoAlerta(80, new SenalAlertaEstadoDto { AleEstado = "ACTIVO" });

        Assert.IsType<OkObjectResult>(listResult);
        Assert.IsType<OkObjectResult>(createResult);
        Assert.IsType<OkObjectResult>(stateResult);
    }

    [Fact]
    public async Task MonitoreoController_Automonitoreo_ListarYRegistrar()
    {
        IMatricesRiesgosMonitoreoService service = InterfaceStub.Create<IMatricesRiesgosMonitoreoService>(out InterfaceStub stub);
        stub.On(nameof(IMatricesRiesgosMonitoreoService.ListarAutomonitoreoAsync), _ => Task.FromResult(ServiceResult<IReadOnlyList<AutomonitoreoDto>>.Ok(new List<AutomonitoreoDto>())));
        stub.On(nameof(IMatricesRiesgosMonitoreoService.RegistrarAutomonitoreoAsync), _ => Task.FromResult(ServiceResult<long>.Ok(90L, "Automonitoreo registrado")));

        var controller = new MatricesRiesgosMonitoreoController(service);
        ConfigurarContextoHttp(controller, xRealIp: "10.10.10.10");

        IActionResult listResult = await controller.ListarAutomonitoreo(2);
        IActionResult regResult = await controller.RegistrarAutomonitoreo(new AutomonitoreoGuardarDto { MonEvaluacionId = 2, MonEstadoRiesgo = "CONTROLADO" });

        Assert.IsType<OkObjectResult>(listResult);
        Assert.IsType<OkObjectResult>(regResult);
    }

    [Fact]
    public async Task MonitoreoController_ObtenerResumen_RetornaResumenOk()
    {
        IMatricesRiesgosMonitoreoService service = InterfaceStub.Create<IMatricesRiesgosMonitoreoService>(out InterfaceStub stub);
        var resumen = new ResumenMatricesOperativoDto { RiesgosActivos = 15, AlertasActivas = 3, PlanesAbiertos = 2 };
        stub.On(nameof(IMatricesRiesgosMonitoreoService.ObtenerResumenOperativoAsync), _ => Task.FromResult(ServiceResult<ResumenMatricesOperativoDto>.Ok(resumen)));

        var controller = new MatricesRiesgosMonitoreoController(service);
        ConfigurarContextoHttp(controller);

        IActionResult result = await controller.ObtenerResumen();

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    [Fact]
    public async Task MonitoreoController_PropagaCodigosDeError()
    {
        IMatricesRiesgosMonitoreoService service = InterfaceStub.Create<IMatricesRiesgosMonitoreoService>(out InterfaceStub stub);
        stub.On(nameof(IMatricesRiesgosMonitoreoService.ListarAlertasAsync), _ => Task.FromResult(ServiceResult<IReadOnlyList<SenalAlertaDto>>.BadRequest("Error")));
        stub.On(nameof(IMatricesRiesgosMonitoreoService.CambiarEstadoAlertaAsync), _ => Task.FromResult(ServiceResult.NotFound("No existe")));

        var controller = new MatricesRiesgosMonitoreoController(service);
        ConfigurarContextoHttp(controller);

        IActionResult errList = await controller.ListarAlertas(0);
        IActionResult errChange = await controller.CambiarEstadoAlerta(99, new SenalAlertaEstadoDto());

        ObjectResult resList = Assert.IsType<ObjectResult>(errList);
        ObjectResult resChange = Assert.IsType<ObjectResult>(errChange);

        Assert.Equal(400, resList.StatusCode);
        Assert.Equal(404, resChange.StatusCode);
    }

    #endregion

    #region 4. MatricesRiesgosReportesController Tests

    [Fact]
    public async Task ReportesController_DescargarExcel_RetornaFileResult_CuandoEsExitoso()
    {
        IMatricesRiesgosAppService matricesService = InterfaceStub.Create<IMatricesRiesgosAppService>(out InterfaceStub matricesStub);
        IMatricesRiesgosReportExportService exportService = InterfaceStub.Create<IMatricesRiesgosReportExportService>(out InterfaceStub exportStub);

        IReadOnlyList<RiesgoReporteFilaDto> filas = new List<RiesgoReporteFilaDto> { new() { RiesgoId = 1, CodigoRiesgo = "R-01" } };
        matricesStub.On(nameof(IMatricesRiesgosAppService.ObtenerConsolidadoTipadoAsync), _ => Task.FromResult(ServiceResult<IReadOnlyList<RiesgoReporteFilaDto>>.Ok(filas)));

        var archivoEsperado = new ArchivoReporteDto(new byte[] { 0x50, 0x4B, 0x03, 0x04 }, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Reporte.xlsx");
        exportStub.On(nameof(IMatricesRiesgosReportExportService.CrearExcelConsolidado), _ => archivoEsperado);

        var controller = new MatricesRiesgosReportesController(matricesService, exportService);
        ConfigurarContextoHttp(controller);

        IActionResult result = await controller.DescargarExcel();

        FileContentResult fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileResult.ContentType);
        Assert.Equal("Reporte.xlsx", fileResult.FileDownloadName);
        Assert.Equal(archivoEsperado.Contenido, fileResult.FileContents);
    }

    [Fact]
    public async Task ReportesController_DescargarExcel_RetornaErrorStatusCode_CuandoFallaServicio()
    {
        IMatricesRiesgosAppService matricesService = InterfaceStub.Create<IMatricesRiesgosAppService>(out InterfaceStub matricesStub);
        IMatricesRiesgosReportExportService exportService = InterfaceStub.Create<IMatricesRiesgosReportExportService>(out _);

        matricesStub.On(nameof(IMatricesRiesgosAppService.ObtenerConsolidadoTipadoAsync), _ => Task.FromResult(ServiceResult<IReadOnlyList<RiesgoReporteFilaDto>>.NotFound("Consolidado no disponible")));

        var controller = new MatricesRiesgosReportesController(matricesService, exportService);
        ConfigurarContextoHttp(controller);

        IActionResult result = await controller.DescargarExcel();

        ObjectResult objResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, objResult.StatusCode);
    }

    [Fact]
    public async Task ReportesController_DescargarPdf_RetornaFileResult_CuandoEsExitoso()
    {
        IMatricesRiesgosAppService matricesService = InterfaceStub.Create<IMatricesRiesgosAppService>(out InterfaceStub matricesStub);
        IMatricesRiesgosReportExportService exportService = InterfaceStub.Create<IMatricesRiesgosReportExportService>(out InterfaceStub exportStub);

        IReadOnlyList<RiesgoReporteFilaDto> filas = new List<RiesgoReporteFilaDto> { new() { RiesgoId = 1, CodigoRiesgo = "R-01" } };
        matricesStub.On(nameof(IMatricesRiesgosAppService.ObtenerConsolidadoTipadoAsync), _ => Task.FromResult(ServiceResult<IReadOnlyList<RiesgoReporteFilaDto>>.Ok(filas)));

        var archivoEsperado = new ArchivoReporteDto(Encoding.ASCII.GetBytes("%PDF-1.4..."), "application/pdf", "Reporte.pdf");
        exportStub.On(nameof(IMatricesRiesgosReportExportService.CrearPdfConsolidado), _ => archivoEsperado);

        var controller = new MatricesRiesgosReportesController(matricesService, exportService);
        ConfigurarContextoHttp(controller);

        IActionResult result = await controller.DescargarPdf();

        FileContentResult fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", fileResult.ContentType);
        Assert.Equal("Reporte.pdf", fileResult.FileDownloadName);
    }

    [Fact]
    public async Task ReportesController_DescargarPdf_RetornaErrorStatusCode_CuandoFallaServicio()
    {
        IMatricesRiesgosAppService matricesService = InterfaceStub.Create<IMatricesRiesgosAppService>(out InterfaceStub matricesStub);
        IMatricesRiesgosReportExportService exportService = InterfaceStub.Create<IMatricesRiesgosReportExportService>(out _);

        matricesStub.On(nameof(IMatricesRiesgosAppService.ObtenerConsolidadoTipadoAsync), _ => Task.FromResult(new ServiceResult<IReadOnlyList<RiesgoReporteFilaDto>>(false, default, "Error interno", 500)));

        var controller = new MatricesRiesgosReportesController(matricesService, exportService);
        ConfigurarContextoHttp(controller);

        IActionResult result = await controller.DescargarPdf();

        ObjectResult objResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objResult.StatusCode);
    }

    #endregion

    #region 5. MatricesRiesgosController Additional Branch & Exception Tests

    private static MatricesRiesgosController CrearMatricesController(out InterfaceStub serviceStub)
    {
        IMatricesRiesgosAppService service = InterfaceStub.Create<IMatricesRiesgosAppService>(out serviceStub);
        ILogger<MatricesRiesgosController> logger = InterfaceStub.Create<ILogger<MatricesRiesgosController>>(out InterfaceStub loggerStub);
        loggerStub.On("Log", _ => null);

        var controller = new MatricesRiesgosController(service, logger);
        ConfigurarContextoHttp(controller, xForwardedFor: "192.168.10.1, 10.0.0.1");
        return controller;
    }

    [Fact]
    public async Task MatricesController_CrearBorrador_Retorna500_CuandoServicioLanzaExcepcion()
    {
        var controller = CrearMatricesController(out InterfaceStub stub);
        stub.On(nameof(IMatricesRiesgosAppService.CrearBorradorFormularioAsync), _ => throw new InvalidOperationException("Fallo BD"));

        IActionResult result = await controller.CrearBorradorFormulario(1, "COD", JToken.Parse("{}"));

        ObjectResult res = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, res.StatusCode);
    }

    [Fact]
    public async Task MatricesController_ClonarVersion_RetornaOkY500()
    {
        var controller = CrearMatricesController(out InterfaceStub stub);
        stub.On(nameof(IMatricesRiesgosAppService.ClonarVersionFormularioAsync), _ => Task.FromResult(ServiceResult<long>.Ok(202L, "Clonada")));

        IActionResult okResult = await controller.ClonarVersionFormulario(1);
        Assert.IsType<OkObjectResult>(okResult);

        stub.On(nameof(IMatricesRiesgosAppService.ClonarVersionFormularioAsync), _ => throw new InvalidOperationException("Error"));
        IActionResult errResult = await controller.ClonarVersionFormulario(1);
        ObjectResult res = Assert.IsType<ObjectResult>(errResult);
        Assert.Equal(500, res.StatusCode);
    }

    [Fact]
    public async Task MatricesController_PublicarVersion_RetornaOkY500()
    {
        var controller = CrearMatricesController(out InterfaceStub stub);
        stub.On(nameof(IMatricesRiesgosAppService.PublicarVersionFormularioAsync), _ => Task.FromResult(ServiceResult.Ok("Publicada")));

        IActionResult okResult = await controller.PublicarVersionFormulario(5);
        Assert.IsType<OkObjectResult>(okResult);

        stub.On(nameof(IMatricesRiesgosAppService.PublicarVersionFormularioAsync), _ => throw new InvalidOperationException("Error"));
        IActionResult errResult = await controller.PublicarVersionFormulario(5);
        ObjectResult res = Assert.IsType<ObjectResult>(errResult);
        Assert.Equal(500, res.StatusCode);
    }

    [Fact]
    public async Task MatricesController_CambiarVigencia_RetornaOkY500()
    {
        var controller = CrearMatricesController(out InterfaceStub stub);
        stub.On(nameof(IMatricesRiesgosAppService.CambiarEstadoVigenciaFormularioAsync), _ => Task.FromResult(ServiceResult.Ok("Estado cambiado")));

        IActionResult okResult = await controller.CambiarEstadoVigenciaFormulario(5, true);
        Assert.IsType<OkObjectResult>(okResult);

        stub.On(nameof(IMatricesRiesgosAppService.CambiarEstadoVigenciaFormularioAsync), _ => throw new InvalidOperationException("Error"));
        IActionResult errResult = await controller.CambiarEstadoVigenciaFormulario(5, true);
        ObjectResult res = Assert.IsType<ObjectResult>(errResult);
        Assert.Equal(500, res.StatusCode);
    }

    [Fact]
    public async Task MatricesController_EliminarVersion_RetornaOkY500()
    {
        var controller = CrearMatricesController(out InterfaceStub stub);
        stub.On(nameof(IMatricesRiesgosAppService.EliminarVersionFormularioAsync), _ => Task.FromResult(ServiceResult.Ok("Eliminada")));

        IActionResult okResult = await controller.EliminarVersionFormulario(5);
        Assert.IsType<OkObjectResult>(okResult);

        stub.On(nameof(IMatricesRiesgosAppService.EliminarVersionFormularioAsync), _ => throw new InvalidOperationException("Error"));
        IActionResult errResult = await controller.EliminarVersionFormulario(5);
        ObjectResult res = Assert.IsType<ObjectResult>(errResult);
        Assert.Equal(500, res.StatusCode);
    }

    [Fact]
    public async Task MatricesController_HistorialVersiones_RetornaOkY500()
    {
        var controller = CrearMatricesController(out InterfaceStub stub);
        stub.On(nameof(IMatricesRiesgosAppService.ListarHistorialVersionesFormularioAsync), _ => Task.FromResult(ServiceResult<List<VersionFormularioDto>>.Ok(new List<VersionFormularioDto>())));

        IActionResult okResult = await controller.ListarHistorialVersionesFormulario("FAM_01");
        Assert.IsType<OkObjectResult>(okResult);

        stub.On(nameof(IMatricesRiesgosAppService.ListarHistorialVersionesFormularioAsync), _ => throw new InvalidOperationException("Error"));
        IActionResult errResult = await controller.ListarHistorialVersionesFormulario("FAM_01");
        ObjectResult res = Assert.IsType<ObjectResult>(errResult);
        Assert.Equal(500, res.StatusCode);
    }

    [Fact]
    public async Task MatricesController_FamiliasCRUD_CubreRamasOkY500()
    {
        var controller = CrearMatricesController(out InterfaceStub stub);

        stub.On(nameof(IMatricesRiesgosAppService.ListarFamiliasFormularioAsync), _ => Task.FromResult(ServiceResult<List<FamiliaFormularioDto>>.Ok(new List<FamiliaFormularioDto>())));
        stub.On(nameof(IMatricesRiesgosAppService.ObtenerFamiliaFormularioPorIdAsync), _ => Task.FromResult(ServiceResult<FamiliaFormularioDto>.Ok(new FamiliaFormularioDto { FamId = 1 })));
        stub.On(nameof(IMatricesRiesgosAppService.CrearFamiliaFormularioAsync), _ => Task.FromResult(ServiceResult<long>.Ok(10L, "Creada")));
        stub.On(nameof(IMatricesRiesgosAppService.ActualizarFamiliaFormularioAsync), _ => Task.FromResult(ServiceResult.Ok("Actualizada")));
        stub.On(nameof(IMatricesRiesgosAppService.DesactivarFamiliaFormularioAsync), _ => Task.FromResult(ServiceResult.Ok("Desactivada")));

        Assert.IsType<OkObjectResult>(await controller.ListarFamiliasFormulario());
        Assert.IsType<OkObjectResult>(await controller.ObtenerFamiliaFormularioPorId(1));
        Assert.IsType<OkObjectResult>(await controller.CrearFamiliaFormulario(new CrearFamiliaFormularioDto { FamCodigo = "F1", FamNombre = "Fam 1" }));
        Assert.IsType<OkObjectResult>(await controller.ActualizarFamiliaFormulario(1, new ActualizarFamiliaFormularioDto { FamNombre = "Fam 1 Mod" }));
        Assert.IsType<OkObjectResult>(await controller.DesactivarFamiliaFormulario(1));

        // Test Exceptions
        stub.On(nameof(IMatricesRiesgosAppService.ListarFamiliasFormularioAsync), _ => throw new InvalidOperationException("Error"));
        stub.On(nameof(IMatricesRiesgosAppService.ObtenerFamiliaFormularioPorIdAsync), _ => throw new InvalidOperationException("Error"));
        stub.On(nameof(IMatricesRiesgosAppService.CrearFamiliaFormularioAsync), _ => throw new InvalidOperationException("Error"));
        stub.On(nameof(IMatricesRiesgosAppService.ActualizarFamiliaFormularioAsync), _ => throw new InvalidOperationException("Error"));
        stub.On(nameof(IMatricesRiesgosAppService.DesactivarFamiliaFormularioAsync), _ => throw new InvalidOperationException("Error"));

        Assert.Equal(500, Assert.IsType<ObjectResult>(await controller.ListarFamiliasFormulario()).StatusCode);
        Assert.Equal(500, Assert.IsType<ObjectResult>(await controller.ObtenerFamiliaFormularioPorId(1)).StatusCode);
        Assert.Equal(500, Assert.IsType<ObjectResult>(await controller.CrearFamiliaFormulario(new CrearFamiliaFormularioDto())).StatusCode);
        Assert.Equal(500, Assert.IsType<ObjectResult>(await controller.ActualizarFamiliaFormulario(1, new ActualizarFamiliaFormularioDto())).StatusCode);
        Assert.Equal(500, Assert.IsType<ObjectResult>(await controller.DesactivarFamiliaFormulario(1)).StatusCode);
    }

    [Fact]
    public async Task MatricesController_EvaluacionesYTransiciones_CubreRamasOkY500()
    {
        var controller = CrearMatricesController(out InterfaceStub stub);

        stub.On(nameof(IMatricesRiesgosAppService.ObtenerEvaluacionAsync), _ => Task.FromResult(ServiceResult<EvaluacionRiesgoDto>.Ok(new EvaluacionRiesgoDto { EvaId = 5 })));
        stub.On(nameof(IMatricesRiesgosAppService.ActualizarEvaluacionAsync), _ => Task.FromResult(ServiceResult.Ok("Actualizada")));
        stub.On(nameof(IMatricesRiesgosAppService.TransicionarEstadoEvaluacionAsync), _ => Task.FromResult(ServiceResult.Ok("Transición completada")));
        stub.On(nameof(IMatricesRiesgosAppService.ObtenerFlujosEvaluacionAsync), _ => Task.FromResult(ServiceResult<List<FlujoEvaluacionDto>>.Ok(new List<FlujoEvaluacionDto>())));

        Assert.IsType<OkObjectResult>(await controller.ObtenerEvaluacion(5));
        Assert.IsType<OkObjectResult>(await controller.ActualizarEvaluacion(5, new EvaluacionRiesgoDto { EvaId = 5 }));
        Assert.IsType<OkObjectResult>(await controller.TransicionarEstadoEvaluacion(5, "REVISADA", "Aprobado"));
        Assert.IsType<OkObjectResult>(await controller.ObtenerFlujosEvaluacion(5));

        // Exceptions
        stub.On(nameof(IMatricesRiesgosAppService.ObtenerEvaluacionAsync), _ => throw new InvalidOperationException("Error"));
        stub.On(nameof(IMatricesRiesgosAppService.ListarEvaluacionesPaginadasAsync), _ => throw new InvalidOperationException("Error"));
        stub.On(nameof(IMatricesRiesgosAppService.CrearEvaluacionAsync), _ => throw new InvalidOperationException("Error"));
        stub.On(nameof(IMatricesRiesgosAppService.ActualizarEvaluacionAsync), _ => throw new InvalidOperationException("Error"));
        stub.On(nameof(IMatricesRiesgosAppService.TransicionarEstadoEvaluacionAsync), _ => throw new InvalidOperationException("Error"));

        Assert.Equal(500, Assert.IsType<ObjectResult>(await controller.ObtenerEvaluacion(5)).StatusCode);
        Assert.Equal(500, Assert.IsType<ObjectResult>(await controller.ListarEvaluacionesPaginadas(new ConsultaEvaluacionPaginadaDto())).StatusCode);
        Assert.Equal(500, Assert.IsType<ObjectResult>(await controller.CrearEvaluacion(new EvaluacionRiesgoDto())).StatusCode);
        Assert.Equal(500, Assert.IsType<ObjectResult>(await controller.ActualizarEvaluacion(5, new EvaluacionRiesgoDto { EvaId = 5 })).StatusCode);
        Assert.Equal(500, Assert.IsType<ObjectResult>(await controller.TransicionarEstadoEvaluacion(5, "REVISADA", "Aprobado")).StatusCode);
    }

    [Fact]
    public async Task MatricesController_Evidencias_CubreRamasOkY500()
    {
        var controller = CrearMatricesController(out InterfaceStub stub);

        stub.On(nameof(IMatricesRiesgosAppService.VincularEvidenciaAsync), _ => Task.FromResult(ServiceResult.Ok("Vinculada")));
        stub.On(nameof(IMatricesRiesgosAppService.CargarArchivoEvidenciaFisicaAsync), _ => Task.FromResult(ServiceResult<EvidenciaDto>.Ok(new EvidenciaDto { EviId = 1 })));
        stub.On(nameof(IMatricesRiesgosAppService.EliminarEvidenciaAsync), _ => Task.FromResult(ServiceResult.Ok("Eliminada")));

        var fileMock = new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("test")), 0, 4, "archivo", "test.pdf");

        Assert.IsType<OkObjectResult>(await controller.VincularEvidencia(new VincularEvidenciaDto { EvidenciaId = 1, EntidadId = 1, TipoEntidad = TipoEntidadEvidencia.Evaluacion }));
        Assert.IsType<OkObjectResult>(await controller.CargarEvidencia(fileMock));
        Assert.IsType<OkObjectResult>(await controller.EliminarEvidencia(1));

        // Exceptions
        stub.On(nameof(IMatricesRiesgosAppService.VincularEvidenciaAsync), _ => throw new InvalidOperationException("Error"));
        stub.On(nameof(IMatricesRiesgosAppService.CargarArchivoEvidenciaFisicaAsync), _ => throw new InvalidOperationException("Error"));
        stub.On(nameof(IMatricesRiesgosAppService.EliminarEvidenciaAsync), _ => throw new InvalidOperationException("Error"));

        Assert.Equal(500, Assert.IsType<ObjectResult>(await controller.VincularEvidencia(new VincularEvidenciaDto())).StatusCode);
        Assert.Equal(500, Assert.IsType<ObjectResult>(await controller.CargarEvidencia(fileMock)).StatusCode);
        Assert.Equal(500, Assert.IsType<ObjectResult>(await controller.EliminarEvidencia(1)).StatusCode);
    }

    [Fact]
    public async Task MatricesController_ConsolidadoYMetodologia_CubreRamasException()
    {
        var controller = CrearMatricesController(out InterfaceStub stub);

        stub.On(nameof(IMatricesRiesgosAppService.ObtenerConsolidadoTipadoAsync), _ => throw new InvalidOperationException("Error"));
        stub.On(nameof(IMatricesRiesgosAppService.ObtenerMetodologiaDinamicaVigenteAsync), _ => throw new InvalidOperationException("Error"));
        stub.On(nameof(IMatricesRiesgosAppService.ObtenerVersionVigenteFormularioAsync), _ => throw new InvalidOperationException("Error"));

        Assert.Equal(500, Assert.IsType<ObjectResult>(await controller.ObtenerConsolidado()).StatusCode);
        Assert.Equal(500, Assert.IsType<ObjectResult>(await controller.ObtenerMetodologiaVigente()).StatusCode);
        Assert.Equal(500, Assert.IsType<ObjectResult>(await controller.ObtenerVersionVigenteFormulario("MATRIZ")).StatusCode);
    }

    #endregion

    #region 6. DTOs & Contracts Property Integrity Tests

    [Fact]
    public void ContratosYDtos_PreservanIntegridadDeCampos()
    {
        var dtoClonar = new ClonarVersionDto { VersionOrigenId = 10, UsrId = 99 };
        Assert.Equal(10L, dtoClonar.VersionOrigenId);
        Assert.Equal(99L, dtoClonar.UsrId);

        var dtoFamilia = new FamiliaFormularioDto
        {
            FamId = 5,
            FamCodigo = "COD",
            FamNombre = "Nombre",
            FamDescripcion = "Desc",
            FamActivo = true,
            FamFechaCreacion = DateTime.UtcNow,
            TotalVersiones = 3,
            TieneVersionVigente = true
        };
        Assert.Equal(5L, dtoFamilia.FamId);
        Assert.True(dtoFamilia.TieneVersionVigente);

        var dtoEvidencia = new EvidenciaRegistroDto
        {
            EviNombreArchivo = "doc.pdf",
            EviExtension = ".pdf",
            EviTamano = 1024,
            EviHash = "hash123",
            EviRuta = "/uploads/doc.pdf",
            EviUsrCreacion = 1
        };
        Assert.Equal("doc.pdf", dtoEvidencia.EviNombreArchivo);
        Assert.Equal(1024L, dtoEvidencia.EviTamano);

        var dtoDescarga = new EvidenciaDescargaDto
        {
            NombreArchivo = "descarga.pdf",
            ContentType = "application/pdf",
            Contenido = new byte[] { 1, 2, 3 }
        };
        Assert.Equal("descarga.pdf", dtoDescarga.NombreArchivo);
        Assert.Equal(3, dtoDescarga.Contenido.Length);

        var dtoUpload = new EvidenciaUploadFormDto { UsrId = 7 };
        Assert.Equal(7L, dtoUpload.UsrId);
        Assert.Null(dtoUpload.Archivo);

        var dtoSenal = new SenalAlertaDto
        {
            AleId = 1,
            AleEvaluacionId = 2,
            AleCodigo = "ALT",
            AleIndicador = "IND",
            AleEstado = "ACTIVO",
            AleFechaDisparo = DateTime.UtcNow
        };
        Assert.Equal("ACTIVO", dtoSenal.AleEstado);
        Assert.NotNull(dtoSenal.AleFechaDisparo);

        var dtoMonitoreo = new AutomonitoreoDto
        {
            MonId = 1,
            MonEvaluacionId = 2,
            MonEstadoRiesgo = "BAJO",
            MonEstadoContr = "ADECUADO",
            MonResultado = "OK",
            MonUsrId = 1,
            MonFecha = DateTime.UtcNow
        };
        Assert.Equal("BAJO", dtoMonitoreo.MonEstadoRiesgo);

        var dtoResumen = new ResumenMatricesOperativoDto
        {
            FechaGeneracion = DateTime.UtcNow,
            RiesgosActivos = 10,
            EvaluacionesActivas = 8,
            EvaluacionesAprobadas = 5,
            RiesgosAltoCritico = 2,
            AlertasActivas = 1,
            PlanesAbiertos = 4,
            ActividadesVencidas = 0,
            AutomonitoreosUltimos30Dias = 6
        };
        Assert.Equal(10, dtoResumen.RiesgosActivos);
        Assert.Equal(6, dtoResumen.AutomonitoreosUltimos30Dias);

        var dtoControl = new ControlRiesgoDto
        {
            ConId = 1,
            ConEvaluacionId = 2,
            ConTipo = "PREVENTIVO",
            ConDescripcion = "Desc",
            ConAutomatizacion = "AUTOMATICO",
            ConEstado = "ACTIVO"
        };
        Assert.Equal("AUTOMATICO", dtoControl.ConAutomatizacion);

        var dtoEvalControl = new EvaluacionControlDto
        {
            EcoId = 1,
            EcoControlId = 2,
            EcoEfectividad = 90.5m,
            EcoComentario = "Comentario"
        };
        Assert.Equal(90.5m, dtoEvalControl.EcoEfectividad);

        var dtoPlan = new PlanMitigacionDto
        {
            PlaId = 1,
            PlaEvaluacionId = 2,
            PlaDescripcion = "Plan",
            PlaAvance = 50m,
            PlaPresupuesto = 1000m,
            PlaFechaInicio = DateTime.UtcNow,
            PlaFechaFin = DateTime.UtcNow.AddDays(10),
            PlaEstado = "EN_PROCESO"
        };
        Assert.Equal(50m, dtoPlan.PlaAvance);

        var dtoActividad = new ActividadPlanDto
        {
            ActId = 1,
            ActPlanId = 2,
            ActDescripcion = "Act",
            ActResponsable = "Resp",
            ActAvance = 100m,
            ActFechaInicio = DateTime.UtcNow,
            ActFechaFin = DateTime.UtcNow.AddDays(5),
            ActEstado = "COMPLETADA"
        };
        Assert.Equal("COMPLETADA", dtoActividad.ActEstado);
    }

    #endregion
}
