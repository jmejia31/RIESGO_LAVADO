using RL.API.Features.Auditoria.Persistence;
using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;
using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Infrastructure.Caching;
using RL.API.Shared.Results;
using RL.API.Tests.Support;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class CachedMatricesRiesgosAppServiceCoverageTests
{
    [Fact]
    public async Task LecturasEstables_UsanScopeClavesNormalizadasYTtlConfigurado()
    {
        MatricesRiesgosAppService inner = CrearInner(out InterfaceStub repo);
        var cache = new RecordingApplicationCache();
        var settings = new ApplicationCacheSettings { FormularioVersionTtlSeconds = 37 };
        var service = new CachedMatricesRiesgosAppService(inner, cache, settings);

        var version = new VersionFormularioDto
        {
            VerId = 7,
            VerFamiliaId = 2,
            VerCodigo = "MATRIZ_RIESGOS_V1",
            VerVersion = 1,
            VerJson = "{}",
            VerEstado = "PUBLISHED",
            VerVigente = true
        };
        var familia = new FamiliaFormularioDto
        {
            FamId = 2,
            FamCodigo = "MATRIZ_RIESGOS",
            FamNombre = "Matriz de Riesgos",
            FamActivo = true
        };

        repo.On(nameof(IMatricesRiesgosRepository.ObtenerVersionVigenteFormularioAsync), _ => Task.FromResult<VersionFormularioDto?>(version));
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), _ => Task.FromResult<VersionFormularioDto?>(version));
        repo.On(nameof(IMatricesRiesgosRepository.ListarHistorialVersionesFormularioAsync), _ => Task.FromResult(new List<VersionFormularioDto> { version }));
        repo.On(nameof(IMatricesRiesgosRepository.ListarFamiliasFormularioAsync), _ => Task.FromResult(new List<FamiliaFormularioDto> { familia }));
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerFamiliaFormularioPorIdAsync), _ => Task.FromResult<FamiliaFormularioDto?>(familia));
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerMetodologiaDinamicaVigenteAsync), _ => Task.FromResult<MetodologiaFormularioDto?>(new MetodologiaFormularioDto()));

        ServiceResult<VersionFormularioDto> vigente = await service.ObtenerVersionVigenteFormularioAsync("  matriz_riesgos  ");
        ServiceResult<VersionFormularioDto> porId = await service.ObtenerVersionFormularioAsync(7);
        ServiceResult<List<VersionFormularioDto>> historial = await service.ListarHistorialVersionesFormularioAsync(" matriz_riesgos ");
        ServiceResult<List<FamiliaFormularioDto>> familias = await service.ListarFamiliasFormularioAsync();
        ServiceResult<FamiliaFormularioDto> familiaPorId = await service.ObtenerFamiliaFormularioPorIdAsync(2);
        ServiceResult<MetodologiaFormularioDto> metodologia = await service.ObtenerMetodologiaDinamicaVigenteAsync();

        Assert.True(vigente.Success);
        Assert.True(porId.Success);
        Assert.True(historial.Success);
        Assert.True(familias.Success);
        Assert.True(familiaPorId.Success);
        Assert.True(metodologia.Success);

        Assert.Collection(
            cache.Calls,
            call => AssertCacheCall(call, "vigente:MATRIZ_RIESGOS", true, settings.FormularioVersionTtl),
            call => AssertCacheCall(call, "version:7", true, settings.FormularioVersionTtl),
            call => AssertCacheCall(call, "historial:MATRIZ_RIESGOS", true, settings.FormularioVersionTtl),
            call => AssertCacheCall(call, "familias-list", true, settings.FormularioVersionTtl),
            call => AssertCacheCall(call, "familia-id:2", true, settings.FormularioVersionTtl),
            call => AssertCacheCall(call, "metodologia-vigente", true, settings.FormularioVersionTtl));
    }

    [Fact]
    public async Task LecturaFallida_NoSeMarcaComoCacheable()
    {
        MatricesRiesgosAppService inner = CrearInner(out InterfaceStub repo);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), _ => Task.FromResult<VersionFormularioDto?>(null));
        var cache = new RecordingApplicationCache();
        var service = new CachedMatricesRiesgosAppService(inner, cache, new ApplicationCacheSettings());

        ServiceResult<VersionFormularioDto> result = await service.ObtenerVersionFormularioAsync(999);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        CacheCall call = Assert.Single(cache.Calls);
        Assert.Equal("version:999", call.Key);
        Assert.False(call.ShouldCache ?? true);
    }

    [Fact]
    public async Task MutacionesExitosas_DeFamiliasYVersiones_InvalidanElScopeDeFormularios()
    {
        MatricesRiesgosAppService inner = CrearInner(out InterfaceStub repo);
        ConfigurarMutacionesExitosas(repo);
        var cache = new RecordingApplicationCache();
        var service = new CachedMatricesRiesgosAppService(inner, cache, new ApplicationCacheSettings());

        Assert.True((await service.CrearBorradorFormularioAsync(1, "MATRIZ_V2", "{}", 1)).Success);
        Assert.True((await service.ClonarVersionFormularioAsync(7, 1)).Success);
        Assert.True((await service.ActualizarBorradorFormularioAsync(7, "{}", 1)).Success);
        Assert.True((await service.PublicarVersionFormularioAsync(7, 1)).Success);
        Assert.True((await service.CambiarEstadoVigenciaFormularioAsync(7, true, 1)).Success);
        Assert.True((await service.EliminarVersionFormularioAsync(7)).Success);
        Assert.True((await service.CrearFamiliaFormularioAsync(new CrearFamiliaFormularioDto
        {
            FamCodigo = "NUEVA_FAMILIA",
            FamNombre = "Nueva Familia"
        })).Success);
        Assert.True((await service.ActualizarFamiliaFormularioAsync(2, new ActualizarFamiliaFormularioDto
        {
            FamNombre = "Familia actualizada",
            FamDescripcion = "Descripción",
            FamActivo = true
        })).Success);
        Assert.True((await service.DesactivarFamiliaFormularioAsync(2)).Success);

        Assert.Equal(9, cache.Invalidations.Count);
        Assert.All(cache.Invalidations, scope => Assert.Equal(ApplicationCacheScopes.MatricesFormularios, scope));
    }

    [Fact]
    public async Task MutacionFallida_NoInvalidaElScope()
    {
        MatricesRiesgosAppService inner = CrearInner(out _);
        var cache = new RecordingApplicationCache();
        var service = new CachedMatricesRiesgosAppService(inner, cache, new ApplicationCacheSettings());

        ServiceResult result = await service.ActualizarBorradorFormularioAsync(7, "   ", 1);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Empty(cache.Invalidations);
    }

    [Fact]
    public async Task LecturasTransaccionales_NoUsanCacheDeDefiniciones()
    {
        MatricesRiesgosAppService inner = CrearInner(out InterfaceStub repo);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerEvaluacionAsync), _ => Task.FromResult<EvaluacionRiesgoDto?>(null));
        repo.On(nameof(IMatricesRiesgosRepository.ListarEvaluacionesPaginadasAsync), _ => Task.FromResult(new List<EvaluacionRiesgoDto>()));
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerFlujosEvaluacionAsync), _ => Task.FromResult(new List<FlujoEvaluacionDto>()));
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerEvidenciaFisicaAsync), _ => Task.FromResult<EvidenciaDto?>(null));
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerConsolidadoTipadoAsync), _ =>
            Task.FromResult<IReadOnlyList<RiesgoReporteFilaDto>>(Array.Empty<RiesgoReporteFilaDto>()));

        var cache = new RecordingApplicationCache();
        var service = new CachedMatricesRiesgosAppService(inner, cache, new ApplicationCacheSettings());

        Assert.Equal(404, (await service.ObtenerEvaluacionAsync(50)).StatusCode);
        Assert.True((await service.ListarEvaluacionesPaginadasAsync(new ConsultaEvaluacionPaginadaDto())).Success);
        Assert.True((await service.ObtenerFlujosEvaluacionAsync(50)).Success);
        Assert.Equal(404, (await service.ObtenerEvidenciaFisicaAsync(70)).StatusCode);
        Assert.True((await service.ObtenerConsolidadoTipadoAsync()).Success);

        Assert.Empty(cache.Calls);
        Assert.Empty(cache.Invalidations);
    }

    private static MatricesRiesgosAppService CrearInner(out InterfaceStub repoStub)
    {
        IMatricesRiesgosRepository repo = InterfaceStub.Create<IMatricesRiesgosRepository>(out repoStub);
        IFormularioValidador validador = InterfaceStub.Create<IFormularioValidador>(out _);
        IMatricesRiesgoService calculador = InterfaceStub.Create<IMatricesRiesgoService>(out _);
        IAuditoriaRepository auditoria = InterfaceStub.Create<IAuditoriaRepository>(out InterfaceStub auditoriaStub);
        auditoriaStub.On("RegistrarAsync", _ => Task.CompletedTask);
        return new MatricesRiesgosAppService(repo, validador, calculador, auditoria);
    }

    private static void ConfigurarMutacionesExitosas(InterfaceStub repo)
    {
        repo.On(nameof(IMatricesRiesgosRepository.CrearBorradorFormularioAsync), _ => Task.FromResult(11L));
        repo.On(nameof(IMatricesRiesgosRepository.ClonarVersionFormularioAsync), _ => Task.FromResult(12L));
        repo.On(nameof(IMatricesRiesgosRepository.ActualizarBorradorFormularioAsync), _ => Task.FromResult(true));
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), args =>
            Task.FromResult<VersionFormularioDto?>(new VersionFormularioDto
            {
                VerId = (long)args[0]!,
                VerEstado = "DRAFT",
                VerJson = "{}",
                VerVigente = false
            }));
        repo.On(nameof(IMatricesRiesgosRepository.PublicarVersionFormularioAsync), _ => Task.FromResult(true));
        repo.On(nameof(IMatricesRiesgosRepository.CambiarEstadoVigenciaFormularioAsync), _ => Task.FromResult(true));
        repo.On(nameof(IMatricesRiesgosRepository.EliminarVersionFormularioAsync), _ => Task.FromResult(true));
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerFamiliaFormularioPorCodigoAsync), _ => Task.FromResult<FamiliaFormularioDto?>(null));
        repo.On(nameof(IMatricesRiesgosRepository.CrearFamiliaFormularioAsync), _ => Task.FromResult(13L));
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerFamiliaFormularioPorIdAsync), _ =>
            Task.FromResult<FamiliaFormularioDto?>(new FamiliaFormularioDto
            {
                FamId = 2,
                FamCodigo = "FAMILIA",
                FamNombre = "Familia",
                FamActivo = true,
                TieneVersionVigente = false
            }));
        repo.On(nameof(IMatricesRiesgosRepository.ActualizarFamiliaFormularioAsync), _ => Task.FromResult(true));
        repo.On(nameof(IMatricesRiesgosRepository.DesactivarFamiliaFormularioAtomicoAsync), _ => Task.FromResult(true));
    }

    private static void AssertCacheCall(CacheCall call, string key, bool shouldCache, TimeSpan ttl)
    {
        Assert.Equal(ApplicationCacheScopes.MatricesFormularios, call.Scope);
        Assert.Equal(key, call.Key);
        Assert.Equal(ttl, call.Ttl);
        Assert.Equal(shouldCache, call.ShouldCache);
    }

    private sealed class RecordingApplicationCache : IApplicationCache
    {
        public List<CacheCall> Calls { get; } = new();
        public List<string> Invalidations { get; } = new();

        public async Task<T> GetOrCreateAsync<T>(
            string scope,
            string key,
            TimeSpan ttl,
            Func<Task<T>> factory,
            Func<T, bool>? shouldCache = null,
            CancellationToken cancellationToken = default)
        {
            T value = await factory();
            Calls.Add(new CacheCall(scope, key, ttl, shouldCache?.Invoke(value)));
            return value;
        }

        public void Invalidate(string scope) => Invalidations.Add(scope);
    }

    private sealed record CacheCall(string Scope, string Key, TimeSpan Ttl, bool? ShouldCache);
}

