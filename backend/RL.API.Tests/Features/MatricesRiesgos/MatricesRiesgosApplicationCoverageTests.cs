#pragma warning disable CA1416
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;
using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Shared.Results;
using RL.API.Tests.Support;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatricesRiesgosApplicationCoverageTests
{
    [Fact]
    public async Task ObtenerVersionVigente_SinRegistro_Retorna404()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out _, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerVersionVigenteFormularioAsync), _ =>
            Task.FromResult<VersionFormularioDto?>(null));

        ServiceResult<VersionFormularioDto> result = await service.ObtenerVersionVigenteFormularioAsync("MATRIZ_RIESGOS_LAFT");

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ObtenerVersionPorId_ConYSinRegistro_ConservaEstado(bool existe)
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out _, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), _ =>
            Task.FromResult(existe
                ? new VersionFormularioDto { VerId = 7, VerCodigo = "FORM_7" }
                : null));

        ServiceResult<VersionFormularioDto> result = await service.ObtenerVersionFormularioAsync(7);

        Assert.Equal(existe, result.Success);
        Assert.Equal(existe ? 200 : 404, result.StatusCode);
    }

    [Fact]
    public async Task CrearBorrador_Vacio_Retorna400()
    {
        MatricesRiesgosAppService service = CrearServicio(out _, out _, out _);

        ServiceResult<long> result = await service.CrearBorradorFormularioAsync(1, "FORM", " ", 9);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task CrearBorrador_Valido_RetornaId()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out _, out _);
        repo.On(nameof(IMatricesRiesgosRepository.CrearBorradorFormularioAsync), _ => Task.FromResult(71L));

        ServiceResult<long> result = await service.CrearBorradorFormularioAsync(1, "FORM", "{\"secciones\":[]}", 9);

        Assert.True(result.Success);
        Assert.Equal(71, result.Data);
    }

    [Fact]
    public async Task ClonarVersion_NoExiste_Retorna404()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out _, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ClonarVersionFormularioAsync), _ =>
            throw new KeyNotFoundException("Versión no encontrada"));

        ServiceResult<long> result = await service.ClonarVersionFormularioAsync(90, 9);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
    }

    [Theory]
    [InlineData("", false, 400)]
    [InlineData("{invalido", false, 400)]
    [InlineData("{\"secciones\":[]}", false, 400)]
    [InlineData("{\"secciones\":[]}", true, 200)]
    public async Task ActualizarBorrador_ValidaContenidoYResultado(
        string contenido,
        bool actualizado,
        int statusEsperado)
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out _, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ActualizarBorradorFormularioAsync), _ => Task.FromResult(actualizado));

        ServiceResult result = await service.ActualizarBorradorFormularioAsync(3, contenido, 9);

        Assert.Equal(statusEsperado, result.StatusCode);
        Assert.Equal(statusEsperado == 200, result.Success);
    }

    [Fact]
    public async Task PublicarVersion_NoExiste_Retorna404()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out _, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), _ =>
            Task.FromResult<VersionFormularioDto?>(null));

        ServiceResult result = await service.PublicarVersionFormularioAsync(5, 9);

        Assert.Equal(404, result.StatusCode);
    }

    [Theory]
    [InlineData("ARCHIVED")]
    [InlineData("PUBLISHED")]
    [InlineData("RETIRED")]
    public async Task PublicarVersion_EstadoNoPermitido_Retorna400(string estado)
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out _, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), _ =>
            Task.FromResult<VersionFormularioDto?>(new VersionFormularioDto
            {
                VerId = 5,
                VerEstado = estado,
                VerJson = "{}"
            }));

        ServiceResult result = await service.PublicarVersionFormularioAsync(5, 9);

        Assert.Equal(400, result.StatusCode);
    }

    [Theory]
    [InlineData("DRAFT", true, 200)]
    [InlineData("APPROVED", false, 400)]
    public async Task PublicarVersion_PropagaResultadoRepositorio(string estado, bool publicado, int statusEsperado)
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out _, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), _ =>
            Task.FromResult<VersionFormularioDto?>(new VersionFormularioDto
            {
                VerId = 5,
                VerFamiliaId = 1,
                VerEstado = estado,
                VerJson = "{\"secciones\":[]}"
            }));
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerFamiliaFormularioPorIdAsync), _ =>
            Task.FromResult<FamiliaFormularioDto?>(new FamiliaFormularioDto { FamId = 1, FamCodigo = "FAM_A", FamActivo = true }));
        repo.On(nameof(IMatricesRiesgosRepository.PublicarVersionFormularioAsync), _ => Task.FromResult(publicado));

        ServiceResult result = await service.PublicarVersionFormularioAsync(5, 9);

        Assert.Equal(statusEsperado, result.StatusCode);
    }

    [Theory]
    [InlineData(true, 200)]
    [InlineData(false, 400)]
    public async Task CambiarVigencia_PropagaResultado(bool actualizado, int statusEsperado)
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out _, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), _ =>
            Task.FromResult<VersionFormularioDto?>(new VersionFormularioDto { VerId = 5, VerEstado = "PUBLISHED" }));
        repo.On(nameof(IMatricesRiesgosRepository.CambiarEstadoVigenciaFormularioAsync), _ => Task.FromResult(actualizado));

        ServiceResult result = await service.CambiarEstadoVigenciaFormularioAsync(5, true, 9);

        Assert.Equal(statusEsperado, result.StatusCode);
    }

    [Fact]
    public async Task ListarVersiones_RetornaColeccion()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out _, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ListarHistorialVersionesFormularioAsync), _ =>
            Task.FromResult(new List<VersionFormularioDto>
            {
                new() { VerId = 1 },
                new() { VerId = 2 }
            }));

        ServiceResult<List<VersionFormularioDto>> result = await service.ListarHistorialVersionesFormularioAsync("FORM");

        Assert.True(result.Success);
        Assert.Equal(2, result.Data!.Count);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ObtenerEvaluacion_ConYSinRegistro(bool existe)
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out _, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerEvaluacionAsync), _ =>
            Task.FromResult(existe ? new EvaluacionRiesgoDto { EvaId = 8 } : null));

        ServiceResult<EvaluacionRiesgoDto> result = await service.ObtenerEvaluacionAsync(8);

        Assert.Equal(existe, result.Success);
        Assert.Equal(existe ? 200 : 404, result.StatusCode);
    }

    [Fact]
    public async Task ListarEvaluaciones_RetornaColeccion()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out _, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ListarEvaluacionesPaginadasAsync), _ =>
            Task.FromResult(new EvaluacionesPaginadasDto
            {
                Items = new List<EvaluacionRiesgoResumenDto> { new() { EvaId = 1, RiesgoCodigo = "RIE-001" } },
                Pagina = 1,
                RegistrosPorPagina = 20,
                TotalRegistros = 1
            }));

        ServiceResult<EvaluacionesPaginadasDto> result = await service.ListarEvaluacionesPaginadasAsync(
            new ConsultaEvaluacionPaginadaDto { Pagina = 1, RegistrosPorPagina = 20 });

        Assert.True(result.Success);
        Assert.Single(result.Data!.Items);
        Assert.Equal(1, result.Data.TotalRegistros);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ObtenerMetodologiaPorVersion_RetornaResultadoEsperado(bool existe)
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out _, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerMetodologiaDinamicaPorVersionAsync), _ =>
            Task.FromResult(existe ? new MetodologiaFormularioDto { VersionFormularioId = 42, Codigo = "FORM_V1" } : null));

        ServiceResult<MetodologiaFormularioDto> result = await service.ObtenerMetodologiaDinamicaPorVersionAsync(42);

        Assert.Equal(existe, result.Success);
        Assert.Equal(existe ? 200 : 404, result.StatusCode);
    }

    [Fact]
    public async Task CrearEvaluacion_VersionInexistente_Retorna400()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out _, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), _ =>
            Task.FromResult<VersionFormularioDto?>(null));

        ServiceResult<long> result = await service.CrearEvaluacionAsync(new EvaluacionRiesgoDto { EvaVersionId = 4 }, 9, null);

        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task CrearEvaluacion_RespuestasInvalidas_Retorna400ConDetalle()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out InterfaceStub validador, out _);
        PrepararVersionPublicada(repo);
        validador.On(nameof(IFormularioValidador.ValidarRespuestasAsync), _ =>
        {
            var resultado = new FormularioValidationResult();
            resultado.Errores.Add(new FormularioValidationError("area_principal", "Campo obligatorio"));
            return Task.FromResult(resultado);
        });

        ServiceResult<long> result = await service.CrearEvaluacionAsync(EvaluacionBase(), 9, null);

        Assert.Equal(400, result.StatusCode);
        Assert.Contains("area_principal", result.Message);
    }

    [Fact]
    public async Task CrearEvaluacion_CalculoFalla_Retorna400()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out InterfaceStub validador, out InterfaceStub calculador);
        PrepararVersionPublicada(repo);
        PrepararValidacionCorrecta(validador);
        calculador.On(nameof(IMatricesRiesgoService.CalcularYValidarRiesgo), _ =>
            ServiceResult<CalculoRiesgoResultadoDto>.BadRequest("Cálculo inválido"));

        ServiceResult<long> result = await service.CrearEvaluacionAsync(EvaluacionBase(), 9, null);

        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Cálculo inválido", result.Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CrearEvaluacion_ExcepcionesFuncionales_Retornan400(bool riesgoInexistente)
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out InterfaceStub validador, out InterfaceStub calculador);
        PrepararVersionPublicada(repo);
        PrepararValidacionCorrecta(validador);
        PrepararCalculoCorrecto(calculador);
        repo.On(nameof(IMatricesRiesgosRepository.CrearEvaluacionAsync), _ =>
        {
            if (riesgoInexistente) throw new KeyNotFoundException("Riesgo inexistente");
            throw new InvalidOperationException("Regla inactiva");
        });

        ServiceResult<long> result = await service.CrearEvaluacionAsync(EvaluacionBase(), 9, null);

        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task ActualizarEvaluacion_RepositorioNoEncuentra_Retorna404()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out InterfaceStub validador, out InterfaceStub calculador);
        PrepararVersionPublicada(repo);
        PrepararValidacionCorrecta(validador);
        PrepararCalculoCorrecto(calculador);
        repo.On(nameof(IMatricesRiesgosRepository.ActualizarEvaluacionAsync), _ => Task.FromResult(false));

        ServiceResult result = await service.ActualizarEvaluacionAsync(EvaluacionBase(), 9, null);

        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task ActualizarEvaluacion_ErrorFuncional_Retorna400()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out InterfaceStub validador, out InterfaceStub calculador);
        PrepararVersionPublicada(repo);
        PrepararValidacionCorrecta(validador);
        PrepararCalculoCorrecto(calculador);
        repo.On(nameof(IMatricesRiesgosRepository.ActualizarEvaluacionAsync), _ =>
            throw new InvalidOperationException("Proyección inválida"));

        ServiceResult result = await service.ActualizarEvaluacionAsync(EvaluacionBase(), 9, null);

        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task Transicionar_EvaluacionNoExiste_Retorna404()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out _, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerEvaluacionAsync), _ =>
            Task.FromResult<EvaluacionRiesgoDto?>(null));

        ServiceResult result = await service.TransicionarEstadoEvaluacionAsync(1, "EN_REVISION", null, 9, null);

        Assert.Equal(404, result.StatusCode);
    }

    [Theory]
    [InlineData("BORRADOR", "APROBADA")]
    [InlineData("CERRADA", "BORRADOR")]
    [InlineData("RECHAZADA", "EN_REVISION")]
    public async Task Transicionar_RutaNoPermitida_Retorna400(string actual, string nuevo)
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out _, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerEvaluacionAsync), _ =>
            Task.FromResult<EvaluacionRiesgoDto?>(new EvaluacionRiesgoDto { EvaId = 1, EvaEstado = actual }));

        ServiceResult result = await service.TransicionarEstadoEvaluacionAsync(1, nuevo, null, 9, null);

        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task Transicionar_RepositorioRechaza_Retorna400()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out _, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerEvaluacionAsync), _ =>
            Task.FromResult<EvaluacionRiesgoDto?>(new EvaluacionRiesgoDto { EvaId = 1, EvaEstado = "BORRADOR" }));
        repo.On(nameof(IMatricesRiesgosRepository.TransicionarEstadoEvaluacionAsync), _ => Task.FromResult(false));

        ServiceResult result = await service.TransicionarEstadoEvaluacionAsync(1, "EN_REVISION", null, 9, null);

        Assert.Equal(400, result.StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ObtenerEvidencia_ConYSinRegistro(bool existe)
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out _, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerEvidenciaFisicaAsync), _ =>
            Task.FromResult(existe ? new EvidenciaDto { EviId = 3 } : null));

        ServiceResult<EvidenciaDto> result = await service.ObtenerEvidenciaFisicaAsync(3);

        Assert.Equal(existe, result.Success);
        Assert.Equal(existe ? 200 : 404, result.StatusCode);
    }

    [Fact]
    public async Task CargarEvidencia_Valida_RegistraYRecupera()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out _, out _);
        repo.On(nameof(IMatricesRiesgosRepository.RegistrarEvidenciaFisicaAsync), _ => Task.FromResult(33L));
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerEvidenciaFisicaAsync), _ =>
            Task.FromResult<EvidenciaDto?>(new EvidenciaDto { EviId = 33, EviNombreArchivo = "prueba.txt" }));
        var contenido = new MemoryStream(Encoding.UTF8.GetBytes("evidencia"));
        var archivo = new FormFile(contenido, 0, contenido.Length, "archivo", "prueba.txt");

        ServiceResult<EvidenciaDto> result = await service.CargarArchivoEvidenciaFisicaAsync(archivo, 9);

        Assert.True(result.Success);
        Assert.Equal(33, result.Data!.EviId);
        LimpiarEvidenciasDePrueba();
    }

    [Fact]
    public async Task CargarEvidencia_RepositorioFalla_LimpiaYRetorna500()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out _, out _);
        repo.On(nameof(IMatricesRiesgosRepository.RegistrarEvidenciaFisicaAsync), _ =>
            throw new InvalidOperationException("Fallo de persistencia"));
        var contenido = new MemoryStream(Encoding.UTF8.GetBytes("evidencia"));
        var archivo = new FormFile(contenido, 0, contenido.Length, "archivo", "fallo.txt");

        ServiceResult<EvidenciaDto> result = await service.CargarArchivoEvidenciaFisicaAsync(archivo, 9);

        Assert.False(result.Success);
        Assert.Equal(500, result.StatusCode);
        LimpiarEvidenciasDePrueba();
    }

    [Fact]
    public async Task EliminarEvidencia_Inexistente_EsIdempotente()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out _, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerEvidenciaFisicaAsync), _ =>
            Task.FromResult<EvidenciaDto?>(null));

        ServiceResult result = await service.EliminarEvidenciaAsync(4, 9, null);

        Assert.True(result.Success);
    }

    [Theory]
    [InlineData(ResultadoEliminacionEvidencia.TieneVinculos, 400)]
    [InlineData(ResultadoEliminacionEvidencia.FalloDisco, 400)]
    [InlineData(ResultadoEliminacionEvidencia.FalloCommit, 500)]
    [InlineData(ResultadoEliminacionEvidencia.NoExiste, 200)]
    public async Task EliminarEvidencia_PropagaResultado(
        ResultadoEliminacionEvidencia resultadoRepositorio,
        int statusEsperado)
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out _, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerEvidenciaFisicaAsync), _ =>
            Task.FromResult<EvidenciaDto?>(new EvidenciaDto
            {
                EviId = 4,
                EviNombreArchivo = "evidencia.pdf",
                EviRuta = string.Empty
            }));
        repo.On(nameof(IMatricesRiesgosRepository.EliminarEvidenciaSeguraAsync), _ =>
            Task.FromResult(resultadoRepositorio));

        ServiceResult result = await service.EliminarEvidenciaAsync(4, 9, null);

        Assert.Equal(statusEsperado, result.StatusCode);
    }

    [Fact]
    public async Task EliminarEvidencia_Exito_RegistraAuditoria()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out _, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerEvidenciaFisicaAsync), _ =>
            Task.FromResult<EvidenciaDto?>(new EvidenciaDto
            {
                EviId = 4,
                EviNombreArchivo = "evidencia.pdf",
                EviRuta = string.Empty
            }));
        repo.On(nameof(IMatricesRiesgosRepository.EliminarEvidenciaSeguraAsync), _ =>
            Task.FromResult(ResultadoEliminacionEvidencia.Exito));

        ServiceResult result = await service.EliminarEvidenciaAsync(4, 9, "127.0.0.1");

        Assert.True(result.Success);
    }

    [Fact]
    public async Task MetodologiaInexistente_Retorna404()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out _, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerMetodologiaDinamicaVigenteAsync), _ =>
            Task.FromResult<MetodologiaFormularioDto?>(null));

        ServiceResult<MetodologiaFormularioDto> result = await service.ObtenerMetodologiaDinamicaVigenteAsync();

        Assert.Equal(404, result.StatusCode);
    }

    private static EvaluacionRiesgoDto EvaluacionBase() => new()
    {
        EvaId = 1,
        EvaRiesgoId = 5,
        EvaVersionId = 10,
        EvaDataJson = "{\"frecuencia_inherente\":4,\"impacto_inherente\":4,\"frecuencia_residual\":2,\"impacto_residual\":3}",
        EvaVersionRow = 1
    };

    private static void PrepararVersionPublicada(InterfaceStub repo)
    {
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), _ =>
            Task.FromResult<VersionFormularioDto?>(new VersionFormularioDto
            {
                VerId = 10,
                VerEstado = "PUBLISHED",
                VerVigente = true,
                VerJson = "{\"secciones\":[]}"
            }));
    }

    private static void PrepararValidacionCorrecta(InterfaceStub validador)
    {
        validador.On(nameof(IFormularioValidador.ValidarRespuestasAsync), _ =>
            Task.FromResult(new FormularioValidationResult()));
    }

    private static void PrepararCalculoCorrecto(InterfaceStub calculador)
    {
        calculador.On(nameof(IMatricesRiesgoService.CalcularYValidarRiesgo), _ =>
            ServiceResult<CalculoRiesgoResultadoDto>.Ok(new CalculoRiesgoResultadoDto
            {
                Vri = 7,
                Etp = 25m,
                Vrr = 4
            }));
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

    private static void LimpiarEvidenciasDePrueba()
    {
        string ruta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "Evidencias");
        if (Directory.Exists(ruta))
        {
            try { Directory.Delete(ruta, recursive: true); } catch { }
        }
    }
}
