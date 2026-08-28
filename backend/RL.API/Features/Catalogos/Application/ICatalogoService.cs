using RL.API.Features.Catalogos.Contracts;

namespace RL.API.Features.Catalogos.Application;

public interface ICatalogoService
{
    Task<List<KeyValuePair<int, string>>> ObtenerRolesAsync();
    Task<List<KeyValuePair<int, string>>> ObtenerDominiosAsync();
    Task<List<Modulo>> ObtenerModulosAsync();
    Task<List<CatalogoMatricesDto>> ListarMatricesAsync(bool incluirInactivos);
    Task<long> CrearCatalogoMatricesAsync(CrearCatalogoMatricesDto dto);
    Task<bool> ActualizarCatalogoMatricesAsync(long id, ActualizarCatalogoMatricesDto dto);
    Task<long> CrearElementoCatalogoMatricesAsync(long catalogoId, CrearElementoCatalogoMatricesDto dto);
    Task<bool> ActualizarElementoCatalogoMatricesAsync(long id, ActualizarElementoCatalogoMatricesDto dto);
}
