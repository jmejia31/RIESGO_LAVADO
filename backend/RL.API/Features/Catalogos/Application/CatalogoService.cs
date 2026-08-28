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

    public Task<List<CatalogoMatricesDto>> ListarMatricesAsync(bool incluirInactivos) => _repo.ListarMatricesAsync(incluirInactivos);
    public Task<long> CrearCatalogoMatricesAsync(CrearCatalogoMatricesDto dto) => _repo.CrearCatalogoMatricesAsync(dto.Codigo, dto.Nombre);
    public Task<bool> ActualizarCatalogoMatricesAsync(long id, ActualizarCatalogoMatricesDto dto) => _repo.ActualizarCatalogoMatricesAsync(id, dto.Nombre, dto.Activo);
    public Task<long> CrearElementoCatalogoMatricesAsync(long catalogoId, CrearElementoCatalogoMatricesDto dto) => _repo.CrearElementoCatalogoMatricesAsync(catalogoId, dto.Codigo, dto.Valor, dto.Orden);
    public Task<bool> ActualizarElementoCatalogoMatricesAsync(long id, ActualizarElementoCatalogoMatricesDto dto) => _repo.ActualizarElementoCatalogoMatricesAsync(id, dto.Valor, dto.Orden, dto.Activo);
}
