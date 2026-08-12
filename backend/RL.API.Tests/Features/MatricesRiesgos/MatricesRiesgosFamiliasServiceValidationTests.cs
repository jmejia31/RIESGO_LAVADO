using System.Collections.Generic;
using System.Threading.Tasks;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;
using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Shared.Results;
using RL.API.Tests.Support;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatricesRiesgosFamiliasServiceValidationTests
{
    private static MatricesRiesgosAppService CrearServicio(out InterfaceStub repoStub)
    {
        IMatricesRiesgosRepository repo = InterfaceStub.Create<IMatricesRiesgosRepository>(out repoStub);
        IFormularioValidador validador = InterfaceStub.Create<IFormularioValidador>(out _);
        IMatricesRiesgoService calculador = InterfaceStub.Create<IMatricesRiesgoService>(out _);
        IAuditoriaRepository auditoria = InterfaceStub.Create<IAuditoriaRepository>(out InterfaceStub auditoriaStub);
        auditoriaStub.On("RegistrarAsync", _ => Task.CompletedTask);
        return new MatricesRiesgosAppService(repo, validador, calculador, auditoria);
    }

    [Fact]
    public async Task ListarFamilias_DevuelveListaExitosa()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo);
        var familiasEsperadas = new List<FamiliaFormularioDto>
        {
            new() { FamId = 1, FamCodigo = "LAFT_MATRIZ", FamNombre = "Matriz LAFT", FamActivo = true }
        };
        repo.On(nameof(IMatricesRiesgosRepository.ListarFamiliasFormularioAsync), _ => Task.FromResult(familiasEsperadas));

        ServiceResult<List<FamiliaFormularioDto>> result = await service.ListarFamiliasFormularioAsync();

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data!);
        Assert.Equal("LAFT_MATRIZ", result.Data![0].FamCodigo);
    }

    [Fact]
    public async Task ObtenerFamiliaPorId_RechazaIdInvalido()
    {
        MatricesRiesgosAppService service = CrearServicio(out _);

        ServiceResult<FamiliaFormularioDto> result = await service.ObtenerFamiliaFormularioPorIdAsync(0);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task ObtenerFamiliaPorId_DevuelveNotFoundSiNoExiste()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerFamiliaFormularioPorIdAsync), _ => Task.FromResult<FamiliaFormularioDto?>(null));

        ServiceResult<FamiliaFormularioDto> result = await service.ObtenerFamiliaFormularioPorIdAsync(99);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task CrearFamilia_NormalizaCodigoYCompruebaUnicidad()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo);
        var dto = new CrearFamiliaFormularioDto
        {
            FamCodigo = "   gtic_matriz   ",
            FamNombre = "  Matriz GTIC  ",
            FamDescripcion = "Descripción GTIC"
        };

        repo.On(nameof(IMatricesRiesgosRepository.ObtenerFamiliaFormularioPorCodigoAsync), _ => Task.FromResult<FamiliaFormularioDto?>(null));
        repo.On(nameof(IMatricesRiesgosRepository.CrearFamiliaFormularioAsync), _ => Task.FromResult(2L));

        ServiceResult<long> result = await service.CrearFamiliaFormularioAsync(dto);

        Assert.True(result.Success);
        Assert.Equal(2L, result.Data);
        StubInvocation llamada = Assert.Single(repo.CallsTo(nameof(IMatricesRiesgosRepository.CrearFamiliaFormularioAsync)));
        Assert.Equal("GTIC_MATRIZ", llamada.Arguments[0]);
        Assert.Equal("Matriz GTIC", llamada.Arguments[1]);
    }

    [Fact]
    public async Task CrearFamilia_RechazaCodigoDuplicado()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo);
        var dto = new CrearFamiliaFormularioDto
        {
            FamCodigo = "MATRIZ_RIESGOS_LAFT",
            FamNombre = "Matriz Duplicada"
        };

        repo.On(nameof(IMatricesRiesgosRepository.ObtenerFamiliaFormularioPorCodigoAsync), _ =>
            Task.FromResult<FamiliaFormularioDto?>(new FamiliaFormularioDto { FamId = 1, FamCodigo = "MATRIZ_RIESGOS_LAFT" }));

        ServiceResult<long> result = await service.CrearFamiliaFormularioAsync(dto);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("Ya existe una familia", result.Message);
    }

    [Fact]
    public async Task ActualizarFamilia_RechazaDesactivacionSiTieneVersionVigente()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo);
        var dto = new ActualizarFamiliaFormularioDto
        {
            FamNombre = "Matriz LAFT",
            FamActivo = false
        };

        repo.On(nameof(IMatricesRiesgosRepository.ObtenerFamiliaFormularioPorIdAsync), _ =>
            Task.FromResult<FamiliaFormularioDto?>(new FamiliaFormularioDto { FamId = 1, FamCodigo = "LAFT", TieneVersionVigente = true }));

        ServiceResult result = await service.ActualizarFamiliaFormularioAsync(1, dto);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("versión publicada vigente", result.Message);
    }

    [Fact]
    public async Task DesactivarFamilia_EjecutaDesactivacionAtomicaTransaccional()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerFamiliaFormularioPorIdAsync), _ =>
            Task.FromResult<FamiliaFormularioDto?>(new FamiliaFormularioDto { FamId = 2, FamCodigo = "GTIC", TieneVersionVigente = false }));
        repo.On(nameof(IMatricesRiesgosRepository.DesactivarFamiliaFormularioAtomicoAsync), _ => Task.FromResult(true));

        ServiceResult result = await service.DesactivarFamiliaFormularioAsync(2);

        Assert.True(result.Success);
        Assert.Contains("desactivada exitosamente", result.Message);
        Assert.Single(repo.CallsTo(nameof(IMatricesRiesgosRepository.DesactivarFamiliaFormularioAtomicoAsync)));
    }

    [Fact]
    public async Task ActualizarBorrador_RechazaModificacionDeVersionPublicadaHistorica()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo);
        repo.On(nameof(IMatricesRiesgosRepository.ActualizarBorradorFormularioAsync), _ => Task.FromResult(false));

        ServiceResult result = await service.ActualizarBorradorFormularioAsync(
            versionId: 10,
            jsonConfig: "{\"secciones\":[]}",
            usuarioId: 1);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("permanezca en DRAFT", result.Message);
    }
}
