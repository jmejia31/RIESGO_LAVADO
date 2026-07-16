using System.Security.Claims;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RL.API.Features.Auditoria;
using RL.API.Features.Auditoria.Application;
using RL.API.Features.Auditoria.Contracts;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Core.Security;
using Xunit;

namespace RL.API.Tests.Features.Auditoria;

public sealed class AuditoriaModuleCharacterizationTests
{
    [Fact]
    public void AuditoriaController_ConservaRutaYModulosAutorizados()
    {
        var controllerType = typeof(AuditoriaController);
        var route = Assert.Single(controllerType.GetCustomAttributes(typeof(RouteAttribute), inherit: true).Cast<RouteAttribute>());
        var obtener = controllerType.GetMethod(nameof(AuditoriaController.ObtenerBitacora))!;
        var exportar = controllerType.GetMethod(nameof(AuditoriaController.RegistrarExportacion))!;

        Assert.Equal("api/[controller]", route.Template);
        Assert.NotNull(controllerType.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).SingleOrDefault());
        Assert.Equal(new[] { 5 }, ObtenerModulos(Assert.Single(obtener.GetCustomAttributes(typeof(ModuloAuthorizeAttribute), inherit: true).Cast<ModuloAuthorizeAttribute>())));
        Assert.Equal(new[] { 4, 5, 7, 8, 9 }, ObtenerModulos(Assert.Single(exportar.GetCustomAttributes(typeof(ModuloAuthorizeAttribute), inherit: true).Cast<ModuloAuthorizeAttribute>())));
    }

    [Fact]
    public async Task ObtenerBitacora_ConservaFiltrosYPaginacion()
    {
        var repository = new AuditoriaRepositoryFake
        {
            Datos = new List<AuditoriaDto> { new() { AudId = 91, Tabla = "RL_AUDITORIA", RegistroId = "91", Accion = "VER" } },
            Total = 27
        };
        var controller = CrearController(repository);
        var inicio = new DateTime(2026, 7, 1);
        var fin = new DateTime(2026, 7, 15);

        var result = Assert.IsType<OkObjectResult>(await controller.ObtenerBitacora(3, 25, "ana", "VER", "Bitacora", "RL_AUDITORIA", inicio, fin));
        var response = Assert.IsType<AuditoriaPaginadoDto>(result.Value);

        Assert.Equal(27, response.TotalRegistros);
        Assert.Single(response.Datos);
        Assert.Equal((3, 25, "ana", "VER", "Bitacora", "RL_AUDITORIA", inicio, fin), repository.UltimaConsulta);
    }

    [Fact]
    public async Task RegistrarExportacion_ConservaAccionExplicitaYContexto()
    {
        var repository = new AuditoriaRepositoryFake();
        var controller = CrearController(repository, usuarioId: 42, ip: "127.0.0.7");
        var dto = new RegistrarExportacionAuditoriaDto
        {
            Tabla = "RL_AUDITORIA",
            RegistroId = "reporte-7",
            Modulo = "Bitacora",
            Detalle = new Dictionary<string, object?>
            {
                ["accion"] = "EXPORTACION_CSV",
                ["archivo"] = "bitacora.csv"
            }
        };

        var result = Assert.IsType<OkObjectResult>(await controller.RegistrarExportacion(dto));

        Assert.NotNull(result.Value);
        var registro = Assert.Single(repository.Registros);
        Assert.Equal("RL_AUDITORIA", registro.Tabla);
        Assert.Equal("reporte-7", registro.RegistroId);
        Assert.Equal("VER", registro.Accion);
        Assert.Contains("EXPORTACION_CSV", registro.DatosNuevos, StringComparison.Ordinal);
        Assert.Equal(42, registro.UsuarioId);
        Assert.Equal("127.0.0.7", registro.Ip);
        Assert.Equal("Bitacora", registro.Modulo);
    }

    [Fact]
    public async Task RegistrarExportacion_RechazaContratoInvalidoSinAuditar()
    {
        var repository = new AuditoriaRepositoryFake();
        var controller = CrearController(repository);

        var result = await controller.RegistrarExportacion(new RegistrarExportacionAuditoriaDto { Tabla = " " });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(repository.Registros);
    }

    [Theory]
    [InlineData("tipoReporte", "Resumen PDF", "GENERACION_REPORTE_PDF")]
    [InlineData("archivo", "bitacora.pdf", "EXPORTACION_PDF")]
    [InlineData("archivo", "bitacora.xlsx", "EXPORTACION_EXCEL")]
    public async Task AuditoriaService_ConservaClasificacionDeExportaciones(string clave, string valor, string accionEsperada)
    {
        var repository = new AuditoriaRepositoryFake();
        var service = new AuditoriaService(repository);
        var dto = new RegistrarExportacionAuditoriaDto
        {
            Tabla = "RL_AUDITORIA",
            RegistroId = "reporte",
            Modulo = "Bitacora",
            Detalle = new Dictionary<string, object?> { [clave] = valor }
        };

        await service.RegistrarExportacionAsync(dto, 7, "127.0.0.1");

        Assert.Contains(accionEsperada, Assert.Single(repository.Registros).DatosNuevos, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ObtenerBitacora_ConservaErrorSeguroConTraceId()
    {
        var controller = CrearController(new AuditoriaRepositoryFake { LanzarEnConsulta = true });
        controller.HttpContext.TraceIdentifier = "trace-auditoria-10";

        var result = Assert.IsType<ObjectResult>(await controller.ObtenerBitacora());
        var json = JsonSerializer.Serialize(result.Value);

        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
        Assert.Contains("trace-auditoria-10", json, StringComparison.Ordinal);
        Assert.DoesNotContain("fallo interno de prueba", json, StringComparison.OrdinalIgnoreCase);
    }

    private static AuditoriaController CrearController(IAuditoriaRepository repository, long usuarioId = 7, string ip = "127.0.0.1")
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()) }, "Test");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(ip);

        return new AuditoriaController(new AuditoriaService(repository))
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    }

    private static int[] ObtenerModulos(ModuloAuthorizeAttribute attribute)
    {
        var field = typeof(ModuloAuthorizeAttribute).GetField("_moduloIds", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return Assert.IsType<HashSet<int>>(field.GetValue(attribute)).OrderBy(id => id).ToArray();
    }

    private sealed class AuditoriaRepositoryFake : IAuditoriaRepository
    {
        public List<AuditoriaDto> Datos { get; init; } = new();
        public int Total { get; init; }
        public bool LanzarEnConsulta { get; init; }
        public (int Pagina, int Limite, string? Buscar, string? Accion, string? Modulo, string? Tabla, DateTime? Inicio, DateTime? Fin)? UltimaConsulta { get; private set; }
        public List<AuditoriaRegistro> Registros { get; } = new();

        public Task RegistrarAsync(string tabla, string registroId, string accion, string? datosAnt, string? datosNvo, long? usrId, string? email, string? ip, string? modulo)
        {
            Registros.Add(new AuditoriaRegistro(tabla, registroId, accion, datosNvo, usrId, ip, modulo));
            return Task.CompletedTask;
        }

        public Task<(List<AuditoriaDto> Datos, int Total)> ObtenerBitacoraPaginadaAsync(int pagina, int limite, string? buscar, string? accion, string? modulo, string? tabla, DateTime? fechaInicio, DateTime? fechaFin)
        {
            if (LanzarEnConsulta)
                return Task.FromException<(List<AuditoriaDto> Datos, int Total)>(new InvalidOperationException("fallo interno de prueba"));

            UltimaConsulta = (pagina, limite, buscar, accion, modulo, tabla, fechaInicio, fechaFin);
            return Task.FromResult((Datos, Total));
        }
    }

    private sealed record AuditoriaRegistro(string Tabla, string RegistroId, string Accion, string? DatosNuevos, long? UsuarioId, string? Ip, string? Modulo);
}
