using RL.API.Features.Catalogos.Contracts;

namespace RL.API.Features.Catalogos.Persistence;

public interface ICatalogoRepository
{
    Task<List<KeyValuePair<int, string>>> ObtenerRolesAsync();
    Task<List<KeyValuePair<int, string>>> ObtenerDominiosAsync();
    Task<List<Modulo>> ObtenerModulosAsync();
}
