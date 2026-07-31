using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;
using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Shared.Results;
using RL.API.Tests.Support;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatricesRiesgosApplicationTests
{
    [Fact]
    public async Task ObtenerVersionVigenteFormulario_SiExiste_RetornaOk()
    {
        var service = CrearServicio(out var repoStub, out _, out _);
        var expected = new VersionFormularioDto { VerId = 1, VerCodigo = "FORM_A", VerVigente = true };
        
        repoStub.On(nameof(IMatricesRiesgosRepository.ObtenerVersionVigenteFormularioAsync), _ => Task.FromResult<VersionFormularioDto?>(expected));

        var result = await service.ObtenerVersionVigenteFormularioAsync("MATRIZ_RIESGOS_LAFT");

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("FORM_A", result.Data!.VerCodigo);
    }

    [Fact]
    public async Task ObtenerVersionFormulario_SiNoExiste_RetornaNotFound()
    {
        var service = CrearServicio(out var repoStub, out _, out _);
        repoStub.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), _ => Task.FromResult<VersionFormularioDto?>(null));

        var result = await service.ObtenerVersionFormularioAsync(999);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task CrearBorradorFormulario_JsonInvalido_RetornaBadRequest()
    {
        var service = CrearServicio(out _, out _, out _);
        var result = await service.CrearBorradorFormularioAsync(1, "FORM_A", "{ roto: true ", 99);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task CrearBorradorFormulario_JsonVacio_RetornaBadRequest()
    {
        var service = CrearServicio(out _, out _, out _);
        var result = await service.CrearBorradorFormularioAsync(1, "FORM_A", "", 99);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task ActualizarBorradorFormulario_JsonInvalido_RetornaBadRequest()
    {
        var service = CrearServicio(out _, out _, out _);
        var result = await service.ActualizarBorradorFormularioAsync(1, "{ rotisimo: ", 99);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task PublicarVersionFormulario_EstadoDiferenteDraft_RetornaBadRequest()
    {
        var service = CrearServicio(out var repoStub, out _, out _);
        var publicado = new VersionFormularioDto { VerId = 1, VerEstado = "PUBLISHED", VerJson = "{}" };
        repoStub.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), _ => Task.FromResult<VersionFormularioDto?>(publicado));

        var result = await service.PublicarVersionFormularioAsync(1, 99);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task CambiarEstadoVigenciaFormulario_FalloRepo_RetornaNotFound()
    {
        var service = CrearServicio(out var repoStub, out _, out _);
        repoStub.On(nameof(IMatricesRiesgosRepository.CambiarEstadoVigenciaFormularioAsync), _ => Task.FromResult(false));

        var result = await service.CambiarEstadoVigenciaFormularioAsync(99, true, 99);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task ObtenerEvaluacion_SiNoExiste_RetornaNotFound()
    {
        var service = CrearServicio(out var repoStub, out _, out _);
        repoStub.On(nameof(IMatricesRiesgosRepository.ObtenerEvaluacionAsync), _ => Task.FromResult<EvaluacionRiesgoDto?>(null));

        var result = await service.ObtenerEvaluacionAsync(999);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task CrearEvaluacion_VersionFormularioInvalido_RetornaBadRequest()
    {
        var service = CrearServicio(out var repoStub, out _, out _);
        repoStub.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), _ => Task.FromResult<VersionFormularioDto?>(null));

        var dto = new EvaluacionRiesgoDto { EvaVersionId = 999 };
        var result = await service.CrearEvaluacionAsync(dto, 99, "127.0.0.1");

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task CrearEvaluacion_ValidadorFalla_RetornaBadRequest()
    {
        var service = CrearServicio(out var repoStub, out var valStub, out _);
        var configForm = new VersionFormularioDto { VerId = 1, VerJson = "{}" };
        repoStub.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), _ => Task.FromResult<VersionFormularioDto?>(configForm));

        var validationFail = new FormularioValidationResult();
        validationFail.Errores.Add(new FormularioValidationError("campo1", "Falla"));
        valStub.On(nameof(IFormularioValidador.ValidarRespuestasAsync), _ => Task.FromResult(validationFail));

        var dto = new EvaluacionRiesgoDto { EvaVersionId = 1, EvaDataJson = "{}" };
        var result = await service.CrearEvaluacionAsync(dto, 99, "127.0.0.1");

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task CrearEvaluacion_CalculoIncoherente_RetornaBadRequest()
    {
        var service = CrearServicio(out var repoStub, out var valStub, out var calcStub);
        var configForm = new VersionFormularioDto { VerId = 1, VerJson = "{}" };
        repoStub.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), _ => Task.FromResult<VersionFormularioDto?>(configForm));
        valStub.On(nameof(IFormularioValidador.ValidarRespuestasAsync), _ => Task.FromResult(new FormularioValidationResult()));

        calcStub.On(nameof(IMatricesRiesgoService.CalcularYValidarRiesgo), _ => ServiceResult<CalculoRiesgoResultadoDto>.BadRequest("Incoherente"));

        var dto = new EvaluacionRiesgoDto { EvaVersionId = 1, EvaDataJson = "{}" };
        var result = await service.CrearEvaluacionAsync(dto, 99, "127.0.0.1");

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task ActualizarEvaluacion_ConcurrenciaOptimistaFalla_RetornaConflict()
    {
        var service = CrearServicio(out var repoStub, out var valStub, out var calcStub);
        var configForm = new VersionFormularioDto { VerId = 1, VerJson = "{}" };
        repoStub.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), _ => Task.FromResult<VersionFormularioDto?>(configForm));
        valStub.On(nameof(IFormularioValidador.ValidarRespuestasAsync), _ => Task.FromResult(new FormularioValidationResult()));
        calcStub.On(nameof(IMatricesRiesgoService.CalcularYValidarRiesgo), _ => ServiceResult<CalculoRiesgoResultadoDto>.Ok(new CalculoRiesgoResultadoDto()));

        repoStub.On(nameof(IMatricesRiesgosRepository.ActualizarEvaluacionAsync), _ => {
            throw new System.Data.DBConcurrencyException("Conflicto.");
        });

        var dto = new EvaluacionRiesgoDto { EvaVersionId = 1, EvaDataJson = "{}" };
        var result = await service.ActualizarEvaluacionAsync(dto, 99, "127.0.0.1");

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
    }

    [Fact]
    public async Task TransicionarEstadoEvaluacion_EstadoInvalido_RetornaBadRequest()
    {
        var service = CrearServicio(out var repoStub, out _, out _);

        // Setup evaluacion actual en estado CERRADA
        var actual = new EvaluacionRiesgoDto { EvaId = 5, EvaEstado = "CERRADA" };
        repoStub.On(nameof(IMatricesRiesgosRepository.ObtenerEvaluacionAsync), _ => Task.FromResult<EvaluacionRiesgoDto?>(actual));

        // Intentar pasar a BORRADOR (no permitido por grafo)
        var result = await service.TransicionarEstadoEvaluacionAsync(5, "BORRADOR", "Reabrir", 99, "127.0.0.1");

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task PublicarVersionFormulario_SiExisteBorrador_PublicaCorrectamente()
    {
        var service = CrearServicio(out var repoStub, out _, out _);

        var borrador = new VersionFormularioDto { VerId = 2, VerEstado = "DRAFT", VerJson = "{}" };
        repoStub.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), _ => Task.FromResult<VersionFormularioDto?>(borrador));
        repoStub.On(nameof(IMatricesRiesgosRepository.PublicarVersionFormularioAsync), _ => Task.FromResult(true));

        var result = await service.PublicarVersionFormularioAsync(2, 99);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ObtenerEvidenciaFisica_SiNoExiste_RetornaNotFound()
    {
        var service = CrearServicio(out var repoStub, out _, out _);
        repoStub.On(nameof(IMatricesRiesgosRepository.ObtenerEvidenciaFisicaAsync), _ => Task.FromResult<EvidenciaDto?>(null));

        var result = await service.ObtenerEvidenciaFisicaAsync(999);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task VincularEvidencias_LlamanAlRepositorioCorrectamente()
    {
        var service = CrearServicio(out var repoStub, out _, out _);

        repoStub.On(nameof(IMatricesRiesgosRepository.VincularEvidenciaRiesgoAsync), _ => Task.FromResult(true));
        repoStub.On(nameof(IMatricesRiesgosRepository.VincularEvidenciaEvaluacionAsync), _ => Task.FromResult(true));
        repoStub.On(nameof(IMatricesRiesgosRepository.VincularEvidenciaControlAsync), _ => Task.FromResult(true));
        repoStub.On(nameof(IMatricesRiesgosRepository.VincularEvidenciaPlanAsync), _ => Task.FromResult(true));
        repoStub.On(nameof(IMatricesRiesgosRepository.VincularEvidenciaActividadAsync), _ => Task.FromResult(true));
        repoStub.On(nameof(IMatricesRiesgosRepository.VincularEvidenciaAlertaAsync), _ => Task.FromResult(true));
        repoStub.On(nameof(IMatricesRiesgosRepository.VincularEvidenciaAutomonitoreoAsync), _ => Task.FromResult(true));
        repoStub.On(nameof(IMatricesRiesgosRepository.VincularEvidenciaRevisionAsync), _ => Task.FromResult(true));
        repoStub.On(nameof(IMatricesRiesgosRepository.VincularEvidenciaAprobacionAsync), _ => Task.FromResult(true));

        Assert.True((await service.VincularEvidenciaRiesgoAsync(new AsociarEvidenciaRiesgoDto(), 99, "127.0.0.1")).Success);
        Assert.True((await service.VincularEvidenciaEvaluacionAsync(new AsociarEvidenciaEvaluacionDto(), 99, "127.0.0.1")).Success);
        Assert.True((await service.VincularEvidenciaControlAsync(new AsociarEvidenciaControlDto(), 99, "127.0.0.1")).Success);
        Assert.True((await service.VincularEvidenciaPlanAsync(new AsociarEvidenciaPlanDto(), 99, "127.0.0.1")).Success);
        Assert.True((await service.VincularEvidenciaActividadAsync(new AsociarEvidenciaActividadDto(), 99, "127.0.0.1")).Success);
        Assert.True((await service.VincularEvidenciaAlertaAsync(new AsociarEvidenciaAlertaDto(), 99, "127.0.0.1")).Success);
        Assert.True((await service.VincularEvidenciaAutomonitoreoAsync(new AsociarEvidenciaAutomonitoreoDto(), 99, "127.0.0.1")).Success);
        Assert.True((await service.VincularEvidenciaRevisionAsync(new AsociarEvidenciaRevisionDto(), 99, "127.0.0.1")).Success);
        Assert.True((await service.VincularEvidenciaAprobacionAsync(new AsociarEvidenciaAprobacionDto(), 99, "127.0.0.1")).Success);
    }

    [Theory]
    [InlineData("BORRADOR", "EN_REVISION")]
    [InlineData("EN_REVISION", "OBSERVADA")]
    [InlineData("EN_REVISION", "APROBADA")]
    [InlineData("EN_REVISION", "RECHAZADA")]
    [InlineData("OBSERVADA", "BORRADOR")]
    [InlineData("APROBADA", "CERRADA")]
    public async Task TransicionarEstadoEvaluacion_CasosValidos_RetornaOk(string actual, string nuevo)
    {
        var service = CrearServicio(out var repoStub, out _, out _);
        
        var evaluacion = new EvaluacionRiesgoDto { EvaId = 1, EvaEstado = actual };
        repoStub.On(nameof(IMatricesRiesgosRepository.ObtenerEvaluacionAsync), _ => Task.FromResult<EvaluacionRiesgoDto?>(evaluacion));
        repoStub.On(nameof(IMatricesRiesgosRepository.TransicionarEstadoEvaluacionAsync), _ => Task.FromResult(true));

        var result = await service.TransicionarEstadoEvaluacionAsync(1, nuevo, "Transición de prueba", 99, "127.0.0.1");

        Assert.True(result.Success);
    }

    [Fact]
    public async Task CargarArchivoEvidenciaFisica_ArchivoNulo_RetornaBadRequest()
    {
        var service = CrearServicio(out _, out _, out _);
        
        var result = await service.CargarArchivoEvidenciaFisicaAsync(null!, 99);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task CargarArchivoEvidenciaFisica_ArchivoVacio_RetornaBadRequest()
    {
        var service = CrearServicio(out _, out _, out _);
        
        // Simular IFormFile vacío
        var fileStub = new FormFile(Stream.Null, 0, 0, "archivo", "vacio.txt");
        var result = await service.CargarArchivoEvidenciaFisicaAsync(fileStub, 99);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task ClonarVersionFormulario_OrigenNoExiste_RetornaNotFound()
    {
        var service = CrearServicio(out var repoStub, out _, out _);
        
        repoStub.On(nameof(IMatricesRiesgosRepository.ClonarVersionFormularioAsync), _ => {
            throw new KeyNotFoundException("No se encontró la versión origen.");
        });

        var result = await service.ClonarVersionFormularioAsync(999, 99);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task VincularEvidencias_FalloRepo_RetornanBadRequest()
    {
        var service = CrearServicio(out var repoStub, out _, out _);

        repoStub.On(nameof(IMatricesRiesgosRepository.VincularEvidenciaRiesgoAsync), _ => Task.FromResult(false));
        repoStub.On(nameof(IMatricesRiesgosRepository.VincularEvidenciaEvaluacionAsync), _ => Task.FromResult(false));
        repoStub.On(nameof(IMatricesRiesgosRepository.VincularEvidenciaControlAsync), _ => Task.FromResult(false));
        repoStub.On(nameof(IMatricesRiesgosRepository.VincularEvidenciaPlanAsync), _ => Task.FromResult(false));
        repoStub.On(nameof(IMatricesRiesgosRepository.VincularEvidenciaActividadAsync), _ => Task.FromResult(false));
        repoStub.On(nameof(IMatricesRiesgosRepository.VincularEvidenciaAlertaAsync), _ => Task.FromResult(false));
        repoStub.On(nameof(IMatricesRiesgosRepository.VincularEvidenciaAutomonitoreoAsync), _ => Task.FromResult(false));
        repoStub.On(nameof(IMatricesRiesgosRepository.VincularEvidenciaRevisionAsync), _ => Task.FromResult(false));
        repoStub.On(nameof(IMatricesRiesgosRepository.VincularEvidenciaAprobacionAsync), _ => Task.FromResult(false));

        Assert.False((await service.VincularEvidenciaRiesgoAsync(new AsociarEvidenciaRiesgoDto(), 99, "127.0.0.1")).Success);
        Assert.False((await service.VincularEvidenciaEvaluacionAsync(new AsociarEvidenciaEvaluacionDto(), 99, "127.0.0.1")).Success);
        Assert.False((await service.VincularEvidenciaControlAsync(new AsociarEvidenciaControlDto(), 99, "127.0.0.1")).Success);
        Assert.False((await service.VincularEvidenciaPlanAsync(new AsociarEvidenciaPlanDto(), 99, "127.0.0.1")).Success);
        Assert.False((await service.VincularEvidenciaActividadAsync(new AsociarEvidenciaActividadDto(), 99, "127.0.0.1")).Success);
        Assert.False((await service.VincularEvidenciaAlertaAsync(new AsociarEvidenciaAlertaDto(), 99, "127.0.0.1")).Success);
        Assert.False((await service.VincularEvidenciaAutomonitoreoAsync(new AsociarEvidenciaAutomonitoreoDto(), 99, "127.0.0.1")).Success);
        Assert.False((await service.VincularEvidenciaRevisionAsync(new AsociarEvidenciaRevisionDto(), 99, "127.0.0.1")).Success);
        Assert.False((await service.VincularEvidenciaAprobacionAsync(new AsociarEvidenciaAprobacionDto(), 99, "127.0.0.1")).Success);
    }

    private static MatricesRiesgosAppService CrearServicio(
        out InterfaceStub repoStub, 
        out InterfaceStub valStub, 
        out InterfaceStub calcStub)
    {
        var repo = InterfaceStub.Create<IMatricesRiesgosRepository>(out repoStub);
        var val = InterfaceStub.Create<IFormularioValidador>(out valStub);
        var calc = InterfaceStub.Create<IMatricesRiesgoService>(out calcStub);
        
        return new MatricesRiesgosAppService(repo, val, calc);
    }
}
