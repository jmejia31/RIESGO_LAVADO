using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Infrastructure.Caching;
using RL.API.Shared.Results;
using RL.API.Tests.Support;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class FamiliasFormularioLifecycleServiceAdditionalTests
{
    [Fact]
    public async Task Archivar_FamiliaInactivaConVersionPublicadaNoVigente_ConfirmaOperacion()
    {
        FamiliasFormularioLifecycleService service = Crear(out InterfaceStub repo);
        repo.On(nameof(IFamiliasFormularioLifecycleRepository.ArchivarFamiliaFormularioAtomicoAsync), _ =>
            Task.FromResult(ResultadoArchivadoFamiliaFormulario.Exito));

        ServiceResult result = await service.ArchivarFamiliaFormularioAsync(9, "Retiro de familia de prueba");

        Assert.True(result.Success);
        Assert.Contains("historial", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Archivar_SinJustificacion_FallaAntesDePersistir()
    {
        FamiliasFormularioLifecycleService service = Crear(out InterfaceStub repo);

        ServiceResult result = await service.ArchivarFamiliaFormularioAsync(9, " ");

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.DoesNotContain(repo.Invocations, invocation => invocation.MethodName == nameof(IFamiliasFormularioLifecycleRepository.ArchivarFamiliaFormularioAtomicoAsync));
    }

    [Fact]
    public async Task Restaurar_Exito_DevuelveFamiliaInactiva()
    {
        FamiliasFormularioLifecycleService service = Crear(out InterfaceStub repo);
        repo.On(nameof(IFamiliasFormularioLifecycleRepository.RestaurarFamiliaFormularioAtomicoAsync), _ =>
            Task.FromResult(ResultadoRestauracionFamiliaFormulario.Exito));

        ServiceResult result = await service.RestaurarFamiliaFormularioAsync(9);

        Assert.True(result.Success);
        Assert.Contains("inactiva", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(ResultadoCambioEstadoFamiliaFormulario.TieneVersionVigente)]
    public async Task Activar_ResultadoNoEsperado_FallaCerrado(ResultadoCambioEstadoFamiliaFormulario backendResult)
    {
        FamiliasFormularioLifecycleService service = Crear(out InterfaceStub repo);
        repo.On(nameof(IFamiliasFormularioLifecycleRepository.ActivarFamiliaFormularioAtomicoAsync), _ => Task.FromResult(backendResult));

        ServiceResult result = await service.ActivarFamiliaFormularioAsync(9);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    private static FamiliasFormularioLifecycleService Crear(out InterfaceStub repoStub)
    {
        IFamiliasFormularioLifecycleRepository repo = InterfaceStub.Create<IFamiliasFormularioLifecycleRepository>(out repoStub);
        IApplicationCache cache = InterfaceStub.Create<IApplicationCache>(out InterfaceStub cacheStub);
        cacheStub.On(nameof(IApplicationCache.Invalidate), _ => null);
        return new FamiliasFormularioLifecycleService(repo, cache);
    }
}
