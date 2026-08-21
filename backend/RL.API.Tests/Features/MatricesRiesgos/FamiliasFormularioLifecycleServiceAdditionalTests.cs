using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Infrastructure.Caching;
using RL.API.Shared.Results;
using RL.API.Tests.Support;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class FamiliasFormularioLifecycleServiceAdditionalTests
{
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
