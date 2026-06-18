using RL.API.Models;
using RL.API.Repositories;

namespace RL.API.Services;

public interface ICatalogoService
{
    Task<List<KeyValuePair<int, string>>> ObtenerRolesAsync();
    Task<List<KeyValuePair<int, string>>> ObtenerDominiosAsync();
    Task<List<Modulo>> ObtenerModulosAsync();
}

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
