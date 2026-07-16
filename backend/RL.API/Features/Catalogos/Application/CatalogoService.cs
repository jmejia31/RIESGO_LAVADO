using RL.API.Features.Catalogos.Contracts;
using RL.API.Features.Catalogos.Persistence;

namespace RL.API.Features.Catalogos.Application;

public class CatalogoService : ICatalogoService
{
    private readonly ICatalogoRepository _repo;

    public CatalogoService(ICatalogoRepository repo)
    {
        _repo = repo;
    }

    public Task<List<KeyValuePair<int, string>>> ObtenerRolesAsync() => _repo.ObtenerRolesAsync();

    public Task<List<KeyValuePair<int, string>>> ObtenerDominiosAsync() => _repo.ObtenerDominiosAsync();

    public Task<List<Modulo>> ObtenerModulosAsync() => _repo.ObtenerModulosAsync();
}
