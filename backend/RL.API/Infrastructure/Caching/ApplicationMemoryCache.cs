using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace RL.API.Infrastructure.Caching;

/// <summary>
/// Alcances de caché BE-02. Cada alcance puede invalidarse de forma explícita y atómica.
/// </summary>
public static class ApplicationCacheScopes
{
    public const string MatricesFormularios = "be02:matrices-formularios";
    public const string ConfiguracionSistema = "be02:configuracion-sistema";
    public const string LoginSlides = "be02:login-slides";
}

/// <summary>
/// TTL configurables de BE-02. Los valores efectivos se acotan para impedir
/// cachés infinitas, desactivadas accidentalmente o excesivamente prolongadas.
/// </summary>
public sealed class ApplicationCacheSettings
{
    public int FormularioVersionTtlSeconds { get; set; } = 120;
    public int ConfiguracionSistemaTtlSeconds { get; set; } = 120;
    public int LoginSlidesTtlSeconds { get; set; } = 60;

    public TimeSpan FormularioVersionTtl => Normalize(FormularioVersionTtlSeconds);
    public TimeSpan ConfiguracionSistemaTtl => Normalize(ConfiguracionSistemaTtlSeconds);
    public TimeSpan LoginSlidesTtl => Normalize(LoginSlidesTtlSeconds);

    private static TimeSpan Normalize(int configuredSeconds) =>
        TimeSpan.FromSeconds(Math.Clamp(configuredSeconds, 5, 900));
}

public interface IApplicationCache
{
    Task<T> GetOrCreateAsync<T>(
        string scope,
        string key,
        TimeSpan ttl,
        Func<Task<T>> factory,
        Func<T, bool>? shouldCache = null,
        CancellationToken cancellationToken = default);

    void Invalidate(string scope);
}

/// <summary>
/// Caché en memoria por instancia con invalidación explícita mediante change tokens.
/// Un lock por alcance evita cache stampede sin crear una colección de locks por cada ID.
/// </summary>
public sealed class ApplicationMemoryCache : IApplicationCache, IDisposable
{
    private readonly IMemoryCache _memoryCache;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _scopeLocks = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _scopeTokens = new(StringComparer.Ordinal);
    private bool _disposed;

    public ApplicationMemoryCache(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public async Task<T> GetOrCreateAsync<T>(
        string scope,
        string key,
        TimeSpan ttl,
        Func<Task<T>> factory,
        Func<T, bool>? shouldCache = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(factory);

        string effectiveKey = $"{scope}:{key}";
        if (_memoryCache.TryGetValue(effectiveKey, out T? cached))
        {
            return cached!;
        }

        SemaphoreSlim gate = _scopeLocks.GetOrAdd(scope, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (_memoryCache.TryGetValue(effectiveKey, out cached))
            {
                return cached!;
            }

            // La generación se captura ANTES de consultar el origen. Si una mutación
            // invalida el alcance mientras la lectura está en vuelo, el resultado viejo
            // se devuelve al llamador original pero jamás repuebla la nueva generación.
            CancellationTokenSource scopeToken = _scopeTokens.GetOrAdd(
                scope,
                static _ => new CancellationTokenSource());

            T value = await factory();
            if (shouldCache is not null && !shouldCache(value))
            {
                return value;
            }

            if (scopeToken.IsCancellationRequested
                || !_scopeTokens.TryGetValue(scope, out CancellationTokenSource? currentToken)
                || !ReferenceEquals(scopeToken, currentToken))
            {
                return value;
            }

            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl <= TimeSpan.Zero ? TimeSpan.FromSeconds(5) : ttl
            };
            options.AddExpirationToken(new CancellationChangeToken(scopeToken.Token));

            _memoryCache.Set(effectiveKey, value, options);
            return value;
        }
        finally
        {
            gate.Release();
        }
    }

    public void Invalidate(string scope)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);

        while (true)
        {
            CancellationTokenSource current = _scopeTokens.GetOrAdd(
                scope,
                static _ => new CancellationTokenSource());
            var replacement = new CancellationTokenSource();

            if (_scopeTokens.TryUpdate(scope, replacement, current))
            {
                current.Cancel();
                current.Dispose();
                return;
            }

            replacement.Dispose();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (CancellationTokenSource token in _scopeTokens.Values)
        {
            token.Cancel();
            token.Dispose();
        }

        foreach (SemaphoreSlim gate in _scopeLocks.Values)
        {
            gate.Dispose();
        }
    }
}
