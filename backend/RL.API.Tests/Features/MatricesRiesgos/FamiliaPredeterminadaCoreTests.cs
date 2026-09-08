using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
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

public sealed class FamiliaPredeterminadaCoreTests
{
    [Fact]
    public async Task ConsultaSinConfiguracion_RetornaContratoValidoYNo404()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out _, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerFamiliaPredeterminadaAsync), _ =>
            Task.FromResult<FamiliaPredeterminadaDto?>(null));

        ServiceResult<FamiliaPredeterminadaDto> result = await service.ObtenerFamiliaPredeterminadaAsync();

        Assert.True(result.Success);
        Assert.Equal(200, result.StatusCode);
        Assert.False(result.Data!.Configurada);
    }

    [Fact]
    public async Task ConsultaConfigurada_DevuelveFamiliaYVersionExactas()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out _, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerFamiliaPredeterminadaAsync), _ =>
            Task.FromResult<FamiliaPredeterminadaDto?>(new FamiliaPredeterminadaDto
            {
                Configurada = true,
                FamiliaId = 22,
                FamiliaCodigo = "FAMILIA_B",
                FamiliaNombre = "Familia B",
                TieneVersionVigente = true,
                VersionVigenteId = 44,
                VersionCodigo = "FAMILIA_B_V3",
                Version = 3
            }));

        ServiceResult<FamiliaPredeterminadaDto> result = await service.ObtenerFamiliaPredeterminadaAsync();

        Assert.True(result.Success);
        Assert.Equal(22, result.Data!.FamiliaId);
        Assert.Equal(44, result.Data.VersionVigenteId);
    }

    [Theory]
    [InlineData(ResultadoFamiliaPredeterminada.Exito, 200)]
    [InlineData(ResultadoFamiliaPredeterminada.NoExiste, 404)]
    [InlineData(ResultadoFamiliaPredeterminada.Inactiva, 400)]
    [InlineData(ResultadoFamiliaPredeterminada.SinVersionVigente, 400)]
    [InlineData(ResultadoFamiliaPredeterminada.Conflicto, 409)]
    public async Task EstablecerFamilia_MapeaResultadoPublicoSeguro(ResultadoFamiliaPredeterminada resultado, int statusCode)
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out _, out _);
        repo.On(nameof(IMatricesRiesgosRepository.EstablecerFamiliaPredeterminadaAsync), _ => Task.FromResult(resultado));

        ServiceResult result = await service.EstablecerFamiliaPredeterminadaAsync(22, 99, "127.0.0.1");

        Assert.Equal(statusCode, result.StatusCode);
        Assert.Equal(resultado == ResultadoFamiliaPredeterminada.Exito, result.Success);
    }

    [Fact]
    public async Task VersionVigenteSinFamiliaExplicita_ResuelveFamiliaPredeterminada()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out _, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerFamiliaPredeterminadaAsync), _ =>
            Task.FromResult<FamiliaPredeterminadaDto?>(new FamiliaPredeterminadaDto
            {
                Configurada = true,
                FamiliaCodigo = "FAMILIA_B",
                TieneVersionVigente = true,
                VersionVigenteId = 44
            }));
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerVersionVigenteFormularioAsync), args =>
            Task.FromResult<VersionFormularioDto?>(new VersionFormularioDto
            {
                VerId = 44,
                VerFamiliaId = 22,
                VerEstado = "PUBLISHED",
                VerVigente = true
            }));

        ServiceResult<VersionFormularioDto> result = await service.ObtenerVersionVigenteFormularioAsync(null);

        Assert.True(result.Success);
        StubInvocation call = Assert.Single(repo.CallsTo(nameof(IMatricesRiesgosRepository.ObtenerVersionVigenteFormularioAsync)));
        Assert.Equal("FAMILIA_B", call.Arguments[0]);
    }

    [Fact]
    public async Task MetodologiaVigente_UsaVersionPredeterminadaExacta()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out _, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerFamiliaPredeterminadaAsync), _ =>
            Task.FromResult<FamiliaPredeterminadaDto?>(new FamiliaPredeterminadaDto
            {
                Configurada = true,
                FamiliaCodigo = "FAMILIA_B",
                TieneVersionVigente = true,
                VersionVigenteId = 44
            }));
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerMetodologiaDinamicaPorVersionAsync), _ =>
            Task.FromResult<MetodologiaFormularioDto?>(new MetodologiaFormularioDto { VersionFormularioId = 44 }));

        ServiceResult<MetodologiaFormularioDto> result = await service.ObtenerMetodologiaDinamicaVigenteAsync();

        Assert.True(result.Success);
        Assert.Equal(44, result.Data!.VersionFormularioId);
        StubInvocation call = Assert.Single(repo.CallsTo(nameof(IMatricesRiesgosRepository.ObtenerMetodologiaDinamicaPorVersionAsync)));
        Assert.Equal(44L, call.Arguments[0]);
        Assert.Empty(repo.CallsTo(nameof(IMatricesRiesgosRepository.ObtenerMetodologiaDinamicaVigenteAsync)));
    }

    [Fact]
    public async Task DesactivarFamiliaPredeterminada_EstaBloqueada()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out _, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerFamiliaFormularioPorIdAsync), _ =>
            Task.FromResult<FamiliaFormularioDto?>(new FamiliaFormularioDto
            {
                FamId = 22,
                FamPredeterminada = true,
                FamActivo = true
            }));

        ServiceResult result = await service.DesactivarFamiliaFormularioAsync(22);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Empty(repo.CallsTo(nameof(IMatricesRiesgosRepository.DesactivarFamiliaFormularioAtomicoAsync)));
    }

    [Fact]
    public async Task RetirarUltimaVersionPredeterminada_EstaBloqueado()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out _, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), _ =>
            Task.FromResult<VersionFormularioDto?>(new VersionFormularioDto
            {
                VerId = 44,
                VerFamiliaId = 22,
                VerEstado = "PUBLISHED",
                VerVigente = true
            }));
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerFamiliaFormularioPorIdAsync), _ =>
            Task.FromResult<FamiliaFormularioDto?>(new FamiliaFormularioDto
            {
                FamId = 22,
                FamPredeterminada = true,
                TieneVersionVigente = true
            }));

        ServiceResult result = await service.CambiarEstadoVigenciaFormularioAsync(44, false, 99);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Empty(repo.CallsTo(nameof(IMatricesRiesgosRepository.CambiarEstadoVigenciaFormularioAsync)));
    }

    [Fact]
    public void EndpointsMantienenAutorizacionAdministrativaYNoHayDefaultHardcodeado()
    {
        MethodInfo setMethod = typeof(MatricesRiesgosController).GetMethod(nameof(MatricesRiesgosController.EstablecerFamiliaPredeterminada))!;
        AuthorizeAttribute authorize = Assert.Single(setMethod.GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal(SystemRoles.Administrador, authorize.Roles);

        MethodInfo vigenteMethod = typeof(MatricesRiesgosController).GetMethod(nameof(MatricesRiesgosController.ObtenerVersionVigenteFormulario))!;
        ParameterInfo parameter = Assert.Single(vigenteMethod.GetParameters());
        Assert.Null(parameter.DefaultValue);
    }

    [Fact]
    public async Task ControladorSetDefault_ConvierteConflictoEn409()
    {
        IMatricesRiesgosAppService service = InterfaceStub.Create<IMatricesRiesgosAppService>(out InterfaceStub stub);
        stub.On(nameof(IMatricesRiesgosAppService.EstablecerFamiliaPredeterminadaAsync), _ =>
            Task.FromResult(ServiceResult.Conflict("Conflicto controlado")));
        var controller = new MatricesRiesgosController(service, NullLogger<MatricesRiesgosController>.Instance);
        var context = new DefaultHttpContext();
        context.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
            new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "99") }, "test"));
        controller.ControllerContext = new ControllerContext { HttpContext = context };

        IActionResult result = await controller.EstablecerFamiliaPredeterminada(22);

        Assert.Equal(409, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    private static MatricesRiesgosAppService CrearServicio(
        out InterfaceStub repoStub,
        out InterfaceStub validadorStub,
        out InterfaceStub calculadorStub)
    {
        IMatricesRiesgosRepository repo = InterfaceStub.Create<IMatricesRiesgosRepository>(out repoStub);
        IFormularioValidador validador = InterfaceStub.Create<IFormularioValidador>(out validadorStub);
        IMatricesRiesgoService calculador = InterfaceStub.Create<IMatricesRiesgoService>(out calculadorStub);
        IAuditoriaRepository auditoria = InterfaceStub.Create<IAuditoriaRepository>(out InterfaceStub auditoriaStub);
        auditoriaStub.On("RegistrarAsync", _ => Task.CompletedTask);
        return new MatricesRiesgosAppService(repo, validador, calculador, auditoria);
    }
}
