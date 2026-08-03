#pragma warning disable CA1416
using System;
using System.Collections.Generic;
using System.Data;
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
    public async Task ObtenerVersionVigente_SiExiste_RetornaContratoVersionado()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out _, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerVersionVigenteFormularioAsync), _ =>
            Task.FromResult<VersionFormularioDto?>(new VersionFormularioDto
            {
                VerId = 10,
                VerCodigo = "FORM_A",
                VerVersion = 2,
                VerEstado = "PUBLISHED",
                VerVigente = true
            }));

        ServiceResult<VersionFormularioDto> result = await service.ObtenerVersionVigenteFormularioAsync("MATRIZ_RIESGOS_LAFT");

        Assert.True(result.Success);
        Assert.Equal(10, result.Data!.VerId);
        Assert.Equal("PUBLISHED", result.Data.VerEstado);
    }

    [Fact]
    public async Task CrearBorrador_DefinicionInvalida_RetornaBadRequest()
    {
        MatricesRiesgosAppService service = CrearServicio(out _, out _, out _);
        ServiceResult<long> result = await service.CrearBorradorFormularioAsync(1, "FORM_A", "{invalido", 99);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task CrearEvaluacion_VersionNoPublicada_RetornaBadRequest()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out _, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), _ =>
            Task.FromResult<VersionFormularioDto?>(new VersionFormularioDto
            {
                VerId = 10,
                VerEstado = "DRAFT",
                VerVigente = false,
                VerJson = "{}"
            }));

        ServiceResult<long> result = await service.CrearEvaluacionAsync(
            new EvaluacionRiesgoDto { EvaVersionId = 10, EvaDataJson = "{}" },
            99,
            "127.0.0.1");

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task CrearEvaluacion_DatosValidos_CalculaYPersiste()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out InterfaceStub validador, out InterfaceStub calculador);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), _ =>
            Task.FromResult<VersionFormularioDto?>(new VersionFormularioDto
            {
                VerId = 10,
                VerEstado = "PUBLISHED",
                VerVigente = true,
                VerJson = "{\"secciones\":[]}"
            }));
        validador.On(nameof(IFormularioValidador.ValidarRespuestasAsync), _ =>
            Task.FromResult(new FormularioValidationResult()));
        calculador.On(nameof(IMatricesRiesgoService.CalcularYValidarRiesgo), _ =>
            ServiceResult<CalculoRiesgoResultadoDto>.Ok(new CalculoRiesgoResultadoDto
            {
                Vri = 7,
                Etp = 25m,
                Vrr = 4
            }));
        repo.On(nameof(IMatricesRiesgosRepository.CrearEvaluacionAsync), _ => Task.FromResult(55L));

        var dto = new EvaluacionRiesgoDto
        {
            EvaRiesgoId = 5,
            EvaVersionId = 10,
            EvaDataJson = "{\"frecuencia_inherente\":4,\"impacto_inherente\":4}"
        };

        ServiceResult<long> result = await service.CrearEvaluacionAsync(dto, 99, "127.0.0.1");

        Assert.True(result.Success);
        Assert.Equal(55, result.Data);
        Assert.Equal(7, dto.EvaVri);
        Assert.Equal(4, dto.EvaVrr);
        Assert.False(string.IsNullOrWhiteSpace(dto.EvaDataCalcJson));
    }

    [Fact]
    public async Task ActualizarEvaluacion_ConflictoOptimista_Retorna409()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out InterfaceStub validador, out InterfaceStub calculador);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), _ =>
            Task.FromResult<VersionFormularioDto?>(new VersionFormularioDto
            {
                VerId = 10,
                VerEstado = "PUBLISHED",
                VerVigente = true,
                VerJson = "{}"
            }));
        validador.On(nameof(IFormularioValidador.ValidarRespuestasAsync), _ =>
            Task.FromResult(new FormularioValidationResult()));
        calculador.On(nameof(IMatricesRiesgoService.CalcularYValidarRiesgo), _ =>
            ServiceResult<CalculoRiesgoResultadoDto>.Ok(new CalculoRiesgoResultadoDto()));
        repo.On(nameof(IMatricesRiesgosRepository.ActualizarEvaluacionAsync), _ =>
            throw new DBConcurrencyException("Conflicto"));

        ServiceResult result = await service.ActualizarEvaluacionAsync(
            new EvaluacionRiesgoDto { EvaVersionId = 10, EvaDataJson = "{}" },
            99,
            "127.0.0.1");

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
    }

    [Theory]
    [InlineData("BORRADOR", "EN_REVISION")]
    [InlineData("EN_REVISION", "OBSERVADA")]
    [InlineData("EN_REVISION", "APROBADA")]
    [InlineData("EN_REVISION", "RECHAZADA")]
    [InlineData("OBSERVADA", "BORRADOR")]
    [InlineData("APROBADA", "CERRADA")]
    public async Task TransicionPermitida_RetornaOk(string actual, string nuevo)
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out _, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerEvaluacionAsync), _ =>
            Task.FromResult<EvaluacionRiesgoDto?>(new EvaluacionRiesgoDto { EvaId = 1, EvaEstado = actual }));
        repo.On(nameof(IMatricesRiesgosRepository.TransicionarEstadoEvaluacionAsync), _ => Task.FromResult(true));

        ServiceResult result = await service.TransicionarEstadoEvaluacionAsync(1, nuevo, "Prueba", 99, "127.0.0.1");

        Assert.True(result.Success);
    }

    [Fact]
    public async Task Consolidado_RetornaFilasTipadas()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out _, out _);
        IReadOnlyList<RiesgoReporteFilaDto> filas = new List<RiesgoReporteFilaDto>
        {
            new() { RiesgoId = 1, EvaluacionId = 2, CodigoRiesgo = "R-001", Vri = 7, Vrr = 4 }
        };
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerConsolidadoTipadoAsync), _ => Task.FromResult(filas));

        ServiceResult<IReadOnlyList<RiesgoReporteFilaDto>> result = await service.ObtenerConsolidadoTipadoAsync();

        Assert.True(result.Success);
        Assert.Single(result.Data!);
        Assert.Equal("R-001", result.Data![0].CodigoRiesgo);
    }

    [Fact]
    public async Task Metodologia_RetornaVersionSeccionesCatalogosYReglas()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out _, out _);
        var metodologia = new MetodologiaFormularioDto
        {
            VersionFormularioId = 10,
            Codigo = "FORM_A",
            Version = 2,
            Secciones = new[] { new SeccionFormularioDto { Clave = "s1", Campos = Array.Empty<CampoFormularioDto>() } },
            Catalogos = Array.Empty<CatalogoMatricesDto>(),
            Reglas = new[] { new ReglaCalculoMatricesDto { Codigo = "CALCULO_VRI_VRR", Version = "1.0" } }
        };
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerMetodologiaDinamicaVigenteAsync), _ =>
            Task.FromResult<MetodologiaFormularioDto?>(metodologia));

        ServiceResult<MetodologiaFormularioDto> result = await service.ObtenerMetodologiaDinamicaVigenteAsync();

        Assert.True(result.Success);
        Assert.Equal(10, result.Data!.VersionFormularioId);
        Assert.Single(result.Data.Reglas);
    }

    [Fact]
    public async Task Vinculaciones_DeLasNueveEntidades_DeleganAlRepositorio()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out _, out _);
        repo.On(nameof(IMatricesRiesgosRepository.VincularEvidenciaRiesgoAsync), _ => Task.FromResult(true));
        repo.On(nameof(IMatricesRiesgosRepository.VincularEvidenciaEvaluacionAsync), _ => Task.FromResult(true));
        repo.On(nameof(IMatricesRiesgosRepository.VincularEvidenciaControlAsync), _ => Task.FromResult(true));
        repo.On(nameof(IMatricesRiesgosRepository.VincularEvidenciaPlanAsync), _ => Task.FromResult(true));
        repo.On(nameof(IMatricesRiesgosRepository.VincularEvidenciaActividadAsync), _ => Task.FromResult(true));
        repo.On(nameof(IMatricesRiesgosRepository.VincularEvidenciaAlertaAsync), _ => Task.FromResult(true));
        repo.On(nameof(IMatricesRiesgosRepository.VincularEvidenciaAutomonitoreoAsync), _ => Task.FromResult(true));
        repo.On(nameof(IMatricesRiesgosRepository.VincularEvidenciaRevisionAsync), _ => Task.FromResult(true));
        repo.On(nameof(IMatricesRiesgosRepository.VincularEvidenciaAprobacionAsync), _ => Task.FromResult(true));

        Assert.True((await service.VincularEvidenciaRiesgoAsync(new AsociarEvidenciaRiesgoDto(), 99, null)).Success);
        Assert.True((await service.VincularEvidenciaEvaluacionAsync(new AsociarEvidenciaEvaluacionDto(), 99, null)).Success);
        Assert.True((await service.VincularEvidenciaControlAsync(new AsociarEvidenciaControlDto(), 99, null)).Success);
        Assert.True((await service.VincularEvidenciaPlanAsync(new AsociarEvidenciaPlanDto(), 99, null)).Success);
        Assert.True((await service.VincularEvidenciaActividadAsync(new AsociarEvidenciaActividadDto(), 99, null)).Success);
        Assert.True((await service.VincularEvidenciaAlertaAsync(new AsociarEvidenciaAlertaDto(), 99, null)).Success);
        Assert.True((await service.VincularEvidenciaAutomonitoreoAsync(new AsociarEvidenciaAutomonitoreoDto(), 99, null)).Success);
        Assert.True((await service.VincularEvidenciaRevisionAsync(new AsociarEvidenciaRevisionDto(), 99, null)).Success);
        Assert.True((await service.VincularEvidenciaAprobacionAsync(new AsociarEvidenciaAprobacionDto(), 99, null)).Success);
    }

    [Fact]
    public async Task CargarEvidencia_ArchivoVacio_RetornaBadRequest()
    {
        MatricesRiesgosAppService service = CrearServicio(out _, out _, out _);
        var archivo = new FormFile(System.IO.Stream.Null, 0, 0, "archivo", "vacio.txt");

        ServiceResult<EvidenciaDto> result = await service.CargarArchivoEvidenciaFisicaAsync(archivo, 99);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
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
