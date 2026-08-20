using System;
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

public class MatricesRiesgosCicloVidaVersionTests
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
    public async Task Publicar_DraftValido_FamiliaActiva_RetornaOk()
    {
        var version = new VersionFormularioDto
        {
            VerId = 10,
            VerFamiliaId = 1,
            VerCodigo = "FORM_A",
            VerVersion = 1,
            VerEstado = "DRAFT",
            VerVigente = false,
            VerJson = "{\"secciones\":[]}"
        };

        var familia = new FamiliaFormularioDto
        {
            FamId = 1,
            FamCodigo = "FAM_A",
            FamNombre = "Familia A",
            FamActivo = true
        };

        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repoStub);
        repoStub.On("ObtenerVersionFormularioAsync", _ => Task.FromResult<VersionFormularioDto?>(version));
        repoStub.On("ObtenerFamiliaFormularioPorIdAsync", _ => Task.FromResult<FamiliaFormularioDto?>(familia));
        repoStub.On("PublicarVersionFormularioAsync", _ => Task.FromResult(true));

        ServiceResult res = await service.PublicarVersionFormularioAsync(10, 99);

        Assert.True(res.Success);
    }

    [Fact]
    public async Task Publicar_Published_Retorna400()
    {
        var version = new VersionFormularioDto
        {
            VerId = 10,
            VerEstado = "PUBLISHED"
        };

        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repoStub);
        repoStub.On("ObtenerVersionFormularioAsync", _ => Task.FromResult<VersionFormularioDto?>(version));

        ServiceResult res = await service.PublicarVersionFormularioAsync(10, 99);

        Assert.False(res.Success);
        Assert.Equal(400, res.StatusCode);
    }

    [Fact]
    public async Task Publicar_VersionInexistente_Retorna404()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repoStub);
        repoStub.On("ObtenerVersionFormularioAsync", _ => Task.FromResult<VersionFormularioDto?>(null));

        ServiceResult res = await service.PublicarVersionFormularioAsync(999, 99);

        Assert.False(res.Success);
        Assert.Equal(404, res.StatusCode);
    }

    [Fact]
    public async Task Publicar_FamiliaInactiva_RetornaErrorYNoInvocaRepositorio()
    {
        var version = new VersionFormularioDto
        {
            VerId = 10,
            VerFamiliaId = 1,
            VerEstado = "DRAFT"
        };

        var familiaInactiva = new FamiliaFormularioDto
        {
            FamId = 1,
            FamCodigo = "FAM_A",
            FamActivo = false
        };

        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repoStub);
        repoStub.On("ObtenerVersionFormularioAsync", _ => Task.FromResult<VersionFormularioDto?>(version));
        repoStub.On("ObtenerFamiliaFormularioPorIdAsync", _ => Task.FromResult<FamiliaFormularioDto?>(familiaInactiva));

        ServiceResult res = await service.PublicarVersionFormularioAsync(10, 99);

        Assert.False(res.Success);
        Assert.Equal(400, res.StatusCode);
        Assert.Contains("inactiva", res.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CambiarVigencia_VersionInexistente_Retorna404()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repoStub);
        repoStub.On("ObtenerVersionFormularioAsync", _ => Task.FromResult<VersionFormularioDto?>(null));

        ServiceResult res = await service.CambiarEstadoVigenciaFormularioAsync(999, true, 99);

        Assert.False(res.Success);
        Assert.Equal(404, res.StatusCode);
    }

    [Fact]
    public async Task CambiarVigencia_Draft_Retorna400YNoInvocaRepositorio()
    {
        var version = new VersionFormularioDto
        {
            VerId = 10,
            VerEstado = "DRAFT",
            VerVigente = false
        };

        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repoStub);
        repoStub.On("ObtenerVersionFormularioAsync", _ => Task.FromResult<VersionFormularioDto?>(version));

        ServiceResult res = await service.CambiarEstadoVigenciaFormularioAsync(10, true, 99);

        Assert.False(res.Success);
        Assert.Equal(400, res.StatusCode);
    }

    [Fact]
    public async Task CambiarVigencia_Published_PropagaResultado()
    {
        var version = new VersionFormularioDto
        {
            VerId = 10,
            VerEstado = "PUBLISHED",
            VerVigente = false
        };

        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repoStub);
        repoStub.On("ObtenerVersionFormularioAsync", _ => Task.FromResult<VersionFormularioDto?>(version));
        repoStub.On("CambiarEstadoVigenciaFormularioAsync", _ => Task.FromResult(true));

        ServiceResult res = await service.CambiarEstadoVigenciaFormularioAsync(10, true, 99);

        Assert.True(res.Success);
    }

    [Fact]
    public async Task Eliminar_DraftNoVigente_Permite()
    {
        var version = new VersionFormularioDto
        {
            VerId = 10,
            VerEstado = "DRAFT",
            VerVigente = false
        };

        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repoStub);
        repoStub.On("ObtenerVersionFormularioAsync", _ => Task.FromResult<VersionFormularioDto?>(version));
        repoStub.On("EliminarVersionFormularioAsync", _ => Task.FromResult(true));

        ServiceResult res = await service.EliminarVersionFormularioAsync(10);

        Assert.True(res.Success);
    }

    [Fact]
    public async Task Eliminar_PublishedVigente_BloqueaYNoInvocaRepositorio()
    {
        var version = new VersionFormularioDto
        {
            VerId = 10,
            VerEstado = "PUBLISHED",
            VerVigente = true
        };

        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repoStub);
        repoStub.On("ObtenerVersionFormularioAsync", _ => Task.FromResult<VersionFormularioDto?>(version));

        ServiceResult res = await service.EliminarVersionFormularioAsync(10);

        Assert.False(res.Success);
        Assert.Equal(400, res.StatusCode);
    }

    [Fact]
    public async Task Eliminar_PublishedHistorica_BloqueaYNoInvocaRepositorio()
    {
        var version = new VersionFormularioDto
        {
            VerId = 10,
            VerEstado = "PUBLISHED",
            VerVigente = false
        };

        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repoStub);
        repoStub.On("ObtenerVersionFormularioAsync", _ => Task.FromResult<VersionFormularioDto?>(version));

        ServiceResult res = await service.EliminarVersionFormularioAsync(10);

        Assert.False(res.Success);
        Assert.Equal(400, res.StatusCode);
        Assert.Contains("historial", res.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Clonar_Published_GeneraDraftYConservaOrigen()
    {
        var versionPublished = new VersionFormularioDto
        {
            VerId = 10,
            VerFamiliaId = 1,
            VerCodigo = "FORM_A",
            VerVersion = 1,
            VerEstado = "PUBLISHED",
            VerVigente = true,
            VerJson = "{\"secciones\":[{\"id\":\"s1\"}]}"
        };

        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repoStub);
        repoStub.On("ObtenerVersionFormularioAsync", _ => Task.FromResult<VersionFormularioDto?>(versionPublished));
        repoStub.On("ClonarVersionFormularioAsync", _ => Task.FromResult(20L));

        ServiceResult<long> res = await service.ClonarVersionFormularioAsync(10, 99);

        Assert.True(res.Success);
        Assert.Equal(20, res.Data);
        Assert.Equal("PUBLISHED", versionPublished.VerEstado);
    }
}
