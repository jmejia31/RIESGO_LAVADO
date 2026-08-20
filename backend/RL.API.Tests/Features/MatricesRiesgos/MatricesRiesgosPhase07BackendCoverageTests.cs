using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RL.API.Core.Security;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Features.MatricesRiesgos;
using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;
using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Shared.Results;
using RL.API.Tests.Support;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatricesRiesgosPhase07BackendCoverageTests
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
    public async Task CrearBorradorFormulario_ValidaJsonInvalido_RetornaBadRequest400()
    {
        MatricesRiesgosAppService service = CrearServicio(out _);

        ServiceResult<long> result = await service.CrearBorradorFormularioAsync(
            familiaId: 1,
            codigoFormulario: "MATRIZ_RIESGOS_LAFT_V2",
            jsonConfig: "{ json_invalido: ",
            usuarioId: 1);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("no es válida", result.Message);
    }

    [Fact]
    public async Task PublicarVersion_ValidaVersionInexistente_Retorna404()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), _ => Task.FromResult<VersionFormularioDto?>(null));

        ServiceResult result = await service.PublicarVersionFormularioAsync(999, 1);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Contains("No se encontró la versión", result.Message);
    }

    [Fact]
    public async Task CambiarEstadoVigencia_ValidaVigenciaInexistente_Retorna404()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), _ => Task.FromResult<VersionFormularioDto?>(null));

        ServiceResult result = await service.CambiarEstadoVigenciaFormularioAsync(888, true, 1);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Contains("No se encontró la versión", result.Message);
    }

    [Fact]
    public async Task EliminarVersionFormulario_ValidaVersionInexistente_Retorna404()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), _ => Task.FromResult<VersionFormularioDto?>(null));

        ServiceResult result = await service.EliminarVersionFormularioAsync(777);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Contains("No se encontró el formulario", result.Message);
    }

    [Fact]
    public void EndpointsSensibles_ExigenRolAdministrador()
    {
        MethodInfo[] metodosPrivilegiados = new[]
        {
            typeof(MatricesRiesgosController).GetMethod(nameof(MatricesRiesgosController.CrearBorradorFormulario))!,
            typeof(MatricesRiesgosController).GetMethod(nameof(MatricesRiesgosController.ClonarVersionFormulario))!,
            typeof(MatricesRiesgosController).GetMethod(nameof(MatricesRiesgosController.ActualizarBorradorFormulario))!,
            typeof(MatricesRiesgosController).GetMethod(nameof(MatricesRiesgosController.PublicarVersionFormulario))!,
            typeof(MatricesRiesgosController).GetMethod(nameof(MatricesRiesgosController.CambiarEstadoVigenciaFormulario))!,
            typeof(MatricesRiesgosController).GetMethod(nameof(MatricesRiesgosController.EliminarVersionFormulario))!
        };

        foreach (MethodInfo metodo in metodosPrivilegiados)
        {
            AuthorizeAttribute authorize = Assert.Single(metodo.GetCustomAttributes<AuthorizeAttribute>());
            Assert.Equal(SystemRoles.Administrador, authorize.Roles);
        }
    }
}
