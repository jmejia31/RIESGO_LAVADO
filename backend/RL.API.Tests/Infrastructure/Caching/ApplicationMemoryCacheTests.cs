using Microsoft.Extensions.Caching.Memory;
using RL.API.Infrastructure.Caching;
using Xunit;

namespace RL.API.Tests.Infrastructure.Caching;

public sealed class ApplicationMemoryCacheTests
{
    [Fact]
    public async Task GetOrCreateAsync_ReutilizaValorDentroDelTtl()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        using var cache = new ApplicationMemoryCache(memory);
        int calls = 0;

        int first = await cache.GetOrCreateAsync(
            "scope-a",
            "key",
            TimeSpan.FromMinutes(1),
            () => Task.FromResult(Interlocked.Increment(ref calls)));
        int second = await cache.GetOrCreateAsync(
            "scope-a",
            "key",
            TimeSpan.FromMinutes(1),
            () => Task.FromResult(Interlocked.Increment(ref calls)));

        Assert.Equal(1, first);
        Assert.Equal(1, second);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Invalidate_ObligaNuevaLecturaSoloEnElAlcanceAfectado()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        using var cache = new ApplicationMemoryCache(memory);
        int callsA = 0;
        int callsB = 0;

        await cache.GetOrCreateAsync("scope-a", "key", TimeSpan.FromMinutes(1), () => Task.FromResult(++callsA));
        await cache.GetOrCreateAsync("scope-b", "key", TimeSpan.FromMinutes(1), () => Task.FromResult(++callsB));

        cache.Invalidate("scope-a");

        int refreshedA = await cache.GetOrCreateAsync("scope-a", "key", TimeSpan.FromMinutes(1), () => Task.FromResult(++callsA));
        int cachedB = await cache.GetOrCreateAsync("scope-b", "key", TimeSpan.FromMinutes(1), () => Task.FromResult(++callsB));

        Assert.Equal(2, refreshedA);
        Assert.Equal(1, cachedB);
        Assert.Equal(2, callsA);
        Assert.Equal(1, callsB);
    }

    [Fact]
    public async Task GetOrCreateAsync_NoCacheaCuandoPredicadoLoRechaza()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        using var cache = new ApplicationMemoryCache(memory);
        int calls = 0;

        int first = await cache.GetOrCreateAsync(
            "scope",
            "key",
            TimeSpan.FromMinutes(1),
            () => Task.FromResult(++calls),
            static _ => false);
        int second = await cache.GetOrCreateAsync(
            "scope",
            "key",
            TimeSpan.FromMinutes(1),
            () => Task.FromResult(++calls),
            static _ => false);

        Assert.Equal(1, first);
        Assert.Equal(2, second);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task GetOrCreateAsync_EvitaCacheStampedeEnMismoAlcance()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        using var cache = new ApplicationMemoryCache(memory);
        int calls = 0;

        Task<int>[] requests = Enumerable.Range(0, 20)
            .Select(_ => cache.GetOrCreateAsync(
                "scope",
                "shared",
                TimeSpan.FromMinutes(1),
                async () =>
                {
                    Interlocked.Increment(ref calls);
                    await Task.Delay(20);
                    return 77;
                }))
            .ToArray();

        int[] results = await Task.WhenAll(requests);

        Assert.All(results, value => Assert.Equal(77, value));
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task InvalidacionConcurrente_NoPermiteRepoblarConLecturaObsoleta()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        using var cache = new ApplicationMemoryCache(memory);
        var lecturaIniciada = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var liberarLectura = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Task<int> staleRead = cache.GetOrCreateAsync(
            "scope",
            "key",
            TimeSpan.FromMinutes(1),
            async () =>
            {
                lecturaIniciada.SetResult();
                await liberarLectura.Task;
                return 1;
            });

        await lecturaIniciada.Task;
        cache.Invalidate("scope");
        liberarLectura.SetResult();

        Assert.Equal(1, await staleRead);

        int freshFactoryCalls = 0;
        int fresh = await cache.GetOrCreateAsync(
            "scope",
            "key",
            TimeSpan.FromMinutes(1),
            () => Task.FromResult(Interlocked.Increment(ref freshFactoryCalls) + 1));

        Assert.Equal(2, fresh);
        Assert.Equal(1, freshFactoryCalls);
    }

    [Fact]
    public void Settings_AcotaTtlEntreCincoYNovecientosSegundos()
    {
        var settings = new ApplicationCacheSettings
        {
            FormularioVersionTtlSeconds = 0,
            ConfiguracionSistemaTtlSeconds = 5000,
            LoginSlidesTtlSeconds = 60
        };

        Assert.Equal(TimeSpan.FromSeconds(5), settings.FormularioVersionTtl);
        Assert.Equal(TimeSpan.FromSeconds(900), settings.ConfiguracionSistemaTtl);
        Assert.Equal(TimeSpan.FromSeconds(60), settings.LoginSlidesTtl);
    }
}
