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
    public async Task Obtener_IdInvalido_NoConsultaRepositorio()
    {
        var service = CrearServicio(out var repo, out _);

        var result = await service.ObtenerAsync(0);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Empty(repo.CallsTo(nameof(IMatricesRiesgosRepository.ObtenerMatrizAsync)));
    }

    [Fact]
    public async Task Crear_DatosValidos_NormalizaYRetornaDetalle()
    {
        var service = CrearServicio(out var repo, out _);
        var detalle = new MatrizRiesgoDetalleDto { MatrizId = 21, NombreSujeto = "Proveedor Uno" };
        repo.On(nameof(IMatricesRiesgosRepository.CrearMatrizAsync), _ => Task.FromResult(21L));
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerMatrizAsync), _ => Task.FromResult<MatrizRiesgoDetalleDto?>(detalle));
        var dto = CrearMatrizValida();
        dto.SujetoTipo = " proveedor ";
        dto.NombreSujeto = " Proveedor Uno ";

        var result = await service.CrearAsync(dto, 7, "analista@ihss.hn", "127.0.0.1");

        Assert.True(result.Success);
        Assert.Same(detalle, result.Data);
        Assert.Equal("PROVEEDOR", dto.SujetoTipo);
        Assert.Equal("Proveedor Uno", dto.NombreSujeto);
        Assert.Single(repo.CallsTo(nameof(IMatricesRiesgosRepository.CrearMatrizAsync)));
    }

    [Fact]
    public async Task Crear_SinDetalles_RechazaSinEscribir()
    {
        var service = CrearServicio(out var repo, out _);
        var dto = CrearMatrizValida();
        dto.Detalles.Clear();

        var result = await service.CrearAsync(dto, 7, null, null);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Empty(repo.CallsTo(nameof(IMatricesRiesgosRepository.CrearMatrizAsync)));
    }

    [Fact]
    public async Task Crear_ReglaRepositorioInvalida_ConvierteExcepcionEnBadRequest()
    {
        var service = CrearServicio(out var repo, out _);
        repo.On(nameof(IMatricesRiesgosRepository.CrearMatrizAsync), _ => throw new InvalidOperationException("Matriz duplicada"));

        var result = await service.CrearAsync(CrearMatrizValida(), 7, null, null);

        Assert.False(result.Success);
        Assert.Equal("Matriz duplicada", result.Message);
    }

    [Fact]
    public async Task Actualizar_MatrizInexistente_DevuelveNotFound()
    {
        var service = CrearServicio(out var repo, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ActualizarMatrizAsync), _ => Task.FromResult(false));

        var result = await service.ActualizarAsync(90, CrearMatrizValida(), 7, null, null);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Empty(repo.CallsTo(nameof(IMatricesRiesgosRepository.ObtenerMatrizAsync)));
    }

    [Fact]
    public async Task Recalcular_SinMotivo_RechazaAntesDePrepararSolicitud()
    {
        var service = CrearServicio(out var repo, out _);

        var result = await service.CalcularAsync(12, new MatrizRiesgoCalcularRequestDto
        {
            TipoCalculo = "FACTOR",
            MotivoCalculo = " "
        }, true, 7, null, null);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Empty(repo.CallsTo(nameof(IMatricesRiesgosRepository.PrepararSolicitudCalculoAsync)));
    }

    [Fact]
    public async Task Calcular_ResultadoValido_PersisteYRetornaMotor()
    {
        var service = CrearServicio(out var repo, out var motor);
        var solicitud = new MatrizCalculoRequestDto { TipoCalculo = "FACTOR" };
        var calculo = new MatrizCalculoResultadoDto { PuntajeResidual = 3.5m, NivelResidual = "ALTO" };
        repo.On(nameof(IMatricesRiesgosRepository.PrepararSolicitudCalculoAsync), _ => Task.FromResult<MatrizCalculoRequestDto?>(solicitud));
        repo.On(nameof(IMatricesRiesgosRepository.PersistirResultadoCalculoAsync), _ => Task.CompletedTask);
        motor.On(nameof(IMatricesRiesgoService.Calcular), _ => ServiceResult<MatrizCalculoResultadoDto>.Ok(calculo));

        var result = await service.CalcularAsync(12, new MatrizRiesgoCalcularRequestDto { TipoCalculo = "FACTOR" }, false, 7, "a@ihss.hn", "127.0.0.1");

        Assert.True(result.Success);
        Assert.Same(calculo, result.Data);
        Assert.Single(motor.CallsTo(nameof(IMatricesRiesgoService.Calcular)));
        Assert.Single(repo.CallsTo(nameof(IMatricesRiesgosRepository.PersistirResultadoCalculoAsync)));
    }

    [Fact]
    public async Task CambiarEstado_NormalizaEstadoYDelegaMotivo()
    {
        var service = CrearServicio(out var repo, out _);
        repo.On(nameof(IMatricesRiesgosRepository.CambiarEstadoAsync), _ => Task.FromResult(true));

        var result = await service.CambiarEstadoAsync(15, new MatrizRiesgoCambiarEstadoRequestDto
        {
            Estado = " aprobada ",
            Motivo = "Aprobación del comité"
        }, 7, null, null);

        Assert.True(result.Success);
        var call = Assert.Single(repo.CallsTo(nameof(IMatricesRiesgosRepository.CambiarEstadoAsync)));
        Assert.Equal("APROBADA", call.Arguments[1]);
        Assert.Equal("Aprobación del comité", call.Arguments[2]);
    }

    [Fact]
    public async Task CambiarEstado_EstadoInvalido_NoInvocaRepositorio()
    {
        var service = CrearServicio(out var repo, out _);

        var result = await service.CambiarEstadoAsync(15, new MatrizRiesgoCambiarEstadoRequestDto
        {
            Estado = "PUBLICADA",
            Motivo = "Cambio"
        }, 7, null, null);

        Assert.False(result.Success);
        Assert.Empty(repo.CallsTo(nameof(IMatricesRiesgosRepository.CambiarEstadoAsync)));
    }

    [Theory]
    [InlineData("EXCEL", "application/vnd.ms-excel")]
    [InlineData("PDF", "application/pdf")]
    public async Task ExportarReporte_FormatoValido_GeneraContenidoYRegistraAuditoria(string formato, string contentType)
    {
        var service = CrearServicio(out var repo, out _);
        var reporte = new MatricesRiesgoReporteDto();
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerReporteAsync), _ => Task.FromResult(reporte));
        repo.On(nameof(IMatricesRiesgosRepository.RegistrarExportacionReporteAsync), _ => Task.CompletedTask);
        var filtro = new MatrizRiesgoReporteFiltroDto { Estado = " aprobada ", SujetoTipo = " proveedor " };

        var result = await service.ExportarReporteAsync(filtro, formato, 7, "a@ihss.hn", "127.0.0.1");

        Assert.True(result.Success);
        Assert.Equal(contentType, result.Data!.ContentType);
        Assert.NotEmpty(result.Data.Contenido);
        Assert.Equal("APROBADA", filtro.Estado);
        Assert.Equal("PROVEEDOR", filtro.SujetoTipo);
        var call = Assert.Single(repo.CallsTo(nameof(IMatricesRiesgosRepository.RegistrarExportacionReporteAsync)));
        Assert.Equal(formato, call.Arguments[1]);
    }

    [Fact]
    public async Task ExportarReporte_FormatoInvalido_NoConsultaRepositorio()
    {
        var service = CrearServicio(out var repo, out _);

        var result = await service.ExportarReporteAsync(new MatrizRiesgoReporteFiltroDto(), "CSV", 7, null, null);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Empty(repo.Invocations);
    }

    [Fact]
    public async Task CrearCriterio_Valido_NormalizaYRetornaCreado()
    {
        var service = CrearServicio(out var repo, out _);
        var criterio = new MatrizRiesgoCriterioDto { CriterioId = 33, Descripcion = "Rango alto" };
        repo.On(nameof(IMatricesRiesgosRepository.CrearCriterioAsync), _ => Task.FromResult(33L));
        repo.On(nameof(IMatricesRiesgosRepository.ListarCriteriosAsync), _ => Task.FromResult(new List<MatrizRiesgoCriterioDto> { criterio }));
        var dto = new MatrizRiesgoCriterioRequestDto
        {
            VariableId = 2,
            Puntaje = 4,
            ValorDesde = 10,
            ValorHasta = 20,
            Descripcion = " Rango alto "
        };

        var result = await service.CrearCriterioAsync(dto, 7, null, null);

        Assert.True(result.Success);
        Assert.Same(criterio, result.Data);
        Assert.Equal("Rango alto", dto.Descripcion);
    }

    [Fact]
    public async Task ActualizarCriterio_NoEncontrado_NoListaCatalogo()
    {
        var service = CrearServicio(out var repo, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ActualizarCriterioAsync), _ => Task.FromResult(false));

        var result = await service.ActualizarCriterioAsync(34, new MatrizRiesgoCriterioRequestDto
        {
            VariableId = 2,
            Puntaje = 4,
            Descripcion = "Rango"
        }, 7, null, null);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Empty(repo.CallsTo(nameof(IMatricesRiesgosRepository.ListarCriteriosAsync)));
    }

    [Fact]
    public async Task InactivarCriterio_MotivoValido_RecortaYDelega()
    {
        var service = CrearServicio(out var repo, out _);
        repo.On(nameof(IMatricesRiesgosRepository.InactivarCriterioAsync), _ => Task.FromResult(true));

        var result = await service.InactivarCriterioAsync(35, new MatrizRiesgoInactivarRequestDto
        {
            Motivo = " Fuera de vigencia "
        }, 7, null, null);

        Assert.True(result.Success);
        var call = Assert.Single(repo.CallsTo(nameof(IMatricesRiesgosRepository.InactivarCriterioAsync)));
        Assert.Equal("Fuera de vigencia", call.Arguments[1]);
    }

    private static MatricesRiesgosAppService CrearServicio(out InterfaceStub repoStub, out InterfaceStub motorStub)
    {
        var repo = InterfaceStub.Create<IMatricesRiesgosRepository>(out repoStub);
        var motor = InterfaceStub.Create<IMatricesRiesgoService>(out motorStub);
        return new MatricesRiesgosAppService(repo, motor);
    }

    private static MatrizRiesgoCrearRequestDto CrearMatrizValida() => new()
    {
        SujetoTipo = "PROVEEDOR",
        NombreSujeto = "Proveedor",
        OrigenDatos = "CAPTURA",
        Detalles = new List<MatrizRiesgoDetalleRequestDto>
        {
            new() { VariableId = 1, Puntaje = 3 }
        }
    };
}
