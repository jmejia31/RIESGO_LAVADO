using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
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

public sealed class MatricesRiesgosFormularioRoundTripTests
{
    private const string JsonRico = """
        {
          "codigoFormulario":"MATRIZ_F63",
          "nombreFormulario":"Matriz F6.3",
          "secciones":[{
            "id":"s1",
            "clave":"s1",
            "titulo":"Sección",
            "orden":1,
            "campos":[{
              "id":"nivel",
              "clave":"nivel",
              "etiqueta":"Nivel",
              "tipo":"selector-catalogo",
              "codigoCatalogo":"CAT_NIVEL",
              "obligatorio":true,
              "extensionCampo":{"flag":false,"cero":0,"nullable":null}
            }]
          }],
          "catalogos":[{
            "codigo":"CAT_NIVEL",
            "nombre":"Nivel",
            "elementos":[
              {"codigo":"001","valor":"Inicial","orden":1,"extensionElemento":{"origen":"F6.3"}},
              {"codigo":"G-IVM","valor":"Especial","orden":2}
            ],
            "extensionCatalogo":{"futuro":true}
          }],
          "reglas":[],
          "extensionRaiz":{"preservar":"sin-perdida","falseValue":false,"zeroValue":0,"nullValue":null}
        }
        """;

    [Fact]
    public async Task ObtenerVersionPorId_RetornaVerJsonCompletoSinPerdida()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), args =>
        {
            Assert.Equal(77L, args[0]);
            return Task.FromResult<VersionFormularioDto?>(new VersionFormularioDto
            {
                VerId = 77,
                VerCodigo = "MATRIZ_F63",
                VerVersion = 4,
                VerEstado = "DRAFT",
                VerVigente = false,
                VerJson = JsonRico
            });
        });

        ServiceResult<VersionFormularioDto> result = await service.ObtenerVersionFormularioAsync(77);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(JsonRico), JsonNode.Parse(result.Data!.VerJson)));
        Assert.Contains("\"001\"", result.Data.VerJson, StringComparison.Ordinal);
        Assert.Contains("\"G-IVM\"", result.Data.VerJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ActualizarBorrador_PropagaJsonRicoAlRepositorioSinPerdidaSemantica()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo);
        string? jsonRecibido = null;
        string? hashRecibido = null;

        repo.On(nameof(IMatricesRiesgosRepository.ActualizarBorradorFormularioAsync), args =>
        {
            Assert.Equal(77L, args[0]);
            jsonRecibido = Assert.IsType<string>(args[1]);
            hashRecibido = Assert.IsType<string>(args[2]);
            Assert.Equal(99L, args[3]);
            return Task.FromResult(true);
        });

        ServiceResult result = await service.ActualizarBorradorFormularioAsync(77, JsonRico, 99);

        Assert.True(result.Success);
        Assert.NotNull(jsonRecibido);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(JsonRico), JsonNode.Parse(jsonRecibido)));
        Assert.False(string.IsNullOrWhiteSpace(hashRecibido));
    }

    [Fact]
    public async Task ActualizarBorrador_JsonInvalido_NoInvocaRepositorio()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo);

        ServiceResult result = await service.ActualizarBorradorFormularioAsync(77, "{json-invalido", 99);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Empty(repo.CallsTo(nameof(IMatricesRiesgosRepository.ActualizarBorradorFormularioAsync)));
    }

    [Fact]
    public async Task EndpointVersionPorId_DelegaIdExactoYRetorna200()
    {
        IMatricesRiesgosAppService app = InterfaceStub.Create<IMatricesRiesgosAppService>(out InterfaceStub stub);
        stub.On(nameof(IMatricesRiesgosAppService.ObtenerVersionFormularioAsync), args =>
        {
            Assert.Equal(77L, args[0]);
            return Task.FromResult(ServiceResult<VersionFormularioDto>.Ok(new VersionFormularioDto
            {
                VerId = 77,
                VerCodigo = "MATRIZ_F63",
                VerEstado = "DRAFT",
                VerJson = JsonRico
            }));
        });

        var controller = new MatricesRiesgosFormulariosController(app);

        IActionResult action = await controller.ObtenerVersionFormulario(77);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(action);
        Assert.Equal(200, ok.StatusCode);
        Assert.Single(stub.CallsTo(nameof(IMatricesRiesgosAppService.ObtenerVersionFormularioAsync)));
    }

    [Fact]
    public async Task EndpointVersionPorId_CuandoNoExiste_Retorna404()
    {
        IMatricesRiesgosAppService app = InterfaceStub.Create<IMatricesRiesgosAppService>(out InterfaceStub stub);
        stub.On(nameof(IMatricesRiesgosAppService.ObtenerVersionFormularioAsync), _ =>
            Task.FromResult(ServiceResult<VersionFormularioDto>.NotFound("No existe.")));

        var controller = new MatricesRiesgosFormulariosController(app);

        IActionResult action = await controller.ObtenerVersionFormulario(404);

        ObjectResult notFound = Assert.IsType<ObjectResult>(action);
        Assert.Equal(404, notFound.StatusCode);
    }

    private static MatricesRiesgosAppService CrearServicio(out InterfaceStub repoStub)
    {
        IMatricesRiesgosRepository repo = InterfaceStub.Create<IMatricesRiesgosRepository>(out repoStub);
        IFormularioValidador validador = InterfaceStub.Create<IFormularioValidador>(out _);
        IMatricesRiesgoService calculador = InterfaceStub.Create<IMatricesRiesgoService>(out _);
        IAuditoriaRepository auditoria = InterfaceStub.Create<IAuditoriaRepository>(out InterfaceStub auditoriaStub);
        auditoriaStub.On("RegistrarAsync", _ => Task.CompletedTask);
        return new MatricesRiesgosAppService(repo, validador, calculador, auditoria);
    }
}
