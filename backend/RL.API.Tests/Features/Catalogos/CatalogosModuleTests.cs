using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RL.API.Features.Catalogos;
using RL.API.Features.Catalogos.Application;
using RL.API.Features.Catalogos.Contracts;
using RL.API.Features.Catalogos.Persistence;
using Xunit;

namespace RL.API.Tests.Features.Catalogos;

public sealed class CatalogosModuleTests
{
    [Fact]
    public async Task CatalogoService_DelegaConsultasAlRepositorio()
    {
        var repository = new CatalogoRepositoryFake();
        var service = new CatalogoService(repository);

        var roles = await service.ObtenerRolesAsync();
        var dominios = await service.ObtenerDominiosAsync();
        var modulos = await service.ObtenerModulosAsync();

        Assert.Single(roles);
        Assert.Single(dominios);
        Assert.Single(modulos);
        Assert.Equal(1, repository.RolesCalls);
        Assert.Equal(1, repository.DominiosCalls);
        Assert.Equal(1, repository.ModulosCalls);
    }

    [Fact]
    public async Task CatalogosController_Roles_ConservaContratoPublico()
    {
        var controller = new CatalogosController(new CatalogoServiceFake());

        var result = Assert.IsType<OkObjectResult>(await controller.Roles());
        var json = JsonSerializer.Serialize(result.Value);

        Assert.Contains("\"success\":true", json, StringComparison.Ordinal);
        Assert.Contains("\"rolId\":7", json, StringComparison.Ordinal);
        Assert.Contains("\"rolNombre\":\"ADMINISTRADOR\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CatalogosController_Modulos_ConservaRutaYCamposPublicos()
    {
        var controller = new CatalogosController(new CatalogoServiceFake());

        var result = Assert.IsType<OkObjectResult>(await controller.Modulos());
        var json = JsonSerializer.Serialize(result.Value);

        Assert.Contains("\"modId\":2", json, StringComparison.Ordinal);
        Assert.Contains("\"modRuta\":\"/usuarios\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("modActivo", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CatalogoServiceAdministraCatalogosMatricesSinCrearModeloParalelo()
    {
        var repository = new CatalogoRepositoryFake();
        var service = new CatalogoService(repository);
        var id = await service.CrearCatalogoMatricesAsync(new CrearCatalogoMatricesDto("CAT_TEST", "Prueba"));
        var elemento = await service.CrearElementoCatalogoMatricesAsync(id, new CrearElementoCatalogoMatricesDto("A", "Activo", 1));

        Assert.Equal(10, id);
        Assert.Equal(20, elemento);
        Assert.Single(await service.ListarMatricesAsync(false));
    }

    private sealed class CatalogoRepositoryFake : ICatalogoRepository
    {
        public int RolesCalls { get; private set; }
        public int DominiosCalls { get; private set; }
        public int ModulosCalls { get; private set; }

        public Task<List<KeyValuePair<int, string>>> ObtenerRolesAsync()
        {
            RolesCalls++;
            return Task.FromResult(new List<KeyValuePair<int, string>> { new(7, "ADMINISTRADOR") });
        }

        public Task<List<KeyValuePair<int, string>>> ObtenerDominiosAsync()
        {
            DominiosCalls++;
            return Task.FromResult(new List<KeyValuePair<int, string>> { new(1, "IHSS") });
        }

        public Task<List<Modulo>> ObtenerModulosAsync()
        {
            ModulosCalls++;
            return Task.FromResult(new List<Modulo> { CrearModulo() });
        }

        public Task<List<CatalogoMatricesDto>> ListarMatricesAsync(bool incluirInactivos) => Task.FromResult(new List<CatalogoMatricesDto> { new(10, "CAT_TEST", "Prueba", true, Array.Empty<ElementoCatalogoMatricesDto>()) });
        public Task<long> CrearCatalogoMatricesAsync(string codigo, string nombre) => Task.FromResult(10L);
        public Task<bool> ActualizarCatalogoMatricesAsync(long id, string nombre, bool activo) => Task.FromResult(true);
        public Task<long> CrearElementoCatalogoMatricesAsync(long catalogoId, string codigo, string valor, int orden) => Task.FromResult(20L);
        public Task<bool> ActualizarElementoCatalogoMatricesAsync(long id, string valor, int orden, bool activo) => Task.FromResult(true);
    }

    private sealed class CatalogoServiceFake : ICatalogoService
    {
        public Task<List<KeyValuePair<int, string>>> ObtenerRolesAsync() =>
            Task.FromResult(new List<KeyValuePair<int, string>> { new(7, "ADMINISTRADOR") });

        public Task<List<KeyValuePair<int, string>>> ObtenerDominiosAsync() =>
            Task.FromResult(new List<KeyValuePair<int, string>> { new(1, "IHSS") });

        public Task<List<Modulo>> ObtenerModulosAsync() =>
            Task.FromResult(new List<Modulo> { CrearModulo() });

        public Task<List<CatalogoMatricesDto>> ListarMatricesAsync(bool incluirInactivos) => Task.FromResult(new List<CatalogoMatricesDto>());
        public Task<long> CrearCatalogoMatricesAsync(CrearCatalogoMatricesDto dto) => Task.FromResult(1L);
        public Task<bool> ActualizarCatalogoMatricesAsync(long id, ActualizarCatalogoMatricesDto dto) => Task.FromResult(true);
        public Task<long> CrearElementoCatalogoMatricesAsync(long catalogoId, CrearElementoCatalogoMatricesDto dto) => Task.FromResult(1L);
        public Task<bool> ActualizarElementoCatalogoMatricesAsync(long id, ActualizarElementoCatalogoMatricesDto dto) => Task.FromResult(true);
    }

    private static Modulo CrearModulo() => new()
    {
        ModId = 2,
        ModNombre = "Usuarios",
        ModDescripcion = "Administración de usuarios",
        ModRuta = "/usuarios",
        ModIcono = "users",
        ModSeccion = "Administración",
        ModActivo = 1
    };
}