public sealed class MatricesRiesgosAppServiceBoundaryCoverageTests
{
    [Fact]
    public async Task CrearFamilia_DtoNulo_Retorna400SinConsultarRepositorio()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo);

        ServiceResult<long> result = await service.CrearFamiliaFormularioAsync(null!);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Empty(repo.Invocations);
    }

    [Theory]
    [InlineData("AB")]
    [InlineData("ABC-DEF")]
    public async Task CrearFamilia_CodigoInvalido_Retorna400(string codigo)
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo);

        ServiceResult<long> result = await service.CrearFamiliaFormularioAsync(new CrearFamiliaFormularioDto
        {
            FamCodigo = codigo,
            FamNombre = "Familia válida"
        });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Empty(repo.Invocations);
    }

    [Fact]
    public async Task CrearFamilia_CodigoMayorA50_Retorna400()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo);

        ServiceResult<long> result = await service.CrearFamiliaFormularioAsync(new CrearFamiliaFormularioDto
        {
            FamCodigo = new string('A', 51),
            FamNombre = "Familia válida"
        });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Empty(repo.Invocations);
    }

    [Fact]
    public async Task CrearFamilia_NombreVacio_Retorna400()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo);

        ServiceResult<long> result = await service.CrearFamiliaFormularioAsync(new CrearFamiliaFormularioDto
        {
            FamCodigo = "FAMILIA_OK",
            FamNombre = "   "
        });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Empty(repo.Invocations);
    }

    [Fact]
    public async Task ObtenerFamilia_IdExistente_Retorna200()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerFamiliaFormularioPorIdAsync), _ =>
            Task.FromResult<FamiliaFormularioDto?>(new FamiliaFormularioDto
            {
                FamId = 8,
                FamCodigo = "FAM_8",
                FamNombre = "Familia 8"
            }));

        ServiceResult<FamiliaFormularioDto> result = await service.ObtenerFamiliaFormularioPorIdAsync(8);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(8L, result.Data!.FamId);
    }

    [Fact]
    public async Task ActualizarFamilia_IdInvalido_Retorna400SinConsultarRepositorio()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo);

        ServiceResult result = await service.ActualizarFamiliaFormularioAsync(0, new ActualizarFamiliaFormularioDto
        {
            FamNombre = "Familia",
            FamActivo = true
        });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Empty(repo.Invocations);
    }

    [Fact]
    public async Task ActualizarFamilia_DtoNulo_Retorna400SinConsultarRepositorio()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo);

        ServiceResult result = await service.ActualizarFamiliaFormularioAsync(3, null!);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Empty(repo.Invocations);
    }

    [Fact]
    public async Task ActualizarFamilia_Inexistente_Retorna404()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerFamiliaFormularioPorIdAsync), _ => Task.FromResult<FamiliaFormularioDto?>(null));

        ServiceResult result = await service.ActualizarFamiliaFormularioAsync(3, new ActualizarFamiliaFormularioDto
        {
            FamNombre = "Familia",
            FamActivo = true
        });

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Empty(repo.CallsTo(nameof(IMatricesRiesgosRepository.ActualizarFamiliaFormularioAsync)));
    }

    [Fact]
    public async Task ActualizarFamilia_NombreVacio_Retorna400()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerFamiliaFormularioPorIdAsync), _ =>
            Task.FromResult<FamiliaFormularioDto?>(new FamiliaFormularioDto { FamId = 3, FamNombre = "Actual" }));

        ServiceResult result = await service.ActualizarFamiliaFormularioAsync(3, new ActualizarFamiliaFormularioDto
        {
            FamNombre = "   ",
            FamActivo = true
        });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Empty(repo.CallsTo(nameof(IMatricesRiesgosRepository.ActualizarFamiliaFormularioAsync)));
    }

    [Fact]
    public async Task ActualizarFamilia_RepositorioRechaza_Retorna400()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerFamiliaFormularioPorIdAsync), _ =>
            Task.FromResult<FamiliaFormularioDto?>(new FamiliaFormularioDto { FamId = 3, FamNombre = "Actual" }));
        repo.On(nameof(IMatricesRiesgosRepository.ActualizarFamiliaFormularioAsync), _ => Task.FromResult(false));

        ServiceResult result = await service.ActualizarFamiliaFormularioAsync(3, new ActualizarFamiliaFormularioDto
        {
            FamNombre = "Nuevo nombre",
            FamActivo = true
        });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task ActualizarFamilia_Exito_NormalizaNombreYDescripcion()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerFamiliaFormularioPorIdAsync), _ =>
            Task.FromResult<FamiliaFormularioDto?>(new FamiliaFormularioDto { FamId = 3, FamNombre = "Actual" }));
        repo.On(nameof(IMatricesRiesgosRepository.ActualizarFamiliaFormularioAsync), _ => Task.FromResult(true));

        ServiceResult result = await service.ActualizarFamiliaFormularioAsync(3, new ActualizarFamiliaFormularioDto
        {
            FamNombre = "  Nuevo nombre  ",
            FamDescripcion = "  Descripción nueva  ",
            FamActivo = true
        });

        Assert.True(result.Success);
        StubInvocation call = Assert.Single(repo.CallsTo(nameof(IMatricesRiesgosRepository.ActualizarFamiliaFormularioAsync)));
        Assert.Equal(3L, call.Arguments[0]);
        Assert.Equal("Nuevo nombre", call.Arguments[1]);
        Assert.Equal("Descripción nueva", call.Arguments[2]);
        Assert.True((bool)call.Arguments[3]!);
    }

    [Fact]
    public async Task DesactivarFamilia_IdInvalido_Retorna400SinConsultarRepositorio()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo);

        ServiceResult result = await service.DesactivarFamiliaFormularioAsync(0);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Empty(repo.Invocations);
    }

    [Fact]
    public async Task DesactivarFamilia_Inexistente_Retorna404()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerFamiliaFormularioPorIdAsync), _ => Task.FromResult<FamiliaFormularioDto?>(null));

        ServiceResult result = await service.DesactivarFamiliaFormularioAsync(5);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Empty(repo.CallsTo(nameof(IMatricesRiesgosRepository.DesactivarFamiliaFormularioAtomicoAsync)));
    }

    [Fact]
    public async Task DesactivarFamilia_RepositorioRechaza_Retorna400()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerFamiliaFormularioPorIdAsync), _ =>
            Task.FromResult<FamiliaFormularioDto?>(new FamiliaFormularioDto { FamId = 5, FamNombre = "Familia" }));
        repo.On(nameof(IMatricesRiesgosRepository.DesactivarFamiliaFormularioAtomicoAsync), _ => Task.FromResult(false));

        ServiceResult result = await service.DesactivarFamiliaFormularioAsync(5);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task EliminarVersion_Vigente_Retorna400SinEliminar()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), _ =>
            Task.FromResult<VersionFormularioDto?>(new VersionFormularioDto { VerId = 4, VerVigente = true }));

        ServiceResult result = await service.EliminarVersionFormularioAsync(4);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Empty(repo.CallsTo(nameof(IMatricesRiesgosRepository.EliminarVersionFormularioAsync)));
    }

    [Theory]
    [InlineData(true, 200)]
    [InlineData(false, 400)]
    public async Task EliminarVersion_NoVigente_ConservaResultadoRepositorio(bool eliminado, int statusCode)
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), _ =>
            Task.FromResult<VersionFormularioDto?>(new VersionFormularioDto { VerId = 4, VerVigente = false }));
        repo.On(nameof(IMatricesRiesgosRepository.EliminarVersionFormularioAsync), _ => Task.FromResult(eliminado));

        ServiceResult result = await service.EliminarVersionFormularioAsync(4);

        Assert.Equal(eliminado, result.Success);
        Assert.Equal(statusCode, result.StatusCode);
    }

    [Fact]
    public async Task CrearEvaluacion_PublicadaPeroNoVigente_Retorna400AntesDeValidar()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), _ =>
            Task.FromResult<VersionFormularioDto?>(new VersionFormularioDto
            {
                VerId = 10,
                VerEstado = "PUBLISHED",
                VerVigente = false,
                VerJson = "{}"
            }));

        ServiceResult<long> result = await service.CrearEvaluacionAsync(new EvaluacionRiesgoDto
        {
            EvaVersionId = 10,
            EvaDataJson = "{}"
        }, 1, null);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("publicada y vigente", result.Message);
        Assert.Empty(repo.CallsTo(nameof(IMatricesRiesgosRepository.CrearEvaluacionAsync)));
    }

    [Fact]
    public async Task ActualizarEvaluacion_VersionInexistente_Retorna400AntesDeValidar()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), _ => Task.FromResult<VersionFormularioDto?>(null));

        ServiceResult result = await service.ActualizarEvaluacionAsync(new EvaluacionRiesgoDto
        {
            EvaId = 20,
            EvaVersionId = 999,
            EvaDataJson = "{}"
        }, 1, null);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("no existe", result.Message);
        Assert.Empty(repo.CallsTo(nameof(IMatricesRiesgosRepository.ActualizarEvaluacionAsync)));
    }

    [Fact]
    public async Task ClonarVersion_OrigenInexistente_Retorna404()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo);
        repo.On(nameof(IMatricesRiesgosRepository.ClonarVersionFormularioAsync), _ => throw new KeyNotFoundException("Versión origen inexistente."));

        ServiceResult<long> result = await service.ClonarVersionFormularioAsync(123, 1);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Contains("inexistente", result.Message);
    }

    [Fact]
    public async Task PublicarVersion_EstadoNoPublicable_Retorna400SinPublicar()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), _ =>
            Task.FromResult<VersionFormularioDto?>(new VersionFormularioDto
            {
                VerId = 9,
                VerEstado = "PUBLISHED",
                VerJson = "{}"
            }));

        ServiceResult result = await service.PublicarVersionFormularioAsync(9, 1);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Empty(repo.CallsTo(nameof(IMatricesRiesgosRepository.PublicarVersionFormularioAsync)));
    }

    [Fact]
    public async Task ActualizarBorrador_DefinicionVacia_Retorna400SinRepositorio()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo);

        ServiceResult result = await service.ActualizarBorradorFormularioAsync(1, "  ", 1);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Empty(repo.Invocations);
    }

    [Fact]
    public async Task ActualizarBorrador_JsonInvalido_Retorna400SinRepositorio()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo);

        ServiceResult result = await service.ActualizarBorradorFormularioAsync(1, "{ json_invalido: ", 1);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("no es válida", result.Message);
        Assert.Empty(repo.Invocations);
    }

    private static MatricesRiesgosAppService CrearServicio(out InterfaceStub repoStub)
    {
        IMatricesRiesgosRepository repo = InterfaceStub.Create<IMatricesRiesgosRepository>(out repoStub);
        IFormularioValidador validador = InterfaceStub.Create<IFormularioValidador>(out _);
        IMatricesRiesgoService calculador = InterfaceStub.Create<IMatricesRiesgoService>(out _);
        IAuditoriaRepository auditoria = InterfaceStub.Create<IAuditoriaRepository>(out InterfaceStub auditoriaStub);
        auditoriaStub.On("RegistrarAsync", _ => Task.CompletedTask);
        return new MatricesRiesgosAppService(repo, validador, calculador, auditoria);
    }
}
