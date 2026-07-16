using RL.API.Features.Catalogos.Contracts;

namespace RL.API.Features.Catalogos.Application;

public interface ICatalogoService
{
    Task<List<KeyValuePair<int, string>>> ObtenerRolesAsync();
    Task<List<KeyValuePair<int, string>>> ObtenerDominiosAsync();
    Task<List<Modulo>> ObtenerModulosAsync();
}
