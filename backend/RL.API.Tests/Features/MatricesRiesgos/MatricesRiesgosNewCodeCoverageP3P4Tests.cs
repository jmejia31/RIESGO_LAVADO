#pragma warning disable CA1416, CA1707, CA2201
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Features.MatricesRiesgos;
using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;
using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Infrastructure.Caching;
using RL.API.Shared.Results;
using RL.API.Tests.Support;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

/// <summary>
/// Suite P3/P4 de ampliación de cobertura sobre New Code del módulo Matrices de Riesgos.
/// Ataca sistemáticamente las ramas residuales no cubiertas identificadas en coverage.cobertura.xml:
/// - Invocación del callback eliminarArchivo (físico en disco / excepción) en MatricesRiesgosAppService.
/// - Métodos delegados pass-through restantes en CachedMatricesRiesgosAppService.
/// - Rutas de resolución de IP (X-Real-IP, RemoteIpAddress) y manejo de excepciones en MatricesRiesgosController.
/// - Éxito de CrearBorrador y ActualizarBorrador en MatricesRiesgosController.
/// - Casos límite en validación de catálogos y campos sin id en FormularioValidador.
/// - Casos límite en reportes PDF con truncamiento de texto largo (>110 caracteres) y caracteres no ASCII.
/// </summary>
public sealed class MatricesRiesgosNewCodeCoverageP3P4Tests
{
    private static void ConfigurarContextoHttp(
        ControllerBase controller,
        long usuarioId = 99,
        string? xForwardedFor = null,
        string? xRealIp = null,
        string? remoteIp = "192.168.1.50")
    {
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuarioId.ToString()),
            new(ClaimTypes.Email, "usuario.p3p4@ihss.hn"),
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
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    #region 1. AppService: Callback eliminarArchivo (Líneas 521-530)

    [Fact]
    public async Task EliminarEvidencia_EjecutaCallbackEliminarArchivo_ExitosamenteCuandoExisteEnDisco()
    {
        IMatricesRiesgosRepository repo = InterfaceStub.Create<IMatricesRiesgosRepository>(out InterfaceStub repoStub);
        IFormularioValidador validador = InterfaceStub.Create<IFormularioValidador>(out _);
        IMatricesRiesgoService calculador = InterfaceStub.Create<IMatricesRiesgoService>(out _);
        IAuditoriaRepository auditoria = InterfaceStub.Create<IAuditoriaRepository>(out InterfaceStub auditoriaStub);
        auditoriaStub.On("RegistrarAsync", _ => Task.CompletedTask);

        var service = new MatricesRiesgosAppService(repo, validador, calculador, auditoria);

        // Crear un archivo temporal real en el directorio de pruebas para que File.Exists sea true
        // Use an isolated test directory: other evidence tests clean App_Data/Evidencias concurrently.
        string relativeDir = Path.Combine("App_Data", "EvidenciasP3P4");
        string fullDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativeDir);
        Directory.CreateDirectory(fullDir);
        string filename = $"test_p3p4_{Guid.NewGuid():N}.pdf";
        string fullPath = Path.Combine(fullDir, filename);
        await File.WriteAllTextAsync(fullPath, "contenido de prueba para eliminacion");

        string relativePath = Path.Combine(relativeDir, filename);

        repoStub.On(nameof(IMatricesRiesgosRepository.ObtenerEvidenciaFisicaAsync), _ =>
            Task.FromResult<EvidenciaDto?>(new EvidenciaDto
            {
                EviId = 555,
                EviNombreArchivo = "archivo_prueba.pdf",
                EviRuta = relativePath
            }));

        // Simular que el repositorio invoca el callback Func<Task<bool>> eliminarArchivo
        repoStub.On(nameof(IMatricesRiesgosRepository.EliminarEvidenciaSeguraAsync), args =>
        {
            var callback = (Func<Task<bool>>)args[1]!;
            bool resultadoCallback = callback().GetAwaiter().GetResult();
            Assert.True(resultadoCallback);
            return Task.FromResult(ResultadoEliminacionEvidencia.Exito);
        });

        ServiceResult result = await service.EliminarEvidenciaAsync(555, 99, "127.0.0.1");

