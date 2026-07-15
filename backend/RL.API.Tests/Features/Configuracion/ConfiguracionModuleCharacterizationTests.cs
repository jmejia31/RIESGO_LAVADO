using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RL.API.Features.Auditoria.Contracts;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Features.Configuracion.Application;
using RL.API.Features.Configuracion.Contracts;
using RL.API.Features.Configuracion.Persistence;
using RL.API.Security;
using Xunit;

namespace RL.API.Tests.Features.Configuracion;

public sealed class ConfiguracionModuleCharacterizationTests
{
    [Fact]
    public void ConfiguracionController_ConservaRutaYProteccionDelModulo()
    {
        var controllerType = typeof(RL.API.Features.Configuracion.ConfiguracionController);
        var route = Assert.Single(controllerType.GetCustomAttributes(typeof(RouteAttribute), inherit: true).Cast<RouteAttribute>());
        var sistema = controllerType.GetMethod(nameof(RL.API.Features.Configuracion.ConfiguracionController.Sistema))!;
        var guardar = controllerType.GetMethod(nameof(RL.API.Features.Configuracion.ConfiguracionController.GuardarSistema))!;

        Assert.Equal("api/[controller]", route.Template);
        Assert.Equal("RL.API.Features.Configuracion", controllerType.Namespace);
        Assert.NotNull(sistema.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true).SingleOrDefault());
        Assert.Equal("ADMINISTRADOR", Assert.Single(guardar.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Cast<AuthorizeAttribute>()).Roles);
        Assert.NotNull(guardar.GetCustomAttributes(typeof(ModuloAuthorizeAttribute), inherit: true).SingleOrDefault());
    }

    [Fact]
    public async Task Sistema_ConservaContratoPublicoSinCamposInternos()
    {
        var controller = new RL.API.Features.Configuracion.ConfiguracionController(
            new ConfiguracionServiceFake(CrearConfiguracion()));

        var result = Assert.IsType<OkObjectResult>(await controller.Sistema());
        var json = JsonSerializer.Serialize(result.Value);

        Assert.Contains("\"success\":true", json, StringComparison.Ordinal);
        Assert.Contains("\"nombreSistema\":\"SGRLA-IHSS\"", json, StringComparison.Ordinal);
        Assert.Contains("\"timeoutSesion\":30", json, StringComparison.Ordinal);
        Assert.DoesNotContain("sfsId", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("validezClaveTemp", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ultimaActualizacion", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConfiguracionService_GuardarSistema_ConservaAuditoriaDeActualizacion()
    {
        var repository = new ConfiguracionRepositoryFake
        {
            Configuracion = CrearConfiguracion(),
            GuardarResultado = true
        };
        var auditoria = new AuditoriaRepositoryFake();
        var service = new ConfiguracionService(repository, auditoria);
        var nuevaConfiguracion = CrearConfiguracion();
        nuevaConfiguracion.NombreSistema = "SGRLA Actualizado";

        var guardado = await service.GuardarConfigSistemaAsync(nuevaConfiguracion, 27, "127.0.0.1");

        Assert.True(guardado);
        var registro = Assert.Single(auditoria.Registros);
        Assert.Equal("RL_CONFIG_SISTEMA", registro.Tabla);
        Assert.Equal("1", registro.RegistroId);
        Assert.Equal("UPDATE", registro.Accion);
        Assert.Equal(27, registro.UsuarioId);
        Assert.Equal("Configuracion", registro.Modulo);
    }

    [Fact]
    public async Task ConfiguracionService_NoAuditaCuandoPersistenciaFalla()
    {
        var repository = new ConfiguracionRepositoryFake
        {
            Configuracion = CrearConfiguracion(),
            GuardarResultado = false
        };
        var auditoria = new AuditoriaRepositoryFake();
        var service = new ConfiguracionService(repository, auditoria);

        var guardado = await service.GuardarConfigSistemaAsync(CrearConfiguracion(), 27, null);

        Assert.False(guardado);
        Assert.Empty(auditoria.Registros);
    }

    [Fact]
    public async Task ConfiguracionService_ActualizarSlide_UsaIdDeRutaYAuditaAnterior()
    {
        var repository = new ConfiguracionRepositoryFake
        {
            ActualizarResultado = true
        };
        repository.Slides.Add(new LoginSlide { Id = 15, ImagenUrl = "/anterior.png", Orden = 1, Activo = true });
        var auditoria = new AuditoriaRepositoryFake();
        var service = new ConfiguracionService(repository, auditoria);
        var actualizado = new LoginSlide { Id = 999, ImagenUrl = "/nuevo.png", Orden = 2, Activo = true };

        var resultado = await service.ActualizarSlideAsync(15, actualizado, 27, "127.0.0.1");

        Assert.True(resultado);
        Assert.Equal(15, actualizado.Id);
        Assert.Same(actualizado, repository.UltimoSlideActualizado);
        var registro = Assert.Single(auditoria.Registros);
        Assert.Equal("15", registro.RegistroId);
        Assert.Equal("UPDATE", registro.Accion);
        Assert.NotNull(registro.DatosAnteriores);
        Assert.NotNull(registro.DatosNuevos);
    }

    private static ConfigSistema CrearConfiguracion() => new()
    {
        SfsId = 1,
        NombreInstitucion = "IHSS",
        NombreSistema = "SGRLA-IHSS",
        LogoUrl = "/logo.png",
        ColorPrimario = "#1e3a8a",
        ColorSecundario = "#1d4ed8",
        TimeoutSesion = 30,
        MaxIntentos = 5,
        ValidezClaveTemp = 15,
        UltimaActualizacion = new DateTime(2026, 7, 15)
    };

    private sealed class ConfiguracionRepositoryFake : IConfiguracionRepository
    {
        public ConfigSistema? Configuracion { get; init; }
        public bool GuardarResultado { get; init; }
        public bool ActualizarResultado { get; init; }
        public List<LoginSlide> Slides { get; } = new();
        public LoginSlide? UltimoSlideActualizado { get; private set; }

        public Task<ConfigSistema?> ObtenerConfigSistemaAsync() => Task.FromResult(Configuracion);
        public Task<List<LoginSlide>> ObtenerSlidesAsync() => Task.FromResult(Slides);
        public Task<List<LoginSlide>> ObtenerTodosSlidesAsync() => Task.FromResult(Slides);
        public Task<bool> GuardarConfigSistemaAsync(ConfigSistema config) => Task.FromResult(GuardarResultado);
        public Task<bool> CrearSlideAsync(LoginSlide slide) => Task.FromResult(false);

        public Task<bool> ActualizarSlideAsync(LoginSlide slide)
        {
            UltimoSlideActualizado = slide;
            return Task.FromResult(ActualizarResultado);
        }

        public Task<bool> EliminarSlideAsync(int id) => Task.FromResult(false);
    }

    private sealed class ConfiguracionServiceFake : IConfiguracionService
    {
        private readonly ConfigSistema _configuracion;

        public ConfiguracionServiceFake(ConfigSistema configuracion)
        {
            _configuracion = configuracion;
        }

        public Task<ConfigSistema?> ObtenerConfigSistemaAsync() => Task.FromResult<ConfigSistema?>(_configuracion);
        public Task<List<LoginSlide>> ObtenerSlidesAsync() => Task.FromResult(new List<LoginSlide>());
        public Task<List<LoginSlide>> ObtenerTodosSlidesAsync() => Task.FromResult(new List<LoginSlide>());
        public Task<bool> GuardarConfigSistemaAsync(ConfigSistema config, long usuarioId, string? ip) => Task.FromResult(false);
        public Task<bool> CrearSlideAsync(LoginSlide slide, long usuarioId, string? ip) => Task.FromResult(false);
        public Task<bool> ActualizarSlideAsync(int id, LoginSlide slide, long usuarioId, string? ip) => Task.FromResult(false);
        public Task<bool> EliminarSlideAsync(int id, long usuarioId, string? ip) => Task.FromResult(false);
        public Task RegistrarCargaImagenAsync(string nombreOriginal, string nombreGuardado, string url, string tipoMime, long tamanioBytes, long usuarioId, string? ip) => Task.CompletedTask;
    }

    private sealed class AuditoriaRepositoryFake : IAuditoriaRepository
    {
        public List<AuditoriaRegistro> Registros { get; } = new();

        public Task RegistrarAsync(string tabla, string registroId, string accion, string? datosAnt, string? datosNvo, long? usrId, string? email, string? ip, string? modulo)
        {
            Registros.Add(new AuditoriaRegistro(tabla, registroId, accion, datosAnt, datosNvo, usrId, modulo));
            return Task.CompletedTask;
        }

        public Task<(List<AuditoriaDto> Datos, int Total)> ObtenerBitacoraPaginadaAsync(int pagina, int limite, string? buscar, string? accion, string? modulo, string? tabla, DateTime? fechaInicio, DateTime? fechaFin) =>
            Task.FromResult((new List<AuditoriaDto>(), 0));
    }

    private sealed record AuditoriaRegistro(string Tabla, string RegistroId, string Accion, string? DatosAnteriores, string? DatosNuevos, long? UsuarioId, string? Modulo);
}
