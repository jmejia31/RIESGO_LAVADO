using Microsoft.Extensions.Caching.Memory;
using RL.API.Features.Auditoria.Contracts;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Features.Configuracion.Application;
using RL.API.Features.Configuracion.Contracts;
using RL.API.Features.Configuracion.Persistence;
using RL.API.Infrastructure.Caching;
using Xunit;

namespace RL.API.Tests.Features.Configuracion;

public sealed class CachedConfiguracionServiceTests
{
    [Fact]
    public async Task Configuracion_SeCacheaYGuardarExitosoInvalida()
    {
        var repository = new FakeConfiguracionRepository
        {
            Config = new ConfigSistema { NombreInstitucion = "IHSS-A" }
        };
        using var memory = new MemoryCache(new MemoryCacheOptions());
        using var cache = new ApplicationMemoryCache(memory);
        var inner = new ConfiguracionService(repository, new NoOpAuditoriaRepository());
        var service = new CachedConfiguracionService(inner, cache, new ApplicationCacheSettings());

        ConfigSistema? first = await service.ObtenerConfigSistemaAsync();
        ConfigSistema? second = await service.ObtenerConfigSistemaAsync();

        Assert.Equal("IHSS-A", first?.NombreInstitucion);
        Assert.Equal("IHSS-A", second?.NombreInstitucion);
        Assert.Equal(1, repository.ConfigReads);

        bool saved = await service.GuardarConfigSistemaAsync(
            new ConfigSistema { NombreInstitucion = "IHSS-B" },
            10,
            "127.0.0.1");
        ConfigSistema? afterMutation = await service.ObtenerConfigSistemaAsync();

        Assert.True(saved);
        Assert.Equal("IHSS-B", afterMutation?.NombreInstitucion);
        Assert.Equal(3, repository.ConfigReads);
    }

    [Fact]
    public async Task Slides_SeCacheanYCrearExitosoInvalidaActivosYTodos()
    {
        var repository = new FakeConfiguracionRepository();
        repository.Slides.Add(new LoginSlide { Id = 1, Activo = true, Titulo = "Uno" });
        using var memory = new MemoryCache(new MemoryCacheOptions());
        using var cache = new ApplicationMemoryCache(memory);
        var inner = new ConfiguracionService(repository, new NoOpAuditoriaRepository());
        var service = new CachedConfiguracionService(inner, cache, new ApplicationCacheSettings());

        Assert.Single(await service.ObtenerSlidesAsync());
        Assert.Single(await service.ObtenerSlidesAsync());
        Assert.Equal(1, repository.ActiveSlideReads);

        bool created = await service.CrearSlideAsync(
            new LoginSlide { Id = 2, Activo = true, Titulo = "Dos" },
            10,
            null);
        List<LoginSlide> afterMutation = await service.ObtenerSlidesAsync();

        Assert.True(created);
        Assert.Equal(2, afterMutation.Count);
        Assert.Equal(2, repository.ActiveSlideReads);
    }

    [Fact]
    public async Task MutacionFallida_NoInvalidaCacheVigente()
    {
        var repository = new FakeConfiguracionRepository
        {
            Config = new ConfigSistema { NombreInstitucion = "IHSS-A" },
            SaveConfigResult = false
        };
        using var memory = new MemoryCache(new MemoryCacheOptions());
        using var cache = new ApplicationMemoryCache(memory);
        var inner = new ConfiguracionService(repository, new NoOpAuditoriaRepository());
        var service = new CachedConfiguracionService(inner, cache, new ApplicationCacheSettings());

        await service.ObtenerConfigSistemaAsync();
        bool saved = await service.GuardarConfigSistemaAsync(
            new ConfigSistema { NombreInstitucion = "NO-GUARDAR" },
            10,
            null);
        ConfigSistema? cached = await service.ObtenerConfigSistemaAsync();

        Assert.False(saved);
        Assert.Equal("IHSS-A", cached?.NombreInstitucion);
        Assert.Equal(2, repository.ConfigReads);
    }

    private sealed class FakeConfiguracionRepository : IConfiguracionRepository
    {
        public ConfigSistema? Config { get; set; }
        public List<LoginSlide> Slides { get; } = [];
        public bool SaveConfigResult { get; set; } = true;
        public int ConfigReads { get; private set; }
        public int ActiveSlideReads { get; private set; }

        public Task<ConfigSistema?> ObtenerConfigSistemaAsync()
        {
            ConfigReads++;
            return Task.FromResult(Config);
        }

        public Task<List<LoginSlide>> ObtenerSlidesAsync()
        {
            ActiveSlideReads++;
            return Task.FromResult(Slides.Where(item => item.Activo).ToList());
        }

        public Task<List<LoginSlide>> ObtenerTodosSlidesAsync() =>
            Task.FromResult(Slides.ToList());

        public Task<bool> GuardarConfigSistemaAsync(ConfigSistema config)
        {
            if (SaveConfigResult)
            {
                Config = config;
            }

            return Task.FromResult(SaveConfigResult);
        }

        public Task<bool> CrearSlideAsync(LoginSlide slide)
        {
            Slides.Add(slide);
            return Task.FromResult(true);
        }

        public Task<bool> ActualizarSlideAsync(LoginSlide slide)
        {
            int index = Slides.FindIndex(item => item.Id == slide.Id);
            if (index < 0)
            {
                return Task.FromResult(false);
            }

            Slides[index] = slide;
            return Task.FromResult(true);
        }

        public Task<bool> EliminarSlideAsync(int id) =>
            Task.FromResult(Slides.RemoveAll(item => item.Id == id) > 0);
    }

    private sealed class NoOpAuditoriaRepository : IAuditoriaRepository
    {
        public Task RegistrarAsync(
            string tabla,
            string registroId,
            string accion,
            string? datosAnt,
            string? datosNvo,
            long? usrId,
            string? email,
            string? ip,
            string? modulo) => Task.CompletedTask;

        public Task<(List<AuditoriaDto> Datos, int Total)> ObtenerBitacoraPaginadaAsync(
            int pagina,
            int limite,
            string? buscar,
            string? accion,
            string? modulo,
            string? tabla,
            DateTime? fechaInicio,
            DateTime? fechaFin) =>
            Task.FromResult((new List<AuditoriaDto>(), 0));
    }
}