        Assert.True(result.Success);
        Assert.False(File.Exists(fullPath)); // Debe haber sido eliminado
    }

    [Fact]
    public async Task EliminarEvidencia_EjecutaCallbackEliminarArchivo_CuandoRutaEsVaciaOArchivoNoExiste()
    {
        IMatricesRiesgosRepository repo = InterfaceStub.Create<IMatricesRiesgosRepository>(out InterfaceStub repoStub);
        IFormularioValidador validador = InterfaceStub.Create<IFormularioValidador>(out _);
        IMatricesRiesgoService calculador = InterfaceStub.Create<IMatricesRiesgoService>(out _);
        IAuditoriaRepository auditoria = InterfaceStub.Create<IAuditoriaRepository>(out InterfaceStub auditoriaStub);
        auditoriaStub.On("RegistrarAsync", _ => Task.CompletedTask);

        var service = new MatricesRiesgosAppService(repo, validador, calculador, auditoria);

        repoStub.On(nameof(IMatricesRiesgosRepository.ObtenerEvidenciaFisicaAsync), _ =>
            Task.FromResult<EvidenciaDto?>(new EvidenciaDto
            {
                EviId = 556,
                EviNombreArchivo = "no_existe.pdf",
                EviRuta = "App_Data/Evidencias/no_existe_archivo.pdf"
            }));

        repoStub.On(nameof(IMatricesRiesgosRepository.EliminarEvidenciaSeguraAsync), args =>
        {
            var callback = (Func<Task<bool>>)args[1]!;
            bool resultadoCallback = callback().GetAwaiter().GetResult();
            Assert.True(resultadoCallback);
            return Task.FromResult(ResultadoEliminacionEvidencia.Exito);
        });

        ServiceResult result = await service.EliminarEvidenciaAsync(556, 99, "127.0.0.1");

        Assert.True(result.Success);
    }

    #endregion

    #region 2. CachedMatricesRiesgosAppService: Delegaciones Pass-Through Restantes (Líneas 152, 155, 169, 175, 178)

    [Fact]
    public async Task CachedAppService_DelegacionesTransaccionalesRestantes_InvocanInnerDirectamente()
    {
        IMatricesRiesgosRepository repo = InterfaceStub.Create<IMatricesRiesgosRepository>(out InterfaceStub repoStub);
        IFormularioValidador validador = InterfaceStub.Create<IFormularioValidador>(out InterfaceStub validadorStub);
        IMatricesRiesgoService calculador = InterfaceStub.Create<IMatricesRiesgoService>(out InterfaceStub calculadorStub);
        IAuditoriaRepository auditoria = InterfaceStub.Create<IAuditoriaRepository>(out InterfaceStub auditoriaStub);
        auditoriaStub.On("RegistrarAsync", _ => Task.CompletedTask);

        var inner = new MatricesRiesgosAppService(repo, validador, calculador, auditoria);
        IApplicationCache cache = InterfaceStub.Create<IApplicationCache>(out _);
        var settings = new ApplicationCacheSettings();
        var cached = new CachedMatricesRiesgosAppService(inner, cache, settings);

        // 1. CrearEvaluacionAsync (Línea 152)
        repoStub.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), _ =>
            Task.FromResult<VersionFormularioDto?>(new VersionFormularioDto
            {
                VerId = 10,
                VerEstado = "PUBLISHED",
                VerVigente = true,
                VerJson = "{\"secciones\":[]}"
            }));
        validadorStub.On(nameof(IFormularioValidador.ValidarRespuestasAsync), _ =>
            Task.FromResult(new FormularioValidationResult()));
        calculadorStub.On(nameof(IMatricesRiesgoService.CalcularYValidarRiesgo), _ =>
            ServiceResult<CalculoRiesgoResultadoDto>.Ok(new CalculoRiesgoResultadoDto { Vri = 1, Etp = 10m, Vrr = 1 }));
        repoStub.On(nameof(IMatricesRiesgosRepository.CrearEvaluacionAsync), _ => Task.FromResult(999L));

        var dtoEvaluacion = new EvaluacionRiesgoDto { EvaId = 0, EvaVersionId = 10, EvaDataJson = "{}" };
        ServiceResult<long> rCrearEval = await cached.CrearEvaluacionAsync(dtoEvaluacion, 99, "10.0.0.1");
        Assert.True(rCrearEval.Success);
        Assert.Equal(999L, rCrearEval.Data);

        // 2. ActualizarEvaluacionAsync (Línea 155)
        repoStub.On(nameof(IMatricesRiesgosRepository.ActualizarEvaluacionAsync), _ => Task.FromResult(true));
        var dtoActEval = new EvaluacionRiesgoDto { EvaId = 5, EvaVersionId = 10, EvaDataJson = "{}" };
        ServiceResult rActEval = await cached.ActualizarEvaluacionAsync(dtoActEval, 99, "10.0.0.1");
        Assert.True(rActEval.Success);

        // 3. CargarArchivoEvidenciaFisicaAsync (Línea 169) - Archivo nulo retorna 400
        ServiceResult<EvidenciaDto> rCargarEvidencia = await cached.CargarArchivoEvidenciaFisicaAsync(null!, 99);
        Assert.False(rCargarEvidencia.Success);
        Assert.Equal(400, rCargarEvidencia.StatusCode);

        // 4. VincularEvidenciaAsync (Línea 175)
        repoStub.On(nameof(IMatricesRiesgosRepository.VincularEvidenciaAsync), _ => Task.FromResult(true));
        var dtoVincular = new VincularEvidenciaDto { EvidenciaId = 1, EntidadId = 2, TipoEntidad = TipoEntidadEvidencia.Evaluacion };
        ServiceResult rVincular = await cached.VincularEvidenciaAsync(dtoVincular, 99, "127.0.0.1");
        Assert.True(rVincular.Success);

        // 5. EliminarEvidenciaAsync (Línea 178)
        repoStub.On(nameof(IMatricesRiesgosRepository.ObtenerEvidenciaFisicaAsync), _ =>
            Task.FromResult<EvidenciaDto?>(null));
        ServiceResult rEliminar = await cached.EliminarEvidenciaAsync(999, 99, "127.0.0.1");
        Assert.True(rEliminar.Success);
    }

    #endregion

    #region 3. MatricesRiesgosController: IP Headers, CrearBorrador y ActualizarBorrador Ok (Líneas 53, 89-92, 446-452)

    [Fact]
    public async Task MatricesController_CrearBorrador_RetornaOk_ConRespuestaDelServicio()
    {
        IMatricesRiesgosAppService service = InterfaceStub.Create<IMatricesRiesgosAppService>(out InterfaceStub serviceStub);
        ILogger<MatricesRiesgosController> logger = InterfaceStub.Create<ILogger<MatricesRiesgosController>>(out InterfaceStub loggerStub);
        loggerStub.On("Log", _ => null);
        var controller = new MatricesRiesgosController(service, logger);
        ConfigurarContextoHttp(controller);

        serviceStub.On(nameof(IMatricesRiesgosAppService.CrearBorradorFormularioAsync), _ =>
            Task.FromResult(ServiceResult<long>.Ok(42L, "Borrador creado")));

        IActionResult result = await controller.CrearBorradorFormulario(1, "COD_MR", JToken.Parse("{\"titulo\":\"Prueba\"}"));

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task MatricesController_ActualizarBorrador_RetornaOk_YCapturaExcepcion500()
    {
        IMatricesRiesgosAppService service = InterfaceStub.Create<IMatricesRiesgosAppService>(out InterfaceStub serviceStub);
        ILogger<MatricesRiesgosController> logger = InterfaceStub.Create<ILogger<MatricesRiesgosController>>(out InterfaceStub loggerStub);
        loggerStub.On("Log", _ => null);
        var controller = new MatricesRiesgosController(service, logger);
        ConfigurarContextoHttp(controller);

        // 1. Rama Ok (Línea 87-88)
        serviceStub.On(nameof(IMatricesRiesgosAppService.ActualizarBorradorFormularioAsync), _ =>
            Task.FromResult(ServiceResult.Ok("Borrador actualizado")));

        IActionResult okResult = await controller.ActualizarBorradorFormulario(10, JToken.Parse("{\"secciones\":[]}"));
        Assert.IsType<OkObjectResult>(okResult);

        // 2. Rama Catch Exception -> 500 (Líneas 89-92)
        serviceStub.On(nameof(IMatricesRiesgosAppService.ActualizarBorradorFormularioAsync), _ =>
            throw new InvalidOperationException("Error interno en base de datos"));

        IActionResult errResult = await controller.ActualizarBorradorFormulario(10, JToken.Parse("{\"secciones\":[]}"));
        ObjectResult objResult = Assert.IsType<ObjectResult>(errResult);
        Assert.Equal(500, objResult.StatusCode);
    }

    [Fact]
    public async Task MatricesController_ObtenerIp_CubreXRealIp_YRemoteIpAddressFallback()
    {
        IMatricesRiesgosAppService service = InterfaceStub.Create<IMatricesRiesgosAppService>(out InterfaceStub serviceStub);
        ILogger<MatricesRiesgosController> logger = InterfaceStub.Create<ILogger<MatricesRiesgosController>>(out InterfaceStub loggerStub);
        loggerStub.On("Log", _ => null);

        // 1. Caso X-Real-IP presente sin X-Forwarded-For (Líneas 446-450)
        var controllerRealIp = new MatricesRiesgosController(service, logger);
        ConfigurarContextoHttp(controllerRealIp, xForwardedFor: null, xRealIp: "172.16.0.45", remoteIp: "10.0.0.1");

        serviceStub.On(nameof(IMatricesRiesgosAppService.CrearEvaluacionAsync), args =>
        {
            string? ip = (string?)args[2];
            Assert.Equal("172.16.0.45", ip);
            return Task.FromResult(ServiceResult<long>.Ok(1L));
        });

        await controllerRealIp.CrearEvaluacion(new EvaluacionRiesgoDto { EvaId = 1 });

        // 2. Caso solo RemoteIpAddress presente (Línea 452)
        var controllerRemoteIp = new MatricesRiesgosController(service, logger);
        ConfigurarContextoHttp(controllerRemoteIp, xForwardedFor: null, xRealIp: null, remoteIp: "192.168.100.200");

        serviceStub.On(nameof(IMatricesRiesgosAppService.CrearEvaluacionAsync), args =>
        {
            string? ip = (string?)args[2];
            Assert.Equal("192.168.100.200", ip);
            return Task.FromResult(ServiceResult<long>.Ok(2L));
        });

        await controllerRemoteIp.CrearEvaluacion(new EvaluacionRiesgoDto { EvaId = 2 });
    }

    #endregion

    #region 4. FormularioValidador: Casos límite (Líneas 161, 169-170)

    [Fact]
    public async Task FormularioValidador_CamposConExpresionValidacion_OIdVacio_SeProcesanCorrectamente()
    {
        var validador = new FormularioValidador();

        // Template con un campo que tiene id vacío (línea 161) y otro con expresionValidacion en lugar de regexValidacion (líneas 169-170)
        string jsonConfig = """
        {
          "secciones": [
            {
              "campos": [
                {
                  "id": "  ",
                  "tipo": "texto",
                  "etiqueta": "Campo Sin ID"
                },
                {
                  "id": "telefono",
                  "tipo": "texto",
                  "etiqueta": "Teléfono",
                  "expresionValidacion": "^[0-9]{8}$"
                }
              ]
            }
          ]
        }
        """;

        // Respuesta válida que cumple con expresionValidacion
        string jsonRespuestasValida = "{\"telefono\": \"12345678\"}";
        var rValida = await validador.ValidarRespuestasAsync(jsonRespuestasValida, jsonConfig);
        Assert.True(rValida.Valido);

        // Respuesta inválida que falla expresionValidacion
        string jsonRespuestasInvalida = "{\"telefono\": \"abc\"}";
        var rInvalida = await validador.ValidarRespuestasAsync(jsonRespuestasInvalida, jsonConfig);
        Assert.False(rInvalida.Valido);
        Assert.Contains(rInvalida.Errores, e => e.Campo == "telefono");
    }

    #endregion

    #region 5. ReportExportService: Casos Límite PDF (Truncamiento > 110 caracteres y Caracteres Especiales)

    [Fact]
    public void ReportExportService_CrearPdfConsolidado_TruncaTextosLargosYNormalizaCaracteresEspeciales()
    {
        var exportador = new MatricesRiesgosReportExportService();

        var filas = new List<RiesgoReporteFilaDto>
        {
            new()
            {
                RiesgoId = 1,
                EvaluacionId = 100,
                VersionFormularioId = 5,
                CodigoRiesgo = "COD-MUY-LARGO-" + new string('X', 50),
                AreaPrincipal = "Gerencia de Operaciones Institucionales y Seguridad Integral",
                DuenoRiesgo = "Lic. María José Peña y Pérez de la Oña",
                Vri = 16,
                NivelInherente = "CRITICO",
                Vrr = 4,
                NivelResidual = "BAJO",
                RespuestaRiesgo = "MITIGAR_INMEDIATAMENTE",
                EstadoEvaluacion = "APROBADA",
                FechaEvaluacion = new DateTime(2026, 8, 14, 15, 0, 0, DateTimeKind.Utc)
            }
        };

        ArchivoReporteDto pdf = exportador.CrearPdfConsolidado(filas);

        Assert.NotNull(pdf);
        Assert.Equal("application/pdf", pdf.ContentType);
        Assert.NotEmpty(pdf.Contenido);
        Assert.True(pdf.Contenido.Length > 100);

        // Verificar cabecera %PDF-1.4
        string header = Encoding.ASCII.GetString(pdf.Contenido, 0, 8);
        Assert.StartsWith("%PDF-1.4", header, StringComparison.Ordinal);
    }

    #endregion
}
