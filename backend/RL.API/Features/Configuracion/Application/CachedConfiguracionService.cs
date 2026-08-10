using RL.API.Features.Configuracion.Contracts;
using RL.API.Infrastructure.Caching;

namespace RL.API.Features.Configuracion.Application;

/// <summary>
/// Decorador BE-02 para configuración y slides de login. Cada mutación exitosa
/// invalida explícitamente el alcance relacionado antes de que una lectura posterior
/// pueda reutilizar información obsoleta.
/// </summary>
public sealed class CachedConfiguracionService : IConfiguracionService
{
    private readonly ConfiguracionService _inner;
    private readonly IApplicationCache _cache;
    private readonly ApplicationCacheSettings _settings;

    public CachedConfiguracionService(
        ConfiguracionService inner,
        IApplicationCache cache,
        ApplicationCacheSettings settings)
    {
        _inner = inner;
        _cache = cache;
        _settings = settings;
    }

    public Task<ConfigSistema?> ObtenerConfigSistemaAsync() =>
        _cache.GetOrCreateAsync(
            ApplicationCacheScopes.ConfiguracionSistema,
            "actual",
            _settings.ConfiguracionSistemaTtl,
            _inner.ObtenerConfigSistemaAsync,
            static value => value is not null);

    public Task<List<LoginSlide>> ObtenerSlidesAsync() =>
        _cache.GetOrCreateAsync(
            ApplicationCacheScopes.LoginSlides,
            "activos",
            _settings.LoginSlidesTtl,
            _inner.ObtenerSlidesAsync);

    public Task<List<LoginSlide>> ObtenerTodosSlidesAsync() =>
        _cache.GetOrCreateAsync(
            ApplicationCacheScopes.LoginSlides,
            "todos",
            _settings.LoginSlidesTtl,
            _inner.ObtenerTodosSlidesAsync);

    public async Task<bool> GuardarConfigSistemaAsync(ConfigSistema config, long usuarioId, string? ip)
    {
        bool result = await _inner.GuardarConfigSistemaAsync(config, usuarioId, ip);
        if (result)
        {
            _cache.Invalidate(ApplicationCacheScopes.ConfiguracionSistema);
        }

        return result;
    }

    public async Task<bool> CrearSlideAsync(LoginSlide slide, long usuarioId, string? ip)
    {
        bool result = await _inner.CrearSlideAsync(slide, usuarioId, ip);
        InvalidateSlidesIfSuccessful(result);
        return result;
    }

    public async Task<bool> ActualizarSlideAsync(int id, LoginSlide slide, long usuarioId, string? ip)
    {
        bool result = await _inner.ActualizarSlideAsync(id, slide, usuarioId, ip);
        InvalidateSlidesIfSuccessful(result);
        return result;
    }

    public async Task<bool> EliminarSlideAsync(int id, long usuarioId, string? ip)
    {
        bool result = await _inner.EliminarSlideAsync(id, usuarioId, ip);
        InvalidateSlidesIfSuccessful(result);
        return result;
    }

    public Task RegistrarCargaImagenAsync(
        string nombreOriginal,
        string nombreGuardado,
        string url,
        string tipoMime,
        long tamanioBytes,
        long usuarioId,
        string? ip) =>
        _inner.RegistrarCargaImagenAsync(
            nombreOriginal,
            nombreGuardado,
            url,
            tipoMime,
            tamanioBytes,
            usuarioId,
            ip);

    private void InvalidateSlidesIfSuccessful(bool success)
    {
        if (success)
        {
            _cache.Invalidate(ApplicationCacheScopes.LoginSlides);
        }
    }
}
