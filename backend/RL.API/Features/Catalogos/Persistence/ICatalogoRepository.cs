using RL.API.Features.Catalogos.Contracts;

namespace RL.API.Features.Catalogos.Persistence;

public interface ICatalogoRepository
{
    Task<List<KeyValuePair<int, string>>> ObtenerRolesAsync();
    Task<List<KeyValuePair<int, string>>> ObtenerDominiosAsync();
    Task<List<Modulo>> ObtenerModulosAsync();
    Task<List<CatalogoMatricesDto>> ListarMatricesAsync(bool incluirInactivos);
    Task<long> CrearCatalogoMatricesAsync(string codigo, string nombre);
    Task<bool> ActualizarCatalogoMatricesAsync(long id, string nombre, bool activo);
    Task<long> CrearElementoCatalogoMatricesAsync(long catalogoId, string codigo, string valor, int orden);
    Task<bool> ActualizarElementoCatalogoMatricesAsync(long id, string valor, int orden, bool activo);
}
